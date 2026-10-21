using MongoDB.Driver;

namespace InvoiceReminders.Infrastructure.Mongo;

/// <summary>
/// Crea los índices al arrancar. Es idempotente: crear un índice que ya existe no hace nada.
/// Son declarativos (intención de consulta y unicidad del correo), no una necesidad de rendimiento.
/// </summary>
internal sealed class MongoIndexInitializer(MongoContext context)
{
    public async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var byCustomerAndStatus = new CreateIndexModel<InvoiceDocument>(
            Builders<InvoiceDocument>.IndexKeys.Ascending(invoice => invoice.CustomerId).Ascending(invoice => invoice.Status),
            new CreateIndexOptions { Name = "ix_clienteId_estado" });
        await context.Invoices.Indexes.CreateOneAsync(byCustomerAndStatus, cancellationToken: cancellationToken);

        var uniqueEmail = new CreateIndexModel<CustomerDocument>(
            Builders<CustomerDocument>.IndexKeys.Ascending(customer => customer.Email),
            new CreateIndexOptions { Name = "ux_email", Unique = true });
        await context.Customers.Indexes.CreateOneAsync(uniqueEmail, cancellationToken: cancellationToken);
    }
}
