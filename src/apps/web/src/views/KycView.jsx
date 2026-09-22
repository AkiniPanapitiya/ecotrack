import React, { useState, useEffect, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { getMyStatus, uploadDocument } from '../services/kycApi';
import { FileUp, CheckCircle2, XCircle, Clock, AlertCircle, Upload, Shield, RefreshCw } from 'lucide-react';

export const KycView = () => {
  const { user } = useAuth();
  const [status, setStatus] = useState(null);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [docType, setDocType] = useState('ID');
  const fileInputRef = useRef(null);

  // Load current status on mount
  useEffect(() => {
    loadStatus();
  }, []);

  const loadStatus = async () => {
    setLoading(true);
    setError('');
    try {
      const res = await getMyStatus();
      setStatus(res.data.response);
      setMessage(res.data.message || '');
    } catch (err) {
      setError('Failed to load KYC status. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleDocTypeChange = (e) => {
    setDocType(e.target.value);
    setMessage('');
    setError('');
  };

  const handleFileChange = (e) => {
    // Clear messages when new file selected
    setMessage('');
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const file = fileInputRef.current?.files?.[0];
    if (!file) {
      setError('Please select a file to upload.');
      return;
    }

    setUploading(true);
    setMessage('');
    setError('');

    try {
      const res = await uploadDocument(docType, file);
      setStatus(res.data.response);
      setMessage('✅ KYC submitted successfully! We will review your document and respond soon.');
      // Reset file input
      if (fileInputRef.current) fileInputRef.current.value = '';
    } catch (err) {
      const msg = err.response?.data?.message || 'Upload failed. Please try again.';
      setError(msg);
    } finally {
      setUploading(false);
    }
  };

  const getStatusBadge = (s) => {
    if (!s) return <span className="badge badge-pending"><Clock size={12} /> Not Submitted</span>;
    switch (s.status) {
      case 'Verified':
        return <span className="badge badge-approved"><CheckCircle2 size={12} /> Verified</span>;
      case 'Rejected':
        return <span className="badge badge-rejected"><XCircle size={12} /> Rejected</span>;
      default:
        return <span className="badge badge-pending"><Clock size={12} /> Pending</span>;
    }
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '4rem' }}>
        <div className="spinner" style={{ margin: '0 auto', marginBottom: '1rem' }} />
        <p style={{ color: 'var(--text-secondary)' }}>Loading KYC status...</p>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '700px', margin: '1rem auto' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '1.5rem' }}>
        <Shield size={24} style={{ color: 'var(--accent)' }} />
        <h1 style={{ fontSize: '1.5rem', fontWeight: 800, margin: 0 }}>KYC Verification</h1>
        <button
            className="btn btn-secondary"
            onClick={loadStatus}
            disabled={loading}
            title="Refresh Status"
            style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: '0.4rem' }}
          >
            <RefreshCw size={16} className={loading ? 'spin' : ''} />
            Refresh
          </button>
      </div>

      {/* Status Badge */}
      <div style={{ marginBottom: '1.5rem', display: 'flex', alignItems: 'center', gap: '0.75rem', flexWrap: 'wrap' }}>
        <span style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>Your Status:</span>
        {getStatusBadge(status)}
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

      {/* ===== VERIFIED ===== */}
      {status && status.status === 'Verified' && (
        <div className="glass-card" style={{ textAlign: 'center', padding: '2rem' }}>
          <CheckCircle2 size={48} style={{ color: 'var(--success)', marginBottom: '1rem' }} />
          <h2 style={{ fontSize: '1.2rem', fontWeight: 700, color: 'var(--success)', marginBottom: '0.5rem' }}>
            KYC Verified ✓
          </h2>
          <p style={{ color: 'var(--text-secondary)', marginBottom: '0.25rem' }}>
            Your {status.documentType} has been verified.
          </p>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>
            Submitted on {new Date(status.submittedAt).toLocaleDateString()}
          </p>
        </div>
      )}

      {/* ===== REJECTED ===== */}
      {status && status.status === 'Rejected' && (
        <div className="glass-card" style={{ textAlign: 'center', padding: '2rem' }}>
          <XCircle size={48} style={{ color: 'var(--danger)', marginBottom: '1rem' }} />
          <h2 style={{ fontSize: '1.2rem', fontWeight: 700, color: 'var(--danger)', marginBottom: '0.5rem' }}>
            KYC Rejected ✗
          </h2>
          <p style={{ color: 'var(--text-secondary)', marginBottom: '1rem' }}>
            Your {status.documentType} was not accepted. Please upload a new document.
          </p>

          {/* Review note from admin */}
          {status.reviewNote && (
            <div className="alert alert-warning" style={{ textAlign: 'left', marginBottom: '1.5rem' }}>
              <AlertCircle size={18} />
              <div style={{ fontWeight: 600, marginBottom: '0.25rem' }}>Admin Note:</div>
              <span>{status.reviewNote}</span>
            </div>
          )}

          <p style={{ color: 'var(--text-secondary)', marginBottom: '1.5rem' }}>
            Upload a new document that meets the requirements.
          </p>

          {/* Re-upload form */}
          <form onSubmit={handleSubmit} style={{ textAlign: 'left' }}>
            <div className="form-group" style={{ marginBottom: '1rem' }}>
              <label className="form-label">Document Type</label>
              <select
                className="form-input"
                value={docType}
                onChange={handleDocTypeChange}
              >
                <option value="ID">ID Card</option>
                <option value="BusinessProof">Business Proof</option>
              </select>
            </div>

            <div className="form-group" style={{ marginBottom: '1rem' }}>
              <label className="form-label">Upload New Document</label>
              <input
                ref={fileInputRef}
                type="file"
                className="form-input"
                accept=".jpg,.jpeg,.png,.pdf"
                onChange={handleFileChange}
              />
              <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
                Accepted: JPG, PNG, PDF — Max 5 MB
              </div>
            </div>

            <button
              type="submit"
              className="btn btn-primary"
              disabled={uploading}
              style={{ width: '100%' }}
            >
              {uploading ? (
                <><div className="spinner-small" /> Uploading...</>
              ) : (
                <><Upload size={18} /> Re-submit Document</>
              )}
            </button>
          </form>
        </div>
      )}

      {/* ===== NOT SUBMITTED or PENDING ===== */}
      {(!status || !status.status || status.status === 'Pending') && (
        <div className="glass-card">
          <h2 style={{ fontSize: '1.15rem', fontWeight: 700, marginBottom: '1.25rem', color: 'var(--text-primary)' }}>
            Upload Your KYC Document
          </h2>

          <form onSubmit={handleSubmit}>
            <div className="form-group" style={{ marginBottom: '1rem' }}>
              <label className="form-label">Document Type *</label>
              <select
                className="form-input"
                value={docType}
                onChange={handleDocTypeChange}
              >
                <option value="ID">ID Card (National ID)</option>
                <option value="BusinessProof">Business Proof (Registration Certificate)</option>
              </select>
            </div>

            <div className="form-group" style={{ marginBottom: '1.5rem' }}>
              <label className="form-label">Upload Document *</label>
              <input
                ref={fileInputRef}
                type="file"
                className="form-input"
                accept=".jpg,.jpeg,.png,.pdf"
                onChange={handleFileChange}
              />
              <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>
                Accepted: JPG, PNG, PDF — Max 5 MB
              </div>
            </div>

            <button
              type="submit"
              className="btn btn-primary"
              disabled={uploading}
              style={{ width: '100%' }}
            >
              {uploading ? (
                <><div className="spinner-small" /> Uploading...</>
              ) : (
                <><FileUp size={18} /> Submit Document for Review</>
              )}
            </button>
          </form>
        </div>
      )}
    </div>
  );
};
