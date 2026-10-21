using System.Globalization;
using InvoiceReminders.Domain.Customers;
using InvoiceReminders.Domain.Invoices;
using InvoiceReminders.Domain.Reminders;

namespace InvoiceReminders.Application.Email;

/// <summary>
/// Redacta el correo de un recordatorio: elige la plantilla por tipo de aviso, formatea
/// los datos en es-CO y añade cabeceras de trazabilidad. No envía nada (SRP).
/// </summary>
public sealed class ReminderEmailComposer : IReminderEmailComposer
{
    public const string MessageIdDomain = "invoice-reminders.test";

    private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

    private readonly Dictionary<ReminderKind, IReminderEmailTemplate> _templates;

    public ReminderEmailComposer(IEnumerable<IReminderEmailTemplate> templates)
    {
        ArgumentNullException.ThrowIfNull(templates);
        _templates = templates.ToDictionary(template => template.Kind);

        var missing = Enum.GetValues<ReminderKind>().Where(kind => !_templates.ContainsKey(kind)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException($"Faltan plantillas de correo para: {string.Join(", ", missing)}.");
        }
    }

    public EmailMessage Compose(Customer customer, Invoice invoice, ReminderTransition transition, Guid runId)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(transition);

        var model = new ReminderEmailModel(
            customer.Name,
            invoice.Id,
            invoice.Amount.ToString("N0", Colombia),
            invoice.Currency,
            invoice.DueOn.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));

        var rendered = _templates[transition.Kind].Render(model);
        var messageId = $"{invoice.Id}.{transition.From}.{runId:N}@{MessageIdDomain}";

        var headers = new Dictionary<string, string>
        {
            ["X-Invoice-Id"] = invoice.Id,
            ["X-Reminder-Kind"] = transition.Kind.ToString(),
            ["X-Reminder-Run-Id"] = runId.ToString("D"),
        };

        return new EmailMessage(customer.Name, customer.Email, rendered.Subject, rendered.TextBody, rendered.HtmlBody, messageId, headers);
    }
}
