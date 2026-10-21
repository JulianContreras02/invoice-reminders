namespace InvoiceReminders.Domain.Reminders;

/// <summary>Tipo de aviso que se envía al cliente en cada transición.</summary>
public enum ReminderKind
{
    /// <summary>La factura estaba en primer recordatorio y pasa a segundo.</summary>
    SecondReminderNotice,

    /// <summary>La factura estaba en segundo recordatorio y el servicio será desactivado.</summary>
    DeactivationNotice,
}
