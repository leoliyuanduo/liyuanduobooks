import { z } from 'zod';
import type { BookFormat, BookOwner } from '../domain/types';

/**
 * Runtime contract for GET /api/v1/bookowners.
 *
 * The published Swagger document declares no response schema, so TypeScript
 * types alone would be a promise we cannot keep. We validate at the boundary
 * and convert to domain types once; nothing past this file sees raw JSON.
 */
export const bookDtoSchema = z.object({
  name: z.string().trim().min(1),
  type: z.string().trim(),
});

export const bookOwnerDtoSchema = z.object({
  name: z.string().trim().min(1),
  age: z.number().int().nonnegative(),
  // Tolerate a missing or null list: an owner with no books is valid data.
  books: z
    .array(bookDtoSchema)
    .nullish()
    .transform((books) => books ?? []),
});

export type BookOwnerDto = z.infer<typeof bookOwnerDtoSchema>;

const FORMAT_LOOKUP: Readonly<Record<string, BookFormat>> = {
  hardcover: 'hardcover',
  paperback: 'paperback',
  ebook: 'ebook',
};

export function toBookFormat(raw: string): BookFormat {
  return FORMAT_LOOKUP[raw.toLowerCase()] ?? 'other';
}

export function toDomainOwner(dto: BookOwnerDto): BookOwner {
  return {
    name: dto.name,
    age: dto.age,
    books: dto.books.map((book) => ({ title: book.name, format: toBookFormat(book.type) })),
  };
}
