#!/usr/bin/env bash
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
cd "$DIR"

echo "=========================================================="
echo "🎯 EcoTrack Sprint 2 JMeter Performance Test Runner"
echo "=========================================================="

if command -v jmeter &> /dev/null; then
    echo "⚡ Running via Apache JMeter CLI..."
    mkdir -p results
    jmeter -n -t ecotrack_sprint2_performance_test.jmx -l results/test_results.jtl -e -o results/html_report
    echo "✅ JMeter HTML dashboard generated at: tests/jmeter/results/html_report/index.html"
else
    echo "⚡ Running via High-Performance Load Test Runner..."
    node run_load_test.js
fi
