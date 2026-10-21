namespace InvoiceReminders.Infrastructure.Seed;

/// <summary>Siembra los datos de demostración. Vive en Infrastructure: no es parte del proceso de negocio.</summary>
public interface IDemoDataSeeder
{
    Task<SeedResult> SeedAsync(SeedMode mode, CancellationToken cancellationToken);
}
