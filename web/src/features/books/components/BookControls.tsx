import type { BookFilter } from '../model';

interface BookControlsProps {
  readonly selected: BookFilter | null;
  readonly busy: boolean;
  readonly onSelect: (filter: BookFilter) => void;
}

/**
 * "Get Books" (primary button) and "Hardcover only" (styled as a link, as in the
 * screenshots). Both are real <button>s: they perform actions, not navigation, so
 * they get keyboard and screen-reader semantics for free. `aria-pressed` exposes
 * which view is active.
 *
 * DOM order is primary action first, which is also the visual order on mobile;
 * on desktop CSS reverses the row so the link sits to the left of the button.
 */
export function BookControls({ selected, busy, onSelect }: BookControlsProps) {
  return (
    <div className="controls" role="group" aria-label="Book filters">
      <button
        type="button"
        className="button-primary"
        aria-pressed={selected === 'all'}
        aria-busy={busy && selected === 'all'}
        onClick={() => onSelect('all')}
      >
        Get Books
      </button>
      <button
        type="button"
        className="button-link"
        aria-pressed={selected === 'hardcover'}
        aria-busy={busy && selected === 'hardcover'}
        onClick={() => onSelect('hardcover')}
      >
        Hardcover only
      </button>
    </div>
  );
}
