import type { CustomerSummary as CustomerSummaryRow } from '../../api/types';
import { KNOWN_STATUSES } from './statusLabels';

export function CustomerSummary({ customers }: { customers: CustomerSummaryRow[] }) {
  return (
    <section className="panel">
      <h2>Resumen por cliente</h2>
      <div className="table-wrapper">
        <table>
          <thead>
            <tr>
              <th scope="col">Cliente</th>
              <th scope="col">Correo</th>
              {KNOWN_STATUSES.map((info) => (
                <th key={info.status} scope="col" className="num">
                  {info.shortLabel}
                </th>
              ))}
              <th scope="col" className="num">Total</th>
            </tr>
          </thead>
          <tbody>
            {customers.map((customer) => (
              <tr key={customer.id}>
                <th scope="row">{customer.name}</th>
                <td>{customer.email}</td>
                {KNOWN_STATUSES.map((info) => (
                  <td key={info.status} className="num">
                    {customer.countsByStatus[info.status] ?? 0}
                  </td>
                ))}
                <td className="num">{customer.total}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
