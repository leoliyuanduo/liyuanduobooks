# Power Platform custom connector

The BFF's contract is published as OpenAPI, so the same API can be used from the
Power Platform without new backend code. This folder is a ready-to-import **custom
connector** definition for `GET /api/v1/books`:

| File                         | Purpose                                                                                                                                                          |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `apiDefinition.swagger.json` | The operation, its parameters (with allowed values, so makers get dropdowns, not free text) and response schema, in the Swagger 2.0 format custom connectors use |
| `apiProperties.json`         | Connector properties: no authentication (public, read-only data), brand colour `#0079c8`                                                                         |
| `check-sync.mjs`             | Fails CI if the connector drifts from the API's OpenAPI document                                                                                                 |

## Why this matters

- **One contract, many clients.** The React app, a Canvas or Code App, a Power Automate flow and a Copilot Studio agent all call the same rules (18+ = adult, alphabetical, hardcover filter) in one place. Nothing is re-implemented in low-code.
- **The guardrails carry over.** Rate limiting, caching, retries and the last-known-good fallback protect low-code consumers too.
- **Agent-ready.** In Copilot Studio, add the connector's `GetBooks` action as a tool. Its summaries and parameter descriptions are what the agent reads to decide when and how to call it ("Which hardcover books do the children own?" → `ageCategory=child, format=hardcover`).

## Import

1. Deploy the BFF (see `infra/README.md`) and replace `host` in `apiDefinition.swagger.json` with the app's host name.
2. With the [Power Platform CLI](https://learn.microsoft.com/power-platform/developer/cli/introduction):

   ```bash
   pac auth create --environment https://<your-env>.crm6.dynamics.com
   pac connector create \
     --api-definition-file integrations/power-platform/apiDefinition.swagger.json \
     --api-properties-file integrations/power-platform/apiProperties.json
   ```

   Or in the maker portal: **Custom connectors → New custom connector → Import an OpenAPI file**.

3. Add the connector to a solution so it moves between environments with the rest of the app (ALM), and use a connection reference rather than a hard-coded connection.

## Production notes

- If the API ever requires sign-in, switch `connectionParameters` to OAuth 2.0 (Entra ID) and protect the endpoint with `.RequireAuthorization()`. The connector then calls on behalf of the signed-in maker or user.
- DLP: classify the connector in the environment's data loss prevention policy (for example, _Business_) before makers can use it.
