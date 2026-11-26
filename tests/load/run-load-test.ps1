<#
.SYNOPSIS
    EcoTrack Load & Performance Testing pipeline (SE3112).

.DESCRIPTION
    Runs the k6 load test in tests/load/ecotrack-load-test.js against the
    Identity, Logistics and Marketplace microservices.
      1. (optional) Starts the backend stack with Docker Compose
      2. Waits until every service health endpoint answers 200
      3. Runs k6 with the chosen load profile (smoke | load | stress | spike)
      4. Writes an HTML dashboard report + JSON summary to tests/load/results
    k6 exits non-zero when a threshold (SLO) is broken, so this script does too.
    If k6 is not installed locally, the grafana/k6 Docker image is used instead.

.EXAMPLE
    ./tests/load/run-load-test.ps1                          # smoke test, stack already running
    ./tests/load/run-load-test.ps1 -Profile load -StartStack -Dashboard
    ./tests/load/run-load-test.ps1 -Profile stress -OpenReport
#>
[CmdletBinding()]
param(
    [ValidateSet('smoke', 'load', 'stress', 'spike')]
    [string]$Profile = 'smoke',
    [switch]$StartStack,
    [switch]$Dashboard,
    [switch]$OpenReport,
    [string]$IdentityUrl = 'http://localhost:5001',
    [string]$LogisticsUrl = 'http://localhost:5002',
    [string]$MarketplaceUrl = 'http://localhost:5003'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location $repoRoot

$k6Image = 'grafana/k6:1.3.0'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$resultsDir = Join-Path $PSScriptRoot 'results'
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
$reportName = "$Profile-$stamp-report.html"
$summaryName = "$Profile-$stamp-summary.json"

function Write-Step([string]$text) { Write-Host "`n=== $text ===" -ForegroundColor Cyan }

# ---------- 1. Start the stack ----------
if ($StartStack) {
    Write-Step 'Starting backend services with Docker Compose'
    docker compose up -d --build db kafka kafka-init identity logistics marketplace
    if ($LASTEXITCODE -ne 0) { throw 'docker compose up failed' }
    Write-Host 'Waiting for MySQL to finish running the migrations...'
    $deadline = (Get-Date).AddMinutes(3)
    while ((docker inspect -f '{{.State.Health.Status}}' ecotrack-db) -ne 'healthy') {
        if ((Get-Date) -gt $deadline) { throw 'MySQL did not become healthy' }
        Start-Sleep -Seconds 3
    }
}

# ---------- 2. Wait for health ----------
Write-Step 'Waiting for services to become healthy'
$health = @{
    'Identity'    = "$IdentityUrl/identity/health"
    'Logistics'   = "$LogisticsUrl/logistics/health"
    'Marketplace' = "$MarketplaceUrl/marketplace/health"
}
foreach ($svc in $health.Keys) {
    $deadline = (Get-Date).AddMinutes(3)
    while ($true) {
        try {
            $r = Invoke-WebRequest -Uri $health[$svc] -UseBasicParsing -TimeoutSec 5
            if ($r.StatusCode -eq 200) { Write-Host "  $svc is healthy" -ForegroundColor Green; break }
        } catch { }
        if ((Get-Date) -gt $deadline) { throw "$svc did not become healthy at $($health[$svc])" }
        Start-Sleep -Seconds 3
    }
}

# ---------- 3. Run k6 ----------
Write-Step "Running k6 ($Profile profile)"
$k6Args = @('run', '-e', "PROFILE=$Profile")
$useDocker = -not (Get-Command k6 -ErrorAction SilentlyContinue)

if ($useDocker) {
    Write-Host "k6 not found locally - using Docker image $k6Image"
    # Inside the container 'localhost' is the container itself, so point at the host.
    $toHost = { param($u) $u -replace 'localhost|127\.0\.0\.1', 'host.docker.internal' }
    $dockerArgs = @('run', '--rm', '-i', '-u', 'root',
        '--add-host', 'host.docker.internal:host-gateway',
        '-v', "$($PSScriptRoot):/scripts",
        '-e', 'K6_WEB_DASHBOARD=true',
        '-e', "K6_WEB_DASHBOARD_EXPORT=/scripts/results/$reportName")
    if ($Dashboard) { $dockerArgs += @('-p', '5665:5665', '-e', 'K6_WEB_DASHBOARD_HOST=0.0.0.0') }
    $dockerArgs += @($k6Image) + $k6Args + @(
        '-e', "IDENTITY_URL=$(& $toHost $IdentityUrl)",
        '-e', "LOGISTICS_URL=$(& $toHost $LogisticsUrl)",
        '-e', "MARKETPLACE_URL=$(& $toHost $MarketplaceUrl)",
        "--summary-export=/scripts/results/$summaryName",
        '/scripts/ecotrack-load-test.js')
    if ($Dashboard) { Write-Host 'Live dashboard: http://localhost:5665' -ForegroundColor Yellow }
    docker @dockerArgs
} else {
    $env:K6_WEB_DASHBOARD = 'true'
    $env:K6_WEB_DASHBOARD_EXPORT = Join-Path $resultsDir $reportName
    if ($Dashboard) { $env:K6_WEB_DASHBOARD_OPEN = 'true' }
    $k6Args += @(
        '-e', "IDENTITY_URL=$IdentityUrl",
        '-e', "LOGISTICS_URL=$LogisticsUrl",
        '-e', "MARKETPLACE_URL=$MarketplaceUrl",
        "--summary-export=$(Join-Path $resultsDir $summaryName)",
        (Join-Path $PSScriptRoot 'ecotrack-load-test.js'))
    k6 @k6Args
}
$exitCode = $LASTEXITCODE

# ---------- 4. Report ----------
Write-Step 'Results'
Write-Host "HTML report : tests/load/results/$reportName"
Write-Host "JSON summary: tests/load/results/$summaryName"
if ($exitCode -eq 0) {
    Write-Host 'PASS - all performance thresholds met' -ForegroundColor Green
} else {
    Write-Host "FAIL - k6 exited with code $exitCode (a threshold was broken or the test errored)" -ForegroundColor Red
}
if ($OpenReport) { Invoke-Item (Join-Path $resultsDir $reportName) }
exit $exitCode
