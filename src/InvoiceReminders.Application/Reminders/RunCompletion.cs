namespace InvoiceReminders.Application.Reminders;

/// <summary>Cómo terminó una ejecución del proceso.</summary>
public enum RunCompletion
{
    /// <summary>Se revisaron todas las facturas candidatas.</summary>
    Completed,

    /// <summary>La aplicación se estaba deteniendo; se devolvió lo procesado hasta entonces.</summary>
    Cancelled,

    /// <summary>
    /// MongoDB falló al leer o al guardar. El lote se detuvo para no enviar correos que no
    /// podrían registrarse (cada uno sería un duplicado en la siguiente ejecución).
    /// </summary>
    StoppedPersistenceUnavailable,
}
