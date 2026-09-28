# Floor-11 calibration with controlled upgrades — 28 September 2026

**The expanded family identifies a useful calibration interval, but none of the six tested settings qualifies.** At 2× Health/Power, the strongest seven-Essence party wins **20/32 (62.5%)**; at 2.5× it wins **2/32 (6.25%)**. Every four-/six-Essence control, including both level-matched controls, wins zero at those two endpoints. No setting is selected or applied.

**Completed follow-ups:** the separately frozen [finer grid and first fresh confirmation](Tower-Floor11-Refined-Calibration-20260928.md) retained all 228 combinations and selected **2.0625×**, whose strongest historical result was 14/32. The separate fresh panel returned **`Fail`** at **135/256 (52.73%)**. Its adjusted interval crosses 50%, so this preserves an observed breach without establishing a true rate above the ceiling. The subsequent [2.25× full-family confirmation](Tower-Floor11-Higher-Setting-Confirmation-20260928.md) now returns **`Pass`**: its strongest team wins **47/256 (18.36%)**, adjusted interval **11.10–28.82%**, and all 30 controls win zero. The recommended next step is checked local application of the 2.25× baseline. These are separate experiments; the original expanded grid's no-selection result remains unchanged. The supported search and repeating equipment curve stay unchanged.

## Results

Each entry is the maximum observed wins out of 32 for an individual complete party in that cohort. These are historical-seed screening results, not pooled rates or confidence bounds.

| Linked factor | Health | Power | Seven-Essence maximum | Six, level 50 | Six, level 60 | Four, level 30 | Intended cells above 50% |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1.500 | 4.3500 | 5.580 | 32 | 16 | 27 | 0 | 104 |
| 1.625 | 4.7125 | 6.045 | 32 | 4 | 12 | 0 | 90 |
| 1.750 | 5.0750 | 6.510 | 32 | 1 | 3 | 0 | 29 |
| 1.875 | 5.4375 | 6.975 | 26 | 0 | 1 | 0 | 4 |
| 2.000 | 5.8000 | 7.440 | 20 | 0 | 0 | 0 | 1 |
| 2.500 | 7.2500 | 9.300 | 2 | 0 | 0 | 0 | 0 |

The strongest sampled build at factors 1.75 through 2.5 is **`floor11-six-1/add/essence.shadow_imp`**, the repeated source party with Shadow Imp appended at every position, level 60 and resistance-and-health gear. It wins 32, 26, 20 and 2 fights respectively. At 2× its mean remaining guardian health is 5.60% and mean fight duration is 111.88 seconds. It loses 12 previously winning seeds relative to factor 1.5 and gains none. Its full ordered ten-character recipe remains in the [cell archive](../TestResults/tower-floor11-expanded-20260928/cells.json); the five original roles are retained and repeated, not replaced by one uniform build.

Keeping the whole upgrade family matters. At 2×, the alternating Shadow Imp upgrade wins **14/32** and the repeated Gnoll Shaman upgrade **11/32**, but selecting either would conceal the stronger 20/32 party. The previous alphabetically selected Blackjaw representatives win only **1/32** and **0/32** at 2×. Their earlier 32/32 tie did not establish that they were the best compositions at harder settings. The original seven-Essence family has zero wins at 2×, exactly as in the earlier calibration.

The control-separation condition holds from factor 1.75 onward. Factors 1.75, 1.875 and 2 still breach the intended ceiling; 2.5 falls below the intended viability floor. Result: **`NoSeparatingSettingInGrid`**. This does not prove that intermediate settings fail or that the endpoints bracket every possible team's viable range. Per-team outcomes need not be monotonic: the repeated Royal Venom addition gains one previously losing seed at both 1.875 and 2 while losing many others. All settings and recipes must remain visible.

There are **five draws**, counted as nonwins: two for alternating Undead at 1.5, one each for alternating Goblin Shaman and Undead at 1.625, and one for repeated Undead at 1.875. Full outcome counts, means and paired changes against factor 1.5 are in [result.json](../TestResults/tower-floor11-expanded-20260928/result.json).

## Frozen comparison

This offline follow-up to the [controlled seventh-Essence screen](Tower-Seventh-Essence-Upgrades-20260928.md) retains its **entire 228-cell family**. It changes only floor 11's guardian Health and Power in captured content copies. The supported search algorithm and the user's repeating equipment curve stay unchanged.

| Cohort | Team/gear combinations | Level | Essences |
| --- | ---: | ---: | ---: |
| Original intended references, all seven gear choices | 84 | 60 | 7 |
| Every legal uniform addition to the two six-Essence sources | 114 | 60 | 7 |
| Original six-Essence controls, all seven gear choices | 14 | 50 | 6 |
| Level-matched six-Essence controls, resistance-and-health | 2 | 60 | 6 |
| Original four-Essence controls, all seven gear choices | 14 | 30 | 4 |

All existing party positions, ordered Essences, gear, rolls and identity pins remain fixed. The additions retain resistance-and-health gear. This does not search mixed seventh-slot assignments or additional gear combinations.

The six linked factors, measured against production-source Health 2.90 and Power 3.72, are **1.5, 1.625, 1.75, 1.875, 2 and 2.5**. Factor 1.5 is the previous diagnostic setting. The finer interval tests the newly discovered strong family, and 2.5 supplies a farther upper probe without assuming that the old family's zero-win result at 2 applies to the upgrades.

Each setting uses the same **32 historical seeds** for every cell. The frozen total is **6 × 228 × 32 = 43,776 fights**, including **7,296 exact baseline replays**. Every input and full report must match the prior screen before stronger settings run. All 1,368 cell/setting combinations are prepared before combat.

## Selection and stopping rule

Choose the lowest tested factor whose strongest seven-Essence team wins **4–16/32**, with **every four-/six-Essence control below 4/32**, including the two level-60 six-Essence controls. Report level-50 and level-60 controls separately. Assess each complete party; do not pool results or select a weaker team to conceal a stronger result.

The intended-team range screens the existing inclusive 10–50% policy. The control requirement is an exploratory progression-separation condition, not a new production restriction or statistical acceptance rule. Complete all six settings, then stop even if no setting qualifies. There is no adaptive interpolation, retry, extra seed draw or automatic confirmation.

These historical-seed measurements cannot establish fresh confirmation, global optimality, acquisition feasibility or production balance acceptance. Any later confirmation needs its own frozen full family, unused seeds and the [existing uncertainty policy](Tower-Balance-Acceptance-Policy.md#evidence-and-acceptance).

## Implementation and verification

Target: the primary game's **offline Balance Harness**. Added:

- [BalanceHarnessExpandedFloor11CalibrationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessExpandedFloor11CalibrationTests.cs): the opt-in native fixture (`LL_EXPANDED_FLOOR11_CALIBRATION`), linked-content isolation checks, and selection tests covering both level controls, strongest-team selection, missing observations and duplicated cells.
- [calibrate-floor11-expanded.py](analysis/calibrate-floor11-expanded.py): an owner that authenticates the prior audited family, freezes the complete grid and source/runtime hashes, and runs through the repository test wrapper with process, time and storage bounds.
- [verify-floor11-expanded.py](analysis/verify-floor11-expanded.py): independent reconstruction of archive integrity, unchanged recipes and gear, content isolation, historical parity, outcomes, paired changes and selection.

The focused regression command passed **32 tests**, with three historical/combat fixtures skipped until explicitly configured. The build had 45 existing warnings and zero errors. Python syntax/CLI and whitespace checks passed. Source helpers used by earlier studies remain unchanged so their existing evidence can still be audited.

The separately owned calibration passed **all ten selected tests**, including its opt-in combat fixture. It completed every one of the **43,776 fights**, with **7,296 exact baseline input/full-report matches**, zero retries and no fresh seeds. Native execution/reconstruction took **1,343.23 seconds**; the owner took **1,348.33 seconds**, with all **eight processes drained**. The sealed study contains **1,088,456,527 bytes**. No required verification command was blocked.

The [independent audit](../TestResults/tower-floor11-expanded-owner-20260928/independent-audit.json) passed on its first run. It authenticated **45,569 files**, verified that only floor-11 Health/Power changes between settings, retained every recipe and identity pin, compared all baseline full reports, and reconstructed every outcome, paired gain/loss, cohort maximum and the no-selection decision. The audit ran no additional fights. The fresh-seed exclusion union remains **834,295**; no team or setting was independently confirmed by this historical-seed screen.

This report and the prior calibration, seventh-Essence report and [search guide](../LL/tools/BalanceHarness/AFFINITY-SEARCH.md) record the expanded-family comparison. Its grid remains closed without choosing a weaker reference or extending the declared run. The completed finer grid above is a separate experiment; it does not change this experiment's frozen design or no-selection result.

The frozen limits are 2 GiB, 2,100 seconds internally and 2,160 seconds for the owning process. The owner must drain every child process. Completed directories cannot be reused; failed evidence is retained without combat retries.

Study manifest: `e8b78df772a42563b1fe966edf6c47bbb4a293467e3a1ca66604999ad86ab3c9`.
Result SHA-256: `d1f3cf71cc5c99d3daceb362121962e3088655dffb38b1f42e7a7fb2a39e9ce5`.

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-floor11-expanded-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessExpandedFloor11CalibrationTests|FullyQualifiedName~BalanceHarnessFloor11CalibrationTests|FullyQualifiedName~BalanceHarnessProgressionUpgradeTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests'
python -B -X utf8 'Balance Harness/analysis/calibrate-floor11-expanded.py' --package 'TestResults/tower-floor11-expanded-owner-20260928' --output 'TestResults/tower-floor11-expanded-20260928' --artifacts 'TestResults/tower-floor11-expanded-build-20260928'
python -B -X utf8 'Balance Harness/analysis/verify-floor11-expanded.py' --study 'TestResults/tower-floor11-expanded-20260928' --owner 'TestResults/tower-floor11-expanded-owner-20260928' --manifest-pin 'e8b78df772a42563b1fe966edf6c47bbb4a293467e3a1ca66604999ad86ab3c9' --receipt 'TestResults/tower-floor11-expanded-owner-20260928/independent-audit.json'
git diff --check
```

Python refers to the bundled runtime. The source study manifest is `c69c0624b45912926bfd3e8bfc02aaa6d1a3f5cf2b8c1ca1ed8e3b4e193f67e8`. Later read-only audits require a new receipt path. Captured studies are local under `TestResults`; a clean checkout does not contain them. This work requires no migrations, production configuration changes, database operations or deployments.
