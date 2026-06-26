# Title & Achievement System Implementation Status

Last updated: 2026-06-25

This tracks the implementation based on `C:\Users\HrHoe\Downloads\title-achievement-system-design.md`.
The target service is the primary Legends Legacy game app under `LL/`.

## Top 5 Work Items

1. **Backend compile and equipped-title display** - completed.
2. **Real progress hooks for Colosseum and dungeons** - completed for the MVP hooks.
3. **Focused backend tests** - completed.
4. **EF Core migration** - completed, generated only.
5. **Angular achievement/title page and API clients** - completed as an MVP page.

## Completed Follow-Up Top 3

1. **Idle combat achievement hooks** - completed.
2. **Essence archive/loadout/ascension hooks** - completed.
3. **Colosseum streak reset and comeback tracking** - completed.

## Completed Next Top 3

1. **Crafting achievement hooks** - completed for direct crafting, blueprint learning, idle tempering attempts, masterpieces, set-tagged crafted items, cursed tempering outcomes, and high-quality low-potential completions.
2. **General progression hooks** - completed for first character/account creation and character-level milestones.
3. **Unlock announcement and audit path** - completed as a rare-unlock realtime event plus a current-character recalculation endpoint.

## Completed Chat Hookup Step

1. **Data-driven achievement system messages** - completed.
2. **Per-player achievement chat delivery** - completed through the existing game realtime event stream.
3. **Optional global achievement chat delivery** - completed through data-driven global message templates.

## Implemented

### Domain and persistence

- Added achievement/title domain entities and enums under `LL/src/Core/Domain/Models/Achievements/`.
- Added achievement/title `DbSet` entries to `IDbContext` and `LLDbContext`.
- Added EF Core configurations for achievement definitions, player progress, title definitions, and title unlocks.
- Added `Character.EquippedTitleDefinitionId` and optional FK mapping to `TitleDefinition`.
- Added dungeon run tracking fields:
  - `DeathsDuringRun`
  - `UsedCheckpointRetreat`
- Generated EF migration `20260625193400_AddAchievementsAndTitles`.

### Seed catalog

- Added data-driven JSON seed data for the MVP achievement catalog under `LL/src/API/API.LL/Data/achievements/`.
- Added data-driven JSON seed data for the MVP title catalog under `LL/src/API/API.LL/Data/titles/`.
- Split achievement definitions by gameplay category:
  - `general.json`
  - `combat.json`
  - `essences.json`
  - `dungeons.json`
  - `crafting.json`
  - `colosseum.json`
  - `hidden.json`
  - `legacy.json`
- Split title definitions by gameplay category under `LL/src/API/API.LL/Data/titles/`:
  - `general.json`
  - `combat.json`
  - `essences.json`
  - `dungeons.json`
  - `crafting.json`
  - `colosseum.json`
  - `hidden.json`
  - `legacy.json`
- Wired the catalog into `LLDbContextExtensions.SeedData`.
- Definitions are updated by stable key rather than EF `HasData`, which keeps the catalog easier to evolve.
- JSON entries use readable string keys instead of GUIDs; internal database GUIDs are derived deterministically from those keys by the seed loader.
- Achievement JSON entries can define `playerSystemMessageTemplate` and `globalSystemMessageTemplate`.
- All seeded achievements currently define a player system message template.
- Exalted/Mythic seeded achievements currently define a global system message template.

### Achievement service

- Added `IAchievementService` and `AchievementService`.
- Implemented achievement overview/list projection.
- Implemented title list projection.
- Implemented hidden and obscured achievement masking.
- Implemented Legacy Renown rank calculation from achievement points.
- Implemented progress updates and non-repeatable unlock guarding.
- Implemented title rewards from source achievements.
- Implemented equip/unequip title validation.
- Implemented same-account Colosseum achievement prevention.
- Implemented dungeon start and completion progress helpers.
- Implemented idle combat progress helper.
- Implemented essence absorption/loadout/ascension progress helpers.
- Implemented Colosseum win-streak reset and hidden comeback tracking.
- Implemented crafting progress helpers.
- Implemented character creation and character-level progress helpers.
- Implemented current-state recalculation for repair/backfill of durable progress facts.
- Implemented data-driven achievement unlock message formatting.
- Implemented per-player achievement unlock realtime publishing for every unlock with a player system message.
- Implemented optional global achievement unlock realtime publishing for unlocks with a global system message.

### API layer

- Added thin MediatR-backed controllers:
  - `GET /api/v1/achievements/overview`
  - `GET /api/v1/achievements`
  - `POST /api/v1/achievements/recalculate`
  - `GET /api/v1/titles`
  - `POST /api/v1/titles/equip`
  - `POST /api/v1/titles/unequip`
- Added achievement/title queries, equip/unequip commands, and recalculation command.

### Integration hooks

- Wired dungeon run start progress from `StartDungeonRunCommand`.
- Wired completed dungeon reward claims to achievement progress from `ClaimDungeonRewardsCommand`.
- Captured dungeon completion facts before run cleanup:
  - completed dungeon id
  - deathless completion
  - no-checkpoint-retreat completion
  - defeated boss keys
- Wired Colosseum battle completion into achievement progress from `ArenaBattleCompletedEventHandler`.
- Wired idle combat reward processing into achievement progress from `IdleCombatOutcomeProcessor`.
- Wired successful Essence operations into achievement progress from `EssenceSystemService`.
- Wired direct crafting and blueprint learning into achievement progress from `CraftingService`.
- Wired idle tempering summaries into achievement progress from `CraftingService` and `TemperingService`.
- Wired first-character/account progress from `UserCreatedEventHandler`.
- Wired character-level milestone progress from `CharacterLevelUpEventHandler`.

### Idle combat hooks

- Defeated monsters now progress `MonstersDefeated`.
- Defeated creature families now progress `CreatureFamilyDefeated`.
- Idle combat losses now progress `PlayerDefeats`.
- Low-health idle combat wins now complete matching `WinCombatBelowHealthPercent` achievements.

### Essence hooks

- Absorbing a new Essence now progresses `EssencesAbsorbed`.
- Soul Archive size now progresses `UniqueEssencesArchived`.
- Completed Essence collection tags now progress `EssenceCollectionCompleted`.
- Saving or activating an active loadout now progresses `EquippedEssenceCountReached`.
- Ascending an Essence now progresses `EssencesAscended` and `EssencesAscendedToTier`.

### Colosseum detail rules

- Win streak progress now resets on non-victory.
- Hidden comeback progress now tracks losing streaks without unlocking early.
- Comeback achievement now unlocks only when a player wins after meeting the losing-streak requirement.

### Crafting hooks

- Direct crafting now progresses `ItemsCrafted`.
- Crafting items with set-style tags now progresses `SetItemsCrafted`.
- Learning a blueprint now progresses `BlueprintsUnlocked`.
- Idle tempering attempts now progress `ItemsTempered`.
- Newly created masterpieces now progress `MasterpiecesCrafted`.
- Negative tempering outcomes now progress `CursedCraftingOutcomes`.
- Completed high-quality items below the configured potential threshold now complete matching `HighQualityItemCraftedBelowPotential` achievements.

### General progression hooks

- First character/account creation now progresses `AccountCreatedOrFirstCharacterCreated`.
- Character level-ups now progress `CharacterLevelReached` using max-level semantics.

### Announcements and recalculation

- Added `AchievementUnlockedMsg` realtime event for achievement system chat.
- Achievement unlock messages support template placeholders:
  - `{achievementKey}`
  - `{achievementName}`
  - `{points}`
  - `{titleName}`
  - `{characterName}`
- Every unlock with `playerSystemMessageTemplate` publishes a character-scoped system chat event.
- Every unlock with `globalSystemMessageTemplate` publishes a world-scoped system chat event.
- Added `POST /api/v1/achievements/recalculate` for the signed-in character.
- Recalculation can repair/backfill progress from durable current-state facts:
  - first character/account existence
  - current character level
  - current Essence archive/loadout/ascension state
  - known blueprint unlocks
  - currently held crafted/masterpiece/set/high-quality-low-potential items
  - dungeon completion records
  - Colosseum match records

### Equipped title display

- Added equipped-title DTO data to character/profile projections.
- Included `EquippedTitleDefinition` when loading character data.
- Updated the Angular character overview profile label to prefer the equipped title display name.

### Angular MVP

- Added shared frontend achievement/title models.
- Added Angular `AchievementService` API client.
- Added a character achievements route.
- Added sidebar navigation entry under Character.
- Added an MVP achievements page with:
  - overview stats
  - category filters
  - progress bars
  - hidden/obscured-safe display from backend DTOs
  - unlocked and locked title lists
  - equip/unequip actions
- Added `AchievementUnlockedMsg` to the Angular realtime event map.
- The Angular chat service now converts achievement unlock realtime events into local `System` chat messages.
- The chat UI now exposes a `System` room and renders system senders without requiring a character tag.
- Added achievement collection search, status filtering, and sort controls.
- Added title search, title status filters, and an equipped-title preview panel.

### Chat persistence

- Added `System` chat channel support to `LL-Chat`.
- Added a protected `POST /api/v1/chat/System` endpoint in `LL-Chat` for service-written system messages.
- Added game-side achievement system chat persistence through an optional HTTP publisher.
- Included global system messages in `LL-Chat` history for all players.
- Included player-scoped system messages in `LL-Chat` history only for the target character.

### Tests

Added focused backend tests for:

- progress unlocks once
- achievement points/title rewards are awarded once
- locked title equip rejection
- character-bound title ownership validation
- hidden/obscured masking
- Legacy Renown thresholds
- same-account Colosseum achievement prevention
- dungeon deathless/no-retreat conditions
- idle combat monster/family/defeat/low-health progress
- Colosseum streak reset and comeback unlock semantics
- essence archive/loadout/ascension progress
- crafting item/tempering/masterpiece/cursed/set/low-potential progress
- current-state recalculation for general and blueprint progress

## Partially Implemented

### Achievement coverage

The achievement engine and catalog exist, and dungeon, idle combat, Colosseum, Essence, crafting, and general progression MVP hooks are wired into gameplay.

Still partial:

- more specialized hidden combat cases, such as `LoseToSpecificCreatureWhileOverpowered`
- seasonal/server-first categories beyond the seeded MVP catalog

### Colosseum detail rules

Basic Colosseum participation, wins, win streak increments/reset, rating-upset progress, same-account filtering, and hidden comeback tracking are implemented.

Still partial:

- more detailed seasonal/ranked rules

### Admin/recalculation tooling

The player API now has a current-character recalculation endpoint. Still partial:

- no admin dashboard UI for recalculation
- no bulk repair job across many accounts
- recalculation can only repair facts that are currently persisted; it cannot reconstruct historical crafted/destroyed items or other events that were never stored

### Frontend polish

The Angular page is beyond the first MVP now, with search/filter/sort controls and a better title browser.

Still partial:

- unlock detail modals
- achievement reward claim/history presentation
- deeper rarity/category visual polish

## Not Done Yet

- Database migration has not been applied to any database.
- No production deployment or infrastructure changes have been made.
- No admin dashboard management/recalculation UI has been implemented.
- No full end-to-end browser/manual QA against a running API has been performed.
- No bulk backfill job has been written for all accounts.
- Achievement system chat persistence requires environment-specific service configuration before it is active.

## Verification

Completed successfully:

```powershell
dotnet build LL\LegendsLegacy.sln
dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj
dotnet ef migrations add AddAchievementsAndTitles --project LL\src\Infrastructure\Persistence\Persistence.LL --startup-project LL\src\API\API.LL
npm.cmd ci
npm.cmd run build
```

Latest backend verification after the completed follow-up top 3:

```powershell
dotnet build LL\LegendsLegacy.sln
dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj
```

Latest backend verification after the split JSON catalog change:

```powershell
dotnet build LL\LegendsLegacy.sln
dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj
```

Latest backend verification after crafting/general/recalculation work:

```powershell
dotnet build LL\LegendsLegacy.sln
dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj
```

Latest verification after achievement system-chat hookup:

```powershell
dotnet build LL\LegendsLegacy.sln
dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj
npm.cmd run build
```

Latest verification after splitting title JSON into `Data/titles`:

```powershell
dotnet build LL\LegendsLegacy.sln
dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj
```

Latest verification after `LL-Chat` persistence and frontend polish:

```powershell
dotnet build LL-Chat\LL-Chat.sln
dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj
dotnet build LL\LegendsLegacy.sln
npm.cmd run build
```

The focused test project now has `188` passing tests.

Notes:

- The first plain `npm run build` failed because the local `npm` shim pointed to a missing global npm install; rerunning with `npm.cmd` fixed that.
- The latest first sandboxed Angular build failed because Angular's resolver could not read parent directories in the sandbox; rerunning the same command with approval succeeded.
- Angular build completed with an existing bundle budget warning: latest initial bundle total was `739.93 kB` against a `512.00 kB` budget.
- `LL-Chat` build reported the existing AutoMapper advisory warning and nullable warnings in chat hub/rate limiter code.
- `npm.cmd ci` reported dependency audit findings from the installed dependency tree: 77 vulnerabilities. No dependency versions were changed.

## Deployment Notes

- The generated migration adds achievement/title tables, dungeon run fact columns, and the character equipped-title FK.
- The generated migration also adds achievement system message template columns.
- The migration was generated only; it was not applied.
- The achievement/title catalog seeds at application startup through existing seed flow.
- `LL` now reads optional `Chat:SystemMessages:BaseUrl`, `Chat:SystemMessages:Secret`, and `Chat:SystemMessages:TimeoutSeconds` configuration.
- `LL-Chat` now reads `SystemMessages:Secret` configuration for the internal system-message endpoint.
- No secret values were committed; the placeholder values are empty and must be supplied per environment.
- No deployment or infrastructure repositories were changed.
