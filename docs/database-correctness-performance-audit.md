# Database Correctness & Performance Audit

## 1. Database Architecture Summary

The repository uses two PostgreSQL/EF Core persistence boundaries:

- `LLDbContext` is the main game database context with roughly 135 active `DbSet` properties. Entity mappings are discovered using `ApplyConfigurationsFromAssembly`. See `LL/src/Infrastructure/Persistence/Persistence.LL/LLDbContext.cs`.
- `ChatDbContext` independently stores messages, restrictions, moderation actions, raid channels, and memberships. See `LL-Chat/Infrastructure/Persistence/Persistence.Chat/ChatDbContext.cs`.

There are 66 main migrations and 8 chat migrations. Both APIs call `MigrateAsync()` at startup in `LL/src/API/API.LL/Program.cs` and `LL-Chat/API/API.Chat/Program.cs`.

Game commands normally pass through `TransactionBehavior`, which:

- Uses an EF execution strategy.
- Opens an explicit transaction.
- Takes an in-process and PostgreSQL advisory lock when a character identifier can be extracted.
- Saves tracked changes and commits after the handler returns.

Shared systems such as raids, World Tower, Region Bosses, tournaments, and marketplace processing frequently use explicit transactions, advisory locks, row locks, concurrency tokens, or worker leases. These systems are generally more defensive than the older guild, transfer, and ordinary arena paths.

The transactional outbox is well developed:

- Messages and consumer deliveries are inserted alongside authoritative gameplay mutations.
- Workers claim with `FOR UPDATE SKIP LOCKED`.
- Deliveries have processing leases, retry metadata, a five-attempt dead-letter path, and bounded batches.
- Completed and failed deliveries have seven- and thirty-day retention.
- Each database consumer and `MarkProcessed` run in one transaction.

Relevant implementations include `GameEventOutboxRepository.cs` and `GameEventOutboxWorker.cs`.

## 2. Overall Assessment

The persistence architecture is thoughtful and fundamentally sound for single-character operations. The command transaction behavior, append-only ledgers, outbox, row-versioned shared systems, worker leasing, and `SKIP LOCKED` usage are good foundations.

The principal weakness is that the safety model is character-centric while several operations modify other characters or shared guild/PvP state. Different code paths also use incompatible lock domains:

- Character advisory locks.
- PostgreSQL row locks.
- Shared-system advisory locks.
- Optimistic concurrency tokens.
- No protection at all on some aggregates.

A transaction alone does not prevent two transactions from reading the same value and later issuing absolute-value updates. As a result, the repository has meaningful lost-update and broken-invariant risks.

The first priorities should be:

1. Establish one consistent locking/concurrency contract for multi-character economy and inventory mutations.
2. Protect guild and arena shared aggregates.
3. Stop committing failed command responses.
4. Serialize or concurrency-protect achievement outbox updates.
5. Address leaderboard, arena, and marketplace result-set scaling.

## 3. Critical Correctness Findings

### 1. Concurrent Cinder transfers can lose recipient currency

**Location:** `CurrencyTransferRepository.TransferCindersAsync`, `CharacterConfiguration`  
**Classification:** A — Correctness Issue  
**Severity:** Critical  
**Frequency:** Moderate, but directly player-triggerable

**Current behavior:** `WireCindersCommand` is serialized by the sender’s character lock. The repository loads both characters and performs:

```text
sender.Cinders -= amount
recipient.Cinders += amount
```

The recipient is not locked, and `Character` has no concurrency token.

**Failure scenario:**

1. Sender A and sender B both transfer 1,000 Cinders to recipient R.
2. Each request holds a different sender advisory lock.
3. Both read R’s balance as 5,000.
4. Both create transfer history and economy-ledger entries.
5. Both assign R’s balance to 6,000.
6. The last update wins: both senders lose currency, but R receives only 1,000.

The same collision can occur between an incoming transfer and the recipient spending or earning Cinders elsewhere.

Marketplace operations row-lock `Entities`, but an ordinary character command uses an advisory lock. These mechanisms do not coordinate, so marketplace refunds/credits can also overwrite a stale character value from an ordinary command.

**Recommended direction:** Define a single multi-character mutation protocol. Lock all affected character IDs in deterministic order using the same primitive, or use atomic balance SQL/concurrency tokens with bounded retry. Do not mix non-coordinating row and advisory locks for the same balance.

---

### 2. Inventory transfers can lose quantities or create duplicate logical stacks

**Location:** `InventoryRepository.TransferItemAsync`, `InventoryItemConfiguration`  
**Classification:** A — Correctness Issue  
**Severity:** Critical  
**Frequency:** Moderate

**Current behavior:** The sender is locked, but the recipient inventory is not. If a recipient already owns a commodity stack, the code executes `existingRecipientStack.Quantity += quantity`. There is no inventory-item concurrency token or database constraint representing one logical stack per item base.

**Failure scenarios:**

- If two senders add to an existing recipient stack, both read the same quantity and the final update loses one transfer.
- If no stack exists, both requests can create separate `InventoryItem` rows with different item-instance IDs, leaving duplicate logical commodity stacks.
- Both transfer histories and ledgers can still report success.

Marketplace expiration uses similar stack read-modify-write behavior while running outside the normal recipient character lock, creating another collision path.

**Recommended direction:** Include recipient inventories in the deterministic multi-character lock protocol. For stackable commodities, consider a schema-level stack identity or atomic quantity update/upsert once the desired inventory model is explicit.

---

### 3. Guild mission progress and guild rewards are vulnerable to lost updates

**Location:** `GuildMissionService.RecordContributionAsync`, `GuildMissionService.ProgressWeeklyMissionAsync`  
**Classification:** A — Correctness Issue  
**Severity:** High  
**Frequency:** Frequent for active guilds

**Current behavior:** Idle combat, dungeons, and crafting can record guild contributions from separate character commands. Each request locks its own character, then updates the shared `GuildMissionInstance.CurrentAmount` with a normal read-modify-write.

Reward claims also update shared `Guild.GuildXp` and `GuildResource.Amount`. These entities have neither a guild-scoped lock nor a concurrency token.

**Failure scenario:**

1. Two guild members complete qualifying actions concurrently.
2. Both load mission progress at 900.
3. Both add 100 and persist 1,000.
4. Their individual contributions and distinct idempotency ledgers can both be recorded.
5. The aggregate increases by only 100 instead of 200.

Concurrent reward claims can similarly lose guild XP or supplies.

**Recommended direction:** Serialize shared guild mutations using a guild-scoped advisory lock, or apply atomic updates/concurrency tokens with retry. Keep the existing per-event idempotency ledgers; they solve duplicate replay, not lost updates between different events.

---

### 4. Concurrent arena battles can overwrite defender ratings and records

**Location:** `StartArenaBattleCommand`, `ColosseumService.StartArenaBattle`, `CharacterArenaProfileConfiguration`  
**Classification:** A — Correctness Issue  
**Severity:** High  
**Frequency:** Moderate; more likely around popular defenders

Only the attacker is protected by the transaction behavior. The defender’s arena profile has no concurrency token.

**Failure scenario:**

1. Attackers A and B challenge defender D concurrently.
2. Both load D’s rating and defense statistics at the same values.
3. Both compute their battle and rating changes.
4. Both insert match history.
5. The second profile update overwrites the first.
6. D loses one rating/statistics transition, while match history records two incompatible transitions.

**Recommended direction:** Lock attacker and defender in deterministic order using an arena-consistent lock, or add optimistic concurrency to arena profiles with a retry strategy that recomputes ratings from fresh state.

---

### 5. Achievement outbox delivery can permanently lose progress across replicas

**Location:** `AchievementGameEventOutboxConsumer`, `PlayerAchievementProgressConfiguration`  
**Classification:** A — Correctness Issue  
**Severity:** High  
**Frequency:** Moderate with one API replica; increasingly likely with multiple replicas

A worker processes its local batch sequentially, but separate API replicas can claim different deliveries for the same character. Achievement progress has a unique identity index but no concurrency token.

**Failure scenario:**

1. Replica A handles an equipment event for character C.
2. Replica B simultaneously handles an idle-combat event for C.
3. Both read achievement progress at 10 and assign 11.
4. Both add distinct `AchievementEventLedger` entries.
5. One progress update is lost.
6. Replay cannot repair it because both outbox IDs are now recorded as processed.

**Recommended direction:** Serialize achievement consumers per character, or add optimistic concurrency with a whole-event retry. The event-ledger insertion must remain in the same transaction as the recomputed progress.

---

### 6. Failed command responses are still committed

**Location:** `TransactionBehavior`, `RegisterCommand`, `GuestLoginCommand`  
**Classification:** A — Correctness Issue  
**Severity:** High  
**Frequency:** Rare, but broad in scope

`IsSuccessfulResponse` controls state-sync invalidation, but it does not control persistence or transaction commit. The behavior calls `SaveChangesAsync` and commits even when `Response.IsSuccess` is false.

**Failure scenario:**

1. Registration creates an identity user and publishes `UserCreatedEvent`.
2. Character/inventory creation is tracked within the transaction.
3. Character reload or token issuance fails.
4. The handler catches the exception and returns `Response.Fail`.
5. Because no exception escapes, the pipeline saves and commits.
6. The client is told registration failed even though all or part of the account was created.

A similar hazard exists whenever a handler mutates tracked entities and subsequently returns a failure result.

**Recommended direction:** Make failed mutation responses roll back by default, with an explicit opt-in for commands whose failure result intentionally persists state. Audit handlers that catch broad exceptions inside transactional commands.

---

### 7. Important guild invariants are check-then-insert without database enforcement

**Location:** `GuildRepository`, `GuildMemberConfiguration`, `GuildMissionOptionConfiguration`  
**Classification:** A — Correctness Issue  
**Severity:** High  
**Frequency:** Rare to moderate, but persistently invalid when triggered

Four invariants are exposed:

- `GuildMember` is unique by `(GuildId, CharacterId)`, but `CharacterId` alone is non-unique. Two guild officers can concurrently approve the same applicant into different guilds.
- Capacity is checked by loading the current member list. Different officers can concurrently approve different applicants and exceed `MaxMembers`.
- Guild names are checked using `Any(g.Name.ToLower() == name.ToLower())`, but there is no normalized unique index. Two creators can persist the same case-insensitive name.
- Weekly mission selection checks `Any(IsSelected)` before updating separate option rows. Two officers can select different options and create two mission instances because the `(GuildId, WeekKey)` indexes are not unique.

**Recommended direction:** Add database constraints for the durable parts of these invariants—unique character membership, normalized guild name, and one selected/active mission per guild/week—combined with a guild-scoped lock for capacity and other aggregate rules.

---

### 8. Colosseum match relationship mapping drops the attacker FK

**Location:** `CharacterConfiguration`, `LLDbContextModelSnapshot`  
**Classification:** A — Correctness Issue  
**Severity:** Medium  
**Frequency:** Every Colosseum match; visible during deletion/history queries

The same `Character.ColosseumMatches` navigation is configured twice, once for `CharacterAId` and once for `CharacterBId`. The current snapshot contains only:

- A foreign key for `CharacterBId`.
- An index on `CharacterBId`.
- No FK or index for `CharacterAId`.

Consequences include unprotected attacker references, asymmetric deletion behavior, and slow attacker history/cooldown queries.

**Recommended direction:** Model attacker and defender as distinct relationships or configure them without reusing one collection navigation. Decide explicitly whether historical matches should cascade, restrict deletion, or retain denormalized character identity.

## 4. Transaction & Concurrency Findings

The central lesson is that the current transaction boundary is necessary but insufficient.

| State | Current protection | Gap |
|---|---|---|
| Ordinary character mutation | Character advisory lock | Does not protect a second character or coordinate with row locks |
| Marketplace participant | `Entities FOR UPDATE` | Does not coordinate with ordinary advisory-only commands |
| Guild mission/resource | Requesting character lock | Shared guild/mission rows are unprotected |
| Arena profile | Attacker character lock | Defender is unprotected |
| Achievement progress | Outbox delivery lease | Lease protects one delivery, not all deliveries for one character |
| World Tower/raids/Region Boss | Named locks, leases, row versions | Generally sound |

Recommended lock discipline:

1. Define lock keys by aggregate, not by request shape.
2. Multi-character operations should resolve all participant IDs before mutation.
3. Acquire locks in sorted order to prevent deadlocks.
4. Ensure every writer of the same resource uses the same lock protocol.
5. Use database constraints as the final invariant guard.
6. Use concurrency retries only where the entire business calculation can safely be repeated.

Avoid simply adding `RowVersion` to `Character` without considering that it is a very broad aggregate: unrelated updates would conflict frequently. Narrower atomic balance operations or a consistent participant-lock protocol may be more maintainable.

## 5. High-Impact Query Problems

### 9. Leaderboards materialize and rank all participants before pagination

**Location:** `LeaderboardRepository.GetLeaderboardAsync`  
**Classification:** B — Significant Performance Issue  
**Severity:** High  
**Frequency:** Frequent when leaderboard UI is active

Each board-specific query first materializes all eligible scores. `LeaderboardRanking.Rank(scores)` runs in memory, and only then does the code call `Skip(...).Take(limit)`.

Several score queries also contain correlated dungeon, raid, achievement, or progression aggregates.

**Performance scenario:** Requesting page 20 performs almost the same database and application work as computing the entire leaderboard. At 10× players, approximately 10× score rows cross the database boundary for every page; at 100×, all pages remain tied to total population.

**Recommended direction:** Move top-N/page ranking into SQL using deterministic ordering and window functions, or maintain periodic leaderboard snapshots where exact realtime ranking is unnecessary. Preserve a separate targeted viewer-rank query.

---

### 10. Arena opponent and ranking queries load all eligible characters

**Location:** `ColosseumRepository.GetArenaOpponentsWithRating`, `ColosseumRepository.GetRankings`  
**Classification:** B — Significant Performance Issue  
**Severity:** High  
**Frequency:** Frequent for arena players

`GetArenaOpponentsWithRating` materializes all eligible `Character` entities and profiles, then sorts by `Math.Abs(rating difference)` and takes 25 in memory. `GetRankings` similarly loads all eligible characters before sorting.

The entities are tracked, although these list operations primarily need IDs, names, and rating fields.

**Recommended direction:** Filter, order, and limit in PostgreSQL and project the small response shape. The nearest-rating query can be expressed as a bounded database query, potentially using two indexed ranges around the player’s rating if a direct absolute-difference sort proves expensive.

---

### 11. Marketplace list and matching queries are unbounded

**Location:** `MarketPlaceRepository.GetMarketPlaceListingsAsync`, `GetMarketPlaceBuyOrdersAsync`, commodity matching queries  
**Classification:** C — Scalability Risk  
**Severity:** High  
**Frequency:** Frequent

The marketplace page loads every active eligible listing with several deep equipment include chains. Buy orders are also returned without pagination.

Commodity matching materializes every price-compatible listing/order before service code decides how much is actually needed. Larger active markets therefore increase:

- Rows and columns transferred.
- EF tracking state.
- Materialized equipment graphs.
- Time before the matching transaction finishes.
- Number of candidate rows considered and potentially locked.

**Recommended direction:** Add server-side filters and deterministic pagination to browsing. For matching, fetch candidates in price-time order in bounded batches and stop when the requested quantity has been satisfied.

---

### 12. Arena combat calculation occurs while the database transaction is open

**Location:** `ColosseumService.StartArenaBattle`, `TransactionBehavior`  
**Classification:** B — Significant Performance Issue  
**Severity:** Medium  
**Frequency:** Every arena battle

The transaction starts before character graphs, opponents, defense snapshots, and combat entities are loaded. Combat simulation and rating calculations occur before the final writes.

This retains a connection and transaction for CPU-heavy work and increases the window for contention and concurrency conflicts.

**Recommended direction:** Split battle preparation from the short authoritative settlement transaction. Capture immutable snapshots/versions, calculate outside the transaction, then lock/revalidate tickets, cooldown, and relevant versions before applying the result.

## 6. Index Analysis

### Missing / Potentially Valuable Indexes

#### ColosseumMatches

- **Query:** Attacker/defender cooldown and attacker-or-defender history ordered by `PlayedAt`.
- **Current index:** `CharacterBId` only.
- **Candidates:** `(CharacterAId, CharacterBId, PlayedAt DESC)` for cooldown and `(CharacterAId, PlayedAt DESC)` / `(CharacterBId, PlayedAt DESC)` for history.
- **Frequency:** Every battle and history view.
- The missing attacker relationship must be fixed before finalizing indexes.

#### GuildMemberContributionPeriods

- **Query:** Global weekly leaderboard filters `PeriodType` and `PeriodKey` without filtering `GuildId`.
- **Current index:** Unique `(GuildId, CharacterId, PeriodType, PeriodKey)`.
- **Candidate:** `(PeriodType, PeriodKey)` with final included/order columns chosen from an actual `EXPLAIN`.
- **Frequency:** Leaderboard reads.
- The current index ordering cannot efficiently serve the global period filter because `GuildId` is its leading column.

#### GameEventOutboxDeliveries stale-processing recovery

- **Query:** `Status = Processing AND ProcessingStartedAt < cutoff`.
- **Current indexes:** `(Status, AvailableAt, CreatedAt)` and `(Consumer, Status, AvailableAt)`.
- **Candidate:** Partial index on `ProcessingStartedAt`/`CreatedAt` where status is Processing.
- **Frequency:** Low in healthy operation, important during worker failure recovery.

These candidates should be validated against production-like cardinalities and `EXPLAIN (ANALYZE, BUFFERS)` before implementation.

### Potentially Redundant Indexes

`GuildMissionOption` defines both:

- `(GuildId, WeekKey)`
- `(GuildId, WeekKey, IsSelected)`

The second B-tree covers queries on the first two leading columns, making the shorter index potentially redundant. More importantly, neither enforces the selection invariant. A partial unique index for selected rows may replace the current pair more usefully.

No broad redundant-index problem was evident elsewhere; many apparently overlapping indexes support different buyer/seller/account and time-oriented access paths.

## 7. Hot Paths

| Operation | Approximate database shape | Main risk |
|---|---|---|
| Idle combat resolution | Character/action reads, inventory and progression mutations, loot/economy history, guild contributions, outbox message plus deliveries, one outer transaction | Shared guild lost updates; write amplification varies with loot/events |
| Dungeon reward claim | Active run, character/inventory, pending reward application, run deletion, ledgers/outbox, one transaction | Generally safe under character lock; failed-response commit behavior remains hazardous |
| Marketplace fill | Candidate listing/order reads, participant row locks, inventory and balance mutation, order history, multiple economy ledger entries, state-sync/outbox | Work grows with market depth; lock-domain collision with ordinary commands |
| Arena battle | Two character/profile graphs, opponent queries, defense snapshot, combat calculation, ticket/rating/stat/match writes | Defender race and long transaction |
| Crafting completion | Queue/recipe/material/inventory mutations, progression, guild contribution, achievement/outbox activity | Shared guild hot rows |
| Guild reward claim | Mission/contribution/member/character/guild/resource reads and writes | Multiple members update the same guild/resource rows |

The `Character` row is a broad hot row because many currencies and progression counters live together. Guild mission instances and guild supply rows are smaller but more strongly shared hot rows.

## 8. Background Worker / Outbox Findings

### 13. Region Boss job cadence is coarser than its configured interval

**Location:** `BackgroundJobRegistrationExtensions`, `RegionBossProgressionJob`  
**Classification:** A — Correctness Issue  
**Severity:** Medium  
**Frequency:** Background, every scheduled execution

The default trigger interval is 30 seconds, but the job’s business key only includes year/month/day/hour/minute:

```text
region-boss-progression:yyyyMMddHHmm
```

`RunOnceAsync` treats a completed business key as already processed. Two fires in the same minute therefore share a key and at most one performs progression. Configuring a ten-second interval still permits only one effective execution per minute.

**Recommended direction:** Align the idempotency-key precision with the configured schedule or derive the key from the exact scheduled fire instant.

---

### 14. External chat/realtime work occurs inside outbox database transactions

**Location:** `GameEventOutboxWorker.ProcessDeliveryAsync`, chat outbox consumers  
**Classification:** C — Scalability Risk  
**Severity:** Medium  
**Frequency:** Moderate, event-dependent

The worker opens a database transaction before invoking every consumer. Chat consumers can spend up to 30 seconds awaiting HTTP, and deliveries in a claimed batch are processed sequentially per replica.

The transaction cannot make the remote HTTP insert and local processed marker atomic. A crash after the remote call but before commit can still repeat the call. Deterministic chat message IDs make the authoritative remote insert substantially idempotent, which limits correctness impact, but the open transaction consumes a database connection and one slow delivery delays the remaining batch.

**Recommended direction:** Separate database-mutating consumers from external delivery consumers. External consumers should use deterministic idempotency IDs and short transactions only for delivery-state transitions.

### Outbox assessment

The outbox is otherwise strong:

- Claiming is multi-instance safe.
- Poison messages stop after five attempts.
- Stale Processing deliveries are recoverable.
- Message-consumer uniqueness prevents duplicate delivery records.
- Database-side consumers atomically update state and mark processed.
- Cleanup is present and bounded.

The remaining authoritative correctness concern is per-character ordering/concurrency for achievements, not duplicate claiming of one delivery.

## 9. Unbounded Data Growth

| Table/Entity | Growth source | Expected growth | Retention present | Risk |
|---|---|---:|---|---|
| `EconomyLedger` | Rewards, transfers, marketplace, fees, acquisitions | Very high; proportional to economy actions | No; append-only enforced | High |
| `BackgroundJobExecutions` | One row per unique scheduled business key | About 10,368 distinct rows/day under checked-in defaults, if enabled | No | High |
| `ChatMessages` | Player and system chat | High; proportional to social activity | None found | High |
| `ColosseumMatches` | One row per arena battle | High | None found | Moderate–High |
| `MarketPlaceOrders` | One row per completed fill | High | None found | Moderate–High |
| `PlayerTransferHistory` | Currency and item transfers | Moderate | None found | Moderate |
| Guild activity/contribution ledgers | Guild actions and progression | Moderate–High | None found | Moderate |
| Achievement/quest/event ledgers | Idempotency and progression events | High on active players | None found | Moderate–High |
| Raid/tournament/World Tower histories and replays | Multiplayer runs and playback | Moderate, potentially large payloads | None found | Moderate |
| Game-event outbox | Gameplay events × consumers | High transient volume | Yes: 7-day processed, 30-day failed | Low–Moderate |

The background-job estimate comes from:

- Tournament progression every ten seconds: 8,640/day.
- Region Boss distinct minute-level keys: at most 1,440/day.
- Marketplace expiration every five minutes: 288/day.

That is roughly 3.8 million execution rows per year before disabled periods or configuration overrides.

Historical data should not be deleted blindly. Recommended policy choices include:

- Keep economy ledgers long-term but partition/archive by time.
- Retain detailed PvP/chat history for a defined user-facing period, with aggregates or archive storage afterward.
- Keep idempotency ledgers long enough to cover replay windows, then prune or partition where business rules permit.
- Add cleanup for background execution rows after a safe operational/debugging window.

## 10. Scalability Risks

At approximately 10× users/data:

- Leaderboard requests transfer and rank roughly 10× the participant rows regardless of requested page.
- Arena opponent/ranking views load roughly 10× the full character/profile entities.
- Marketplace pages and matching load substantially more active rows and object graphs.
- Shared guild rows see more write contention and more frequent correctness races.
- Economy, achievement, chat, and job-execution indexes become materially larger.

At approximately 100×:

- Whole-population ranking paths are unlikely to remain viable as synchronous request queries.
- Unpaginated marketplace and guild-directory graphs become memory and payload risks.
- Append-only operational tables will require retention, partitioning, or archival.
- Long arena transactions and sequential external outbox delivery will reduce throughput well before query correctness fails.
- Startup-coupled migrations become increasingly operationally sensitive as tables grow.

### Migration operational risk

Both APIs apply migrations during process startup. EF provides migration coordination, but schema changes remain coupled to application readiness and every replica starts with migration capability.

Historical migrations include destructive `Up` operations. For example, `20260820090330_RemoveRaidSealEconomy` deletes marketplace orders and item definitions before dropping columns; its `Down` cannot restore deleted data.

This may be intentional product evolution, but it means production rollout needs:

- A reviewed migration plan.
- Backups and rollback strategy.
- Estimates for locks/table rewrites.
- A single controlled migration executor rather than relying solely on concurrent API startup.

## 11. Low-Priority Optimizations

- `GetAllGuildsAsync` loads every guild with owner, members, and buildings and uses tracking. Add pagination/projection before the guild population becomes large; it is not as urgent as the leaderboard and marketplace paths.
- Core currencies use `long`, marketplace prices use `long`, and no currency is stored as floating point. Selective database checks such as nonnegative balances, inventory quantities, order quantities, and unit prices would add defense in depth, but should follow remediation of the actual concurrency paths and a legacy-data audit.
- Marketplace item summaries issue several focused aggregate queries. They are reasonable at modest request volume; consider caching or consolidation only after measurement.
- The outbox stale-processing partial index is operationally useful but lower priority than the main pending-delivery path because stale recovery should be uncommon.

## 12. Things Investigated That Are Fine

- Direct `DbContext` use and repositories coexist intentionally; this is not itself a problem.
- The main command pipeline normally keeps gameplay mutations, ledgers, state-sync revisions, and outbox insertion in one transaction.
- Dungeon active-run uniqueness, row versions, reward application, and deletion provide strong protection against ordinary double claims.
- World Tower, raids, Region Bosses, and tournaments generally use named advisory locks, work leases, optimistic concurrency, or `SKIP LOCKED` appropriately.
- Marketplace listing/order row locks and deterministic participant lock ordering protect competing marketplace operations from double filling the same order. The identified flaw is interoperability with non-market character mutations.
- Quest and Event Quest progress use uniqueness/idempotency and, in important paths, concurrency tokens more defensively than achievement progress.
- Read-only chat queries are generally no-tracking, deterministically ordered, and capped around 200 rows.
- Large single-guild graph loading uses `AsSplitQuery` and `AsNoTracking`, which is reasonable for that intentionally comprehensive view.
- The outbox has bounded retries, dead-letter behavior, stale lease recovery, consumer uniqueness, observability, and retention.
- Time handling predominantly uses `DateTimeOffset`/UTC and PostgreSQL `timestamp with time zone`. No material `DateTime.Now` persistence problem was found.
- Floating-point fields are used for combat/stat modifiers, not authoritative currency.

## 13. Recommended Maintenance Order

1. Implement a consistent deterministic locking or atomic-update protocol for all multi-character currency mutations.
2. Apply the same protocol to inventory transfers, marketplace settlement, expiration refunds, and recipient stack updates.
3. Change command transaction semantics so failed mutation responses roll back; audit broad exception catches in registration and reward handlers.
4. Protect shared guild mission, guild XP, and guild resource updates; add database uniqueness for membership, names, and weekly mission selection.
5. Protect defender arena profiles and shorten the arena settlement transaction.
6. Serialize or optimistic-retry achievement processing per character so distinct outbox deliveries cannot lose progress.
7. Correct the dual Colosseum relationship mapping, then add indexes based on cooldown/history query plans.
8. Move leaderboard paging/ranking into PostgreSQL or a snapshot model.
9. Add marketplace browsing pagination and bounded price-time matching.
10. Correct Region Boss job-key precision and add retention for `BackgroundJobExecutions`.
11. Separate external outbox HTTP/realtime delivery from long database transactions.
12. Establish retention/partitioning policies for economy, chat, PvP, marketplace, and progression ledgers.
13. Move production migrations toward a controlled deployment step and review destructive migrations for backup/rollback requirements.

## Audit Scope and Verification

The audit was performed through static inspection of entity configurations, current model snapshots, migrations, handlers, repositories, workers, and transaction/outbox paths.

No live database was available, so index recommendations require production-like PostgreSQL cardinalities and `EXPLAIN (ANALYZE, BUFFERS)` before implementation.

No build or test suite was run because the original audit was analysis-only and made no executable changes.
