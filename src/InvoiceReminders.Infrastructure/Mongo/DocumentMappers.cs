using InvoiceReminders.Domain.Customers;
using InvoiceReminders.Domain.Invoices;

namespace InvoiceReminders.Infrastructure.Mongo;

/// <summary>Frontera de traducción entre el contrato de MongoDB y el modelo de dominio.</summary>
internal static class DocumentMappers
{
    public static Customer ToDomain(this CustomerDocument document) =>
        new(document.Id, document.Name, document.Email);

    public static Invoice ToDomain(this InvoiceDocument document) =>
        new(
            document.Id,
            document.CustomerId,
            document.Amount,
            document.Currency,
            DateOnly.FromDateTime(document.IssuedOn),
            DateOnly.FromDateTime(document.DueOn),
            InvoiceStatus.From(document.Status ?? string.Empty),
            new DateTimeOffset(DateTime.SpecifyKind(document.UpdatedAt, DateTimeKind.Utc)));

    public static CustomerDocument ToDocument(this Customer customer, DateTime createdAtUtc) =>
        new()
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            CreatedAt = createdAtUtc,
        };

    public static InvoiceDocument ToDocument(this Invoice invoice) =>
        new()
        {
            Id = invoice.Id,
            CustomerId = invoice.CustomerId,
            Amount = invoice.Amount,
            Currency = invoice.Currency,
            IssuedOn = invoice.IssuedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            DueOn = invoice.DueOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            Status = invoice.Status.Value,
            UpdatedAt = invoice.UpdatedAt.UtcDateTime,
        };
}
