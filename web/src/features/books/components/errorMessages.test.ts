import { describe, expect, it } from 'vitest';
import { ApiError, type ApiErrorKind } from '../../../lib/http';
import { describeError } from './errorMessages';

describe('describeError', () => {
  it.each<[ApiErrorKind, RegExp]>([
    ['timeout', /too long/],
    ['network', /could not reach/],
    ['http', /returned an error/],
    ['contract', /did not understand/],
    ['aborted', /cancelled/],
  ])('gives a user-friendly message for %s errors', (kind, expected) => {
    expect(describeError(new ApiError(kind, 'technical detail'))).toMatch(expected);
  });

  it('never leaks technical detail for unknown errors', () => {
    expect(describeError(new Error('TypeError at line 42'))).toBe(
      'Something went wrong while loading books.',
    );
  });
});
