import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { disposalApi, logisticsApi } from '../services/api';
import { AlertCircle, CheckCircle2, Loader2, Recycle, Printer, Copy, Check, ClipboardPaste } from 'lucide-react';
import { copyTextToClipboard, pasteTextFromClipboard } from '../utils/clipboard';

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
  const { user } = useAuth();
  const [searchParams] = useSearchParams();
  const [disposalMethod, setDisposalMethod] = useState('');
  const [itemId, setItemId] = useState(searchParams.get('itemId') || '');
  const [availableItems, setAvailableItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const [certData, setCertData] = useState(null);
  const [copied, setCopied] = useState(false);
  const [isPrinting, setIsPrinting] = useState(false);

  const isRecycler = user?.role === 'Recycler';
  const isUser = user?.role === 'User';

  useEffect(() => {
    const paramId = searchParams.get('itemId');
    if (paramId) {
      setItemId(paramId);
    }
  }, [searchParams]);

  useEffect(() => {
    if (isRecycler && user?.userId) {
      logisticsApi.getRecyclerSchedule(user.userId)
        .then((res) => {
          const items = [];
          (res.data || []).forEach((pickup) => {
            (pickup.items || []).forEach((item) => {
              items.push({
                ...item,
                pickupStatus: pickup.status,
                pickupAddress: pickup.pickupAddress,
              });
            });
          });
          setAvailableItems(items);
        })
        .catch((err) => {
          console.warn('Could not load recycler schedule for quick-selection', err);
        });
    }
  }, [isRecycler, user?.userId]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess(false);

    if (!disposalMethod.trim()) {
      setError('Disposal method is required.');
      return;
    }
    if (!itemId) {
      setError('Please select an item to certify.');
      return;
    }

    setLoading(true);
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
      if (msg.includes('Disposal method is required')) {
        setError('Disposal method is required.');
      } else {
        setError(msg);
      }
    } finally {
      setLoading(false);
    }
  };

  const handlePrint = (cert) => {
    setIsPrinting(true);
    const certContent = `
**ECO-TRACK DISPOSAL CERTIFICATION**

Certificate ID: ${cert.id}
Item: ${cert.itemName} (x${cert.quantity})
Condition: ${cert.itemCondition}
Disposal Method: ${cert.disposalMethod}
Disposed At: ${new Date(cert.disposedAt).toLocaleString()}
Recycler: ${cert.recyclerName}

This certifies that the above item was properly disposed of.
    `.trim();

    const win = window.open('', '_blank');
    win.document.write(`<html><head><title>Disposal Certificate</title>
      <style>body{font-family:Arial,sans-serif;padding:40px;max-width:600px;margin:0 auto;}
      h1{color:#10b981;}pre{background:#f9fafb;padding:20px;border-radius:8px;border:1px solid #e5e7eb;line-height:1.6;}
      .footer{margin-top:20px;font-size:12px;color:#6b7280;text-align:center;}</style></head><body>
      <h1>EcoTrack Disposal Certification</h1><pre>${certContent}</pre>
      <div class="footer">System-generated certification record | ${new Date().toLocaleString()}</div></body></html>`);
    win.document.close();
    win.print();
    setTimeout(() => setIsPrinting(false), 500);
  };

  const copyCertId = async (certId) => {
    const ok = await copyTextToClipboard(certId);
    if (ok) {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  if (isRecycler) {
    return (
      <div style={{ maxWidth: '600px', margin: '2rem auto', padding: '0 1rem' }}>
        <div className="glass-card">
          <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
            <div style={{ display: 'inline-flex', padding: '12px', borderRadius: '16px', background: 'var(--primary-light)', marginBottom: '1rem' }}>
              <Recycle size={28} style={{ color: 'var(--primary)' }} />
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
            <div className="alert alert-success" style={{ marginBottom: '1rem', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '8px', flexWrap: 'wrap' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <CheckCircle2 size={18} />
                <span>Certification created: <strong>{certData.disposalMethod}</strong> (Cert ID: {certData.id})</span>
              </div>
              <button
                type="button"
                onClick={() => copyCertId(certData.id)}
                style={{
                  background: 'rgba(255,255,255,0.15)',
                  border: '1px solid rgba(255,255,255,0.3)',
                  borderRadius: '6px',
                  color: 'white',
                  cursor: 'pointer',
                  padding: '4px 10px',
                  fontSize: '0.75rem',
                  display: 'inline-flex',
                  alignItems: 'center',
                  gap: '4px',
                }}
                title="Copy Certificate ID"
              >
                {copied ? <Check size={12} /> : <Copy size={12} />}
                {copied ? 'Copied!' : 'Copy Cert ID'}
              </button>
            </div>
          )}

          <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1.25rem' }}>
            {availableItems.length > 0 && (
              <div>
                <label style={{ ...labelStyle, fontSize: '0.82rem', color: 'var(--text-secondary)' }}>
                  Quick Select from Your Collected Items
                </label>
                <select
                  className="form-select"
                  value={itemId}
                  onChange={(e) => {
                    setItemId(e.target.value);
                    setError('');
                  }}
                  style={{
                    ...inputStyle,
                    cursor: 'pointer',
                    fontSize: '0.85rem',
                    padding: '0.6rem 0.85rem',
                    backgroundColor: 'var(--bg-surface)',
                    color: 'var(--text-primary)',
                  }}
                  disabled={loading}
                >
                  <option value="" style={{ backgroundColor: '#111827', color: '#9ca3af' }}>-- Choose an item to auto-fill ID --</option>
                  {availableItems.map((item) => (
                    <option key={item.id} value={item.id} style={{ backgroundColor: '#111827', color: '#f9fafb' }}>
                      {item.itemName} (x{item.quantity}) — Status: {item.pickupStatus} (ID: {item.id})
                    </option>
                  ))}
                </select>
              </div>
            )}

            <div>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.4rem' }}>
                <label style={{ ...labelStyle, marginBottom: 0 }}>Pickup Item ID *</label>
                <button
                  type="button"
                  onClick={async () => {
                    const text = await pasteTextFromClipboard();
                    if (text) {
                      setItemId(text.trim());
                      setError('');
                    }
                  }}
                  style={{
                    background: 'none',
                    border: 'none',
                    color: 'var(--primary)',
                    cursor: 'pointer',
                    fontSize: '0.8rem',
                    fontWeight: 600,
                    display: 'inline-flex',
                    alignItems: 'center',
                    gap: '4px',
                    padding: '2px 4px',
                  }}
                  title="Paste ID from clipboard"
                >
                  <ClipboardPaste size={14} />
                  Paste from Clipboard
                </button>
              </div>
              <input
                type="text"
                className="form-input"
                value={itemId}
                onChange={(e) => setItemId(e.target.value)}
                placeholder="Enter PickupItem GUID"
                style={inputStyle}
                disabled={loading}
              />
              <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '4px', lineHeight: 1.4 }}>
                Copy the PickupItem ID from Item Valuations or Schedule Management, use the Quick Select above, or click "Certify" directly on any item.
              </p>
            </div>

            <div>
              <label style={labelStyle}>Disposal Method *</label>
              <select
                className="form-select"
                value={disposalMethod}
                onChange={(e) => { setDisposalMethod(e.target.value); setError(''); }}
                style={{
                  ...inputStyle,
                  cursor: loading ? 'not-allowed' : 'pointer',
                  backgroundColor: 'var(--bg-surface)',
                  color: 'var(--text-primary)',
                }}
                disabled={loading}
              >
                <option value="" style={{ backgroundColor: '#111827', color: '#9ca3af' }}>-- Select disposal method --</option>
                {DISPOSAL_METHODS.map((m) => (
                  <option key={m} value={m} style={{ backgroundColor: '#111827', color: '#f9fafb' }}>{m}</option>
                ))}
              </select>
              {disposalMethod && (
                <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '4px' }}>
                  Selected: {disposalMethod}
                </p>
              )}
            </div>

            {error && (
              <div className="alert alert-danger" style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '0.9rem' }}>
                <AlertCircle size={16} /> {error}
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
  }

  if (isUser) {
    return (
      <div style={{ maxWidth: '800px', margin: '2rem auto', padding: '0 1rem' }}>
        <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
          <div style={{ display: 'inline-flex', padding: '12px', borderRadius: '16px', background: 'var(--primary-light)', marginBottom: '1rem' }}>
            <CheckCircle2 size={28} style={{ color: 'var(--primary)' }} />
          </div>
          <h1 style={{ fontSize: '1.75rem', fontWeight: 800, marginBottom: '0.5rem' }}>
            My Disposal Certifications
          </h1>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>
            View certification details for your disposed items.
          </p>
        </div>

        {!certData && (
          <div className="glass-card" style={{ textAlign: 'center', padding: '4rem 2rem' }}>
            <CheckCircle2 size={48} style={{ color: 'var(--text-secondary)', marginBottom: '1rem' }} />
            <p style={{ color: 'var(--text-secondary)', fontSize: '1rem', margin: 0 }}>No disposal certifications yet.</p>
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', marginTop: '0.5rem' }}>
              Items you've had picked up will be certified as disposed by the recycler.
            </p>
          </div>
        )}

        {certData && certData.length > 0 && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
            {certData.map((cert) => (
              <div key={cert.id} className="glass-card" style={{ overflow: 'hidden' }}>
                <div style={{
                  padding: '1rem 1.5rem', background: 'linear-gradient(135deg, #065F46 0%, #047857 100%)',
                  color: 'white', display: 'flex', justifyContent: 'space-between', alignItems: 'center'
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                    <Recycle size={20} />
                    <div>
                      <div style={{ fontWeight: 700, fontSize: '1rem' }}>Disposal Certified</div>
                      <div style={{ fontSize: '0.75rem', opacity: 0.8 }}>Certificate ID: {cert.id}</div>
                    </div>
                  </div>
                  <div style={{ display: 'flex', gap: '8px' }}>
                    <button
                      onClick={() => copyCertId(cert.id)}
                      style={{
                        padding: '6px 10px', border: '1px solid rgba(255,255,255,0.3)',
                        borderRadius: '8px', background: 'rgba(255,255,255,0.1)',
                        color: 'white', cursor: 'pointer', display: 'flex',
                        alignItems: 'center', gap: '4px', fontSize: '0.8rem'
                      }}
                      title="Copy certificate ID"
                    >
                      {copied ? <Check size={14} /> : <Copy size={14} />}
                      {copied ? 'Copied!' : 'Copy ID'}
                    </button>
                    <button
                      onClick={() => handlePrint(cert)}
                      disabled={isPrinting}
                      style={{
                        padding: '6px 10px', border: '1px solid rgba(255,255,255,0.3)',
                        borderRadius: '8px', background: 'rgba(255,255,255,0.1)',
                        color: 'white', cursor: isPrinting ? 'not-allowed' : 'pointer',
                        display: 'flex', alignItems: 'center', gap: '4px', fontSize: '0.8rem'
                      }}
                      title="Print certificate"
                    >
                      <Printer size={14} /> {isPrinting ? 'Printing...' : 'Print'}
                    </button>
                  </div>
                </div>
                <div style={{ padding: '1.5rem' }}>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))', gap: '1rem' }}>
                    <div style={{ padding: '0.75rem', background: 'var(--bg-secondary)', borderRadius: '10px' }}>
                      <div style={{ fontSize: '0.7rem', fontWeight: 600, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '4px' }}>Item</div>
                      <div style={{ fontWeight: 600, fontSize: '0.95rem' }}>{cert.itemName}</div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Qty: {cert.quantity} | {cert.itemCondition}</div>
                    </div>
                    <div style={{ padding: '0.75rem', background: 'var(--bg-secondary)', borderRadius: '10px' }}>
                      <div style={{ fontSize: '0.7rem', fontWeight: 600, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '4px' }}>Disposal Method</div>
                      <div style={{ fontWeight: 600, fontSize: '0.95rem', color: '#059669' }}>
                        <Recycle size={12} style={{ display: 'inline', verticalAlign: 'middle', marginRight: '4px' }} />
                        {cert.disposalMethod}
                      </div>
                    </div>
                    <div style={{ padding: '0.75rem', background: 'var(--bg-secondary)', borderRadius: '10px' }}>
                      <div style={{ fontSize: '0.7rem', fontWeight: 600, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '4px' }}>Date</div>
                      <div style={{ fontWeight: 600, fontSize: '0.95rem' }}>
                        {new Date(cert.disposedAt).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
                      </div>
                    </div>
                    <div style={{ padding: '0.75rem', background: 'var(--bg-secondary)', borderRadius: '10px' }}>
                      <div style={{ fontSize: '0.7rem', fontWeight: 600, color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '4px' }}>Recycler</div>
                      <div style={{ fontWeight: 600, fontSize: '0.95rem' }}>{cert.recyclerName}</div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>ID: {cert.recyclerId}</div>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    );
  }

  return null;
};

const labelStyle = {
  display: 'block', fontWeight: 600, fontSize: '0.9rem',
  marginBottom: '0.4rem', color: 'var(--text-primary)',
};

const inputStyle = {
  width: '100%', padding: '0.75rem 1rem', fontSize: '0.95rem',
  border: '1px solid var(--border-color)', borderRadius: '8px',
  backgroundColor: 'var(--bg-surface)', color: 'var(--text-primary)',
};

const buttonStyle = {
  padding: '0.75rem 1.5rem', fontSize: '0.95rem', fontWeight: 600,
  border: 'none', borderRadius: '8px', backgroundColor: '#10b981',
  color: '#fff', cursor: 'pointer', display: 'flex',
  alignItems: 'center', justifyContent: 'center', gap: '8px',
};

export default DisposalCertificationView;
