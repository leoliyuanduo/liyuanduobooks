import { useQueries } from '@tanstack/react-query';
import { config } from '../../../config';
import { ApiError } from '../../../lib/http';
import { fetchAllBooks } from '../api/booksApi';
import { AGE_CATEGORIES, type AgeCategory, type Book, type BookFilter, toBooksRequest } from '../model';

export interface BooksByCategoryState {
  /** Books per category, present once that category has loaded. */
  readonly books: Partial<Record<AgeCategory, readonly Book[]>>;
  readonly isLoading: boolean;
  readonly isFetching: boolean;
  readonly error: Error | null;
  readonly refetch: () => void;
}

const MAX_RETRY_DELAY_MS = 10_000;

/** Honour the server's Retry-After when it sends one; otherwise exponential backoff. */
export function retryDelay(failureCount: number, error: Error): number {
  if (error instanceof ApiError && error.retryAfterMs !== undefined) {
    return Math.min(error.retryAfterMs, MAX_RETRY_DELAY_MS);
  }
  return Math.min(500 * 2 ** failureCount, 4000);
}

export function shouldRetry(failureCount: number, error: Error): boolean {
  return failureCount < config.maxRetries && (!(error instanceof ApiError) || error.isRetryable);
}

/**
 * One query per age category, run in parallel. TanStack Query provides
 * de-duplication (double clicks), caching per (category, filter), cancellation via
 * AbortSignal, and retries. `filter === null` means "nothing requested yet".
 */
export function useBooksByCategory(filter: BookFilter | null): BooksByCategoryState {
  return useQueries({
    queries: AGE_CATEGORIES.map((ageCategory) => ({
      queryKey: ['books', ageCategory, filter] as const,
      queryFn: ({ signal }: { signal: AbortSignal }) =>
        fetchAllBooks(toBooksRequest(ageCategory, filter ?? 'all'), signal),
      enabled: filter !== null,
      staleTime: config.staleTimeMs,
      retry: shouldRetry,
      retryDelay,
    })),
    combine: (results) => {
      const books: Partial<Record<AgeCategory, readonly Book[]>> = {};
      AGE_CATEGORIES.forEach((category, index) => {
        const data = results[index]?.data;
        if (data) books[category] = data;
      });

      return {
        books,
        isLoading: results.some((result) => result.isLoading),
        isFetching: results.some((result) => result.isFetching),
        error: results.find((result) => result.error)?.error ?? null,
        refetch: () => {
          for (const result of results) {
            if (result.isError) void result.refetch();
          }
        },
      };
    },
  });
}
