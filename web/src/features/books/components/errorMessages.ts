import { ApiError } from '../../../lib/http';

/**
 * Maps technical failures to user-facing copy. Users get an actionable message;
 * technical detail goes to the logs (with a correlation id), never to the screen.
 */
export function describeError(error: Error): string {
  if (error instanceof ApiError) {
    switch (error.kind) {
      case 'timeout':
        return 'The book service is taking too long to respond.';
      case 'network':
        return 'We could not reach the book service. Check your connection.';
      case 'http':
        return 'The book service returned an error.';
      case 'contract':
        return 'The book service sent data we did not understand.';
      case 'aborted':
        return 'The request was cancelled.';
    }
  }
  return 'Something went wrong while loading books.';
}
