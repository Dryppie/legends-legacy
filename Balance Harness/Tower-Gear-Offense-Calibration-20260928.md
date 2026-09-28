# Floor-13 gear-aware offense calibration — 28 September 2026

Follow-up: the [reference check and supported search are complete](Tower-Floor13-Geared-Search-Evaluation-20260928.md). The context passed reference screening, but the best generated finalist's 87/128 held-out wins versus the benchmark's 86/128 did not demonstrate an improvement. The benchmark and search policy were retained.

**Result: guardian offense 4.20 is the sole qualifying diagnostic setting in the frozen grid.** The strongest tested profile, resistance-and-health, won **22/32 fights (68.75%)** there. Original offense 3.36 remained saturated at 32/32, while offense 5.04, 6.72 and 10.08 produced no wins with any of the seven profiles.

The complete **1,120-fight calibration** and independent audit passed. This provides a candidate context for evaluating the supported search without weakening the known gear benchmark. It does **not** demonstrate a search improvement, confirm performance on fresh seeds, or approve a production balance change. The 68.75% observed result exceeds the gameplay policy's 50% ceiling; calibration eligibility concerns room to measure search gains, not balance acceptance.

## Frozen scope and results

The [preceding review](Tower-Gear-Aware-Benchmark-Review-20260928.md) froze five offense settings and all seven previously measured gear profiles. Ten characters, seven Essences each, level 60, Uncommon Fine tier-2/rank-3 gear, baseline rolls, no styles, hypothetical ownership, and the uncleared/no-contributions state remain fixed. Every character position, item/Essence identity and equipped Essence order is preserved. Attribute rules **18**, equipment release **4** and **healing-v1** remain captured.

Only floor 13's `guardianScaling.offense` changes, on separate captured content copies. Guardian health, defenses, penetration, regeneration, abilities, stagger, all other floors and combat settings are unchanged. The five producing combat assemblies exactly match the preceding coverage and confirmation runtime.

All 35 cells used the same 32 already-consumed historical seeds. Every row below shows wins out of 32; there was no fresh allocation, adaptive extension, retry or automatic search.

| Gear profile | 3.36 (×1.00) | 4.20 (×1.25) | 5.04 (×1.50) | 6.72 (×2.00) | 10.08 (×3.00) |
| --- | ---: | ---: | ---: | ---: | ---: |
| Original | 8 | 0 | 0 | 0 | 0 |
| Precision | 7 | 0 | 0 | 0 | 0 |
| Ability haste | 21 | 0 | 0 | 0 | 0 |
| Restorer specialization | 11 | 0 | 0 | 0 | 0 |
| Armor and health | 12 | 0 | 0 | 0 | 0 |
| **Resistance and health** | **32** | **22** | **0** | **0** | **0** |
| Health and regeneration | 28 | 0 | 0 | 0 | 0 |
| **Best profile** | **32** | **22** | **0** | **0** | **0** |

The predeclared rule selects the lowest offense whose **best** profile wins 4–28 fights inclusive, after completing the entire grid. Thus 4.20 qualifies; a weaker profile could not make the saturated 3.36 setting qualify. The coarse grid shows a steep observed transition between 4.20 and 5.04. It does not locate an exact threshold or establish monotonic behavior between sampled points.

Resistance remains essential among these measured bundles at 4.20. The six other profiles all losing does not establish that their every possible Essence composition is unviable. Likewise, seven profiles of one fixed composition do not establish the globally strongest available gear or team.

## Decision and next step

Retain **offense 4.20 with explicit resistance-and-health gear** as the diagnostic benchmark candidate. The complete selected content and scope are saved in [variant-01](../TestResults/tower-gear-calibration-20260928/variant-01/scope.json); all seven exact parties and historical seeds remain in [controls.json](../TestResults/tower-gear-calibration-20260928/controls.json). These saved seeds are not fresh validation evidence.

Keep `affinity-creation-with-benchmark-validation-v1` unchanged. Next, bind all three supported-search reference compositions and the inventory to the selected content, applying resistance-and-health consistently to their gear. Check stronger known references before spending on search. If those reveal another ceiling, this candidate context is unsuitable; do not hide them or substitute weaker equipment. A later bounded search needs a separately frozen fresh schedule and its existing validation gate. No search plan has been rebound or executed by this calibration.

The previously confirmed floor-13 and floor-15 gear results retain their original encounter scopes. Their confirmation does not transfer to this altered floor. Intended player budgets remain unresolved as described in the preceding review. No production guardian value was changed.

## Implementation and verification

Target: the primary game's offline Balance Harness tests and analysis tools.

- Added `LL/tests/EssenceSystem.Tests/BalanceHarnessGearCalibrationTests.cs`: the fixed matrix, isolated content variants, exact source-party checks, all-input preparation, bounded execution and native archive reconstruction. It reuses `TowerLoadoutArchive` and the existing fixture archive helpers; no gameplay or harness assembly changed.
- Added `Balance Harness/analysis/run-tower-gear-calibration.py`: authenticates the frozen review packet and source archives, requires the unchanged combat runtime, freezes the request, and uses the existing Windows process owner and backend test wrapper. New output directories are mandatory; it cannot resume or extend a run.
- Added `Balance Harness/analysis/verify-tower-gear-calibration.py`: independently hashes the complete archive, checks that only the selected offense field changed, reconstructs all results from raw reports, matches baseline reports, and recomputes best-profile eligibility and the final selection.
- Added this result record and updated the benchmark review and `LL/tools/BalanceHarness/AFFINITY-SEARCH.md` with the completed result and next step.

**22 focused checks passed**, with the scientific opt-in skipped, before execution. These include the selection boundaries, rejection of missing/duplicate cells, preservation of stronger controls, selection without assuming monotonic results, and isolation of the content change. The actual run then passed **all nine checks** in the calibration fixture. All **1,120 inputs** were prepared before combat, and **224 original floor-13 input hashes** matched the prior coverage archive before the first attempt.

Native readback reconstructed every recipe, input hash, cache identity and report binding. Independent readback authenticated **1,512 files** and **all 1,120 raw reports**, recomputed the 35 result rows and final selection, and matched **224 baseline inputs and complete battle reports** to the old coverage archive. All 1,120 cache identities were distinct. The audit ran zero fights.

The first sandboxed build could not read the user's NuGet configuration. Rerunning the same wrapper with approved access succeeded, with existing build warnings. Python syntax/command checks and `git diff --check` passed. No requested command remains blocked.

No migrations, production configuration changes, deployments or shared-database operations. Existing working-tree changes, including the attribute tooltip edits, are preserved.

## Execution and retained evidence

The experiment took **39.49 seconds**; its process owner took **41.58 seconds** and drained all eight processes. The sealed archive occupies **57,821,020 bytes**. Limits were 840 fixture seconds, 900 owner seconds and 1 GiB. Exactly 1,120 attempts and completions, zero retries, zero new seeds, zero confirmed teams, zero search runs. The latest scientific seed exclusion union remains **834,058**; its source ledger hash was checked before and after execution.

- Study: `TestResults/tower-gear-calibration-20260928/` (outside the fresh-seed registry).
- Owner declaration, request, process receipt, test log and independent readback: `TestResults/tower-gear-calibration-owner-20260928/`.
- Frozen proposal packet: `TestResults/tower-gear-benchmark-review-20260928/`.
- Successful focused-test log: `TestResults/tower-gear-calibration-verification-approved-20260928.log`.
- Runtime build: `TestResults/tower-gear-profiles-build-20260928/`.

Archive manifest SHA-256: `a827de90aa8c9a68409ec65615f447d123629c3dcf62e1000ba7675718063e65`.

Result SHA-256: `0a36117e43fa97df0a6afb1a02ce61f0000d344165a007df8de8c715b1c96b31`.

Commands, with Python referring to the bundled runtime:

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-gear-profiles-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessGearCalibrationTests|FullyQualifiedName~BalanceHarnessGearProfileTests'
python -B -X utf8 'Balance Harness/analysis/run-tower-gear-calibration.py' --help
python -B -X utf8 'Balance Harness/analysis/verify-tower-gear-calibration.py' --help
python -B -X utf8 'Balance Harness/analysis/run-tower-gear-calibration.py' --package TestResults/tower-gear-calibration-owner-20260928 --output TestResults/tower-gear-calibration-20260928 --artifacts TestResults/tower-gear-profiles-build-20260928
python -B -X utf8 'Balance Harness/analysis/verify-tower-gear-calibration.py' --study TestResults/tower-gear-calibration-20260928 --owner TestResults/tower-gear-calibration-owner-20260928 --manifest-pin a827de90aa8c9a68409ec65615f447d123629c3dcf62e1000ba7675718063e65 --receipt TestResults/tower-gear-calibration-owner-20260928/independent-readback.json
git diff --check
```

The execution command records the completed run; reusing those output paths is rejected. The audit requires a new receipt path if repeated.
