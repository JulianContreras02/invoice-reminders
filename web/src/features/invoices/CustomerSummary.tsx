import type { CustomerSummary as CustomerSummaryRow } from '../../api/types';

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
              <th scope="col" className="num">Primer rec.</th>
              <th scope="col" className="num">Segundo rec.</th>
              <th scope="col" className="num">Desactivadas</th>
              <th scope="col" className="num">Pagadas</th>
              <th scope="col" className="num">Total</th>
            </tr>
          </thead>
          <tbody>
            {customers.map((customer) => (
              <tr key={customer.id}>
                <td>{customer.name}</td>
                <td>{customer.email}</td>
                <td className="num">{customer.countsByStatus.primerrecordatorio ?? 0}</td>
                <td className="num">{customer.countsByStatus.segundorecordatorio ?? 0}</td>
                <td className="num">{customer.countsByStatus.desactivado ?? 0}</td>
                <td className="num">{customer.countsByStatus.pagada ?? 0}</td>
                <td className="num">{customer.total}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
