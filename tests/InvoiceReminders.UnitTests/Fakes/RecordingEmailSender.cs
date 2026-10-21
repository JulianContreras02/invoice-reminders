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

    public void FailFor(string address) => _failingAddresses.Add(address);

    public Task<EmailDispatchResult> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_failingAddresses.Contains(message.ToAddress))
        {
            throw new EmailDeliveryException($"SMTP no disponible para {message.ToAddress}.");
        }

        journal.Record($"send:{message.Headers["X-Invoice-Id"]}");
        _sent.Add(message);
        AfterSend?.Invoke(message);
        return Task.FromResult(new EmailDispatchResult(message.MessageId));
    }
}
