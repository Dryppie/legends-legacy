# Idle Combat Flow and Performance Analysis

## Executive summary

The current idle-combat flow is bounded and network-efficient, but it is not CPU-efficient. After 24 hours away, the API synchronously replays approximately **8,641 real battles**.

The main optimization opportunity is to preserve deterministic combat results and the existing progression semantics while reducing:

- Combat-state allocations.
- Repeated immutable setup.
- Per-encounter reward and progression fan-out.
- Redundant persistence and realtime notifications.
- The duration of the command transaction and character lock.

The current transport is not the primary problem. A normal 24-hour backlog fits into one API request. The expensive part is exact server-side replay of every encounter and the associated reward processing.

## Implementation status (2026-08-18)

The first production-safe optimization phase is implemented. It deliberately preserves exact combat replay and the existing 1,000-encounter progression checkpoints.

Implemented:

- Added structured completion logging and `System.Diagnostics.Metrics` measurements for total resolution, batch and encounter counts, template preparation, hostile-template cache hits, simulation duration and allocations, reward calculation, progression application, and final settlement.
- Reused immutable hostile source entities and prepared hostile combat templates across internal batches. Player entities and friendly templates are still rebuilt at each checkpoint so level and essence progression affect later battles exactly as before.
- Removed non-final per-entity combat statistics as soon as each result is created. Outcome and post-combat health remain available for rewards, achievements, and prophecy progress.
- Split progression-affecting work from deferrable settlement. Experience and guild mission contributions remain checkpoint-local; inventory, currency, creature archive, prophecy, and the aggregate idle-combat outbox event are flushed once on the final internal batch.
- Stored only compact settlement data between internal batches rather than retaining complete encounter facts and combat results.
- Added ordered grouped essence-drop processing so shared factors and modifiers are resolved once per internal batch while random-roll and resonance order remain unchanged.
- Added batched creature-archive persistence that uses one lookup/save operation and preserves the earliest and latest checkpoint timestamps.
- Added regression coverage for hostile-template reuse, progression-sensitive player-template rebuilding, final-only settlement, aggregate outbox semantics, and archive timestamps.

Still measurement-gated:

- A repeatable 1-hour/8-hour/24-hour performance harness and production-like before/after measurements.
- True within-batch streaming, which would reduce the remaining peak retention of up to 1,000 encounter records but requires a wider orchestration contract change.
- Combat runtime/deep-clone pooling, bounded parallelism, time-budgeted continuation, and global catch-up admission control.
- Approximate offline settlement. This remains an explicit game-design choice and is not part of the exact-semantics implementation.

## Current flow

1. The client calls `POST CharacterActions/Resolve`.
2. `CharacterActionService` first loads the action schedule and then loads the complete combat action, including its area, creatures, and gathering nodes.
3. `IdleCombatPlanner` converts elapsed time into encounters:
   - One encounter every 10 seconds.
   - At most 24 hours of offline progress.
   - An inclusive due boundary, producing `1 + elapsed / cadence` encounters.
4. `CombatService` divides the work into internal orchestration batches.
5. For every internal batch, the system:
   - Loads the player and all possible creatures for the area.
   - Builds and prepares friendly and hostile combat templates.
   - Generates deterministic encounter identities, creature selections, and random seeds.
   - Sequentially simulates every encounter.
   - Calculates loot, experience, currencies, essence drops, gathering rewards, and sigil drops.
   - Applies character, inventory, profession, guild, creature archive, prophecy, and outbox changes.
6. All internal batches run inside one command transaction and one per-character command lock.
7. `CombatSessionAccumulator` combines the batches into one response. It returns totals for the complete interval while retaining playback data only for the final encounter.

### Relevant implementation

- API endpoint: `LL/src/API/API.LL/Controllers/V1/CharacterActionsController.cs`
- Action resolution: `LL/src/Infrastructure/Service/Services.LL/CharacterActions/CharacterActionService.cs`
- Internal batching: `LL/src/Infrastructure/Service/Services.LL/CharacterActions/CombatService.cs`
- Encounter planning: `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Idle/IdleCombatPlanner.cs`
- Encounter orchestration: `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Idle/IdleCombatOrchestrator.cs`
- Template preparation: `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Resolution/Idle/IdleCombatResolutionSessionFactory.cs`
- Encounter resolution: `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Resolution/Idle/IdleCombatResolutionSession.cs`
- Reward calculation: `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Idle/IdleCombatRewardCalculator.cs`
- Outcome processing: `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Idle/IdleCombatOutcomeProcessor.cs`
- Transaction boundary: `LL/src/Core/Application/MediatR/Behaviors/TransactionBehavior.cs`

## Twenty-four-hour workload

The API configuration currently specifies:

- Encounter cadence: 10 seconds.
- Maximum offline duration: 24 hours.
- Maximum encounters per internal batch: 1,000.
- Maximum internal batches per API resolution: 100.

For a complete 24-hour backlog:

```text
1 + 86,400 seconds / 10 seconds = 8,641 encounters
```

This normally requires:

- 8,641 exact combat simulations.
- 9 internal orchestration and reward batches.
- 1 client request.

Each combat allows up to 6,000 engine ticks, making the theoretical upper bound approximately:

```text
8,641 encounters * 6,000 ticks = 51,846,000 combat ticks
```

Actual battles will usually finish earlier, but every tick can evaluate combatants, active abilities, attacks, effects, statuses, conditions, regeneration, barriers, and summons.

## Existing strengths

The current design already addresses several important risks:

- Offline processing is lazy. Players who do not return consume no combat-processing resources.
- Offline progress is capped at 24 hours.
- A normal 24-hour backlog fits in one API request.
- Encounter identities and random seeds are stable and deterministic.
- Source entities are bulk-loaded per internal batch rather than per encounter.
- Combat loot is generated through a batch API.
- Only the final encounter captures the full event log.
- The response is compacted before being returned to the client.
- Character-level command locking prevents concurrent tabs from resolving the same boundary independently.

These should be preserved during optimization.

## Primary bottlenecks

### 1. Exact sequential combat replay

Every encounter deep-clones each participating `CombatEntity` and executes the complete combat engine. Runtime cost therefore grows with:

- Number of due encounters.
- Number of combatants per encounter.
- Number of ticks before victory, defeat, or timeout.
- Ability, effect, status, condition, and summon complexity.

As long as offline combat must be exactly equivalent to individual online battles, this cost cannot be reduced to constant time. It can only be made substantially cheaper per battle or safely parallelized.

### 2. Allocation volume

Each internal batch currently creates and retains up to 1,000 complete `CombatEncounterRecord` objects and their `CombatResult` graphs before reward processing finishes.

Additional allocations include:

- Deep-cloned combatants.
- Runtime participant lists.
- Encounter plans and identities.
- Reward facts and per-encounter calculated outcomes.
- Loot groups and inventory objects.
- Gathering result objects, including failed gathering attempts.
- Prophecy progress events.

Although the response is compacted, temporary allocations can still create considerable garbage-collection pressure.

### 3. Reward and progression fan-out

The complete reward pipeline runs once per internal batch. A full day therefore performs it approximately nine times.

Notable scaling work includes:

- One essence-drop call for every victorious encounter.
- One essence resonance operation for every eligible defeated creature.
- Gathering rolls for every victory and matching gathering node.
- At least one prophecy progress event per encounter, plus one for each defeated creature.
- Sorting prophecy events and testing each event against active prophecies.
- Inventory insertion, loot-history recording, and realtime loot messages per batch.
- Creature archive, guild contribution, and idle-combat outbox processing per batch.

Some underlying services use request-scoped caches, which can avoid repeated database reads after the first lookup. However, the per-encounter loops, asynchronous method calls, objects, and downstream processing remain.

### 4. Long transaction and command lock

The entire catch-up runs in one HTTP command transaction. This provides atomicity, but it also means:

- The per-character lock is held for the complete replay.
- The database context tracks accumulated mutations until the final save and commit.
- A cancellation, timeout, or exception rolls back the entire catch-up.
- A large number of players returning together can cause a CPU spike even though they do not contend for the same character lock.

This is an operational risk independently of average single-player latency.

### 5. Repeated setup

Each internal batch creates a new resolution session, reloads source entities, rebuilds templates, and prepares them for combat.

For a 24-hour backlog this happens approximately nine times. The character template must sometimes be refreshed, but the area, creatures, hostile templates, and much of the reward metadata are immutable throughout the request.

### 6. Initial double action query

Resolving an action first loads enough data to determine its type and then loads the complete combat graph. This produces two action queries for every resolution.

It is a valid smaller optimization target, but it is unlikely to materially affect a 24-hour replay compared with combat simulation and reward processing.

## Semantic progression constraint

The current 1,000-encounter batch boundary is not merely a memory limit. It is also a progression checkpoint.

After each internal batch:

- Character experience is awarded.
- The character may gain one or more levels.
- Level-ups immediately increase base Power and Max Health.
- Attuned essences receive combat experience.
- Gathering professions receive experience and may level up.
- Essence resonance and pity state advance.

The next internal batch reloads and prepares the combat character after those changes. Later battles can therefore use a stronger character than earlier battles.

Consequently, processing all 8,641 encounters from one frozen combat snapshot would change outcomes and rewards. Any exact optimization must preserve the current progression checkpoint behavior, or deliberately redefine it as a game-balance change.

## Recommended exact-semantics architecture

Introduce a request-scoped idle-combat catch-up coordinator with two distinct units of work:

1. Small streaming chunks that bound temporary memory.
2. Existing 1,000-encounter semantic checkpoints that preserve progression behavior.

### 1. Load immutable catch-up context once

At the start of the request, load and prepare data that cannot change during catch-up:

- Area and spawn configuration.
- Creature source entities.
- Hostile combat templates.
- Reward tables and item-base metadata.
- Gathering-node definitions.
- Other immutable combat and reward definitions.

Reuse this context across every semantic checkpoint. Reload or rebuild only the friendly/player portion when progression changes combat-relevant state.

### 2. Stream lightweight encounter facts

After each combat, immediately reduce the full result to a lightweight record containing only data required by rewards and progression:

- Encounter identity, sequence, and timestamp.
- Victory, defeat, or draw.
- Spawned and defeated creature identities.
- Surviving player health percentage.
- Experience and currency basis.
- Data required for achievements and prophecies.

Retain the complete `CombatResult` only for the final encounter that must be shown to the player. This avoids holding 1,000 full result graphs until the reward phase.

### 3. Preserve semantic checkpoints

At the current 1,000-encounter boundary, apply only progression that can influence later processing:

- Character experience, level, and base combat attributes.
- Equipped or attuned essence experience and combat-relevant state.
- Gathering profession experience and level.
- Essence resonance, pity, and focus-related state.

After applying it, rebuild the friendly template only when relevant state actually changed. Enemy templates should remain reusable.

The initial implementation should retain the current 1,000-encounter checkpoint size. Changing it would change when level-ups affect later battles and therefore constitutes a balance change.

### 4. Defer non-influencing side effects

Aggregate state that cannot affect later battles and apply it once at the end of the catch-up:

- Inventory loot.
- Cinders and soulstones.
- Guild mission contribution.
- Creature archive counters.
- Loot history.
- Realtime loot notification.
- Idle-combat completion outbox payload.
- State-sync invalidation.

Stackable rewards should be combined before persistence. Unique equipment must still retain independent instances and database rows.

### 5. Add an ordered batch essence API

Essence drops currently involve an asynchronous call for each victorious encounter, followed by per-creature processing.

A dedicated catch-up batch API should:

- Accept the ordered sequence of defeated creature groups.
- Preserve the current random roll order.
- Preserve resonance and pity transitions after every creature.
- Load focus, resonance, loot-table, and item-base data once.
- Return aggregated drops and final resonance mutations.

This removes thousands of asynchronous state-machine transitions without changing results.

### 6. Aggregate prophecy progress

Prophecy processing should consume grouped progress facts instead of thousands of individual event objects.

Grouping must retain all criteria used by prophecies, including:

- Occurrence window and relevant daily or weekly boundary.
- Encounter result.
- Enemy count.
- Creature definition.
- Profession or gathering type.
- Low-health victory thresholds.
- Treasure quantities.

The grouping window must be split whenever accepted prophecies or prophecy periods differ, ensuring the optimized calculation remains semantically equivalent.

### 7. Aggregate persistence and notifications

After final settlement, emit:

- One consolidated loot-history entry.
- One consolidated realtime loot notification.
- One consolidated combat outbox payload where consumers support aggregate counts.
- One guild contribution update.
- Grouped creature archive mutations.
- One state-sync invalidation per changed scope.

This reduces both immediate request work and asynchronous outbox follow-up work.

## Subsequent exact optimizations

### Reduce combat cloning

`DeepCloneForEncounter` is necessary because combatants are mutated during simulation, but a full domain-object clone may be more data than the engine needs.

Possible improvements include:

- Immutable prepared templates plus compact mutable runtime state.
- Resettable combat-state arrays.
- Object pooling for frequently allocated runtime collections.
- Precompiled ability and effect graphs shared across encounters.

This should be driven by allocation profiling because it affects the core combat engine.

### Bounded parallel simulation

Encounters inside one 1,000-fight semantic checkpoint use the same starting combat snapshot and stable per-encounter random seeds. They may therefore be candidates for parallel simulation.

This must not be implemented until:

- The combat executor and catalog dependencies are proven thread-safe.
- Encounter simulation is isolated from shared random sources.
- Reward rolls remain ordered and sequential where required.
- A global concurrency limit prevents each request from independently saturating all CPU cores.

Parallelism can improve one player's latency while making system-wide overload worse, so it should not be the first optimization.

### Time-budgeted continuation or durable worker

A measured wall-time or CPU budget can stop a command before it monopolizes an API instance. Remaining work could then continue through:

- A subsequent client resolution request.
- A durable background catch-up job.

A worker improves request reliability and smooths bursts but does not reduce the total amount of CPU work. Jobs would require an idempotency identity such as character, schedule generation, and processed time range.

### Global admission control

Add a process-wide or distributed concurrency limit for large catch-ups. Small normal resolutions should not wait behind many 24-hour replays.

Admission control should distinguish ordinary one-encounter resolutions from large offline catch-ups and expose queue-delay metrics.

## Approximate offline settlement

The only way to reduce thousands of battles to nearly constant work is to stop replaying every battle exactly.

A possible hybrid would:

- Statistically settle older offline time.
- Simulate only the final 100 to 1,000 encounters exactly.
- Generate deterministic aggregate rewards from a build and area snapshot.

This would be substantially faster, but it would change:

- Exact win and loss sequences.
- Essence pity progression.
- Unique loot rolls.
- Health-based achievements.
- Creature-specific prophecy progress.
- Progression and level-up timing.

Approximate settlement must therefore be treated as an explicit game-design and balance decision, not as a transparent technical optimization.

## Benchmark and observability requirements

There is currently no dedicated idle-combat performance benchmark in the repository. Runtime metrics now expose the major phases, but a representative repeatable harness is still needed for:

- 1 hour: approximately 361 encounters.
- 8 hours: approximately 2,881 encounters.
- 24 hours: approximately 8,641 encounters.
- Early-, mid-, and late-game character builds.
- Areas with different enemy counts and ability complexity.
- Fast victories, fast defeats, and near-timeout battles.

Measure the following separately:

- Planning duration.
- Template loading and preparation duration.
- Combat simulation duration and encounters per second.
- Average and maximum ticks per encounter.
- Reward calculation duration.
- Prophecy processing duration.
- Persistence and transaction duration.
- Database query and affected-row counts.
- Allocated bytes and garbage collections.
- Loot and outbox payload sizes.
- Character-lock duration.
- Catch-up queue delay and concurrency.
- Cancellation and rollback rate.

This separation is necessary to determine whether combat execution, allocations, reward processing, or persistence dominates on production-like hardware.

## Recommended implementation order

1. **Partially complete:** performance instrumentation is implemented; the 1-hour/8-hour/24-hour benchmark harness remains.
2. **Partially complete:** compact cross-batch settlement and final-result-only statistics are implemented; true within-batch streaming remains.
3. **Complete:** immutable hostile source and template context is reused across internal batches.
4. **Complete:** ordered grouped essence processing is implemented.
5. **Complete:** prophecy input is aggregated without losing encounter timestamps.
6. **Complete:** progression-affecting checkpoint writes are separated from deferrable rewards and side effects.
7. **Partially complete:** inventory, currency, archive, prophecy, and idle-combat outbox work is consolidated; guild contributions intentionally remain checkpoint-local, and broader state-sync consolidation should be measured before further changes.
8. **Pending measurement:** profile and optimize combat runtime cloning and allocation behavior.
9. **Pending measurement:** add catch-up concurrency and transaction-time protection.
10. **Game-design decision:** consider approximate settlement only if exact replay remains too expensive after profiling and optimization.

## Expected outcome

This approach will not make exact replay independent of encounter count. It should, however:

- Preserve deterministic combat outcomes and rewards.
- Preserve the current point at which progression affects later battles.
- Remove most repeated immutable setup.
- Substantially reduce temporary allocations.
- Reduce reward and prophecy method-call fan-out.
- Reduce database and realtime operations.
- Make catch-up latency and resource consumption measurable and controllable.

The combat engine will remain the irreducible cost. If that cost is still unacceptable after these changes, the remaining decision is whether the game should continue guaranteeing exact offline replay or adopt a deliberately approximate settlement model.
