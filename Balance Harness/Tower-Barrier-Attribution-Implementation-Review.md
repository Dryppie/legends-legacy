# Opt-in barrier damage attribution

**16 September 2026 — implemented; all 30 focused synthetic checks passed.** The optional mechanic observer can now record who consumed each watched barrier, the exact consumed amount and its position relative to linked pulse applications. Offline accounting flags unexplained consumption and unfinished activations. This implements the next step from the [Web Weaver postmortem](Tower-Web-Weaver-Postmortem.md); no stronger party or full-encounter observer parity is established.

## Changes and use

- [CombatMechanicDiagnostics](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatMechanicDiagnostics.cs) adds the explicit `CaptureBarrierDamage` init property. Its default is false. The existing constructor and positional records keep their signatures; the new nullable event metadata is omitted from JSON when absent. Default traces remain schema 1. Opting in produces schema 2, including when no barrier was damaged.
- [FastCombatEngine](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs) observes the entire result of `ConsumeBarrierWithSources` immediately after consumption and before absorption callbacks. It records the actual contribution amounts before integer logging conversions, without repeating mitigation, selecting targets or drawing random values. The disabled branch does not enumerate contributions or construct metadata.
- [CombatBarrierDamageAccounting](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatBarrierDamageAccounting.cs) reconciles watched contributions and associates each damage observation with the number of linked waves already emitted. It is a separate offline calculation, outside combat execution.
- [CombatBarrierDamageDiagnosticsTests](../LL/tests/EssenceSystem.Tests/CombatBarrierDamageDiagnosticsTests.cs) supplies 14 new synthetic cases. The existing 16 observer cases remain unchanged.

Attach a fresh collector to a fresh engine, using the existing barrier-effect filter and maximum-event bound:

```csharp
var diagnostics = new CombatMechanicDiagnostics(barrierEffectIds, [], [], maximumEvents: 8192)
{
    CaptureBarrierDamage = true
};
engine.MechanicDiagnostics = diagnostics;
```

After an independently authorized execution completes, `CombatBarrierDamageAccounting.Analyze(diagnostics.Snapshot())` returns per-barrier accounts and ordered contributions. This step adds no combat launcher, automatic experiment or persistent setting. Current launchers continue their existing behavior unless explicitly changed to opt in.

Each damage event identifies the defender, barrier effect/activation/application order and provider, plus the attacker, optional attacking effect ID, reporting label, damage type and delivery. An empty provider ID explicitly means unknown. A missing attacking effect ID remains null: do not infer a canonical ability from the display label. Attacker-level attribution still works when the effect is unknown. Health overflow and consumption of another contribution are excluded from the watched contribution's amount.

Pulse windows follow recorded engine order, including same-tick damage and pulses. Repeated target applications at the same activation/effect/tick count as one wave. This follows the current trace's wave definition; it is not a generic reconstruction of multiple hypothetical waves at an identical timestamp. Start, end and contribution identities remain separate when barriers overlap.

## Completeness and limits

Accounting requires a completed, untruncated schema-2 trace with no dropped observations. Invalid identities, non-finite amounts, unmatched consumption, duplicate starts, out-of-order ticks and linked applications without an active observed barrier are rejected. Storage uses the existing shared event cap, including its dropped-observation counter. Enabling per-hit attribution consumes that cap faster; its adequate size for a full encounter remains unmeasured.

A broken barrier reconciles against its accepted start amount; a timeout also subtracts its observed remaining amount. `UnattributedAmount` preserves the residual, positive for missing consumption and negative for overcounting. Reconciliation uses `max(0.00001, acceptedAmount * 0.000001)` solely for single-precision runtime rounding. An activation without a recorded end has a null residual and `Reconciled = false`, even when the encounter trace itself is complete.

Damage is only one barrier consumer. Ability costs, style spending and negative adjustments are not assigned to an attacker by this hook. They cause a residual or an unfinished activation. Do not present such an account as complete damage attribution. Existing schema-1 start/end hooks also use rounded logging eligibility in some fractional edge cases; a schema-2 consumed contribution without a corresponding observed start is rejected, rather than supplied an invented start. This change does not alter those gameplay branches or their linked-effect behavior.

The result is an observation and accounting tool. It does not estimate the causal effect of a replacement, reconstruct natural versus forced attacks, supply Haste uptime or select a party. Synthetic non-interference evidence does not establish full encounter parity or a measured whole-run speedup. The failed Web Weaver package remains closed without an automatic slot-only variant or additional values.

## Verification and preservation

One isolated service build and one isolated focused-test build succeeded with **zero warnings and errors**. They used pinned local runtime dependencies and the already available package cache; sealed build outputs were not replaced. The public constructor signatures remain compatible with the captured harness binary used by these fixtures.

The backend invocation went through the required wrapper:

```powershell
./build/run-tests.ps1 -NoBuild -ArtifactsPath TestResults/balance/tower-barrier-attribution-20260916/tests -Filter 'FullyQualifiedName~CombatMechanicDiagnosticsTests|FullyQualifiedName~CombatBarrierDamageDiagnosticsTests'
```

**30 executed /30 passed /0 failed /0 skipped.** New cases cover two-hit accounting, provider/attacker separation, overlapping contributions recorded before break callbacks, overflow exclusion, fractional amounts, unknown providers, Guard/Cover and redirected consumption, non-damage spending at both break and timeout, same-tick pulse ordering and target deduplication, timeout/censoring, cap exhaustion, invalid data, filters, schema-2 round trips and unchanged schema-1 fields. Synthetic enabled/disabled runs compare event-log serialization, health/barrier state and the next values from all three engine random streams. The existing compact archived report still round-trips unchanged. These are fabricated damage boundaries, not encounter runs or balance-seed replays.

The [verification receipt](../TestResults/balance/tower-barrier-attribution-20260916/verification.json) records **7.938 seconds** for restore/build/test work under its 12-second ceiling, with every owned process tree confirmed empty. The previous shared TRX was copied before the wrapper overwrote it, and both versions are retained and charged. No required command remains blocked or unrun. Full application builds, the full backend suite, encounter parity and performance benchmarks were outside this focused scope. Exploratory reads with guessed paths did not locate files; subsequent source discovery resolved the needed locations.

The [preservation record](../TestResults/balance/tower-barrier-attribution-20260916/preservation.json) checks sealed packages, pinned runtime dependencies, seed-history files and unrelated dirty files. The two intentionally edited existing source files are retained as before-edit snapshots, so their older pinned bytes remain available. New source hashes match the successful build. No sealed experiment is rewritten or rerun.

The [completion receipt](../TestResults/balance/tower-barrier-attribution-20260916/completion.json) carries forward the prior limits and charges the fixed **20-second** implementation allowance, including static work, setup, bounded verification and publication. It stays within the **20-MiB** output ceiling and applies no transfer or cap increase. Only **1.66788013849873 engineering seconds** remain after this conservative charge; further implementation must respect that balance. All **483,720** exclusions remain intact. V19 retains 253 required recipes and 512 unused confirmation values, reliability **Unresolved**; adoption remains **Hold** and prior diagnostics/pilots remain **Closed**. No further combat is authorized by this implementation.

Changed files are the three engine diagnostic source files, the new test file, this review, current notices in the BalanceHarness README and five handoff documents, and the isolated evidence/build package. No gameplay rules, content, migrations, persistent configuration or deployment changed.
