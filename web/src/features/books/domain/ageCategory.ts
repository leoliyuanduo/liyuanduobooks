import type { AgeCategory } from './types';

/** Owners aged this or above are adults. Single source of truth for the business rule. */
export const ADULT_MIN_AGE = 18;

/**
 * Classifies an owner by age.
 * - 18 and above -> 'adult'
 * - 17 and below -> 'child'
 *
 * Ages are validated as non-negative integers at the API boundary, so this
 * function can stay total and simple.
 */
export function toAgeCategory(age: number): AgeCategory {
  return age >= ADULT_MIN_AGE ? 'adult' : 'child';
}
