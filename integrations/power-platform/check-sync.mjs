// Fails if the Power Platform connector (Swagger 2.0) drifts from the BFF's OpenAPI contract.
// Run: node integrations/power-platform/check-sync.mjs   (from the repository root; used in CI)
import { readFileSync } from 'node:fs';

const openApi = JSON.parse(readFileSync('api/src/BupaBooks.Host/openapi/bupa-books.json', 'utf8'));
const connector = JSON.parse(readFileSync('integrations/power-platform/apiDefinition.swagger.json', 'utf8'));

const problems = [];
const expectEqual = (what, actual, expected) => {
  if (JSON.stringify(actual) !== JSON.stringify(expected)) {
    problems.push(`${what}: connector has ${JSON.stringify(actual)}, API has ${JSON.stringify(expected)}`);
  }
};

const apiOperation = openApi.paths['/api/v1/books']?.get;
const connectorOperation = connector.paths['/api/v1/books']?.get;
if (!apiOperation || !connectorOperation) {
  console.error('GET /api/v1/books is missing from one of the documents');
  process.exit(1);
}

expectEqual('operationId', connectorOperation.operationId, apiOperation.operationId);

const apiParams = Object.fromEntries(apiOperation.parameters.map((p) => [p.name, p]));
const connectorParams = Object.fromEntries(connectorOperation.parameters.map((p) => [p.name, p]));
expectEqual('parameter names', Object.keys(connectorParams).sort(), Object.keys(apiParams).sort());

for (const [name, apiParam] of Object.entries(apiParams)) {
  const connectorParam = connectorParams[name];
  if (!connectorParam) continue;
  expectEqual(`${name}.required`, Boolean(connectorParam.required), Boolean(apiParam.required));
  expectEqual(`${name}.enum`, connectorParam.enum ?? null, apiParam.schema?.enum ?? null);
  expectEqual(`${name}.type`, connectorParam.type, apiParam.schema?.type ?? 'string');
}

expectEqual(
  'response status codes',
  Object.keys(connectorOperation.responses).sort(),
  Object.keys(apiOperation.responses).sort(),
);

const apiBook = openApi.components.schemas.BookResponse;
const apiFormats = openApi.components.schemas.BookFormatContract.enum;
expectEqual('Book properties', Object.keys(connector.definitions.Book.properties).sort(), Object.keys(apiBook.properties).sort());
expectEqual('Book.format enum', connector.definitions.Book.properties.format.enum, apiFormats);

if (problems.length > 0) {
  console.error('Power Platform connector is out of sync with the API contract:\n- ' + problems.join('\n- '));
  process.exit(1);
}
console.log('Power Platform connector matches the API contract.');
