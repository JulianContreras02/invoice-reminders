using InvoiceReminders.Domain.Customers;

namespace InvoiceReminders.UnitTests.Domain;

public sealed class CustomerTests
{
    [Theory]
    [InlineData("contacto@perez-asociados.test")]
    [InlineData("Facturacion@Rodriguez-Abogados.test")]
    public void HasDeliverableEmail_PlainValidAddress_IsTrue(string email)
    {
        Assert.True(new Customer("cli-1", "Cliente", email).HasDeliverableEmail());
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-es-un-correo")]
    [InlineData("Juan Pérez <juan@perez.test>")]
    [InlineData(" juan@perez.test")]
    public void HasDeliverableEmail_InvalidOrNamedAddress_IsFalse(string email)
    {
        Assert.False(new Customer("cli-1", "Cliente", email).HasDeliverableEmail());
    }
}
