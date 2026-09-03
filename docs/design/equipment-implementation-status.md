# Equipment implementation status

Updated: 3 September 2026.

The equipment drop rework is implemented for Shenic and Meran. The equipment Forge has been removed in full. Crafting, gathering, queued tempering, equipment salvaging, Tempered Scrap, reusable Blueprint rewards, and Alpha compatibility paths are also absent from the supported player flow.

## Current state

| Area | Status |
| --- | --- |
| Starter equipment | Implemented with authored choices and durable per-character grants. |
| Ordinary combat drops | Implemented for released Shenic and Meran areas, including elective plain-item targets. |
| Protected dungeon rewards | Implemented with target selection, frozen run commitments, first-clear guarantees, and bad-luck progress. |
| Baseline recovery | Implemented for missing starter and plain entitlements. |
| Equipment evaluation | Implemented from authored item identity, tier, rank, native/active style, behavior, set, and stat weights. |
| Ownership and transfer | Implemented for personal binding, unbound marketplace transfer, guild donation, and guild loans. |
| Forge and equipment mutation | Removed. Players cannot improve rank, learn/apply styles, salvage equipment, or preview/pay for mutations. |
| Forge-related rewards | Removed from tournaments, Champion Market, guild shop, raids, Prophecies, quests, event quests, dungeons, and selection containers. |
| Player UI | Forge route/page/link/help removed. Inventory is browse/equip oriented and no longer has Scrap mode. |
| LiveOps | Forge investment/style/salvage details removed from support snapshots. Further LiveOps work remains deferred. |

## Deliberately deferred

- A replacement equipment-upgrade design.
- Rules for selecting gear awarded by quests.
- Further end-to-end balance work after those designs are known.
- Authenticated gameplay and PostgreSQL acceptance after applying pending migrations locally.

Existing equipment keeps its authored rank and style because those values are part of drop identity and deterministic stat evaluation. They are immutable in current gameplay.

## Configuration and schema

`EquipmentProgression` now contains four capability switches: starter acquisition, protected acquisition, baseline recovery, and ordinary acquisition. All default to enabled. There is no Forge capability or price catalog.

The generated `RemoveEquipmentForge` migration drops `ModelECharacterStyles`, `ModelEForgeReceipts`, tournament Blueprint/Scrap columns, and ordinary-acquisition Scrap remainder. It has not been applied. Historical migrations remain so a database can still be built through its migration chain.

See [Forge removal](equipment-forge-removal.md), [current equipment contract](equipment-specification.md), and [post-Alpha cleanup](equipment-post-alpha-cleanup.md).

## Verification record

Verified on 3 September 2026:

- `build/run-tests.ps1`: 1,870 backend tests passed.
- Player application: 628 tests passed and the development build completed.
- LiveOps application: 38 tests passed and the production build completed.
- All 114 API data and player-help JSON files parsed successfully.
- EF Core reported no model changes beyond the generated migration.
- The source-reference audit found no remaining Forge API, service, policy, route, UI, salvage currency, or persisted Forge-state references. The unrelated combat location/ability named **The Unlit Forge** remains.

No deployment or database update was performed.
