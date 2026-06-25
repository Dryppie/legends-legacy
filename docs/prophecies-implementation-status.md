# Prophecies Implementation Status

Last updated: 2026-06-26

Branch: `prophecy-implementation`

## Summary

The first Prophecies implementation is in place as an end-to-end MVP for the `LL` game application. It adds backend domain models, persistence, API endpoints, progress event wiring, reward claiming, and an Angular page for daily and weekly prophecies.

The implementation supports:

- Three daily prophecy choices per character and day.
- One accepted daily prophecy at a time.
- Auto-generated weekly Greater Prophecy.
- Weekly Revelation favor progress and milestone claims.
- Reward snapshots at prophecy generation time.
- Prophecy cache rewards as real stackable inventory items.
- Prophecy cache opening from the Prophecies page.
- Weighted prophecy cache reward tables with multiple rolls per cache type.
- JSON-authored prophecy definitions loaded from API content data.
- Recent prophecy history in the overview and page.
- Toast feedback for accept and claim actions.
- Ready-to-claim indicators and sidebar notification counts for actionable prophecy rewards.
- Richer reward presentation with grouped reward rows, reward categories, cache contents previews, and clearer claim/open toast summaries.
- Contextual prophecy guidance with specific action links for combat, dungeons, essences, archive, gathering, treasure, and crafting objectives.
- Live Prophecy progress events over the game realtime channel, with in-place page updates and progress/completion toasts.
- Event-driven progress from combat, dungeons, crafting, and essence systems.
- A cleaner player-facing Prophecies page layout with compact daily choices, a top Weekly Revelation track, focused active progress, actionable rewards, and owned caches only.
- EF Core migrations for new prophecy tables and new character reward balances.

## Added

### Domain Model

Added prophecy domain types under `LL/src/Core/Domain/Models/Prophecies/`:

- `ProphecyDefinition`
- `PlayerProphecyInstance`
- `WeeklyRevelationProgress`
- `ProphecyRewardSnapshot`
- `ProphecyProgressSnapshot`
- Prophecy enums for category, scope, status, slot type, difficulty, and objective type.
- `IProphecyRepository`

The character model now also includes prophecy-related persistent balances:

- `FateEcho`
- `SigilFragments`
- `AscensionStoneFragments`

These were also added to the character DTOs used by the backend and Angular client.

### Application Layer

Added prophecy service contract and CQRS-style use cases:

- `IProphecyService`
- `GetPropheciesOverviewCommand`
- `AcceptProphecyCommand`
- `ClaimProphecyCommand`
- `ClaimWeeklyRevelationMilestoneCommand`
- `ProphecyProgressNotification`
- `IProphecyDefinitionProvider`
- Prophecy service result/read models for overview, claims, weekly milestones, and cache opening.
- Prophecy DTOs and DTO mapping helpers.

The overview command is intentionally implemented through the command pipeline because loading the overview can generate missing daily or weekly instances for the current period.

Layering cleanup completed after review:

- Weekly milestone reward construction lives in `ProphecyService`; the DTO mapper only maps service output.
- Prophecy service result models live in Application contracts rather than Domain.
- Prophecy DTOs are split into focused files.
- Inventory cache consume/count behavior uses `IInventoryRepository`; `IInventoryService` remains a higher-level inventory operation service.

### API

Added `PropheciesController` with endpoints:

- `GET /api/v1/prophecies`
- `POST /api/v1/prophecies/{id}/accept`
- `POST /api/v1/prophecies/{id}/claim`
- `POST /api/v1/prophecies/weekly-revelation/claim`
- `POST /api/v1/prophecies/caches/open`

### Persistence

Added prophecy persistence support:

- `DbSet` entries in `IDbContext` and `LLDbContext`.
- EF configurations for prophecy definitions, player prophecy instances, and weekly revelation progress.
- `ProphecyRepository`.
- Dependency injection registration for the repository.

Generated migrations:

- `20260625140651_AddProphecies`
- `20260625141051_AddCharacterFateEcho`
- `20260625141321_AddProphecyCharacterFragments`

The migrations create the prophecy tables and add prophecy reward balance columns to characters.

### Prophecy Content

Added JSON-authored prophecy definitions under `LL/src/API/API.LL/Data/prophecies/`:

- `daily.json`
- `weekly.json`

Added prophecy definition loading under `LL/src/Infrastructure/Service/Services.LL/Prophecies/`:

- `JsonProphecyDefinitionProvider`

The provider loads the API content files through the same content root and shared JSON serializer options used by other authored game data. It validates empty files, duplicate ids, missing required fields, missing allowed slots, and invalid weights at startup.

### Prophecy Service

Added `ProphecyService` under `LL/src/Infrastructure/Service/Services.LL/Prophecies/`.

Implemented behavior:

- UTC daily periods.
- UTC weekly periods starting Monday.
- On-demand syncing of JSON-authored prophecy definitions into persistence.
- Deterministic daily slot generation for `Steady`, `Focused`, and `Ominous`.
- One auto-accepted weekly Greater Prophecy.
- Daily accept flow that declines the other daily choices.
- Completion checks and claim flow.
- Weekly favor grants from completed daily prophecies.
- Weekly milestone claim flow for 3, 5, and 7 favor.
- Reward application for cinders, soulstones, character XP, Fate Echo, sigil fragments, and ascension stone fragments.
- Reward application for prophecy cache item grants.
- Opening owned prophecy caches by consuming one cache item and rolling weighted rewards.

Supported objective types:

- Kill creatures.
- Kill different creature types.
- Win encounters.
- Clear dungeon rooms.
- Complete dungeons.
- Resolve dungeon events.
- Gain essence XP.
- Archive or feed essence.
- Gather resources.
- Temper items.
- Spend potential.
- Gain treasure progress.
- Win after a meaningful defeat.

### Progress Event Wiring

Progress notifications are published from existing gameplay systems:

- Idle combat rewards.
- Dungeon combat rewards.
- Dungeon action execution.
- Crafting tempering.
- Essence archive and combat XP flows.

The prophecy service consumes these notifications and updates active prophecy progress when the event matches the objective, category, and active period.

### Inventory Rewards

Prophecy cache rewards are now granted into the character inventory as stackable, bound consumable items.

Implemented cache item bases:

- `greater_prophecy_cache`
- `revelation_cache_small`
- `revelation_cache_greater`
- `revelation_cache_perfect_week`

These item bases are ensured on demand before a prophecy reward is granted.

Prophecy caches can also be opened from the Prophecies page. Opening a cache consumes one owned cache item and rolls weighted reward entries for immediate scalar rewards such as cinders, soulstones, Fate Echo, sigil fragments, and ascension stone fragments.

### Frontend

Added Angular prophecy client and page:

- `prophecy.service.ts`
- `prophecies.routes.ts`
- `prophecies-page.component.ts`
- `prophecies-page.component.html`

Added navigation:

- Dashboard route for `/game/prophecies`.
- Sidebar entry under World.

The page shows:

- Daily prophecy choices.
- Active daily prophecy.
- Weekly Revelation progress.
- Weekly milestone rewards.
- Greater Prophecy progress.
- Claim and accept actions.
- Contextual action links for related gameplay areas.
- Recent claimed, declined, expired, and completed prophecy history returned by the overview API.
- Toast feedback for accepted prophecies, prophecy claims, and Weekly Revelation claims.
- Ready-to-claim panel for completed prophecies, unlocked weekly milestones, and owned caches.
- Reward rows with compact markers, quantity labels, reward categories, and cache content chips.
- Specific action buttons and hints that point players toward the gameplay area that progresses each prophecy.
- Live progress/completion toasts and in-place card updates when prophecy progress events are received.
- Prophecy cache inventory counts and open actions.
- Sidebar notification counts for actionable prophecy rewards after the overview is loaded.
- Compact daily prophecy cards that emphasize objective, progress, reward preview, and action.
- The Prophecies page now uses the shared `app-default-header` pattern used by character overview and inventory tabs.
- Weekly Revelation is shown near the top as a compact horizontal milestone track, with milestone reward details shown on hover.
- Daily prophecy cards and the Greater Prophecy panel now have explicit spacing and show hidden reward offers from `+N` chips on hover.
- Weekly Revelation now uses a tighter split favor summary and reward rail with true favor positions, subtle favor ticks, rail end caps, and smaller medallion-backed diamond milestone controls; Greater Prophecy claimed state is shown in the panel label/action area instead of the category tag row, and the sidebar Prophecies item relies only on the notification dot.
- Weekly Revelation rail, fill, favor ticks, and final milestone alignment were tuned so the track sits on the milestone centerline and the last node lines up with the end cap.
- The sidebar Prophecies notification dot now covers missing daily acceptance, completed prophecies, weekly milestone rewards, and unopened cache rewards.
- Completed daily and Greater prophecies no longer show guidance links as if they are still in progress; target-reached prophecies are reconciled to completed on overview/claim.
- Daily and Greater Prophecy target values were increased substantially for a 10-second action cadence, and current offered/accepted prophecies are rebalanced upward on overview without changing completed or claimed history.
- The Daily Claimed and empty Cache side panels were removed; daily cards are the single source of daily state, claimable prophecy/milestone rewards sit below the cards when present, and owned caches render as compact openable tiles.
- Reward overflow and Weekly Revelation milestone hover details now use CDK connected overlays with viewport push/fallback positioning instead of clipped in-card popups.
- The page now scrolls from directly below the Weekly Favor/Revelation block, keeping the header and weekly progress visible while daily, Greater, and history sections scroll.
- Prophecy sidebar notification counts are now refreshed from the sidebar when a character is available, so refreshes show actionable prophecy badges before visiting the Prophecies page.
- Prophecy application DTOs now follow the repository mapping convention: class DTOs implement `IMapFrom<T>`, configure maps through `Mapping(Profile)`, and command handlers use injected `IMapper` instead of the removed static DTO mapper.
- Condensed Greater Prophecy and recent history sections that avoid repeating full reward rows above the fold.

## Partially Added

### Prophecy Content Authoring

The system is data-driven at the `ProphecyDefinition` level. The initial daily and weekly definition set now lives in `LL/src/API/API.LL/Data/prophecies/daily.json` and `LL/src/API/API.LL/Data/prophecies/weekly.json`, loaded through `JsonProphecyDefinitionProvider`.

What exists:

- A reusable prophecy definition table.
- JSON-authored daily and weekly prophecy definitions.
- Startup validation for duplicate ids, missing required fields, missing slot assignment, and invalid weights.
- Weighted/difficulty-aware selection.
- On-demand persistence sync from JSON definitions when the overview is loaded.

What is partial:

- No admin UI exists for authoring or tuning prophecy definitions.
- Prophecy definition edits require an app restart/redeploy; there is no hot reload or database authoring workflow.

### General Item Rewards

The reward snapshot model includes both explicit `Items` and `CacheItemId`.

What exists:

- Reward snapshots can carry item/cache metadata.
- The UI can display configured snapshot rewards.
- Scalar character rewards are persisted and applied.
- Prophecy cache IDs are granted as real stackable inventory items.
- Prophecy cache IDs can be opened into weighted scalar reward rolls.

What is partial:

- Explicit `Items` rewards only grant if the referenced item bases already exist.
- Prophecy caches now roll weighted scalar reward tables, but do not yet roll arbitrary item loot tables.

### Event Coverage

Progress is event-driven for the currently wired systems.

What exists:

- Combat wins/losses and defeated creatures.
- Resource gathering results from combat reward flows.
- Dungeon room, dungeon completion, dungeon event, and treasure progress.
- Crafting tempering and potential spending.
- Essence archive and essence XP.

What is partial:

- Future systems will need to publish `ProphecyProgressNotification` events explicitly.
- There is no generic event bus mapping for every possible gameplay action yet.

### Frontend Verification

The Angular page and API service were added using existing frontend patterns.

What exists:

- Route, sidebar entry, page component, template, API service, DTOs, recent history, and toast feedback.
- Owned cache counts, cache open actions, ready-to-claim panel, and sidebar notification count sync.
- The Prophecies page compiles through the local Angular CLI development build.

What is partial:

- The page has not been visually verified in-browser in this branch.
- The normal `npm run build:development` script still cannot be used because the global npm shim is broken on this machine before Angular starts.

### Automated Test Coverage

The existing backend test suite still passes.

What exists:

- Backend compilation succeeds.
- Existing EssenceSystem tests pass.

What is partial:

- There are no dedicated prophecy unit/integration tests yet for generation, accept, progress, claim, weekly favor, or milestone behavior.
- There are no frontend tests for the prophecy page.

## Not Implemented Yet

- Arbitrary item loot-table cache contents.
- Mail/inbox reminders when prophecy progress changes.
- Background jobs for period rollover or cleanup; current generation happens on overview load.
- Dedicated prophecy tests for service behavior, repository queries, API endpoints, and frontend rendering.
- Production deployment, database migration application, or environment configuration changes.
- Economy balancing pass for reward amounts and objective targets.
- Player-facing explanation/tutorial flow beyond the Prophecies page itself.

## Verification

Commands run:

```powershell
dotnet build LegendsLegacy.sln
dotnet test tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj --no-build --logger "console;verbosity=normal"
git diff --check
npm run build:development
.\node_modules\.bin\ng.cmd build --configuration development
Get-Content .\LL\src\API\API.LL\Data\prophecies\daily.json | ConvertFrom-Json | Select-Object -ExpandProperty definitions | Measure-Object
Get-Content .\LL\src\API\API.LL\Data\prophecies\weekly.json | ConvertFrom-Json | Select-Object -ExpandProperty definitions | Measure-Object
dotnet build .\LL\src\Infrastructure\Service\Services.LL\Services.LL.csproj
dotnet build .\LL\src\API\API.LL\API.LL.csproj -p:BaseOutputPath=.\LL\artifacts\codex-api-build\
```

Results:

- `dotnet build LegendsLegacy.sln` succeeded.
- `dotnet test ...` succeeded with 161 passing tests.
- `git diff --check` succeeded; only line-ending warnings were reported.
- `npm run build:development` failed before Angular started because the local npm shim points to missing `C:\Users\HrHoe\AppData\Roaming\npm\node_modules\npm\bin\npm-cli.js`.
- `.\node_modules\.bin\ng.cmd build --configuration development` succeeded.
- Prophecy JSON parsed successfully: `daily.json` contains 20 definitions and `weekly.json` contains 8 definitions.
- `dotnet build .\LL\src\Infrastructure\Service\Services.LL\Services.LL.csproj` succeeded with existing warnings.
- `dotnet build .\LL\src\API\API.LL\API.LL.csproj -p:BaseOutputPath=.\LL\artifacts\codex-api-build\` succeeded with existing warnings; the isolated output folder was removed afterward.

## Deployment Notes

- EF Core migrations were generated but not applied to any database.
- No secrets, environment-specific values, or deployment files were changed.
- No services were deployed.
- Applying this branch requires running the new migrations in the target environment before the prophecy API is used.
