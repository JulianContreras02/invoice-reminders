using InvoiceReminders.Application;
using InvoiceReminders.Application.Email;
using InvoiceReminders.Application.Ports;
using InvoiceReminders.Application.Reminders;
using InvoiceReminders.Infrastructure;
using InvoiceReminders.Infrastructure.Options;
using InvoiceReminders.Infrastructure.Seed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InvoiceReminders.UnitTests.Composition;

/// <summary>
/// Valida el grafo real de dependencias sin abrir conexiones: el cliente de MongoDB no se
/// conecta hasta la primera operación, así que ninguna de estas pruebas necesita Docker.
/// </summary>
public sealed class DependencyInjectionTests
{
    private static readonly Dictionary<string, string?> ValidSettings = new()
    {
        ["Mongo:ConnectionString"] = "mongodb://localhost:27017",
        ["Mongo:Database"] = "invoice_reminders_tests",
        ["Smtp:Host"] = "localhost",
        ["Smtp:Port"] = "1025",
        ["Smtp:FromAddress"] = "recordatorios@invoice-reminders.test",
    };

    [Fact]
    public void Container_WithValidConfiguration_ResolvesEveryServiceTheApiUses()
    {
        using var provider = BuildProvider(ValidSettings);
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ProcessRemindersUseCase>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IInvoiceSummaryQueries>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDemoDataSeeder>());
        Assert.NotNull(provider.GetRequiredService<IEmailSender>());
        Assert.Equal(2, provider.GetServices<IReminderEmailTemplate>().Count());
    }

    [Fact]
    public void Options_WithoutMongoConnectionString_FailNamingTheEnvironmentVariable()
    {
        var settings = new Dictionary<string, string?>(ValidSettings) { ["Mongo:ConnectionString"] = null };
        using var provider = BuildProvider(settings);

        var exception = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<MongoOptions>>().Value);

        Assert.Contains("Mongo__ConnectionString", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Options_WithSmtpUserButNoPassword_FailNamingBothVariables()
    {
        var settings = new Dictionary<string, string?>(ValidSettings) { ["Smtp:Username"] = "usuario" };
        using var provider = BuildProvider(settings);

        var exception = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SmtpOptions>>().Value);

        Assert.Contains("Smtp__Username", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SeedOptions_ByDefault_NeitherSeedNorAllowReset()
    {
        using var provider = BuildProvider(ValidSettings);

        var seed = provider.GetRequiredService<IOptions<SeedOptions>>().Value;

        Assert.Equal(SeedMode.Never, seed.Mode);
        Assert.False(seed.AllowReset);
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddApplication();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
