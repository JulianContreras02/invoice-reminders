using InvoiceReminders.Domain.Customers;
using InvoiceReminders.Domain.Invoices;

namespace InvoiceReminders.Infrastructure.Seed;

/// <summary>
/// Datos deterministas para la demostración: 3 clientes y 10 facturas que cubren ambas
/// transiciones y estados que el proceso debe ignorar. Ejecutar el proceso tres veces
/// produce 7, 4 y 0 correos. Se usa el dominio reservado .test para no escribir jamás
/// a direcciones reales aunque alguien apunte a un SMTP real.
/// </summary>
public static class DemoData
{
    public static readonly DateTime CreatedAtUtc = new(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateTimeOffset LastUpdated = new(2026, 9, 10, 14, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<Customer> Customers { get; } =
    [
        new("cli-001", "Pérez & Asociados S.A.S.", "contacto@perez-asociados.test"),
        new("cli-002", "Rodríguez Abogados Ltda.", "facturacion@rodriguez-abogados.test"),
        new("cli-003", "Consultorio Jurídico Gómez", "admin@gomez-juridico.test"),
    ];

    public static IReadOnlyList<Invoice> Invoices { get; } =
    [
        NewInvoice("FAC-2026-0101", "cli-001", 1_250_000m, InvoiceStatus.FirstReminder, issuedMonth: 8),
        NewInvoice("FAC-2026-0102", "cli-001", 880_000m, InvoiceStatus.SecondReminder, issuedMonth: 7),
        NewInvoice("FAC-2026-0103", "cli-001", 2_300_000m, InvoiceStatus.Paid, issuedMonth: 6),
        NewInvoice("FAC-2026-0201", "cli-002", 640_000m, InvoiceStatus.FirstReminder, issuedMonth: 8),
        NewInvoice("FAC-2026-0202", "cli-002", 1_975_000m, InvoiceStatus.FirstReminder, issuedMonth: 8),
        NewInvoice("FAC-2026-0203", "cli-002", 410_000m, InvoiceStatus.Deactivated, issuedMonth: 5),
        NewInvoice("FAC-2026-0301", "cli-003", 1_120_000m, InvoiceStatus.SecondReminder, issuedMonth: 7),
        NewInvoice("FAC-2026-0302", "cli-003", 530_000m, InvoiceStatus.SecondReminder, issuedMonth: 7),
        NewInvoice("FAC-2026-0303", "cli-003", 990_000m, InvoiceStatus.Paid, issuedMonth: 6),
        NewInvoice("FAC-2026-0304", "cli-003", 760_000m, InvoiceStatus.FirstReminder, issuedMonth: 8),
    ];

    private static Invoice NewInvoice(string id, string customerId, decimal amount, InvoiceStatus status, int issuedMonth)
    {
        var issuedOn = new DateOnly(2026, issuedMonth, 1);
        return new Invoice(id, customerId, amount, "COP", issuedOn, issuedOn.AddDays(30), status, LastUpdated);
    }
}
