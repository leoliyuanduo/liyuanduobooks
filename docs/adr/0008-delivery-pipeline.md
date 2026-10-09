# ADR-0008: Delivery: one image, tested once, published by SHA

- **Status:** Accepted
- **Date:** 2026-10-09

## Context

The role is measured on delivery performance (DORA metrics). Reviewers also need to run the solution without installing .NET and Node.

## Decision

- **One image** (`Dockerfile`): the BFF serves the React build. One artefact, one origin, one thing to deploy and roll back.
- **Distroless runtime** (`aspnet:10.0-noble-chiseled-extra`): no shell or package manager, non-root by default. `-extra` keeps ICU for culture-aware sorting. Compose adds a read-only filesystem, no Linux capabilities and `no-new-privileges`.
- **The tested image is the shipped image.** `container.yml` builds the image, runs Playwright E2E (desktop, mobile, axe) against it with a stubbed upstream, and scans it (Trivy) before anything is published. On `main` it is pushed to GitHub Container Registry, tagged `sha-<commit>`, with provenance and an SBOM.
- **Supply chain.** Actions are pinned to commit SHAs; Dependabot covers actions, NuGet, npm and Docker; CodeQL and dependency review run on every PR.
- **Deployment target deliberately out of scope.** Infrastructure as code (for example Terraform for Azure Container Apps) and a deploy workflow were prototyped and then removed: for a one-screen challenge, untested-by-reviewers cloud plumbing adds more to explain than it proves. The image is ready for any container host; the app already exposes `/health/live` and `/health/ready` probes and honours forwarded headers from a trusted proxy.

## Consequences

- **Lead time** and **change failure rate** are supported by fast PR feedback, E2E on the exact image, and immutable SHA-tagged artefacts. A deploy step would reference `sha-<commit>`, never `latest`, so rollback means redeploying a previous SHA.
- Reviewers can run everything locally with `docker compose up --build`; no cloud account is needed.
