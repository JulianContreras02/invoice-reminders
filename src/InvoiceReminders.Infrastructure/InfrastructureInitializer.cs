using InvoiceReminders.Infrastructure.Mongo;
using InvoiceReminders.Infrastructure.Options;
using InvoiceReminders.Infrastructure.Seed;
using Microsoft.Extensions.Options;

namespace InvoiceReminders.Infrastructure;

/// <summary>
/// Orden de arranque: esperar MongoDB → índices → semilla según configuración. Todas sus
/// dependencias llegan por constructor; la Api solo resuelve esta clase.
/// </summary>
internal sealed class InfrastructureInitializer(
    MongoStartupWaiter waiter,
    MongoIndexInitializer indexes,
    IDemoDataSeeder seeder,
    IOptions<SeedOptions> seedOptions) : IInfrastructureInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await waiter.WaitUntilAvailableAsync(cancellationToken);
        await indexes.EnsureIndexesAsync(cancellationToken);
        await seeder.SeedAsync(seedOptions.Value.Mode, cancellationToken);
    }
}
