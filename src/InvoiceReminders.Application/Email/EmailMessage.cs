namespace InvoiceReminders.Application.Email;

/// <summary>
/// Correo listo para enviar. Nombre y dirección van separados (nunca "Nombre &lt;email&gt;")
/// para que un nombre como "Pérez &amp; Asociados S.A.S." no rompa el parseo de la dirección.
/// <see cref="MessageId"/> va sin corchetes angulares.
/// </summary>
public sealed record EmailMessage(
    string ToName,
    string ToAddress,
    string Subject,
    string TextBody,
    string HtmlBody,
    string MessageId,
    IReadOnlyDictionary<string, string> Headers);
