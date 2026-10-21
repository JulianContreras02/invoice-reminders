using System.Net.Sockets;
using InvoiceReminders.Infrastructure.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace InvoiceReminders.Infrastructure.Health;

/// <summary>
/// Comprueba que el servidor SMTP acepta conexiones TCP. Si no, la aplicación queda "Degraded":
/// sigue sirviendo el resumen, y el proceso reportará los correos fallidos sin cambiar estados.
/// </summary>
internal sealed class SmtpTcpHealthCheck(IOptions<SmtpOptions> options) : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var smtp = options.Value;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(smtp.Host, smtp.Port, timeout.Token);
            return HealthCheckResult.Healthy($"SMTP acepta conexiones en {smtp.Host}:{smtp.Port}.");
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, $"SMTP no disponible en {smtp.Host}:{smtp.Port}.", ex);
        }
    }
}
