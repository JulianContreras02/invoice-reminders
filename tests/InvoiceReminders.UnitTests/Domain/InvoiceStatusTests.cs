using InvoiceReminders.Domain.Invoices;

namespace InvoiceReminders.UnitTests.Domain;

public sealed class InvoiceStatusTests
{
    [Fact]
    public void Constants_MatchTheLiteralsStoredInMongo()
    {
        Assert.Equal("primerrecordatorio", InvoiceStatus.FirstReminder.Value);
        Assert.Equal("segundorecordatorio", InvoiceStatus.SecondReminder.Value);
        Assert.Equal("desactivado", InvoiceStatus.Deactivated.Value);
        Assert.Equal("pagada", InvoiceStatus.Paid.Value);
    }

    [Fact]
    public void From_UnknownLiteral_IsPreservedWithoutNormalizing()
    {
        var status = InvoiceStatus.From("En Revisión ");

        Assert.Equal("En Revisión ", status.Value);
        Assert.Equal("En Revisión ", status.ToString());
    }

    [Fact]
    public void Equality_IsOrdinalAndCaseSensitive()
    {
        Assert.Equal(InvoiceStatus.FirstReminder, InvoiceStatus.From("primerrecordatorio"));
        Assert.NotEqual(InvoiceStatus.FirstReminder, InvoiceStatus.From("PrimerRecordatorio"));
    }
}
