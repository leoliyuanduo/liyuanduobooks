import { describe, expect, it } from 'vitest';
import { ApiError, type ApiErrorKind } from '../../../lib/http';
import { describeError } from './errorMessages';

describe('describeError', () => {
  it.each<[ApiErrorKind, RegExp]>([
    ['timeout', /too long/],
    ['network', /could not reach/],
    ['contract', /did not understand/],
    ['aborted', /cancelled/],
  ])('gives a user-friendly message for %s errors', (kind, expected) => {
    expect(describeError(new ApiError(kind, 'technical detail'))).toMatch(expected);
  });

  it.each<[number | undefined, RegExp]>([
    [503, /temporarily unavailable/],
    [504, /too long/],
    [502, /provider returned an error/],
    [500, /returned an error/],
    [undefined, /returned an error/],
  ])('maps HTTP %s from the BFF to a helpful message', (status, expected) => {
    const error = new ApiError('http', 'technical detail', status === undefined ? {} : { status });
    expect(describeError(error)).toMatch(expected);
  });

  it('never leaks technical detail for unknown errors', () => {
    expect(describeError(new Error('TypeError at line 42'))).toBe(
      'Something went wrong while loading books.',
    );
  });
});
