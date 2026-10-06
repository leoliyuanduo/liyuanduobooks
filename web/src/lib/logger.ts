/**
 * Minimal structured logger.
 *
 * Every entry is a single object with a stable `event` name plus context, which is
 * what log pipelines (Application Insights, Datadog, etc.) query on. In production
 * the `sink` would be swapped for a telemetry SDK; call sites do not change.
 */
export type LogLevel = 'debug' | 'info' | 'warn' | 'error';

export interface LogEntry {
  readonly level: LogLevel;
  readonly event: string;
  readonly timestamp: string;
  readonly context?: Readonly<Record<string, unknown>>;
}

export type LogSink = (entry: LogEntry) => void;

const consoleSink: LogSink = (entry) => {
  // eslint-disable-next-line no-console -- the console is the dev sink
  console[entry.level](`[${entry.event}]`, entry);
};

let sink: LogSink = consoleSink;

/** Swap the sink (e.g. to App Insights in production, or a spy in tests). */
export function setLogSink(next: LogSink): void {
  sink = next;
}

function log(level: LogLevel, event: string, context?: Record<string, unknown>): void {
  sink({ level, event, timestamp: new Date().toISOString(), ...(context ? { context } : {}) });
}

export const logger = {
  debug: (event: string, context?: Record<string, unknown>) => log('debug', event, context),
  info: (event: string, context?: Record<string, unknown>) => log('info', event, context),
  warn: (event: string, context?: Record<string, unknown>) => log('warn', event, context),
  error: (event: string, context?: Record<string, unknown>) => log('error', event, context),
};
