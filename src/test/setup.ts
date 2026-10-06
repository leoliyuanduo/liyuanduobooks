import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, beforeEach, vi } from 'vitest';
import { setLogSink } from '../lib/logger';

beforeEach(() => {
  // Keep test output quiet; individual tests can install a spy sink.
  setLogSink(() => {});
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
});
