# Floor 10: expanded-family health calibration — 29 September 2026

**Applied locally and verified:** floor-10 health is **12.8371 (+1%)**, with offense unchanged at **7.13**. Fresh confirmation of all **38 exact recipes / five compositions** establishes **two viable compositions**, winning **72/256 (28.13%)** and **68/256 (26.56%)**. Their family-adjusted intervals are **20.08–37.86%** and **18.73–36.21%**; every tested recipe's upper bound is below 50%. All **9,728 inputs and 38 complete replays** match the applied content. Final backend verification: **91 passed**, four intentional opt-in skips. No deployment occurred.

## Prospective scope

Target: the primary LL API's World Tower data and offline Balance Harness. The [search challenge](Tower-Floor10-Search-Challenge-20260929.md) is closed without confirmation. It found two actual new compositions; the full-family screen gave their strongest recipes **37/96 and 28/96**, compared with **16/96** for the old leader. The stronger candidate exceeded that scope's conservative selection margin, which does not establish a true win rate above 50%. This separate calibration tests small health increases to seek more confirmation margin while preserving two viable compositions.

Retain all **38 exact recipes / five actual compositions / seven gear profiles** from `TestResults/tower-balance-pass-floor10-supported-search-expanded-screen-study-20260929`, manifest **`75cbba8df703ec658527834ece8db87da4b3feb7b24c8bdca8dd049d6a393786`**. Prior closeout: `TestResults/tower-floor10-supported-search-evidence-20260929.json`, SHA **`6a9c26622e9951d2c2969faa57e434f93f8f2c8572e1a4af926463d2053d1a4d`**. Keep every original recipe, both exact finalists, every projected reference and all gear variants. Preserve raw Essence order, actor/item/Essence identities and party positions. Actual composition counts use per-slot Essence sets without rewriting recipes.

Entry floor-10 health/offense is **12.71 / 7.13**, defense/resistance **2.29 / 2.29**, penetration/regeneration **1 / 1**. Whole-Tower SHA: **`0b52aedce19700d542dbcb9a0b2441d272f1181457642e03ba8c7a14e20f01fd`**. Initial exclusion union: **909,066**, including all unused reservations from prior failures. Keep fifteen level-50 characters, six level-1 unascended/unevolved Essences each, tier-2 Legendary/Masterpiece/rank-5 equipment, baseline rolls and no styles. Ownership is hypothetical; the candidate compositions remain closely related poison builds.

Freeze before any combat:

1. Authenticate the complete source, prior closeout and all captured runtime/input pins. Reuse the preserved corrected floor-10 build; run the relevant backend regressions through `build/run-tests.ps1 -NoBuild`. Prepare the entire current family with zero fights/seeds and require current content/settings parity. Both historical qualifications remain closed; no need to replay them again when the current qualified runtime and inputs match.
2. Evaluate exactly **+1%, +2%, +3% health** relative to entry: **12.8371, 12.9642, 13.0913**. Each candidate uses a separate fresh **64-seed panel across all 38 recipes**, **2,432 fights**. Only health changes in isolated content copies. Every cell must win at most **26/64**, and at least two actual compositions must have a cell at **13/64 or above**, to qualify for stability testing.
3. Rank eligible settings by qualifying-composition count, second-best composition wins, strongest rate nearest 30%, smallest health change, then label. Freeze at most the first **two** for independent **128-seed complete-family stability panels**, **4,864 fights each**. Evaluate every frozen nominee. Stability requires every cell at most **44/128**, with at least two actual compositions at **26/128 or above**. Use the same ranking to select one eligible setting. Otherwise close without confirmation.
4. Use 80% resource admission margins against **840 native seconds / 2 GiB**. Before allocation, project the largest 128-seed selection panel from the preceding complete 96-seed screen; project confirmation from the selected 128-seed stability panel. Freeze one independent **256-seed full-family confirmation**, **9,728 fights**, at the selected setting. No selection observations are pooled into it. Across all **38 exact cells**, require every approximate simultaneous 95% Bonferroni-Wilson upper bound at most 50%, and at least two actual compositions with a lower bound at least 10%. The native one-composition `Pass` alone is insufficient.
5. Only after full acceptance, apply the confirmed floor-10 health value locally. Preserve every other field and floor. Verify all **9,728 confirmed input hashes and 38 full battle reports** against current content, using existing confirmation seeds only. Rerun relevant backend regressions after the data edit and independently reconcile selection, input pins, process receipts, exact content changes and seed accounting. A failed confirmation or technical check permits no fallback or retry in this scope.

Maximum **26,752 study fights + 38 conditional verification replays = 26,790 executions**, **704 fresh reservations** (192 grid + at most 256 stability + 256 confirmation). Each phase remains below **20,000 fights / 840 native seconds / 900 process seconds / 2 GiB**. Post-application verification uses the existing **600-native / 660-owner-second** bound. No retries, panel extensions, reused seeds, pooled samples, dropped recipes, search runs, algorithm changes, Essence permutations or identity optimization.

The driver is `TestResults/tower-floor10-expanded-calibration-driver-20260929.py`; the preserved runtime is `TestResults/tower-floor10-diversity-supported-build-20260929`. Freeze the driver and a copy of this protocol before allocation. Keep all earlier closed archives unchanged, including both floor-10 search scopes. Evidence under ignored `TestResults` is local and needs separate preservation. No dungeon work, replacement supplies, migration, dependency/configuration change, database operation or deployment is included.

## Completed selection and confirmation

Every panel retained all 38 exact recipes. Only +1% met the grid gate; the +2% and +3% panels each had just one composition at or above 13/64. These are separate fresh panels, not pooled observations or paired estimates of a health effect.

| Health change | Health | Best recipe per actual composition, wins / 64 | Qualifying compositions |
| --- | ---: | --- | ---: |
| +1% | 12.8371 | 19, 16, 8, 3, 0 | 2 |
| +2% | 12.9642 | 13, 9, 7, 6, 0 | 1 |
| +3% | 13.0913 | 14, 7, 3, 3, 0 | 1 |

The single frozen stability candidate, **+1%**, produced **36, 31, 21, 7 and 0 wins / 128** for the five composition leaders. Two compositions exceeded 26/128, and every recipe stayed at or below 44/128. Confirmation admission projected **335.577 native seconds / 313,544,916 bytes**, below the predeclared 80% limits. One independent 256-seed confirmation followed:

| Composition | Best gear | Wins | Adjusted interval | Mean reported seconds |
| --- | --- | ---: | --- | ---: |
| New `c01a33fe…` | armor-and-health | **72/256 (28.13%)** | **20.08–37.86%** | 72.04 |
| New `52889519…` | armor-and-health | **68/256 (26.56%)** | **18.73–36.21%** | 72.11 |
| Alternating original | armor-and-health | 24/256 (9.38%) | 5.00–16.90% | 75.46 |
| Repeated original | armor-and-health | 15/256 (5.86%) | 2.64–12.50% | 75.95 |
| Authored original | armor-and-health | 0/256 | 0.00–3.88% | 76.94 |

Bounds are approximate simultaneous 95% Bonferroni-Wilson intervals over **all 38 exact cells**, not just the five rows shown. This family/sample size requires qualifying cells at **42–102 wins**, with every cell at most 102. Exactly **two cells / two actual compositions** qualify; both come from the preceding supported search. The original leader no longer clears the 10% lower-bound rule at the new setting and expanded family. This is two confirmed viable choices in the new family, not three cumulative choices obtained by mixing old and new results.

All **30 recipes using other gear profiles won 0/256**. The successful lineups each make two Essence substitutions on one character relative to the alternating reference; both remain related poison builds. Counting ignores Essence permutations, identity fields and gear variants without rewriting any archived scenario. This result establishes the declared fixed-budget family; it does not establish broad archetype diversity, alternate-gear viability or ordinary-player acquisition. Durations are descriptive, with no approved pacing threshold.

## Local application and verification

Only [floor-10 `guardianScaling.health`](../LL/src/API/API.LL/Data/world-tower/tower-floors.json) changed: **12.71 → 12.8371**. Offense **7.13**, defense/resistance **2.29 / 2.29**, penetration/regeneration **1 / 1**, every other guardian field and every other floor remain unchanged. Parsed whole-Tower comparison exactly matched the accepted confirmation snapshot.

Post-application native verification reproduced **all 9,728 confirmed input hashes and one full report per exact recipe, 38 replays**. It allocated zero fresh seeds. Final Tower SHA: **`aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`**.

This scope completed **21,888 study fights + 38 replays = 21,926 executions**, reserved **576 fresh values** and used **749.652 native study seconds**. Exclusion union: **909,642**. All phases completed without retry and drained their processes. Reconciliation checked **777 unchanged current input/runtime pins**, separately verifying the authorized live Tower edit. The entire 38-cell source family is preserved.

The relevant backend suite passed **91 tests before and 91 after application**, with four intentional opt-in skips on each ordinary run, through `build/run-tests.ps1 -NoBuild`. It used the previously preserved corrected build; no rebuild or historical binary substitution occurred. Each native scientific phase and application verification was enabled separately and passed. The maintained owner passed **14 fresh Python reference/retention tests**; the new driver passed **11 fresh selection, ranking, family, resource and accounting checks**. All required commands completed; no verification is blocked.

| Immutable artifact under `TestResults` | SHA-256 |
| --- | --- |
| `tower-floor10-expanded-calibration-evidence-20260929.json` | `34d3d46f32c31a34126d8aac9b0a64d8340b874ead8ab95e1eae64091a68816b` |
| `tower-balance-pass-floor10-expanded-calibration-confirmation-study-20260929/files.json` | `d8992d0e6c06caa589c92a9d74066a5b28aa83a56ba091ff8b4bbb1889bbef69` |
| `tower-balance-pass-floor10-expanded-calibration-confirmation-study-20260929/result.json` | `fad70d8ceb9a5c8b0c2a0c7b2bb7eea5f78eb46018f12d3e0fe958a0ad0eb904` |
| `tower-balance-pass-floor10-expanded-calibration-confirmation-owner-20260929/independent-audit.json` | `0d6273bed2f7a9aa4a4347ef0bc18beac4c0f14f2aa6ef88db3593da24fc195e` |
| `tower-balance-pass-floor10-expanded-calibration-confirmation-owner-20260929/seed-ledger.json` | `004f6404fd455db359f420f9678a30ed11a025ef3870c215e663ffc7deeffb84` |
| `tower-floor10-expanded-calibration-application-owner-20260929/result.json` | `a72cdc6c6e7c1e654e9884f64ae1b1ad256e791d874e8f497910475faca4147e` |
| `tower-floor10-expanded-calibration-final-regression-20260929.trx` | `f2c17bb7226f648b5388c92a8871935261c65ad8c13cc788ee8469c7f067f775` |

Driver SHA: **`6cd93ec1162e09ff1ef2c9b0ebcb6db06200a04bed43811207152bb4d63cab88`**. The frozen `tower-floor10-expanded-calibration-driver-20260929/protocol.md` preserves the pre-combat protocol. Publishing this result changes the maintained report, not that snapshot. Never repin prior inputs or overwrite completed outputs. Ignored local `TestResults` evidence must be preserved separately from a clean checkout.

Commands executed (`python` denotes the bundled runtime; completed output paths cannot be reused):

```powershell
$filter = 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests|FullyQualifiedName~BalanceHarnessFloor10QualificationTests'
./build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/tower-floor10-diversity-supported-build-20260929 -Filter $filter
python -B -X utf8 'Balance Harness/analysis/test-tower-reference-coverage.py'
python -B -X utf8 TestResults/tower-floor10-expanded-calibration-checks-20260929.py
python -B -X utf8 TestResults/tower-floor10-expanded-calibration-driver-20260929.py
python -B -X utf8 TestResults/tower-floor10-expanded-calibration-apply-20260929.py
# Repeat the backend command above after application, preserving both TRX receipts.
python -B -X utf8 TestResults/tower-floor10-expanded-calibration-collect-20260929.py
git -c core.safecrlf=false diff --check
```

Maintained changes in this scope are the one floor-10 health value, this report, the [handoff](Tower-Continuation-Handoff-20260928.md), both harness guides and follow-up notices in the preceding floor-10/status reports. The earlier harness implementation changes remain intact. No new combat/search code, equipment curve, dungeon/supply behavior, dependency, migration or application configuration changed. No database operation or deployment occurred.

## Continuation

Use the accepted **38-cell confirmation**, manifest **`d8992d0e6c06caa589c92a9d74066a5b28aa83a56ba091ff8b4bbb1889bbef69`**, as the new floor-10 baseline at **health 12.8371 / offense 7.13**. Preserve all five compositions, seven gear profiles and exact nominees. Do not revert to the older 21-cell family, reuse the rejected +2%/+3% observations as confirmation, reimport finalists or rerun either closed search scope.

Together with the preceding per-floor confirmations, all fifteen floors now have at least two viable compositions in their latest accepted families. That is a fixed-budget milestone, not broad balance coverage: poison-related builds and one gear profile still dominate most floors. The next useful Tower work is an intentional broader-archetype or alternate-gear challenge that retains known strongest recipes and uses the existing search algorithm. Floor-8 pacing still needs a user-approved duration target before tuning; ordinary acquisition remains a separate unresolved dimension and does not justify restoring selectable supplies or diverting this queue into dungeons. No active study remains.
