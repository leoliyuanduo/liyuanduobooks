# ADR-0001: React + TypeScript (Vite) for the client

- **Status:** Accepted
- **Date:** 2026-10-06

## Context

The brief allows any technology, including Power Platform. Reviewers want to see understanding of the problem, sound engineering choices and production quality. The role emphasises strictly typed languages, automated testing, Git workflows and CI/CD.

## Options considered

| Option                         | Pros                                                                                                  | Cons                                                                     |
| ------------------------------ | ----------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| **React + TypeScript + Vite**  | Strict typing; mature testing (Vitest, Testing Library); trivial CI; easy for anyone to clone and run | Needs a proxy/BFF for CORS and auth in production                        |
| ASP.NET Core BFF + TS UI       | Server-side caching, retries and auth; shows C#                                                       | Two deployables; more work than this scope justifies                     |
| Power Apps (Canvas / Code App) | Fast to build; fits the Dataverse ecosystem                                                           | Harder to review in Git, unit test and gate in CI; needs a tenant to run |
| Blazor WASM                    | One language (C#)                                                                                     | Larger payload; smaller frontend ecosystem                               |

## Decision

React 19 + TypeScript (strict) + Vite, with TanStack Query for server state and zod for runtime validation.

## Consequences

- Reviewers can run it with `npm ci && npm run dev`, with no tenant or licence needed.
- The same-origin proxy (ADR-0002) is the seam where a C# BFF or APIM policy can be added later without changing the UI.
