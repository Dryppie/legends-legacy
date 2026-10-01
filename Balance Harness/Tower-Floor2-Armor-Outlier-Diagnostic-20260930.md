# Floor 2: full-armor outlier diagnosis — 30 September 2026

**Subsequent trial applied:** The [fixed Gale 0.35 / Feast 0.65 trial](Tower-Floor2-Feast-Pressure-20260930.md) passed complete independent confirmation, native parity and post-application regressions. Its damage coefficients and descriptions are now applied locally. The diagnostic and proposed protocol below remain historical records.

Target: the primary LL World Tower and offline Balance Harness. Explain the single remaining ceiling control in the [fixed-pressure precision study](Tower-Floor2-Fixed-Pressure-Precision-20260930.md), without changing gameplay or reopening its acceptance decision.

## Frozen diagnostic protocol

1. Authenticate the precision publication SHA `088d29ef55da18ff5867d0d998a33bbc90c78ad647844bb0c84b4924716060b9`, independent evidence SHA `f299818ff85e001186f981cca31039ad56d55e175f0f65b2260ebe4ba1070d26`, all four 115-recipe screen archives and their complete aggregate. They remain rejected at isolated Gale/Feast **0.35/0.55**. Live coefficients remain **0.65/0.20**.
2. Select the exact partial armor profile `mixed-armor-baseline-slots-3-4` and full `armor-and-health` profile for D, old A and C. Recount **six recipes × 512 saved seeds = 3,072 archived outcomes**. Preserve every raw recipe, identity, Essence order, slot and budget. Require equal seed panels; compare all wins, discordant outcomes, damage, healing, threat, prepared attributes and per-character death statistics. These six explanatory views do not replace the full 115-recipe acceptance family.
3. Detailed replay selection is fixed independently of outcomes: the **first eight declared seeds from each of four batches**, for all six recipes, in batch/seed/recipe order. This is **32 saved seeds × six recipes = 192 replays**. No new reservations, extensions, retries or hand-picked wins. Use the exact five combat assemblies in `TestResults/tower-floor2-precision-runtime-20260930`; each replay uses its archive's isolated content, never the live catalog.
4. Require full equality with the saved report after removing only the newly requested event log, plus native input/result equality. Reconcile original-party recipient damage/healing/deaths and guardian recipient damage/healing. Split timelines into **0–21 seconds**, **21–42 seconds**, **42 seconds onward**, and the whole fight. Retain exposure duration, damage source and original party slot; keep summon/other-actor output explicit. Report missing exact Feast stack-consumption telemetry as unknown.
5. Before replay, admit twice the preceding 32-replay pressure diagnostic's measured time/bytes scaled to 192. Freeze a **1,200-second total replay deadline**, **60 seconds per owned process**, **64 MiB per log**, and **2 GiB total output**. Estimates must remain below 80% of the total limits. Preserve failed evidence and stop on any execution or parity failure. No raised caps or reruns.
6. Run the new descriptive-helper safeguards and existing recipient/window checks. Authenticate and reuse the unchanged 108-pass backend suite / four opt-in skips; do not count it as fresh. Native replay is the original CLI, not a new engine or fixture. Independently reread the detailed logs and outcomes, recompute summaries, verify exact selection and all hashes, and reconcile the unchanged **915,673** exclusions before publication.

The result is descriptive mechanism evidence, not a causal estimate from changing one stat or a new acceptance sample. Native identities and full recipes can differ between distinct compositions; within each composition the two compared recipes differ only in equipment. Whole-fight totals depend on fight duration; fixed-window results retain their exposure duration. Detailed-panel win counts are illustrative and never replace the complete 512-seed comparison. Any proposed adjustment after diagnosis requires a separately frozen trial and independent acceptance. No search redesign, dungeon, supplies, acquisition, migration, configuration or deployment work is included.


## Completed diagnosis

**Verified descriptive diagnostic.** Recounted **3,072 saved fights** and matched **192 detailed replays**, covering all six declared recipes and all four source batches. The full 512-seed outcomes remain:

| Composition | Two armored characters | Full armor | Net full-armor gain |
| --- | ---: | ---: | ---: |
| D | 143/512 | 251/512 | +108 |
| A | 121/512 | 187/512 | +66 |
| C | 121/512 | 180/512 | +59 |

D full armor wins 150 seeds where A full armor loses, while A wins 86 where D loses: a net **64/512** advantage. Against C full armor, D gains 143 and loses 72: net **71/512**. D's partial-gear advantage over either is only **22/512**. Full armor helps every composition, but helps D most in this saved panel. These are paired descriptive comparisons, not a new acceptance test.

### What explains the difference

**D and A differ only in the two slot-1 Essences.** The raw scenarios, identities, equipment and other characters match after substituting D's `venomous_snake` + `viper` back to A's `alpha_wolf` + `elder_treant_thornstorm`. Their complete prepared slot-1 attribute dictionaries are identical: full-armor MaxHealth **2,115**, Armor **47.30%**, Power **30.36**. The advantage therefore comes from the ability package and ensuing combat trajectory, not an unequal attribute or equipment budget.

**The offensive difference is visible during the middle of the fight.** Across the fixed 32-seed detailed panel, mean actual health damage to Velka is:

| Recipe | 0–21 seconds | 21–42 seconds | Slot-1 contribution, 21–42 seconds |
| --- | ---: | ---: | ---: |
| D full armor | 1978.0 | 2318.4 | 487.8 |
| A full armor | 1950.0 | 2179.1 | 392.9 |
| C full armor | 1961.8 | 2236.8 | 429.2 |

D's 21–42-second damage exceeds A's by **139.3**, of which slot 1 contributes **94.9**. The observed package includes Poison and Viper's extra strike against a Poisoned target; the diagnosis does not isolate the individual effects of the two substitutions. Across all 512 fights, D full armor deals mean **5,747.9** reported damage versus A **5,664.6**, while receiving **1,932.7** healing plus regeneration versus A **2,068.9**. Its first party death occurs at mean **21.80 seconds**, versus A **22.04**. Greater sustain or a substantially later first death does not explain D's advantage.

**Full armor keeps supporting damage alive longer.** In D's full 512-seed comparison, slot-2 deaths before 42 seconds fall from **199 to 83**, and slot-5 deaths from **157 to 47**, when moving from the partial profile to full armor. Slots 3 and 4 already have armor in both profiles. In the detailed panel, full armor adds only **30.2** boss health damage in the first 21 seconds but **144.8** in seconds 21–42 and **385.7** after 42 seconds. The latter includes a longer observed fight: **23.58** versus **19.27** seconds of late exposure. This supports pressure on the surviving party later in the encounter, while preserving the already tested opening.

**Feast is an available pressure adjustment, not a proven solution.** In the detailed panel, D full armor receives mean **2,407.4** health damage from Feast across the whole fight, versus **1,817.4** with partial armor; after 42 seconds the figures are **1,501.9** and **879.8**. There is no Feast damage before 21 seconds in this selected panel. Whole-fight totals include different durations, Bleed states and survival, so these differences must not be treated as a predicted response curve. Exact consumed stack counts remain unknown; condition-instance removal logs do not count partially consumed stacks.

The detailed sample has only 32 seeds per recipe: D/A/C full armor win **15/12/12**, while partial armor wins **8/6/10**. It supports timing and attribution, not a replacement for the complete 512-seed outcomes. D full armor remains rejected at **251/512**, adjusted upper **56.73%**; this analysis neither proves its true win rate exceeds 50% nor changes the acceptance rule.

## Recommended next trial

**Test one fixed candidate: Gale 0.35 / Feast 0.65.** Keep the tested opening coefficient fixed and increase Feast damage from the rejected candidate's **0.55 to 0.65** (about **18.2%**). The evidence points to damage and survival through the middle/later fight. The +0.10 magnitude is a bounded hypothesis to test, not a fitted or guaranteed passing value. It may also suppress partial-gear teams, so the same two-composition minimum remains mandatory.

Prepared proposal: `TestResults/tower-floor2-feast-pressure-proposal-20260930.json`, SHA **`bdb20113cb2217ba4e026d614476c0ef6ca274e192d7d4aa7d7f65be47090dd2`**, status **`ProposedNotAllocated`**. It allocates **zero fights and zero reservations**. Use the existing aggregate guards with a separately frozen protocol/driver and fresh paths: one four-batch **512-seed × 115-recipe screen**, then only if it passes one independent equivalent confirmation. Maximum **117,760 fresh fights / 1,024 reservations**; gates remain **76–216 wins / 512**, with two actual partial-gear compositions and every full-family upper bound ≤50%. Keep all previous seeds excluded and all 115 recipes retained. No adaptive sweep, extension, dropped outlier or pooled historical observations.

The proposed isolated v2 coefficient plan is validated against the unchanged live-catalog source. It would change only Gale/Feast damage coefficients and corresponding descriptions. Keep guardian health/offense **2.6618600366 / 2.5333570679**, Dive **1.60**, cooldowns, targeting, Bleed, Scent, Feast's stack limit and healing parameters. Realized healing can still change with the combat trajectory. Any application requires complete independent confirmation and native input/replay parity first.

## Verification and retained state

**22 fresh Python safeguards passed**: ten new paired/recipient/window checks and twelve existing mixed-armor checks. The unchanged **108 backend passes / four opt-in skips** were authenticated and reused, not rerun. All **192 original CLI replays** matched saved native inputs and complete outcomes after removing only the added event log. Independent collection reread the saved outcomes and detailed logs, checked pairing, recipient totals, source/slot attribution, full-family manifests, process receipts and the unchanged seed history.

Replay processes used **262.14 seconds** in total; archive size **403,142,914 bytes** and largest log **2,559,060 bytes**, within the declared bounds. Every process exited successfully with zero active children; no retries. Exclusions remain **915,673**. All 102 live JSON catalogs, combat binaries, source archives and prior accepted floor-5/floor-6 changes are unchanged. No new acceptance fights, seed reservations, migrations, configuration changes, database actions or deployment. No required verification is blocked.

Maintained additions are `analysis/diagnose-tower-armor-outlier.py` and its ten-test companion. New ignored drivers, collector, frozen replay logs, receipts and next-candidate proposal are under `TestResults`. This report, the previous precision report, status, handoff and both harness guides record the result. No engine, C# test, ability-candidate helper or aggregate contract changed in this diagnostic.

## Evidence

- Independent evidence: `TestResults/tower-floor2-outlier-evidence-20260930.json`, SHA **`cd8f50cf5feee55d559c98cc12e69e04be4aecb6e113040b2e5ec7ff5acd386a`**.
- Diagnostic archive: `TestResults/tower-floor2-outlier-diagnostic-20260930`, manifest SHA **`b04a715bd0cfd13446940dff4c87d6ba7daefb7fe2c109ed1d177697542b29a8`**. It contains the immutable protocol, declarations, all 192 attempt/process/log records, saved summaries, paired counts and event-window details.
- Original 512-seed aggregate evidence remains `TestResults/tower-floor2-precision-evidence-20260930.json`, SHA **`f299818ff85e001186f981cca31039ad56d55e175f0f65b2260ebe4ba1070d26`**, status `PrecisionScreenNotAccepted`.
- Latest reservation ledger remains `TestResults/tower-balance-pass-floor2-precision-screen-4-owner-20260929/seed-ledger.json`, SHA **`b4ddc09bd4c9dae5b7f9ca187f581f88e36750eb7cea3aca19222bb07ed56f38`**, plus all ancestors/later reservations.
- Live-catalog recipe source remains `TestResults/tower-balance-pass-floor2-two-armor-expanded-screen-study-20260929`, manifest **`b8e1f3b5bd00c1feac0035976c10f8960d12e1ea03d6a8378ee73baeb20d9862`**. The rejected precision archives are diagnostic sources, not implicit live-catalog sources.
- Tower SHA **`0d416f3cbbf24f7b856fad129e13efcf8ed4a9d5c7677d4bc4b6c51a5061c5e6`**; abilities SHA **`fe9f03f9f6b35e49d8dc589199d690c930a781afb5335bd2736598a091c368db`**. Live Gale/Feast remains **0.65/0.20**. Runtime remains `TestResults/tower-floor2-precision-runtime-20260930`.
