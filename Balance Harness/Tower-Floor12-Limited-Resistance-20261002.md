# Floor 12: limited Resistance + Health diagnostic — 2 October 2026

## Completed diagnostic result

The complete **7,712-fight / 32-fresh-seed** panel finished and passed independent raw-outcome, recipe/order and native-participant checks. All **241 recipes / nine actual compositions**, including all 131 original controls, remain in the result. Of the **128 equipment-eligible recipes**, **122 won 0/32 and six won 1/32**. Floor 12 remains unresolved for expected limited equipment; this diagnostic is not acceptance.

| Original parent | Baseline | Best limited equipment | Full specialization | Median first death: baseline / limited / full |
| --- | ---: | --- | ---: | --- |
| e8fa67a114cf… | 0/32 | 1/32, eight items on slots 4 + 10 | 13/32, forty items | 18 / 18 / 36 s |
| 046ef3e3ae8a… | 0/32 | 1/32, eight items on slots 2 + 9 | 10/32, forty items | 18 / 18 / 36 s |

The separate [saved-report review](../TestResults/tower-floor12-diagnostic-mechanics-review-20261002/completion.json) completed **192 reports / four paired contrasts**, with zero combat or replays. Same-seed first-death delays have median zero for both limited variants and 9.5/9 seconds for full specialization. All 192 reports contain an unreconciled mitigation/prevention flag; recorded guardian ability/type totals reconcile. These descriptive comparisons do not isolate one gear attribute or prove a specific win probability.

Native study time was **199.59 seconds**. Current qualification had already matched **19,912 historical inputs / 131 full replays**, and all **241 recipes** prepared natively. Verification passed **133 Python preparation checks**, the **26 fresh backend qualification guards**, and the preparation/diagnostic fixtures without skips. Final exclusions: **928,732**. No live guardian, kit, progression, engine, migration, configuration or deployment change occurred.

Next is a separately frozen [Power/penetration diagnostic](Tower-Floor12-Penetration-Diagnostic-20261002.md): one isolated offense factor **0.50** and penetration factor **40**, retaining the complete family. It tests whether reducing ordinary-gear pressure while narrowing the Resistance advantage produces viable limited-equipment routes. The original equipment panel is closed; its outcomes are not reused as acceptance samples.

## Original frozen scope

Target: the primary LL World Tower and its offline Balance Harness. This experiment compares equipment against unchanged Volgrin. It does not select or apply a guardian change, change search, or reopen dungeon/acquisition work.

The original 131-recipe confirmation has two viable compositions, using forty specialized items across ten characters. Their exact baselines both won 0/152. Historical survival analysis shows earlier party deaths in those baselines, but cannot establish whether specializing just one or two characters is sufficient. Some historical damage-prevention telemetry does not reconcile, so exact prevented-damage attribution is not a decision criterion.

Preserve the complete proposed **241-recipe / nine-composition family**: all **131 original controls**, plus every one- and two-character Resistance + Health subset for both historical leaders (**110 variants**). Exactly **128 recipes** meet the limit of eight specialized items on at most two characters. The other recipes remain controls; equipment variants do not count as different compositions.

Retain ten level-60 characters, seven level-1 unascended/unevolved Essences each, tier-2 equipment, baseline rolls and no styles. Keep the repeating floor-cycle gear and stronger retained Legendary gear as authored. Every raw Essence, item and actor identity/order remains fixed. Volgrin health **9.414125**, offense **11.9**, regeneration **1.0**, and the entire guardian kit remain unchanged.

## Prerequisites and one finite panel

Wait for the floor-10 closeout, original floor-12 current-runtime qualification, and the complete native preparation to finish successfully. Require the `QualifiedFamilyPrepared` receipt, **19,912 matched historical inputs / 131 historical replays**, **241 native preparations**, all **133 Python guards**, and unchanged verified source/catalog/runtime hashes. Any prerequisite failure stops this experiment before allocation.

After these prerequisites, freeze one shared **32-fresh-seed panel across all 241 recipes: 7,712 fights and 32 reservations maximum**. Exclude the entire inherited seed union, expected to contain **928,700** values before this panel. Use the existing native `screen` mode with no candidate or content change. The panel is diagnostic only and has no acceptance or application branch.

Resource admission uses the closed original floor-12 confirmation: **19,912 fights in 418.8762948 seconds**, and its authenticated archived byte size. Double both observed per-fight time and bytes when projecting this panel. Require projections below 80% of the existing **840-second native / 2-GiB** limits, plus **2 GiB free-disk reserve**. The native process owner remains bounded at 900 seconds; an outer owner bounds preflight, execution and audit together at 1,800 seconds. Reject before seed allocation if admission fails. Runtime/size estimates are extrapolations, not guarantees.

Run the complete panel once. No interim selection, dropped control, changed recipe, retry, replacement seed, extension, pooled historical outcome, confirmation or automatic retuning is permitted. Preserve failed attempts and their reservations.

## Independent review and interpretation

After the native owner closes, authenticate every output and independently recount all 7,712 raw outcomes. Verify the exact recipe/seed schedule, all row means and constant prepared participants within each recipe. Keep every result, including all 131 original controls.

Report the strongest equipment-eligible recipe for each actual composition. For descriptive ranking only, sort by wins descending, remaining guardian health ascending, then exact recipe ID. For each of the two original leaders, show its exact baseline, best limited variant and full-specialization control, using this panel alone. Do not use guardian health or duration as balance acceptance criteria. Do not infer impossibility from zero wins on 32 seeds, or promotion from a noisy winner.

The next decision follows the complete result: if limited equipment remains ineffective, use the observed survival/output differences to define a separate finite mechanical diagnostic before choosing an adjustment. If limited equipment looks promising, define an independent complete-family acceptance scope. Neither follow-up is allocated here.

A separate read-only [report review](../TestResults/tower-floor12-diagnostic-mechanics-review-20261002.py) is queued behind successful closure. It will inspect the already saved baseline/limited/full reports for those two parent compositions: at most six unique recipes and 192 reports. The review checks first-death timing, remaining health, initial-character damage and guardian ability totals, and records any unreconciled telemetry. It uses no new combat, detailed-event replays or seeds, and does not select a candidate. Conditional selection of the limited recipe remains explicit; these comparisons are descriptive.

## Execution and evidence

Floor 10 is applied and fully verified locally. The first floor-12 build could not read the existing NuGet configuration inside the sandbox; it stopped before compilation, qualification, preparation or diagnostic allocation, and restored its source edits. The [recovery record](../TestResults/tower-floor12-build-access-recovery-20261002.json) preserves that failure and binds replacement scripts with fresh output/prerequisite paths. The replacement current build passed 99 Python and 26 native guards. Its [historical qualification](../TestResults/tower-floor12-current-qualification-buildrepair1-20261002/completion.json) passed: **19,912 input matches / 131 full replays / zero new seeds**. All 27 closed source/owner pins were checked, and the native process exited successfully with no timeout or surviving children. The preparation integration passed **133 Python checks** and completed all 241 seed-free native preparations. The later complete diagnostic is reported above.

Driver: `TestResults/tower-floor12-limited-resistance-screen-buildrepair1-20261002.py`.

Control: `TestResults/tower-floor12-limited-resistance-screen-driver-buildrepair1-20261002/`. Its `pending.json` records conditional intent; only `declaration.json`, written after all prerequisites and resource admission, freezes an executable panel. Its `completion.json` confirms the complete independently recounted diagnostic. Preserve `failure.json` if any stage stops.

Prepared source: `TestResults/tower-balance-pass-floor12-qualified-limited-resistance-preparation-study-20260929/`.

Study output: `TestResults/tower-balance-pass-floor12-limited-resistance-diagnostic-study-20260929/`.

Evidence: `TestResults/tower-floor12-limited-resistance-screen-driver-buildrepair1-20261002/evidence.json`.

Backend execution uses `build/run-tests.ps1`. No migration, application configuration change or deployment is included. This diagnostic does not complete floor-12 balance acceptance or the final current-version floors 1–15 sweep.

## Pre-execution verification

The original driver passed syntax validation and six checks covering complete prerequisites, incomplete qualification, changed/incomplete family preparation, historical/incomplete runtimes, the exact real saved family, and missing/duplicated controls. The [verification receipt](../TestResults/tower-floor12-limited-resistance-screen-checks-20261002.json) binds those original bytes; the recovery record verifies that the replacement changes only routing paths, preserving the exact diagnostic guards and budgets. The measured historical archive contains **451,768,182 bytes**; doubled projections were **324.47 seconds and 333.73 MiB** for 7,712 fights. These pre-execution checks ran no combat and allocated no seeds. The subsequent diagnostic backend execution and independent review both completed successfully.
