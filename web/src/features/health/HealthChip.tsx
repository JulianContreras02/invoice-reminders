import type { HealthReport } from '../../api/types';

const SERVICE_NAMES: Record<string, string> = { mongo: 'MongoDB', smtp: 'SMTP' };

export function HealthChip({ health }: { health: HealthReport | null }) {
  if (!health) {
    return <span className="chip chip--muted">Comprobando servicios…</span>;
  }

  const failing = health.checks.filter((check) => check.status !== 'Healthy');
  if (failing.length === 0) {
    return <span className="chip chip--ok">API, MongoDB y SMTP listos</span>;
  }

  const names = failing.map((check) => SERVICE_NAMES[check.name] ?? check.name).join(' y ');
  return (
    <div className="health" role="status">
      <span className="chip chip--error">{names} no disponible</span>
      {failing.map((check) => check.description && <small key={check.name}>{check.description}</small>)}
    </div>
  );
}
