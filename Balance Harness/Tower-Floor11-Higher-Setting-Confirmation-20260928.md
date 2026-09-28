# Floor-11 confirmation at factor 2.25 — 28 September 2026

## Result: Pass for the complete tested family

The fixed **58,368-fight confirmation returned `Pass`** at **Health 6.525 / Power 8.37**. All 228 combinations received all 256 fresh seeds. The strongest tested seven-Essence party was the repeated source with Shadow Imp appended at every position: **47/256 wins (18.36%)**, with an approximate simultaneous adjusted interval of **11.10–28.82%**. Every intended upper bound is below 50%, and this team's lower bound exceeds 10%.

Every one of the **30 four-/six-Essence controls won 0/256**, including both level-60 controls. Their adjusted upper bounds are **5.06%**, below the exploratory 10% separation limit. All three frozen criteria pass. There were no draws, retries, extra samples or recipe changes.

| Seven-Essence party, level 60 and resistance-and-health gear | Fresh wins | Rate | Adjusted interval |
|---|---:|---:|---:|
| Repeated source + Shadow Imp | 47/256 | 18.36% | 11.10–28.82% |
| Alternating source + Shadow Imp | 20/256 | 7.81% | 3.54–16.36% |
| Repeated source + Gnoll Shaman | 19/256 | 7.42% | 3.30–15.86% |
| Repeated source + Lizardfolk Elementalist | 12/256 | 4.69% | 1.70–12.26% |

The result satisfies the existing existential viability rule; it does **not** demonstrate broad build viability. Only one intended team reaches 10% observed wins or has an adjusted lower bound at least 10%, and only nine of the 198 intended combinations win any fight. The original five roles of the repeated source remain intact; the uniform seventh addition does not make every character's complete build identical. All outcomes and bounds are in [result.json](../TestResults/balance/tower-floor11-higher-setting-20260928/result.json), with seed-free ordered recipes in [cells.json](../TestResults/balance/tower-floor11-higher-setting-20260928/cells.json).

**Current status:** the [separate checked local application](Tower-Floor11-Checked-Application-20260928.md) has applied **Health 6.525 / Power 8.37**. The current build reproduced all **58,368 input hashes** and all **12 representative complete battle reports**, with an independent audit. Close this floor-11 calibration cycle at **2.25× for this captured family and budget**. Keep the repeating equipment curve, supported search and all retained references. Build diversity, unsearched combinations and practical acquisition remain separate questions. The confirmation itself applied no production change; the subsequent local application includes no deployment or service restart.

The native run passed all **eight selected tests**, including its opt-in scientific fixture. Execution and reconstruction took **1,553.72 seconds**; the owner took **1,559.25 seconds** and drained all **eight processes**. The allocator accepted 256 candidates with **zero collisions**, producing a completed exclusion union of **834,807**. The prior factor-2.0625 `Fail` remains preserved as [prior-result.json](../TestResults/balance/tower-floor11-higher-setting-20260928/prior-result.json).

The [independent audit](../TestResults/tower-floor11-higher-setting-owner-20260928/independent-audit.json) passed on its first run. It authenticated **58,709 files / 1,386,130,125 bytes**, independently derived the fresh allocation, reconstructed every outcome and simultaneous bound, verified the unchanged recipes and producing runtime, and confirmed both the new `Pass` and preservation of the previous `Fail`. It ran **zero additional fights**.

Study manifest: `41a93659a05583d0cf55d91b98c2ef4442bfeba117ab1fea7d9daa0131827378`.
Result SHA-256: `6833b23f6409b86f1acc0e658838a718d59da441148083b0408dbfb69ecf5791`.

## Frozen follow-up

The [previous full-family confirmation](Tower-Floor11-Refined-Calibration-20260928.md) rejected factor 2.0625: the repeated Shadow Imp upgrade won **135/256 (52.73%)**, above the observed 50% ceiling. Its adjusted interval crossed 50%, so that result did not establish a true clear rate above the ceiling. All 30 four-/six-Essence controls won zero. The earlier `Fail`, source files, results and auditor remain unchanged.

This separate experiment fixes **factor 2.25: Health 6.525 / Power 8.37**, using variant 3 of the completed finer calibration. Both values are 9.09% higher than the prior confirmation's setting. Its strongest historical result was 8/32, providing a reason to test it with more observed room below 50%; neither monotonicity nor fresh success is assumed. The calibration's original lowest-eligible selection is preserved, while this follow-up explicitly selects the already screened 2.25 setting before any new seeds or combat.

All **228 combinations** remain fixed: 198 intended seven-Essence parties at level 60, fourteen six-Essence controls at level 50, two six-Essence controls at level 60, and fourteen four-Essence controls at level 30. Ordered Essences, party positions, gear choices, rolls and identity pins are retained. The user's repeating equipment curve and the supported search policy stay unchanged. This is a scoped floor-11 balance experiment, not a search-policy comparison or a universal minimum-Essence claim.

## Samples, assessment and stop rule

Every combination receives the same **256 fresh seeds**, for exactly **58,368 fights**. The complete prior exclusion union is **834,551**, including all values used in the failed confirmation. The existing deterministic allocator uses master **2026092815**, version `tower-floor11-fixed-family-confirmation-v2`, and one declared 1/255 allocation block. Full registry checks under the existing allocation lease precede allocation and repeat after execution. The expected completed union is **834,807**. No historical outcomes enter the estimates.

The existing approximate simultaneous 95% Bonferroni-Wilson calculation uses all **228 cells**. Its fixed criteria are unchanged:

- Every intended seven-Essence upper bound must be at most 50%.
- At least one intended seven-Essence lower bound must be at least 10%.
- Every four-/six-Essence control upper bound must be below 10%.

An observed intended rate above 50%, an observed control rate at least 10%, or every intended upper bound below 10% yields `Fail`. Meeting all three interval conditions yields `Pass`; otherwise the complete run is `Inconclusive`. Draws are nonwins. A weaker party cannot replace an above-ceiling result. The confidence statement is approximate and scoped to this fixed study; it is not a campaign-wide guarantee across repeated calibration attempts or a guarantee about unsearched recipes.

Complete the fixed panel once, then stop and report its outcome. No extension, retry, reselection, retuning or production application is part of this run. The prior failure stays attached to its original setting; the new result cannot rewrite it. Comparisons between the two distinct fresh panels are descriptive, not paired estimates.

## Implementation and verification

Target: the primary game's offline Balance Harness. Added:

- [BalanceHarnessFloor11HigherSettingConfirmationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessFloor11HigherSettingConfirmationTests.cs): a separate opt-in fixture (`LL_FLOOR11_HIGHER_SETTING_CONFIRMATION`) with exact setting selection, prior-failure retention, native preparation, complete allocation/accounting and full-family assessment. Selection tests reject substituted settings, changed values, ineligible/missing/duplicate entries and incomplete calibration. Existing interval and missing-family checks are retained.
- [confirm-floor11-higher-setting.py](analysis/confirm-floor11-higher-setting.py): authenticates both prior audits, freezes the new request and source hashes, requires the qualified game runtime, and owns the bounded wrapper process.
- [verify-floor11-higher-setting.py](analysis/verify-floor11-higher-setting.py): independently authenticates the complete archive, preserves the prior failed result, derives the fresh allocation, reconstructs every outcome and calculates the uncertainty bounds independently using Python's normal quantile.

The run uses the exact five game assemblies captured in the finer calibration. After the isolated test build, those hash-checked DLLs are copied only into the new generated test directory, followed by focused `-NoBuild` regression checks. Native execution must match the complete captured execution identity. All 228 combinations are prepared before allocation or combat. Existing sealed fixtures and scripts are not edited.

The focused build and regression run passed **23 tests**, with three scientific fixtures skipped until explicitly configured. The build had **45 existing warnings and zero errors**. The same **23 tests passed again against the captured game assemblies**. Both Python scripts passed syntax and CLI checks. Backend tests run through `build/run-tests.ps1`.

This report, the preceding finer/expanded reports and `AFFINITY-SEARCH.md` now record the completed result and latest exclusion ledger. Report links and whitespace checks passed, including the new untracked files. No required verification command was blocked. The unrelated equipment-migration changes already present in the working tree were preserved.

The limits are **2 GiB**, **2,400 seconds internally** and **2,460 seconds for the owner**, which must drain all children. Captured evidence remains local under `TestResults`; a clean checkout does not contain it. Completed output directories cannot be reused, and later read-only audits need a new receipt path.

Calibration manifest: `5a006fc55a27decfa5a41662d34bfacab09223457c004cbc35c2c62da12a9340`.
Prior confirmation manifest: `8556cf861be1d4d1c23e66bed92c757ee481dc762be8da77bc8d0b49ec57c77d`.

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-floor11-higher-setting-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessFloor11HigherSettingConfirmationTests|FullyQualifiedName~BalanceHarnessFloor11FamilyConfirmationTests|FullyQualifiedName~BalanceHarnessAffinityTeamConfirmationTests'
# After qualifying the five captured game DLLs in the new test directory:
./build/run-tests.ps1 -NoBuild -ArtifactsPath 'TestResults/tower-floor11-higher-setting-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessFloor11HigherSettingConfirmationTests|FullyQualifiedName~BalanceHarnessFloor11FamilyConfirmationTests|FullyQualifiedName~BalanceHarnessAffinityTeamConfirmationTests'
python -B -X utf8 'Balance Harness/analysis/confirm-floor11-higher-setting.py' --package 'TestResults/tower-floor11-higher-setting-owner-20260928' --output 'TestResults/balance/tower-floor11-higher-setting-20260928' --artifacts 'TestResults/tower-floor11-higher-setting-build-20260928' --source-pin '5a006fc55a27decfa5a41662d34bfacab09223457c004cbc35c2c62da12a9340'
python -B -X utf8 'Balance Harness/analysis/verify-floor11-higher-setting.py' --owner 'TestResults/tower-floor11-higher-setting-owner-20260928' --manifest-pin '41a93659a05583d0cf55d91b98c2ef4442bfeba117ab1fea7d9daa0131827378' --receipt 'TestResults/tower-floor11-higher-setting-owner-20260928/independent-audit.json'
git diff --check
```

Python refers to the bundled runtime. This work includes no migrations, shared-database operations, production configuration changes or deployments.
