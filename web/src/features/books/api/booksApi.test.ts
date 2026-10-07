import { describe, expect, it } from 'vitest';
import { booksFor, page } from '../../../test/fixtures/bffBooks';
import { jsonResponse, mockFetch, mockFetchWith, requestedUrls } from '../../../test/http';
import { fetchAllBooks, MAX_PAGES, PAGE_SIZE } from './booksApi';

describe('fetchAllBooks', () => {
  it('asks our server (not the upstream) for one category, at the maximum page size', async () => {
    const fetchMock = mockFetch(jsonResponse(page(booksFor('adult'))));

    const books = await fetchAllBooks({ ageCategory: 'adult' });

    expect(books.map((book) => book.title)).toEqual([
      'Great Expectations',
      "Gulliver's Travels",
      'Hamlet',
      'Jane Eyre',
      'React: The Ultimate Guide',
      'Wuthering Heights',
    ]);
    expect(requestedUrls(fetchMock)).toEqual([`/api/v1/books?ageCategory=adult&limit=${PAGE_SIZE}`]);
  });

  it('passes the format filter to the server', async () => {
    const fetchMock = mockFetch(jsonResponse(page(booksFor('child', 'hardcover'))));

    await fetchAllBooks({ ageCategory: 'child', format: 'hardcover' });

    expect(new URL(requestedUrls(fetchMock)[0] ?? '', 'http://x').searchParams.get('format')).toBe(
      'hardcover',
    );
  });

  it('follows cursors until the last page and keeps the server order', async () => {
    const [first, second, third] = booksFor('adult');
    const fetchMock = mockFetch(
      jsonResponse(page([first!, second!], 'cursor-2')),
      jsonResponse(page([third!], null)),
    );

    const books = await fetchAllBooks({ ageCategory: 'adult' });

    expect(books.map((book) => book.id)).toEqual([first!.id, second!.id, third!.id]);
    expect(new URL(requestedUrls(fetchMock)[1] ?? '', 'http://x').searchParams.get('cursor')).toBe(
      'cursor-2',
    );
  });

  it('uses one correlation id for every page of the same fetch', async () => {
    const fetchMock = mockFetch(jsonResponse(page([], 'next')), jsonResponse(page([])));

    await fetchAllBooks({ ageCategory: 'adult' });

    const ids = fetchMock.mock.calls.map(([, init]) => new Headers(init?.headers).get('X-Correlation-ID'));
    expect(ids[0]).toMatch(/[0-9a-f-]{36}/);
    expect(ids[1]).toBe(ids[0]);
  });

  it('gives up instead of looping forever if the server never ends paging', async () => {
    const fetchMock = mockFetchWith(() => jsonResponse(page([], 'same-cursor-again')));

    await expect(fetchAllBooks({ ageCategory: 'adult' })).rejects.toMatchObject({ kind: 'contract' });
    expect(fetchMock).toHaveBeenCalledTimes(MAX_PAGES);
  });

  it('rejects a response that is not a books page', async () => {
    mockFetch(jsonResponse({ unexpected: true }));
    await expect(fetchAllBooks({ ageCategory: 'adult' })).rejects.toMatchObject({ kind: 'contract' });
  });

  it('surfaces problem-details errors with status and Retry-After', async () => {
    mockFetch(jsonResponse({ title: 'Unavailable', status: 503 }, 503, { 'Retry-After': '15' }));

    await expect(fetchAllBooks({ ageCategory: 'adult' })).rejects.toMatchObject({
      kind: 'http',
      status: 503,
      retryAfterMs: 15_000,
    });
  });
});
