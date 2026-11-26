import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { reportApi } from '../services/reportApi';
import { TrendingUp, ShoppingCart, Wallet, Package, Clock, AlertCircle, Download, Filter, RotateCcw } from 'lucide-react';

const EMPTY_FILTERS = { category: '', from: '', to: '' };

/// Sales, Inventory & Revenue Report (Story 21, E6.3)
/// Admin sees every listing, Recycler sees only their own listings.
/// Filter by category and date range, then export as CSV or PDF.
function SalesReportView() {
  const { user, token } = useAuth();
  const authToken = token || localStorage.getItem('ecotrack_token');

  const [loading, setLoading] = useState(true);
  const [report, setReport] = useState(null);
  const [categories, setCategories] = useState([]);
  const [filters, setFilters] = useState(EMPTY_FILTERS);
  const [applied, setApplied] = useState(EMPTY_FILTERS);
  const [error, setError] = useState('');
  const [exporting, setExporting] = useState('');

  useEffect(() => {
    loadCategories();
  }, []);

  useEffect(() => {
    loadReport();
  }, [applied]);

  const loadCategories = async () => {
    try {
      const data = await reportApi.getCategories(authToken);
      setCategories(Array.isArray(data.categories) ? data.categories : []);
    } catch {
      setCategories([]);
    }
  };

  const loadReport = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await reportApi.getSalesReport(applied, authToken);
      setReport(data.report || null);
    } catch (err) {
      setError(err.message || 'Could not load the sales report. Please try again.');
      setReport(null);
    } finally {
      setLoading(false);
    }
  };

  const applyFilters = (e) => {
    e.preventDefault();
    if (filters.from && filters.to && filters.from > filters.to) {
      setError("The 'From' date must be on or before the 'To' date.");
      return;
    }
    setApplied({ ...filters });
  };

  const resetFilters = () => {
    setFilters(EMPTY_FILTERS);
    setApplied(EMPTY_FILTERS);
  };

  const handleExport = async (format) => {
    setExporting(format);
    setError('');
    try {
      await reportApi.exportSalesReport(format, applied, authToken);
    } catch (err) {
      setError(err.message || 'Could not export the report. Please try again.');
    } finally {
      setExporting('');
    }
  };

  const formatPrice = (price) => {
    if (price === null || price === undefined) return '—';
    return new Intl.NumberFormat('en-SL', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(price) + ' LKR';
  };

  const totals = report?.totals;
  const rows = report?.categories || [];
  const isFiltered = applied.category || applied.from || applied.to;

  const tiles = [
    { label: 'Total Sales', value: totals ? totals.totalSales : '—', sub: totals ? formatPrice(totals.salesValue) + ' sales value' : '', icon: ShoppingCart, color: '#10b981' },
    { label: 'Revenue (Paid)', value: totals ? formatPrice(totals.revenue) : '—', sub: totals ? formatPrice(totals.refundedAmount) + ' refunded' : '', icon: Wallet, color: '#3b82f6' },
    { label: 'Pending Payments', value: totals ? formatPrice(totals.pendingAmount) : '—', sub: 'Not yet marked as Paid', icon: Clock, color: '#f59e0b' },
    { label: 'Inventory In Stock', value: totals ? totals.inStock : '—', sub: totals ? `${totals.reserved} reserved · ${totals.sold} sold` : '', icon: Package, color: '#a855f7' },
  ];

  return (
    <div style={{ maxWidth: '1100px', margin: '0 auto', padding: '1.5rem 1rem' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '1rem', flexWrap: 'wrap', marginBottom: '0.5rem' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
          <TrendingUp size={26} style={{ color: 'var(--accent)' }} />
          <h1 style={{ fontSize: '1.5rem', fontWeight: 800, margin: 0 }}>Sales, Inventory &amp; Revenue Report</h1>
        </div>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button
            className="btn btn-secondary"
            style={{ padding: '0.45rem 0.9rem', fontSize: '0.8rem', display: 'flex', alignItems: 'center', gap: '0.35rem' }}
            disabled={!!exporting || loading}
            onClick={() => handleExport('csv')}
          >
            <Download size={14} />
            {exporting === 'csv' ? 'Exporting...' : 'Export CSV'}
          </button>
          <button
            className="btn btn-primary"
            style={{ padding: '0.45rem 0.9rem', fontSize: '0.8rem', display: 'flex', alignItems: 'center', gap: '0.35rem' }}
            disabled={!!exporting || loading}
            onClick={() => handleExport('pdf')}
          >
            <Download size={14} />
            {exporting === 'pdf' ? 'Exporting...' : 'Export PDF'}
          </button>
        </div>
      </div>
      <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: 0, marginBottom: '1.25rem' }}>
        {user?.role === 'Admin'
          ? 'Marketplace performance across all recyclers.'
          : 'Performance of your own listings.'}
        {' '}Sales and revenue use the order date. Inventory shows the current listing count.
      </p>

      {/* Filters */}
      <form className="glass-card" onSubmit={applyFilters} style={{ padding: '1rem', marginBottom: '1.25rem', display: 'flex', gap: '1rem', alignItems: 'flex-end', flexWrap: 'wrap' }}>
        <div style={{ flex: '1 1 180px' }}>
          <label className="form-label" htmlFor="report-category">Category</label>
          <select
            id="report-category"
            className="form-select"
            value={filters.category}
            onChange={(e) => setFilters(f => ({ ...f, category: e.target.value }))}
          >
            <option value="">All categories</option>
            {categories.map(c => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </div>
        <div style={{ flex: '1 1 150px' }}>
          <label className="form-label" htmlFor="report-from">From</label>
          <input
            id="report-from"
            type="date"
            className="form-input"
            value={filters.from}
            max={filters.to || undefined}
            onChange={(e) => setFilters(f => ({ ...f, from: e.target.value }))}
          />
        </div>
        <div style={{ flex: '1 1 150px' }}>
          <label className="form-label" htmlFor="report-to">To</label>
          <input
            id="report-to"
            type="date"
            className="form-input"
            value={filters.to}
            min={filters.from || undefined}
            onChange={(e) => setFilters(f => ({ ...f, to: e.target.value }))}
          />
        </div>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button type="submit" className="btn btn-primary" style={{ padding: '0.7rem 1rem', display: 'flex', alignItems: 'center', gap: '0.35rem' }}>
            <Filter size={14} />
            Apply
          </button>
          <button type="button" className="btn btn-secondary" style={{ padding: '0.7rem 1rem', display: 'flex', alignItems: 'center', gap: '0.35rem' }} onClick={resetFilters}>
            <RotateCcw size={14} />
            Reset
          </button>
        </div>
      </form>

      {error && (
        <div className="alert alert-danger" style={{ marginBottom: '1rem' }}>
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}

      {/* Summary tiles */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '0.75rem', marginBottom: '1.25rem' }}>
        {tiles.map(tile => (
          <div key={tile.label} className="glass-card" style={{ padding: '1rem', borderLeft: '3px solid ' + tile.color }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', fontSize: '0.8rem', color: 'var(--text-secondary)', marginBottom: '0.4rem' }}>
              <tile.icon size={14} style={{ color: tile.color }} />
              {tile.label}
            </div>
            <div style={{ fontSize: '1.35rem', fontWeight: 800 }}>{loading ? '…' : tile.value}</div>
            <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '0.25rem' }}>{loading ? '' : tile.sub}</div>
          </div>
        ))}
      </div>

      {/* By category table */}
      {loading && !report ? (
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: '20vh', gap: '0.75rem' }}>
          <TrendingUp size={36} style={{ color: 'var(--primary)', opacity: 0.8 }} />
          <span style={{ color: 'var(--text-secondary)', fontSize: '0.95rem' }}>Loading sales report...</span>
        </div>
      ) : rows.length === 0 ? (
        <div className="glass-card" style={{ textAlign: 'center', padding: '3rem 2rem' }}>
          <TrendingUp size={48} style={{ color: 'var(--text-secondary)', marginBottom: '1rem' }} />
          <p style={{ color: 'var(--text-secondary)', fontSize: '1rem', margin: 0 }}>
            {isFiltered ? 'No report data for the selected filters.' : 'No listings yet, so there is nothing to report.'}
          </p>
        </div>
      ) : (
        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Category</th>
                <th style={{ textAlign: 'right' }}>Sales</th>
                <th style={{ textAlign: 'right' }}>Sales Value</th>
                <th style={{ textAlign: 'right' }}>Revenue</th>
                <th style={{ textAlign: 'right' }}>Pending</th>
                <th style={{ textAlign: 'right' }}>Refunded</th>
                <th style={{ textAlign: 'right' }}>In Stock</th>
                <th style={{ textAlign: 'right' }}>Reserved</th>
                <th style={{ textAlign: 'right' }}>Sold</th>
              </tr>
            </thead>
            <tbody>
              {rows.map(row => (
                <tr key={row.category}>
                  <td style={{ fontWeight: 600 }}>{row.category}</td>
                  <td style={{ textAlign: 'right' }}>{row.totalSales}</td>
                  <td style={{ textAlign: 'right' }}>{formatPrice(row.salesValue)}</td>
                  <td style={{ textAlign: 'right', color: 'var(--accent)', fontWeight: 600 }}>{formatPrice(row.revenue)}</td>
                  <td style={{ textAlign: 'right' }}>{formatPrice(row.pendingAmount)}</td>
                  <td style={{ textAlign: 'right' }}>{formatPrice(row.refundedAmount)}</td>
                  <td style={{ textAlign: 'right' }}>{row.inStock}</td>
                  <td style={{ textAlign: 'right' }}>{row.reserved}</td>
                  <td style={{ textAlign: 'right' }}>{row.sold}</td>
                </tr>
              ))}
              {totals && rows.length > 1 && (
                <tr>
                  <td style={{ fontWeight: 800 }}>Total</td>
                  <td style={{ textAlign: 'right', fontWeight: 700 }}>{totals.totalSales}</td>
                  <td style={{ textAlign: 'right', fontWeight: 700 }}>{formatPrice(totals.salesValue)}</td>
                  <td style={{ textAlign: 'right', fontWeight: 700, color: 'var(--accent)' }}>{formatPrice(totals.revenue)}</td>
                  <td style={{ textAlign: 'right', fontWeight: 700 }}>{formatPrice(totals.pendingAmount)}</td>
                  <td style={{ textAlign: 'right', fontWeight: 700 }}>{formatPrice(totals.refundedAmount)}</td>
                  <td style={{ textAlign: 'right', fontWeight: 700 }}>{totals.inStock}</td>
                  <td style={{ textAlign: 'right', fontWeight: 700 }}>{totals.reserved}</td>
                  <td style={{ textAlign: 'right', fontWeight: 700 }}>{totals.sold}</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

export default SalesReportView;
