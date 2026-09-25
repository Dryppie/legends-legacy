# Benchmark-relative validation: native persistence and independent audit

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The v5 native adapter now persists and verifies the six-panel validation contract.** A separate Python auditor independently recounts the panels and exact paired gate. Qualification uses synthetic literal battle reports at the existing native boundary. No simulator, native input preparation, production seed allocation, runtime admission or scientific study ran.

The [evaluator contract](Tower-Benchmark-Validation-Contract-Implementation.md) remains unchanged: 528 evaluations, a 16-value nomination panel, one frozen non-benchmark challenger, and 60 fresh paired validation values. Completed validation returns the challenger only when the exact gate passes; otherwise it returns the benchmark. Incomplete validation produces neither a selected output nor a validation decision. This is a provisional search-output rule, not confirmation or adoption.

## Persistence and reconstruction

[TowerProposalRacingNative.cs](../LL/tools/BalanceHarness/TowerProposalRacingNative.cs) uses the existing exclusive evidence writer, create-new files, write-through writes and durable flushes. At 408 completed evaluations, it writes `validation-freeze.json` before `panel-06.json`, the first validation charge, input preparation or battle dispatch. The challenger freeze binds the version, plan hash, nomination panel hash and challenger/benchmark identities. The panel freeze supplies the exact recipes, seed order and remaining 120 evaluations.

After all 528 authenticated observations, the adapter saves `validation-decision.json` and then `search.json`. The decision retains the exact integer tail numerator and denominator, discordant counts, freeze/panel hashes, pass/fail and selected identity. A write failure cannot return a successful completed adapter result. Interrupted or failed evaluation retains available charges and partial evidence, without publishing a decision; verification rejects incomplete archives. The adapter does not resume or retry an existing evidence directory.

Native verification requires the exact v5 member set: the previous plan, search, two batches and two journals; six panel freezes; and both validation files. It regenerates the entire proposal/evaluation report, authenticates all 528 observations against literal archived battles and bound inputs, and matches every standalone freeze, decision and journal. Missing, extra or altered evidence fails verification. The public verifier continues to require an external manifest pin and captured content/runtime bindings.

Earlier v1–v4 execution paths retain their five panels and existing evidence membership. No validation files are written for those versions. The closed comparison designs still reject v5 arms. `tower-proposal-racing-check` now reports `ValidPlan`, `nativeExecutionSupported: true`, `admissionRequired: true` and zero fights for a valid v5 plan. There is no new launch command.

## Independent audit

[audit-benchmark-validation.py](analysis/audit-benchmark-validation.py) reads one externally pinned complete compressed native archive. It reuses the established archive authentication, literal gzip battle/recipe reader and fitness primitives. It independently checks the six-panel schedule and value separation, candidate legality and accepted membership, adaptive feedback, pruning/diversity, nominee order, challenger choice, validation pairing, integer decision and both journals.

Nomination and gate decisions use recounted observations. Exact decision counts and fractions use integer equality, including values beyond floating-point precision. A coherent change to both saved copies of a decision or challenger freeze does not bypass recomputation.

The complementary native reconstruction remains required for deterministic proposal RNG, detailed affinity derivation, production input materialization and canonical hashes containing .NET numeric serialization. The Python audit does not claim to reconstruct those mechanisms. Its CLI accepts the established `gzip-json-v1` archive format and requires the externally supplied SHA-256 of `files.json`; it checks the archive inventory before and after recounting.

```powershell
python -B "Balance Harness/analysis/audit-benchmark-validation.py" <complete-native-archive> <manifest-sha256>
```

Successful output is `IndependentlyRecounted`, with `nativeReconstructionRequired: true`, `admissionRequired: true` and `newFights: 0`. The six retained cross-language fixtures are synthetic evidence from the injected native boundary, not prepared production archives. No full production v5 archive was created or audited during this qualification.

## Changed files and design choices

- [Native adapter](../LL/tools/BalanceHarness/TowerProposalRacingNative.cs): ordered durable freeze/decision writes, six-panel membership and reconstruction checks, truthful plan-check capability.
- [Native tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessBenchmarkValidationNativeTests.cs): all three proposal policies, ordinary/owned checkpoint modes, pass/fallback, interruptions, write failure before validation, tampering and retry rejection. Optional fixture export uses a fresh `TOWER_VALIDATION_FIXTURE_OUTPUT` directory.
- [Evaluator tests](../LL/tests/EssenceSystem.Tests/BalanceHarnessBenchmarkValidationTests.cs): replace the temporary native-rejection assertion with continued rejection by existing study bindings.
- [Independent auditor](analysis/audit-benchmark-validation.py) and [Python qualification tests](analysis/test-benchmark-validation-audit.py): independent recount and cross-language/tamper checks.
- This report and seven current-status banners: update the handoff while preserving prior report bodies.

The implementation extends the established persistence protocol and leaves evaluator semantics, generation policy and scientific study contracts unchanged. No migrations, dependencies, gameplay defaults, application configuration or deployments change.

## Next work

Define a separately versioned prospective comparison for the v5 output rule, including its pairing with a declared control, nomination/validation allocation, fresh held-out evaluation, explicit endpoints and cumulative resource budget. Then implement and qualify its binding, reservation, execution and audit path before seeking runtime admission. Existing 73-value-per-search bindings and old admission artifacts cannot authorize the new 109-value contract (108 panel values plus one proposal root).

The preceding pilot remains `AbandonThisConfiguration`. This engineering qualification provides no new evidence of search improvement and does not reopen, retry or extend that pilot.

## Verification

**266 backend tests and 13 Python tests passed, with no failures or skips.** The backend set includes 12 new native qualification cases, all 32 evaluator-contract cases, and 222 related racing, generation, affinity, native-adapter and closed-study regression cases. The Python suite reads six actual C#-emitted synthetic fixtures covering all three proposal policies and both checkpoint modes; it tests pass/fallback, altered literal battles, changed freezes/decisions, missing/extra members, interrupted evidence, reordered pairs, reused values and torn journals. Both languages check all 1,891 valid discordant-count pairs. Python additionally rejects a one-unit numerator change at 2**60, which floating-point equality would lose.

Backend checks ran through `build/run-tests.ps1` with isolated artifacts at `.artifacts/benchmark-validation-native-20260924`. The final filter included `BalanceHarnessBenchmarkValidation`, `BalanceHarnessBatchRacingTests`, `BalanceHarnessAdaptiveRacingTests`, `BalanceHarnessBenchmarkTieSelectionTests`, `BalanceHarnessProposalPolicyTests`, `BalanceHarnessProposalNativeTests`, `BalanceHarnessDamageAffinityTests`, `BalanceHarnessAffinityCreationTests`, `BalanceHarnessAffinityCreationNativeTests`, and `BalanceHarnessBenchmarkTieStudyTests` (joined with the usual `FullyQualifiedName~` filters).

```powershell
python -B -X utf8 "Balance Harness/analysis/test-benchmark-validation-audit.py" TestResults/benchmark-validation-native-verification-20260924/fixtures -v
dotnet .artifacts/benchmark-validation-native-20260924/bin/BalanceHarness/release/BalanceHarness.dll tower-proposal-racing-check TestResults/benchmark-validation-native-verification-20260924/fixtures/policy-3-owned/plan.json
```

The first sandboxed build could not read the existing user NuGet configuration. The same repository wrapper succeeded with elevated filesystem access; no configuration was changed. Three new test-only nullability warnings were corrected before the final run. The final incremental build had zero errors and 13 existing warnings. No required verification command remains blocked.

Evidence: [final backend log](../TestResults/benchmark-validation-native-regression-20260924.log), [final TRX](../TestResults/benchmark-validation-native-regression-20260924.trx), [Python log](../TestResults/benchmark-validation-native-python-20260924.log), [plan-check result](../TestResults/benchmark-validation-native-plan-check-20260924.log), and [qualification/preservation record](../TestResults/benchmark-validation-native-verification-20260924/verification.json). The first 44-test focused run and failed sandbox build are also retained. The source snapshot and implementation patch distinguish this change from the already modified working tree.

Prior implementation evidence, 13 historical manifest/closeout/plan pins, and every consumed source of the preceding saved-stage diagnosis remain unchanged. Only seven current-status banners changed in preceding documents. No scientific manifest, seed registry, resource declaration or earlier result was rewritten. The last published history remains 721,365 excluded values across 252 files; it was neither rescanned nor modified for this task. Engineering tests do not change the scientific study's resource accounting.
