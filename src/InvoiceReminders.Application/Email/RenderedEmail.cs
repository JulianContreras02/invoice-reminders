namespace InvoiceReminders.Application.Email;

public sealed record RenderedEmail(string Subject, string TextBody, string HtmlBody);
