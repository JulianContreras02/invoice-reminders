using InvoiceReminders.Infrastructure.Seed;

namespace InvoiceReminders.Infrastructure.Options;

/// <summary>
/// La semilla es opt-in: por defecto no siembra ni permite reiniciar datos, para que nunca
/// ocurra por accidente en un entorno real. Se activa en docker compose y en Development.
/// </summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public SeedMode Mode { get; set; } = SeedMode.Never;

    public bool AllowReset { get; set; }
}
