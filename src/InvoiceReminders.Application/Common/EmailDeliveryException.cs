namespace InvoiceReminders.Application.Common;

/// <summary>El correo no pudo entregarse al servidor SMTP. El estado de la factura no debe cambiar.</summary>
public sealed class EmailDeliveryException : Exception
{
    public EmailDeliveryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public EmailDeliveryException(string message)
        : base(message)
    {
    }

    public EmailDeliveryException()
    {
    }
}
