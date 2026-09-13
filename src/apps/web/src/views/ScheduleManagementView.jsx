import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { logisticsApi } from '../services/api';
import { Calendar, Clock, AlertCircle, CheckCircle, Truck, MapPin, Phone, Scale, MessageSquare } from 'lucide-react';

const TIME_SLOTS = [
  'Morning (09:00 - 12:00)',
  'Afternoon (12:00 - 15:00)',
  'Evening (15:00 - 18:00)',
];

export const ScheduleManagementView = () => {
  const { user } = useAuth();
  const [pendingPickups, setPendingPickups] = useState([]);
  const [myPickups, setMyPickups] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedSlot, setSelectedSlot] = useState({});
  const [conflictWarning, setConflictWarning] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const loadData = async () => {
    setLoading(true);
    try {
      const [pendingRes, mineRes] = await Promise.all([
        logisticsApi.getPendingPickups(),
        logisticsApi.getRecyclerSchedule(user.userId),
      ]);
      setPendingPickups(pendingRes.data);
      setMyPickups(mineRes.data);

      const initialSlots = {};
      pendingRes.data.forEach((p) => {
        initialSlots[p.id] = {
          date: p.preferredDate?.split('T')[0] || '',
          timeSlot: p.timeSlot || '',
        };
      });
      setSelectedSlot(initialSlots);
    } catch (err) {
      console.error('Failed to load schedule data', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleSlotChange = (pickupId, field, value) => {
    setSelectedSlot((prev) => ({
      ...prev,
      [pickupId]: { ...prev[pickupId], [field]: value },
    }));
    setConflictWarning('');
  };

  const handleConfirm = async (pickupId) => {
    const slot = selectedSlot[pickupId];
    if (!slot?.date || !slot?.timeSlot) {
      setConflictWarning('Please choose both a date and a time slot.');
      return;
    }

    setConflictWarning('');
    setSuccessMessage('');

    try {
      await logisticsApi.confirmSchedule(pickupId, {
        recyclerId: user.userId,
        scheduledDate: slot.date,
        scheduledTimeSlot: slot.timeSlot,
      });
      setSuccessMessage('Pickup scheduled successfully.');
      loadData();
    } catch (err) {
      if (err.response?.status === 409) {
        setConflictWarning('This time slot is already booked.');
      } else {
        setConflictWarning(err.response?.data?.message || 'Something went wrong. Please try again.');
      }
    }
  };

  if (loading) {
    return <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-secondary)' }}>Loading schedule...</div>;
  }

  return (
    <div style={{ maxWidth: '900px', margin: '2rem auto' }}>
      <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '1.5rem' }}>
        Collection Schedule
      </h1>

      {successMessage && (
        <div className="alert alert-success"><CheckCircle size={18} /><span>{successMessage}</span></div>
      )}
      {conflictWarning && (
        <div className="alert alert-danger"><AlertCircle size={18} /><span>{conflictWarning}</span></div>
      )}

      <div className="glass-card" style={{ marginBottom: '2rem' }}>
        <h2 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: '8px' }}>
          <Truck size={20} style={{ color: 'var(--primary)' }} />
          My Schedule
        </h2>
        {myPickups.length === 0 && <p style={{ color: 'var(--text-secondary)' }}>No pickups scheduled yet.</p>}
        {myPickups.map((pickup) => (
          <div key={pickup.id} style={{ padding: '1rem 0', borderBottom: '1px solid var(--border-color, #333)' }}>
            <strong>{pickup.category}</strong> — {pickup.pickupAddress}
            <div style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', marginTop: '0.25rem', display: 'flex', alignItems: 'center', gap: '6px' }}>
              <Calendar size={14} />
              {new Date(pickup.scheduledDate).toLocaleDateString()} — {pickup.scheduledTimeSlot}
            </div>
          </div>
        ))}
      </div>

      <div className="glass-card">
        <h2 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: '1.25rem' }}>
          Pending Pickups
        </h2>
        {pendingPickups.length === 0 && <p style={{ color: 'var(--text-secondary)' }}>No pending pickups right now.</p>}

        <div style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
          {pendingPickups.map((pickup) => (
            <div
              key={pickup.id}
              style={{
                padding: '1.25rem',
                background: 'rgba(255,255,255,0.03)',
                border: '1px solid var(--border-color, #333)',
                borderRadius: 'var(--radius-md)',
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', flexWrap: 'wrap', gap: '0.5rem' }}>
                <h3 style={{ fontSize: '1.05rem', fontWeight: 700 }}>{pickup.category}</h3>
                <span className="badge badge-pending">
                  <Scale size={12} />
                  {pickup.estimatedWeightKg} kg
                </span>
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.4rem', marginTop: '0.75rem', color: 'var(--text-secondary)', fontSize: '0.9rem' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <MapPin size={14} />
                  {pickup.pickupAddress}
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <Phone size={14} />
                  {pickup.contactPhone}
                </div>
                {pickup.specialInstructions && (
                  <div style={{ display: 'flex', alignItems: 'flex-start', gap: '6px' }}>
                    <MessageSquare size={14} style={{ marginTop: '2px', flexShrink: 0 }} />
                    <span>{pickup.specialInstructions}</span>
                  </div>
                )}
              </div>

              <div
                style={{
                  marginTop: '0.9rem',
                  padding: '0.6rem 0.9rem',
                  background: 'rgba(6, 182, 212, 0.1)',
                  border: '1px solid rgba(6, 182, 212, 0.25)',
                  borderRadius: 'var(--radius-sm)',
                  fontSize: '0.85rem',
                  color: 'var(--accent)',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '6px',
                }}
              >
                <Calendar size={14} />
                Customer requested: {new Date(pickup.preferredDate).toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' })} — {pickup.timeSlot}
              </div>

              <div style={{ display: 'flex', gap: '0.75rem', marginTop: '1rem', flexWrap: 'wrap', alignItems: 'center' }}>
                <input
                  type="date"
                  className="form-input"
                  style={{ width: 'auto' }}
                  value={selectedSlot[pickup.id]?.date || ''}
                  onChange={(e) => handleSlotChange(pickup.id, 'date', e.target.value)}
                />
                <select
                  className="form-input"
                  style={{ width: 'auto' }}
                  value={selectedSlot[pickup.id]?.timeSlot || ''}
                  onChange={(e) => handleSlotChange(pickup.id, 'timeSlot', e.target.value)}
                >
                  <option value="">Select time slot</option>
                  {TIME_SLOTS.map((slot) => (
                    <option key={slot} value={slot}>{slot}</option>
                  ))}
                </select>
                <button className="btn btn-primary" onClick={() => handleConfirm(pickup.id)}>
                  <Clock size={16} /> Confirm
                </button>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};