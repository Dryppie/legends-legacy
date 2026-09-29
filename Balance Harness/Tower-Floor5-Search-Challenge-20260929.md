# Floor 5 supported-search challenge — 29 September 2026

**Later applied calibration:** the [expanded-family calibration](Tower-Floor5-Expanded-Calibration-20260929.md) raised floor-5 health by 4% to **3.3102803755** and confirmed the complete 89-cell family. Two lineups qualify at **70/200 and 43/200**; all 17,800 inputs and 89 replays match. The search results below remain the preceding discovery evidence. Use the [current handoff](Tower-Continuation-Handoff-20260928.md) for the 903,652-value exclusion union and floor-7 queue.

**Searches completed; stronger builds expose an expanded-family ceiling problem.** Four new compositions were found with the existing algorithm. In the fresh **89-cell screen**, their best cells won **80/128, 62/128, 35/128 and 35/128**, versus **29/128** for the old leader. The strongest exceeds 50% in this screen, so the declared acceptance path stopped before confirmation. This is useful search progress and a reason for a separate recalibration, not a passing balance result. Gameplay values remain unchanged.

Target: the primary game's offline Balance Harness. This prospective challenge follows the [closed scalar/diversity scopes](Tower-Floor5-Diversity-20260929.md). Guardian health/offense stay **3.1829618995 / 4.4702934848** throughout. Keep `affinity-creation-with-benchmark-validation-v1`, the approved gear curve, ten level-40 characters with five unascended/unevolved Essences, tier-1 Epic / Fine / rank-3 gear, fixed rolls and positions, no styles and canonical Essence order. Ownership is hypothetical; no acquisition source is added.

## Frozen inputs and harness corrections

Use the completed current-content baseline at `TestResults/tower-balance-pass-floor5-diversity-grid-baseline-study-20260929`, manifest SHA **`c1d09b07023a1afcaf20886e173009bf434d7b7bb7d2cfb5b83120d87f59b135`**. Its **60 exact recipes / eight actual compositions** remain in every expanded-family evaluation. All source data, runtime DLLs and C# hashes still match; no backend source is newer than the existing `tower-reference-coverage-build-20260929` artifacts. The intentional Python owner change is preserved separately from the authenticated original `owner-source.py`; no historical archive is repinned.

Before combat, the owner now supports `--search-gear`, selects three distinct per-slot Essence compositions rather than counting duplicate labels/identities, and preserves both exact generated nominees and their frozen gear projections. Exact scenario duplicates share one execution but retain every provenance link. These are experiment orchestration and coverage corrections, not a changed proposal/search algorithm. Fourteen seed-free regressions passed, covering gear selection, duplicate references, immutable recipes, exact finalist retention, repeated imports and illegal projection changes.

Whole-Tower SHA: **`9487aa92e78bb603f571a728aa43713647e11384f4463f95979a6694bca701b5`**. Initial exclusion union: **902,402**, anchored by `TestResults/tower-balance-pass-floor5-diversity-refinement-health-025-owner-20260929/seed-ledger.json`, SHA `ca83e61d106c670ee9af7bf79d5d1da44a6b6e0838ce04f9d9d110a5c93ea970`. Exclude all linked reservations, including unused ones and closed scopes.

## Declared sequence and decisions

1. Authenticate the saved source and all unchanged runtime inputs; prepare the same 60 recipes against current content with zero fights. Verify settings and content parity. Do not modify any guardian scalar.
2. Run exactly **two independent searches**, one at resistance-and-health and one at ability-haste. For each gear profile, select its three best distinct measured compositions from the frozen baseline by wins, lower mean guardian health and ordinal ID; use its strongest as benchmark. At resistance-and-health these are `297b0b…`, the historical supply-dependent composition at the declared hypothetical budget, and `1c2450…` (14, 7, 6 wins out of 64). At ability-haste they are `1c2450…`, `297b0b…` and the historical composition (7, 3, 2 wins). This does not transfer historical supply ownership to the game.
3. Each search uses the unchanged **528-fight policy** and **128 separate evaluation seeds for all five nominees**: 1,168 fights and 237 reserved values per search. Preserve all three exact projected references and both generated finalists, regardless of the internal benchmark-retention verdict or their evaluation scores. The two searches have disjoint seeds and are not a paired gear comparison or evidence of search-policy superiority.
4. Freeze an expanded family containing all original 60 cells, all exact search references/finalists, and both finalists from each search at all seven existing gear profiles. Deduplicate only identical complete scenarios. Preserve all identities and all import provenance. Maximum family size is **98** (60 + two searches × [3 references + 2 exact finalists + 14 gear projections]). If neither search produces an actual composition outside the original eight, close `NoNovelComposition` without additional combat.
5. Otherwise screen the **entire expanded family on 128 fresh seeds**. A confirmation is eligible only if the maximum cell wins are at most **48/128**, at least **two distinct compositions** have a cell with at least **24/128**, and at least **one of those qualifying compositions is outside the original eight**. No recipe is removed based on this screen. If any requirement fails, close `NoEligibleDiversityConfirmation` and preserve all evidence without confirmation or content changes.
6. If eligible, confirm the **whole frozen expanded family once on 200 fresh seeds per cell**. This sample count is declared now to fit even 98 cells within the existing 20,000-fight phase limit. Approximate simultaneous 95% Bonferroni-Wilson intervals use the actual complete family size. Every upper bound must be at most 50%; at least two actual compositions must have a cell lower bound at least 10%, including at least one composition absent from the original eight. Otherwise close `DiversityNotConfirmed`; do not extend samples, pool phases, retry, change settings or drop recipes.
7. On acceptance only, run the existing native parity check against unchanged current content: every confirmation input and one complete replay per cell, no new seeds. Record the additional confirmed choices and the limits of their differences. This pass never edits gameplay data. Record fight duration separately; no pacing threshold is introduced.

Resource limits remain native **840 seconds**, bounded process **900 seconds**, **2 GiB output** and **20,000 fights per phase**. Project confirmation time/archive size as 200/128 of the complete-family screen and require both within 80% of the native/output limits before allocating its panel. Maximum scope: zero-combat preparation, **2,336 search/evaluation fights + 12,544 screening fights + 19,600 confirmation fights = 34,480 study fights**, plus at most **98 existing-seed parity replays**. Maximum new reservations: **802** (474 search/evaluation + 128 screening + 200 confirmation). Exact output paths are exclusive; preserve technical failures without retry or overwrite.

The ignored execution owner is `TestResults/tower-floor5-search-challenge-driver-20260929.py`. It archives this protocol, source decisions, runtime pins, all closed-scope outcomes and the extra composition/novelty assessment. Keep every phase ledger under the `tower-balance-pass-…-owner-20260929` history registry. No active old scalar study is resumed. Use the [current handoff](Tower-Continuation-Handoff-20260928.md) for subsequent work.

## Completed searches and expanded family

Both searches completed **1,168 fights each**, with native reconstruction and independent outcome audits. Both returned `ChallengerNeedsConfirmation` under the unchanged search policy. These evaluation observations are selection evidence; they do not establish a passing floor or universal search superiority.

| Search gear | First generated finalist | Second generated finalist | Retained benchmark, same panel |
| --- | ---: | ---: | ---: |
| Resistance-and-health | `bc504d…`: **78/128 (60.94%)** | `9bd681…`: **61/128 (47.66%)** | `297b0b…`: **24/128 (18.75%)** |
| Ability-haste | `fd5824…`: **41/128 (32.03%)** | `3427a0…`: **29/128 (22.66%)** | `1c2450…`: **11/128 (8.59%)** |

The frozen expanded family has **89 exact cells, 12 actual compositions and four new compositions**. The 60 original cells are unchanged. Of 38 import provenance entries, 29 add scenarios: four exact finalists, 24 additional gear variants and one exact projected reference. Four projections match their already-added exact finalists, and five reference entries match existing exact scenarios. Every one of the ten search-nominee entries is represented. No identity-distinct recipe was collapsed merely because its Essences match.

Each new composition differs from its nearest old lineup by exactly two Essence substitutions on one character:

| New composition | Nearest old composition | Slot | Removed Essences | Added Essences |
| --- | --- | ---: | --- | --- |
| `bc504d…` | `297b0b…` | 7 | Lumo Wisp, Wood Nymph | Venomous Spiderling, Viper |
| `9bd681…` | `297b0b…` | 2 | Blue Slime, Forest Spirit | Spider Queen Royal Venom, Viper |
| `fd5824…` | `1c2450…` | 7 | Forest Spirit, Hollow Stag | Venomous Spiderling, Viper |
| `3427a0…` | `1c2450…` | 7 | Blue Slime, Lumo Wisp | Spider Queen Royal Venom, Viper |

These are real build changes with fixed party positions and canonical ordering. They remain related poison-focused teams; the result does not establish broad archetype diversity. Full IDs and exact recipes are retained in each search's `evaluation-cells.json` and the driver's `frozen-expanded-family.json`.

## Fresh expanded-family screen and closed decision

All **89 × 128 = 11,392 fights** completed on a separate seed panel. The native run took **277.03 seconds**, with no retry; the independent audit authenticated the saved outcomes and reconstructed every reported mean. No search evaluation observation was pooled into this screen.

| Composition | Best measured gear | Wins / 128 | Observed rate | Mean engine seconds, all outcomes |
| --- | --- | ---: | ---: | ---: |
| New `bc504d…` | Resistance-and-health | 80 | 62.50% | 119.13 |
| New `9bd681…` | Resistance-and-health | 62 | 48.44% | 121.45 |
| New `fd5824…` | Resistance-and-health | 35 | 27.34% | 121.55 |
| New `3427a0…` | Ability-haste | 35 | 27.34% | 112.20 |
| Prior leader `297b0b…` | Resistance-and-health | 29 | 22.66% | 126.49 |
| Prior alternative `1c2450…` | Ability-haste | 12 | 9.38% | 111.53 |
| Historical supply-dependent composition, hypothetical declared gear | Resistance-and-health | 9 | 7.03% | 120.70 |
| Remaining five old compositions | Every tested profile | 0 each | 0% | See saved rows |

Five compositions exceeded the screening minimum of 24 wins, including all four new ones. The maximum **80/128** exceeds the declared **48/128** eligibility ceiling, so the driver closed **`NoEligibleDiversityConfirmation`**. No confirmation panel was reserved, no conditional parity replay ran, and no gameplay value changed. These are screening rates, not confirmed simultaneous balance intervals. The older 60-cell confirmation remains valid evidence for its frozen family but cannot establish the ceiling for these additional recipes.

**Next: recalibrate floor 5 against this complete 89-cell family.** Keep all twelve compositions, every exact original/projected reference and each new finalist/gear variant. Declare bounded guardian settings and a fresh complete-family confirmation before further combat. The strongest newly found route warrants testing increased difficulty; the rejected weakening scopes remain closed. Keep the current player budget, supported search, canonical order and existing acceptance rule, requiring at least two distinct viable compositions after calibration. A 200-seed complete-family confirmation would contain 17,800 fights within the current phase cap; declare its exact sample count prospectively. Do not infer that stronger settings will pass without running them. Floor 7 remains the next independent diversity target; acquisition and dungeon work are not prerequisites.

## Evidence, verification and changed files

This scope completed **four phases / 13,728 study fights**: zero-combat preparation, two searches with nominee evaluation, and the expanded-family screen. It reserved **602 fresh values**, bringing the complete exclusion union to **903,004**. Native phases totaled **369.15 seconds**, excluding audits and wrapper overhead. All phases completed with zero retries and drained bounded processes; no confirmation or parity replay ran because the screening ceiling failed.

The [evidence index](../TestResults/tower-floor5-search-challenge-evidence-20260929.json) binds the four manifests/audits, verifies **599 immutable input/runtime pins**, all original and exact nominated recipes, complete import provenance, and byte-identical Tower content. Local ignored artifacts are required to inspect the evidence; they are not included in a clean checkout.

| Artifact | Path under `TestResults` | SHA-256 |
| --- | --- | --- |
| Evidence index | `tower-floor5-search-challenge-evidence-20260929.json` | `82456e271e0d35a33597cc57fa7e3828bb1288607d862bebb900b836a2b20dc2` |
| Expanded-family manifest | `tower-balance-pass-floor5-search-challenge-expanded-screen-study-20260929/files.json` | `13a0e8be32ef663f6995c5e8c4c78db4ce595a1f56180224614625b0d67e3a11` |
| Independent screen audit | `tower-balance-pass-floor5-search-challenge-expanded-screen-owner-20260929/independent-audit.json` | `7f7aa3f651b37e24ab82fb5e78539245e4f78361e25a306555404dfc772fd2f8` |
| Latest ledger | `tower-balance-pass-floor5-search-challenge-expanded-screen-owner-20260929/seed-ledger.json` | `fb99dd82a0227cda3d97995a2a7040675cad46c579716f24562b900b74783325` |
| Final regression TRX | `tower-floor5-search-challenge-final-regression-20260929.trx` | `3f55d94610417adb38141cc172e964867943c9c9abd72c03568a346df6ff34ab` |

**82 backend regressions passed**, with three intentional opt-in skips. **14 Python tests passed** (six preceding reference-coverage tests plus eight challenge/import regressions). The existing compiled backend was reused after hash and source-timestamp verification; C# and combat/search implementation did not change. Preserve the old owner snapshots: the intentional Python orchestration edits do not authorize repinning or rerunning historical archives against the new owner.

Changed maintained files:

- [run-tower-balance-pass.py](analysis/run-tower-balance-pass.py): explicit measured gear selection; reference deduplication by actual per-slot Essence sets; exact finalist retention plus safe gear projection; scenario-based import IDs/deduplication and complete provenance. The strongest distinct measured reference becomes the explicit benchmark. No default proposal policy or search fight budget changed.
- [test-tower-reference-coverage.py](analysis/test-tower-reference-coverage.py): regression coverage for those selection/import guarantees, including duplicate identities, preserved raw recipes, repeated imports and rejected slot/budget changes.
- This report, the current handoff, harness README/search guide and historical follow-up notices: measured progress, the expanded family and the next calibration scope. The local driver, collector and evidence remain under ignored `TestResults`.

Representative completed commands (Python is the bundled runtime; output paths must never be reused):

```powershell
python -B -X utf8 'Balance Harness/analysis/test-tower-reference-coverage.py'
python -B -X utf8 'TestResults/tower-floor5-search-challenge-driver-20260929.py'
./build/run-tests.ps1 -NoBuild -ArtifactsPath 'TestResults/tower-reference-coverage-build-20260929' -Filter 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests'
python -B -X utf8 'TestResults/tower-floor5-search-challenge-collect-20260929.py'
git -c core.safecrlf=false diff --check
```

For future, separately declared searches, `--mode search --search-gear ability-haste` selects that measured profile; omitting the option still selects the strongest measured cell's profile. `--add-search <completed-search> --retain-search-references` preserves both exact generated finalists, their frozen gear variants and all three exact references. The tests verify that Essence order is not changed and duplicate representations do not become independent compositions.

No required verification was blocked. No gameplay data, production configuration, dependency, migration or database change was made; no API was started and nothing was deployed. Tower SHA remains **`9487aa92e78bb603f571a728aa43713647e11384f4463f95979a6694bca701b5`**. All 903,004 exclusions and earlier closed scopes remain preserved, and no study is active.
