import { describe, expect, it, vi } from 'vitest';
import { logger, setLogSink, type LogEntry } from './logger';

describe('logger', () => {
  it('emits structured entries with level, event, timestamp and context', () => {
    const entries: LogEntry[] = [];
    setLogSink((entry) => entries.push(entry));

    logger.debug('a');
    logger.info('b', { count: 1 });
    logger.warn('c');
    logger.error('d');

    expect(entries.map((entry) => [entry.level, entry.event])).toEqual([
      ['debug', 'a'],
      ['info', 'b'],
      ['warn', 'c'],
      ['error', 'd'],
    ]);
    expect(entries[1]?.context).toEqual({ count: 1 });
    expect(entries[0]).not.toHaveProperty('context');
    expect(() => new Date(entries[0]?.timestamp ?? '').toISOString()).not.toThrow();
  });
});

describe('default console sink', () => {
  it('writes to the console method matching the level', async () => {
    vi.resetModules();
    const fresh = await import('./logger');
    const spy = vi.spyOn(console, 'warn').mockImplementation(() => {});

    fresh.logger.warn('something.odd', { id: 1 });

    expect(spy).toHaveBeenCalledWith('[something.odd]', expect.objectContaining({ level: 'warn' }));
  });
});
