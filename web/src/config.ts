/**
 * Runtime configuration, read once from Vite env variables (see .env.example).
 *
 * The browser calls a same-origin path (`/api` by default). In development the
 * Vite dev server proxies it to the upstream API (vite.config.ts); in production
 * the hosting layer (Azure Front Door / APIM / Static Web Apps route, or a BFF)
 * does the same. This avoids CORS issues and keeps the upstream URL out of the bundle.
 */
function readPositiveInt(value: string | undefined, fallback: number): number {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback;
}

export const config = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? '/api',
  requestTimeoutMs: readPositiveInt(import.meta.env.VITE_REQUEST_TIMEOUT_MS, 8000),
  maxRetries: 2,
  /** Book-owner data changes rarely; avoid refetching on every click. */
  staleTimeMs: 5 * 60 * 1000,
} as const;
