import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useState } from 'react';
import { ErrorBoundary } from './components/ErrorBoundary';
import { BooksPage } from './features/books/components/BooksPage';

export function App() {
  // One client per app instance (created lazily so tests get a fresh one per render).
  const [queryClient] = useState(() => new QueryClient());

  return (
    <QueryClientProvider client={queryClient}>
      <header className="app-header">
        <div className="app-header__inner">
          <h1 className="app-header__title">Owners and Books</h1>
        </div>
      </header>
      <main className="app-main">
        <ErrorBoundary>
          <BooksPage />
        </ErrorBoundary>
      </main>
    </QueryClientProvider>
  );
}
