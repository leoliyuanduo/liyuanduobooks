# ADR-0008: Delivery: one image, tested once, deployed by SHA

- **Status:** Accepted
- **Date:** 2026-10-09

## Context

The role is measured on delivery performance (DORA metrics). Reviewers also need to run the solution without installing .NET and Node.

## Decision

- **One image** (`Dockerfile`): the BFF serves the React build. One artefact, one origin, one thing to deploy and roll back.
- **Distroless runtime** (`aspnet:10.0-noble-chiseled-extra`): no shell or package manager, non-root by default. `-extra` keeps ICU for culture-aware sorting. Compose adds a read-only filesystem, no Linux capabilities and `no-new-privileges`.
- **The tested image is the shipped image.** `container.yml` builds the image, runs Playwright E2E (desktop, mobile, axe) against it with a stubbed upstream, and scans it (Trivy) before anything is published. On `main` it is pushed to GHCR, tagged `sha-<commit>`, with provenance and an SBOM.
- **Deployment by immutable tag.** `deploy.yml` applies Terraform with `container_image = …:sha-<commit>`; `:latest` is rejected. Rollback means redeploying a previous SHA.
- **No stored cloud secrets.** GitHub OIDC is federated to an Entra ID app that is trusted only for this repository's `production` environment. That environment has required reviewers (the approval gate), and the identity has Contributor on one resource group only.
- **Supply chain.** Actions are pinned to commit SHAs; Dependabot covers actions, NuGet, npm, Docker and Terraform; CodeQL and dependency review run on every PR.

## Consequences

- **Lead time:** merge to running in production is one pipeline run plus an approval click. **Change failure rate** is reduced by E2E on the exact image and a post-deploy smoke test. **MTTR:** redeploy the previous SHA.
- The deploy workflow is inert until Azure is configured (`infra/README.md`), so the repository stays green for reviewers without a subscription.
