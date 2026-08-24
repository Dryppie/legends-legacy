# Accidental Complexity Audit

## 1. Architecture & Complexity Summary

The repository follows a recognizable layered architecture:

```text
Angular client
↓ HTTP / SignalR
ASP.NET controllers
↓
MediatR commands and queries
↓
Application interfaces
↓
Services.LL business logic
↓
IDbContext / repositories
↓
EF Core / PostgreSQL
```

Mutations receive additional global behavior:

```text
Command
↓
TransactionBehavior
↓
Database transaction + character lock
↓
Business mutation
↓
State-revision invalidation + outbox records
↓
Commit
↓
Outbox worker
↓
Consumers / SignalR
↓
Angular invalidation and refetch
```

The transaction, outbox, realtime, state-revision, and stale-response machinery is complicated for legitimate reasons. It protects atomicity, concurrency, retry safety, multi-instance delivery, and frontend consistency.

Accidental complexity is concentrated in four areas:

- Cross-cutting behavior that must be described in several registries and switches.
- Historical application/host coupling, especially AdminDashboard code inside the common Application assembly.
- Duplicated multiplayer playback and client-state infrastructure.
- Large feature services and state stores that have accumulated transport, orchestration, persistence, and presentation responsibilities.

Several architectural generations coexist:

```text
Older:
MediatR → large service → IQueryable repository → EF

Newer:
MediatR → feature service → IDbContext + outbox

Host-specific newer:
Targeted MediatR registration → host-specific services
```

The newer patterns are generally simpler without weakening architectural boundaries.

## 2. Overall Assessment

Accidental complexity is a significant but concentrated problem. It is not pervasive evidence that the whole architecture needs replacing.

The main problem is change amplification, followed by duplication and historical layering. General abstraction is not the dominant issue: many interfaces and pipeline layers provide real value.

The highest-return areas are:

1. State-sync metadata and invalidation inference.
2. Host/application registration boundaries.
3. Multiplayer playback duplication.
4. Outbox/realtime contract registries.
5. Historical compatibility paths.
6. Large multiplayer and Guild state coordinators.

## 3. Highest-Value Simplification Candidates

### Finding 1 — State synchronization has multiple sources of truth

**Location:** `LL/src/Core/Application/MediatR/Synchronization/StateSyncCommandScopeCatalog.cs`, `LL/src/Core/Application/MediatR/Behaviors/TransactionBehavior.cs`, `LL/src/Presentation/ll/src/app/core/services/api/api.service.ts`, `LL/src/Presentation/ll/src/app/core/services/real-time/game-realtime/state-sync-coordinator.service.ts`  
**Classification:** A — Strong Accidental Complexity; E — Change Amplification  
**Cleanup value:** High

**Current architecture:**

```text
Command type
↓
642-line central command/scope catalog
↓
612-line TransactionBehavior
↓
Tracked-entity and serialized-outbox inspection
↓
Domain revision
↓
Frontend mutation's handled-scope array
↓
State-store registration and response acceptance/rejection
```

**Why it exists:** The system must prevent a realtime invalidation from overwriting a newer authoritative mutation response and must recover from late or stale HTTP responses.

**Why it now appears more complicated than necessary:** The same semantic fact—“this command affects these scopes, and its response authoritatively handles these scopes”—is declared or inferred in several places.

Concrete evidence:

- The central catalog manually registers command types and combinations such as authoritative response scopes, world scopes, inventory-on-change, and character refresh flags.
- `TransactionBehavior` knows feature-specific persistence details and parses marketplace and guild outbox JSON via `GetMarketplaceAffectedCharacterIds` and `TryGetGuildAudienceId`.
- The production Angular code contains 64 `stateSyncScopesHandledByResponse` declarations and 23 `stateSync.register(...)` calls.
- State-scope names themselves are generated into TypeScript, which is a good existing single-source pattern. Response-handling semantics are not.

**What could potentially disappear:**

- The central type-to-scope catalog as an independent knowledge hub.
- JSON inspection of outbox payloads inside the transaction behavior.
- Per-HTTP-call frontend handled-scope arrays.
- Several special booleans and entity-specific inference branches.

**What must remain:** Atomic revision writes, character locking, world/character audience distinction, response-handled revision semantics, stale-response protection, reconnect recovery, and observability.

**Simpler direction:** Establish one authoritative mutation descriptor close to the command/response contract and make the pipeline and client contract consume it. A typed affected-audience contribution should replace serialized-payload inspection. This is justified only if it removes the catalog, JSON knowledge, and client arrays—not if it adds another registry alongside them.

**Risk:** High. State synchronization has subtle correctness guarantees and needs contract and concurrency tests before any structural change.

---

### Finding 2 — The shared service registrar is both duplicated and too broad

**Location:** `LL/src/Infrastructure/Service/Services.LL/DependencyInjection.cs`  
**Classification:** A — Strong Accidental Complexity; D — Duplication Complexity  
**Cleanup value:** High

**Current architecture:** `AddLiveOpsServices` contains a focused subset, while `AddServices` registers nearly every game feature. Several registrations occur in both methods, and some are repeated twice within the same file.

Exact duplicated registrations include:

- `IAccountAccessPolicy` at lines 170 and 232.
- `ILiveOpsService` at 171 and 233.
- `ILiveOpsAccountRiskService` at 172 and 266.
- `IAccountTemporalCorrelationService` at 173 and 269.
- `IInventoryService` at 175 and 593.
- `IGameEventOutbox`, realtime broadcaster, immediate publisher, and state-sync service at 178–181 and 646–649.

**Why it exists:** The repository needed a straightforward composition root as features accumulated and later added a separate LiveOps API.

**Why it is accidental:** A registration change may silently affect multiple hosts, and duplicate registrations obscure which lifetime or implementation actually wins. The broad method also forces hosts to load unrelated options and feature graphs.

**What could disappear:**

- Duplicate option-binding and service-registration blocks.
- Repeated registrations in the same composition path.
- The assumption that one method must know every feature required by every host.

**What must remain:** Explicit host composition, option validation, no-op adapters for hosts without realtime infrastructure, and Core’s independence from Infrastructure.

**Simpler direction:** Extract a small number of cohesive registration groups already implied by the hosts—base game infrastructure, administration, realtime/outbox, multiplayer, and content definitions—and let the current public registration methods compose those groups.

**Risk:** Low for exact duplicate removal; medium for host graph reduction because missing registrations are startup failures.

---

### Finding 3 — AdminDashboard application code leaks into the game API and worker

**Location:** `LL/src/API/API.LL/Program.cs`, `LL/src/Worker/Worker.LL/Program.cs`, `LL/src/API/API.LiveOps/Hosting/LiveOpsApplication.cs`  
**Classification:** C — Historical Complexity; E — Change Amplification  
**Cleanup value:** High

**Current architecture:**

```text
API.LL / Worker
↓
AddApplication scans the whole Application assembly
↓
_AdminDashboard handlers are discovered
↓
Services.AdminDashboard must be referenced and registered
```

The main API comment explicitly records this workaround and suggests splitting the application layer. The worker also registers `AddAdminDashboardServices`, although its scheduled responsibilities are marketplace, tournament, and region-boss work.

**Why it exists:** All application handlers historically lived in one assembly and were registered via broad MediatR scanning.

**Why it is accidental:** Unrelated hosts have compile-time and startup dependencies on AdminDashboard infrastructure solely because handler discovery is assembly-wide. LiveOps already demonstrates a repository-native alternative through targeted handler registration.

**What could disappear:**

- `Services.AdminDashboard` references from API.LL and Worker.LL.
- AdminDashboard service registration in unrelated hosts.
- Discovery of AdminDashboard handlers in the shared game host.

**What must remain:** MediatR behaviors used by game commands, dependency direction, AdminDashboard handler behavior, and host-specific authorization.

**Simpler direction:** Move the dashboard handlers into a host-specific application assembly or apply targeted handler registration similar to LiveOps.

**Risk:** Medium. Handler coverage and pipeline behavior must be checked carefully.

---

### Finding 4 — Durable event and realtime contracts require several manual registries

**Location:** `LL/src/Core/Application/UseCases/Outbox/GameEventTypes.cs`, `LL/src/Infrastructure/Service/Services.LL/Outbox/GameEventOutboxConsumerRegistry.cs`, `LL/src/API/API.LL/HostedServices/GameEventOutboxWorker.cs`, `LL/src/Core/Application/WebSockets/Contracts/GameRealtimeEventNames.cs`, `LL/src/Presentation/ll/src/app/core/services/real-time/game-realtime/game-realtime-contracts.ts`  
**Classification:** D — Duplication Complexity; E — Change Amplification  
**Cleanup value:** High

**Current architecture:**

```text
Event type constant
↓
Payload type
↓
Event-to-consumer registry
↓
Consumer-name constant
↓
Consumer implementation
↓
DI registration
↓
Worker state-scope fallback
↓
Backend realtime event name
↓
TypeScript event name + payload map
↓
Frontend handler
```

**Why it exists:** Durable events need fan-out and independent retry per consumer, while frontend events need an explicit wire contract.

**Why it is accidental:** Routing, consumer identity, state invalidation, and wire naming are declared independently. The worker also contains a fallback switch mapping the quest, event-quest, and achievement consumers to scopes.

**What could disappear:**

- Parallel consumer-name and routing declarations.
- Worker fallback scope switch.
- Hand-maintained backend/frontend event-name duplication.
- Some manual DI ceremony.

**What must remain:** Per-consumer delivery records, retry/dead-letter behavior, event version compatibility, audience routing, and the explicit durable/immediate distinction.

**Simpler direction:** One authoritative event/consumer descriptor should provide routing and invalidation metadata, with frontend wire names or contract checks generated from the authoritative backend contract. Avoid reflection if it merely converts compile-time mistakes into runtime failures.

**Risk:** Medium. Outbox routing errors can silently lose downstream behavior.

---

### Finding 5 — Four multiplayer features implement the same playback codec

**Location:** `LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/TournamentGroundsService.cs`, `LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs`, `LL/src/Infrastructure/Service/Services.LL/Raids/RaidPlaybackBundleBuilder.cs`, `LL/src/Infrastructure/Service/Services.LL/RegionBosses/RegionBossPlaybackBundleBuilder.cs`, `LL/src/Presentation/ll/src/app/core/services/client-side/combat/raid-playback.service.ts`  
**Classification:** D — Duplication Complexity; E — Change Amplification  
**Cleanup value:** High

All four backend implementations contain substantially the same protocol:

- Sparse checkpoint frames.
- Thirty-second keyframes.
- Entity-state, total, and ability-total deltas.
- JSON serialization.
- Brotli compression.
- Uncompressed and compressed size limits.
- SHA-256 bundle hashes.

The frontend independently implements four similar materializers with cache management, binary search, walking back to a keyframe, and applying sparse deltas.

**Why it exists:** The features were built separately and have different DTO names and persisted artifacts.

**Why it is accidental:** The compression and materialization algorithm is the same concept; differences are primarily feature schemas and limits.

**What could disappear:**

- Four independent implementations of keyframe selection, sparse delta application, compression, hashing, and cache traversal.
- Duplicate codec-specific tests.

Feature-specific DTOs, metadata, storage, and access checks would remain.

**What must remain:** Persisted format compatibility, deterministic reconstruction, feature-specific authorization, bundle limits, hash verification, and existing playback URLs.

**Simpler direction:** Consolidate the low-level sparse playback codec and frontend materializer while retaining small feature-specific adapters. Do not unify multiplayer lifecycle or combat semantics.

**Risk:** Medium to high because persisted playback artifacts are compatibility-sensitive. Golden bundle tests should precede consolidation.

---

### Finding 6 — TournamentGroundsRepository does not hide EF Core

**Location:** `LL/src/Core/Domain/Models/Colosseum/Tournaments/ITournamentGroundsRepository.cs`, `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Colosseum/TournamentGroundsRepository.cs`, `LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/TournamentGroundsService.cs`  
**Classification:** C — Historical Complexity; B — Likely Accidental Complexity  
**Cleanup value:** High

The interface exposes 15 `IQueryable<T>` properties, generic add/find methods, `SaveChangesAsync`, conditional transaction creation, and an advisory-lock method. The implementation largely forwards those properties directly to `LLDbContext`. Callers still use `Include`, `ThenInclude`, `AsNoTracking`, tracking behavior, and transaction semantics.

**Purpose of the layer:** It centralizes access to tournament tables and isolates PostgreSQL advisory-lock mechanics.

**Why it is accidental:** It claims a persistence abstraction but requires callers to understand EF almost completely. Newer Raid, RegionBoss, and WorldTower code already uses `IDbContext` directly.

**What could disappear:**

- One interface.
- One pass-through implementation.
- Fifteen renamed DbSet facades.
- The transaction wrapper.

A narrow lock capability might remain if `IDbContext` is not the appropriate home for tournament advisory locks.

**What must remain:** Advisory locking, transaction reuse, EF tracking semantics, query performance, and test coverage around concurrent tournament advancement.

**Simpler direction:** Use the established `IDbContext` pattern directly, retaining only the narrow concurrency behavior that genuinely hides PostgreSQL details.

**Risk:** Medium to high because the 4,105-line service contains many complex queries and transaction paths.

---

### Finding 7 — Multiplayer services accumulated several independent capabilities

**Location:** `LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/TournamentGroundsService.cs`, `LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs`, `LL/src/Infrastructure/Service/Services.LL/Raids/RaidService.cs`  
**Classification:** B — Likely Accidental Complexity  
**Cleanup value:** High

The feature domains themselves are genuinely complex. The accidental part is that each main service owns several separable capabilities:

- Read models and directory queries.
- Lobby/team mutations.
- Worker scheduling, claims, or simulation.
- Combat snapshot and resolution orchestration.
- Playback construction and delivery.
- Rewards and vendor purchases.
- Chat and realtime publication.

Callers—including query handlers, mutation handlers, and hosted workers—use largely disjoint portions of the APIs.

**What could disappear:** The “one service is the entire multiplayer feature” concept and large constructor dependency sets. No domain behavior needs to disappear.

**What must remain:** Feature-level invariants, transaction ownership, lease behavior, deterministic combat, reward idempotency, and coherent lobby mutation rules.

**Simpler direction:** Separate along capabilities already visible in public methods: queries, lobby mutations, resolution workers, rewards, and playback. Avoid a new generic “multiplayer framework.”

**Risk:** High. This should follow lower-risk protocol and persistence cleanup.

---

### Finding 8 — GuildStateService is a frontend feature platform

**Location:** `LL/src/Presentation/ll/src/app/core/services/api/guild/guild-state.service.ts`  
**Classification:** B — Likely Accidental Complexity  
**Cleanup value:** High

The 1,083-line store owns:

- Membership and guild identity.
- Directory, invitations, and applications.
- Roles and permissions.
- Buildings.
- Missions and personal orders.
- Shop state.
- Vault actions.
- Notifications.
- Auth-token refresh after membership changes.
- Seven state-sync registrations.
- A custom realtime handler table.
- Request epochs and late-response rejection.

The backend already recognizes buildings, missions, shop, vault, and core membership as separate services.

**Why it exists:** Guild screens share identity and receive overlapping realtime events.

**Why it is accidental:** The store has become the coordinator for every guild subdomain, making a small shop or mission change require understanding membership transitions, auth refresh, realtime echo suppression, and unrelated caches.

**What could disappear:** The all-purpose GuildStateService and its internal generic realtime routing table. A small membership/session coordinator would remain.

**What must remain:** Atomic guild-identity transitions, auth refresh when membership claims change, realtime echo suppression, and coordinated cache clearing when leaving or changing guilds.

**Simpler direction:** Align client stores with the existing backend subdomains and retain a narrow guild identity coordinator.

**Risk:** Medium.

---

### Finding 9 — Frontend stores repeatedly implement the state-sync protocol

**Location:** `LL/src/Presentation/ll/src/app/core/services/api/inventory/inventory-state.service.ts`, `LL/src/Presentation/ll/src/app/core/services/api/dungeon/dungeon-state.service.ts`, `LL/src/Presentation/ll/src/app/core/services/api/market-place/market-place-state.service.ts`  
**Classification:** D — Duplication Complexity; E — Change Amplification  
**Cleanup value:** Medium

Stores repeatedly coordinate:

- Request epochs.
- Loading and error state.
- `DomainVersionTracker`.
- `latestRevision`.
- `acceptSnapshotResponse`.
- `rejectMutationResponse`.
- State-sync registration.
- Authoritative response merging.

Feature-specific merging is legitimate. Reimplementing protocol acceptance and stale-response rules is not.

**What could disappear:** Repeated state-protocol orchestration from feature stores.

**What must remain:** Feature-specific merge semantics, late-request protection, explicit loading behavior, and authoritative response handling.

**Simpler direction:** Give the existing coordinator or API layer one standard “versioned authoritative snapshot” operation. Feature stores should provide the fetch and apply functions rather than reimplementing the protocol. This is worthwhile only if it deletes protocol code from many stores.

**Risk:** Medium.

---

### Finding 10 — Compatibility bridges are still used by the current frontend

**Location:** `LL/src/Core/Application/UseCases/CharacterActions/Dtos/Responses/CharacterActionDto.cs`, `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Resolution/CombatEncounterResultFactory.cs`, `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Dungeon/DungeonCombatSessionFactory.cs`  
**Classification:** C — Historical Complexity; E — Change Amplification  
**Cleanup value:** Medium to high

There are two active compatibility families:

1. `CharacterActionDto` retains `NextResolutionAt` and `HasPendingCombatResolution` aliases for clients predating `NextResolutionAtUtc` and `HasMoreDueWork`. The current Angular client and its tests still reference both generations.
2. Combat resolution writes player/enemy post-state into the old `CombatResult`, while dungeon presentation writes aggregate loot, XP, and gathering rewards into the last encounter and also builds a `CombatSummary`.

**Why it exists:** Rolling deployment compatibility and an ongoing migration to richer combat/session models.

**Why it is accidental now:** The current client has adopted the compatibility surface, making it impossible to retire merely by waiting for old deployments to disappear.

**What could disappear:**

- Two character-action aliases and dual frontend interpretation.
- Duplicate combat post-state and reward placement.
- Compatibility-only presentation mutation inside resolution/reward factories.

**What must remain:** A safe client-version rollout window, old persisted replay compatibility where applicable, correct polling deadlines, and the current combat UI’s required data.

**Simpler direction:** First migrate the current client and tests completely to the explicit contracts. Then define an operational retirement point before deleting backend aliases. Combat needs an explicit authoritative read-model choice before removing either representation.

**Risk:** Medium operational risk for character actions; high data/UI risk for combat.

## 4. Historical Architecture Layers

Three historical layers are especially visible:

- **AdminDashboard in the shared Application assembly:** newer LiveOps code uses targeted registration, while older dashboard handlers force every broad Application host to provide AdminDashboard services.
- **Tournament repository generation:** it predates the direct `IDbContext` pattern used by newer multiplayer features but leaks EF semantics through `IQueryable`.
- **Compatibility generations:** character-action and combat response migrations remain embedded in current-client behavior.

A smaller historical smell is optional cross-feature dependencies. Achievement, prophecy, guild, tournament, colosseum, and other services accept optional collaborators such as `IAchievementService?` or `IGameEventOutbox?`. Production DI normally supplies them, so behavior can differ primarily in tests or partial hosts. This is **F — Needs Investigation**: replacing optional dependencies with required dependencies or explicit no-op adapters could make behavior more discoverable, but only after confirming intentional reduced-capability hosts.

## 5. Excessive Abstraction / Indirection

The strongest questionable abstraction is `ITournamentGroundsRepository`; it adds names but not isolation.

The Essence mapping profile is another form of hidden indirection. `LL/src/Core/Application/UseCases/Essences/Dtos/SoulArchiveMappingProfile.cs` contains only two AutoMapper registrations, but its converter performs progression eligibility, costs, requirements, grants, ability scaling comparisons, and player-facing formatting across 372 lines.

**Classification:** B — Likely Accidental Complexity  
**Cleanup value:** Medium

An explicit Essence read-model assembler would make those responsibilities discoverable. The AutoMapper converter concept could disappear without removing the API DTO boundary.

I did not find evidence that controller → MediatR → handler is generally excessive. Even small command handlers benefit from uniform transactions, exception translation, locks, and state invalidation.

## 6. Over-Generalized Systems

No major generic rule engine or unused plugin framework was found.

Two smaller cases are notable:

- The 787-line `AddServices` method behaves like a universal host registrar despite materially different host needs.
- `JsonDocumentReader<T>` is a generic abstraction currently used as a small one-purpose startup document wrapper. Its cost is low, so it is not independently worth prioritizing.

The combat orchestrator and outcome-processor registries are not over-generalized: Idle and Dungeon have genuinely different setup, reward, and session behavior.

## 7. God Objects / Responsibility Accumulation

The main responsibility accumulators are:

- TournamentGroundsService — 4,105 lines.
- WorldTowerService — 2,917 lines.
- RaidService — 2,126 lines.
- GuildStateService — 1,083 lines.
- StateSync command catalog plus TransactionBehavior as a combined global knowledge hub.

Their size alone is not the finding. The evidence is that unrelated callers use distinct capabilities and that transport/playback/realtime concerns are embedded with feature rules.

## 8. Micro-Abstraction / Fragmentation

I did not find a strong repository-wide micro-abstraction explosion worth recommending for removal.

Combat resolution and rewards are split among factories, calculators, appliers, session builders, and outcome processors, but those boundaries represent independently testable rules and support at least Idle and Dungeon variations.

Some MediatR notification chains for user and character creation are indirect, but each handler establishes a meaningful application boundary or enqueues durable follow-up work. Their cleanup value appears low.

## 9. Duplicate Concepts / Infrastructure

### Dungeon definitions validate overlapping invariants twice

`DungeonCatalogValidator` validates the raw family/difficulty document. `DungeonDefinitionMaterializer` expands it, after which `DungeonDefinitionValidator` repeats several name, room, range, multiplier, and encounter checks.

**Classification:** D, E  
**Cleanup value:** Medium

Raw-schema and derived-semantic validation should both remain, but each invariant should have one authoritative stage.

### LiveOps preview and execution duplicate eligibility rules

`LiveOpsActionPreviewService` and `LiveOpsService` separately validate reasons, notes, expiry, players, restrictions, and compensation items.

**Classification:** D — Duplication Complexity  
**Cleanup value:** Medium

The preview’s hashing, expiry token, and confirmation step are justified safety features. A shared read-only eligibility evaluation could eliminate rule drift while execution remains authoritative.

### Exact duplicate operation-result types

`TowerOperationResult`, `RaidOperationResult`, and `RegionBossOperationResult` are structurally identical.

**Classification:** D  
**Cleanup value:** Low to medium

One existing application-level result type could replace these exact duplicates. Richer feature-specific results with meaningful error codes should remain separate.

## 10. Mapping / Model Complexity

Most mapping boundaries are justified:

```text
EF entity
↓
Application/domain result
↓
API DTO
↓
TypeScript contract
↓
Feature state
```

They prevent the frontend from depending directly on EF and allow response-specific shaping.

The significant exceptions are:

- Essence business and presentation logic hidden inside an AutoMapper converter.
- Combat rewards and post-state represented simultaneously in legacy `CombatResult` and newer encounter/session summary models.
- Character-action schedule semantics represented by both explicit and compatibility fields.
- Playback formats repeated through four backend and four frontend model families even though the materialization protocol is the same.

## 11. Event / Pipeline Complexity

The durable event pipeline itself is appropriate:

- `TransactionBehavior` commits state and outbox messages together.
- `GameEventOutboxWorker` claims deliveries, processes each consumer transactionally, retries failures, and records delivery metrics.
- `OutboxGameRealtimeBroadcaster` sends persistent state notifications through the outbox.
- Only `WorldTowerCombatFrameUpdated` uses the immediate publisher, appropriately treating it as ephemeral high-frequency state.

One event convention needs investigation:

```text
Crafting / idle combat
├─ synchronous MediatR prophecy notification
│  └─ ProphecyService mutation + completion outbox
└─ durable gameplay outbox event
   ├─ Quests
   ├─ Achievements
   └─ Event quests
```

For example, `IdleCombatOutcomeProcessor` publishes prophecy progress synchronously and separately enqueues `IdleCombatEncounterCompleted`. Crafting follows a similar split.

**Classification:** F — Needs Investigation  
**Cleanup value:** Potentially medium

There may be a valid consistency reason for prophecy progression being synchronous. If not, this is a historical competing progression architecture. Any consolidation must first decide whether progression is required before command commit or may be eventually consistent.

## 12. Change Amplification

### Add a new state-synchronized mutation

```text
Command and handler
↓
StateSyncCommandScopeCatalog
↓
TransactionBehavior entity/audience inference
↓
Frontend API handled-scope declaration
↓
Feature state registration
↓
Snapshot/mutation acceptance logic
↓
Backend and frontend tests
```

Necessary: revision creation, affected audience, authoritative-response semantics.  
Accidental: separate catalog entry, JSON payload inspection, and client-side scope redeclaration.

### Add a durable realtime event

```text
GameEventTypes
↓
Payload
↓
Consumer registry
↓
Consumer name
↓
Consumer implementation
↓
DI registration
↓
Worker scope reporting/fallback
↓
Backend realtime name
↓
TypeScript event-name and payload map
↓
Frontend handler
```

Necessary: payload, delivery consumer, client handling.  
Accidental: independent name/routing/scope registries.

### Change multiplayer playback encoding

```text
Tournament backend codec
↓
Tower backend codec
↓
Raid backend codec
↓
Region-boss backend codec
↓
Four client materializers
↓
Persisted-format tests and feature tests
```

The compatibility work is necessary. Repeating the same algorithm eight times is not.

### Add a dungeon definition field or invariant

```text
JSON document type
↓
Catalog JSON
↓
Catalog validator
↓
Materializer
↓
Domain definition
↓
Domain validator
↓
Consumers/tests
```

Both schema and semantic validation are necessary; repeated validation of unchanged invariants is accidental.

### Retire a character-action schedule field

```text
Backend compatibility alias
↓
TypeScript DTO
↓
CharacterActionsStateService
↓
Polling helper
↓
Progress/current-action/combat components
↓
Tests
↓
Deployment compatibility window
```

The deployment window is necessary. Continued use of both fields by the current client is accidental.

### Add a worker-only feature

```text
Worker job
↓
Whole Application assembly scan
↓
Broad AddServices graph
↓
Unrelated options/providers
↓
AdminDashboard service registration
```

The job and its feature dependencies are necessary. Loading unrelated host capabilities is not.

## 13. Simple Things That Are Hard

1. **Add a mutation that updates client state.** The developer must understand command scope profiles, tracked-entity inference, outbox audiences, response-handled scopes, revision headers, and feature-store synchronization.
2. **Add a durable client notification.** It crosses event type constants, routing maps, consumer names, DI, state-scope reporting, realtime names, TypeScript contracts, and handlers.
3. **Add or change a multiplayer playback field.** The same sparse materialization and compatibility rules are independently embedded in four features on both sides of the API.
4. **Add a dungeon content invariant.** It requires deciding whether to implement it in the raw catalog validator, derived definition validator, or both.
5. **Retire an API compatibility field.** Current-client code and tests use the aliases intended only for older clients.
6. **Change a Guild subfeature.** A shop, building, mission, or vault change requires understanding the central store’s membership transitions, auth refresh, realtime dispatch, state sync, and unrelated caches.
7. **Add a host-specific handler or job.** Broad assembly scanning and monolithic service registration pull unrelated application and infrastructure dependencies into the host.

## 14. Complexity That Is Justified

- **FastCombatEngine:** Its tick processing, abilities, statuses, summons, stagger, overtime, and deterministic event flow represent real game-domain complexity. Size alone is not evidence for simplification.
- **Transactional outbox:** It protects state/event atomicity and independent consumer retries.
- **TransactionBehavior’s transaction and character locking:** These protect concurrency and prevent overlapping character mutations. Only its feature-specific invalidation knowledge is suspect.
- **State revisions and stale-response repair:** Necessary for coordinating HTTP and SignalR across reconnects and late responses.
- **WorldTower work leasing and playback-frame workers:** Claiming, lease renewal, cancellation on lease loss, and parallel processing are legitimate distributed-worker concerns.
- **Quartz persistent scheduling and idempotency:** Appropriate for scheduled tournament, marketplace, and region-boss operations.
- **LiveOps preview confirmation and state hashing:** Appropriate safeguards around destructive administrative actions.
- **Character-action catch-up and polling:** Deadlines, offline catch-up, generations, and stale-request protection are inherent to the feature.
- **Content-definition validation:** Startup fail-fast behavior and cross-reference checks are valuable; only overlapping checks should be reduced.

## 15. Things That Look Over-Engineered but Are Fine

- `IGameRealtimeBroadcaster` is not a needless SignalR wrapper. It enforces durable delivery and isolates the single immediate-frame exception.
- `IStateSyncService` and the generated TypeScript scope constants preserve an important backend/frontend consistency contract.
- `IDbContext` leaks EF concepts, but it preserves the Core-to-Infrastructure dependency direction and supports the global transaction behavior. Removing it would mostly move complexity.
- MediatR command handlers that delegate to services still receive consistent locking, transactions, exception conversion, and state invalidation.
- Separate Idle and Dungeon combat outcome processors represent real behavioral differences.
- Most provider interfaces for content definitions isolate JSON-backed content and provide stable application boundaries.
- Development-only progression workers in API.LL do not appear to be competing production execution paths.
- Authentication, authorization policies, and account restriction indexing address different security concerns and should not be collapsed.

## 16. Simplification Opportunities by Risk

### Low-Risk Simplifications

- Remove exact duplicate DI registrations.
- Converge the three structurally identical multiplayer operation-result types.
- Stop the current Angular client from consuming compatibility aliases, while retaining backend aliases through the rollout window.
- Clarify or require currently optional dependencies where every production host already supplies them.

### Medium-Risk Simplifications

- Narrow host service composition.
- Separate AdminDashboard handler registration from game API and worker registration.
- Move Essence DTO construction out of AutoMapper.
- Remove overlapping dungeon validation.
- Share LiveOps read-only eligibility rules.
- Split Guild frontend state along existing feature boundaries.
- Consolidate frontend versioned-snapshot protocol handling.
- Consolidate playback codecs with golden compatibility tests.

### High-Risk Simplifications

- Replace StateSyncCommandScopeCatalog and entity/outbox inference.
- Simplify TournamentGrounds persistence access.
- Remove combat response compatibility representations.
- Split TournamentGrounds, WorldTower, and Raid service responsibilities.
- Change the progression event consistency model.

## 17. Recommended Simplification Order

1. **Remove exact duplicate DI registrations.** This is independently reviewable and makes subsequent host analysis trustworthy.
2. **Converge exact duplicate result types and document compatibility retirement criteria.** These are small concept deletions with limited behavioral risk.
3. **Narrow Application and AdminDashboard registration.** Use the proven targeted LiveOps pattern to remove unrelated host dependencies.
4. **Extract Essence read-model construction and de-duplicate dungeon/LiveOps validation rules.** These are feature-local changes with clear ownership.
5. **Build golden playback compatibility tests, then consolidate the backend and frontend codec primitives.** Keep feature schemas and authorization independent.
6. **Make event routing and state invalidation metadata single-source.** Start with one event family and demonstrate that existing registries or switches actually disappear.
7. **Simplify the TournamentGrounds persistence facade.** Preserve advisory locks and transaction behavior explicitly.
8. **Reduce frontend state-sync boilerplate, then split Guild state.** Establish the shared protocol seam before decomposing the largest store.
9. **Replace the central command-scope catalog and JSON invalidation inference.** Do this incrementally by feature scope, with concurrency and stale-response tests.
10. **Split the large multiplayer services last.** Use the capability seams exposed by earlier playback, persistence, and event cleanup. Avoid a repository-wide multiplayer framework.

## Audit Verification

The audit used read-only repository and file inventory, targeted symbol and call-path searches, line-count comparisons, and `git status`/`git diff --stat`.

Backend and frontend test suites were not run because the audit itself was analysis-only and introduced no implementation changes.

At the time of the audit, the following pre-existing untracked files were present and were not modified:

- `docs/gathering-level-progression.md`
- `docs/gathering-system-progression-analysis.md`

The audit introduced no migrations, configuration changes, or deployment implications.
