import React, { useEffect, useState } from 'react';
import { Calendar, Clock, MessageSquare, PackageCheck } from 'lucide-react';
import { dropoffApi } from '../services/api';

// ECO-26 / E4.2: Shows the drop-off record linked to a collected pickup.
function DropoffDetails({ pickupId }) {
  const [record, setRecord] = useState(null);
  const [loading, setLoading] = useState(true);
  const [missing, setMissing] = useState(false);

  useEffect(() => {
    let active = true;
    setLoading(true);
    dropoffApi
      .getDropoff(pickupId)
      .then((res) => { if (active) setRecord(res.data); })
      .catch(() => { if (active) setMissing(true); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [pickupId]);

  if (loading) {
    return <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.5rem' }}>Loading drop-off details...</p>;
  }

  // Pickups collected before ECO-26 have no drop-off record.
  if (missing || !record) {
    return <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.5rem' }}>No drop-off record available.</p>;
  }

  const formattedDate = new Date(record.dropoffDate).toLocaleDateString('en-US', {
    year: 'numeric', month: 'short', day: 'numeric',
  });

  return (
    <div
      style={{
        marginTop: '0.75rem',
        padding: '0.6rem 0.9rem',
        background: 'rgba(16, 185, 129, 0.08)',
        border: '1px solid rgba(16, 185, 129, 0.25)',
        borderRadius: 'var(--radius-sm)',
        fontSize: '0.85rem',
        color: 'var(--text-secondary)',
        display: 'flex',
        flexDirection: 'column',
        gap: '0.35rem',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontWeight: 600, color: 'var(--primary)' }}>
        <PackageCheck size={14} /> Drop-off record
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', flexWrap: 'wrap' }}>
        <Calendar size={14} /> {formattedDate}
        {record.dropoffTime && (
          <>
            <Clock size={14} style={{ marginLeft: '0.5rem' }} /> {record.dropoffTime}
          </>
        )}
      </div>
      <div style={{ display: 'flex', alignItems: 'flex-start', gap: '6px' }}>
        <MessageSquare size={14} style={{ marginTop: '2px', flexShrink: 0 }} />
        <span>{record.conditionNotes || 'No condition notes recorded.'}</span>
      </div>
    </div>
  );
}

export default DropoffDetails;
