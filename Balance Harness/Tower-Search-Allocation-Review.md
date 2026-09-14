# V13 search-allocation implementation and stopped-run review

14 September 2026 (Europe/Copenhagen). The campaign directory retains its 13 September UTC start date. **Implementation complete; experiment stopped at its time cap. Reliability and full-family acceptance: Unresolved. Adoption: Hold.**

Keep v17 experimental and leave defaults unchanged. The bounded comparison stopped at its 5,400-second time cap during confirmation. Reliability and complete-family acceptance are Unresolved; this is an operationally incomplete study, not a method Fail 0/3 or a Pass. The stopped campaign receives no resume, retries, extension or replacement outcomes.

## What was implemented

The [frozen plan](Tower-Search-Allocation-Plan.md) compares one 768-candidate v13 search with two isolated 384-candidate searches at equal generation cost. Each component owns its parents, exploration, named random stream and ranked 128-module library. Existing v13 operators and outcome ranking remain unchanged. Deep uses 192 initial fresh candidates; the pair uses 96 + 96. Proposal limits are 8,192 versus 4,096 + 4,096. The larger initial allocation means deep is not an exact continuation of an earlier 384-candidate trajectory.

All nine components must finish before six groups freeze their complete ranking, original top two and top 32. Exact duplicate recipes receive one selection position while both evaluations remain charged and every component origin remains retained; conflicting duplicate evidence rejects grouping. Each group then receives 32 × 64 fresh screening fights. All 74 preceding controls, all original/screened nominees and all observed above-ceiling discoveries/screens enter the complete confirmation family. Capacity is 112, with declared breach headroom and no truncation.

The unchanged candidate reliability rule requires a ≥10% adjusted rate lower bound, a paired improvement lower bound against deep >0, and an anchor difference lower bound ≥−10 percentage points in at least two of three allocation units. The strong-control comparison remains separate. No confirmation confidence intervals or gate results were fabricated for the unfinished family.

## What actually completed

| Phase | Planned fights for the selected family | Started | Completion markers | Reconstructable reports | Status |
| --- | ---: | ---: | ---: | ---: | --- |
| Discovery | 36,864 | 36,864 | 36,864 | 36,864 | Complete: 4,608 evaluations |
| Screening | 12,288 | 12,288 | 12,288 | 12,288 | Complete: six groups |
| Confirmation | 48,128 | 11,764 | 11,763 | 11,744 | Stopped during the first batch |
| Total | 97,280 | 60,916 | 60,915 | 60,896 | Incomplete |

The [durable stop record](../TestResults/balance/tower-search-allocation-20260913/failure.json) records cancellation at **5,400.007 seconds**, with exit code **130** and no implicit retry. One started fight was interrupted. Nineteen completed confirmation outcomes were not committed before cancellation; their completion markers remain in accounting, but their reports cannot be reconstructed. The 367 complete confirmation chunks retain 11,744 reports. No outcomes were regenerated and no chunks were published after the stop.

The archive contains **2,017,963,655 bytes (1.88 GiB)** in **53,380 files**, below the 4 GiB cap. The time cap, rather than the 106,496-fight or 112-recipe caps, stopped execution. The family was Ready with 94 recipes. Its saved prefix has 22 complete 512-trial cells, one 480-trial cell and 71 unstarted cells. The absence of a completed family prevents ordinary/joint acceptance and method reliability assessment. Earlier control measurements were not substituted.

## Screening evidence and saved recipes

All 4,608 distinct candidate recipes and all 12,288 screening fights are retained. Deep primaries scored 1/64, 5/64 and 3/64 in screening; isolated-pair primaries scored 0/64, 27/64 and 0/64. The second isolated primary is promising screening evidence, but none of the six primaries reached confirmation. The frozen family has 94 recipes: 74 preceding controls and 20 new recipes. Only 22 cells have all 512 confirmation reports saved, one has 480 and 71 were unstarted; no full-family or reliability conclusion follows.

| Root generation seed | Deep primary screen wins / 64 | Isolated-pair primary screen wins / 64 | Confirmation of both primaries |
| --- | ---: | ---: | --- |
| 1614029318 | 1 | 0 | Not started |
| 1816290420 | 5 | 27 | Not started |
| 233022769 | 3 | 0 | Not started |

The second isolated primary, [team-b4c41e1136b71c41f1f35fbe25b7a8d1](../TestResults/balance/tower-search-allocation-work-20260913/exports/team-b4c41e1136b71c41f1f35fbe25b7a8d1.json), won **27/64 screening fights (42.19%)**, versus **5/64** for its paired deep primary. It rose from discovery rank 11, where it had won 1/8. This is promising selection evidence, not confirmed viability, a causal advantage or a reliability pass. All six primaries remained unstarted in confirmation.

The [94-build export](../TestResults/balance/tower-search-allocation-work-20260913/exports/saved-builds.md) contains complete seed-free recipes and every source: 74 prior controls and 20 new recipes. Each exported measurement distinguishes CompleteCell, PartialCell and NotStarted within an incomplete family. All **4,608 distinct discovered recipes** remain in [all-evaluated-recipes.json](../TestResults/balance/tower-search-allocation-20260913/all-evaluated-recipes.json), and the [complete grouped rankings](../TestResults/balance/tower-search-allocation-20260913/shortlist.json) remain frozen.

## Mechanism and execution-cost findings

All nine component searches, six grouped rankings, 4,608 recipe identities and component origins, same-component parent links, ranked libraries and donor uses passed reconstruction. The executed groups contained no duplicate complete recipes; duplicate handling was exercised by regression tests. Discovery took 4,754.82 seconds, versus the 2,657.49-second linear estimate from twice the preceding discovery duration. Doubling evaluated candidates therefore used 3.58 times the preceding discovery time. Complete-tree storage scans at batch/chunk boundaries are a concrete scaling hypothesis in the code; the timing comparison does not isolate their cost from other runtime work.

| Component / root seed | Evaluations | Initial fresh | Positive discovery candidates | First positive position | Maximum contributing ancestry depth |
| --- | ---: | ---: | ---: | ---: | ---: |
| loadout-composition-joint / 1614029318 | 768 | 192 | 22 | 531 | 32 |
| isolated-a-loadout-composition-joint / 1614029318 | 384 | 96 | 0 | — | 20 |
| isolated-b-loadout-composition-joint / 1614029318 | 384 | 96 | 0 | — | 18 |
| loadout-composition-joint / 1816290420 | 768 | 192 | 15 | 530 | 28 |
| isolated-a-loadout-composition-joint / 1816290420 | 384 | 96 | 0 | — | 12 |
| isolated-b-loadout-composition-joint / 1816290420 | 384 | 96 | 17 | 271 | 22 |
| loadout-composition-joint / 233022769 | 768 | 192 | 49 | 238 | 16 |
| isolated-a-loadout-composition-joint / 233022769 | 384 | 96 | 0 | — | 20 |
| isolated-b-loadout-composition-joint / 233022769 | 384 | 96 | 0 | — | 19 |

Ancestry depth is the longest contributing-parent path, not the earlier v16 closest-parent founder label. All six groups retained 768 charged evaluations, 768 distinct recipes and 768 component origins. The actual run had no duplicate complete recipes; regression tests cover duplicate grouping and conflicting evidence. The [independent mechanism audit](../TestResults/balance/tower-search-allocation-work-20260913/mechanism.json) records all ranked libraries, donor use and component distributions.

Discovery alone used **4,754.82 seconds (79.25 minutes)**. The linear preparation estimate was **2,657.49 seconds (44.29 minutes)**, an underestimate of **2,097.34 seconds (34.96 minutes)**. Screening used **328.55 seconds**; confirmation ran for **307.48 seconds** before cancellation. The planning envelope therefore did not reserve enough actual execution time to reach a complete confirmation family.

The [compact campaign runner](../LL/tools/BalanceHarness/TowerBulkCampaign.cs) scans the growing directory tree at batch/chunk storage boundaries, while the enclosing comparison also checks its whole output periodically. Repeated full-tree scans create a concrete scaling risk as candidate archives accumulate. This source inspection and the timing ratio motivate profiling; they do not establish that all excess time was storage work. Any optimization must preserve complete storage enforcement, identity checks, interruption accounting and exact evidence reconstruction.

## Verification and preserved evidence

**495 relevant backend tests passed** through `build/run-tests.ps1`: 434 generation/benchmark/compact/precision/staged cases and 61 accounting/evaluator cases. The 30 added cases cover deep-v13 parity at the declared budget, separate component streams and ancestry, independence under changed outcomes elsewhere, local ranked libraries, duplicate provenance and conflicting measurements, full grouped-ranking tampering, equal budgets and scoped caps, compact captured reconstruction, and the shared selection/confirmation contract.

All four preceding campaigns reconstructed **9,024 evaluations and 48 feedback probes** exactly without combat. The stopped-run audit then reconstructed every new discovery report, all six complete screening archives, every grouped rank, selection and the full 94-recipe family. It verified the saved confirmation prefix with the frozen compact verifier and a guard that rejects combat. It invoked no resume or publication operation and checked the campaign inventory before and after. The independent Python audit verified complete normalized recipes, all source origins, module hashes and donors.

Seventeen earlier packages / **232,600 files**, plus the four-file Markdown follow-up, passed full preservation checks both before execution and after the stopped-run audit. Their files and sealed manifests remain unchanged. The [closure receipt](../TestResults/balance/tower-search-allocation-work-20260913/final-verification.json) and [stopped inventory](../TestResults/balance/tower-search-allocation-work-20260913/stopped-files.json) preserve the producing source, tests, stopped archive, exports and documentation snapshots. This receipt certifies the closure audit, not a successfully completed experiment.

The complete `tower-allocation-verify` command could not be used: its contract requires a finished campaign manifest and summary, which a cancelled comparison deliberately does not publish. The separate [stopped audit](../TestResults/balance/tower-search-allocation-work-20260913/stopped-audit/Program.cs) verifies the available scope explicitly. The complete-result analysis/sealing templates prepared before execution remain unused; they were not altered to manufacture a completion or acceptance result.

Commands and checks:

```powershell
dotnet build LL/tools/BalanceHarness/BalanceHarness.csproj --configuration Release --no-restore
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore -p:BuildProjectReferences=false -p:StaticWebAssetsEnabled=false
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBoss|FullyQualifiedName~BalanceHarnessTowerLoadout|FullyQualifiedName~BalanceHarnessTowerPartyLineage|FullyQualifiedName~BalanceHarnessTowerSearchAllocation|FullyQualifiedName~BalanceHarnessTowerFinalistRescreen|FullyQualifiedName~BalanceHarnessTowerFeedback|FullyQualifiedName~BalanceHarnessTowerGenerationFeedback|FullyQualifiedName~BalanceHarnessTowerCompact|FullyQualifiedName~BalanceHarnessTowerSearchBenchmark|FullyQualifiedName~BalanceHarnessTowerPrecision|FullyQualifiedName~BalanceHarnessTowerStaged'
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerBulkTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests'
dotnet TestResults/balance/tower-search-allocation-20260913/executable/BalanceHarness.dll tower-allocation-check --run TestResults/balance/tower-search-allocation-20260913
dotnet TestResults/balance/tower-search-allocation-20260913/executable/BalanceHarness.dll tower-allocation-run --run TestResults/balance/tower-search-allocation-20260913
dotnet TestResults/balance/tower-search-allocation-work-20260913/stopped-audit/bin/Release/net10.0/StoppedAudit.dll
git -c core.safecrlf=false diff --check
```

Exact test filters and logs are retained in the work package. The harness build and stopped audit build had zero warnings; the test build retained five unrelated existing warnings. Auxiliary builds used process-local APPDATA under TEMP and an empty package-source configuration. The established test-build workaround changed no repository configuration. A mistyped anchor in the initial preparation command was rejected before any campaign directory or persisted allocation; the corrected preparation passed. A Python reporting syntax error was corrected before that script executed. These were not combat retries.

## Changed files

| Files | Change |
| --- | --- |
| [TowerSearchAllocation.cs](../LL/tools/BalanceHarness/TowerSearchAllocation.cs) | Named component streams, complete grouping, duplicate recipe identity and origin retention. |
| [TowerBossGeneration.cs](../LL/tools/BalanceHarness/TowerBossGeneration.cs), [TowerBossDiscoveryContract.cs](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs) | Versioned component/attempt budgets, same-component provenance and scoped fight ceiling. |
| [TowerBossPartyGenerator.cs](../LL/tools/BalanceHarness/TowerBossPartyGenerator.cs), [TowerPartyCoverage.cs](../LL/tools/BalanceHarness/TowerPartyCoverage.cs), [TowerLoadoutComposition.cs](../LL/tools/BalanceHarness/TowerLoadoutComposition.cs) | Admit v17 to existing v13 generation and loadout mechanics. |
| [TowerGenerationComparisonDesign.cs](../LL/tools/BalanceHarness/TowerGenerationComparisonDesign.cs), [TowerFeedbackBenchmark.cs](../LL/tools/BalanceHarness/TowerFeedbackBenchmark.cs), [TowerFeedbackBenchmarkRun.cs](../LL/tools/BalanceHarness/TowerFeedbackBenchmarkRun.cs), [Program.cs](../LL/tools/BalanceHarness/Program.cs) | Separate components from comparison groups, grouped nomination, 74 controls/112 capacity, actual budgets and additive CLI. |
| [Allocation tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerSearchAllocationTests.cs), [comparison tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerFeedbackBenchmarkTests.cs) | New allocation regressions and shared contract cases. |
| Frozen allocation plan, this review and six active guides | Protocol, measured stop, saved builds, current exclusions and next boundary. |

## Next boundary

The next work package should preserve the completed generation and screening, then prepare a separately frozen confirmation-only follow-up for all 94 saved recipes on a new shared schedule. Keep the six selected primaries, their comparators, the full family and the unchanged 2/3 reliability rule fixed before reserving fresh confirmation seeds; 512 trials per recipe would require 48,128 fights. This requires its own explicit contract and measured time/storage budget, with the original allocation study permanently recorded as stopped; do not resume it, append replacement trials or silently reinterpret its limit. Do not regenerate the 4,608 candidates merely to replace the stopped run. First profile archive finalization and complete-tree storage checks from saved evidence without combat, and validate the cost envelope for the separate follow-up. Preserve integrity, interruption accounting and exact reconstruction in any performance change. Carry all 94 recipes, distinguish 74 previously confirmed controls from 20 newly selected recipes, and exclude all 479,021 reserved values, used or unused. Keep Kharad, the player budget and defaults fixed. Further optimizer variants, practical acquisition and floors 6–11 remain separate work. This recommendation allocates no new fights or seeds.

The current [seed ledger](../TestResults/balance/tower-search-allocation-20260913/seed-ledger.json) has **479,021 distinct reservations**, including 587 new values and every unused value. Nine derived component random streams were distinct; the three root generation seeds are the reserved generation values. No analysis allocated further seeds.

No gameplay source, content, catalog, default policy, migration, service deployment or external environment changed. Kharad and the player budget remain fixed. Practical acquisition and floors 6–11 remain open.
