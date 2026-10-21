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
    /// <summary>Registra los adaptadores y su configuración validada (MongoDB, SMTP y semilla).</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        AddValidatedOptions<MongoOptions>(services, configuration, MongoOptions.SectionName);
        AddValidatedOptions<SmtpOptions>(services, configuration, SmtpOptions.SectionName);
        AddValidatedOptions<SeedOptions>(services, configuration, SeedOptions.SectionName);

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
        services.AddTransient<IInfrastructureInitializer, InfrastructureInitializer>();

        services.AddHealthChecks()
            .AddCheck<MongoHealthCheck>("mongo", HealthStatus.Unhealthy, [HealthTags.Ready])
            .AddCheck<SmtpTcpHealthCheck>("smtp", HealthStatus.Degraded, [HealthTags.Ready]);

        return services;
    }

    private static void AddValidatedOptions<TOptions>(IServiceCollection services, IConfiguration configuration, string sectionName)
        where TOptions : class =>
        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
}
