const PAYMENTS_BASE = '/api/payments';

class PaymentApiService {
  async _request(path, options = {}) {
    const headers = { 'Content-Type': 'application/json', ...(options.headers || {}) };
    const res = await fetch(`${PAYMENTS_BASE}${path}`, {
      ...options,
      headers: {
        ...(options.auth ? { Authorization: `Bearer ${options.token}` } : {}),
        ...headers,
      },
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      const msg = data?.message || data?.title || `HTTP ${res.status}`;
      throw { status: res.status, message: msg, data };
    }
    return data;
  }

  // Admin: all records, Recycler: own sales, User: own purchases
  async getPayments(status = '', page = 1, pageSize = 10, token) {
    const params = new URLSearchParams();
    if (status) params.set('status', status);
    params.set('page', page);
    params.set('pageSize', pageSize);
    return this._request(`?${params.toString()}`, {
      method: 'GET',
      auth: true,
      token,
    });
  }

  async getPaymentForOrder(orderId, token) {
    return this._request(`/order/${orderId}`, {
      method: 'GET',
      auth: true,
      token,
    });
  }

  // status: 'Paid' or 'Refunded' (Recycler who sold the item, or Admin)
  async updateStatus(paymentId, status, token) {
    return this._request(`/${paymentId}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
      auth: true,
      token,
    });
  }

  async markAsPaid(paymentId, token) {
    return this.updateStatus(paymentId, 'Paid', token);
  }

  async refund(paymentId, token) {
    return this.updateStatus(paymentId, 'Refunded', token);
  }
}

export const paymentApi = new PaymentApiService();
