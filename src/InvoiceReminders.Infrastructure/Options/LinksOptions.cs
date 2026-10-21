namespace InvoiceReminders.Infrastructure.Options;

/// <summary>Enlaces que la interfaz muestra al usuario (por ejemplo, la bandeja de Mailpit).</summary>
public sealed class LinksOptions
{
    public const string SectionName = "Links";

    public string? MailpitUrl { get; set; }
}
