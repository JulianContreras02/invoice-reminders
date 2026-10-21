using InvoiceReminders.Api.Concurrency;
using InvoiceReminders.Infrastructure.Options;
using InvoiceReminders.Infrastructure.Seed;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace InvoiceReminders.Api.Endpoints;

internal static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/admin/seed/reset", ResetAsync)
            .AddEndpointFilter<SingleOperationFilter>()
            .WithName("ResetDemoData")
            .WithSummary("Restaura los datos de demostración. Solo si Seed__AllowReset=true.");
        return group;
    }

    private static async Task<Results<Ok<SeedResult>, ProblemHttpResult>> ResetAsync(
        IDemoDataSeeder seeder,
        IOptions<SeedOptions> options,
        CancellationToken cancellationToken)
    {
        if (!options.Value.AllowReset)
        {
            return TypedResults.Problem(
                title: "Reinicio deshabilitado",
                detail: "Active Seed__AllowReset=true para permitir reiniciar los datos de demostración.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        return TypedResults.Ok(await seeder.SeedAsync(SeedMode.Always, cancellationToken));
    }
}
