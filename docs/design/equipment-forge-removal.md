# Equipment Forge removal

Updated: 3 September 2026.

The equipment Forge has been removed rather than retained as a disabled or legacy path. Equipment upgrades and quest-specific gear selection will be designed later.

## Removed surfaces

- Forge API controller, service interface/implementation, repository, policy, commands, queries, DTOs, and domain mutation models.
- Price catalog and dependency-injection registration.
- Rank improvement, style learning/application, salvage, mutation quotes, operation receipts, and related economy/game events.
- Learned-style and Forge-receipt persistence.
- Player route, page, state service, links, help guide, inventory Scrap mode, and realtime state-sync scope.
- LiveOps Forge investment, learned-style, and salvage projections.
- Tempered Scrap, equipment Blueprint items/selection containers, and Forge-oriented rewards from current content catalogs.

## Retained equipment data

Equipment definitions still author tier, rank, native/active style, stat weights, behavior, and set identity. Drops and rewards need these values to create deterministic gear. They are not player-upgrade state and cannot be changed through current gameplay.

Some style identifiers retain their established `blueprint_` prefix. They are content keys only; no item, learning, or Forge workflow exists for them.

## Database change

`20260903184525_RemoveEquipmentForge` drops:

- `ModelECharacterStyles`;
- `ModelEForgeReceipts`;
- `TournamentRewardGrants.BlueprintSelectionBoxes`;
- `TournamentRewardGrants.TemperedScrap`;
- `ModelEOrdinaryProgress.ScrapRemainder`.

The migration is generated but unapplied. It intentionally contains no data conversion or compensation.

## Follow-up boundary

A future upgrade design must define how gear changes, what it costs, how stats are recalculated, how ownership and trading react, and whether new persistence is required. Quest gear selection likewise needs an authored selection contract. Neither concern is represented by a placeholder in the current implementation.
