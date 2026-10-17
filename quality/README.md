# Code Quality & Metrics (SE3112)

Owner: Rajapaksha R M M D C (Diloosha) — IT24610798
Role: Code Quality & Metrics — automated static analysis, coverage metrics and a quality gate for the EcoTrack microservices.

## Tools and why

| Tool | What it measures | Why this tool |
|---|---|---|
| Roslyn .NET analyzers (`Directory.Build.props`) | Bugs, security, performance and style rules (CAxxxx, IDExxxx, CSxxxx) on every build | Built into the .NET 10 SDK, no extra packages, runs in the IDE, locally and in CI |
| `.editorconfig` | The team coding standard (naming, style, rule severities) | One file read by Visual Studio, VS Code, Rider, `dotnet build` and `dotnet format` |
| `dotnet format` | Formatting violations | Ships with the SDK; `--verify-no-changes` makes it a check, not a fixer |
| Coverlet (`coverlet.collector`) | Line and branch coverage of the 3 test projects | The standard .NET coverage collector; outputs cobertura and opencover |
| ReportGenerator | HTML coverage report, per-class coverage, risk hotspots (cyclomatic complexity, CRAP score), badges | Turns raw XML into something a human can read |
| SonarQube Community | Code smells, technical debt, duplication, cognitive complexity, A–E ratings, quality gate | Industry-standard dashboard; one view for C# and React code |

## Files

```
Directory.Build.props                 turns analyzers on for every project
.editorconfig                         coding standard + rule severities (with reasons)
quality/coverlet.runsettings          coverage formats and exclusions
quality/quality-gate.json             ALL thresholds in one place (local gate + SonarQube gate)
quality/run-code-quality.ps1          the pipeline (same script locally and in GitHub Actions)
quality/sonarqube/docker-compose.sonar.yml   local SonarQube server
quality/sonarqube/setup-quality-gate.ps1     creates the "EcoTrack Gate" through the SonarQube Web API
.github/workflows/code-quality.yml    CI job (push / PR / manual / callable from the main pipeline)
```

## Run it

```powershell
# 1. Quality pipeline without SonarQube (about 1-2 minutes)
./quality/run-code-quality.ps1 -OpenReport

# 2. Start SonarQube (first time takes 1-2 minutes to boot)
docker compose -f quality/sonarqube/docker-compose.sonar.yml up -d
#    open http://localhost:9000  -> admin / admin -> set a new password
#    My Account -> Security -> Generate Token (type: User token)
$env:SONAR_TOKEN = "squ_paste_your_token"

# 3. One-time: create the project + custom quality gate
./quality/sonarqube/setup-quality-gate.ps1

# 4. Full pipeline including SonarQube
./quality/run-code-quality.ps1 -Sonar -OpenReport
```

Outputs go to `quality-reports/` (git-ignored):
`coverage/index.html`, `static-analysis-findings.csv`, `sarif/*.sarif`, `format/format-report.json`, `quality-summary.md`.

## Quality gate

Local / CI gate (`quality/quality-gate.json`):

| Check | Rule |
|---|---|
| Unit tests | all pass |
| Line coverage | ≥ 20% |
| Branch coverage | ≥ 10% |
| Analyzer warnings | ≤ maxWarnings (ratchet: set just above today's baseline, lower it as debt is paid) |
| Formatting | reported, non-blocking |

SonarQube "EcoTrack Gate":

| Metric | Fails when | Scope |
|---|---|---|
| Coverage | < 20% | overall code (baseline) |
| Duplicated lines | > 5% | overall code |
| Maintainability rating | worse than B | overall code |
| Coverage on new code | < 80% | new code ("clean as you code") |
| Duplication on new code | > 3% | new code |
| New issues | > 0 | new code |
| Security hotspots reviewed | < 100% | new code |

## CI

`.github/workflows/code-quality.yml` runs on push/PR to `Dev`, `main`, `Deploy`, can be started manually, and can be called from the main pipeline:

```yaml
  code-quality:
    uses: ./.github/workflows/code-quality.yml
    secrets: inherit
```

If the repository secret `SONAR_TOKEN` and variable `SONAR_ORGANIZATION` are set (SonarCloud), the job also uploads to SonarCloud. Without them it runs the local metrics gate. Reports are uploaded as the `code-quality-reports` artifact and the gate table appears in the run's Summary tab.
