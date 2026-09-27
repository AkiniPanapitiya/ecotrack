const ORDERS_BASE = '/api/orders';

class OrderApiService {
  async _request(path, options = {}) {
    const headers = { 'Content-Type': 'application/json', ...(options.headers || {}) };
    const res = await fetch(`${ORDERS_BASE}${path}`, {
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

  async placeOrder(listingId, token) {
    return this._request('', {
      method: 'POST',
      body: JSON.stringify({ listingId }),
      auth: true,
      token,
    });
  }

  async getMyOrders(page = 1, pageSize = 10, token) {
    const params = new URLSearchParams();
    params.set('page', page);
    params.set('pageSize', pageSize);
    return this._request(`?${params.toString()}`, {
      method: 'GET',
      auth: true,
      token,
    });
  }

  async getOrderById(orderId, token) {
    return this._request(`/${orderId}`, {
      method: 'GET',
      auth: true,
      token,
    });
  }
}

export const orderApi = new OrderApiService();
