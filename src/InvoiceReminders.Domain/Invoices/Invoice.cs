namespace InvoiceReminders.Domain.Invoices;

/// <summary>Factura de un cliente. <see cref="Id"/> es el número de factura (ADR 0005).</summary>
public sealed record Invoice(
    string Id,
    string CustomerId,
    decimal Amount,
    string Currency,
    DateOnly IssuedOn,
    DateOnly DueOn,
    InvoiceStatus Status,
    DateTimeOffset UpdatedAt);
