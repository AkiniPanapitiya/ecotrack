import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { disposalApi } from '../services/api';
import { AlertCircle, CheckCircle2, Loader2, Recycle, WifiOff, WifiOff as RecycleIcon } from 'lucide-react';

const DISPOSAL_METHODS = [
  'Recycling',
  'Shredding',
  'Refurbishment',
  'Material Recovery',
  'Safe Disposal',
  'Energy Recovery',
  'Incineration',
  'Donation',
];

export const DisposalCertificationView = () => {
  const { user, token } = useAuth();
  const [disposalMethod, setDisposalMethod] = useState('');
  const [itemId, setItemId] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const [certData, setCertData] = useState(null);

  if (user?.role !== 'Recycler') {
    return (
      <div style={{ textAlign: 'center', padding: '3rem' }}>
        <AlertCircle size={32} style={{ color: 'var(--text-muted)', marginBottom: '1rem' }} />
        <p>This page is only available to Recyclers.</p>
      </div>
    );
  }

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!disposalMethod.trim()) {
      setError('Disposal method is required.');
      return;
    }
    if (!itemId) {
      setError('Please select an item to certify.');
      return;
    }

    setLoading(true);
    setError('');
    try {
      const res = await disposalApi.createCertificate({
        pickupItemId: itemId,
        disposalMethod: disposalMethod.trim(),
      });
      setSuccess(true);
      setCertData(res.data);
      setDisposalMethod('');
      setItemId('');
    } catch (err) {
      const msg = err.response?.data?.message || 'Failed to create certification.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ maxWidth: '600px', margin: '2rem auto', padding: '0 1rem' }}>
      <div className="glass-card">
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <div style={{ display: 'inline-flex', padding: '12px', borderRadius: '16px', background: 'var(--primary-light)', marginBottom: '1rem' }}>
            <RecycleIcon size={28} style={{ color: 'var(--primary)' }} />
          </div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '0.5rem' }}>
            Disposal Certification
          </h1>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>
            Certify that a collected item has been properly disposed of.
          </p>
        </div>

        {error && (
          <div className="alert alert-danger" style={{ marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: '8px' }}>
            <AlertCircle size={18} /> {error}
          </div>
        )}

        {success && certData && (
          <div className="alert alert-success" style={{ marginBottom: '1rem', display: 'flex', alignItems: 'center', gap: '8px' }}>
            <CheckCircle2 size={18} />
            <span>Certification created: <strong>{certData.disposalMethod}</strong> for item {certData.pickupItemId}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
          <div>
            <label style={labelStyle}>Pickup Item ID</label>
            <input
              type="text"
              value={itemId}
              onChange={(e) => setItemId(e.target.value)}
              placeholder="Enter PickupItem GUID"
              style={inputStyle}
              disabled={loading}
            />
            <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '4px' }}>
              Copy the PickupItem ID from your schedule or collection page.
            </p>
          </div>

          <div>
            <label style={labelStyle}>Disposal Method *</label>
            <select
              value={disposalMethod}
              onChange={(e) => { setDisposalMethod(e.target.value); setError(''); }}
              style={{ ...inputStyle, cursor: loading ? 'not-allowed' : 'pointer' }}
              disabled={loading}
            >
              <option value="">-- Select disposal method --</option>
              {DISPOSAL_METHODS.map((m) => (
                <option key={m} value={m}>{m}</option>
              ))}
            </select>
            {disposalMethod && (
              <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '4px' }}>
                Selected: {disposalMethod}
              </p>
            )}
          </div>

          {disposalError && (
            <div className="alert alert-danger" style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '0.9rem' }}>
              <AlertCircle size={16} /> {disposalError}
            </div>
          )}

          <button
            type="submit"
            disabled={loading || !disposalMethod.trim() || !itemId}
            style={{
              ...buttonStyle,
              opacity: (!disposalMethod.trim() || !itemId || loading) ? 0.6 : 1,
            }}
          >
            {loading ? (
              <>
                <Loader2 size={18} className="spin" />
                Creating Certification...
              </>
            ) : (
              <>
                <Recycle size={18} />
                Create Disposal Certification
              </>
            )}
          </button>
        </form>
      </div>
    </div>
  );
};

const labelStyle = {
  display: 'block',
  fontWeight: 600,
  fontSize: '0.9rem',
  marginBottom: '0.4rem',
  color: 'var(--text-primary)',
};

const inputStyle = {
  width: '100%',
  padding: '0.75rem 1rem',
  fontSize: '0.95rem',
  border: '1px solid #d1d5db',
  borderRadius: '8px',
  backgroundColor: '#fff',
  color: '#1f2937',
  transition: 'border-color 0.2s',
};

const buttonStyle = {
  padding: '0.75rem 1.5rem',
  fontSize: '0.95rem',
  fontWeight: 600,
  border: 'none',
  borderRadius: '8px',
  backgroundColor: '#10b981',
  color: '#fff',
  cursor: 'pointer',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  gap: '8px',
  marginTop: '0.5rem',
};

export default DisposalCertificationView;
