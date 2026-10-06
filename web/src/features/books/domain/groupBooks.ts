import { toAgeCategory } from './ageCategory';
import type { AgeCategory, BookFilter, BookOwner, BooksByCategory, OwnedBook } from './types';

/**
 * Locale-aware, case-insensitive comparison so "apple" and "Apple" sort together
 * and accented titles sort where a human expects them. `numeric` makes "Book 2"
 * come before "Book 10".
 */
const titleCollator = new Intl.Collator('en-AU', { sensitivity: 'base', numeric: true });

export function compareByTitle(a: OwnedBook, b: OwnedBook): number {
  return titleCollator.compare(a.title, b.title) || titleCollator.compare(a.ownerName, b.ownerName);
}

const FILTER_PREDICATES: Readonly<Record<BookFilter, (book: OwnedBook) => boolean>> = {
  all: () => true,
  hardcover: (book) => book.format === 'hardcover',
};

/**
 * Core business logic: flatten owners into books, apply the filter, group by the
 * owner's age category and sort each group alphabetically by title.
 *
 * Pure and synchronous, so it is trivially unit-testable and cheap to memoise.
 */
export function groupBooksByAgeCategory(owners: readonly BookOwner[], filter: BookFilter): BooksByCategory {
  const groups: Record<AgeCategory, OwnedBook[]> = { adult: [], child: [] };
  const matches = FILTER_PREDICATES[filter];

  for (const owner of owners) {
    const category = toAgeCategory(owner.age);
    for (const book of owner.books) {
      const owned: OwnedBook = { ...book, ownerName: owner.name };
      if (matches(owned)) {
        groups[category].push(owned);
      }
    }
  }

  return {
    adult: groups.adult.sort(compareByTitle),
    child: groups.child.sort(compareByTitle),
  };
}
