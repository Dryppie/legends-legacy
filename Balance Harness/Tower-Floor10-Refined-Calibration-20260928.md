# Floor-10 finer Health/Power calibration — 28 September 2026

**Selected for fresh confirmation: factor 7.75, Health 12.71 / Power 7.13.** The fixed **6,048-fight** screen completed and independently audited as `DiagnosticCalibrationCandidate`. The strongest combination, the alternating retained party with armor-and-health gear, won **7/32 (21.88%)**. The repeated party with the same gear won **5/32 (15.63%)**. All other 19 combinations won zero.

**Completed follow-up:** the [separate fresh confirmation](Tower-Floor10-Family-Confirmation-20260928.md) retained all 21 combinations and passed after **5,376 fights / 256 fresh seeds**. The alternating armor-and-health party won **55/256 (21.48%)**, with adjusted bounds of **14.75–30.20%**; the repeated party won **23/256 (8.98%)**, and the other 19 combinations won zero. The independent audit confirmed the result. The [checked local application](Tower-Floor10-Checked-Application-20260928.md) has now applied the exact setting and matched all 5,376 inputs plus 23 representative complete reports on the current build. This historical screen remains separate selection evidence. Only one combination establishes 10% viability, and no lower-Essence separation is established. Next, assess gear carried from floor 10 to 11 while retaining the supported search.

## Results

Every rate below is per complete party, with 32 historical seeds. The strongest party is used for the ceiling; outcomes are never pooled across combinations.

| Factor | Health | Power | Strongest wins / 32 | Cells above 50% | Cells with any win |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 6 | 9.840 | 5.520 | 32 | 10 | 15 |
| 6.25 | 10.250 | 5.750 | 32 | 6 | 13 |
| 6.5 | 10.660 | 5.980 | 32 | 2 | 9 |
| 6.75 | 11.070 | 6.210 | 32 | 2 | 4 |
| 7 | 11.480 | 6.440 | 32 | 2 | 2 |
| 7.25 | 11.890 | 6.670 | 31 | 2 | 2 |
| 7.5 | 12.300 | 6.900 | 17 | 1 | 2 |
| **7.75** | **12.710** | **7.130** | **7** | **0** | **2** |
| 8 | 13.120 | 7.360 | 0 | 0 | 0 |

The 7.5 setting does not qualify: the alternating armor-and-health party wins **17/32 (53.13%)**, above the observed ceiling, even though its repeated counterpart wins **13/32 (40.63%)**. Selecting that weaker result would hide the breach. At 7.75 both surviving combinations lie inside the observed band, making it the sole eligible grid point and therefore the lowest eligible factor under the frozen rule. No true-rate bound or formal balance acceptance follows from these selection samples.

There were no draws anywhere in the grid. At 7.75, all-outcome mean duration is **74.47 seconds** for the repeated armor-and-health party and **72.97 seconds** for the alternating party; no pacing threshold has been approved. Relative to factor 6, these parties lose 27 and 25 previously winning seeds, respectively, gaining none. Full counts, paired changes, duration and guardian-health means are in [result.json](../TestResults/tower-floor10-refined-20260928/result.json); exact ordered recipes remain in [cells.json](../TestResults/tower-floor10-refined-20260928/cells.json).

## Fixed design

Target: the primary game's offline Balance Harness. The [completed broad grid](Tower-Floor10-Linked-Calibration-20260928.md) found 32/32 wins for all three armor-and-health parties at 6× and zero wins for every tested combination at 8×. This separate experiment tests **6, 6.25, 6.5, 6.75, 7, 7.25, 7.5, 7.75 and 8×**, relative to the unchanged local floor-10 Health **1.64** / Power **0.92**. Each variant changes those two values together in isolated content copies.

The entire **21-cell family** is retained: authored, repeated and alternating six-Essence parties, each with all seven gear choices. Parties contain 15 level-50, tier-2 characters with **Legendary / Masterpiece / rank-5 gear**, baseline rolls, no styles, and level-1 unascended/unevolved Essences. Full recipes, actor/item identity pins, party positions and ordered Essences are preserved. Ownership remains hypothetical. The family contains no lower-Essence cohort and therefore cannot establish that six Essences are necessary.

Each cell receives the same **32 historical seeds** used in the broad grid. The fixed total is **9 × 21 × 32 = 6,048 fights**, including **1,344 boundary replays**. All **189 setting/recipe combinations** must prepare before any combat; both boundary input panels must also match their archived hashes. Execution then completes factor 6, factor 8, and only afterward the seven ascending intermediate factors. Every boundary report must match the complete archived report before any intermediate combat begins. The attempt journal records setting, cell and seed so the independent audit can reconstruct this order.

The selection rule is unchanged: choose the **lowest tested factor whose strongest retained combination wins 4–16/32**. Every cell must remain at or below 50%, and at least one must reach 10% observed viability. These historical observations only screen the inclusive 10–50% target; they do not establish uncertainty bounds, fresh confirmation, or a universal ceiling. Every declared setting runs, without adaptive extension, extra seeds, retries, weaker-team substitution or automatic confirmation/application. Paired gains and losses use factor 6 as their reference. Draws count as nonwins.

The source broad-grid manifest is `c67912fac6fa2f72b82676859c9f2cb525585d0cd6200f604b86d808c432bcf1`, authenticated with its independent audit. Current content must match all 29 captured files. The applied floor-11 values remain present in every copy. The run uses newly built current game assemblies, captures their execution identity and executable files, and qualifies them against both complete boundary panels. Settings remain attributes 18, equipment release 4, `healing-v1`, the captured threat rules and 10 ticks per checkpoint.

The limits are **840 seconds internally**, **900 seconds for the process owner**, and **2 GiB** of output. The owner freezes request, code and executable hashes before combat and drains its entire process tree. Preparation, all diagnostic battles and native archive reconstruction are inside the native budget. Build/regression work and the read-only Python audit are separate. The archive retains incomplete evidence on failure and cannot be resumed or overwritten.

Execution and native reconstruction took **248.13 seconds**. The owner completed in **250.42 seconds** and drained all **eight processes**. All 6,048 starts completed; there were no retries, fresh seeds, new searches or newly confirmed teams. The latest scientific exclusion union remains **834,807**.

The [independent audit](../TestResults/tower-floor10-refined-owner-20260928/independent-audit.json) passed on its first run. It authenticated **6,854 files / 236,505,214 bytes**, reconstructed all outcomes and the exact execution order, verified unchanged recipes and isolated content edits, reproduced all **1,344 boundary input/full-report comparisons**, and independently selected factor 7.75. It ran zero additional fights.

## Implementation

- [BalanceHarnessRefinedFloor10CalibrationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessRefinedFloor10CalibrationTests.cs): separate opt-in fixture (`LL_REFINED_FLOOR10_CALIBRATION`), complete-family validation, both boundary gates, per-attempt accounting, isolated content and full native reconstruction. Tests cover the inclusive strongest-team rule, missing/duplicate/invalid observations, content isolation, and rejection of intermediate execution with incomplete boundary evidence.
- [calibrate-floor10-refined.py](analysis/calibrate-floor10-refined.py): pinned broad-grid import and bounded owner.
- [verify-floor10-refined.py](analysis/verify-floor10-refined.py): independent authentication of the full inventory, all recipes and outcomes, boundary report parity, execution order, means, paired changes and final selection.

The earlier calibration fixtures, scripts and study archives remain unchanged. This work includes no new gameplay values, appsettings changes, migrations, dependencies, deployments, service restarts or database operations. The repeating equipment curve and supported search remain inputs to the calibration. Unrelated existing equipment-migration work is preserved.

The new fixture, owner, auditor and this report are the implementation additions. Updated the broad-grid report, equipment-baseline follow-up and `AFFINITY-SEARCH.md` to point to the completed candidate screen and the need for fresh confirmation.

## Verification and reproduction

**70 focused backend checks passed**, with three scientific fixtures skipped until configured. After finalizing the detailed attempt journal, the incremental build and all **13 ordinary finer-grid checks** passed. The separately owned run then passed all **14 selected tests**, including the scientific fixture. Tests used `build/run-tests.ps1`. The full build reported **45 existing warnings and zero errors**; the incremental build reported **16 existing warnings and zero errors**. Python syntax/CLI, documentation links and whitespace checks passed. No required verification command remains blocked or unrun.

Study manifest: `b866bb74e5aa897dcd241423208fe53a47bdf5a583846434a07c460f6f6a015f`.
Result SHA-256: `f91253773ee50d102c548030ba50542101d07cc7ee0cbeb5754943286a0a665b`.

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-floor10-refined-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessRefinedFloor10CalibrationTests|FullyQualifiedName~BalanceHarnessFloor10CalibrationTests|FullyQualifiedName~BalanceHarnessFloor11CalibrationTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~WorldTowerTests'
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-floor10-refined-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessRefinedFloor10CalibrationTests'
python -B -X utf8 'Balance Harness/analysis/calibrate-floor10-refined.py' --package 'TestResults/tower-floor10-refined-owner-20260928' --output 'TestResults/tower-floor10-refined-20260928' --artifacts 'TestResults/tower-floor10-refined-build-20260928'
python -B -X utf8 'Balance Harness/analysis/verify-floor10-refined.py' --study 'TestResults/tower-floor10-refined-20260928' --owner 'TestResults/tower-floor10-refined-owner-20260928' --manifest-pin 'b866bb74e5aa897dcd241423208fe53a47bdf5a583846434a07c460f6f6a015f' --receipt 'TestResults/tower-floor10-refined-owner-20260928/independent-audit.json'
git -c core.safecrlf=false diff --check
```

Python denotes the bundled runtime. The commands document the completed run: its output paths cannot be reused, and later read-only audits require a new receipt path. Captured evidence and executables remain local under ignored `TestResults`, absent from a clean checkout. No fresh confirmation or content application is launched by this report.
