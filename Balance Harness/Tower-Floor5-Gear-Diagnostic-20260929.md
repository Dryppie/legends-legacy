# Floor 5 paired gear diagnostic — 29 September 2026

Target: primary LL Tower data and the offline Balance Harness. Explain the survival difference before declaring further calibration. No gameplay changes or new acceptance evidence are produced by this diagnostic.

**Closed on the first replay: native detailed JSON exceeded the 8 MiB log limit.** The original output archive remains immutable. One replay executed, zero complete reports verified, zero fresh seeds. The separately declared [output recovery](Tower-Floor5-Gear-Diagnostic-Recovery-20260929.md) completed all 32 fixed replays; see its findings and the joint-calibration report.

## Frozen diagnostic

- Use the complete 103-recipe / fourteen-composition source `TestResults/tower-balance-pass-floor5-gear-refinement-selection-study-20260929`, manifest `f6ce1d02e51dd3047fa0011f9f64f481da1e777735097b7ebae1712a8ee5adb1`. Its +4.5% health setting is an **unapplied candidate**, not current game content.
- Compare composition `6b30bf986f2fe6fa34e905a84a1947ceb5198bee155e191742d7005307f85dca` on resistance-and-health and health-and-regeneration. Preserve exact recipes, IDs, positions and Essence order.
- Authenticate all saved reports and summarize all 128 paired seeds. Replay the **first sixteen seeds in declared order on both profiles**, without selecting by outcome: at most **32 repeated fights, zero new seeds**. Existing exclusions remain 910,647.
- Use original preserved assemblies in `TestResults/tower-floor10-diversity-supported-build-20260929`; no rebuild. Existing `tower-loadout-replay --detailed` verifies original input, preparation and outcome. Independently require full saved-report equality after removing the added log, then reconcile recipient damage, healing, regeneration and first death to events.
- Record first-death time, actual healing/regeneration, incoming damage and attack sources, including a common first-40-second window. These paired observations explain these fights; they do not isolate every causal effect of gear or establish population win rates.
- Cap execution at 660 seconds overall, 60 seconds per replay, 8 MiB per process log and 256 MiB total. Stop on any mismatch or resource failure; do not retry or overwrite evidence.
- Output: `TestResults/tower-floor5-gear-diagnostic-20260929`. Driver: [diagnose-tower-gear.py](analysis/diagnose-tower-gear.py). Preserve archives and live Tower SHA `aebb3e9e342b777883e7a7f2642f0078f5685108a91747d85f32beb6bacf87c2`.

Use the findings to decide whether to declare a separate small health/offense calibration. Any such trial must retain all 103 recipes, both required equipment profiles, at least two actual compositions, the complete-family upper ceiling and fresh independent confirmation. No search algorithm change, dungeon work, equipment supply, migration or deployment is in scope.
