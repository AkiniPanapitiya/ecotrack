import React, { useState, useEffect } from 'react';
import { useAuth } from '../context/AuthContext';
import { Package, Clock, CheckCircle2, AlertCircle, X as XIcon, ChevronLeft, ChevronRight } from 'lucide-react';

const PAGE_SIZE = 10;

function MyOrdersView() {
  const { user, token } = useAuth();
  const authToken = token || localStorage.getItem('ecotrack_token');

  const [loading, setLoading] = useState(true);
  const [orders, setOrders] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(PAGE_SIZE);
  const [error, setError] = useState('');

  useEffect(() => {
    loadOrders();
  }, [page]);

  const loadOrders = async () => {
    setLoading(true);
    setError('');
    try {
      const res = await fetch(`/api/orders?page=${page}&pageSize=${pageSize}`, {
        headers: authToken ? { Authorization: `Bearer ${authToken}` } : {},
      });
      if (!res.ok) {
        const errorData = await res.json().catch(() => ({}));
        throw new Error(errorData.message || 'Failed to load orders');
      }
      const data = await res.json();
      const orderList = Array.isArray(data.orders) ? data.orders : (data.orders?.orders || []);
      const count = data.totalCount ?? data.orders?.totalCount ?? orderList.length;
      setOrders(orderList);
      setTotalCount(count);
    } catch (err) {
      setError(err.message || 'Could not load orders. Please try again.');
      setOrders([]);
    } finally {
      setLoading(false);
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);
  const hasNext = page < totalPages;
  const hasPrev = page > 1;

  const formatPrice = (price) => {
    if (!price) return '—';
    return new Intl.NumberFormat('en-SL').format(price) + ' LKR';
  };

  const formatDate = (dateStr) => {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-SL', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  };

  const getStatusBadge = (status) => {
    switch (status) {
      case 'Placed':
        return (
          <span style={{
            display: 'inline-flex', alignItems: 'center', gap: '0.35rem',
            padding: '0.2rem 0.65rem', fontSize: '0.7rem', fontWeight: 600,
            borderRadius: '9999px', textTransform: 'uppercase', letterSpacing: '0.03em',
            background: '#78350f', color: '#fde68a',
          }}>
            <Clock size={10} /> Placed
          </span>
        );
      case 'Completed':
        return (
          <span style={{
            display: 'inline-flex', alignItems: 'center', gap: '0.35rem',
            padding: '0.2rem 0.65rem', fontSize: '0.7rem', fontWeight: 600,
            borderRadius: '9999px', textTransform: 'uppercase', letterSpacing: '0.03em',
            background: '#065f46', color: '#a7f3d0',
          }}>
            <CheckCircle2 size={10} /> Completed
          </span>
        );
      case 'Cancelled':
        return (
          <span style={{
            display: 'inline-flex', alignItems: 'center', gap: '0.35rem',
            padding: '0.2rem 0.65rem', fontSize: '0.7rem', fontWeight: 600,
            borderRadius: '9999px', textTransform: 'uppercase', letterSpacing: '0.03em',
            background: '#991b1b', color: '#fca5a5',
          }}>
            <XIcon size={10} /> Cancelled
          </span>
        );
      default:
        return null;
    }
  };

  if (loading && orders.length === 0) {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: '50vh', gap: '0.75rem' }}>
        <Package size={36} style={{ color: 'var(--primary)', opacity: 0.8 }} />
        <span style={{ color: 'var(--text-secondary)', fontSize: '0.95rem' }}>Loading orders...</span>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '900px', margin: '0 auto', padding: '1.5rem 1rem' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.5rem' }}>
        <Package size={26} style={{ color: 'var(--accent)' }} />
        <h1 style={{ fontSize: '1.5rem', fontWeight: 800, margin: 0 }}>My Orders</h1>
        <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
          {totalCount} order{totalCount !== 1 ? 's' : ''}
        </span>
      </div>

      {error && (
        <div className="alert alert-danger" style={{ marginBottom: '1rem' }}>
          <AlertCircle size={16} />
          <span>{error}</span>
        </div>
      )}

      {orders.length === 0 && !loading ? (
        <div className="glass-card" style={{ textAlign: 'center', padding: '4rem 2rem' }}>
          <Package size={48} style={{ color: 'var(--text-secondary)', marginBottom: '1rem' }} />
          <p style={{ color: 'var(--text-secondary)', fontSize: '1rem', margin: 0 }}>
            No orders yet.
          </p>
          <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', marginTop: '0.5rem' }}>
            Browse the marketplace to find items to purchase.
          </p>
        </div>
      ) : (
        <>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
            {orders.map((order) => (
              <div
                key={order.id}
                className="glass-card"
                style={{
                  padding: '1rem',
                  borderLeft: '3px solid ' + (
                    order.status === 'Completed' ? '#10b981' :
                    order.status === 'Cancelled' ? '#ef4444' : '#f59e0b'
                  ),
                }}
              >
                <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: '1rem' }}>
                  {/* Left: item info */}
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
                      <h3 style={{
                        fontSize: '0.95rem', fontWeight: 700, margin: 0,
                        whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis',
                      }}>
                        {order.listing?.title || 'Unknown Item'}
                      </h3>
                      {getStatusBadge(order.status)}
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', marginBottom: '0.5rem' }}>
                      <span style={{ fontWeight: 700, color: 'var(--accent)', fontSize: '0.95rem' }}>
                        {formatPrice(order.priceAtPurchase)}
                      </span>
                    </div>
                    <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'flex', alignItems: 'center', gap: '0.3rem' }}>
                      <Clock size={12} />
                      Ordered {formatDate(order.createdAt)}
                    </div>
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
                  style={{ padding: '0.45rem 0.75rem', minWidth: '36px', fontSize: '0.85rem' }}
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

export default MyOrdersView;
