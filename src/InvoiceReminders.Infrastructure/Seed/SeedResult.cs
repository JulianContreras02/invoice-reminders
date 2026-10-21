namespace InvoiceReminders.Infrastructure.Seed;

public sealed record SeedResult(SeedMode Mode, bool Applied, int Customers, int Invoices);
