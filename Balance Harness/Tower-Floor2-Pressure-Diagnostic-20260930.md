# Floor 2: opening and later pressure diagnostic — 30 September 2026

Target: primary LL World Tower and offline Balance Harness. Explain the gear gap in the two new nominees from the [two-character armor search](Tower-Floor2-Two-Armor-Search-20260930.md). This is a descriptive investigation using existing outcomes, not a balance acceptance panel.

## Frozen diagnostic protocol

- Source: the complete 115-recipe expanded screen, manifest `b8e1f3b5bd00c1feac0035976c10f8960d12e1ea03d6a8378ee73baeb20d9862`. Authenticate its archives, current publication, all runtime/catalog pins and the **914,265**-seed exclusion union.
- Select both generated nominees C `5a063eba5f7bbbb419a5fd25fb0958d5909b0b3efaf1c8c8c390cb2db96b9e90` and D `34497de3dcfec2b2b8f551acc8be20721fbba47e097128dab532218c1d012e0d`, each at exact gear `mixed-armor-baseline-slots-3-4` and `armor-and-health`. Require equipment-only differences within each pair, preserving every identity, position, raw Essence order and budget.
- Recount all **512 saved fights** for those four recipes. Replay the **first eight declared seeds**, in original order, for all four recipes: **32 detailed replays**, regardless of outcomes. No new seeds, acceptance samples, replacement samples or retries.
- Use the original hash-checked BalanceHarness CLI. Require full saved-report equality after removing only the detailed event log, and reconcile recipient damage, healing and deaths against saved statistics.
- Measure guardian damage by source before 21 seconds, from 21 to before 42 seconds, and over the full fight; record first deaths, Gale/Feast damage timing and Feast damage recipients per tick. Count explicit depleted Bleed-instance logs separately. The engine logs removal only when an instance reaches zero, so these logs **cannot establish exact consumed stack counts**. Do not label instance removals as stack counts or infer stacks from damage after mitigation, variance and critical hits.
- Cap the scope at **32 replays, 600 seconds, 60 seconds per replay, 64 MiB per log and 512 MiB total**. Save declarations, original commands, owned process receipts and raw logs in a new exclusive archive; verify zero active child processes and unchanged catalogs, runtime and seed history at closure.

Use this evidence to decide whether a finite joint Gale/Feast damage-coefficient trial is worth testing. Preserve Feast stack count, healing, cooldowns, target selection, Bleed, Dive and guardian scalars. Any fresh trial needs its own prospective coefficients and selection rules, all 115 retained recipes, unchanged two-composition/eight-item and whole-family ceiling requirements, independent confirmation and application verification. No live edits are authorized by diagnostic success alone.

## Completed findings

**All 32 detailed replays matched the complete saved reports**, and all **512 original fights** were recounted. No seeds or acceptance samples were added. The archive used **67,566,892 bytes**, with **43.59 summed owned-process seconds**; every process exited with zero active children. The complete 115-recipe source was independently authenticated and its outcomes reconstructed. Runtime, catalogs and the **914,265**-seed exclusion union were unchanged at diagnostic closure.

| Nominee / gear | Mean damage before 21 s | Mean Feast damage, 21–42 s | Mean recipients at first damaging Feast | Mean Feast damage, entire fight |
| --- | ---: | ---: | ---: | ---: |
| C / two armored | 3,620 | 389 | 2.00 | 605 |
| C / full armor | 3,492 | 480 | 2.88 | 879 |
| D / two armored | 3,628 | 325 | 1.63 | 563 |
| D / full armor | 3,441 | 503 | 2.88 | 833 |

Each row is the same predeclared eight-seed prefix, with five original characters. The first damaging Feast occurs at **21 or 26 seconds**; median first death is **20 seconds** in every row. Before 21 seconds, Gale direct damage averages **1,096 / 934** for C with two/full armor and **1,071 / 882** for D. Feast cannot cause damage before its first activation. During seconds 21–42, it contributes more health damage against the full-armor variants in this sample despite their stronger mitigation; it also reaches more recipients on the first damaging cast.

This supports testing the proposed damage redistribution, but does not predict acceptance. Whole-fight totals are affected by survival time and later target availability. Fixed windows reduce that difference without isolating mitigation or proving causation: the damage, deaths, RNG consumption and targeting trajectories can diverge between gear variants. The detailed subset wins are C **0/8 versus 2/8**, D **1/8 versus 3/8**, while the full source panel is C **12/128 versus 41/128**, D **18/128 versus 59/128**. Do not replace the full results with the small replay subset.

**Exact consumed-stack counts remain unavailable.** There were zero observed fully depleted Bleed-instance removal logs in these replays, while Feast plainly dealt damage. The engine consumes individual charges but logs removal only when an instance reaches zero. It computes damage from Power × damage coefficient × consumed charges, separately calculates healing, and caps that healing across the cast. Neither zero depletion logs nor damage totals establish zero consumption or a precise consumed-stack count. This limitation does not prevent a controlled damage-only coefficient trial.

## Follow-up and evidence

The separately frozen [joint Gale/Feast trial](Tower-Floor2-Gale-Feast-Trial-20260930.md) tests **0.50/0.40, 0.50/0.60 and 0.50/0.80**, retaining all 115 recipes. Those finite trial magnitudes are hypotheses informed by this diagnostic and the earlier failed Gale-only reduction. They are not accepted gameplay settings.

- Diagnostic archive: `TestResults/tower-floor2-pressure-diagnostic-20260930/files.json`, SHA **`33f9170b8417318dda7081bfa2c25f9228e7397f9946779f80c9b65bdf320992`**.
- Independent evidence: `TestResults/tower-floor2-pressure-diagnostic-evidence-20260930.json`, SHA **`8a205d5681e2d87d2c0d426959c61d85d667da21f4dea31afeaf9562f681558f`**.
- Owner and collector: `TestResults/tower-floor2-pressure-diagnostic-driver-20260930.py` and `TestResults/tower-floor2-pressure-diagnostic-collect-20260930.py`. The archive preserves the original protocol, declarations, commands, detailed logs, per-replay process receipts and reconstructed summaries.

No maintained diagnostic implementation, game data, C# runtime, migrations, configuration or deployment changed in this diagnostic. Full-report parity and an independent raw-log recount are its verification; candidate-helper safeguards and fresh Velka tests belong to the separate trial.
