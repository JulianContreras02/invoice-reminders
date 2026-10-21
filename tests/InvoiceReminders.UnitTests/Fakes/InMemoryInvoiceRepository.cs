using InvoiceReminders.Application.Ports;
using InvoiceReminders.Application.Reminders;
using InvoiceReminders.Domain.Invoices;

namespace InvoiceReminders.UnitTests.Fakes;

/// <summary>
/// Repositorio en memoria con la misma semántica que el de MongoDB: filtra por cliente y estado,
/// y el avance de estado solo ocurre si la factura sigue en el estado esperado (compare-and-set).
/// </summary>
public sealed class InMemoryInvoiceRepository(OperationJournal journal) : IInvoiceRepository
{
    private readonly Dictionary<string, Invoice> _invoices = new(StringComparer.Ordinal);
    private readonly Dictionary<string, InvoiceStatus> _concurrentChanges = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Exception> _readFailures = new(StringComparer.Ordinal);
    private readonly List<StatusChange> _changes = [];
    private Exception? _updateFailure;

    public IReadOnlyList<StatusChange> Changes => _changes;

    public bool? LastUpdateTokenCouldBeCancelled { get; private set; }

    public InMemoryInvoiceRepository Add(params Invoice[] invoices)
    {
        foreach (var invoice in invoices)
        {
            _invoices[invoice.Id] = invoice;
        }

        return this;
    }

    public Invoice Get(string invoiceId) => _invoices[invoiceId];

    /// <summary>Simula que otro proceso cambia la factura justo antes de la actualización.</summary>
    public void SimulateConcurrentChange(string invoiceId, InvoiceStatus status) => _concurrentChanges[invoiceId] = status;

    /// <summary>Hace que toda actualización lance la excepción indicada (p. ej. MongoDB caído).</summary>
    public void FailUpdatesWith(Exception exception) => _updateFailure = exception;

    /// <summary>Hace que la lectura de las facturas de un cliente lance la excepción indicada.</summary>
    public void FailReadsFor(string customerId, Exception exception) => _readFailures[customerId] = exception;

    public Task<IReadOnlyList<Invoice>> GetEligibleByCustomerAsync(
        string customerId,
        IReadOnlyCollection<InvoiceStatus> statuses,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_readFailures.TryGetValue(customerId, out var failure))
        {
            throw failure;
        }

        IReadOnlyList<Invoice> eligible = _invoices.Values
            .Where(invoice => invoice.CustomerId == customerId && statuses.Contains(invoice.Status))
            .OrderBy(invoice => invoice.Id, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(eligible);
    }

    public Task<bool> TryAdvanceStatusAsync(
        string invoiceId,
        InvoiceStatus expectedCurrent,
        InvoiceStatus next,
        StatusChange change,
        CancellationToken cancellationToken)
    {
        journal.Record($"update:{invoiceId}");
        LastUpdateTokenCouldBeCancelled = cancellationToken.CanBeCanceled;
        cancellationToken.ThrowIfCancellationRequested();

        if (_updateFailure is not null)
        {
            throw _updateFailure;
        }

        if (_concurrentChanges.Remove(invoiceId, out var concurrentStatus))
        {
            _invoices[invoiceId] = _invoices[invoiceId] with { Status = concurrentStatus };
        }

        var current = _invoices[invoiceId];
        if (current.Status != expectedCurrent)
        {
            return Task.FromResult(false);
        }

        _invoices[invoiceId] = current with { Status = next, UpdatedAt = change.At };
        _changes.Add(change);
        return Task.FromResult(true);
    }
}
