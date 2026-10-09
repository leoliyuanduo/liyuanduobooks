# Instructions for AI coding agents

This file tells AI agents (Copilot, Claude, Cursor, Codex and others) how to work in this repository. The same rules apply to humans; agents just need them written down. If an instruction here conflicts with a request, stop and ask.

## What this is

A React app with its own server. The **server** (`api/`, ASP.NET Core .NET 10 BFF) fetches the Bupa book-owner API and applies the business rules. The **client** (`web/`, React 19 + TypeScript) shows books grouped by owner age category. Read `README.md` and `docs/adr/` before changing design.

## Commands (run before you say "done")

```bash
cd api && dotnet build && dotnet test --filter "Category!=LiveContract"
cd web && npm run ci              # API types in sync, format, lint, typecheck, tests + coverage, build
```

E2E (needs Docker): `docker compose -f compose.yaml -f tests/compose.e2e.yaml up --build -d`, then `cd web && npm run e2e`.

Never claim a change works without running these. If you could not run them, say so explicitly.

## Architecture rules (enforced by `api/tests/BupaBooks.ArchitectureTests`)

- `BupaBooks.Application/Domain/` depends on nothing outside itself. Business rules (18+ = adult, sorting, filtering) live **only** here.
- `BupaBooks.Application` must not reference Infrastructure, Host, ASP.NET Core, `System.Net.Http`, `System.Text.Json` or Polly.
- Only `Program.cs` (the composition root) may use `BupaBooks.Infrastructure`. Endpoints talk to Application.
- Bupa's JSON shape stays in `Infrastructure/BupaApi` (anti-corruption layer). Never pass upstream DTOs outward.
- The React client contains **no business rules**: it sends intent (`ageCategory`, `format`) and renders what the server returns.

## The API contract

- `api/src/BupaBooks.Host/openapi/bupa-books.json` and `web/src/api/schema.d.ts` are **generated**. Never edit them by hand.
- Change the contract in C# (`Host/Contracts`, `Host/ApiDocs`), run `dotnet build`, then `npm run generate:api` in `web/`, and commit both files. CI fails if either is stale.
- Changes must be additive within `/api/v1` (new optional fields or parameters). Anything breaking needs a new version and an ADR.

## Conventions

- **C#:** nullable on, warnings are errors, file-scoped namespaces, `sealed` by default, value objects for domain primitives, `[LoggerMessage]` source-generated logging, `ConfigureAwait(false)` outside Host. No MediatR, no AutoMapper.
- **TypeScript:** `strict` + `exactOptionalPropertyTypes` + `noUncheckedIndexedAccess`. No `any`, no non-null assertions in app code, types come from the generated schema.
- **Errors:** RFC 9457 problem details. Upstream failures become `BookSourceException` in Infrastructure and are mapped to 502/503/504 in Host.
- **Tests:** every behaviour change ships with a test at the lowest level that proves it (domain unit test → adapter test → integration test → E2E). Bug fixes start with a failing test.
- **Commits:** Conventional Commits (`feat(api): …`). One logical change per commit.

## Guardrails: never do these without explicit approval

- Add a dependency. Check maintenance, licence and known vulnerabilities first, and pin the version in `Directory.Packages.props` or `package.json`.
- Weaken a gate: lower coverage thresholds, suppress analyzer warnings, skip or delete tests, add `--legacy-peer-deps`, or unpin a GitHub Action from its commit SHA.
- Log, return or store owner names or ages. They are personal information and never leave the server.
- Put secrets, tokens or connection strings in the repository.
- Call the live Bupa API in normal tests. It is rate-limited and intermittently returns empty responses; use the fixtures in `api/tests/TestData` or the stub in `tests/upstream-stub`.

## When unsure

- Verify library APIs against their source or docs; do not guess names or versions. This repo has caught deprecated APIs and vulnerable versions that way.
- Prefer the smallest change that satisfies the requirement, and explain trade-offs in the PR description (or a new ADR for design changes).
