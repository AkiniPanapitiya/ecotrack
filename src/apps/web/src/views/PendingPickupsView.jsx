import React, { useEffect, useState, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { logisticsApi } from '../services/api';
import { Truck, PackageSearch, Clock, MapPin, Phone, ArrowRight } from 'lucide-react';

function PendingPickupsView() {
  const { user } = useAuth();
  const [pickups, setPickups] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const loadPickups = useCallback(async () => {
    if (!user?.userId) return;
    try {
      setLoading(true);
      setError('');
      const data = await logisticsApi.getPendingPickups();
      setPickups(data || []);
    } catch (err) {
      setError('Could not load pending pickups. Please try again.');
    } finally {
      setLoading(false);
    }
  }, [user]);

  useEffect(() => {
    loadPickups();
  }, [loadPickups]);

  return (
    <div style={{ maxWidth: '900px', margin: '2rem auto', padding: '0 1rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '0.25rem' }}>Pending Pickups</h1>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
            View available pickup requests and submit valuations for collected items
          </p>
        </div>
        <span style={{ background: 'var(--primary-light)', color: 'var(--primary)', padding: '0.35rem 0.75rem', borderRadius: '20px', fontSize: '0.8rem', fontWeight: 700, display: 'flex', alignItems: 'center', gap: '4px' }}>
          <PackageSearch size={13} />
          {pickups.length} request{pickups.length !== 1 ? 's' : ''}
        </span>
      </div>

      {loading && <p style={{ color: 'var(--text-secondary)', textAlign: 'center', padding: '2rem' }}>Loading pending pickups...</p>}

      {!loading && error && <div className="alert alert-danger" style={{ maxWidth: '900px', margin: '2rem auto', padding: '1rem 1.25rem' }}>
        <Truck size={18} style={{ color: 'var(--danger)', marginRight: '0.5rem', flexShrink: 0 }} />
        <span>{error}</span>
      </div>}

      {!loading && !error && pickups.length === 0 && (
        <div className="glass-card" style={{ textAlign: 'center', padding: '3rem 1.5rem' }}>
          <Truck size={40} style={{ color: 'var(--text-secondary)', marginBottom: '1rem', display: 'block' }} />
          <p style={{ color: 'var(--text-secondary)', fontSize: '1rem', marginBottom: '1.5rem' }}>No pending pickup requests available.</p>
          <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem' }}>New pickup requests from users will appear here once submitted.</p>
        </div>
      )}

      {!loading && !error && pickups.length > 0 && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {pickups.map((p) => (
            <div key={p.id} className="glass-card" style={{ padding: '1.25rem', borderLeft: '4px solid var(--warning)' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.75rem', flexWrap: 'wrap', gap: '0.5rem' }}>
                <div>
                  <span style={{ fontWeight: 800, fontSize: '1.05rem', color: 'var(--text-primary)' }}>{p.category}</span>
                  <span style={{ background: p.status === 'Pending' ? 'var(--warning)' : p.status === 'Scheduled' ? 'var(--info)' : 'var(--muted)', color: p.status === 'Pending' ? '#000' : '#fff', padding: '0.2rem 0.5rem', borderRadius: '4px', fontSize: '0.7rem', fontWeight: 700, marginLeft: '0.5rem', textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                    {p.status}
                  </span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', color: 'var(--text-secondary)', fontSize: '0.85rem' }}>
                  <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}><Clock size={13} />{p.timeSlot}</span>
                  <span>{new Date(p.preferredDate).toLocaleDateString()}</span>
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '0.75rem', marginBottom: '1rem', color: 'var(--text-secondary)', fontSize: '0.88rem' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}><MapPin size={14} />{p.pickupAddress}</div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}><Phone size={14} />{p.contactPhone}</div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}><PackageSearch size={14} />{p.estimatedWeightKg} kg estimated</div>
              </div>

              {p.items && p.items.length > 0 && (
                <div style={{ marginBottom: '1rem', padding: '0.75rem', background: 'rgba(255,255,255,0.03)', borderRadius: 'var(--radius-md)', maxHeight: '120px', overflow: 'auto' }}>
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginBottom: '0.5rem', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.5px' }}>Items ({p.items.length})</div>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.35rem' }}>
                    {p.items.map((item, idx) => (
                      <div key={idx} style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.88rem', padding: '0.25rem 0', borderBottom: idx < p.items.length - 1 ? '1px solid var(--border-color)' : 'none' }}>
                        <span style={{ color: 'var(--text-primary)' }}>{item.itemName} <span style={{ color: 'var(--text-secondary)', fontWeight: 400 }}>× {item.quantity}</span></span>
                        <span style={{ color: 'var(--text-secondary)', fontSize: '0.8rem' }}>{item.itemCondition}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              <a href={`/valuations?pickupId=${p.id}`} style={{ display: 'inline-flex', alignItems: 'center', gap: '6px', padding: '0.6rem 1.25rem', background: 'var(--primary)', color: '#fff', borderRadius: 'var(--radius-md)', textDecoration: 'none', fontWeight: 600, fontSize: '0.9rem', marginTop: '0.5rem' }}>
                View Items for Valuation
                <ArrowRight size={16} />
              </a>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export { PendingPickupsView };
