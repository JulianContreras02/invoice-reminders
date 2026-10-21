export type StatusTone = 'first' | 'second' | 'deactivated' | 'paid' | 'other';

interface StatusInfo {
  label: string;
  tone: StatusTone;
}

// Los literales son el contrato de datos de MongoDB (ADR 0005); aquí solo se traducen para la vista.
const KNOWN_STATUSES: Record<string, StatusInfo> = {
  primerrecordatorio: { label: 'Primer recordatorio', tone: 'first' },
  segundorecordatorio: { label: 'Segundo recordatorio', tone: 'second' },
  desactivado: { label: 'Desactivada', tone: 'deactivated' },
  pagada: { label: 'Pagada', tone: 'paid' },
};

export const FILTERABLE_STATUSES = Object.keys(KNOWN_STATUSES);

export function describeStatus(status: string): StatusInfo {
  return KNOWN_STATUSES[status] ?? { label: status, tone: 'other' };
}
