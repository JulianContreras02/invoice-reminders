import type { CustomerSummary, InvoiceFilter } from '../../api/types';
import { KNOWN_STATUSES } from './statusLabels';

interface InvoiceFiltersProps {
  filter: InvoiceFilter;
  customers: CustomerSummary[];
  disabled: boolean;
  onChange: (filter: InvoiceFilter) => void;
}

export function InvoiceFilters({ filter, customers, disabled, onChange }: InvoiceFiltersProps) {
  return (
    <div className="filters">
      <label>
        Cliente
        <select
          value={filter.customerId}
          disabled={disabled}
          onChange={(event) => onChange({ ...filter, customerId: event.target.value })}
        >
          <option value="">Todos</option>
          {customers.map((customer) => (
            <option key={customer.id} value={customer.id}>
              {customer.name}
            </option>
          ))}
        </select>
      </label>
      <label>
        Estado
        <select value={filter.status} disabled={disabled} onChange={(event) => onChange({ ...filter, status: event.target.value })}>
          <option value="">Todos</option>
          {KNOWN_STATUSES.map((info) => (
            <option key={info.status} value={info.status}>
              {info.label}
            </option>
          ))}
        </select>
      </label>
    </div>
  );
}
