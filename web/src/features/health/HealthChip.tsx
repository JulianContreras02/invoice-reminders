import type { HealthReport } from '../../api/types';

export function HealthChip({ health }: { health: HealthReport | null }) {
  if (!health) {
    return <span className="chip chip--muted">Comprobando servicios…</span>;
  }

  const failing = health.checks.filter((check) => check.status !== 'Healthy');
  if (failing.length === 0) {
    return <span className="chip chip--ok">API, MongoDB y SMTP listos</span>;
  }

  const names = failing.map((check) => (check.name === 'smtp' ? 'SMTP' : 'MongoDB')).join(' y ');
  return (
    <span className="chip chip--error" title={failing.map((check) => check.description).join(' ')}>
      {names} no disponible
    </span>
  );
}
