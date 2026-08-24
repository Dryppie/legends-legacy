# Backend Architecture Consistency Audit

## 1. Architecture Summary

The main backend is pursuing a pragmatic CQRS architecture:

```text
Authenticated HTTP controller
    → MediatR command/query
    → application handler
    → domain/service orchestration
    → repository / EF Core
    → transaction pipeline or explicit feature transaction
    → state-revision invalidation + transactional outbox
    → HTTP response
```

The intended conventions are unusually explicit in `LL/src/Core/Application/AGENTS.md` and `LL/src/Infrastructure/Service/Services.LL/AGENTS.md`:

- Mutations use `ICommand<T>` and therefore the transaction pipeline.
- Reads use `IQuery<T>`.
- Controllers are thin transport adapters.
- Services own orchestration and business decisions.
- Repositories are intended to own EF/query details.
- Handlers map domain/service results to DTOs.
- Persistent realtime notifications are stored through the outbox.
- State revisions tell clients which HTTP state must be refreshed.

Normal mutations are wrapped by `TransactionBehavior`, which opens a transaction, takes a per-character advisory lock, invokes the handler, determines changed synchronization scopes, saves, and commits.

Complex multiplayer state machines—Marketplace, raids, region bosses, and World Tower—sometimes opt out of the generic transaction behavior and own specialized locks, leases, and phase transactions. Those are generally intentional exceptions.

Scheduled work belongs primarily to the persistent, clustered Quartz worker. Queue-like realtime work—outbox delivery, raid resolution, and tower simulation/playback—runs as lease-based hosted workers in the API.

`LL-Chat`, `API.LiveOps`, and the development-only `API.AdminDashboard` are separate boundaries with legitimately different concerns.

## 2. Overall Assessment

The backend is generally coherent at its outer boundaries: controllers are mostly thin, claim-derived character IDs are used consistently, recent multiplayer systems handle concurrency carefully, and the outbox/background infrastructure is substantially more robust than average.

The inconsistencies are nevertheless meaningful:

- Two direct-transfer paths can lose currency or inventory under concurrency.
- The generic transaction behavior can commit a failed operation without state invalidation.
- A production-reachable simulation endpoint allows unbounded CPU and memory work.
- A query persists quest state outside the command/state-sync pipeline.
- The exception adapter obscures established HTTP/concurrency handling and leaks exception messages.
- Repository-oriented services and direct-EF services represent two architectural generations.
- Shared currency/reward rules are implemented differently across features.

Priority should be correctness and availability first, followed by transaction/error semantics, then persistence and reward-path consolidation.

## 3. Highest-Risk Findings

### F1 — Direct transfers do not lock all affected characters

**Files/classes involved:** `WireCindersCommand`, `TransferInventoryItemCommand`, `CurrencyTransferRepository`, `InventoryRepository`, `TransactionBehavior`  
**Classification:** A — Clear architectural inconsistency  
**Severity:** Critical

**Expected pattern:** Every participant whose balance or inventory is changed should be locked in deterministic order. Marketplace already does this through `LockCharactersAsync`, for example in `MarketPlaceService` and row locks in `MarketPlaceRepository`.

**Actual implementation:** The generic pipeline discovers one `CharacterId` and locks only that character in `TransactionBehavior`. Both transfer commands expose the sender as that ID:

- `LL/src/Core/Application/UseCases/Characters/Commands/WireCinders/WireCindersCommand.cs`
- `LL/src/Core/Application/UseCases/Inventories/Commands/TransferInventoryItem/TransferInventoryItemCommand.cs`

The recipient is resolved later, after the lock has been selected.

**Evidence:** `CurrencyTransferRepository` directly performs:

```text
sender.Cinders -= amount
recipient.Cinders += amount
```

`Character` has no configured concurrency token in `CharacterConfiguration`.

`InventoryRepository` reads and increments an existing recipient stack. `InventoryItem` likewise has no concurrency token or uniqueness constraint for one stack per item base.

**Failure scenario:** Two different senders concurrently transfer to the same recipient. Each command holds a different sender lock. Both read the same recipient value and later write an absolute updated value. The last update wins:

- Both senders are debited.
- Both transfer histories and ledger entries can be committed.
- The recipient receives only one increment.

For stackable items, one quantity increment can be lost. If no recipient stack existed, concurrent transfers can create competing stacks.

**Recommended direction:** Reuse the Marketplace participant-locking convention for both direct transfers. Resolve all participant IDs before mutation, lock them in stable order, and add concurrent different-sender/same-recipient integration tests.

**Implementation risk:** Medium. Lock ordering must be uniform across Marketplace and direct transfers to prevent deadlocks.

### F2 — Failed `Response<T>` results can commit mutations while skipping invalidation

**Files/classes involved:** `TransactionBehavior`, dungeon and inventory mutation handlers  
**Classification:** A  
**Severity:** High

**Expected pattern:** A command failure should either leave no committed state or explicitly represent a partial/committed outcome. Any committed state should advance the relevant state revisions.

**Actual implementation:** `IsSuccessfulResponse` gates only state-sync invalidation. `SaveChanges` and transaction commit still execute for a failed response in `TransactionBehavior`. The same behavior exists when a surrounding transaction already exists.

**Concrete path:** `ClaimDungeonRewardsCommand`:

1. Claims loot and changes the run to `RewardsClaimed` in `DungeonRunService`.
2. Deletes the completed run.
3. Enqueues the completion event.
4. Reloads inventory and character.
5. Returns failure if either reload fails.

The failed result skips `InvalidateChangedScopesAsync`, but the rewards, deletion, and outbox changes are committed.

`StartDungeonRunCommand` has the same shape: entry costs are consumed and the run is created, after which a failed inventory reload returns a failure response.

`ScrapEquipmentsCommand` contains another post-mutation failure.

**Why this matters:** The client receives “failed” even though state changed. A retry may see missing or consumed state, and other connected clients receive no revision invalidation for the committed mutation.

**Recommended direction:** Define failed command responses as rollback outcomes in the transaction behavior. Before changing it globally, inventory existing commands for any intentionally committed failure result and convert those to an explicit success/partial-success result.

**Implementation risk:** High because it changes a cross-cutting semantic. Add focused pipeline tests before changing behavior.

### F3 — The combat simulator is production-reachable and unbounded

**Files/classes involved:** `_SimulateController`, `SimulatorService`  
**Classification:** A  
**Severity:** High

**Expected pattern:** Development or diagnostic workloads are environment-gated, tightly authorized, bounded, and cancellation-aware. Recent raid, region-boss, and World Tower development endpoints explicitly return 404 outside Development.

**Actual implementation:** `_SimulateController` inherits only the general authentication requirement. It has no development guard, admin policy, multiplayer policy, or parameter validation.

`PlayerTeamSize`, `EnemyTeamSize`, `Fights`, and `Tier` are arbitrary integers. `SimulatorService`:

- Allocates teams from caller-provided sizes.
- Loops for caller-provided `fights`.
- Uses `CancellationToken.None` for combat.
- Starts a `Task.Run` for every generated combatant.

**Why this matters:** Any authenticated account can request enough work to exhaust API CPU, thread-pool capacity, or memory, and request cancellation will not stop the combat loop.

**Recommended direction:** Remove the route from the production surface or follow the repository’s existing development-endpoint guard. Add strict upper bounds, an appropriate policy, and propagate the request cancellation token.

**Implementation risk:** Low.

### F4 — Quest journal reads persist state outside the mutation pipeline

**Files/classes involved:** `GetQuestJournalQuery`, `QuestService`  
**Classification:** A  
**Severity:** High

**Expected pattern:** Reads are `IQuery<T>` and do not persist. Lazy state materialization uses a command when it must create or update records; Prophecies already follows that precedent with `GetPropheciesOverviewCommand`.

**Actual implementation:** `GetQuestJournalQuery` is an `IQuery`, but its handler directly injects `IDbContext` and takes an ad hoc character lock.

`QuestService.GetJournalAsync` creates newly available quest rows, upgrades definitions, changes active state, chooses a pinned quest, and calls `SaveChangesAsync`.

**Why this matters:** The write bypasses the command transaction behavior and its state-revision catalog. The requesting client receives the new journal, but another device or tab is not invalidated, and the reconnect revision checkpoint does not reflect the persisted change.

**Recommended direction:** Separate journal materialization from the pure read, or model the operation as a command as Prophecies does. Ensure persisted initialization participates in the normal transaction and quest invalidation path.

**Implementation risk:** Medium because the query is also invoked during game bootstrap.

### F5 — Generic exception-to-response conversion bypasses established error handling

**Files/classes involved:** `ExceptionToResponseBehaviour`, `ResponseResultFilter`, `ConcurrencyExceptionHandler`  
**Classification:** A  
**Severity:** High

**Expected pattern:** Known business failures use `Response<T>`. Unexpected and concurrency exceptions reach centralized exception handling, where concurrency is mapped to HTTP 409.

**Actual implementation:** `ExceptionToResponseBehaviour` is registered outside `TransactionBehavior`. It catches every exception and, when the response has a static `Fail(string)`, returns `Fail(ex.Message)` through reflection.

`ResponseResultFilter` then maps every failed response to HTTP 400. Meanwhile, `ConcurrencyExceptionHandler` is designed to return HTTP 409.

**Why this matters:**

- `Response<T>` commands prevent concurrency exceptions from reaching the 409 handler.
- Operational, database, and programming failures are reported as client mistakes.
- Raw exception messages may reveal schema, provider, or configuration details.
- Logging and monitoring see a normal failed response rather than an unhandled server failure.

**Recommended direction:** Limit the behavior to known business/domain exceptions. Allow unexpected and concurrency exceptions to reach the existing centralized handlers. Preserve the current `Response<T>` model for anticipated gameplay failures.

**Implementation risk:** Medium; API clients may currently assume that all failures are 400.

## 4. Highest-Value Architecture Cleanup

### F6 — The service/persistence boundary has split into two generations

**Location:** `IDbContext`, Services.LL, raid/tower/region-boss/guild/essence services  
**Classification:** B — Likely inconsistency  
**Severity:** High

**Expected architecture:** Services use repository interfaces; EF queries, `DbSet`, and transaction implementation stay behind persistence abstractions.

**Current implementation:** `IDbContext` exposes nearly the entire EF model, change tracking, `SaveChanges`, transactions, and advisory locks to Application. Direct consumers include `WorldTowerService`, `RaidService`, `RegionBossService`, `GuildShopService`, `GuildVaultService`, `GuildMissionService`, `CreatureArchiveService`, `EssenceCodexCollectionService`, `SoulstoneUpgradeService`, and several outbox consumers.

**Why this matters:** Adding a feature can require knowledge of EF, global synchronization scope inference, locks, reward mutation, and realtime semantics in the same service. This makes it easier for newer features to bypass repository-level concurrency or query conventions.

**Recommended direction:** Do not mechanically wrap every query. Start with ordinary services such as guild shop/building and collection lookups. Document specialized direct-EF exceptions for bulk/lease/state-machine operations, and move only reusable locking/query operations behind focused persistence contracts.

**Implementation risk:** Medium to high. Large multiplayer services should be migrated incrementally, not rewritten.

### F7 — Shared currency grants use inconsistent arithmetic and mutation paths

**Location:** combat, Prophecies, Colosseum, guild shop, region bosses  
**Classification:** B  
**Severity:** Medium

**Expected architecture:** Shared balances should enforce the same overflow and invariant rules regardless of reward source.

**Current implementation:** Region-boss rewards use checked arithmetic, and direct transfers explicitly reject overflow. Other paths use unchecked `+=`:

- `CharacterCurrencyRewardWriter`
- `ProphecyService`
- `TournamentGroundsService`
- `GuildShopService`

**Why this matters:** Near `long.MaxValue`, identical currency behaves differently by reward source and can wrap negative on unchecked paths. More generally, balance invariants can drift as new reward systems are added.

**Recommended direction:** Reuse one existing checked balance-mutation convention for Cinders and Soulstones while retaining feature-specific grant records and idempotency mechanisms.

**Implementation risk:** Medium; historical over-cap balances and error semantics need a decision.

### F8 — Region-boss scheduling configuration is defeated by minute-level idempotency

**Location:** Quartz region-boss registration and job  
**Classification:** A  
**Severity:** Medium

**Expected architecture:** A configured 10/30-second progression interval should result in distinct eligible executions at that cadence.

**Current implementation:** The trigger supports intervals down to ten seconds, but `RegionBossProgressionJob` constructs its idempotency key only to minute precision.

Every trigger in the same minute therefore shares the same business key and is skipped after the first completion.

**Why this matters:** The operational setting advertises sub-minute progression but actual transitions can be delayed for almost a minute.

**Recommended direction:** Align business-key precision with the supported trigger cadence, as Tournament Grounds already does with seconds.

**Implementation risk:** Low.

### F9 — APIs own production database migration at startup

**Location:** main API and Chat startup  
**Classification:** C — Needs investigation  
**Severity:** Medium

Both `API.LL/Program.cs` and `API.Chat/Program.cs` call `Database.MigrateAsync()` before serving traffic.

This couples availability to schema migration and gives every API replica potential schema ownership. Whether this is unsafe depends on the deployment topology and EF/provider migration locking, which are outside this repository.

**Recommended direction:** Verify the deployment contract. If multiple replicas or rolling deployments are used, assign migration ownership to a controlled deployment step or one designated process.

**Implementation risk:** Medium to high operationally; do not change without checking deployment practice.

## 5. Transaction Consistency

The dominant transaction implementation is strong: command-only wrapping, execution strategy, database transaction, advisory character lock, state-revision calculation, outbox persistence, and rollback on exceptions.

The important exceptions are:

- **Critical:** Cross-character transfers lock only one participant.
- **High:** Failed `Response<T>` objects still commit.
- **High:** The quest query persists outside the pipeline.
- **Reasonable:** Raid, region-boss, and tower state machines use explicit phase transactions and resource-specific locks because a single generic character transaction is insufficient.
- **Reasonable:** Internal `SaveChanges` inside an already-open transaction does not itself break atomicity; the pipeline tracks `SaveChangesVersion`.

No evidence was found of direct SignalR delivery being used as authoritative persistent state inside ordinary gameplay transactions.

## 6. MediatR / Application Flow Consistency

Most main-game endpoints follow the expected controller → command/query → handler pattern.

Meaningful deviations:

- Quest journal is a mutating query.
- Simulator requests use plain `IRequest` and bypass transactional markers, although they currently have no persistence.
- A small number of older reads—`GetRegion`, creature archive, and essence codex—still use plain `IRequest<T>` instead of `IQuery<T>`.
- Admin Dashboard commands and queries largely predate the marker conventions.
- `ExceptionToResponseBehaviour` makes MediatR the effective HTTP error adapter through reflection, even though the API also has centralized exception handling.

The marker-only inconsistencies are low risk. Quest and exception semantics are not.

## 7. Database / Data Access Consistency

Three persistence styles coexist:

1. Repository-oriented gameplay services.
2. Direct `IDbContext` services.
3. Specialized raw SQL/advisory-lock operations for queues and multiplayer state machines.

The third is often justified. The inconsistency is that ordinary services and specialized state machines both consume the same very broad Application-level EF facade, despite repository ownership being the documented convention.

The strongest concrete effect is concurrency drift: Marketplace implements participant locking, while direct transfers do not.

## 8. Realtime / Outbox Consistency

The intended realtime design is coherent:

- Database state remains authoritative.
- Commands advance state-revision scopes.
- Durable gameplay events are persisted to the outbox.
- The outbox worker claims work with `FOR UPDATE SKIP LOCKED`, retries, dead-letters after bounded attempts, and commits consumer mutations with their invalidations.

The principal violations are indirect:

- Failed command responses can commit without invalidation.
- Quest journal GETs persist without advancing revisions.

World Tower combat frames intentionally bypass the outbox. They are transient, sequenced presentation events rather than authoritative state, so this is reasonable.

## 9. Background Processing Consistency

The Quartz worker is persistent and clustered, and jobs use a database-backed execution record to make retries observable and idempotent. Marketplace expiration, tournament progression, and region-boss progression call the same feature services used elsewhere.

The sub-minute region-boss key defect is the only confirmed scheduling mismatch.

The API-hosted workers are not necessarily competing schedulers:

- Outbox delivery is a durable queue consumer.
- Raid and tower workers process leased state-machine work.
- Development progression loops are Development-only.
- The one-time title backfill is startup maintenance.

These are distinct from cron-like scheduling and therefore do not need to be forced into Quartz solely for uniformity.

## 10. Validation / Authorization Consistency

Validation is primarily divided into:

- Controller/model binding for HTTP shape.
- Handler checks for request-specific concerns.
- Service/domain checks for gameplay rules.
- Database constraints and locks for concurrency invariants.

That division is mostly consistent. There is no pervasive FluentValidation pipeline to which features are expected to conform.

The simulator is the material validation gap: unbounded numeric inputs reach resource-intensive infrastructure.

Authorization and ownership are generally sound:

- Main controllers inherit `[Authorize]`.
- Character IDs normally come from claims rather than caller-selected IDs.
- Multiplayer controllers commonly require `MultiplayerAllowed`.
- Guild, raid, and Marketplace operations recheck membership, roles, ownership, or restrictions in their service logic.
- Direct transfers resolve the target and validate guest/restriction rules server-side.

No confirmed arbitrary-character ownership bypass was found. The simulator’s missing development/admin boundary is an availability exposure rather than another player’s data-access vulnerability.

## 11. Cross-Feature Inconsistencies

| Shared concern | Modern/strong example | Divergent implementation | Risk |
|---|---|---|---|
| Multi-character asset movement | Marketplace locks every participant | Direct currency/item transfer locks sender only | Lost currency/items |
| Lazy state creation | Prophecy overview is a command | Quest journal is a query that saves | Missing revisions/stale clients |
| Currency overflow | Region boss and direct transfer check overflow | Combat, Prophecy, tournament, guild shop use unchecked `+=` | Rule drift/negative balance |
| Complex multiplayer mutation | Raid/tower/region boss use feature locks and leases | Older direct transfers rely only on generic lock | Race conditions |
| Persistent realtime | Transactional outbox + state scopes | Quest GET persists without the pipeline | Cross-device stale state |
| Scheduled progression | Tournament key includes seconds | Region-boss key includes only minutes | Configuration ignored |

Rewards are the most fragmented shared concept. Combat, dungeon claims, tournaments, Prophecies, guild shop, raids, region bosses, and World Tower all have feature-specific grant paths. Feature-specific grant records are appropriate, but common character-balance invariants should not vary among them.

## 12. Architectural Fossils

### Fossil 1 — Broad EF facade during a repository-oriented transition

**Original pattern:** Application/services directly consume a shared `IDbContext` exposing the full EF model.

**Current pattern:** Documented conventions require focused repository interfaces, while recent complex systems also use specialized lock/lease persistence.

**Remaining usages:** Guild services, collection services, soulstone upgrades, raids, region bosses, World Tower, synchronization, and outbox consumers.

**Why it appears obsolete:** The explicit current instructions prohibit EF dependencies in services, but the shared interface still makes bypassing that boundary easy.

**Recommended direction:** Migrate ordinary services first and explicitly document specialized state-machine exceptions.

**Migration risk:** High if attempted wholesale; manageable as focused feature changes.

### Fossil 2 — Reflection-based result conversion as the universal error model

**Original pattern:** Convert exceptions to `Response<T>.Fail` and let an MVC filter return 400.

**Current pattern:** The API also has Problem Details and a dedicated concurrency handler.

**Remaining usages:** Every `Response<T>` MediatR request because the behavior is globally registered.

**Why it appears obsolete:** It prevents the newer centralized handlers from distinguishing concurrency, unexpected server errors, and business failures.

**Recommended direction:** Preserve results for anticipated gameplay failures; stop translating arbitrary exceptions.

**Migration risk:** Medium due HTTP status changes.

### Fossil 3 — Admin Dashboard source-file services embedded in the main Application graph

**Original pattern:** Dashboard handlers use plain `IRequest`, manually constructed JSON readers, and direct source-file updates.

**Current pattern:** `API.AdminDashboard` is explicitly Development-only and loopback-restricted, while normal gameplay uses DI-backed services and persisted data.

**Remaining usages:** `ItemService` manually creates `ItemBaseJsonReader`. Its constructor reads and immediately rewrites `items.json`, and updates use non-atomic `File.WriteAllText`. The main API still registers Admin Dashboard services with an explicit TODO.

**Why it appears obsolete:** The dashboard boundary is only partially separated, and its file-access pattern predates the safer atomic approach used by the creature reader.

**Recommended direction:** Keep the workbench separate, remove its dependency from the gameplay API when callers allow, and apply the existing atomic file-update convention to items.

**Migration risk:** Low to medium because it is development tooling.

## 13. Suspicious Areas Requiring Investigation

- **Startup migrations:** Determine replica count, rollout strategy, and provider migration-lock behavior before changing ownership.
- **Arena ticket reads:** `ColosseumService.GetArenaTicketStatusAsync` mutates the tracked ticket status during a query but does not save it. This may intentionally calculate lazy regeneration for the response, but the object is also marked updated in the repository. Classification: C, Low.
- **World-event reconnect semantics:** Region Boss and World Tower rely heavily on semantic realtime messages and explicit state fetches rather than the same character-scope catalog used by ordinary features. This may be correct, but reconnect/missed-message integration tests should confirm that all authoritative world state is recoverable.
- **Currency/reward provenance:** Different reward systems maintain different grant records and ledger coverage. It was not clear that `EconomyLedger` is intended to record every reward, so absence from that ledger was not classified as a defect.

## 14. Things That Look Inconsistent but Are Actually Reasonable

- `[NonTransactional]` on raid, region-boss, and World Tower commands is justified where the service owns resource-specific advisory locks and multi-phase transactions.
- Direct EF/raw SQL in queue claiming and lease renewal is appropriate for `SKIP LOCKED`, atomic claiming, and bulk state transitions.
- World Tower combat frames are transient playback data; bypassing the durable outbox is reasonable when sequence numbers and authoritative persisted state support recovery.
- `LL-Chat` persists messages before direct SignalR publication. Since chat history is authoritative and can be reloaded, it does not need to mimic the main game’s state-invalidation architecture exactly.
- Development-only progression hosted services do not conflict with production Quartz scheduling.
- Controllers performing cookie handling, request claim extraction, environment guards, or HTTP preview confirmation are legitimate transport responsibilities.
- Duplicate-looking state-sync calls are transaction-ID/scope deduplicated; they represent ownership ambiguity but not duplicate revision increments.

## 15. Recommended Cleanup Order

1. **Fix direct-transfer locking.** Add concurrent different-sender/same-recipient tests for Cinders and stackable inventory, then lock every participant deterministically.
2. **Close the simulator endpoint exposure.** Environment/policy gate it, bound inputs, and propagate cancellation.
3. **Define failed-command transaction semantics.** Add pipeline tests, inventory intentional exceptions, then prevent failed `Response<T>` results from committing silently.
4. **Correct exception routing.** Let concurrency and unexpected exceptions reach Problem Details while retaining `Response<T>` for expected gameplay failures.
5. **Move quest journal materialization into a transactional mutation path.** Verify bootstrap and cross-device invalidation behavior.
6. **Unify shared balance invariants.** Apply the same checked overflow behavior across reward sources without replacing feature-specific grant/idempotency records.
7. **Fix region-boss job-key precision.**
8. **Clarify the persistence boundary.** Start with conventional direct-EF services; document exceptions for specialized state machines.
9. **Confirm migration ownership with deployment infrastructure.**
10. **Retire low-risk fossils.** Separate Admin Dashboard registrations from the gameplay host and migrate remaining plain request markers opportunistically.

## Audit Verification

The audit was performed through read-only request-flow tracing, targeted repository searches, configuration and dependency-injection inspection, and worktree verification.

Backend tests were not run because the audit was explicitly analysis-only and test execution would generate build artifacts.

No source files were modified as part of the audit.
