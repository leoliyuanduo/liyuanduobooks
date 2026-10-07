# ADR-0006: Design for a rate-limited, intermittently failing upstream

- **Status:** Accepted
- **Date:** 2026-10-07

## Context

Probing `GET /api/v1/bookowners` on 7 Oct 2026, with calls spaced about 2 seconds apart:

1. **Rate limiting:** three rapid calls returned `429` with the body `"Too many requests"`.
2. **Empty success responses:** roughly 1 call in 6 returned `200 OK` with an **empty body**.

A retry policy that only looks at status codes treats (2) as success. The JSON parse then fails, and the user sees an error for a failure that a second attempt would have fixed.

## Decision

- **`EmptyResponseGuardHandler`** sits _inside_ the standard resilience pipeline (closest to the network). It turns an empty `2xx` body into a transient `HttpRequestException`, which the pipeline retries like a `5xx`.
- **Retry:** up to 3 retries, exponential backoff from 500 ms with jitter, within a 10 s total budget (3 s per attempt). `429` responses are retried, honouring `Retry-After` when upstream sends it.
- **Cache:** validated upstream data is cached for 5 minutes in `HybridCache`. Concurrent requests on a cold cache share **one** upstream call (stampede protection), which keeps us well under the rate limit. Failures are never cached.
- **Client:** the React app does not retry `502` (the server already retried); it retries `503`/`504` and honours `Retry-After`. Its request timeout (15 s) exceeds the server's upstream budget, so the server always gets to answer with a proper error first.

## Consequences

- Users rarely see upstream flakiness: after the first successful call, requests are served from cache.
- Data can be up to 5 minutes stale, which is acceptable for a book list (tunable via `BupaApi:CacheDuration`).
- Both behaviours are covered by tests (`Empty_upstream_responses_are_retried`, `Rate_limited_responses_are_retried`), and the scheduled live contract test exercises the real endpoint.
