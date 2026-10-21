using InvoiceReminders.Domain.Reminders;

namespace InvoiceReminders.Application.Email;

/// <summary>Aviso para una factura encontrada en segundo recordatorio: el servicio va a ser desactivado.</summary>
public sealed class DeactivationNoticeTemplate : IReminderEmailTemplate
{
    public ReminderKind Kind => ReminderKind.DeactivationNotice;

    public RenderedEmail Render(ReminderEmailModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return ReminderEmailLayout.Render(
            $"Factura {model.InvoiceNumber}: aviso de desactivación del servicio",
            [
                $"Estimado(a) {model.CustomerName}:",
                $"La factura {model.InvoiceNumber} por {model.AmountFormatted} {model.Currency}, vencida el {model.DueOnFormatted}, " +
                "se encontraba en segundo recordatorio y continúa sin pago, por lo que el servicio asociado va a ser desactivado.",
                "Para evitarlo, realice el pago y comuníquese con nosotros.",
                "Si ya realizó el pago, por favor ignore este mensaje. Este es un mensaje automático; no responda a este correo.",
            ]);
    }
}
