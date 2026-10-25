// ============================================================
// EcoTrack - k6 Load & Performance Test
// SE3112 Advanced Software Engineering (Owner: Stef)
//
// Run:   k6 run -e PROFILE=load tests/load/ecotrack-load-test.js
// Profiles: smoke | load | stress | spike   (see lib/config.js)
// ============================================================
import http from 'k6/http';
import { check, group, sleep, fail } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';
import {
  IDENTITY_URL, LOGISTICS_URL, MARKETPLACE_URL,
  USER_POOL_SIZE, TEST_PASSWORD, PROFILE,
  buildScenarios, thresholds,
} from './lib/config.js';

// ---------- Custom metrics (shown in the k6 summary) ----------
const pickupsCreated = new Counter('pickups_created');
const bookingJourneyFailed = new Rate('booking_journey_failed');
const bookingJourneyDuration = new Trend('booking_journey_duration', true);

export const options = {
  scenarios: buildScenarios(PROFILE),
  thresholds,
  // Show p(90), p(95) and p(99) in the end-of-test summary.
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
  tags: { testid: `ecotrack-${PROFILE}` },
};

const JSON_HEADERS = { 'Content-Type': 'application/json' };

// A 401/404 on purpose is a *correct* answer for negative tests, so it
// must not be counted in http_req_failed.
const expect401 = http.expectedStatuses(401);
const expect404 = http.expectedStatuses(404);

function authHeaders(token) {
  return Object.assign({ Authorization: `Bearer ${token}` }, JSON_HEADERS);
}

function futureDate(daysAhead) {
  const d = new Date();
  d.setUTCDate(d.getUTCDate() + daysAhead);
  return d.toISOString();
}

function randomPhone() {
  return `07${Math.floor(10000000 + Math.random() * 89999999)}`;
}

// ============================================================
// setup(): runs ONCE before the test.
// 1. Checks every service is up (fails fast instead of a wall of errors).
// 2. Registers a pool of citizen accounts used by the booking VUs.
// 3. Creates one pickup that the read-only VUs can query.
// ============================================================
export function setup() {
  const services = {
    identity: `${IDENTITY_URL}/identity/health`,
    logistics: `${LOGISTICS_URL}/logistics/health`,
    marketplace: `${MARKETPLACE_URL}/marketplace/health`,
  };
  for (const [svc, url] of Object.entries(services)) {
    const res = http.get(url, { tags: { name: 'setup' } });
    if (res.status !== 200) {
      fail(`${svc} service is not healthy at ${url} (status ${res.status}). Start the stack first.`);
    }
  }

  const runId = Date.now();
  const users = [];
  for (let i = 0; i < USER_POOL_SIZE; i++) {
    const email = `k6_${runId}_${i}@loadtest.ecotrack.lk`;
    const res = http.post(`${IDENTITY_URL}/api/Auth/register`, JSON.stringify({
      fullName: `Load Test User ${i}`,
      email,
      password: TEST_PASSWORD,
      role: 'User',
      phoneNumber: randomPhone(),
      address: '12 Galle Road, Colombo 03',
    }), { headers: JSON_HEADERS, tags: { name: 'setup' } });

    if (res.status !== 201) {
      fail(`Could not register load-test user ${email}: ${res.status} ${res.body}`);
    }
    users.push({ email, userId: res.json('userId') });
  }

  // One known pickup for the read-heavy scenario.
  const login = http.post(`${IDENTITY_URL}/api/Auth/login`,
    JSON.stringify({ email: users[0].email, password: TEST_PASSWORD }),
    { headers: JSON_HEADERS, tags: { name: 'setup' } });
  const seed = http.post(`${LOGISTICS_URL}/api/Pickup`, JSON.stringify(pickupPayload()),
    { headers: authHeaders(login.json('token')), tags: { name: 'setup' } });
  if (seed.status !== 201) {
    fail(`Could not create seed pickup: ${seed.status} ${seed.body}`);
  }

  console.log(`Profile "${PROFILE}": ${users.length} test users registered, seed pickup ${seed.json('id')}`);
  return { users, seedPickupId: seed.json('id') };
}

function pickupPayload() {
  const categories = ['Laptops', 'Mobile Phones', 'Batteries', 'Monitors', 'Printers'];
  return {
    category: categories[Math.floor(Math.random() * categories.length)],
    estimatedWeightKg: Number((1 + Math.random() * 20).toFixed(1)),
    pickupAddress: '45 Kandy Road, Kadawatha',
    contactPhone: randomPhone(),
    preferredDate: futureDate(3),
    timeSlot: 'Morning (09:00 - 12:00)',
    specialInstructions: 'k6 load test - safe to delete',
    items: [{ itemName: 'Old laptop', quantity: 1, itemCondition: 'Used', estimatedWeightKg: 2.5 }],
  };
}

// ============================================================
// Scenario 1 - "browsers": read-heavy traffic most users generate.
// ============================================================
export function browseJourney(data) {
  group('Service health', () => {
    const res = http.get(`${IDENTITY_URL}/identity/health`, { tags: { name: 'identity_health' } });
    check(res, {
      'identity health is 200': (r) => r.status === 200,
      'identity reports healthy': (r) => r.json('status') === 'healthy',
    });
  });

  group('Marketplace browsing', () => {
    const page = 1 + Math.floor(Math.random() * 3);
    let res = http.get(`${MARKETPLACE_URL}/api/listings?page=${page}&pageSize=10`,
      { tags: { name: 'listings_browse' } });
    check(res, {
      'browse listings is 200': (r) => r.status === 200,
      'browse returns pagination': (r) => r.json('pagination.page') === page,
    });

    const term = ['laptop', 'phone', 'monitor', 'battery'][Math.floor(Math.random() * 4)];
    res = http.get(`${MARKETPLACE_URL}/api/listings?search=${term}`, { tags: { name: 'listings_search' } });
    check(res, { 'search listings is 200': (r) => r.status === 200 });
  });

  group('Pickup tracking', () => {
    let res = http.get(`${LOGISTICS_URL}/api/Pickup/${data.seedPickupId}/status`,
      { tags: { name: 'pickup_status' } });
    check(res, {
      'pickup status is 200': (r) => r.status === 200,
      'pickup status has a value': (r) => typeof r.json('status') === 'string',
    });

    // Edge case: unknown pickup must return 404 quickly, not 500.
    res = http.get(`${LOGISTICS_URL}/api/Pickup/00000000-0000-0000-0000-000000000000/status`,
      { tags: { name: 'pickup_status_not_found' }, responseCallback: expect404 });
    check(res, { 'unknown pickup is 404': (r) => r.status === 404 });
  });

  sleep(1 + Math.random() * 2); // think time between page views
}

// ============================================================
// Scenario 2 - "bookers": log in and book an e-waste pickup
// (crosses Identity -> Logistics, i.e. JWT issued by one service
//  is validated by another under load).
// ============================================================
export function bookingJourney(data) {
  const user = data.users[(__VU - 1) % data.users.length];
  const started = Date.now();
  let ok = true;

  group('Login', () => {
    // Edge case: wrong password must be rejected with 401 under load.
    const bad = http.post(`${IDENTITY_URL}/api/Auth/login`,
      JSON.stringify({ email: user.email, password: 'WrongPassword!1' }),
      { headers: JSON_HEADERS, tags: { name: 'auth_login_invalid' }, responseCallback: expect401 });
    check(bad, { 'wrong password is 401': (r) => r.status === 401 });

    const res = http.post(`${IDENTITY_URL}/api/Auth/login`,
      JSON.stringify({ email: user.email, password: TEST_PASSWORD }),
      { headers: JSON_HEADERS, tags: { name: 'auth_login' } });
    ok = check(res, {
      'login is 200': (r) => r.status === 200,
      'login returns a JWT': (r) => typeof r.json('token') === 'string' && r.json('token').split('.').length === 3,
    }) && ok;
    user.token = res.status === 200 ? res.json('token') : null;
  });

  if (!user.token) {
    bookingJourneyFailed.add(true);
    sleep(1);
    return;
  }

  let pickupId = null;
  group('Book pickup', () => {
    const res = http.post(`${LOGISTICS_URL}/api/Pickup`, JSON.stringify(pickupPayload()),
      { headers: authHeaders(user.token), tags: { name: 'pickup_create' } });
    ok = check(res, {
      'create pickup is 201': (r) => r.status === 201,
      'new pickup is Pending': (r) => r.json('status') === 'Pending',
      'pickup belongs to logged-in user': (r) => r.json('userId') === user.userId,
    }) && ok;
    if (res.status === 201) {
      pickupId = res.json('id');
      pickupsCreated.add(1);
    }
  });

  if (pickupId) {
    group('Track booking', () => {
      let res = http.get(`${LOGISTICS_URL}/api/Pickup/${pickupId}`, { tags: { name: 'pickup_get' } });
      ok = check(res, { 'get pickup is 200': (r) => r.status === 200 }) && ok;

      res = http.get(`${LOGISTICS_URL}/api/Pickup/user/${user.userId}`, { tags: { name: 'pickup_list_user' } });
      ok = check(res, {
        'user pickup list is 200': (r) => r.status === 200,
        'new pickup appears in user list': (r) => (r.json() || []).some((p) => p.id === pickupId),
      }) && ok;
    });
  }

  bookingJourneyFailed.add(!ok);
  bookingJourneyDuration.add(Date.now() - started);
  sleep(1 + Math.random() * 2);
}
