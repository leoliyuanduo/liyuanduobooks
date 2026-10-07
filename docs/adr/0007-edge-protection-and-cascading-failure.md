# ADR-0007: Edge protection and preventing cascading failure

- **Status:** Accepted
- **Date:** 2026-10-07

## Context

The brief does not state expected traffic. The upstream is rate-limited and flaky (ADR-0006). Without limits, one noisy client could exhaust our share of the upstream's rate limit for everyone, and a slow upstream could tie up our threads and connections until our own service fails too (a cascading failure).

## Decision

### Inbound (Host)

| Control                                                        | Setting                                                                                                                                        | Why                                                                                                                                                                   |
| -------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Per-client-IP rate limit** (token bucket) on `/api/v1/books` | Burst 20, then 10 per 10 s; `429` problem details + `Retry-After`                                                                              | Fairness and abuse protection. One screen load is 2 requests, so real users never hit it. Health checks and OpenAPI are not limited. IPv6 clients are grouped by /64. |
| **Forwarded headers** from known proxies only                  | `ForwardedHeaders:KnownProxies` (empty = trust none beyond loopback)                                                                           | The rate limit is only as good as the client IP; trusting `X-Forwarded-For` from anyone would let callers spoof a fresh bucket per request.                           |
| **Request timeout**                                            | 15 s, returns `504`                                                                                                                            | Bounds the lifetime of every request so slow work cannot pile up. Above the 10 s upstream budget, so upstream errors surface first.                                   |
| **Security headers**                                           | Strict CSP (`'self'` only), `nosniff`, `DENY` framing, `no-referrer`, COOP/CORP, no `Server` header; HSTS + HTTPS redirect outside development | Defence in depth for the served React app.                                                                                                                            |
| **Response compression**                                       | Brotli/gzip, including over HTTPS                                                                                                              | Smaller JS bundle and JSON. BREACH does not apply: no secrets or reflected input in responses. ETags are weak (`W/`) because encodings differ.                        |

### Outbound (Infrastructure)

| Control                            | Setting                                                           | Why                                                                                                                                                       |
| ---------------------------------- | ----------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Bulkhead** (concurrency limiter) | 10 concurrent upstream calls, queue 10                            | A slow upstream cannot consume every connection and thread.                                                                                               |
| **Retry**                          | 3 retries, exponential from 500 ms with jitter, within 10 s total | Rides out transient failures (5xx, 429, timeouts, empty bodies) without retry storms.                                                                     |
| **Circuit breaker**                | Opens at 50% failures over ≥ 5 calls in 30 s; open for 15 s       | Stops hammering a dead upstream and fails fast. The library default (minimum 100 calls) would never trip at our traffic.                                  |
| **Cache with stampede protection** | `HybridCache`, 5 min                                              | Most requests never reach upstream; concurrent misses share one call.                                                                                     |
| **Last-known-good fallback**       | Serve the last successful data up to 1 h old when upstream fails  | Graceful degradation: while the breaker is open, users still get (slightly old) books instead of an error. Logged as a warning (event 1005) for alerting. |

## Consequences

- Users can see data up to an hour old during an upstream outage. That is acceptable for a book list; set `BupaApi:MaxStaleness` to `00:00:00` to disable it.
- The in-process rate limiter and caches are per instance. With multiple instances, move rate limiting to the gateway (Front Door/APIM) and use Redis behind `HybridCache`.
- All limits are configuration, validated at startup.
