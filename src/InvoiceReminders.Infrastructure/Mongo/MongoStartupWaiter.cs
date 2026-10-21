using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace InvoiceReminders.Infrastructure.Mongo;

/// <summary>
/// Espera a que MongoDB responda antes de abrir el puerto HTTP. Si nunca responde, termina
/// con un mensaje que dice qué hacer, en lugar de fallar en la primera petición.
/// </summary>
internal sealed class MongoStartupWaiter(MongoContext context, ILogger<MongoStartupWaiter> logger)
{
    private const int MaxAttempts = 15;
    private static readonly TimeSpan DelayBetweenAttempts = TimeSpan.FromSeconds(2);

    public async Task WaitUntilAvailableAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await context.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
                logger.LogInformation("MongoDB disponible (base {Database})", context.Database.DatabaseNamespace.DatabaseName);
                return;
            }
            catch (Exception ex) when (ex is MongoException or TimeoutException)
            {
                logger.LogWarning("Esperando MongoDB (intento {Attempt}/{MaxAttempts}): {Reason}", attempt, MaxAttempts, ex.Message);
                await Task.Delay(DelayBetweenAttempts, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "No fue posible conectar con MongoDB. Revise Mongo__ConnectionString o, si ejecuta la API fuera de Docker, " +
            "levante las dependencias con: docker compose up -d mongo mailpit");
    }
}
