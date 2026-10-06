import { describe, expect, it } from 'vitest';
import fixture from '../../../test/fixtures/bookOwners.json';
import { toDomainOwner, bookOwnerDtoSchema } from '../api/schema';
import { toAgeCategory } from './ageCategory';
import { groupBooksByAgeCategory } from './groupBooks';
import { headingFor } from './headings';
import type { BookOwner } from './types';

const titles = (books: readonly { title: string }[]) => books.map((book) => book.title);

// The fixture is a verbatim capture of the live API response (6 Oct 2026).
const liveOwners: BookOwner[] = fixture.map((raw) => toDomainOwner(bookOwnerDtoSchema.parse(raw)));

describe('toAgeCategory', () => {
  it.each([
    [0, 'child'],
    [17, 'child'],
    [18, 'adult'],
    [19, 'adult'],
    [120, 'adult'],
  ] as const)('classifies age %i as %s', (age, expected) => {
    expect(toAgeCategory(age)).toBe(expected);
  });
});

describe('groupBooksByAgeCategory', () => {
  it('groups all books from the live API data by owner age, sorted alphabetically', () => {
    const result = groupBooksByAgeCategory(liveOwners, 'all');

    expect(titles(result.adult)).toEqual([
      'Great Expectations',
      "Gulliver's Travels",
      'Hamlet',
      'Jane Eyre',
      'React: The Ultimate Guide',
      'Wuthering Heights',
    ]);
    expect(titles(result.child)).toEqual([
      'Great Expectations',
      'Hamlet',
      'Little Red Riding Hood',
      'The Hobbit',
    ]);
  });

  it('shows only hardcover books when the hardcover filter is applied', () => {
    const result = groupBooksByAgeCategory(liveOwners, 'hardcover');

    expect(titles(result.adult)).toEqual([
      'Great Expectations',
      "Gulliver's Travels",
      'Hamlet',
      'React: The Ultimate Guide',
    ]);
    expect(titles(result.child)).toEqual(['Great Expectations', 'Little Red Riding Hood']);
    expect([...result.adult, ...result.child].every((book) => book.format === 'hardcover')).toBe(true);
  });

  it('sorts case-insensitively and numerically', () => {
    const owners: BookOwner[] = [
      {
        name: 'A',
        age: 30,
        books: [
          { title: 'zebra', format: 'ebook' },
          { title: 'Book 10', format: 'ebook' },
          { title: 'Apple', format: 'ebook' },
          { title: 'Book 2', format: 'ebook' },
        ],
      },
    ];
    expect(titles(groupBooksByAgeCategory(owners, 'all').adult)).toEqual([
      'Apple',
      'Book 2',
      'Book 10',
      'zebra',
    ]);
  });

  it('keeps every owned copy when two owners in the same category own the same title', () => {
    const owners: BookOwner[] = [
      { name: 'Ann', age: 40, books: [{ title: 'Hamlet', format: 'hardcover' }] },
      { name: 'Bob', age: 50, books: [{ title: 'Hamlet', format: 'paperback' }] },
    ];
    const result = groupBooksByAgeCategory(owners, 'all');
    expect(result.adult.map((book) => book.ownerName)).toEqual(['Ann', 'Bob']);
  });

  it('returns both categories as empty lists when there is no data', () => {
    expect(groupBooksByAgeCategory([], 'all')).toEqual({ adult: [], child: [] });
  });

  it('does not mutate its input', () => {
    const owners: BookOwner[] = [
      {
        name: 'Ann',
        age: 40,
        books: [
          { title: 'Z', format: 'hardcover' },
          { title: 'A', format: 'hardcover' },
        ],
      },
    ];
    const snapshot = structuredClone(owners);
    groupBooksByAgeCategory(owners, 'all');
    expect(owners).toEqual(snapshot);
  });
});

describe('headingFor', () => {
  it.each([
    ['adult', 'all', 'Books owned by Adults'],
    ['child', 'all', 'Books owned by Children'],
    ['adult', 'hardcover', 'Hardcover Books owned by Adults'],
    ['child', 'hardcover', 'Hardcover Books owned by Children'],
  ] as const)('%s + %s -> "%s"', (category, filter, expected) => {
    expect(headingFor(category, filter)).toBe(expected);
  });
});
