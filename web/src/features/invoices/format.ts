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

// TimeSpan de .NET serializado como "hh:mm:ss.fffffff" → "0,15 s" o "1 min 05 s".
export function formatDuration(timeSpan: string): string {
  const [hours = '0', minutes = '0', seconds = '0'] = timeSpan.split(':');
  const totalSeconds = Number(hours) * 3600 + Number(minutes) * 60 + Number(seconds);
  if (Number.isNaN(totalSeconds)) return timeSpan;
  if (totalSeconds < 60) return `${totalSeconds.toLocaleString('es-CO', { maximumFractionDigits: 2 })} s`;
  const wholeMinutes = Math.floor(totalSeconds / 60);
  return `${wholeMinutes} min ${String(Math.round(totalSeconds % 60)).padStart(2, '0')} s`;
}

export function formatDateTime(isoDateTime: string): string {
  return new Date(isoDateTime).toLocaleString('es-CO', { dateStyle: 'short', timeStyle: 'short' });
}
