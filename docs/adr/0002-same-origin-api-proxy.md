# ADR-0002: Call the API through a same-origin `/api` path

- **Status:** Accepted, amended 2026-10-07: `/api` now goes to our own BFF (`api/`), not to the Bupa API (see ADR-0004)
- **Date:** 2026-10-06

## Context

The upstream API is on a different origin, and its responses do not advertise CORS headers we can depend on. In production, an enterprise app would also need auth, rate limiting and caching in front of third-party or internal services.

## Decision

The browser only calls `/api/...` on its own origin. Vite's dev and preview servers proxy this to `API_PROXY_TARGET`. In production, the hosting layer (Azure Static Web Apps linked API, Front Door, APIM, or a small BFF) does the same.

## Consequences

- No CORS dependency, and the upstream URL is not baked into the bundle.
- A single place to add OAuth2/OIDC token exchange, response caching, a circuit breaker and request logging.
- Local development needs the proxy, which `npm run dev` already includes.
