using InvoiceReminders.Domain.Invoices;
using InvoiceReminders.Infrastructure.Mongo;
using InvoiceReminders.Infrastructure.Seed;

namespace InvoiceReminders.UnitTests.Infrastructure;

/// <summary>La frontera entre el dominio y el contrato de MongoDB no pierde ni desplaza datos.</summary>
public sealed class DocumentMappersTests
{
    [Fact]
    public void Invoice_RoundTripsThroughTheMongoDocumentUnchanged()
    {
        var invoice = DemoData.Invoices[0];

        var roundTripped = invoice.ToDocument().ToDomain();

        Assert.Equal(invoice, roundTripped);
    }

    [Fact]
    public void InvoiceDocument_StoresDatesAsUtcMidnightAndStatusAsTheExactLiteral()
    {
        var invoice = DemoData.Invoices[0] with { DueOn = new DateOnly(2026, 8, 31), Status = InvoiceStatus.SecondReminder };

        var document = invoice.ToDocument();

        Assert.Equal(new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc), document.DueOn);
        Assert.Equal(DateTimeKind.Utc, document.DueOn.Kind);
        Assert.Equal("segundorecordatorio", document.Status);
    }

    [Fact]
    public void InvoiceDocument_WithMissingStatus_MapsToAnUnknownStatusInsteadOfFailing()
    {
        var document = DemoData.Invoices[0].ToDocument();
        document.Status = null!;

        var invoice = document.ToDomain();

        Assert.Equal(string.Empty, invoice.Status.Value);
    }
}
