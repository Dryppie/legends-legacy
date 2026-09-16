# Completion versus allocation comparison: preflight stopped

15 September 2026. **Not ready for combat approval.** The controller and tests built, all **118 backend tests** and **four metric fixtures** passed, but the native preflight reached its time limit while traversing the seed-registry directories. No fresh seeds, control preparations or fights occurred. The failed package is preserved without a retry. Adoption remains Hold.

## What completed and what failed

The [frozen protocol](Tower-Group-Allocation-Comparison-Protocol.md) prepared a 44-team-per-policy comparison of group completion against category allocation. It retained shared captured gameplay inputs, fixed ordinal ability order, top-two screening, one finalist per policy and two fixed controls. Its proposed 45 fresh values and maximum 512 fights remain **unallocated and unexecuted**.

- The isolated controller built with zero warnings/errors. The isolated tests built with one existing `xUnit2031` warning in `BalanceHarnessCompositionSearchTests.cs:188`.
- **118/118 backend tests passed** through `build/run-tests.ps1`: 110 existing verified cases plus eight adapted comparison-controller cases. **4/4 Python metric fixtures passed**. Synthetic evaluators and engine guards only.
- The native `check` command received **59 seconds** within its 60-second phase allowance. The wrapper recorded the failed phase at **59.203 seconds**. `check.log` shows cancellation inside `TowerCompleteFamilyInputs.HistoryRegistry`, called before loading the reservation union or preparing either control.
- The preceding completion comparison's successful preflight took **77.781 seconds**. Setting this preparation's per-command cap below that known duration was a preparation error. The directory traversal is the observed stopping point; no phase profile was saved, so its full runtime or timing share is unknown.
- The timeout wrapper's `taskkill` attempt reported `Access denied`; the native cancellation token then produced `OperationCanceledException`, and the wrapper waited for the child to exit before recording failure. No process was intentionally left running.
- Only `check/started.json` was created by the preflight. The independent preparation audit and normal success publisher were **not run**. Therefore matched live fixture definitions, current registry completeness, two prepared controls, 66 interval fixtures and the new independent schedule/trace audit are **not verified in this package**. Earlier sealed allocation verification remains separate evidence.
- All **15,090 indexed files across 37 predecessor packages** matched before/after. Frozen owned source and unrelated dirty files retained their captured hashes. The existing **482,596 reservations** were preserved; their fresh full-registry membership check did not finish.

## Measured cost and preserved evidence

| Phase | Seconds | Category |
| --- | ---: | --- |
| bootstrap | 3.422 | Diagnostic |
| build | 3.500 | Compilation |
| freeze | 0.140 | Diagnostic |
| metrics-tests | 0.203 | Diagnostic |
| setup | 0.750 | Diagnostic |
| test-build | 1.891 | Compilation |
| tests | 5.984 | Diagnostic |
| check (failed) | 59.203 | Diagnostic |

New diagnostic time before evidence closure: **69.702 seconds**. Cumulative before closure: **1283.629 seconds**. Compilation: **5.391 seconds**, recorded separately. New output before closure: **71.20 MiB**, plus **1,664,956,540 carried bytes**. The [completion receipt](../TestResults/balance/tower-group-allocation-comparison-preparation-20260915/control/completion.json) includes measured closure and a conservative one-second receipt/seal allowance; `preparation-files.json` seals the failed evidence package and does **not** certify readiness.

The cumulative ceilings remain 1,800 diagnostic seconds and 4 GiB. No older caps changed. Zero retries, fresh seeds, combat or replays were used. No historical results were altered or pooled.

## Exact commands and unresolved work

These commands describe the one attempt already made. Do not rerun the sealed paths.

```powershell
$comparison = 'TestResults/balance/tower-group-allocation-comparison-preparation-20260915'
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python -B "$comparison/freeze.py"
& $python -B "$comparison/workflow.py" bootstrap
& $python -B "$comparison/workflow.py" build
& $python -B "$comparison/workflow.py" test-build
& $python -B "$comparison/workflow.py" tests
& $python -B "$comparison/workflow.py" metrics-tests
& $python -B "$comparison/workflow.py" check  # Timed out; no retry.
& $python -B "$comparison/close_failure.py" # Evidence closure only.
```

`workflow.py tests` invokes `build/run-tests.ps1 -NoBuild` against isolated artifacts; `control/tests-command.json` preserves the exact filter and `control/tests.trx` preserves all outcomes. Builds used cached metadata with `--no-restore`. All producing inputs remain frozen. `audit`, `publish`, `bind`, `run`, `verify` and `audit-execution` were not run because the preflight failed, not because of missing seed authorization during preparation.

Next resolve the complete seed-registry traversal's preparation cost and cancellation behavior before proposing another combat launch. Any replacement diagnostic needs its exact work and time allowance frozen first, must respect the no-retry boundary, and must retain all reservation membership, hash and new-ledger checks. Do not skip that check, reuse incomplete output, extend the failed package's limit or allocate fresh seeds. This report does not authorize a replacement run.

Changed files: the frozen comparison protocol, this failure/readiness review, six active Markdown handoffs and the isolated preparation evidence package containing adapted controller/tests/audit/metrics/workflow and copied verified source. No game source/content, defaults, configuration or migrations changed; no deployment implications. No comparative-strength or speedup result was obtained. Historical reliability **Fail 1/3**, deep recovery **0/3**, sealed v19 **Unresolved**, its 253 recipes and unused 512 confirmation values, and adoption **Hold** remain unchanged.
