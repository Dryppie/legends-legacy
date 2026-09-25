# Benchmark preference on positive selection ties

23 September 2026. Target: the offline `LL/tools/BalanceHarness`.

The opt-in selector recommended by the [creation-stage review](Tower-Affinity-Creation-Stage-Review.md) is implemented. When the fixed benchmark shares the highest positive win count on the final selection panel, it is selected. Every other case uses the existing selector, including its older-primary preference, zero-win health ordering and frozen nominee order. This is an engineering implementation; no improvement in held-out performance has been established.

## Contract and behavior

An opted-in `TowerProposalRacingPlan` uses both:

```json
{
  "version": "tower-proposal-racing-v4",
  "selectionPolicyVersion": "tower-racing-benchmark-positive-tie-v1"
}
```

These are fields on a complete existing plan, not a standalone runnable request. The benchmark is resolved from `racing.benchmarkReferenceId`; `racing.scope.stages.selectionPrimaryReferenceId` remains the legacy fallback reference. Versions 1–3 reject a non-null selector field. Version 4 rejects a missing, empty or unknown selector identity. It accepts the existing proposal-policy versions and still requires the appropriate affinity inventory, legal scope and fixed 528-observation allocation.

| Final selection scores | Result with opt-in |
| --- | --- |
| Benchmark and primary tie at the positive maximum | Benchmark |
| Benchmark and a challenger tie at the positive maximum | Benchmark |
| Benchmark uniquely leads | Benchmark, as before |
| Primary and challenger tie above the benchmark | Primary, as before |
| Challengers tie above both references | Existing frozen nominee order |
| Everyone has zero wins | Existing guardian-health ordering, then frozen nominee order |

For example, benchmark 28, primary 30 and challenger 30 still selects the primary even if the challenger comes first in the nominee order. Merely changing the old selector's primary argument to the benchmark would change this case; the implementation instead applies an explicit benchmark override followed by the original selector.

Generation, owner placement, proposal streams, screen and continuation panels, pruning, common-panel ranking, nominee membership and ordering, and the final 40-observation panel are unchanged. No margin threshold or stability rule has been fitted.

The nullable selector field is omitted from legacy plan and report serialization. Existing JSON shapes, canonical hashes and default behavior therefore remain intact. Version 4 reports explicitly retain the selector identity. The complete plan hash also flows through panel freezes, request hashes, feedback references, native archive algorithm identity and cache keys. Equal physical scenarios under different selector plans have different evidence identities. Reconstruction rejects a changed or missing selector, a swapped plan, changed requests or an altered selected output.

The existing `tower-proposal-racing-check` command validates the complete opt-in plan without running combat and reports the selector identity and `admissionRequired: true`. Native archive verification uses the same plan validation and deterministic reconstruction before checking literal battle reports and charge/input journals. Its verified output also includes the selector identity. Legacy command output omits the new field.

## Separation from closed studies

The existing preservation and creation comparison contracts remain fixed. Their pair validator explicitly requires the previous racing versions, so neither arm can silently acquire the new selector. The independent Python study auditor remains version-specific and is unchanged. Supporting a new scientific selector comparison will require a new prospective comparison contract, corresponding owned execution and independent audit support, followed by runtime/resource admission and fresh values.

No scientific search, held-out evaluation, reservation, entropy draw, admission or policy promotion was performed for this implementation. The creation pilot remains `Inconclusive`, and its larger-evaluation gate remains unmet. The earlier pilot can motivate this hypothesis but cannot serve as fresh confirmation evidence.

## Changed files

- [TowerBatchRacing.cs](../LL/tools/BalanceHarness/TowerBatchRacing.cs): positive-maximum benchmark override with the unchanged selector as fallback.
- [TowerProposalPolicies.cs](../LL/tools/BalanceHarness/TowerProposalPolicies.cs): v4 plan/report identity, version validation and benchmark resolution.
- [TowerProposalRacingNative.cs](../LL/tools/BalanceHarness/TowerProposalRacingNative.cs): selector identity in zero-combat check and verification output.
- [TowerProposalComparison.cs](../LL/tools/BalanceHarness/TowerProposalComparison.cs): keep closed comparison pairs on their declared selectors.
- [BalanceHarnessBenchmarkTieSelectionTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessBenchmarkTieSelectionTests.cs) and [BalanceHarnessProposalNativeTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessProposalNativeTests.cs): rule boundaries, serialization compatibility, unchanged proposal paths, request binding, native reconstruction and tamper rejection.
- This report and current-status links in the harness guides and earlier assessments.

There are no migrations, application configuration changes, deployments or gameplay-default changes. Any future admitted run must capture the final producing runtime; the old admission does not qualify the new executable.

## Verification

**All 242 distinct selected backend cases have passing results:** 216 related regression cases from the broad run and all 26 selector cases from the final focused run. No cases were skipped. The related coverage includes frozen/adaptive racing, legacy golden hashes, proposal policies, creation, native archive reconstruction, tampering, and both closed study designs. Both ordinary and owned panel-freeze native adapters were exercised with v4 literal evidence.

The first broad run reported 239 passes and three failures in a new serialization assertion. That assertion incorrectly compared JSON-node output with typed serialization, which escapes a date's plus sign differently. The correction compares the old and new typed shapes directly; all 26 selector cases then passed. Production code did not change between those runs: the tested harness DLL remained `4d2c19dc7734c5a7738466d90feff3fcf9e81589568eecd885877217121cef07`. The final build had zero errors and 13 existing warnings; the earlier full build had 42 warnings.

The required wrapper commands were:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessBenchmarkTieSelectionTests|FullyQualifiedName~BalanceHarnessBatchRacingTests|FullyQualifiedName~BalanceHarnessAdaptiveRacingTests|FullyQualifiedName~BalanceHarnessProposalPolicyTests|FullyQualifiedName~BalanceHarnessProposalNativeTests|FullyQualifiedName~BalanceHarnessAffinityCreation|FullyQualifiedName~BalanceHarnessDamageAffinityTests|FullyQualifiedName~BalanceHarnessProposalStudyTests' -ArtifactsPath 'TestResults/benchmark-tie-selection-build-20260923'

./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessBenchmarkTieSelectionTests' -ArtifactsPath 'TestResults/benchmark-tie-selection-build-20260923'
```

Logs and results: [broad log](../TestResults/benchmark-tie-selection-tests-approved-20260923.log), [broad TRX](../TestResults/benchmark-tie-selection-tests-broad-20260923.trx), [final log](../TestResults/benchmark-tie-selection-tests-final-20260923.log), [final TRX](../TestResults/benchmark-tie-selection-tests-final-20260923.trx). The initial sandboxed build could not read the installed NuGet configuration; the approved wrapper run resolved that access problem. No required command remains blocked.

The public zero-combat plan check accepted the [legacy shape](../TestResults/benchmark-tie-selection-verification-20260923/legacy-check.log) and [explicit opt-in shape](../TestResults/benchmark-tie-selection-verification-20260923/opt-in-check.log), preserving the proposal-policy hash while changing the full plan hash. It [rejected](../TestResults/benchmark-tie-selection-verification-20260923/invalid-version-check.log) a v3 plan carrying the new selector. These are structural checks of separately copied historical plan inputs, not admitted plans or scientific runs; no old values were evaluated or reallocated.

[Producing-symbol verification](../TestResults/benchmark-tie-selection-verification-20260923/compiled-source-files.json) binds the six changed C# files to the tested harness and test assembly. The [completion receipt](../TestResults/benchmark-tie-selection-verification-20260923/completion.json) retains the passing-case accounting, command outputs, changed-file hashes and unchanged historical evidence pins. Scoped whitespace and report-link checks passed. The scientific pilot, its plan/admission, sealed stage review and previous completion receipts remain untouched.

Next is a separately versioned prospective selector comparison with the same generation policy in both arms, followed by matching owned execution and independent audit support. It must preserve the distinction between descriptive pilot evidence and a fresh evaluation, and qualify the final runtime before any new scientific allocation.
