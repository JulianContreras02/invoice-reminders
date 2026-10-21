namespace InvoiceReminders.Domain.Customers;

/// <summary>Deudor dueño de las facturas; recibe los recordatorios en <see cref="Email"/>.</summary>
public sealed record Customer(string Id, string Name, string Email);
