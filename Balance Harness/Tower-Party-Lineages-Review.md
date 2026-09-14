# Whole-party lineage implementation and comparison review

13 September 2026. **Complete and verified: reliability Fail 0/3; adoption Hold.** 68,608 fights completed with zero retries. The requirement remains at least two of three restarts.

Keep v16 experimental and leave the default unchanged. Protecting alternative founders was exercised throughout refinement, but the candidate policy failed reliability 0/3 and performed worse than v13 in restart two. This comparison provides no support for adopting it.

## What changed and why

The [saved-history audit](../TestResults/balance/tower-party-lineages-work-20260913/diagnosis.json) found a single closest-parent founder among the top four parties at 2,347 of 2,601 examined prefixes across nine prior v13 arms. Final alternatives lay at ranks 55–184. This was descriptive evidence for testing parent allocation, not proof that convergence caused failure.

The [frozen plan](Tower-Party-Lineages-Plan.md) changes only the main four-place parent beam: two globally ranked elites plus the best candidates from other founders, with ranked fallback. A completed fresh proposal establishes a founder. A child inherits from the contributing complete party with the most matching ordered Essence positions; ties use party then proposal ID. Full contributing ancestry remains saved. Labels describe structure, not statistical independence. Keeping two elite seats reserves half the main-beam choices for refinement but does not promise unchanged realized depth.

Both methods retain 384 candidates × 8 discovery fights, the same initial 96 candidates, v13 random-stream initialization, operators, exploration rule and ranked 128-module library. There is no feedback sampling, diverse module library or control-derived generation. Both arms receive 32 × 64 independent screening. All 62 prior controls and every original/screened nominee remain in the complete 512-trial family. The v16 capacity is 96, including breach headroom; overflow stops without truncation. Historical comparison contracts are unchanged.

## Measured result

| Restart / generation seed | Screened v13 wins/512 | Lineage wins/512 | Lineage adjusted rate interval | Paired improvement interval (percentage points) | Reliability |
| --- | ---: | ---: | --- | --- | --- |
| 1 / -352672989 | 0 | 0 | 0.00%–2.45% | -1.96 to 1.96 | Fail |
| 2 / 1506542031 | 42 | 0 | 0.00%–2.45% | -12.95 to -3.14 | Fail |
| 3 / 975036652 | 0 | 0 | 0.00%–2.45% | -1.96 to 1.96 | Fail |

All six lineage nominees won 0/512, with adjusted upper bounds of 2.45%. V13's second primary won 42/512 (8.20%), with an adjusted lower bound of 4.81%; its other primaries scored zero. No new nominee has supported viability at 10%. The seven supported viable recipes are prior controls. The lineage-minus-v13 paired difference in restart two is −8.20 percentage points, with adjusted interval −12.95 to −3.14.

The historical anchor won **0/512** and the fixed strongest preceding control won **145/512**. The **74-recipe** family returned ordinary/joint **Pass/Pass**, with **7** supported viable recipes and **0** observed confirmation ceiling breaches. Family acceptance and optimizer reliability have different scopes.

The unchanged reliability gate requires a rate lower bound ≥10%, paired improvement lower bound over screened v13 >0, and paired difference lower bound against the historical anchor ≥−10 percentage points. At least two of three restarts must pass all components. Strong-control comparisons remain separate. Joint alpha is .025 across the complete rate family and .025 across nine differences (eighteen discordance intervals). Independent Python calculations reproduced all intervals within 1e-8. These approximate intervals do not establish lifetime repeated-study coverage, global optimality, acquisition coverage or acceptance of the full discovered family.

## Mechanism check

The [independent mechanism audit](../TestResults/balance/tower-party-lineages-work-20260913/mechanism.json) recomputes every new-arm label, every recorded ranked-library hash and all composition donor sources. It compares beams at the same saved prefixes without replaying descendants or changing outcomes. The operating beam preserves its two elites and alternative founders. A diverse parent beam does not require diverse top-ranked winners or module-library founders.

| Method / seed | Single-founder ranked beam prefixes / 289 | Operating beam founder counts at final prefix | Final operating source ranks | Final operating depths | Positive discovery candidates |
| --- | ---: | ---: | --- | --- | ---: |
| loadout-composition-joint / -352672989 | 254 | 1 | [1, 2, 3, 4] | [11, 10, 11, 10] | 0 |
| lineage-loadout-composition-joint / -352672989 | 229 | 3 | [1, 2, 57, 76] | [1, 2, 8, 4] | 0 |
| loadout-composition-joint / 1506542031 | 264 | 1 | [1, 2, 3, 4] | [18, 17, 18, 17] | 8 |
| lineage-loadout-composition-joint / 1506542031 | 247 | 3 | [1, 2, 27, 37] | [11, 10, 11, 16] | 0 |
| loadout-composition-joint / 975036652 | 263 | 1 | [1, 2, 3, 4] | [14, 15, 12, 11] | 0 |
| lineage-loadout-composition-joint / 975036652 | 256 | 3 | [1, 2, 58, 63] | [14, 14, 8, 10] | 0 |

All 1,152 new-arm labels, 682 library hashes and 1,284 composition donor uses matched independent reconstruction. At every one of the 867 examined new-arm prefixes, the operating beam represented three or four founders. Each final new-arm library represented three founders, with the largest supplying 96, 97 and 89 of 128 modules. The three v13 libraries instead represented one, one and two founders. Maximum inherited depths were 10/16/15 for v16 versus 14/19/17 for v13. V13's first discovery win appeared at candidate 307, at inherited depth 15; all eight positive candidates appeared between 307 and 380. These are descriptive traces, not proof that greater depth will cause success or that a particular parent was suppressed by the other policy.

## Retained work and verification

All **2,304 evaluations**, **1,965 distinct complete recipes**, **192 screening slots** and **74 confirmation recipes** are retained, including zero-win recipes. See [results](../TestResults/balance/tower-party-lineages-work-20260913/results.json), [complete seed-free builds](../TestResults/balance/tower-party-lineages-work-20260913/exports/saved-builds.md), and [all discovery recipes](../TestResults/balance/tower-party-lineages-20260913/all-evaluated-recipes.json). Full generation retains founder labels, all contributing parents, depth, matching positions and module traces.

Execution used **2387.27 seconds** and **1,586,839,872 campaign bytes** (1.48 GiB). Durable accounting matches all 68,608 starts/completions. Limits were 79,872 fights, 5,400 seconds and 4 GiB. No retries, resumes, extensions or diagnostic combat replays. The [latest ledger](../TestResults/balance/tower-party-lineages-20260913/seed-ledger.json) retains all 477,847 prior values plus 587 new reservations: **478,434** distinct values. Exclude every array, including unused values.

**465 relevant tests passed**: 404 generation/benchmark/compact/precision/staged cases and 61 accounting/evaluator cases. The 38 added cases cover founder inheritance, tie/order/placement behavior, same-arm ancestry, malformed sources, two elites and alternative seats, deterministic descendants, unchanged v13 and ranked libraries, compact reconstruction, equal budgets, full controls, complete screening, tampering, overflow, interruption and the exact 2/3 gate.

Three previous campaigns reconstructed **6,720 evaluations and 48 feedback probes** exactly, including original shortlists and gameplay assemblies, under a no-combat guard. The completed new campaign passed captured verification of generation, screening, confirmation, provenance, inventory hashes and accounting. Fifteen earlier sealed packages / **202,575 files**, plus the four-file Markdown follow-up, passed full hash checks. The [completion receipt](../TestResults/balance/tower-party-lineages-work-20260913/final-verification.json) seals producing source, tests, exports, analysis and documentation snapshots.

| Files | Change |
| --- | --- |
| [TowerPartyLineages.cs](../LL/tools/BalanceHarness/TowerPartyLineages.cs) | Deterministic founder labels and two-elite/two-alternative parent selection. |
| [TowerBossGeneration.cs](../LL/tools/BalanceHarness/TowerBossGeneration.cs), [TowerBossDiscoveryContract.cs](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs) | Opt-in dispatch, additive saved labels, independent same-arm provenance. |
| [TowerBossPartyGenerator.cs](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs), [TowerPartyCoverage.cs](../LL/tools/BalanceHarness/TowerPartyCoverage.cs), [TowerLoadoutComposition.cs](../LL/tools/BalanceHarness/TowerLoadoutComposition.cs) | Admit the explicit v16 policy to existing mechanics and loadout operators. |
| [TowerGenerationComparisonDesign.cs](../LL/tools/BalanceHarness/TowerGenerationComparisonDesign.cs), [TowerFeedbackBenchmark.cs](../LL/tools/BalanceHarness/TowerFeedbackBenchmark.cs), [Program.cs](../LL/tools/BalanceHarness/Program.cs) | 62-control/96-capacity contract, label validation and additive lineage commands. |
| [Lineage tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerPartyLineageTests.cs), [comparison tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerFeedbackBenchmarkTests.cs) | Kernel regressions and shared comparison-contract coverage. |
| Frozen plan, this review and six active guides | Measured outcome, latest exclusions, complete recipes and next boundary. |

Commands executed:

```powershell
dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-restore
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore -p:BuildProjectReferences=false -p:StaticWebAssetsEnabled=false
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBoss|FullyQualifiedName~BalanceHarnessTowerLoadout|FullyQualifiedName~BalanceHarnessTowerPartyLineage|FullyQualifiedName~BalanceHarnessTowerFinalistRescreen|FullyQualifiedName~BalanceHarnessTowerFeedback|FullyQualifiedName~BalanceHarnessTowerGenerationFeedback|FullyQualifiedName~BalanceHarnessTowerCompact|FullyQualifiedName~BalanceHarnessTowerSearchBenchmark|FullyQualifiedName~BalanceHarnessTowerPrecision|FullyQualifiedName~BalanceHarnessTowerStaged'
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBulkTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests'
dotnet TestResults/balance/tower-party-lineages-20260913/executable/BalanceHarness.dll tower-lineage-check --run TestResults/balance/tower-party-lineages-20260913
dotnet TestResults/balance/tower-party-lineages-20260913/executable/BalanceHarness.dll tower-lineage-run --run TestResults/balance/tower-party-lineages-20260913
dotnet TestResults/balance/tower-party-lineages-20260913/executable/BalanceHarness.dll tower-lineage-verify --run TestResults/balance/tower-party-lineages-20260913
git -c core.safecrlf=false diff --check
```

All required checks completed. The harness build had no warnings; the test build retained five unrelated existing warnings. The auxiliary audit used process-local APPDATA under TEMP and an empty package-source configuration. Test building used the established no-restore/no-project-reference/static-assets workaround. No repository or production configuration changed.

## Decision and next boundary

Keep v16 experimental and leave the default unchanged. Protecting alternative founders was exercised throughout refinement, but the candidate policy failed reliability 0/3 and performed worse than v13 in restart two. This comparison provides no support for adopting it.

The next substantial optimizer step should test v13 search-budget allocation: compare one 768-candidate search with two isolated 384-candidate searches at the same total generation cost, keeping separate parent pools and module libraries for the isolated searches. Treat the two isolated searches as one allocation unit, freeze its aggregate original top two and top 32 before screening, and preserve all component provenance. Run three paired allocation units under the unchanged 2/3 reliability gate. Candidate 307's first win and the earlier late successes argue against splitting the budget into short 96-candidate trajectories; they do not establish which allocation will work. First implement and validate that aggregation contract and freeze measured time/storage limits. Carry all 74 confirmed recipes as external controls: the current 62-control/96-capacity contract cannot cover 74 controls plus up to 24 original/screened nominees, so the new capacity must cover at least 98 recipes plus declared breach headroom. Keep Kharad, the player budget and independent confirmation fixed, and exclude all 478,434 reserved values. This is a proposed follow-up, not a new allocation; the completed lineage study receives no extension. Practical acquisition and floors 6–11 remain separate open work.

Kharad and the player budget remain fixed. The prior complete-family precision Pass is unchanged. No gameplay source, content, catalog, default policy, migration, service deployment or external environment changed. Practical acquisition and floors 6–11 remain open.
