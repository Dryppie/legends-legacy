# Guild Expansion Implementation Status

This document describes the guild expansion implementation currently shipped in the Legends Legacy repository. It is a status note for the current code, not the full long-term guild design.

## Shipped

### Guild Currencies and Resources

Shipped:

- `Guild.GuildXp`
- `Guild.GuildLevel`
- `Character.GuildFavor`
- `Character.GuildHonors`
- `GuildResourceType.GuildSupplies`
- Guild-owned `GuildResource` rows for internal shared resources

Guild Favor and Guild Honors are personal character currencies. Guild XP and Guild Supplies are guild-owned progression resources.

Guild Supplies are earned through guild activity such as personal orders and missions. Manual deposits are intentionally not part of the current guild economy.

Guild level is calculated in `GuildMissionService` with the current first-pass formula:

```text
GuildLevel = max(1, GuildXp / 10000 + 1)
```

### Guild Missions

Shipped:

- Persistent mission tables:
  - `GuildMissionOption`
  - `GuildMissionInstance`
  - `GuildMissionContribution`
  - `PersonalGuildOrder`
  - `GuildMemberContributionPeriod`
  - `GuildContributionLedger`
- Lazy weekly mission option generation.
- Leader/officer weekly mission selection.
- Automatic default mission selection after the 24-hour selection grace period.
- Weekly mission contribution tiers:
  - None
  - Bronze
  - Silver
  - Gold
  - Platinum
- Weekly mission reward claim flow.
- Personal daily orders generated lazily per guild member.
- Personal order reward claim flow.
- Contribution aggregation for daily and weekly periods.
- Contribution idempotency through `GuildContributionLedger`.

Current weekly mission definitions:

- Monster Extermination
- Dungeon Expedition
- Craftsmen's Commission

Current personal daily order definitions:

- Defeat 100 creatures
- Clear 5 dungeon rooms
- Complete 20 tempering actions

Claimed personal orders award Guild Favor, Guild XP, Guild Supplies, and contribution-period reward aggregates.

### Contribution Hooks

Shipped gameplay contribution sources:

- Idle combat victories contribute `CreaturesDefeated`.
- Dungeon room completion contributes `DungeonRoomsCleared`.
- Completed dungeon runs contribute `DungeonsCompleted`.
- Crafting item creation contributes `ItemsCrafted`.
- Tempering sessions contribute `TemperingActionsCompleted`.
- Tempering-completed items also contribute `ItemsCrafted`.

### Guild Shop

Shipped:

- Guild shop overview service.
- Guild shop purchase service.
- Weekly purchase tracking through `GuildShopPurchase`.
- Character currency spending:
  - Guild Favor
  - Guild Honors
- Shop reward grants:
  - Cinders
  - Soulstones
  - Fate Echo
- Shop stock is loaded through `Data/guild-content.json` with in-code defaults as fallback.
- Weekly and prestige stock use deterministic weekly rotation.
- Market Office level gates common, expanded, weekly, and prestige stock.
- Purchase validation for:
  - weekly purchase limit
  - weekly contribution requirement
  - Market Office requirement
  - Guild Favor balance
  - Guild Honors balance

Current JSON-backed shop stock:

- Common:
  - Cinder Purse
  - Soulstone Cache
  - Cinder Satchel
- Weekly:
  - Builder's Crate
  - Soulstone Bundle
  - Echo Stipend
- Prestige:
  - Honor Reliquary
  - Elder Cache

### Guild Buildings

Shipped:

- The previous altar/smith/obelisk upgrade-definition system has been removed.
- `GuildBuilding` persists one building state per guild/building type.
- `GuildActivityLog` records construction and upgrade activity.
- Guild Hall exists as the permanent root building and is created lazily for existing guilds.
- Guild Hall level requirements gate access to advanced buildings.
- Guilds can construct every non-permanent building once requirements and Guild Supply costs are met.
- Leaders and officers can spend Guild Supplies to construct and upgrade buildings.
- Construction and upgrades use lazy completion through `CompletesAt`.
- Building definitions expose a benefits list with active, future, and planned entries.
- Building definitions are loaded through `Data/guild-content.json` with in-code defaults as fallback.
- Mission Board level improves guild mission option count, personal daily order count, and mission rewards.
- Market Office level gates guild shop categories.
- Treasury level reduces building Guild Supply costs and construction/upgrade time.
- The frontend Buildings tab shows building benefits, costs, construction/upgrade state, and activity log entries.

Current JSON-backed building list:

- Guild Hall
- Mission Board
- Market Office
- Raid Hall
- War Room
- Workshop
- Training Grounds
- Essence Sanctum
- Treasury

### Guild Vault Removal

Shipped removal:

- The manual Vault tab has been removed from the in-guild frontend.
- The guild donation API and command have been removed.
- `GuildService.DonateToGuildAsync` has been removed.
- Guild members can no longer deposit Cinders, Soulstones, or inventory materials into guild resources.
- `GuildResourceType` now only represents `GuildSupplies`.

Guild-owned resources remain internally for systems such as missions and buildings. Treasury should be treated as future resource infrastructure, not a deposit box.

### API Endpoints

Shipped through `GuildController` and MediatR handlers:

```http
GET  /api/v1/guild/getBuildings
POST /api/v1/guild/constructBuilding
POST /api/v1/guild/upgradeBuilding
GET  /api/v1/guild/getMissions
POST /api/v1/guild/selectMission
POST /api/v1/guild/claimOrderReward
POST /api/v1/guild/claimWeeklyMissionReward
GET  /api/v1/guild/getShop
POST /api/v1/guild/purchaseShopItem
```

The removed endpoint is:

```http
POST /api/v1/guild/donate
```

### Frontend

Shipped in the Angular `ll` presentation app:

- Buildings tab.
- Missions tab.
- Shop tab.
- Raids and Wars locked-state tabs.
- Guild mission models.
- Guild shop models.
- Guild building models.
- Guild realtime handling for building changes.
- Guild state service integration for:
  - loading buildings
  - constructing buildings
  - upgrading buildings
  - loading missions
  - selecting missions
  - claiming personal order rewards
  - claiming weekly mission rewards
  - loading shop
  - purchasing shop items

The Vault tab and donation UI have been removed.

### Tests

Shipped focused tests:

- `GuildMissionServiceTests`
  - mission overview lazily generates weekly options and daily orders
  - contribution idempotency prevents duplicate progress
  - personal order rewards can be claimed once
- `GuildBuildingServiceTests`
  - overview lazily creates Guild Hall
  - construction spends Guild Supplies without requiring building slots
  - overview lazily finalizes completed construction

## Partially Shipped

### Guild Missions

Partially shipped:

- Combat, Dungeon, Crafting, and Tempering are wired to gameplay contribution sources.
- Mission definitions are loaded from `Data/guild-content.json`.
- Mission Board level can add an additional weekly option and improve mission rewards.
- Upgraded Mission Board weekly options use deterministic weekly rotation.

Still partial:

- Gathering, Colosseum, Essence, Raid, and War metrics are modeled but not connected to gameplay services.
- Mission reset display is still lightweight and not a live ticking timer.
- There is no guild activity log entry for mission selection.

### Personal Guild Orders

Partially shipped:

- Daily order definitions are data-driven.
- Mission Board level can add a fourth daily order.
- Leaving a guild relies on membership access checks rather than explicit cleanup of unclaimed order rewards.

Still partial:

- No order rerolling.
- No weekly personal order set.
- No rare order variants.
- No dynamic order selection by player activity profile.

### Contribution Eligibility and Anti-Abuse

Partially shipped:

- Contribution tiers exist.
- Weekly mission rewards require contribution.
- Shop purchases can require weekly contribution.
- Contribution events use idempotency keys.

Still partial:

- No officer action audit logs outside building construction/upgrade logs.
- No account-level weekly caps.
- No retention/cleanup policy for contribution ledgers.

Intentionally excluded:

- Member scaling logic.
- A 48-72 hour lockout that prevents new guild members from taking guild quests.

### Guild Shop

Partially shipped:

- Stock is loaded from `Data/guild-content.json`.
- Common, weekly, and prestige stock categories exist.
- Weekly and prestige stock rotate deterministically by week.
- Market Office requirements are enforced.
- Rewards are limited to direct currency/resource grants.

Still partial:

- Item, chest, cosmetic, and title rewards are not implemented.

### Guild Buildings

Partially shipped:

- Raid Hall and War Room are buildable placeholders with frontend locked-state destinations.
- Treasury implements current construction cost/time efficiency rather than deposit storage.

Still partial:

- No admin dashboard tooling for building tuning.
- No full Raid Hall or War Room gameplay systems.
- No Training Grounds or Essence Sanctum gameplay effects.
- No Treasury deposit/storage behavior.

### Frontend

Partially shipped:

- Buildings, Missions, and Shop tabs are functional.
- Locked reasons are shown for shop items.
- Mission/order progress is shown.
- Building benefit, cost, status, and activity log information is shown.
- Shop items are grouped by stock category with requirement and reward cards.
- Raids and Wars have locked-state tabs.

Still partial:

- No contribution leaderboard.
- Reward preview is richer for direct currency rewards, but item/chest/cosmetic/title previews are not available until those rewards exist.

## Not Shipped Yet

### Guild Raids

Not shipped:

- raid definitions
- raid registration
- raid participants
- raid instances
- raid HP scaling
- raid attempts
- raid phases
- raid contribution scoring
- raid completion/failure finalization
- raid rewards
- raid frontend tab/page

### Guild Wars

Not shipped:

- war registration
- war rosters
- roster locks
- matchmaking
- defense snapshots
- war maps
- attacks
- scoring
- seasons
- ratings
- war rewards
- war frontend tab/page

### Permission System Expansion

Not shipped:

- explicit permission enum/model
- centralized permission service
- granular permissions such as:
  - `SelectGuildMission`
  - `SpendGuildSupplies`
  - `StartRaid`
  - `RegisterWar`
  - `ManageWarDefense`

Current mission selection and Guild Supply spending use:

```text
Leader or Officer
```

### Data-Driven Guild Content

Shipped:

- JSON mission definitions
- JSON shop stock
- JSON/data-driven building definitions

Partially shipped:

- `Data/guild-content.json` is consumed by a JSON-backed provider with in-code defaults as fallback.

Still partial:

- reward profile definitions
- admin dashboard tooling for guild content
- schema validation or diagnostics for guild content

## Persistence and Migrations

Current generated migration:

```text
20260627123523_AddGuildExpansion
```

It is generated on top of the existing `20260626132310_BaseMigration` baseline and includes the current guild expansion schema:

- `GuildLevel` and `GuildXp` to `Guilds`
- `GuildFavor` and `GuildHonors` to character rows in `Entities`
- `GuildContributionLedgers`
- `GuildMemberContributionPeriods`
- `GuildMissionInstances`
- `GuildMissionOptions`
- `GuildMissionContributions`
- `PersonalGuildOrders`
- `GuildShopPurchases`
- `GuildBuildings`
- `GuildActivityLogs`

It also drops the previous `GuildBuildingUpgrade` table. `GuildBuildings` no longer includes a building slot column. The current schema only models `GuildSupplies` as a guild resource.

The migrations have been generated only. They have not been applied to any database by this implementation pass.

## Verification

The following commands passed after implementation:

```powershell
dotnet test LL\tests\EssenceSystem.Tests\EssenceSystem.Tests.csproj
dotnet build LL\src\Infrastructure\Service\Services.LL\Services.LL.csproj
dotnet build LL\src\Infrastructure\Persistence\Persistence.LL\Persistence.LL.csproj
dotnet build LL\src\API\API.LL\API.LL.csproj -p:OutDir=C:\repos\Legends-Legacy\legends-legacy\artifacts\api-build-verify\
& "C:\Users\HrHoe\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe" ".\node_modules\@angular\cli\bin\ng.js" build --configuration development
```

The Angular build was run from `LL/src/Presentation/ll` through the local Angular CLI because the global `npm` shim on this machine is missing `npm-cli.js`.

## Deployment Notes

Before deployment:

- Apply the EF Core migrations to the target environment database through the normal deployment process.
- Confirm existing guild rows receive `GuildLevel = 1`.
- Confirm whether the target database already has old guild building or vault data. The current generated migration represents the current desired schema and does not preserve old guild building upgrades or non-`GuildSupplies` guild vault deposits.
- Confirm frontend environment uses the updated API.
- Consider whether the new guild tables need operational monitoring or cleanup jobs later, especially `GuildContributionLedgers`.

No infrastructure-as-code changes were made.

## Recommended Next Steps

1. Add contribution hooks for Colosseum and Essence gameplay.
2. Add guild activity logs for mission selection, reward claims, and shop purchases.
3. Add schema validation/diagnostics for `Data/guild-content.json`.
4. Add item, chest, cosmetic, and title reward support to the guild shop.
5. Add contribution leaderboard UI.
6. Implement Guild Raids before Guild Wars.
7. Add admin dashboard tooling for guild content tuning.
