using InvoiceReminders.Application.Ports;
using InvoiceReminders.Infrastructure.Email;
using InvoiceReminders.Infrastructure.Health;
using InvoiceReminders.Infrastructure.Mongo;
using InvoiceReminders.Infrastructure.Options;
using InvoiceReminders.Infrastructure.Seed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace InvoiceReminders.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra los adaptadores. Es el único lugar de la solución que lee configuración.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        AddValidatedOptions<MongoOptions>(services, configuration, MongoOptions.SectionName);
        AddValidatedOptions<SmtpOptions>(services, configuration, SmtpOptions.SectionName);
        AddValidatedOptions<SeedOptions>(services, configuration, SeedOptions.SectionName);
        AddValidatedOptions<LinksOptions>(services, configuration, LinksOptions.SectionName);

        // Un único cliente: es thread-safe y mantiene el pool de conexiones.
        services.AddSingleton<IMongoClient>(provider =>
        {
            var mongo = provider.GetRequiredService<IOptions<MongoOptions>>().Value;
            var settings = MongoClientSettings.FromConnectionString(mongo.ConnectionString);
            settings.ApplicationName = "invoice-reminders";
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            return new MongoClient(settings);
        });
        services.AddSingleton<MongoContext>();

        services.AddScoped<IInvoiceRepository, MongoInvoiceRepository>();
        services.AddScoped<ICustomerRepository, MongoCustomerRepository>();
        services.AddScoped<IInvoiceSummaryQueries, MongoInvoiceSummaryQueries>();
        services.AddSingleton<IEmailSender, MailKitEmailSender>();

        services.AddTransient<IDemoDataSeeder, MongoDemoDataSeeder>();
        services.AddTransient<MongoIndexInitializer>();
        services.AddTransient<MongoStartupWaiter>();

        services.AddHealthChecks()
            .AddCheck<MongoHealthCheck>("mongo", HealthStatus.Unhealthy, [HealthTags.Ready])
            .AddCheck<SmtpTcpHealthCheck>("smtp", HealthStatus.Degraded, [HealthTags.Ready]);

        return services;
    }

    /// <summary>Esperar MongoDB, crear índices y sembrar según configuración. Se ejecuta antes de abrir el puerto.</summary>
    public static async Task InitializeInfrastructureAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        await provider.GetRequiredService<MongoStartupWaiter>().WaitUntilAvailableAsync(cancellationToken);
        await provider.GetRequiredService<MongoIndexInitializer>().EnsureIndexesAsync(cancellationToken);

        var seed = provider.GetRequiredService<IOptions<SeedOptions>>().Value;
        await provider.GetRequiredService<IDemoDataSeeder>().SeedAsync(seed.Mode, cancellationToken);
    }

    private static void AddValidatedOptions<TOptions>(IServiceCollection services, IConfiguration configuration, string sectionName)
        where TOptions : class =>
        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
