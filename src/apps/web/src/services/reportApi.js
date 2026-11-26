const REPORTS_BASE = '/api/reports';

class ReportApiService {
  async _request(path, options = {}) {
    const headers = { 'Content-Type': 'application/json', ...(options.headers || {}) };
    const res = await fetch(`${REPORTS_BASE}${path}`, {
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

  _filterParams({ category = '', from = '', to = '' } = {}) {
    const params = new URLSearchParams();
    if (category) params.set('category', category);
    if (from) params.set('from', from);
    if (to) params.set('to', to);
    return params;
  }

  // Admin: all listings, Recycler: own listings. Dates are yyyy-MM-dd (inclusive).
  async getSalesReport(filters, token) {
    const params = this._filterParams(filters);
    const query = params.toString();
    return this._request(`/sales${query ? `?${query}` : ''}`, {
      method: 'GET',
      auth: true,
      token,
    });
  }

  async getCategories(token) {
    return this._request('/sales/categories', {
      method: 'GET',
      auth: true,
      token,
    });
  }

  // format: 'csv' or 'pdf'. Downloads the file in the browser.
  async exportSalesReport(format, filters, token) {
    const params = this._filterParams(filters);
    params.set('format', format);
    const res = await fetch(`${REPORTS_BASE}/sales/export?${params.toString()}`, {
      method: 'GET',
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      throw { status: res.status, message: data?.message || `HTTP ${res.status}`, data };
    }

    const disposition = res.headers.get('Content-Disposition') || '';
    const match = disposition.match(/filename="?([^";]+)"?/i);
    const fileName = match ? match[1] : `sales-report.${format}`;

    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
    return fileName;
  }
}

export const reportApi = new ReportApiService();
