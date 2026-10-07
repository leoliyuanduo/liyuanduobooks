/**
 * Runtime configuration, read once from Vite env variables (see .env.example).
 *
 * The browser only ever talks to OUR server (the BFF in /api), on the same origin:
 * in development the Vite dev server proxies /api to the BFF (vite.config.ts); in
 * production the BFF serves this app itself. No CORS, and no upstream URLs in the bundle.
 */
function readPositiveInt(value: string | undefined, fallback: number): number {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback;
}

export const config = {
  /** Origin of the BFF. Empty = same origin (the default and recommended setup). */
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? '',
  /** Must exceed the BFF's own upstream budget (10 s incl. retries) so the BFF can answer with a proper error first. */
  requestTimeoutMs: readPositiveInt(import.meta.env.VITE_REQUEST_TIMEOUT_MS, 15000),
  maxRetries: 2,
  /** The BFF caches upstream data for 5 minutes; refetching more often gains nothing. */
  staleTimeMs: 5 * 60 * 1000,
} as const;
