using System.ComponentModel.DataAnnotations;

namespace InvoiceReminders.Infrastructure.Options;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    [Required(ErrorMessage = "Falta la cadena de conexión de MongoDB (variable de entorno Mongo__ConnectionString).")]
    public string ConnectionString { get; set; } = string.Empty;

    [Required(ErrorMessage = "Falta el nombre de la base de datos (variable de entorno Mongo__Database).")]
    public string Database { get; set; } = "invoice_reminders";
}
