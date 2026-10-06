# ADR-0003: Validate API responses at runtime, tolerating bad records

- **Status:** Accepted
- **Date:** 2026-10-06

## Context

The Swagger document for `/api/v1/bookowners` declares only `200: Success`, with no response schema. A TypeScript interface would therefore be an unchecked assumption: if the shape drifts, the UI could crash or silently show wrong groupings (for example, `age: "17"` compared as a string).

## Decision

- Parse the payload with zod at the boundary (`api/schema.ts`), then map it to separate domain types.
- **The whole payload is not an array** → contract error. Show a friendly message and log it.
- **An individual record is invalid** (missing name, negative or non-integer age, wrong types) → skip it and log a structured warning with the index and issues. Do not include personal data in the log.
- `books: null` or a missing `books` field → treat as an empty list.
- An unknown book `type` → map to `other`.

## Consequences

- One bad record cannot blank the page for every user. Partial data is preferred, and the drift is visible in the logs.
- The trade-off: a user could see an incomplete list without being told. In production, a `recordSkipped` alert threshold would bring this to the team's attention quickly.
