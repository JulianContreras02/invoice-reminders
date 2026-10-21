using System.Text.Json.Serialization;
using InvoiceReminders.Api;
using InvoiceReminders.Api.Concurrency;
using InvoiceReminders.Api.Endpoints;
using InvoiceReminders.Api.Errors;
using InvoiceReminders.Api.Health;
using InvoiceReminders.Application;
using InvoiceReminders.Infrastructure;
using InvoiceReminders.Infrastructure.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Composition root: el único lugar donde se conectan las capas (ADR 0002).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOptions<LinksOptions>().BindConfiguration(LinksOptions.SectionName);
builder.Services.AddSingleton<OperationGate>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// Esperar MongoDB, crear índices y sembrar ANTES de abrir el puerto. Si falla, el proceso
// termina con un mensaje claro en el log en vez de servir una interfaz vacía.
try
{
    await using var startupScope = app.Services.CreateAsyncScope();
    await startupScope.ServiceProvider.GetRequiredService<IInfrastructureInitializer>().InitializeAsync(app.Lifetime.ApplicationStopping);
}
#pragma warning disable CA1031 // Frontera del proceso: cualquier fallo de arranque debe registrarse y terminar con código distinto de cero.
catch (Exception ex)
#pragma warning restore CA1031
{
    app.Logger.LogCritical(ex, "La aplicación no pudo iniciar: {Reason}", ex.Message);
    return 1;
}

app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.MapScalarApiReference();

var api = app.MapGroup("/api");
api.MapInvoiceEndpoints();
api.MapReminderEndpoints();
api.MapAdminEndpoints();
api.MapMetaEndpoints();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains(HealthTags.Ready),
    ResponseWriter = HealthJsonResponseWriter.WriteAsync,
});

// Una ruta /api inexistente es un 404, no la página de React; el resto de rutas sirve la SPA.
app.MapFallback("/api/{**path}", () => TypedResults.NotFound());
app.MapFallbackToFile("index.html");

await app.RunAsync();
return 0;
