namespace InvoiceReminders.UnitTests.Fakes;

/// <summary>Registro ordenado de operaciones compartido por los dobles, para verificar el orden envío → actualización.</summary>
public sealed class OperationJournal
{
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries => _entries;

    public void Record(string entry) => _entries.Add(entry);
}
