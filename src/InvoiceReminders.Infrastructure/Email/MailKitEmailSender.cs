using System.Net.Sockets;
using InvoiceReminders.Application.Common;
using InvoiceReminders.Application.Email;
using InvoiceReminders.Application.Ports;
using InvoiceReminders.Infrastructure.Options;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace InvoiceReminders.Infrastructure.Email;

/// <summary>
/// Envía por SMTP con MailKit (Microsoft desaconseja System.Net.Mail.SmtpClient para desarrollo nuevo).
/// Crea un SmtpClient por envío porque no es thread-safe; por eso este servicio puede ser Singleton.
/// El correo cuenta como entregado en cuanto el servidor acepta el mensaje: un fallo posterior al
/// cerrar la conexión no lo convierte en "no enviado" (eso causaría un duplicado en la siguiente ejecución).
/// </summary>
internal sealed class MailKitEmailSender(IOptions<SmtpOptions> options, ILogger<MailKitEmailSender> logger) : IEmailSender
{
    // Mensaje sin datos personales ni detalles de infraestructura: llega a la interfaz.
    // El detalle (servidor, causa) queda en la excepción interna, que se registra en el log.
    private const string DeliveryFailedMessage = "El servidor de correo no está disponible o rechazó el mensaje.";

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var smtp = options.Value;

        using var client = new SmtpClient { Timeout = (int)TimeSpan.FromSeconds(smtp.TimeoutSeconds).TotalMilliseconds };
        try
        {
            using var mime = BuildMimeMessage(message, smtp);
            await client.ConnectAsync(smtp.Host, smtp.Port, ToSecureSocketOptions(smtp.Security), cancellationToken);
            if (!string.IsNullOrEmpty(smtp.Username))
            {
                await client.AuthenticateAsync(smtp.Username, smtp.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
        }
        catch (Exception ex) when (IsDeliveryFailure(ex, cancellationToken))
        {
            throw new EmailDeliveryException(DeliveryFailedMessage, ex);
        }

        logger.LogInformation("Correo {MessageId} aceptado por el servidor SMTP", message.MessageId);
        await DisconnectQuietlyAsync(client, message.MessageId);
    }

    /// <summary>
    /// Fallos de red, protocolo, autenticación, formato o tiempo de espera. Una cancelación pedida
    /// por quien llama no es un fallo de entrega y se deja propagar.
    /// </summary>
    internal static bool IsDeliveryFailure(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        SocketException or IOException or TimeoutException => true,
        SmtpCommandException or SmtpProtocolException => true,
        AuthenticationException or SslHandshakeException => true,
        ServiceNotConnectedException or ServiceNotAuthenticatedException => true,
        ParseException => true,
        _ => false,
    };

    private async Task DisconnectQuietlyAsync(SmtpClient client, string messageId)
    {
        try
        {
            await client.DisconnectAsync(quit: true, CancellationToken.None);
        }
#pragma warning disable CA1031 // El servidor ya aceptó el mensaje: ningún fallo al cerrar puede convertirlo en "no enviado".
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogDebug(ex, "El correo {MessageId} ya fue aceptado; falló solo el cierre de la conexión SMTP", messageId);
        }
    }

    private static MimeMessage BuildMimeMessage(EmailMessage message, SmtpOptions smtp)
    {
        var mime = new MimeMessage
        {
            Subject = message.Subject,
            MessageId = message.MessageId,
        };
        mime.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));

        foreach (var (name, value) in message.Headers)
        {
            mime.Headers.Add(name, value);
        }

        mime.Body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody }.ToMessageBody();
        return mime;
    }

    private static SecureSocketOptions ToSecureSocketOptions(SmtpSecurity security) => security switch
    {
        SmtpSecurity.None => SecureSocketOptions.None,
        SmtpSecurity.StartTls => SecureSocketOptions.StartTls,
        SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
        SmtpSecurity.Auto => SecureSocketOptions.Auto,
        _ => throw new ArgumentOutOfRangeException(nameof(security), security, "Modo de seguridad SMTP no soportado."),
    };
}
