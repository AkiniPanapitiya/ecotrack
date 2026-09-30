import React, { useState, useEffect, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { getPendingSubmissions, reviewDocument, getDocumentBlob } from '../services/kycApi';
import {
  Shield, CheckCircle2, XCircle, Clock, AlertCircle, RefreshCw, ArrowLeft, Users,
  Eye, FileText, Image as ImageIcon, Download, ExternalLink, ZoomIn, ZoomOut, RotateCcw, X
} from 'lucide-react';

export const KycAdminView = () => {
  const { user } = useAuth();
  const [submissions, setSubmissions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(null); // docId being processed
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [rejectNote, setRejectNote] = useState({}); // docId -> note text
  const [rejectDialog, setRejectDialog] = useState(null); // { docId, recyclerName, recyclerEmail, docType, reason, error }

  // Document Viewer Modal State
  const [viewingDoc, setViewingDoc] = useState(null); // { sub, frontUrl, backUrl, hasBackFile, loading, error, isPdf }
  const [zoomLevel, setZoomLevel] = useState(1);
  const [modalRejectOpen, setModalRejectOpen] = useState(false);
  const [modalRejectReason, setModalRejectReason] = useState('');
  const modalScrollRef = useRef(null);

  useEffect(() => {
    loadSubmissions();
  }, []);

  // Clean up object URLs
  useEffect(() => {
    return () => {
      if (viewingDoc?.frontUrl) URL.revokeObjectURL(viewingDoc.frontUrl);
      if (viewingDoc?.backUrl) URL.revokeObjectURL(viewingDoc.backUrl);
    };
  }, [viewingDoc]);

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

  const handleOpenDocModal = async (sub) => {
    setZoomLevel(1);
    setModalRejectOpen(false);
    setModalRejectReason('');

    const isPdf = (sub.fileName || '').toLowerCase().endsWith('.pdf') || (sub.fileType || '').includes('pdf');
    const hasBack = sub.hasBackFile || !!sub.backFileName;

    setViewingDoc({
      sub,
      documentId: sub.documentId,
      fileName: sub.fileName,
      backFileName: sub.backFileName,
      docType: sub.documentType,
      hasBackFile: hasBack,
      isPdf,
      loading: true,
      frontUrl: null,
      backUrl: null,
      error: null
    });

    try {
      const resFront = await getDocumentBlob(sub.documentId, 'front');
      const docIsPdf = isPdf || resFront.data.type === 'application/pdf';
      const frontBlob = new Blob([resFront.data], { type: docIsPdf ? 'application/pdf' : resFront.data.type || 'image/jpeg' });
      const frontUrl = URL.createObjectURL(frontBlob);

      let backUrl = null;
      if (hasBack) {
        try {
          const resBack = await getDocumentBlob(sub.documentId, 'back');
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
        error: err.response?.data?.message || 'Could not fetch document file for preview.'
      }));
    }
  };

  const handleCloseDocModal = () => {
    if (viewingDoc?.frontUrl) URL.revokeObjectURL(viewingDoc.frontUrl);
    if (viewingDoc?.backUrl) URL.revokeObjectURL(viewingDoc.backUrl);
    setViewingDoc(null);
    setZoomLevel(1);
    setModalRejectOpen(false);
    setModalRejectReason('');
  };

  const handleOpenRejectDialog = (sub) => {
    setRejectDialog({
      docId: sub.documentId,
      recyclerName: sub.recyclerName,
      recyclerEmail: sub.recyclerEmail,
      docType: sub.documentType,
      reason: rejectNote[sub.documentId] || modalRejectReason || '',
      error: ''
    });
  };

  const handleConfirmRejectDialog = async () => {
    if (!rejectDialog) return;
    const trimmed = (rejectDialog.reason || '').trim();
    if (!trimmed) {
      setRejectDialog((prev) => ({
        ...prev,
        error: 'Please enter a rejection reason explaining why the document was rejected.'
      }));
      return;
    }
    await handleAction(rejectDialog.docId, 'reject', trimmed);
  };

  const handleAction = async (docId, action, customReason = null) => {
    const noteText = customReason !== null ? customReason : rejectNote[docId];

    if (action === 'reject' && (!noteText || !noteText.trim())) {
      setError('Please provide a reason explaining why the document was rejected.');
      return;
    }

    setActionLoading(docId);
    setMessage('');
    setError('');

    try {
      const status = action === 'verify' ? 'Verified' : 'Rejected';
      const note = action === 'reject' ? noteText.trim() : 'Document verified and approved.';
      await reviewDocument(docId, status, note);

      setMessage(
        action === 'verify'
          ? 'Document verified successfully.'
          : 'Document rejected. The recycler has been notified with your review note to re-upload.'
      );

      // Remove from pending submissions list
      setSubmissions((prev) => prev.filter((s) => s.documentId !== docId));

      // Clean up reject note
      const newNotes = { ...rejectNote };
      delete newNotes[docId];
      setRejectNote(newNotes);

      // Close reject dialog if open
      setRejectDialog(null);

      // If viewing in modal, close modal
      if (viewingDoc?.documentId === docId) {
        handleCloseDocModal();
      }
    } catch (err) {
      const msg = err.response?.data?.message || 'Action failed. Please try again.';
      setError(msg);
      if (rejectDialog) {
        setRejectDialog((prev) => ({ ...prev, error: msg }));
      }
    } finally {
      setActionLoading(null);
    }
  };

  const handleRejectNoteChange = (docId, value) => {
    setRejectNote((prev) => ({ ...prev, [docId]: value }));
    if (error) setError('');
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

  const formatDate = (dateStr) => {
    try {
      return new Date(dateStr).toLocaleString();
    } catch {
      return dateStr;
    }
  };

  const formatFileSize = (bytes) => {
    if (!bytes) return '';
    const kb = bytes / 1024;
    return kb > 1024 ? `${(kb / 1024).toFixed(1)} MB` : `${Math.round(kb)} KB`;
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
    <div style={{ maxWidth: '980px', margin: '1rem auto', padding: '0 1rem' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '1.5rem' }}>
        <Shield size={26} style={{ color: 'var(--warning)' }} />
        <div>
          <h1 style={{ fontSize: '1.5rem', fontWeight: 800, margin: 0 }}>KYC Admin Panel</h1>
          <p style={{ fontSize: '0.85rem', color: 'var(--text-muted)', margin: 0 }}>
            Inspect uploaded identification documents and verify or reject recycler accounts
          </p>
        </div>
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
      <div
        className="glass-card"
        style={{
          marginBottom: '1.5rem',
          padding: '1rem 1.5rem',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: '1rem'
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <div
            style={{
              width: '42px',
              height: '42px',
              borderRadius: '50%',
              background: 'rgba(245, 158, 11, 0.15)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center'
            }}
          >
            <Users size={20} style={{ color: 'var(--warning)' }} />
          </div>
          <div>
            <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Pending KYC Verifications</div>
            <div style={{ fontSize: '1.4rem', fontWeight: 800 }}>{submissions.length}</div>
          </div>
        </div>
        <span style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
          Review both Front and Back sides thoroughly before verifying.
        </span>
      </div>

      {/* Messages */}
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

      {/* Empty state */}
      {!loading && submissions.length === 0 && (
        <div className="glass-card" style={{ textAlign: 'center', padding: '3.5rem 1.5rem' }}>
          <CheckCircle2 size={52} style={{ color: 'var(--success)', marginBottom: '1rem' }} />
          <h2 style={{ fontSize: '1.25rem', fontWeight: 700, color: 'var(--success)', marginBottom: '0.5rem' }}>
            All Caught Up!
          </h2>
          <p style={{ color: 'var(--text-secondary)', maxWidth: '420px', margin: '0 auto' }}>
            There are no pending KYC submissions waiting for review at the moment.
          </p>
        </div>
      )}

      {/* Pending list */}
      {submissions.length > 0 && (
        <div className="glass-card" style={{ padding: '1.5rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1.25rem' }}>
            <h2 style={{ fontSize: '1.1rem', fontWeight: 700, margin: 0, color: 'var(--text-primary)' }}>
              Submissions Awaiting Verification ({submissions.length})
            </h2>
            <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
              Click <strong>View Document</strong> to inspect the uploaded file(s)
            </span>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.85rem' }}>
            {submissions.map((sub) => {
              const isPdf = (sub.fileName || '').toLowerCase().endsWith('.pdf') || (sub.fileType || '').includes('pdf');
              const hasTwoPhotos = sub.hasBackFile || !!sub.backFileName;
              return (
                <div
                  key={sub.documentId}
                  className="kyc-admin-row"
                  style={{
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '0.75rem',
                    padding: '1rem 1.25rem',
                    background: 'rgba(245, 158, 11, 0.05)',
                    border: '1px solid rgba(245, 158, 11, 0.2)',
                    borderRadius: 'var(--radius-md)'
                  }}
                >
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '0.75rem' }}>
                    {/* Recycler Info */}
                    <div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.2rem', flexWrap: 'wrap' }}>
                        <strong style={{ fontSize: '1rem', color: 'var(--text-primary)' }}>{sub.recyclerName}</strong>
                        <span
                          className="badge"
                          style={{
                            fontSize: '0.7rem',
                            padding: '0.15rem 0.5rem',
                            background: sub.documentType === 'ID' ? 'rgba(6, 182, 212, 0.15)' : 'rgba(16, 185, 129, 0.15)',
                            color: sub.documentType === 'ID' ? '#22d3ee' : '#34d399',
                            border: `1px solid ${sub.documentType === 'ID' ? 'rgba(6, 182, 212, 0.3)' : 'rgba(16, 185, 129, 0.3)'}`
                          }}
                        >
                          {sub.documentType === 'ID' ? 'National ID (NIC)' : 'Business Proof (BR)'}
                        </span>
                        <span
                          className="badge"
                          style={{
                            fontSize: '0.7rem',
                            padding: '0.15rem 0.45rem',
                            background: 'rgba(255,255,255,0.06)',
                            color: 'var(--text-muted)'
                          }}
                        >
                          {isPdf ? '📄 PDF' : hasTwoPhotos ? '📷 2 Photos (Front & Back)' : '📷 Image'}
                        </span>
                      </div>
                      <div style={{ fontSize: '0.82rem', color: 'var(--text-secondary)' }}>
                        {sub.recyclerEmail}
                      </div>
                      <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.2rem' }}>
                        Files: <strong>{sub.fileName}</strong>
                        {sub.backFileName ? ` & ${sub.backFileName}` : ''}
                        {sub.fileSize ? ` (${formatFileSize(sub.fileSize)})` : ''} · Submitted {formatDate(sub.submittedAt)}
                      </div>
                    </div>

                    {/* Action Buttons */}
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', flexWrap: 'wrap' }}>
                      {/* VIEW DOCUMENT BUTTON */}
                      <button
                        type="button"
                        className="btn btn-secondary"
                        onClick={() => handleOpenDocModal(sub)}
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: '0.4rem',
                          padding: '0.45rem 0.85rem',
                          background: 'rgba(6, 182, 212, 0.12)',
                          color: '#22d3ee',
                          borderColor: 'rgba(6, 182, 212, 0.3)'
                        }}
                        title="Inspect uploaded document"
                      >
                        <Eye size={15} /> View Document
                      </button>

                      {/* VERIFY BUTTON */}
                      <button
                        className="btn btn-success"
                        onClick={() => handleAction(sub.documentId, 'verify')}
                        disabled={actionLoading === sub.documentId}
                        style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', padding: '0.45rem 0.85rem' }}
                        title="Verify this submission"
                      >
                        {actionLoading === sub.documentId ? (
                          <div className="spinner-small" />
                        ) : (
                          <><CheckCircle2 size={15} /> Verify</>
                        )}
                      </button>

                      {/* REJECT BUTTON */}
                      <button
                        type="button"
                        className="btn btn-danger"
                        onClick={() => handleOpenRejectDialog(sub)}
                        disabled={actionLoading === sub.documentId}
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: '0.4rem',
                          padding: '0.45rem 0.85rem',
                          backgroundColor: 'var(--danger)',
                          color: '#fff',
                          borderColor: 'var(--danger)',
                          fontWeight: 600,
                          cursor: 'pointer'
                        }}
                        title="Reject this submission with guidance for recycler"
                      >
                        <XCircle size={15} /> Reject
                      </button>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* ===== ADMIN DOCUMENT PREVIEW MODAL ===== */}
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
          onClick={handleCloseDocModal}
        >
          <div
            style={{
              background: '#0f172a',
              border: '1px solid var(--border-color)',
              borderRadius: 'var(--radius-lg)',
              width: '100%',
              maxWidth: viewingDoc.hasBackFile ? '1100px' : '960px',
              maxHeight: '94vh',
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
                <Shield size={22} style={{ color: 'var(--warning)' }} />
                <div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                    <h3 style={{ fontSize: '1.05rem', fontWeight: 800, margin: 0 }}>
                      {viewingDoc.sub.recyclerName}
                    </h3>
                    <span
                      className="badge"
                      style={{
                        fontSize: '0.72rem',
                        padding: '0.15rem 0.45rem',
                        background: viewingDoc.docType === 'ID' ? 'rgba(6, 182, 212, 0.15)' : 'rgba(16, 185, 129, 0.15)',
                        color: viewingDoc.docType === 'ID' ? '#22d3ee' : '#34d399'
                      }}
                    >
                      {viewingDoc.docType === 'ID' ? 'National ID (NIC)' : 'Business Proof'}
                    </span>
                    {viewingDoc.hasBackFile && (
                      <span className="badge" style={{ fontSize: '0.72rem', padding: '0.15rem 0.45rem', background: 'rgba(245, 158, 11, 0.15)', color: '#fbbf24' }}>
                        Both Sides Uploaded
                      </span>
                    )}
                  </div>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                    {viewingDoc.sub.recyclerEmail} · {viewingDoc.fileName}
                    {viewingDoc.backFileName ? ` & ${viewingDoc.backFileName}` : ''}
                  </span>
                </div>
              </div>

              {/* Controls */}
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
                  onClick={handleCloseDocModal}
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
              ref={modalScrollRef}
              style={{
                flex: '1 1 auto',
                overflowY: 'auto',
                padding: '1.25rem',
                background: '#090d16',
                minHeight: 0,
                maxHeight: '66vh'
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
                    height: '580px',
                    border: 'none',
                    borderRadius: 'var(--radius-sm)',
                    background: '#fff'
                  }}
                />
              )}

              {/* TWO SEPARATE PHOTOS (Front & Back) SIDE-BY-SIDE */}
              {viewingDoc.hasBackFile && viewingDoc.frontUrl && !viewingDoc.isPdf && (
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(360px, 1fr))', gap: '1.25rem', width: '100%' }}>
                  {/* Front Side Card */}
                  <div
                    style={{
                      background: 'rgba(255, 255, 255, 0.03)',
                      border: '1px solid var(--border-color)',
                      borderRadius: 'var(--radius-md)',
                      padding: '1rem'
                    }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '0.75rem' }}>
                      <span style={{ fontWeight: 700, color: '#38bdf8', fontSize: '0.9rem' }}>
                        📷 FRONT SIDE PHOTO
                      </span>
                      <a
                        href={viewingDoc.frontUrl}
                        target="_blank"
                        rel="noreferrer"
                        className="btn btn-secondary"
                        style={{ padding: '3px 8px', fontSize: '0.75rem', display: 'flex', alignItems: 'center', gap: '4px' }}
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

                  {/* Back Side Card */}
                  <div
                    style={{
                      background: 'rgba(255, 255, 255, 0.03)',
                      border: '1px solid var(--border-color)',
                      borderRadius: 'var(--radius-md)',
                      padding: '1rem'
                    }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '0.75rem' }}>
                      <span style={{ fontWeight: 700, color: '#38bdf8', fontSize: '0.9rem' }}>
                        📷 BACK SIDE PHOTO
                      </span>
                      {viewingDoc.backUrl ? (
                        <a
                          href={viewingDoc.backUrl}
                          target="_blank"
                          rel="noreferrer"
                          className="btn btn-secondary"
                          style={{ padding: '3px 8px', fontSize: '0.75rem', display: 'flex', alignItems: 'center', gap: '4px' }}
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
                        Back side photo was not provided or could not be loaded.
                      </div>
                    )}
                  </div>
                </div>
              )}

              {/* SINGLE PHOTO / COMPOSITE DOCUMENT */}
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
                        justifyContent: 'space-between',
                        flexWrap: 'wrap',
                        gap: '0.5rem'
                      }}
                    >
                      <span>💡 <strong>Front and Back sides</strong> are included in this document. Scroll down or click below to inspect both sides.</span>
                      <div style={{ display: 'flex', gap: '0.5rem' }}>
                        <button
                          type="button"
                          className="btn btn-secondary"
                          onClick={() => {
                            if (modalScrollRef.current) {
                              modalScrollRef.current.scrollTop = 0;
                            }
                          }}
                          style={{ padding: '3px 8px', fontSize: '0.75rem' }}
                        >
                          ⬆️ Front Side
                        </button>
                        <button
                          type="button"
                          className="btn btn-secondary"
                          onClick={() => {
                            if (modalScrollRef.current) {
                              modalScrollRef.current.scrollTop = modalScrollRef.current.scrollHeight;
                            }
                          }}
                          style={{ padding: '3px 8px', fontSize: '0.75rem' }}
                        >
                          ⬇️ Back Side
                        </button>
                      </div>
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

            {/* Modal Footer / Review Actions */}
            <div
              style={{
                flexShrink: 0,
                position: 'sticky',
                bottom: 0,
                zIndex: 10,
                padding: '1rem 1.25rem',
                borderTop: '1px solid var(--border-color)',
                background: '#0f172a',
                display: 'flex',
                flexDirection: 'column',
                gap: '0.75rem'
              }}
            >
              {modalRejectOpen ? (
                <div style={{ width: '100%' }}>
                  <label style={{ fontSize: '0.82rem', fontWeight: 600, color: 'var(--danger)', display: 'block', marginBottom: '0.35rem' }}>
                    Reason for Rejection *
                  </label>
                  <textarea
                    className="form-input"
                    rows={2}
                    placeholder="Enter clear instructions for why the document was rejected so the recycler can fix it..."
                    value={modalRejectReason}
                    onChange={(e) => setModalRejectReason(e.target.value)}
                    style={{ width: '100%', fontSize: '0.85rem', marginBottom: '0.5rem' }}
                  />
                  <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '0.5rem' }}>
                    <button
                      type="button"
                      className="btn btn-secondary"
                      onClick={() => setModalRejectOpen(false)}
                      style={{ padding: '0.4rem 0.85rem' }}
                    >
                      Cancel
                    </button>
                    <button
                      type="button"
                      className="btn btn-danger"
                      onClick={() => {
                        const trimmed = (modalRejectReason || '').trim();
                        if (!trimmed) {
                          setError('Please provide a reason explaining why the document was rejected.');
                          return;
                        }
                        handleAction(viewingDoc.documentId, 'reject', trimmed);
                      }}
                      disabled={actionLoading === viewingDoc.documentId}
                      style={{
                        padding: '0.45rem 1.15rem',
                        backgroundColor: 'var(--danger)',
                        color: '#fff',
                        borderColor: 'var(--danger)',
                        fontWeight: 700,
                        display: 'flex',
                        alignItems: 'center',
                        gap: '0.45rem',
                        boxShadow: '0 4px 12px rgba(239, 68, 68, 0.4)',
                        cursor: 'pointer'
                      }}
                    >
                      {actionLoading === viewingDoc.documentId ? (
                        <div className="spinner-small" />
                      ) : (
                        <><XCircle size={16} /> Confirm Rejection</>
                      )}
                    </button>
                  </div>
                </div>
              ) : (
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '0.75rem' }}>
                  <span style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
                    Make a verification decision for <strong>{viewingDoc.sub.recyclerName}</strong>:
                  </span>
                  <div style={{ display: 'flex', gap: '0.65rem' }}>
                    <button
                      type="button"
                      className="btn btn-danger"
                      onClick={() => handleOpenRejectDialog({
                        documentId: viewingDoc.documentId,
                        recyclerName: viewingDoc.sub.recyclerName,
                        recyclerEmail: viewingDoc.sub.recyclerEmail,
                        documentType: viewingDoc.docType
                      })}
                      disabled={actionLoading === viewingDoc.documentId}
                      style={{
                        padding: '0.45rem 1rem',
                        display: 'flex',
                        alignItems: 'center',
                        gap: '0.4rem',
                        backgroundColor: 'var(--danger)',
                        color: '#fff',
                        borderColor: 'var(--danger)',
                        fontWeight: 600,
                        cursor: 'pointer'
                      }}
                    >
                      <XCircle size={16} /> Reject Document
                    </button>

                    <button
                      type="button"
                      className="btn btn-success"
                      onClick={() => handleAction(viewingDoc.documentId, 'verify')}
                      disabled={actionLoading === viewingDoc.documentId}
                      style={{ padding: '0.45rem 1.25rem', display: 'flex', alignItems: 'center', gap: '0.4rem', fontWeight: 600, cursor: 'pointer' }}
                    >
                      {actionLoading === viewingDoc.documentId ? (
                        <div className="spinner-small" />
                      ) : (
                        <><CheckCircle2 size={16} /> Verify & Approve</>
                      )}
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* ===== DEDICATED REJECT CONFIRMATION MODAL ===== */}
      {rejectDialog && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0, 0, 0, 0.75)',
            backdropFilter: 'blur(6px)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 2200,
            padding: '1rem'
          }}
          onClick={() => !actionLoading && setRejectDialog(null)}
        >
          <div
            className="glass-card"
            style={{
              background: '#0f172a',
              border: '1px solid rgba(239, 68, 68, 0.4)',
              borderRadius: 'var(--radius-lg)',
              width: '100%',
              maxWidth: '520px',
              padding: '1.75rem',
              boxShadow: '0 25px 60px rgba(0, 0, 0, 0.85)',
              position: 'relative'
            }}
            onClick={(e) => e.stopPropagation()}
          >
            {/* Header */}
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.25rem' }}>
              <div
                style={{
                  width: '42px',
                  height: '42px',
                  borderRadius: '50%',
                  background: 'rgba(239, 68, 68, 0.15)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0
                }}
              >
                <XCircle size={24} style={{ color: '#f87171' }} />
              </div>
              <div style={{ flex: 1 }}>
                <h3 style={{ fontSize: '1.15rem', fontWeight: 800, margin: 0, color: '#f87171' }}>
                  Reject KYC Submission
                </h3>
                <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)', margin: '0.15rem 0 0 0' }}>
                  {rejectDialog.recyclerName} ({rejectDialog.recyclerEmail})
                </p>
              </div>
              <button
                type="button"
                onClick={() => setRejectDialog(null)}
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

            {/* Document badge */}
            <div style={{ marginBottom: '1rem', display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
              <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>Document:</span>
              <span
                className="badge"
                style={{
                  fontSize: '0.75rem',
                  padding: '0.2rem 0.6rem',
                  background: rejectDialog.docType === 'ID' ? 'rgba(6, 182, 212, 0.15)' : 'rgba(16, 185, 129, 0.15)',
                  color: rejectDialog.docType === 'ID' ? '#22d3ee' : '#34d399',
                  border: `1px solid ${rejectDialog.docType === 'ID' ? 'rgba(6, 182, 212, 0.3)' : 'rgba(16, 185, 129, 0.3)'}`
                }}
              >
                {rejectDialog.docType === 'ID' ? 'National ID (NIC)' : 'Business Proof'}
              </span>
            </div>

            {/* Rejection comment input */}
            <div style={{ marginBottom: '1.25rem' }}>
              <label style={{ display: 'block', fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-primary)', marginBottom: '0.35rem' }}>
                Rejection Reason / Guidance for Recycler <span style={{ color: 'var(--danger)' }}>*</span>
              </label>
              <p style={{ fontSize: '0.78rem', color: 'var(--text-muted)', margin: '0 0 0.5rem 0' }}>
                Please provide specific instructions on what needs to be fixed before the recycler re-submits.
              </p>
              <textarea
                className="form-input"
                rows={3}
                autoFocus
                placeholder="e.g., The photo of the back side of your NIC was not provided or is unreadable. Please upload clear photos of both sides."
                value={rejectDialog.reason}
                onChange={(e) => setRejectDialog((prev) => ({ ...prev, reason: e.target.value, error: '' }))}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
                    e.preventDefault();
                    handleConfirmRejectDialog();
                  }
                }}
                style={{
                  width: '100%',
                  fontSize: '0.85rem',
                  padding: '0.65rem 0.75rem',
                  borderColor: rejectDialog.error ? 'var(--danger)' : undefined,
                  borderRadius: 'var(--radius-sm)'
                }}
              />
              {rejectDialog.error && (
                <div style={{ color: '#f87171', fontSize: '0.8rem', marginTop: '0.4rem', display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
                  <AlertCircle size={14} />
                  <span>{rejectDialog.error}</span>
                </div>
              )}
            </div>

            {/* Buttons */}
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: '0.75rem' }}>
              <button
                type="button"
                className="btn btn-secondary"
                onClick={() => setRejectDialog(null)}
                disabled={actionLoading === rejectDialog.docId}
                style={{ padding: '0.55rem 1.1rem', fontSize: '0.875rem' }}
              >
                Cancel
              </button>
              <button
                type="button"
                className="btn btn-danger"
                onClick={handleConfirmRejectDialog}
                disabled={actionLoading === rejectDialog.docId}
                style={{
                  padding: '0.55rem 1.3rem',
                  fontSize: '0.875rem',
                  backgroundColor: 'var(--danger)',
                  color: '#fff',
                  fontWeight: 700,
                  display: 'flex',
                  alignItems: 'center',
                  gap: '0.45rem',
                  boxShadow: '0 4px 14px rgba(239, 68, 68, 0.4)',
                  cursor: 'pointer'
                }}
              >
                {actionLoading === rejectDialog.docId ? (
                  <div className="spinner-small" />
                ) : (
                  <><XCircle size={16} /> Confirm Rejection</>
                )}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
