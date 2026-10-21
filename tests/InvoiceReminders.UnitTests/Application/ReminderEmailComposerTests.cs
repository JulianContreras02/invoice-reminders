using InvoiceReminders.Application.Email;
using InvoiceReminders.Domain.Customers;
using InvoiceReminders.Domain.Invoices;
using InvoiceReminders.Domain.Reminders;

namespace InvoiceReminders.UnitTests.Application;

public sealed class ReminderEmailComposerTests
{
    private static readonly Guid RunId = Guid.Parse("0199a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b");
    private static readonly Customer Customer = new("cli-001", "Pérez & Asociados S.A.S.", "contacto@perez-asociados.test");

    private static readonly Invoice Invoice = new(
        "FAC-2026-0101",
        "cli-001",
        1_250_000m,
        "COP",
        new DateOnly(2026, 8, 1),
        new DateOnly(2026, 8, 31),
        InvoiceStatus.FirstReminder,
        DateTimeOffset.UnixEpoch);

    private readonly ReminderEmailComposer _composer = new([new SecondReminderTemplate(), new DeactivationNoticeTemplate()]);

    [Fact]
    public void Compose_SendsToTheCustomerWithNameAndAddressSeparated()
    {
        var message = _composer.Compose(Customer, Invoice, Transition(ReminderKind.SecondReminderNotice), RunId);

        Assert.Equal("contacto@perez-asociados.test", message.ToAddress);
        Assert.Equal("Pérez & Asociados S.A.S.", message.ToName);
    }

    [Fact]
    public void Compose_FirstReminder_InformsThatItMovedToSecondReminder()
    {
        var message = _composer.Compose(Customer, Invoice, Transition(ReminderKind.SecondReminderNotice), RunId);

        Assert.Equal("Factura FAC-2026-0101: ha pasado a segundo recordatorio", message.Subject);
        Assert.Contains("ha pasado a segundo recordatorio", message.TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_SecondReminder_InformsThatTheServiceWillBeDeactivated()
    {
        var message = _composer.Compose(Customer, Invoice with { Status = InvoiceStatus.SecondReminder }, Transition(ReminderKind.DeactivationNotice), RunId);

        Assert.Equal("Factura FAC-2026-0101: aviso de desactivación del servicio", message.Subject);
        Assert.Contains("va a ser desactivado", message.TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_FormatsAmountAndDueDateForColombia()
    {
        var message = _composer.Compose(Customer, Invoice, Transition(ReminderKind.SecondReminderNotice), RunId);

        Assert.Contains("1.250.000 COP", message.TextBody, StringComparison.Ordinal);
        Assert.Contains("31/08/2026", message.TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_EncodesHtmlButKeepsPlainTextReadable()
    {
        var message = _composer.Compose(Customer, Invoice, Transition(ReminderKind.SecondReminderNotice), RunId);

        // WebUtility.HtmlEncode también codifica el rango Latin-1 (é → &#233;), así que se verifica solo el "&".
        Assert.Contains("&amp; Asociados", message.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("& Asociados", message.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Pérez & Asociados", message.TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_AddsTraceabilityHeadersAndDeterministicMessageId()
    {
        var message = _composer.Compose(Customer, Invoice, Transition(ReminderKind.SecondReminderNotice), RunId);

        Assert.Equal("FAC-2026-0101", message.Headers["X-Invoice-Id"]);
        Assert.Equal("SecondReminderNotice", message.Headers["X-Reminder-Kind"]);
        Assert.Equal(RunId.ToString("D"), message.Headers["X-Reminder-Run-Id"]);
        Assert.Equal($"FAC-2026-0101.primerrecordatorio.{RunId:N}@invoice-reminders.test", message.MessageId);
    }

    [Fact]
    public void Constructor_MissingTemplate_FailsFast()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new ReminderEmailComposer([new SecondReminderTemplate()]));

        Assert.Contains(nameof(ReminderKind.DeactivationNotice), exception.Message, StringComparison.Ordinal);
    }

    private static ReminderTransition Transition(ReminderKind kind) =>
        ReminderPolicy.Transitions.Single(transition => transition.Kind == kind);
}
