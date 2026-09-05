# Equipment cleanup after Alpha

Updated: 4 September 2026.

Alpha data does not need to be preserved. The supported game no longer carries runtime conversion, refund, compatibility, or fallback paths for removed crafting, gathering, queued tempering, equipment salvaging, or Forge data.

## Removed after Alpha

- Crafting and gathering player APIs, profession state, queued tempering, recipes, tools, related objectives, and persistence.
- Cohort policies, alternate response/route/configuration aliases, reward adapters, refunds, and conversion services.
- Retired quest definitions and their saved progress through the one-time `RemoveRetiredAlphaQuestProgress` migration.
- The equipment Forge, including prices, rank/style mutations, learned styles, operation receipts, Scrap production/spending, Blueprint reward items, UI, help, and state-sync scope.
- Starter and plain-equipment recovery APIs, services, commands, receipts, feature flag, client contracts, and tests.

## Retained current behavior

- Authored equipment evaluation, drops, dungeon target protection, starter grants, ownership, marketplace transfer, and guild ownership.
- Plain-target award counters used to validate the active equipment quest objective.
- Authored tier, rarity, quality, frozen attribute roll, stats, and set identity on an awarded item. Reinforcement changes rank, while consumable Blueprint Variants can replace compatible style and set identity.
- Shared stat and simulation helpers used by live equipment evaluation, offline combat, and balance tooling; these now live under the equipment progression domain and expose no crafting or Forge route.

## Migrations

- `RemoveAlphaProfessionsAndTemperingQueues` removes obsolete profession, recipe, gathering, and queue storage.
- `RemoveRetiredAlphaQuestProgress` deletes progress for quest ID/version pairs absent from the frozen post-Alpha catalog.
- `ScopeOrdinaryEquipmentSelectionsByPool` adds pool identity to ordinary reward-selection receipts.
- `RemoveEquipmentForge` drops learned-style and Forge-receipt tables, tournament Blueprint/Scrap columns, and ordinary-acquisition Scrap remainder.
- `RemoveEquipmentRecovery` drops starter/plain recovery receipts and the recovery-only frozen baseline payload from plain-target counters.
- `RemoveRetiredToolsAndGatheringArtifacts` deletes retired Tool data, then drops the Tool-bonus table and `ItemBases.GatheringType`.

The final migration chain was applied to the local `legends_legacy` PostgreSQL database on 5 September 2026. No shared or production database was changed. No compensation or conversion runs because there is no Alpha data to retain.

An `Unknown quest definition` error for a deleted crafting/gathering quest indicates the API has not yet run the saved-progress cleanup migration. Rebuild and restart the local API; do not restore deleted content or add a compatibility fallback.

See [Forge removal](equipment-forge-removal.md) and [implementation status](equipment-implementation-status.md).
