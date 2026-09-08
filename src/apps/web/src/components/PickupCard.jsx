import React, { useState } from 'react';

const statusStyles = {
  Requested: { background: 'var(--warning-bg)', color: '#fbbf24', border: '1px solid rgba(245, 158, 11, 0.3)' },
  Scheduled: { background: 'rgba(6, 182, 212, 0.15)', color: '#22d3ee', border: '1px solid rgba(6, 182, 212, 0.3)' },
  Collected: { background: 'var(--success-bg)', color: '#34d399', border: '1px solid rgba(16, 185, 129, 0.3)' },
  Cancelled: { background: 'var(--danger-bg)', color: '#f87171', border: '1px solid rgba(239, 68, 68, 0.3)' },
};

function PickupCard({ pickup, onCancel, onReschedule }) {
  const [showDateInput, setShowDateInput] = useState(false);
  const [newDate, setNewDate] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const canModify = pickup.status !== 'Collected' && pickup.status !== 'Cancelled';
  const badgeStyle = statusStyles[pickup.status] || statusStyles.Requested;

  const formattedDate = pickup.scheduledDate
    ? new Date(pickup.scheduledDate).toLocaleDateString('en-US', {
        year: 'numeric', month: 'short', day: 'numeric',
      })
    : 'Not scheduled yet';

  const handleCancel = async () => {
    setBusy(true);
    setError('');
    const result = await onCancel(pickup.id);
    if (!result?.success) setError(result?.message || 'Something went wrong.');
    setBusy(false);
  };

  const handleConfirmReschedule = async () => {
    if (!newDate) {
      setError('Please pick a date first.');
      return;
    }
    setBusy(true);
    setError('');
    const result = await onReschedule(pickup.id, newDate);
    if (!result?.success) {
      setError(result?.message || 'Something went wrong.');
    } else {
      setShowDateInput(false);
      setNewDate('');
    }
    setBusy(false);
  };

  return (
    <div className="glass-card" style={{ padding: '1.25rem 1.5rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '0.75rem' }}>
        <div>
          <span className="badge" style={badgeStyle}>{pickup.status}</span>
          <p style={{ marginTop: '0.6rem', fontWeight: 600, color: 'var(--text-primary)' }}>
            {pickup.category || 'E-waste pickup'}
          </p>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginTop: '0.15rem' }}>
            {formattedDate}
          </p>
        </div>

        {canModify && (
          <div style={{ display: 'flex', gap: '0.6rem' }}>
            <button
              className="btn btn-secondary"
              onClick={handleCancel}
              disabled={busy}
              style={{ padding: '0.5rem 1rem', fontSize: '0.85rem' }}
            >
              Cancel
            </button>
            <button
              className="btn btn-primary"
              onClick={() => setShowDateInput((v) => !v)}
              disabled={busy}
              style={{ padding: '0.5rem 1rem', fontSize: '0.85rem' }}
            >
              Reschedule
            </button>
          </div>
        )}
      </div>

      {showDateInput && (
        <div style={{ marginTop: '1rem', display: 'flex', gap: '0.6rem', alignItems: 'center', flexWrap: 'wrap' }}>
          <input
            type="date"
            value={newDate}
            onChange={(e) => setNewDate(e.target.value)}
            style={{
              background: 'rgba(255,255,255,0.06)',
              border: '1px solid var(--border-color)',
              borderRadius: 'var(--radius-sm)',
              padding: '0.5rem 0.75rem',
              color: 'var(--text-primary)',
            }}
          />
          <button
            className="btn btn-primary"
            onClick={handleConfirmReschedule}
            disabled={busy}
            style={{ padding: '0.5rem 1rem', fontSize: '0.85rem' }}
          >
            Confirm
          </button>
        </div>
      )}

      {error && (
        <p style={{ color: 'var(--danger)', fontSize: '0.85rem', marginTop: '0.6rem' }}>
          {error}
        </p>
      )}
    </div>
  );
}

export default PickupCard;