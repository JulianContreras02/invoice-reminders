import type { OutcomeKind, ReminderRunResult } from '../../api/types';
import { StatusBadge } from '../invoices/StatusBadge';

const OUTCOME_LABELS: Record<OutcomeKind, { label: string; tone: 'ok' | 'warn' | 'error' | 'muted' }> = {
  SentAndUpdated: { label: 'Enviado y actualizado', tone: 'ok' },
  EmailFailed: { label: 'Correo fallido (estado intacto)', tone: 'error' },
  UpdateConflict: { label: 'Conflicto: otro proceso la cambió', tone: 'warn' },
  UpdateFailedAfterSend: { label: 'Enviado, pero no actualizado', tone: 'error' },
  SkippedNoEmail: { label: 'Omitida: cliente sin correo', tone: 'muted' },
  SkippedNotEligible: { label: 'Omitida: sin transición', tone: 'muted' },
};

interface RunResultPanelProps {
  result: ReminderRunResult;
  mailpitUrl: string | null;
}

export function RunResultPanel({ result, mailpitUrl }: RunResultPanelProps) {
  const counters = [
    { label: 'Candidatas', value: result.candidates },
    { label: 'Correos enviados', value: result.emailsSent },
    { label: 'Actualizadas', value: result.updated },
    { label: 'Correos fallidos', value: result.emailFailed },
    { label: 'Conflictos', value: result.updateConflicts },
    { label: 'Enviadas sin actualizar', value: result.updateFailedAfterSend },
    { label: 'Omitidas', value: result.skipped },
  ];

  return (
    <section className={`panel result ${result.hasFailures ? 'result--failures' : ''}`} aria-live="polite">
      <div className="result__header">
        <h2>Resultado de la ejecución</h2>
        {mailpitUrl && (
          <a className="button button--secondary" href={mailpitUrl} target="_blank" rel="noreferrer">
            Abrir Mailpit
          </a>
        )}
      </div>

      <p className="muted">
        Ejecución <span className="mono">{result.runId}</span> · {result.customersProcessed} cliente(s) · duración {result.duration}
        {result.cancelled && ' · interrumpida por apagado de la aplicación'}
      </p>

      <dl className="counters">
        {counters.map((counter) => (
          <div key={counter.label}>
            <dt>{counter.label}</dt>
            <dd>{counter.value}</dd>
          </div>
        ))}
      </dl>

      {result.outcomes.length === 0 ? (
        <p className="empty">No había facturas en primer ni en segundo recordatorio.</p>
      ) : (
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th scope="col">Factura</th>
                <th scope="col">Cliente</th>
                <th scope="col">Antes</th>
                <th scope="col">Después</th>
                <th scope="col">Desenlace</th>
                <th scope="col">Detalle</th>
              </tr>
            </thead>
            <tbody>
              {result.outcomes.map((outcome) => {
                const info = OUTCOME_LABELS[outcome.kind];
                return (
                  <tr key={outcome.invoiceId}>
                    <td className="mono">{outcome.invoiceId}</td>
                    <td>{outcome.customerName}</td>
                    <td>
                      <StatusBadge status={outcome.previousStatus} />
                    </td>
                    <td>{outcome.newStatus ? <StatusBadge status={outcome.newStatus} /> : '—'}</td>
                    <td>
                      <span className={`outcome outcome--${info.tone}`}>{info.label}</span>
                    </td>
                    <td className="muted">{outcome.error ?? ''}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <p className="hint">Para repetir la demostración desde cero: reinicie los datos y vacíe la bandeja de Mailpit.</p>
    </section>
  );
}
