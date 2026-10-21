import type { InvoiceRow } from '../../api/types';
import { formatDate, formatDateTime, formatMoney } from './format';
import { StatusBadge } from './StatusBadge';

export function InvoiceTable({ invoices }: { invoices: InvoiceRow[] }) {
  if (invoices.length === 0) {
    return <p className="empty">No hay facturas que coincidan con el filtro.</p>;
  }

  return (
    <div className="table-wrapper">
      <table>
        <thead>
          <tr>
            <th scope="col">Factura</th>
            <th scope="col">Cliente</th>
            <th scope="col">Correo</th>
            <th scope="col" className="num">Monto</th>
            <th scope="col">Emisión</th>
            <th scope="col">Vencimiento</th>
            <th scope="col">Estado</th>
            <th scope="col">Actualizada</th>
          </tr>
        </thead>
        <tbody>
          {invoices.map((invoice) => (
            <tr key={invoice.id}>
              <td className="mono">{invoice.id}</td>
              <td>{invoice.customerName}</td>
              <td>{invoice.customerEmail}</td>
              <td className="num">{formatMoney(invoice.amount, invoice.currency)}</td>
              <td>{formatDate(invoice.issuedOn)}</td>
              <td>{formatDate(invoice.dueOn)}</td>
              <td>
                <StatusBadge status={invoice.status} />
              </td>
              <td>{formatDateTime(invoice.updatedAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
