# System-level tests

Unit, integration and architecture tests live next to the code they test (`api/tests`, `web/src/**/*.test.ts`). This folder holds the tests that exercise the **whole system** as one container:

| Path                       | What                                                                                                                                                                                                                                                |
| -------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `compose.e2e.yaml`         | Compose override: the real app image, with the Bupa API replaced by a stub (`upstream-stub/`) serving the captured live payload, and our rate limit raised so the test burst is not throttled. Run E2E against this, not plain `docker compose up`. |
| `upstream-stub/nginx.conf` | The stub upstream.                                                                                                                                                                                                                                  |
| `load/books.js`            | k6 load test (50 virtual users; fails if p95 > 250 ms or errors > 1%).                                                                                                                                                                              |

```bash
cd web && npm run e2e:up              # = docker compose -f compose.yaml -f tests/compose.e2e.yaml up --build -d --wait
npm run e2e                           # Playwright E2E + axe (specs in web/e2e)
npm run e2e:down
k6 run tests/load/books.js            # with E2E_RATE_LIMIT=1000000 set when starting compose
```
