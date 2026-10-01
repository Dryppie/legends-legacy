# Floor 5 expanded-family calibration — 29 September 2026

**Subsequent floor-5 work:** the [alternate-gear search](Tower-Floor5-Alternate-Gear-Challenge-20260929.md) expanded the family to **103 recipes / fourteen compositions**. Its later [health refinement](Tower-Floor5-Gear-Refinement-20260929.md) found no tested setting satisfying both gear viability and the strongest-recipe ceiling; no scalar was applied. This report's 89-cell accepted result and live health/offense remain unchanged, but they do not establish balance for the expanded family. Keep all 103 recipes for the next damage/healing diagnostic and prospective calibration. The [current handoff](Tower-Continuation-Handoff-20260928.md) records **910,647 exclusions**. Next-step language below remains historical.

**Applied locally and verified:** floor-5 guardian health is **3.3102803755 (+4%)**, with offense unchanged at **4.4702934848**. Fresh confirmation across **89 exact recipes / twelve compositions** establishes two distinct viable lineups at **70/200 (35%)** and **43/200 (21.5%)**; every adjusted upper bound is below 50%. All **17,800 native inputs and 89 full replays** match the applied content. **82 backend regressions and six seed-free selection checks passed**, with three intentional opt-in skips. These remain related poison-focused teams with hypothetical gear ownership; broader archetypes and ordinary-player acquisition are not established.

Target: primary-game floor-5 guardian content and the offline Balance Harness. This prospective scope follows the [supported-search challenge](Tower-Floor5-Search-Challenge-20260929.md), whose strongest new lineup won 80/128 at the current setting. Keep the entire **89-cell / twelve-composition family**, including all 60 earlier recipes and every exact new reference/finalist/gear variant. Do not import the searches a second time.

Player budget remains ten level-40 characters, five unascended/unevolved Essences each, tier-1 Epic / Fine / rank-3 equipment, fixed rolls and party positions, canonical Essence order and no styles. Full ownership remains hypothetical; withdrawn supplies stay withdrawn. Keep `affinity-creation-with-benchmark-validation-v1`; this scope does not alter or rerun the search algorithm.

## Frozen entry and prospective rule

Source: `TestResults/tower-balance-pass-floor5-search-challenge-expanded-screen-study-20260929`, manifest **`13a0e8be32ef663f6995c5e8c4c78db4ce595a1f56180224614625b0d67e3a11`**. Entry health/offense: **3.1829618995 / 4.4702934848**. Whole-Tower SHA: **`9487aa92e78bb603f571a728aa43713647e11384f4463f95979a6694bca701b5`**. Its 490 pinned inputs remain unchanged, and no backend C# source is newer than `TestResults/tower-reference-coverage-build-20260929`.

Starting exclusion union: **903,004**, anchored by `TestResults/tower-balance-pass-floor5-search-challenge-expanded-screen-owner-20260929/seed-ledger.json`, SHA **`fb99dd82a0227cda3d97995a2a7040675cad46c579716f24562b900b74783325`**. Preserve every linked reservation, all closed weakening scopes and the completed search scope. Historical observations are selection context and are never pooled into this scope's assessments.

1. Authenticate the saved source, runtime and current content. Perform zero-fight preparation of the unchanged 89 recipes against current content, checking native settings parity.
2. Screen health factors **1.02, 1.04 and 1.06**, relative to entry health. Each gets **64 fresh seeds across all 89 cells**. Offense, other guardian scalars, mechanics and every player recipe stay fixed. This bounded health-only range tests whether modest increases can contain the newly discovered strongest team; no untested candidate is assumed to pass.
3. A grid setting qualifies only if its strongest cell wins at most **28/64**, and at least **two actual per-slot Essence compositions** have a cell with at least **12/64 wins**. Rank by most qualifying compositions, then highest second-composition wins, then maximum rate closest to 30%, then smallest health increase, then ordinal setting label. Retain at most **two** settings for a separate stability panel. If none qualifies, close this scope without confirmation or content changes.
4. Screen each retained setting on **128 new seeds across all 89 cells**. Require maximum wins at most **48/128** and at least two compositions with at least **24/128 wins**. Apply the same ranking and freeze one setting, or close without confirmation if none qualifies. Gear/identity variants do not count as new compositions.
5. Confirm the selected setting **once on 200 fresh seeds per cell**, all **89 cells / 17,800 fights**. Approximate simultaneous 95% Bonferroni-Wilson intervals across the entire 89-cell family must have every upper bound at most 50%, and at least two distinct compositions with a cell lower bound at least 10%. The harness's one-composition `Pass` alone is insufficient. No pooling, retries, sample extension, changed settings, dropped recipes or selection from confirmation outcomes.
6. Only if both gates pass, apply the exact confirmed floor-5 health value locally. Require offense unchanged and whole-Tower semantic equality to the confirmed snapshot after this one authorized scalar substitution. Verify **all 17,800 native inputs and 89 full replays** against current content, without new seeds. Then run the repository regression wrapper and update the handoff. If selection or confirmation fails, preserve evidence and leave guardian content unchanged.

Resource limits per phase: native **840 seconds**, process **900 seconds**, **2 GiB output**, at most **20,000 fights**. Before confirmation, project time and archive size from the selected stability screen by **200/128**, and require both below 80% of the native/output limits. Maximum scope: preparation; three 5,696-fight grid screens; at most two 11,392-fight stability screens; one 17,800-fight confirmation = **57,672 study fights**, plus at most **89 existing-seed application replays** (**57,761 total executions**). Maximum new seed reservations: **648** (192 + 256 + 200). Report duration separately; no pacing threshold is invented.

The exclusive local driver is `TestResults/tower-floor5-expanded-calibration-driver-20260929.py`. It saves this protocol, entry content and decisions before combat. Owners remain under `tower-balance-pass-…-owner-20260929` so future history imports every reservation. No completed path is resumed, overwritten or silently retried. The [current handoff](Tower-Continuation-Handoff-20260928.md) is the continuation entry point.

## Completed grid screens

All three settings completed **5,696 fights each / 17,088 total**, with independent saved-outcome audits. The table lists each composition's best cell, sorted by wins; all 89 recipes remain included.

| Health increase | Best wins per composition, out of 64 | Qualifying compositions | Decision |
| --- | --- | ---: | --- |
| +2% | 28, 22, 11, 10, 10, 5, 4, 0, 0, 0, 0, 0 | 2 | First stability nominee |
| +4% | 28, 18, 9, 8, 6, 5, 2, 0, 0, 0, 0, 0 | 2 | Second stability nominee |
| +6% | 13, 8, 4, 3, 3, 2, 0, 0, 0, 0, 0, 0 | 1 | Ineligible: second composition below 12 wins |

The +2% and +4% settings both reached the maximum screening allowance; neither is accepted from these observations. The +2% setting ranks first because its second composition has more wins. The leading `bc504d…` composition averaged **120.59 seconds** at +2% and **121.69 seconds** at +4%, versus **119.13 seconds** in the earlier current-setting screen. These small-panel durations are descriptive, not a pacing acceptance test.

## Stability selection and frozen confirmation

Both stability panels completed **11,392 fights each**, using disjoint fresh seeds and the unchanged full family:

| Health increase | Best wins per composition, out of 128 | Decision |
| --- | --- | --- |
| +2% | 58, 31, 30, 24, 18, 6, 4, 0, 0, 0, 0, 0 | Reject: maximum 58 exceeds 48 |
| +4% | 47, 25, 25, 13, 10, 4, 1, 0, 0, 0, 0, 0 | Eligible: three compositions reach 24, maximum is 47 |

Only **+4% health** advances: **3.3102803755**, with offense fixed at **4.4702934848**. The full candidate Tower file differs from the entry file solely in floor-5 health. The driver froze this setting before reserving its **200 fresh seeds per cell / 17,800-fight confirmation**. Screening observations are not included in confirmation intervals. With 89 cells and 200 observations, the declared adjusted bounds correspond to no cell exceeding 75 wins and at least two distinct compositions having a cell with 35 or more wins; the actual interval calculation remains authoritative.

## Fresh confirmation and checked application

All **17,800 confirmation fights** completed in **424.49 native seconds**, with zero retries, omitted cells or reused selection observations. The independent audit returned `Pass`, and the separate actual-composition assessment established **two viable compositions**. The approximate simultaneous 95% Bonferroni-Wilson intervals include all 89 cells.

| Composition / best gear | Wins / 200 | Observed rate | Adjusted interval | Mean engine seconds, all outcomes |
| --- | ---: | ---: | ---: | ---: |
| New `bc504d…` / resistance-and-health | 70 | 35.00% | **24.51–47.18%** | 122.54 |
| New `9bd681…` / resistance-and-health | 43 | 21.50% | **13.23–32.97%** | 123.94 |
| New `fd5824…` / resistance-and-health | 23 | 11.50% | 5.80–21.52% | 120.19 |
| New `3427a0…` / ability-haste | 19 | 9.50% | 4.46–19.08% | 105.67 |
| Prior leader `297b0b…` / resistance-and-health | 12 | 6.00% | 2.32–14.62% | 124.11 |
| Prior alternative `1c2450…` / ability-haste | 4 | 2.00% | 0.42–8.97% | 102.46 |
| Historical supply-dependent composition / resistance-and-health | 3 | 1.50% | 0.26–8.19% | 117.33 |
| Remaining five compositions / all profiles | 0 each | 0% | 0–5.62% | See archived rows |

The strongest upper bound is **47.18%**. The two qualifying cells are different actual Essence lineups, not gear or identity copies. Both descend from `297b0b…`: `bc504d…` changes two Essences on slot 7 to Venomous Spiderling/Viper, while `9bd681…` changes two on slot 2 to Spider Queen Royal Venom/Viper. Their complete differences are recorded in the [search challenge](Tower-Floor5-Search-Challenge-20260929.md). Both favor resistance-and-health equipment. The third lineup's observed rate is above 10%, but its lower bound does not establish that threshold. The earlier leader's previous viability must not be transferred to this harder setting.

The guarded application changed exactly `/floors/4/guardianScaling/health` (the zero-based floor-5 entry) from **3.1829618995 to 3.3102803755**, retaining the file's formatting. Whole-Tower semantic comparison proves that no other scalar, floor or metadata changed. Native application verification matched **all 17,800 input hashes and 89 complete battle replays**, one per cell, on the current game file. No new seeds were allocated for application.

This supports the declared complete family and budget, not arbitrary parties, broad archetype diversity or acquisition readiness. The leading mean duration is about **123 seconds**; no pacing threshold was applied. No lower-Essence necessity claim follows.

## Evidence and final verification

The scope completed **seven phases / 57,672 study fights**, plus **89 application replays**, for **57,761 executions**. Native study time totaled **1,378.94 seconds**, excluding audits and application. All bounded processes drained, every phase completed without retry, and the +2% rejection and +6% screening failure remain preserved. Six seed-free checks validated actual-composition grouping, ranking and the frozen grid.

| Artifact | Path under `TestResults` | SHA-256 |
| --- | --- | --- |
| Evidence index | `tower-floor5-expanded-calibration-evidence-20260929.json` | `5871e5b07cfb9a42151f33adfe02bc960013e5e68dc4bd509b200edae167afbf` |
| Confirmation manifest | `tower-balance-pass-floor5-expanded-calibration-confirmation-study-20260929/files.json` | `58d675f7350e6f56730c70f2807edbc71cd64f0d3b792489a5d393ecb9c0cab9` |
| Confirmation result | `tower-balance-pass-floor5-expanded-calibration-confirmation-study-20260929/result.json` | `66830aa0c0b4dd0e0279a6d044a3684bf34339d7003c44026082ddb927f52dc6` |
| Independent confirmation audit | `tower-balance-pass-floor5-expanded-calibration-confirmation-owner-20260929/independent-audit.json` | `e88578400290b247611c715655ce246185aeeafbef8bbc1a432deef55d1d90e2` |
| Latest ledger | `tower-balance-pass-floor5-expanded-calibration-confirmation-owner-20260929/seed-ledger.json` | `ed3c39cf40795c6e830a274a7184d5c7df110f72bb8d5443f0f37cbd7e05405a` |
| Application result | `tower-floor5-expanded-calibration-application-owner-20260929/result.json` | `42738953f60e7bacd8c9fac2ab98e58f2427d0f3058fdccb7e93ff55db14ae01` |
| Final regression TRX | `tower-floor5-expanded-calibration-final-regression-20260929.trx` | `2104d9752cf07d96bcaacb4506f43b1541b8e56e31a5f513cef8c53249980a3d` |

The final collector verifies **761 immutable input/source/runtime pins**, unchanged recipes, both acceptance conditions and the authorized single-field production change. Latest exclusion union: **903,652**, including all **648 new reservations** and the complete preceding history. Current Tower SHA: **`0a53b4b4e453e13aee5a94cfedf8be65171928cdf8e95dc5a9bfdab954161cc8`**. Preserve all archives, unused reservations and prior owner snapshots; the local ignored evidence is not available in a clean checkout. No active study remains.

**82 backend regressions passed**, with three intentional opt-in skips. Existing compiled artifacts were reused after source-timestamp and captured-hash checks; no backend implementation changed. Completed commands (Python means the bundled runtime; these output paths must never be rerun):

```powershell
python -B -X utf8 'TestResults/tower-floor5-expanded-calibration-driver-20260929.py'
python -B -X utf8 'TestResults/tower-floor5-expanded-calibration-apply-20260929.py'
./build/run-tests.ps1 -NoBuild -ArtifactsPath 'TestResults/tower-reference-coverage-build-20260929' -Filter 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests'
python -B -X utf8 'TestResults/tower-floor5-expanded-calibration-collect-20260929.py'
git -c core.safecrlf=false diff --check
```

Changed maintained files: [tower-floors.json](../LL/src/API/API.LL/Data/world-tower/tower-floors.json), this report, the current handoff, harness README/search guide and follow-up notices in the preceding floor-5/coverage reports. Only floor-5 health changes gameplay. There are no migrations, dependency or production-configuration changes, API startups, database operations or deployments. No required verification was blocked. This is a local data edit that reaches players only through the normal content rollout.

**Next:** address floor-7 diversity using its complete 38-cell family and approved budget: five level-40 characters with five Essences, tier-1 Unique / Exceptional / rank-4 gear. Preserve its exact references and all seed exclusions, and refresh the saved family against current content before new combat because floor 5 has changed. Floors 8/12/13 and broader archetypes on floors 3/5 remain open; review pacing separately. Keep the supported search and progression assumptions. Dungeon modeling and replacement supplies are not prerequisites.
