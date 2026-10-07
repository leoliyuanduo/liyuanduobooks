import type { Book, BookFormat, BooksPage } from '../../features/books/model';

/**
 * What the BFF returns for the live upstream data (6 Oct 2026), mirroring the
 * BFF's own integration-test data. Typed against the generated OpenAPI types, so
 * if the API contract changes, this fixture stops compiling.
 */
const ALL: Readonly<Record<'adult' | 'child', readonly Book[]>> = {
  adult: [
    { id: 'b7', title: 'Great Expectations', format: 'hardcover' },
    { id: 'b5', title: "Gulliver's Travels", format: 'hardcover' },
    { id: 'b1', title: 'Hamlet', format: 'hardcover' },
    { id: 'b6', title: 'Jane Eyre', format: 'paperback' },
    { id: 'b4', title: 'React: The Ultimate Guide', format: 'hardcover' },
    { id: 'b2', title: 'Wuthering Heights', format: 'paperback' },
  ],
  child: [
    { id: 'b8', title: 'Great Expectations', format: 'hardcover' },
    { id: 'b3', title: 'Hamlet', format: 'paperback' },
    { id: 'b9', title: 'Little Red Riding Hood', format: 'hardcover' },
    { id: 'b10', title: 'The Hobbit', format: 'ebook' },
  ],
};

export function booksFor(ageCategory: 'adult' | 'child', format?: BookFormat): Book[] {
  return ALL[ageCategory].filter((book) => format === undefined || book.format === format);
}

export function page(items: readonly Book[], nextCursor: string | null = null): BooksPage {
  return { items: [...items], page: { limit: 100, nextCursor, totalCount: items.length } };
}

/** Routes a request URL to the BFF response the real server would give. */
export function bffResponse(url: string): BooksPage {
  const query = new URL(url, 'http://localhost').searchParams;
  const ageCategory = query.get('ageCategory');
  if (ageCategory !== 'adult' && ageCategory !== 'child') {
    throw new Error(`Test fixture got an invalid ageCategory: ${String(ageCategory)}`);
  }
  const format = (query.get('format') ?? undefined) as BookFormat | undefined;
  return page(booksFor(ageCategory, format));
}
