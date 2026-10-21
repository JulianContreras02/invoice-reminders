using System.Net.Sockets;
using InvoiceReminders.Infrastructure.Email;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace InvoiceReminders.UnitTests.Infrastructure;

/// <summary>
/// La regla que decide qué es un fallo de entrega. El envío real no se prueba por unidad
/// (sería probar MailKit); se valida contra Mailpit en la prueba de humo del compose.
/// </summary>
public sealed class MailKitEmailSenderTests
{
    public static TheoryData<Exception> TransportFailures =>
    [
        new SocketException((int)SocketError.ConnectionRefused),
        new IOException("conexión cerrada"),
        new TimeoutException(),
        new SmtpProtocolException("respuesta inválida"),
        new AuthenticationException("credenciales inválidas"),
    ];

    [Theory]
    [MemberData(nameof(TransportFailures))]
    public void IsDeliveryFailure_TransportProblems_AreDeliveryFailures(Exception exception)
    {
        Assert.True(MailKitEmailSender.IsDeliveryFailure(exception, CancellationToken.None));
    }

    [Fact]
    public void IsDeliveryFailure_TimeoutOfTheClientItself_IsADeliveryFailure()
    {
        // MailKit cancela internamente al vencer su Timeout: el llamador no pidió cancelar.
        Assert.True(MailKitEmailSender.IsDeliveryFailure(new OperationCanceledException(), CancellationToken.None));
    }

    [Fact]
    public void IsDeliveryFailure_CancellationRequestedByTheCaller_IsNotADeliveryFailure()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        Assert.False(MailKitEmailSender.IsDeliveryFailure(new OperationCanceledException(cancelled.Token), cancelled.Token));
    }

    [Fact]
    public void IsDeliveryFailure_ProgrammingErrors_AreNotHidden()
    {
        Assert.False(MailKitEmailSender.IsDeliveryFailure(new NullReferenceException(), CancellationToken.None));
        Assert.False(MailKitEmailSender.IsDeliveryFailure(new InvalidOperationException(), CancellationToken.None));
    }
}
