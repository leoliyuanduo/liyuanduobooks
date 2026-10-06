import { useMemo, useState } from 'react';
import { groupBooksByAgeCategory } from '../domain/groupBooks';
import { headingFor } from '../domain/headings';
import { AGE_CATEGORIES, type BookFilter } from '../domain/types';
import { useBookOwners } from '../hooks/useBookOwners';
import { BookGroup } from './BookGroup';
import { describeError } from './errorMessages';
import { FilterToolbar } from './FilterToolbar';

/**
 * Container component: owns the UI state (which filter is selected) and wires
 * the query to presentational components. Data is fetched once and filtered
 * client-side, so switching filters is instant and does not hit the API.
 */
export function BooksPage() {
  // null = the user has not asked for books yet (the challenge says "when selected").
  const [filter, setFilter] = useState<BookFilter | null>(null);
  const query = useBookOwners(filter !== null);

  const grouped = useMemo(
    () => (query.data && filter ? groupBooksByAgeCategory(query.data, filter) : null),
    [query.data, filter],
  );

  const handleSelect = (next: BookFilter) => {
    setFilter(next);
    // Let a user recover from a failed load by clicking again.
    if (query.isError) void query.refetch();
  };

  return (
    <>
      <FilterToolbar selected={filter} busy={query.isFetching} onSelect={handleSelect} />

      {/* Polite live region: screen readers announce loading and errors. */}
      <div className="status" role="status" aria-live="polite">
        {query.isLoading && <p className="status__loading">Loading books…</p>}
      </div>

      {query.isError && (
        <div className="alert" role="alert">
          <p>{describeError(query.error)}</p>
          <button type="button" className="button button--secondary" onClick={() => void query.refetch()}>
            Try again
          </button>
        </div>
      )}

      {filter === null && !query.data && <p className="hint">Select “Get Books” to load the book list.</p>}

      {grouped && filter && (
        <div className="book-groups">
          {AGE_CATEGORIES.map((category) => (
            <BookGroup key={category} heading={headingFor(category, filter)} books={grouped[category]} />
          ))}
        </div>
      )}
    </>
  );
}
