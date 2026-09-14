# Search portfolio implementation review

Completed **14 September 2026**. Target: offline BalanceHarness. The [frozen experiment plan](Tower-Search-Portfolio-Plan.md) governs one separate execution.

## Implemented behavior

V19 compares a 1,536-candidate deep search with a fixed 768-deep plus 512+256 isolated portfolio. Both units receive 384 initial fresh evaluations, up to 16,384 proposals and identical discovery/screening/confirmation schedules. The portfolio deep component has a distinct named stream. All components keep separate state and ancestry; the isolated pair retains v18's allocation rule and exact trajectories under identical synthetic measurements.

The complete three-component portfolio merges before screening. Duplicate complete recipes retain all charges and origins; conflicting measurements are rejected. Both units have one top-32 screen per root. Original nominees, screened nominees, every required ceiling breach and all 112 prior controls enter the frozen confirmation family. The numerical 2/3 gate is unchanged.

V19 alone permits 1,536 candidates per comparison unit, 16,384 proposals, 112 imported controls, capacity 144, 159,744 fights, six execution hours and 8 GiB. Historical validators and limits remain unchanged. Existing allocation records, continuation state, integrity checks, attempt accounting and captured-executable reconstruction are reused.

## Changed files

- [TowerSearchPortfolio.cs](../LL/tools/BalanceHarness/TowerSearchPortfolio.cs) adds the version, method identities, separate stream and fixed component budgets.
- [TowerBossGeneration.cs](../LL/tools/BalanceHarness/TowerBossGeneration.cs), [TowerLateAllocation.cs](../LL/tools/BalanceHarness/TowerLateAllocation.cs) and [TowerSearchAllocation.cs](../LL/tools/BalanceHarness/TowerSearchAllocation.cs) add the fourth component, reuse persistent allocation and merge all three portfolio sources.
- [TowerGenerationComparisonDesign.cs](../LL/tools/BalanceHarness/TowerGenerationComparisonDesign.cs), [TowerFeedbackBenchmark.cs](../LL/tools/BalanceHarness/TowerFeedbackBenchmark.cs) and [TowerFeedbackBenchmarkRun.cs](../LL/tools/BalanceHarness/TowerFeedbackBenchmarkRun.cs) bind comparison groups, controls and policy-specific resource limits.
- [TowerBossDiscoveryContract.cs](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs), [TowerBossPartyGenerator.cs](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs), [TowerPartyCoverage.cs](../LL/tools/BalanceHarness/TowerPartyCoverage.cs) and [TowerLoadoutComposition.cs](../LL/tools/BalanceHarness/TowerLoadoutComposition.cs) register v19 under existing legality/mechanic rules.
- [Program.cs](../LL/tools/BalanceHarness/Program.cs) adds `tower-portfolio-prepare/check/run/verify` commands.
- [Portfolio tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerSearchPortfolioTests.cs) and [shared comparison tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerFeedbackBenchmarkTests.cs) cover equal effort, both choices, isolated parity, separate libraries/parents, cancellation, exhaustion, tampered allocation, duplicates, controls, nomination integrity, overflow and the gate.
- Six active guides, including the harness README, record current status, commands and the authoritative ledger. The new plan and this review retain the implementation boundary.

## Verification

**388 distinct tests passed** through `build/run-tests.ps1`, including **39 new-policy cases**. The focused batch's 39 overlap the broader 388 and are not added. The first focused run exposed the legacy 96-reference import limit; its v19-only limit was corrected to 112, then focused and broader suites passed. Six pre-existing warnings remain; no new warning was introduced.

The saved-measurement audit reproduced complete generation and frozen shortlists for **18,240 evaluations and 48 feedback probes** across v13 through v18. Gameplay assemblies and runtime matched. The [historical audit](../TestResults/balance/tower-search-portfolio-work-20260914/historical-audit.json) used zero new fights or seeds.

The [preflight](../TestResults/balance/tower-search-portfolio-work-20260914/preflight.json) passed nine checks: unchanged prepared copy, four changed protocol limits/retry settings, omitted control, extra file, started package and repeated execution. It binds all 112 controls, source, executable, unchanged content/settings/gameplay and the **480,707-reservation ledger**. Three fresh roots are **-1038588442, -867718476 and -242256866**; all 12 derived component streams are distinct. The producing source and plan are retained in [producing inputs](../TestResults/balance/tower-search-portfolio-work-20260914/producing-inputs.json).

The sandbox build could not read the user NuGet configuration. The required test runner and audit build succeeded with the necessary filesystem access; no required command remains blocked. No game content, configuration, migration, deployment, catalog or default optimizer changed. Implementation readiness does not establish improved reliability; the separate experiment must complete reconstruction before a result is reported.

## Later checkout changes

Closure detected concurrent gameplay/content edits outside this task. The 388-test result, source bindings and captured experiment apply to the producing version verified before those edits. The later checkout was not retested by this task. See the [version boundary](Tower-Search-Portfolio-Review.md#concurrent-checkout-boundary) and preserved drift receipt before building a follow-up.
