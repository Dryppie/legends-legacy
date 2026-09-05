# Equipment implementation status

Updated: 5 September 2026.

The equipment drop rework is implemented for Shenic and Meran. The equipment Forge has been removed in full. Crafting, gathering, queued tempering, equipment salvaging, Tempered Scrap, reusable Blueprint rewards, and Alpha compatibility paths are also absent from the supported player flow.

## Current state

| Area | Status |
| --- | --- |
| Starter equipment | Implemented with authored choices and durable per-character grants. |
| Ordinary combat drops | Implemented for every released Shenic and Meran combat area at 0.03% per victory, with any archetype, region tier, rank 0, seven rarity outcomes, five qualities, and a frozen ±5% attribute-budget roll. |
| Dungeon equipment drops | Implemented at 20% per completion, with any archetype, region tier, rank 1, improved higher-rarity odds, the same quality distribution, and a frozen ±5% attribute-budget roll. |
| Regional Sigil drops | Implemented in every released combat area at 1/4,320 per victory, selecting uniformly among that region's dungeon families. |
| Equipment recovery | Removed with the Forge-era recovery surface. |
| Equipment evaluation | Implemented from authored item identity, tier, rarity, quality, rank, frozen attribute roll, native/active style, behavior, set, and stat weights. |
| Ownership and transfer | Implemented for personal binding, unbound marketplace transfer, guild donation, and guild loans. |
| Equipment upgrades | Reinforcement and dismantling use the equipment panel. Consumable blueprints apply or replace compatible variants through a transactional preview/confirmation flow. The old Forge and permanent style learning remain removed. |
| Variant drops and rewards | Areas and dungeons explicitly roll base versus themed variant gear. Dungeon blueprint choices have an independent roll and a fourth-completion guarantee. The Soul Archive quest includes an introductory Fury blueprint. |
| Player UI | Equipment panel includes held blueprints, source/guarantee progress, exact conversion stats, payment and set-replacement confirmation. Forge route/page and Scrap mode remain removed. |
| LiveOps | Forge investment/style/salvage details removed from support snapshots. Further LiveOps work remains deferred. |

## Deliberately deferred

- Rules for selecting gear awarded by quests.
- Further end-to-end balance work after those designs are known.

Existing frozen variants keep their original allocation mode until explicitly converted. New variants add a separate 15% bonus budget without removing any base attributes. Reinforcement and conversion preserve rarity, quality and attribute rolls. See [consumable blueprint implementation](equipment-blueprints-implementation.md) for the current rules and release requirements.

## Configuration and schema

`EquipmentProgression` retains three compatibility switches for starter grants, dungeon drops, and area drops. All default to enabled. There is no Forge, recovery, target-selection, protection, or Forge price catalog.

The generated `RestoreRandomEquipmentDrops` migration removes ordinary selection/progress, dungeon protection progress/receipts, and frozen dungeon target commitments. `AddEquipmentBlueprintProgress` adds the dungeon Blueprint Variant guarantee counter, and `RemoveLegacyEquipmentFields` removes the obsolete Forge-era columns from item instances and equipment snapshots. `RemoveRetiredToolsAndGatheringArtifacts` safely removes retired Tool records before dropping the Tool-bonus table and the final gathering column. The full chain has been applied to the local PostgreSQL database. Historical migrations remain so a database can still be built through its migration chain.

The dormant crafting definition provider, recipes, materials, reusable Blueprint catalog, crafting roll/Potential services, crafting configuration, and crafting-only tests have also been deleted. Current equipment Quality and attribute rolls belong to the live combat-drop implementation rather than those retired services.

See [Forge removal](equipment-forge-removal.md), [Forge responsibilities and removal impact](equipment-forge-removal-impact.md), [current equipment contract](equipment-specification.md), and [post-Alpha cleanup](equipment-post-alpha-cleanup.md).

## Verification record

Verified on 5 September 2026:

- `build/run-tests.ps1 -NoBuild -Configuration Debug`: 1,763 backend tests passed after a clean redirected build.
- Player application: 589 tests passed; the production build passed with the existing initial-bundle budget warning.
- LiveOps application: 35 tests passed.
- Admin Dashboard: development build passed to an isolated output directory. Its normal production build could not inline the configured Google Font without network access.
- The source-reference audit found no remaining live Tool/gathering model references, retired crafting catalog/services, retired combat reward fields, crafting/gathering progression events or objectives, or player-facing crafting/tempering UI contracts.
- `git diff --check` passed.

No deployment or shared-environment database update was performed. The local `legends_legacy` PostgreSQL database was migrated and passed rebuilt API startup and health checks.
