namespace InvoiceReminders.Application.Reminders;

public sealed record InvoiceOutcome(
    string InvoiceId,
    string CustomerId,
    string CustomerName,
    string PreviousStatus,
    string? NewStatus,
    OutcomeKind Kind,
    string? MessageId,
    string? Error);
