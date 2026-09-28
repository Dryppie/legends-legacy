# Floor-10 linked Health/Power calibration — 28 September 2026

**Follow-up completed:** the [finer 6×–8× grid](Tower-Floor10-Refined-Calibration-20260928.md) ran 6,048 fights and selected **7.75×: Health 12.71 / Power 7.13** as a diagnostic candidate. Its strongest combination won 7/32 (21.88%); the 7.5× setting still had a 17/32 result above the ceiling. All 21 combinations and both complete boundary panels were retained. The next step is fresh confirmation of the complete family at 7.75×. The broad-grid result below remains unchanged, and no candidate has been applied.

**No tested setting qualifies.** All **5,376 declared fights** completed. The strongest retained combinations still win **32/32 at 6× Health/Power**, while every combination wins **0/32 at 8× and 12×**. The result is `NoSettingInGrid`; no candidate is selected or applied.

**Recommendation at completion:** test a separately frozen finer grid inside 6×–8×, retaining all 21 combinations. That follow-up is now complete, as linked above. The broad screen located a useful interval without proving monotonic behavior or guaranteeing an acceptable setting inside it. Keep the requested equipment curve and supported search. The resulting candidate requires fresh confirmation before application.

## Frozen screen

Target: the primary game's offline Balance Harness. The [repeating-equipment baseline](Tower-Equipment-Cycle-Baseline-20260928.md) recorded **32/32 wins for all 21 floor-10 combinations**, with mean battle duration 12.26–16.20 seconds. This screen preserves that entire family and changes only the Mad King's Health and Power in isolated content copies.

Each party has **15 characters**, **six Essences per character**, **level 50 / tier 2**, and **Legendary / Masterpiece / rank-5 equipment**. Rolls remain at baseline, Essences remain level 1 and unascended/unevolved, and styles are absent. Ownership is hypothetical. Ordered Essences, positions, identity pins, equipment and all seven specialization choices remain intact.

The three parties are `floor10-authored`, `floor10-retained-1` (repeated deployment) and `floor10-retained-2` (alternating deployment). Each retains original gear, precision, ability haste, restorer specialization, armor-and-health, resistance-and-health, and health-and-regeneration. The latter two parties remain known strong controls; no weaker reference can replace an above-ceiling result. This family contains no lower-Essence cohort, so it cannot establish that six Essences are necessary.

The fixed grid is deliberately broad because the baseline is at the observed ceiling. Both guardian values use the same factor, preserving their ratio:

| Linked factor | Health | Power | Strongest wins / 32 | Cells above 50% |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 1.640 | 0.920 | 32 | 21 |
| 1.5 | 2.460 | 1.380 | 32 | 21 |
| 2 | 3.280 | 1.840 | 32 | 21 |
| 3 | 4.920 | 2.760 | 32 | 21 |
| 4 | 6.560 | 3.680 | 32 | 21 |
| 6 | 9.840 | 5.520 | 32 | 10 |
| 8 | 13.120 | 7.360 | 0 | 0 |
| 12 | 19.680 | 11.040 | 0 | 0 |

Every setting receives **21 combinations × the same 32 historical seeds**: **5,376 fights total**, including **672 full baseline replays**. All 168 setting/recipe combinations must prepare before combat. Every baseline input and complete report must match the prior equipment-cycle screen before stronger variants start. No extra seeds, retries, early winner selection, adaptive interpolation or automatic confirmation are allowed.

The exploratory selection rule chooses the lowest tested factor whose **strongest retained combination wins 4–16/32**. Thus every measured combination must stay at or below 50%, and at least one must reach 10% observed viability. This discretizes the existing inclusive 10–50% target for a 32-seed diagnostic; it is not a new acceptance threshold. Draws are nonwins. All eight factors run, even if an earlier one appears promising or none qualifies. Historical samples do not provide fresh confirmation, and repeated grid inspection does not establish a true win-rate bound.

At 1× through 4× every cell wins 32/32. At 6× the gear/composition differences become visible:

| Six-Essence party | Original | Precision | Haste | Restorer | Armor/health | Resistance/health | Health/regen |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Authored | 0 | 0 | 0 | 0 | **32** | 0 | 0 |
| Retained 1, repeated | 29 | 31 | 10 | 26 | **32** | 1 | 15 |
| Retained 2, alternating | 31 | 31 | 19 | 17 | **32** | 2 | 15 |

Every entry is wins out of 32, without pooling. Three cells fall inside the observed 4–16/32 screening band, but choosing one of those would hide the ten above-ceiling combinations. All three armor-and-health variants remain perfect. At 8× and 12× all 21 cells have zero wins. There are no draws anywhere in the grid.

Mean battle durations by cell range from 50.55–100.19 seconds at 6×, 45.10–75.78 at 8×, and 30.68–49.79 at 12×. These are descriptive all-outcome means, not an approved pacing assessment. The experiment changes both Health and Power together; it does not isolate which mechanic causes the observed transition. Full outcomes, guardian-health means and paired gains/losses remain in [result.json](../TestResults/tower-floor10-linked-20260928/result.json).

## Runtime and evidence

The source baseline manifest is `79fd4c8f4db16d4b98dd3b78dd0a53224499a0a85ed6ba92cc0cceeacffc5abe`. Its independent audit and all consumed inputs are authenticated. The current local content includes the [completed floor-11 application](Tower-Floor11-Checked-Application-20260928.md), whose manifest is `a499d45ac176b3a9e13118b6cc1cabd28943685641ce7b78af0e7e99aba5cb2d`. Comparing the entire content inventory permits only that already checked floor-11 change relative to the historical baseline. Each new content variant then changes only floor 10's two declared fields; the applied floor-11 values remain intact.

The run uses freshly built current assemblies, captures their hashes and executable files, and qualifies them against all 672 baseline inputs/reports. Effective settings must remain attributes 18, equipment balance 4, `healing-v1`, the captured threat rules and 10 ticks per checkpoint. Native reconstruction verifies every recipe, seed, materialized input, cache identity and saved report. The independent Python auditor reconstructs every count, mean, paired gain/loss, variant summary and selection from raw battle reports.

The native limit is **840 seconds**; the owner limit is **900 seconds**, with process-tree drainage. Output is capped at **2 GiB**. The owner freezes source, executable and request hashes before combat, and preserves incomplete evidence on failure. Builds and ordinary regression-test simulations sit outside the declared diagnostic combat/time budget.

Execution and native reconstruction took **171.11 seconds**; the owner completed in **173.44 seconds**, draining all **eight processes**. All 672 baseline input hashes and complete reports matched before stronger variants ran. All 5,376 attempts completed with zero retries, fresh seeds, new searches or newly confirmed teams. The scientific exclusion union therefore remains **834,807** from the preceding confirmation.

The [independent audit](../TestResults/tower-floor10-linked-owner-20260928/independent-audit.json) passed on its first run. It authenticated **6,097 files / 205,543,020 bytes**, verified all recipes and isolated content changes, reproduced every outcome and summary, confirmed all 672 baseline full-report comparisons and independently returned `NoSettingInGrid`. The audit ran no additional fights.

Implementation:

- [BalanceHarnessFloor10CalibrationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessFloor10CalibrationTests.cs): opt-in fixture (`LL_FLOOR10_CALIBRATION`), complete retained-family/budget validation, isolated content variants, parity gating, durable attempts and final verification. Ordinary tests check strongest-team selection, the inclusive screen boundaries, missing/duplicate/invalid rows, disallowed factors and content isolation.
- [calibrate-floor10-linked.py](analysis/calibrate-floor10-linked.py): source/application authentication, fixed request and bounded process owner.
- [verify-floor10-linked.py](analysis/verify-floor10-linked.py): independent read-only reconstruction and audit.
- [BalanceHarnessFloor11CalibrationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessFloor11CalibrationTests.cs): two setup lines make its historical variant-isolation unit test explicitly construct the old 2.90 / 3.72 baseline in memory. Its scientific fixture and calibration logic are unchanged. The original source bytes and hash are preserved under `TestResults/tower-floor10-fixture-compatibility-20260928`; historical source-pin checks require the original source snapshot, not the revised working-tree file. Saved studies and their manifests are untouched.

This work includes no production content edits, appsettings changes, migrations, dependencies, deployments, service restarts or database operations. The existing supported search and repeating equipment curve remain the inputs to this calibration. Unrelated equipment-migration work is preserved.

## Verification and reproduction

**75 focused backend checks passed**, with four scientific fixtures skipped until explicitly configured. The separately owned floor-10 run then passed all **seven selected tests**, including its combat fixture. Tests ran through `build/run-tests.ps1`. The initial full build had **45 existing warnings and zero errors**; the final incremental build had **16 existing warnings and zero errors**. Python syntax/CLI, report links and whitespace checks passed. No required verification command remains blocked or unrun.

The older floor-11 unit-test source was saved before its two-line setup correction and independently matches the historical source pin `ea999af944fcb9ca457973cb3b9ed3132b7a5889ec648e8c00fddf2e7370c317`. Both the original source and the completed studies remain available for reproduction. Its ordinary test now passes against the applied content without modifying that content.

Study manifest: `c67912fac6fa2f72b82676859c9f2cb525585d0cd6200f604b86d808c432bcf1`.
Result SHA-256: `ca5d8706d0d6ff2e305a308ef6d95c706ad034ba8f02a355497a413adab8211d`.

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-floor10-linked-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessFloor10CalibrationTests|FullyQualifiedName~BalanceHarnessFloor11CalibrationTests|FullyQualifiedName~BalanceHarnessExpandedFloor11CalibrationTests|FullyQualifiedName~BalanceHarnessRefinedFloor11CalibrationTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~WorldTowerTests'
python -B -X utf8 'Balance Harness/analysis/calibrate-floor10-linked.py' --package 'TestResults/tower-floor10-linked-owner-20260928' --output 'TestResults/tower-floor10-linked-20260928' --artifacts 'TestResults/tower-floor10-linked-build-20260928'
python -B -X utf8 'Balance Harness/analysis/verify-floor10-linked.py' --study 'TestResults/tower-floor10-linked-20260928' --owner 'TestResults/tower-floor10-linked-owner-20260928' --manifest-pin 'c67912fac6fa2f72b82676859c9f2cb525585d0cd6200f604b86d808c432bcf1' --receipt 'TestResults/tower-floor10-linked-owner-20260928/independent-audit.json'
git -c core.safecrlf=false diff --check
```

Python denotes the bundled runtime. These commands record the completed operation, whose output paths cannot be reused. Later read-only audits need a new receipt path. Captured evidence and executable files are local under ignored `TestResults`, absent from a clean checkout. The new report, equipment-baseline follow-up and search guide now record this result; the earlier floor-11 application report links to it. No finer screen or fresh confirmation is launched by this report.
