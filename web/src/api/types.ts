// Contratos de la API. Reflejan los DTO de C# serializados en camelCase.

export interface InvoiceRow {
  id: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  amount: number;
  currency: string;
  issuedOn: string;
  dueOn: string;
  status: string;
  updatedAt: string;
}

export interface CustomerSummary {
  id: string;
  name: string;
  email: string;
  countsByStatus: Record<string, number>;
  total: number;
}

export interface InvoiceSummary {
  invoices: InvoiceRow[];
  countsByStatus: Record<string, number>;
  customers: CustomerSummary[];
  total: number;
}

export interface InvoiceFilter {
  customerId: string;
  status: string;
}

export type OutcomeKind =
  | 'SentAndUpdated'
  | 'EmailFailed'
  | 'UpdateConflict'
  | 'UpdateFailedAfterSend'
  | 'SkippedNoEmail';

export type RunCompletion = 'Completed' | 'Cancelled' | 'StoppedPersistenceUnavailable';

export interface InvoiceOutcome {
  invoiceId: string;
  customerId: string;
  customerName: string;
  previousStatus: string;
  newStatus: string | null;
  kind: OutcomeKind;
  messageId: string | null;
  error: string | null;
}

export interface ReminderRunResult {
  runId: string;
  startedAt: string;
  duration: string;
  customerId: string | null;
  customersProcessed: number;
  completion: RunCompletion;
  outcomes: InvoiceOutcome[];
  candidates: number;
  updated: number;
  updateConflicts: number;
  updateFailedAfterSend: number;
  emailFailed: number;
  emailsSent: number;
  skipped: number;
  failed: number;
  hasFailures: boolean;
}

export interface SeedResult {
  mode: string;
  applied: boolean;
  customers: number;
  invoices: number;
}

export interface Meta {
  version: string;
  environment: string;
  mailpitUrl: string | null;
  allowReset: boolean;
}

export interface HealthCheck {
  name: string;
  status: string;
  description: string | null;
}

export interface HealthReport {
  status: string;
  checks: HealthCheck[];
}

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}
