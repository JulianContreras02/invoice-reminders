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
            .WithSummary("Datos de entorno que la interfaz necesita (sin secretos).");
        return group;
    }

    private static Ok<MetaDto> GetMeta(
        IHostEnvironment environment,
        IOptions<LinksOptions> links,
        IOptions<SeedOptions> seed,
        IOptions<SmtpOptions> smtp,
        IOptions<MongoOptions> mongo)
    {
        var version = typeof(MetaEndpoints).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev";

        return TypedResults.Ok(new MetaDto(
            version,
            environment.EnvironmentName,
            links.Value.MailpitUrl,
            seed.Value.AllowReset,
            smtp.Value.Host,
            smtp.Value.Port,
            mongo.Value.Database));
    }

    internal sealed record MetaDto(
        string Version,
        string Environment,
        string? MailpitUrl,
        bool AllowReset,
        string SmtpHost,
        int SmtpPort,
        string MongoDatabase);
}
