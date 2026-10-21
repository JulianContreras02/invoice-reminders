using InvoiceReminders.Application.Common;
using InvoiceReminders.Application.Email;

namespace InvoiceReminders.Application.Ports;

/// <summary>Transporte de correo. Contrato: devuelve el resultado del envío o lanza <see cref="EmailDeliveryException"/>; nunca un éxito parcial.</summary>
public interface IEmailSender
{
    Task<EmailDispatchResult> SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
