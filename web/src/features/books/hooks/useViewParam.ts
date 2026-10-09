import { useCallback, useEffect, useState } from 'react';
import type { BookFilter } from '../model';

const PARAM = 'view';
const VIEWS: ReadonlySet<string> = new Set<BookFilter>(['all', 'hardcover']);

function readView(): BookFilter | null {
  const value = new URLSearchParams(window.location.search).get(PARAM);
  return value !== null && VIEWS.has(value) ? (value as BookFilter) : null;
}

/**
 * The selected view lives in the URL (?view=all | ?view=hardcover), so a view can be
 * bookmarked or shared, survives a reload, and Back/Forward move between views.
 * Unknown values are ignored rather than trusted.
 */
export function useViewParam(): [BookFilter | null, (view: BookFilter) => void] {
  const [view, setView] = useState<BookFilter | null>(readView);

  useEffect(() => {
    const onPopState = () => setView(readView());
    window.addEventListener('popstate', onPopState);
    return () => window.removeEventListener('popstate', onPopState);
  }, []);

  const selectView = useCallback((next: BookFilter) => {
    const url = new URL(window.location.href);
    if (url.searchParams.get(PARAM) !== next) {
      url.searchParams.set(PARAM, next);
      window.history.pushState(null, '', url);
    }
    setView(next);
  }, []);

  return [view, selectView];
}
