using InvoiceReminders.Application.Email;
using InvoiceReminders.Application.Reminders;
using InvoiceReminders.Domain.Invoices;
using InvoiceReminders.Domain.Reminders;
using InvoiceReminders.Infrastructure.Seed;
using InvoiceReminders.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiceReminders.UnitTests.Composition;

/// <summary>La semilla cumple la historia que documenta la guía de verificación: 7, 4 y 0 correos.</summary>
public sealed class DemoDataTests
{
    [Fact]
    public void Customers_AreThreeWithDistinctEmails()
    {
        Assert.Equal(3, DemoData.Customers.Count);
        Assert.Equal(3, DemoData.Customers.Select(customer => customer.Email).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Invoices_AreTenWithUniqueIdsAndKnownOwners()
    {
        var customerIds = DemoData.Customers.Select(customer => customer.Id).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(10, DemoData.Invoices.Count);
        Assert.Equal(10, DemoData.Invoices.Select(invoice => invoice.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(DemoData.Invoices, invoice => Assert.Contains(invoice.CustomerId, customerIds));
    }

    [Fact]
    public void Invoices_CoverBothTransitionsAndStatusesTheProcessIgnores()
    {
        int Count(InvoiceStatus status) => DemoData.Invoices.Count(invoice => invoice.Status == status);

        Assert.Equal(4, Count(InvoiceStatus.FirstReminder));
        Assert.Equal(3, Count(InvoiceStatus.SecondReminder));
        Assert.Equal(1, Count(InvoiceStatus.Deactivated));
        Assert.Equal(2, Count(InvoiceStatus.Paid));
    }

    [Fact]
    public void EveryCustomer_HasAtLeastOneEligibleInvoice()
    {
        Assert.All(DemoData.Customers, customer =>
            Assert.Contains(DemoData.Invoices, invoice =>
                invoice.CustomerId == customer.Id && ReminderPolicy.EligibleStatuses.Contains(invoice.Status)));
    }

    [Fact]
    public async Task ThreeConsecutiveRuns_SendSevenThenFourThenZeroEmails()
    {
        var journal = new OperationJournal();
        var customers = new InMemoryCustomerRepository().Add([.. DemoData.Customers]);
        var invoices = new InMemoryInvoiceRepository(journal).Add([.. DemoData.Invoices]);
        var sender = new RecordingEmailSender(journal);
        var sut = new ProcessRemindersUseCase(
            customers,
            invoices,
            new ReminderEmailComposer([new SecondReminderTemplate(), new DeactivationNoticeTemplate()]),
            sender,
            new FixedTimeProvider(DateTimeOffset.UnixEpoch),
            NullLogger<ProcessRemindersUseCase>.Instance);
        var ct = TestContext.Current.CancellationToken;

        var first = await sut.RunAsync(null, ct);
        var second = await sut.RunAsync(null, ct);
        var third = await sut.RunAsync(null, ct);

        Assert.Equal(7, first.EmailsSent);
        Assert.Equal(3, sender.Sent.Select(message => message.ToAddress).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(4, second.EmailsSent);
        Assert.Equal(0, third.Candidates);
        Assert.Equal(8, DemoData.Invoices.Count(invoice => invoices.Get(invoice.Id).Status == InvoiceStatus.Deactivated));
    }
}
