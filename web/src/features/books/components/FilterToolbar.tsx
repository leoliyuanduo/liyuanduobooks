import type { BookFilter } from '../domain/types';

interface FilterOption {
  readonly filter: BookFilter;
  readonly label: string;
}

const OPTIONS: readonly FilterOption[] = [
  { filter: 'all', label: 'Get Books' },
  { filter: 'hardcover', label: 'Hardcover Only' },
];

interface FilterToolbarProps {
  readonly selected: BookFilter | null;
  readonly busy: boolean;
  readonly onSelect: (filter: BookFilter) => void;
}

/**
 * Toggle buttons. `aria-pressed` tells assistive tech which view is active;
 * a native <button> gives keyboard and focus behaviour for free.
 */
export function FilterToolbar({ selected, busy, onSelect }: FilterToolbarProps) {
  return (
    <div className="toolbar" role="group" aria-label="Book filters">
      {OPTIONS.map(({ filter, label }) => (
        <button
          key={filter}
          type="button"
          className="button"
          aria-pressed={selected === filter}
          aria-busy={busy && selected === filter}
          onClick={() => onSelect(filter)}
        >
          {label}
        </button>
      ))}
    </div>
  );
}
