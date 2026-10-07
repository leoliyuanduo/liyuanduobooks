import { describe, expect, it, vi } from 'vitest';
import { ApiError, getJson, parseRetryAfter } from './http';

describe('getJson', () => {
  it('times out a request that never completes', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>(
        (_url, init) =>
          new Promise((_resolve, reject) => {
            init?.signal?.addEventListener('abort', () => reject(init.signal?.reason));
          }),
      ),
    );

    await expect(getJson('/slow', { timeoutMs: 20 })).rejects.toMatchObject({ kind: 'timeout' });
  });

  it('omits the correlation header when none is given', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValue(new Response('[]'));
    vi.stubGlobal('fetch', fetchMock);

    await getJson('/x', { timeoutMs: 1000 });

    expect(new Headers(fetchMock.mock.calls[0]?.[1]?.headers).has('X-Correlation-ID')).toBe(false);
  });
});

describe('ApiError.isRetryable', () => {
  it.each([
    [new ApiError('network', 'x'), true],
    [new ApiError('timeout', 'x'), true],
    [new ApiError('http', 'x', { status: 503 }), true],
    [new ApiError('http', 'x', { status: 504 }), true],
    [new ApiError('http', 'x', { status: 429 }), true],
    [new ApiError('http', 'x', { status: 502 }), false],
    [new ApiError('http', 'x', { status: 400 }), false],
    [new ApiError('http', 'x'), false],
    [new ApiError('contract', 'x'), false],
    [new ApiError('aborted', 'x'), false],
  ])('%o -> %s', (error, expected) => {
    expect(error.isRetryable).toBe(expected);
  });
});

describe('parseRetryAfter', () => {
  it.each([
    ['15', 15_000],
    ['0', 0],
    [null, undefined],
    ['', undefined],
    ['soon', undefined],
    ['-3', undefined],
  ])('%s -> %s', (header, expected) => {
    expect(parseRetryAfter(header)).toBe(expected);
  });
});

describe('getJson errors', () => {
  it('reports network failures and HTTP errors distinctly', async () => {
    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockRejectedValue(new TypeError('Failed to fetch')));
    await expect(getJson('/x', { timeoutMs: 1000 })).rejects.toMatchObject({ kind: 'network' });

    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockResolvedValue(new Response('{}', { status: 404 })));
    await expect(getJson('/x', { timeoutMs: 1000 })).rejects.toMatchObject({ kind: 'http', status: 404 });
  });

  it('reports a non-JSON body as a contract error', async () => {
    vi.stubGlobal('fetch', vi.fn<typeof fetch>().mockResolvedValue(new Response('<html>')));
    await expect(getJson('/x', { timeoutMs: 1000 })).rejects.toMatchObject({ kind: 'contract' });
  });

  it('reports a caller cancellation as aborted', async () => {
    const controller = new AbortController();
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>((_url, init) => {
        controller.abort();
        return Promise.reject(init?.signal?.reason ?? new Error('aborted'));
      }),
    );
    await expect(getJson('/x', { timeoutMs: 1000, signal: controller.signal })).rejects.toMatchObject({
      kind: 'aborted',
    });
  });
});
