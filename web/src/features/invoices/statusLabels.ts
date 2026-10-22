export type StatusTone = 'first' | 'second' | 'deactivated' | 'paid' | 'other';

export interface StatusInfo {
  status: string;
  label: string;
  pluralLabel: string;
  shortLabel: string;
  tone: StatusTone;
}

// Única fuente de los literales de estado en la interfaz. Los literales son el contrato de datos
// de MongoDB (ADR 0005); aquí solo se traducen para la vista, en el orden del proceso.
export const KNOWN_STATUSES: readonly StatusInfo[] = [
  { status: 'primerrecordatorio', label: 'Primer recordatorio', pluralLabel: 'Primer recordatorio', shortLabel: 'Primer rec.', tone: 'first' },
  { status: 'segundorecordatorio', label: 'Segundo recordatorio', pluralLabel: 'Segundo recordatorio', shortLabel: 'Segundo rec.', tone: 'second' },
  { status: 'desactivado', label: 'Desactivada', pluralLabel: 'Desactivadas', shortLabel: 'Desactivadas', tone: 'deactivated' },
  { status: 'pagada', label: 'Pagada', pluralLabel: 'Pagadas', shortLabel: 'Pagadas', tone: 'paid' },
];

export function describeStatus(status: string): StatusInfo {
  return (
    KNOWN_STATUSES.find((info) => info.status === status) ?? {
      status,
      label: status || '(sin estado)',
      pluralLabel: status,
      shortLabel: status,
      tone: 'other',
    }
  );
}

/** Facturas en estados que la interfaz no conoce. El backend devuelve todos los conteos; aquí solo se agrupan para la vista. */
export function countOtherStatuses(countsByStatus: Record<string, number>): number {
  return Object.entries(countsByStatus)
    .filter(([status]) => !KNOWN_STATUSES.some((info) => info.status === status))
    .reduce((total, [, count]) => total + count, 0);
}
