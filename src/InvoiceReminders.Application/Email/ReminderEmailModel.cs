namespace InvoiceReminders.Application.Email;

/// <summary>Datos ya formateados que necesitan las plantillas.</summary>
public sealed record ReminderEmailModel(
    string CustomerName,
    string InvoiceNumber,
    string AmountFormatted,
    string Currency,
    string DueOnFormatted);
