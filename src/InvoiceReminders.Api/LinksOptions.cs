namespace InvoiceReminders.Api;

/// <summary>
/// Enlaces que la interfaz muestra al usuario (por ejemplo, la bandeja de Mailpit).
/// Es configuración de presentación: vive en la Api, que es quien la expone.
/// </summary>
public sealed class LinksOptions
{
    public const string SectionName = "Links";

    public string? MailpitUrl { get; set; }
}
