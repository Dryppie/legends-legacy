# Benchmark-relative validation: evaluator contract

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The opt-in 528-fight validation policy is implemented at the evaluator and deterministic reconstruction boundary.** It freezes one non-benchmark challenger after nomination, evaluates that exact recipe against the benchmark on 60 fresh paired values, and returns the benchmark when the completed validation gate is unmet. Existing v1–v4 proposal policies retain their schedules, selectors and serialized report shape.

This follows the [saved-stage diagnosis](Tower-Benchmark-Tie-Stage-Review.md): strict training-score leaders accounted for the remaining benchmark deficit, including 52 of 75 net lost wins at roots 5 and 8. The new policy is an engineering hypothesis. Its implementation provides no combat evidence of improvement, and the previous pilot remains `AbandonThisConfiguration`.

## Frozen policy semantics

The new proposal racing version is `tower-proposal-racing-v5`, with explicit `selectionPolicyVersion: tower-racing-benchmark-validation-v1`. Mixing this selector with an earlier version, omitting its selector identity, retaining the old panel schedule or changing the fixed budget fails before a checkpoint or evaluator request.

| Stage | Members | Common values | Physical evaluations |
| --- | ---: | ---: | ---: |
| Wave 1 screen | 12 | 8 | 96 |
| Wave 1 continuation | 7 | 8 | 56 |
| Wave 2 screen | 15 | 8 | 120 |
| Wave 2 continuation | 7 | 8 | 56 |
| Nomination | 5 | 16 | 80 |
| Validation | 2 | 60 | 120 |
| Total | | **108 distinct panel values** | **528** |

The proposal root is separate from all 108 panel values. The six panel roles are `wave-1-screen`, `wave-1-continuation`, `wave-2-screen`, `wave-2-continuation`, `nomination`, and `validation`, in that order. All panel values must be mutually distinct and disjoint from the scope's excluded history, generation values, existing schedules and retained reference scenarios. The caller supplies already authorized values; this API does not allocate them.

Generation, pruning, diversity, the first four panels and the five-member nominee ordering remain unchanged. The shorter nomination panel chooses one of the four non-benchmark nominees using the existing positive-win ranking, primary-reference tie preference and zero-win health/nominee ordering. Other retained references are eligible challengers. Benchmark leadership on nomination does not skip validation or change its budget.

Before the first validation request, the checkpoint contains a `ValidationFreeze` binding the policy version, plan hash, nomination panel hash, challenger identity and benchmark identity. The ordinary panel freeze binds both exact recipes, all 60 values and their request order. The challenger occupies the first 60 requests and the benchmark the next 60, with ordinal pairing by seed. No observation can nominate a different challenger during validation.

For the complete validation panel, let `G` be challenger-only wins and `L` benchmark-only wins. Draws and defeats both count as non-wins. The policy passes only when `G > L` and:

```text
20 × sum(comb(G + L, k), k = G .. G + L) <= 2 ** (G + L)
```

This is the fixed one-sided exact discordant-pair gate at 0.05. The calculation uses `BigInteger` recurrence and comparison; no floating threshold rounding occurs. Its exact numerator and denominator fit in signed 64-bit integers for at most 60 discordant pairs and are retained in `ValidationDecision`, together with both binding hashes, gains, losses, pass/fail and selected identity. Zero discordance gives a tail probability of one and returns the benchmark.

All 120 validation requests execute before the gate is applied. Failed, cancelled, partial, mismatched or reordered evidence cannot become a completed benchmark fallback: the report has no selected output or validation decision. The attempt that fails remains charged. Deterministic reconstruction regenerates proposals, stage choices, challenger freeze, requests and gate, then compares the complete report.

This is a per-search provisional output rule, not a family-wide error guarantee, team confirmation or adoption criterion. The 16/60 split is a versioned fixed-budget proposal, not a setting optimized against historical held-out outcomes. It may frequently fall back and may miss small improvements. Fresh held-out evaluation remains necessary to assess the eventual selected outputs.

## Compatibility and execution boundary

The shared report adds two nullable fields, `validationFreeze` and `validationDecision`, both omitted from JSON for older versions. Existing wrappers continue to require their original five-panel schedule. The new wrapper explicitly opts into the six-panel contract. Older positive ties, zero-win fallbacks, proposal streams and first-wave feedback semantics are unchanged.

`tower-proposal-racing-check` can validate a v5 evaluator plan without combat. It reports `ValidEvaluatorContract`, `nativeExecutionSupported: false` and `admissionRequired: true`. Existing native execution and native archive verification explicitly reject v5, and the closed comparison designs reject it as an arm. This prevents a valid evaluator plan from being mistaken for a qualified scientific execution path.

No new prospective comparison, fresh-value allocation, runtime admission, native preparation or combat was performed. The scientific manifests, original plan, admissions, publication verifications and execution handoffs remain historical evidence. No existing allowance or exclusion ledger was changed.

## Changed implementation and next step

| File | Change |
| --- | --- |
| [TowerBenchmarkValidation.cs](../LL/tools/BalanceHarness/TowerBenchmarkValidation.cs) | New frozen-challenger records, nomination rule, exact gate and complete paired-panel decision. |
| [TowerBatchRacing.cs](../LL/tools/BalanceHarness/TowerBatchRacing.cs) | Two-stage output path and checkpoint ordering; reference contrasts use only references measured on the current panel. |
| [TowerBatchRacingContract.cs](../LL/tools/BalanceHarness/TowerBatchRacingContract.cs) | Omitted nullable report fields and explicit six-panel validation. |
| [TowerAdaptiveRacing.cs](../LL/tools/BalanceHarness/TowerAdaptiveRacing.cs) | Internal opt-in schedule validation; public legacy validation remains unchanged. |
| [TowerProposalPolicies.cs](../LL/tools/BalanceHarness/TowerProposalPolicies.cs) | Explicit v5 identity and dispatch, reusing the existing generator and reconstruction. |
| [TowerProposalRacingNative.cs](../LL/tools/BalanceHarness/TowerProposalRacingNative.cs) | Rejects unqualified v5 execution/verification and labels the zero-combat contract check. |
| [BalanceHarnessBenchmarkValidationTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessBenchmarkValidationTests.cs) | Synthetic full trajectories, gate boundaries, exhaustive count arithmetic, freeze/reconstruction checks and failure paths. |
| [BalanceHarnessBenchmarkTieSelectionTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessBenchmarkTieSelectionTests.cs) | Unknown-version test uses an actually unknown identifier now that v5 exists. |

The next implementation step is **native persistence and independent audit support for this exact contract**. Persist the challenger freeze before dispatch, retain and verify six panels, reconstruct paired validation and its exact gate independently, and test interruption and evidence tampering at that boundary. Keep existing native versions readable. A separately designed comparison and resource admission must follow that qualification; this implementation does not authorize reusing or extending the closed pilot.

## Verification

**254 backend tests passed, with zero failures or skips:** 144 focused tests and 110 related regression tests. The focused group includes 32 new validation cases, 47 frozen-racing cases, 39 adaptive-racing cases and 26 benchmark-tie cases. The new gate test independently checks all 1,891 possible gain/loss count pairs using a Pascal triangle. Related tests cover proposal policies, damage affinities, affinity creation, existing native adapters and closed selector-study bindings.

All backend tests ran through the repository wrapper, using an isolated build output:

```powershell
./build/run-tests.ps1 -ArtifactsPath .artifacts/benchmark-validation-contract-20260924 -Filter 'FullyQualifiedName~BalanceHarnessBenchmarkValidationTests|FullyQualifiedName~BalanceHarnessBatchRacingTests|FullyQualifiedName~BalanceHarnessAdaptiveRacingTests|FullyQualifiedName~BalanceHarnessBenchmarkTieSelectionTests'
./build/run-tests.ps1 -NoBuild -ArtifactsPath .artifacts/benchmark-validation-contract-20260924 -Filter 'FullyQualifiedName~BalanceHarnessProposalPolicyTests|FullyQualifiedName~BalanceHarnessProposalNativeTests|FullyQualifiedName~BalanceHarnessDamageAffinityTests|FullyQualifiedName~BalanceHarnessAffinityCreationTests|FullyQualifiedName~BalanceHarnessAffinityCreationNativeTests|FullyQualifiedName~BalanceHarnessBenchmarkTieStudyTests'
```

The first sandboxed build could not read the user NuGet configuration. The same wrapper succeeded with the required filesystem access; no NuGet or application configuration was changed. The build had zero errors and 42 existing warnings. No required verification command remains blocked. Test fixtures supplied literal outcomes and guarded against native preparation/combat; no historical held-out outcomes were used to fit the policy.

Evidence: [focused build/test log](../TestResults/benchmark-validation-contract-tests-elevated-20260924.log), [focused TRX](../TestResults/benchmark-validation-contract-tests-20260924.trx), [regression log](../TestResults/benchmark-validation-contract-regression-20260924.log), [regression TRX](../TestResults/benchmark-validation-contract-regression-20260924.trx), and [source, document and preservation verification](../TestResults/benchmark-validation-contract-verification-20260924/verification.json). The failed sandbox build log is retained separately. Scoped whitespace and report-link checks pass; the seven current-status banners are the only edits to preceding documents. Thirteen historical evidence pins and every consumed source of the preceding diagnostic were preserved.

Changed repository files comprise the eight implementation/test files listed above, this report and seven status links. There are no migrations, dependencies added, application configuration changes, deployments or gameplay-default changes. The last published history remains 721,365 excluded values across 252 files; this implementation did not rescan or write the live registry. Test/build work is engineering work and does not alter the previous scientific charges or limits.
