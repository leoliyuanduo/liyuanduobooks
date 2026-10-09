// k6 load test for the BFF (https://grafana.com/docs/k6/).
// Runs against the real image with a stubbed upstream (compose.e2e.yaml), so it measures
// OUR service: caching, serialisation, middleware. Not the Bupa API's rate limit.
//
//   E2E_RATE_LIMIT=1000000 docker compose -f compose.yaml -f compose.e2e.yaml up --build -d
//   k6 run load/books.js                      (BASE_URL defaults to http://localhost:8080)
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

export const options = {
  scenarios: {
    // Realistic browsing: each virtual user loads a view (two parallel requests) and pauses.
    browse: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '20s', target: 50 },
        { duration: '1m', target: 50 },
        { duration: '10s', target: 0 },
      ],
      gracefulRampDown: '5s',
    },
  },
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
  // The run fails if these service-level objectives are missed.
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<250', 'p(99)<500'],
    checks: ['rate>0.99'],
  },
};

const VIEWS = [
  ['adult', null],
  ['child', null],
  ['adult', 'hardcover'],
  ['child', 'hardcover'],
];

function url(ageCategory, format) {
  return `${BASE_URL}/api/v1/books?ageCategory=${ageCategory}${format ? `&format=${format}` : ''}`;
}

export default function () {
  const format = Math.random() < 0.5 ? null : 'hardcover';
  const responses = http.batch(
    VIEWS.filter(([, f]) => f === format).map(([ageCategory]) => ['GET', url(ageCategory, format)]),
  );

  for (const response of responses) {
    check(response, {
      'status is 200': (r) => r.status === 200,
      'has items': (r) => Array.isArray(r.json('items')),
    });
  }

  sleep(1 + Math.random());
}
