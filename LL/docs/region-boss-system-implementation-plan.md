# Region Boss System: Architecture and Implementation Plan

## Executive Summary

The Region Boss system fits the current architecture well as a scheduled multiplayer auto-combat event. It should be a new bounded context that reuses combat, power ratings, playback, Quartz, rewards, the game-event outbox, and SignalR infrastructure without being embedded into Raids or Tournament Grounds.

The recommended implementation is a scheduled event with a 10-minute signup window. When signup closes, registered players are divided into broad power bands and deterministically randomized into parties of up to five. Each party receives an independent, server-simulated encounter against an endlessly scaling version of the same Region Boss. The complete encounter is resolved once and delivered as compact playback rather than as a live combat command stream.

The implementation requires targeted, default-disabled extensions to the shared combat engine for:

- Lazy hostile-wave generation.
- Downed players and delayed revival.
- Recovery between waves.
- Hostile-only, time-based Regional Fury.
- Playback metadata for Boss Level, Fury, Downed state, and revival timers.

It should not introduce milestone mechanics, role-based matchmaking, per-level database rows, live 10 Hz SignalR traffic, or a Region-Boss-specific copy of the combat engine.

## Existing Systems That Can Be Reused

### Raids

Raids provide the closest reusable multiplayer-combat infrastructure. Existing Raid code already demonstrates:

- Character and account uniqueness within an event.
- Current-character combat hydration.
- Power-rating capture.
- Party assignment.
- Deterministic combat seeds.
- Multiple friendly combatants in one encounter.
- Simulation leases and retry-safe resolution.
- Compact playback artifacts.
- Participant combat statistics.
- Claimable reward entitlements.
- Transaction-scoped PostgreSQL advisory locks.
- Definition snapshots and hashes for in-progress content stability.

Relevant implementations include:

- `LL/src/Core/Domain/Models/Raids/RaidModels.cs`
- `LL/src/Infrastructure/Service/Services.LL/Raids/RaidService.cs`
- `LL/src/Infrastructure/Service/Services.LL/Raids/RaidCombatResolver.cs`
- `LL/src/Infrastructure/Service/Services.LL/Raids/RaidPlaybackBundleBuilder.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Raids/RaidConfigurations.cs`

The Region Boss domain should reuse these patterns, but it should not be implemented as a Raid tier or placed inside `RaidService`. Raid orchestration contains raid-specific lanes, preparation encounters, contribution scoring, reward rules, and mechanical difficulty milestones that do not fit Region Bosses.

### Tournament Grounds

Tournament Grounds provides the closest scheduled lifecycle:

1. Ensure an upcoming occurrence exists.
2. Open registration at a configured time.
3. Close registration.
4. Generate groups or a bracket.
5. Resolve scheduled combat.
6. Expose playback.
7. Finalize standings and rewards.

Its transaction boundaries, schedule-level lock, per-event advisory locks, frozen combat snapshots, and Quartz-owned progression are directly applicable.

Relevant implementations include:

- `LL/src/Core/Domain/Models/Colosseum/Tournaments/TournamentInstance.cs`
- `LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/TournamentGroundsService.cs`
- `LL/src/Worker/Worker.LL/BackgroundJobs/TournamentGroundsProgressionJob.cs`
- `LL/docs/tournament-grounds-quartz-integration.md`

### World Tower

World Tower provides:

- A five-character maximum party convention.
- Party-slot validation.
- Multi-character combat.
- Leased simulation and playback processing.
- Participant-count boss scaling.
- Worker-safe retry behavior.

Its participant-count scaling is particularly useful when the signup count cannot be divided into exact groups of five.

Relevant implementations include:

- `LL/src/Core/Domain/Models/WorldTower/WorldTowerPartyRules.cs`
- `LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerGuardianScaling.cs`
- `LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs`

### Combat Engine

Combat is automatic, server-simulated at 10 ticks per second, and optionally exposed through compact playback checkpoints. The engine already supports:

- Multiple friendly and hostile combatants.
- Threat and organic tanking behavior.
- Healing, barriers, regeneration, mitigation, and damage redirection.
- Ability cooldowns, effects, statuses, summons, and stagger.
- Reinforcement waves without resetting friendly runtime state.
- A hard tick budget.
- Detailed aggregate combat statistics.

Relevant implementations include:

- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatEngineExecutor.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Resolution/Models/CombatEncounterRuntime.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Stats/CombatStatsAggregator.cs`

### Power Ratings and Current Character Builds

Region Boss combat should load each player's current equipment, attributes, and equipped Essences when the run is resolved. No character snapshots are created for signup. The existing power-rating service is suitable for broad matchmaking bands, and ratings are refreshed when signup closes before parties are formed.

Power rating should be used only to prevent extreme mismatches. It should not become a strict matchmaking rating or a source of rewards.

Relevant implementations include:

- `LL/src/Infrastructure/Persistence/Persistence.LL/QueryProfiles/EntityQueryProfiles.cs`
- `LL/src/Core/Application/Interfaces/Services/LL/PowerRatings/IPowerRatingService.cs`
- `LL/src/Infrastructure/Service/Services.LL/PowerRatings/CombatRatingCalculator.cs`

### Rewards

The generic reward-table system can author Region Boss reward content. It supports currencies, experience, items, nested reward tables, weighted drops, and deterministic testability through injected randomness.

The roller does not persist results, so Region Boss settlement should roll rewards once and snapshot the results in a claimable entitlement. This prevents later reward-table edits from changing rewards that players have already earned.

Relevant implementations include:

- `LL/src/Core/Domain/Models/Rewards/RewardTableDefinition.cs`
- `LL/src/Infrastructure/Service/Services.LL/Rewards/RewardRoller.cs`
- `LL/src/API/API.LL/Data/rewards/reward-tables.json`
- `LL/docs/reward-table-system-spec.md`

### Quartz, Outbox, and SignalR

`Worker.LL` is already the canonical host for durable global schedules. The game-event outbox allows a worker transaction to enqueue realtime delivery that the API later publishes through SignalR.

Relevant implementations include:

- `LL/src/Worker/Worker.LL/BackgroundJobs/BackgroundJobRegistrationExtensions.cs`
- `LL/src/Infrastructure/Persistence/Persistence.LL/BackgroundJobs/BackgroundJobExecutionService.cs`
- `LL/src/Infrastructure/Service/Services.LL/Outbox/OutboxGameRealtimeBroadcaster.cs`
- `LL/src/Infrastructure/RealTime/RealTime.LL/GameHub.cs`
- `LL/src/API/API.LL/HostedServices/GameEventOutboxWorker.cs`

## Existing Gaps and Conflicts

### No Revival Support

Dead combatants remain dead. The combat loop skips dead entities and ends when the friendly team has no living combatants. Region Boss revival therefore requires an explicit, optional engine rule.

### Reinforcement Waves Are Eager

Existing reinforcement waves are fully constructed before combat starts. An endless encounter should not materialize hundreds or thousands of boss instances in advance. The runtime needs a lazy next-wave provider.

### Existing Overtime Is Not Boss-Only

The current overtime multiplier is applied by `GetEffectivePower` without checking team membership. As a result, it can increase player power as well as hostile power. Regional Fury needs a hostile-only timed attribute ramp.

### Raid Plus-Level Scaling Changes Mechanics

`RaidPlusDifficulty.Create` must not be reused. It introduces milestone ranks that change add counts, survival thresholds, overtime timing, and overtime strength. That directly conflicts with the requirement that Region Boss mechanics remain consistent at every level.

The low-level attribute-modifier technique used by `RaidCombatScaling` can be extracted or reused, but Region Boss curves must be separate and purely numerical.

### Playback Schema Is Insufficient

Current Raid playback treats entity maximum health as static metadata. A Region Boss reuses one identity while maximum health changes every level. Region Boss playback frames need their own schema containing:

- Current Boss Level.
- Current and maximum boss health.
- Regional Fury stacks.
- Downed state.
- Revival time remaining.
- Death count.
- Level-transition recovery events.

### Region Progression Is Indirect

There is no authoritative `Character.CurrentRegion` property. Region access is expressed through level requirements, completed quests, and World Tower requirements on content.

Region Boss eligibility should use explicit definition fields rather than adding a new current-region property solely for this feature.

## Proposed Domain Model

### RegionBossEvent

One row represents one scheduled occurrence.

Recommended fields:

- `Id`
- `RegionBossDefinitionId`
- `RegionId`
- `Status`
- `SignupStartsAtUtc`
- `SignupClosesAtUtc`
- `EncounterStartsAtUtc`
- `PlaybackStartsAtUtc`
- `PlaybackEndsAtUtc`
- `CompletedAtUtc`
- `CancelledAtUtc`
- `CancellationReason`
- `DefinitionHash`
- `DefinitionSnapshotJson`
- `MatchmakingAlgorithmVersion`
- `CombatRulesVersion`
- `RowVersion`
- `CreatedAtUtc`
- `UpdatedAtUtc`

### RegionBossSignup

One row represents one registered account and character.

Recommended fields:

- `Id`
- `RegionBossEventId`
- `CharacterId`
- `AccountId`
- `CharacterName`
- `CharacterSnapshotId`
- `LoadoutHash`
- `PowerRating`
- `PowerRatingAlgorithmVersion`
- `BuildFingerprint`
- `RegionBossRunId`, nullable until matching
- `PartySlot`, nullable until matching
- `SignedUpAtUtc`
- `SnapshotRefreshedAtUtc`

Required uniqueness:

- `(RegionBossEventId, CharacterId)`
- `(RegionBossEventId, AccountId)`
- `(RegionBossRunId, PartySlot)` when assigned

### RegionBossRun

One row represents one matched party and its encounter instance. A separate permanent party entity is unnecessary.

Recommended fields:

- `Id`
- `RegionBossEventId`
- `PartyNumber`
- `PartySize`
- `MatchmakingBand`
- `PartySizeScalingVersion`
- `RandomSeed`
- `Status`
- `StartedAtUtc`
- `ResolvedAtUtc`
- `PlaybackStartsAtUtc`
- `PlaybackEndsAtUtc`
- `HighestLevelDefeated`
- `CurrentBossLevel`
- `CurrentBossMaxHealth`
- `CurrentBossHealthRemaining`
- `CurrentBossProgressBasisPoints`
- `DurationTicks`
- `FuryStacksAtEnd`
- `TerminationReason`
- `SimulationLeaseOwner`
- `SimulationLeaseUntil`
- `SimulationAttempts`
- `RowVersion`

Required uniqueness:

- `(RegionBossEventId, PartyNumber)`

### RegionBossParticipantResult

One row per run participant.

Recommended fields:

- `RegionBossRunId`
- `CharacterId`
- `DamageDone`
- `DamageTaken`
- `HealingDone`
- `HealingReceived`
- `BarrierGenerated`
- `DamagePrevented`
- `ThreatGenerated`
- `Deaths`
- `Revivals`
- `DownedTicks`

There should be no contribution rank or contribution-based reward field.

### RegionBossPlayback and RegionBossPlaybackArtifact

Follow the Raid playback/artifact split:

- Metadata and bundle hash in `RegionBossPlayback`.
- Brotli-compressed bytes in `RegionBossPlaybackArtifact`.
- One playback per `RegionBossRun`.

### RegionBossRewardGrant

One row represents one claimable, already-resolved entitlement.

Recommended fields:

- `Id`
- `RegionBossEventId`
- `RegionBossRunId`
- `RegionBossDefinitionId`
- `CharacterId`
- `RewardKey`
- `RewardKind`
- `MilestoneLevel`
- `RewardSnapshotJson`
- `Status`
- `CreatedAtUtc`
- `ClaimedAtUtc`

Required uniqueness:

- `(RegionBossEventId, CharacterId, RewardKey)`

## Definition and Content Model

Create a validated catalog such as:

```text
LL/src/API/API.LL/Data/region-bosses/region-bosses.json
```

Each definition should contain:

- Stable boss ID and display name.
- Region ID.
- Creature ID and fixed combat identity.
- Image and presentation metadata.
- Eligibility requirements.
- Level-one attribute multipliers.
- Pure numerical level-scaling curves.
- Regional Fury configuration.
- Downed and revival configuration.
- Between-level recovery configuration.
- Party-size scaling configuration.
- Reward milestone references.
- Recurrence schedule.
- Definition and rules version.

The catalog must be validated during API and Worker startup. Each event must snapshot and hash its effective definition so a deployment cannot alter an event that is already open or in progress.

## Event Lifecycle

Recommended statuses:

1. `Scheduled`
2. `SignupOpen`
3. `Matching`
4. `Resolving`
5. `Playback`
6. `Settled`
7. `Cancelled`

### Scheduled

Quartz ensures that an upcoming occurrence exists for every enabled Region Boss. Creation uses a schedule-level advisory lock and a unique `(RegionBossDefinitionId, SignupStartsAtUtc)` index.

### SignupOpen

At `SignupStartsAtUtc`, the lifecycle job opens signup. Players can:

- Sign up.
- Withdraw.
- Refresh their frozen loadout.

### Matching

At `SignupClosesAtUtc`, the event is locked and signups are frozen. Eligibility is rechecked, invalid signups are removed or marked ineligible, and all remaining players are assigned to runs in one transaction.

### Resolving

Each party run is claimed with a simulation lease. Combat is simulated outside the database transaction. The result and playback artifact are committed only if the same worker still owns the lease.

### Playback

After all runnable parties have playback artifacts, the event receives a common playback start and end time. Clients download their party's playback bundle and advance it locally.

### Settled

After playback ends:

- Results become authoritative and visible.
- The leaderboard is finalized.
- Reward grants become claimable.
- History and personal-best comparisons include the event.

### Cancelled

Cancellation should record a stable reason, such as:

- Insufficient eligible signups.
- Invalid or missing boss definition.
- Administrative cancellation.

## Signup and Eligibility

Signup should follow the Raid and Tournament patterns:

- One account per event.
- Use the current character combat setup when the run is resolved.
- Capture and refresh the display power rating and algorithm version before matchmaking.
- Permit withdrawal only while signup is open.
- Re-check eligibility when signup closes.

Recommended eligibility fields in the Region Boss definition:

- `LevelRequirement`
- `RequiredCompletedQuestId`
- `RequiredTowerFloor`

Do not introduce a `Character.CurrentRegion` property. Current region progression is already represented through content gates.

## Party Formation

### Goals

- Parties should feel random.
- Extreme power mismatches should be avoided.
- No tank, healer, or DPS role requirements should be imposed.
- Matching must be deterministic under retry.

### Recommended Algorithm

1. Sort eligible signups by captured power rating.
2. Form broad contiguous bands where the highest rating is no more than approximately 1.6 to 1.75 times the lowest rating.
3. Merge undersized bands with the closest adjacent band.
4. Deterministically shuffle each band using the event ID and matchmaking algorithm version.
5. Partition toward parties of five.
6. Rebalance remainders so parties contain three to five players where possible.

Examples:

- 6 players become `3 + 3`.
- 7 players become `4 + 3`.
- 8 players become `4 + 4`.
- 9 players become `5 + 4`.
- 11 players become `4 + 4 + 3`.

Exact groups of five are impossible for arbitrary signup counts. Randomly excluding unmatched players would be especially harmful when rewards are based on group progression. Five should therefore be the maximum party size, with three as the recommended minimum.

If fewer than three eligible players register for the entire event, cancel the occurrence rather than creating a badly distorted competitive result.

### Smaller Parties

For parties of three or four, apply versioned participant-count scaling based on the World Tower approach. Primarily scale boss health; avoid large offensive reductions that would make defensive requirements meaningless.

Persist party size and the scaling version so results remain explainable.

## Encounter Creation

Each `RegionBossRun` creates one combat runtime containing:

- Three to five friendly combatants built from frozen snapshots.
- One Region Boss at Boss Level 1.
- A 6,000-tick maximum duration, corresponding to ten minutes at ten ticks per second.
- A lazy hostile-wave provider that creates the next boss level after the current boss dies.
- Optional Region Boss lifecycle rules for Fury, Downed players, revival, and between-level recovery.

Use a deterministic seed derived from the event and run IDs. If a worker crashes after simulation but before commit, retrying the run must produce the same result.

## Boss Level Scaling

Difficulty progression must remain purely numerical. Do not reuse `RaidPlusDifficulty.Create`, which changes encounter mechanics at milestones.

A reasonable initial calibration is:

```text
Max Health      = Level1Health × 1.12^(Level - 1)
Power           = Level1Power  × 1.055^(Level - 1)
Armor           = Level1Armor  × (1 + 0.03 × (Level - 1))
Resistance      = Level1Resist × (1 + 0.03 × (Level - 1))
```

Penetration can receive modest linear growth if simulation shows that defensive parties become disproportionately effective. Attack Speed should not scale with Boss Level because Regional Fury already owns the time-pressure role.

Do not change with level:

- Ability selection.
- Cooldowns.
- Targeting rules.
- Summons or add counts.
- Stagger behavior.
- Phases.
- Status mechanics.
- Visual identity.

Health should grow faster than offense so early levels are cleared quickly while later levels consume increasingly more of the fixed encounter clock. Armor and Resistance should use linear rather than exponential growth to avoid abrupt mitigation cliffs.

### Numeric Safety

There is no authored maximum Boss Level, but the combat engine ultimately exposes health through integer values. Scaling should:

- Calculate using `double` or log-space intermediates.
- Use checked conversions.
- Validate that realistically reachable ten-minute values remain combat-safe.
- Avoid silently wrapping or producing non-finite values.

The fixed encounter duration creates a practical reachable limit without imposing a game-design maximum.

## Regional Fury

Regional Fury should be derived from elapsed encounter ticks and should persist across Boss Levels.

Recommended initial rules:

- Gain one stack every 600 ticks, or 60 seconds.
- Apply Fury only to the hostile Region Boss.
- Increase Power by 6% per stack.
- Increase Attack Speed by 4 percentage points per stack.
- Do not increase health, Armor, or Resistance.

Because the encounter ends at 600 seconds, nine stacks meaningfully affect combat before expiration. During the final minute, the boss has approximately:

- 54% increased Power.
- 36 percentage points of additional Attack Speed.

This roughly doubles late basic-attack pressure while increasing ability damage more moderately. It discourages indefinite defensive stalling without immediately invalidating tanks, healers, barriers, and mitigation.

Fury should not be implemented with the current generic overtime multiplier because that multiplier is not team-scoped. Add a default-disabled, team-scoped timed attribute ramp to the shared combat engine.

## Downed and Revival Handling

### Downed State

When a player reaches zero health:

- Record a death.
- Mark the player Downed.
- Prevent actions and normal targeting.
- Schedule revival if at least one party member remains alive.

If all party members are Downed simultaneously, terminate the run immediately.

### Revival Delay

Recommended initial curve:

```text
First death:       15 seconds
Each later death: +10 seconds
Maximum delay:     60 seconds
```

This is intentionally simple and understandable. Repeated deaths become expensive without permanently removing a player from the event.

### Revival State

On revival:

- Restore 25% maximum health.
- Clear barriers and cover relationships.
- Clear boss-owned harmful timed effects.
- Preserve the accumulated death count.
- Do not restore summons lost on death.

### Between-Level Recovery

When a Boss Level is defeated:

- Heal living players for 20% maximum health.
- Immediately revive Downed players at 25% maximum health.
- Preserve accumulated death counts.
- Reset boss threat and stagger state.
- Preserve player cooldown progression and player-owned effects where technically valid.

This creates useful interplay between damage, healing, defense, threat management, and keeping at least one party member alive.

### Combat-Engine Implementation

Implement revival as an optional generic encounter rule in `FastCombatEngine`, disabled by default. Existing Idle, Dungeon, Raid, World Tower, and PvP behavior must remain unchanged unless the option is explicitly enabled.

Do not copy `FastCombatEngine` into a Region Boss-specific engine. A duplicated engine would quickly diverge from shared ability, threat, status, and balance fixes.

## Endless Progression Within One Combat Runtime

Each Boss Level should behave as a hostile reinforcement wave. Reinforcement waves already preserve friendly runtime state, including health, cooldowns, statuses, and combat statistics.

Add a lazy hostile-wave provider:

1. Boss Level 1 is supplied as the initial hostile wave.
2. When the boss dies, request Level 2 from the provider.
3. Clone the prepared base boss.
4. Apply the numerical curve for the requested level.
5. Spawn it with the same abilities and mechanics.
6. Apply between-level recovery.
7. Emit a transition checkpoint.
8. Continue until party defeat or the 6,000-tick budget expires.

This is preferable to invoking a fresh combat simulation for every level. Separate simulations would reset cooldowns, statuses, threat, and ability runtime, allowing transition behavior to distort the outcome.

## Run Termination

Recommended termination reasons:

- `PartyDefeated`
- `TimeExpired`
- `Cancelled`
- `SimulationError`

A normal run ends when:

- All party members are Downed simultaneously, or
- The encounter reaches 6,000 ticks.

The run should not terminate merely because one or more members are Downed while another member remains alive.

## Scoring and Progress Recording

Persist the party result as:

- Highest Boss Level defeated.
- Current Boss Level.
- Current boss maximum health.
- Current boss remaining health.
- Progress basis points against the current level.
- Duration ticks.
- Fury stacks at termination.
- Termination reason.

Recommended leaderboard order:

1. `HighestLevelDefeated`, descending.
2. `CurrentBossProgressBasisPoints`, descending.
3. `DurationTicks`, descending, only as a final natural tiebreaker.
4. `RunId`, for deterministic ordering.

Example presentation:

```text
Level 17 defeated
Level 18 — 42.00% completed
```

Individual damage, healing, mitigation, and deaths are post-combat statistics only. They must not contribute to primary score or reward eligibility.

Personal improvement can initially be derived from indexed run history. Do not create a personal-best table until query volume demonstrates a need for one.

## Reward Distribution

Reward content should be data-driven through milestone brackets:

```text
minimumLevelDefeated → rewardTableId
```

At settlement:

1. Determine the highest qualifying progression bracket, or explicitly configured cumulative brackets.
2. Roll the referenced reward table once.
3. Persist the complete reward result in `RegionBossRewardGrant`.
4. Allow the player to claim it later.
5. Apply the grant under a character advisory lock and an idempotent unique key.

Every member of the same party receives the same progression bracket. Individual combat statistics must not alter the package.

Leaderboard rewards should be omitted from the first release. If introduced later, represent them as a separate, smaller `LeaderboardBonus` grant created only after every run is terminal. This keeps random matchmaking from dominating the primary reward economy.

## API Surface

Suggested endpoints:

```text
GET    /region-bosses/region/{regionId}/current
GET    /region-bosses/{eventId}
POST   /region-bosses/{eventId}/signup
DELETE /region-bosses/{eventId}/signup
POST   /region-bosses/{eventId}/refresh-loadout
GET    /region-bosses/{eventId}/run
GET    /region-bosses/runs/{runId}/playback
GET    /region-bosses/{eventId}/leaderboard
GET    /region-bosses/history
POST   /region-bosses/{eventId}/rewards/claim
```

Playback and run-detail endpoints must verify that the authenticated character is assigned to the requested run. Public event state and finalized leaderboard results can be available to all authenticated players.

## Realtime Updates

Add contracts such as:

- `RegionBossEventUpdated`
- `RegionBossPartyUpdated`

Use audiences as follows:

- Public event phase and signup-count changes: existing World audience.
- Party assignment, playback readiness, and personal reward changes: existing Characters audience targeting the assigned participants.

A new SignalR group is unnecessary initially. Authenticated connections already join their character group, and public event updates can reuse the World group.

Do not stream combat frames through SignalR. SignalR should act as a refresh or invalidation signal. REST remains authoritative, and Angular should download one compact playback bundle and advance it locally.

## Frontend Work

Add a Region Boss card to the Region page and a dedicated Region Boss feature page containing:

- Event status.
- Signup and encounter countdowns.
- Eligibility status.
- Sign up, withdraw, and refresh-loadout actions.
- Captured power rating and snapshot time.
- Assigned party members.
- Playback availability.
- Boss Level and current-level progress.
- Regional Fury indicator.
- Downed and revival countdown presentation.
- Party combat statistics.
- Final result and leaderboard.
- Reward claim state.
- Previous-best comparison.

Raid playback components provide a useful structural reference, but Region Boss playback needs a dedicated schema because maximum boss health, Boss Level, Fury, and revival state change during the run.

## Persistence Requirements

The implementation requires an EF Core migration containing:

- Region Boss event table.
- Signup table.
- Run table.
- Participant result table.
- Playback metadata and artifact tables.
- Reward grant table.
- Foreign keys and delete behavior.
- Unique signup, party-slot, run-number, playback, and reward indexes.
- Status and due-time indexes for worker polling.
- Row-version concurrency tokens.

Recommended due-work indexes include:

- `(Status, SignupStartsAtUtc)` on events.
- `(Status, SignupClosesAtUtc)` on events.
- `(Status, PlaybackEndsAtUtc)` on events.
- `(Status, SimulationLeaseUntil)` on runs.
- `(RegionBossEventId, HighestLevelDefeated, CurrentBossProgressBasisPoints)` on runs.
- `(CharacterId, Status)` on reward grants.

Do not persist per-tick or per-level rows. Playback bytes hold the detailed visual history; result tables hold queryable summaries.

## Background and Scheduled Processing

Use two Quartz jobs.

### Region Boss Lifecycle Job

Responsibilities:

- Ensure upcoming occurrences exist.
- Open due signup windows.
- Close signup and form parties.
- Transition to playback when all runs are ready.
- Settle events after playback ends.

The job should use `IBackgroundJobExecutionService` with a deterministic business key and reconcile persisted state rather than assuming every scheduled fire occurred.

### Region Boss Resolution Job

Responsibilities:

- Find queued or expired-lease runs.
- Claim a bounded batch.
- Simulate runs outside transactions.
- Build playback artifacts.
- Commit results if the lease is still owned.
- Release or expire leases after cancellation or failure.

Start with bounded sequential processing suitable for the current deployment. The lease design leaves room for later partitioning or additional workers without changing the domain model.

## Concurrency and Transactional Concerns

### Schedule Creation

- Acquire a schedule-level advisory lock.
- Enforce a unique occurrence key.
- Treat the unique index as the final race-condition backstop.

### Signup and Withdrawal

- Acquire the event advisory lock.
- Validate the event status and current time inside the transaction.
- Enforce character and account uniqueness with database indexes.
- Do not rely only on an in-memory signup count.

### Matchmaking

- Lock the event.
- Freeze and revalidate signups.
- Generate all assignments deterministically.
- Insert runs and assignments in one transaction.
- Store the matchmaking algorithm version.

### Simulation

- Claim a run under a short transaction.
- Simulate outside the transaction.
- Re-lock the run before writing results.
- Verify status and lease ownership.
- Use a deterministic combat seed so retrying is safe.

### Rewards

- Create reward grants idempotently.
- Lock the character while applying a claim.
- Use a unique reward key.
- Write inventory, currencies, economy ledger entries, claim status, state invalidation, and outbox delivery in the same transaction.

### Event Finalization

- Finalize only when every run is terminal.
- Keep simulation errors visible rather than silently omitting a party.
- Permit administrative retry of errored runs.
- Define an operational fallback if a run exhausts its retry budget.

## Important Design Decisions

### Region Boss Is a New Bounded Context

It should reuse Raid, Tournament, World Tower, combat, and reward infrastructure but own its domain states and rules. Adding Region Boss behavior to `RaidService` would further enlarge an already substantial service and would entangle incompatible reward and encounter concepts.

### Snapshot-Based Auto-Combat Is the Correct Fit

The current game does not have a live multiplayer combat command stream. A precomputed simulation plus synchronized playback fits existing behavior and avoids building a new realtime game server.

### One Continuous Combat Runtime

Boss Levels should be reinforcement waves inside one runtime. This preserves player cooldowns, effects, health, statistics, Fury, and revival timing.

### Mechanics Remain Fixed

Only numerical attributes and Regional Fury change. No milestone levels, new abilities, phase changes, add-count changes, or awakened forms should be introduced.

### Progression Rewards Dominate

Reward brackets derive from the party's Boss Level progress. Personal DPS has no reward impact. Leaderboard bonuses are deferred and secondary.

### Five Is a Maximum, Not an Exclusion Rule

Arbitrary registration totals cannot always form exact groups of five. Rebalancing into parties of three to five is fairer than randomly excluding registered players.

## Unnecessary Complexity to Avoid

- Do not create one database row per Boss Level.
- Do not persist Fury or revival timers each tick.
- Do not create a permanent Region Boss social-party model.
- Do not introduce role-based matchmaking.
- Do not build a live 10 Hz SignalR combat stream.
- Do not add a separate Region Boss combat engine.
- Do not invoke a new simulation for every Boss Level.
- Do not add personal-best projection tables in the first version.
- Do not add leaderboard rewards in the first version.
- Do not create a dynamic Quartz trigger per party.
- Do not attach Region Boss logic directly to the minimal legacy `RegionService`.

## Recommended Implementation Order

1. Define Region Boss domain terminology, statuses, configuration schema, and version constants.
2. Add JSON definition models, provider, validator, and representative Region Boss content.
3. Add unit tests for Boss Level scaling, numeric safety, Fury boundaries, revival delays, and transition recovery.
4. Extend the shared combat runtime with lazy hostile waves, default-disabled Downed rules, transition recovery, and hostile-only timed ramps.
5. Run the complete existing combat regression suite before building Region Boss orchestration.
6. Implement `RegionBossCombatResolver` and the Region Boss playback schema and builder.
7. Add EF entities, configurations, indexes, advisory-lock helpers, and the migration.
8. Implement event queries, signup, withdrawal, loadout refresh, eligibility, and deterministic matchmaking.
9. Implement leased run resolution and retry behavior.
10. Implement event-wide playback readiness and settlement.
11. Implement milestone reward grants and idempotent claiming.
12. Add outbox event types, realtime contracts, state-sync scopes, and consumers.
13. Add API commands, queries, DTOs, endpoints, and authorization checks.
14. Add Angular API/state services, Region integration, signup UI, playback, results, leaderboard, and rewards.
15. Register Quartz lifecycle and resolution jobs in `Worker.LL`.
16. Add PostgreSQL concurrency and integration tests.
17. Add end-to-end lifecycle tests.
18. Calibrate curves, Fury, revival delays, and recovery through combat simulation matrices.

## Verification Strategy

### Domain and Matchmaking Tests

- Deterministic assignments for the same event and signups.
- Different event seeds produce different valid shuffles.
- Power-band boundaries and undersized-band merging.
- Remainder handling for signup counts from 3 upward.
- No duplicate character or account assignment.
- Party-slot uniqueness.

### Combat Tests

- Boss mechanics and ability IDs remain unchanged at every level.
- Health and offensive curves produce expected values.
- Fury changes at exact 600-tick boundaries.
- Fury affects hostile bosses but not players.
- A Downed player revives while an ally survives.
- The run ends immediately when all players are Downed.
- Repeated deaths extend revival time and respect the cap.
- Level victory applies the configured recovery.
- Fury and friendly cooldown state persist between levels.
- The run terminates at exactly 6,000 ticks.
- Existing Idle, Dungeon, Raid, World Tower, and PvP outcomes are unchanged when Region Boss options are disabled.

### Persistence and Concurrency Tests

- Simultaneous signup attempts cannot place two characters from one account in an event.
- Signup cannot race successfully with signup closure.
- Multiple lifecycle workers cannot create duplicate occurrences or parties.
- Only one worker owns a run lease.
- An expired lease can be reclaimed.
- A stale worker cannot overwrite the new owner's result.
- Reward settlement and claiming are idempotent.
- Outbox messages commit atomically with state changes.

### Playback Tests

- The final checkpoint is always present.
- Every frame has valid Boss Level and Fury metadata.
- Changing maximum boss health is represented correctly.
- Downed and revival state round-trips through compression.
- Bundle size limits are enforced.
- Angular playback reaches the same final score as the authoritative result.

### End-to-End Tests

- Create occurrence.
- Open signup.
- Register and refresh loadouts.
- Close signup.
- Match parties.
- Resolve every run.
- Publish playback readiness.
- Finalize leaderboard.
- Create and claim rewards.
- Process the complete flow safely after simulated worker retries.

Backend verification should use the repository-standard test runner:

```powershell
build/run-tests.ps1
```

Frontend verification must use npm from `LL/src/Presentation/ll`, with its cache placed beneath `$env:TEMP` rather than inside the checkout.

## Migration, Configuration, and Deployment Implications

### Migration

An EF Core migration is required for the Region Boss tables, indexes, foreign keys, and concurrency tokens. The migration must be generated but not applied to shared or production databases from this repository workflow.

### Configuration

Both API and Worker need the same:

- Region Boss feature flag.
- Content root.
- Schedule configuration.
- Lifecycle sweep interval.
- Resolution batch size.
- Lease duration.
- Playback timing configuration.

API and Worker must load the same validated Region Boss definitions.

### Quartz

The existing Quartz PostgreSQL schema is sufficient. No Quartz schema change should be necessary.

### Deployment

Once enabled, `Worker.LL` becomes required for Region Boss scheduling, matchmaking, resolution, and settlement. The API remains responsible for player commands, queries, playback downloads, and SignalR delivery.

Infrastructure-as-code is maintained in a separate repository and must not be modified from this repository.

## Recommended Initial Balance Defaults

These values are starting points for simulation, not final live balance:

| Mechanic | Initial value |
|---|---:|
| Signup duration | 10 minutes |
| Encounter duration | 6,000 ticks / 10 minutes |
| Maximum party size | 5 |
| Recommended minimum party size | 3 |
| Health growth per Boss Level | 12% exponential |
| Power growth per Boss Level | 5.5% exponential |
| Armor growth per Boss Level | 3% linear |
| Resistance growth per Boss Level | 3% linear |
| Fury interval | 60 seconds |
| Fury Power per stack | 6% |
| Fury Attack Speed per stack | 4 percentage points |
| First revival delay | 15 seconds |
| Additional delay per death | 10 seconds |
| Maximum revival delay | 60 seconds |
| Revival health | 25% maximum health |
| Living-player level-clear healing | 20% maximum health |
| Downed-player level-clear revival | 25% maximum health |

These values should be calibrated across offensive, defensive, sustain, control, and mixed party simulations before release.

## Final Recommendation

Build Region Bosses as a scheduled, snapshot-based multiplayer combat feature with their own domain state and data definitions. Reuse Raid snapshots, playback, leases, and rewards; Tournament scheduling and locking; World Tower party-size concepts; the shared combat engine; Quartz; and outbox-backed realtime delivery.

The only substantial shared-system work should be carefully scoped combat-engine extensions for lazy waves, Downed revival, transition recovery, and hostile-only Fury. Once those capabilities exist behind default-disabled options, the rest of the feature can follow established repository patterns without creating parallel infrastructure.
