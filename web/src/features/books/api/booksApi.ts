import { config } from '../../../config';
import { ApiError, getJson } from '../../../lib/http';
import { logger } from '../../../lib/logger';
import type { Book, BooksPage, BooksRequest } from '../model';

const BOOKS_PATH = '/api/v1/books';
/** The API's maximum page size, to minimise round trips. */
export const PAGE_SIZE = 100;
/** Defensive cap so a server bug (e.g. a cursor that never ends) cannot loop forever. */
export const MAX_PAGES = 50;

/**
 * Lightweight shape check. The BFF is ours and its types are generated, so full
 * schema validation would be redundant; this only guards against something that
 * is not our API answering (a proxy error page, a misrouted request).
 */
function isBooksPage(value: unknown): value is BooksPage {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Partial<BooksPage>;
  return Array.isArray(candidate.items) && typeof candidate.page === 'object' && candidate.page !== null;
}

/**
 * Fetches every book for one age category (and optional format), following
 * cursors until the last page. The server has already filtered and sorted;
 * pages arrive in order, so concatenating them preserves alphabetical order.
 */
export async function fetchAllBooks(request: BooksRequest, signal?: AbortSignal): Promise<Book[]> {
  const correlationId = crypto.randomUUID();
  const startedAt = performance.now();
  const books: Book[] = [];
  let cursor: string | null = null;

  for (let pageNumber = 1; pageNumber <= MAX_PAGES; pageNumber++) {
    const params = new URLSearchParams({ ageCategory: request.ageCategory, limit: String(PAGE_SIZE) });
    if (request.format) params.set('format', request.format);
    if (cursor) params.set('cursor', cursor);

    const body = await getJson(`${config.apiBaseUrl}${BOOKS_PATH}?${params.toString()}`, {
      timeoutMs: config.requestTimeoutMs,
      correlationId,
      ...(signal ? { signal } : {}),
    });

    if (!isBooksPage(body)) {
      logger.error('books.contractViolation', { correlationId, ageCategory: request.ageCategory });
      throw new ApiError('contract', 'Unexpected response format from the books service');
    }

    books.push(...body.items);
    cursor = body.page.nextCursor;

    if (cursor === null) {
      logger.info('books.fetched', {
        correlationId,
        ageCategory: request.ageCategory,
        format: request.format ?? 'all',
        count: books.length,
        pages: pageNumber,
        durationMs: Math.round(performance.now() - startedAt),
      });
      return books;
    }
  }

  logger.error('books.tooManyPages', { correlationId, maxPages: MAX_PAGES });
  throw new ApiError('contract', `Gave up after ${MAX_PAGES} pages`);
}
