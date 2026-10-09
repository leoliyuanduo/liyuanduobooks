import { headingFor } from '../headings';
import { useBooksByCategory } from '../hooks/useBooks';
import { useViewParam } from '../hooks/useViewParam';
import { AGE_CATEGORIES, type BookFilter } from '../model';
import { BookCard } from './BookCard';
import { BookControls } from './BookControls';
import { describeError } from './errorMessages';

/**
 * Container component: owns which view is selected (kept in the URL) and wires the
 * server state to presentational components. Nothing is fetched until the user asks
 * ("When you click Get Books, all books are shown") or opens a link to a view.
 */
export function BooksPage() {
  const [filter, setFilter] = useViewParam();
  const { books, isLoading, isFetching, error, refetch } = useBooksByCategory(filter);

  const handleSelect = (next: BookFilter) => {
    setFilter(next);
    // Let a user recover from a failed load by clicking again.
    if (error) refetch();
  };

  const allLoaded = filter !== null && AGE_CATEGORIES.every((category) => books[category] !== undefined);

  return (
    <>
      {/* Polite live region: screen readers announce loading. */}
      <div className="status" role="status" aria-live="polite">
        {isLoading && <p className="status__loading">Loading books…</p>}
      </div>

      {error && !allLoaded && (
        <div className="alert" role="alert">
          <p>{describeError(error)}</p>
          <button type="button" className="button-secondary" onClick={refetch}>
            Try again
          </button>
        </div>
      )}

      {filter === null && (
        <p className="hint">Select “Get Books” to see every book, grouped by the age of its owner.</p>
      )}

      {allLoaded && (
        <div className="book-cards">
          {AGE_CATEGORIES.map((category) => (
            <BookCard key={category} heading={headingFor(category, filter)} books={books[category] ?? []} />
          ))}
        </div>
      )}

      <hr className="divider" />
      <BookControls selected={filter} busy={isFetching} onSelect={handleSelect} />
    </>
  );
}
