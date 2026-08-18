# Idle Combat Catch-up Optimization Plan

## Purpose

This document turns the measured 24-hour idle-combat result into an implementation and verification plan. It supplements [Idle Combat Flow and Performance Analysis](idle-combat-flow-performance-analysis.md), which describes the architecture and semantic constraints in detail.

The objective is to reduce catch-up latency and allocation pressure without changing deterministic combat outcomes, random-roll order, progression checkpoints, rewards, or the final encounter playback.

## Measured baseline

The following measurement was captured on 2026-08-18 from the local API's current Debug build. The prepared admin account resolved a complete 24-hour backlog.

| Measurement | Result |
| --- | ---: |
| Total resolve duration | 20.16 seconds |
| Encounters | 8,640 |
| Internal batches | 9 |
| Overall throughput | 429 encounters/second |
| Simulation duration | 19.02 seconds (94.35%) |
| Template preparation | 269.86 ms (1.34%) |
| Progression application | 339.83 ms (1.69%) |
| Final settlement | 321.50 ms (1.59%) |
| Reward calculation | 172.28 ms (0.85%) |
| Simulation allocation | 21.04 GiB |
| Runtime allocation during request | 21.31 GiB |
| Average CPU use | 1.35 logical cores |
| GC pause time | 548.1 ms |
| GC collections | 930 Gen 0, 9 Gen 1, 2 Gen 2 |
| Working set | 279.8-359.8 MiB |

The raw counter capture is stored locally at `TestResults/idle-combat-24h.json`.

### Interpretation

The reward, persistence, and immutable-template optimizations are working. Together, all measured non-simulation phases account for approximately 5.5% of the request.

The next bottleneck is the combat simulation itself:

- Simulation consumes more than 94% of wall time.
- Approximately 2.49 MiB is allocated per encounter.
- The request is not exhausting available CPU capacity. Adding database optimizations will not materially improve this result.
- GC pauses are visible but are not the majority of the duration. Allocation reduction should still improve both execution time and server throughput.

The simulation-allocation metric uses the process-wide `GC.GetTotalAllocatedBytes` counter. Background work can contribute to it, but the custom simulation total and the process runtime total differ by only about 0.27 GiB. This makes simulation the clear source of nearly all allocation during the measured window.

## Non-negotiable behavior

Every optimization must preserve:

1. Stable encounter identities and deterministic random seeds.
2. Encounter and random-number consumption order.
3. Exact combat outcomes for the same inputs.
4. The existing 1,000-encounter progression checkpoints.
5. Level, essence, profession, resonance, and pity changes affecting later checkpoints at the same boundaries as today.
6. Full playback data for the final encounter.
7. Reward totals, unique item identities, prophecy progress, archive progress, guild contribution, and outbox semantics.
8. Atomic rollback and per-character concurrency protection unless a separately designed continuation architecture replaces them.

Changing the checkpoint size or statistically settling battles is a balance change, not a transparent performance optimization.

## Phase 0: Release baseline (intentionally skipped)

Decision recorded on 2026-08-18: use the measured Debug run as the working baseline and proceed directly to profiling. The development PC is better specified than the production server, so the current 20.16-second result is already sufficient evidence that optimization is required.

This means absolute Debug-versus-production comparisons will not be made. Improvements must instead be validated with repeatable before/after runs on the same machine, build mode, character fixture, area, and backlog duration. A production-like baseline can still be added later if capacity planning requires it.

## Phase 1: obtain an allocation and CPU profile

Counters identify the expensive phase but not the responsible types and call sites. Capture profiles before introducing pooling or a new runtime representation.

### Tasks

- Capture one 24-hour CPU trace covering only `CharacterActions/Resolve`.
- Capture a separate allocation trace for the same deterministic fixture.
- Produce a ranked list of:
  - Methods by inclusive and exclusive CPU time.
  - Allocated types by total bytes and object count.
  - Allocation call stacks inside `IdleCombatOrchestrator`, the combat executor, cloning, effects, statuses, and result construction.
- Confirm how much allocation belongs to:
  - `DeepCloneForEncounter` and cloned entity graphs.
  - Mutable combat runtime state.
  - Event/statistics collections.
  - `CombatResult` and `CombatEncounterRecord` graphs.
  - Ability, effect, status, condition, targeting, and summon processing.
  - Reward-facing projections.

### Decision gate

Do not add object pools until the profile identifies the dominant objects and demonstrates that their lifetimes are safe to reset and reuse.

### Captured findings (2026-08-18)

A verbose GC/allocation and sampled-CPU trace was captured while the admin character resolved 8,641 encounters in 18.98 seconds. The allocation events covering the request represented 18.08 GiB of sampled allocation. This is lower than the 21.31 GiB runtime-counter total because allocation ticks are sampled, but it is sufficient to rank types.

The leading sampled allocation types were:

| Type | Sampled allocation |
| --- | ---: |
| `Func<RuntimeCondition, bool>` | 4,019.10 MiB |
| `List<CompiledTrigger>` | 1,788.70 MiB |
| `FastCombatEngine` condition/summon closure | 1,186.18 MiB |
| `ListWhereIterator<RuntimeCondition>` | 1,027.18 MiB |
| `ListWhereIterator<RuntimeCombatant>` | 963.94 MiB |
| `List<RuntimeStatus>` | 799.75 MiB |
| `ListWhereIterator<RuntimeAbility>` | 797.32 MiB |
| `CombatEvent` | 544.08 MiB |
| `List<RuntimeBarrierContribution>` | 399.82 MiB |
| `List<RuntimeCondition>` | 388.54 MiB |

Generated closure types were mapped back to `ConditionsPass`, `EffectCanResolve`, `HasReachedSummonCap`, and `Publish`. The largest `RuntimeCondition` predicate and iterator allocations came from `RuntimeCombatant.GetConditionStacks`. Target selection, active-ability filtering, status/condition aggregation, and trigger publication also created high-frequency LINQ iterators and delegates.

`CombatEncounterRecord` and the idle reward projections did not appear among the leading allocation types. Within-batch result streaming therefore remains worthwhile for peak retention, but it is not the first allocation optimization.

The trace was interrupted immediately after the request and its final block was incomplete. Allocation events were recovered successfully, but managed CPU symbols were incomplete. A clean CPU-only trace should be captured after the first allocation reduction if wall time remains high.

### First measured optimization (implemented 2026-08-18)

The following hot paths were rewritten as allocation-free indexed loops:

- Runtime condition, status, barrier, and source-damage aggregation.
- Runtime trigger-index construction.
- Active-ability readiness and trigger/effect resolution checks.
- Event-listener combatant and ability traversal.
- Summon lookup and summon-cap checks.
- First/random enemy selection while preserving random-call count and candidate order.
- Compiled-condition evaluation.

The implementation preserves event-start combatant boundaries, deterministic candidate order, short-circuit behavior, and random-number consumption. The backend correctness suite passes all 1,172 fast tests.

### Post-change 24-hour measurement (2026-08-18)

The prepared admin fixture was reset to the same 24-hour boundary and resolved with the optimized Debug build under the same counter collector. The API reported 8,641 processed actions; the idle-combat instrument recorded the comparable 8,640 combat encounters in 9 internal batches.

| Measurement | Baseline | Optimized | Change |
| --- | ---: | ---: | ---: |
| Total resolve duration | 20.16 s | 14.10 s | **-30.1%** |
| Simulation duration | 19.02 s | 13.14 s | **-30.9%** |
| Simulation allocation | 21.04 GiB | 10.48 GiB | **-50.2%** |
| Runtime allocation during request | 21.31 GiB | 10.08 GiB | **-52.7%** |
| CPU time | 27.19 core-s | 17.05 core-s | **-37.3%** |
| GC pause time | 548.1 ms | 343.4 ms | **-37.3%** |
| Gen 0 collections | 930 | 582 | **-37.4%** |
| Working-set range | 279.8-359.8 MiB | 242.8-297.8 MiB | lower |
| Resolve time per encounter | 2.333 ms | 1.632 ms | **-30.1%** |
| Simulation allocation per encounter | 2.493 MiB | 1.242 MiB | **-50.2%** |

The direct HTTP stopwatch measured 14.17 seconds, consistent with the instrumented 14.10-second server duration. Encounter normalization removes the one-action difference in the API result count.

This clears the gate: the patch materially reduces both duration and allocation, so it should be retained. Simulation remains approximately 93% of the optimized resolve duration, however, and the run remains above the provisional 10-second product target. A clean CPU-only trace and a fresh allocation ranking are now the next evidence-gathering step; the eliminated iterator and closure types should no longer obscure the remaining hot paths.

The optimized raw counter capture is stored locally at `TestResults/idle-combat-24h-after-hot-loops.json`.

### Second profile and optimization (implemented 2026-08-18)

The admin fixture was reset again and captured in a finalized combined sampled-CPU and verbose-allocation trace. Profiling overhead increased the direct request measurement to 17.44 seconds, so that duration is not used as a performance comparison. The trace is used only to rank the remaining hot allocations and their call stacks.

| Remaining allocation type | Sampled allocation | Dominant source |
| --- | ---: | --- |
| `List<CompiledTrigger>` | 2,031.95 MiB | `RuntimeAbility.Tick` cooldown-key snapshots |
| `List<RuntimeStatus>` | 921.29 MiB | status tick and event-publication snapshots |
| boxed `RuntimeCombatant` enumerators | 731.41 MiB | tick-phase `IReadOnlyList` traversal |
| `ListWhereIterator<RuntimeCombatant>` | 653.26 MiB | regeneration and summon filtering |
| `List<RuntimeBarrierContribution>` | 457.63 MiB | barrier tick snapshots |
| `List<RuntimeCondition>` | 449.32 MiB | condition tick snapshots |
| `List<RuntimeEffect>` | 434.47 MiB | active-effect tick snapshots |
| `RuntimeCondition[]` | 340.34 MiB | materializing condition snapshots |
| summon predicate/iterator objects | 554.38 MiB | expired summon-group filtering |

The resulting implementation:

- Reuses per-runtime cooldown-key buffers instead of allocating a new trigger list every tick.
- Reuses engine-owned snapshot buffers for effects, statuses, conditions, barriers, summon groups, and standalone summons.
- Retains snapshot-at-phase-start behavior, list order, and removal behavior when callbacks mutate the source collections.
- Uses a cleared, `finally`-returned `ArrayPool<RuntimeStatus>` snapshot for recursive event publication, where a single reusable engine buffer would not be reentrancy-safe.
- Replaces boxed `IReadOnlyList` enumeration and regeneration/summon LINQ filters with indexed traversal.
- Selects the oldest consumable condition with a direct minimum scan and reuses a sorted thorns snapshot.

The finalized trace is stored locally at `TestResults/idle-combat-24h-profile-after-hot-loops.nettrace`. The next gate is another same-fixture 24-hour counter run. The second patch should be retained only if it preserves outputs and produces another material allocation reduction without regressing wall time.

### Reusable-buffer 24-hour measurement (2026-08-18)

The same 24-hour admin fixture was resolved with lightweight counters after the reusable-buffer changes. The API reported 8,641 processed actions; the comparable combat instrumentation recorded 8,640 encounters in 9 batches.

| Measurement | Original baseline | First optimization | Reusable buffers | Change from first | Change from baseline |
| --- | ---: | ---: | ---: | ---: | ---: |
| Total resolve duration | 20.16 s | 14.10 s | 11.06 s | **-21.6%** | **-45.2%** |
| Simulation duration | 19.02 s | 13.14 s | 10.14 s | **-22.8%** | **-46.7%** |
| Simulation allocation | 21.04 GiB | 10.48 GiB | 3.06 GiB | **-70.8%** | **-85.5%** |
| Runtime allocation | 21.31 GiB | 10.08 GiB | 3.20 GiB | **-68.3%** | **-85.0%** |
| CPU time | 27.19 core-s | 17.05 core-s | 12.55 core-s | **-26.4%** | **-53.9%** |
| GC pause time | 548.1 ms | 343.4 ms | 253.7 ms | **-26.1%** | **-53.7%** |
| Gen 0 collections | 930 | 582 | 311 | **-46.6%** | **-66.6%** |
| Working-set range | 279.8-359.8 MiB | 242.8-297.8 MiB | 255.3-306.4 MiB | slightly higher | lower peak |
| Resolve time per encounter | 2.333 ms | 1.632 ms | 1.280 ms | **-21.6%** | **-45.1%** |
| Simulation allocation per encounter | 2.493 MiB | 1.242 MiB | 0.363 MiB | **-70.8%** | **-85.4%** |

The direct HTTP stopwatch measured 11.15 seconds, consistent with the 11.06-second instrumented server duration. The patch passes its retention gate and exceeds the plan's final allocation and GC-pause reduction thresholds. It narrowly misses the provisional 50% wall-time reduction gate and remains above the provisional 10-second product target on the local Debug fixture.

Simulation still accounts for approximately 91.7% of the request. The next evidence-gathering step is a fresh post-buffer allocation/CPU trace; the removed snapshot allocations should no longer obscure the remaining engine costs. The raw counter capture is stored locally at `TestResults/idle-combat-24h-after-reusable-buffers.json`.

### Third profile and optimization (implemented 2026-08-18)

The admin fixture was reset and captured in a second finalized combined sampled-CPU and verbose-allocation trace after the reusable-buffer changes. The direct request took 13.90 seconds under profiling overhead; as with the prior traces, that duration is not used for the before/after wall-time comparison.

| Remaining allocation type | Sampled allocation | Dominant source |
| --- | ---: | --- |
| `CombatEvent` | 601.51 MiB | record construction and `with` cloning throughout event publication and damage resolution |
| `System.String` | 138.71 MiB | condition identifiers and other combat strings |
| `System.Int32[]` | 118.45 MiB | runtime collection growth and framework internals |
| `CompiledEffect` | 104.42 MiB | repeated ability compilation during encounter setup |
| boxed `CombatTeam` | 100.22 MiB | team `ToString` calls in statistics logging |
| `EquipmentStatBudgetRule` | 92.98 MiB | recreating immutable rules on every catalog lookup |
| ordered barrier iterators | 81.32 MiB | sorting barrier contributions before consumption |
| `RuntimeBarrierConsumption` | 32.43 MiB | result objects created even when no barrier exists |
| `List<RuntimeBarrierConsumptionEntry>` | 30.60 MiB | contribution lists created even when no barrier exists |

The resulting experiment and retained implementation changes:

- Experiments with changing the engine-private immutable `CombatEvent` from a reference record to a value record. This experiment is later rejected by the measured latency and CPU gate below; the retained implementation continues using the reference record.
- Returns prebuilt immutable `EquipmentStatBudgetRule` instances from the catalog instead of reconstructing equivalent records in combat stat calculations.
- Maps valid condition and combat-team enum values to cached string literals, retaining the prior formatting fallback for unknown enum values.
- Reuses a combatant-owned barrier ordering buffer for both cap trimming and source-aware barrier consumption. Sorting direction and snapshot-before-mutation behavior are preserved.
- Returns a shared immutable empty barrier-consumption result when damage has no barrier to consume; attributed non-empty consumption still gets a request-specific result and entries.

The finalized trace is stored locally at `TestResults/idle-combat-24h-profile-after-reusable-buffers.nettrace`. The next retention gate is another unprofiled same-fixture 24-hour counter run. Repeated ability compilation is deliberately deferred until this lower-risk batch is measured because caching compiled definitions requires a clear invalidation and identity policy.

### Event-and-catalog 24-hour measurement (2026-08-18)

The same capped 24-hour fixture was resolved with lightweight counters after the event-and-catalog allocation changes. The API reported 8,641 processed actions; the idle-combat instrumentation recorded the comparable 8,640 encounters in 9 batches.

| Measurement | Reusable buffers | Event and catalog | Change from reusable buffers | Change from baseline |
| --- | ---: | ---: | ---: | ---: |
| Total resolve duration | 11.06 s | 11.73 s | **+6.1%** | **-41.8%** |
| Simulation duration | 10.14 s | 10.78 s | **+6.3%** | **-43.3%** |
| Simulation allocation | 3.06 GiB | 2.04 GiB | **-33.3%** | **-90.3%** |
| Runtime allocation | 3.20 GiB | 2.08 GiB | **-35.1%** | **-90.3%** |
| CPU time | 12.55 core-s | 12.33 core-s | **-1.7%** | **-54.7%** |
| GC pause time | 253.7 ms | 197.0 ms | **-22.3%** | **-64.1%** |
| Gen 0 collections | 311 | 202 | **-35.0%** | **-78.3%** |
| Working-set range | 255.3-306.4 MiB | 266.4-311.6 MiB | slightly higher | lower peak |
| Resolve time per encounter | 1.280 ms | 1.357 ms | **+6.0%** | **-41.8%** |
| Simulation allocation per encounter | 0.363 MiB | 0.242 MiB | **-33.3%** | **-90.3%** |

The direct HTTP stopwatch measured 11.81 seconds, consistent with the 11.73-second instrumented duration. The allocation, CPU, collection, and pause metrics support retaining the batch for server throughput, but the single-run wall-time result regressed by 6.1% and remains above the provisional 10-second target. Because CPU time decreased slightly while wall time increased, this result does not prove that the value-record representation added compute cost; scheduling and run-to-run variability remain plausible. Repeat the unprofiled measurement once before declaring the latency gate passed or failed, then capture a clean CPU-only trace if simulation remains dominant.

The raw counter capture is stored locally at `TestResults/idle-combat-24h-after-event-catalog.json`.

The repeat run recorded an 11.44-second server resolve and 11.51-second direct HTTP duration. Simulation took 10.47 seconds and allocated 2.03 GiB; the request-window runtime counters recorded 2.17 GiB allocated, 12.83 core-seconds, 215.4 ms of GC pause, and 191 Gen 0 collections. Compared with the reusable-buffer run, the repeat is 3.5% slower while still allocating approximately one-third less. Both samples therefore show a modest latency regression, while independently confirming the allocation and GC improvement.

The engine then tested passing the large immutable value event by readonly reference through its non-nullable hot paths. The repeat raw capture is stored locally at `TestResults/idle-combat-24h-after-event-catalog-repeat.json`.

That readonly-reference run failed its gate: server resolve increased to 12.16 seconds, direct HTTP duration to 12.25 seconds, and simulation to 11.13 seconds. Simulation allocation remained at 2.03 GiB, but request CPU increased to 14.98 core-seconds; the optimization therefore produced no additional allocation benefit while making the measured compute path slower. The raw capture is stored locally at `TestResults/idle-combat-24h-after-event-readonly-ref.json`.

The complete value-event experiment has been reverted and `CombatEvent` is again a reference record. The immutable rule cache, enum-string literals, reusable barrier ordering, and shared empty barrier result remain. The next gate measures this retained subset in isolation before proceeding to repeated ability-compilation work.

### Retained catalog/string/barrier measurement (2026-08-18)

The same 24-hour fixture was resolved after restoring the reference-record event while retaining only the independent immutable-rule, cached-string, reusable-barrier, and shared-empty-result changes. The API reported 8,641 processed actions and the combat instrumentation recorded the comparable 8,640 encounters in 9 batches.

| Measurement | Reusable buffers | Retained subset | Change |
| --- | ---: | ---: | ---: |
| Total resolve duration | 11.06 s | 10.90 s | **-1.4%** |
| Simulation duration | 10.14 s | 9.28 s | **-8.5%** |
| Simulation allocation | 3.06 GiB | 2.41 GiB | **-21.2%** |
| Runtime allocation | 3.20 GiB | 2.59 GiB | **-19.0%** |
| CPU time | 12.55 core-s | 11.48 core-s | **-8.5%** |
| GC pause time | 253.7 ms | 257.7 ms | +1.6% |
| Gen 0 collections | 311 | 269 | **-13.5%** |
| Working-set range | 255.3-306.4 MiB | 268.0-312.6 MiB | slightly higher |

The direct HTTP stopwatch measured 10.97 seconds. The subset passes its retention gate: it improves duration, CPU, and allocation while avoiding the value-event regression. Compared with the original baseline, resolve duration is approximately 46.0% lower, simulation allocation 88.6% lower, runtime allocation 87.9% lower, CPU time 57.8% lower, GC pause 53.0% lower, and Gen 0 collections 71.1% lower. The remaining request is still slightly above the provisional 10-second target, so the next measured optimization targets repeated immutable ability-catalog compilation. The raw capture is stored locally at `TestResults/idle-combat-24h-after-retained-catalog-barrier.json`.

### Compiled-catalog reuse measurement (2026-08-18)

The production JSON catalog now lazily compiles its immutable abilities, statuses, and summons once per application lifetime. The scoped executor also reuses stable ascension/evolution variants for the request when the combatant has no temporary ability modifiers. Supplemental abilities, mutable/fake providers, and temporary dungeon/run variants retain the existing compilation path.

The same 24-hour fixture again resolved 8,640 comparable encounters in 9 batches:

| Measurement | Retained subset | Compiled-catalog reuse | Change |
| --- | ---: | ---: | ---: |
| Total resolve duration | 10.896 s | 10.944 s | +0.4% |
| Direct HTTP duration | 10.966 s | 11.015 s | +0.4% |
| Simulation duration | 9.276 s | 9.981 s | +7.6% |
| Simulation allocation | 2.4092 GiB | 1.6495 GiB | **-31.5%** |
| Runtime allocation | 2.5894 GiB | 1.6546 GiB | **-36.1%** |
| CPU time | 11.4844 core-s | 11.0312 core-s | **-3.9%** |
| GC pause time | 257.7 ms | 156.5 ms | **-39.3%** |
| Gen 0 collections | 269 | 156 | **-42.0%** |
| Working-set range | 268.0-312.6 MiB | 264.6-316.4 MiB | comparable |

The total request change is only 48 ms and is within normal run-to-run noise, while allocation, collection frequency, pause time, and CPU all improve materially. The cache therefore passes the server-throughput retention gate even though it does not improve this single request's measured wall time. The apparent redistribution between instrumented simulation and uninstrumented resolve overhead does not change the end-to-end result; a clean CPU-only trace is the appropriate next step before another compute-path change. The raw capture is stored locally at `TestResults/idle-combat-24h-after-compiled-catalog.json`.

### Clean CPU trace and transient-identity optimization (2026-08-18)

A sampled-thread-time trace was captured from the compiled-catalog build while the same fixture resolved 8,641 actions. The profiled HTTP request took 11.29 seconds. Isolating stack intervals containing `FastCombatEngine.Run` produced 9.85 seconds of engine samples.

| Engine sample attribution | Self time | Share |
| --- | ---: | ---: |
| Runtime GC polling/safepoints | 4,968 ms | 50.4% |
| `Guid.NewGuid()` | 2,860 ms | 29.0% |
| All other individual methods | each below 151 ms | each below 1.6% |

The GUID work was entirely attributable to `EffectExecutionContext` creation and summon identity. These identifiers are private to one engine execution: activation IDs correlate barriers with linked effects, summon-group IDs correlate members created by one activation, and summon actor IDs distinguish runtime combatants. None requires process-wide or cryptographic uniqueness.

The engine now uses monotonic per-engine activation and summon sequences instead. Activation strings and the context's healing/group dictionaries are created lazily, so ordinary triggers no longer allocate two unused dictionaries or format an identity. Group members still share one activation-scoped group ID, separate activations remain distinct, and seeded runs become more reproducible. The next same-fixture counter run is the retention gate for this change.

The raw trace is stored locally at `TestResults/idle-combat-24h-cpu-after-compiled-catalog.nettrace`; its converted stack data is at `TestResults/idle-combat-24h-cpu-after-compiled-catalog.speedscope.json`.

### Deterministic-identity 24-hour measurement (2026-08-18)

The same 24-hour fixture resolved 8,640 comparable encounters in 9 batches after replacing transient GUIDs and making execution-context state lazy:

| Measurement | Compiled-catalog reuse | Deterministic identities | Change |
| --- | ---: | ---: | ---: |
| Total resolve duration | 10.944 s | 9.856 s | **-9.9%** |
| Direct HTTP duration | 11.015 s | 9.931 s | **-9.8%** |
| Simulation duration | 9.981 s | 8.311 s | **-16.7%** |
| Simulation allocation | 1.6495 GiB | 1.4385 GiB | **-12.8%** |
| Request-window runtime allocation | 1.6546 GiB | 1.6759 GiB | +1.3% |
| CPU time | 11.0312 core-s | 10.8906 core-s | **-1.3%** |
| GC pause time | 156.5 ms | 160.0 ms | +2.2% |
| Gen 0 collections | 156 | 125 | **-19.9%** |
| Working-set range | 264.6-316.4 MiB | 262.2-315.2 MiB | slightly lower |

The change passes its retention gate and brings this local Debug fixture below the provisional 10-second target for the first time. The instrumented simulation allocation fell materially, while the one-second process-wide request window recorded a small allocation and pause-time increase. Background workers and sampling-boundary alignment can affect those process-wide values, so one repeat run is appropriate before treating the 10-second result as stable. The raw capture is stored locally at `TestResults/idle-combat-24h-after-deterministic-identities.json`.

## Phase 2: stop retaining full within-batch result graphs

Status: deferred until the hot-loop optimization has been remeasured. The allocation profile did not identify encounter-record retention as a leading source of total allocation.

This is the first implementation candidate because it is bounded in scope and does not require parallel execution or approximate outcomes.

`IdleCombatOrchestrator` currently builds a `CombatEncounterRecord` for each encounter and retains all records until the batch is passed into reward processing. A batch may contain 1,000 records. Even though non-final statistics are removed, the remaining result graph still survives longer than necessary.

### Proposed shape

Introduce a compact, idle-specific batch projection that contains only the data needed after simulation:

- Encounter identity, sequence, and timestamp.
- Outcome and post-combat player health.
- Spawned and defeated creature identifiers/counts.
- Reward and progression inputs.
- Achievement and prophecy facts.
- The complete final `CombatResult` only when final playback is requested.

For each encounter:

1. Resolve combat exactly as today.
2. Immediately project the result into compact immutable facts.
3. Feed those facts into the batch accumulator.
4. Retain the full result only for the final encounter.
5. Release references to all other runtime and result graphs before the next encounter.

### Likely files and contracts

- `Combat/Layers/Orchestration/Idle/IdleCombatOrchestrator.cs`
- `Combat/Layers/Orchestration/Models/CombatOrchestrationResult.cs`
- `Combat/Layers/Rewards/Idle/IdleCombatRewardFactBuilder.cs`
- `Combat/Layers/Rewards/Models/IdleCombatRewardFacts.cs`
- `Combat/Layers/Resolution/Idle/IdleCombatResolutionSession.cs`
- `Combat/CombatSessionAccumulator.cs`

Use existing namespaces and contract locations after confirming their current paths. Do not make reward services depend on the combat engine's mutable runtime types.

### Required tests

- Golden comparison of all encounter outcomes for fixed seeds before and after the change.
- Exact equality of total experience, currencies, items, essence drops, gathering rewards, sigils, resonance, and pity state.
- Exact equality at the 1,000-encounter progression boundary.
- Equality of final encounter statistics and playback events.
- Equality of prophecy, archive, guild contribution, and outbox outputs.
- Cancellation and exception tests proving pooled or temporary state cannot leak into another request.

### Exit condition

- No semantic regression in the deterministic comparison suite.
- At least a 30% reduction in allocated bytes per encounter, or profiling evidence showing that retained result graphs are not a material allocator.
- No regression in total Release duration, database work, or final response size.

If this phase does not materially reduce allocation, keep the simpler implementation and proceed using the profiler's actual top allocator.

## Phase 3: replace deep domain cloning with compact runtime state

If profiling confirms `DeepCloneForEncounter` or its descendants dominate allocation, separate immutable combat definitions from per-encounter mutable state.

### Design direction

- Keep prepared entity, ability, and effect definitions immutable and share them across encounters.
- Represent mutable health, resources, cooldowns, durations, stacks, barriers, summons, and threat in compact runtime structures.
- Reset or recreate only this compact state for each encounter.
- Prefer arrays or indexed buffers where combatant and ability counts are bounded and stable.
- Keep domain entities and EF-tracked entities outside the combat runtime.
- Add pooling only for large, frequently allocated buffers with simple ownership.

### Safety requirements

- A rented object must have exactly one owner.
- All mutable fields and collection counts must be reset before reuse.
- Buffers must be returned in `finally` blocks after cancellation or exceptions.
- No pooled data may be retained by the final playback result.
- Pool sizes must be bounded; the pool must not become an unbounded memory cache.
- Deterministic iteration and random-number order must remain unchanged.

### Exit condition

Compared with the Phase 0 Release baseline:

- At least a 70% reduction in simulation allocation per encounter.
- At least a 40% reduction in 24-hour simulation duration.
- No semantic differences across fixed-seed golden scenarios.
- No sustained increase in working set after repeated catch-ups.

## Phase 4: optimize proven CPU hot paths

After allocation lifetime is corrected, optimize only call sites that remain prominent in the CPU profile.

Candidates may include:

- Repeated ability/effect condition evaluation.
- Target-list creation and sorting.
- Status and effect collection scans.
- Per-tick LINQ and iterator allocations.
- Repeated derived-stat calculation.
- Combat event creation for non-final encounters.
- Encounter reset and setup work.

Prefer local, benchmarked changes. Avoid rewriting the entire combat engine unless the profile demonstrates that its runtime representation is the limiting factor.

### Exit condition

The Release benchmark shows a meaningful improvement outside run-to-run noise, and every change has a focused correctness test.

## Phase 5: add operational protection

Even a faster exact replay scales linearly with encounters. Protect API availability when many players return simultaneously.

### Tasks

- Classify resolutions as ordinary or large catch-ups using due encounter count.
- Add a bounded process-wide concurrency limit for large catch-ups.
- Expose queue delay, active catch-ups, rejected/cancelled requests, and total catch-up CPU time.
- Keep ordinary one-encounter resolutions outside the large-work queue where safe.
- Define an HTTP timeout comfortably above the measured Release p95.
- Alert on p95 duration, allocation per encounter, queue delay, failure rate, and rollback rate.

A durable continuation worker should be considered only if optimized Release p95 still exceeds the acceptable request duration or large-login bursts threaten API availability. Moving work to a worker improves reliability and admission control but does not reduce total CPU work.

## Parallelism decision

Do not parallelize encounter simulation first.

The measured request averages only 1.35 logical cores, so parallelism could reduce individual latency. However, every returning player could then consume several cores, reducing total server capacity and amplifying login bursts.

Bounded parallel simulation is appropriate only after:

- Allocation pressure is substantially reduced.
- The executor and shared catalogs are proven thread-safe.
- Each encounter uses only its deterministic seed and isolated runtime state.
- Reward and pity rolls remain ordered where required.
- A global concurrency budget coordinates work across all requests.
- Load testing demonstrates better aggregate throughput, not only better single-user latency.

## Performance acceptance criteria

Final thresholds should be set from the Phase 0 Release baseline. Recommended initial gates are:

| Metric | Required result |
| --- | --- |
| Deterministic outputs | Exact match |
| 24-hour wall time | At least 50% below Release baseline |
| Allocation per encounter | At least 70% below Release baseline |
| GC pause time | At least 50% below Release baseline |
| Database command count | No regression |
| Final response size | No regression |
| Sustained working set | No growth across repeated runs |
| Concurrent catch-up throughput | No regression at the same CPU limit |

An absolute latency target should be chosen after the Release baseline is known. A reasonable first product target is a 24-hour p95 below 10 seconds on production-equivalent hardware, followed by a lower target if profiling shows it is attainable without semantic changes.

## Rollout sequence

1. Record and retain the Release baseline.
2. Add deterministic golden fixtures before changing the runtime.
3. Implement one optimization phase at a time.
4. Run fast correctness tests through `build/run-tests.ps1`.
5. Run the deterministic catch-up comparisons.
6. Repeat the 1-hour, 8-hour, and 24-hour Release benchmark matrix.
7. Compare phase timings, allocation, GC, CPU, database commands, and output equivalence.
8. Deploy behind normal release controls and monitor p50/p95 metrics.
9. Roll back if output mismatches, allocation regresses, or catch-up failure rate increases.

## Immediate work order

- [x] Accept the existing Debug counter run as the working baseline.
- [x] Capture a verbose allocation profile for the 24-hour case.
- [x] Rank allocation types and map generated closures to source methods.
- [x] Remove the leading hot-loop LINQ, delegate, and iterator allocations.
- [x] Repeat the 24-hour counter run and compare duration, allocation, and GC activity.
- [x] Capture a finalized post-optimization allocation/CPU trace and rank the newly exposed allocators.
- [x] Replace per-tick collection snapshots with reusable, mutation-safe buffers.
- [x] Repeat the 24-hour counter run after the reusable-buffer optimization.
- [x] Capture and rank a fresh post-buffer allocation/CPU trace.
- [x] Experiment with per-event value records alongside immutable stat-rule, enum-string, and barrier optimizations.
- [x] Repeat the 24-hour counter run after the event-and-catalog optimization.
- [x] Repeat the event-and-catalog run once to resolve the wall-time variance.
- [x] Pass non-nullable value events by readonly reference through measured hot paths.
- [x] Measure and reject the readonly-reference value-event adjustment.
- [x] Restore the reference-record event while retaining the independent catalog, string, and barrier changes.
- [x] Measure the retained catalog, string, and barrier subset.
- [x] Cache immutable catalog compilation and stable player essence variants.
- [x] Capture a clean CPU-only trace if simulation remains the dominant phase.
- [ ] Add fixed-seed, before/after golden result coverage.
- [ ] Prototype compact within-batch encounter facts.
- [ ] Benchmark the prototype and keep it only if it materially reduces allocation.
- [ ] Redesign deep cloning only if profiling identifies it as a major remaining source.
- [ ] Optimize remaining measured CPU hot paths.
- [ ] Add global admission control and production alerts.
- [ ] Reassess whether exact replay meets the product latency target.

## Explicitly deferred

- Approximate or statistical offline settlement.
- Changing the 1,000-encounter progression checkpoint.
- Unbounded per-request parallelism.
- Splitting the transaction or moving work to a durable worker without a separate idempotency and recovery design.
- Further reward/database optimization without evidence that it has become material.

These options remain available, but the current measurements do not justify taking their semantic or operational risks before addressing simulation allocation and CPU cost.
