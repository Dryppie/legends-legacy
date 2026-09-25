# Adaptive racing pilot admission

**Later execution:** the user authorized one campaign. It [stopped after 2,640 search fights because of a launcher monitoring race](Tower-Adaptive-Racing-Comparison-Execution.md), before held-out evaluation. This document records the earlier successful admission. Its sealed package remains unchanged, but its request is no longer runnable: the output exists and the failed batch is permanently reserved.

The offline pilot is **admitted without reservation**, using `tower-adaptive-racing-comparison-v1`. The [implementation](Tower-Adaptive-Racing-Comparison-Implementation.md) and [frozen plan](Tower-Adaptive-Racing-Comparison-Plan.json) remain unchanged. This step created a concrete machine-specific request and authenticated its captured dependencies. It performed **zero scientific launches, zero fresh seed allocations and zero fights**. It does not establish the adaptive search's strength or that a real run will fit its resource allowance.

## Retained package

- [Admission receipt](../TestResults/adaptive-racing-comparison-admission-20260923/admission.json)
- [Executable request](../TestResults/adaptive-racing-comparison-admission-20260923/request.json)
- [External package pin](../TestResults/adaptive-racing-comparison-admission-20260923-pin.json)
- [Native no-reservation check](../TestResults/adaptive-racing-comparison-admission-20260923/native-check.json)
- [Runtime compatibility and stored-observation reconstruction](../TestResults/adaptive-racing-comparison-admission-20260923/context.json)
- [Sealed-package and live-history verification](../TestResults/adaptive-racing-admission-verification-20260923.log)
- [Bounded publication-check process receipt](../TestResults/adaptive-racing-admission-verification-20260923.json)

The package manifest SHA-256 is `d17df9559fe8c091dc72ff2f338203cbaeb444e6e39db6bd1455fd1a7bd73e53`. The request file SHA-256 is `3d9dc117154e2293f58fed347b14027acc39d3f47e314957d3a84f7c396c4412`.

Only the four tested `BalanceHarness` files replace files in the original reconciled capture: DLL, portable PDB, dependency manifest and runtime configuration. Each matches the corresponding file in the backend test output. Every other runtime file and every content/settings file retains its original captured hash. Current gameplay changes in the checkout do not enter this package.

The loaded execution identity is `41385143ac5c20eada2a6ac1e11cafae637cebf3ed49b7fc2448f541ee017789`. Its settings identity remains `f9587e8941a021443aecef37dccf0d5dad250a28df32673cacf7f2df50b12b74`.

The portable symbols match the harness DLL and authenticate all **211 producing source documents**, including generated build sources. The compatibility process resolves **895 methods**, including the adaptive generator, frozen racing kernel, native adapter, comparison entry points, closures and async state machines. Using only stored synthetic observations, it exactly reconstructs the first engineering root: two proposal batches, five panels and 528 recorded evaluations. Those observations are engineering fixtures, not fresh scientific evidence.

The context uses `dotnet exec` with the harness's runtime configuration and the installed managed PowerShell host. This supplies the captured harness's required shared frameworks and avoids the earlier confirmation admission's PowerShell framework failure. The six actual reference preparations occur only in the public native `comparison-check`, which has an active guard against combat. Both the compatibility and native-check process trees exited successfully with no active descendants.

## Full history and frozen scope

The before-and-after independent scans agree on **633,313 reserved values across 240 ledger files**. The native check independently reconstructs that union and validates the existing pending-reservation recovery receipts. The full inventory matches the prior history closeout hash `37e6eb81d6e61c3f0733c93ea309e0fdae4d9d812913835fdfa7043e01a52c0a`.

The admission reads the previous completed comparison's manifest-bound request to retain recovery provenance. It does not re-audit that comparison's past battle reports or use them to claim adaptive efficacy. Every current historical ledger is scanned, including failed or abandoned reservations covered by the existing recovery protocol. Registry and output writer leases protect admission from concurrent owned allocations.

The request retains the original floor-5 scope, all three reference recipes, and `399bc776…` as the primary for positive selection ties. The strongest independently confirmed reference `96b94357…` remains the development benchmark. The twelve paired roots each give the baseline and adaptive search exactly 528 fights. Held-out panels begin only after all 24 outputs are frozen. The maximum is **28,032 physical fights**, with 4,428 assigned values from a single 16,384-word entropy batch; every fresh exposed tail value remains permanently reserved if a later campaign is launched.

At admission completion, no scientific output directory existed. There is no retry, replacement-root or resume permission. Admission does not promote the policy, adopt a team or change the separate pending 52,000-fight confirmation.

## Resources and verification

This admission has its own **600-second / 512-MiB** allowance, charged in full at the start with no refund or transfer. Sealing completed in **170.141 seconds**, retaining **47,635,191 bytes** plus the **506-byte** external pin. The compatibility child took 1.594 seconds; the native check took 19.750 seconds. The remainder includes full history scanning, capture checks, package construction and receipt verification.

A subsequent read-only publication check authenticated every sealed package member twice and repeated the complete live-history scan under registry/output leases. It made no additional native preparations. Its owned process took **47.547 seconds** and exited with an empty job. The check used the remainder of the **same original admission deadline**, with storage checked across the package, pin, wrapper, verification log and receipt. Publication verification completed **254.875 seconds from admission start**, within the original 600-second allowance; no second admission charge or extension was created. The observed retained bytes before its small final receipt were 47,639,257.

The prospective campaign retains its separate **10,800-second / 6-GiB** launch ceiling and **10,680-second / 5.75-GiB** native ceiling. Its request explicitly uses `priorSeconds: 0` and `priorBytes: 0`; preparatory admission is not silently subtracted from, or counted as free work inside, that launch allowance. Together these two declared allowances are 11,400 seconds and 6.5 GiB. They are caps, not throughput or storage predictions, and are not complete historical engineering totals.

**Sixteen admission-helper tests and twelve reused evidence-verifier tests passed.** The helper tests cover captured-runtime identity and membership, settings/scope drift, unscheduled panels, complete-history shape, separate resource accounting, framework selection, failed test receipts, owned-process closeout and existing-output rejection. The evidence tests cover full-byte verification, path safety, file/membership races and deadline interruption. The [helper test log](../TestResults/adaptive-racing-admission-helper-tests-20260923.log) is retained in the package. PowerShell parsing, Python syntax and whitespace checks passed.

The preceding implementation's **180 backend and 48 Python tests** remain the regression evidence. This step did not change C# sources or rebuild the harness; it compared the four retained harness files with the original test output, checked the passing 180-test TRX, and exercised the captured runtime as described above. The recorded Python logs are retained as preceding implementation evidence, not represented as new runs in this step.

The relevant commands were:

```powershell
python -B -X utf8 'Balance Harness/analysis/test-adaptive-racing-admission.py'
python -B -X utf8 'Balance Harness/analysis/test-confirmation-evidence-verification.py'
python -B -X utf8 'Balance Harness/analysis/prepare-adaptive-racing-admission.py' prepare
python -B -X utf8 'TestResults/verify-adaptive-admission-20260923.py'
```

Here `python` denotes `C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`, since Python is absent from `PATH`. Native processes were launched through the retained Windows Job owner. No required verification command was blocked.

The last command is a one-time read-only wrapper tied to the original monotonic deadline, not a reusable launch tool. It invoked the retained `prepare.py verify --live` with the external package pin. Later verification can use that retained verifier directly with a separately explicit bounded read-only allowance; the expired wrapper must not be extended or reused as an admission retry.

## Launch boundary

The original assessment explicitly prohibited launching a new combat campaign. Implementation first stopped at this reviewable request, after which the user explicitly authorized one launch. Prelaunch verification authenticated the package and live registry; the subsequent failure is recorded in the execution report. If history or any bound input changes, fail closed and reassess admission; do not edit the sealed request or bypass its checks.

The following historical command was executed once from the repository root. **Do not run it again: its output exists and its history is stale.**

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B -X utf8 'TestResults/adaptive-racing-comparison-admission-20260923/run-reference-exploration-comparison.py' --request 'TestResults/adaptive-racing-comparison-admission-20260923/request.json' --harness 'TestResults/adaptive-racing-comparison-admission-20260923/runtime/BalanceHarness.dll'
```

The owner enforces the launch/native ceilings, retains failed reservations, and requires both native and independent saved-row verification before publication. No extension, restart, seed refill or promotion is implied by launch authorization.

## Changed files and operational implications

- `analysis/prepare-adaptive-racing-admission.py`: the new single-attempt, seed-free package builder and read-only verifier; reuses the pinned history reader, archive verifier, Windows Job owner and writer-lease implementation.
- `analysis/reference-exploration-context.ps1`: an explicit adaptive mode for JIT compatibility and stored-observation reconstruction; earlier modes retain their behavior.
- `analysis/test-adaptive-racing-admission.py`: sixteen admission contract and failure-boundary tests.
- This document and the implementation document: admission status, evidence links, separate accounting and the unexecuted launch command.
- Local `TestResults` artifacts: sealed machine-specific request, captured inputs/runtime/source evidence, process receipts and verification output. These are local artifacts rather than environment-specific application configuration.

There are no gameplay changes, database migrations, application configuration changes or deployment requirements. The failed three-reference confirmation admission and its proposed resource amendment were not modified or retried.

The later [second adaptive pilot](Tower-Adaptive-Racing-Pilot-02.md) has a distinct declaration, admission and fresh exclusion history that includes this first attempt's permanently reserved values. It completed both scientific audits and reached `AbandonThisConfiguration`. The historical admission described here remains closed and cannot be reused.
