using InvoiceReminders.Application.Invoices;
using InvoiceReminders.Application.Ports;
using MongoDB.Driver;

namespace InvoiceReminders.Infrastructure.Mongo;

/// <summary>
/// Lecturas para la interfaz: filas filtradas y conteos por estado y por cliente.
/// Los conteos se agregan en MongoDB ($group por cliente y estado) y siempre cubren todas
/// las facturas, aunque la tabla esté filtrada.
/// </summary>
internal sealed class MongoInvoiceSummaryQueries(MongoContext context) : IInvoiceSummaryQueries
{
    public async Task<InvoiceSummaryDto> GetSummaryAsync(InvoiceFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var customers = await context.Customers
            .Find(FilterDefinition<CustomerDocument>.Empty)
            .SortBy(customer => customer.Id)
            .ToListAsync(cancellationToken);

        var aggregated = await context.Invoices.Aggregate()
            .Group(
                invoice => new { invoice.CustomerId, invoice.Status },
                group => new { group.Key.CustomerId, group.Key.Status, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var groups = aggregated.Select(group => new StatusCount(group.CustomerId, group.Status, group.Count)).ToArray();

        var invoices = await context.Invoices
            .Find(BuildFilter(filter))
            .SortBy(invoice => invoice.CustomerId)
            .ThenBy(invoice => invoice.Id)
            .ToListAsync(cancellationToken);

        var customersById = customers.ToDictionary(customer => customer.Id, StringComparer.Ordinal);
        var rows = invoices.Select(invoice => ToRow(invoice, customersById)).ToArray();

        var customerSummaries = customers
            .Select(customer =>
            {
                var own = groups.Where(group => group.CustomerId == customer.Id).ToArray();
                return new CustomerSummaryDto(customer.Id, customer.Name, customer.Email, CountByStatus(own), own.Sum(group => group.Count));
            })
            .ToArray();

        return new InvoiceSummaryDto(rows, CountByStatus(groups), customerSummaries, groups.Sum(group => group.Count));
    }

    private static FilterDefinition<InvoiceDocument> BuildFilter(InvoiceFilter filter)
    {
        var builder = Builders<InvoiceDocument>.Filter;
        var result = builder.Empty;

        if (!string.IsNullOrWhiteSpace(filter.CustomerId))
        {
            result &= builder.Eq(invoice => invoice.CustomerId, filter.CustomerId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            result &= builder.Eq(invoice => invoice.Status, filter.Status);
        }

        return result;
    }

    private static InvoiceRowDto ToRow(InvoiceDocument document, Dictionary<string, CustomerDocument> customersById)
    {
        customersById.TryGetValue(document.CustomerId, out var customer);
        var invoice = document.ToDomain();

        return new InvoiceRowDto(
            invoice.Id,
            invoice.CustomerId,
            customer?.Name ?? "(cliente desconocido)",
            customer?.Email ?? string.Empty,
            invoice.Amount,
            invoice.Currency,
            invoice.IssuedOn,
            invoice.DueOn,
            invoice.Status.Value,
            invoice.UpdatedAt);
    }

    private static Dictionary<string, int> CountByStatus(IEnumerable<StatusCount> groups) =>
        groups
            .GroupBy(group => group.Status, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Count), StringComparer.Ordinal);

    private sealed record StatusCount(string CustomerId, string Status, int Count);
}
