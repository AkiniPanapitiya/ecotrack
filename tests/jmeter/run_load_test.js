const http = require('http');
const fs = require('fs');
const path = require('path');

const IDENTITY_HOST = 'localhost';
const IDENTITY_PORT = 5001;
const LOGISTICS_HOST = 'localhost';
const LOGISTICS_PORT = 5002;

const CONCURRENCY = 20;
const ITERATIONS_PER_USER = 5;

const results = [];

function makeRequest(options, postData = null) {
  return new Promise((resolve) => {
    const startTime = Date.now();
    const req = http.request(options, (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        const duration = Date.now() - startTime;
        resolve({
          statusCode: res.statusCode,
          duration,
          success: res.statusCode >= 200 && res.statusCode < 400,
          label: `${options.method} ${options.path}`
        });
      });
    });

    req.on('error', (err) => {
      const duration = Date.now() - startTime;
      resolve({
        statusCode: 500,
        duration,
        success: false,
        label: `${options.method} ${options.path}`,
        error: err.message
      });
    });

    if (postData) {
      req.write(postData);
    }
    req.end();
  });
}

async function runWorker(workerId) {
  for (let i = 0; i < ITERATIONS_PER_USER; i++) {
    // 1. POST /api/Auth/login
    const loginRes = await makeRequest({
      hostname: IDENTITY_HOST,
      port: IDENTITY_PORT,
      path: '/api/Auth/login',
      method: 'POST',
      headers: { 'Content-Type': 'application/json' }
    }, JSON.stringify({ email: 'qa_test@ecotrack.lk', password: 'Password@123' }));
    results.push(loginRes);

    // 2. POST /api/Auth/forgot-password
    const forgotRes = await makeRequest({
      hostname: IDENTITY_HOST,
      port: IDENTITY_PORT,
      path: '/api/Auth/forgot-password',
      method: 'POST',
      headers: { 'Content-Type': 'application/json' }
    }, JSON.stringify({ email: 'qa_test@ecotrack.lk' }));
    results.push(forgotRes);

    // 3. GET /identity/health
    const idHealth = await makeRequest({
      hostname: IDENTITY_HOST,
      port: IDENTITY_PORT,
      path: '/identity/health',
      method: 'GET'
    });
    results.push(idHealth);

    // 4. GET /api/Pickup/user/5c0cec89-567f-42cb-819f-8aee0cf9cd52
    const pickupsRes = await makeRequest({
      hostname: LOGISTICS_HOST,
      port: LOGISTICS_PORT,
      path: '/api/Pickup/user/5c0cec89-567f-42cb-819f-8aee0cf9cd52',
      method: 'GET'
    });
    results.push(pickupsRes);

    // 5. GET /api/Pickup/a1111111-1111-1111-1111-111111111111/status
    const statusRes = await makeRequest({
      hostname: LOGISTICS_HOST,
      port: LOGISTICS_PORT,
      path: '/api/Pickup/a1111111-1111-1111-1111-111111111111/status',
      method: 'GET'
    });
    results.push(statusRes);

    // 6. GET /logistics/health
    const logHealth = await makeRequest({
      hostname: LOGISTICS_HOST,
      port: LOGISTICS_PORT,
      path: '/logistics/health',
      method: 'GET'
    });
    results.push(logHealth);
  }
}

async function main() {
  console.log('===============================================================');
  console.log('🚀 Starting EcoTrack Sprint 2 API Load & Performance Benchmark');
  console.log(`📊 Concurrency: ${CONCURRENCY} Virtual Users | Iterations: ${ITERATIONS_PER_USER}`);
  console.log('===============================================================\n');

  const startAll = Date.now();
  const workers = [];
  for (let w = 0; w < CONCURRENCY; w++) {
    workers.push(runWorker(w));
  }
  await Promise.all(workers);
  const totalDuration = (Date.now() - startAll) / 1000;

  // Aggregate stats per label
  const stats = {};
  for (const r of results) {
    if (!stats[r.label]) {
      stats[r.label] = { count: 0, totalMs: 0, minMs: Infinity, maxMs: -Infinity, errors: 0, durations: [] };
    }
    const s = stats[r.label];
    s.count++;
    s.totalMs += r.duration;
    s.minMs = Math.min(s.minMs, r.duration);
    s.maxMs = Math.max(s.maxMs, r.duration);
    s.durations.push(r.duration);
    if (!r.success) s.errors++;
  }

  // Save CSV Report
  const resultsDir = path.join(__dirname, 'results');
  fs.mkdirSync(resultsDir, { recursive: true });
  const csvLines = ['sampler_label,aggregate_report_count,average,aggregate_report_median,aggregate_report_90%_line,aggregate_report_min,aggregate_report_max,aggregate_report_error%,aggregate_report_rate'];

  console.log('-------------------------------------------------------------------------------------------------------------');
  console.log('Label                               | Samples | Avg (ms) | Min (ms) | Max (ms) | 90% Line | Error % | Throughput (req/s)');
  console.log('-------------------------------------------------------------------------------------------------------------');

  let totalSamples = 0;
  let totalErrors = 0;
  let sumAvg = 0;

  for (const [label, s] of Object.entries(stats)) {
    s.durations.sort((a, b) => a - b);
    const avg = Math.round(s.totalMs / s.count);
    const p90 = s.durations[Math.floor(s.durations.length * 0.9)];
    const median = s.durations[Math.floor(s.durations.length * 0.5)];
    const errPct = ((s.errors / s.count) * 100).toFixed(2);
    const throughput = (s.count / totalDuration).toFixed(2);

    totalSamples += s.count;
    totalErrors += s.errors;
    sumAvg += s.totalMs;

    console.log(
      `${label.padEnd(35)} | ${String(s.count).padStart(7)} | ${String(avg).padStart(8)} | ${String(s.minMs).padStart(8)} | ${String(s.maxMs).padStart(8)} | ${String(p90).padStart(8)} | ${String(errPct + '%').padStart(7)} | ${String(throughput + '/s').padStart(18)}`
    );

    csvLines.push(`"${label}",${s.count},${avg},${median},${p90},${s.minMs},${s.maxMs},${errPct}%,${throughput}`);
  }

  console.log('-------------------------------------------------------------------------------------------------------------');
  const overallAvg = Math.round(sumAvg / totalSamples);
  const overallErrPct = ((totalErrors / totalSamples) * 100).toFixed(2);
  const overallThroughput = (totalSamples / totalDuration).toFixed(2);
  console.log(
    `TOTAL / SUMMARY                     | ${String(totalSamples).padStart(7)} | ${String(overallAvg).padStart(8)} |        - |        - |        - | ${String(overallErrPct + '%').padStart(7)} | ${String(overallThroughput + '/s').padStart(18)}`
  );
  console.log('-------------------------------------------------------------------------------------------------------------\n');

  fs.writeFileSync(path.join(resultsDir, 'summary_report.csv'), csvLines.join('\n'));
  console.log(`✅ CSV Results successfully written to: tests/jmeter/results/summary_report.csv`);
  console.log(`⏱️ Total Execution Time: ${totalDuration.toFixed(2)}s | Total Requests: ${totalSamples} | Error Rate: ${overallErrPct}%\n`);
}

main();
