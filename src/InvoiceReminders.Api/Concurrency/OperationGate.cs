namespace InvoiceReminders.Api.Concurrency;

/// <summary>Permite una sola operación de escritura masiva a la vez en esta instancia (ejecutar o reiniciar).</summary>
internal sealed class OperationGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool TryEnter() => _semaphore.Wait(TimeSpan.Zero);

    public void Exit() => _semaphore.Release();

    public void Dispose() => _semaphore.Dispose();
}
