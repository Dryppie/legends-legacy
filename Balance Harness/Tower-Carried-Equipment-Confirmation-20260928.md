# Fresh confirmation of carried-equipment floor 11 — 28 September 2026

Target: the primary game's offline Balance Harness. This protocol is prepared while the [finer carried-equipment calibration](Tower-Carried-Equipment-Refined-Calibration-20260928.md) runs. Execute it only if that completed, independently audited calibration selects an eligible setting. Use its lowest eligible multiplier exactly once. An ineligible or missing candidate stops this follow-up before allocation.

## Frozen confirmation

Preserve all **344 recipes**: **312 seven-Essence level-60**, **14 six-Essence level-50**, **four six-Essence level-60** and **14 four-Essence level-30** parties. Keep their exact equipment, identities, positions and Essence order. Legendary / Masterpiece / rank-5 equipment remains carried from floor 10. The four-Essence parties are hypothetical ownership stress controls, not proven progression paths.

Build a separate current runtime. Before allocating fresh seeds, replay all **11,008 historical fights** at the selected setting, matching every input and complete report to the finer calibration. Native reconstruction and independent audit must preserve these qualification results. Historical wins are excluded from confirmation estimates.

After runtime qualification, reserve one **256-seed** panel under `tower-floor11-carried-confirmation-v1/block-1`, master **2026092817**, using the existing durable SHA-256 allocator. Refresh the complete reservation registry, including recoveries, and exclude all **835,063** historical values. The floor-10 family confirmation supplies the latest audited exclusion union; its balance result is separate from this floor-11 assessment. Do not preview or replace the new panel.

Run every recipe on the complete shared panel: **88,064 fresh fights**, plus **11,008 qualification fights**, for **99,072 total**. Keep failures and draws. No historical pooling, recipe removal, alternate multiplier, extension, retries, deployment or live content application.

Use approximate simultaneous 95% Bonferroni-Wilson intervals over all **344** cells:

- **Pass:** every intended upper bound is at most 50%, at least one intended lower bound is at least 10%, and every lower-Essence control upper bound is below 10%.
- **Fail:** any intended observed rate exceeds 50%, any control observed rate reaches 10%, or every intended upper bound is below 10%.
- **Inconclusive:** neither condition is met.

These are per-study bounds for this fixed family, not coverage across all earlier studies or unsearched recipes. Retain all level-matched controls and compare each cell separately. The shared seed panel does not make teams independent; Bonferroni coverage does not require that independence.

Bounds: **4,200 seconds** native, **4,260 seconds** owned process, **4 GiB** output. Builds, ordinary tests and independent read-only audit are separate. Qualification must finish before fresh reservation; a later failure preserves any reservations and completed evidence. The exclusion union becomes **835,319** if all 256 fresh values are reserved.

Live content is pinned to the finer calibration's current-content snapshot. Floor 10 remains **12.71 / 7.13** and floor 11 **6.525 / 8.37** during this study. Selected scaling exists only in isolated content copies. No migration or application configuration change is required.

## Implementation

- [BalanceHarnessCarriedConfirmationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessCarriedConfirmationTests.cs): opt-in fixture (`LL_CARRIED_CONFIRMATION`), lowest-candidate guard, current-runtime qualification, full-family confirmation and simultaneous assessment.
- [confirm-carried-equipment.py](analysis/confirm-carried-equipment.py): audited-source admission, frozen input/runtime pins, declaration and bounded process owner.
- [verify-carried-confirmation.py](analysis/verify-carried-confirmation.py): independent schedule, allocation, full-report parity, fresh outcomes and interval reconstruction.

The protocol above was written before fresh-seed allocation. The completed finer calibration independently verified **66,048 fights** and selected **2.75×: Health 17.94375 / Power 23.0175**. Its strongest intended teams won **9/32** and all 32 lower-Essence controls **0/32**. Its archive manifest is `5da2cb8fe7af95cd8b3d3f218ca516d45de1c510fcf9216c5177c06f45044151`; result SHA-256 is `0cf27af0c0cb2d8234ab6deda0c6a6780b61d0f77824a94eba844592efb84340`. The [confirmation declaration](../TestResults/tower-carried-confirmation-owner-20260928/declaration.json) freezes this exact candidate and the source/runtime hashes before its first qualification replay. Execution results and verification are recorded below.

## Preparation verification

The separate current build passed **33 focused tests**, with four opt-in studies skipped, using `build/run-tests.ps1`. It reported **44 existing warnings and zero errors**. The first build was blocked by a namespace-resolution error in concurrent LiveOps support-case work; that file was corrected in the shared workspace before the successful build. This study made no edits to that unrelated code. All five game assemblies match between the new harness and test output directories. No captured historical DLLs were substituted.

Evidence: [successful build/test log](../TestResults/tower-carried-confirmation-preparation-20260928/final-build-and-tests.log), [focused TRX](../TestResults/tower-carried-confirmation-preparation-20260928/focused-tests.trx), and [initial blocked build log](../TestResults/tower-carried-confirmation-preparation-20260928/build-and-tests.log). Both Python entry points passed syntax and CLI checks; the confirmation protocol's links and the new files' whitespace passed inspection before launch.

The native [runtime-qualification receipt](../TestResults/balance/tower-carried-confirmation-20260928/runtime-qualification.json) confirms **11,008 matched inputs and full reports**, reconstructed before any fresh reservation. The [durable reservation](../TestResults/balance/tower-carried-confirmation-20260928/reservation.json) completed with **256 candidates, zero rejections and 256 accepted fresh values**. The exclusion union is now **835,319**; those values remain reserved regardless of the confirmation verdict. All declared combat completed under the original bounds.


## Confirmation results

The native and independently audited assessment is **Pass** at **2.75×: Health 17.94375 / Power 23.0175**. All **88,064 fresh fights** and **11,008 qualification replays** finished: **99,072 total**, without retries or extensions. All rows use exactly 256 fresh samples; none includes a historical win. No fresh fight ended in a draw.

| Cohort | Combinations | Strongest wins / 256 | Observed rate | Adjusted bounds for strongest cell |
| --- | ---: | ---: | ---: | --- |
| Seven Essences, level 60 | 312 | 93 | 36.33% | 25.92–48.19% |
| Six Essences, level 50 | 14 | 1 | 0.39% | 0.024–6.05% |
| Six Essences, level 60 | 4 | 0 | 0% | 0–5.34% |
| Four Essences, level 30 | 14 | 0 | 0% | 0–5.34% |

Every intended upper bound is below 50%, seven intended lower bounds exceed 10%, and every lower-Essence control upper bound is below 10%. With 256 samples and this fixed family of 344, the declared Pass rule corresponds to a strongest intended count of **44–97**, and all control counts at most **7**. The observed maxima, **93 and 1**, meet that rule. The intervals use the frozen approximate simultaneous policy; these are not ordinary unadjusted individual 95% intervals.

The seven combinations establishing viability are:

| Parent pattern | Gear profile | Added Essence | Wins / 256 | Observed rate | Adjusted bounds |
| --- | --- | --- | ---: | ---: | --- |
| Repeated | Armor and health | Shadow Imp | 93 | 36.33% | 25.92–48.19% |
| Repeated | Resistance and health | Gnoll Shaman | 86 | 33.59% | 23.52–45.42% |
| Repeated | Resistance and health | Shadow Imp | 78 | 30.47% | 20.83–42.19% |
| Repeated | Armor and health | Gnoll Shaman | 74 | 28.91% | 19.50–40.56% |
| Alternating | Armor and health | Shadow Imp | 74 | 28.91% | 19.50–40.56% |
| Alternating | Armor and health | Gnoll Shaman | 68 | 26.56% | 17.53–38.09% |
| Alternating | Resistance and health | Gnoll Shaman | 46 | 17.97% | 10.65–28.71% |

These are related combinations using two added Essences, not seven independent demonstrations of broad build diversity. The alternating resistance-and-health Shadow Imp upgrade wins **42/256**, but its lower bound is **9.46%**, so it does not establish 10% viability under the declared rule. No recipe was removed to improve the verdict.

Of the 312 intended combinations, **124 have at least one win**, **188 win zero**, and **seven establish viability**. All **84 older seven-Essence reference combinations win 0/256**. The passing combinations are controlled seventh-Essence upgrades of the strong six-Essence parents. This makes the retained upgrade coverage consequential; it does not establish that the existing search found those builds independently or that the search algorithm improved.

Only two lower-Essence controls win any fight: the repeated level-50 six-Essence parent's precision and restorer-specialization profiles each win **1/256**. Every other lower-Essence control, including all four level-matched controls, wins zero. These sampled families support the declared checkpoint separation; they do not prove that every possible six-Essence team is incapable of clearing the floor.

The [complete result](../TestResults/balance/tower-carried-confirmation-20260928/result.json) retains all 344 rows and intervals. [Recipes](../TestResults/balance/tower-carried-confirmation-20260928/cells.json), [selected setting](../TestResults/balance/tower-carried-confirmation-20260928/selected.json), [seed ledger](../TestResults/balance/tower-carried-confirmation-20260928/seed-ledger.json) and [native completion](../TestResults/balance/tower-carried-confirmation-20260928/completion.json) retain the unchanged family and complete accounting.

## Execution verification

The enabled fixture passed **eight tests**, including the scientific confirmation. Native preparation, historical qualification, fresh combat and reconstruction took **2,848.18 seconds**. The owner took **2,856.22 seconds** and drained all **eight processes**, below its 4,260-second limit. The [test log](../TestResults/tower-carried-confirmation-owner-20260928/confirmation.log), [verified eight-test TRX](../TestResults/tower-carried-confirmation-owner-20260928/confirmation-tests.trx), [process receipt](../TestResults/tower-carried-confirmation-owner-20260928/process.json) and [owner completion](../TestResults/tower-carried-confirmation-owner-20260928/completion.json) preserve that evidence.

The [independent audit](../TestResults/tower-carried-confirmation-owner-20260928/independent-audit.json) passed on its first run. It authenticated **99,907 files**, reconstructed all **11,008 qualification reports** and **88,064 fresh reports**, verified the complete allocation journal and **835,319-value exclusion union**, preserved every recipe, and independently reproduced all simultaneous bounds and the **Pass** verdict. The sealed archive occupies **2,411,869,579 bytes**, within 4 GiB. This audit ran zero fights.

Final checks passed for **105 local documentation links**, Python syntax/CLI, whitespace in all **11 scoped files**, and `git -c core.safecrlf=false diff --check`. All **176 frozen input/source/recovery pins** remain intact. The four preserved TRX files confirm 50 and 33 focused-test passes, then 10 calibration-enabled and eight confirmation-enabled passes; these overlapping suites are not summed as distinct tests. No required command remains blocked or unrun. The initial unrelated build failure was resolved before any confirmation combat. The live floor-file hash remains `dab5fe4db2f92af1e0441418ac63856f5ee75af8f26e19af258703f929020124`.

Archive manifest: `6a0d98f41362a3428816679efc9d0e8a90a88a37789030fbc38afa0e4652427d`.
Result SHA-256: `d5334add4af7c7d334842e9ecef1779aa7447f02dbcfc312898556f7c459dbc9`.

Commands executed from the repository root (Python denotes the bundled runtime):

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-carried-confirmation-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessCarriedConfirmationTests|FullyQualifiedName~BalanceHarnessRefinedCarriedCalibrationTests|FullyQualifiedName~BalanceHarnessCarryForwardEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionUpgradeTests|FullyQualifiedName~WorldTowerTests'
# Historical execution record; completed directories cannot be reused or resumed.
python -B -X utf8 'Balance Harness/analysis/confirm-carried-equipment.py' --package 'TestResults/tower-carried-confirmation-owner-20260928' --output 'TestResults/balance/tower-carried-confirmation-20260928' --artifacts 'TestResults/tower-carried-confirmation-build-20260928' --source-pin '5da2cb8fe7af95cd8b3d3f218ca516d45de1c510fcf9216c5177c06f45044151'
python -B -X utf8 'Balance Harness/analysis/verify-carried-confirmation.py' --owner 'TestResults/tower-carried-confirmation-owner-20260928' --manifest-pin '6a0d98f41362a3428816679efc9d0e8a90a88a37789030fbc38afa0e4652427d' --receipt 'TestResults/tower-carried-confirmation-owner-20260928/independent-audit.json'
git -c core.safecrlf=false diff --check
```

The ignored `TestResults` evidence is local and absent from a clean checkout. Later read-only audits require new receipt paths. Preserve the **835,319-value exclusion union**, including this panel, regardless of subsequent application or study decisions.

## Recommendation and scope

The subsequent [checked local application](Tower-Carried-Equipment-Checked-Application-20260928.md) now integrates **Health 17.94375 / Power 23.0175**. It matches all **88,064 inputs** and **470 complete reports** on the current build, covering every confirmed recipe and both observed outcomes. Independent audit verifies exactly the intended two fields changed. The repeating equipment curve and supported search remain unchanged. Further scalar sweeps are unnecessary for this declared family; retain the confirmed builds as progression references.

Acceptance is limited to the fixed carried-equipment family, level/tier budgets and hypothetical ownership assumptions. This study does not establish equipment acquisition feasibility, universal seven-Essence necessity, broad build diversity or balance across every ten-floor block. The older Rare-budget confirmation remains historical evidence under its own settings and assumptions.

**Historical scope of this confirmation:** added the confirmation fixture, owner, auditor and this report; updated follow-up links/results in the finer and coarse calibration reports, equipment baseline and supported-search guide. No production content, application configuration, dependency or migration changes were made. No database operation, deployment or service restart occurred. During this study, live floor 10 remained **12.71 / 7.13** and floor 11 **6.525 / 8.37**; the later local application is recorded above. Unrelated equipment-migration and LiveOps work is preserved.
