# Owners and Books: Bupa coding challenge

A React app with its own server. The **server** (an ASP.NET Core BFF) fetches the Bupa book-owner API and applies the business rules. The **client** (React + TypeScript) fetches from that server and shows every book alphabetically under the age category of its owner (**Adults** 18+, **Children** 17 and under), with a **Hardcover only** view.

| Desktop (≥ 600px)                                         | Mobile (< 600px)                                                   |
| --------------------------------------------------------- | ------------------------------------------------------------------ |
| ![Desktop, all books](docs/screenshots/desktop-1-all.png) | ![Mobile, hardcover only](docs/screenshots/mobile-2-hardcover.png) |

## Reviewer's guide (5 minutes)

1. **Run it:** `docker compose up --build`, then open http://localhost:8080. One image serves the React app and the API.
2. **Requirements → code:** see [the table below](#how-the-requirements-map-to-the-code). Every requirement points at the code and the test that proves it.
3. **The business rules:** [`Age.cs`](api/src/BupaBooks.Application/Domain/Age.cs) (18+), [`BookSelection.cs`](api/src/BupaBooks.Application/Domain/BookSelection.cs) and [`BookOrdering.cs`](api/src/BupaBooks.Application/Domain/BookOrdering.cs). Pure functions, unit-tested at the 17/18 boundary against the live data.
4. **The interesting engineering is at the edges:**
   - The Bupa API is rate-limited and intermittently returns empty `200`s. See [`DependencyInjection.cs`](api/src/BupaBooks.Infrastructure/DependencyInjection.cs) (retry, bulkhead, circuit breaker), [`EmptyResponseGuardHandler.cs`](api/src/BupaBooks.Infrastructure/BupaApi/EmptyResponseGuardHandler.cs) and [`BupaBookOwnerSource.cs`](api/src/BupaBooks.Infrastructure/BupaBookOwnerSource.cs) (cache + last-known-good fallback).
   - Inbound protection is in [`Program.cs`](api/src/BupaBooks.Host/Program.cs).
5. **One contract, two languages:** the server generates [`openapi/bupa-books.json`](api/src/BupaBooks.Host/openapi/bupa-books.json) at build time. The client generates [`schema.d.ts`](web/src/api/schema.d.ts) from it, and CI fails if either drifts.
6. **Quality gates:** [what runs where](#quality-gates). Unit, integration, architecture, contract and end-to-end tests (with axe accessibility checks), plus scanning.
7. **Delivery:** the [Dockerfile](Dockerfile), [workflows](.github/workflows) and [k6 load test](tests/load/books.js).
8. **Why it is built this way:** eight short [ADRs](#key-decisions).
9. **How AI was used:** [`docs/ai-assisted-development.md`](docs/ai-assisted-development.md) (decisions I made, AI mistakes and what caught them) and [`AGENTS.md`](AGENTS.md) (the rules AI agents follow in this repo).

**Deliberately not built** (and why): see [Not built, and why](#not-built-and-why).

## Quick start

### One command (Docker)

```bash
docker compose up --build        # http://localhost:8080
```

The image is distroless (chiseled Ubuntu), runs as non-root on a read-only filesystem, and calls the real Bupa API. For a fully offline, deterministic stack (Bupa replaced by a stub serving the captured live data):

```bash
docker compose -f compose.yaml -f tests/compose.e2e.yaml up --build
```

### Local development

Requires the **.NET 10 SDK** and **Node.js 20.19+**:

```bash
cd api && dotnet run --project src/BupaBooks.Host     # http://localhost:5080 (API reference at /scalar/v1)
cd web && npm ci && npm run dev                       # http://localhost:5173, proxies /api to the server
```

### Checks

```bash
cd api
dotnet build                                    # warnings and vulnerable packages fail the build; regenerates the OpenAPI document
dotnet test --filter "Category!=LiveContract"   # unit, adapter, integration and architecture tests (offline)
dotnet test --filter "Category=LiveContract"    # checks the real Bupa API still matches our expectations

cd web
npm run ci                                      # API types in sync, format, lint, typecheck, tests + coverage gate, build
npm run e2e                                     # Playwright against http://localhost:8080 (start the e2e compose stack first)
```

## How the requirements map to the code

| Requirement                                       | Where it lives                                                                                    | Verified by                                      |
| ------------------------------------------------- | ------------------------------------------------------------------------------------------------- | ------------------------------------------------ |
| Server side that returns the data                 | `api/` → `GET /api/v1/books?ageCategory=&format=` ([ADR-0004](docs/adr/0004-bff-api-contract.md)) | `Host.IntegrationTests`, E2E                     |
| Client side that fetches from our server          | `web/src/features/books/api/booksApi.ts` (same-origin `/api`)                                     | `booksApi.test.ts`, `App.test.tsx`, E2E          |
| Must be a React app                               | `web/`                                                                                            | n/a                                              |
| Adults ≥ 18, Children ≤ 17                        | `Application/Domain/Age.cs` (`AdultMinimumAge`)                                                   | `AgeTests` (17 and 18 boundaries)                |
| All books, alphabetical, under their age category | `Application/Domain/BookSelection.cs`, `BookOrdering.cs`                                          | `BookSelectionTests` (live data), E2E            |
| "Get Books" / "Hardcover only"                    | `web/.../BookControls.tsx` → `?format=hardcover` on the server                                    | `App.test.tsx`, `BookSelectionTests`, E2E        |
| Dynamic headings                                  | `web/src/features/books/headings.ts`                                                              | `headings.test.ts`, `App.test.tsx`, E2E          |
| 600px breakpoint, `#0079c8`, reference layout     | `web/src/index.css` (mobile-first, `@media (min-width: 600px)`)                                   | E2E layout tests on desktop and mobile viewports |

## Architecture

```
Browser ──► one container ─────────────────────────────────────────► Bupa API
            ├─ React build (wwwroot)                                  rate-limited, intermittently
            └─ BFF: rate limit → validation → cache → retry/breaker   returns empty responses
```

### Server (`api/`): clean architecture in three projects ([ADR-0005](docs/adr/0005-bff-clean-architecture.md))

```
BupaBooks.Host ─────────► BupaBooks.Application ◄───── BupaBooks.Infrastructure
(endpoint, validation,     (Domain/ model + rules,       (typed HttpClient, resilience,
 problem details, ETag,     GetBooks use case,            HybridCache, anti-corruption
 rate limit, OpenAPI)       paging, IBookOwnerSource)     mapping of Bupa's JSON)
```

- **The server owns every business rule.** The client sends intent (`ageCategory=adult&format=hardcover`); it never sees an age.
- **No personal data leaves the server.** Responses contain `{ id, title, format }` only.
- **No cascading failure** ([ADR-0006](docs/adr/0006-upstream-behaviour.md), [ADR-0007](docs/adr/0007-edge-protection-and-cascading-failure.md)):
  - retries with backoff and jitter (including for Bupa's empty `200`s);
  - a bulkhead of 10 concurrent upstream calls, and a circuit breaker tuned to actually trip at this traffic;
  - a 5-minute cache with stampede protection;
  - a last-known-good fallback that serves data up to an hour old during an outage.
- **Edge protection:**
  - a per-client-IP rate limit that only trusts forwarded headers from known proxies;
  - a 15 s request timeout;
  - strict CSP and security headers, and no `Server` header;
  - HSTS outside development, and Brotli/gzip compression.
- **Boundaries are enforced by tests**: `ArchitectureTests` fail the build if the domain depends on anything else, or if Host code (other than `Program.cs`) uses Infrastructure.

### Client (`web/`)

```
web/src/
├── api/schema.d.ts           Generated from the server's OpenAPI document (npm run generate:api)
├── lib/http.ts               fetch wrapper: timeout, typed ApiError, Retry-After, correlation id
└── features/books/
    ├── model.ts              Types derived from the generated contract
    ├── headings.ts           "Hardcover Books owned by Adults" (display text, not a business rule)
    ├── api/booksApi.ts       GET /api/v1/books, follows paging cursors
    ├── hooks/useBooks.ts     TanStack Query: one query per category, caching, retries
    ├── hooks/useViewParam.ts The selected view lives in the URL (?view=hardcover): shareable, Back works
    └── components/           BooksPage (container), BookCard, BookControls
```

- **Retry only what is transient.** The client retries `503`/`504`/`429`/network errors, honouring `Retry-After`. It does **not** retry `502`, because the server has already retried the upstream.

## Quality gates

| Gate                                                                     | Where                                 | Runs             |
| ------------------------------------------------------------------------ | ------------------------------------- | ---------------- |
| C# warnings as errors, nullable, analyzers, NuGet vulnerability audit    | `api-ci.yml`                          | every PR         |
| Unit, adapter, integration (`WebApplicationFactory`), architecture tests | `api-ci.yml`                          | every PR         |
| OpenAPI drift, client type drift                                         | `api-ci.yml`, `web-ci.yml`            | every PR         |
| TypeScript strict, lint (0 warnings), Prettier, 90%/85% coverage gate    | `web-ci.yml`                          | every PR         |
| E2E on the real image: desktop + mobile, axe WCAG 2.2 AA                 | `container.yml`                       | every PR         |
| Image scan (Trivy, HIGH/CRITICAL), SBOM                                  | `container.yml`                       | every PR         |
| CodeQL (C#, TypeScript), dependency review (vulnerabilities, licences)   | `codeql.yml`, `dependency-review.yml` | every PR, weekly |
| Live upstream contract check                                             | `upstream-contract.yml`               | daily            |
| Load test (p95 < 250 ms, < 1% errors)                                    | `load-test.yml`                       | on demand        |

All GitHub Actions are **pinned to commit SHAs**; Dependabot keeps actions, NuGet, npm and Docker base images current.

## Delivery

- **Image:** multi-stage [Dockerfile](Dockerfile). Node builds the client, the .NET SDK publishes the server, and the runtime is `aspnet:10.0-noble-chiseled-extra` (distroless, non-root, with ICU for `en-AU` sorting).
- **Pipeline** ([ADR-0008](docs/adr/0008-delivery-pipeline.md)): PR checks → on `main`, the exact image that passed E2E and scanning is pushed to GitHub Container Registry, tagged by commit SHA, with provenance and an SBOM. Any container host can run it: it listens on 8080 and exposes `/health/live` and `/health/ready` probes.
- **Load test:** [`tests/load/books.js`](tests/load/books.js) (k6, 50 virtual users) runs against the real image with a stubbed upstream; results are published to the job summary.

## Key decisions

| ADR                                                            | Decision                                                                          |
| -------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| [0001](docs/adr/0001-technology-choice.md)                     | React + TypeScript + Vite client                                                  |
| [0002](docs/adr/0002-same-origin-api-proxy.md)                 | Browser only calls same-origin `/api` (served by our BFF)                         |
| [0003](docs/adr/0003-runtime-validation.md)                    | Validate untrusted upstream data, skip bad records (in the BFF)                   |
| [0004](docs/adr/0004-bff-api-contract.md)                      | API contract: filterable, paged books collection; no owner data; id assumption    |
| [0005](docs/adr/0005-bff-clean-architecture.md)                | Three-project clean architecture; domain in Application, enforced by tests        |
| [0006](docs/adr/0006-upstream-behaviour.md)                    | Upstream is rate-limited and intermittently returns empty bodies: retry and cache |
| [0007](docs/adr/0007-edge-protection-and-cascading-failure.md) | Per-IP rate limit, security headers, bulkhead, circuit breaker, stale fallback    |
| [0008](docs/adr/0008-delivery-pipeline.md)                     | One distroless image; the tested image is the shipped image, tagged by commit SHA |

## Assumptions

**Scope and data**

- **Server and client.** The brief requires "a server side, to return data similar to the above, and a client side, to fetch the data from your server". We read "data similar to the above" as the grouped output: `GET /api/v1/books?ageCategory=…` returns one heading's books, already filtered and sorted.
- **Upstream:** `GET https://digitalcodingtest.bupa.com.au/api/v1/bookowners`, unauthenticated, returning `[{ name, age, books: [{ name, type }] }]`. Its Swagger document declares no response schema, so the server validates every record.
- **Identifiers.** The brief says the data is _"similar to"_ the structure shown, so we assume production data carries stable ids. The test endpoint sends none, so the server derives deterministic ids. Clients treat `id` as opaque.
- **Access:** the book list is public, read-only data, so no sign-in. Owner names and ages are still treated as personal information and never leave the server.

**Business rules**

- **Age:** whole years as supplied (no date-of-birth arithmetic). 18 and over is an Adult; 17 and under is a Child. Records with a missing or negative age, a non-numeric age, or a missing name are skipped and logged (without personal data).
- **"Alphabetical":** `en-AU`, case-insensitive, numeric-aware ("Book 2" before "Book 10"), ties broken by id. Leading articles are **not** ignored ("The Hobbit" sorts under T). Titles are trimmed. The upstream order carries no meaning.
- **Duplicates:** every owned copy is listed. Two adults who both own _Hamlet_ produce two entries.
- **Book types** match case-insensitively. "Hardcover" means the upstream type `Hardcover` (not synonyms such as "Hardback"). Unknown types become `other`: shown under "Get Books", never under "Hardcover only".

**Behaviour and quality targets**

- **Fetching on demand:** nothing loads until the user clicks a button or opens a link to a view (`?view=hardcover`).
- **Freshness:** data can be up to 5 minutes old (cache), or up to 1 hour during an upstream outage. Acceptable for a book list; both values are configuration.
- **Scale:** single region, modest traffic. The rate limits are starting values to tune against real traffic.
- **Browsers:** current evergreen browsers (last two versions). **Accessibility:** WCAG 2.2 AA. **Language:** English only.
- **Deployment:** behind a TLS-terminating reverse proxy, configured as trusted (`ForwardedHeaders` settings) so the rate limit sees real client IPs.

## Not built, and why

| Not built                          | Why / what it would take                                                                                                          |
| ---------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| Authentication (OAuth2/OIDC)       | The data is public and read-only. The endpoint group is ready for `.RequireAuthorization()` with Entra ID.                        |
| Distributed cache / rate limiting  | One replica is enough here. With more: Redis behind `HybridCache` (one line), rate limiting at Front Door/APIM.                   |
| Cloud deployment (IaC + CD)        | Out of scope for a one-screen challenge; the image is ready for any host (e.g. Azure Container Apps via Terraform). See ADR-0008. |
| Application Insights wiring        | OpenTelemetry is in place and exports over OTLP when configured; structured JSON logs go to stdout.                               |
| Mutation testing, visual snapshots | Useful at team scale (Stryker.NET / StrykerJS, Playwright snapshots); low value for one screen.                                   |
| Multi-region, Kubernetes           | Over-engineering for one read-only screen; a managed container host covers scaling.                                               |

## Use of AI tooling

Built with an AI coding assistant, under the same rules I would apply in a team. Full account: [`docs/ai-assisted-development.md`](docs/ai-assisted-development.md); agent rules: [`AGENTS.md`](AGENTS.md).

- **Grounded in evidence:** the live API, its Swagger document and the challenge page were inspected before designing. Library APIs and container tags were checked against their sources, not assumed.
- **Constrained by guardrails:** strict compilers, vulnerability audits, architecture tests, contract drift checks, E2E and accessibility tests on every PR. These gates caught real AI mistakes: vulnerable package versions, lenient JSON number parsing, a deprecated .NET API.
- **Reviewed by a person:** I made the design decisions, built and tested every change on my machine, and can explain every line. I am accountable for everything committed under my name.
