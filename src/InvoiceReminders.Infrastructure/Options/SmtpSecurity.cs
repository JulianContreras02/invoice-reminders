namespace InvoiceReminders.Infrastructure.Options;

/// <summary>Modo de cifrado de la conexión SMTP.</summary>
public enum SmtpSecurity
{
    /// <summary>Sin TLS: Mailpit en local (puerto 1025).</summary>
    None,

    /// <summary>STARTTLS obligatorio (habitual en el puerto 587).</summary>
    StartTls,

    /// <summary>TLS implícito desde la conexión (puerto 465).</summary>
    SslOnConnect,

    /// <summary>Deja que MailKit decida según el puerto y las capacidades del servidor.</summary>
    Auto,
}
