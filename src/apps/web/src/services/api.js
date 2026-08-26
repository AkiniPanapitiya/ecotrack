import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5001/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// ECO-12: Auth Service Registration Endpoint
export const authApi = {
  register: (data) => api.post('/auth/register', data),
};

export default api;
