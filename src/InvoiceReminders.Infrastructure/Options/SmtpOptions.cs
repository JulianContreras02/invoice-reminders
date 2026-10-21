using System.ComponentModel.DataAnnotations;

namespace InvoiceReminders.Infrastructure.Options;

public sealed class SmtpOptions : IValidatableObject
{
    public const string SectionName = "Smtp";

    [Required(ErrorMessage = "Falta el servidor SMTP (variable de entorno Smtp__Host).")]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Smtp__Port debe estar entre 1 y 65535.")]
    public int Port { get; set; } = 1025;

    [Required(ErrorMessage = "Falta el remitente (variable de entorno Smtp__FromAddress).")]
    [EmailAddress(ErrorMessage = "Smtp__FromAddress no es una dirección de correo válida.")]
    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "Recordatorios de facturación";

    public SmtpSecurity Security { get; set; } = SmtpSecurity.None;

    public string? Username { get; set; }

    public string? Password { get; set; }

    [Range(1, 120, ErrorMessage = "Smtp__TimeoutSeconds debe estar entre 1 y 120.")]
    public int TimeoutSeconds { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Username) != string.IsNullOrEmpty(Password))
        {
            yield return new ValidationResult(
                "Smtp__Username y Smtp__Password deben configurarse juntos.",
                [nameof(Username), nameof(Password)]);
        }
    }
}
