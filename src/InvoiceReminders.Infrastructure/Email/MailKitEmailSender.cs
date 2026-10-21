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
/// Traduce cualquier fallo de transporte a <see cref="EmailDeliveryException"/>.
/// </summary>
internal sealed class MailKitEmailSender(IOptions<SmtpOptions> options, ILogger<MailKitEmailSender> logger) : IEmailSender
{
    public async Task<EmailDispatchResult> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var smtp = options.Value;

        try
        {
            using var mime = BuildMimeMessage(message, smtp);
            using var client = new SmtpClient { Timeout = (int)TimeSpan.FromSeconds(smtp.TimeoutSeconds).TotalMilliseconds };

            await client.ConnectAsync(smtp.Host, smtp.Port, ToSecureSocketOptions(smtp.Security), cancellationToken);
            if (!string.IsNullOrEmpty(smtp.Username))
            {
                await client.AuthenticateAsync(smtp.Username, smtp.Password ?? string.Empty, cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);
        }
        catch (Exception ex) when (IsDeliveryFailure(ex, cancellationToken))
        {
            throw new EmailDeliveryException(
                $"No se pudo entregar el correo a {message.ToAddress} vía {smtp.Host}:{smtp.Port}: {ex.Message}", ex);
        }

        logger.LogInformation("Correo {MessageId} entregado a {To}", message.MessageId, message.ToAddress);
        return new EmailDispatchResult(message.MessageId);
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

    /// <summary>
    /// Fallos de red, protocolo, autenticación o tiempo de espera. Una cancelación pedida por
    /// quien llama no es un fallo de entrega y se deja propagar.
    /// </summary>
    private static bool IsDeliveryFailure(Exception exception, CancellationToken cancellationToken) => exception switch
    {
        OperationCanceledException => !cancellationToken.IsCancellationRequested,
        SocketException or IOException or TimeoutException => true,
        SmtpCommandException or SmtpProtocolException => true,
        AuthenticationException or SslHandshakeException => true,
        ServiceNotConnectedException or ServiceNotAuthenticatedException => true,
        ParseException => true,
        _ => false,
    };
}
