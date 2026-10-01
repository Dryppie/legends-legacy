# Floor 10: Mad King pressure diagnosis — 1 October 2026

## Completed findings

All **96 historical replays** matched their complete saved reports. An independent audit checked **582,629 events**, including recipient damage/mitigation, healing, regeneration, first casualties and event-order source attribution. No new seeds were used by this replay scope.

| Composition | Equipment | Median first death | Bloodbath share of party Health damage before first death | Mean party damage/second before → after first death |
| --- | --- | ---: | ---: | ---: |
| A: c01a33fe… | baseline | 30.0s | 76.4% | 1,628 → 1,254 |
| A | eight-item armor | 22.0s | 71.9% | 1,559 → 1,403 |
| A | full armor | 40.0s | 66.0% | 1,788 → 1,458 |
| B: 52889519… | baseline | 22.9s | 74.8% | 1,544 → 1,301 |
| B | eight-item armor | 22.9s | 71.3% | 1,572 → 1,370 |
| B | full armor | 40.0s | 64.8% | 1,795 → 1,412 |

These are sixteen saved observations per recipe, separate from the full 32-seed equipment screen. Rates divide total actual damage by total window exposure; different window lengths, ongoing effects and survival changes prevent a causal interpretation of those rates alone.

Every first casualty in the **32 limited-armor replays occurred on an unarmored character**. Cleaver caused 20, Bloodbath 11 and a basic attack one. Bloodbath caused 339 of all 480 limited-party deaths and supplied about **88% of the King's healing** over those fights. The limited parties dealt approximately **87,100–87,400** Health damage to the King, who healed approximately **31,800–31,900**. This is both distributed physical pressure and meaningful recovery feedback, rather than one protected tank solving the fight.

At first casualty, Unrestrained's native +40% outgoing/incoming modifiers were active in 18/32 limited replays and 30/32 full-armor replays. Bloodlust's net logged lifesteal contribution ranged from 0–30 percentage points in the limited comparisons and 15–35 in full armor. The archive retains all guardian status notifications without inferring unlogged condition stacks.

Armor equipment is a bundle: light-character Health rises **4,018 → 4,890**, Armor **30.24 → 60.88%**, block **42.6 → 0**, regeneration **194.13 → 136.49**, and raw Tenacity **160 → 0**. Heavy characters rise from Health **5,997 → 6,852** and Armor **51.66 → 65.16%**. Native offensive attributes are preserved in the evidence; some light slots lose 0.33 Power. Do not treat this as changing only Armor.

The evidence supports a floor-specific Power/penetration test to narrow this equipment gap. The four Mad King abilities are shared with the Ant King, so they remain unchanged. The subsequent [isolated candidate diagnostic](Tower-Floor10-Penetration-Diagnostic-20261001.md) is complete and promising; read that report for the latest continuation.

## Verification

Native replay time was **198.37 seconds**. **71 focused Python checks passed**, including 19 new casualty/selection tests. The unchanged **315 backend checks / zero skips** were authenticated and reused; no native code was edited. The subsequent candidate scope separately ran 12 penetration checks and one native study fixture. The known pre-existing Kharad behavior-manifest failure remains visible in historical regression evidence.

Independent evidence: `TestResults/tower-floor10-pressure-evidence-20261001.json`, SHA-256 `d4069af661f71eecced07c685e70f4065c898bf96f0b02c7050b2ce77d6b841b`. Logs, lossless-compression receipts and the frozen protocol are in `TestResults/tower-floor10-pressure-diagnostic-20261001`; supervision in `tower-floor10-pressure-owner-20261001`. Replay exclusions remained **927,660**. No migration, application configuration, live catalog/engine or deployment change.

## Frozen replay protocol

Target: primary LL World Tower and the offline Balance Harness. Execute the already frozen `tower-floor10-pressure-diagnostic-proposal-20261001.json` (SHA-256 `80eb74e9bf1b9aa634703c2b8b91961bdb6015e071453d14f5f443e91741c572`): **96 historical replays**, six exact recipes across the first sixteen saved seeds. The recipes are baseline, best eight-item armor and full armor for each of the two historical winners. Preserve raw Essence order, actor/item identities, positions, expected progression, all catalogs and qualified production assemblies.

No fresh fights or seeds, retry, extension, acceptance, confirmation or local gameplay application. Exact complete report equality is required after removing only the added event log. Independently reconcile recipient damage, mitigation, healing, regeneration and first deaths; verify source attribution and event-order windows. Include killing damage before the first Death event and preserve same-tick order. Report party output before and after death, separating original actors, friendly summons and guardian self-damage. Record all native prepared attributes and the guardian's logged status notifications. Net Unrestrained and Bloodlust modifier notifications describe their own contributions, not total attributes or unlogged condition stacks.

Fixed bounds: native 600 seconds, supervising owner 660 seconds, 512 MiB output, 60 seconds per replay and 16 MiB per raw log. Each verified raw log is losslessly compressed and its hash/size preserved before deleting that owned temporary log. Prior measured 96-replay cost, scaled by 1.5 for party size and doubled, projects 445.04 seconds and less than 200 MiB; admission limits are 480 seconds and 409.6 MiB, plus a 2-GiB free-disk reserve. Stop and preserve any failure without retry. Initial/final seed exclusions must remain **927,660**.

Reuse the authenticated 315 backend checks with zero skips because no native code changes. Run the new diagnostic's accounting tests and its existing reducer/compression dependencies. Independently audit all closed native reports before drawing conclusions. Select any following isolated balance candidate only after this diagnosis; no candidate or fresh study is authorized by this protocol itself.
