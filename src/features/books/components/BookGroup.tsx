import { useId } from 'react';
import type { OwnedBook } from '../domain/types';

interface BookGroupProps {
  readonly heading: string;
  readonly books: readonly OwnedBook[];
}

export function BookGroup({ heading, books }: BookGroupProps) {
  const headingId = useId();

  return (
    <section className="book-group" aria-labelledby={headingId}>
      <h2 id={headingId} className="book-group__heading">
        {heading}
      </h2>
      {books.length === 0 ? (
        <p className="book-group__empty">No books to show.</p>
      ) : (
        <ul className="book-group__list">
          {books.map((book) => (
            // Titles are not unique across owners, so the key includes the owner.
            <li key={`${book.ownerName}::${book.title}`} className="book-group__item">
              {book.title}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
