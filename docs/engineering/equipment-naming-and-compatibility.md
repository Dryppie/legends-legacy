# Equipment naming and storage contracts

Updated: 3 September 2026. Scope: the primary LL game, its player/API contracts, and offline equipment tools.

The active game uses `EquipmentState`, `EquipmentData`, and equipment acquisition terminology. The equipment Forge and its public/storage contracts have been removed. Alpha compatibility is not supported.

## Current contracts

- Player metadata uses `progression` for the canonical frozen equipment descriptor.
- Starter routes are `equipment/starter-options` and `equipment/starter-claim`.
- `EquipmentProgression` configures starter acquisition, protected acquisition, baseline recovery, and ordinary acquisition.
- Runtime content uses `equipment-*.v1.json` catalogs for definitions, styles, acquisition pools, and starters.
- Persisted equipment JSON keeps authored identity, tier, rank, native/active style, stats, provenance, and ownership.
- Current tables retain starter grants, ordinary selection/progress, protected reward progress/receipts, and recovery receipts.

`ModelECharacterStyles` and `ModelEForgeReceipts` are dropped by `RemoveEquipmentForge`. Tournament Blueprint/Scrap fields and ordinary Scrap remainder are also removed. Historical migrations and some physical `ModelE*` names remain only as the database creation history and established storage names for current equipment.

Internal style IDs may still use a `blueprint_` prefix because they identify authored stat/style profiles embedded in equipment content. They are not learnable items and do not imply a Blueprint inventory or Forge operation.

The API and player client should be released together when these contract removals ship. No deployment or migration application was performed by this change.

See [equipment contract](../design/equipment-specification.md) and [Forge removal](../design/equipment-forge-removal.md).
