# ADR-0004: BFF API contract: a filterable, paged books collection

- **Status:** Accepted
- **Date:** 2026-10-06

## Context

The React client originally fetched the upstream owners payload and applied the business rules itself (18+ = adult, filter, sort). We are introducing an ASP.NET Core BFF so that business rules live in one tested place, and need a contract between the BFF and the client.

Options considered:

| Option                          | Shape                                        | Verdict                                                                                                                                                             |
| ------------------------------- | -------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Owner-centric resource          | `owners[{ ownerId, name, age, books[] }]`    | Rejected: pushes grouping and sorting back to the client, exposes personal data the screen does not show, and paging owners breaks alphabetical order across pages. |
| Screen-shaped grouped view      | `groups[{ category, books[] }]`              | Viable, but awkward to page (per group) and tied to one screen.                                                                                                     |
| **Filterable books collection** | `GET /books?ageCategory&format&limit&cursor` | **Chosen.**                                                                                                                                                         |

## Decision

```
GET /api/v1/books?ageCategory=adult|child   (required)
                 &format=hardcover|paperback|ebook   (optional, omit = all)
                 &limit=1..100   (optional, default 50)
                 &cursor=<opaque>   (optional, from page.nextCursor)

200 OK   ETag, Cache-Control: private, max-age=60
{ "items": [ { "id": "…", "title": "…", "format": "hardcover" } ],
  "page":  { "limit": 50, "nextCursor": null, "totalCount": 4 } }
```

- **The client states intent; the server applies every rule** (age classification, filtering, sorting). React never sees an age.
- **No owner data in responses**: data minimisation (Australian Privacy Principles). Owners exist only inside the BFF.
- **Sorting:** title, `en-AU`, case-insensitive, numeric-aware; ties broken by `id` so paging is deterministic.
- **Each item is one owned copy.** Two owners of _Hamlet_ yield two items with different ids.
- **Paging:** applied after filtering and sorting. The cursor is opaque (currently an encoded offset) so it can become a keyset cursor without a breaking change.
- **Errors:** RFC 9457 `application/problem+json` with `traceId`. `400` validation (all errors at once), `502` upstream error or invalid payload, `503` + `Retry-After` circuit open, `504` upstream timeout.
- **Versioning:** URL segment `/v1`. Additive changes (new optional fields or parameters) stay in v1.

## Assumption: identifiers

The brief describes the data as _"similar to"_ the structure shown, so we assume the production API exposes stable ids for owners and books. The test endpoint (verified 6 Oct 2026) returns none. The anti-corruption layer therefore accepts optional upstream ids (string or number) and, when absent, derives a deterministic id from the record's content and position. Derived ids are stable only while the upstream data is unchanged. Clients must treat `id` as an opaque string.

## Consequences

- The screen makes two parallel calls (adults, children). Both are served from the BFF's cache, so this is cheap.
- The contract is generated from code (`openapi/bupa-books.json`, committed). CI fails on drift; the client generates its types from it.
