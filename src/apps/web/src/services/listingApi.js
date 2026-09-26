const LISTINGS_BASE = '/api/listings';

class ListingApiService {
  async _request(path, options = {}) {
    const headers = {
      'Content-Type': 'application/json',
      ...(options.headers || {}),
    };
    const res = await fetch(`${LISTINGS_BASE}${path}`, {
      ...options,
      headers: {
        ...(options?.auth ? { Authorization: `Bearer ${options.token}` } : {}),
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

  async browse(keyword = '', page = 1, pageSize = 12, token = null) {
    const params = new URLSearchParams();
    if (keyword.trim()) params.set('keyword', keyword.trim());
    params.set('page', page);
    params.set('pageSize', pageSize);
    return this._request(`?${params.toString()}`, {
      method: 'GET',
      auth: !!token,
      token,
    });
  }

  async getById(id, token = null) {
    return this._request(`/${id}`, { method: 'GET', auth: !!token, token });
  }

  async create(payload, token = null) {
    return this._request('', {
      method: 'POST',
      body: JSON.stringify(payload),
      auth: !!token,
      token,
    });
  }

  async update(id, payload, token = null) {
    return this._request(`/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
      auth: !!token,
      token,
    });
  }

  async delete(id, token = null) {
    return this._request(`/${id}`, {
      method: 'DELETE',
      auth: !!token,
      token,
    });
  }
}

export const ListingService = new ListingApiService();
