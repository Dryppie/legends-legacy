# World Tower mechanic telemetry: implementation and verification

**The opt-in observer is implemented, and all 16 focused synthetic checks pass. No combat, fresh production values or replays were run.** It can distinguish Seal barrier breaks from timeouts, pillar kills from timed expiry and owner-death cleanup, and actual Resonance stacks from attempted changes. This closes the observation gap identified in the [source review](Tower-Seal-Pillar-Feasibility-Review.md); it does not establish a stronger team.

The scientific result remains **ImprovementNotDemonstrated**, the practical pilot **Closed**, adoption **Hold**, and V19 reliability **Unresolved**. All **483,640** exclusions, including V19's unused confirmation values, remain preserved. The earlier 5,454 pillar end events remain non-kill endings, not a reconstructed count of timed expiries or Resonance gains.

## Implementation and use

[CombatMechanicDiagnostics.cs](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatMechanicDiagnostics.cs) owns copied effect/status/group filters and a bounded list of immutable scalar events. [FastCombatEngine.cs](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs) observes the existing mechanic boundaries. No callback can mutate combat, consume randomness or change targeting. Default observation is null; existing constructors, rules, options, result types and normal report schemas are unchanged. One collector belongs to one engine encounter.

[CombatEngineExecutor.cs](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatEngineExecutor.cs) adds `ExecuteTowerPlaybackObservedAsync` using the existing Tower playback rules and checkpoints. [TowerBattleRunner.cs](../LL/tools/BalanceHarness/TowerBattleRunner.cs) adds `RunObservedAsync`, returning the ordinary battle report alongside a separate schema-1 trace. Normal `RunAsync` still uses the original execution path. No command-line flag, automatic observer, search strategy or automatic archive write was added.

The following illustrates a **future separately authorized diagnostic**, not a command executed by this scope:

```csharp
var observer = new CombatMechanicDiagnostics(
    barrierEffectIds: ["effect.creature.kharad.seal_of_ascension.barrier"],
    statusIds: ["status.kharad.resonance"],
    summonGroupIds: ["summon-group.kharad.twin_pillars"]);
var observed = await runner.RunObservedAsync(input, observer, token);
// Keep the trace separate from ordinary study reports and bind it to that input/run identity.
HarnessJson.WriteNew(diagnosticPath, observed);
```

These IDs are caller configuration; the engine contains no new Kharad-specific rule. The trace records its filters, completion state, final tick, event cap and truncation state. The default cap is 8,192 observations, with a supported range of 1–65,536. Correlation storage is bounded too. Reaching the cap stops retention without interrupting combat. `DroppedObservations` counts recognized observations rejected by the cap; it cannot enumerate pulses from correlations that were themselves dropped. Only a **completed, untruncated** trace is eligible for exhaustive analysis. Cancellation or failure before the engine returns leaves completion false.

| Observation | Meaning and interpretation |
| --- | --- |
| Barrier start, break, timeout | Match activation and application-order IDs. Values are accepted barrier on start, the last consumed portion on break, and remaining barrier on timeout. An unmatched start at battle end is censored, not a timeout. |
| Linked periodic application | One target application attempt after the chance check, before effect resolution; it is neither a damage amount nor proof of nonzero damage. For this Seal definition, distinct activation/effect/tick tuples describe pulse waves. Several target records can belong to one wave. |
| Summon spawn and end | Member ID, owner ID and group instance identify the pillar. Spawn includes the group's scheduled end tick. Kill, timed expiry and owner-death cleanup have separate kinds. Source ID here is the owner, not the killer. |
| Group resolution | Emitted only at the existing resolution boundary while the owner is alive; value is the actual living-member count immediately before timed cleanup, including zero. Owner-death cleanup is not group resolution. |
| Status state | Actual clamped stacks and persistent lock flag after apply/modify/removal. An attempted reduction at the locked maximum is recorded as unchanged. First lock tick and stack exposure can be derived in event order; same-tick observations do not add elapsed time. |

## Verification and its limits

The complete service and harness source sets compiled in isolated projects against the previously pinned dependency runtime. This avoids rebuilding unrelated APIs and test dependencies. Both `Services.LL.dll` and `BalanceHarness.dll` now have new execution identities; all other copied dependencies remain pinned. The old runtime and sealed studies were not overwritten. Any later combat protocol must bind the new identities and establish observer-on/off combat parity before drawing gameplay conclusions.

The permanent [synthetic tests](../LL/tests/EssenceSystem.Tests/CombatMechanicDiagnosticsTests.cs) cover copied filters, immutable snapshots, bounds/truncation, encounter ownership, activation-specific pulse cancellation, break versus timeout representation, locked-status cancellation independence, actual stack clamping and lock timing, zero/one/two group survivors, owner-death cleanup, compact death logging, and trace serialization. They invoke fabricated primitive boundaries, never an encounter loop or damage resolver. Spawn/break event encoding is synthetic; full production damage-to-break and spawn-to-expiry integration remains untested under the zero-combat constraint.

A byte-identical copy of one saved compact report is retained as the [compatibility fixture](../LL/tests/EssenceSystem.Tests/Fixtures/mechanic-compact-report-v1.json.gz), from pilot `study/battles/trial-000641.json.gz`. It passes canonical JSON round-trip equality without diagnostic fields. Sealed archive hashes are unchanged. These checks establish schema compatibility and preservation of existing bytes, **not newly simulated outcome or byte parity**.

Final focused verification used the required entry point:

```powershell
build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/balance/tower-mechanic-telemetry-20260916/tests -Filter 'FullyQualifiedName~CombatMechanicDiagnosticsTests'
```

**16 passed; zero failed or skipped.** The initial isolated test build failed on missing required fields in two fabricated effects; the fixtures were corrected and all failed-build costs/logs retained. The first 14-test pass was followed by the scheduled-expiry field and two additional survivor cases, then final service/harness/test rebuilds and the 16-test pass. Final builds have no warnings or errors. Scoped whitespace checks, links, runtime/source hashes and unrelated dirty-file preservation are recorded in the [verification package](../TestResults/balance/tower-mechanic-telemetry-20260916/verification.json). No required check remains blocked. Broad backend tests, combat parity, replays and strength/performance measurements were intentionally outside this authorization.

## Scope and next decision

Changes are the observer, three engine/executor/runner files, synthetic tests and compatibility fixture, this review, README and five handoff notices. The [completion receipt](../TestResults/balance/tower-mechanic-telemetry-20260916/completion.json) carries forward prior cumulative usage and charges setup, failed attempts, successful verification, retained output, overwritten artifacts and publication.

The approved transfer is **180 diagnostic seconds /128 MiB** from unused run capacity to engineering: engineering caps **1,225 seconds /624 MiB**, run **2,075 seconds /368 MiB**, audit unchanged **300 seconds /32 MiB**. Overall caps remain **7,980 diagnostic seconds /5,804,916,736 bytes**. This combined implementation scope stays within 180 seconds /128 MiB; unused capacity does not authorize combat.

The next scientific step is a prospectively bounded diagnostic that first establishes observer parity, then measures Seal duration/pulse exposure and pillar/Resonance timing on a fixed team panel. That evidence should support a complete-party hypothesis before another search change. Any such scope needs an explicit fight/seed budget and stopping rule; none was executed or approved here. There are no content or gameplay-rule changes, migrations, persistent application configuration changes or deployments.
