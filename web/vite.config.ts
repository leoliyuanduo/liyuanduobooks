/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react';
import { defineConfig, loadEnv } from 'vite';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const bff = env['API_PROXY_TARGET'] ?? 'http://localhost:5080';

  // Same-origin proxy: the browser calls /api/*, Vite forwards to our BFF (api/), never to Bupa directly.
  // In production the BFF serves this app itself, so the same relative URLs just work.
  const proxy = {
    '/api': { target: bff, changeOrigin: true },
  };

  return {
    plugins: [react()],
    server: { proxy },
    preview: { proxy },
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: ['./src/test/setup.ts'],
      css: false,
      coverage: {
        provider: 'v8',
        include: ['src/**/*.{ts,tsx}'],
        exclude: ['src/main.tsx', 'src/**/*.d.ts', 'src/test/**'], // .d.ts files are types only: nothing to cover
        thresholds: { lines: 90, functions: 90, branches: 85, statements: 90 },
      },
    },
  };
});
