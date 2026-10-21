namespace InvoiceReminders.Application.Reminders;

/// <summary>
/// Resultado de una ejecución. Los contadores se derivan de <see cref="Outcomes"/>, así que
/// estas invariantes se cumplen por construcción (y están cubiertas por pruebas):
/// Candidates = EmailsSent + EmailFailed + Skipped;
/// EmailsSent = Updated + UpdateConflicts + UpdateFailedAfterSend.
/// </summary>
public sealed record ReminderRunResult(
    Guid RunId,
    DateTimeOffset StartedAt,
    TimeSpan Duration,
    string? CustomerId,
    int CustomersProcessed,
    RunCompletion Completion,
    IReadOnlyList<InvoiceOutcome> Outcomes)
{
    public int Candidates => Outcomes.Count;

    public int Updated => Count(OutcomeKind.SentAndUpdated);

    public int UpdateConflicts => Count(OutcomeKind.UpdateConflict);

    public int UpdateFailedAfterSend => Count(OutcomeKind.UpdateFailedAfterSend);

    public int EmailFailed => Count(OutcomeKind.EmailFailed);

    public int EmailsSent => Updated + UpdateConflicts + UpdateFailedAfterSend;

    public int Skipped => Count(OutcomeKind.SkippedNoEmail);

    /// <summary>Fallos reales; los conflictos se reportan aparte como aviso.</summary>
    public int Failed => EmailFailed + UpdateFailedAfterSend;

    public bool HasFailures => Failed > 0 || Completion == RunCompletion.StoppedPersistenceUnavailable;

    private int Count(OutcomeKind kind) => Outcomes.Count(outcome => outcome.Kind == kind);
}
