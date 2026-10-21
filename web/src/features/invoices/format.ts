const currencyFormatters = new Map<string, Intl.NumberFormat>();

export function formatMoney(amount: number, currency: string): string {
  let formatter = currencyFormatters.get(currency);
  if (!formatter) {
    formatter = new Intl.NumberFormat('es-CO', { style: 'currency', currency, minimumFractionDigits: 0, maximumFractionDigits: 0 });
    currencyFormatters.set(currency, formatter);
  }
  return formatter.format(amount);
}

// Las fechas sin hora llegan como "2026-08-31": se muestran tal cual en formato local sin
// convertir zonas horarias, para que nunca aparezca el día anterior.
export function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.slice(0, 10).split('-');
  return `${day}/${month}/${year}`;
}

export function formatDateTime(isoDateTime: string): string {
  return new Date(isoDateTime).toLocaleString('es-CO', { dateStyle: 'short', timeStyle: 'short' });
}
