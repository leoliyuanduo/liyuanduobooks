import { z } from 'zod';
import { config } from '../../../config';
import { ApiError, getJson } from '../../../lib/http';
import { logger } from '../../../lib/logger';
import type { BookOwner } from '../domain/types';
import { bookOwnerDtoSchema, toDomainOwner } from './schema';

const BOOK_OWNERS_PATH = '/v1/bookowners';

/**
 * Fetches and validates book owners.
 *
 * Validation policy (a deliberate trade-off, see docs/adr/0003):
 * - If the payload is not an array at all, the contract is broken -> fail loudly.
 * - If individual records are malformed, skip them and log a warning, so one
 *   bad record does not blank the whole page for every user.
 */
export async function fetchBookOwners(signal?: AbortSignal): Promise<BookOwner[]> {
  const correlationId = crypto.randomUUID();
  const url = `${config.apiBaseUrl}${BOOK_OWNERS_PATH}`;
  const startedAt = performance.now();

  const payload = await getJson(url, {
    timeoutMs: config.requestTimeoutMs,
    correlationId,
    ...(signal ? { signal } : {}),
  });

  const list = z.array(z.unknown()).safeParse(payload);
  if (!list.success) {
    logger.error('bookOwners.contractViolation', { correlationId, reason: 'payload is not an array' });
    throw new ApiError('contract', 'Unexpected response format from the book owners service');
  }

  const owners: BookOwner[] = [];
  list.data.forEach((record, index) => {
    const parsed = bookOwnerDtoSchema.safeParse(record);
    if (parsed.success) {
      owners.push(toDomainOwner(parsed.data));
    } else {
      logger.warn('bookOwners.recordSkipped', {
        correlationId,
        index,
        issues: parsed.error.issues.map((issue) => `${issue.path.join('.')}: ${issue.message}`),
      });
    }
  });

  logger.info('bookOwners.fetched', {
    correlationId,
    ownerCount: owners.length,
    skipped: list.data.length - owners.length,
    durationMs: Math.round(performance.now() - startedAt),
  });

  return owners;
}
