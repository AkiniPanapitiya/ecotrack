import React, { useState } from 'react';
import { PackageCheck } from 'lucide-react';
import { dropoffApi } from '../services/api';

// ECO-26 / E4.2: Driver/recycler records the drop-off for a scheduled pickup.
const pad = (n) => String(n).padStart(2, '0');
const todayLocal = () => {
  const d = new Date();
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
};
const nowLocalTime = () => {
  const d = new Date();
  return `${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

function DropoffForm({ pickupId, onRecorded, onCancel }) {
  const [dropoffDate, setDropoffDate] = useState(todayLocal());
  const [dropoffTime, setDropoffTime] = useState(nowLocalTime());
  const [conditionNotes, setConditionNotes] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    if (!dropoffDate) {
      setError('Drop-off date is required.');
      return;
    }

    setBusy(true);
    try {
      const res = await dropoffApi.recordDropoff(pickupId, {
        dropoffDate,
        dropoffTime: dropoffTime || null,
        conditionNotes: conditionNotes.trim() || null,
      });
      onRecorded?.(res.data);
    } catch (err) {
      setError(err.response?.data?.message || 'Could not record the drop-off. Please try again.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <form
      onSubmit={handleSubmit}
      noValidate
      style={{
        padding: '0.9rem 1rem',
        background: 'rgba(16, 185, 129, 0.06)',
        border: '1px solid rgba(16, 185, 129, 0.25)',
        borderRadius: 'var(--radius-sm)',
        display: 'flex',
        flexDirection: 'column',
        gap: '0.75rem',
      }}
    >
      <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
        <div className="form-group" style={{ marginBottom: 0 }}>
          <label className="form-label" htmlFor={`dropoff-date-${pickupId}`}>Drop-off date</label>
          <input
            id={`dropoff-date-${pickupId}`}
            type="date"
            className={`form-input${error === 'Drop-off date is required.' ? ' input-error' : ''}`}
            style={{ width: 'auto' }}
            value={dropoffDate}
            max={todayLocal()}
            onChange={(e) => setDropoffDate(e.target.value)}
          />
        </div>
        <div className="form-group" style={{ marginBottom: 0 }}>
          <label className="form-label" htmlFor={`dropoff-time-${pickupId}`}>Drop-off time</label>
          <input
            id={`dropoff-time-${pickupId}`}
            type="time"
            className="form-input"
            style={{ width: 'auto' }}
            value={dropoffTime}
            onChange={(e) => setDropoffTime(e.target.value)}
          />
        </div>
      </div>

      <div className="form-group" style={{ marginBottom: 0 }}>
        <label className="form-label" htmlFor={`dropoff-notes-${pickupId}`}>Item condition notes</label>
        <textarea
          id={`dropoff-notes-${pickupId}`}
          className="form-textarea"
          rows={3}
          maxLength={1000}
          placeholder="e.g. Laptop screen cracked, battery intact"
          value={conditionNotes}
          onChange={(e) => setConditionNotes(e.target.value)}
        />
      </div>

      {error && <p className="form-error" style={{ margin: 0 }}>{error}</p>}

      <div style={{ display: 'flex', gap: '0.6rem' }}>
        <button type="submit" className="btn btn-primary" disabled={busy}>
          <PackageCheck size={16} /> {busy ? 'Saving...' : 'Confirm Drop-off'}
        </button>
        <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={busy}>
          Cancel
        </button>
      </div>
    </form>
  );
}

export default DropoffForm;
