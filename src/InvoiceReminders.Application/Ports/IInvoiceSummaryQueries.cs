using InvoiceReminders.Application.Invoices;

namespace InvoiceReminders.Application.Ports;

/// <summary>Lecturas para la interfaz gráfica; separadas del repositorio que usa el proceso (ISP).</summary>
public interface IInvoiceSummaryQueries
{
    Task<InvoiceSummaryDto> GetSummaryAsync(InvoiceFilter filter, CancellationToken cancellationToken);
}
