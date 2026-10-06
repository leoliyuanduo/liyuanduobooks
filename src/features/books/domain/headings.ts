import type { AgeCategory, BookFilter } from './types';

const CATEGORY_LABELS: Readonly<Record<AgeCategory, string>> = {
  adult: 'Adults',
  child: 'Children',
};

const FILTER_PREFIXES: Readonly<Record<BookFilter, string>> = {
  all: 'Books',
  hardcover: 'Hardcover Books',
};

/**
 * Produces the section heading, e.g. "Books owned by Adults" or
 * "Hardcover Books owned by Children" (the dynamic-heading requirement).
 * Typed records mean adding a new filter or category is a compile error until
 * its label exists.
 */
export function headingFor(category: AgeCategory, filter: BookFilter): string {
  return `${FILTER_PREFIXES[filter]} owned by ${CATEGORY_LABELS[category]}`;
}
