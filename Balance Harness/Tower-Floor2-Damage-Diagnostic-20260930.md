# Tower floor 2 damage diagnostic — 30 September 2026

Target: primary LL World Tower and offline Balance Harness. Investigate the failed eight-specialized-item target from the [mixed-armor comparison](Tower-Floor2-Mixed-Armor-20260930.md). Keep all live guardian data, progression budgets, supported search and accepted floor-5/floor-6 changes unchanged.

## Frozen diagnostic protocol

- Authenticate the complete 98-recipe floor-2 screen, manifest `0d0be643c70c46fae49e735c8d303571e3e59769892248e06d77e202e91bd3bc`, and original proposal `e8f85582d948ec8ddd9930feae5ef124ab1d41963ef12eb3afeae66a5bb16f68`. Preserve all raw recipes and original archives.
- For each of the two previously selected parent compositions, compare its baseline, strongest two-character armor subset, strongest four-character subset and full armor. Select subset leaders by wins, then lowest remaining guardian health, then ID, exactly as in the preceding report. These eight recipes are selected for explanation, not a new acceptance claim. Recount all **1,024 saved fights** (eight recipes × 128 seeds), including per-recipient statistics, deaths and prepared attributes.
- The saved reports have no event timeline. Replay the **first eight declared seeds in their existing order for all eight recipes: exactly 64 historical replays**, using the original `TestResults/tower-floor6-partial-restoration-build-20260930` assemblies and `tower-loadout-replay --detailed`. No outcome-dependent seed selection, new seeds, new acceptance samples, altered recipes, candidate abilities or search.
- Require every replay to reproduce its original native input and complete saved report, excluding only the newly captured event log. Reconcile event health damage, healing, regeneration, first deaths, raw incoming damage and typed mitigation against each original recipient's statistics. Exclude summons from original-party totals. Compare fixed **[0,15-second)** and **[0,20-second)** windows, source/recipient breakdowns, last damage before first deaths, three-second death context and guardian ability/condition events. Treat whole-fight totals as duration-dependent and the sample as descriptive.
- Bounds: **64 replay attempts, 60 seconds per replay, 1,200 seconds overall, 64 MiB per log, 2 GiB total**. Stop on mismatch or resource failure; preserve evidence without retry, resume or automatic expansion. Use owned-process cleanup and verify no active descendants remain. Record every attempt and output. This allowance incorporates the earlier floor-5 verbose-output experience.
- Freeze this protocol, implementation, source, runtime, live catalog and ledger hashes before the first replay. Exclusions start and finish at **913,772**. Afterward independently reconstruct summaries from the captured logs and verify immutable archives, unchanged catalogs and zero seed allocations. Run focused Python diagnostic safeguards; reuse authenticated unchanged backend regression results. No C# change or backend rebuild is planned.

Use the results to recommend the next bounded balance investigation. Do not infer a successful ability adjustment from diagnostic replays or silently replace the two-character target with four characters. Any prospective candidate and fresh screen/confirmation require their own frozen protocol and retained-family ceiling.

## Completed findings

**All 64 detailed replays matched their complete saved reports.** The diagnostic also recounted all **1,024 saved fights** in the eight selected recipes. It allocated zero new seeds and changed no game data. Archive size: **117,918,036 bytes**; summed owned-process time: **76.62 seconds**. Every process exited with zero active descendants. Exclusions remained **913,772** at diagnostic closure; later trial reservations are separate.

The original full-panel results remain baseline **1/128 and 1/128**, two armored characters **12/128 and 9/128**, four armored **27/128 and 27/128**, and full armor **41/128 and 29/128**. Median first death across all 128 seeds was **17.9 / 15.7 seconds** at baseline, **20.0 / 20.0** with two armored characters, **20.9 / 21.0** with four and **21.0 / 21.0** with all five. These are descriptive measurements, not a new acceptance panel.

### Opening damage and deaths

Across all 64 detailed replays, the first Rending Dive dealt damage at **10 seconds** and the first Crimson Gale at **13 seconds**. The first Dive's target was among the first characters to die in **63/64** replays. Slots 2–5 took most of that early focus; simply protecting the character in slot 1 does not absorb Velka's lowest-health attacks. The source code confirms selection by health percentage, subject to forced-target rules.

| Equipment | Mean health damage before 15 s: A / B | Original-character deaths before 15 s: A / B |
| --- | ---: | ---: |
| Baseline | 2,899 / 2,943 | 5 / 4 |
| Two armored characters | 2,558 / 2,783 | 1 / 2 |
| Four armored characters | 2,330 / 2,349 | 0 / 0 |
| Full armor | 2,310 / 2,277 | 0 / 0 |

Each column uses the same first eight declared seeds. Death counts are across those eight fights, not percentages. Some baseline and two-character fights already contain deaths inside the fixed window, so even this window includes survival and targeting effects; it is not a pure mitigation counterfactual. The sample is deliberately fixed and small. For example, A/full-armor lost all eight detailed samples despite winning 41/128 in the complete screen; no outcome-dependent replacement seeds were selected.

For the two-character recipes, Crimson Gale's direct hit plus its attributed Bleed contributed **1,084 / 1,185 health damage before 15 seconds**, or **42.38% / 42.56%** of all health damage. Rending Dive plus its Bleed contributed **748 / 823**; Basic Attack contributed **726 / 776**. Gale's direct component alone was **991 / 1,061**. This establishes a substantial party-wide hit after the initial lowest-health strike. It does not establish that lowering Gale will satisfy the gear target or preserve the full-armor ceiling.

Feast on Wounds first dealt damage at **21 seconds or later** in this sample, so it cannot explain the observed deaths before 15 seconds. It becomes relevant later, especially after better-protected parties survive to its first cast. Scent of Weakness triggers when someone is wounded; the engine's Haste condition increases Basic Attack rate by 25%, rather than shortening these active ability cooldowns. Repeated Haste application log entries are not separate multiplicative speed stacks.

### The equipment tradeoff

The saved armor-and-health profile changes several attributes together. On slots 2–4, armor rises from **13.25% to 40.97%**, and health from **1,410 to 1,638**. On each specialized character, block chance falls **14.04 → 0**, regeneration **48.8 → 34.76**, and Tenacity **56.16 → 0**. Character identities, Essence order and offensive attributes remain unchanged. Therefore this experiment measures that complete equipment tradeoff, not armor alone.

The engine applies armor mitigation to both Physical and Bleed damage. Tenacity separately resists harmful applications. Full armor can therefore sustain more total Bleed events through lower resistance to application and longer survival; larger whole-fight Bleed totals do not mean armor increases the damage of an otherwise identical Bleed tick. Haste timing, target selection, resisted applications and RNG consumption can diverge between gear variants despite paired seeds.

## Chosen balancing follow-up

Test **Crimson Gale direct damage 0.65 → 0.50** in one separately frozen isolated scope, preserving its Bleed, cooldown, targeting, Rending Dive, Feast, Scent and all guardian scalars. This probes the party-wide portion of the opening sequence while retaining Velka's hunting mechanic. It is a test magnitude, not a predicted optimum. The [Gale trial](Tower-Floor2-Gale-Trial-20260930.md) defines a new full-family screen and conditional independent confirmation, retaining all 98 recipes and the at-most-two-armored-character target. It must still reject a setting that makes any fully armored control too strong. Diagnostic replays contribute no acceptance samples.

## Verification and files

The new `analysis/diagnose-tower-mixed-armor.py` selects exact matched recipes, replays a fixed seed prefix and reconciles recipients, windows and source damage. The existing `analysis/diagnose-tower-gear.py` now accepts an explicit expected party count while retaining its ten-character default. **17 fresh Python safeguards pass**: twelve new selection/accounting/window tests and five existing diagnostic tests. The unchanged **97 backend passes / four opt-in skips** were authenticated and reused; no C# rebuild was needed. Native historical replays used the saved BalanceHarness CLI, not a substitute combat model.

The independent collector re-read all captured logs and original reports, recalculated damage windows and ability attribution, checked recipient totals, process cleanup, resource usage and zero seed allocation, and authenticated all source/runtime/catalog pins. No verification remains blocked. No migration, configuration change or deployment. Reports and continuation guides record the diagnostic separately from the subsequent fresh candidate trial.

- Diagnostic archive: `TestResults/tower-floor2-damage-diagnostic-20260930/files.json`, SHA **`04493373d757043963f9597040a60642d92a71ed33d07ae9052fa0b7ce05be6c`**.
- Independent evidence: `TestResults/tower-floor2-damage-diagnostic-evidence-20260930.json`, SHA **`c738a2876e33a06d7de33415b073471c54efcded47c7afb1e18e79f3fe8cefe4`**.
- Frozen protocol, declaration, saved summaries, all 64 attempt/process receipts, raw detailed logs and reconciled details remain inside the diagnostic archive. Do not overwrite or rerun that output directory.
