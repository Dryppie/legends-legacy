# Floor-10 five-Essence controls and acquisition audit — 28 September 2026

Target: the primary game's offline Balance Harness. Follow the [completed carried-equipment application](Tower-Carried-Equipment-Checked-Application-20260928.md) with controlled lower-Essence coverage on floor 10 and a static audit of the ordinary equipment acquisition channel. Keep the requested repeating equipment curve, supported search and applied boss values.

## Frozen screen

Retain all **21 floor-10 references** from the [fresh confirmation](Tower-Floor10-Family-Confirmation-20260928.md): authored, repeated and alternating parents, each with all seven gear profiles and all 15 characters. Source manifest: `95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944`.

For every parent/profile, remove each original Essence position 1–6 uniformly across the party. Preserve the order and instance identities of every surviving Essence, character identities, gear identities, specializations, positions, quality, rank and rolls. Compare the resulting five-Essence party at **level 50 / tier 2** (isolating the removal) and **level 40 / tier 1** (a legal earlier-progression budget). Tier 2 requires level 50, so the latter cannot retain tier-2 stats. Both use the same Legendary/Masterpiece/rank-5 assumptions; ownership remains hypothetical. The 126 controls per level plus 21 references give **273 cells**. These uniform removals are systematic local controls, not an exhaustive five-Essence search.

Use exactly the **first 32 historical seeds**, in order, from the 256-seed floor-10 confirmation panel. On a separate current build, first prepare all 273 cells without combat and reconstruct **all 5,376 original input hashes**. Replay the first 32 reports of every reference: **672 complete input/report matches**. Only after every match passes may the **252 controls × 32 = 8,064** additional historical diagnostic fights begin. Total **8,736 fights**, with zero fresh seeds, retries, extensions, recipe selection or boss tuning. The exclusion union remains **835,319**.

Record every cell's wins, draws, mean guardian health, duration, and gained/lost wins against its exact six-Essence parent on paired seeds. A control at **4/32 or more** is an observed lower-Essence concern, using the existing descriptive 10% screen threshold. Otherwise record no observed breach. Neither outcome establishes balance acceptance or universal Essence necessity; no historical results enter a new confidence estimate.

Bounds: **1,200 seconds** native preparation/combat/reconstruction, **1,260 seconds** owned process, **2 GiB** output, **8,736 maximum fights**. Compilation, ordinary tests and independent read-only audit are separate. Freeze current source/runtime/content hashes and the exact schedule before launch. Stop on failure, retain partial evidence and never resume or retry combat. Authenticate the complete archive and independently reconstruct recipes, paired outcomes, source report parity, acquisition arithmetic and process accounting afterward.

Pin the current floor file to `5fdb290f74b71401a1ca56f7d89be49505077c9846ba139522e533c259947f05`: floor 10 **12.71 / 7.13**, floor 11 **17.94375 / 23.0175**. Other captured content must match the confirmation. No gameplay content, settings, migration, database, deployment or restart changes are part of this screen.

## Acquisition scope

For each of the 21 original recipes, load the production ordinary-drop catalog and reinforcement prices. Check every exact gear definition belongs to the ordinary Legendary pool; count actual items and occupied slots; calculate rank-1→5 reinforcement costs from the authored tier prices. Report the probability of **any** Legendary/Masterpiece item per Champion equipment drop and per completion at mastery 0 and 10. The item-count/probability calculation intentionally ignores fit, specialization, duplicates, style, stat rolls and ownership. It describes an optimistic count for that channel, not the expected time to acquire the specified loadout.

This audit does not model all quest/event/trading channels, dungeon access, sigils, successful clears, resource income or player schedules. Rank reinforcement does not upgrade rarity or quality. Reference rolls fixed at 1 are evaluation assumptions, not a guaranteed random award. The screen therefore remains hypothetical ownership evidence even when all definitions have a legal ordinary drop path.

This protocol was written before any screen combat. Preparation, execution, audit results and the resulting recommendation are recorded below after completion.

## Implementation and preparation

- [TowerEssenceAblation.cs](../LL/tools/BalanceHarness/TowerEssenceAblation.cs) creates the complete set of ordered removals while preserving surviving identities. The [reference builder](../LL/src/Infrastructure/Service/Services.LL/PowerRatings/EquipmentReferenceBuildFactory.cs) adds optional original Essence indices and an optional original tier to its existing identity pin. Null fields retain historical serialization and identities; actual level/tier/Essences still determine legality and combat stats.
- [TowerEquipmentAcquisitionAudit.cs](../LL/tools/BalanceHarness/TowerEquipmentAcquisitionAudit.cs) reads production ordinary acquisition rules and reuses the existing occupied-slot reinforcement calculation.
- [BalanceHarnessFloor10ControlTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessFloor10ControlTests.cs) tests legal preparation, every removal position, surviving identities, invalid identity maps and acquisition arithmetic. Its scientific fixture is opt-in through `LL_FLOOR10_CONTROLS`.
- [screen-floor10-controls.py](analysis/screen-floor10-controls.py) freezes the sources/runtime and owns the bounded process; [verify-floor10-controls.py](analysis/verify-floor10-controls.py) independently reconstructs the declared family and results without combat.

The final preparation build passed **129 focused tests**, with two opt-in studies skipped, **17 warnings and zero errors**. Coverage includes canonical reference builds, existing progression upgrades/equipment/preview, gear profiles, equipment acquisition and upgrades, and World Tower. All backend tests used `build/run-tests.ps1`. The [successful log](../TestResults/tower-floor10-controls-preparation-20260928/final-build-and-tests.log) and [preserved TRX](../TestResults/tower-floor10-controls-preparation-20260928/focused-tests.trx) retain the results.

The [initial test run](../TestResults/tower-floor10-controls-preparation-20260928/build-and-tests.log) failed seven checks because the first level-40 draft retained illegal tier-2 gear. The controls were corrected to tier 1, and the reference identity pin was extended to preserve identities across that explicit tier comparison. All seven failures were resolved before screen combat. No gameplay eligibility rule was changed. Python syntax/CLI checks and an independent reconstruction of all 273 recipe declarations and 21 gear-definition inventories passed before launch.
