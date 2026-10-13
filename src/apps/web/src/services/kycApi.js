import axios from 'axios';
import { serviceUrl } from './api';

const api = axios.create({
  baseURL: import.meta.env.VITE_IDENTITY_API_URL || serviceUrl('identity', 5001, '/api'),
  headers: { 'Content-Type': 'application/json' },
});

// Attach token from localStorage & handle FormData
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('ecotrack_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  if (config.data instanceof FormData) {
    delete config.headers['Content-Type'];
  }
  return config;
});

// POST /kyc/upload — upload KYC document (multipart form)
export const uploadDocument = (documentType, file, backFile = null) => {
  const formData = new FormData();
  formData.append('DocumentType', documentType);
  formData.append('File', file);
  if (backFile) {
    formData.append('BackFile', backFile);
  }
  return api.post('/kyc/upload', formData);
};

// GET /kyc/my-status — get recycler's own verification status
export const getMyStatus = () => api.get('/kyc/my-status');

// GET /kyc/pending — admin: list all pending submissions
export const getPendingSubmissions = () => api.get('/kyc/pending');

// PUT /kyc/review/:documentId — admin: verify or reject
export const reviewDocument = (documentId, status, reviewNote) =>
  api.put(`/kyc/review/${documentId}`, { status, reviewNote });

// GET /kyc/document/:documentId — get document blob for preview or download (supports ?side=front or ?side=back)
export const getDocumentBlob = (documentId, side = 'front') =>
  api.get(`/kyc/document/${documentId}?side=${side}`, { responseType: 'blob' });

// Helper to get direct preview URL with token query parameter
export const getDocumentUrl = (documentId) => {
  const token = localStorage.getItem('ecotrack_token');
  const base = import.meta.env.VITE_IDENTITY_API_URL || serviceUrl('identity', 5001, '/api');
  return `${base}/kyc/document/${documentId}${token ? `?token=${encodeURIComponent(token)}` : ''}`;
};

// GET /admin/users — admin: list all users with roles (Story 8)
export const getUsers = (params) => api.get('/admin/users', { params });

// PUT /admin/users/:userId/role — admin: change a user's role (Story 8)
export const changeUserRole = (userId, data) =>
  api.put(`/admin/users/${userId}/role`, data);

// PATCH /admin/users/:userId/active — admin: toggle user active status (Story 8 + new)
export const toggleUserActive = (userId, isActive) =>
  api.patch(`/admin/users/${userId}/active`, { isActive });

export default api;
