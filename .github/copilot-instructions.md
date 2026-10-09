# Copilot instructions

The full rules for AI agents are in [`AGENTS.md`](../AGENTS.md) at the repository root. Read it first. In short:

- Business rules (18+ = adult, alphabetical order, hardcover filter) live only in `api/src/BupaBooks.Application/Domain/`. The React client sends intent and renders results.
- Never edit generated files: `api/src/BupaBooks.Host/openapi/bupa-books.json` and `web/src/api/schema.d.ts`. Change the C# contract, run `dotnet build`, then `npm run generate:api`.
- Owner names and ages are personal information: never log, return or store them outside the server.
- Do not add dependencies, weaken quality gates, or call the live Bupa API in tests without explicit approval.
- Before saying a change is done, run `dotnet build && dotnet test --filter "Category!=LiveContract"` in `api/` and `npm run ci` in `web/`.
