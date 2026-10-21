import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../../api/client';
import type { HealthReport, InvoiceFilter, InvoiceSummary, Meta, ReminderRunResult } from '../../api/types';
import { HealthChip } from '../health/HealthChip';
import { RunResultPanel } from '../reminders/RunResultPanel';
import { CustomerSummary } from './CustomerSummary';
import { InvoiceFilters } from './InvoiceFilters';
import { InvoiceTable } from './InvoiceTable';
import { SummaryCards } from './SummaryCards';

const NO_FILTER: InvoiceFilter = { customerId: '', status: '' };

function messageOf(error: unknown): string {
  if (error instanceof ApiError) return error.message;
  return 'No se pudo contactar la API. Verifique que el contenedor "app" esté en ejecución.';
}

export function InvoicesPage() {
  const [filter, setFilter] = useState<InvoiceFilter>(NO_FILTER);
  const [summary, setSummary] = useState<InvoiceSummary | null>(null);
  const [meta, setMeta] = useState<Meta | null>(null);
  const [health, setHealth] = useState<HealthReport | null>(null);
  const [runResult, setRunResult] = useState<ReminderRunResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const refresh = useCallback(async (currentFilter: InvoiceFilter) => {
    try {
      const [nextSummary, nextHealth] = await Promise.all([api.getSummary(currentFilter), api.getHealth()]);
      setSummary(nextSummary);
      setHealth(nextHealth);
      setError(null);
    } catch (caught) {
      setError(messageOf(caught));
    }
  }, []);

  useEffect(() => {
    void refresh(filter);
  }, [filter, refresh]);

  useEffect(() => {
    api.getMeta().then(setMeta).catch((caught: unknown) => setError(messageOf(caught)));
  }, []);

  const runOperation = async (operation: () => Promise<void>) => {
    setBusy(true);
    setNotice(null);
    try {
      await operation();
      await refresh(filter);
    } catch (caught) {
      if (caught instanceof ApiError && caught.status === 409) {
        setNotice(caught.message);
      } else {
        setError(messageOf(caught));
      }
    } finally {
      setBusy(false);
    }
  };

  const runReminders = (customerId?: string) =>
    runOperation(async () => setRunResult(await api.runReminders(customerId)));

  const resetDemoData = () => {
    const confirmed = window.confirm(
      '¿Restaurar los 3 clientes y las 10 facturas de demostración? Recuerde vaciar también la bandeja de Mailpit.',
    );
    if (!confirmed) return;
    void runOperation(async () => {
      await api.resetDemoData();
      setRunResult(null);
      setNotice('Datos de demostración restaurados.');
    });
  };

  const selectedCustomer = summary?.customers.find((customer) => customer.id === filter.customerId);

  return (
    <main className="page">
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
          <button type="button" className="link" onClick={() => void refresh(filter)}>
            Reintentar
          </button>
        </div>
      )}
      {notice && (
        <div className="alert alert--info" role="status">
          {notice}
        </div>
      )}

      {!summary ? (
        <p className="muted">Cargando facturas…</p>
      ) : (
        <>
          <SummaryCards summary={summary} />
          <CustomerSummary customers={summary.customers} />

          <section className="panel">
            <div className="toolbar">
              <InvoiceFilters filter={filter} customers={summary.customers} onChange={setFilter} />
              <div className="actions">
                <button type="button" className="button" disabled={busy} onClick={() => void runReminders()}>
                  {busy ? 'Procesando…' : 'Ejecutar recordatorios (todos)'}
                </button>
                {selectedCustomer && (
                  <button type="button" className="button button--secondary" disabled={busy} onClick={() => void runReminders(selectedCustomer.id)}>
                    Ejecutar solo para {selectedCustomer.name}
                  </button>
                )}
                {meta?.allowReset && (
                  <button type="button" className="button button--ghost" disabled={busy} onClick={resetDemoData}>
                    Reiniciar datos de demostración
                  </button>
                )}
              </div>
            </div>
            <h2>Facturas ({summary.invoices.length})</h2>
            <InvoiceTable invoices={summary.invoices} />
          </section>

          {runResult && <RunResultPanel result={runResult} mailpitUrl={meta?.mailpitUrl ?? null} />}
        </>
      )}

      <footer className="muted footer">
        {meta && `Versión ${meta.version} · entorno ${meta.environment} · base ${meta.mongoDatabase} · SMTP ${meta.smtpHost}:${meta.smtpPort}`}
      </footer>
    </main>
  );
}
