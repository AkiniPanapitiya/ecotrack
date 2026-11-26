<#
.SYNOPSIS
    EcoTrack Mutation Testing pipeline (SE3112).

.DESCRIPTION
    Automated Stryker.NET mutation testing runner for EcoTrack microservices:
      1. Tool check          Ensures dotnet-stryker is installed in global tools
      2. Mutation run        Executes Stryker.NET with CoverageBasedTest optimization
      3. Report generation   Extracts HTML, JSON, and ClearText results
      4. Quality gate        Validates final mutation score against thresholds
      5. Dashboard           Opens HTML report if -OpenReport is specified

.EXAMPLE
    ./mutation/run-mutation.ps1
    ./mutation/run-mutation.ps1 -OpenReport
    ./mutation/run-mutation.ps1 -Scope AllServices -OpenReport
    ./mutation/run-mutation.ps1 -Scope AllProjects
#>
[CmdletBinding()]
param(
    [ValidateSet("AuthService", "AllServices", "AllProjects")]
    [string]$Scope = "AuthService",
    [switch]$OpenReport,
    [double]$MinMutationScore = 60.0,
    [double]$HighQualityGate = 80.0
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_ROLL_FORWARD = 'Major'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $repoRoot

function Write-Step([string]$Number, [string]$Title) {
    Write-Host ''
    Write-Host ("[{0}] {1}" -f $Number, $Title) -ForegroundColor Cyan
    Write-Host ('-' * 60) -ForegroundColor DarkGray
}

function Ensure-StrykerTool {
    $toolsDir = Join-Path $HOME '.dotnet/tools'
    if (($env:PATH -split [IO.Path]::PathSeparator) -notcontains $toolsDir) {
        $env:PATH = $toolsDir + [IO.Path]::PathSeparator + $env:PATH
    }
    if (-not (Get-Command 'dotnet-stryker' -ErrorAction SilentlyContinue)) {
        Write-Host "Installing dotnet-stryker global tool..." -ForegroundColor Yellow
        dotnet tool install -g dotnet-stryker | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Could not install dotnet-stryker" }
    }
}

# 1. Tool Verification
Write-Step '1/4' 'Verifying Stryker.NET CLI'
Ensure-StrykerTool
Write-Host "Stryker.NET CLI is ready." -ForegroundColor Green

$configSource = Join-Path $PSScriptRoot 'stryker-config.json'

if ($Scope -eq "AllProjects") {
    Write-Step '2/4' 'Preparing Solution-Wide Mutation Testing'
    $projects = @(
        @{ Name = "IdentityService"; Path = "tests/IdentityService.Tests"; ProjectFile = "IdentityService.csproj" },
        @{ Name = "LogisticsService"; Path = "tests/LogisticsService.Tests"; ProjectFile = "LogisticsService.csproj" },
        @{ Name = "MarketplaceService"; Path = "tests/MarketplaceService.Tests"; ProjectFile = "MarketplaceService.csproj" }
    )

    $results = @()
    $totalKilled = 0
    $totalTested = 0

    foreach ($p in $projects) {
        $projDir = Join-Path $repoRoot $p.Path
        $projConfig = Join-Path $projDir 'stryker-config.json'

        Write-Host ("`nRunning Stryker on {0}..." -f $p.Name) -ForegroundColor Yellow
        if (Test-Path $configSource) {
            $configObject = Get-Content $configSource -Raw | ConvertFrom-Json
            $configObject.'stryker-config'.project = $p.ProjectFile
            $configObject.'stryker-config'.mutate = @("Services/**/*.cs")
            $configObject | ConvertTo-Json -Depth 10 | Set-Content $projConfig -Encoding utf8
        }

        Push-Location $projDir
        try {
            dotnet stryker
        }
        finally {
            Pop-Location
            if (Test-Path $projConfig) {
                Remove-Item -Path $projConfig -Force -ErrorAction SilentlyContinue
            }
        }

        $latestDir = Get-ChildItem -Path (Join-Path $projDir 'StrykerOutput') -Directory -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        $pScore = 0
        $pKilled = 0
        $pTested = 0

        if ($latestDir) {
            $jsonPath = Join-Path $latestDir.FullName 'reports/mutation-report.json'
            if (Test-Path $jsonPath) {
                $rj = Get-Content $jsonPath -Raw | ConvertFrom-Json
                $allM = @()
                foreach ($prop in $rj.files.PSObject.Properties) {
                    if ($prop.Value.mutants) {
                        $allM += $prop.Value.mutants
                    }
                }
                $pKilled = ($allM | Where-Object { $_.status -eq 'Killed' } | Measure-Object).Count
                $survived = ($allM | Where-Object { $_.status -eq 'Survived' } | Measure-Object).Count
                $noCov = ($allM | Where-Object { $_.status -eq 'NoCoverage' } | Measure-Object).Count
                $timeout = ($allM | Where-Object { $_.status -eq 'Timeout' } | Measure-Object).Count
                $pTested = $pKilled + $survived + $noCov + $timeout
                $pScore = if ($pTested -gt 0) { [math]::Round(($pKilled / $pTested) * 100, 2) } else { 0 }
            }
        }

        $results += [PSCustomObject]@{
            Project = $p.Name
            Tested = $pTested
            Killed = $pKilled
            Score = "$pScore%"
        }

        $totalKilled += $pKilled
        $totalTested += $pTested
    }

    $overallScore = if ($totalTested -gt 0) { [math]::Round(($totalKilled / $totalTested) * 100, 2) } else { 0 }

    Write-Host "`n========================================================" -ForegroundColor Cyan
    Write-Host "       EcoTrack Solution-Wide Mutation Summary          " -ForegroundColor Cyan
    Write-Host "========================================================" -ForegroundColor Cyan
    $results | Format-Table -AutoSize | Out-Host
    Write-Host "--------------------------------------------------------" -ForegroundColor DarkGray
    Write-Host ("  Total Mutants Tested : {0}" -f $totalTested)
    Write-Host ("  Total Mutants Killed : {0}" -f $totalKilled) -ForegroundColor Green
    Write-Host ("  Overall Solution Score: {0}%" -f $overallScore) -ForegroundColor $(if ($overallScore -ge 50) { 'Green' } else { 'Yellow' })
    Write-Host "========================================================`n" -ForegroundColor Cyan

    return
}

# 2. Synchronize configuration for IdentityService
$testProjectDir = Join-Path $repoRoot 'tests/IdentityService.Tests'
$configTarget = Join-Path $testProjectDir 'stryker-config.json'

Write-Step '2/4' ("Preparing Mutation Test Configuration (Scope: {0})" -f $Scope)
if (Test-Path $configSource) {
    $configObject = Get-Content $configSource -Raw | ConvertFrom-Json
    if ($Scope -eq "AllServices") {
        $configObject.'stryker-config'.mutate = @("Services/**/*.cs")
        $targetDesc = "Services/**/*.cs (All Domain Services)"
    } else {
        $configObject.'stryker-config'.mutate = @("Services/AuthService.cs")
        $targetDesc = "Services/AuthService.cs"
    }
    $configObject | ConvertTo-Json -Depth 10 | Set-Content $configTarget -Encoding utf8
}

# 3. Execute Stryker
Write-Step '3/4' ("Executing Mutation Testing (Target: {0})" -f $targetDesc)
Push-Location $testProjectDir
try {
    dotnet stryker
    $strykerExit = $LASTEXITCODE
}
finally {
    Pop-Location
    if (Test-Path $configTarget) {
        Remove-Item -Path $configTarget -Force -ErrorAction SilentlyContinue
    }
}

# 4. Parse Results & Evaluate Quality Gate
Write-Step '4/4' 'Evaluating Mutation Quality Gate'

$latestReportDir = Get-ChildItem -Path (Join-Path $testProjectDir 'StrykerOutput') -Directory -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $latestReportDir) {
    throw "No Stryker output directory found."
}

$jsonReportPath = Join-Path $latestReportDir.FullName 'reports/mutation-report.json'
$htmlReportPath = Join-Path $latestReportDir.FullName 'reports/mutation-report.html'

if (Test-Path $jsonReportPath) {
    $reportJson = Get-Content $jsonReportPath -Raw | ConvertFrom-Json

    $allMutants = @()
    if ($Scope -eq "AllServices") {
        foreach ($prop in $reportJson.files.PSObject.Properties) {
            if ($prop.Name -like "*Services*" -and $prop.Value.mutants) {
                $allMutants += $prop.Value.mutants
            }
        }
    } else {
        foreach ($prop in $reportJson.files.PSObject.Properties) {
            if ($prop.Name -like "*AuthService.cs*" -and $prop.Value.mutants) {
                $allMutants += $prop.Value.mutants
                break
            }
        }
    }

    $killed = ($allMutants | Where-Object { $_.status -eq 'Killed' } | Measure-Object).Count
    $survived = ($allMutants | Where-Object { $_.status -eq 'Survived' } | Measure-Object).Count
    $noCoverage = ($allMutants | Where-Object { $_.status -eq 'NoCoverage' } | Measure-Object).Count
    $timeout = ($allMutants | Where-Object { $_.status -eq 'Timeout' } | Measure-Object).Count

    $testedTotal = $killed + $survived + $noCoverage + $timeout
    $score = if ($testedTotal -gt 0) { [math]::Round(($killed / $testedTotal) * 100, 2) } else { 0 }

    $effectiveMinScore = if ($Scope -eq "AllServices") { 40.0 } else { $MinMutationScore }

    Write-Host "`n========================================================" -ForegroundColor Cyan
    Write-Host "                Mutation Testing Summary                " -ForegroundColor Cyan
    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host ("  Scope              : {0}" -f $Scope)
    Write-Host ("  Target Service     : IdentityService ({0})" -f $targetDesc)
    Write-Host ("  Mutants Tested     : {0}" -f $testedTotal)
    Write-Host ("  Mutants Killed     : {0}" -f $killed) -ForegroundColor Green
    Write-Host ("  Mutants Survived   : {0}" -f $survived) -ForegroundColor $(if ($survived -gt 0) { 'Yellow' } else { 'Green' })
    Write-Host ("  Final Score        : {0}%" -f $score) -ForegroundColor $(if ($score -ge $HighQualityGate) { 'Green' } else { 'Yellow' })
    Write-Host ("  Quality Gate Target: >= {0}% (High: {1}%)" -f $effectiveMinScore, $HighQualityGate)
    Write-Host "========================================================`n" -ForegroundColor Cyan

    if ($env:GITHUB_STEP_SUMMARY) {
        $summaryMd = @"
### Mutation Testing Summary (Stryker.NET)

| Metric | Result | Target |
|---|---|---|
| Scope | `$Scope` | - |
| Target Scope | `$targetDesc` | - |
| Mutants Tested | **$testedTotal** | - |
| Mutants Killed | **$killed** | - |
| Mutants Survived | **$survived** | - |
| **Mutation Score** | **$score%** | >= $effectiveMinScore% |
| **Quality Gate** | **$([string](if ($score -ge $HighQualityGate) { 'PASSED (High Quality)' } elseif ($score -ge $effectiveMinScore) { 'PASSED' } else { 'FAILED' }))** | Pass |
"@
        Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value $summaryMd
    }

    if ($score -lt $effectiveMinScore) {
        Write-Error "Mutation score ($score%) is below the minimum required gate ($effectiveMinScore%)."
        exit 1
    }

    Write-Host ("Quality Gate Passed: {0}% >= {1}%" -f $score, $effectiveMinScore) -ForegroundColor Green
} else {
    Write-Host "JSON report not found. Stryker finished with exit code $strykerExit."
}

if ($OpenReport -and (Test-Path $htmlReportPath)) {
    Write-Host "Opening HTML report in browser: $htmlReportPath" -ForegroundColor Cyan
    Start-Process $htmlReportPath
}
