import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { LayoutGrid, Search, ChevronLeft, ChevronRight, AlertCircle, CheckCircle2, Loader2, DollarSign, Mail, Clock, X as XIcon } from 'lucide-react';

const PAGE_SIZE = 12;

function MarketplaceView() {
  const { user, token } = useAuth();
  const [loading, setLoading] = useState(true);
  const [listings, setListings] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(PAGE_SIZE);
  const [searchTerm, setSearchTerm] = useState('');
  const [error, setError] = useState('');
  const [selectedListing, setSelectedListing] = useState(null);

  const authToken = token || localStorage.getItem('ecotrack_token');

  // Reset to page 1 when search changes
  const handleSearch = (value) => {
    setSearchTerm(value);
    setPage(1);
    setSelectedListing(null);
  };

  // Load listings when params change
  useEffect(() => {
    loadListings();
  }, [page, pageSize, searchTerm]);

  const loadListings = async () => {
    setLoading(true);
    setError('');
    try {
      const params = new URLSearchParams();
      if (searchTerm.trim()) params.set('keyword', searchTerm.trim());
      params.set('page', page);
      params.set('pageSize', pageSize);

      const res = await fetch(`/api/listings?${params.toString()}`, {
        headers: {
          ...(authToken ? { Authorization: `Bearer ${authToken}` } : {})
        }
      });
      if (!res.ok) throw new Error('Failed to load listings');
      const data = await res.json();
      setListings(data.listings || []);
      setTotalCount(data.pagination?.totalCount || 0);
    } catch (err) {
      setError('Could not load listings. Please try again.');
      setListings([]);
    } finally {
      setLoading(false);
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);
  const hasNext = page < totalPages;
  const hasPrev = page > 1;

  const getStatusBadge = (status) => {
    switch (status) {
      case 'Available':
        return (
          <span className="badge badge-available" style={{
            display: 'inline-flex', alignItems: 'center', gap: '0.35rem',
            padding: '0.2rem 0.65rem', fontSize: '0.7rem', fontWeight: 600,
            borderRadius: '9999px', textTransform: 'uppercase', letterSpacing: '0.03em',
            background: '#065f46', color: '#a7f3d0',
          }}>
            <CheckCircle2 size={10} /> Available
          </span>
        );
      case 'Reserved':
        return (
          <span className="badge badge-reserved" style={{
            display: 'inline-flex', alignItems: 'center', gap: '0.35rem',
            padding: '0.2rem 0.65rem', fontSize: '0.7rem', fontWeight: 600,
            borderRadius: '9999px', textTransform: 'uppercase', letterSpacing: '0.03em',
            background: '#78350f', color: '#fde68a',
          }}>
            <Clock size={10} /> Reserved
          </span>
        );
      case 'Sold':
        return (
          <span className="badge badge-sold" style={{
            display: 'inline-flex', alignItems: 'center', gap: '0.35rem',
            padding: '0.2rem 0.65rem', fontSize: '0.7rem', fontWeight: 600,
            borderRadius: '9999px', textTransform: 'uppercase', letterSpacing: '0.03em',
            background: '#991b1b', color: '#fca5a5',
          }}>
            <CheckCircle2 size={10} /> Sold
          </span>
        );
      default:
        return null;
    }
  };

  const formatPrice = (price) => {
    if (!price) return '—';
    return new Intl.NumberFormat('en-SL').format(price) + ' LKR';
  };

  if (loading && listings.length === 0) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '60vh' }}>
        <Loader2 size={36} className="spin" style={{ color: 'var(--primary)' }} />
      </div>
    );
  }

  // Detail view modal
  if (selectedListing) {
    return (
      <div style={{
        position: 'fixed', inset: 0, zIndex: 1000,
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        background: 'rgba(0,0,0,0.6)', backdropFilter: 'blur(4px)',
      }}>
        <div className="glass-card" style={{
          maxWidth: '520px', width: '90%', maxHeight: '85vh', overflowY: 'auto',
          padding: '2rem'
        }}>
          {/* Close */}
          <button
            style={{
              position: 'absolute',
              top: '12px',
              right: '12px',
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: 'var(--text-secondary)',
              padding: '4px',
            }}
            onClick={() => setSelectedListing(null)}
          >
            <XIcon size={20} />
          </button>

          {/* Photo */}
          {selectedListing.photoPath && (
            <div style={{ marginBottom: '1.25rem', borderRadius: 'var(--radius)', overflow: 'hidden', background: 'var(--surface-color)' }}>
              <img
                src={selectedListing.photoPath}
                alt={selectedListing.title}
                style={{ width: '100%', height: '200px', objectFit: 'cover' }}
              />
            </div>
          )}

          {/* Status + Title */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.75rem' }}>
            {getStatusBadge(selectedListing.status)}
          </div>
          <h2 style={{ fontSize: '1.35rem', fontWeight: 800, margin: '0 0 0.5rem' }}>
            {selectedListing.title}
          </h2>

          {/* Price */}
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '1.25rem' }}>
            <DollarSign size={18} style={{ color: 'var(--accent)' }} />
            <span style={{ fontSize: '1.15rem', fontWeight: 700, color: 'var(--accent)' }}>
              {formatPrice(selectedListing.price)}
            </span>
          </div>

          {/* Description */}
          {selectedListing.description && (
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.9rem', lineHeight: 1.6, marginBottom: '1.25rem' }}>
              {selectedListing.description}
            </p>
          )}

          {/* Related item */}
          {selectedListing.valuation && (
            <div style={{
              padding: '0.75rem 1rem',
              background: 'rgba(255,255,255,0.03)',
              borderRadius: 'var(--radius-sm)',
              border: '1px solid var(--border-color)',
              marginBottom: '1.25rem',
            }}>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginBottom: '0.4rem' }}>Original item</div>
              <div style={{ fontWeight: 600, fontSize: '0.9rem' }}>{selectedListing.valuation.itemName || '—'}</div>
              <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.3rem' }}>
                Condition: {selectedListing.valuation.condition} · Valued at {formatPrice(selectedListing.valuation.price)}
              </div>
            </div>
          )}

          {/* Created */}
          <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginBottom: '1.5rem' }}>
            Listed {new Date(selectedListing.createdAt).toLocaleDateString('en-SL', {
              day: 'numeric', month: 'short', year: 'numeric'
            })}
          </div>

          {/* Contact button */}
          <button
          className="btn btn-primary"
          style={{ width: '100%', padding: '0.8rem', fontSize: '0.95rem' }}
          >
          <Mail size={18} style={{ marginRight: '0.5rem' }} />
          Contact Seller
          </button>
        </div>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '1.5rem 1rem' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.5rem' }}>
        <LayoutGrid size={26} style={{ color: 'var(--accent)' }} />
        <h1 style={{ fontSize: '1.5rem', fontWeight: 800, margin: 0 }}>Marketplace</h1>
        <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
          {totalCount} available item{totalCount !== 1 ? 's' : ''}
        </span>
      </div>

      {/* Search */}
      <div style={{ position: 'relative', marginBottom: '1.5rem' }}>
        <Search size={16} style={{
          position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)',
          color: 'var(--text-muted)'
        }} />
        <input
          type="text"
          className="form-input"
          placeholder="Search listings..."
          value={searchTerm}
          onChange={(e) => handleSearch(e.target.value)}
          style={{ paddingLeft: '2.5rem', paddingRight: '1rem', width: '100%', maxWidth: '400px' }}
        />
        {searchTerm && (
          <button
            style={{
              position: 'absolute', right: '10px', top: '50%', transform: 'translateY(-50%)',
              background: 'none', border: 'none', cursor: 'pointer',
              color: 'var(--text-muted)', padding: '2px 6px', fontSize: '0.8rem',
            }}
            onClick={() => handleSearch('')}
          >
            <X size={14} />
          </button>
        )}
      </div>

      {/* Error */}
      {error && (
        <div className="alert alert-danger" style={{ marginBottom: '1rem' }}>
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}

      {/* Listings Grid */}
      {listings.length === 0 && !loading ? (
        <div className="glass-card" style={{ textAlign: 'center', padding: '4rem 2rem' }}>
          <LayoutGrid size={48} style={{ color: 'var(--text-secondary)', marginBottom: '1rem' }} />
          <p style={{ color: 'var(--text-secondary)', fontSize: '1rem', margin: 0 }}>
            No listings found.
          </p>
          {searchTerm && (
            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', marginTop: '0.5rem' }}>
              Try a different search term.
            </p>
          )}
        </div>
      ) : (
        <>
          <div style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))',
            gap: '1rem',
          }}>
            {listings.map((listing) => (
              <div
                key={listing.id}
                className="glass-card"
                style={{
                  padding: '0',
                  cursor: 'pointer',
                  overflow: 'hidden',
                  transition: 'transform 0.15s ease, box-shadow 0.15s ease',
                }}
                onClick={() => setSelectedListing(listing)}
                onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') setSelectedListing(listing); }}
                tabIndex={0}
                role="button"
              >
                {/* Photo */}
                {listing.photoPath && (
                  <div style={{ height: '150px', background: 'var(--surface-color)', overflow: 'hidden' }}>
                    <img
                      src={listing.photoPath}
                      alt={listing.title}
                      style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                    />
                  </div>
                )}

                {/* Content */}
                <div style={{ padding: '1rem' }}>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                    {getStatusBadge(listing.status)}
                  </div>

                  <h3 style={{
                    fontSize: '0.95rem', fontWeight: 700, margin: '0 0 0.35rem',
                    whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis',
                  }}>
                    {listing.title}
                  </h3>

                  {listing.valuation?.itemName && (
                    <p style={{ fontSize: '0.75rem', color: 'var(--text-muted)', margin: '0 0 0.75rem' }}>
                      From: {listing.valuation.itemName}
                    </p>
                  )}

                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', marginBottom: '0.5rem' }}>
                    <DollarSign size={14} style={{ color: 'var(--accent)' }} />
                    <span style={{ fontWeight: 700, color: 'var(--accent)', fontSize: '1rem' }}>
                      {formatPrice(listing.price)}
                    </span>
                  </div>

                  <div style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>
                    {new Date(listing.createdAt).toLocaleDateString('en-SL', {
                      day: 'numeric', month: 'short'
                    })}
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div style={{
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              gap: '0.5rem', marginTop: '2rem',
            }}>
              <button
                className="btn btn-secondary"
                style={{ padding: '0.45rem 0.75rem', display: 'flex', alignItems: 'center', gap: '0.3rem' }}
                onClick={() => setPage(p => p - 1)}
                disabled={!hasPrev}
              >
                <ChevronLeft size={16} />
                Previous
              </button>

              {Array.from({ length: totalPages }, (_, i) => i + 1).map(p => (
                <button
                  key={p}
                  className={`btn ${p === page ? 'btn-primary' : 'btn-secondary'}`}
                  style={{
                    padding: '0.45rem 0.75rem',
                    minWidth: '36px',
                    fontSize: '0.85rem',
                  }}
                  onClick={() => setPage(p)}
                >
                  {p}
                </button>
              ))}

              <button
                className="btn btn-secondary"
                style={{ padding: '0.45rem 0.75rem', display: 'flex', alignItems: 'center', gap: '0.3rem' }}
                onClick={() => setPage(p => p + 1)}
                disabled={!hasNext}
              >
                Next
                <ChevronRight size={16} />
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}

export default MarketplaceView;
