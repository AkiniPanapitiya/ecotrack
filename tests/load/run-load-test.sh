#!/usr/bin/env bash
# EcoTrack Load & Performance Test (SE3112) - Linux/macOS/CI runner.
# Usage: ./tests/load/run-load-test.sh [smoke|load|stress|spike]
# Env:   IDENTITY_URL, LOGISTICS_URL, MARKETPLACE_URL (default localhost:5001-5003)
set -euo pipefail

PROFILE="${1:-smoke}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
RESULTS="$SCRIPT_DIR/results"
STAMP="$(date +%Y%m%d-%H%M%S)"
IDENTITY_URL="${IDENTITY_URL:-http://localhost:5001}"
LOGISTICS_URL="${LOGISTICS_URL:-http://localhost:5002}"
MARKETPLACE_URL="${MARKETPLACE_URL:-http://localhost:5003}"
mkdir -p "$RESULTS"

echo "=== Waiting for services to become healthy ==="
for url in "$IDENTITY_URL/identity/health" "$LOGISTICS_URL/logistics/health" "$MARKETPLACE_URL/marketplace/health"; do
  for i in $(seq 1 60); do
    if curl -fs "$url" > /dev/null; then echo "  OK  $url"; break; fi
    [ "$i" -eq 60 ] && { echo "  FAIL $url"; exit 1; }
    sleep 3
  done
done

echo "=== Running k6 ($PROFILE profile) ==="
export K6_WEB_DASHBOARD=true
export K6_WEB_DASHBOARD_EXPORT="$RESULTS/$PROFILE-$STAMP-report.html"
k6 run \
  -e PROFILE="$PROFILE" \
  -e IDENTITY_URL="$IDENTITY_URL" \
  -e LOGISTICS_URL="$LOGISTICS_URL" \
  -e MARKETPLACE_URL="$MARKETPLACE_URL" \
  --summary-export="$RESULTS/$PROFILE-$STAMP-summary.json" \
  "$SCRIPT_DIR/ecotrack-load-test.js"
