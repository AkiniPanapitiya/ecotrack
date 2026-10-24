import React from 'react';
import { Clock, CheckCircle2, RotateCcw } from 'lucide-react';

const STYLES = {
  Pending: { background: '#78350f', color: '#fde68a', Icon: Clock },
  Paid: { background: '#065f46', color: '#a7f3d0', Icon: CheckCircle2 },
  Refunded: { background: '#1e3a8a', color: '#bfdbfe', Icon: RotateCcw },
};

/// Shows a payment record status: Pending, Paid or Refunded (Story 20)
export const PaymentStatusBadge = ({ status, prefix = '' }) => {
  const style = STYLES[status];
  if (!style) return null;
  const { Icon } = style;
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: '0.35rem',
      padding: '0.2rem 0.65rem', fontSize: '0.7rem', fontWeight: 600,
      borderRadius: '9999px', textTransform: 'uppercase', letterSpacing: '0.03em',
      background: style.background, color: style.color, whiteSpace: 'nowrap',
    }}>
      <Icon size={10} /> {prefix}{status}
    </span>
  );
};

export default PaymentStatusBadge;
