using InvoiceReminders.Domain.Invoices;
using InvoiceReminders.Domain.Reminders;

namespace InvoiceReminders.UnitTests.Domain;

/// <summary>La máquina de estados completa, escrita como especificación ejecutable.</summary>
public sealed class ReminderPolicyTests
{
    [Theory]
    [InlineData("primerrecordatorio", "segundorecordatorio", ReminderKind.SecondReminderNotice)]
    [InlineData("segundorecordatorio", "desactivado", ReminderKind.DeactivationNotice)]
    public void TryGetTransition_EligibleStatus_ReturnsNextStatusAndNotice(string current, string expectedNext, ReminderKind expectedKind)
    {
        var found = ReminderPolicy.TryGetTransition(InvoiceStatus.From(current), out var transition);

        Assert.True(found);
        Assert.NotNull(transition);
        Assert.Equal(expectedNext, transition.To.Value);
        Assert.Equal(expectedKind, transition.Kind);
    }

    [Theory]
    [InlineData("desactivado")]
    [InlineData("pagada")]
    [InlineData("pendiente")]
    [InlineData("")]
    [InlineData("PrimerRecordatorio")]
    [InlineData("primer recordatorio")]
    public void TryGetTransition_StatusOutsideTheProcess_ReturnsFalse(string current)
    {
        var found = ReminderPolicy.TryGetTransition(InvoiceStatus.From(current), out var transition);

        Assert.False(found);
        Assert.Null(transition);
    }

    [Fact]
    public void EligibleStatuses_ContainsExactlyTheTwoReminderStatuses()
    {
        var eligible = ReminderPolicy.EligibleStatuses.Select(status => status.Value).Order(StringComparer.Ordinal);

        Assert.Equal(new[] { "primerrecordatorio", "segundorecordatorio" }, eligible);
    }
}
