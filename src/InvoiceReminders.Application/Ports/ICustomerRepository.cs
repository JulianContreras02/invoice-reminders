using InvoiceReminders.Domain.Customers;

namespace InvoiceReminders.Application.Ports;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken);

    Task<Customer?> GetByIdAsync(string customerId, CancellationToken cancellationToken);
}
