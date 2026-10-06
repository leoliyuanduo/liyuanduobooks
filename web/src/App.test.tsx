import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { App } from './App';
import fixture from './test/fixtures/bookOwners.json';
import { jsonResponse, mockFetch } from './test/http';

function listItems(heading: string): string[] {
  const section = screen.getByRole('region', { name: heading });
  return within(section)
    .queryAllByRole('listitem')
    .map((item) => item.textContent ?? '');
}

describe('App (integration)', () => {
  it('does not call the API until the user selects "Get Books"', () => {
    const fetchMock = mockFetch(jsonResponse(fixture));
    render(<App />);

    expect(fetchMock).not.toHaveBeenCalled();
    expect(screen.getByText(/select “get books”/i)).toBeInTheDocument();
  });

  it('shows all books grouped by owner age category and sorted alphabetically', async () => {
    mockFetch(jsonResponse(fixture));
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByRole('button', { name: 'Get Books' }));

    expect(await screen.findByRole('heading', { name: 'Books owned by Adults' })).toBeInTheDocument();
    expect(listItems('Books owned by Adults')).toEqual([
      'Great Expectations',
      "Gulliver's Travels",
      'Hamlet',
      'Jane Eyre',
      'React: The Ultimate Guide',
      'Wuthering Heights',
    ]);
    expect(listItems('Books owned by Children')).toEqual([
      'Great Expectations',
      'Hamlet',
      'Little Red Riding Hood',
      'The Hobbit',
    ]);
    expect(screen.getByRole('button', { name: 'Get Books' })).toHaveAttribute('aria-pressed', 'true');
  });

  it('filters to hardcover books, changes the headings and does not refetch', async () => {
    const fetchMock = mockFetch(jsonResponse(fixture));
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByRole('button', { name: 'Get Books' }));
    await screen.findByRole('heading', { name: 'Books owned by Adults' });
    await user.click(screen.getByRole('button', { name: 'Hardcover Only' }));

    expect(screen.getByRole('heading', { name: 'Hardcover Books owned by Adults' })).toBeInTheDocument();
    expect(listItems('Hardcover Books owned by Children')).toEqual([
      'Great Expectations',
      'Little Red Riding Hood',
    ]);
    expect(screen.queryByText('The Hobbit')).not.toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledOnce();
  });

  it('can be used directly via "Hardcover Only" without "Get Books" first', async () => {
    mockFetch(jsonResponse(fixture));
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByRole('button', { name: 'Hardcover Only' }));

    expect(
      await screen.findByRole('heading', { name: 'Hardcover Books owned by Adults' }),
    ).toBeInTheDocument();
  });

  it('shows an empty-state message for a category with no matching books', async () => {
    mockFetch(jsonResponse([{ name: 'Kid', age: 9, books: [{ name: 'E-Reader Fun', type: 'Ebook' }] }]));
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByRole('button', { name: 'Hardcover Only' }));

    const section = await screen.findByRole('region', { name: 'Hardcover Books owned by Children' });
    expect(within(section).getByText('No books to show.')).toBeInTheDocument();
  });

  it('shows a friendly error for a non-retryable failure and recovers on "Try again"', async () => {
    const fetchMock = mockFetch(jsonResponse({ error: 'not found' }, 404), jsonResponse(fixture));
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByRole('button', { name: 'Get Books' }));

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('The book service returned an error.');
    expect(fetchMock).toHaveBeenCalledTimes(1); // 404 is not retried

    await user.click(within(alert).getByRole('button', { name: 'Try again' }));

    expect(await screen.findByRole('heading', { name: 'Books owned by Adults' })).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('retries transient failures automatically before showing data', async () => {
    const fetchMock = mockFetch(jsonResponse({}, 503), jsonResponse(fixture));
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByRole('button', { name: 'Get Books' }));

    expect(
      await screen.findByRole('heading', { name: 'Books owned by Adults' }, { timeout: 3000 }),
    ).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});
