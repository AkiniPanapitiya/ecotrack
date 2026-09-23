import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_IDENTITY_API_URL || 'http://localhost:5001/api',
  headers: { 'Content-Type': 'application/json' },
});

// Attach token from localStorage
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('ecotrack_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// POST /kyc/upload — upload KYC document (multipart form)
export const uploadDocument = (documentType, file) => {
  const formData = new FormData();
  formData.append('DocumentType', documentType);
  formData.append('File', file);
  return api.post('/kyc/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
};

// GET /kyc/my-status — get recycler's own verification status
export const getMyStatus = () => api.get('/kyc/my-status');

// GET /kyc/pending — admin: list all pending submissions
export const getPendingSubmissions = () => api.get('/kyc/pending');

// PUT /kyc/review/:documentId — admin: verify or reject
export const reviewDocument = (documentId, status, reviewNote) =>
  api.put(`/kyc/review/${documentId}`, { status, reviewNote });

// GET /admin/users — admin: list all users with roles (Story 8)
export const getUsers = (params) => api.get('/admin/users', { params });

// PUT /admin/users/:userId/role — admin: change a user's role (Story 8)
export const changeUserRole = (userId, data) =>
  api.put(`/admin/users/${userId}/role`, data);

// PATCH /admin/users/:userId/active — admin: toggle user active status (Story 8 + new)
export const toggleUserActive = (userId, isActive) =>
  api.patch(`/admin/users/${userId}/active`, { isActive });

export default api;
