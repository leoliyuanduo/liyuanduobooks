import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { setLogSink, type LogEntry } from '../lib/logger';
import { ErrorBoundary } from './ErrorBoundary';

function Boom(): never {
  throw new Error('render failed');
}

describe('ErrorBoundary', () => {
  it('renders children when nothing throws', () => {
    render(
      <ErrorBoundary>
        <p>fine</p>
      </ErrorBoundary>,
    );
    expect(screen.getByText('fine')).toBeInTheDocument();
  });

  it('shows a fallback and logs when a child throws', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {}); // React logs caught errors
    const entries: LogEntry[] = [];
    setLogSink((entry) => entries.push(entry));

    render(
      <ErrorBoundary>
        <Boom />
      </ErrorBoundary>,
    );

    expect(screen.getByRole('alert')).toHaveTextContent(/something went wrong/i);
    expect(entries.some((entry) => entry.event === 'ui.unhandledRenderError')).toBe(true);
  });
});
