# ADR-0005: BFF structure: three projects, clean architecture

- **Status:** Accepted
- **Date:** 2026-10-06

## Context

The BFF has one read-only use case, one upstream dependency and no database. We want the benefits of clean architecture (testable business rules, a replaceable upstream, clear boundaries) without ceremony that the problem does not justify.

## Decision

Three projects, dependencies pointing inward:

```
BupaBooks.Host ─────────► BupaBooks.Application ◄───── BupaBooks.Infrastructure
     └──── DI registration only (Program.cs) ─────────────────────▲
```

- **Application** holds the domain model in a `Domain/` folder (Owner, OwnedBook, Age, AgeCategory, BookFormat, BookSelection, BookOrdering), the use case (`GetBooksHandler`), paging, and the port `IBookOwnerSource`. It has no project references.
- **Infrastructure** implements the port: typed `HttpClient` + standard resilience pipeline (retry, timeouts, circuit breaker), `HybridCache` (stampede protection), and the anti-corruption layer that maps Bupa's JSON (`BupaBookOwnerDto`) to the domain.
- **Host** is the composition root and HTTP surface: endpoint, request validation, problem details, ETags, OpenAPI, health checks, OpenTelemetry.

### Why the domain model is not in Infrastructure

Infrastructure implements a port that returns `Owner`. If `Owner` lived in Infrastructure, Application would need to reference Infrastructure while Infrastructure references Application: a circular reference. Breaking it by moving the port into Infrastructure would couple business rules to the upstream JSON shape. Infrastructure _produces_ domain objects; it does not _define_ them (the same way an EF Core DbContext materialises entities it does not own).

### Why no separate Domain project

One use case does not justify a fourth project. The trade-off is that the compiler no longer stops `Domain/` from depending on the rest of Application; **architecture tests** (NetArchTest) enforce it instead, together with "only Program.cs may use Infrastructure".

### Deliberately not used

- **Aggregates / repositories / domain events:** read-only, no persistence, no invariants spanning entities.
- **MediatR:** a plain handler class is clearer for one query (and MediatR is now commercially licensed).
- **AutoMapper:** explicit mapping is easier to read and debug.

## Consequences

- Business rules are unit-tested without HTTP; the HTTP pipeline is tested with `WebApplicationFactory` and a fake port; the upstream adapter is tested with a stub `HttpMessageHandler`.
- If a second use case or a database appears, splitting `Domain/` into its own project is mechanical.
