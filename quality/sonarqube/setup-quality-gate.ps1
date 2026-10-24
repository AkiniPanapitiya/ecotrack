<#
.SYNOPSIS
    One-time setup of the EcoTrack project and custom Quality Gate on a SonarQube server.

.DESCRIPTION
    Reads the gate conditions from quality/quality-gate.json and uses the SonarQube Web API to:
      1. create the "ecotrack" project (if it does not exist)
      2. (re)create the "EcoTrack Gate" quality gate with those conditions
      3. assign that gate to the project
    Safe to run again: the gate is rebuilt from the JSON file each time.

.EXAMPLE
    ./quality/sonarqube/setup-quality-gate.ps1 -Token squ_xxxxxxxx
#>
[CmdletBinding()]
param(
    [string]$SonarUrl = 'http://localhost:9000',
    [string]$Token = $env:SONAR_TOKEN
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Token)) {
    throw 'No token. Pass -Token <your SonarQube USER token> or set $env:SONAR_TOKEN.'
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$config = Get-Content (Join-Path $repoRoot 'quality/quality-gate.json') -Raw | ConvertFrom-Json
$sonar = $config.sonar

$basic = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($Token):"))
$headers = @{ Authorization = "Basic $basic" }

function Invoke-Sonar {
    param([string]$Method, [string]$Path, [hashtable]$Body = @{})
    $uri = "$($SonarUrl.TrimEnd('/'))/api/$Path"
    if ($Method -eq 'GET') {
        return Invoke-RestMethod -Method Get -Uri $uri -Headers $headers -Body $Body
    }
    return Invoke-RestMethod -Method Post -Uri $uri -Headers $headers -Body $Body -ContentType 'application/x-www-form-urlencoded'
}

Write-Host "Connecting to $SonarUrl ..." -ForegroundColor Cyan
$status = Invoke-Sonar GET 'system/status'
if ($status.status -ne 'UP') {
    throw "SonarQube is not ready yet (status: $($status.status)). Wait a minute and retry."
}

$projects = Invoke-Sonar GET 'projects/search' @{ projects = $sonar.projectKey }
if ($projects.components.Count -eq 0) {
    Invoke-Sonar POST 'projects/create' @{ project = $sonar.projectKey; name = $sonar.projectName } | Out-Null
    Write-Host "Created project '$($sonar.projectKey)'" -ForegroundColor Green
}
else {
    Write-Host "Project '$($sonar.projectKey)' already exists" -ForegroundColor Yellow
}

$gates = Invoke-Sonar GET 'qualitygates/list'
if ($gates.qualitygates | Where-Object { $_.name -eq $sonar.gateName }) {
    Invoke-Sonar POST 'qualitygates/destroy' @{ name = $sonar.gateName } | Out-Null
    Write-Host "Removed old '$($sonar.gateName)' to rebuild it" -ForegroundColor Yellow
}

Invoke-Sonar POST 'qualitygates/create' @{ name = $sonar.gateName } | Out-Null
Write-Host "Created quality gate '$($sonar.gateName)'" -ForegroundColor Green

foreach ($condition in $sonar.conditions) {
    try {
        Invoke-Sonar POST 'qualitygates/create_condition' @{
            gateName = $sonar.gateName
            metric   = $condition.metric
            op       = $condition.op
            error    = $condition.error
        } | Out-Null
        Write-Host ("  + {0,-32} fails when {1} {2}" -f $condition.metric, $condition.op, $condition.error)
    }
    catch {
        Write-Host ("  ! skipped {0}: this SonarQube version does not accept it ({1})" -f $condition.metric, $_.Exception.Message) -ForegroundColor Yellow
    }
}

Invoke-Sonar POST 'qualitygates/select' @{ gateName = $sonar.gateName; projectKey = $sonar.projectKey } | Out-Null
Write-Host "Gate '$($sonar.gateName)' assigned to '$($sonar.projectKey)'. Setup complete." -ForegroundColor Green
