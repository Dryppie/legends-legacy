# Incumbent tie comparison runner and verifier

22 September 2026. The offline runner, native saved-archive verifier, owned Windows launcher and independent Python row auditor implement the [frozen prospective protocol](Tower-Practical-Incumbent-Tie-Comparison-Plan.md). The subsequent [completed experiment](Tower-Practical-Incumbent-Tie-Comparison-Execution.md) passed both archive audits and returned **SupportsIncumbentTieForFrozenOutputs** (+2.575 points; conditional lower bound +1.930). No production entropy, fresh scientific values or combat were used in the earlier implementation/test step documented below. Both search and selector defaults remain unchanged.

## Execution and evidence

`tower-incumbent-tie-comparison-v1` runs the incumbent generator once for each of 24 restarts. Each search measures 46 candidates on eight discovery values and four nominees on 32 selection values. Both actual selector implementations consume the same measurements and nominee order. Each saved search includes the proposal history, selection rows, selected physical recipes and reasons.

All 48 outputs are durably written to `study/outputs-freeze.json` after exactly 11,904 completed search attempts. Only then can confirmation begin. Exact physical convergence contributes a zero difference; absolute wins remain null. For each differing pair, baseline then candidate receives the same reserved 1,000-value panel. There are no extra reference controls or cross-restart cache reuse. Total fights are `11,904 + 2,000 K`, where `K` is the number of differing restarts. Proposal exhaustion, missing roots, interruption and failed freeze publication cannot release a partial confirmation family or replacement search.

Reservation draws one 131,072-byte cryptographic batch after a durable Pending marker. All bytes are persisted before observing cancellation. Fresh values first fill all 24 search blocks, then all 24 confirmation blocks; the unused fresh tail and unused confirmation blocks remain permanently excluded. There is no refill, retry or resume. The maximum historical count is 967,232. Incomplete exposed reservations remain available for a separate recovery audit and continue to block subsequent allocation when Pending.

The endpoint divides net gained wins by **24,000**, including identical roots. It uses `conditional-range-hoeffding-depletion-v1`, the one-point integer gate of 240 net wins, a strictly positive lower bound and at least three positive restarts. This differs from the historical generator-comparison gates, which remain unchanged. The result concerns these frozen outputs; it does not automatically promote a selector or adopt a team.

## Entry points and launch ownership

- `dotnet BalanceHarness.dll tower-incumbent-tie-comparison-check request.json` validates the fixed contract, complete live history, captured scope, content and compatible retained runtime. It allocates no values and fights no battles.
- `python build/run-incumbent-tie-comparison.py --request request.json --harness <prepared-runtime>/BalanceHarness.dll` owns the single scientific launch. It creates a hidden, suspended process, assigns it to a Windows Job before resuming it, and proves the whole job empty before completion. The native `-run` command takes that launch output directory and rejects direct execution without a live owner/job.
- `dotnet <retained-runtime>/BalanceHarness.dll tower-incumbent-tie-comparison-verify <completed-output>` verifies the complete archive using the producing runtime. It reconstructs the generator, both choices, panels, global freeze, prepared inputs, outcomes and arithmetic from saved evidence. A combat guard forbids new fights.
- `python "Balance Harness/analysis/audit-incumbent-tie-comparison.py" <completed-output> --manifest-sha256 <frozen-files-json-sha256>` authenticates a separately pinned archive and independently reads direct battle outcomes, checks both selection rules, recounts all stages and paired gains/losses, and reproduces the confidence bound and decision. It never invokes native code. Save its stdout outside the sealed archive.

The combined owner allowance is fixed at 4,500 seconds and 4 GiB. Native work has 4,440 seconds and 3,968 MiB, leaving 60 seconds and 128 MiB for parent finalization. Native cancellation, a hard native deadline, parent ownership checks, storage checks and an enclosing watchdog bound failure handling. The shared Windows Job helper now accepts an optional storage-check callback and waits for the terminated root during exception cleanup; existing callers retain their defaults.

The request contains `version`, absolute `captureRoot`, `contentRoot`, `templatePath`, `templateHash`, `registryRoot`, a new `outputRoot` directly beneath that registry, `requiredHistory`, `pendingHistoryRecoveries`, `recoveryReceiptHashes`, `maximumSeconds=4500` and `maximumBytes=4294967296`. No extra allocation settings or policy overrides are supported.

The source template is derived from the exact pinned historical capture with `id=incumbent-tie-template`, the incumbent generator, no anchored primary, empty schedules, a refreshed complete historical union, the new compatible execution hash and `maximumBattles=3496`. That last field satisfies the existing ordinary search-definition validation ceiling; the comparison's actual custom executor and attempt ledger impose the smaller search-plus-chosen-pair accounting. It does not execute an ordinary study or its extra controls.

## Archive contents

The output retains the original request and launch, captured template/manifest, refreshed template and history pins, complete entropy bytes and allocation, reservation ledger, attempt journal, one shared content/runtime archive, 24 search records, the global output freeze, direct compressed battle reports, result, native receipt and enclosing completion/manifest. The native verifier rejects altered selections, reordered or reused panels, partial phases, missing/extra battle and recipe files, changed gameplay dependencies, changed scope and incomplete ownership receipts. The independent audit adds separately implemented outcome counting and decision arithmetic.

`NoSelectorDifferences` is a valid completed result with no confirmation. Other completed decisions are `DoNotPromoteIncumbentTie` and `SupportsIncumbentTieForFrozenOutputs`. An incomplete launch or integrity failure cannot pass publication verification, even if some intermediate result files exist.

## Verification

**108 backend regression tests passed**, including the selector, legacy comparisons and diagnostic archives. After the final saved-settings correction, **all 25 comparison tests passed again** on rebuilt source. **Nine Python checks** passed, including Windows Job descendant completion, deadline termination, storage-callback failure and an independent recount of the C# synthetic archive. The original **seven planning checks** also passed. Final build: 37 existing warnings, zero errors. CLI help, scoped whitespace and local Markdown links were checked.

Evidence: [engineering receipt](../TestResults/incumbent-tie-comparison-verification-20260922/verification.json), [regression TRX](../TestResults/incumbent-tie-comparison-verification-20260922/tests-regressions.trx), [final comparison TRX](../TestResults/incumbent-tie-comparison-verification-20260922/tests-final.trx), [final build/test log](../TestResults/incumbent-tie-comparison-rebuild-20260922.log) and [independent Python test log](../TestResults/incumbent-tie-comparison-python-final-20260922.log).

```powershell
./build/run-tests.ps1 -ArtifactsPath TestResults/incumbent-tie-comparison-build-20260922 -Filter 'FullyQualifiedName~BalanceHarnessIncumbentTieComparisonTests|FullyQualifiedName~BalanceHarnessIncumbentTieTests|FullyQualifiedName~BalanceHarnessIncumbentSelectionTests|FullyQualifiedName~BalanceHarnessAllocationComparisonTests|FullyQualifiedName~BalanceHarnessAnchoredComparisonTests|FullyQualifiedName~BalanceHarnessSelectionDiagnosticTests'
./build/run-tests.ps1 -ArtifactsPath TestResults/incumbent-tie-comparison-build-20260922 -Filter 'FullyQualifiedName~BalanceHarnessIncumbentTieComparisonTests'
python -B build/test-incumbent-tie-comparison.py --fixture TestResults/incumbent-tie-comparison-fixture-20260922
python -B 'Balance Harness/analysis/practical-incumbent-tie-design.py' --self-test
```

Python commands used the bundled runtime. The first sandboxed backend build could not read the user's NuGet configuration; the required wrapper subsequently completed with approved access. No required check remains blocked. Tests use literal reports and fixed integer bytes, with a combat guard; they do not claim runtime compatibility or scientific power from those fixtures. The retained synthetic archive is explicitly engineering evidence and cannot pass public captured-runtime admission. The optional `BALANCE_HARNESS_TIE_FIXTURE_EXPORT` test environment variable exports a fresh synthetic archive for the independent row test; it requires a new directory and is unrelated to production reservation.

## Prepared launch package

The [captured runtime and admission review](Tower-Practical-Incumbent-Tie-Comparison-Admission.md) records the concrete package, producing-source/runtime inventories and request pins. The original gameplay assemblies, content and settings passed compatibility with the tested harness. Public admission returned `ReadyNoReservation` for 505,562 exclusions across 222 ledger files, with zero new values and zero fights at that stage. The subsequent [single experiment completed](Tower-Practical-Incumbent-Tie-Comparison-Execution.md) with 19,904 fights, both audits passing and 538,326 permanent exclusions at close. This scope is closed; the historical packages and prior experimental decisions remain immutable.

No gameplay code, application configuration, database schema or migrations changed in this work. There is no service deployment.
