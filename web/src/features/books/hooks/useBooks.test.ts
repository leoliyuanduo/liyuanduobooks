import { describe, expect, it } from 'vitest';
import { ApiError } from '../../../lib/http';
import { retryDelay, shouldRetry } from './useBooks';

describe('retry policy', () => {
  it('honours the server Retry-After hint, capped at 10 seconds', () => {
    expect(retryDelay(1, new ApiError('http', 'x', { status: 503, retryAfterMs: 2000 }))).toBe(2000);
    expect(retryDelay(1, new ApiError('http', 'x', { status: 503, retryAfterMs: 60_000 }))).toBe(10_000);
  });

  it('otherwise backs off exponentially', () => {
    expect(retryDelay(1, new Error('x'))).toBe(1000);
    expect(retryDelay(2, new Error('x'))).toBe(2000);
    expect(retryDelay(5, new Error('x'))).toBe(4000);
  });

  it('retries transient errors a limited number of times', () => {
    const unavailable = new ApiError('http', 'x', { status: 503 });
    expect(shouldRetry(0, unavailable)).toBe(true);
    expect(shouldRetry(2, unavailable)).toBe(false);
  });

  it('does not retry a 502: the server already retried the upstream', () => {
    expect(shouldRetry(0, new ApiError('http', 'x', { status: 502 }))).toBe(false);
  });
});
