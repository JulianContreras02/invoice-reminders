using System.Net.Mail;
using InvoiceReminders.Application.Common;
using InvoiceReminders.Application.Email;
using InvoiceReminders.Application.Ports;
using InvoiceReminders.Domain.Customers;
using InvoiceReminders.Domain.Invoices;
using InvoiceReminders.Domain.Reminders;
using Microsoft.Extensions.Logging;

namespace InvoiceReminders.Application.Reminders;

/// <summary>
/// Proceso de recordatorios: por cada cliente toma sus facturas en primer o segundo recordatorio,
/// envía el correo correspondiente y, solo si el envío fue aceptado, avanza el estado (ADR 0004).
/// Es la única clase que conoce el orden componer → enviar → actualizar.
/// </summary>
public sealed class ProcessRemindersUseCase(
    ICustomerRepository customers,
    IInvoiceRepository invoices,
    IReminderEmailComposer composer,
    IEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<ProcessRemindersUseCase> logger)
{
    /// <param name="customerId">Cliente a procesar, o <c>null</c> para todos.</param>
    /// <param name="cancellationToken">Se comprueba solo entre facturas: nunca entre el envío y la actualización.</param>
    public async Task<ReminderRunResult> RunAsync(string? customerId, CancellationToken cancellationToken)
    {
        var runId = Guid.CreateVersion7();
        var startedAt = timeProvider.GetUtcNow();
        using var _ = logger.BeginScope(new Dictionary<string, object> { ["RunId"] = runId });

        var targets = await LoadTargetsAsync(customerId, cancellationToken);
        var outcomes = new List<InvoiceOutcome>();
        var customersProcessed = 0;
        var cancelled = false;

        foreach (var customer in targets)
        {
            // Entre clientes también: con el token cancelado la consulta al repositorio lanzaría en lugar de devolver el resultado parcial.
            if (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }

            var eligible = await invoices.GetEligibleByCustomerAsync(customer.Id, ReminderPolicy.EligibleStatuses, cancellationToken);
            customersProcessed++;

            foreach (var invoice in eligible)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancelled = true;
                    break;
                }

                outcomes.Add(await ProcessInvoiceAsync(customer, invoice, runId, cancellationToken));
            }

            if (cancelled)
            {
                break;
            }
        }

        var result = new ReminderRunResult(runId, startedAt, timeProvider.GetUtcNow() - startedAt, customerId, customersProcessed, cancelled, outcomes);
        logger.LogInformation(
            "Recordatorios {RunId}: {Candidates} candidatas, {Sent} enviados, {Updated} actualizadas, {Failed} fallidas, {Skipped} omitidas en {Duration}",
            runId, result.Candidates, result.EmailsSent, result.Updated, result.Failed, result.Skipped, result.Duration);
        return result;
    }

    private async Task<IReadOnlyList<Customer>> LoadTargetsAsync(string? customerId, CancellationToken cancellationToken)
    {
        if (customerId is null)
        {
            var all = await customers.GetAllAsync(cancellationToken);
            return [.. all.OrderBy(customer => customer.Id, StringComparer.Ordinal)];
        }

        var single = await customers.GetByIdAsync(customerId, cancellationToken)
            ?? throw new CustomerNotFoundException(customerId);
        return [single];
    }

    private async Task<InvoiceOutcome> ProcessInvoiceAsync(Customer customer, Invoice invoice, Guid runId, CancellationToken cancellationToken)
    {
        if (!ReminderPolicy.TryGetTransition(invoice.Status, out var transition))
        {
            return Outcome(customer, invoice, OutcomeKind.SkippedNotEligible, error: "El estado no tiene transición definida.");
        }

        if (!MailAddress.TryCreate(customer.Email, out _))
        {
            logger.LogWarning("Factura {InvoiceId} omitida: el cliente {CustomerId} no tiene un correo válido", invoice.Id, customer.Id);
            return Outcome(customer, invoice, OutcomeKind.SkippedNoEmail, error: "El cliente no tiene un correo válido.");
        }

        var message = composer.Compose(customer, invoice, transition, runId);

        EmailDispatchResult dispatch;
        try
        {
            dispatch = await emailSender.SendAsync(message, cancellationToken);
        }
        catch (EmailDeliveryException ex)
        {
            logger.LogWarning(ex, "No se envió el correo de la factura {InvoiceId} del cliente {CustomerId}; el estado no cambia", invoice.Id, customer.Id);
            return Outcome(customer, invoice, OutcomeKind.EmailFailed, error: ex.Message);
        }

        var change = new StatusChange(runId, dispatch.MessageId, timeProvider.GetUtcNow(), StatusChange.ReminderReason);
        try
        {
            // El correo ya salió: la actualización no se cancela aunque la aplicación se esté deteniendo.
            var advanced = await invoices.TryAdvanceStatusAsync(invoice.Id, transition.From, transition.To, change, CancellationToken.None);
            if (!advanced)
            {
                logger.LogWarning("La factura {InvoiceId} cambió de estado durante la ejecución; no se actualizó", invoice.Id);
                return Outcome(customer, invoice, OutcomeKind.UpdateConflict, messageId: dispatch.MessageId, error: "Otro proceso cambió el estado antes.");
            }
        }
#pragma warning disable CA1031 // El adaptador de persistencia puede lanzar excepciones del driver que Application no conoce; el lote no debe abortar.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(ex, "El correo {MessageId} de la factura {InvoiceId} salió pero el estado no se actualizó", dispatch.MessageId, invoice.Id);
            return Outcome(customer, invoice, OutcomeKind.UpdateFailedAfterSend, messageId: dispatch.MessageId, error: ex.Message);
        }

        return Outcome(customer, invoice, OutcomeKind.SentAndUpdated, newStatus: transition.To.Value, messageId: dispatch.MessageId);
    }

    private static InvoiceOutcome Outcome(
        Customer customer,
        Invoice invoice,
        OutcomeKind kind,
        string? newStatus = null,
        string? messageId = null,
        string? error = null) =>
        new(invoice.Id, customer.Id, customer.Name, customer.Email, invoice.Status.Value, newStatus, kind, messageId, error);
}
