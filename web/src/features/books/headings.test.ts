import { describe, expect, it } from 'vitest';
import { headingFor } from './headings';
import { toBooksRequest } from './model';

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

describe('toBooksRequest', () => {
  it('omits the format for "all" and sets it for "hardcover"', () => {
    expect(toBooksRequest('adult', 'all')).toEqual({ ageCategory: 'adult' });
    expect(toBooksRequest('child', 'hardcover')).toEqual({ ageCategory: 'child', format: 'hardcover' });
  });
});
