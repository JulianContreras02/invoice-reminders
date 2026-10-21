using InvoiceReminders.Application.Common;
using InvoiceReminders.Application.Email;

namespace InvoiceReminders.Application.Ports;

/// <summary>
/// Transporte de correo. Contrato: si la tarea termina sin excepción, el servidor aceptó el
/// mensaje; ante cualquier fallo de transporte lanza <see cref="EmailDeliveryException"/>.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
