# Dungeon Roguelite Request Status

This document maps the original dungeon roguelite request to the current implementation state.

## Implemented

### Step 0 - Codebase Discovery and Safety

- Reviewed the existing backend dungeon models, services, DTOs, persistence mapping, API action pattern, Angular service, and dungeon page.
- Kept the implementation incremental and preserved the existing `/dungeon/executeAction/{runId}` endpoint flow.
- Added the short implementation note in `docs/dungeon-roguelite-implementation-note.md`.
- Verified the solution builds and focused backend tests pass.

### Step 1 - Add Dungeon Run State

- Added `DungeonRunState` on `DungeonRun`.
- State tracks:
  - Pressure
  - Reward multiplier percent
  - Active boon IDs
  - Run flags
  - Secured loot
  - Unsecured loot
  - Current route options
  - Current event choices
  - Current checkpoint choices
  - Current boon choices
- Added JSONB persistence for `DungeonRun.State`.
- Added migration `20260622120000_AddDungeonRunState`.
- New runs initialize default state in `DungeonRunFactory`.
- `DungeonRunDto` now returns the nested state to the frontend.

### Step 2 - Add Dungeon Pressure / Danger Meter

- Added `IDungeonPressureService` and `DungeonPressureService`.
- Pressure clamps between `0` and `100`.
- Implemented default reward multiplier thresholds:
  - `0-24`: `100%`
  - `25-49`: `110%`
  - `50-74`: `125%`
  - `75-99`: `145%`
  - `100`: `175%`
- Combat, miniboss, event, checkpoint, and route choices can adjust pressure.
- Reward multiplier is returned in dungeon DTO state and displayed in the Angular UI.
- Added tests for clamping and threshold multiplier calculation.

### Step 3 - Add Route Choices Between Rooms

- Added `IDungeonRouteService` and `DungeonRouteService`.
- After room resolution, the run generates route options for the next pre-generated room.
- Route options include:
  - Display name
  - Room type
  - Risk level
  - Pressure delta
  - Tags
  - Possible rewards
  - Unknown flag
- Added `choose_route` action support.
- Invalid route IDs are rejected by the route service.
- Existing linear play remains compatible because non-route actions auto-select the first available route.
- Added route generation/selection tests.

### Step 4 - Add Temporary Dungeon Boons

- Added `IDungeonBoonService` and `DungeonBoonService`.
- Added `DungeonBoonDefinition` and JSON-backed temporary boon definitions in `LL/src/API/API.LL/Data/dungeon-boons.json`.
- Added `IDungeonBoonDefinitionProvider` and `JsonDungeonBoonDefinitionProvider`.
- Added `choose_boon` action support.
- Chosen boon IDs persist in `DungeonRunState.ActiveBoonIds`.
- Boon choice generation is deterministic and rarity-weighted.
- Active boon IDs now resolve to temporary combat attribute modifiers.
- Active boon IDs now resolve to temporary combat ability modifiers.
- Dungeon combat orchestration carries run attribute modifiers into combat planning and session setup.
- Dungeon combat orchestration carries run ability modifiers into combat planning and session setup.
- The dungeon combat session factory applies boon attribute and ability modifiers to player combat templates before combat preparation.
- The combat engine applies run-scoped ability modifiers when compiling equipped essence abilities.
- Boon choices and active boon IDs are returned in DTOs and displayed in the Angular UI.
- Added tests for boon choice generation, selection, duplicate prevention, choice clearing, JSON definition loading, active boon attribute modifiers, active boon ability modifiers, and combat-engine application of temporary ability modifiers.

### Step 5 - Improve Checkpoints

- Added `checkpoint_choice` action support.
- Added `IDungeonCheckpointService` and `DungeonCheckpointService` to keep checkpoint choice generation and effects out of the main run service.
- Added checkpoint choices:
  - Withdraw
  - Focus
  - Push deeper
  - Rest
- Implemented effects:
  - Withdraw ends the run safely and marks secured loot.
  - Focus generates boon choices.
  - Push deeper increases pressure and reward multiplier bonus.
  - Rest reduces pressure and trims unsecured loot.
- Checkpoint choices are returned in DTOs and displayed in the Angular UI.
- Rest now reduces pending currency and item rewards so the claimable ledger matches unsecured loot state.
- Withdraw snapshots claimable rewards into secured loot and clears unsecured loot.
- Withdrawn reward claims now pay from secured loot instead of any remaining pending or unsecured rewards.
- Added tests for focus, push deeper, rest, withdraw, item trimming, secured loot snapshotting, and secured-only reward claiming.

### Step 6 - Add Better Event Rooms

- Added `event_choice` action support.
- Added `DungeonEventDefinition`, `DungeonEventChoiceDefinition`, `IDungeonEventChoiceService`, and `DungeonEventChoiceService`.
- Added generated event choices for current event outcome types:
  - Extra combat
  - Treasure
  - Shrine
  - Trap
- Event choices can:
  - Adjust pressure
  - Grant loot
  - Generate boon choices
  - Add and remove run flags
  - Convert an event into combat
  - Expose flag requirements for event chains
  - Roll deterministic ambush chances
  - Reveal hidden route options after event resolution
- Added `IDungeonEventDefinitionProvider` and `JsonDungeonEventDefinitionProvider`.
- Added JSON-backed event definitions in `LL/src/API/API.LL/Data/dungeon-events.json`.
- Added dungeon-specific authored events for Goblin Mines and Forgotten Catacombs.
- Event choices are returned in DTOs and displayed in the Angular UI.
- Added tests for treasure, shrine, trap, flag requirement, hidden route selection, and JSON event provider behavior.

Partial only:

- Hidden route generation is intentionally lightweight: event choices inject a known shortcut route to the next room.

### Step 7 - Make Bosses Reflect the Run

- Added `IDungeonBossModifierService` and `DungeonBossModifierService`.
- Boss rooms now calculate active boss modifiers from:
  - Dungeon mechanic threshold boss modifier IDs
  - Current pressure fallback thresholds when no custom thresholds are authored
  - Checkpoint push count
  - Event and route flags from earlier run choices
- Boss modifiers are converted into hostile-side dungeon combat attribute modifiers.
- Dungeon combat orchestration carries enemy attribute modifiers through the request, plan, and resolution session setup.
- The dungeon combat session factory applies boss modifiers to hostile combat templates before combat preparation.
- Current boss modifiers are stored in `DungeonRunState.CurrentBossModifiers`, returned in DTOs, and displayed in the Angular dungeon run UI.
- Added authored boss threshold IDs to Goblin Mines `Alarm Level` and Forgotten Catacombs `Curse`.
- Added tests for threshold/flag-based boss modifier generation, non-boss room exclusion, and combat plan propagation of enemy modifiers.

### Step 8 - Add Dungeon-Specific Mechanics

- Added `DungeonMechanicDefinition`.
- Extended `DungeonDefinition` with optional `Mechanic`.
- Existing dungeon definitions without a mechanic fall back to generic Pressure behavior.
- Added mechanic config to:
  - Goblin Mines: `Alarm Level`
  - Forgotten Catacombs: `Curse`
- New runs store mechanic ID, display name, max value, and active threshold state from the dungeon definition.
- Loaded runs refresh mechanic metadata from the dungeon definition instead of using hardcoded dungeon ID checks.
- Custom mechanic threshold reward multiplier bonuses are used by pressure calculations.
- Custom mechanic threshold descriptions are returned in DTO state and displayed in the Angular dungeon UI.
- Regular combat and miniboss rooms consume mechanic threshold `enemyModifierIds` as hostile-side combat modifiers.
- Boss rooms consume mechanic threshold `bossModifierIds` through the Step 7 boss modifier path.
- Added authored enemy modifier IDs and reward multiplier bonuses to Goblin Mines `Alarm Level` and Forgotten Catacombs `Curse`.
- Added tests for custom mechanic threshold state, reward multiplier calculation, and regular enemy threshold modifiers.

### Step 9 - Dungeon Mastery

- Added `CharacterDungeonMastery` keyed by character and dungeon definition.
- Added EF configuration, repository, DbSet registration, and migration `20260622193000_AddCharacterDungeonMastery`.
- Added `IDungeonMasteryService` and `DungeonMasteryService`.
- Mastery XP is awarded on dungeon completion and guarded against duplicate awards for the same run.
- Completion XP includes completed boss rooms, completed miniboss rooms, high-pressure completion, and existing optional-objective run flags.
- Mastery level is calculated from cumulative XP thresholds.
- Available dungeon previews now return mastery level, XP, next-level XP, completion count, and bonus state.
- Mastery bonuses are loaded from JSON through `IDungeonMasteryBonusDefinitionProvider`.
- At mastery level 2, new runs for that dungeon start with the JSON-authored `+5%` reward multiplier bonus.
- Dungeon cards display mastery level, XP progress, clears, and active/upcoming mastery bonuses.
- Completed-run reward screens display the mastery XP reason breakdown.
- Added tests for XP gain, idempotent completion awards, boss/optional/high-pressure XP, level calculation, JSON mastery bonus loading, and the level-2 reward bonus.

### Step 10 - Frontend: Dungeon Run State UI

- Updated Angular dungeon API models.
- Updated `DungeonStateService` with route, boon, event choice, and checkpoint choice helpers.
- Updated the dungeon page to display:
  - Mechanic/pressure value
  - Active mechanic thresholds
  - Reward multiplier
  - Route options
  - Event choices
  - Checkpoint choices
  - Boon choices
  - Active temporary boons
  - Active boss effects
- Existing combat, event inspect/accept/ignore, checkpoint continue/withdraw, reward claim, and failed-run flows remain present.

### Step 11 - Frontend: Start Dungeon Strategy and Preparation UI

- Intentionally skipped by product decision.
- No strategy selector, preparation options, start-run strategy payload, or auto-running/offline dungeon strategy behavior will be implemented for this slice.

### Step 12 - Tests and Validation

- Added focused backend tests for:
  - Pressure clamping
  - Pressure threshold reward multipliers
  - Route generation and selection
  - Boon generation and selection
  - Boon duplicate prevention
  - Active boon attribute modifiers
  - Active boon ability modifiers
  - JSON boon definition loading
  - Combat-engine application of temporary ability modifiers
  - Checkpoint focus, push deeper, rest, withdraw, item trimming, and secured-only claiming
  - Treasure, shrine, and trap event choice effects
  - JSON event definition loading and dungeon-specific event resolution
  - Boss modifier generation and combat plan propagation
  - Custom mechanic threshold reward/state behavior
  - Regular enemy mechanic threshold modifiers
  - JSON dungeon definition mechanic and threshold loading
  - JSON route definition loading and dungeon-specific route generation
  - Dungeon mastery XP, level calculation, JSON bonus loading, and reward multiplier bonus
- Verified backend tests pass.
- Verified frontend build passes through direct Angular CLI invocation.

### Step 13 - Example Content: Goblin Mines

- Added Goblin Mines mechanic name: `Alarm Level`.
- Added Goblin Mines authored events for explosive storage and captured miner.
- Added Goblin Mines authored route table entries for combat, event, miniboss, and checkpoint rooms in `LL/src/API/API.LL/Data/dungeon-routes.json`.
- Existing Goblin Mines completion/content remains intact.

### Step 14 - Example Content: Forgotten Catacombs

- Added Forgotten Catacombs mechanic name: `Curse`.
- Added Forgotten Catacombs authored events for bound spirits and cursed reliquaries.
- Added Forgotten Catacombs authored route table entries for combat, event, miniboss, and checkpoint rooms in `LL/src/API/API.LL/Data/dungeon-routes.json`.
- Existing Forgotten Catacombs completion/content remains intact.

## Missing Or Not Yet Implemented

### Step 4 - Full Boon Combat Integration

Implemented for the requested Step 4 scope.

- JSON-backed boon definitions are loaded through `IDungeonBoonDefinitionProvider`.
- Attribute-style boon modifiers are applied to dungeon combat.
- Ability-style boon modifiers are applied to equipped essence abilities during dungeon combat.
- Boon generation is rarity-weighted and deterministic for the run state.

Remaining future polish, outside the Step 4 acceptance criteria:

- More authored boon content can be added over time.
- Boon targeting can become richer than exact ability effect IDs if future ability content needs tag-wide matching.

### Step 5 - Checkpoints

Implemented for the requested Step 5 scope.

### Step 6 - Event Room Future Polish

Implemented for the requested Step 6 scope.

Remaining future polish, outside the current acceptance criteria:

- Ambushes convert the event into an immediate combat room using the dungeon definition's existing combat encounter pool.
- Hidden routes use the next room as a known shortcut route rather than a fully authored alternate path graph.

### Step 7 - Make Bosses Reflect the Run

Implemented for the requested Step 7 scope.

Remaining future polish, outside the current acceptance criteria:

- Boss modifier definitions are currently code-backed IDs rather than a dedicated JSON boss-modifier catalog.
- Boss modifiers affect hostile attributes; future content can add boss-specific ability behavior if the combat engine needs it.

### Step 8 - Full Mechanic Threshold Customization

Implemented for the requested Step 8 scope.

Remaining future polish, outside the current acceptance criteria:

- Enemy and boss modifier IDs are still resolved by code-backed known IDs rather than a dedicated JSON modifier catalog.

### Step 9 - Dungeon Mastery

Implemented for the requested Step 9 scope.

### Step 11 - Frontend: Start Dungeon Strategy and Preparation UI

Intentionally skipped by product decision.

### Step 12 - Remaining Test Areas

Implemented for the requested scope. Strategy and preparation tests are intentionally omitted because Step 11 is out of scope.

### Step 13 - Full Goblin Mines Example Content

Implemented for the requested Step 13 scope.

Remaining future polish, outside the current acceptance criteria:

- More Goblin Mines route/event variants can be authored over time.

### Step 14 - Full Forgotten Catacombs Example Content

Implemented for the requested Step 14 scope.

Remaining future polish, outside the current acceptance criteria:

- More Forgotten Catacombs route/event variants can be authored over time.

### Step 15 - Final Cleanup

Partially complete.

- Backend and frontend build verification was completed.
- Focused backend tests pass.
- Migration was added but not applied.
- More cleanup should happen after any future JSON boss-modifier catalog work.

## Verification Completed

- Earlier verification had `dotnet build LL/LegendsLegacy.sln` passing.
- Full solution build should be rerun after locked API output DLLs are released by running `API.LL` process `50064` and Visual Studio process `11444`.
- `dotnet build LL/src/Infrastructure/Service/Services.LL/Services.LL.csproj --no-restore` passed.
- `dotnet build LL/src/Infrastructure/Persistence/Persistence.LL/Persistence.LL.csproj --no-restore` passed.
- `dotnet test LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --no-restore` passed with `146` tests.
- Angular build passed by invoking the local Angular CLI directly with the bundled Node runtime.

## Known Caveats

- `npm run build` could not be used directly in this environment because the npm shim points to a missing AppData npm CLI.
- Angular production build needs network access to inline Google Fonts.
- Some generated frontend files may show line-ending-only status noise after running frontend generation/build scripts.
