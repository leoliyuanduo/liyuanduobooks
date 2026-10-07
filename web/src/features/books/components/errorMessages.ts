import { ApiError } from '../../../lib/http';

/**
 * Maps technical failures to user-facing copy. Users get an actionable message;
 * technical detail goes to the logs (with a correlation id), never to the screen.
 * HTTP statuses follow the BFF contract (ADR-0004).
 */
export function describeError(error: Error): string {
  if (error instanceof ApiError) {
    switch (error.kind) {
      case 'timeout':
        return 'The book service is taking too long to respond.';
      case 'network':
        return 'We could not reach the book service. Check your connection.';
      case 'http':
        return describeStatus(error.status);
      case 'contract':
        return 'The book service sent data we did not understand.';
      case 'aborted':
        return 'The request was cancelled.';
    }
  }
  return 'Something went wrong while loading books.';
}

function describeStatus(status: number | undefined): string {
  switch (status) {
    case 503:
      return 'The book service is temporarily unavailable. Please try again shortly.';
    case 504:
      return 'The book service is taking too long to respond.';
    case 502:
      return 'The book provider returned an error. Please try again.';
    default:
      return 'The book service returned an error.';
  }
}
