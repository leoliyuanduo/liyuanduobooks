import { describe, expect, it, vi } from 'vitest';
import { getJson } from './http';

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
