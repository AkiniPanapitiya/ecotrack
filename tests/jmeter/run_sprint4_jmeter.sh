#!/usr/bin/env bash
# Run JMeter Performance Test for Sprint 4
# EcoTrack - Logistics, Marketplace & Analytics Services
# Usage: ./run_sprint4_jmeter.sh [-n] [-t testplan] [-j jmeter] [-l results] [-e html]

set -e

# Defaults
NON_INTERACTIVE=false
TESTPLAN="tests/jmeter/Sprint4_Performance_Test_Plan.jmx"
JMETER="jmeter"
RESULTS="tests/jmeter/results/sprint4_report"
HTML_REPORT="tests/jmeter/sprint4_html_report"
LOG_LEVEL="-Jjmeterengine.logger=INFO"

# Parse arguments
while getopts "nt:j:l:e:" opt; do
  case $opt in
    n) NON_INTERACTIVE=true ;;
    t) TESTPLAN="$OPTARG" ;;
    j) JMETER="$OPTARG" ;;
    l) RESULTS="$OPTARG" ;;
    e) HTML_REPORT="$OPTARG" ;;
    *) echo "Usage: $0 [-n] [-t testplan] [-j jmeter] [-l results_dir] [-e html_dir]"; exit 1 ;;
  esac
done

# Verify JMeter exists
if ! command -v $JMETER &>/dev/null; then
  echo "ERROR: JMeter not found at $JMETER"
  exit 1
fi

# Verify test plan exists
if [ ! -f "$TESTPLAN" ]; then
  echo "ERROR: Test plan not found: $TESTPLAN"
  exit 1
fi

echo "========================================="
echo "EcoTrack Sprint 4 JMeter Performance Test"
echo "========================================="
echo "Test Plan: $TESTPLAN"
echo "JMeter: $JMETER"
echo "Results Dir: $RESULTS"
echo "HTML Report: $HTML_REPORT"
echo ""

# Create results directory
mkdir -p "$RESULTS"

# Run JMeter in non-GUI mode with listeners for HTML report
$JMETER -n -t "$TESTPLAN" -l "$RESULTS/result.jtl" $LOG_LEVEL -e -d "$HTML_REPORT" -j "$RESULTS/jmeter.log"

echo ""
echo "========================================="
echo "Test Complete!"
echo "========================================="
echo "Raw results: $RESULTS/result.jtl"
echo "HTML Dashboard: $HTML_REPORT/index.html"
echo ""
echo "Recorded metrics:"
echo "  - Throughput (requests/sec)"
echo "  - Average response time"
echo "  - 95th percentile response time"
echo "  - Error rate (%)"
echo ""
echo "Scenarios executed:"
echo "  - Baseline Load (10 users, 60s ramp)"
echo "  - Ramp-Up to Peak (50 users, 120s ramp)"
echo "  - Short Spike (100 users, 10s ramp)"