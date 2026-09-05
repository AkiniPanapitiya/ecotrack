import axios from 'axios';

const IDENTITY_API_URL = import.meta.env.VITE_IDENTITY_API_URL || 'http://localhost:5001/api';
const LOGISTICS_API_URL = import.meta.env.VITE_LOGISTICS_API_URL || 'http://localhost:5002/api';

const api = axios.create({
  baseURL: IDENTITY_API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

const logisticsClient = axios.create({
  baseURL: LOGISTICS_API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
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

attachAuthToken(api);
attachAuthToken(logisticsClient);

// ECO-12 / ECO-13: Auth Service Endpoints
export const authApi = {
  register: (data) => api.post('/auth/register', data),
  login: (data) => api.post('/auth/login', data),
  logout: () => api.post('/auth/logout'),
  forgotPassword: (data) => api.post('/auth/forgot-password', data),
  resetPassword: (data) => api.post('/auth/reset-password', data),
};

// ECO-14: Profile Management Endpoints
export const profileApi = {
  getProfile: () => api.get('/profile'),
  updateProfile: (data) => api.put('/profile', data),
};

// ECO-15: Logistics Pickup Booking Endpoints
export const logisticsApi = {
  createPickup: (data) => logisticsClient.post('/pickup', data),
  getPickupById: (id) => logisticsClient.get(`/pickup/${id}`),
  getUserPickups: (userId) => logisticsClient.get(`/pickup/user/${userId}`),
};

export default api;
