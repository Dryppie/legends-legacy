# Floor 12: isolated Power and penetration diagnostic — 2 October 2026

## Completed result: do not nominate 0.50

The full **7,712-fight / 32-seed** diagnostic completed and passed independent recount. The best limited-equipment recipes reached **14/32 and 15/32**, but the strongest twelve-item, two-healer Restoration controls reached **23/32, 22/32 and 21/32**. That overshoot does not support promoting this setting to acceptance. All 241 recipes and 131 original controls remain; no ceiling-exceeding control is removed.

| Original parent | Baseline | Best limited Resistance + Health | Full Resistance + Health |
| --- | ---: | --- | ---: |
| e8fa67a114cf… | 5/32 | 14/32, four items on slot 2 | 14/32 |
| 046ef3e3ae8a… | 13/32 | 15/32, eight items on slots 3 + 4 | 14/32 |

All **241 native preparations / 2,410 friendly participants** match the unchanged source exactly. Only guardian Power **4,393.0586 → 2,196.5293** and raw Armor/Magic Penetration **1.5840001 → 63.360004** differ; the combat cap remains 40. The isolated catalog contains only the two declared floor-12 scaling changes. The native study took **221.43 seconds**; **12 Python penetration guards**, eight participant-comparison checks and the native fixture passed. Final exclusions: **928,764**.

The subsequent [192-report review](../TestResults/tower-floor12-penetration-mechanics-review-20261002/completion.json) also completed. Median first deaths are **18 / 27 / 32 seconds** for the first baseline/limited/full comparison and **18 / 27 / 36 seconds** for the second. All 192 reports contain an unreconciled mitigation/prevention flag; recorded guardian damage totals reconcile. No extra fights or replays were used for this review.

Next is the separately frozen [three-setting refinement](Tower-Floor12-Pressure-Refinement-20261002.md): original-offense factors **0.55 / 0.575 / 0.60**, penetration fixed at 40, preserving the entire family. The 0.50 experiment is closed. **Live floor 12 remains at offense 11.9 / penetration 1; no acceptance, confirmation or application occurred.**

## Fixed scope

Test one isolated Volgrin setting: offense **11.9 → 5.95** (factor **0.50**), penetration **1 → 40**. Raw typed penetration consequently rises from about 1.584 to 63.36; the existing combat rule caps its effective value at **40 percentage points**. Keep health **9.414125**, defenses, regeneration, the complete ability kit, every other floor and all raw player builds unchanged. This is a diagnostic of existing Tower scaling fields, with no engine change or live gameplay application.

The complete unchanged-guardian [equipment diagnostic](Tower-Floor12-Limited-Resistance-20261002.md) produced **122 equipment-eligible recipes at 0/32 and six at 1/32**. The strongest full-specialization controls reached **13/32 and 10/32**. In the subsequent review of all **192 saved baseline/limited/full reports**, limited gear retains an 18-second median first death; full specialization reaches 36 seconds. Recorded damage is predominantly Magical, including party-wide Rattling Sky. The review cannot precisely attribute prevented damage because some telemetry does not reconcile.

Initial-attribute arithmetic in the [late-floor analysis](Tower-Late-Floors-Planning-20261002.md) supports pairing higher penetration with lower Power to narrow the defense advantage. The 0.50 factor lies within the 0.487–0.514 range that approximately preserves an initial magical hit against the saved fully specialized characters, while reducing the corresponding baseline hit. This is a hypothesis, not a win-rate prediction. Conditions, block, regeneration, gear tradeoffs and combat feedback remain in the native simulation.

## One complete panel

Use the unchanged, qualified **241-recipe / nine-composition** source from the completed equipment diagnostic. Retain all **131 original controls and 110 limited variants**, including all **128 equipment-eligible recipes**. Keep ten level-60 characters, tier-2 equipment and seven Essences; preserve the approved repeating gear curve, retained stronger gear, raw Essence order, actor positions and item order.

Freeze **32 fresh seeds across all 241 recipes: 7,712 fights and 32 new reservations maximum**, excluding all **928,732** prior reservations. Use the existing isolated penetration contract and current qualified runtime. Complete the entire panel once, with no interim selection, dropped controls, retry, replacement seeds, extension, pooled observations, second setting, confirmation or application.

Before allocation, authenticate the complete previous diagnostic, native preparation, current-runtime proof and saved-report review; rerun the existing penetration rejection tests. Double the observed time and archive bytes of the completed 7,712-fight source for resource admission. The time projection is **399.18 seconds**, below 80% of the native **840-second** limit. Retain the **900-second native process owner**, **2-GiB output limit**, at least **2 GiB of free-disk reserve** and an outer **1,800-second** preflight/execution/audit limit. Any failed admission stops before allocation.

After the owner closes, authenticate all archives and independently recount every outcome and exact recipe/seed schedule. Compare every recipe's native prepared participants with the original: all friendly participants must match exactly, and the only guardian attribute changes may be Power ×0.50 and raw Armor/Magic Penetration ×40. Verify the isolated catalog delta and all current source/runtime/catalog pins. Preserve any failed attempt and its reservations.

Report the limited-equipment leader of each actual composition, the two original baseline/limited/full comparisons, and the overall maximum. These 32-sample observations cannot establish acceptance. A promising fixed setting requires a separate complete-family screen and independent confirmation under the unchanged viability and ceiling gates, followed by application parity before any gameplay edit.

## Evidence

Driver: `TestResults/tower-floor12-penetration-run-20261002.py`.

Control: `TestResults/tower-floor12-penetration-driver-20261002/`.

Study: `TestResults/tower-balance-pass-floor12-penetration-diagnostic-study-20260929/`.

No migration, environment configuration change, deployment or live guardian edit is included. Floor 12 remains unresolved until separate acceptance succeeds.
