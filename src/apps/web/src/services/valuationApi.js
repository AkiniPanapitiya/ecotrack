import axios from 'axios';

// ECO-17: Marketplace Valuation Endpoints
const MARKETPLACE_API_URL = import.meta.env.VITE_MARKETPLACE_API_URL || 'http://localhost:5003/api';

const marketplaceClient = axios.create({
  baseURL: MARKETPLACE_API_URL,
  headers: { 'Content-Type': 'application/json' },
});

// Attach token if present
const attachAuthToken = (client) => {
  client.interceptors.request.use((config) => {
    const token = localStorage.getItem('ecotrack_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });
};

attachAuthToken(marketplaceClient);

export const valuationApi = {
  createValuation: (pickupItemId, data) =>
    marketplaceClient.post(`/valuations`, { pickupItemId, ...data }),
  updateValuation: (pickupItemId, data) =>
    marketplaceClient.put(`/valuations/${pickupItemId}`, data),
  getValuation: (pickupItemId) =>
    marketplaceClient.get(`/valuations/${pickupItemId}`),
};
