<#
.SYNOPSIS
    EcoTrack Code Quality & Metrics pipeline (SE3112).

.DESCRIPTION
    One script, used both on a laptop and in GitHub Actions:
      1. Formatting check        dotnet format (whitespace rules from .editorconfig)
      2. Static analysis         Roslyn .NET analyzers during the build -> SARIF files
      3. Tests + coverage        dotnet test + Coverlet (cobertura + opencover)
      4. Coverage report         ReportGenerator (HTML, risk hotspots, badges, JSON)
      5. SonarQube (optional)    code smells, technical debt, duplication, ratings, quality gate
                                 (re-builds under the scanner and re-uses the coverage files from step 3)
      6. Quality gate            thresholds from quality/quality-gate.json -> PASS / FAIL
    Everything is written to ./quality-reports

.EXAMPLE
    ./quality/run-code-quality.ps1
    ./quality/run-code-quality.ps1 -Sonar -SonarToken squ_xxx -OpenReport
#>
[CmdletBinding()]
param(
    [switch]$Sonar,
    [string]$SonarUrl = 'http://localhost:9000',
    [string]$SonarToken = $env:SONAR_TOKEN,
    [string]$SonarOrganization = '',
    [switch]$SkipFormat,
    [switch]$OpenReport
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_ROLL_FORWARD = 'Major'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $repoRoot

$solution = 'EcoTrack.slnx'
$out = Join-Path $repoRoot 'quality-reports'
$sarifDir = Join-Path $out 'sarif'
$testDir = Join-Path $out 'TestResults'
$coverageDir = Join-Path $out 'coverage'
$config = Get-Content (Join-Path $repoRoot 'quality/quality-gate.json') -Raw | ConvertFrom-Json
$stopwatch = [Diagnostics.Stopwatch]::StartNew()

function Write-Step([string]$Number, [string]$Title) {
    Write-Host ''
    Write-Host ("[{0}] {1}" -f $Number, $Title) -ForegroundColor Cyan
    Write-Host ('-' * 60) -ForegroundColor DarkGray
}

function Install-DotnetTool([string]$Package, [string]$Command) {
    $toolsDir = Join-Path $HOME '.dotnet/tools'
    if (($env:PATH -split [IO.Path]::PathSeparator) -notcontains $toolsDir) {
        $env:PATH = $toolsDir + [IO.Path]::PathSeparator + $env:PATH
    }
    if (-not (Get-Command $Command -ErrorAction SilentlyContinue)) {
        Write-Host "Installing $Package ..."
        dotnet tool install --global $Package | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Could not install $Package" }
    }
}

function Format-Rating($value) {
    switch ([int][double]$value) { 1 { 'A' } 2 { 'B' } 3 { 'C' } 4 { 'D' } 5 { 'E' } default { '-' } }
}

function Format-Debt($minutes) {
    $m = [int]$minutes
    $days = [math]::Floor($m / 480)
    $hours = [math]::Floor(($m % 480) / 60)
    $mins = $m % 60
    if ($days -gt 0) { return "{0}d {1}h" -f $days, $hours }
    if ($hours -gt 0) { return "{0}h {1}min" -f $hours, $mins }
    return "{0}min" -f $mins
}

# ------------------------------------------------------------------ setup
Write-Host '============================================================' -ForegroundColor Green
Write-Host '  EcoTrack - Code Quality & Metrics Pipeline (SE3112)' -ForegroundColor Green
Write-Host '============================================================' -ForegroundColor Green

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $sarifDir -Force | Out-Null

Install-DotnetTool 'dotnet-reportgenerator-globaltool' 'reportgenerator'
if ($Sonar) {
    if ([string]::IsNullOrWhiteSpace($SonarToken)) { throw 'Sonar needs a token: -SonarToken <token> or $env:SONAR_TOKEN' }
    Install-DotnetTool 'dotnet-sonarscanner' 'dotnet-sonarscanner'
}

# ------------------------------------------------------------------ 1. formatting
$formatIssues = 0
if (-not $SkipFormat) {
    Write-Step '1/6' 'Formatting check (dotnet format whitespace, .editorconfig)'
    $formatDir = Join-Path $out 'format'
    dotnet format whitespace $repoRoot --folder --verify-no-changes --report $formatDir --verbosity quiet `
        --exclude '**/obj/**' '**/bin/**' '**/node_modules/**' | Out-Null
    $formatReport = Join-Path $formatDir 'format-report.json'
    if (Test-Path $formatReport) {
        $formatData = @(Get-Content $formatReport -Raw | ConvertFrom-Json)
        foreach ($doc in $formatData) { $formatIssues += @($doc.FileChanges).Count }
        Write-Host ("Files needing formatting: {0}   Formatting changes: {1}" -f $formatData.Count, $formatIssues)
    }
    else {
        Write-Host 'All C# files follow the formatting rules.' -ForegroundColor Green
    }
}
else {
    Write-Step '1/6' 'Formatting check skipped (-SkipFormat)'
}

# ------------------------------------------------------------------ 2. build + static analysis
Write-Step '2/6' 'Build with Roslyn static analysis (warnings saved to SARIF)'
dotnet build $solution -c Release --no-incremental -tl:off -nologo -clp:ErrorsOnly -p:EcoQualitySarif=true
if ($LASTEXITCODE -ne 0) { throw 'Build failed - fix the compile errors above first.' }

$findings = New-Object System.Collections.Generic.List[object]
foreach ($file in Get-ChildItem $sarifDir -Filter '*.sarif') {
    $sarif = Get-Content $file.FullName -Raw | ConvertFrom-Json
    foreach ($run in $sarif.runs) {
        foreach ($result in @($run.results)) {
            if ($null -eq $result) { continue }
            if ($result.suppressions -and @($result.suppressions).Count -gt 0) { continue }
            $level = if ($result.level) { $result.level } else { 'warning' }
            $location = ''
            if ($result.locations) {
                $physical = $result.locations[0].physicalLocation
                $location = "{0}:{1}" -f ($physical.artifactLocation.uri -replace '^.*?/(src|tests)/', '$1/'), $physical.region.startLine
            }
            $findings.Add([pscustomobject]@{
                Project  = $file.BaseName
                Rule     = $result.ruleId
                Level    = $level
                Message  = $result.message.text
                Location = $location
            })
        }
    }
}
$findings | Export-Csv (Join-Path $out 'static-analysis-findings.csv') -NoTypeInformation

$warnings = @($findings | Where-Object { $_.Level -eq 'warning' -or $_.Level -eq 'error' })
$suggestions = @($findings | Where-Object { $_.Level -eq 'note' })
Write-Host ("Analyzer warnings: {0}   Suggestions: {1}" -f $warnings.Count, $suggestions.Count)

if ($warnings.Count -gt 0) {
    Write-Host ''
    Write-Host 'Top rules (warnings):' -ForegroundColor Yellow
    $warnings | Group-Object Rule | Sort-Object Count -Descending | Select-Object -First 8 |
        ForEach-Object {
            $text = ([string]$_.Group[0].Message) -replace '\s+', ' '
            if ($text.Length -gt 70) { $text = $text.Substring(0, 70) + '...' }
            '{0,5}  {1,-8} {2}' -f $_.Count, $_.Name, $text
        } |
        Out-Host
    Write-Host ''
    Write-Host 'Warnings per project:' -ForegroundColor Yellow
    $warnings | Group-Object Project | Sort-Object Count -Descending |
        ForEach-Object { '{0,5}  {1}' -f $_.Count, $_.Name } | Out-Host
}

# ------------------------------------------------------------------ 3. tests + coverage
Write-Step '3/6' 'Unit tests with Coverlet code coverage'
dotnet test $solution -c Release --no-build --nologo `
    --settings quality/coverlet.runsettings `
    --collect:'XPlat Code Coverage' `
    --results-directory $testDir `
    --logger 'trx'
$testExit = $LASTEXITCODE
if ($testExit -eq 0) { Write-Host 'All unit tests passed.' -ForegroundColor Green }
else { Write-Host 'Some unit tests FAILED (see above).' -ForegroundColor Red }

# ------------------------------------------------------------------ 4. coverage report
Write-Step '4/6' 'Coverage report (ReportGenerator)'
reportgenerator "-reports:$testDir/**/coverage.cobertura.xml" "-targetdir:$coverageDir" `
    '-reporttypes:Html;JsonSummary;Badges;MarkdownSummaryGithub;TextSummary' `
    '-assemblyfilters:-*.Tests' '-title:EcoTrack Code Coverage' '-verbosity:Warning'
if ($LASTEXITCODE -ne 0) { throw 'ReportGenerator failed - no coverage files were produced.' }

$coverage = (Get-Content (Join-Path $coverageDir 'Summary.json') -Raw | ConvertFrom-Json)
$lineCov = [double]$coverage.summary.linecoverage
$branchCov = [double]$coverage.summary.branchcoverage
Write-Host ''
Write-Host ('{0,-34} {1,8} {2,8}' -f 'Assembly', 'Line %', 'Branch %') -ForegroundColor Yellow
foreach ($asm in $coverage.coverage.assemblies) {
    Write-Host ('{0,-34} {1,8} {2,8}' -f $asm.name, $asm.coverage, $asm.branchcoverage)
}
Write-Host ('{0,-34} {1,8} {2,8}' -f 'TOTAL', $lineCov, $branchCov) -ForegroundColor Yellow

# ------------------------------------------------------------------ 5. sonarqube
$sonarGate = $null
$sonarMeasures = @{}
if ($Sonar) {
    Write-Step '5/6' "SonarQube analysis -> $SonarUrl"
    $sonarCfg = $config.sonar
    $beginArgs = @(
        'sonarscanner', 'begin',
        "/k:$($sonarCfg.projectKey)",
        "/n:$($sonarCfg.projectName)",
        "/d:sonar.host.url=$SonarUrl",
        "/d:sonar.token=$SonarToken",
        '/d:sonar.cs.opencover.reportsPaths=quality-reports/TestResults/**/coverage.opencover.xml',
        '/d:sonar.cs.vstest.reportsPaths=quality-reports/TestResults/*.trx',
        '/d:sonar.exclusions=**/bin/**,**/obj/**,**/node_modules/**,**/dist/**,**/dist_test/**,coverage/**,quality-reports/**,tests/jmeter/**,src/apps/web/single-file-kyc.html,src/apps/web/audit-report.json',
        '/d:sonar.coverage.exclusions=**/Program.cs,tests/**,src/apps/web/**',
        '/d:sonar.qualitygate.wait=true',
        '/d:sonar.qualitygate.timeout=300'
    )
    if ($SonarOrganization) { $beginArgs += "/o:$SonarOrganization" }
    & dotnet @beginArgs
    if ($LASTEXITCODE -ne 0) { throw 'SonarScanner begin failed (is SonarQube running? is the token valid?)' }

    dotnet build $solution -c Release --no-incremental -tl:off -nologo -clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { throw 'Build for SonarQube failed.' }

    & dotnet sonarscanner end "/d:sonar.token=$SonarToken"
    $sonarExit = $LASTEXITCODE

    $basic = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$($SonarToken):"))
    $headers = @{ Authorization = "Basic $basic" }
    $api = "$($SonarUrl.TrimEnd('/'))/api"
    $key = $config.sonar.projectKey
    try {
        $gateResponse = Invoke-RestMethod -Uri "$api/qualitygates/project_status?projectKey=$key" -Headers $headers
        $sonarGate = $gateResponse.projectStatus.status

        $wanted = 'ncloc', 'coverage', 'bugs', 'vulnerabilities', 'code_smells', 'security_hotspots',
                  'sqale_index', 'sqale_debt_ratio', 'duplicated_lines_density', 'complexity',
                  'cognitive_complexity', 'reliability_rating', 'security_rating', 'sqale_rating'
        $available = (Invoke-RestMethod -Uri "$api/metrics/search?ps=500" -Headers $headers).metrics.key
        $keys = ($wanted | Where-Object { $available -contains $_ }) -join ','
        $measures = Invoke-RestMethod -Uri "$api/measures/component?component=$key&metricKeys=$keys" -Headers $headers
        foreach ($m in $measures.component.measures) { $sonarMeasures[$m.metric] = $m.value }

        Write-Host ''
        Write-Host 'SonarQube metrics' -ForegroundColor Yellow
        $rows = [ordered]@{
            'Lines of code'            = $sonarMeasures['ncloc']
            'Coverage (Sonar)'         = "$($sonarMeasures['coverage'])%"
            'Bugs'                     = $sonarMeasures['bugs']
            'Vulnerabilities'          = $sonarMeasures['vulnerabilities']
            'Security hotspots'        = $sonarMeasures['security_hotspots']
            'Code smells'              = $sonarMeasures['code_smells']
            'Technical debt'           = if ($sonarMeasures['sqale_index']) { Format-Debt $sonarMeasures['sqale_index'] } else { '-' }
            'Debt ratio'               = "$($sonarMeasures['sqale_debt_ratio'])%"
            'Duplicated lines'         = "$($sonarMeasures['duplicated_lines_density'])%"
            'Cyclomatic complexity'    = $sonarMeasures['complexity']
            'Cognitive complexity'     = $sonarMeasures['cognitive_complexity']
            'Reliability rating'       = Format-Rating $sonarMeasures['reliability_rating']
            'Security rating'          = Format-Rating $sonarMeasures['security_rating']
            'Maintainability rating'   = Format-Rating $sonarMeasures['sqale_rating']
        }
        foreach ($row in $rows.GetEnumerator()) { Write-Host ('  {0,-24} {1}' -f $row.Key, $row.Value) }
        Write-Host "  Dashboard: $SonarUrl/dashboard?id=$key" -ForegroundColor Cyan
    }
    catch {
        Write-Host "Could not read results from SonarQube API: $($_.Exception.Message)" -ForegroundColor Red
        if (-not $sonarGate) { $sonarGate = if ($sonarExit -eq 0) { 'OK' } else { 'ERROR' } }
    }
}
else {
    Write-Step '5' 'SonarQube skipped (add -Sonar -SonarToken <token> to include it)'
}

# ------------------------------------------------------------------ 6. quality gate
Write-Step '6/6' 'Quality gate'
$gate = $config
$checks = New-Object System.Collections.Generic.List[object]
function Add-Check([string]$Name, [string]$Actual, [string]$Rule, [bool]$Passed, [bool]$Blocking = $true) {
    $status = if ($Passed) { 'PASS' } elseif ($Blocking) { 'FAIL' } else { 'WARN' }
    $checks.Add([pscustomobject]@{ Check = $Name; Actual = $Actual; Rule = $Rule; Status = $status })
}

Add-Check 'Unit tests' ($(if ($testExit -eq 0) { 'all passed' } else { 'failures' })) 'must all pass' ($testExit -eq 0)
Add-Check 'Line coverage' "$lineCov%" ">= $($gate.coverage.minLineCoverage)%" ($lineCov -ge $gate.coverage.minLineCoverage)
Add-Check 'Branch coverage' "$branchCov%" ">= $($gate.coverage.minBranchCoverage)%" ($branchCov -ge $gate.coverage.minBranchCoverage)
Add-Check 'Analyzer warnings' "$($warnings.Count)" "<= $($gate.staticAnalysis.maxWarnings)" ($warnings.Count -le $gate.staticAnalysis.maxWarnings)
if (-not $SkipFormat) {
    Add-Check 'Formatting' "$formatIssues changes" '0 changes' ($formatIssues -eq 0) ([bool]$gate.staticAnalysis.failOnFormatting)
}
if ($Sonar) {
    Add-Check 'SonarQube gate' "$sonarGate" "'$($config.sonar.gateName)' = OK" ($sonarGate -eq 'OK')
}

foreach ($c in $checks) {
    $color = switch ($c.Status) { 'PASS' { 'Green' } 'WARN' { 'Yellow' } default { 'Red' } }
    Write-Host ('  {0,-5} {1,-18} {2,-14} (rule {3})' -f $c.Status, $c.Check, $c.Actual, $c.Rule) -ForegroundColor $color
}
$failed = @($checks | Where-Object { $_.Status -eq 'FAIL' }).Count -gt 0

# ------------------------------------------------------------------ summary file
$md = New-Object System.Text.StringBuilder
[void]$md.AppendLine('## EcoTrack Code Quality & Metrics')
[void]$md.AppendLine('')
[void]$md.AppendLine("**Quality gate: $(if ($failed) { 'FAILED' } else { 'PASSED' })**")
[void]$md.AppendLine('')
[void]$md.AppendLine('| Check | Actual | Rule | Status |')
[void]$md.AppendLine('|---|---|---|---|')
foreach ($c in $checks) { [void]$md.AppendLine("| $($c.Check) | $($c.Actual) | $($c.Rule) | $($c.Status) |") }
[void]$md.AppendLine('')
if ($warnings.Count -gt 0) {
    [void]$md.AppendLine('### Top static-analysis rules')
    [void]$md.AppendLine('| Rule | Count |')
    [void]$md.AppendLine('|---|---|')
    $warnings | Group-Object Rule | Sort-Object Count -Descending | Select-Object -First 10 |
        ForEach-Object { [void]$md.AppendLine("| $($_.Name) | $($_.Count) |") }
    [void]$md.AppendLine('')
}
$ghCoverage = Join-Path $coverageDir 'SummaryGithub.md'
if (Test-Path $ghCoverage) { [void]$md.AppendLine((Get-Content $ghCoverage -Raw)) }
$md.ToString() | Set-Content (Join-Path $out 'quality-summary.md') -Encoding UTF8
if ($env:GITHUB_STEP_SUMMARY) { $md.ToString() | Add-Content $env:GITHUB_STEP_SUMMARY -Encoding UTF8 }

$stopwatch.Stop()
Write-Host ''
Write-Host ("Reports: {0}" -f $out)
Write-Host ("  Coverage HTML : {0}" -f (Join-Path $coverageDir 'index.html'))
Write-Host ("  Findings CSV  : {0}" -f (Join-Path $out 'static-analysis-findings.csv'))
Write-Host ("  Summary       : {0}" -f (Join-Path $out 'quality-summary.md'))
Write-Host ("Finished in {0:N0}s" -f $stopwatch.Elapsed.TotalSeconds)

if ($OpenReport) { Invoke-Item (Join-Path $coverageDir 'index.html') }

if ($failed) {
    Write-Host 'QUALITY GATE FAILED' -ForegroundColor Red
    exit 1
}
Write-Host 'QUALITY GATE PASSED' -ForegroundColor Green
exit 0
