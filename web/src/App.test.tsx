import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { App } from './App';
import { bffResponse } from './test/fixtures/bffBooks';
import { jsonResponse, mockFetch, mockFetchWith, requestedUrls } from './test/http';

function listItems(heading: string): string[] {
  const section = screen.getByRole('region', { name: heading });
  return within(section)
    .queryAllByRole('listitem')
    .map((item) => item.textContent ?? '');
}

/** A fake BFF: answers every request the way the real server would. */
const fakeServer = () => mockFetchWith((url) => jsonResponse(bffResponse(url)));

describe('App (integration, against a fake BFF)', () => {
  it('shows the title and does not call the server until the user asks', () => {
    const fetchMock = fakeServer();
    render(<App />);

    expect(screen.getByRole('heading', { level: 1, name: 'Owners and Books' })).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
    expect(screen.getByText(/select “get books”/i)).toBeInTheDocument();
  });

  it('"Get Books" shows all books under each age category, in server order', async () => {
    const fetchMock = fakeServer();
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

    // The client only states intent; the server decides who is an adult.
    expect(requestedUrls(fetchMock).sort()).toEqual([
      '/api/v1/books?ageCategory=adult&limit=100',
      '/api/v1/books?ageCategory=child&limit=100',
    ]);
  });

  it('"Hardcover only" asks the server for hardcovers and changes the headings', async () => {
    const fetchMock = fakeServer();
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByRole('button', { name: 'Get Books' }));
    await screen.findByRole('heading', { name: 'Books owned by Adults' });
    await user.click(screen.getByRole('button', { name: 'Hardcover only' }));

    expect(
      await screen.findByRole('heading', { name: 'Hardcover Books owned by Adults' }),
    ).toBeInTheDocument();
    expect(listItems('Hardcover Books owned by Adults')).toEqual([
      'Great Expectations',
      "Gulliver's Travels",
      'Hamlet',
      'React: The Ultimate Guide',
    ]);
    expect(listItems('Hardcover Books owned by Children')).toEqual([
      'Great Expectations',
      'Little Red Riding Hood',
    ]);
    expect(requestedUrls(fetchMock).filter((url) => url.includes('format=hardcover'))).toHaveLength(2);
  });

  it('serves a previously viewed filter from cache instead of calling the server again', async () => {
    const fetchMock = fakeServer();
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByRole('button', { name: 'Get Books' }));
    await screen.findByRole('heading', { name: 'Books owned by Adults' });
    await user.click(screen.getByRole('button', { name: 'Hardcover only' }));
    await screen.findByRole('heading', { name: 'Hardcover Books owned by Adults' });
    await user.click(screen.getByRole('button', { name: 'Get Books' }));

    expect(await screen.findByRole('heading', { name: 'Books owned by Adults' })).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(4);
  });

  it('shows an empty-state message for a category with no matching books', async () => {
    mockFetchWith((url) =>
      jsonResponse({ items: [], page: { limit: 100, nextCursor: null, totalCount: 0 }, url }),
    );
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByRole('button', { name: 'Hardcover only' }));

    const section = await screen.findByRole('region', { name: 'Hardcover Books owned by Children' });
    expect(within(section).getByText('No books to show.')).toBeInTheDocument();
  });

  it('shows a friendly error for a 502 (not retried: the server already retried) and recovers', async () => {
    let fail = true;
    const fetchMock = mockFetchWith((url) =>
      fail ? jsonResponse({ title: 'Bad gateway', status: 502 }, 502) : jsonResponse(bffResponse(url)),
    );
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByRole('button', { name: 'Get Books' }));

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('The book provider returned an error. Please try again.');
    expect(fetchMock).toHaveBeenCalledTimes(2); // one per category, no retries

    fail = false;
    await user.click(within(alert).getByRole('button', { name: 'Try again' }));

    expect(await screen.findByRole('heading', { name: 'Books owned by Adults' })).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('retries a 503 after the Retry-After the server asked for', async () => {
    const fetchMock = mockFetch(
      jsonResponse({ status: 503 }, 503, { 'Retry-After': '0' }),
      jsonResponse(bffResponse('/api/v1/books?ageCategory=child')),
      jsonResponse(bffResponse('/api/v1/books?ageCategory=adult')),
    );
    const user = userEvent.setup();
    render(<App />);

    await user.click(screen.getByRole('button', { name: 'Get Books' }));

    expect(await screen.findByRole('heading', { name: 'Books owned by Children' })).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });
});
