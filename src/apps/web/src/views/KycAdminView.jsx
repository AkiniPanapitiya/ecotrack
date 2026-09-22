import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { getPendingSubmissions, reviewDocument } from '../services/kycApi';
import { Shield, CheckCircle2, XCircle, Clock, AlertCircle, RefreshCw, ArrowLeft, Users } from 'lucide-react';

export const KycAdminView = () => {
  const { user } = useAuth();
  const [submissions, setSubmissions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(null); // docId being processed
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [rejectNote, setRejectNote] = useState({}); // docId -> note text

  useEffect(() => {
    loadSubmissions();
  }, []);

  const loadSubmissions = async () => {
    setLoading(true);
    setMessage('');
    setError('');
    try {
      const res = await getPendingSubmissions();
      setSubmissions(res.data.submissions || []);
    } catch (err) {
      setError('Failed to load pending submissions. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleAction = async (docId, action) => {
    if (action === 'reject' && !rejectNote[docId]) {
      setError('Please provide a reason for rejection.');
      return;
    }

    setActionLoading(docId);
    setMessage('');
    setError('');

    try {
      const status = action === 'verify' ? 'Verified' : 'Rejected';
      const note = action === 'reject' ? rejectNote[docId] : 'Document verified and approved.';
      await reviewDocument(docId, status, note);

      setMessage(
        action === 'verify'
          ? 'Document verified successfully.'
          : 'Document rejected. Recycler has been notified to re-upload.'
      );

      // Remove from list
      setSubmissions((prev) => prev.filter((s) => s.documentId !== docId));
      // Clean up reject note
      const newNotes = { ...rejectNote };
      delete newNotes[docId];
      setRejectNote(newNotes);
    } catch (err) {
      const msg = err.response?.data?.message || 'Action failed. Please try again.';
      setError(msg);
    } finally {
      setActionLoading(null);
    }
  };

  const handleRejectNoteChange = (docId, value) => {
    setRejectNote((prev) => ({ ...prev, [docId]: value }));
    if (error) setError('');
  };

  const formatDate = (dateStr) => {
    try {
      return new Date(dateStr).toLocaleString();
    } catch {
      return dateStr;
    }
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '4rem' }}>
        <div className="spinner" style={{ margin: '0 auto', marginBottom: '1rem' }} />
        <p style={{ color: 'var(--text-secondary)' }}>Loading pending submissions...</p>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '900px', margin: '1rem auto' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '1.5rem' }}>
        <Shield size={24} style={{ color: 'var(--warning)' }} />
        <h1 style={{ fontSize: '1.5rem', fontWeight: 800, margin: 0 }}>KYC Admin Panel</h1>
        <button
          className="btn btn-secondary"
          onClick={loadSubmissions}
          disabled={loading}
          title="Refresh"
          style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: '0.4rem' }}
        >
          <RefreshCw size={16} className={loading ? 'spin' : ''} />
          Refresh
        </button>
      </div>

      {/* Summary */}
      <div className="glass-card" style={{ marginBottom: '1.5rem', padding: '1rem 1.5rem', display: 'flex', alignItems: 'center', gap: '1rem' }}>
        <Users size={20} style={{ color: 'var(--accent)' }} />
        <div>
          <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginBottom: '0.15rem' }}>Total Pending</div>
          <div style={{ fontSize: '1.4rem', fontWeight: 800 }}>{submissions.length}</div>
        </div>
      </div>

      {/* Messages */}
      {message && (
        <div className="alert alert-success" style={{ marginBottom: '1rem' }}>
          <CheckCircle2 size={18} />
          <span>{message}</span>
        </div>
      )}

      {error && (
        <div className="alert alert-danger" style={{ marginBottom: '1rem' }}>
          <AlertCircle size={18} />
          <span>{error}</span>
        </div>
      )}

      {/* Empty state */}
      {!loading && submissions.length === 0 && (
        <div className="glass-card" style={{ textAlign: 'center', padding: '3rem' }}>
          <CheckCircle2 size={48} style={{ color: 'var(--success)', marginBottom: '1rem' }} />
          <h2 style={{ fontSize: '1.2rem', fontWeight: 700, color: 'var(--success)', marginBottom: '0.5rem' }}>
            All Caught Up
          </h2>
          <p style={{ color: 'var(--text-secondary)' }}>
            No pending KYC submissions at the moment.
          </p>
        </div>
      )}

      {/* Pending list */}
      {submissions.length > 0 && (
        <div className="glass-card">
          <h2 style={{ fontSize: '1.1rem', fontWeight: 700, marginBottom: '1rem', color: 'var(--text-primary)' }}>
            Pending Submissions
          </h2>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
            {submissions.map((sub) => (
              <div
                key={sub.documentId}
                className="kyc-admin-row"
                style={{
                  display: 'grid',
                  gridTemplateColumns: 'auto 1fr auto',
                  alignItems: 'center',
                  gap: '1rem',
                  padding: '1rem 1.25rem',
                  background: 'rgba(245, 158, 11, 0.06)',
                  border: '1px solid rgba(245, 158, 11, 0.2)',
                  borderRadius: 'var(--radius-md)',
                }}
              >
                {/* Icon + status */}
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  <Clock size={18} style={{ color: 'var(--warning)' }} />
                  <span className="badge badge-pending" style={{ fontSize: '0.75rem', padding: '0.2rem 0.5rem' }}>
                    Pending
                  </span>
                </div>

                {/* Details */}
                <div style={{ minWidth: 0 }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.25rem', flexWrap: 'wrap' }}>
                    <strong style={{ fontSize: '0.95rem' }}>{sub.recyclerName}</strong>
                    <span className="badge" style={{ fontSize: '0.7rem', padding: '0.15rem 0.4rem', background: 'rgba(255,255,255,0.06)' }}>
                      {sub.documentType === 'ID' ? 'ID Card' : 'Business Proof'}
                    </span>
                  </div>
                  <div style={{ fontSize: '0.82rem', color: 'var(--text-secondary)', marginBottom: '0.2rem' }}>
                    {sub.recyclerEmail}
                  </div>
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                    File: {sub.fileName} · Submitted {formatDate(sub.submittedAt)}
                  </div>

                  {/* Reject note input (shown when reject button clicked) */}
                  {rejectNote[sub.documentId] !== undefined && (
                    <div style={{ marginTop: '0.5rem', paddingTop: '0.5rem', borderTop: '1px solid var(--border-color)' }}>
                      <textarea
                        className="form-input"
                        rows={2}
                        placeholder="Reason for rejection (required)..."
                        value={rejectNote[sub.documentId] || ''}
                        onChange={(e) => handleRejectNoteChange(sub.documentId, e.target.value)}
                        style={{ width: '100%', fontSize: '0.82rem', padding: '0.4rem 0.6rem', borderRadius: 'var(--radius-sm)' }}
                      />
                    </div>
                  )}
                </div>

                {/* Actions */}
                <div style={{ display: 'flex', gap: '0.5rem', flexShrink: 0 }}>
                  <button
                    className="btn btn-success"
                    onClick={() => handleAction(sub.documentId, 'verify')}
                    disabled={actionLoading === sub.documentId}
                    title="Verify this submission"
                  >
                    {actionLoading === sub.documentId ? (
                      <div className="spinner-small" />
                    ) : (
                      <><CheckCircle2 size={16} /> Verify</>
                    )}
                  </button>
                  <button
                    className="btn btn-danger"
                    onClick={() => {
                      setRejectNote((prev) => ({ ...prev, [sub.documentId]: prev[sub.documentId] || '' }));
                      if (error) setError('');
                    }}
                    disabled={actionLoading === sub.documentId}
                    title="Reject — a reason is required"
                    style={{ backgroundColor: 'var(--danger)', color: '#fff', borderColor: 'var(--danger)' }}
                  >
                    {actionLoading === sub.documentId ? (
                      <div className="spinner-small" />
                    ) : (
                      <><XCircle size={16} /> Reject</>
                    )}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Instructions */}
      <div className="glass-card" style={{ marginTop: '1.5rem', padding: '1rem 1.25rem', fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
        <strong style={{ color: 'var(--text-primary)', display: 'block', marginBottom: '0.3rem' }}>How it works:</strong>
        <ul style={{ paddingLeft: '1.2rem', margin: 0 }}>
          <li>Click <strong>Verify</strong> to approve a submission as-is.</li>
          <li>Click <strong>Reject</strong> to open the reason box, type why it was rejected, then confirm. A rejection note is required.</li>
          <li>When rejected, the recycler sees the note and can re-upload a new document.</li>
        </ul>
      </div>
    </div>
  );
};
