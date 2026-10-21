namespace InvoiceReminders.Domain.Invoices;

/// <summary>
/// Estado de una factura tal como se guarda en MongoDB. Es el único lugar del código
/// que conoce los literales del contrato de datos (ADR 0005). Cualquier valor
/// desconocido se conserva sin normalizar para que nunca se pierda información.
/// </summary>
public readonly record struct InvoiceStatus
{
    public static readonly InvoiceStatus FirstReminder = new("primerrecordatorio");
    public static readonly InvoiceStatus SecondReminder = new("segundorecordatorio");
    public static readonly InvoiceStatus Deactivated = new("desactivado");
    public static readonly InvoiceStatus Paid = new("pagada");
    public static readonly InvoiceStatus Pending = new("pendiente");

    private InvoiceStatus(string value) => Value = value;

    public string Value { get; }

    public static InvoiceStatus From(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new InvoiceStatus(value);
    }

    public bool Equals(InvoiceStatus other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override int GetHashCode() => Value is null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value ?? string.Empty;
}
