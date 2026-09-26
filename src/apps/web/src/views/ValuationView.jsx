import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useAuth } from '../context/AuthContext';
import { valuationApi } from '../services/valuationApi';
import { logisticsApi } from '../services/api';
import { PackageSearch, Edit3, CheckCircle2, XCircle, Loader2, Tag, AlertCircle, Loader } from 'lucide-react';

const CONDITIONS = ['Good', 'Fair', 'Poor'];

function ValuationView() {
  const { user } = useAuth();
  const [loading, setLoading] = useState(true);
  const [pickups, setPickups] = useState([]);
  const [selectedPickup, setSelectedPickup] = useState(null);
  const [selectedItems, setSelectedItems] = useState([]);
  const [existingValuation, setExistingValuation] = useState(null);
  const [price, setPrice] = useState('');
  const [condition, setCondition] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [filterCondition, setFilterCondition] = useState('All');
  const [filterStatus, setFilterStatus] = useState('All');
  // Per-item valuation map: itemId -> { price, condition } or null
  const [valuationMap, setValuationMap] = useState({});

  // Reload when user changes (login/logout)
  useEffect(() => {
    if (user?.userId) loadPickups();
  }, [user]);

  // Clear messages when selection changes
  const clearMessages = () => {
    setMessage('');
    setError('');
    setPrice('');
    setCondition('');
    setExistingValuation(null);
  };

  const loadPickups = async () => {
    if (!user?.userId) return;
    setLoading(true);
    setError('');
    try {
      const res = await logisticsApi.getRecyclerSchedule(user.userId);
      const pickupsData = Array.isArray(res.data) ? res.data : [];
      const sorted = [...pickupsData].sort((a, b) =>
        new Date(b.createdAt) - new Date(a.createdAt)
      );
      setPickups(sorted);
    } catch (err) {
      setError('Could not load your collected items. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handlePickupSelect = async (pickup) => {
    if (selectedPickup?.id === pickup.id) {
      // Deselect
      setSelectedPickup(null);
      setSelectedItems([]);
      setExistingValuation(null);
      clearMessages();
      return;
    }
    setSelectedPickup(pickup);
    const items = pickup.items || [];
    setSelectedItems(items);
    clearMessages();
    loadExistingValuation(items);
  };

  const handleItemToggle = (item) => {
    if (!selectedPickup) return;
    const current = [...selectedItems];
    const idx = current.findIndex(i => i.id === item.id);
    if (idx >= 0) {
      current.splice(idx, 1);
    } else {
      current.push(item);
    }
    setSelectedItems(current);
    // Reload valuations for the new selection — don't reset existing form data
    loadExistingValuation(current);
  };

  const selectAllItems = () => {
    if (!selectedPickup) return;
    setSelectedItems([...selectedPickup.items]);
    setExistingValuation(null);
    clearMessages();
  };

  const deselectAllItems = () => {
    setSelectedItems([]);
    setExistingValuation(null);
    clearMessages();
  };

  const loadExistingValuation = useCallback(async (itemIds) => {
    if (itemIds.length === 0) {
      setValuationMap({});
      return;
    }
    setError('');
    // Build a map of itemId -> valuation data from API
    const newMap = { ...valuationMap };
    await Promise.all(
      itemIds.map(async (item) => {
        try {
          const res = await valuationApi.getValuation(item.id);
          if (res.data?.valuation) {
            newMap[item.id] = {
              price: res.data.valuation.price,
              condition: res.data.valuation.condition,
            };
          } else {
            delete newMap[item.id];
          }
        } catch {
          delete newMap[item.id];
        }
      })
    );
    setValuationMap(newMap);
  }, [valuationMap]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!selectedPickup || selectedItems.length === 0) {
      setError('Please select at least one collected item.');
      return;
    }
    const priceNum = parseFloat(price);
    if (!price || isNaN(priceNum) || priceNum <= 0) {
      setError('Price is required.');
      return;
    }
    if (!condition) {
      setError('Please select a condition.');
      return;
    }

    setSubmitting(true);
    setError('');
    setMessage('');

    try {
      // Determine which items need create vs update
      const toUpdate = selectedItems.filter(item => valuationMap[item.id]);
      const toCreate = selectedItems.filter(item => !valuationMap[item.id]);

      // Update existing valuations
      for (const item of toUpdate) {
        await valuationApi.updateValuation(item.id, { price: priceNum, condition });
      }
      // Create new valuations
      for (const item of toCreate) {
        await valuationApi.createValuation(item.id, { price: priceNum, condition });
      }

      const total = selectedItems.length;
      const updated = toUpdate.length;
      const created = toCreate.length;
      let msg;
      if (created > 0 && updated > 0) {
        msg = `Valuation updated for ${updated} item(s) and created for ${created} new item(s). Total: ${total} item(s) at ${priceNum.toLocaleString('en-SL')} LKR (${condition}).`;
      } else if (updated > 0) {
        msg = `Valuation updated successfully. ${total} item(s) now valued at ${priceNum.toLocaleString('en-SL')} LKR (${condition}).`;
      } else {
        msg = `Valuation saved successfully. ${total} item(s) valued at ${priceNum.toLocaleString('en-SL')} LKR (${condition}).`;
      }
      setMessage(msg);

      // Refresh valuation map for the current selection
      loadExistingValuation(selectedItems);
    } catch (err) {
      const msg = err.response?.data?.message
        || err.response?.data?.title
        || (err.response?.data?.errors?.price?.[0])
        || (err.response?.data?.errors?.condition?.[0])
        || (err.response?.data?.errors?.dto?.[0])
        || `HTTP ${err.response?.status || 'unknown'}: ${err.message || 'Request failed'}`;
      setError(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const getStatusBadge = (item) => {
    // Status from pickup level, not item level
    if (!selectedPickup) return null;
    switch (selectedPickup.status) {
      case 'Collected':
        return <span className="badge badge-approved"><CheckCircle2 size={11} /> Collected</span>;
      case 'Scheduled':
        return <span className="badge badge-pending"><Loader2 size={11} /> Scheduled</span>;
      default:
        return <span className="badge badge-pending"><Loader2 size={11} /> {selectedPickup.status}</span>;
    }
  };

  const allItems = selectedPickup?.items || [];
  const filteredItems = allItems.filter(item => {
    const matchSearch = !searchTerm ||
      item.itemName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      item.id?.toLowerCase().includes(searchTerm.toLowerCase());
    // Filter by per-item valuation condition (Good/Fair/Poor)
    const itemValuation = valuationMap[item.id];
    const itemValuationCondition = itemValuation?.condition || null;
    const matchCondition = filterCondition === 'All' ||
      (filterCondition !== 'All' && itemValuationCondition === filterCondition);
    const hasValuation = !!valuationMap[item.id];
    const matchStatus = filterStatus === 'All' ||
      (filterStatus === 'Valued' && hasValuation) ||
      (filterStatus === 'Not Valued' && !hasValuation);
    return matchSearch && matchCondition && matchStatus;
  });

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '4rem' }}>
        <Loader size={32} style={{ color: 'var(--primary)', margin: '0 auto', marginBottom: '1rem' }} className="spin" />
        <p style={{ color: 'var(--text-secondary)' }}>Loading your collected items...</p>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '960px', margin: '1rem auto' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.5rem' }}>
        <Tag size={24} style={{ color: 'var(--accent)' }} />
        <h1 style={{ fontSize: '1.5rem', fontWeight: 800, margin: 0 }}>Item Valuations</h1>
        {user?.role && (
          <span className={`badge ${user.role === 'Recycler' ? 'badge-recycler' : 'badge-user'}`} style={{ marginLeft: '0.5rem' }}>
            {user.role}
          </span>
        )}
      </div>

      {/* Selected Pickup Bar */}
      {selectedPickup && (
        <div className="glass-card" style={{ marginBottom: '1.25rem', padding: '1rem 1.5rem' }}>
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '0.75rem' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <PackageSearch size={18} style={{ color: 'var(--primary)' }} />
                <strong style={{ fontSize: '0.95rem' }}>{selectedPickup.category}</strong>
              </div>
              {getStatusBadge(selectedPickup)}
              {selectedPickup.preferredDate && (
                <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                  {new Date(selectedPickup.preferredDate).toLocaleDateString('en-SL', {
                    day: 'numeric', month: 'short', year: 'numeric'
                  })}
                </span>
              )}
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <button
                className="btn btn-secondary"
                style={{ padding: '0.4rem 0.75rem', fontSize: '0.8rem' }}
                onClick={selectAllItems}
              >
                Select All
              </button>
              <button
                className="btn btn-secondary"
                style={{ padding: '0.4rem 0.75rem', fontSize: '0.8rem' }}
                onClick={deselectAllItems}
              >
                Clear
              </button>
              <button
                className="btn btn-logout"
                style={{ padding: '0.4rem 0.75rem', fontSize: '0.8rem' }}
                onClick={() => handlePickupSelect(selectedPickup)}
              >
                Close
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Error */}
      {error && (
        <div className="alert alert-danger">
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}

      {/* Success message */}
      {message && (
        <div className="alert alert-success">
          <CheckCircle2 size={16} />
          <span>{message}</span>
        </div>
      )}

      {/* Pickup List */}
      {pickups.length === 0 && !selectedPickup && (
        <div className="glass-card" style={{ textAlign: 'center', padding: '3rem 1.5rem' }}>
          <PackageSearch size={40} style={{ color: 'var(--text-secondary)', marginBottom: '1rem' }} />
          <p style={{ color: 'var(--text-secondary)' }}>No assigned pickups yet.</p>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', marginTop: '0.5rem' }}>
            Assigned pickups from Schedule Management will appear here to value.
          </p>
        </div>
      )}

      {!selectedPickup && pickups.length > 0 && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
          {pickups.map((p) => (
            <div
              key={p.id}
              className="glass-card"
              style={{
                padding: '1rem 1.5rem',
                cursor: 'pointer',
                border: selectedPickup?.id === p.id ? '1px solid var(--primary)' : '1px solid var(--border-color)',
                background: selectedPickup?.id === p.id ? 'rgba(16, 185, 129, 0.08)' : undefined,
                transition: 'all 0.2s ease',
              }}
              onClick={() => handlePickupSelect(p)}
              onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') handlePickupSelect(p); }}
              tabIndex={0}
              role="button"
            >
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '0.75rem' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                  <PackageSearch size={18} style={{ color: 'var(--primary)' }} />
                  <div>
                    <strong style={{ fontSize: '0.95rem' }}>{p.category}</strong>
                    <span style={{ color: 'var(--text-muted)', fontSize: '0.8rem', marginLeft: '0.5rem' }}>
                      {p.estimatedWeightKg ? `${p.estimatedWeightKg} kg` : ''}
                    </span>
                  </div>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
                  {getStatusBadge(p)}
                  {p.items?.length > 0 && (
                    <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                      {p.items.length} item{p.items.length !== 1 ? 's' : ''}
                    </span>
                  )}
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                    Click to select
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Item Selection + Form */}
      {selectedPickup && (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 380px', gap: '1.5rem', alignItems: 'start' }}>
          {/* Left: Items */}
          <div className="glass-card">
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1rem', flexWrap: 'wrap', gap: '0.5rem' }}>
              <h2 style={{ fontSize: '1rem', fontWeight: 700, margin: 0 }}>
                Collected Items
                {selectedItems.length > 0 && (
                  <span style={{ color: 'var(--primary)', fontSize: '0.8rem', marginLeft: '0.5rem' }}>
                    ({selectedItems.length} selected)
                  </span>
                )}
              </h2>
              {allItems.length > 1 && (
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                  <button
                    className="btn btn-secondary"
                    style={{ padding: '0.35rem 0.65rem', fontSize: '0.75rem' }}
                    onClick={selectAllItems}
                  >
                    All
                  </button>
                  <button
                    className="btn btn-secondary"
                    style={{ padding: '0.35rem 0.65rem', fontSize: '0.75rem' }}
                    onClick={deselectAllItems}
                  >
                    None
                  </button>
                </div>
              )}
            </div>

            {/* Search & Filter */}
            <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1rem', flexWrap: 'wrap' }}>
              <input
                type="text"
                className="form-input"
                placeholder="Search items..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                style={{ flex: '1', minWidth: '140px' }}
              />
              <select
                className="form-select"
                value={filterCondition}
                onChange={(e) => setFilterCondition(e.target.value)}
                style={{ width: 'auto', minWidth: '110px' }}
              >
                <option value="All">All Conditions</option>
                {CONDITIONS.map(c => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>
              <select
                className="form-select"
                value={filterStatus}
                onChange={(e) => setFilterStatus(e.target.value)}
                style={{ width: 'auto', minWidth: '120px' }}
              >
                <option value="All">All Status</option>
                <option value="Valued">Valued</option>
                <option value="Not Valued">Not Valued</option>
              </select>
            </div>

            {allItems.length === 0 && (
              <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', textAlign: 'center', padding: '1rem' }}>
                No items found in this pickup request.
              </p>
            )}

            {allItems.length > 0 && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem', maxHeight: '360px', overflowY: 'auto' }}>
                {filteredItems.length === 0 && (
                  <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', textAlign: 'center', padding: '1rem' }}>
                    {searchTerm !== '' ? 'No items match the search.' : 'No items match the current filters.'}
                  </p>
                )}
                {filteredItems.map((item) => {
                  const isValued = valuationMap[item.id];
                  return (
                    <div
                      key={item.id}
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: '0.75rem',
                        padding: '0.65rem 0.75rem',
                        background: isValued ? 'rgba(16, 185, 129, 0.08)' : 'rgba(255, 255, 255, 0.02)',
                        borderRadius: 'var(--radius-sm)',
                        border: isValued ? '1px solid rgba(16, 185, 129, 0.25)' : '1px solid var(--border-color)',
                        transition: 'background 0.2s ease',
                      }}
                    >
                      <input
                        type="checkbox"
                        checked={selectedItems.some(i => i.id === item.id)}
                        onChange={() => handleItemToggle(item)}
                        onClick={(e) => e.stopPropagation()}
                        style={{ cursor: 'pointer', accentColor: 'var(--primary)' }}
                      />
                      <div style={{ flex: 1, minWidth: 0 }}>
                        <div style={{ fontWeight: 600, fontSize: '0.9rem', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                          {item.itemName}
                        </div>
                        <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                          ID: {item.id}
                        </div>
                      </div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                        <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', textTransform: 'capitalize' }}>
                          {valuationMap[item.id]?.condition || item.itemCondition}
                        </span>
                        {valuationMap[item.id] && (
                          <CheckCircle2 size={14} style={{ color: 'var(--success)' }} />
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Right: Valuation Form */}
          <div className="glass-card">
            <h2 style={{ fontSize: '1rem', fontWeight: 700, marginBottom: '1.25rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <Edit3 size={16} style={{ color: 'var(--primary)' }} />
              Valuation
            </h2>

            {selectedItems.length === 0 ? (
              <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>
                Select items to value.
              </p>
            ) : (
              <form onSubmit={handleSubmit}>
                {/* Per-item valuation info */}
                {selectedItems.length > 0 && Object.keys(valuationMap).length > 0 && (
                  <div style={{
                    padding: '0.75rem 1rem',
                    background: 'rgba(16, 185, 129, 0.1)',
                    border: '1px solid rgba(16, 185, 129, 0.2)',
                    borderRadius: 'var(--radius-sm)',
                    marginBottom: '1rem',
                    fontSize: '0.85rem',
                    color: '#34d399',
                  }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.4rem' }}>
                      <CheckCircle2 size={14} />
                      <strong style={{ fontWeight: 600 }}>Existing Valuation{" "}
                        {Object.keys(valuationMap).length > 1 && (
                          <span style={{ fontWeight: 400, color: 'var(--text-muted)' }}>
                            ({Object.keys(valuationMap).length} items)
                          </span>
                        )}
                      </strong>
                    </div>
                    <div style={{ color: 'var(--text-muted)', fontSize: '0.78rem', marginBottom: '0.3rem' }}>
                      {Object.keys(valuationMap).length} of {selectedItems.length} selected items already valued
                    </div>
                  </div>
                )}

                {/* Price */}
                <div className="form-group">
                  <label className="form-label">Price (LKR)</label>
                  <input
                    type="number"
                    className="form-input"
                    placeholder="e.g. 5000"
                    value={price}
                    onChange={(e) => {
                      setPrice(e.target.value);
                      setError('');
                    }}
                    min="0"
                    step="0.01"
                    disabled={submitting}
                    autoFocus
                  />
                  {error === 'Price is required.' && (
                    <div className="form-error">
                      <AlertCircle size={13} />
                      <span>Price is required.</span>
                    </div>
                  )}
                </div>

                {/* Condition */}
                <div className="form-group">
                  <label className="form-label">Condition</label>
                  <select
                    className="form-select"
                    value={condition}
                    onChange={(e) => {
                      setCondition(e.target.value);
                      setError('');
                    }}
                    disabled={submitting}
                  >
                    <option value="">— Select —</option>
                    {CONDITIONS.map(c => (
                      <option key={c} value={c}>{c}</option>
                    ))}
                  </select>
                </div>

                {/* Summary */}
                {selectedItems.length > 0 && price && condition && (
                  <div style={{
                    padding: '0.65rem 1rem',
                    background: 'rgba(16, 185, 129, 0.08)',
                    border: '1px solid rgba(16, 185, 129, 0.2)',
                    borderRadius: 'var(--radius-sm)',
                    marginBottom: '1rem',
                    fontSize: '0.8rem',
                    color: 'var(--text-secondary)',
                  }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.3rem' }}>
                      <PackageSearch size={12} />
                      <span style={{ fontWeight: 600 }}>Summary</span>
                    </div>
                    <div>
                      {selectedItems.length} item{selectedItems.length !== 1 ? 's' : ''} at{' '}
                      <strong style={{ color: 'var(--text-primary)' }}>
                        {parseFloat(price).toLocaleString('en-SL')} LKR
                      </strong>{' '}
                      ({condition})
                    </div>
                  </div>
                )}

                {/* Submit */}
                <button
                  type="submit"
                  className="btn btn-primary"
                  style={{ width: '100%', padding: '0.8rem' }}
                  disabled={submitting || !price || !condition}
                >
                  {submitting ? (
                    <>
                      <Loader size={18} className="spin" />
                      Processing...
                    </>
                  ) : existingValuation ? (
                    <>
                      <Edit3 size={18} />
                      Update Valuation
                    </>
                  ) : (
                    <>
                      <CheckCircle2 size={18} />
                      Save Valuation
                    </>
                  )}
                </button>
              </form>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

export default ValuationView;
