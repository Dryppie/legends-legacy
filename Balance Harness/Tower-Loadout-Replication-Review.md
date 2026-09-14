# Fresh challenger confirmation and unchanged-v13 replication: completed

13 September 2026. **Unchanged-v13 replication passed the reliability gate: two of three independent restarts qualified.** The successful primaries won **225/256 (87.89%)** and **244/256 (95.31%)**, with joint intervals **79.79–93.03%** and **89.00–98.08%**. Both support improvement over their deeper-v4 comparators and the fixed anchor. The third primary won zero. This establishes the frozen replication result while exposing a substantial ceiling breach; balance acceptance remains **Fail**.

The separate fresh confirmation also supports a breach for the earlier saved challenger: **548/1,000 (54.80%)**, joint interval **50.13–59.39%**. Search reliability, tested-family balance acceptance, practical acquisition and near-optimality remain separate conclusions.

Both frozen studies completed: **32,840 fights**, zero combat retries or lost attempts, including eight predeclared diagnostic fights in replication. Publication required no manual recovery. Both captured verifiers and the independent arithmetic/ancestry audits passed. Kharad remains **Health 3.04881408 / Power 3.85370128**.

## Confirmation result

All eight external builds received the same 1,000 fresh seeds. The complete recipes are in the [readable confirmation export](../TestResults/balance/tower-loadout-replication-work-20260913/confirmation-exports/saved-builds.md), with [all trial outcomes](../TestResults/balance/tower-loadout-confirmation-20260913/run/evidence.json) and an [independent audit](../TestResults/balance/tower-loadout-replication-work-20260913/confirmation-exports/analysis.json).

| Saved build | Wins / 1,000 | Joint interval |
| --- | ---: | --- |
| `team-1a924a7cfff12298633bee909cdea4ad` | 483 | 43.67%–52.96% |
| `team-1abe76ca1891d97a91d484f0a3662048` | 300 | 25.91%–34.44% |
| `team-38248d838d1db9634fd82536c177df0a` | 351 | 30.79%–39.67% |
| `team-3a69c759178064021dc5cf7124d7f4f5` | 295 | 25.43%–33.92% |
| `team-693ffa8ec0b654154a06722aba06a968` | 236 | 19.87%–27.79% |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | 303 | 26.19%–34.75% |
| `team-ac85939be8c0d4020590070f30d580f5` | 273 | 23.35%–31.65% |
| `team-df06d15779b79eec34ac53c14fb0e138` | 548 | 50.13%–59.39% |

The new challenger `team-df06d15779b79eec34ac53c14fb0e138` is now both an observed breach and a confidence-supported ceiling breach under the frozen approximate Wilson procedure. The adjusted lower bound is only 0.13 percentage points above 50%; preserve the exact interval and its study scope. Ordinary and joint family assessments are **Fail / Fail**. Earlier observed breaches remain recorded rather than being pooled with or erased by this study.

Against the strongest older control, `team-1a924a7cfff12298633bee909cdea4ad`, the challenger gained 285 paired victories and lost 220: **+6.50 percentage points**, adjusted interval **−0.36 to +13.28 points**. Its observed rate is higher, but the predeclared paired test does **not** establish superiority. The other new saved recipe remains viable at **273/1,000 (27.30%)**.

Joint alpha .025 covers the eight rate intervals, and .025 covers the two discordance probabilities for the one fixed paired comparison. Wilson multipliers are 16 and 4. No adaptive extension, replacement comparison or pooling occurred.

## Independent replication result

| Method | Restart | Primary / 256 | Secondary / 256 | Primary joint interval | Combined gate |
| --- | ---: | ---: | ---: | --- | --- |
| coverage-deep-joint | 780215308 | 2 | 2 | 0.11%–5.30% | Comparator |
| loadout-composition-joint | 780215308 | 0 | 0 | 0.00%–3.91% | Fail |
| coverage-deep-joint | 933825761 | 0 | 0 | 0.00%–3.91% | Comparator |
| loadout-composition-joint | 933825761 | 225 | 224 | 79.79%–93.03% | Pass |
| coverage-deep-joint | -667305989 | 94 | 94 | 27.69%–46.78% | Comparator |
| loadout-composition-joint | -667305989 | 244 | 243 | 89.00%–98.08% | Pass |

| New primary | Viable ≥10% | Improvement over v4, pp | Difference from anchor, pp | Combined gate |
| --- | --- | --- | --- | --- |
| `team-ef66a5cfc6618c2c9c5f3657258376a8` | False | -0.78 [-4.96, +3.45] | -31.25 [-40.70, -19.57] | Fail |
| `team-7e3f1a686a7ed2a06ec4319146e16c2f` | True | +87.89 [+76.66, +92.85] | +56.64 [+41.37, +67.87] | Pass |
| `team-2b1c3eaba3a3e20c5777230b3bcba956` | True | +58.59 [+44.33, +68.67] | +64.06 [+49.68, +73.87] | Pass |

Require at least two of three fixed new primaries to satisfy every component: rate lower bound ≥10%, paired improvement lower bound versus same-restart deeper v4 >0, and paired difference lower bound versus fixed anchor ≥−10 points. Rank-two finalists remain exploratory and do not replace primaries. The earlier 1/3 study is kept separate; it is not pooled into this replication.

All **20 recipes × 256 validation outcomes** are complete, including zero-win finalists and all eight external controls. The ordinary replication assessment is **Fail**; its joint family assessment is **Fail**. Read [all selected recipes and measurements](../TestResults/balance/tower-loadout-replication-work-20260913/exports/saved-builds.md), [machine-readable builds](../TestResults/balance/tower-loadout-replication-20260913/saved-builds.json), [per-trial evidence](../TestResults/balance/tower-loadout-replication-20260913/validation/evidence.json), and [the independent audit](../TestResults/balance/tower-loadout-replication-work-20260913/exports/analysis.json).

| Method | Restart | Discovery candidates with wins | First winning candidate | Best discovery rate |
| --- | ---: | ---: | ---: | ---: |
| coverage-deep-joint | 780215308 | 6 | 329 | 12.50% |
| loadout-composition-joint | 780215308 | 0 | None | 0.00% |
| coverage-deep-joint | 933825761 | 0 | None | 0.00% |
| loadout-composition-joint | 933825761 | 48 | 232 | 100.00% |
| coverage-deep-joint | -667305989 | 44 | 234 | 37.50% |
| loadout-composition-joint | -667305989 | 72 | 221 | 100.00% |

These discovery findings describe the completed searches; they do not identify a causal operator or Essence effect. Both arms’ first 96 evaluated parties match in each paired restart. Every library hash and module source/placement trace was reconstructed from earlier completed parties in its own arm. All six generation, nomination, fitness and statistical source files match the original v13 producing snapshot byte for byte. The two saved reference additions and confirmation outcomes never entered independent construction, parent selection or ranking.

## Publication fix and allocation

The [frozen plan](Tower-Loadout-Replication-Plan.md) and both protocols were saved before either study’s first fight: [confirmation protocol](../TestResults/balance/tower-loadout-confirmation-20260913/protocol.json) and [replication protocol](../TestResults/balance/tower-loadout-replication-20260913/protocol.json). Replication retains 384 candidates per arm, eight discovery samples, three paired restarts, the original v13 operators and unchanged six paired comparisons. Its additional two controls increase the complete rate family and reserved held-out cost; no generation policy changed.

| Accounting | Confirmation | Replication |
| --- | ---: | ---: |
| Started / completed fights | 8,000 / 8,000 | 24,840 / 24,840 |
| Frozen fight cap | 8,000 | 24,840 |
| Execute seconds / cap | 174.24 / 1,200 | 1631.29 / 2,700 |
| Separate zero-combat verification | 8.85 s | 68.53 s |
| Retained package cap | 1 GiB | 2 GiB |

The work/export cap is 256 MiB. Exact final storage, source/document hashes and preservation checks are in the [final verification receipt](../TestResults/balance/tower-loadout-replication-work-20260913/final-verification.json). The authoritative [seed ledger](../TestResults/balance/tower-loadout-replication-20260913/seed-ledger.json) contains **474,187 distinct reservations**: 472,856 prior reservations, 1,000 confirmation seeds, then 331 generation/discovery/screen/validation seeds. Every historical array, including unused and constructor seeds, remains excluded.

Publication now retries only the directory rename, at most four times, with 50/100/200 ms waits and persisted receipt/data validation. Cancellation after a complete chunk’s last fight preserves that chunk before publication stops. Persistent failure keeps pending files. Explicit `tower-compact-recover-publication` validates the entire saved prefix, recipes, content, prepared reports, exact ordered seeds, hashes and durable attempts before publishing one complete next chunk. It executes no combat and leaves existing resume identity and budget checks intact. It rejects sealed archives, damaged or incomplete records, conflicting destinations and ambiguous pending state. A benchmark start marker still forbids an implicit rerun.

## Changed files and verification

| Files | Change |
| --- | --- |
| `LL/tools/BalanceHarness/TowerCompactPublication.cs` | Bounded filesystem publication and explicit verified zero-combat recovery. |
| `TowerCompactBundle.cs`, `TowerCompactResume.cs` in that directory | Reuse full prefix verification for pending chunks; persist a completed chunk across cancellation. |
| `TowerSearchBenchmark.cs`, `Program.cs` in that directory | Separate eight-control replication profile and recovery/preparation commands. |
| `LL/tests/EssenceSystem.Tests/BalanceHarnessTowerCompactPublicationTests.cs`, `BalanceHarnessTowerLoadoutCompositionTests.cs` | Failure injection, corruption/order/journal rejection, no-loss continuation and unchanged replication boundary/cost tests. |
| Tower plan/review, active handoff/status documents and harness README | Completed evidence, commands, current reservations and next decision. |
| New `TestResults/balance/tower-loadout-*20260913` confirmation/replication/work packages | Captured executables, protocols, independent audits, exact recipe exports and receipts. |

Release build succeeded. **155 backend tests passed** through `build/run-tests.ps1`; the first attempt caught an incorrectly scoped new reference fixture, corrected before the final complete passing run. Existing warnings in unrelated files remain. The package-free study/export helpers built successfully using temporary isolated NuGet configuration; no command remains blocked. `git diff --check` passed. No migration, application configuration change, shared database action, deployment, infrastructure edit, catalog/default promotion or later-floor change occurred. Earlier gameplay C# files, historical reviews and all three previous v13 evidence/work packages are hash-preserved.

Both captured verifiers reconstructed complete evidence without fights. The independent audit recomputed every rate interval, the confirmation’s one paired interval, all six replication paired intervals and gate decisions. It checked frozen nomination rank, all generated library hashes and exact same-arm ancestry/placements. Five malformed evidence variants were rejected in each audit. Typed .NET checks verified every exported recipe against its frozen scenario.

```powershell
dotnet TestResults/balance/tower-loadout-confirmation-20260913/executable/Study.dll verify-confirmation
dotnet TestResults/balance/tower-loadout-replication-20260913/executable/BalanceHarness.dll tower-search-benchmark-verify --run TestResults/balance/tower-loadout-replication-20260913
```

## Next decision

The next major step is a bounded Kharad calibration study against the expanded saved portfolio. The new 225/256 and 244/256 primaries and their 224/256 and 243/256 secondaries must be explicit ceiling controls, alongside the previously confirmed 548/1,000 challenger and the earlier controls. Keep the viable deeper-v4 finalists as well. First review the already saved detailed replays to decide which calibration parameters to test, then freeze a small setting grid, complete comparison families, fresh schedules and held-out selection rules before new combat. Evaluate every declared family at each declared setting; no single-build tuning or later-floor expansion follows from this result. The unchanged search met this replication’s gate, but practical acquisition and independent challenges at candidate settings remain required. No calibration setting is selected or applied here, and no further combat allocation is created. Practical acquisition and floors 6–11 remain downstream work.
