# LegendsLegacy telemetry, analytics, and game balance design

Status: design proposal, based on repository inspection on 2026-09-25. Paths and types under **Existing** are present in the checkout; names under **Proposed** are new design concepts. This document does not implement telemetry or change deployment configuration.

## 1. Executive summary

LegendsLegacy already stores substantial game history. Start by making that history queryable, then preserve the few facts that are lost at the moment of play: the build used for an encounter, the encounter outcome and reward attribution, and durable transitions between build and progression states. A small PostgreSQL reporting schema and scheduled SQL aggregation are enough for beta. Keep operational metrics in the existing .NET metrics/logging path; do not put player, item, or build IDs in metric labels. Do not stream combat ticks, replay logs, UI clicks, or all outbox messages into an analytics platform.

The first decision loop should answer: **where do players stall, what build did they use, and did a reward or build change help?** Measure eligible players and attempts, not just winning battles. Show denominators, cohort, game/balance version, and sample size beside every rate. Treat comparisons between builds as observational until controlled by progression, power, encounter, and player history.

## 2. Existing architecture and boundaries

| System | Existing source of truth and action boundary | Analytics consequence |
| --- | --- | --- |
| Accounts/characters | `LL/src/Core/Domain/Models/Users/AppUser.cs`; `.../Entities/Characters/Character.cs`; `LLDbContext.Users` and `.Characters` | Account creation and current character state exist; `AppUser.UpdatedUtc` is not a defensible activity signal. |
| Commands/transactions | MediatR commands in `LL/src/Core/Application/UseCases`; `MediatR/Behaviors/TransactionBehavior.cs` manages the transaction, save, and state invalidation | Add durable facts in the same successful command transaction where possible; beware nested commands and retries. |
| Idle combat | `CharacterActions/CombatService.cs` processes scheduled encounters in batches; `Combat/Layers/Orchestration/Idle/IdleCombatOrchestrator.cs`; `Combat/Layers/Rewards/Idle/IdleCombatOutcomeProcessor.cs` | Offline catch up is server activity, not a client session. The current `combat.idle_encounter_completed` outbox payload combines batches and has `ActionCount`/winning count, not a distinct record of each fight. |
| Shared combat | `Combat/Engine/FastCombatEngine.cs`, `Combat/Engine/CombatEngineExecutor.cs`, `Combat/Layers/Resolution/CombatEncounterResultFactory.cs`; `Domain.Models.Combat.CombatResult` | `CombatResult` already holds outcome, teams, duration, `EntityStats`, compact telemetry, and optional log. `EngineOutcome` and `ContentOutcome` can differ; report the mode appropriate to the design question. |
| Dungeons | `DungeonRun`, `DungeonRunState`, `RunReward`, `DungeonCompletionRecord`; `DungeonCombatOutcomeProcessor`, `DungeonRunRewardClaimer`; `DungeonRunRepository` | Run start/status/deaths/retreat/completion/reward claim and a run snapshot ID are persisted. Room or fight analysis needs a stable encounter fact if not retained in run state. |
| PvP | `ColosseumService`, `ColosseumMatchResult`, `ArenaDefenseSnapshot`, arena profiles/tickets; tournament entities/replays | Match outcome, ratings before/after, glory, and serialized combat result exist. Separate attacker from defender and distinguish voluntary participation from being selected as opponent. |
| Tower/raids/bosses | `WorldTowerService`, `TowerAttempt`, `TowerFloorProgress`, rally participant snapshots; raid and region boss runs/participant results; simulation/finalization workers | These are server or group attempts with persistent IDs. Tower floor progress is server scoped; do not label it a personal floor without joining participant history. |
| Essences | `EssenceSystemService`, `EssenceRepository`, `PlayerEssence`, `EssenceLoadout`/slots; JSON definition and creature loot table providers | Absorption, dust investment, ascension, evolution, loadout changes, activity selection, and drops have distinct boundaries. Current ownership/selection is queryable; prior loadouts require history. |
| Styles | `CombatStyleService`, `CharacterCombatStyle`/selection, `CombatStyleSnapshot`, JSON catalog | The code calls these **Combat Styles**. Selection can include refinement, upgrades and a Conduit channel; a selected style can be ineffective for an incompatible activity. Measure resolved style **in combat**, not only global selection. |
| Equipment | Ordinary idle drops are rolled by `CombatAcquisitionRewardProcessor` and settled through `IdleCombatRewardCalculator`/`InventoryLootRewardWriter`; `EquipmentAcquisitionService` rolls protected dungeon/mini boss/treasury rewards, `DungeonPendingRewardWriter` stores pending loot, and `DungeonRunRewardClaimer` grants claims. `EquipmentUpgradeService`, `EquipmentInstance`, loadouts/slots and item provenance handle later use | Acquired time/source, rarity/quality/tier, progression state, favorite, and upgrades exist. Generated, pending, claimed and equipped are separate stages. The ledger covers certain flows, not every currency mutation. |
| Guild/social | Guild/member/building/mission/contribution/shop/vault entities; separate raid content | Membership and contributions are queryable; historical eligibility, offers, and exposure usually are not. `GuildContentProvider` labels guild wars as a future unlock placeholder, so do not report guild war participation yet. |
| Prophecies/tasks | `ProphecyService`, `PlayerProphecyInstance`, `DailyProphecyRerollState`, quest/event quest progress and ledgers | Acceptance, reroll, completion, claims have durable state. Offer set is needed to compute choice **when offered**. |
| Economy | `Character` holds Cinders, Soulstones, FateEcho, GuildFavor, TowerTokens, RaidTrophies; arena profile holds Glory; essence system uses dust; inventory resources include sigils, parts/blueprints. `EconomyLedgerEntry` tracks selected item and currency flows | Do a source/sink coverage audit for each resource. Current balances are stock, not historical flow. Do not infer total currency minting from the current ledger without proving coverage. |
| Admin/monetization | `API.LiveOps` has audit, support, account risk, compensation and operational status; `API.AdminDashboard` is a separate CRUD/diagnostics boundary. Nobility membership/daily rewards exist; `CreateSignetCheckoutQuery` says the alpha gateway has no side effects | Start with a restricted BI report or LiveOps read only aggregate view. Nobility can be segmented for progression, but billing/revenue claims require an actual payment source. |
| Chat | `LL-Chat/` is independently deployable | Exclude message bodies and social graph from gameplay analytics. Guild membership/activity is enough for initial social analysis. |
| Strongholds | Design documents exist; no stronghold classes/controllers were found under `LL/src` in this audit | Deferred. Do not create stronghold events until implementation has authoritative actions and a specific decision. |

The primary database context is `LL/src/Infrastructure/Persistence/Persistence.LL/LLDbContext.cs`. Its existing sets include `CharacterActions`, `PlayerEssences`, both loadout types, `DungeonRuns`, `ColosseumMatches`, `TowerAttempts`, guild records, `EconomyLedger`, `ItemInstances`, `GameEventOutboxMessages` and deliveries. Existing `CharacterSnapshot`/`EquippedEssenceSnapshot`/`EquipmentSnapshot`, created through `CharacterSnapshotService`, are already used for committed battles. Do not automatically clone all of them per idle encounter: their object graph and numeric style tuning can be larger than an analytics build record.

### Current logging, telemetry and domain events

`IdleCombatTelemetry` records process measurements. `FastCombatEngine` and `CombatStatsAggregator` produce compact combat counters; `AbilityStats` holds uses, hits, damage, healing, barrier, threat, stagger and related values. `GameEventOutboxWorker` has `Meter`, `ActivitySource`, log scopes and lag reporting. `TransactionBehavior` and `StateSyncService` have operational metrics. LiveOps exposes outbox health. These are observability and debugging facilities, not a historical player analytics warehouse.

`GameEventTypes` and `GameEventPayloads` contain durable notifications for character creation/level, Essence absorption/loadout/ascension, aggregated idle combat, dungeon start/completion, Colosseum completion, prophecy completion, guild missions and inventory grants. Other messages are for realtime or chat. MediatR notifications such as `LootGeneratedEvent` and prophecy progress are in process and not automatically durable. Outbox messages do not carry a general schema version. `GameEventOutboxConsumerRegistry` fixes consumers **at enqueue time**; adding an analytics consumer later will not replay old rows. `GameEventOutboxRepository` deletes processed messages after retention. Consequently, the outbox can deliver selected analytics facts after commit, but is neither a permanent event store nor a complete history. Do not attach analytics to every existing message; many payloads lack item IDs, run IDs, build, or per encounter outcomes.

## 3. Goals, principles and decisions

1. Every fact must answer a named decision: tuning an encounter/drop, clarifying a feature, fixing a stall, or diagnosing an exploit. Drop fields that do not support one.
2. Capture once at the authoritative transition. Use IDs for joins, UTC occurrence time for chronology, ingestion time for delay, and idempotent keys for retries.
3. Distinguish **eligible**, **exposed**, **started**, **completed**, **claimed**, and **reused**. These are different denominators.
4. Store compact per encounter facts where needed; aggregate trivial idle farming and high volume ability details. No per tick production pipeline.
5. Store build at **commit time** or encounter start, with versioned content and relevant power. Never join a historical battle to today's loadout.
6. Separate a player action from scheduled server resolution; separate play time from wall time and offline progression.
7. Show n, unique characters, uncertainty, and version for rates. Prefer medians and percentiles to means for skewed time/economy.
8. Respect data minimization: no email, username, chat text, IP, device fingerprint, or precise session trail in balance facts.

## 4. Event taxonomy and fact definitions

Use **immutable business facts**, **state snapshots**, **derived daily facts**, and **operational measurements**. A fact is not necessarily a new outbox notification. Existing run/match/ledger rows can be extracted into reporting tables. Do not duplicate identical events merely to populate a dashboard.

| Proposed fact / source | Grain and essential columns | Why it is worth keeping | Priority |
| --- | --- | --- | --- |
| `analytics_combat_fact` | One resolved encounter or deliberate aggregate interval; `fact_id`, `character_id`, `occurred_at`, `mode`, `source_id`, `encounter_key`, `outcome`, duration/ticks, health fractions, damage/heal/barrier totals, `build_snapshot_id`, reward reference, content/balance version, `aggregation_count` | Difficulty cliffs, farming, success by build; aggregate idle when an interval would otherwise explode | P0 |
| `analytics_build_snapshot` + child essence/equipment rows | One canonical build captured when committed to content; immutable `snapshot_id`, fingerprint, character, effective style, level/power, content version and compact slot data | Historical attribution and post failure switching | P0 |
| `analytics_progression_fact` | One first milestone or durable region/area transition per character | Cohorts, funnel, bottleneck | P0 |
| `analytics_item_lifecycle_fact` | One item acquisition, equip/replace, favorite, dismantle, upgrade or transfer with item instance ID and source | Drop versus actual value, time to upgrade | P1 |
| `analytics_essence_change_fact` | One absorb, equip/unequip, dust spend, ascend/evolve with definition and player essence ID | Owned to tried to used funnel and investment | P1 |
| `analytics_style_change_fact` | One selection/config change plus effective style in build snapshot | Unlock/selection versus actual use and switching | P1 |
| `analytics_feature_offer_fact` | One eligibility/exposure/offer set when the server actually generates it; later selected ID | Prophecy choice when offered, unlock to usage funnels | P1, narrowly scoped |
| `analytics_economy_flow_fact` | One resource delta with source/sink, quantity, balance after, reference ID | Source/sink coverage, inflation and suspicious generation | P1 after mutation audit |
| Run/match/tower/raid/guild facts | Derive from existing durable rows; add only missing participant/build/version or offer timestamps | Avoid duplicate writes | P1/P2 |
| `analytics_activity_day` | One account/character/day derived from meaningful server action or session heartbeat policy | Retention with declared definition | P1 |

**Contracts.** An envelope should hold `event_id` (stable UUID or operation key), `event_type`, `schema_version`, `occurred_at_utc`, `recorded_at_utc`, `account_analytics_id`, `character_id`, `source_kind`, `source_id`, `game_release`, `balance_version`, and typed payload. Payloads are distinct records (`CombatFactV1`, `BuildSnapshotV1`, `ItemLifecycleV1`, etc.), rather than one object with dozens of nulls. Content keys such as Essence definition, area, creature, dungeon and style use catalog IDs; player/item/run/match/attempt use persistent GUIDs. Release and balance version must be captured at event time. `EquipmentStatBudgetCatalog.BalanceVersion` and `CombatStyleSnapshot.ContentVersion` are existing version inputs, but a single combat content manifest version is still needed.

**Delivery.** Prefer inserting analytics fact rows in the same transaction as gameplay for P0 fields, with a unique `(source_kind, source_id, fact_kind, subject_id)` key. For reporting outside the OLTP database, enqueue a *small, purpose built* outbox notice or read committed facts incrementally. Existing outbox delivery is at least once: consumers must upsert by immutable fact ID. Check whether direct `SaveChangesAsync` in `WorldTowerService` and finalizer workers uses the same transaction boundary before adding producers. Never let a reporting outage fail combat; a failed consumer should retry or backfill from durable facts. Set a storage budget and record dropped/late/failure counts as operational measurements.

**Schema evolution.** Add optional fields within a version, issue V2 for semantic changes, keep old readers until retention expires, and publish a versioned data dictionary with definitions and sample payloads. Do not silently change what `won` or `duration` means. Persist source IDs and sufficient raw facts so aggregates can be rebuilt when eligibility or region filters change. Maintain a small schema registry in code/tests; avoid a separate registry service initially.

A compact proposed C# contract sketch (not existing code):

```csharp
public sealed record AnalyticsEnvelope<T>(
    Guid EventId, string EventType, int SchemaVersion,
    DateTimeOffset OccurredAtUtc, Guid? CharacterId,
    string SourceKind, string SourceId, string BalanceVersion, T Payload);

public sealed record CombatFactV1(
    string Mode, string EncounterKey, Guid BuildSnapshotId,
    int Attempts, int Wins, int Losses, int Draws,
    int? DurationTicks, int? RemainingHealthPercent,
    long DamageDone, long DamageTaken, long HealingDone,
    long BarrierGenerated, int AggregationCount);

public sealed record BuildSnapshotV1(
    Guid SnapshotId, string Fingerprint, string ContentVersion, int CharacterLevel,
    int PowerRating, string? EffectiveCombatStyleId,
    IReadOnlyList<EssenceSlotV1> Essences,
    IReadOnlyList<EquipmentSlotV1> Equipment);

public sealed record EssenceSlotV1(
    int Slot, string DefinitionId, int Level, int AscensionTier);
public sealed record EquipmentSlotV1(
    string Slot, string TemplateId, int Tier, string Rarity,
    string Quality, int? ProgressionRank);
```

`EconomyFlowFactV1` would separately require resource ID, signed delta, source/sink code, operation ID and balance after; `ProgressionMilestoneV1` would require milestone ID and first reached time. Do not put these unrelated fields into `CombatFactV1`. For aggregated idle rows, `Attempts = Wins + Losses + Draws = AggregationCount`; do not fill `DurationTicks` or remaining health with a misleading average when unavailable.

## 5. Combat and build analytics

### Encounter grain and volume

For deliberate dungeon, boss, PvP, raid and Tower battles, keep one compact result per attempt or participant with outcome, duration, rounds/ticks, team health at end, deaths/revives, damage/healing/barrier, encounter key, and build reference. `EntityStats` and `AbilityStats` can produce top level and bounded per ability summaries; add a child ability summary only when deciding ability tuning. Distinguish damage **attempted**, effective health damage and prevention, using existing engine definitions. Overhealing/consumed shielding are not consistently established as first class analytics facts; validate engine coverage before claiming those metrics. Keep full combat logs/replays only under existing gameplay retention or sampled debugging, not as permanent analytics.

Idle combat is special. `CombatService` can settle multiple batches after offline time. One outbox message combines those batches, so it cannot support per monster win rates, attempts before success, or a loss followed by a build switch. At P0, emit a compact aggregate per `(character, area, captured build, balance version, time bucket)` **plus** individually record boss, first encounter in a new area, failure, and encounter following a build change. Include `encounter_count`, win/loss counts, reward sums and time span. If detailed per encounter analysis becomes necessary, a bounded option can write one fact per encounter only after measuring volume. A clean unit is the deterministic schedule generation and boundary; retries must not duplicate settled encounters. Count `no work due` as no combat, and distinguish actual combat time from hours of offline catch up.

For win rate, numerator is victories and denominator all resolved eligible attempts, with draw/abort separate. For progression wall, compare adjacent *eligible and attempted* areas and number of distinct players. Repeated failure is consecutive attempts against the same encounter within a character's history; a build switch is a change in canonical build fingerprint between attempts. The resulting improvement is a signal for investigation, not proof that a specific Essence caused it. Time to kill requires consistent engine duration units and excludes aborted/no contest results. Show P50/P90 and the censored share where the encounter was not killed.

### Compact build snapshot

The existing `CharacterSnapshot` already captures level, base attributes, `CombatStyleSnapshot`, equipment instances/rolls and equipped Essence slots for committed content. For analytics, project it into a smaller immutable snapshot keyed by a fingerprint: level, content version, activity, effective style/refinement/upgrades/mastery, character power or combat rating, selected derived stats, sorted Essence `(slot, definition, level, ascension, evolved)`, and equipment `(slot, template, rarity, quality, tier, progression rank/style, normalized important rolls)`. Keep item instance IDs in a restricted child table for lifecycle joins, not a dashboard dimension. Store **both** IDs and a few comparable numeric values, because catalog definitions change. Avoid full ability definitions, image/name, JSON tuning and all combat stats in every snapshot. Reuse an unchanged fingerprint per character/version/activity, but record the chosen snapshot ID for each attempt. Canonicalize slot ordering and floating values before hashing. A build fingerprint describes composition, not a player identity.

Combination analysis starts from the child Essence rows: singleton and pair counts by eligible owners and by actual combat participation. Derive trios on demand for sufficiently common builds; never emit every pair/trio as a separate event. Report pair lift `P(A,B)/(P(A)P(B))` only with ownership/slot eligibility and minimum n, and compare across matched level, power, activity and version strata. Build diversity: distinct build fingerprints per 100 participating characters, effective number of builds `exp(Shannon entropy)`, top one/top five share, and median Jaccard similarity of Essence sets. Report sample size and available catalog/slot constraints. A high diversity number can still hide one dominant competitive build; segment by endgame PvP, region, and power band.

## 6. System specific decision metrics

### Essences and abilities

Ownership = active eligible characters with a `PlayerEssence` definition / active eligible characters; current equipment = characters whose *activity resolved* loadout contains it / characters who can equip it. Ever equipped requires history and cannot be reconstructed reliably from current loadouts. Usage = participating encounters with it / encounters with available slots in the same activity; also show unique players to prevent farmers dominating. Compare win/damage/healing/barrier by progression, rating/power, encounter and version; report the Essence's contribution when engine attribution exists. `AbilityStats` supports uses/hits/effective totals but some effects have only entity level counts; only claim effect specific rates after validating source mapping. Capture end of combat summaries, not activation events. Acquisition comes from item provenance and absorption from `PlayerEssence.AbsorbedAt`; first use and time to dust/ascension need new history. Define abandonment as acquired and not used for 30 active days among characters still active, with right censoring.

### Equipment

Use `ItemInstance.AcquiredAtUtc`/`AcquisitionSource`, `EquipmentInstance` rarity/quality/tier/progression/favorite and inventory lifecycle. Treat equipment generated as pending dungeon reward differently from claimed inventory item. Track the share of acquired items equipped within 7 active days, favorite rate, dismantle/transfer rate, first equip lag, and replacement time by slot. A **meaningful upgrade** should be a published rule (e.g., normalized combat rating increase above a small threshold or higher progression rank), not any equip click. The `EquipmentUpgradeReceipt` is an excellent idempotent source for upgrade spend and outcome. Median *active time* between meaningful slot upgrades requires a trustworthy activity clock; until then use wall days plus settled combat encounters, clearly labeled. Compare drop distribution with equip time weighted distribution. Inspect events are optional UI signals, useful only after a concrete question about discovery; no need to track every inventory view. There is favorite and dismantle behavior; do not invent a sell/salvage funnel if no such action is implemented.

### Combat Styles / Doctrines

Use the code's Combat Style terminology. Derive current selection/mastery from persisted style rows. Add a selection history at successful `CombatStyleService.SelectAsync`; snapshot the *resolved* style for each activity. Calculate unlock/selection/use denominators separately. Selection without resolved use, especially Conduit incompatible with a loadout, is a design issue. Switching frequency should be per active character week and also after repeated failures. Long term loyalty is share of eligible combats using the same style across four active weeks. Compare style performance only within encounter, power, level, Essence and version cohorts. Examine the top build fingerprint share within each style to detect homogeneous styles.

### Progression, regions, encounters and rewards

Derive character creation, first combat, first Essence absorption, first equipment equip/meaningful upgrade, first region/area entry, boss attempt/clear, dungeon eligible/start/complete/claim, PvP eligible/first voluntary match, guild eligible/join, Tower eligible/first attempt. Verify exact unlock gates in use cases before implementing eligibility. Avoid a fixed linear funnel when these branches can be entered in different orders. For each milestone show unique eligible characters, reached, median calendar time, median active days, and settled encounter count; right censor recent accounts. `CharacterAction` schedule gives earned combat cadence, not human active time. Use account age for onboarding, character age for progression, active days for player effort, and simulated combat duration for fight tuning. Reward attribution needs source ID, generated versus claimed and spent, plus later use in a build; a reward that is never claimed or equipped has a different problem than a low drop rate. Farming flags compare rewards per settled encounter and per active player within region/version rather than raw counts.

### Dungeons, Tower, PvP, raids and bosses

For dungeons derive entrance, status, time, deaths/retreat, pending rewards and claim from `DungeonRun`; denominator for participation requires unlocked/eligible characters. Group by definition, grade, mastery, region and snapshot. Mark a dungeon as potentially mandatory farming if its share of power granting rewards and repeat runs rises while other available content is ignored. Tower data has rally, attempt, floor, mode, first clear, duration, participant snapshots and success. Measure floor reach from actual eligible participants, attempts per floor and conditional success, with server unlock as a separate event. PvP reports must distinguish attacker usage, defender snapshot usage, ticket availability/consumption, match rating gap and season/tournament participation. Ratings, glory and combat result are already in `ColosseumMatchResult`; season comparability needs a version/season key. Group raid and region boss outcomes need participant grain and contribution, not just group win rate. Never compare top rated players with all players.

### Guilds, prophecies, Stronghold, achievements and monetization

Guild conversion = joined / eligible nonmember characters; retention comparison must match account age, progression and prior engagement and still cannot establish causal benefit. Measure participation concentration with top 20% member contribution share and percent of members with zero contribution. Guild building investment and mission choice should have available option denominators. Prophecy selection rate is `selected / times offered to eligible character`; persist compact offer set IDs when generated, with a deterministic offer instance ID, then compare accepted, completed, rerolled and claimed. Quest/event quest, achievements and titles can mostly use existing progress and unlock rows; track UI opens only to diagnose a discovery problem. Stronghold telemetry starts when the system has real authoritative building/use/resource actions. For Nobility, report coverage and gameplay effects by cohort, but do not calculate conversion or revenue without billing transaction data; avoid treating gameplay reward events as sales.

### Economy

Build a resource mutation map for Cinders, Soulstones, FateEcho, GuildFavor, TowerTokens, RaidTrophies, Glory, Essence dust, equipment parts and sigil resources. For each, identify every award/spend/transfer code path and test that `opening balance + sources - sinks + transfers = closing balance`. `EconomyLedgerEntry` already covers certain flows and provenance, but its event enum and call sites do not establish full coverage of every character balance. A new typed resource delta should be written only for uncovered major flows; do not double count an existing ledger row and a new fact. Report sources/sinks per unique active character and per settled encounter, balance P50/P90/P99 by region/age, zero balance share, and days to spend. A stock increase is not inflation by itself. Flag impossible gains against server configured reward ceilings with source reference and replayable operation ID; it is an investigation signal, not automatic enforcement.

### Retention, engagement, adoption and content use

Define `active_day` as a distinct UTC day with a meaningful authenticated server action (combat settlement attributable to a player initiated active schedule, build change, run start, PvP match, guild action). Publish a separate `passive_progress_day` for offline settlement so D1/D7 are not inflated by background work. D1/D3/D7/D14/D30 are calendar day returns from account creation, with a declared timezone and an observation window long enough to mature. Show cohorts by account registration week, region reached and version, but avoid segmenting to tiny cells. An optional session is a bounded authenticated foreground heartbeat with idle timeout, consent/policy review and no page by page tracking; do not infer a session from SignalR invalidation. Also measure meaningful progression return and content completion, not DAU alone. Churn is descriptive (no meaningful action for 7/14/30 days) and right censored; do not call a newly created or offline progressing account churned.

Feature adoption rate = unique eligible characters performing the first meaningful action within 14 days of unlock / unique characters eligible for the full 14 day window. Feature retention = adopters who repeat a meaningful action in days 15–42 / adopters with a full 42 day observation window. Offer/open/interact may be useful diagnostics, but unlock and use are the default. Content utilization = unique eligible characters attempting content / eligible characters, plus attempts per adopter and rewards per attempt. An avoided monster requires measuring opportunity: available area, presented target or encounter selection, and player time in that area. Raw encounter count alone cannot distinguish avoidance from rarity.

## 7. Segmentation, causal caution and small samples

Standard dimensions: release and content/balance version, UTC cohort, account/character age, region/area, level, combat rating or item power band, activity mode, encounter/dungeon/floor/season, voluntary versus passive participation, and new versus returning players. Use fixed bins defined in a versioned dictionary. Never tag these high cardinality IDs on OpenTelemetry/Prometheus instruments; they belong in SQL rows. Keep raw content IDs in dimensions rather than a separate time series per combination.

Display `wins 31/47 (66%)`, unique characters 19, and a Wilson interval where informative. Hide/suppress comparisons under a minimum of ~20 distinct characters or mark them exploratory; use a higher bar for automated alerts. Deduplicate repeated combats by character or use character weighted estimates so a single farmer does not determine an Essence win rate. Compare within the same content version and encounter/power strata. For guild or build benefit, use matched cohorts or within character change analysis and explicitly label residual confounding. If a patch changes eligibility, do not interpret a raw before/after difference as a balance effect. Pre-register a few primary balance questions per patch. Small population data can still show obvious bugs and trajectories; it rarely justifies fine grained causal rankings.

## 8. Analytics architecture, storage and aggregation

**Beta architecture:** PostgreSQL in the existing persistence stack, separate `analytics` schema or table prefix with access restricted to service and read only reporting role. The transactional writer records P0 immutable facts and pointers to snapshots. A scheduled worker aggregates daily; current worker infrastructure includes Quartz jobs under `LL/src/Worker/Worker.LL/BackgroundJobs` and hosted workers in `API.LL`. Choose one existing job host after deployment topology check. Keep analytics queries out of hot gameplay endpoints; query a read replica or bounded materialized tables if load warrants it. Backfill existing `DungeonRuns`, `ColosseumMatches`, `TowerAttempts`, `PlayerEssences`, item provenance and ledger rows with provenance flag `backfill`, and mark unavailable fields as unknown, never fabricated.

**Proposed tables:** `analytics_fact_ingest` (optional external ingest/idempotency envelope), `analytics_combat_fact` (partitionable by occurred month), `analytics_build_snapshot`, `analytics_build_essence`, `analytics_build_equipment`, `analytics_progression_fact`, `analytics_item_lifecycle_fact`, `analytics_essence_change_fact`, `analytics_feature_offer_fact`, `analytics_economy_flow_fact`, and `analytics_daily_metric` (`metric_key`, date, dimensions JSON with controlled keys, numerator, denominator, distinct characters, version). In milestone one, build only the five exact tables in Section 15, then add domains by decision. Index combat on `(occurred_at, mode, encounter_key, balance_version)` and `(character_id, occurred_at)`; build children on snapshot and content definition. Store JSON only for versioned bounded details, not the whole combat log. An optional analytics `source_row_id` makes historical recomputation and audit easy.

**Aggregates:** each daily job reads a high water mark plus late window, upserts deterministic keys, and records source count/checksum and completion time. Rebuild a date/version from immutable facts when definitions change. Materialize `daily_encounter`, `daily_build_usage`, `daily_essence_usage`, `daily_resource_flow`, and `daily_progression` rather than a generic cube. For approximate distinct users, start with SQL exact counts at beta scale. Keep a metric catalog with numerator, denominator, time grain, exclusion criteria, and the decision it informs. Check reconciliation: raw combat count by mode versus source run/match/settlement count, currency deltas versus balances, and duplicate fact ratio. Dashboard freshness and missing event rates are first class operational signals.

**Scale later:** only when measured query/write pressure justifies it, move analytical reads to a replica/warehouse through the outbox/CDC with the same contracts and idempotency. Avoid Kafka, ClickHouse, a data lake, dedicated analytics API and custom admin frontend for beta. Existing BI against aggregate views is cheaper to iterate. If an internal page becomes worthwhile, `API.LiveOps` is the natural read only authorization boundary, with aggregate only endpoints; do not extend the CRUD AdminDashboard with raw player histories by default.

## 9. Privacy, retention and operation

Use pseudonymous account analytics ID derived from an internal mapping, character GUID only in restricted fact tables, and aggregated dashboard rows with small cell suppression. Pseudonymized data remains personal data when it can be linked back to a person; store the lookup separately with narrower access, consistent with [EDPB guidance](https://www.edpb.europa.eu/topics/ai-and-technology/anonymisation-pseudonymisation_en). Maintain a documented erasure/retention path from account ID to every fact/snapshot and derived table; delete or irreversibly anonymize on applicable request, then recompute aggregates. Keep direct account identifiers, email, chat content, IP and device data out of the reporting schema. Validate lawful basis, purpose notice, access roles and retention against [GDPR Articles 5 and 17](https://eur-lex.europa.eu/eli/reg/2016/679) with counsel before production. Protect item IDs and exact timestamps in restricted views since they can reidentify small cohorts.

Initial retention proposal, to be adjusted after volume and privacy review: raw sampled combat detail/replays 7–30 days; compact individual combat facts 90–180 days; daily aggregates 24 months; build snapshots and behavior facts 12 months or less if no longer needed; operational logs/traces 14–30 days; security/audit data follows its separate approved retention schedule. Persist versioned monthly aggregate and cohort tables before deleting raw facts; after deletion, changing old eligibility rules cannot be fully recomputed. State that boundary clearly in reports. Outbox retention is independent of analytics retention.

Alert on ingestion lag, failed deliveries, duplicate rate, missing build reference, reconciliation drift, and daily job failure. For balance flags require a mature baseline, two independent windows, meaningful minimum distinct characters and an effect size threshold; use Wilson/Bayesian shrinkage for sparse cells and include absolute change. Example: area success falls 20 percentage points with at least 50 characters in each version and stable mix; investigate, do not auto tune. For exploit flags compare progression speed and resource gain against maximum plausible schedule/rewards, using account age and version; review manually. Provide a developer command or restricted diagnostics endpoint that displays fact IDs, source operation ID, schema version and redacted payload for one action. Keep it out of public APIs.

## 10. Dashboards

| Report | Decision and measures | Filters and display | Refresh / alert |
| --- | --- | --- | --- |
| Game health | Is onboarding/retention/progression healthy? Mature D1/D7, first combat/Essence/upgrade funnels, active cohorts, errors | cohort, version, region; funnel and trend with n | daily; data freshness alert |
| Combat balance | Which encounters became walls or trivial? attempt/win, P50/P90 duration, health left, repeat failures | mode, encounter, region, power band, version; ranked table + adjacent area plot | daily; large version delta flag |
| Essence and builds | Which options are owned, tried, used, effective, or ignored? owner conditional use, combinations, diversity, contribution | activity, level/power, region, version; table + pair matrix only above n | daily; concentration flag |
| Equipment | Do drops become upgrades? generated/claimed/equipped, time to meaningful upgrade by slot, rarity use | source, region, slot, tier, version; lifecycle funnel + percentile plot | daily |
| Styles | Are selections used and competitive? unlock/select/resolved use, mastery, switching, build concentration | activity, region, power/version; table + trend | weekly |
| Progression/content | Where do players stop? reached/eligible, time/attempts to next milestone, ignored content | cohort, region, version; milestone funnel, survival plot | daily |
| Dungeon/Tower | Which run/floor blocks progress? entrance/eligible, completion, attempts, reward claim | definition/grade/floor/mode/version; ranked table and floor curve | daily |
| PvP | Is matchmaking/meta healthy? ticket use, rating gaps, win by gap, top build share | season, rating band, attacker/defender, version; histograms + table | daily |
| Economy | Are sources/sinks drifting? net flow and balance percentiles by resource | source/sink, level/region, version; waterfall + percentile trend | daily; reconciliation alert |
| Guild/prophecy/other | Is eligible content being used? join/eligible, contribution concentration, chosen/offered, completion | guild size, prophecy option, cohort/version; tables | weekly |

All percentages link to their numerator/denominator definitions and display counts. No dashboard should surface one player by default. Refresh more frequently only if it changes a live decision.

## 11. Analytics questions catalogue

The following are decision questions, not a mandate to collect 110 new event types. A report is useful only when its denominator, observation window, segment and version are explicit. A question with no current action owner can be postponed.

### Onboarding and retention

1. What share of created accounts creates a character within one day?
2. What share of characters starts meaningful combat within one active day?
3. What share of first combat participants acquires an Essence within seven active days?
4. What share of first Essence owners absorbs it, rather than leaving it as an item?
5. Which first region/area has the largest eligible to attempted drop?
6. Which first region/area has the largest attempted to cleared drop?
7. How many settled encounters does the median new character need before the first meaningful equipment upgrade?
8. Which onboarding milestone predicts failure to return on D7 after controlling for cohort and first day activity?
9. How does D7 meaningful action retention differ from D7 passive offline progression?
10. Among eligible first week accounts, how many have a second active day versus only one long initial session?
11. After three failed encounters in the same area, what share returns for another attempt within seven days?
12. Do players who start as guests progress differently from registered accounts of the same age and version?

### Combat, monsters and regions

13. Which encounter has the lowest success among characters within its intended power range?
14. Which adjacent area pair has the greatest fall in unique character success rate?
15. Which encounter has the highest median failed attempts before first clear?
16. Which monster has the highest P90 fight duration among successful intended level fights?
17. Which monster is farmed most per eligible active character, excluding offline catch up duplication?
18. Which available monster is attempted least after accounting for opportunity to encounter it?
19. Which bosses are defeated with unusually high remaining player health in the intended power range?
20. Which bosses cause repeated deaths without a corresponding rise in rewards or progress?
21. What share of encounters end in engine victory but content failure, by mode and version?
22. Do encounters with a high revive count also have a high abandonment rate?
23. Which status effects contribute most to wins in matched encounter and power strata?
24. Which ability has high use count but low effective damage/healing per use against its intended target?
25. How often does a failed encounter lead to a build change before the next attempt?
26. Among changers after failure, how many then clear the same encounter, compared with similar nonchangers?
27. How many unique characters are exposed to each area before its boss, rather than only farming earlier areas?
28. Does a new balance version change attempt rate as well as win rate for the same encounter?

### Essences and builds

29. Which Essence has less than 2% combat use among eligible owners in its intended progression range?
30. Which Essence is commonly equipped in a saved loadout but rarely resolved in combat?
31. Which Essence has the longest median time from acquisition to first combat use?
32. Which Essences are absorbed but receive no dust investment within 30 active days?
33. Which Essences are consistently kept in the first slot, and does that reflect a mechanical advantage?
34. Which Essences are mainly placed in backup/activity specific loadouts?
35. Which pairs occur more often than expected among characters owning both options and enough slots?
36. Which pairs almost never coexist despite similar progression availability?
37. What is the top complete Essence composition share in endgame PvE versus endgame PvP?
38. What share of competitive players share at least seven of the same ten Essence definitions, where ten slots are available?
39. How does effective number of builds change from early to late regions?
40. Which Essence rises sharply in use immediately after a balance patch, within matched cohorts?
41. Which Essence is removed most often after a defeat and then restored after a win?
42. Which Essence's win association disappears after matching on encounter, item power and level?
43. Which Essence has high contribution in one activity but low contribution in another?
44. Are summoning Essences extending combat duration without improving completion?
45. Which abilities generate many barriers but little actual damage prevention, where prevention is attributable?
46. What share of Essence acquisitions originate from each creature family and region?
47. Does an Essence drop lead to absorption more often when obtained from focused creature farming?
48. Which Essence upgrade tier yields the largest change in performance for comparable players?

### Equipment and rewards

49. Which equipment slot has the longest median number of settled encounters between meaningful upgrades?
50. Which item templates drop often but are equipped by few eligible owners?
51. Which rare items are replaced within one active day?
52. What share of generated dungeon equipment is claimed, equipped, favorited or dismantled?
53. How does equip probability vary by rarity, quality, tier and acquisition source?
54. Which item stat combinations are kept longest in the same slot?
55. Which affixes appear disproportionately in successful builds after power matching?
56. Which equipment type has the highest proportion of upgrades reversed within one week?
57. How many drops does a median character receive before upgrading each slot?
58. Do blueprint rewards lead to subsequent equipment upgrades, or remain unused?
59. Which region produces equipment too weak to displace what entrants already own?
60. What share of power increase comes from equipment upgrades versus Essence/style progression?
61. Which reward source produces the highest *used* equipment power per eligible attempt?
62. Are protected dungeon equipment drops solving long droughts in the intended slot/tier?

### Combat Styles

63. Which Combat Style is selected by the most eligible characters but resolved in the fewest fights?
64. What share of Conduit selections resolve to no effective style due to incompatible channel/loadout?
65. How many active weeks does a median character keep the same effective style?
66. Which style is chosen only for PvP or one dungeon rather than across activity types?
67. Which style's mastery levels are reached but never used in subsequent combat?
68. What share of a style's users have the same Essence build fingerprint?
69. Does style switching rise after repeated failure at a specific boss?
70. Is a style's apparent success explained by higher equipment power or progression?
71. Which refinement/upgrade choices are rarely selected among eligible style owners?
72. Which style choice leads to the largest divergence between selected and actual combat use?

### Dungeons, Tower, bosses and raids

73. Which eligible dungeon has the lowest first entrance rate?
74. Which dungeon room has the highest retreat or death rate among runs reaching it?
75. Which dungeon has the largest gap between completion and reward claim?
76. Which dungeon attracts most repeat runs after mastery is already capped?
77. Which dungeon supplies a disproportionate share of meaningful equipment upgrades?
78. Which Tower floor has the largest eligible participant to first attempt drop?
79. Which Tower floor has the highest share of characters failing at least five times?
80. Do build changes between Tower attempts precede clears in matched power cohorts?
81. Which Tower rally mode has the highest cancellation or incomplete roster rate?
82. How does first clear time change after a Tower balance version change?
83. Which raid/region boss has the highest signup to participation drop?
84. Are boss rewards concentrated among a small share of repeat participants?

### PvP and competitive meta

85. What share of PvP eligible characters initiates at least one match per season?
86. How often do tickets reach cap among otherwise active eligible characters?
87. What is the distribution of attacker/defender rating gaps at match time?
88. Does attacker win rate vary sharply with rating gap after removing rematches?
89. Which build composition accounts for the largest share of top rating band **attacker** matches?
90. How different is the defender snapshot meta from the voluntary attacker meta?
91. Does one Style dominate top rating bands after controlling for player count?
92. Which Essence pairs become dominant only after a tournament or season reset?
93. Are repeated opponent pairings associated with abnormal rating or glory gains?
94. What share of participating characters stops PvP after three consecutive losses?

### Economy, guild, prophecies and adoption

95. Which source generates most Cinders per active character in each region?
96. Which resource has growing P90 balances while spend frequency falls?
97. Which source/sink code paths fail to reconcile against character balance changes?
98. Are unusual reward gains concentrated in one content version or source operation?
99. Which guild mission options are selected most **when offered** to eligible guilds?
100. What fraction of guild members contributes nothing over four active weeks?
101. Is guild joining associated with D30 retention after matching prior activity and progression?
102. Which guild buildings receive investment but show little subsequent use or member benefit?
103. Which prophecy has the lowest selection rate among times it was offered?
104. Which prophecies are accepted but rarely completed by eligible characters?
105. Do rerolls concentrate on particular prophecy categories or difficulty levels?
106. Which unlocked feature has the largest gap between eligible and first meaningful use?
107. Which adopted feature has the weakest 15–42 day repeat use rate?
108. Which achievement/title rewards are earned but never selected or displayed?
109. Which Nobility benefits are used after coverage starts, without assuming payment conversion?
110. When Strongholds exist, which buildings are upgraded but rarely activated or used?

## 12. Prioritization and implementation roadmap

| Phase / priority | Implement and validate | Likely codebase touch points, database and release implications |
| --- | --- | --- |
| 0: audit / P0 | Inventory exact settlement, activity, reward and currency mutation boundaries; choose baseline game/balance manifest; measure idle encounter volume; write metric dictionary and source coverage matrix | Read `CombatService`, both outcome processors, `WorldTowerService`, `ColosseumService`, `DungeonRunRepository`, `EssenceSystemService`, item/ledger repositories. No migration. Establish data volume and retention budget before schema. |
| 1: foundation / P0 | Typed fact contracts, idempotent writer, compact build projection, transactional combat facts for first mode, daily encounter aggregate | New contracts/interface in Core Application, persistence in `Persistence.LL`, producer at settled outcome boundary in `Services.LL`, EF migration for proposed tables, Quartz/worker job, configuration for enabled modes/retention. No changes to combat rules. |
| 2: core game / P0–P1 | Add dungeon/PvP/Tower/raid adapters from durable source rows; progression milestones, Essence and equipment lifecycle; validate coverage | `DungeonRun`/`ColosseumMatchResult`/`TowerAttempt` extractors plus `EssenceSystemService`, equipment mutation boundaries, migrations for new fact kinds. Use existing domain IDs. Add small APIs only for read only aggregate reporting. |
| 3: feature / P1–P2 | Prophecy offer denominator, style selection history, guild eligibility and participation, complete resource flow coverage | `ProphecyService`, `CombatStyleService`, guild commands/services, economy writers. New offer/economy tables and reconciliation job. Avoid UI events until an observed discovery question needs them. |
| 4: reporting / P1 | Aggregate views, cohort definitions, dashboard or restricted BI connection, automated data quality checks | Scheduled job in existing worker host; read only reporting role or `API.LiveOps` aggregate endpoints after authorization review. `LL/src/Presentation/dashboard` only if BI fails to meet the decision workflow. |
| 5: advanced / P3 | Pair/trio lift, matched build comparisons, anomaly flags, selective sampling, warehouse | Add only with player volume and query cost evidence. A new analytical store is a deployment project, not a prerequisite. |

P0 is deliberately small: historical build and outcome are expensive to reconstruct. P1 includes acquisition lifecycle and resource flow once authoritative boundaries are mapped. P2 includes offer/open diagnostics, guild contribution and detailed content utilization. P3 includes per ability attribution beyond the current engine summary, causal experiments, session instrumentation, and a separate warehouse. Explicitly reject blanket page views, all ticks/targets, all pair/trio events, every outbox payload, and Stronghold placeholders.

### Concrete codebase changes by layer

* **Core Application:** add `IAnalyticsFactWriter` and typed immutable contract records under a new analytics namespace; expose no EF types. A fact writer should accept a stable source operation and resolved build, not ask the current character state after the result. Keep domain rules in Core, and avoid making analytics failure alter the result calculation.
* **Infrastructure Service:** create projections at `IdleCombatOutcomeProcessor`/dungeon outcome and `CombatEncounterResultFactory` boundaries, then adapters at `ColosseumService` and Tower finalization. Use `CombatResult.EntityStats`, `CompactTelemetry`, `EngineOutcome` and `ContentOutcome` deliberately. Capture reward source and amount after settlement, not the predicted roll.
* **Infrastructure Persistence:** add EF entities/configurations to `LLDbContext`, idempotent unique keys, indexes, monthly partition plan if volume calls for it, and a migration. Add backfill/extractor code from existing run/match/ledger rows with a `quality` flag. Migrations must be reviewed and deployed in order; do not apply to shared or production DB as part of this design.
* **API and worker:** register writer/services through existing DI, add a single daily aggregation job to the existing worker host, and expose aggregate read only reports via LiveOps or BI. No SignalR analytics feed is needed. If the existing outbox is used for external delivery, add an explicit analytics consumer registration and its own status/lag; do not modify existing realtime contracts to carry large facts.
* **Frontend:** no change for P0. Later, a narrow feature exposure event can be sent when the server cannot know that a feature was actually shown; protect it from ad blockers, retries and double render, and never use it as authoritative outcome.
* **Config/deployment:** toggle analytics fact capture by mode, define retention and job batch size, provision reporting access, add monitors for lag/reconciliation. No external service or deployment is required for the first phase, but production use requires a schema migration and retention process.

## 13. Testing and historical integrity

Test typed contract serialization/deserialization across V1/V2 and replay of older payloads. Integration tests should run the successful command twice with the same operation ID and assert one fact, then roll back a transaction and assert zero facts. Test idle catch up with multiple batches, empty due work, partial failure and a resumed resolution; reconcile `aggregation_count` to settled encounter counts and rewards. Test dungeon retreat, unclaimed reward, PvP attacker/defender, and Tower simulation then finalization once. Verify a build captured before a change remains unchanged after the current loadout changes. Test source row deletion/retention expectations. Compare ledger flows with before/after balances by currency and fail data quality checks on unexplained drift, without failing player commands. Synthetic local/staging facts should be isolated from production cohorts, with test accounts excluded by an explicit flag rather than name guesses.

Operational checks: event schema version coverage, duplicate fact ratio, late event age, missing build reference, raw to aggregate reconciliation, job high water mark, aggregate freshness, suppressed small cells, and privacy deletion verification. A developer diagnostics view should show one action's trace ID, fact ID, source ID and redacted contract; this is for verification, not an unrestricted player tracking page.

## 14. Risks and trade offs

* **Overcollection:** per tick/activation/target events would multiply idle combat volume and obscure decisions. Begin with `AbilityStats` and sampled investigations.
* **Misleading comparisons:** highly progressed players choose different Essences and content. Stratification and within character analyses help but do not establish causality.
* **Outbox misuse:** existing payloads target quests/realtime and are deleted; registering a consumer does not create a permanent history or enrich older messages. Purpose built durable facts are required for historical build/result questions.
* **Database load:** analytical scans of `LLDbContext` tables can compete with gameplay. Use bounded reporting tables, indexes, daily jobs and read only connections; measure before introducing another database.
* **Sparse beta data:** rare trios and floor slices can make confident looking but unstable charts. Display counts, suppress tiny segments and favor broader windows.
* **Version ambiguity:** content can change in JSON catalogs and code. Capture a single release/balance manifest with facts and preserve relevant numeric build values.
* **Privacy:** granular sequences can reidentify players in a small game. Restrict detail, aggregate early and implement deletion before production retention grows.
* **Historical gaps:** current state cannot recover earlier equip/selection or the precise build used in past idle fights. Backfills must carry an `unknown` flag rather than pretend exactness.

## 15. What I Would Build First

**Milestone:** one trustworthy combat balance report for idle areas plus committed dungeon/PvP records, with the ability to join the build used at the time. Keep the first implementation to one or two sprints of backend work, then review data quality before adding lifecycle events.

1. **First contracts:** `CombatFactV1` and `BuildSnapshotV1`. `CombatFactV1` has one stable source key, mode/area/encounter, occurred time, outcome counts, duration, end health, compact team totals, reward totals, build ID and version. Its idle `aggregation_count` is explicit. Record a small `ProgressionMilestoneV1` only for first combat/first area clear if source data cannot derive it.
2. **First tables:** `analytics_build_snapshot`, `analytics_build_essence`, `analytics_build_equipment`, `analytics_combat_fact`, `analytics_daily_encounter`. Avoid a generic event blob table in milestone one. Use uniqueness on source operation and fact grain.
3. **First service:** `IAnalyticsFactWriter` in Core Application with a `Persistence.LL` implementation that adds the fact and snapshot in the same transaction. Add a `BuildSnapshotProjector` in Services.LL based on the committed combat preparation or existing `CharacterSnapshot`. Use deterministic fingerprint/ID reuse within a character and version. Make a failed analytics write visible; if a write must be nonblocking, write only a tiny transactional outbox notice and persist the fact after commit with replay, never silently drop it.
4. **First integrations:** idle settlement at `IdleCombatOutcomeProcessor` (aggregate ordinary offline encounters, individualize failures/first clear), then derive dungeon and Colosseum facts from `DungeonRun`/`ColosseumMatchResult` without duplicating existing result JSON. Capture source IDs and confirm transaction boundaries. Tower follows when these reconcile.
5. **First job and dashboard:** daily `encounter × mode × area × balance version × power band` wins/attempts/unique characters, P50 duration and failure repeats. The first page is a ranked area difficulty table with adjacent area comparison and counts, plus a drilldown to aggregate Essence/Style/equipment composition for the selected area. Refresh daily.
6. **First tests:** duplicate command/retry, rollback, multi batch idle catch up, build before/after switch, and aggregate reconciliation with source settled counts. Use the repository's backend test entry point `build/run-tests.ps1` when implementing code.

Illustrative queries against the **proposed** schema (column names are design examples, not current database objects):

```sql
-- Area difficulty: compare players and attempts within one balance version.
SELECT encounter_key, sum(wins) AS wins, sum(attempts) AS attempts,
       count(DISTINCT character_id) AS characters,
       sum(wins)::numeric / nullif(sum(attempts), 0) AS win_rate
FROM analytics_combat_fact
WHERE mode = 'idle' AND balance_version = :version
  AND occurred_at_utc >= :from_utc AND occurred_at_utc < :to_utc
GROUP BY encounter_key
HAVING count(DISTINCT character_id) >= 20
ORDER BY win_rate, attempts DESC;

-- Essence use among combat participants, with actual eligible ownership
-- denominator supplied by an ownership/slot eligibility view at the same date.
SELECT e.essence_definition_id, count(DISTINCT f.character_id) AS users,
       sum(f.attempts) AS represented_attempts
FROM analytics_combat_fact f
JOIN analytics_build_essence e ON e.snapshot_id = f.build_snapshot_id
WHERE f.mode = :mode AND f.balance_version = :version
GROUP BY e.essence_definition_id;

-- Repeated failure followed by a changed build and a win.
WITH ordered AS (
  SELECT character_id, encounter_key, occurred_at_utc, build_snapshot_id, wins,
         lag(build_snapshot_id) OVER
           (PARTITION BY character_id, encounter_key ORDER BY occurred_at_utc)
           AS prior_build,
         lag(wins) OVER
           (PARTITION BY character_id, encounter_key ORDER BY occurred_at_utc)
           AS prior_wins
  FROM analytics_combat_fact
  WHERE aggregation_count = 1
)
SELECT encounter_key, count(*) AS changed_after_loss_then_won
FROM ordered
WHERE prior_wins = 0 AND wins = 1 AND prior_build <> build_snapshot_id
GROUP BY encounter_key;
```

The SQL assumes `attempts`/`wins` are explicit counts, even for an individual fight. The sequence query deliberately excludes aggregated idle intervals: those cannot establish exact order. This limitation is a reason to retain individual failures and immediate subsequent encounters, not a reason to log every idle tick.

**Stop and evaluate after this milestone.** If the first report changes no tuning or design decision, fix the question/metric before expanding the event surface. If it reveals real walls, add the next smallest source: Essence/item lifecycle facts and prophecy offer denominators. That keeps telemetry tied to decisions rather than to a wish list.
