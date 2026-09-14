# Coordinated complete-loadout search: completed comparison

13 September 2026. The authorized implementation and comparison are complete. **The new independent search found a 135/256 (52.73%) challenger, but reliability remains Fail at 1/3 restarts, with two required.** The ordinary and joint-adjusted balance assessments are **Fail / Fail** because that observed rate exceeds the 50% ceiling. No settings, defaults or catalogs are promoted.

Read [all 18 saved builds and their ordered Essences](../TestResults/balance/tower-loadout-composition-work-20260913/exports/saved-builds.md), the [complete recipe/measurement export](../TestResults/balance/tower-loadout-composition-recovered-20260913/saved-builds.json), [trial outcomes](../TestResults/balance/tower-loadout-composition-recovered-20260913/validation/evidence.json), and the [independent statistical and ancestry audit](../TestResults/balance/tower-loadout-composition-work-20260913/exports/analysis.json). Every zero-win finalist and all six external controls are retained.

## Result and decision

| Method | Restart | Primary wins / 256 | Secondary wins / 256 | Primary joint interval | Combined gate |
| --- | ---: | ---: | ---: | --- | --- |
| Deeper v4 | -242719991 | 3 | 2 | 0.23–5.87% | Comparator |
| Coordinated loadouts | -242719991 | **135** | **75** | **42.85–62.41%** | **Pass** |
| Deeper v4 | 1354574116 | 0 | 0 | 0–3.84% | Comparator |
| Coordinated loadouts | 1354574116 | 0 | 1 | 0–3.84% | Fail |
| Deeper v4 | -631396627 | 0 | 0 | 0–3.84% | Comparator |
| Coordinated loadouts | -631396627 | 0 | 0 | 0–3.84% | Fail |

The successful primary is [`team-df06d15779b79eec34ac53c14fb0e138`](../TestResults/balance/tower-loadout-composition-work-20260913/exports/recipes/team-df06d15779b79eec34ac53c14fb0e138.json). Its discovery score was 5/8, screening 36/64 and validation 135/256. Its paired validation gain over its same-restart v4 primary is **+51.56 percentage points**, adjusted interval **+38.50 to +60.95**. Its gain over the fixed anchor is **+28.13 points**, adjusted interval **+11.45 to +42.80**. It supports viability, improvement and anchor competitiveness under the frozen rule.

The same restart's exploratory secondary, [`team-ac85939be8c0d4020590070f30d580f5`](../TestResults/balance/tower-loadout-composition-work-20260913/exports/recipes/team-ac85939be8c0d4020590070f30d580f5.json), scored **75/256 (29.30%)**, joint interval **21.14–39.04%**. It is a separately saved viable build, not another successful restart. Primaries were never replaced using held-out results.

| External control | Validation wins / 256 | Observed rate |
| --- | ---: | ---: |
| `team-1a924a7cfff12298633bee909cdea4ad` | 125 | 48.83% |
| `team-1abe76ca1891d97a91d484f0a3662048` — fixed anchor | 63 | 24.61% |
| `team-38248d838d1db9634fd82536c177df0a` | 79 | 30.86% |
| `team-3a69c759178064021dc5cf7124d7f4f5` | 74 | 28.91% |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | 84 | 32.81% |
| `team-693ffa8ec0b654154a06722aba06a968` | 56 | 21.88% |

The new primary has the highest observed rate in this family. Superiority over the strongest saved control was not a predeclared paired comparison and is not established by that ordering. Its interval crosses 50%, so this is an **observed ceiling breach**, not a confidence-supported assertion that its true rate exceeds 50%. The acceptance policy still requires Fail; prior ceiling breaches remain unresolved. Search reliability, balance acceptance and technical verification remain separate conclusions.

Keep v13 experimental. This study supports the ability to discover a much stronger independent party in one restart, while rejecting the required reliability claim. It does not establish near-optimality, practical acquisition, later-floor readiness or a setting suitable for application.

## What changed and what was isolated

The [frozen design](Tower-Loadout-Composition-Plan.md) adds opt-in `independent-loadout-composition-v13`. It retains up to 128 distinct complete ordered character loadouts from fully measured generated parties in the current arm, selected by existing whole-party rank and source slot. Loadouts receive no standalone fitness. Coordinated distribution, composition, shared refinement and complete-loadout placement alternate with v4 refinement operators.

Initial construction, one-in-four fresh proposals, parent selection, whole-party fitness and eight-seed discovery sampling remain unchanged. Both arms begin with the same v4 random stream. The actual campaign's first 96 evaluated parties match in each paired restart. Saved controls, their recipes/counts/ancestry and held-out outcomes never enter independent generation. Illegal and duplicate proposals consume proposal attempts but no fights.

Each arm evaluated 384 parties. The new arms exercised **105, 108 and 106** accepted coordinated proposals, with **58, 60 and 63** accepted changes affecting multiple characters outside complete recomposition. The library and its exact source/placement traces were independently reconstructed. The first new-arm restart found 19 candidates with discovery wins, starting at candidate 257; the second found one, at candidate 376; the third found none. Deeper v4 found three winning candidates in its first restart and none in the others. These are descriptive search findings, not causal attribution to one operator or Essence.

## Frozen allocation and publication recovery

The [original protocol](../TestResults/balance/tower-loadout-composition-20260913/protocol.json), SHA-256 `792256d828ff7fb6201d88c533c3bcb19d08381cd5be5e25ef4a066bb77d4f53`, froze three paired restarts, 384 candidates per arm, eight discovery seeds, two discovery-ranked finalists per arm, six external controls, 64 descriptive screening seeds and 256 separate validation seeds. Validation was unconditional. Joint alpha .025 covers the 18-cell rate family; .025 covers six paired comparisons through 12 discordance intervals.

| Accounting | Result |
| --- | ---: |
| Discovery | 18,432 fights; 2,304 evaluated parties |
| Screening | 1,152 fights; all 18 cells |
| Validation | 4,608 fights; all 18 × 256 outcomes |
| Fixed historical parity / finalist replay checks | 4 / 4 fights |
| **Total attempts / completed fights** | **24,200 / 24,200** |
| Repeated combat attempts / fresh-seed retries | **0 / 0** |
| Charged execution, including copying/recovery/reconstruction | **1,827.77 seconds / 2,700 cap** |
| Separate complete verification | **65.49 seconds; zero fights** |

Windows denied the directory rename that publishes a completed chunk after 16,876 fights. All eight pending outcomes and their hashes, exact seeds and durable attempt records were intact. The [interrupted package](../TestResults/balance/tower-loadout-composition-20260913/failure.json) remains unchanged, covered by a [23,312-file manifest](../TestResults/balance/tower-loadout-composition-work-20260913/interrupted-files.json).

An explicit [recovery amendment](../TestResults/balance/tower-loadout-composition-recovered-20260913/recovery-amendment.json) retained a separate copy. Its helper published the intact chunk with zero combat, reconstructed the saved prefix using the original executable and completed exactly **7,324 previously unstarted fights**. It preserved the protocol, recipes, ordered schedules, decision rules and attempt journal. It charged 1,186 seconds for the original interval, 219 seconds for recovery preparation, and the remaining recovery/execution within the original allowance. Neither combat nor time caps increased. Both physical packages together remain below the original 2 GiB storage cap; exact final sizes and preservation checks are recorded in the work receipt.

The [ledger](../TestResults/balance/tower-loadout-composition-recovered-20260913/seed-ledger.json) contains **472,856 distinct reservations**: all 472,525 prior reservations plus 331 fresh generation/discovery/screen/validation seeds. Recovery introduced none. Exclude every array, including unused and constructor-only history, in any later study.

## Files and verification

| Files | Purpose |
| --- | --- |
| `LL/tools/BalanceHarness/TowerLoadoutComposition.cs` | Ordered-loadout library, coordinated proposals and source/placement traces. |
| `TowerBossGeneration.cs`, `TowerBossPartyGenerator.cs`, `TowerPartyCoverage.cs`, `TowerBossDiscoveryContract.cs` in that directory | Opt-in policy integration, unchanged v4 comparator stream, legality and same-arm independent provenance. |
| `TowerSearchBenchmark.cs`, `TowerSearchBenchmarkResults.cs`, `Program.cs` in that directory | Separate equal-cost profile, unconditional validation, six paired comparisons and CLI preparation. The existing v12 profile remains supported. |
| `LL/tests/EssenceSystem.Tests/BalanceHarnessTowerLoadoutCompositionTests.cs` | Comparator/initial-population parity, coordinated reachability, inventory limits, ancestry isolation, real compact execution/reconstruction and statistical decisions. |
| Tower plan, handoff, discovery plan/implementation status, strategy status, acceptance-policy banner and harness README | Design, current evidence, commands, seed exclusions and next boundary. |
| `TestResults/balance/tower-loadout-composition-work-20260913/` | Test logs, preservation receipts, captured recovery helper, independent analysis and readable recipe exports. |

Release build and **107 relevant backend tests passed**, run through `build/run-tests.ps1`. The first automatic restore could not read the sandbox-restricted user NuGet configuration; the main build used existing restored dependencies and the prescribed script with `-NoBuild`. The package-free recovery helper used an isolated temporary application-data directory and explicit empty package sources, then built with zero warnings/errors. No command remains blocked. The main test build retains five warnings in unchanged existing files.

The captured benchmark verifier reconstructed all discovery, nominations, screening, validation, six paired comparisons, replay parity and global attempt accounting. The independent Python audit separately checked all 18 rate intervals, all six paired intervals and gate decisions, matching initial populations, exact finalist recipes, every library hash and generated source/placement trace. Five malformed evidence variants were rejected: missing cell, missing trial, reordered seeds, duplicated seed and unknown outcome.

From the repository root, verify the complete recovered package without new fights:

```powershell
dotnet TestResults/balance/tower-loadout-composition-recovered-20260913/executable/BalanceHarness.dll tower-search-benchmark-verify --run TestResults/balance/tower-loadout-composition-recovered-20260913
```

All 1,832 gameplay C# files and 82 prior historical reviews remain unchanged. Kharad remains **Health 3.04881408 / Power 3.85370128**, with the original fixed gear, character budget and untrained/unevolved Essences. There are no migrations, runtime configuration changes, database changes, deployments, infrastructure edits, catalog promotions or new floor coverage. The directory-publication interruption remains a tooling limitation; this study's recovery is explicit, not a new automatic retry mode.

## Next boundary

Preserve the new challenger and viable secondary as external reference candidates. A separately frozen fresh confirmation should assess the new ceiling challenger alongside the existing strongest control before any calibration. Independently, an unchanged-v13 replication should determine whether the successful search path repeats; the current 1/3 failure cannot be replaced by its two successful recipes from one restart. Keep every saved recipe outside independent construction and ranking.

No further combat allocation is created by this review. Do not alter the policy using the successful recipe, retune Kharad, promote defaults, or expand to floors 6–11 on this result alone.
