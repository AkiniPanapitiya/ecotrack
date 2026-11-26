import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { paymentApi } from '../services/paymentApi';
import { PaymentStatusBadge } from '../components/PaymentStatusBadge';
import { Wallet, Clock, AlertCircle, CheckCircle2, RotateCcw, ChevronLeft, ChevronRight } from 'lucide-react';

const PAGE_SIZE = 10;
const FILTERS = ['', 'Pending', 'Paid', 'Refunded'];

/// Escrow & Payment Record (Story 20, E6.2)
/// Recycler sees payments for their own sales, Admin sees all.
/// Record only: no real payment gateway is used.
function PaymentRecordsView() {
  const { user, token } = useAuth();
  const authToken = token || localStorage.getItem('ecotrack_token');

  const [loading, setLoading] = useState(true);
  const [payments, setPayments] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [busyId, setBusyId] = useState(null);

  useEffect(() => {
    loadPayments();
  }, [page, statusFilter]);

  const loadPayments = async () => {
    setLoading(true);
    setError('');
    try {
      const data = await paymentApi.getPayments(statusFilter, page, PAGE_SIZE, authToken);
      setPayments(Array.isArray(data.payments) ? data.payments : []);
      setTotalCount(data.totalCount ?? 0);
    } catch (err) {
      setError(err.message || 'Could not load payment records. Please try again.');
      setPayments([]);
    } finally {
      setLoading(false);
    }
  };

  const handleUpdate = async (payment, newStatus) => {
    const confirmText = newStatus === 'Paid'
      ? `Mark the payment for "${payment.listingTitle || 'this order'}" as Paid?`
      : `Refund the payment for "${payment.listingTitle || 'this order'}"? The order will be cancelled.`;
    if (!window.confirm(confirmText)) return;

    setBusyId(payment.id);
    setError('');
    setSuccess('');
    try {
      const data = await paymentApi.updateStatus(payment.id, newStatus, authToken);
      setSuccess(data.message || 'Payment updated.');
      if (data.payment) {
        setPayments(prev => prev.map(p => (p.id === payment.id ? data.payment : p)));
      } else {
        await loadPayments();
      }
    } catch (err) {
      setError(err.message || 'Could not update the payment. Please try again.');
    } finally {
      setBusyId(null);
    }
  };

  const changeFilter = (value) => {
    setStatusFilter(value);
    setPage(1);
    setSuccess('');
  };

  const totalPages = Math.ceil(totalCount / PAGE_SIZE);
  const hasNext = page < totalPages;
  const hasPrev = page > 1;

  const formatPrice = (price) => {
    if (price === null || price === undefined) return '—';
    return new Intl.NumberFormat('en-SL').format(price) + ' LKR';
  };

  const formatDate = (dateStr) => {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-SL', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  };

  const borderColor = (status) => (
    status === 'Paid' ? '#10b981' :
    status === 'Refunded' ? '#3b82f6' : '#f59e0b'
  );

  return (
    <div style={{ maxWidth: '900px', margin: '0 auto', padding: '1.5rem 1rem' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '0.5rem' }}>
        <Wallet size={26} style={{ color: 'var(--accent)' }} />
        <h1 style={{ fontSize: '1.5rem', fontWeight: 800, margin: 0 }}>Payment Records</h1>
        <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
          {totalCount} record{totalCount !== 1 ? 's' : ''}
        </span>
      </div>
      <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: 0, marginBottom: '1.25rem' }}>
        {user?.role === 'Admin'
          ? 'Escrow record for every marketplace order.'
          : 'Escrow record for orders on your listings.'}
        {' '}This is a record only. No real payment is taken or returned.
      </p>

      {/* Status filter */}
      <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap', marginBottom: '1rem' }}>
        {FILTERS.map(f => (
          <button
            key={f || 'all'}
            className={`btn ${statusFilter === f ? 'btn-primary' : 'btn-secondary'}`}
            style={{ padding: '0.4rem 0.9rem', fontSize: '0.8rem' }}
            onClick={() => changeFilter(f)}
          >
            {f || 'All'}
          </button>
        ))}
      </div>

      {error && (
        <div className="alert alert-danger" style={{ marginBottom: '1rem' }}>
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}
      {success && (
        <div className="alert alert-success" style={{ marginBottom: '1rem' }}>
          <CheckCircle2 size={16} />
          <span>{success}</span>
        </div>
      )}

      {loading && payments.length === 0 ? (
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: '30vh', gap: '0.75rem' }}>
          <Wallet size={36} style={{ color: 'var(--primary)', opacity: 0.8 }} />
          <span style={{ color: 'var(--text-secondary)', fontSize: '0.95rem' }}>Loading payment records...</span>
        </div>
      ) : payments.length === 0 ? (
        <div className="glass-card" style={{ textAlign: 'center', padding: '4rem 2rem' }}>
          <Wallet size={48} style={{ color: 'var(--text-secondary)', marginBottom: '1rem' }} />
          <p style={{ color: 'var(--text-secondary)', fontSize: '1rem', margin: 0 }}>
            No payment records{statusFilter ? ` with status ${statusFilter}` : ''}.
          </p>
        </div>
      ) : (
        <>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
            {payments.map((payment) => (
              <div
                key={payment.id}
                className="glass-card"
                style={{ padding: '1rem', borderLeft: '3px solid ' + borderColor(payment.status) }}
              >
                <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: '1rem', flexWrap: 'wrap' }}>
                  <div style={{ flex: 1, minWidth: '200px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.6rem', marginBottom: '0.5rem', flexWrap: 'wrap' }}>
                      <h3 style={{ fontSize: '0.95rem', fontWeight: 700, margin: 0 }}>
                        {payment.listingTitle || 'Unknown Item'}
                      </h3>
                      <PaymentStatusBadge status={payment.status} />
                      {payment.orderStatus === 'Cancelled' && (
                        <span style={{ fontSize: '0.7rem', color: '#fca5a5', fontWeight: 600 }}>ORDER CANCELLED</span>
                      )}
                    </div>
                    <div style={{ fontWeight: 700, color: 'var(--accent)', fontSize: '0.95rem', marginBottom: '0.5rem' }}>
                      {formatPrice(payment.amount)}
                    </div>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
                      <span style={{ display: 'flex', alignItems: 'center', gap: '0.3rem' }}>
                        <Clock size={12} /> Ordered {formatDate(payment.createdAt)}
                      </span>
                      {payment.paidAt && <span>Paid {formatDate(payment.paidAt)}</span>}
                      {payment.refundedAt && <span>Refunded {formatDate(payment.refundedAt)}</span>}
                      <span>Order #{String(payment.orderId).slice(0, 8)}</span>
                    </div>
                  </div>

                  <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
                    {payment.status === 'Pending' && (
                      <button
                        className="btn btn-primary"
                        style={{ padding: '0.45rem 0.9rem', fontSize: '0.8rem', display: 'flex', alignItems: 'center', gap: '0.35rem' }}
                        disabled={busyId === payment.id}
                        onClick={() => handleUpdate(payment, 'Paid')}
                      >
                        <CheckCircle2 size={14} />
                        {busyId === payment.id ? 'Saving...' : 'Mark as Paid'}
                      </button>
                    )}
                    {payment.status === 'Paid' && (
                      <button
                        className="btn btn-secondary"
                        style={{ padding: '0.45rem 0.9rem', fontSize: '0.8rem', display: 'flex', alignItems: 'center', gap: '0.35rem' }}
                        disabled={busyId === payment.id}
                        onClick={() => handleUpdate(payment, 'Refunded')}
                      >
                        <RotateCcw size={14} />
                        {busyId === payment.id ? 'Saving...' : 'Refund'}
                      </button>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '0.5rem', marginTop: '2rem' }}>
              <button
                className="btn btn-secondary"
                style={{ padding: '0.45rem 0.75rem', display: 'flex', alignItems: 'center', gap: '0.3rem' }}
                onClick={() => setPage(p => p - 1)}
                disabled={!hasPrev}
              >
                <ChevronLeft size={16} />
                Previous
              </button>
              <span style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
                Page {page} of {totalPages}
              </span>
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

export default PaymentRecordsView;
