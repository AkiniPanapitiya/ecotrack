import React, { useState, useEffect, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { getMyStatus, uploadDocument, getDocumentBlob } from '../services/kycApi';
import {
  FileUp, CheckCircle2, XCircle, Clock, AlertCircle, Upload, Shield, RefreshCw,
  Eye, FileText, Image as ImageIcon, X, Download, ExternalLink, ZoomIn, ZoomOut, RotateCcw
} from 'lucide-react';

export const KycView = () => {
  const { user } = useAuth();
  const [status, setStatus] = useState(null);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  // Form states
  const [docType, setDocType] = useState('ID'); // 'ID' | 'BusinessProof'
  const [formatType, setFormatType] = useState('photo'); // 'photo' | 'pdf'

  // Photo uploads
  const [frontPhoto, setFrontPhoto] = useState(null);
  const [frontPreview, setFrontPreview] = useState(null);
  const [backPhoto, setBackPhoto] = useState(null);
  const [backPreview, setBackPreview] = useState(null);
  const [singlePhoto, setSinglePhoto] = useState(null);
  const [singlePreview, setSinglePreview] = useState(null);

  // PDF upload
  const [pdfFile, setPdfFile] = useState(null);

  // Document preview modal
  const [viewingDoc, setViewingDoc] = useState(null); // { frontUrl, backUrl, loading, error, fileName, backFileName, docType, isPdf, hasBackFile }
  const [zoomLevel, setZoomLevel] = useState(1);
  const docScrollRef = useRef(null);

  const frontInputRef = useRef(null);
  const backInputRef = useRef(null);
  const singlePhotoInputRef = useRef(null);
  const pdfInputRef = useRef(null);

  useEffect(() => {
    loadStatus();
  }, []);

  // Cleanup object URLs on unmount
  useEffect(() => {
    return () => {
      if (frontPreview) URL.revokeObjectURL(frontPreview);
      if (backPreview) URL.revokeObjectURL(backPreview);
      if (singlePreview) URL.revokeObjectURL(singlePreview);
      if (viewingDoc?.frontUrl) URL.revokeObjectURL(viewingDoc.frontUrl);
      if (viewingDoc?.backUrl) URL.revokeObjectURL(viewingDoc.backUrl);
    };
  }, [frontPreview, backPreview, singlePreview, viewingDoc]);

  const loadStatus = async () => {
    setLoading(true);
    setError('');
    try {
      const res = await getMyStatus();
      setStatus(res.data.response);
      if (res.data.response?.hasSubmittedDocument && res.data.response?.status === 'Pending') {
        setMessage('Your KYC document has been submitted and is pending review.');
      } else {
        setMessage('');
      }
    } catch (err) {
      if (err.response?.status === 401) {
        setError('Your session has expired. Please log out from the top-right menu and log back in.');
      } else if (err.response?.status === 403) {
        setError('Access denied. Only recyclers can view or submit KYC documents.');
      } else {
        const msg = err.response?.data?.message || err.message || 'Failed to load KYC status. Please try again.';
        setError(msg);
      }
    } finally {
      setLoading(false);
    }
  };

  const resetUploadInputs = () => {
    if (frontPreview) URL.revokeObjectURL(frontPreview);
    if (backPreview) URL.revokeObjectURL(backPreview);
    if (singlePreview) URL.revokeObjectURL(singlePreview);
    setFrontPhoto(null);
    setFrontPreview(null);
    setBackPhoto(null);
    setBackPreview(null);
    setSinglePhoto(null);
    setSinglePreview(null);
    setPdfFile(null);
    if (frontInputRef.current) frontInputRef.current.value = '';
    if (backInputRef.current) backInputRef.current.value = '';
    if (singlePhotoInputRef.current) singlePhotoInputRef.current.value = '';
    if (pdfInputRef.current) pdfInputRef.current.value = '';
  };

  const handleDocTypeChange = (newType) => {
    setDocType(newType);
    resetUploadInputs();
    setMessage('');
    setError('');
  };

  const handleFormatChange = (newFormat) => {
    setFormatType(newFormat);
    resetUploadInputs();
    setMessage('');
    setError('');
  };

  // Image handling
  const handleFrontPhotoSelect = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (frontPreview) URL.revokeObjectURL(frontPreview);
    setFrontPhoto(file);
    setFrontPreview(URL.createObjectURL(file));
    setError('');
  };

  const handleBackPhotoSelect = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (backPreview) URL.revokeObjectURL(backPreview);
    setBackPhoto(file);
    setBackPreview(URL.createObjectURL(file));
    setError('');
  };

  const handleSinglePhotoSelect = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (singlePreview) URL.revokeObjectURL(singlePreview);
    setSinglePhoto(file);
    setSinglePreview(URL.createObjectURL(file));
    setError('');
  };

  const handlePdfSelect = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setPdfFile(file);
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setMessage('');

    let mainFile = null;
    let backFileToUpload = null;

    if (docType === 'ID') {
      if (formatType === 'photo') {
        if (!frontPhoto || !backPhoto) {
          setError('Please upload both the Front Side and Back Side photos of your National Identity Card.');
          return;
        }
        mainFile = frontPhoto;
        backFileToUpload = backPhoto;
      } else {
        if (!pdfFile) {
          setError('Please select a PDF file containing both the front and back of your NIC.');
          return;
        }
        mainFile = pdfFile;
      }
    } else {
      // BusinessProof
      if (formatType === 'photo') {
        if (!singlePhoto) {
          setError('Please upload a clear photo of your Business Registration (BR) certificate.');
          return;
        }
        mainFile = singlePhoto;
      } else {
        if (!pdfFile) {
          setError('Please select a PDF file of your Business Registration (BR) certificate.');
          return;
        }
        mainFile = pdfFile;
      }
    }

    setUploading(true);
    try {
      const res = await uploadDocument(docType, mainFile, backFileToUpload);
      setStatus(res.data.response);
      setMessage('KYC verification document submitted successfully. Your submission is now under review by an administrator.');
      resetUploadInputs();
    } catch (err) {
      if (err.response?.status === 401) {
        setError('Your session has expired. Please log out and log in again.');
      } else if (err.response?.status === 403) {
        setError('Access denied. Only recyclers can upload KYC documents.');
      } else {
        const msg = err.response?.data?.message || err.response?.data?.title || 'Upload failed. Please try again.';
        setError(msg);
      }
    } finally {
      setUploading(false);
    }
  };

  // Preview Document Modal
  const handleOpenPreview = async (documentId, fileName, docTypeHint, hasBack) => {
    if (!documentId) return;
    setZoomLevel(1);
    const isPdf = (fileName || '').toLowerCase().endsWith('.pdf');
    const backExists = hasBack !== undefined ? hasBack : (status?.hasBackFile || !!status?.backFileName);

    setViewingDoc({
      documentId,
      fileName: fileName || 'kyc-document',
      backFileName: status?.backFileName || null,
      docType: docTypeHint || status?.documentType || 'KYC Document',
      hasBackFile: backExists,
      loading: true,
      frontUrl: null,
      backUrl: null,
      error: null,
      isPdf
    });

    try {
      const resFront = await getDocumentBlob(documentId, 'front');
      const docIsPdf = isPdf || resFront.data.type === 'application/pdf';
      const frontBlob = new Blob([resFront.data], { type: docIsPdf ? 'application/pdf' : resFront.data.type || 'image/jpeg' });
      const frontUrl = URL.createObjectURL(frontBlob);

      let backUrl = null;
      if (backExists) {
        try {
          const resBack = await getDocumentBlob(documentId, 'back');
          const backBlob = new Blob([resBack.data], { type: resBack.data.type || 'image/jpeg' });
          backUrl = URL.createObjectURL(backBlob);
        } catch {
          // ignore if back side not available
        }
      }

      setViewingDoc((prev) => ({
        ...prev,
        loading: false,
        frontUrl,
        backUrl,
        isPdf: docIsPdf
      }));
    } catch (err) {
      setViewingDoc((prev) => ({
        ...prev,
        loading: false,
        error: err.response?.data?.message || 'Could not load document preview.'
      }));
    }
  };

  const handleClosePreview = () => {
    if (viewingDoc?.frontUrl) URL.revokeObjectURL(viewingDoc.frontUrl);
    if (viewingDoc?.backUrl) URL.revokeObjectURL(viewingDoc.backUrl);
    setViewingDoc(null);
    setZoomLevel(1);
  };

  const handleDownloadFile = (url, name) => {
    if (!url) return;
    const a = document.createElement('a');
    a.href = url;
    a.download = name || 'kyc-document';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  };

  const formatFileSize = (bytes) => {
    if (!bytes) return '';
    const kb = bytes / 1024;
    return kb > 1024 ? `${(kb / 1024).toFixed(1)} MB` : `${Math.round(kb)} KB`;
  };

  const getStatusBadge = (s) => {
    if (!s || !s.hasSubmittedDocument || !s.status || s.status === 'Not Submitted') {
      return (
        <span
          className="badge"
          style={{
            background: 'rgba(107, 114, 128, 0.2)',
            color: '#9ca3af',
            border: '1px solid rgba(107, 114, 128, 0.3)',
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.4rem',
            padding: '0.3rem 0.8rem',
            borderRadius: '9999px',
            fontSize: '0.8rem',
            fontWeight: 600
          }}
        >
          <Clock size={12} /> Not Submitted
        </span>
      );
    }
    switch (s.status) {
      case 'Verified':
        return <span className="badge badge-approved"><CheckCircle2 size={12} /> Verified</span>;
      case 'Rejected':
        return <span className="badge badge-rejected"><XCircle size={12} /> Rejected</span>;
      case 'Pending':
        return <span className="badge badge-pending"><Clock size={12} /> Pending Review</span>;
      default:
        return (
          <span
            className="badge"
            style={{
              background: 'rgba(107, 114, 128, 0.2)',
              color: '#9ca3af',
              border: '1px solid rgba(107, 114, 128, 0.3)'
            }}
          >
            <Clock size={12} /> Not Submitted
          </span>
        );
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

  const isSubmittedAndPending = status?.hasSubmittedDocument && status?.status === 'Pending';
  const isVerified = status?.hasSubmittedDocument && status?.status === 'Verified';
  const isRejected = status?.hasSubmittedDocument && status?.status === 'Rejected';
  const isNotSubmitted = !status || !status.hasSubmittedDocument || status?.status === 'Not Submitted';

  return (
    <div style={{ maxWidth: '820px', margin: '1rem auto', padding: '0 1rem' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '1.5rem' }}>
        <Shield size={26} style={{ color: 'var(--accent)' }} />
        <div>
          <h1 style={{ fontSize: '1.5rem', fontWeight: 800, margin: 0 }}>KYC Verification</h1>
          <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', margin: 0 }}>
            Identity & Business Certification for EcoTrack Recyclers
          </p>
        </div>
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

      {/* Status Banner */}
      <div
        className="glass-card"
        style={{
          marginBottom: '1.5rem',
          padding: '1rem 1.25rem',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: '0.75rem'
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <span style={{ color: 'var(--text-secondary)', fontSize: '0.9rem' }}>Verification Status:</span>
          {getStatusBadge(status)}
        </div>
        {status?.submittedAt && (
          <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
            Submitted on {new Date(status.submittedAt).toLocaleDateString()}
          </span>
        )}
      </div>

      {/* Alerts */}
      {message && (
        <div className="alert alert-success" style={{ marginBottom: '1.25rem' }}>
          <CheckCircle2 size={18} />
          <span>{message}</span>
        </div>
      )}

      {error && (
        <div className="alert alert-danger" style={{ marginBottom: '1.25rem' }}>
          <AlertCircle size={18} />
          <span>{error}</span>
        </div>
      )}

      {/* ===== 1. VERIFIED STATE ===== */}
      {isVerified && (
        <div className="glass-card" style={{ textAlign: 'center', padding: '2.5rem 1.5rem' }}>
          <CheckCircle2 size={52} style={{ color: 'var(--success)', marginBottom: '1rem' }} />
          <h2 style={{ fontSize: '1.3rem', fontWeight: 800, color: 'var(--success)', marginBottom: '0.5rem' }}>
            KYC Verified
          </h2>
          <p style={{ color: 'var(--text-secondary)', maxWidth: '500px', margin: '0 auto 1.5rem' }}>
            Your <strong>{status.documentType === 'ID' ? 'National ID (NIC)' : 'Business Registration'}</strong> document has been successfully reviewed and verified. You have full access to recycler features.
          </p>
          <div style={{ display: 'inline-flex', gap: '0.75rem' }}>
            {status.documentId && (
              <button
                type="button"
                className="btn btn-secondary"
                onClick={() => handleOpenPreview(status.documentId, status.fileName, status.documentType, status.hasBackFile)}
                style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}
              >
                <Eye size={16} /> View Verified Document
              </button>
            )}
          </div>
        </div>
      )}

      {/* ===== 2. PENDING REVIEW STATE ===== */}
      {isSubmittedAndPending && (
        <div className="glass-card" style={{ padding: '2rem 1.5rem' }}>
          <div style={{ display: 'flex', alignItems: 'flex-start', gap: '1rem', marginBottom: '1.5rem' }}>
            <div
              style={{
                width: '48px',
                height: '48px',
                borderRadius: '50%',
                background: 'rgba(245, 158, 11, 0.15)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                flexShrink: 0
              }}
            >
              <Clock size={28} style={{ color: 'var(--warning)' }} />
            </div>
            <div>
              <h2 style={{ fontSize: '1.2rem', fontWeight: 700, margin: '0 0 0.4rem 0', color: 'var(--text-primary)' }}>
                KYC Submission Under Review
              </h2>
              <p style={{ color: 'var(--text-secondary)', margin: 0, fontSize: '0.9rem', lineHeight: 1.5 }}>
                Your verification documents have been received and are currently queued for administrator review. You will receive an update once an administrator reviews your submission.
              </p>
            </div>
          </div>

          {/* Submission Details Card */}
          <div
            style={{
              background: 'rgba(255, 255, 255, 0.03)',
              border: '1px solid var(--border-color)',
              borderRadius: 'var(--radius-md)',
              padding: '1rem 1.25rem',
              marginBottom: '1.5rem'
            }}
          >
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem' }}>
              <div>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block', textTransform: 'uppercase' }}>
                  Document Type
                </span>
                <span style={{ fontWeight: 600, fontSize: '0.95rem' }}>
                  {status.documentType === 'ID' ? 'National Identity Card (NIC)' : 'Business Registration (BR)'}
                </span>
              </div>
              <div>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block', textTransform: 'uppercase' }}>
                  Uploaded Files
                </span>
                <span style={{ fontSize: '0.9rem', wordBreak: 'break-all' }}>
                  {status.fileName || 'Document'}
                  {status.backFileName ? ` & ${status.backFileName}` : ''}
                </span>
              </div>
              <div>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block', textTransform: 'uppercase' }}>
                  Date Submitted
                </span>
                <span style={{ fontSize: '0.9rem' }}>
                  {status.submittedAt ? new Date(status.submittedAt).toLocaleString() : 'Recently'}
                </span>
              </div>
            </div>
          </div>

          {/* Action button */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            {status.documentId && (
              <button
                type="button"
                className="btn btn-secondary"
                onClick={() => handleOpenPreview(status.documentId, status.fileName, status.documentType, status.hasBackFile)}
                style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}
              >
                <Eye size={16} /> View Submitted Document
              </button>
            )}
            <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
              (Re-uploading is disabled while review is in progress)
            </span>
          </div>
        </div>
      )}

      {/* ===== 3. REJECTED STATE ===== */}
      {isRejected && (
        <div
          className="glass-card"
          style={{
            padding: '1.5rem',
            marginBottom: '1.5rem',
            border: '1px solid rgba(239, 68, 68, 0.3)',
            background: 'rgba(239, 68, 68, 0.05)'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'flex-start', gap: '1rem', marginBottom: '1rem' }}>
            <XCircle size={32} style={{ color: 'var(--danger)', flexShrink: 0, marginTop: '2px' }} />
            <div>
              <h2 style={{ fontSize: '1.15rem', fontWeight: 700, margin: '0 0 0.3rem 0', color: 'var(--danger)' }}>
                KYC Verification Was Not Accepted
              </h2>
              <p style={{ color: 'var(--text-secondary)', fontSize: '0.88rem', margin: 0 }}>
                The administrator could not verify your previous submission. Please review the reason below and submit an updated document.
              </p>
            </div>
          </div>

          {status.reviewNote && (
            <div
              style={{
                background: 'rgba(245, 158, 11, 0.1)',
                border: '1px solid rgba(245, 158, 11, 0.3)',
                borderRadius: 'var(--radius-sm)',
                padding: '0.85rem 1rem',
                marginBottom: '0.5rem'
              }}
            >
              <div style={{ fontSize: '0.8rem', fontWeight: 700, color: 'var(--warning)', marginBottom: '0.2rem' }}>
                ADMIN FEEDBACK / REASON:
              </div>
              <div style={{ fontSize: '0.9rem', color: 'var(--text-primary)' }}>{status.reviewNote}</div>
            </div>
          )}
        </div>
      )}

      {/* ===== 4. UPLOAD FORM ===== */}
      {(isNotSubmitted || isRejected) && (
        <div className="glass-card" style={{ padding: '1.75rem' }}>
          <h2 style={{ fontSize: '1.2rem', fontWeight: 700, marginBottom: '0.5rem', color: 'var(--text-primary)' }}>
            {isRejected ? 'Re-Submit KYC Document' : 'Submit Your KYC Verification Documents'}
          </h2>
          <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '1.5rem' }}>
            To operate as a certified recycler on EcoTrack, you must verify your identity or business registration.
          </p>

          <form onSubmit={handleSubmit}>
            {/* Step 1: Document Type */}
            <div className="form-group" style={{ marginBottom: '1.25rem' }}>
              <label className="form-label" style={{ fontWeight: 600, marginBottom: '0.5rem' }}>
                1. Select Document Type *
              </label>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem' }}>
                <button
                  type="button"
                  onClick={() => handleDocTypeChange('ID')}
                  className={`btn ${docType === 'ID' ? 'btn-primary' : 'btn-secondary'}`}
                  style={{
                    padding: '0.85rem',
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    gap: '0.35rem',
                    textAlign: 'center',
                    border: docType === 'ID' ? '2px solid var(--primary)' : '1px solid var(--border-color)'
                  }}
                >
                  <Shield size={20} />
                  <span style={{ fontWeight: 700, fontSize: '0.9rem' }}>National ID (NIC)</span>
                  <span style={{ fontSize: '0.75rem', opacity: 0.8 }}>Individual / Personal Verification</span>
                </button>

                <button
                  type="button"
                  onClick={() => handleDocTypeChange('BusinessProof')}
                  className={`btn ${docType === 'BusinessProof' ? 'btn-primary' : 'btn-secondary'}`}
                  style={{
                    padding: '0.85rem',
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    gap: '0.35rem',
                    textAlign: 'center',
                    border: docType === 'BusinessProof' ? '2px solid var(--primary)' : '1px solid var(--border-color)'
                  }}
                >
                  <FileText size={20} />
                  <span style={{ fontWeight: 700, fontSize: '0.9rem' }}>Business Proof (BR)</span>
                  <span style={{ fontSize: '0.75rem', opacity: 0.8 }}>Registered Business Certificate</span>
                </button>
              </div>
            </div>

            {/* Step 2: Upload Format Selector */}
            <div className="form-group" style={{ marginBottom: '1.25rem' }}>
              <label className="form-label" style={{ fontWeight: 600, marginBottom: '0.5rem' }}>
                2. Choose Upload Format *
              </label>
              <div style={{ display: 'inline-flex', background: 'rgba(255, 255, 255, 0.05)', borderRadius: '8px', padding: '4px', gap: '4px' }}>
                <button
                  type="button"
                  onClick={() => handleFormatChange('photo')}
                  className={`btn ${formatType === 'photo' ? 'btn-primary' : 'btn-secondary'}`}
                  style={{
                    padding: '0.5rem 1.25rem',
                    fontSize: '0.85rem',
                    display: 'flex',
                    alignItems: 'center',
                    gap: '0.4rem',
                    borderRadius: '6px'
                  }}
                >
                  <ImageIcon size={16} /> Photos / Images (JPG, PNG)
                </button>
                <button
                  type="button"
                  onClick={() => handleFormatChange('pdf')}
                  className={`btn ${formatType === 'pdf' ? 'btn-primary' : 'btn-secondary'}`}
                  style={{
                    padding: '0.5rem 1.25rem',
                    fontSize: '0.85rem',
                    display: 'flex',
                    alignItems: 'center',
                    gap: '0.4rem',
                    borderRadius: '6px'
                  }}
                >
                  <FileText size={16} /> PDF Document (PDF)
                </button>
              </div>
            </div>

            {/* ===== CASE A: NIC + PHOTOS ===== */}
            {docType === 'ID' && formatType === 'photo' && (
              <div style={{ marginBottom: '1.5rem' }}>
                <div
                  style={{
                    background: 'rgba(6, 182, 212, 0.08)',
                    border: '1px solid rgba(6, 182, 212, 0.25)',
                    borderRadius: 'var(--radius-sm)',
                    padding: '0.85rem 1rem',
                    marginBottom: '1rem',
                    fontSize: '0.85rem',
                    color: 'var(--text-secondary)'
                  }}
                >
                  <strong style={{ color: 'var(--accent)', display: 'block', marginBottom: '0.25rem' }}>
                    💡 National ID (NIC) Photo Upload Instructions:
                  </strong>
                  To verify your identity, <strong>both the Front Side and Back Side photos are required</strong>. Upload clear photos of both sides below so administrators can inspect both parts of your card.
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '1rem' }}>
                  {/* Front Side */}
                  <div
                    style={{
                      border: '1px dashed var(--border-color)',
                      borderRadius: 'var(--radius-md)',
                      padding: '1rem',
                      background: 'rgba(255, 255, 255, 0.02)',
                      textAlign: 'center'
                    }}
                  >
                    <div style={{ fontWeight: 600, fontSize: '0.85rem', marginBottom: '0.5rem', color: '#38bdf8' }}>
                      📷 Front Side Photo *
                    </div>
                    {frontPreview ? (
                      <div>
                        <img
                          src={frontPreview}
                          alt="NIC Front Preview"
                          style={{
                            width: '100%',
                            height: '150px',
                            objectFit: 'contain',
                            background: '#0b1120',
                            borderRadius: 'var(--radius-sm)',
                            marginBottom: '0.5rem'
                          }}
                        />
                        <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginBottom: '0.5rem', wordBreak: 'break-all' }}>
                          {frontPhoto?.name} ({formatFileSize(frontPhoto?.size)})
                        </div>
                        <button
                          type="button"
                          className="btn btn-secondary"
                          style={{ fontSize: '0.75rem', padding: '0.3rem 0.6rem' }}
                          onClick={() => {
                            URL.revokeObjectURL(frontPreview);
                            setFrontPhoto(null);
                            setFrontPreview(null);
                            if (frontInputRef.current) frontInputRef.current.value = '';
                          }}
                        >
                          Change Photo
                        </button>
                      </div>
                    ) : (
                      <div
                        onClick={() => frontInputRef.current?.click()}
                        style={{
                          cursor: 'pointer',
                          padding: '1.5rem 0.5rem',
                          display: 'flex',
                          flexDirection: 'column',
                          alignItems: 'center',
                          gap: '0.5rem'
                        }}
                      >
                        <Upload size={24} style={{ color: 'var(--text-muted)' }} />
                        <span style={{ fontSize: '0.82rem', color: 'var(--text-secondary)' }}>Click to upload Front Side</span>
                        <span style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>JPG, PNG up to 5 MB</span>
                      </div>
                    )}
                    <input
                      ref={frontInputRef}
                      type="file"
                      accept=".jpg,.jpeg,.png"
                      onChange={handleFrontPhotoSelect}
                      style={{ display: 'none' }}
                    />
                  </div>

                  {/* Back Side */}
                  <div
                    style={{
                      border: '1px dashed var(--border-color)',
                      borderRadius: 'var(--radius-md)',
                      padding: '1rem',
                      background: 'rgba(255, 255, 255, 0.02)',
                      textAlign: 'center'
                    }}
                  >
                    <div style={{ fontWeight: 600, fontSize: '0.85rem', marginBottom: '0.5rem', color: '#38bdf8' }}>
                      📷 Back Side Photo *
                    </div>
                    {backPreview ? (
                      <div>
                        <img
                          src={backPreview}
                          alt="NIC Back Preview"
                          style={{
                            width: '100%',
                            height: '150px',
                            objectFit: 'contain',
                            background: '#0b1120',
                            borderRadius: 'var(--radius-sm)',
                            marginBottom: '0.5rem'
                          }}
                        />
                        <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginBottom: '0.5rem', wordBreak: 'break-all' }}>
                          {backPhoto?.name} ({formatFileSize(backPhoto?.size)})
                        </div>
                        <button
                          type="button"
                          className="btn btn-secondary"
                          style={{ fontSize: '0.75rem', padding: '0.3rem 0.6rem' }}
                          onClick={() => {
                            URL.revokeObjectURL(backPreview);
                            setBackPhoto(null);
                            setBackPreview(null);
                            if (backInputRef.current) backInputRef.current.value = '';
                          }}
                        >
                          Change Photo
                        </button>
                      </div>
                    ) : (
                      <div
                        onClick={() => backInputRef.current?.click()}
                        style={{
                          cursor: 'pointer',
                          padding: '1.5rem 0.5rem',
                          display: 'flex',
                          flexDirection: 'column',
                          alignItems: 'center',
                          gap: '0.5rem'
                        }}
                      >
                        <Upload size={24} style={{ color: 'var(--text-muted)' }} />
                        <span style={{ fontSize: '0.82rem', color: 'var(--text-secondary)' }}>Click to upload Back Side</span>
                        <span style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>JPG, PNG up to 5 MB</span>
                      </div>
                    )}
                    <input
                      ref={backInputRef}
                      type="file"
                      accept=".jpg,.jpeg,.png"
                      onChange={handleBackPhotoSelect}
                      style={{ display: 'none' }}
                    />
                  </div>
                </div>
              </div>
            )}

            {/* ===== CASE B: NIC + PDF ===== */}
            {docType === 'ID' && formatType === 'pdf' && (
              <div style={{ marginBottom: '1.5rem' }}>
                <div
                  style={{
                    background: 'rgba(6, 182, 212, 0.08)',
                    border: '1px solid rgba(6, 182, 212, 0.25)',
                    borderRadius: 'var(--radius-sm)',
                    padding: '0.85rem 1rem',
                    marginBottom: '1rem',
                    fontSize: '0.85rem',
                    color: 'var(--text-secondary)'
                  }}
                >
                  <strong style={{ color: 'var(--accent)', display: 'block', marginBottom: '0.25rem' }}>
                    📄 National ID (NIC) PDF Guidelines:
                  </strong>
                  The uploaded PDF file <strong>must contain both the Front Side and Back Side</strong> of your National Identity Card.
                  <ul style={{ margin: '0.35rem 0 0 1.2rem', padding: 0 }}>
                    <li>Ensure the NIC number, full name, photo, and official signatures are sharp and clearly legible.</li>
                    <li>PDF files containing only one side of the NIC cannot be verified and will be rejected.</li>
                  </ul>
                </div>

                <div
                  style={{
                    border: '1px dashed var(--border-color)',
                    borderRadius: 'var(--radius-md)',
                    padding: '1.5rem',
                    background: 'rgba(255, 255, 255, 0.02)',
                    textAlign: 'center'
                  }}
                >
                  {pdfFile ? (
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '1rem' }}>
                      <FileText size={32} style={{ color: 'var(--accent)' }} />
                      <div style={{ textAlign: 'left' }}>
                        <div style={{ fontWeight: 600, fontSize: '0.9rem' }}>{pdfFile.name}</div>
                        <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>{formatFileSize(pdfFile.size)}</div>
                      </div>
                      <button
                        type="button"
                        className="btn btn-secondary"
                        onClick={() => {
                          setPdfFile(null);
                          if (pdfInputRef.current) pdfInputRef.current.value = '';
                        }}
                        style={{ padding: '0.3rem 0.6rem', fontSize: '0.75rem' }}
                      >
                        Change PDF
                      </button>
                    </div>
                  ) : (
                    <div
                      onClick={() => pdfInputRef.current?.click()}
                      style={{
                        cursor: 'pointer',
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                        gap: '0.5rem'
                      }}
                    >
                      <Upload size={28} style={{ color: 'var(--accent)' }} />
                      <span style={{ fontWeight: 600, fontSize: '0.9rem' }}>Select PDF File (NIC Front & Back)</span>
                      <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>PDF up to 5 MB</span>
                    </div>
                  )}
                  <input
                    ref={pdfInputRef}
                    type="file"
                    accept=".pdf"
                    onChange={handlePdfSelect}
                    style={{ display: 'none' }}
                  />
                </div>
              </div>
            )}

            {/* ===== CASE C: BUSINESS PROOF + PDF ===== */}
            {docType === 'BusinessProof' && formatType === 'pdf' && (
              <div style={{ marginBottom: '1.5rem' }}>
                <div
                  style={{
                    background: 'rgba(16, 185, 129, 0.08)',
                    border: '1px solid rgba(16, 185, 129, 0.25)',
                    borderRadius: 'var(--radius-sm)',
                    padding: '0.85rem 1rem',
                    marginBottom: '1rem',
                    fontSize: '0.85rem',
                    color: 'var(--text-secondary)'
                  }}
                >
                  <strong style={{ color: 'var(--primary)', display: 'block', marginBottom: '0.25rem' }}>
                    🏢 Business Registration (BR) PDF Guidelines:
                  </strong>
                  Upload your official <strong>Certificate of Business Registration (BR)</strong> or Certificate of Incorporation.
                  <ul style={{ margin: '0.35rem 0 0 1.2rem', padding: 0 }}>
                    <li>Must include all pages of the registration certificate.</li>
                    <li>Ensure business name, registration number, date of incorporation, and official registrar seals/signatures are completely legible.</li>
                  </ul>
                </div>

                <div
                  style={{
                    border: '1px dashed var(--border-color)',
                    borderRadius: 'var(--radius-md)',
                    padding: '1.5rem',
                    background: 'rgba(255, 255, 255, 0.02)',
                    textAlign: 'center'
                  }}
                >
                  {pdfFile ? (
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '1rem' }}>
                      <FileText size={32} style={{ color: 'var(--primary)' }} />
                      <div style={{ textAlign: 'left' }}>
                        <div style={{ fontWeight: 600, fontSize: '0.9rem' }}>{pdfFile.name}</div>
                        <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>{formatFileSize(pdfFile.size)}</div>
                      </div>
                      <button
                        type="button"
                        className="btn btn-secondary"
                        onClick={() => {
                          setPdfFile(null);
                          if (pdfInputRef.current) pdfInputRef.current.value = '';
                        }}
                        style={{ padding: '0.3rem 0.6rem', fontSize: '0.75rem' }}
                      >
                        Change PDF
                      </button>
                    </div>
                  ) : (
                    <div
                      onClick={() => pdfInputRef.current?.click()}
                      style={{
                        cursor: 'pointer',
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                        gap: '0.5rem'
                      }}
                    >
                      <Upload size={28} style={{ color: 'var(--primary)' }} />
                      <span style={{ fontWeight: 600, fontSize: '0.9rem' }}>Select Business Registration PDF</span>
                      <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>PDF up to 5 MB</span>
                    </div>
                  )}
                  <input
                    ref={pdfInputRef}
                    type="file"
                    accept=".pdf"
                    onChange={handlePdfSelect}
                    style={{ display: 'none' }}
                  />
                </div>
              </div>
            )}

            {/* ===== CASE D: BUSINESS PROOF + PHOTO ===== */}
            {docType === 'BusinessProof' && formatType === 'photo' && (
              <div style={{ marginBottom: '1.5rem' }}>
                <div
                  style={{
                    background: 'rgba(16, 185, 129, 0.08)',
                    border: '1px solid rgba(16, 185, 129, 0.25)',
                    borderRadius: 'var(--radius-sm)',
                    padding: '0.85rem 1rem',
                    marginBottom: '1rem',
                    fontSize: '0.85rem',
                    color: 'var(--text-secondary)'
                  }}
                >
                  <strong style={{ color: 'var(--primary)', display: 'block', marginBottom: '0.25rem' }}>
                    📷 Business Registration (BR) Photo Guidelines:
                  </strong>
                  Upload a clear, high-resolution, uncropped photo or scan of your official <strong>Certificate of Business Registration (BR)</strong>.
                  <ul style={{ margin: '0.35rem 0 0 1.2rem', padding: 0 }}>
                    <li>Lay the certificate on a flat, well-lit surface without glare, shadows, or cropped edges.</li>
                    <li>The business name, registration number, and official government stamp must be clearly visible.</li>
                  </ul>
                </div>

                <div
                  style={{
                    border: '1px dashed var(--border-color)',
                    borderRadius: 'var(--radius-md)',
                    padding: '1.5rem',
                    background: 'rgba(255, 255, 255, 0.02)',
                    textAlign: 'center'
                  }}
                >
                  {singlePreview ? (
                    <div>
                      <img
                        src={singlePreview}
                        alt="Business Proof Preview"
                        style={{
                          maxWidth: '100%',
                          maxHeight: '240px',
                          objectFit: 'contain',
                          borderRadius: 'var(--radius-sm)',
                          marginBottom: '0.75rem'
                        }}
                      />
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginBottom: '0.5rem' }}>
                        {singlePhoto?.name} ({formatFileSize(singlePhoto?.size)})
                      </div>
                      <button
                        type="button"
                        className="btn btn-secondary"
                        onClick={() => {
                          URL.revokeObjectURL(singlePreview);
                          setSinglePhoto(null);
                          setSinglePreview(null);
                          if (singlePhotoInputRef.current) singlePhotoInputRef.current.value = '';
                        }}
                        style={{ padding: '0.35rem 0.75rem', fontSize: '0.8rem' }}
                      >
                        Change Photo
                      </button>
                    </div>
                  ) : (
                    <div
                      onClick={() => singlePhotoInputRef.current?.click()}
                      style={{
                        cursor: 'pointer',
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                        gap: '0.5rem'
                      }}
                    >
                      <Upload size={28} style={{ color: 'var(--primary)' }} />
                      <span style={{ fontWeight: 600, fontSize: '0.9rem' }}>Upload Certificate Photo</span>
                      <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>JPG, PNG up to 5 MB</span>
                    </div>
                  )}
                  <input
                    ref={singlePhotoInputRef}
                    type="file"
                    accept=".jpg,.jpeg,.png"
                    onChange={handleSinglePhotoSelect}
                    style={{ display: 'none' }}
                  />
                </div>
              </div>
            )}

            {/* Submit Button */}
            <button
              type="submit"
              className="btn btn-primary"
              disabled={uploading}
              style={{ width: '100%', padding: '0.85rem', fontSize: '0.95rem', fontWeight: 700 }}
            >
              {uploading ? (
                <><div className="spinner-small" style={{ marginRight: '0.5rem' }} /> Processing & Submitting...</>
              ) : (
                <><FileUp size={18} style={{ marginRight: '0.5rem' }} /> {isRejected ? 'Re-Submit Document for Review' : 'Submit Document for Review'}</>
              )}
            </button>
          </form>
        </div>
      )}

      {/* ===== DOCUMENT PREVIEW MODAL ===== */}
      {viewingDoc && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0, 0, 0, 0.85)',
            backdropFilter: 'blur(8px)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
            padding: '1rem'
          }}
          onClick={handleClosePreview}
        >
          <div
            style={{
              background: '#0f172a',
              border: '1px solid var(--border-color)',
              borderRadius: 'var(--radius-lg)',
              width: '100%',
              maxWidth: viewingDoc.hasBackFile ? '1060px' : '900px',
              maxHeight: '92vh',
              display: 'flex',
              flexDirection: 'column',
              boxShadow: '0 25px 50px -12px rgba(0, 0, 0, 0.7)',
              overflow: 'hidden'
            }}
            onClick={(e) => e.stopPropagation()}
          >
            {/* Modal Header */}
            <div
              style={{
                padding: '1rem 1.25rem',
                borderBottom: '1px solid var(--border-color)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                flexWrap: 'wrap',
                gap: '0.5rem'
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                <Shield size={20} style={{ color: 'var(--accent)' }} />
                <div>
                  <h3 style={{ fontSize: '1rem', fontWeight: 700, margin: 0 }}>
                    {viewingDoc.docType === 'ID' ? 'National ID (NIC) Document' : 'Business Registration Document'}
                  </h3>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                    {viewingDoc.fileName}
                    {viewingDoc.backFileName ? ` & ${viewingDoc.backFileName}` : ''}
                  </span>
                </div>
              </div>

              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                {viewingDoc.frontUrl && (
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => handleDownloadFile(viewingDoc.frontUrl, viewingDoc.fileName)}
                    title="Download Front Document"
                    style={{ padding: '6px 10px', fontSize: '0.8rem', display: 'flex', alignItems: 'center', gap: '4px' }}
                  >
                    <Download size={14} /> Download {viewingDoc.hasBackFile ? 'Front' : 'File'}
                  </button>
                )}
                {viewingDoc.backUrl && (
                  <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => handleDownloadFile(viewingDoc.backUrl, viewingDoc.backFileName || 'nic_back.jpg')}
                    title="Download Back Document"
                    style={{ padding: '6px 10px', fontSize: '0.8rem', display: 'flex', alignItems: 'center', gap: '4px' }}
                  >
                    <Download size={14} /> Download Back
                  </button>
                )}
                <button
                  type="button"
                  onClick={handleClosePreview}
                  style={{
                    background: 'none',
                    border: 'none',
                    color: 'var(--text-muted)',
                    cursor: 'pointer',
                    padding: '4px',
                    display: 'flex',
                    alignItems: 'center'
                  }}
                >
                  <X size={20} />
                </button>
              </div>
            </div>

            {/* Modal Body */}
            <div
              ref={docScrollRef}
              style={{
                flex: 1,
                overflowY: 'auto',
                padding: '1.25rem',
                background: '#090d16',
                minHeight: '400px',
                maxHeight: '75vh'
              }}
            >
              {viewingDoc.loading && (
                <div style={{ textAlign: 'center', padding: '4rem 1rem' }}>
                  <div className="spinner" style={{ margin: '0 auto 1rem' }} />
                  <p style={{ color: 'var(--text-secondary)' }}>Loading document preview...</p>
                </div>
              )}

              {viewingDoc.error && (
                <div className="alert alert-danger" style={{ maxWidth: '420px', margin: '2rem auto' }}>
                  <AlertCircle size={18} />
                  <span>{viewingDoc.error}</span>
                </div>
              )}

              {/* PDF Preview */}
              {viewingDoc.frontUrl && viewingDoc.isPdf && (
                <iframe
                  src={viewingDoc.frontUrl}
                  title="KYC PDF Viewer"
                  style={{
                    width: '100%',
                    height: '620px',
                    border: 'none',
                    borderRadius: 'var(--radius-sm)',
                    background: '#fff'
                  }}
                />
              )}

              {/* TWO SEPARATE PHOTOS (Front & Back) */}
              {viewingDoc.hasBackFile && viewingDoc.frontUrl && !viewingDoc.isPdf && (
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(340px, 1fr))', gap: '1.25rem', width: '100%' }}>
                  {/* Front Photo Card */}
                  <div
                    style={{
                      background: 'rgba(255, 255, 255, 0.02)',
                      border: '1px solid var(--border-color)',
                      borderRadius: 'var(--radius-md)',
                      padding: '1rem'
                    }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '0.75rem' }}>
                      <span style={{ fontWeight: 700, color: '#38bdf8', fontSize: '0.85rem' }}>
                        📷 FRONT SIDE PHOTO
                      </span>
                      <a
                        href={viewingDoc.frontUrl}
                        target="_blank"
                        rel="noreferrer"
                        className="btn btn-secondary"
                        style={{ padding: '3px 8px', fontSize: '0.75rem', display: 'flex', alignItems: 'center', gap: '3px' }}
                      >
                        <ExternalLink size={12} /> Open Full Size
                      </a>
                    </div>
                    <img
                      src={viewingDoc.frontUrl}
                      alt="NIC Front Side"
                      style={{
                        width: '100%',
                        maxHeight: '440px',
                        objectFit: 'contain',
                        borderRadius: 'var(--radius-sm)',
                        background: '#0b1120'
                      }}
                    />
                  </div>

                  {/* Back Photo Card */}
                  <div
                    style={{
                      background: 'rgba(255, 255, 255, 0.02)',
                      border: '1px solid var(--border-color)',
                      borderRadius: 'var(--radius-md)',
                      padding: '1rem'
                    }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '0.75rem' }}>
                      <span style={{ fontWeight: 700, color: '#38bdf8', fontSize: '0.85rem' }}>
                        📷 BACK SIDE PHOTO
                      </span>
                      {viewingDoc.backUrl ? (
                        <a
                          href={viewingDoc.backUrl}
                          target="_blank"
                          rel="noreferrer"
                          className="btn btn-secondary"
                          style={{ padding: '3px 8px', fontSize: '0.75rem', display: 'flex', alignItems: 'center', gap: '3px' }}
                        >
                          <ExternalLink size={12} /> Open Full Size
                        </a>
                      ) : null}
                    </div>
                    {viewingDoc.backUrl ? (
                      <img
                        src={viewingDoc.backUrl}
                        alt="NIC Back Side"
                        style={{
                          width: '100%',
                          maxHeight: '440px',
                          objectFit: 'contain',
                          borderRadius: 'var(--radius-sm)',
                          background: '#0b1120'
                        }}
                      />
                    ) : (
                      <div style={{ textAlign: 'center', padding: '3rem', color: 'var(--text-muted)' }}>
                        Back side photo not available
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* SINGLE PHOTO / COMPOSITE IMAGE */}
              {!viewingDoc.hasBackFile && viewingDoc.frontUrl && !viewingDoc.isPdf && (
                <div style={{ width: '100%', textAlign: 'center' }}>
                  {viewingDoc.fileName?.includes('Front_and_Back') && (
                    <div
                      style={{
                        background: 'rgba(6, 182, 212, 0.1)',
                        border: '1px solid rgba(6, 182, 212, 0.25)',
                        borderRadius: 'var(--radius-sm)',
                        padding: '0.6rem 1rem',
                        marginBottom: '1rem',
                        fontSize: '0.85rem',
                        color: 'var(--accent)',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between'
                      }}
                    >
                      <span>💡 <strong>Front and Back sides</strong> are included in this document. Scroll down to inspect the full document.</span>
                      <button
                        type="button"
                        className="btn btn-secondary"
                        onClick={() => {
                          if (docScrollRef.current) {
                            docScrollRef.current.scrollTop = docScrollRef.current.scrollHeight;
                          }
                        }}
                        style={{ padding: '3px 8px', fontSize: '0.75rem' }}
                      >
                        ⬇️ Scroll to Back Side
                      </button>
                    </div>
                  )}

                  <div style={{ display: 'inline-block', maxWidth: '100%' }}>
                    <img
                      src={viewingDoc.frontUrl}
                      alt="KYC Document Preview"
                      style={{
                        transform: `scale(${zoomLevel})`,
                        transformOrigin: 'top center',
                        transition: 'transform 0.15s ease',
                        maxWidth: '100%',
                        borderRadius: 'var(--radius-sm)',
                        boxShadow: '0 4px 20px rgba(0,0,0,0.5)'
                      }}
                    />
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
