/**
 * Domain model for the "books by owner age category" feature.
 *
 * These types are deliberately decoupled from the API DTOs (see ../api/schema.ts).
 * The API shape can change without rippling through the UI: only the mapper changes.
 */

/** Age categories defined by the business rule (see ./ageCategory.ts). */
export const AGE_CATEGORIES = ['adult', 'child'] as const;
export type AgeCategory = (typeof AGE_CATEGORIES)[number];

/**
 * Known book formats. Anything the API sends that we do not recognise is
 * mapped to 'other' rather than failing, so a new format does not break the page.
 */
export const BOOK_FORMATS = ['hardcover', 'paperback', 'ebook', 'other'] as const;
export type BookFormat = (typeof BOOK_FORMATS)[number];

export interface Book {
  readonly title: string;
  readonly format: BookFormat;
}

export interface BookOwner {
  readonly name: string;
  readonly age: number;
  readonly books: readonly Book[];
}

/** The filters a user can apply. A union (not a boolean) so new filters are additive. */
export type BookFilter = 'all' | 'hardcover';

/** A book as displayed: the owner is kept so list keys are stable and data is traceable. */
export interface OwnedBook extends Book {
  readonly ownerName: string;
}

/** Books grouped by owner age category. Every category is always present (possibly empty). */
export type BooksByCategory = Readonly<Record<AgeCategory, readonly OwnedBook[]>>;
