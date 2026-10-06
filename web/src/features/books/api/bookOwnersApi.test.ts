import { describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../../lib/http';
import { setLogSink, type LogEntry } from '../../../lib/logger';
import { jsonResponse, mockFetch } from '../../../test/http';
import { fetchBookOwners } from './bookOwnersApi';
import { toBookFormat } from './schema';

describe('fetchBookOwners', () => {
  it('calls the same-origin endpoint and maps DTOs to domain owners', async () => {
    const fetchMock = mockFetch(
      jsonResponse([{ name: 'Jane', age: 23, books: [{ name: 'Hamlet', type: 'Hardcover' }] }]),
    );

    const owners = await fetchBookOwners();

    expect(fetchMock).toHaveBeenCalledOnce();
    const [url, init] = fetchMock.mock.calls[0] ?? [];
    expect(url).toBe('/api/v1/bookowners');
    expect(new Headers(init?.headers).get('X-Correlation-ID')).toMatch(/[0-9a-f-]{36}/);
    expect(owners).toEqual([{ name: 'Jane', age: 23, books: [{ title: 'Hamlet', format: 'hardcover' }] }]);
  });

  it('treats null or missing books as an empty list', async () => {
    mockFetch(
      jsonResponse([
        { name: 'A', age: 1, books: null },
        { name: 'B', age: 2 },
      ]),
    );
    const owners = await fetchBookOwners();
    expect(owners.map((owner) => owner.books)).toEqual([[], []]);
  });

  it('skips malformed records, keeps valid ones and logs a warning', async () => {
    const entries: LogEntry[] = [];
    setLogSink((entry) => entries.push(entry));
    mockFetch(
      jsonResponse([
        { name: 'Valid', age: 30, books: [] },
        { name: 'Bad age', age: -1, books: [] },
        { name: 'Bad age type', age: '30', books: [] },
        { age: 30, books: [] },
      ]),
    );

    const owners = await fetchBookOwners();

    expect(owners.map((owner) => owner.name)).toEqual(['Valid']);
    expect(entries.filter((entry) => entry.event === 'bookOwners.recordSkipped')).toHaveLength(3);
  });

  it('fails with a contract error when the payload is not an array', async () => {
    mockFetch(jsonResponse({ owners: [] }));
    await expect(fetchBookOwners()).rejects.toMatchObject({ kind: 'contract' });
  });

  it('fails with a contract error when the body is not JSON', async () => {
    mockFetch(new Response('<html>oops</html>', { status: 200 }));
    await expect(fetchBookOwners()).rejects.toMatchObject({ kind: 'contract' });
  });

  it('surfaces HTTP errors with their status', async () => {
    mockFetch(jsonResponse({ message: 'down' }, 503));
    await expect(fetchBookOwners()).rejects.toMatchObject({ kind: 'http', status: 503 });
  });

  it('surfaces network failures', async () => {
    mockFetch(new TypeError('Failed to fetch'));
    await expect(fetchBookOwners()).rejects.toMatchObject({ kind: 'network' });
  });

  it('reports a cancelled request as aborted', async () => {
    const controller = new AbortController();
    vi.stubGlobal(
      'fetch',
      vi.fn<typeof fetch>((_url, init) => {
        controller.abort();
        return Promise.reject(init?.signal?.reason ?? new Error('aborted'));
      }),
    );
    await expect(fetchBookOwners(controller.signal)).rejects.toMatchObject({ kind: 'aborted' });
  });
});

describe('ApiError.isRetryable', () => {
  it.each([
    [new ApiError('network', 'x'), true],
    [new ApiError('timeout', 'x'), true],
    [new ApiError('http', 'x', { status: 500 }), true],
    [new ApiError('http', 'x', { status: 503 }), true],
    [new ApiError('http', 'x', { status: 429 }), true],
    [new ApiError('http', 'x', { status: 404 }), false],
    [new ApiError('http', 'x', { status: 400 }), false],
    [new ApiError('contract', 'x'), false],
    [new ApiError('aborted', 'x'), false],
  ])('%o -> %s', (error, expected) => {
    expect(error.isRetryable).toBe(expected);
  });
});

describe('toBookFormat', () => {
  it.each([
    ['Hardcover', 'hardcover'],
    ['HARDCOVER', 'hardcover'],
    ['Paperback', 'paperback'],
    ['Ebook', 'ebook'],
    ['Audiobook', 'other'],
    ['', 'other'],
  ] as const)('%s -> %s', (raw, expected) => {
    expect(toBookFormat(raw)).toBe(expected);
  });
});
