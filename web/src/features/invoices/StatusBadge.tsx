import { describeStatus } from './statusLabels';

export function StatusBadge({ status }: { status: string }) {
  const { label, tone } = describeStatus(status);
  return (
    <span className={`badge badge--${tone}`} title={status}>
      {label}
    </span>
  );
}
