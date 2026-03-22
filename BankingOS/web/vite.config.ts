import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  root: __dirname,
  plugins: [react()],
  resolve: {
    alias: {
      '@shared': path.resolve(__dirname, '../../src'),
      '@root': path.resolve(__dirname, '../..')
    }
  },
  server: {
    port: 3007,
    fs: {
      allow: [
        path.resolve(__dirname),
        path.resolve(__dirname, '../../src'),
        path.resolve(__dirname, '../..')
      ]
    }
  },
  build: {
    outDir: path.resolve(__dirname, 'dist'),
    emptyOutDir: true
  }
});
