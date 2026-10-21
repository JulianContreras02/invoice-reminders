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
    private const string EmailFailedMessage = "El correo no se entregó; el estado no cambió.";
    private const string UpdateFailedMessage = "El correo salió, pero no se pudo guardar el nuevo estado; la siguiente ejecución lo reintentará.";

    /// <param name="customerId">Cliente a procesar, o <c>null</c> para todos.</param>
    /// <param name="cancellationToken">
    /// Apagado de la aplicación. Se atiende solo entre facturas y entre clientes: una factura que
    /// empezó a procesarse termina su envío y su actualización (el SMTP tiene su propio tiempo límite).
    /// </param>
    public async Task<ReminderRunResult> RunAsync(string? customerId, CancellationToken cancellationToken)
    {
        var runId = Guid.CreateVersion7();
        var startedAt = timeProvider.GetUtcNow();
        using var _ = logger.BeginScope(new Dictionary<string, object> { ["RunId"] = runId });

        var targets = await LoadTargetsAsync(customerId, cancellationToken);
        var outcomes = new List<InvoiceOutcome>();
        var customersProcessed = 0;
        var completion = RunCompletion.Completed;

        foreach (var customer in targets)
        {
            var (eligible, readCompletion) = await ReadEligibleAsync(customer, cancellationToken);
            if (readCompletion != RunCompletion.Completed)
            {
                completion = readCompletion;
                break;
            }

            customersProcessed++;
            completion = await ProcessCustomerAsync(customer, eligible, runId, outcomes, cancellationToken);
            if (completion != RunCompletion.Completed)
            {
                break;
            }
        }

        var result = new ReminderRunResult(runId, startedAt, timeProvider.GetUtcNow() - startedAt, customerId, customersProcessed, completion, outcomes);
        logger.LogInformation(
            "Recordatorios {RunId} ({Completion}): {Candidates} candidatas, {Sent} enviados, {Updated} actualizadas, {Failed} fallidas, {Skipped} omitidas en {Duration}",
            runId, completion, result.Candidates, result.EmailsSent, result.Updated, result.Failed, result.Skipped, result.Duration);
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

    private async Task<(IReadOnlyList<Invoice> Eligible, RunCompletion Completion)> ReadEligibleAsync(
        Customer customer,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ([], RunCompletion.Cancelled);
        }

        try
        {
            var eligible = await invoices.GetEligibleByCustomerAsync(customer.Id, ReminderPolicy.EligibleStatuses, cancellationToken);
            return (eligible, RunCompletion.Completed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return ([], RunCompletion.Cancelled);
        }
#pragma warning disable CA1031 // El adaptador puede lanzar excepciones del driver que Application no conoce; se devuelve el resultado parcial.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(ex, "No se pudieron leer las facturas del cliente {CustomerId}; el lote se detiene", customer.Id);
            return ([], RunCompletion.StoppedPersistenceUnavailable);
        }
    }

    private async Task<RunCompletion> ProcessCustomerAsync(
        Customer customer,
        IReadOnlyList<Invoice> eligible,
        Guid runId,
        List<InvoiceOutcome> outcomes,
        CancellationToken cancellationToken)
    {
        foreach (var invoice in eligible)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return RunCompletion.Cancelled;
            }

            if (!ReminderPolicy.TryGetTransition(invoice.Status, out var transition))
            {
                // Imposible si el repositorio cumple su contrato (filtra por EligibleStatuses).
                logger.LogError("El repositorio devolvió la factura {InvoiceId} en el estado no elegible {Status}", invoice.Id, invoice.Status);
                continue;
            }

            var outcome = await ProcessInvoiceAsync(customer, invoice, transition, runId);
            outcomes.Add(outcome);

            if (outcome.Kind == OutcomeKind.UpdateFailedAfterSend)
            {
                // Con la persistencia caída, cada correo adicional sería un duplicado seguro.
                return RunCompletion.StoppedPersistenceUnavailable;
            }
        }

        return RunCompletion.Completed;
    }

    /// <summary>Una factura es una unidad de trabajo: no se cancela entre el envío y la actualización.</summary>
    private async Task<InvoiceOutcome> ProcessInvoiceAsync(Customer customer, Invoice invoice, ReminderTransition transition, Guid runId)
    {
        if (!customer.HasDeliverableEmail())
        {
            logger.LogWarning("Factura {InvoiceId} omitida: el cliente {CustomerId} no tiene un correo entregable", invoice.Id, customer.Id);
            return Outcome(customer, invoice, OutcomeKind.SkippedNoEmail, error: "El cliente no tiene un correo válido.");
        }

        var message = composer.Compose(customer, invoice, transition, runId);

        try
        {
            await emailSender.SendAsync(message, CancellationToken.None);
        }
        catch (EmailDeliveryException ex)
        {
            logger.LogWarning(ex, "No se envió el correo de la factura {InvoiceId} del cliente {CustomerId}; el estado no cambia", invoice.Id, customer.Id);
            return Outcome(customer, invoice, OutcomeKind.EmailFailed, error: EmailFailedMessage);
        }

        var change = new StatusChange(runId, message.MessageId, timeProvider.GetUtcNow(), StatusChange.ReminderReason);
        try
        {
            var advanced = await invoices.TryAdvanceStatusAsync(invoice.Id, transition.From, transition.To, change, CancellationToken.None);
            if (!advanced)
            {
                logger.LogWarning("La factura {InvoiceId} cambió de estado durante la ejecución; no se actualizó", invoice.Id);
                return Outcome(customer, invoice, OutcomeKind.UpdateConflict, messageId: message.MessageId, error: "Otro proceso cambió el estado antes.");
            }
        }
#pragma warning disable CA1031 // El adaptador de persistencia puede lanzar excepciones del driver que Application no conoce.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(ex, "El correo {MessageId} de la factura {InvoiceId} salió pero el estado no se actualizó", message.MessageId, invoice.Id);
            return Outcome(customer, invoice, OutcomeKind.UpdateFailedAfterSend, messageId: message.MessageId, error: UpdateFailedMessage);
        }

        return Outcome(customer, invoice, OutcomeKind.SentAndUpdated, newStatus: transition.To.Value, messageId: message.MessageId);
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
