import { useState } from 'react';
import { api } from '../../api/client';
import type { InvoiceFilter, ReminderRunResult } from '../../api/types';
import { HealthChip } from '../health/HealthChip';
import { RunResultPanel } from '../reminders/RunResultPanel';
import { CustomerSummary } from './CustomerSummary';
import { InvoiceFilters } from './InvoiceFilters';
import { InvoiceTable } from './InvoiceTable';
import { SummaryCards } from './SummaryCards';
import { useInvoiceData } from './useInvoiceData';
import { useMeta } from './useMeta';
import { useOperation } from './useOperation';

const NO_FILTER: InvoiceFilter = { customerId: '', status: '' };

const RESET_CONFIRMATION =
  '¿Restaurar los 3 clientes y las 10 facturas de demostración? Recuerde vaciar también la bandeja de Mailpit.';

function describeRun(result: ReminderRunResult): string {
  const base = `Ejecución terminada: ${result.emailsSent} correos enviados, ${result.updated} facturas actualizadas, ${result.failed} fallos.`;
  if (result.completion === 'StoppedPersistenceUnavailable') return `${base} El lote se detuvo porque MongoDB no respondió.`;
  if (result.completion === 'Cancelled') return `${base} La aplicación se estaba deteniendo.`;
  return base;
}

export function InvoicesPage() {
  const [filter, setFilter] = useState<InvoiceFilter>(NO_FILTER);
  const [runResult, setRunResult] = useState<ReminderRunResult | null>(null);
  const { summary, health, error: loadError, reload } = useInvoiceData(filter);
  const meta = useMeta();
  const operation = useOperation(reload);

  const runReminders = (customerId?: string) =>
    operation.run(async () => {
      const result = await api.runReminders(customerId);
      setRunResult(result);
      return describeRun(result);
    });

  const resetDemoData = () => {
    if (!window.confirm(RESET_CONFIRMATION)) return;
    void operation.run(async () => {
      await api.resetDemoData();
      setRunResult(null);
      return 'Datos de demostración restaurados.';
    });
  };

  const error = operation.error ?? loadError;
  const selectedCustomer = summary?.customers.find((customer) => customer.id === filter.customerId);

  return (
    <main className="page" aria-busy={operation.busy}>
      <header className="page__header">
        <div>
          <h1>Resumen de facturas</h1>
          <p className="muted">Recordatorios de cobro: primer recordatorio → segundo recordatorio → desactivación.</p>
        </div>
        <div className="page__status">
          <HealthChip health={health} />
          {meta?.mailpitUrl && (
            <a href={meta.mailpitUrl} target="_blank" rel="noreferrer">
              Ver correos enviados (Mailpit)
            </a>
          )}
        </div>
      </header>

      {error && (
        <div className="alert alert--error" role="alert">
          {error}{' '}
          {error === loadError && (
            <button type="button" className="link" onClick={reload}>
              Reintentar
            </button>
          )}
        </div>
      )}

      {/* Montada desde el inicio para que los lectores de pantalla anuncien cada resultado. */}
      <div className={operation.notice ? 'alert alert--info' : 'sr-only'} role="status" aria-live="polite">
        {operation.notice}
      </div>

      {!summary ? (
        <p className="muted">Cargando facturas…</p>
      ) : (
        <>
          <SummaryCards summary={summary} />
          <CustomerSummary customers={summary.customers} />

          <section className="panel" aria-labelledby="invoices-title">
            <div className="toolbar">
              <InvoiceFilters filter={filter} customers={summary.customers} disabled={operation.busy} onChange={setFilter} />
              <div className="actions">
                <button type="button" className="button" disabled={operation.busy} onClick={() => void runReminders()}>
                  {operation.busy ? 'Procesando…' : 'Ejecutar recordatorios (todos)'}
                </button>
                {selectedCustomer && (
                  <button
                    type="button"
                    className="button button--secondary"
                    disabled={operation.busy}
                    onClick={() => void runReminders(selectedCustomer.id)}
                  >
                    Ejecutar solo para {selectedCustomer.name}
                  </button>
                )}
                {meta?.allowReset && (
                  <button type="button" className="button button--ghost" disabled={operation.busy} onClick={resetDemoData}>
                    Reiniciar datos de demostración
                  </button>
                )}
              </div>
            </div>
            <h2 id="invoices-title">Facturas ({summary.invoices.length})</h2>
            <InvoiceTable invoices={summary.invoices} />
          </section>

          {runResult && <RunResultPanel result={runResult} mailpitUrl={meta?.mailpitUrl ?? null} />}
        </>
      )}

      <footer className="muted footer">{meta && `Versión ${meta.version} · entorno ${meta.environment}`}</footer>
    </main>
  );
}
