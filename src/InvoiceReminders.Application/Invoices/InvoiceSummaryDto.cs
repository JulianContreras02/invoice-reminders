namespace InvoiceReminders.Application.Invoices;

public sealed record InvoiceSummaryDto(
    IReadOnlyList<InvoiceRowDto> Invoices,
    IReadOnlyDictionary<string, int> CountsByStatus,
    IReadOnlyList<CustomerSummaryDto> Customers,
    int Total);

public sealed record InvoiceRowDto(
    string Id,
    string CustomerId,
    string CustomerName,
    string CustomerEmail,
    decimal Amount,
    string Currency,
    DateOnly IssuedOn,
    DateOnly DueOn,
    string Status,
    DateTimeOffset UpdatedAt);

public sealed record CustomerSummaryDto(
    string Id,
    string Name,
    string Email,
    IReadOnlyDictionary<string, int> CountsByStatus,
    int Total);
