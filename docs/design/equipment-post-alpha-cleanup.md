# Equipment cleanup after Alpha

Updated: 3 September 2026.

Alpha data does not need to be preserved. The supported game no longer carries runtime conversion, refund, compatibility, or fallback paths for removed crafting, gathering, queued tempering, equipment salvaging, or Forge data.

## Removed after Alpha

- Crafting and gathering player APIs, profession state, queued tempering, recipes, tools, related objectives, and persistence.
- Cohort policies, alternate response/route/configuration aliases, reward adapters, refunds, and conversion services.
- Retired quest definitions and their saved progress through the one-time `RemoveRetiredAlphaQuestProgress` migration.
- The equipment Forge, including prices, rank/style mutations, learned styles, operation receipts, Scrap production/spending, Blueprint reward items, UI, help, and state-sync scope.

## Retained current behavior

- Authored equipment evaluation, drops, dungeon target protection, starter grants, recovery, ownership, marketplace transfer, and guild ownership.
- Authored rank, style, stats, and set identity on an awarded item. These values are currently immutable.
- Shared stat and simulation helpers that still have active offline or combat-tool consumers and expose no player crafting or Forge route.

## Migrations

- `RemoveAlphaProfessionsAndTemperingQueues` removes obsolete profession, recipe, gathering, and queue storage.
- `RemoveRetiredAlphaQuestProgress` deletes progress for quest ID/version pairs absent from the frozen post-Alpha catalog.
- `ScopeOrdinaryEquipmentSelectionsByPool` adds pool identity to ordinary reward-selection receipts.
- `RemoveEquipmentForge` drops learned-style and Forge-receipt tables, tournament Blueprint/Scrap columns, and ordinary-acquisition Scrap remainder.

The migrations were generated locally and were not applied by this work. The API applies pending migrations at startup, so a local database changes when its rebuilt API next starts. No compensation or conversion runs because there is no Alpha data to retain.

An `Unknown quest definition` error for a deleted crafting/gathering quest indicates the API has not yet run the saved-progress cleanup migration. Rebuild and restart the local API; do not restore deleted content or add a compatibility fallback.

See [Forge removal](equipment-forge-removal.md) and [implementation status](equipment-implementation-status.md).
