import type { InvoiceSummary } from '../../api/types';

interface Card {
  label: string;
  value: number;
  tone: string;
}

export function SummaryCards({ summary }: { summary: InvoiceSummary }) {
  const counts = summary.countsByStatus;
  const known = (counts.primerrecordatorio ?? 0) + (counts.segundorecordatorio ?? 0) + (counts.desactivado ?? 0) + (counts.pagada ?? 0);

  const cards: Card[] = [
    { label: 'Primer recordatorio', value: counts.primerrecordatorio ?? 0, tone: 'first' },
    { label: 'Segundo recordatorio', value: counts.segundorecordatorio ?? 0, tone: 'second' },
    { label: 'Desactivadas', value: counts.desactivado ?? 0, tone: 'deactivated' },
    { label: 'Pagadas', value: counts.pagada ?? 0, tone: 'paid' },
    { label: 'Otros estados', value: summary.total - known, tone: 'other' },
    { label: 'Total', value: summary.total, tone: 'total' },
  ];

  return (
    <section className="cards" aria-label="Facturas por estado">
      {cards.map((card) => (
        <div key={card.label} className={`card card--${card.tone}`}>
          <span className="card__value">{card.value}</span>
          <span className="card__label">{card.label}</span>
        </div>
      ))}
    </section>
  );
}
