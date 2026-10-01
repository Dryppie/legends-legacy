# Floor 9: limited Resistance + Health equipment — 1 October 2026

Target: the primary LL game's World Tower and offline Balance Harness. Test expected-progression equipment against unchanged **Ni, the Ninefold**, carrying forward the accepted floor-8 catalog.

## Completed result

**Floor 9 remains unresolved.** The complete four-batch screen closed **`LimitedResistanceScreenNotAccepted`** after **18,944 fresh fights / 128 new reservations**. All **148 recipes / five actual compositions** were retained and independently audited. Every one of the **115 equipment-eligible recipes won 0/128**. No recipe failed the ceiling; the largest adjusted upper bound is **38.88%**. The minimum was **25/128** for at least two distinct eligible compositions. No confirmation or gameplay edit followed.

| Comparison | Baseline | Best eight-item pair | Original forty-item Resistance + Health |
| --- | ---: | ---: | ---: |
| A — `5b6c297c…` | 0/128 | 0/128 | 30/128 (23.44%) |
| B — `reference-3` | 0/128 | 0/128 | 18/128 (14.06%) |

All one-character and two-character substitutions lost every fight. Among tied eight-item variants, lowest mean remaining guardian Health selects slots **3+4 for A** and **4+8 for B** for the next descriptive replay comparison (45.95% and 47.98% mean remaining guardian Health). These are diagnostic selections, not viable routes. Another retained fully specialized composition won **20/128**. The duplicate projected B representation also won **18/128** and does not count as an additional composition. The simultaneous upper bound for each zero-win recipe is **9.12%**; this conclusion applies to the declared family, not every possible party.

This closes the unchanged-Ni equipment experiment. Historical full-party gear success does not establish success under the limited-equipment requirement. The result does not identify the causal ability or justify selecting an offense coefficient without a damage/timing diagnosis.

## Verification and implementation

Before fresh allocation, independently recounted all **9,728 historical outcomes**, qualified **9,728 native inputs / 38 full historical replays** against the accepted catalog, and prepared all **148 exact recipes** without combat or new seeds. The prior floor-8 aggregate was reconstructed during qualification; no additional floor-8 fights were run.

The implementation adds the exact floor-9 family contract and a separate four-by-32 aggregate contract in the Python harness, plus matching native application safeguards. It also fixes historical qualification after the accepted Venomspawn inheritance change: only the exact receipt-bound, fully accepted summon delta is permitted. Every other summon difference remains rejected. The approved expected-progression curve, raw Essence order, actor identities, positions and all **102 live catalog files** are unchanged.

**439 distinct Python checks / 20 suites and 442 distinct backend cases pass; four intentional backend skips.** The native cases passed in both the fresh build and the runtime retaining the five previously qualified production assemblies. Six native preparation probes agree between those runtimes. Counts are distinct cases, not the sum of repeated executions. All four fresh study fixtures passed, and independent reconstruction checked every raw outcome, prepared participant, recipe schedule, interval, equipment count and seed reservation. `git diff --check` is required by publication closure.

Executed verification wrappers are `TestResults/tower-floor9-limited-resistance-tests-20261001.py` and `TestResults/tower-floor9-limited-resistance-runtime-20261001.py`; the latter invokes **`build/run-tests.ps1`** for both native checks. Study execution is `TestResults/tower-floor9-limited-resistance-driver-20261001.py`, with independent audit in `TestResults/tower-floor9-limited-resistance-collect-20261001.py`. Existing receipt paths are immutable; a future execution needs fresh paths. Earlier 427-case preparation checks are included in these totals, not additional cases.

The initial read-only review encountered a missing reviewer-source pin in the predecessor publication before creating output. It was resolved by explicitly binding the current reviewer and passing its six safeguards. No combat retry or replacement seed was used. All required checks completed. There are **no migrations, application configuration changes or deployment implications** from this step.

## Next Tower work

Run one **96-historical-replay Ni diagnostic**, proposed and not executed: both original leader compositions, each with baseline, selected eight-item pair and original forty-item equipment, across the first sixteen declared seeds of the first completed screening batch. Use every selected seed regardless of outcome; verify complete saved-report parity after removing the added event log. Preserve all 148 recipes for any later acceptance study.

Determine first-casualty sources and mitigation, Ninth Seal damage, surviving copies and permanent Power gains, damage to Ni versus copies, and the effects of One Among Nine health swaps, healing and lost party output. Treat the whole equipment profile as the comparison. Add a Ni-specific diagnostic contract and guards and pass measured resource admission before execution. Maximum **96 replays / 840 seconds / 2 GiB**, at most **60 seconds and 16 MiB per replay**, with doubled admission estimates below **672 seconds / 80% of 2 GiB**. Allocate **zero new seeds or acceptance fights**. No balance coefficient or gameplay candidate is selected.

The frozen proposal is `TestResults/tower-floor9-ni-pressure-diagnostic-proposal-20261001.json`, SHA `31d196272058c1f03a1e7861c272171c615732e379d858e087cf4a854c10473e`. It includes exact recipe/trial identities and the paired seed selection. Do not resume confirmation of this failed screen, extend it, pool historical outcomes or move to dungeons/acquisition. Floor 8 remains accepted and locally applied. Floors **9–10 and 12–15** and the final current-version **1–15 sweep** remain.

Final exclusions: **925,996**, including all preceding 925,868 and exactly 128 new reservations. Declaration SHA: `1a0c9ba14ea97af6e00407823215610f4b3d6091a25dad8c6b5d6d813ceb7e4b`. Independent evidence SHA: `5c6e9e8c110b3d4d52470946f24adef3afad5cbfd16688a7da941fc47a658dda`. Final publication and readback receipts are `TestResults/tower-floor9-limited-resistance-publication-20261001/completion.json` and `TestResults/tower-floor9-limited-resistance-publication-check-20261001.json`. The pre-allocation protocol is preserved in the closed driver directory; the protocol below retains its original decisions.

## Frozen protocol

Retain the exact **38 historical recipes / five actual compositions**, including the three projected reference controls. Count equivalent compositions once without rewriting their raw Essence order, identities, equipment order or party positions.

The historical 256-seed family has two distinct qualifying Resistance + Health compositions at **58/256 and 47/256**. Both use **40 specialized items across ten characters**; no saved composition qualifies within eight items on two characters. One projected representation also wins 47/256 and is retained as a control, without counting as another composition. These historical results select the equipment comparison; they do not contribute outcomes to the new acceptance decision.

Keep the exact archived baselines for those two parent lineups. Add every one-character and two-character Resistance + Health subset: **10 singles and 45 pairs per parent**, changing only the saved Chest, Head, Legs and Necklace items for the selected characters. The complete family is **148 recipes / five actual compositions**. Exactly **115 recipes** meet the limit of **eight specialized items on at most two characters**, including the five original baselines. Keep every other recipe as a ceiling control.

The comparison uses the entire saved equipment profile, which changes several attributes. It measures equipment dependence without attributing the result to Resistance alone.

Preserve **ten level-40 characters, five level-1 unascended/unevolved Essences each, tier-1 Unique / Exceptional / Rank-4 gear, fixed roll 1, and no active styles**. Ni stays at Health **2.970703125**, offense **4.7036132812**, defense/resistance **2.55**, penetration/regeneration **1.0**. Do not change any live catalog, search algorithm or acquisition rule.

Before allocation, authenticate and recount the historical source, qualify all **9,728 historical native inputs / 38 full historical battles** against the current catalog, and prepare the complete new family natively with zero fights. Require fresh Python and backend guards through `build/run-tests.ps1`, retaining the qualified production combat assemblies. Catalog qualification may admit only the exact receipt-bound, fully accepted Venomspawn inheritance; unrelated summon changes remain rejected.

Use the separate `tower-balance-limited-resistance-aggregate-v1` contract. Freeze **four complete 32-seed batches per phase** before allocating any seed. Screen all 148 recipes on **128 fresh shared seeds**, totaling **18,944 fights**. Only a complete passing screen permits an independent confirmation with the same sample size and unchanged family. Maximum **37,888 fresh fights / 256 reservations**. Preserve all **925,868** preceding excluded seeds. No historical pooling, interim statistical decision, retry, replacement seed, extension, dropped control or additional candidate.

Each complete phase stands alone. Use the existing approximate simultaneous 95% Bonferroni-Wilson adjustment over all **148 recipes**, alpha 0.05. Require at least two actual equipment-eligible compositions with adjusted lower bounds **at least 10%**, and every recipe with an adjusted upper bound **at most 50%**. At 128 samples, the exact count gates are **25 or more wins** for a qualifying recipe and **43 or fewer wins** for every recipe. Labels do not establish equipment eligibility or distinct composition identity.

Each native batch contains **4,736 fights**. Double the measured historical resource cost for the first batch of each phase, then re-evaluate admission from the preceding completed batch in that phase. Require the doubled estimate below **672 seconds** and **80% of 2 GiB**, with free disk for all remaining doubled archive estimates plus 2 GiB. Keep the **840-second native / 900-second owner / 20,000-fight / 2-GiB** limits. The historical reference completed 9,728 fights in 194.9380479 seconds: the initial doubled estimate is about **189.81 seconds per 32-seed batch**. A single 128-seed batch would estimate about **759.23 seconds**, so it is not admissible. During native work, observe supervisor output and keep active study, owner and control files unopened.

At closure, independently authenticate each archive, recount every raw result, verify the prepared participants and complete recipe schedule, recompute intervals and actual equipment eligibility, and reconcile the exact seed union. If both phases pass, verify all **18,944 confirmation inputs / 592 full historical replays** against the unchanged live catalog. No duplicate isolated verification or new application seeds. A failed complete phase closes this trial; it does not establish floor-9 balance.

## Evidence paths

- Historical review: `TestResults/tower-floor9-gear-review-20261001`.
- Historical source: `TestResults/tower-balance-pass-floor9-reference-coverage-confirmation-study-20260929`, manifest `2d5d67eb2fad561358f6e19e1c6573d7bd0ffc4abfe9fbdcddd53f4abe73ffb4`.
- Current-catalog qualification: `TestResults/tower-floor9-gear-qualification-20261001`.
- Prepared family: `TestResults/tower-balance-pass-floor9-limited-resistance-preparation-study-20260929`.
- Trial declaration and phase receipts: `TestResults/tower-floor9-limited-resistance-driver-20261001`.
- Independent trial evidence: `TestResults/tower-floor9-limited-resistance-evidence-20261001.json`.

The repeating gear curve and approved Essence progression remain unchanged. Stay on World Tower; floors 9–10 and 12–15 and the final current-version 1–15 sweep remain until their respective checks close successfully.
