import { useQuery } from '@tanstack/react-query';
import { config } from '../../../config';
import { ApiError } from '../../../lib/http';
import { fetchBookOwners } from '../api/bookOwnersApi';

export const bookOwnersQueryKey = ['bookOwners'] as const;

/**
 * Server state for book owners, via TanStack Query, which gives us:
 * - request de-duplication (double-clicks do not fire two requests),
 * - caching (switching filters does not refetch; filtering is client-side),
 * - cancellation (the AbortSignal is passed through to fetch),
 * - retries with exponential backoff, limited to transient errors.
 *
 * `enabled` lets the UI fetch on demand ("Get Books") rather than on page load.
 */
export function useBookOwners(enabled: boolean) {
  return useQuery({
    queryKey: bookOwnersQueryKey,
    queryFn: ({ signal }) => fetchBookOwners(signal),
    enabled,
    staleTime: config.staleTimeMs,
    retry: (failureCount, error) =>
      failureCount < config.maxRetries && (!(error instanceof ApiError) || error.isRetryable),
    retryDelay: (attempt) => Math.min(500 * 2 ** attempt, 4000),
  });
}
