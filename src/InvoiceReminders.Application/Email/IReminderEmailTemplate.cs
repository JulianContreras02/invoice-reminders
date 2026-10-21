using InvoiceReminders.Domain.Reminders;

namespace InvoiceReminders.Application.Email;

/// <summary>Plantilla de un tipo de aviso. Se registran como colección en el contenedor (OCP).</summary>
public interface IReminderEmailTemplate
{
    ReminderKind Kind { get; }

    RenderedEmail Render(ReminderEmailModel model);
}
