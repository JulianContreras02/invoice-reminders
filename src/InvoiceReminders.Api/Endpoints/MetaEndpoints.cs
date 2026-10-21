using System.Reflection;
using InvoiceReminders.Infrastructure.Options;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace InvoiceReminders.Api.Endpoints;

internal static class MetaEndpoints
{
    public static RouteGroupBuilder MapMetaEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/meta", GetMeta)
            .WithName("GetMeta")
            .WithSummary("Lo que la interfaz necesita saber del entorno; sin datos de infraestructura.");
        return group;
    }

    private static Ok<MetaDto> GetMeta(IHostEnvironment environment, IOptions<LinksOptions> links, IOptions<SeedOptions> seed)
    {
        var version = typeof(MetaEndpoints).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";
        return TypedResults.Ok(new MetaDto(version, environment.EnvironmentName, links.Value.MailpitUrl, seed.Value.AllowReset));
    }

    internal sealed record MetaDto(string Version, string Environment, string? MailpitUrl, bool AllowReset);
}
