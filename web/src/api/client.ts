import type {
  HealthReport,
  InvoiceFilter,
  InvoiceSummary,
  Meta,
  ProblemDetails,
  ReminderRunResult,
  SeedResult,
} from './types';

export class ApiError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  // HeadersInit puede ser Headers, un arreglo de pares o un objeto: Headers los normaliza todos
  // (propagarlo con "..." no compila con TS estricto y pierde los valores de una instancia de Headers).
  const headers = new Headers(init?.headers);
  if (!headers.has('Accept')) headers.set('Accept', 'application/json');
  const response = await fetch(path, { ...init, headers });

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as ProblemDetails | null;
    throw new ApiError(problem?.detail ?? problem?.title ?? `Error HTTP ${response.status}`, response.status);
  }

  return (await response.json()) as T;
}

function query(values: Record<string, string>): string {
  const params = new URLSearchParams(Object.entries(values).filter(([, value]) => value !== ''));
  const text = params.toString();
  return text ? `?${text}` : '';
}

export const api = {
  getSummary: (filter: InvoiceFilter) =>
    request<InvoiceSummary>(`/api/invoices${query({ customerId: filter.customerId, status: filter.status })}`),

  runReminders: (customerId?: string) =>
    request<ReminderRunResult>(`/api/reminders/run${query({ customerId: customerId ?? '' })}`, { method: 'POST' }),

  resetDemoData: () => request<SeedResult>('/api/admin/seed/reset', { method: 'POST' }),

  getMeta: () => request<Meta>('/api/meta'),

  // /health/ready responde 503 con el mismo JSON cuando MongoDB no está disponible.
  getHealth: async (): Promise<HealthReport> => {
    const response = await fetch('/health/ready', { headers: { Accept: 'application/json' } });
    return (await response.json()) as HealthReport;
  },
};
