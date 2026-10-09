# Security policy

## Reporting a vulnerability

Please report security issues privately through **GitHub → Security → Report a vulnerability** (private vulnerability reporting), not in a public issue. Include steps to reproduce and the impact you observed. You will get an acknowledgement within two business days.

## What is in place

- **Code:** CodeQL (C#, TypeScript) on every pull request and weekly; strict compilers; no secrets in the repository.
- **Dependencies:** NuGet audit fails the build on known vulnerabilities; `npm audit` and dependency review on pull requests; Dependabot for every ecosystem.
- **Image:** distroless, non-root runtime; Trivy scan blocks HIGH/CRITICAL fixable vulnerabilities; SBOM and provenance published with each image.
- **Runtime:** per-client rate limiting, strict Content Security Policy and security headers, HTTPS only (HSTS), request timeouts, RFC 9457 errors that never expose internal details.
- **Cloud access:** GitHub OIDC to Entra ID; least-privilege role on a single resource group; approval gate on production.
- **Data:** owner names and ages are treated as personal information. They never leave the server and are never logged.
