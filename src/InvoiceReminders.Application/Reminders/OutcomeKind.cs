namespace InvoiceReminders.Application.Reminders;

/// <summary>Desenlace del proceso para una factura candidata.</summary>
public enum OutcomeKind
{
    /// <summary>El correo salió y el estado avanzó.</summary>
    SentAndUpdated,

    /// <summary>El correo no salió; el estado quedó intacto.</summary>
    EmailFailed,

    /// <summary>El correo salió, pero otro actor ya había movido la factura (aviso, no fallo).</summary>
    UpdateConflict,

    /// <summary>El correo salió y la actualización falló; la siguiente ejecución lo reintentará.</summary>
    UpdateFailedAfterSend,

    /// <summary>El cliente no tiene un correo válido; no se envía nada.</summary>
    SkippedNoEmail,

    /// <summary>Defensa en profundidad: el estado no tiene transición definida.</summary>
    SkippedNotEligible,
}
