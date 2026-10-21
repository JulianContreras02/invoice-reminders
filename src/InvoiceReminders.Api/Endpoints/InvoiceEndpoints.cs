using InvoiceReminders.Application.Invoices;
using InvoiceReminders.Application.Ports;
using Microsoft.AspNetCore.Http.HttpResults;

namespace InvoiceReminders.Api.Endpoints;

internal static class InvoiceEndpoints
{
    public static RouteGroupBuilder MapInvoiceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/invoices", GetSummaryAsync)
            .WithName("GetInvoiceSummary")
            .WithSummary("Resumen de facturas: filas filtradas y conteos por estado y por cliente.");
        return group;
    }

    private static async Task<Ok<InvoiceSummaryDto>> GetSummaryAsync(
        string? customerId,
        string? status,
        IInvoiceSummaryQueries queries,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.GetSummaryAsync(new InvoiceFilter(customerId, status), cancellationToken));
}
