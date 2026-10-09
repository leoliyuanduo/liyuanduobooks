# Contributing

## Workflow

1. Branch from `main`: `feat/<short-name>`, `fix/<short-name>` or `chore/<short-name>`.
2. Commit using [Conventional Commits](https://www.conventionalcommits.org/) (`feat(api): …`, `fix(web): …`). The history doubles as the changelog.
3. Open a pull request and fill in the template. CI must be green: build, tests, contract drift, E2E, scans.
4. At least one approving review, then **squash merge**. `main` is protected: no direct pushes, required checks, linear history.
5. Merging to `main` publishes an image; production deploys after an approval in the `production` environment.

## Before you push

```bash
cd api && dotnet build && dotnet test --filter "Category!=LiveContract"
cd web && npm run ci
```

If you change the API, commit the regenerated `api/src/BupaBooks.Host/openapi/bupa-books.json`, run `npm run generate:api` in `web/`, and update `integrations/power-platform/apiDefinition.swagger.json`. CI checks all three agree.

## AI-assisted changes

AI tools are welcome. The author is accountable for every line: read it, understand it, test it, and tick the checkbox in the PR template.
