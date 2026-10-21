namespace InvoiceReminders.Application.Invoices;

/// <summary>Filtro de la tabla de facturas. Los conteos del resumen siempre cubren todas las facturas.</summary>
public sealed record InvoiceFilter(string? CustomerId, string? Status);
