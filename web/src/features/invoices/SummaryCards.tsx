import type { InvoiceSummary } from '../../api/types';
import { countOtherStatuses, KNOWN_STATUSES } from './statusLabels';

export function SummaryCards({ summary }: { summary: InvoiceSummary }) {
  const cards = [
    ...KNOWN_STATUSES.map((info) => ({ label: info.pluralLabel, value: summary.countsByStatus[info.status] ?? 0, tone: info.tone })),
    { label: 'Otros estados', value: countOtherStatuses(summary.countsByStatus), tone: 'other' },
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
