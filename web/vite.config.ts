/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react';
import { defineConfig, loadEnv } from 'vite';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const upstream = env['API_PROXY_TARGET'] ?? 'https://digitalcodingtest.bupa.com.au';

  // Same-origin proxy: the browser calls /api/*, Vite forwards to the upstream API.
  // This sidesteps CORS in dev/preview; production uses the hosting layer for the same.
  const proxy = {
    '/api': { target: upstream, changeOrigin: true, secure: true },
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
        exclude: ['src/main.tsx', 'src/vite-env.d.ts', 'src/test/**'],
        thresholds: { lines: 90, functions: 90, branches: 85, statements: 90 },
      },
    },
  };
});
