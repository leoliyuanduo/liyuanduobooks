/**
 * Thin, typed wrapper over fetch.
 *
 * - Every failure becomes an `ApiError` with a discriminated `kind`, so callers
 *   (and the retry policy) can branch exhaustively instead of string-matching.
 * - Requests have a hard timeout; a hung upstream must not hang the UI.
 * - A correlation id is sent and logged so a UI error can be traced to server logs.
 */
export type ApiErrorKind = 'network' | 'timeout' | 'http' | 'contract' | 'aborted';

export class ApiError extends Error {
  readonly kind: ApiErrorKind;
  readonly status: number | undefined;

  constructor(kind: ApiErrorKind, message: string, options?: { status?: number; cause?: unknown }) {
    super(message, options?.cause === undefined ? undefined : { cause: options.cause });
    this.name = 'ApiError';
    this.kind = kind;
    this.status = options?.status;
  }

  /**
   * Transient failures are worth retrying; client errors and bad payloads are not
   * (retrying a 404 or a malformed response just delays the inevitable).
   * GET is idempotent, so retrying it is safe.
   */
  get isRetryable(): boolean {
    switch (this.kind) {
      case 'network':
      case 'timeout':
        return true;
      case 'http':
        return this.status === undefined || this.status >= 500 || this.status === 429 || this.status === 408;
      case 'contract':
      case 'aborted':
        return false;
    }
  }
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
    throw new ApiError('http', `Request failed with HTTP ${response.status}`, { status: response.status });
  }

  try {
    return (await response.json()) as unknown;
  } catch (cause) {
    throw new ApiError('contract', 'Response was not valid JSON', { status: response.status, cause });
  }
}
