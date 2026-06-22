# Dungeon Roguelite Expedition Slice

This change extends the existing dungeon system rather than replacing it.

Extended classes and surfaces:

- `DungeonRun` now owns a JSON-backed `DungeonRunState` for pressure, temporary boons, flags, secured/unsecured loot, and current decision options.
- `DungeonDefinition` now supports an optional `DungeonMechanicDefinition`; existing definitions fall back to generic Pressure.
- `DungeonRunFactory` initializes default run state for new runs.
- `DungeonRunService` continues to use `/dungeon/executeAction/{runId}` and now handles route, event, checkpoint, and boon choice actions alongside the existing fight/continue/withdraw actions.
- `DungeonRunDto` returns run state to the Angular dungeon page.
- The Angular dungeon page displays pressure/mechanic state, reward multiplier, route choices, event choices, checkpoint choices, and temporary boons.

Design notes:

- Route choices currently decorate the next pre-generated room and apply route pressure. This keeps existing dungeon definitions and active run structure compatible while allowing later dynamic room generation.
- Pressure thresholds are centralized in `DungeonPressureService`.
- Boons are stored and displayed as run-only state. Deeper combat-stat integration is intentionally left for a later combat pipeline pass.
- Goblin Mines uses `Alarm Level`; Forgotten Catacombs uses `Curse`; dungeons without a mechanic still display `Pressure`.
