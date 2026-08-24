# Dead Code & Obsolete Architecture Audit

## 1. Executive Summary

The repository contains several meaningful architectural fossils, but most current feature systems are well connected. The largest cleanup opportunities are:

- A fully abandoned level-trigger architecture.
- An old combat simulator that remains publicly routable despite key operations being no-ops.
- Gameplay code depending on the development AdminDashboard service layer.
- World Tower v1 playback retained beside the default v2 bundle architecture.
- Two substantial clusters of unreachable Angular artifacts.
- Several unused DI registrations, repository interface methods, domain models, configuration sections, and project remnants.

The dominant current architecture is coherent: ASP.NET controllers and hosted workers call MediatR/application or feature services; EF Core repositories implement persistence; AutoMapper and MediatR use assembly discovery; SignalR is delivered through an outbox/realtime layer; Quartz runs in `Worker.LL`; Angular uses standalone components, lazy routes, signal-backed state services, and REST plus SignalR invalidation.

Controllers, MediatR handlers, EF configurations, AutoMapper resolvers, hosted services, migrations, and serialized DTOs were not classified as dead merely because their type names had few static references.

---

## 2. Highest-Value Findings

### Finding: Abandoned level-trigger architecture

**Location:** `LL/src/Core/Common/Helpers/Leveling/LevelTriggerLoader.cs` and `LL/src/Core/Domain/Components/Leveling`  
**Classification:** A — Definitely Dead  
**Cleanup value:** High

**What I found:**

- An eight-file condition/action framework based on `LevelTrigger`, `ILevelCondition`, and `ILevelAction`.
- Its loader no longer initializes its collection; JSON loading is entirely commented out.
- `Data/progression/levelTriggers.json` is zero bytes.
- The two actions return `Task.CompletedTask` and do nothing.

**Evidence:**

- Loading is disabled in `LevelTriggerLoader.cs`.
- No consumer calls `LevelTriggerLoader.Instance` or `GetLevelTriggers()`.
- Current progression uses `LevelingService`, `JsonCharacterExperienceProgressionProvider`, and `CharacterLevelUpEvent`.

**Recommended next step:** Remove the loader, trigger model, two conditions, two no-op actions, interfaces, and empty JSON in one change.

**Risk of removal:** Low.

### Finding: Old simulator remains routable but does not simulate authored abilities

**Location:** `LL/src/API/API.LL/Controllers/V1/_SimulateController.cs`, `LL/src/Infrastructure/Service/Services.LL/_Simulator/SimulatorService.cs`  
**Classification:** B — Probably Obsolete  
**Cleanup value:** High

**What I found:**

- Two API endpoints still expose the original simulator.
- The service is registered and reachable through MediatR.
- Both ability-selection methods return completed tasks without doing anything.
- Essence-combination calculation always returns an empty combination.

**Evidence:**

- `PickRandomAbilities` and `PickSpecificAbility` are no-ops.
- `GetEssenceComboKey` returns an empty combination.
- No frontend, build script, test, or documentation invokes the endpoints.
- Current diagnostics use `AbilityBalanceSimulator`, `AbilityBalanceAuditService`, calibration runners, the dashboard diagnostics API, and `tools/BalanceCalibration`.

**Recommended next step:** Confirm the endpoints have no manual or external consumers, then remove controller → commands → interface → DI registration → service as one chain.

**Risk of removal:** Medium because the endpoints are technically externally callable.

### Finding: Gameplay depends on the development AdminDashboard layer

**Location:** `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Dungeon/DungeonEncounterParticipantResolver.cs`, `LL/src/API/API.LL/Program.cs`  
**Classification:** B — Probably Obsolete architecture  
**Cleanup value:** High

**What I found:**

- Dungeon combat imports `Application.Interfaces.Services.AdminDashboard.ICreatureService`.
- This forces the production game API to reference `Services.AdminDashboard` and register all AdminDashboard services.
- The actual dungeon operation only needs `ICreatureRepository.GetCreaturesByKey`.
- `Program.cs` itself labels this dependency as temporary.

**Recommended next step:** Move the creature-key lookup behind a gameplay-owned interface or repository dependency. Then remove `Services.AdminDashboard` from the game API’s references and DI registration while retaining it in the development dashboard host.

**Risk of removal:** High if attempted as a simple deletion; low-to-medium as a focused dependency-boundary cleanup.

### Finding: World Tower v1 and v2 playback coexist

**Location:** `LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerOptions.cs`, `LL/src/Presentation/ll/src/app/features/game/world/tower/rally/tower-rally.component.ts`  
**Classification:** B — Probably Obsolete  
**Cleanup value:** High

**What I found:**

- New attempts default to compact schema-v2 bundle playback.
- The old architecture still includes frame dispatch, a hosted playback worker, `WorldTowerCombatFrameUpdated`, frame recovery endpoints, serialized legacy timelines, and frontend incremental-frame handling.
- The frontend explicitly branches on `schemaVersion >= 2` and retains v1 fallback recovery.

**Recommended next step:** Verify production contains no schema-v1 attempts and that rollback compatibility is no longer required. Then remove v1 in a dedicated migration-aware cleanup.

**Risk of removal:** High until persisted production data and rollout status are verified.

### Finding: Main Angular application has an unreachable artifact cluster

**Location:** `LL/src/Presentation/ll/src/app`  
**Classification:** A — Definitely Dead  
**Cleanup value:** High

**What I found:**

- 27 production TypeScript files, representing 46 files when associated templates, styles, and specs are included, have no inbound import, route, or template-selector reference.
- Examples include `auth.component.ts`, `public.component.ts`, `navbar.component.ts`, and `combat-overview.component.ts`.
- The current auth and public route files load child route trees directly, bypassing the wrapper components.

Other unreachable groups include:

- `EquipmentViewComponent`, `GuildMembersComponent`, and `LandingHeaderComponent`.
- Old banner, character badge/attributes, combat avatar/stats/overview, back button, equipment-slot, filters, and three marketplace page components.
- `AudienceDto`, older audience interfaces, three DTO/model files, two pipes, and `rarity.utils.ts`.

**Recommended next step:** Delete this cluster in small feature-oriented groups and run the Angular production build after each group.

**Risk of removal:** Low.

### Finding: Development dashboard contains copied, unreachable player UI

**Location:** `LL/src/Presentation/dashboard/src/app`  
**Classification:** A — Definitely Dead  
**Cleanup value:** Medium–High

**What I found:**

- The dashboard routes only reach creatures, items, diagnostics, essence catalog, the dashboard shell, and sidebar.
- At least 28 production files have no inbound import, including many copied player components, validators, models, pipes, `CharacterService`, and `named-storage-keys.ts`.
- Representative dead folders include `shared/components/character*`, equipment overview, inventory slot, current action, modal container, profession header, countdown, banner, button, dropdown, and several validators.

**Recommended next step:** Remove the copied shared UI in groups, preserving only dependencies reachable from `dashboard.routes.ts`.

**Risk of removal:** Low.

---

## 3. Architectural Fossils

### Level-trigger rules

Originally intended to load declarative level conditions and actions from JSON. Replaced by explicit progression providers and MediatR level-up events. Nothing calls the old loader, and its data file is empty.

### First-generation combat simulator

Originally intended for essence combination testing. Replaced by the current `FastCombatEngine` diagnostics and calibration tools. It remains routed but has lost its ability-selection implementation.

### AdminDashboard as a gameplay dependency

The development content editor originally supplied a general creature service. Dungeon combat still consumes that dashboard abstraction, making the production API register the dashboard layer.

### World Tower per-frame playback

Originally persisted and dispatched individual frames through a worker and SignalR. Replaced by immutable compressed schema-v2 bundles played against server timestamps. The old path remains for persisted-data rollout compatibility.

### Compatibility-shaped API contracts

Colosseum exposes REST-style routes and PascalCase predecessor routes on the same actions. Character actions similarly emit old `NextResolutionAt` and `HasPendingCombatResolution` aliases. These are intentional fossils, but their retirement criteria are not encoded.

### Old combat/domain vocabulary

Several unused types appear to predate the current `FastCombatEngine`, runtime ability catalog, checkpoints, and combat event model:

- `AttackOutcome`, `DamageResult`, and `CombatLogEntry`.
- `CombatConstants`, `ArmorDamageReductionConstants`, and `ElementConstants`.
- `CreatureRoles`, `CreatureDefenseTypes`, `BossProfiles`, and `CreatureTemplate`.
- `SummonCreatureFactory`, which hard-codes old summoned-creature behavior.

No current code, tests, DI, EF mapping, or JSON binding references these symbols.

---

## 4. Cleanup Chains

1. **Level-trigger chain**  
   `LevelTriggerLoader` → `LevelTrigger` → condition/action interfaces → four implementations → empty `levelTriggers.json`.

2. **Simulator chain**  
   `_SimulateController` → two MediatR commands/handlers → `ISimulatorService` → `SimulatorService` → DI registration.

3. **Admin dependency chain**  
   `DungeonEncounterParticipantResolver` → AdminDashboard `ICreatureService` → AdminDashboard `CreatureService` → full AdminDashboard DI registration → production API project reference.

4. **World Tower v1 chain**  
   Legacy timeline/cursor persistence → playback worker → frame publication event → SignalR contract → frame recovery endpoint → Angular v1 playback and recovery branch → legacy options.

5. **Tier-package chain**  
   `TierPackage` → `ITierPackageProvider` → `InMemoryTierPackageProvider` → DI registration. No consumer resolves the interface.

6. **Old chat grouping chain**  
   Unused `ChatRoute` → unused `JoinPublic`/`LeavePublic`; clients are automatically joined to `pub:` and messages are sent to `pub:`, not `pub:{room}`.

7. **Angular orphan chains**  
   Unreachable component class → template/style → component spec → feature-specific model/pipe/helper.

---

## 5. Definitely Dead

High-confidence removal candidates:

- The complete level-trigger chain described above.
- Empty orphan project `LL/src/Infrastructure/Persistence/Persistence.AdminDashboard/Persistence.AdminDashboard.csproj`: zero source files, not referenced, not in the solution, and still targets .NET 8.
- Unused transaction skeleton: `Transaction`, `PaymentMethod`, and `Status` under `Domain/Models/Users/Transactions`.
- Unused DI service `IDateTimeProviderService` and its implementation/registration.
- Unused tier-package provider chain.
- Registered but unconsumed `ICombatEncounterResolver` / `DefaultCombatEncounterResolver`. Feature-specific orchestrators compose the same lower-level services directly.
- Unregistered and incomplete `CombatEncounterFactory`, whose encounter selection throws `NotImplementedException`.
- Unused `SharedRandomSource`; the active reward flow uses resolution-scoped randomness.
- Unused query-profile members `SnapshotReady`, `Basic`, and `CombatReadyWithLoot`; other members in those files are active.
- Marketplace repository interface methods whose implementation only throws. Current marketplace mutations use the service’s actual transaction flow.
- `ICreatureRepository.UpdateCreatureAsync` and its throwing implementation. Admin editing writes JSON instead.
- Four unused AdminDashboard mapping artifacts: `EquipmentMapper`, `EquipmentToJsonDto`, `ItemAttributeMapper`, and `ItemAttributeModifierToJsonDto`.
- `ChatRoute`, `JoinPublic`, `LeavePublic`, and likely `LeaveGuild`. The first three have no client caller and do not match the actual public-group delivery model.
- Main Angular orphan cluster.
- Development-dashboard orphan cluster.
- Unused Angular service/state methods including `getCurrentAction`, `setOverview`, `setDungeons`, `selectedInventoryEssenceQuantity`, `setInvites`, `setAllGuilds`, `decrementListing`, and `emitUpdate`.
- `package-lock.json.backup` and four unreferenced `placeholder1.png`/`placeholder2.png` assets.
- Stale `.csproj` folder and exclusion declarations for directories that no longer exist.

---

## 6. Probably Obsolete

- Old simulator chain.
- AdminDashboard dependency from dungeon combat.
- World Tower v1 playback chain.
- Colosseum compatibility aliases such as `GetStatus`, `GetArenaOpponents`, `StartArenaBattle`, `UpdateDefenseSnapshot`, `GetChampionMarket`, and `PurchaseChampionMarketItem`. The checked-in Angular client uses the newer REST-style routes where both exist.
- Character-action `NextResolutionAt` and `HasPendingCombatResolution` compatibility aliases. The current frontend still contains fallback reads, so backend and frontend cleanup must be coordinated.
- Raid playback manifest method and corresponding endpoint. Current gameplay fetches playback bundles directly.
- World Tower client methods `getAttemptPlayback` and `getAttemptReport`; production code does not call them, although the server endpoints remain externally reachable.

---

## 7. Needs Investigation

- Confirm external, mobile, or manual consumers before removing any routed compatibility endpoint. Repository-local reachability cannot prove the absence of deployed clients.
- Confirm persisted World Tower schema-v1 rows and rollback requirements before deleting old playback columns, cursor state, events, or the worker branch.
- Decide whether superseded design documents are intentionally retained as history. If so, move them to an explicit archive rather than silently deleting them.

---

## 8. Stale Documentation / Configuration

- Root `README.md` describes MSSQL and Azure Data Studio, while current persistence and migrations use PostgreSQL/Npgsql. It also omits Worker, Chat, LiveOps, and the development content workbench.
- `docs/world-tower-realtime-combat-plan.md` explicitly says it is superseded but remains beside current plans.
- `docs/tutorial-architecture-replacement-plan.md` describes `CharacterTutorialProgress`, `TutorialController`, and `TutorialService`; none exists now. The current tutorial is quest/tour based.
- `crafting-v2-implementation-status.md` contains machine-specific attachment and download paths.
- `Combat:UseV2Engine` remains in game and AdminDashboard settings, but no production code reads it. A test helper still inserts the unused key.
- Quartz configuration remains in the game API and Chat API settings, although neither host registers or references Quartz. Quartz now belongs to `Worker.LL`.
- Several project files retain folder/exclusion entries for deleted architecture such as `Models/Abilities/Usages`, `WebSockets`, `Masteries`, old region feature folders, and old use-case folders.
- `package-lock.json.backup` is a stale second dependency snapshot.

No migration, configuration, or deployment change was performed during the audit.

---

## 9. Recommended Cleanup Order

1. Remove the empty project, backup lockfile, placeholder assets, stale project metadata, and unused configuration keys.
2. Remove the abandoned level-trigger chain and empty JSON.
3. Remove isolated dead domain models, starting with the transaction skeleton and old combat primitives.
4. Remove unused DI chains: date-time provider, tier packages, default encounter resolver, and shared random source.
5. Remove obsolete repository interface methods and throwing implementations.
6. Remove main Angular orphan components, models, and pipes in small feature groups.
7. Remove the development-dashboard orphan UI cluster.
8. Confirm the simulator has no external consumers, then remove its complete cross-layer chain.
9. Decouple dungeon participant resolution from AdminDashboard and remove the production API’s AdminDashboard dependency.
10. Establish telemetry and client-version gates for Colosseum and character-action compatibility contracts, then remove them in a coordinated backend/frontend change.
11. Retire World Tower v1 only after checking production data and rollback requirements; handle persistence removal with a dedicated EF migration.
12. Refresh the root README and archive or label historical design documents.

---

## 10. Things Investigated But Decided Are NOT Dead

- MediatR command/query handlers with no explicit construction: they are assembly-registered.
- AutoMapper resolvers in `CharacterActionDto`: they are discovered through mapping configuration.
- EF configurations, entities, snapshots, migrations, and design-time context factories.
- Controllers with only declaration-level references: ASP.NET routing discovers them.
- Hosted workers such as outbox, raid, account restriction, World Tower, Tournament Grounds, and Region Boss workers.
- `CharacterQueryProfiles.EntireCharacter` and `EntityQueryProfiles.CombatReady`: both have repository consumers.
- Chat `RateLimiter`: called by `ChatHub.Send`.
- In-memory chat presence: a legitimate fallback when Redis presence is disabled.
- AdminDashboard itself: it is an intentionally development-only content workbench, not the same system as LiveOps.
- World Tower `combat-result` and tournament replay endpoints: current Angular components still use them.
- Tutorial tour JSON assets: they are part of the current first-party overlay and quest-driven onboarding presentation.
- EF migrations that appear tied to retired implementations: retained as database history.

---

## Verification and Audit Constraints

Verification was read-only and included:

- Solution membership and project-reference inspection.
- Symbol and call-site tracing.
- Dependency-injection registration tracing.
- Angular import, route, and template-selector reachability.
- SignalR event and hub invocation matching.
- Configuration binding and project-structure inspection.

No production code, configuration, migration, or deployment artifact was changed as part of the audit. Builds and tests were not run during the analysis-only audit because no code changed and those commands would generate build artifacts.
