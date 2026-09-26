import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    host: true,
    proxy: {
      '/api/listings': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api/valuations': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/uploads': {
        target: 'http://localhost:5003',
        changeOrigin: true,
      },
      '/api': {
        target: 'http://localhost:5002',
        changeOrigin: true,
      },
    },
  },
});