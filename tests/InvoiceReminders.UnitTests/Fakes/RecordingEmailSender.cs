using InvoiceReminders.Application.Common;
using InvoiceReminders.Application.Email;
using InvoiceReminders.Application.Ports;

namespace InvoiceReminders.UnitTests.Fakes;

/// <summary>Emisor que guarda los correos en memoria y puede fallar para direcciones concretas, como un SMTP caído.</summary>
public sealed class RecordingEmailSender(OperationJournal journal) : IEmailSender
{
    private readonly List<EmailMessage> _sent = [];
    private readonly HashSet<string> _failingAddresses = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<EmailMessage> Sent => _sent;

    /// <summary>Se invoca después de cada envío aceptado.</summary>
    public Action<EmailMessage>? AfterSend { get; set; }

    /// <summary>Si alguno de los envíos recibió un token cancelable (no debería: un envío no se interrumpe).</summary>
    public bool AnySendTokenCouldBeCancelled { get; private set; }

    public void FailFor(string address) => _failingAddresses.Add(address);

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();
        AnySendTokenCouldBeCancelled |= cancellationToken.CanBeCanceled;

        if (_failingAddresses.Contains(message.ToAddress))
        {
            throw new EmailDeliveryException("SMTP no disponible.");
        }

        journal.Record($"send:{message.Headers["X-Invoice-Id"]}");
        _sent.Add(message);
        AfterSend?.Invoke(message);
        return Task.CompletedTask;
    }
}
