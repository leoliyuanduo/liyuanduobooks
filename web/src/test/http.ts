import { vi } from 'vitest';

/** Builds a JSON Response the way the real API would. */
export function jsonResponse(body: unknown, status = 200, headers: Record<string, string> = {}): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': status >= 400 ? 'application/problem+json' : 'application/json', ...headers },
  });
}

/** Replaces global fetch with a mock that resolves the given responses in order. */
export function mockFetch(...responses: Array<Response | Error>) {
  const fetchMock = vi.fn<typeof fetch>();
  for (const response of responses) {
    if (response instanceof Error) fetchMock.mockRejectedValueOnce(response);
    else fetchMock.mockResolvedValueOnce(response);
  }
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

/** Replaces global fetch with a function of the request URL (a tiny fake server). */
export function mockFetchWith(handler: (url: string) => Response | Promise<Response>) {
  const fetchMock = vi.fn<typeof fetch>((input) => Promise.resolve(handler(String(input))));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

/** The URLs fetch was called with, for assertions. */
export function requestedUrls(fetchMock: ReturnType<typeof vi.fn<typeof fetch>>): string[] {
  return fetchMock.mock.calls.map(([input]) => String(input));
}
