# Tower floors 1–3 and 7–9 — calibration, 29 September 2026

Target: primary LL game Tower guardian data and offline Balance Harness. Continue the [confirmed floor-4/6 pass](Tower-Floors4-6-Balance-Pass-20260929.md), preserving floors 4–6, the user's repeating gear curve and the supported `affinity-creation-with-benchmark-validation-v1` search. Hypothetical benchmark ownership does not establish ordinary-player acquisition. Selectable dungeon supplies remain withdrawn.

## Prospective scope

| Floors | Characters | Level | Essences per character | Gear |
| --- | ---: | ---: | ---: | --- |
| 1–3 | 5 | 30 | 4 | Tier 1, Rare / Standard / rank 2 |
| 7 | 5 | 40 | 5 | Tier 1, Unique / Exceptional / rank 4 |
| 8–9 | 10 | 40 | 5 | Tier 1, Unique / Exceptional / rank 4 |

Baseline rolls, no styles, unascended/unevolved Essences. Preserve ordinal Essence order and native identity/position semantics; do not search permutations. Three retained compositions × seven gear profiles form the initial 21-cell family. Stronger gear profiles stay in the family. No new acquisition or reward implementation is in scope.

For each floor, in order 1, 2, 3, 7, 8, 9:

1. Prepare the full party through native rules, capture current content, and screen the original setting with 32 fresh seeds per cell (672 fights). Finish application of each floor before capturing the next one.
2. If needed, bracket a nontrivial setting with joint health/offense factors: double an easy setting or halve a hard setting within 0.25–16×; refine by midpoints. Prefer a strongest observed win rate of 15–35%, near 25%. Cap initial selection at twelve screens including the original. Do not assume other floors' factors or gear profiles transfer. These observations are selection evidence, not acceptance evidence.
3. Run the unchanged 528-fight supported affinity search from the three measured references at the strongest gear profile, then evaluate all five nominees on 128 separate seeds (1,168 total fights). Retain both generated finalists at all seven profiles, producing a fixed 35-cell family.
4. Screen that whole family at the candidate setting. If needed, continue bracket/refinement for at most eight expanded-family screens, with the same scalar limits and selection range. Retain every candidate archive, even unsuccessful ones. Freeze the selected setting and all 35 combinations before confirmation.
5. Confirm on 256 fresh seeds per cell (8,960 fights). Use the existing approximate simultaneous 95% Bonferroni-Wilson intervals separately per floor: every upper bound ≤50%, at least one lower bound ≥10%. Report every floor, including failures/inconclusive results. Do not pool phases, extend a confirmation, discard cells, or quietly retry a failed confirmation. A failed/inconclusive floor stays unapplied pending a separately declared follow-up.
6. Apply only a passing setting; check all 8,960 native inputs and one full archived battle per cell against the applied content. Record pacing and composition/gear diversity separately from acceptance. No whole-Tower, universal-build or Essence-necessity claim follows from these bounded families.

Every phase uses a fresh owner/output pair, the existing 900-second process / 840-second native limit, 2 GiB native output cap and at most 20,000 fights. Build via `build/run-tests.ps1` into `TestResults/tower-floors1379-build-20260929`. Preserve all **887,616** inherited exclusions and every subsequent balance-pass reservation, including unused ones. No deployment, shared database, migration, dependency, configuration or search-policy change.

Entry Tower SHA-256: **`44a87b7872265bfc55c4253bbc83a9577f6acd287e5d7cff80100b7776d55830`**. Entry ledger: `TestResults/tower-balance-pass-floor6-calibration-confirmation-owner-20260929/seed-ledger.json`, SHA **`9025d61a1a3a18a58069d4f8ed586c328942908333e9cf9ba6584e68d18b04e8`**. Old archives are immutable and local; later applications must preserve earlier floors' confirmed fields.

## Execution and interpretation

The expanded native regression suite passes **64 tests**, with three intentional opt-in skips and no failures. Projection support now admits floors 1–9 while preserving existing version strings and policies. Preparation tests assert each floor's full party, rank/rarity/quality, unascended Essences and ordinal order before any study fights.

The local [selection driver](../TestResults/tower-floors1379-selection-driver-20260929.py) executes the prospective scalar protocol through the existing bounded owner; it does not change the supported composition search or write production content. Each floor gets a new control directory with the driver hash, per-phase declarations/logs and frozen selection. Expanded-family bracketing starts afresh: a hard setting for the three retained compositions is not assumed hard for generated teams. Final application remains a separate verified step.

Floor 1's original-setting screen completed 672 fights and exposed multiple **32/32** routes. Continue the declared bracket at joint factor **2×**, keeping every profile and composition. This is selection evidence only.

Floor 1's initial bracket selected **1.375×** after five 21-cell screens. At that setting the supported search found generated teams with **82/128 and 60/128** wins, versus a strongest retained result of **29/128**. Six expanded-family screens retained all 35 combinations and selected **1.4609375×**; the strongest screening result was **6/32**. That setting and family were frozen before the 8,960-fight confirmation. Both search finalists remain included, with no Essence-order variants.

### Floor 1 inconclusive result and separate follow-up declaration

The first confirmation completed all 8,960 fights and returned **Inconclusive**. Its strongest result was **26/256 (10.16%)**, which did not establish the 10% lower-bound threshold; the next results were 13/256 and 11/256. No floor-1 data change was applied. Retain the original confirmation, audit, manifest and all reserved seeds. It is not extended, retried or pooled.

Declare one separate follow-up at a **different setting**, starting at joint factor **1.41796875×**, halfway between the earlier easy 1.375× and the inconclusive 1.4609375×. Keep the exact same 35 compositions/profiles. Use at most four new 32-seed screens, narrowing that bracket if needed, preferring **7–10 wins out of 32** rather than the low edge of the earlier window. Freeze one selected setting before at most one additional 8,960-fight confirmation. Report both confirmations regardless of the second verdict. Apply only if that fresh panel passes; otherwise floor 1 remains unapplied. This is a new declared calibration step, not reuse of the closed confirmation.

Before any floor-2/3/7/8/9 study starts, tighten their expanded-family selection window to **7–10/32** to leave more room above the lower acceptance threshold. Initial reference selection remains 15–35%; all phase/fight limits, family sizes, search and confirmation rules remain unchanged. The [v2 local driver](../TestResults/tower-floors1379-selection-driver-v2-20260929.py) implements only that prospective window change. Preserve the original driver and floor-1 evidence.

The four floor-1 follow-up screens tested factors **1.41796875, 1.396484375, 1.4072265625, 1.41259765625**, with strongest rates **6/32, 13/32, 11/32, 9/32** respectively. Freeze the last setting for the single separate follow-up confirmation, retaining all 35 cells and excluding every prior seed. Its two strongest health-and-regeneration teams screened at 9/32 and 7/32; this is not yet acceptance evidence.

The separate floor-1 follow-up confirmation **passed**. The two generated health-and-regeneration teams won **92/256 (35.94%)** and **61/256 (23.83%)**, with adjusted intervals **27.08–45.87%** and **16.44–33.21%**. The first team also establishes viability in armor-and-health gear: **42/256**, interval **10.34–25.04%**. All 35 upper bounds are below 50%. This is three viable cells across two compositions and two profiles; the earlier 26/256 result remains a separate inconclusive setting.

Floor 1 is applied locally at **health 2.5101634336 / offense 2.4508682344**, preserving every other guardian field. The checked application must complete before floor 2 captures its content. The strongest two combinations' mean engine durations are 76.61 and 75.84 seconds across wins and defeats; these are not player-time measurements.

### Floor 2 selection

Floor 1's application check matched all **8,960 inputs and 35 full reports** before floor 2 was prepared. Floor 2 initially had multiple 32/32 routes. Seven initial screens selected **1.28125×**, with retained armor-and-health teams at 7/32. The supported search's separate evaluation produced stronger teams at **63/128 and 56/128**, versus a strongest reference at 35/128. Eight expanded-family screens selected **1.30126953125×**, with strongest results **10/32 and 8/32**. Freeze that full 35-cell family for the independent confirmation.

The local [application/continuation helper](../TestResults/tower-floors1379-apply-and-continue-20260929.py) can apply completed passing studies in floor order. It first requires a passing result, checks that substituting only the target floor's health/offense exactly reproduces the confirmed whole-Tower JSON, records the previous values and source, then invokes the existing input/full-report application verifier before preparing the next floor. It stops on any incomplete, failed or inconclusive study. No guardian is changed solely from a screening result.

Floor 2's fresh confirmation **passed**. Its strongest generated teams won **82/256 (32.03%)** and **63/256 (24.61%)**, intervals **23.57–41.86%** and **17.10–34.05%**. A retained composition also establishes viability at **48/256 (18.75%)**, interval **12.22–27.67%**. All three viable cells use armor-and-health gear; all 35 upper bounds remain below 50%. Apply **health 2.6618600366 / offense 2.5333570679**. All 8,960 native inputs and 35 complete battle replays match. Mean durations for the strongest two are 68.12 / 69.32 seconds.

### Floor 3 selection

Floor 3 also had multiple 32/32 routes at its original setting. Three initial screens selected **1.5×**. The supported search's generated teams won **49/128 and 27/128**, versus the strongest retained team's 26/128. Eight expanded-family screens selected **1.5234375×**, with strongest results **9/32 and 8/32** in health-and-regeneration gear; resistance-and-health also had 6/32. Freeze this setting and all 35 cells before fresh confirmation.
