using InvoiceReminders.Domain.Invoices;

namespace InvoiceReminders.Domain.Reminders;

/// <summary>Una fila de la máquina de estados: desde qué estado, hacia cuál y con qué aviso.</summary>
public sealed record ReminderTransition(InvoiceStatus From, InvoiceStatus To, ReminderKind Kind);
