import React, { useState, useEffect, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { Plus, AlertCircle, CheckCircle2, Loader2, Upload, X } from 'lucide-react';

function CreateListingView() {
  const { user, token } = useAuth();
  const [loading, setLoading] = useState(true);
  const [valuations, setValuations] = useState([]);
  const [selectedValuation, setSelectedValuation] = useState(null);
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
      const res = await fetch('/api/listings', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${authToken}`,
        },
        body: JSON.stringify({
          valuationId: selectedValuation.id,
          title: title.trim(),
          description: description.trim() || null,
          price: priceNum,
          photoPath: photoPath ? photoPath.trim() : null,
        }),
      });

      const data = await res.json();

      if (!res.ok) {
        setError(data.message || data.title || 'Failed to create listing.');
        return;
      }

      setMessage('Listing created successfully.');
      setTitle('');
      setDescription('');
      setPrice('');
      handleRemovePhoto();
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
          <label className="form-label">Valued Item</label>
          {valuations.length === 0 ? (
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>
              No valued items available. Value collected items first.
            </p>
          ) : (
            <select
              className="form-select"
              value={selectedValuation?.id || ''}
              onChange={(e) => {
                const v = valuations.find(v => v.id === e.target.value);
                setSelectedValuation(v || null);
                setError('');
              }}
              style={{ width: '100%' }}
              disabled={submitting}
            >
              <option value="">— Select a valued item —</option>
              {valuations.map(v => (
                <option key={v.id} value={v.id}>
                  {v.itemName || 'Valued Item'} — {v.condition} · {new Intl.NumberFormat('en-SL').format(v.price)} LKR
                </option>
              ))}
            </select>
          )}
        </div>

        {selectedValuation && (
          <div style={{
            padding: '0.75rem 1rem',
            background: 'rgba(16, 185, 129, 0.08)',
            border: '1px solid rgba(16, 185, 129, 0.2)',
            borderRadius: 'var(--radius-sm)',
            marginBottom: '1.5rem',
            fontSize: '0.85rem',
            color: 'var(--text-secondary)',
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.3rem' }}>
              <CheckCircle2 size={14} style={{ color: 'var(--success)' }} />
              <strong style={{ color: 'var(--text-primary)' }}>
                {selectedValuation.itemName || 'Valued E-Waste Item'}
              </strong>
            </div>
            <div>Condition: <strong>{selectedValuation.condition}</strong> · Valued at{' '}
              <strong>{new Intl.NumberFormat('en-SL').format(selectedValuation.price)} LKR</strong>
            </div>
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
              Creating...
            </>
          ) : (
            <>
              <Plus size={18} style={{ marginRight: '0.5rem' }} />
              Create Listing
            </>
          )}
        </button>
      </div>
    </div>
  );
}

export default CreateListingView;
