using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InvoiceReminders.Api.Health;

/// <summary>
/// Respuesta JSON por chequeo. La salida por defecto es texto plano y no permitiría a la
/// interfaz distinguir "MongoDB caído" de "SMTP caído".
/// </summary>
internal static class HealthJsonResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var payload = new HealthResponse(
            report.Status.ToString(),
            [.. report.Entries.Select(entry => new HealthCheckEntry(entry.Key, entry.Value.Status.ToString(), entry.Value.Description))]);

        return context.Response.WriteAsJsonAsync(payload);
    }

    private sealed record HealthResponse(string Status, IReadOnlyList<HealthCheckEntry> Checks);

    private sealed record HealthCheckEntry(string Name, string Status, string? Description);
}
