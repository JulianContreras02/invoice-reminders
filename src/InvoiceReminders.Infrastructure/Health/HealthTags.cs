namespace InvoiceReminders.Infrastructure.Health;

public static class HealthTags
{
    /// <summary>Chequeos que deciden si la aplicación está lista para atender (MongoDB y SMTP).</summary>
    public const string Ready = "ready";
}
