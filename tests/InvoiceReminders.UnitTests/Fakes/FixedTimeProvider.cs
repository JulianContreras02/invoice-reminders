namespace InvoiceReminders.UnitTests.Fakes;

/// <summary>Reloj fijo para que las pruebas sean deterministas.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
