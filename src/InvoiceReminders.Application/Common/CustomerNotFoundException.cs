namespace InvoiceReminders.Application.Common;

public sealed class CustomerNotFoundException : Exception
{
    public CustomerNotFoundException(string customerId)
        : base($"No existe un cliente con id '{customerId}'.")
    {
        CustomerId = customerId;
    }

    public CustomerNotFoundException()
    {
        CustomerId = string.Empty;
    }

    public CustomerNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
        CustomerId = string.Empty;
    }

    public string CustomerId { get; }
}
