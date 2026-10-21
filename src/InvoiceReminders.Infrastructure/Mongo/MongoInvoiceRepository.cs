using InvoiceReminders.Application.Ports;
using InvoiceReminders.Application.Reminders;
using InvoiceReminders.Domain.Invoices;
using MongoDB.Driver;

namespace InvoiceReminders.Infrastructure.Mongo;

internal sealed class MongoInvoiceRepository(MongoContext context) : IInvoiceRepository
{
    private static readonly FilterDefinitionBuilder<InvoiceDocument> Filter = Builders<InvoiceDocument>.Filter;

    public async Task<IReadOnlyList<Invoice>> GetEligibleByCustomerAsync(
        string customerId,
        IReadOnlyCollection<InvoiceStatus> statuses,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(statuses);

        var filter = Filter.Eq(document => document.CustomerId, customerId)
            & Filter.In(document => document.Status, statuses.Select(status => status.Value));

        var documents = await context.Invoices
            .Find(filter)
            .SortBy(document => document.Id)
            .ToListAsync(cancellationToken);

        return [.. documents.Select(document => document.ToDomain())];
    }

    /// <summary>
    /// Compare-and-set sobre un único documento: el filtro incluye el estado anterior, así que
    /// una factura nunca avanza dos veces aunque dos ejecuciones compitan. $set y $push son atómicos.
    /// </summary>
    public async Task<bool> TryAdvanceStatusAsync(
        string invoiceId,
        InvoiceStatus expectedCurrent,
        InvoiceStatus next,
        StatusChange change,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(change);

        var filter = Filter.Eq(document => document.Id, invoiceId)
            & Filter.Eq(document => document.Status, expectedCurrent.Value);

        var history = new StatusChangeDocument
        {
            From = expectedCurrent.Value,
            To = next.Value,
            At = change.At.UtcDateTime,
            Reason = change.Reason,
            RunId = change.RunId.ToString("D"),
            MessageId = change.MessageId,
        };

        var update = Builders<InvoiceDocument>.Update
            .Set(document => document.Status, next.Value)
            .Set(document => document.UpdatedAt, change.At.UtcDateTime)
            .Push(document => document.StatusHistory, history);

        var result = await context.Invoices.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }
}
