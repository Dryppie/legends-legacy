# Loadout-retention implementation and comparison review

13 September 2026. **Complete and verified: reliability Fail 0/3; adoption Hold.** 62,464 fights completed with zero retries. The requirement remains at least two of three restarts.

Keep v15 experimental and leave the default unchanged. The 64/64 retention allocation expanded module reuse but failed reliability at 0/3 and performed worse than screened v13 in restart one. This completed comparison provides no support for adopting it.

## What changed and why

The [saved-history audit](../TestResults/balance/tower-loadout-retention-work-20260913/diagnosis.json) verified 682 recorded library hashes across six earlier v13 arms with zero new fights. Earlier first wins occurred at evaluated candidates 282 and 256. Two independently supported older recipes were born at 241 and 258, initially scoring 0/8. Their ancestral modules remained available: the evidence does not establish eviction as the cause of failure. Only 182–260 of roughly 1,900 distinct observed modules entered the ranked library during refinement. This motivated a bounded test of broader reuse, without importing successful recipes.

The [frozen plan](Tower-Loadout-Retention-Plan.md) changes one mechanism: v15 retains 64 highest-ranked modules plus 64 chosen for structural distance. Membership differences precede ordering differences; complete-party rank and source slot break ties. The unchanged v13 comparator retains its ranked 128. Both use the same initial 96 candidates, 384 × 8 discovery budget, operators and player budget. Both get 32 × 64 independent screening and complete 512-trial confirmation. All 48 prior confirmed recipes stay external controls. Capacity rose to 80 solely to retain them and every nomination; overflow stops without truncation.

## Measured result

| Restart / generation seed | Screened v13 wins/512 | Retention wins/512 | Retention adjusted rate interval | Paired improvement interval (percentage points) | Reliability |
| --- | ---: | ---: | --- | --- | --- |
| 1 / 1414588091 | 87 | 0 | 0.00%–2.39% | -22.93–-10.39 | Fail |
| 2 / 825587780 | 0 | 0 | 0.00%–2.39% | -1.96–1.96 | Fail |
| 3 / 1193373650 | 0 | 0 | 0.00%–2.39% | -1.96–1.96 | Fail |

All six retention nominees won 0/512, with adjusted upper bounds of 2.39%, below the 10% target. The first screened v13 primary won 87/512 (16.99%) and is a newly generated recipe with supported viability; its adjusted lower bound is 11.92%. Retention's paired difference against that primary is −16.99 percentage points (adjusted interval −22.93 to −10.39). The other paired primaries both scored zero. The 62-recipe family Pass includes seven viable prior controls and this one new v13 team; it does not make generation reliable.

The historical anchor won **0/512** and the strongest preceding control won **156/512**. The complete **62-recipe** family returned ordinary/joint **Pass/Pass**, with **8** supported viable recipes and **0** observed confirmation ceiling breaches. Family acceptance and generation reliability have different scopes.

The unchanged reliability gate requires a supported rate ≥10%, supported paired improvement over the same-restart screened v13 primary, and recovery relative to the historical anchor within ten percentage points. Strong-control comparisons are reported separately. Joint alpha is .025 across the full rate family and .025 across nine differences (eighteen discordance intervals). Independent Python calculations reproduced every interval within 1e-8. These approximate intervals do not establish lifetime repeated-study coverage, global optimality or acceptance of the full discovered family.

## Mechanism check

The [post-hoc coverage check](../TestResults/balance/tower-loadout-retention-work-20260913/coverage.json) counts actual donor uses outside the ranked 128 at the same completed prefix. Counts include all composition proposals, including duplicates or rejected proposals; only completed candidates supply fitness. It measures whether the changed library was used; it is not a replay of unchanged descendants or causal proof of a performance difference.

| Method / seed | Distinct ordered loadouts observed | Composition donor uses outside same-prefix ranked 128 | Distinct such donors | Positive discovery candidates |
| --- | ---: | ---: | ---: | ---: |
| loadout-composition-joint / 1414588091 | 1920 | 0/219 | 0 | 13 |
| retained-loadout-composition-joint / 1414588091 | 1906 | 115/235 | 77 | 0 |
| loadout-composition-joint / 825587780 | 1904 | 0/224 | 0 | 0 |
| retained-loadout-composition-joint / 825587780 | 1924 | 105/227 | 82 | 0 |
| loadout-composition-joint / 1193373650 | 1916 | 0/214 | 0 | 0 |
| retained-loadout-composition-joint / 1193373650 | 1903 | 93/227 | 69 | 0 |

A separate [genealogy audit](../TestResults/balance/tower-loadout-retention-work-20260913/lineages.json) examines twelve completed v13/v15 arms across three campaigns. In every arm, the final four training leaders share a common ancestor born between evaluated candidates 240 and 351. This explains why many distinct beam members do not necessarily represent independent search lineages. It is descriptive evidence for investigating whole-party lineage retention, not proof that convergence caused a failure.

## Retained work and verification

All **2,304 evaluated candidates**, **1,850 distinct complete recipes**, **192 screening slots** and **62 confirmation recipes** are retained, including zero-win recipes. See [results](../TestResults/balance/tower-loadout-retention-work-20260913/results.json), [seed-free builds](../TestResults/balance/tower-loadout-retention-work-20260913/exports/saved-builds.md), and [all discovery recipes](../TestResults/balance/tower-loadout-retention-20260913/all-evaluated-recipes.json). Full generation evidence retains ancestry and module hashes.

Execution used **2200.93 seconds** and **1,528,002,158 campaign bytes** (1.42 GiB). Durable accounting matches all 62,464 starts/completions. No retries, resumes, extensions or diagnostic combat replays. The [latest ledger](../TestResults/balance/tower-loadout-retention-20260913/seed-ledger.json) retains all 477,260 prior reservations plus 587 new values: **477,847 distinct values**. Exclude every array.

**427 relevant tests passed**, including 28 additional cases: ranked-core retention, structural novelty, order sensitivity, determinism, unchanged comparator/initial population, same-arm ancestry, equal budgets, all controls, complete screening, tampering, overflow, interrupted accounting and exact 2/3 reliability. The initial broad run passed 365/366; one new assertion incorrectly compared array references. After changing it to compare serialized content, the corrected case and 61 accounting/evaluator tests passed. No implementation change or campaign retry resulted.

Both earlier searches reconstructed all **4,416 evaluations and 48 feedback probes** exactly, with identical shortlists and gameplay assemblies, under a no-combat guard. The new complete campaign passed captured verification, including generation, every screen, confirmation, provenance, inventory hashes and accounting. Thirteen preceding evidence packages / 172,986 files passed preservation checks. The [completion receipt](../TestResults/balance/tower-loadout-retention-work-20260913/final-verification.json) seals producing source, results, tests, exports and documentation.

| Files | Change |
| --- | --- |
| [TowerLoadoutRetention.cs](../LL/tools/BalanceHarness/TowerLoadoutRetention.cs) | Ranked core and deterministic structural reserve from completed same-arm modules. |
| [TowerGenerationComparisonDesign.cs](../LL/tools/BalanceHarness/TowerGenerationComparisonDesign.cs), [TowerFeedbackBenchmark.cs](../LL/tools/BalanceHarness/TowerFeedbackBenchmark.cs), [TowerFeedbackBenchmarkRun.cs](../LL/tools/BalanceHarness/TowerFeedbackBenchmarkRun.cs) | Shared versioned v14/v15 comparison; full controls, budgets, selection and captured verification. |
| [TowerBossGeneration.cs](../LL/tools/BalanceHarness/TowerBossGeneration.cs), [TowerBossDiscoveryContract.cs](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs), [TowerBossPartyGenerator.cs](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs), [TowerPartyCoverage.cs](../LL/tools/BalanceHarness/TowerPartyCoverage.cs), [TowerLoadoutComposition.cs](../LL/tools/BalanceHarness/TowerLoadoutComposition.cs), [Program.cs](../LL/tools/BalanceHarness/Program.cs) | Opt-in version/method, unchanged existing operators and additive retention commands. |
| [Retention tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerLoadoutRetentionTests.cs), [comparison tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerFeedbackBenchmarkTests.cs) | New kernel regressions and the same contract suite exercised for both comparison versions. |
| This review, frozen plan and six active guides | Measured result, current ledger, saved builds and next boundary. |

Commands executed:

```powershell
dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-restore
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore -p:BuildProjectReferences=false -p:StaticWebAssetsEnabled=false
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBoss|FullyQualifiedName~BalanceHarnessTowerLoadout|FullyQualifiedName~BalanceHarnessTowerFinalistRescreen|FullyQualifiedName~BalanceHarnessTowerFeedback|FullyQualifiedName~BalanceHarnessTowerGenerationFeedback|FullyQualifiedName~BalanceHarnessTowerCompact|FullyQualifiedName~BalanceHarnessTowerSearchBenchmark|FullyQualifiedName~BalanceHarnessTowerPrecision|FullyQualifiedName~BalanceHarnessTowerStaged'
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerLoadoutRetentionTests.Retains_ranked_core|FullyQualifiedName~BalanceHarnessTowerBulkTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests'
dotnet TestResults/balance/tower-loadout-retention-20260913/executable/BalanceHarness.dll tower-retention-check --run TestResults/balance/tower-loadout-retention-20260913
dotnet TestResults/balance/tower-loadout-retention-20260913/executable/BalanceHarness.dll tower-retention-run --run TestResults/balance/tower-loadout-retention-20260913
dotnet TestResults/balance/tower-loadout-retention-20260913/executable/BalanceHarness.dll tower-retention-verify --run TestResults/balance/tower-loadout-retention-20260913
git -c core.safecrlf=false diff --check
```

All required verification completed. The test build had five existing unrelated warnings. The auxiliary audit used a process-local APPDATA directory under TEMP and an empty package-source configuration; backend test building used the established no-restore/no-project-reference/static-assets workaround. No repository or production configuration changed.

## Decision and next boundary

Keep v15 experimental and leave the default unchanged. The 64/64 retention allocation expanded module reuse but failed reliability at 0/3 and performed worse than screened v13 in restart one. This completed comparison provides no support for adopting it.

The next optimizer step should preserve multiple promising whole-party lineages while retaining v13's ranked loadout library. The saved genealogy audit shows that the final four training leaders in each of twelve examined arms share a late common ancestor, despite turnover among many individual beam members. Use those saved trajectories to specify lineage separation, parent allocation and preserved refinement depth before freezing one equal-budget comparison. This is a hypothesis requiring measurement, not an established fix. Keep the player budget, Kharad and the 2/3 reliability requirement fixed; this completed study receives no extension or follow-on allocation. Carry forward all 62 confirmed recipes and every value in the 477,847-reservation ledger.

Kharad and the complete character/gear/Essence budget remain fixed. The prior 17,821-recipe precision Pass with 18 supported viable teams remains unchanged. No gameplay source, content, catalog, default policy, migration, service deployment or external environment changed. Practical acquisition and floors 6–11 remain open.
