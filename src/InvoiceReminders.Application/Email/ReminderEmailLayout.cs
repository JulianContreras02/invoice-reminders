using System.Net;
using System.Text;

namespace InvoiceReminders.Application.Email;

/// <summary>Arma texto plano y HTML a partir de los mismos párrafos. Todo texto se codifica antes de insertarse en el HTML.</summary>
internal static class ReminderEmailLayout
{
    public static RenderedEmail Render(string subject, IReadOnlyList<string> paragraphs)
    {
        var text = string.Join(Environment.NewLine + Environment.NewLine, paragraphs);

        var html = new StringBuilder();
        html.Append("<!doctype html><html lang=\"es\"><body style=\"font-family:Arial,sans-serif;color:#1f2933\">");
        html.Append("<h2>").Append(WebUtility.HtmlEncode(subject)).Append("</h2>");
        foreach (var paragraph in paragraphs)
        {
            html.Append("<p>").Append(WebUtility.HtmlEncode(paragraph)).Append("</p>");
        }

        html.Append("</body></html>");
        return new RenderedEmail(subject, text, html.ToString());
    }
}
