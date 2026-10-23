# Mutation Testing (SE3112)

**Owner:** Ekanayake E M D D B (Dilanka) — IT24610796
**Role:** Mutation Testing — automated fault-injection testing, mutant resilience metrics and a quality gate for the EcoTrack microservices.

## Tools and why

| Tool | What it measures | Why this tool |
|---|---|---|
| Stryker.NET (`dotnet-stryker`) | Test suite fault-detection effectiveness (Mutation score %) | Official, standard mutation testing tool for modern .NET (`net10.0`); mutates the Roslyn AST |
| `CoverageBasedTest` filter | Test execution runtime optimization | Maps test coverage to source lines so Stryker only executes tests that touch the mutated code |
| Stryker HTML reporter | Per-mutant diff, killed/survived status, mutation tree | Interactive report for human review; inspects exactly which statement was changed and which test caught it |
| Stryker JSON reporter | Mutation score, killed/survived counts, threshold validation | Machine-readable output for the CI/CD pipeline and automated quality gate enforcement |
| xUnit + Moq | Test runner and mocking framework | Existing unit test framework of the EcoTrack microservices solution |

## Files

```
mutation/README.md                    this file — station overview, tool choices and quality gate
mutation/stryker-config.json          ALL thresholds in one place (targets, reporters, quality gates)
mutation/run-mutation.ps1             the pipeline (same script locally and in GitHub Actions)
.github/workflows/mutation-testing.yml CI job (push / PR / manual / callable from the main pipeline)
src/Identity/Services/AuthService.cs  target service under test (core auth, registration, tokens)
tests/IdentityService.Tests/AuthServiceTests.cs unit test suite killing injected mutants
```

## Run it

```powershell
# 1. Mutation pipeline for core domain service (about 1 minute, 82.86% score)
./mutation/run-mutation.ps1 -OpenReport

# 2. Pipeline across all domain services in IdentityService
./mutation/run-mutation.ps1 -Scope AllServices -OpenReport

# 3. Solution-wide pipeline across all microservices (Identity, Logistics, Marketplace)
./mutation/run-mutation.ps1 -Scope AllProjects
```

Outputs go to `tests/IdentityService.Tests/StrykerOutput/` (git-ignored):
`reports/mutation-report.html`, `reports/mutation-report.json`.

## Quality gate

Local / CI gate (`mutation/stryker-config.json`):

| Check | Rule | Status |
|---|---|---|
| Unit tests | all pass (66 / 66) | PASS |
| Mutation score (high) | ≥ 80% (high quality target) | PASS (82.86%) |
| Mutation score (low) | ≥ 60% (minimum allowed gate) | PASS (82.86%) |
| Mutation score (break) | ≥ 50% (fails build if below) | PASS (82.86%) |
| Mutants killed | 87 / 105 tested mutants | +30 killed over baseline |

## CI

`.github/workflows/mutation-testing.yml` runs on push/PR to `Dev`, `main`, `Deploy`, can be started manually, and can be called from the main pipeline:

```yaml
  mutation-testing:
    uses: ./.github/workflows/mutation-testing.yml
```

Reports are uploaded as the `stryker-mutation-report` artifact and the gate summary table appears in the run's Summary tab.
