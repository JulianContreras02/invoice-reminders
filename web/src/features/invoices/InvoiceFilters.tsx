import type { CustomerSummary, InvoiceFilter } from '../../api/types';
import { describeStatus, FILTERABLE_STATUSES } from './statusLabels';

interface InvoiceFiltersProps {
  filter: InvoiceFilter;
  customers: CustomerSummary[];
  onChange: (filter: InvoiceFilter) => void;
}

export function InvoiceFilters({ filter, customers, onChange }: InvoiceFiltersProps) {
  return (
    <div className="filters">
      <label>
        Cliente
        <select value={filter.customerId} onChange={(event) => onChange({ ...filter, customerId: event.target.value })}>
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
        <select value={filter.status} onChange={(event) => onChange({ ...filter, status: event.target.value })}>
          <option value="">Todos</option>
          {FILTERABLE_STATUSES.map((status) => (
            <option key={status} value={status}>
              {describeStatus(status).label}
            </option>
          ))}
        </select>
      </label>
    </div>
  );
}
