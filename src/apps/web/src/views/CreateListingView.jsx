import React, { useState, useEffect, useRef } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Plus, AlertCircle, CheckCircle2, Loader2, Upload, X, Search, Copy, Check, ClipboardPaste, ArrowRight, Tag, ShoppingBag } from 'lucide-react';
import { copyTextToClipboard, pasteTextFromClipboard } from '../utils/clipboard';
import { getMyStatus } from '../services/kycApi';

function CreateListingView() {
  const { user, token } = useAuth();
  const [searchParams] = useSearchParams();
  const [loading, setLoading] = useState(true);
  const [valuations, setValuations] = useState([]);
  const [selectedValuation, setSelectedValuation] = useState(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [copiedId, setCopiedId] = useState(null);
  const [kycStatus, setKycStatus] = useState(null);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [price, setPrice] = useState('');
  const [photoPath, setPhotoPath] = useState('');
  const [photoFile, setPhotoFile] = useState(null);
  const [photoPreview, setPhotoPreview] = useState('');
  const [uploadingPhoto, setUploadingPhoto] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const fileInputRef = useRef(null);

  const authToken = token || localStorage.getItem('ecotrack_token');

  useEffect(() => {
    if (authToken) loadValuations();
  }, [authToken]);

  useEffect(() => {
    getMyStatus()
      .then((res) => {
        setKycStatus(res.data?.response || null);
      })
      .catch(() => {
        // Non-critical, ignore
      });
  }, []);

  const loadValuations = async () => {
    setLoading(true);
    setError('');
    try {
      const res = await fetch('/api/valuations', {
        headers: { Authorization: `Bearer ${authToken}` }
      });
      if (!res.ok) throw new Error('Failed to load valuations');
      const data = await res.json();
      const list = Array.isArray(data) ? data : data?.data || [];
      setValuations(list.filter(v => !v.status || v.status === 'Available' || v.status === 'Collected'));
    } catch {
      setError('Could not load your valued items.');
      setValuations([]);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectValuation = (v) => {
    setSelectedValuation(v || null);
    setError('');
    if (v) {
      setTitle((prev) => {
        if (!prev.trim() || valuations.some(item => prev === `${item.itemName} (${item.condition})`)) {
          return `${v.itemName || 'Item'} (${v.condition || 'Good'})`;
        }
        return prev;
      });
      setPrice((prev) => {
        if (!prev || valuations.some(item => prev === String(item.price))) {
          return v.price ? String(v.price) : '';
        }
        return prev;
      });
    }
  };

  // Pre-select item if valuationId or itemId was passed in URL search params
  useEffect(() => {
    if (valuations.length > 0 && !selectedValuation) {
      const paramValuationId = searchParams.get('valuationId');
      const paramItemId = searchParams.get('itemId');

      if (paramValuationId || paramItemId) {
        const matched = valuations.find(v =>
          (paramValuationId && v.id?.toLowerCase() === paramValuationId.toLowerCase()) ||
          (paramItemId && v.pickupItemId?.toLowerCase() === paramItemId.toLowerCase())
        );
        if (matched) {
          handleSelectValuation(matched);
        }
      }
    }
  }, [valuations, searchParams]);

  const handlePasteId = async () => {
    const text = await pasteTextFromClipboard();
    if (!text) return;
    const clean = text.trim();
    if (!clean) return;

    const matched = valuations.find(v =>
      (v.id && v.id.toLowerCase() === clean.toLowerCase()) ||
      (v.pickupItemId && v.pickupItemId.toLowerCase() === clean.toLowerCase())
    );

    if (matched) {
      handleSelectValuation(matched);
      setMessage(`Found and selected: ${matched.itemName || 'Valued Item'} (${matched.condition})`);
      setError('');
    } else {
      setError(`No valued item found matching ID "${clean}". Make sure the item has been valued first.`);
    }
  };

  const handleCopy = async (id) => {
    if (!id) return;
    const ok = await copyTextToClipboard(id);
    if (ok) {
      setCopiedId(id);
      setTimeout(() => setCopiedId(null), 2000);
    }
  };

  const filteredValuations = valuations.filter((v) => {
    if (!searchTerm.trim()) return true;
    const term = searchTerm.toLowerCase();
    return (
      (v.itemName && v.itemName.toLowerCase().includes(term)) ||
      (v.condition && v.condition.toLowerCase().includes(term)) ||
      (v.id && v.id.toLowerCase().includes(term)) ||
      (v.pickupItemId && v.pickupItemId.toLowerCase().includes(term))
    );
  });

  const handlePhotoChange = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      setError('Please select an image file (PNG, JPG, WEBP).');
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      setError('Image file size must not exceed 5 MB.');
      return;
    }

    setError('');
    setPhotoFile(file);
    setPhotoPreview(URL.createObjectURL(file));
    setUploadingPhoto(true);

    try {
      const formData = new FormData();
      formData.append('file', file);

      const res = await fetch('/api/listings/upload-photo', {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${authToken}`,
        },
        body: formData,
      });

      const data = await res.json();
      if (!res.ok) {
        throw new Error(data.message || 'Failed to upload photo.');
      }

      setPhotoPath(data.photoPath);
    } catch (err) {
      setError(err.message || 'Failed to upload photo.');
      setPhotoFile(null);
      setPhotoPreview('');
      setPhotoPath('');
      if (fileInputRef.current) fileInputRef.current.value = '';
    } finally {
      setUploadingPhoto(false);
    }
  };

  const handleRemovePhoto = () => {
    setPhotoFile(null);
    setPhotoPreview('');
    setPhotoPath('');
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setMessage('');

    if (!selectedValuation) {
      setError('Please select a valued item to list.');
      return;
    }
    if (uploadingPhoto) {
      setError('Please wait for photo to finish uploading.');
      return;
    }
    if (!title.trim()) {
      setError('Title is required.');
      return;
    }
    const priceNum = parseFloat(price);
    if (!price || isNaN(priceNum) || priceNum <= 0) {
      setError('Price is required.');
      return;
    }

    setSubmitting(true);
    try {
      const isUpdating = Boolean(selectedValuation.isListed && selectedValuation.listingId);
      const url = isUpdating ? `/api/listings/${selectedValuation.listingId}` : '/api/listings';
      const method = isUpdating ? 'PUT' : 'POST';

      const payload = isUpdating
        ? {
            title: title.trim(),
            description: description.trim() || null,
            price: priceNum,
            photoPath: photoPath ? photoPath.trim() : null,
            status: selectedValuation.listingStatus || 'Available',
          }
        : {
            valuationId: selectedValuation.id,
            title: title.trim(),
            description: description.trim() || null,
            price: priceNum,
            photoPath: photoPath ? photoPath.trim() : null,
          };

      const res = await fetch(url, {
        method,
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${authToken}`,
        },
        body: JSON.stringify(payload),
      });

      const data = await res.json();

      if (!res.ok) {
        setError(data.message || data.title || (isUpdating ? 'Failed to update listing.' : 'Failed to create listing.'));
        return;
      }

      setMessage(isUpdating ? 'Listing updated successfully.' : 'Listing created successfully.');
      if (!isUpdating) {
        setTitle('');
        setDescription('');
        setPrice('');
        setSelectedValuation(null);
        handleRemovePhoto();
      }
      loadValuations();
    } catch {
      setError('Network error. Please try again.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '60vh' }}>
        <Loader2 size={36} className="spin" style={{ color: 'var(--primary)' }} />
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '720px', margin: '0 auto', padding: '1.5rem 1rem' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.5rem' }}>
        <Plus size={26} style={{ color: 'var(--accent)' }} />
        <h1 style={{ fontSize: '1.5rem', fontWeight: 800, margin: 0 }}>Create Listing</h1>
      </div>

      {/* KYC Status Advisory Banner */}
      {kycStatus && (!kycStatus.hasSubmittedDocument || kycStatus.status !== 'Verified') && (
        <div
          style={{
            padding: '0.75rem 1rem',
            background: 'rgba(245, 158, 11, 0.1)',
            border: '1px solid rgba(245, 158, 11, 0.3)',
            borderRadius: 'var(--radius-sm)',
            marginBottom: '1.25rem',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            flexWrap: 'wrap',
            gap: '0.5rem',
            fontSize: '0.85rem',
            color: 'var(--text-primary)'
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <AlertCircle size={16} style={{ color: 'var(--warning)', flexShrink: 0 }} />
            <span>
              Recycler Notice: Your KYC status is{' '}
              <strong style={{ color: kycStatus.status === 'Rejected' ? 'var(--danger)' : 'var(--warning)' }}>
                {kycStatus.status || 'Not Submitted'}
              </strong>
              . Complete verification to earn the verified badge and build marketplace buyer trust.
            </span>
          </div>
          <Link
            to="/kyc"
            className="btn btn-secondary"
            style={{ fontSize: '0.75rem', padding: '0.3rem 0.65rem', whiteSpace: 'nowrap', display: 'inline-flex', alignItems: 'center', gap: '4px' }}
          >
            Verify KYC <ArrowRight size={12} />
          </Link>
        </div>
      )}

      {error && (
        <div className="alert alert-danger" style={{ marginBottom: '1rem' }}>
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}

      {message && (
        <div className="alert alert-success" style={{ marginBottom: '1rem' }}>
          <CheckCircle2 size={16} />
          <span>{message}</span>
        </div>
      )}

      <div className="glass-card" style={{ padding: '1.5rem' }}>
        {/* Valued Item Selection */}
        <div className="form-group" style={{ marginBottom: '1.25rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.45rem' }}>
            <label className="form-label" style={{ marginBottom: 0 }}>
              Valued Item <span style={{ color: 'var(--danger)' }}>*</span>
            </label>
            {valuations.length > 0 && (
              <button
                type="button"
                onClick={handlePasteId}
                className="btn btn-secondary"
                style={{
                  padding: '3px 8px',
                  fontSize: '0.75rem',
                  display: 'inline-flex',
                  alignItems: 'center',
                  gap: '4px',
                  background: 'rgba(255, 255, 255, 0.08)',
                }}
                title="Paste Valuation ID or Item ID from clipboard"
              >
                <ClipboardPaste size={12} />
                Paste ID
              </button>
            )}
          </div>

          {valuations.length === 0 ? (
            <div
              style={{
                padding: '1.25rem',
                background: 'rgba(255, 255, 255, 0.02)',
                border: '1px dashed var(--border-color)',
                borderRadius: 'var(--radius-sm)',
                textAlign: 'center',
              }}
            >
              <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', margin: '0 0 0.5rem 0' }}>
                No valued items available to list. Value your collected pickup items first.
              </p>
              <Link
                to="/valuations"
                className="btn btn-secondary"
                style={{ fontSize: '0.8rem', padding: '0.35rem 0.75rem', display: 'inline-flex', alignItems: 'center', gap: '5px' }}
              >
                <Tag size={13} /> Go to Valuations
              </Link>
            </div>
          ) : (
            <>
              {valuations.length > 2 && (
                <div style={{ position: 'relative', marginBottom: '0.5rem' }}>
                  <Search
                    size={14}
                    style={{ position: 'absolute', left: '10px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }}
                  />
                  <input
                    type="text"
                    className="form-input"
                    placeholder="Search valued items by name, condition, or ID..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    style={{ paddingLeft: '32px', fontSize: '0.85rem' }}
                    disabled={submitting}
                  />
                  {searchTerm && (
                    <button
                      type="button"
                      onClick={() => setSearchTerm('')}
                      style={{
                        position: 'absolute',
                        right: '10px',
                        top: '50%',
                        transform: 'translateY(-50%)',
                        background: 'none',
                        border: 'none',
                        color: 'var(--text-muted)',
                        cursor: 'pointer',
                        padding: 0
                      }}
                    >
                      <X size={14} />
                    </button>
                  )}
                </div>
              )}

              <select
                className="form-select"
                value={selectedValuation?.id || ''}
                onChange={(e) => {
                  const v = valuations.find(v => v.id === e.target.value);
                  handleSelectValuation(v || null);
                }}
                style={{ width: '100%' }}
                disabled={submitting}
              >
                <option value="">— Select a valued item ({filteredValuations.length} available) —</option>
                {filteredValuations.map(v => (
                  <option key={v.id} value={v.id}>
                    {v.itemName || 'Valued Item'} — {v.condition} · {new Intl.NumberFormat('en-SL').format(v.price)} LKR {v.isListed ? `[Listed: ${v.listingStatus || 'Active'}]` : ''}
                  </option>
                ))}
              </select>
            </>
          )}
        </div>

        {selectedValuation && (
          <div style={{
            padding: '1rem',
            background: 'rgba(16, 185, 129, 0.08)',
            border: '1px solid rgba(16, 185, 129, 0.25)',
            borderRadius: 'var(--radius-sm)',
            marginBottom: '1.5rem',
            fontSize: '0.85rem',
            color: 'var(--text-secondary)',
          }}>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '0.5rem', flexWrap: 'wrap', gap: '0.4rem' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <CheckCircle2 size={16} style={{ color: 'var(--success)' }} />
                <strong style={{ color: 'var(--text-primary)', fontSize: '0.95rem' }}>
                  {selectedValuation.itemName || 'Valued E-Waste Item'}
                </strong>
                <span
                  style={{
                    fontSize: '0.7rem',
                    fontWeight: 600,
                    padding: '2px 8px',
                    borderRadius: '9999px',
                    background: 'rgba(16, 185, 129, 0.2)',
                    color: '#34d399',
                    border: '1px solid rgba(16, 185, 129, 0.3)'
                  }}
                >
                  {selectedValuation.condition}
                </span>
              </div>
              <button
                type="button"
                onClick={() => {
                  setSelectedValuation(null);
                  setError('');
                }}
                style={{
                  background: 'none',
                  border: 'none',
                  color: 'var(--text-muted)',
                  cursor: 'pointer',
                  fontSize: '0.75rem',
                  display: 'inline-flex',
                  alignItems: 'center',
                  gap: '3px'
                }}
                title="Clear selected item and choose another"
              >
                <X size={12} /> Change Item
              </button>
            </div>

            <div style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))',
              gap: '0.6rem',
              marginTop: '0.5rem',
              background: 'rgba(0, 0, 0, 0.2)',
              padding: '0.75rem',
              borderRadius: '6px'
            }}>
              <div>
                <span style={{ fontSize: '0.72rem', color: 'var(--text-muted)', display: 'block', textTransform: 'uppercase' }}>
                  Valuation Estimate
                </span>
                <strong style={{ color: 'var(--success)', fontSize: '0.92rem' }}>
                  {new Intl.NumberFormat('en-SL').format(selectedValuation.price)} LKR
                </strong>
              </div>

              <div>
                <span style={{ fontSize: '0.72rem', color: 'var(--text-muted)', display: 'block', textTransform: 'uppercase' }}>
                  Valuation ID
                </span>
                <div style={{ display: 'flex', alignItems: 'center', gap: '4px', marginTop: '2px' }}>
                  <code style={{ fontSize: '0.72rem', color: 'var(--text-secondary)' }}>
                    {selectedValuation.id.slice(0, 8)}...{selectedValuation.id.slice(-4)}
                  </code>
                  <button
                    type="button"
                    onClick={() => handleCopy(selectedValuation.id)}
                    style={{
                      background: 'rgba(255, 255, 255, 0.08)',
                      border: '1px solid rgba(255, 255, 255, 0.15)',
                      borderRadius: '4px',
                      color: copiedId === selectedValuation.id ? 'var(--success)' : 'var(--text-secondary)',
                      cursor: 'pointer',
                      padding: '1px 5px',
                      fontSize: '0.7rem',
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: '2px'
                    }}
                    title="Copy full Valuation ID"
                  >
                    {copiedId === selectedValuation.id ? <Check size={11} /> : <Copy size={11} />}
                    {copiedId === selectedValuation.id ? 'Copied' : 'Copy'}
                  </button>
                </div>
              </div>

              {selectedValuation.pickupItemId && (
                <div>
                  <span style={{ fontSize: '0.72rem', color: 'var(--text-muted)', display: 'block', textTransform: 'uppercase' }}>
                    Pickup Item ID
                  </span>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '4px', marginTop: '2px' }}>
                    <code style={{ fontSize: '0.72rem', color: 'var(--text-secondary)' }}>
                      {selectedValuation.pickupItemId.slice(0, 8)}...{selectedValuation.pickupItemId.slice(-4)}
                    </code>
                    <button
                      type="button"
                      onClick={() => handleCopy(selectedValuation.pickupItemId)}
                      style={{
                        background: 'rgba(255, 255, 255, 0.08)',
                        border: '1px solid rgba(255, 255, 255, 0.15)',
                        borderRadius: '4px',
                        color: copiedId === selectedValuation.pickupItemId ? 'var(--success)' : 'var(--text-secondary)',
                        cursor: 'pointer',
                        padding: '1px 5px',
                        fontSize: '0.7rem',
                        display: 'inline-flex',
                        alignItems: 'center',
                        gap: '2px'
                      }}
                      title="Copy full Pickup Item ID"
                    >
                      {copiedId === selectedValuation.pickupItemId ? <Check size={11} /> : <Copy size={11} />}
                      {copiedId === selectedValuation.pickupItemId ? 'Copied' : 'Copy'}
                    </button>
                  </div>
                </div>
              )}
            </div>

            {selectedValuation.isListed && (
              <div style={{
                marginTop: '0.75rem',
                padding: '0.6rem 0.85rem',
                background: 'rgba(59, 130, 246, 0.1)',
                border: '1px solid rgba(59, 130, 246, 0.25)',
                borderRadius: 'var(--radius-sm)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                flexWrap: 'wrap',
                gap: '0.5rem',
                fontSize: '0.82rem'
              }}>
                <span style={{ color: '#93c5fd' }}>
                  This item is already listed on Marketplace (Status: <strong>{selectedValuation.listingStatus || 'Active'}</strong>). Submitting will update its listing details.
                </span>
                <Link
                  to="/marketplace"
                  className="btn btn-secondary"
                  style={{ fontSize: '0.75rem', padding: '0.25rem 0.6rem', display: 'inline-flex', alignItems: 'center', gap: '4px' }}
                >
                  <ShoppingBag size={12} /> View in Marketplace
                </Link>
              </div>
            )}
          </div>
        )}

        {/* Title */}
        <div className="form-group" style={{ marginBottom: '1rem' }}>
          <label className="form-label">Title <span style={{ color: 'var(--danger)' }}>*</span></label>
          <input
            type="text"
            className="form-input"
            placeholder="e.g. Refurbished Dell Laptop — i5, 8GB, 256GB SSD"
            value={title}
            onChange={(e) => { setTitle(e.target.value); setError(''); }}
            disabled={submitting}
            maxLength={200}
          />
          {error === 'Title is required.' && (
            <div className="form-error">
              <AlertCircle size={13} />
              <span>Title is required.</span>
            </div>
          )}
        </div>

        {/* Description */}
        <div className="form-group" style={{ marginBottom: '1rem' }}>
          <label className="form-label">Description <span style={{ color: 'var(--text-muted)', fontSize: '0.8rem' }}>(optional)</span></label>
          <textarea
            className="form-input"
            placeholder="Describe the item condition, specs, what's included..."
            value={description}
            onChange={(e) => { setDescription(e.target.value); setError(''); }}
            disabled={submitting}
            rows={4}
            maxLength={2000}
            style={{ resize: 'vertical', minHeight: '80px' }}
          />
          <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)', marginTop: '0.3rem' }}>
            {description.length}/2000
          </div>
        </div>

        {/* Price */}
        <div className="form-group" style={{ marginBottom: '1rem' }}>
          <label className="form-label">Price (LKR) <span style={{ color: 'var(--danger)' }}>*</span></label>
          <input
            type="number"
            className="form-input"
            placeholder="e.g. 15000"
            value={price}
            onChange={(e) => { setPrice(e.target.value); setError(''); }}
            disabled={submitting}
            min="0.01"
            step="0.01"
          />
          {error === 'Price is required.' && (
            <div className="form-error">
              <AlertCircle size={13} />
              <span>Price is required.</span>
            </div>
          )}
        </div>

        <div className="form-group" style={{ marginBottom: '1.5rem' }}>
          <label className="form-label">Product Photo <span style={{ color: 'var(--text-muted)', fontSize: '0.8rem' }}>(optional)</span></label>
          <input
            ref={fileInputRef}
            type="file"
            accept="image/png,image/jpeg,image/webp"
            style={{ display: 'none' }}
            onChange={handlePhotoChange}
            disabled={submitting || uploadingPhoto}
          />

          {!photoPreview ? (
            <div
              onClick={() => fileInputRef.current?.click()}
              style={{
                border: '2px dashed var(--border-color)',
                borderRadius: 'var(--radius-md)',
                padding: '1.5rem',
                textAlign: 'center',
                cursor: 'pointer',
                background: 'rgba(255,255,255,0.02)',
              }}
            >
              <Upload size={24} style={{ color: 'var(--accent)', margin: '0 auto 0.5rem', display: 'block' }} />
              <div style={{ fontWeight: 600, fontSize: '0.9rem', marginBottom: '0.25rem' }}>
                Click to choose photo
              </div>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                PNG, JPG or WEBP up to 5 MB
              </div>
            </div>
          ) : (
            <div style={{
              borderRadius: 'var(--radius-md)',
              border: '1px solid var(--border-color)',
              padding: '0.75rem',
              background: 'rgba(255,255,255,0.03)',
              display: 'flex',
              alignItems: 'center',
              gap: '1rem',
            }}>
              <img
                src={photoPreview}
                alt="Preview"
                style={{ width: '64px', height: '64px', objectFit: 'cover', borderRadius: 'var(--radius-sm)' }}
              />
              <div style={{ flex: 1, minWidth: 0 }}>
                <div style={{ fontSize: '0.85rem', fontWeight: 600, textOverflow: 'ellipsis', overflow: 'hidden', whiteSpace: 'nowrap' }}>
                  {photoFile?.name || 'Selected photo'}
                </div>
                <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                  {uploadingPhoto ? (
                    <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                      <Loader2 size={12} className="spin" /> Uploading...
                    </span>
                  ) : photoPath ? (
                    <span style={{ color: 'var(--success)', display: 'flex', alignItems: 'center', gap: '4px' }}>
                      <CheckCircle2 size={12} /> Ready for listing
                    </span>
                  ) : null}
                </div>
              </div>
              <button
                type="button"
                className="btn btn-sm"
                onClick={handleRemovePhoto}
                disabled={uploadingPhoto || submitting}
                style={{
                  background: 'rgba(239, 68, 68, 0.1)',
                  color: 'var(--danger)',
                  border: 'none',
                  padding: '0.4rem 0.6rem',
                  borderRadius: 'var(--radius-sm)',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '4px',
                  fontSize: '0.75rem',
                }}
              >
                <X size={14} /> Remove
              </button>
            </div>
          )}
        </div>

        {/* Submit */}
        <button
          type="submit"
          className="btn btn-primary"
          style={{ width: '100%', padding: '0.8rem', fontSize: '0.95rem' }}
          onClick={handleSubmit}
          disabled={submitting || !selectedValuation}
        >
          {submitting ? (
            <>
              <Loader2 size={18} className="spin" style={{ marginRight: '0.5rem' }} />
              {selectedValuation?.isListed ? 'Updating...' : 'Creating...'}
            </>
          ) : (
            <>
              {selectedValuation?.isListed ? (
                <>
                  <CheckCircle2 size={18} style={{ marginRight: '0.5rem' }} />
                  Update Listing
                </>
              ) : (
                <>
                  <Plus size={18} style={{ marginRight: '0.5rem' }} />
                  Create Listing
                </>
              )}
            </>
          )}
        </button>
      </div>
    </div>
  );
}

export default CreateListingView;
