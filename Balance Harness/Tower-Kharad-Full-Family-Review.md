# Kharad complete-family confirmation — 13 September 2026

**Complete execution; Inconclusive acceptance; application Hold.** All **17,821** registered recipes now have complete measurements at the linked +16% candidate. The study completed **489,168 fights**, with **zero retries**, in **10,739.82 seconds (178.997 minutes)**. One required recipe remains above the confidence-bound ceiling: **75/192 (39.06%)**, adjusted **26.40%–53.39%**. No observed rate exceeded 50%; **21** teams have supported lower bounds of at least 10%.

Read the [frozen plan](Tower-Kharad-Full-Family-Plan.md), [protocol](../TestResults/balance/tower-kharad-full-family-20260913/protocol.json), [assessment](../TestResults/balance/tower-kharad-full-family-20260913/campaign/assessment.json), [independent audit](../TestResults/balance/tower-kharad-full-family-work-20260913/analysis.json), [saved selected builds](../TestResults/balance/tower-kharad-full-family-work-20260913/exports/saved-builds.md), and [application decision](../TestResults/balance/tower-kharad-full-family-work-20260913/application-decision.json).

## What changed

The offline staged evaluator now accepts opt-in schema 2 / `tower-staged-bonferroni-wilson-95-v2` with up to **20,000 recipes**. Schema 1 retains its 10,000-recipe limit. Both use the same two fixed looks, alpha .025 per stage, complete-stage Bonferroni-adjusted Wilson intervals, strict observed-ceiling rule and **500,000-fight maximum**. The ordinary evaluator remains unchanged. Reports identify the selected policy; file batches never divide the statistical family.

The implementation changes are in [TowerStagedBalance.cs](../LL/tools/BalanceHarness/TowerStagedBalance.cs) and [its regression tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerStagedTests.cs). The existing staged runner and CLI perform execution and reconstruction. The isolated producing helper, inputs, source, binaries and tests are captured in the new package. No combat engine, independent generator, live Tower data, retained catalog or dependency was changed by this increment.

## Complete family and outcome

All 35 inventory source hashes and the compressed family hash were checked. All 17,821 typed normalized identities and fixed-budget parties were checked and materialized before combat. The exact seed-free family retains origins from the original 2,918-party family, intervening searches, preserved partial/recovered reports, references and the candidate study. Importing historical recipes does not re-certify their old outcomes; their new measurements provide this decision.

The 327 forced anchors are the union of 32 candidate finalists, 62 recorded discovery-breach recipes and 243 historical anchor nominations. The remaining 17,494 recipes received 24 fresh fights each. **17,460** won zero and resolved below the ceiling with adjusted upper **49.19684%**. The other **34** advanced, with a first-stage maximum of **5/24**. The full second-stage family was therefore **361**, within the frozen capacity of 416, and each received 192 separate fresh fights.

| Completed allocation | Fights |
| --- | ---: |
| First stage: 17,494 × 24 | 419,856 |
| Second stage: 361 × 192 | 69,312 |
| Total actual starts and completions | **489,168** |
| Frozen maximum | 499,728 |
| Retries, lost attempts, diagnostics, verification fights | **0** |

Every final row has complete evidence. **17,820** final upper bounds meet the ceiling; one does not. The stronger observed saved teams below demonstrate why the earlier 32-recipe Pass could not certify the full inventory. These are descriptive ranks on the common fresh schedule, without a new paired-superiority claim.

| Saved recipe | Wins / 192 | Adjusted interval |
| --- | ---: | --- |
| [team-db3598435c3754b58e2b6d9c10ff7be2](../TestResults/balance/tower-kharad-full-family-work-20260913/exports/recipes/team-db3598435c3754b58e2b6d9c10ff7be2.json) | 75 | 26.40%–53.39% |
| [team-83da44857d911e2966ae36379a458153](../TestResults/balance/tower-kharad-full-family-work-20260913/exports/recipes/team-83da44857d911e2966ae36379a458153.json) | 66 | 22.40%–48.73% |
| [team-8e226b03f5e188e6ef219064e77e279c](../TestResults/balance/tower-kharad-full-family-work-20260913/exports/recipes/team-8e226b03f5e188e6ef219064e77e279c.json) | 63 | 21.10%–47.15% |
| [team-a7e5de669c4a17287d84060e8ab6359b](../TestResults/balance/tower-kharad-full-family-work-20260913/exports/recipes/team-a7e5de669c4a17287d84060e8ab6359b.json) | 52 | 16.44%–41.22% |
| [team-66400a38e71a16e2856f1613c1810769](../TestResults/balance/tower-kharad-full-family-work-20260913/exports/recipes/team-66400a38e71a16e2856f1613c1810769.json) | 51 | 16.03%–40.67% |
| [team-5e2a01c69e56420eeea58a441d63736a](../TestResults/balance/tower-kharad-full-family-work-20260913/exports/recipes/team-5e2a01c69e56420eeea58a441d63736a.json) | 50 | 15.62%–40.12% |
| [team-b949c1679cb782dac3d6031d12f9e17b](../TestResults/balance/tower-kharad-full-family-work-20260913/exports/recipes/team-b949c1679cb782dac3d6031d12f9e17b.json) | 49 | 15.21%–39.56% |
| [team-06c18168810d815b19f2d7442e8de514](../TestResults/balance/tower-kharad-full-family-work-20260913/exports/recipes/team-06c18168810d815b19f2d7442e8de514.json) | 48 | 14.80%–39.01% |

The remaining uncertain team, `team-db3598435c3754b58e2b6d9c10ff7be2`, came from the earlier unchanged-v13 replication: `loadout-composition-joint`, generation seed `-667305989`, recorded discovery **7/8**. It was imported as an external anchor. Its recipe was already saved; this work measured it at the candidate rather than discovering it again. All 361 selected recipes now have individual seed-free exports and retained origins; the full 17,821-recipe inventory remains available as well. No catalog was promoted.

## Application and next bounded step

**Hold application.** The 53.39% upper bound prevents full-family acceptance despite the observed 39.06% rate. Live Kharad stays at **Health 3.04881408 / Power 3.85370128**; the isolated candidate remains **Health 3.5366243328 / Power 4.4702934848**. The candidate’s earlier independent-search reliability **Fail 0/3** remains separate from both this result and the prior live-setting **Pass 2/3**. Server-wide search quality, unsearched recipes, acquisition and floors 6–11 remain open.

A [separate precision design](Tower-Kharad-Precision-Resolution-Design.md) narrows the next measurement to **one team × 1,000 fresh fights**. Independent arithmetic assigns alpha **.025** to the existing first stage, **.0125** to the entire existing 361-team second stage and **.0125** to the complete fresh family. After tightening all retained second-stage bounds, only the same team remains uncertain; every other retained second-stage upper is at most **49.31504%**, and **17** retained teams still meet the viability threshold. This is a design, with no new seeds or combat allocation and no result claim. The complete Inconclusive study remains closed and unchanged. A prospective composite evaluator and frozen source/schedule/resource contract are required before that separate study.

## Verification and preservation

The captured executable reconstructed every batch, recipe, schedule, selection, interval, report and durable attempt count under a guard that forbids new combat. The independent audit matched every interval and decision, checked all family IDs, seed exclusions, exact schedules, manifests and journals, and confirmed **475,054 distinct reserved seed values**. The ledger retains all 474,838 preceding values, including unused reservations, and adds 24 + 192 fresh values. Samples from the two stages and closed studies were never pooled. Wilson coverage remains approximate; this is not a lifetime repeated-study guarantee.

The package contains **9,517,431,552 bytes (8.864 GiB)**, within its 12-GiB cap, and execution stayed inside four hours. Verification and test runs are separate from the experiment’s fight accounting. The historical candidate descriptor was copied unchanged; its original relative content root belongs to the source calibration package. This study explicitly uses its own `content/` directory and verifies the same 16 content hashes.

Commands completed successfully from the repository root:

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore --verbosity quiet
./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarnessTowerStagedTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests|FullyQualifiedName~BalanceHarnessTowerBulkTests|FullyQualifiedName~BalanceHarnessTowerCompactTests|FullyQualifiedName~BalanceHarnessTowerCompactPublicationTests'
dotnet TestResults/balance/tower-kharad-full-family-20260913/executable/Study.dll verify
```

All **130 relevant tests passed**. New checks cover the 20,000 limit and unchanged combat ceiling, strict schema/policy pairing, global confidence calculations over 17,821 cells, retained overflow and both policy versions’ cancellation/recovery/resume/tamper behavior. The initial test run exposed an older fixture assumption about discarding a pending chunk; it was corrected to require explicit zero-combat publication recovery and preserve all four completed fights. The producing helper initially omitted runtime manifests; that packaging issue was corrected before freezing the protocol. Both setup corrections and their original logs are retained, with **zero campaign fights** before the final freeze. Existing unrelated build warnings remain; no required verification command is blocked.

Protocol SHA-256: `dc1bdaaf5bbde2edf98ca95d302067c109143b50f005c3280a8d2164dc71e6b5`. Final study manifest SHA-256: `4656ba889374a71889dd32476843e7121ce47b77c6cc3548767e9ca92ff9fd25`. The [final work receipt](../TestResults/balance/tower-kharad-full-family-work-20260913/final-verification.json) records the documentation snapshot, source changes, prior-file preservation and new work manifest. Historical reviews and packages remain unchanged.

Changed scope: the staged evaluator and its tests, six active Markdown guides, this review and the new plan/design documents; isolated ignored artifact packages retain the measurements, binaries, recipes and audits. There are no migrations, deployments, shared-database changes, live configuration changes or service restarts.
