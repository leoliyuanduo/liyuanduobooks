import { useId } from 'react';
import type { Book } from '../model';

interface BookCardProps {
  readonly heading: string;
  readonly books: readonly Book[];
}

/** One age category: a blue heading block above the list of titles (as in the reference screenshots). */
export function BookCard({ heading, books }: BookCardProps) {
  const headingId = useId();

  return (
    <section className="book-card" aria-labelledby={headingId}>
      <h2 id={headingId} className="book-card__heading">
        {heading}
      </h2>
      {books.length === 0 ? (
        <p className="book-card__empty">No books to show.</p>
      ) : (
        <ul className="book-card__list">
          {books.map((book) => (
            // Ids are unique per owned copy, so two owners' copies of "Hamlet" are distinct keys.
            <li key={book.id}>{book.title}</li>
          ))}
        </ul>
      )}
    </section>
  );
}
