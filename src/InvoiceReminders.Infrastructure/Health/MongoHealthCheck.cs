using InvoiceReminders.Infrastructure.Mongo;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace InvoiceReminders.Infrastructure.Health;

internal sealed class MongoHealthCheck(MongoContext mongo) : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            await mongo.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: timeout.Token);
            return HealthCheckResult.Healthy("MongoDB responde.");
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("MongoDB no responde.", ex);
        }
    }
}
