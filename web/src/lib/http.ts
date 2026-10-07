/**
 * Thin, typed wrapper over fetch.
 *
 * - Every failure becomes an `ApiError` with a discriminated `kind`, so callers
 *   (and the retry policy) can branch exhaustively instead of string-matching.
 * - Requests have a hard timeout; a hung upstream must not hang the UI.
 * - A correlation id is sent and logged so a UI error can be traced to server logs.
 */
export type ApiErrorKind = 'network' | 'timeout' | 'http' | 'contract' | 'aborted';

/**
 * Statuses worth retrying from the browser. Deliberately excludes 502: the BFF
 * already retried the upstream before answering "bad gateway", so retrying again
 * only multiplies load. 503 (circuit open) and 504 (timeout) are worth one more try.
 */
const RETRYABLE_STATUSES: ReadonlySet<number> = new Set([408, 429, 500, 503, 504]);

export interface ApiErrorOptions {
  readonly status?: number;
  readonly retryAfterMs?: number;
  readonly cause?: unknown;
}

export class ApiError extends Error {
  readonly kind: ApiErrorKind;
  readonly status: number | undefined;
  /** Server-provided hint (Retry-After header) for when to try again. */
  readonly retryAfterMs: number | undefined;

  constructor(kind: ApiErrorKind, message: string, options?: ApiErrorOptions) {
    super(message, options?.cause === undefined ? undefined : { cause: options.cause });
    this.name = 'ApiError';
    this.kind = kind;
    this.status = options?.status;
    this.retryAfterMs = options?.retryAfterMs;
  }

  /**
   * Transient failures are worth retrying; client errors and bad payloads are not
   * (retrying a 400 or a malformed response just delays the inevitable).
   * GET is idempotent, so retrying it is safe.
   */
  get isRetryable(): boolean {
    switch (this.kind) {
      case 'network':
      case 'timeout':
        return true;
      case 'http':
        return this.status !== undefined && RETRYABLE_STATUSES.has(this.status);
      case 'contract':
      case 'aborted':
        return false;
    }
  }
}

/** Parses a Retry-After header given in seconds (the form the BFF sends). */
export function parseRetryAfter(value: string | null): number | undefined {
  if (value === null || value.trim() === '') return undefined;
  const seconds = Number(value);
  return Number.isFinite(seconds) && seconds >= 0 ? seconds * 1000 : undefined;
}

export interface GetJsonOptions {
  readonly timeoutMs: number;
  readonly signal?: AbortSignal;
  readonly correlationId?: string;
}

export async function getJson(url: string, options: GetJsonOptions): Promise<unknown> {
  const timeoutSignal = AbortSignal.timeout(options.timeoutMs);
  const signal = options.signal ? AbortSignal.any([options.signal, timeoutSignal]) : timeoutSignal;
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (options.correlationId) headers['X-Correlation-ID'] = options.correlationId;

  let response: Response;
  try {
    response = await fetch(url, { method: 'GET', headers, signal });
  } catch (cause) {
    if (timeoutSignal.aborted) {
      throw new ApiError('timeout', `Request timed out after ${options.timeoutMs} ms`, { cause });
    }
    if (options.signal?.aborted) {
      throw new ApiError('aborted', 'Request was cancelled', { cause });
    }
    throw new ApiError('network', 'Network request failed', { cause });
  }

  if (!response.ok) {
    const retryAfterMs = parseRetryAfter(response.headers.get('Retry-After'));
    throw new ApiError('http', `Request failed with HTTP ${response.status}`, {
      status: response.status,
      ...(retryAfterMs === undefined ? {} : { retryAfterMs }),
    });
  }

  try {
    return (await response.json()) as unknown;
  } catch (cause) {
    throw new ApiError('contract', 'Response was not valid JSON', { status: response.status, cause });
  }
}
