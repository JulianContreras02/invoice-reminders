using InvoiceReminders.Domain.Reminders;

namespace InvoiceReminders.Application.Email;

/// <summary>Aviso para una factura encontrada en primer recordatorio: pasa a segundo recordatorio.</summary>
public sealed class SecondReminderTemplate : IReminderEmailTemplate
{
    public ReminderKind Kind => ReminderKind.SecondReminderNotice;

    public RenderedEmail Render(ReminderEmailModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return ReminderEmailLayout.Render(
            $"Factura {model.InvoiceNumber}: ha pasado a segundo recordatorio",
            [
                $"Estimado(a) {model.CustomerName}:",
                $"Le informamos que la factura {model.InvoiceNumber} por {model.AmountFormatted} {model.Currency}, " +
                $"con fecha de vencimiento {model.DueOnFormatted}, no registra pago y ha pasado a segundo recordatorio.",
                "Le invitamos a regularizar el pago a la mayor brevedad para evitar la desactivación del servicio.",
                "Si ya realizó el pago, por favor ignore este mensaje. Este es un mensaje automático; no responda a este correo.",
            ]);
    }
}
