# Equipment cleanup after Alpha

Updated: 3 September 2026.

Alpha has ended and its data does not need to be preserved. The current game uses equipment drops, starter grants, Forge ranks/styles, and Scrap directly. This supersedes the earlier cohort rollout, conversion and retirement-adapter plan.

## Removed

- Equipment cohort policy and per-character feature selection, including alternate quest catalogs, reward conversions and profession visibility rules.
- Crafting/profession endpoints and commands, queued tempering start/resume/cancel support, queue repositories and action/session DTOs.
- Crafting queue, profession, recipe unlock/mastery and area gathering persistence. New-character and administrator seeds no longer create professions, tools or crafting test materials.
- Combat/dungeon gathering processors, gathering previews, gathering mastery benefits and the superseded random sigil-drop path.
- Obsolete materials, tools, catalyst crates, crafting/gathering quests, old quest versions, profession Prophecies/achievements/titles and Soulstone constellations. Current rewards are authored in their normal catalogs; no runtime conversion is needed.
- Retired-constellation refund support, automatic old quest-choice upgrades and crafted-item stat migration.
- Player profession routes, old queue/session panels, profession experience updates, cohort-specific help variants, old equipment response aliases and starter route aliases.

## Current behavior

- `EquipmentProgression` is the configuration section. Starter acquisition, Forge, protected acquisition, baseline recovery and ordinary acquisition default to enabled. Explicit capability switches remain available. The separate quest-integration switch and old configuration alias are gone.
- Forge tempering/rank improvements remain available. Ordinary and protected drops, starter/recovery claims, style learning, salvage and ownership rules continue to use canonical equipment descriptors.
- Guild shops, event rewards, tournament grants, Champion Market and raid vendors grant their authored current rewards. Tournament reward storage now records `TemperedScrap` directly.
- The player UI uses the current equipment and quest flow without waiting for cohort information. Leaderboards expose the current combat/activity boards.
- Prophecy cache rewards are validated before consuming the cache or changing character balances.

## Schema and deployment

`20260903115622_RemoveAlphaProfessionsAndTemperingQueues` removes `AreaGatheringNode`, `CharacterRecipeMasteries`, `CharacterRecipeUnlocks`, `CraftingQueueItems` and `Professions`, drops `CharacterActions.ReturnToCombatAreaId` and `TournamentRewardGrants.DeliveredTemperedScrap`, and renames the tournament cache entitlement column to `TemperedScrap`.

`20260903121634_RemoveRetiredAlphaQuestProgress` deletes saved quest progress whose ID/version is absent from the frozen post-Alpha catalog, including the removed crafting/gathering quests and superseded onboarding/region versions. Their objective rows cascade-delete. Current quest progress is retained; removed versions restart through normal quest availability. This prevents journal loading from trying to resolve deleted definitions. It is a one-time data deletion, with no runtime fallback or conversion, and rollback cannot recover the deleted progress.

The migrations were generated locally and have not been applied by this task. They contain no Alpha reward compensation, refund or data-conversion routines. Historical EF migrations remain to preserve a runnable schema creation chain. The API applies pending migrations at startup, so rebuild and restart it to run the cleanup. API and player-client changes should be released together because retired routes and response fields are removed.

## Local quest-journal troubleshooting

An error such as Unknown quest definition 'quest.crafting.armor_and_adornment' means the journal is resolving a saved quest whose definition has been removed. A removed DefinitionVersion on a retained quest can fail similarly. The data cleanup belongs in the one-time migration above; do not restore obsolete JSON or add a runtime legacy fallback.

Rebuild and restart the local API so its startup migration runner includes RemoveRetiredAlphaQuestProgress. After successful startup, reload the quest journal. The migration retains exactly the 29 ID/version pairs shipped at cleanup time and removes other regular quest progress, with objective cascade deletion. This frozen list is migration history; future catalog changes need their own deliberate content/data decision rather than edits to an already-applied migration.

Source: [startup migration runner](../../LL/src/API/API.LL/Program.cs), [strict quest definition lookup](../../LL/src/Infrastructure/Service/Services.LL/Quests/JsonQuestDefinitionProvider.cs), and [quest-progress cleanup](../../LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260903121634_RemoveRetiredAlphaQuestProgress.cs).

## Dungeon reward-table follow-up

The cleanup removed Great Tree and Tangled Cave completion tables because they contained only gathering-tool rewards. The dungeon materializer still generated their IDs automatically, causing reward previews and completion processing to request missing tables. Each difficulty now authors `completionRewardTableIds` explicitly: six Region 1 Blueprint tables remain referenced, while all six Region 2 difficulties initially used empty lists. The subsequent [Meran expansion](equipment-region-two-progression.md) authors six Blueprint-only completion tables and Tier 2 equipment pools. First-clear Blueprints and standard Monster Core rewards remain intact.

The loader validates authored references and the catalog rejects blank/duplicate IDs. The all-dungeon preview regression reproduced the original missing-table error, then passed with the fix. A fresh `build/run-tests.ps1` run passed 77 focused dungeon, definition, preview, reward, equipment and content-registration tests with no build errors; existing analyzer warnings remain. Rebuild and restart the API with the updated catalog. This follow-up adds no migration or configuration setting, applies no database change and performs no deployment. Authenticated gameplay was not exercised.

## Region 2 follow-up

Meran now has a complete Tier 2 acquisition and Forge loop. The separate `ScopeOrdinaryEquipmentSelectionsByPool` migration adds pool identity to current selection receipts; it does not restore Alpha conversion or compensation. See [content, prices, validation and startup implications](equipment-region-two-progression.md).

## Retained dependencies

Shared equipment stat allocation, set definitions and the older recipe/tempering simulation used by `CanonicalEquipmentBuildFactory`, the ability simulator and offline balance tools remain. Those files still have active consumers; they no longer expose a player crafting/queued-tempering API. Existing equipment identity strings and physical equipment table names do not create a separate gameplay path.

Current operation receipts, ownership checks, frozen reward descriptors and retry/concurrency handling remain necessary for current gameplay. Baseline equipment recovery is also a current gameplay feature, not an Alpha data migration.

## Frontend acceptance follow-up — 3 September 2026

The five outstanding browser failures were reproduced and resolved. Three RegionService tests still expected deleted gathering metadata or a `Region/1/gathering` request. The current region layout is local combat content; its unused API dependency and region gathering DTO fields have now been removed. Region tests cover the current combat rosters, level requirements, Tower gate and area-to-region mapping.

The world-map redirect failure used an idle action with no combat area but expected Meran. Its regression now supplies the Meran combat action during delayed bootstrap and verifies that navigation waits for that state. Separate checks cover deleted actions and bootstrap failure. No return-to-gathering behavior or runtime compatibility branch was restored.

The quest failure also exposed a real content bug: the current `Into the Ruins` definition requests `tutorial-lumo-ruins`, whose asset still described gathering and targeted a removed panel. That canonical [tour asset](../../LL/src/Presentation/ll/src/assets/help/tours/tutorial-lumo-ruins.json) now describes the expedition and battle victory, targeting the visible Lumo card and Battle control. The unused `tutorial-lumo-ruins-equipment.json` duplicate and gathering tour getter were removed. The quest presenter follows the authored tour ID directly. A browser regression loads the real JSON and advances through both steps using the real tour action watcher.

Verification from `LL/src/Presentation/ll`, with the npm cache under `$env:TEMP/ll-npm-cache`:

- The initial focused `npm.cmd run test:ci` reproduced five failures in ten tests.
- The final focused run selecting the region, redirect, quest-presenter and first-party-tour specs passed **17 tests**.
- Full `npm.cmd run test:ci` passed **643 tests**, zero failures. The earlier five-failure result is superseded.
- `npm.cmd run build:development` passed with existing Angular warnings.
- `build/run-tests.ps1` rebuilt and passed **94 backend equipment-flow tests**. The filter selected `EquipmentIntegrationTests`, `EquipmentRegionTwoTests`, `EquipmentAcquisitionTests`, `EquipmentProgressionPlainRecoveryTests`, `ForgeTests` and `CombatAcquisitionTests`. These cover grants/equip, regional progress, paid rank/style/salvage, recovery, frozen dungeon rewards and retries. The first sandboxed build could not read the user NuGet configuration; the authorized retry succeeded. No backend source changes were needed.

Authenticated verification was attempted at `http://localhost:4200/game/character/forge`. The existing admin session reached the Forge loading screen, but equipment data did not finish loading; a subsequent reload remained at an empty app root. The frontend returned HTTP 200, while `http://localhost:7050/healthz/live` exceeded an eight-second timeout. API responsiveness, including a possible paused debugger, must be resolved before continuing the walkthrough. This does not verify completed item grants, Forge mutations, dungeon claims, or the applied PostgreSQL schema. No gameplay mutation was submitted during this attempt.

This follow-up changes frontend content, unused frontend types/dependencies and tests. It adds no migration or configuration, restarts no API, and performs no deployment or database update. The player build includes the canonical tour fix without a quest-data migration.

## Verification

- Player development build and all TypeScript spec compilation pass.
- 128 affected browser tests pass, covering equipment/Forge, inventory, marketplace, onboarding, action handling, session summaries, leveling and help.
- The API builds using a separate temporary output directory so the running local API is not interrupted.
- `build/run-tests.ps1` passed the full backend suite (1,901 tests). After removing the remaining unused profession helpers and their obsolete tests, the final tree compiled and passed 29 focused character-progression, offline-reward, profile and content-registration checks through the same wrapper.
- `dotnet ef migrations has-pending-model-changes --configuration Release --no-build` reports no missing model changes, using Persistence.LL as both project and startup project.
- Quest cleanup follow-up: `build/run-tests.ps1` built successfully and passed 39 focused quest/event-quest and equipment quest integration tests. EF generated the new migration SQL successfully and again reported no missing model changes.
- Executed the generated cleanup statement against an in-memory SQLite fixture: all 46 deleted quest definitions and an unknown quest were removed, while 88 current progress rows and 176 objective rows remained unchanged. Cascading deletion, case-insensitive IDs, repeat execution and an empty database also passed. This checks the SQL behavior without connecting to the game database; PostgreSQL application remains pending API restart.

All planned automated checks for this cleanup completed. The generated cleanup SQL was exercised in SQLite, not against the game PostgreSQL database. Applying pending migrations and checking the authenticated quest journal remain acceptance steps. API build output used a temporary directory because the running local API held its normal Debug binaries. The task stopped no service, applied no game-database migration and deployed nothing.

This cleanup does not complete higher-region/tier equipment coverage, raid-entry redesign or integrated economy/combat balance acceptance. Further LiveOps feature work remains deferred.

## Documentation follow-up

The 3 September Markdown follow-up updates the README, Beta roadmap, equipment specification/status, Region 1 plan, storage notes and quest guides. Older crafting/gathering/tempering plans and implementation audits are explicitly historical. Current documents describe enabled equipment defaults, direct rewards and one-time deletion of obsolete quest progress, without Alpha conversion or compensation requirements.

Documentation checks passed across 22 edited Markdown files: 256 local links, including 11 heading targets; all 29 regular quest titles against the current JSON catalog; the single remaining event definition; acceptance/removal/migration requirement IDs; code-fence balance and whitespace. Deleted historical source files are identified as removed rather than left as broken links. This follow-up changes documentation only; application tests were not rerun, and no new migration, configuration change, database update or deployment was performed.
