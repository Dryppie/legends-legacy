# Finer floor-11 calibration with retained equipment — 28 September 2026

Target: the primary game's offline Balance Harness. This follows the independently audited [coarse carried-equipment calibration](Tower-Carried-Equipment-Calibration-20260928.md). Keep the exact **344 recipes**, ordered Essences, equipment, identities, party positions and progression assumptions. The cohorts remain **312 seven-Essence level-60**, **14 six-Essence level-50**, **four six-Essence level-60** and **14 four-Essence level-30** parties. All use their retained Legendary / Masterpiece / rank-5 equipment; the four-Essence parties remain tier-1 stress controls with hypothetical ownership.

## Frozen finer grid

Use the same 32 historical seeds, retaining each recipe's complete archived 256-value seed list for input identity. First replay the entire saved 2× family: **11,008 input hashes and complete reports**. Only after every report matches may the finer variants run. Prepare all **2,064 variant/recipe combinations** with combat disabled before starting fights.

| Variant | Multiplier of locally applied floor-11 setting | Health | Power |
| --- | ---: | ---: | ---: |
| 0, baseline parity | 2× | 13.05 | 16.74 |
| 1 | 2.25× | 14.68125 | 18.8325 |
| 2 | 2.5× | 16.3125 | 20.925 |
| 3 | 2.75× | 17.94375 | 23.0175 |
| 4 | 3× | 19.575 | 25.11 |
| 5 | 3.5× | 22.8375 | 29.295 |

Run all six settings and all 344 combinations at 32 seeds each: **66,048 fights**, including baseline parity. Change only floor-11 Health/Power in isolated content copies. Do not change live content, family membership, gear, Essence order, the repeating equipment curve or the supported search. Capture a newly built current runtime; no historical game DLL substitution is used.

Select the lowest declared multiplier whose strongest intended combination wins **4–16/32** and whose **every lower-Essence control wins fewer than 4/32**. Keep the separate level-50 and level-60 controls. Save all outcomes, draws, paired gains/losses, duration and remaining-health means. This is historical selection, not balance acceptance; do not pool teams or historical stages as independent estimates. A qualifying setting must receive a separately frozen fresh-seed confirmation of the full family before any application.

Bounds: **2,700 seconds** for native preparation/combat/reconstruction, **2,760 seconds** for the owned process, and **3 GiB** output. Builds, ordinary tests and the independent read-only audit are separate. Run no additional settings, retries, resampling or adaptive extension. Retain evidence on failure. Allocate no fresh seeds in this study; the existing exclusion union is **835,063**.

Source manifest: `4a9f8b8d2cca36888f3fb79e44a6225c54c30733f1e580510a6a7deffd0de6f3`.
Expected live floor-file SHA-256: `dab5fe4db2f92af1e0441418ac63856f5ee75af8f26e19af258703f929020124`.
Live floor 10 stays **12.71 / 7.13** and floor 11 stays **6.525 / 8.37** during the study.

## Implementation

- [BalanceHarnessRefinedCarriedCalibrationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessRefinedCarriedCalibrationTests.cs): opt-in fixed grid (`LL_REFINED_CARRIED_CALIBRATION`), full-family decision checks, isolated two-field edits, baseline parity and native reconstruction.
- [calibrate-carried-refined.py](analysis/calibrate-carried-refined.py): audited import, current/source/runtime pins, frozen declaration and bounded process owner.
- [verify-carried-refined.py](analysis/verify-carried-refined.py): independent full-family/schedule/report reconstruction and selection verification.

The protocol above was written before combat. All six settings completed with the frozen family, source code and schedule unchanged.

## Results

All **66,048 scheduled historical fights** completed. The selected candidate is **2.75×**, setting isolated floor-11 Health to **17.94375** and Power to **23.0175**. It is the lowest eligible grid point. These results select a candidate; they do not establish fresh-sample balance acceptance.

| Multiplier | Best seven / level-60 wins | Best six / level-50 wins | Best six / level-60 wins | Best four wins | Intended cells above 16/32 | Eligible |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| 2× | 32 | 15 | 18 | 0 | 126 | No |
| 2.25× | 28 | 3 | 3 | 0 | 26 | No |
| 2.5× | 19 | 1 | 1 | 0 | 3 | No |
| **2.75×** | **9** | **0** | **0** | **0** | **0** | **Yes, selected** |
| 3× | 6 | 0 | 0 | 0 | 0 | Yes |
| 3.5× | 1 | 0 | 0 | 0 | 0 | No |

Every entry is the strongest individual combination within its cohort, out of the same 32 historical seeds. Teams and shared seeds are not pooled as independent estimates. The 2× baseline retains its two draws; no other setting has a draw. All outcomes remain in the archive.

At 2.75×, the two leaders both win **9/32 (28.125%)**: the repeated resistance-and-health parent upgraded with **Gnoll Shaman**, and the repeated armor-and-health parent upgraded with **Shadow Imp**. Their exact IDs are `floor11-six-1/add/essence.gnoll_shaman` and `floor11-six-1/armor/add/essence.shadow_imp`. Nine intended combinations fall in the descriptive 4–16/32 screening band. All 32 lower-Essence controls, including all four level-matched six-Essence controls, win zero. The separate fresh study retains every recipe, including the other intended teams.

The armor Shadow Imp route newly added in the coarse calibration is therefore material to coverage. At 2.5× the strongest team is the alternating armor Shadow Imp upgrade at 19/32; selecting a weaker recipe would hide that setting's failure. At 3×, the alternating armor Gnoll Shaman upgrade leads at 6/32. The frozen lowest-eligible rule preserves the 2.75× choice.

Full evidence: [2,064 result rows and six summaries](../TestResults/tower-carried-refined-20260928/result.json), [344 unchanged recipes](../TestResults/tower-carried-refined-20260928/cells.json), [runtime qualification](../TestResults/tower-carried-refined-20260928/runtime-qualification.json), and [preflight](../TestResults/tower-carried-refined-20260928/preflight.json). All **11,008 baseline inputs and complete reports** matched before any finer-setting combat. All **2,064 combinations** were prepared with combat disabled.

## Verification

**50 focused tests passed**, with four opt-in studies skipped. The bounded calibration then passed **10 tests**, including its enabled scientific fixture. Backend execution used `build/run-tests.ps1`. The preparation build reported **45 existing warnings and zero errors**. Its [build/test log](../TestResults/tower-carried-refined-preparation-20260928/build-and-tests.log), [focused TRX](../TestResults/tower-carried-refined-preparation-20260928/focused-tests.trx), [calibration log](../TestResults/tower-carried-refined-owner-20260928/calibration.log) and [calibration TRX](../TestResults/tower-carried-refined-owner-20260928/calibration-tests.trx) preserve the evidence.

Native preparation, combat and reconstruction took **1,778.44 seconds**; the owner took **1,784.11 seconds** and drained all **eight processes**. No fight was retried. The [declaration](../TestResults/tower-carried-refined-owner-20260928/declaration.json), [process receipt](../TestResults/tower-carried-refined-owner-20260928/process.json) and [completion receipt](../TestResults/tower-carried-refined-owner-20260928/completion.json) bind the limits, source hashes and outputs.

The [independent audit](../TestResults/tower-carried-refined-owner-20260928/independent-audit.json) passed on its first run. It authenticated **68,538 files**, reconstructed every report, paired count and decision, preserved all 344 imported recipes, and independently matched all **11,008 baseline inputs and full reports**. The archive occupies **1,672,828,949 bytes**, within 3 GiB. The audit ran no combat and allocated no seeds.

Final syntax/CLI, documentation-link and whitespace checks passed, including `git -c core.safecrlf=false diff --check`. All frozen source/input hashes and the live floor-file hash remain unchanged. No required calibration command remains blocked or unrun. Unrelated equipment-migration and LiveOps work was preserved.

Archive manifest: `5da2cb8fe7af95cd8b3d3f218ca516d45de1c510fcf9216c5177c06f45044151`.
Result SHA-256: `0cf27af0c0cb2d8234ab6deda0c6a6780b61d0f77824a94eba844592efb84340`.

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-carried-refined-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessRefinedCarriedCalibrationTests|FullyQualifiedName~BalanceHarnessCarriedCalibrationTests|FullyQualifiedName~BalanceHarnessCarryForwardEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionUpgradeTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~WorldTowerTests'
# Historical execution record: completed directories cannot be reused or resumed.
python -B -X utf8 'Balance Harness/analysis/calibrate-carried-refined.py' --package 'TestResults/tower-carried-refined-owner-20260928' --output 'TestResults/tower-carried-refined-20260928' --artifacts 'TestResults/tower-carried-refined-build-20260928'
python -B -X utf8 'Balance Harness/analysis/verify-carried-refined.py' --study 'TestResults/tower-carried-refined-20260928' --owner 'TestResults/tower-carried-refined-owner-20260928' --manifest-pin '5da2cb8fe7af95cd8b3d3f218ca516d45de1c510fcf9216c5177c06f45044151' --receipt 'TestResults/tower-carried-refined-owner-20260928/independent-audit.json'
```

Python denotes the bundled runtime. These ignored local archives are absent from a clean checkout. A later read-only audit needs a new receipt path and runs no combat.

## Follow-up and scope

The separately frozen [complete-family fresh confirmation](Tower-Carried-Equipment-Confirmation-20260928.md) subsequently completed and independently passed at exactly **2.75×**. It replayed **11,008 qualification fights**, then ran **88,064 fresh fights** over all 344 recipes and 256 unused seeds. The strongest intended team won **93/256 (36.33%)**, with adjusted bounds **25.92–48.19%**; the strongest lower-Essence control won **1/256**, upper bound **6.05%**. Seven intended combinations establish 10% viability. No historical wins were pooled into that verdict. The subsequent [checked local application](Tower-Carried-Equipment-Checked-Application-20260928.md) integrates **Health 17.94375 / Power 23.0175**, matching all **88,064 confirmed inputs** and **470 complete reports**, with independent audit. It preserves the complete family, requested curve and supported search. The live-value statements below describe this earlier calibration before that application.

This calibration changes no live boss value, repeating equipment curve, supported search policy, dependency, configuration or migration. Floor 10 remains **12.71 / 7.13** and floor 11 **6.525 / 8.37**, with the original live floor-file hash preserved. There is no deployment, database operation or service restart. The calibration allocates zero fresh seeds; its exclusion history remains **835,063**. New files are the finer calibration fixture, owner, auditor and this report; follow-up links are maintained in the coarse report, equipment baseline and supported-search guide.
