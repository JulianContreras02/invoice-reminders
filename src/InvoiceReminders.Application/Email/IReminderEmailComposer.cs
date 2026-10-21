using InvoiceReminders.Domain.Customers;
using InvoiceReminders.Domain.Invoices;
using InvoiceReminders.Domain.Reminders;

namespace InvoiceReminders.Application.Email;

public interface IReminderEmailComposer
{
    EmailMessage Compose(Customer customer, Invoice invoice, ReminderTransition transition, Guid runId);
}
