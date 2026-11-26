// ============================================================
// EcoTrack k6 load tests - shared configuration
// SE3112 Load & Performance Testing (Owner: Stef)
// ============================================================

// Service base URLs. Override with -e IDENTITY_URL=... when the
// services are not running on localhost (e.g. inside Docker or CI).
export const IDENTITY_URL = __ENV.IDENTITY_URL || 'http://localhost:5001';
export const LOGISTICS_URL = __ENV.LOGISTICS_URL || 'http://localhost:5002';
export const MARKETPLACE_URL = __ENV.MARKETPLACE_URL || 'http://localhost:5003';

// Number of test accounts created in setup(). Each booking VU logs in
// with one of these, so we never measure one hot user row only.
export const USER_POOL_SIZE = Number(__ENV.USER_POOL_SIZE || 10);
export const TEST_PASSWORD = 'LoadTest@123';

// ------------------------------------------------------------
// Load profiles. Pick one with -e PROFILE=smoke|load|stress|spike
//
// Two scenarios run side by side to mimic real traffic:
//   browsers - anonymous/read-heavy traffic (health, marketplace, pickup status)
//   bookers  - logged-in citizens who log in and book a pickup (write path)
// ------------------------------------------------------------
const profiles = {
  // Sanity check: proves the script and the services work. Used in CI.
  smoke: {
    browsers: { executor: 'constant-vus', vus: 2, duration: '30s' },
    bookers: { executor: 'constant-vus', vus: 1, duration: '30s' },
  },

  // Expected normal day: ramp up, hold, ramp down.
  load: {
    browsers: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 20 },
        { duration: '1m', target: 20 },
        { duration: '20s', target: 0 },
      ],
    },
    bookers: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 5 },
        { duration: '1m', target: 5 },
        { duration: '20s', target: 0 },
      ],
    },
  },

  // Keep pushing past normal load to find where the system starts to degrade.
  stress: {
    browsers: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 25 },
        { duration: '30s', target: 50 },
        { duration: '30s', target: 100 },
        { duration: '30s', target: 100 },
        { duration: '20s', target: 0 },
      ],
    },
    bookers: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 5 },
        { duration: '30s', target: 10 },
        { duration: '30s', target: 20 },
        { duration: '30s', target: 20 },
        { duration: '20s', target: 0 },
      ],
    },
  },

  // Sudden burst of traffic (e.g. a marketplace promotion), then recovery.
  spike: {
    browsers: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '10s', target: 5 },
        { duration: '5s', target: 150 },
        { duration: '30s', target: 150 },
        { duration: '5s', target: 5 },
        { duration: '20s', target: 5 },
        { duration: '5s', target: 0 },
      ],
    },
    bookers: { executor: 'constant-vus', vus: 3, duration: '75s' },
  },
};

export const PROFILE = __ENV.PROFILE || 'smoke';

export function buildScenarios(profileName) {
  const profile = profiles[profileName];
  if (!profile) {
    throw new Error(`Unknown PROFILE "${profileName}". Use one of: ${Object.keys(profiles).join(', ')}`);
  }
  return {
    browsers: Object.assign({ exec: 'browseJourney', gracefulStop: '10s' }, profile.browsers),
    bookers: Object.assign({ exec: 'bookingJourney', gracefulStop: '10s' }, profile.bookers),
  };
}

// ------------------------------------------------------------
// Pass/fail criteria (Service Level Objectives).
// If any threshold is crossed, k6 exits with a non-zero code,
// which fails the CI job.
// ------------------------------------------------------------
export const thresholds = {
  // Whole system
  http_req_failed: ['rate<0.01'],                    // < 1% unexpected errors
  http_req_duration: ['p(95)<800', 'p(99)<1500'],    // 95% of requests under 0.8s
  checks: ['rate>0.99'],                             // > 99% of assertions pass

  // Per endpoint (tagged with { name: ... })
  'http_req_duration{name:identity_health}': ['p(95)<200'],
  'http_req_duration{name:auth_login}': ['p(95)<1500'],       // bcrypt hashing is CPU-bound by design
  'http_req_duration{name:auth_login_invalid}': ['p(95)<1500'],
  'http_req_duration{name:pickup_create}': ['p(95)<1000'],
  'http_req_duration{name:pickup_get}': ['p(95)<500'],
  'http_req_duration{name:pickup_status}': ['p(95)<500'],
  'http_req_duration{name:pickup_list_user}': ['p(95)<500'],
  'http_req_duration{name:listings_browse}': ['p(95)<500'],
  'http_req_duration{name:listings_search}': ['p(95)<500'],

  // Custom business metrics
  pickups_created: ['count>0'],
  booking_journey_failed: ['rate<0.02'],
  booking_journey_duration: ['p(95)<3000'],
};
