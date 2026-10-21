using InvoiceReminders.Application.Ports;
using InvoiceReminders.Domain.Customers;

namespace InvoiceReminders.UnitTests.Fakes;

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly List<Customer> _customers = [];

    public InMemoryCustomerRepository Add(params Customer[] customers)
    {
        _customers.AddRange(customers);
        return this;
    }

    public Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Customer>>([.. _customers]);

    public Task<Customer?> GetByIdAsync(string customerId, CancellationToken cancellationToken) =>
        Task.FromResult(_customers.FirstOrDefault(customer => customer.Id == customerId));
}
