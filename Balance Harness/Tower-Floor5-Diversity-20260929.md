# Floor 5 Tower build diversity — 29 September 2026

**Latest applied result:** after the retained searches expanded the family, the [89-cell calibration](Tower-Floor5-Expanded-Calibration-20260929.md) confirmed and applied **+4% floor-5 health** with two viable lineups at 70/200 and 43/200. The rejected weakening scopes below remain closed. The [current handoff](Tower-Continuation-Handoff-20260928.md) records all 903,652 exclusions and next work on floor 7.

**Later search result:** the [supported-search challenge](Tower-Floor5-Search-Challenge-20260929.md) found four new lineups. Their strongest fresh expanded-family result was 80/128, above the target in that screen. The next scope is calibration against all 89 recipes, with 903,004 exclusions. The rejected weakening scopes below remain closed and unchanged.

**Completed without a content change.** The initial sweep and separately declared refinement ran **61,440 fights across all 60 recipes / eight compositions**. No tested setting passed the selection gates, so neither scope allocated confirmation seeds or applied guardian changes. The prior floor-5 values remain **health 3.1829618995 / offense 4.4702934848**. The complete earlier confirmation still establishes one viable composition; broader choice remains unresolved. **82 backend regressions passed**, with three intentional opt-in skips.

Target: the primary game's offline Balance Harness and floor-5 guardian data. This prospective pass retains the supported `affinity-creation-with-benchmark-validation-v1` search, all exact recipes, canonical Essence order and the approved progression budget. It adds no acquisition source and performs no deployment.

## Frozen starting point

The [complete reference-coverage confirmation](Tower-Reference-Coverage-20260929.md) contains **60 exact cells and eight distinct per-slot Essence compositions**. All cells, including original and projected references, remain in every screen and confirmation. Differences in labels, identities or gear do not count as new compositions. The strongest composition won **55/256** with resistance-and-health gear; the next distinct composition won **26/256** with ability-haste. The historical supply-dependent composition won at most **13/256**. These are selection context, not observations to pool into new confirmation.

Budget: ten level-40 characters, five unascended/unevolved Essences each, tier-1 Epic / Fine / rank-3 equipment, fixed rolls and positions, no styles. Full ownership is hypothetical; the withdrawn selectable supplies remain withdrawn.

Source: `TestResults/tower-balance-pass-floor5-reference-coverage-confirmation-study-20260929`, manifest SHA `4c91e38778017356e7b4c864ba55dc2be6572a1fe1a1a93c36a509b3db17dc9e`. Entry health/offense: **3.1829618995 / 4.4702934848**. Current whole-Tower SHA: `9487aa92e78bb603f571a728aa43713647e11384f4463f95979a6694bca701b5`. Entry exclusions: **901,378**, including unused reservations. The latest predecessor ledger is `TestResults/tower-balance-pass-floor3-diversity-refinement-confirmation-owner-20260929/seed-ledger.json`, SHA `7d6bedc984eebb48c7281088befa00cd8fb6120ee1f8e5cfb064c33953e36418`.

## Declared selection and acceptance

1. Authenticate the complete saved family and runtime. Prepare its unchanged recipes against current content with **zero fights**, admitting only the already-applied changes on other floors. Keep floor 5 and every non-Tower catalog identical.
2. Screen six settings, each on **64 fresh seeds across all 60 cells**: unchanged baseline; health −1.5%; health −3%; offense −1.5%; offense −3%; and health/offense both −1.5%. Factors are relative to entry values. Each setting has its own disjoint panel.
3. A changed setting is eligible for stability screening only if its strongest cell has at most **28/64 wins** and at least two actual compositions have a best cell with at least **12/64 wins**. Rank by most qualifying compositions, then most wins for the second composition, then strongest rate closest to 30%, then smallest sum of absolute scalar changes, then ordinal setting label. Retain at most **two** settings.
4. Screen each retained setting on **128 new seeds across all 60 cells**. Eligibility requires strongest cell at most **48/128 wins** and at least two compositions with at least **24/128 wins**. Apply the same ranking and freeze at most one confirmation setting. If none qualifies, close this scope without a confirmation or content change.
5. Confirm the single frozen setting on **256 fresh seeds per cell**, all **60 cells / 15,360 fights**. Approximate simultaneous 95% Bonferroni-Wilson intervals across the complete family must have every upper bound at most 50% and at least **two distinct compositions** with a cell lower bound at least 10%. The normal one-composition harness `Pass` alone is insufficient. Never pool selection observations, extend this confirmation, retry it, remove recipes or select a different setting from its results.
6. Only after both gates pass, apply the exact confirmed guardian scalars locally. Verify all 15,360 native inputs and one complete replay per cell against the applied file. Report duration separately; no pacing threshold is invented. If acceptance fails, preserve the evidence and leave production content unchanged.

Every phase uses the existing bounded owner: native 840 seconds, process 900 seconds, at most 20,000 fights and 2 GiB output. Before confirmation, twice the chosen stability run's measured time and archive size must fit within 80% of those native/output limits. The current build is `TestResults/tower-reference-coverage-build-20260929`; captured input hashes and backend source timestamps were checked before declaration. No pinned harness code or runtime may change during execution.

Maximum scope: zero-combat preparation, six grid screens, two stability screens, one confirmation = **53,760 new fights and 896 reserved seeds**, plus at most **60 application replays** without new seeds. All earlier ledgers remain excluded, including any unused values. Output directories are exclusive, immutable and never resumed or overwritten. A technical failure closes its phase; any later follow-up needs a separately declared scope.

Local execution helper: `TestResults/tower-floor5-diversity-driver-20260929.py`; it saves its protocol, entry content, decisions and hashes before combat. These local artifacts are ignored and unavailable in a clean checkout. The [current handoff](Tower-Continuation-Handoff-20260928.md) remains the continuation entry point.

## Completed selection screens

All six grid screens completed and their full outcomes were independently recounted: **23,040 fights**, zero retries. The table lists the best cell from each distinct composition, sorted by wins; gear variants and projected copies do not increase this count.

| Setting, relative to entry | Best wins per composition, out of 64 | Qualified compositions | Selection |
| --- | --- | ---: | --- |
| Unchanged | 14, 7, 7, 1, 0, 0, 0, 0 | 1 | Baseline only |
| Health −1.5% | 16, 11, 10, 1, 1, 0, 0, 0 | 1 | Ineligible |
| Health −3% | 18, 14, 9, 3, 2, 0, 0, 0 | 2 | First stability nominee |
| Offense −1.5% | 20, 10, 4, 2, 1, 0, 0, 0 | 1 | Ineligible |
| Offense −3% | 19, 13, 9, 1, 0, 0, 0, 0 | 2 | Second stability nominee |
| Health/offense −1.5% each | 26, 12, 6, 1, 1, 0, 0, 0 | 2 | Ranked third; not advanced |

The predeclared ranking favors health −3% and offense −3% because their second compositions have more wins than the combined setting. No grid result is accepted as balance confirmation. The driver froze both stability nominees before their new panels were allocated.

Both stability screens completed, adding **15,360 fights**:

| Setting | Best wins per composition, out of 128 | Gate outcome |
| --- | --- | --- |
| Health −3% | 54, 34, 14, 3, 2, 0, 0, 0 | Reject: leader exceeds 48 wins |
| Offense −3% | 38, 17, 12, 2, 1, 1, 0, 0 | Reject: only one composition reaches 24 wins |

The initial scope is closed **`NoStableCandidate`**, with **38,400 fights, 640 new reservations and 902,018 total exclusions**. No confirmation panel was allocated and no production content changed. Preserve this closed scope; the following refinement is separate and does not turn either failed setting into an accepted result.

## Separately declared health refinement

Declared after the above stability audits and before any refinement fights: test health factors **0.98, 0.9775 and 0.975** relative to the original **3.1829618995** health, with offense fixed at **4.4702934848**. The −1.5% grid result and rejected −3% stability result bracket a plausible smaller adjustment, but do not prove that any midpoint is acceptable.

Each setting receives **128 fresh seeds across the same 60 exact cells**. Keep the existing stability gate (maximum 48/128, at least two actual compositions with 24/128), the same ranking, and at most one complete **60 × 256 fresh confirmation**. Keep the same two-composition family-adjusted acceptance, resource preflight, source/runtime pins and application verification. If no candidate qualifies or confirmation fails, close this refinement without changing the game. Do not extend or repeat its confirmation.

The refinement imports all **902,018** exclusions, reserves at most **640** new values, and runs at most **38,400 study fights plus 60 existing-seed application replays**. Combined maximum across both scopes is **76,860 executions**, including replays, and **1,280 new reserved values**. Its exclusive helper and control directory are `TestResults/tower-floor5-diversity-refinement-driver-20260929.py` and `TestResults/tower-floor5-diversity-refinement-driver-20260929`. The first driver and its archived declaration remain immutable.

## Refinement outcome and next decision

| Health reduction | Best wins per composition, out of 128 | Gate outcome |
| --- | --- | --- |
| 2% | 52, 16, 10, 2, 2, 0, 0, 0 | Leader above 48; second below 24 |
| 2.25% | 54, 27, 13, 1, 1, 0, 0, 0 | Leader above 48 |
| 2.5% | 53, 22, 17, 4, 2, 0, 0, 0 | Leader above 48; second below 24 |

The refinement is closed **`NoRefinementCandidate`**, with **23,040 fights and 384 new reservations**. Both scopes remain closed and unapplied. No confirmation or application replay was run, because no setting qualified; the conditional application helpers were prepared but never executed. This is a negative selection result, not a failed fresh balance confirmation and not proof that all scalar settings are impossible. No outcome was pooled or used to relax the gate.

The broader reductions helped the alternative ability-haste lineup but also favored the strongest resistance-and-health lineup. The smaller adjustments did not meet both requirements. The rejected −3% health stability run averaged **124.75 engine seconds** for its leader, close to the current-setting confirmation's **126.19 seconds**; this does not establish a pacing improvement.

**Recommended next work:** keep the existing guardian values and run a compact, prospectively declared challenge with the **existing supported search** at this same player budget. Start from the best distinct retained lineups and explicitly examine resistance-and-health and ability-haste gear, rather than extending these closed scalar panels. This is an application of the retained algorithm, not another algorithm-development cycle. Preserve the complete 60-cell family and import every exact new search reference/finalist before any later full-family acceptance. First determine whether another viable lineup can be found at the current setting; then decide whether a new calibration scope is justified. Floor 7 is the next independent diversity target; floors 8/12/13 and broader archetypes also remain open. Acquisition modeling and replacement supplies are not prerequisites.

## Evidence and verification

Both scopes completed **12 phases, 61,440 native fights and 1,024 disjoint new seed reservations**, with zero retries and all bounded processes drained. Native combat totaled **1,557.65 seconds**, excluding audits and wrapper overhead. Each phase's independent audit authenticated the saved reports and recounted all outcomes. The final seed-free collector checked **912 immutable input/source/runtime pins**, complete recipe preservation and byte-identical production Tower content.

| Evidence | Path under `TestResults` | SHA-256 |
| --- | --- | --- |
| Combined evidence index | `tower-floor5-diversity-evidence-20260929.json` | `ed6c1fb8a28cae448e5c7312ee7a741f9426c4778bd6f586237a61c6cb8f152c` |
| Latest ledger | `tower-balance-pass-floor5-diversity-refinement-health-025-owner-20260929/seed-ledger.json` | `ca83e61d106c670ee9af7bf79d5d1da44a6b6e0838ce04f9d9d110a5c93ea970` |
| Final regression TRX | `tower-floor5-diversity-final-regression-20260929.trx` | `6df2da0d9db36ed73f8435e81a591ce330dd401b10da2b571648e7eaabf6a872` |

Latest exclusion union: **902,402**. Preserve every linked ancestor, unused reservation, original protocol snapshot and rejected screen. Tower SHA remains **`9487aa92e78bb603f571a728aa43713647e11384f4463f95979a6694bca701b5`**. No study remains active. The ignored local evidence is required to inspect this chain; a clean checkout does not include it.

Five seed-free selection checks verified family grouping and ranking. The existing compiled runtime was reused after checking its pins and that no backend C# source was newer. The final regression run passed **82 checks**, with three intentional opt-in skips. The final collector's initial assumption that preparation owns a seed ledger was corrected to require no preparation seeds and no ledger; its successful rerun was seed-free and did not rerun any combat.

Completed commands (use the bundled Python runtime; never reuse these study output paths):

```powershell
python -B -X utf8 'TestResults/tower-floor5-diversity-driver-20260929.py'
python -B -X utf8 'TestResults/tower-floor5-diversity-refinement-driver-20260929.py'
./build/run-tests.ps1 -NoBuild -ArtifactsPath 'TestResults/tower-reference-coverage-build-20260929' -Filter 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests'
python -B -X utf8 'TestResults/tower-floor5-diversity-closed-collect-20260929.py'
git -c core.safecrlf=false diff --check
```

Changed maintained files are this report, the current handoff, harness README/search guide and follow-up notices in the preceding calibration/diversity/coverage reports. Execution helpers and evidence are local under `TestResults`. This pass changes **no gameplay data, combat/search implementation, dependencies, configuration, migrations or databases**, and performs no API startup or deployment. No required verification remains blocked; confirmation and application were intentionally not run after the failed selection gates.
