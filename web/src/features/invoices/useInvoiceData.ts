import { useCallback, useEffect, useState } from 'react';
import { api } from '../../api/client';
import { messageOf } from '../../api/errors';
import type { HealthReport, InvoiceFilter, InvoiceSummary } from '../../api/types';

/**
 * Resumen de facturas y salud de los servicios para el filtro vigente.
 * Si el filtro cambia antes de que llegue una respuesta, esa respuesta se descarta:
 * la tabla nunca muestra datos de un filtro anterior.
 */
export function useInvoiceData(filter: InvoiceFilter) {
  const [summary, setSummary] = useState<InvoiceSummary | null>(null);
  const [health, setHealth] = useState<HealthReport | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [reloadCount, setReloadCount] = useState(0);

  useEffect(() => {
    let stale = false;

    Promise.all([api.getSummary(filter), api.getHealth()])
      .then(([nextSummary, nextHealth]) => {
        if (stale) return;
        setSummary(nextSummary);
        setHealth(nextHealth);
        setError(null);
      })
      .catch((caught: unknown) => {
        if (!stale) setError(messageOf(caught));
      });

    return () => {
      stale = true;
    };
  }, [filter, reloadCount]);

  // Recarga con el filtro vigente en el momento de recargar, no con el de cuando empezó una operación.
  const reload = useCallback(() => setReloadCount((count) => count + 1), []);

  return { summary, health, error, reload };
}
