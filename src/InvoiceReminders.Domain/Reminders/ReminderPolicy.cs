using System.Diagnostics.CodeAnalysis;
using InvoiceReminders.Domain.Invoices;

namespace InvoiceReminders.Domain.Reminders;

/// <summary>
/// Máquina de estados de los recordatorios. Añadir una transición es añadir una fila
/// aquí y una plantilla de correo; el caso de uso no cambia (principio abierto/cerrado).
/// </summary>
public static class ReminderPolicy
{
    public static IReadOnlyList<ReminderTransition> Transitions { get; } =
    [
        new(InvoiceStatus.FirstReminder, InvoiceStatus.SecondReminder, ReminderKind.SecondReminderNotice),
        new(InvoiceStatus.SecondReminder, InvoiceStatus.Deactivated, ReminderKind.DeactivationNotice),
    ];

    /// <summary>Estados que el proceso debe revisar; cualquier otro se ignora.</summary>
    public static IReadOnlyCollection<InvoiceStatus> EligibleStatuses { get; } =
        Transitions.Select(transition => transition.From).ToArray();

    public static bool TryGetTransition(InvoiceStatus current, [NotNullWhen(true)] out ReminderTransition? transition)
    {
        transition = Transitions.FirstOrDefault(candidate => candidate.From == current);
        return transition is not null;
    }
}
