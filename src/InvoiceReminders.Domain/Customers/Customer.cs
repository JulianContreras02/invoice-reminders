using System.Net.Mail;

namespace InvoiceReminders.Domain.Customers;

/// <summary>Deudor dueño de las facturas; recibe los recordatorios en <see cref="Email"/>.</summary>
public sealed record Customer(string Id, string Name, string Email)
{
    /// <summary>
    /// El correo es una dirección simple y válida. Se rechazan formatos con nombre
    /// ("Juan &lt;juan@x.test&gt;"): el nombre del destinatario ya viene en <see cref="Name"/>.
    /// </summary>
    public bool HasDeliverableEmail() =>
        MailAddress.TryCreate(Email, out var parsed)
        && string.Equals(parsed.Address, Email, StringComparison.OrdinalIgnoreCase);
}
