# Floor-11 calibration with retained equipment — 28 September 2026

**Completed and independently verified: no setting in the four-point grid qualifies.** At **2×**, the strongest seven-Essence party still wins **32/32**, while a level-60 six-Essence control wins **18/32**. At **4×**, the strongest intended party wins only **1/32**, and all controls win zero. At **8×**, every combination wins zero. Recommend one finer bounded comparison between 2× and 4×, retaining the complete family; no setting is selected or applied here.

Target: the primary game's offline Balance Harness. The [carried-equipment screen](Tower-Carried-Equipment-Screen-20260928.md) found 32/32 wins for every tested combination, including the six-Essence parties retained from floor 10. This bounded calibration tests four declared Health/Power settings with retained Legendary / Masterpiece / rank-5 equipment. The requested repeating ten-floor reference curve and supported search remain unchanged.

## Frozen protocol

Retain all **228** carried-equipment recipes, in their archived order. Append **116** controlled upgrades using the two six-Essence, level-50 armor-and-health parents: for each parent, one level-60 six-Essence control and all **57 legal uniform seventh-Essence additions** at level 60. Use the existing progression helper to preserve equipment, original six Essences, character/item/Essence identities and party positions. An addition is appended to each member; existing Essences are not reordered. Parent recipes are not mutated.

This produces **344 combinations**: **312 seven-Essence level-60 intended parties**, **14 six-Essence level-50 controls**, **four six-Essence level-60 controls**, and **14 four-Essence level-30 controls**. Every party has ten members. The first three cohorts retain tier 2; the four-Essence controls retain tier 1. Baseline rolls, no styles, and level-1 unascended/unevolved Essences remain as captured. Equipment ownership remains hypothetical. The four-Essence parties are stress controls, not demonstrated floor-10 progression paths. The 14 six-Essence level-50 recipes retain the previous audit's exact first-ten-member links to saved floor-10 parties.

Use the same **32 historical seeds** as the carried-equipment screen. Preserve the complete archived 256-seed list in every recipe for input identity, but execute only the frozen 32-value panel. New armor upgrades inherit that same list. No fresh seeds, search, resampling, adaptive pruning, retries, extension, automatic confirmation or content application are permitted.

| Variant | Multiplier of current floor-11 setting | Health | Power |
| --- | ---: | ---: | ---: |
| 0 | 1× | 6.525 | 8.37 |
| 1 | 2× | 13.05 | 16.74 |
| 2 | 4× | 26.10 | 33.48 |
| 3 | 8× | 52.20 | 66.96 |

These are coarse geometric brackets, not proposed production values. Change only floor-11 `guardianScaling.health` and `guardianScaling.offense` in isolated copied content. Keep all other floors, guardian rules, rewards, defense, resistance and settings unchanged. Floor 10 remains at its locally applied **12.71 / 7.13** setting.

Prepare all **1,376 variant/recipe combinations** with combat disabled, and match all **7,296 imported baseline input hashes**. Execute variant 0 in cell/seed order, first reproducing every complete saved report for the 228 imported cells. Only after all 7,296 match may the 116 new baseline armor upgrades execute. Then complete variants 1–3 over the entire 344-cell family, regardless of intermediate outcomes. Total **44,032 fights**, including those historical parity replays; **11,008 per setting**.

Record wins, draws, remaining guardian health, duration and paired gains/losses against variant 0 for every combination. Preserve all results. A diagnostic candidate is the lowest declared multiplier whose strongest intended party wins **4–16/32**, with **every lower-Essence control below 4/32**, including all four level-60 six-Essence controls. This retains the descriptive 10–50% target; it is not a statistical acceptance gate. A coarse grid with no candidate cannot establish that no intermediate value works. Historical selection requires later fresh confirmation, with a new exclusion-aware schedule.

Limits: **1,800 seconds** for the native variant preparation/combat/reconstruction stage, **1,860 seconds** for the owned test process, and **2 GiB** output. Builds, ordinary regression checks and the independent read-only audit are separate. The owner pins current and captured content, source code and executable hashes; it launches through `build/run-tests.ps1` and drains the process tree. No historical DLL substitution is used. Failed evidence remains intact, without combat retry or resume. This study stays outside the fresh-seed registry; the recorded exclusion union is **835,063**.

## Implementation

- [BalanceHarnessCarriedCalibrationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessCarriedCalibrationTests.cs): opt-in fixed calibration (`LL_CARRIED_CALIBRATION`), full-family decision checks, copied-content boundaries, legal armor upgrades with identity preservation, baseline replay gate and native archive reconstruction.
- [calibrate-carried-equipment.py](analysis/calibrate-carried-equipment.py): audited source import, current-content/executable validation, frozen declaration and bounded process ownership.
- [verify-carried-calibration.py](analysis/verify-carried-calibration.py): independent reconstruction of all 116 armor upgrades, complete schedule, reports, paired observations, variant edits and decision.

Source manifest: `76e028152ed3ccfb09e5e3bf60ff5ae3acabcb0a8f6db18a395806ed19f256f1`.
Expected live floor-file SHA-256: `dab5fe4db2f92af1e0441418ac63856f5ee75af8f26e19af258703f929020124`.

The protocol above was written before combat. All four settings completed without modifying the frozen fixture, owner, auditor, family or schedule.

## Results

All **44,032 scheduled fights** completed. The decision is **`NoSeparatingSettingInGrid`**, with `selected: null`. Each entry below is the strongest individual combination in that cohort, out of 32 historical seeds. These are descriptive maxima; neither the teams nor their shared seed observations are pooled as independent estimates of one team's strength.

| Multiplier | Best seven-Essence / level-60 wins | Best six-Essence / level-50 wins | Best six-Essence / level-60 wins | Best four-Essence wins | Intended combinations above 16/32 | Lower-Essence controls at least 4/32 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1× | 32 | 32 | 32 | 32 | 312 | 32 |
| 2× | 32 | 15 | 18 | 0 | 126 | 15 |
| 4× | 1 | 0 | 0 | 0 | 0 | 0 |
| 8× | 0 | 0 | 0 | 0 | 0 | 0 |

At 1×, **all 344 combinations** win 32/32, including all newly added armor upgrades. At 2×, 90 intended combinations fall in the descriptive 4–16/32 band, but stronger intended parties and 15 lower-Essence controls still violate the rule. Selecting only those 90 would conceal the failed family-wide criteria. Two fights at 2× end in draws; every draw remains recorded. No other setting has a draw.

The strongest six-Essence level-60 control at 2× is the repeated resistance-and-health party, **18/32**, compared with **15/32** for its unchanged level-50 parent. The armor-and-health repeated parent rises from **4/32** at level 50 to **8/32** at level 60; the alternating armor parent remains **3/32** at both levels. Both armor parents upgraded with Shadow Imp win **30/32**. Those comparisons preserve the gear and existing instance identities. They support retaining the separate level controls, rather than attributing every improvement to a seventh Essence.

The **only victory at 4×** comes from the newly included alternating armor-and-health parent upgraded with **Gnoll Shaman** (`floor11-six-2/armor/add/essence.gnoll_shaman`). Its 1/32 result is below the screening viability threshold. It is neither a selected team nor evidence of a reliably viable build. The original 228-cell family alone would have reported no wins at this setting, so the added progression route materially improves coverage.

The complete [result](../TestResults/tower-carried-calibration-20260928/result.json) retains **1,376 rows**, all four summaries, paired gains/losses, guardian-health means and durations. The [344 exact recipes](../TestResults/tower-carried-calibration-20260928/cells.json) preserve every team and ordered Essence list. These historical-seed outcomes allocate no fresh statistical samples and establish no universal Essence-count requirement, acquisition path or search-algorithm improvement.

## Verification

The [independent audit](../TestResults/tower-carried-calibration-owner-20260928/independent-audit.json) passed on its first run. It authenticated **45,706 files**, independently reconstructed all **116 armor upgrades**, verified the unchanged 228 imported recipes, inspected all **44,032 reports** and reproduced every paired count and decision. All **7,296 imported baseline inputs and complete reports** matched the source. Native verification also reconstructed the input and cache identities across every variant. The audit ran zero fights.

Native execution took **1,106.56 seconds**. The owner completed in **1,113.72 seconds**, draining all **eight processes**. The sealed archive occupies **1,104,905,097 bytes**, within the 2-GiB limit. The [declaration](../TestResults/tower-carried-calibration-owner-20260928/declaration.json), [process receipt](../TestResults/tower-carried-calibration-owner-20260928/process.json) and [completion receipt](../TestResults/tower-carried-calibration-owner-20260928/completion.json) retain the limits, pins and accounting. No fight was retried or added after observing results.

**41 focused tests passed**, with three opt-in scientific fixtures skipped. The owned calibration then passed **11 tests**, including its enabled scientific fixture. All backend execution used `build/run-tests.ps1`. The final build had **16 existing warnings and zero errors**. The initial sandboxed restore could not access the existing NuGet configuration; an authorized run resolved that restriction. Two preliminary unit-test runs exposed an incorrect assumption that the generic authored test party had the same legal-addition count as the historical study parents. The final unit test checks generic identity/equipment preservation, while the scientific preflight separately requires exactly 57 additions for each real parent. This was corrected before protocol freezing and before combat; the actual study family stayed unchanged.

Test evidence is retained in [final-build-and-tests.log](../TestResults/tower-carried-calibration-preparation-20260928/final-build-and-tests.log), [focused-tests.trx](../TestResults/tower-carried-calibration-preparation-20260928/focused-tests.trx), [calibration.log](../TestResults/tower-carried-calibration-owner-20260928/calibration.log) and [calibration-tests.trx](../TestResults/tower-carried-calibration-owner-20260928/calibration-tests.trx). Earlier unsuccessful setup logs are preserved in the preparation directory. Python syntax/CLI, **77 local documentation links**, seven-file whitespace and `git -c core.safecrlf=false diff --check` passed. Final checks verified all frozen source/input hashes and the unchanged live floor-file hash. No required command remains blocked or unrun. Unrelated equipment-migration and LiveOps work is preserved.

Archive manifest: `4a9f8b8d2cca36888f3fb79e44a6225c54c30733f1e580510a6a7deffd0de6f3`.
Result SHA-256: `f10d7e9d0615645f22afaf63e5d24fa101f74a884c29993c3dcf8e7b16d8e9c0`.

Commands executed from the repository root (Python denotes the bundled runtime):

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-carried-calibration-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessCarriedCalibrationTests|FullyQualifiedName~BalanceHarnessCarryForwardEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionUpgradeTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~WorldTowerTests'
# Historical execution record; the completed directories cannot be reused or resumed.
python -B -X utf8 'Balance Harness/analysis/calibrate-carried-equipment.py' --package 'TestResults/tower-carried-calibration-owner-20260928' --output 'TestResults/tower-carried-calibration-20260928' --artifacts 'TestResults/tower-carried-calibration-build-20260928'
python -B -X utf8 'Balance Harness/analysis/verify-carried-calibration.py' --study 'TestResults/tower-carried-calibration-20260928' --owner 'TestResults/tower-carried-calibration-owner-20260928' --manifest-pin '4a9f8b8d2cca36888f3fb79e44a6225c54c30733f1e580510a6a7deffd0de6f3' --receipt 'TestResults/tower-carried-calibration-owner-20260928/independent-audit.json'
git -c core.safecrlf=false diff --check
```

Build output and archives remain local under ignored `TestResults`, absent from a clean checkout. Further read-only audits need a new receipt path. The earlier sealed studies, their source scripts and their fixtures remain intact.

## Recommendation and scope

**Completed follow-up:** the [finer carried-equipment grid](Tower-Carried-Equipment-Refined-Calibration-20260928.md) ran and independently verified **66,048 fights** across all 344 recipes at 2×, 2.25×, 2.5×, 2.75×, 3× and 3.5×. It selected **2.75×: Health 17.94375 / Power 23.0175**. The strongest intended teams won **9/32** and every lower-Essence control **0/32**. The [separate complete-family fresh confirmation](Tower-Carried-Equipment-Confirmation-20260928.md) subsequently independently passed across 256 unused seeds and all 344 recipes: strongest intended **93/256**, strongest control **1/256**. Neither study changes live content; guarded local integration is next. The original recommendation below records the coarse study's next step.

**Use one finer, separately bounded grid between 2× and 4× before deciding whether scalar calibration can meet the target.** A proposed set of interior points is **2.25×, 2.5×, 2.75×, 3× and 3.5×**; these are future experimental choices, not tested or approved content values. Retain all 344 combinations and the separate six-Essence level controls. A setting must satisfy the entire-family rule and then receive fresh confirmation before application. No interpolation or assumed monotonic win rate can turn this coarse screen into acceptance.

The present result narrows the next useful interval; it does not show that every intermediate setting fails. If the finer pass still cannot keep intended parties viable while limiting the matched lower-Essence parties, review encounter mechanics or the checkpoint requirement instead of continuing indefinite scaling sweeps. The user-requested repeating equipment curve and supported search should remain in place while that progression question is resolved.

Changed files: the new calibration fixture, owner, auditor and this report, plus follow-up links/results in the carried-equipment screen, equipment baseline and supported-search guide. The study changes no live boss value, equipment rule, search policy, dependency, configuration or migration. There is no deployment, database operation or service restart. The previously applied floor-10 **12.71 / 7.13** and floor-11 **6.525 / 8.37** values remain in the working tree, with the same live floor-file hash recorded above. Fresh-seed allocation remains zero; the recorded exclusion union is **835,063**.
