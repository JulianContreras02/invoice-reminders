using InvoiceReminders.Api.Concurrency;
using InvoiceReminders.Application.Reminders;
using Microsoft.AspNetCore.Http.HttpResults;

namespace InvoiceReminders.Api.Endpoints;

internal static class ReminderEndpoints
{
    public static RouteGroupBuilder MapReminderEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/reminders/run", RunAsync)
            .AddEndpointFilter<SingleOperationFilter>()
            .WithName("RunReminders")
            .WithSummary("Ejecuta el proceso de recordatorios para todos los clientes o para uno (?customerId=).");
        return group;
    }

    /// <remarks>
    /// El lote usa el token de apagado de la aplicación, no el de la petición: si el usuario
    /// cierra la pestaña, el proceso no queda a medias entre un envío y su actualización.
    /// </remarks>
    private static async Task<Ok<ReminderRunResult>> RunAsync(
        string? customerId,
        ProcessRemindersUseCase useCase,
        IHostApplicationLifetime lifetime) =>
        TypedResults.Ok(await useCase.RunAsync(string.IsNullOrWhiteSpace(customerId) ? null : customerId, lifetime.ApplicationStopping));
}
