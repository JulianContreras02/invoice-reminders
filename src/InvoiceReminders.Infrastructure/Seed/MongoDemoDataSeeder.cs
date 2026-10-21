using InvoiceReminders.Infrastructure.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace InvoiceReminders.Infrastructure.Seed;

internal sealed class MongoDemoDataSeeder(MongoContext context, ILogger<MongoDemoDataSeeder> logger) : IDemoDataSeeder
{
    public async Task<SeedResult> SeedAsync(SeedMode mode, CancellationToken cancellationToken)
    {
        if (mode == SeedMode.Never)
        {
            return NotApplied(mode);
        }

        if (mode == SeedMode.IfEmpty && await HasCustomersAsync(cancellationToken))
        {
            logger.LogInformation("La base ya tiene datos; no se siembra");
            return NotApplied(mode);
        }

        if (mode == SeedMode.Always)
        {
            await ClearAsync(cancellationToken);
        }

        await InsertDemoDataAsync(cancellationToken);
        logger.LogInformation("Semilla aplicada: {Customers} clientes y {Invoices} facturas", DemoData.Customers.Count, DemoData.Invoices.Count);
        return new SeedResult(mode, Applied: true, DemoData.Customers.Count, DemoData.Invoices.Count);
    }

    private static SeedResult NotApplied(SeedMode mode) => new(mode, Applied: false, Customers: 0, Invoices: 0);

    private async Task<bool> HasCustomersAsync(CancellationToken cancellationToken) =>
        await context.Customers.CountDocumentsAsync(FilterDefinition<CustomerDocument>.Empty, cancellationToken: cancellationToken) > 0;

    private async Task ClearAsync(CancellationToken cancellationToken)
    {
        await context.Invoices.DeleteManyAsync(FilterDefinition<InvoiceDocument>.Empty, cancellationToken);
        await context.Customers.DeleteManyAsync(FilterDefinition<CustomerDocument>.Empty, cancellationToken);
    }

    private async Task InsertDemoDataAsync(CancellationToken cancellationToken)
    {
        await context.Customers.InsertManyAsync(
            DemoData.Customers.Select(customer => customer.ToDocument(DemoData.CreatedAtUtc)),
            cancellationToken: cancellationToken);
        await context.Invoices.InsertManyAsync(
            DemoData.Invoices.Select(invoice => invoice.ToDocument()),
            cancellationToken: cancellationToken);
    }
}
