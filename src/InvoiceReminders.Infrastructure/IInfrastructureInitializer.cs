namespace InvoiceReminders.Infrastructure;

/// <summary>Prepara la infraestructura antes de atender peticiones: esperar MongoDB, crear índices y sembrar.</summary>
public interface IInfrastructureInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
