# Floor-10 fixed-family fresh confirmation — 28 September 2026

**Pass at 7.75×: Health 12.71 / Power 7.13.** All **5,376 fights** completed across the fixed **21 combinations**, each on **256 fresh seeds**, and the independent audit confirmed the verdict. The alternating retained party with armor-and-health gear won **55/256 (21.48%)**, with an approximate simultaneous adjusted interval of **14.75–30.20%**.

**Applied in a separate checked operation:** the [local application](Tower-Floor10-Checked-Application-20260928.md) now sets Health **12.71** / Power **7.13**. The current build matched all **5,376 archived inputs** and **23 complete representative reports**, with independent audit. The requested equipment curve and supported search remain in place. Next, assess floor 10→11 with Legendary gear carried forward. This confirmation remains the strength evidence; the [finer calibration](Tower-Floor10-Refined-Calibration-20260928.md) remains separate historical selection evidence. Target: the primary game's offline Balance Harness.

## Results

| Combination | Wins / 256 | Win rate | Adjusted interval |
| --- | ---: | ---: | ---: |
| Alternating retained party / armor-and-health | 55 | 21.48% | 14.75–30.20% |
| Repeated retained party / armor-and-health | 23 | 8.98% | 4.89–15.93% |
| Each of the other 19 combinations | 0 | 0% | 0–3.48% |

Every upper bound is below 50%, and the alternating party's lower bound exceeds 10%, satisfying the frozen rule. Only that combination establishes the required viability under this uncertainty policy. The repeated party's interval straddles 10%; its earlier 5/32 selection result is not pooled with these fresh observations. There were no draws. All 21 combinations remain in [result.json](../TestResults/balance/tower-floor10-family-confirmation-20260928/result.json), and their exact ordered recipes remain in [cells.json](../TestResults/balance/tower-floor10-family-confirmation-20260928/cells.json).

This is a narrow confirmed route within the retained family, not evidence of broad build diversity or a minimum six-Essence requirement. The family contains no lower-Essence cohort. The floor-11 confirmation has a separate reference gear budget; this result does not resolve carrying stronger floor-10 gear into floor 11.

## Frozen design

Retain the authored, repeated and alternating six-Essence parties, each with all seven gear profiles. Every party contains 15 level-50, tier-2 characters using Legendary / Masterpiece / rank-5 gear, baseline rolls, no styles, and level-1 unascended/unevolved Essences. Full actor/item identities, party positions and ordered Essences stay as captured. Ownership remains hypothetical. There are no lower-Essence controls, so this study cannot establish that six Essences are necessary.

Each cell receives the same 256-seed panel, allocated after preparing every recipe without combat. The latest complete exclusion source is the [floor-11 confirmation ledger](../TestResults/balance/tower-floor11-higher-setting-20260928/seed-ledger.json), containing **834,807** historical/reserved values. The complete registry must match that union before allocation and remain unchanged throughout execution. Existing recovery receipts remain pinned. Master **2026092816** and domain `tower-floor10-fixed-family-confirmation-v1/block-1` feed the existing SHA-256 rejection allocator. Every accepted seed is durably reserved before combat; no historical results enter the new estimates.

Acceptance uses approximate simultaneous **95% Bonferroni-Wilson intervals across all 21 cells**. Every upper bound must be at most 50%, and at least one lower bound must reach 10%. Any observed rate above 50%, or all upper bounds below 10%, yields `Fail`. Other unresolved evidence yields `Inconclusive`. Draws count as nonwins. The complete family runs once, without pooling, weaker-team substitution, extension, retuning or retries. Coverage applies to this fixed study, not all experiments or unsearched recipes.

Use the exact five captured game assemblies, content and sanitized settings from finer-grid `variant-07`; change only the archive's algorithm label and seed schedule. Settings remain attributes 18, equipment release 4, `healing-v1`, captured threat rules and 10 ticks per checkpoint. Current repository content must still match the source baseline before launch. This study does not apply the candidate to the local floor file.

Limits: **840 seconds** internally, **900 seconds** for the process owner, **2 GiB** output, **5,376 attempts**, and zero retries. Preparation, combat and native reconstruction are inside the native budget. The owner drains its process tree. The subsequent read-only audit independently authenticates the archive, derives the seed panel, reconstructs all outcomes and intervals, and checks the verdict without running fights. Failed or incomplete evidence is retained and cannot be resumed or overwritten.

## Implementation

- [BalanceHarnessFloor10FamilyConfirmationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessFloor10FamilyConfirmationTests.cs): separate opt-in fixture (`LL_FLOOR10_FAMILY_CONFIRMATION`), exact selection/family checks, complete fresh reservation, per-attempt accounting, archived reports and native input reconstruction. Tests reject incomplete families, changed budgets/settings, invalid counts and unadjusted uncertainty bounds.
- [confirm-floor10-family.py](analysis/confirm-floor10-family.py): pinned candidate/history import, frozen declaration and bounded process owner.
- [verify-floor10-family-confirmation.py](analysis/verify-floor10-family-confirmation.py): independent inventory, seed derivation, recipe/outcome, budget, interval and assessment audit.

Earlier sealed fixtures, scripts and archives remain inputs. No migrations, configuration changes, dependencies, deployments, service restarts or database operations are part of this confirmation.

## Execution

The protocol above was written before fresh allocation. Native execution and reconstruction took **259.75 seconds**; the process owner took **262.09 seconds**, exited successfully and drained all **eight processes**. All 5,376 starts completed, with no cache reuse, retry or omitted cell. All 256 accepted seeds were fresh, with zero allocation rejections. The latest completed exclusion union is now **835,063**, recorded in the [new ledger](../TestResults/balance/tower-floor10-family-confirmation-20260928/seed-ledger.json); retain it with the full registry for future allocation.

The [independent audit](../TestResults/tower-floor10-family-confirmation-owner-20260928/independent-audit.json) passed on its first run. It authenticated **5,510 files / 219,761,152 bytes**, independently reproduced the complete seed allocation, reconstructed every recipe/outcome count and all 21 adjusted intervals, and confirmed `Pass`. It executed zero additional fights. Native verification also reconstructed all 5,376 combat input hashes and cache identities.

The freshly built five game binaries had different hashes from the candidate's captured binaries. The new test output directory therefore received the five authenticated captured DLLs before the second regression run and confirmation; the source archive stayed untouched. Both runtimes passed the focused checks. The [qualification receipt](../TestResults/tower-floor10-confirmation-preparation-20260928/runtime-qualification.json) records the before/after hashes and confirms all 29 current content files match the source baseline. The local floor file remained SHA-256 `32ece402099702d29e34977c507adbb53c5d2a0a5994f82fc9a4507f7b4d6e8e`, with floor 10 still **1.64 / 0.92** and the previously applied floor-11 values intact. No current-runtime equivalence beyond the reported checks is inferred from differing binary hashes.

## Verification and reproduction

**62 focused backend checks passed** on the fresh build, then the same **62 passed** using the captured game binaries. Two opt-in scientific fixtures were skipped in each regression run. The separately owned confirmation then passed **all seven selected tests**, including the real scientific fixture. All backend tests used `build/run-tests.ps1`. The build reported **45 existing warnings and zero errors**. Python syntax/CLI, documentation-link and whitespace checks passed. No required verification command remains blocked or unrun.

The new fixture, owner, auditor and this report are the implementation additions. The finer-grid report, equipment-baseline follow-up and `AFFINITY-SEARCH.md` now point to the completed confirmation. Unrelated equipment-migration work remains untouched.

Candidate source manifest: `b866bb74e5aa897dcd241423208fe53a47bdf5a583846434a07c460f6f6a015f`.
Confirmation manifest: `95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944`.
Result SHA-256: `b2fd5bbbf849d15699f8a66907f68e1c16b53e37837e4cc4e8a71842dfaa7be5`.

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-floor10-family-confirmation-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessFloor10FamilyConfirmationTests|FullyQualifiedName~BalanceHarnessRefinedFloor10CalibrationTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~WorldTowerTests'
# After verifying and placing the captured game DLLs in the new test output directory:
./build/run-tests.ps1 -NoBuild -ArtifactsPath 'TestResults/tower-floor10-family-confirmation-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessFloor10FamilyConfirmationTests|FullyQualifiedName~BalanceHarnessRefinedFloor10CalibrationTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessProgressionPreviewTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~WorldTowerTests'
python -B -X utf8 'Balance Harness/analysis/confirm-floor10-family.py' --package 'TestResults/tower-floor10-family-confirmation-owner-20260928' --output 'TestResults/balance/tower-floor10-family-confirmation-20260928' --artifacts 'TestResults/tower-floor10-family-confirmation-build-20260928' --source-pin 'b866bb74e5aa897dcd241423208fe53a47bdf5a583846434a07c460f6f6a015f'
python -B -X utf8 'Balance Harness/analysis/verify-floor10-family-confirmation.py' --owner 'TestResults/tower-floor10-family-confirmation-owner-20260928' --manifest-pin '95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944' --receipt 'TestResults/tower-floor10-family-confirmation-owner-20260928/independent-audit.json'
git -c core.safecrlf=false diff --check
```

Python denotes the bundled runtime. These commands document the completed run: its output paths cannot be reused, and later read-only audits require a new receipt path. Captured evidence and executables are local under ignored `TestResults` and absent from a clean checkout. This report launches no application or further experiment.
