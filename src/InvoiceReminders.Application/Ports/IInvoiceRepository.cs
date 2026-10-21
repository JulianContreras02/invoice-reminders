using InvoiceReminders.Application.Reminders;
using InvoiceReminders.Domain.Invoices;

namespace InvoiceReminders.Application.Ports;

/// <summary>
/// Lo que el proceso de recordatorios necesita de la persistencia de facturas, y nada más.
/// No existe un "actualizar estado" incondicional: el único cambio posible es el avance
/// condicionado al estado anterior (compare-and-set, ADR 0004).
/// </summary>
public interface IInvoiceRepository
{
    Task<IReadOnlyList<Invoice>> GetEligibleByCustomerAsync(
        string customerId,
        IReadOnlyCollection<InvoiceStatus> statuses,
        CancellationToken cancellationToken);

    /// <returns><c>true</c> si la factura seguía en <paramref name="expectedCurrent"/> y avanzó; <c>false</c> si otro actor la movió antes.</returns>
    Task<bool> TryAdvanceStatusAsync(
        string invoiceId,
        InvoiceStatus expectedCurrent,
        InvoiceStatus next,
        StatusChange change,
        CancellationToken cancellationToken);
}
