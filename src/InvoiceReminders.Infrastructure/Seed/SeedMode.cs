namespace InvoiceReminders.Infrastructure.Seed;

public enum SeedMode
{
    /// <summary>No siembra nada (valor por defecto, seguro para entornos reales).</summary>
    Never,

    /// <summary>Siembra solo si la base no tiene clientes (docker compose y Development).</summary>
    IfEmpty,

    /// <summary>Borra y vuelve a sembrar los mismos datos (botón "Reiniciar datos").</summary>
    Always,
}
