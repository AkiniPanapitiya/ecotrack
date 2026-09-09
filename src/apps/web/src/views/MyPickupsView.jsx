import React, { useEffect, useState, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import PickupCard from '../components/PickupCard';
import { usePickupActions } from '../hooks/usePickupActions';
import { PackageSearch } from 'lucide-react';

export const MyPickupsView = () => {
  const { user } = useAuth();
  const { cancelPickup, reschedulePickup, getMyPickups } = usePickupActions();

  const [pickups, setPickups] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadPickups = useCallback(async () => {
    if (!user?.userId) return;
    try {
      setLoading(true);
      setError('');
      const data = await getMyPickups(user.userId);
      setPickups(data);
    } catch (err) {
      console.error('Failed to load pickups:', err);
      setError('Could not load your pickups. Please try again later.');
    } finally {
      setLoading(false);
    }
  }, [user]);

  useEffect(() => {
    loadPickups();
  }, [loadPickups]);

  const handleCancel = async (id) => {
    const result = await cancelPickup(id);
    if (result.success) loadPickups();
    return result;
  };

  const handleReschedule = async (id, newDate) => {
    const result = await reschedulePickup(id, newDate);
    if (result.success) loadPickups();
    return result;
  };

  return (
    <div style={{ maxWidth: '1000px', margin: '1rem auto' }}>
      <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '1.5rem' }}>My Pickups</h1>

      {loading && <p style={{ color: 'var(--text-secondary)' }}>Loading your pickups...</p>}

      {!loading && error && <p style={{ color: 'var(--danger)' }}>{error}</p>}

      {!loading && !error && pickups.length === 0 && (
        <div className="glass-card" style={{ textAlign: 'center', padding: '3rem 1.5rem' }}>
          <PackageSearch size={40} style={{ color: 'var(--text-secondary)', marginBottom: '1rem' }} />
          <p style={{ color: 'var(--text-secondary)' }}>You have no pickup requests yet.</p>
        </div>
      )}

      {!loading && !error && pickups.length > 0 && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {pickups.map((p) => (
            <PickupCard
              key={p.id}
              pickup={p}
              onCancel={handleCancel}
              onReschedule={handleReschedule}
            />
          ))}
        </div>
      )}
    </div>
  );
};

export default MyPickupsView;