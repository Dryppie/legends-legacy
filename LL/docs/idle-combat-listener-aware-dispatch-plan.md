# Idle Combat Listener-Aware Dispatch Plan

## Goal

Reduce 24-hour idle-combat catch-up CPU time and allocation by avoiding `CombatEvent` construction and combatant scans for event types that cannot currently trigger an ability or status.

The target is a bounded optimization of `FastCombatEngine`, not a change to combat rules, replay fidelity, rewards, or idle batching.

## Why this direction

The retained engine still allocates heavily around `CombatEvent`, but two direct representation changes have already failed the latency gate:

- A large value-type event reduced allocation but increased CPU and wall time.
- A reusable mutable reference-frame stack reduced simulation allocation by 40.0% but increased CPU by 7.1% and simulation time by 25.8%.

Both approaches changed the cost of every observed event. The proposed design instead keeps the accepted reference record and existing `Publish` implementation for observed events, while making events with no possible listener nearly free.

Current `Publish` behavior scans the combatant list, looks up ability listeners, and checks status listeners for every event. Many event families may have no listener in a particular encounter. This must be measured before implementation; the optimization should be abandoned if the unobserved share is too small.

## Hypothesis

Maintain a conservative per-engine listener mask keyed by `AbilityTriggerEvent`.

At an event call site:

1. Check the mask without allocating.
2. Return immediately when the event has no registered listener.
3. Otherwise construct the existing immutable `CombatEvent` and call the existing `Publish` path.

The mask may produce false positives, which only preserve today's work. It must never produce a false negative, because that would skip combat behavior.

This is fundamentally different from the rejected event prototypes: it does not enlarge a value passed through hot methods, pool mutable event state, alter condition evaluation, or change the representation of an event that is actually dispatched.

## Non-goals

- Do not approximate or aggregate battles.
- Do not reorder event, trigger, effect, target, or status processing.
- Do not change random-number consumption for observed events.
- Do not change cooldown, summon, barrier, condition, death, logging, statistics, or replay semantics.
- Do not add a shared or static listener cache.
- Do not combine this work with result-graph compaction, parallel simulation, or admission control.

## Phase 0: measure the opportunity

Add opt-in engine diagnostics that are disabled in normal execution and collect:

- Event attempts by `AbilityTriggerEvent`.
- Event attempts for which no live or potentially relevant listener exists.
- Combatant iterations performed by `Publish`.
- Ability-listener and status-listener matches.
- Trigger executions.
- Target-specific `CombatEvent` clones.

Run the fixed 24-hour fixture once with diagnostics and the accepted fingerprint.

Proceed only when either:

- At least 20% of event attempts have no possible listener; or
- Unobserved events account for at least 20% of combatant iterations.

Remove or compile out the diagnostic counters before performance comparison. Do not benchmark the instrumented build as the baseline.

## Phase 1: conservative listener-presence index

Add an engine-owned listener index with one bit per `AbilityTriggerEvent`. An `ulong` is preferred if the enum fits; otherwise use a fixed-size integer array or bitset with no per-event allocation.

Register listeners from:

- Every combatant's compiled ability triggers when the combatant enters the encounter.
- Starting runtime statuses.
- A status before it can publish or react to subsequent events.
- A summon before it can participate in event publication.
- Any other runtime source that owns `TriggersByEvent`.

The first prototype should be monotonic: bits can transition from absent to present but are not cleared during the encounter. This is deliberately conservative:

- Dead combatants and removed statuses can leave a bit set, causing harmless extra work.
- Newly added statuses and summons cannot be missed.
- The implementation avoids reference counts and mutation-order bugs.

Keep registration local to `FastCombatEngine` unless an existing runtime creation boundary provides a clearly safer hook. Do not make domain or compiled ability models depend on engine diagnostics.

## Phase 2: lazy event materialization

Introduce a private scalar entry point similar to:

```csharp
private void PublishIfObserved(
    AbilityTriggerEvent eventType,
    RuntimeCombatant? source,
    RuntimeCombatant? target,
    string? abilityId,
    IReadOnlyList<RuntimeCombatant> combatants,
    int magnitude = 0,
    RuntimeCombatant? instigator = null,
    long? barrierApplicationOrder = null,
    ConditionRemovalReason? removalReason = null,
    DamageType damageType = DamageType.None,
    AttackType attackType = AttackType.None,
    bool wasCritical = false,
    bool wasDirectHit = false)
```

Its behavior must be limited to:

1. Test the listener bit.
2. Return when absent.
3. Construct the existing `CombatEvent` record.
4. Call the unchanged `Publish` method.

Convert call sites in measured order rather than all at once:

1. High-frequency basic-attack and damage events.
2. Health-change and heal events.
3. Status and condition lifecycle events.
4. Barrier events.
5. Summon and interval events.
6. Combat-start and remaining cold events.

Benchmark after groups 1 and 2. Stop there if they capture nearly all benefit. A smaller retained patch is preferable to mechanically converting cold paths.

Record cloning for target-specific conditions remains unchanged in this phase. It has already been isolated experimentally and is not worth combining with the listener-index result.

## Correctness tests

Add focused tests for:

- An event with no listener performs no trigger work.
- An initial active or passive ability registers every trigger event it owns.
- A starting status registers its events.
- A dynamically applied status can observe the next matching event.
- A newly created summon can observe matching events immediately.
- Removing a status or killing a combatant may leave a conservative bit without changing behavior.
- Nested publication observes listeners registered earlier in the same execution chain.
- Death events still reach the dead source under the existing special rule.
- Source-scoped, lifecycle, cooldown, effect-order, and status-snapshot behavior remain unchanged.
- Two separate engine instances cannot leak listener state into one another.

The fixed benchmark must continue to produce:

`a6c348f6d81ebb54092d776d88bf0e34ac9d3b13ce2712fc35ce04aff0ec918f`

## Performance experiment

Use the guarded local benchmark database and fixed clock from `build/measure-idle-combat.ps1`.

To reduce machine-load ambiguity:

1. Capture a three-run accepted baseline.
2. Capture a three-run prototype result.
3. Revert the prototype and capture a three-run control.
4. Require every run to match the golden fingerprint.
5. Compare the prototype with both surrounding controls, not with an older faster run.

Record server resolve, simulation duration, simulation allocation, request-window allocation, CPU, GC pause, Gen 0/1/2 collections, encounter count, and correctness fingerprint.

## Retention gate

Retain the prototype only when all conditions hold:

- Golden fingerprint matches on every run.
- All backend correctness tests pass, excluding clearly identified unrelated failures already present in both control and prototype.
- Median CPU and simulation duration do not regress by more than 2% against either surrounding control.
- At least one of median CPU or simulation duration improves by 5% or more.
- Simulation allocation falls by at least 10%.
- No new per-event heap allocation is introduced by the listener index.
- The implementation remains engine-local and does not add configuration or migration requirements.

If results are inside the noise band, revert. Allocation reduction alone is not sufficient.

## Likely files

- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`
- Existing combat tests under `LL/tests/EssenceSystem.Tests/`
- `build/measure-idle-combat.ps1` only if a non-production diagnostic field must be added to benchmark output
- `LL/docs/idle-combat-catchup-optimization-plan.md` for the measured decision

No database migration, API contract, frontend change, or deployment configuration should be required.

## Risks and controls

| Risk                                            | Control                                                                                                     |
| ----------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| A dynamically added listener is not registered  | Monotonic registration at every runtime creation/application boundary, plus dynamic-status and summon tests |
| Event enum outgrows the bit representation      | Assert supported enum range during development and use a fixed array when it no longer fits                 |
| Listener mask becomes shared between encounters | Store it only on the `FastCombatEngine` instance and test engine isolation                                  |
| Scalar helper increases JIT/code size           | Convert hot event groups incrementally and inspect CPU before converting cold call sites                    |
| Machine load produces a false win or loss       | Use baseline/prototype/reverted-control medians from the same session                                       |
| Diagnostics distort the result                  | Use diagnostics only to qualify the idea; remove or disable them for retention measurements                 |

## Stop conditions and fallback

Stop and do not implement lazy materialization if Phase 0 shows less than a 20% unobserved-event or avoided-scan opportunity.

Revert immediately if the fingerprint differs or the CPU/latency gate fails. Preserve the diagnostic report and rejection rationale in the existing optimization plan.

If this design fails, stop micro-optimizing `CombatEvent`. The next justified work is a separately scoped architectural project—compact encounter result retention or operational catch-up admission control—selected using fresh heap-retention and production-concurrency evidence.

## Implementation order

1. Restore a buildable worktree and required benchmark credentials.
2. Add and run opt-in event-observation diagnostics.
3. Decide whether the measured opportunity passes the 20% qualification gate.
4. Add listener registration and focused correctness tests.
5. Convert only basic-attack, damage, heal, and health-change event call sites.
6. Run focused combat tests and the complete backend suite.
7. Run baseline/prototype/reverted-control benchmark sets.
8. Retain or revert strictly according to the gate.
9. Update the existing optimization plan with measurements and the decision.
