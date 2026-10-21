namespace InvoiceReminders.Application.Reminders;

/// <summary>Lo que se registra en el historial de la factura cuando avanza de estado.</summary>
public sealed record StatusChange(Guid RunId, string? MessageId, DateTimeOffset At, string Reason)
{
    public const string ReminderReason = "recordatorio";
}
