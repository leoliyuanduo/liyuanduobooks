import type { AgeCategory, BookFilter } from './model';

const CATEGORY_LABELS: Readonly<Record<AgeCategory, string>> = {
  adult: 'Adults',
  child: 'Children',
};

const FILTER_PREFIXES: Readonly<Record<BookFilter, string>> = {
  all: 'Books',
  hardcover: 'Hardcover Books',
};

/**
 * Section heading, e.g. "Books owned by Adults" or "Hardcover Books owned by Children"
 * (the dynamic-heading requirement). Display text lives in the client: it is
 * presentation, not a business rule, and could be localised without touching the API.
 */
export function headingFor(category: AgeCategory, filter: BookFilter): string {
  return `${FILTER_PREFIXES[filter]} owned by ${CATEGORY_LABELS[category]}`;
}
