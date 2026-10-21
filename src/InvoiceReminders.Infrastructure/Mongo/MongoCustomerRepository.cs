using InvoiceReminders.Application.Ports;
using InvoiceReminders.Domain.Customers;
using MongoDB.Driver;

namespace InvoiceReminders.Infrastructure.Mongo;

internal sealed class MongoCustomerRepository(MongoContext context) : ICustomerRepository
{
    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken)
    {
        var documents = await context.Customers
            .Find(FilterDefinition<CustomerDocument>.Empty)
            .SortBy(document => document.Id)
            .ToListAsync(cancellationToken);

        return [.. documents.Select(document => document.ToDomain())];
    }

    public async Task<Customer?> GetByIdAsync(string customerId, CancellationToken cancellationToken)
    {
        var document = await context.Customers
            .Find(customer => customer.Id == customerId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }
}
