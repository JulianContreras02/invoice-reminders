using InvoiceReminders.Infrastructure.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace InvoiceReminders.Infrastructure.Mongo;

/// <summary>Colecciones tipadas. Singleton: IMongoDatabase e IMongoCollection son thread-safe.</summary>
internal sealed class MongoContext
{
    public const string CustomersCollection = "clientes";
    public const string InvoicesCollection = "facturas";

    public MongoContext(IMongoClient client, IOptions<MongoOptions> options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        Database = client.GetDatabase(options.Value.Database);
        Customers = Database.GetCollection<CustomerDocument>(CustomersCollection);
        Invoices = Database.GetCollection<InvoiceDocument>(InvoicesCollection);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<CustomerDocument> Customers { get; }

    public IMongoCollection<InvoiceDocument> Invoices { get; }
}
