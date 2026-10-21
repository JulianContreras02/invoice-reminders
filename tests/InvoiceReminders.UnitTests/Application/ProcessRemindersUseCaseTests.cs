using InvoiceReminders.Application.Common;
using InvoiceReminders.Application.Email;
using InvoiceReminders.Application.Reminders;
using InvoiceReminders.Domain.Customers;
using InvoiceReminders.Domain.Invoices;
using InvoiceReminders.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiceReminders.UnitTests.Application;

public sealed class ProcessRemindersUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 15, 0, 0, TimeSpan.Zero);

    private static readonly Customer Perez = new("cli-001", "Pérez & Asociados S.A.S.", "contacto@perez-asociados.test");
    private static readonly Customer Rodriguez = new("cli-002", "Rodríguez Abogados Ltda.", "facturacion@rodriguez-abogados.test");
    private static readonly Customer Gomez = new("cli-003", "Consultorio Jurídico Gómez", "admin@gomez-juridico.test");

    private readonly OperationJournal _journal = new();
    private readonly InMemoryCustomerRepository _customers = new();
    private readonly InMemoryInvoiceRepository _invoices;
    private readonly RecordingEmailSender _sender;

    public ProcessRemindersUseCaseTests()
    {
        _invoices = new InMemoryInvoiceRepository(_journal);
        _sender = new RecordingEmailSender(_journal);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RunAsync_FirstReminder_SendsEmailBeforeAdvancingToSecondReminder()
    {
        _customers.Add(Perez);
        _invoices.Add(NewInvoice("FAC-1", Perez, InvoiceStatus.FirstReminder));

        var result = await CreateSut().RunAsync(null, Ct);

        Assert.Equal(new[] { "send:FAC-1", "update:FAC-1" }, _journal.Entries);
        Assert.Equal(InvoiceStatus.SecondReminder, _invoices.Get("FAC-1").Status);
        var outcome = Assert.Single(result.Outcomes);
        Assert.Equal(OutcomeKind.SentAndUpdated, outcome.Kind);
        Assert.Equal("segundorecordatorio", outcome.NewStatus);
        Assert.Contains("ha pasado a segundo recordatorio", Assert.Single(_sender.Sent).Subject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_SecondReminder_SendsDeactivationNoticeAndDeactivates()
    {
        _customers.Add(Perez);
        _invoices.Add(NewInvoice("FAC-1", Perez, InvoiceStatus.SecondReminder));

        await CreateSut().RunAsync(null, Ct);

        Assert.Equal(InvoiceStatus.Deactivated, _invoices.Get("FAC-1").Status);
        Assert.Contains("aviso de desactivación", Assert.Single(_sender.Sent).Subject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_StatusesOutsideTheProcess_AreNeitherEmailedNorChanged()
    {
        _customers.Add(Perez);
        _invoices.Add(
            NewInvoice("FAC-1", Perez, InvoiceStatus.Paid),
            NewInvoice("FAC-2", Perez, InvoiceStatus.Deactivated),
            NewInvoice("FAC-3", Perez, InvoiceStatus.Pending));

        var result = await CreateSut().RunAsync(null, Ct);

        Assert.Empty(_sender.Sent);
        Assert.Equal(0, result.Candidates);
        Assert.Equal(InvoiceStatus.Paid, _invoices.Get("FAC-1").Status);
        Assert.Equal(InvoiceStatus.Deactivated, _invoices.Get("FAC-2").Status);
        Assert.Equal(InvoiceStatus.Pending, _invoices.Get("FAC-3").Status);
    }

    [Fact]
    public async Task RunAsync_EmailFails_KeepsStatusAndContinuesWithTheNextInvoice()
    {
        _customers.Add(Perez, Rodriguez);
        _invoices.Add(
            NewInvoice("FAC-1", Perez, InvoiceStatus.FirstReminder),
            NewInvoice("FAC-2", Rodriguez, InvoiceStatus.FirstReminder));
        _sender.FailFor(Perez.Email);

        var result = await CreateSut().RunAsync(null, Ct);

        Assert.Equal(InvoiceStatus.FirstReminder, _invoices.Get("FAC-1").Status);
        Assert.Equal(InvoiceStatus.SecondReminder, _invoices.Get("FAC-2").Status);
        Assert.DoesNotContain("update:FAC-1", _journal.Entries);
        Assert.Equal(OutcomeKind.EmailFailed, result.Outcomes.Single(outcome => outcome.InvoiceId == "FAC-1").Kind);
        Assert.True(result.HasFailures);
    }

    [Fact]
    public async Task RunAsync_InvoiceChangedByAnotherProcess_ReportsConflictWithoutThrowing()
    {
        _customers.Add(Perez);
        _invoices.Add(NewInvoice("FAC-1", Perez, InvoiceStatus.FirstReminder));
        _invoices.SimulateConcurrentChange("FAC-1", InvoiceStatus.Paid);

        var result = await CreateSut().RunAsync(null, Ct);

        var outcome = Assert.Single(result.Outcomes);
        Assert.Equal(OutcomeKind.UpdateConflict, outcome.Kind);
        Assert.Equal(InvoiceStatus.Paid, _invoices.Get("FAC-1").Status);
        Assert.False(result.HasFailures);
    }

    [Fact]
    public async Task RunAsync_UpdateFailsAfterSending_ReportsItWithTheMessageIdAndNoInternalDetails()
    {
        _customers.Add(Perez);
        _invoices.Add(NewInvoice("FAC-1", Perez, InvoiceStatus.FirstReminder));
        _invoices.FailUpdatesWith(new TimeoutException("MongoDB no responde en mongo:27017"));

        var result = await CreateSut().RunAsync(null, Ct);

        var outcome = Assert.Single(result.Outcomes);
        Assert.Equal(OutcomeKind.UpdateFailedAfterSend, outcome.Kind);
        Assert.Equal(Assert.Single(_sender.Sent).MessageId, outcome.MessageId);
        Assert.DoesNotContain("mongo", outcome.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunAsync_UpdateFailsAfterSending_StopsSendingFurtherEmails()
    {
        _customers.Add(Perez, Rodriguez);
        _invoices.Add(
            NewInvoice("FAC-1", Perez, InvoiceStatus.FirstReminder),
            NewInvoice("FAC-2", Perez, InvoiceStatus.SecondReminder),
            NewInvoice("FAC-3", Rodriguez, InvoiceStatus.FirstReminder));
        _invoices.FailUpdatesWith(new TimeoutException("MongoDB no responde"));

        var result = await CreateSut().RunAsync(null, Ct);

        Assert.Single(_sender.Sent);
        Assert.Equal(RunCompletion.StoppedPersistenceUnavailable, result.Completion);
        Assert.True(result.HasFailures);
    }

    [Fact]
    public async Task RunAsync_ReadingACustomerFails_StopsAndKeepsWhatWasAlreadyDone()
    {
        _customers.Add(Perez, Rodriguez, Gomez);
        _invoices.Add(
            NewInvoice("FAC-1", Perez, InvoiceStatus.FirstReminder),
            NewInvoice("FAC-2", Rodriguez, InvoiceStatus.FirstReminder),
            NewInvoice("FAC-3", Gomez, InvoiceStatus.FirstReminder));
        _invoices.FailReadsFor(Rodriguez.Id, new TimeoutException("MongoDB no responde"));

        var result = await CreateSut().RunAsync(null, Ct);

        Assert.Equal(RunCompletion.StoppedPersistenceUnavailable, result.Completion);
        Assert.Equal("FAC-1", Assert.Single(result.Outcomes).InvoiceId);
        Assert.Equal(InvoiceStatus.SecondReminder, _invoices.Get("FAC-1").Status);
        Assert.Equal(InvoiceStatus.FirstReminder, _invoices.Get("FAC-3").Status);
        Assert.Equal(1, result.CustomersProcessed);
    }

    [Fact]
    public async Task RunAsync_CustomerWithoutValidEmail_IsSkippedWithoutSending()
    {
        var withoutEmail = new Customer("cli-009", "Sin Correo", "no-es-un-correo");
        _customers.Add(withoutEmail);
        _invoices.Add(NewInvoice("FAC-9", withoutEmail, InvoiceStatus.FirstReminder));

        var result = await CreateSut().RunAsync(null, Ct);

        Assert.Empty(_sender.Sent);
        Assert.Equal(OutcomeKind.SkippedNoEmail, Assert.Single(result.Outcomes).Kind);
        Assert.Equal(InvoiceStatus.FirstReminder, _invoices.Get("FAC-9").Status);
    }

    [Fact]
    public async Task RunAsync_UnknownCustomer_ThrowsCustomerNotFound()
    {
        _customers.Add(Perez);

        var exception = await Assert.ThrowsAsync<CustomerNotFoundException>(() => CreateSut().RunAsync("cli-404", Ct));

        Assert.Equal("cli-404", exception.CustomerId);
    }

    [Fact]
    public async Task RunAsync_ThreeCustomers_EachOneReceivesOnlyItsOwnInvoices()
    {
        _customers.Add(Perez, Rodriguez, Gomez);
        _invoices.Add(
            NewInvoice("FAC-0101", Perez, InvoiceStatus.FirstReminder),
            NewInvoice("FAC-0201", Rodriguez, InvoiceStatus.FirstReminder),
            NewInvoice("FAC-0202", Rodriguez, InvoiceStatus.SecondReminder),
            NewInvoice("FAC-0301", Gomez, InvoiceStatus.SecondReminder));

        await CreateSut().RunAsync(null, Ct);

        Assert.Equal(4, _sender.Sent.Count);
        Assert.All(_sender.Sent, message =>
        {
            var invoiceId = message.Headers["X-Invoice-Id"];
            var owner = _invoices.Get(invoiceId).CustomerId;
            var expectedAddress = new[] { Perez, Rodriguez, Gomez }.Single(customer => customer.Id == owner).Email;
            Assert.Equal(expectedAddress, message.ToAddress);
        });
    }

    [Fact]
    public async Task RunAsync_WithCustomerId_ProcessesOnlyThatCustomer()
    {
        _customers.Add(Perez, Rodriguez);
        _invoices.Add(
            NewInvoice("FAC-1", Perez, InvoiceStatus.FirstReminder),
            NewInvoice("FAC-2", Rodriguez, InvoiceStatus.FirstReminder));

        var result = await CreateSut().RunAsync(Rodriguez.Id, Ct);

        Assert.Equal("FAC-2", Assert.Single(result.Outcomes).InvoiceId);
        Assert.Equal(InvoiceStatus.FirstReminder, _invoices.Get("FAC-1").Status);
    }

    [Fact]
    public async Task RunAsync_ConsecutiveRuns_AdvanceExactlyOneStepPerRun()
    {
        _customers.Add(Perez);
        _invoices.Add(NewInvoice("FAC-1", Perez, InvoiceStatus.FirstReminder));
        var sut = CreateSut();

        await sut.RunAsync(null, Ct);
        Assert.Equal(InvoiceStatus.SecondReminder, _invoices.Get("FAC-1").Status);

        await sut.RunAsync(null, Ct);
        Assert.Equal(InvoiceStatus.Deactivated, _invoices.Get("FAC-1").Status);

        var third = await sut.RunAsync(null, Ct);
        Assert.Equal(0, third.Candidates);
        Assert.Equal(2, _sender.Sent.Count);
    }

    [Fact]
    public async Task RunAsync_CancelledBetweenInvoices_StopsButNeverCancelsTheUpdateOfASentEmail()
    {
        _customers.Add(Perez);
        _invoices.Add(
            NewInvoice("FAC-1", Perez, InvoiceStatus.FirstReminder),
            NewInvoice("FAC-2", Perez, InvoiceStatus.FirstReminder));
        using var shutdown = new CancellationTokenSource();
        _sender.AfterSend = _ => shutdown.Cancel();

        var result = await CreateSut().RunAsync(null, shutdown.Token);

        Assert.Equal(RunCompletion.Cancelled, result.Completion);
        Assert.Equal(InvoiceStatus.SecondReminder, _invoices.Get("FAC-1").Status);
        Assert.Equal(InvoiceStatus.FirstReminder, _invoices.Get("FAC-2").Status);
        Assert.False(_invoices.LastUpdateTokenCouldBeCancelled);
        Assert.False(_sender.AnySendTokenCouldBeCancelled);
    }

    [Fact]
    public async Task RunAsync_ShutdownBeforeTheNextCustomer_ReturnsPartialResultWithoutReadingIt()
    {
        _customers.Add(Perez, Rodriguez);
        _invoices.Add(
            NewInvoice("FAC-1", Perez, InvoiceStatus.FirstReminder),
            NewInvoice("FAC-2", Rodriguez, InvoiceStatus.FirstReminder));
        using var shutdown = new CancellationTokenSource();
        _sender.AfterSend = _ => shutdown.Cancel();

        var result = await CreateSut().RunAsync(null, shutdown.Token);

        Assert.Equal(RunCompletion.Cancelled, result.Completion);
        Assert.Equal(1, result.CustomersProcessed);
        Assert.Equal(OutcomeKind.SentAndUpdated, Assert.Single(result.Outcomes).Kind);
        Assert.Equal(InvoiceStatus.FirstReminder, _invoices.Get("FAC-2").Status);
    }

    [Fact]
    public async Task RunAsync_RecordsRunIdAndMessageIdInTheStatusHistory()
    {
        _customers.Add(Perez);
        _invoices.Add(NewInvoice("FAC-1", Perez, InvoiceStatus.FirstReminder));

        var result = await CreateSut().RunAsync(null, Ct);

        var change = Assert.Single(_invoices.Changes);
        Assert.Equal(result.RunId, change.RunId);
        Assert.Equal(Assert.Single(_sender.Sent).MessageId, change.MessageId);
        Assert.Equal(Now, change.At);
        Assert.Equal(StatusChange.ReminderReason, change.Reason);
    }

    [Fact]
    public async Task RunAsync_MixedOutcomes_CountersSatisfyTheDocumentedInvariants()
    {
        var withoutEmail = new Customer("cli-009", "Sin Correo", "no-es-un-correo");
        _customers.Add(Perez, Rodriguez, withoutEmail);
        _invoices.Add(
            NewInvoice("FAC-0101", Perez, InvoiceStatus.FirstReminder),
            NewInvoice("FAC-0102", Perez, InvoiceStatus.SecondReminder),
            NewInvoice("FAC-0201", Rodriguez, InvoiceStatus.FirstReminder),
            NewInvoice("FAC-0901", withoutEmail, InvoiceStatus.FirstReminder));
        _invoices.SimulateConcurrentChange("FAC-0102", InvoiceStatus.Paid);
        _sender.FailFor(Rodriguez.Email);

        var result = await CreateSut().RunAsync(null, Ct);

        Assert.Equal(4, result.Candidates);
        Assert.Equal(1, result.Updated);
        Assert.Equal(1, result.UpdateConflicts);
        Assert.Equal(1, result.EmailFailed);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(result.Candidates, result.EmailsSent + result.EmailFailed + result.Skipped);
        Assert.Equal(result.EmailsSent, result.Updated + result.UpdateConflicts + result.UpdateFailedAfterSend);
        Assert.Equal(1, result.Failed);
        Assert.Equal(3, result.CustomersProcessed);
    }

    private ProcessRemindersUseCase CreateSut() =>
        new(
            _customers,
            _invoices,
            new ReminderEmailComposer([new SecondReminderTemplate(), new DeactivationNoticeTemplate()]),
            _sender,
            new FixedTimeProvider(Now),
            NullLogger<ProcessRemindersUseCase>.Instance);

    private static Invoice NewInvoice(string id, Customer owner, InvoiceStatus status) =>
        new(id, owner.Id, 500_000m, "COP", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31), status, Now.AddDays(-30));
}
