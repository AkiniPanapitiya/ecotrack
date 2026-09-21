import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    host: true,
    proxy: {
      '/api/identity': {
        target: 'http://localhost:5001',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/identity/, '/api'),
      },
      '/api/marketplace': {
        target: 'http://localhost:5003',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/marketplace/, '/marketplace'),
      },
      '/marketplace': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/analytics': {
        target: 'http://localhost:5004',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/analytics/, '/analytics'),
      },
      '/api': {
        target: 'http://localhost:5002',
        changeOrigin: true,
      },
    },
  },
});