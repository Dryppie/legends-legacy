# Complete retained-family launch accounting: final gate stopped

Completed **15 September 2026** under the [frozen verification protocol](Tower-Complete-Family-Launch-Protocol.md). Target: offline `LL/tools/BalanceHarness`. **The candidate is not ready to launch.** Accounting and preservation checks passed, but final code review found a result-size defect before any combat occurred. The exact request is blocked by a durable post-check failure marker. No full-study approval is requested for this executable.

## What was implemented

`TowerCompleteFamilyLaunch.cs` adds a separate request contract and check/run/verify adapters. It preserves the original reservation protocol and all ten initial study files. The launch request binds the captured executable, full producing inventory and completed reservation observer files. Only the harness may differ from the original execution identity; captured gameplay DLLs, runtime, OS and architecture remain bound. Raw request bytes are hashed from the same locked stream used for parsing.

`TowerCompleteFamilyInputs.cs` retains strict legacy identity checks and adds an internal identity context for the explicitly verified original reservation. `TowerCompleteFamilyRun.cs` accepts the tightened setup/time/storage envelope and records the launch request hash in its durable start; verification checks the same context. Its selection, combat preparation, batch loop, durable attempt charging, cancellation and archive reconstruction stay shared. `Program.cs` adds the three launch entry points. `BalanceHarnessTowerCompleteLaunchTests.cs` adds 38 zero-combat cases covering budgets, tampering, captured identity, start accounting, bounded control writes and interruption.

There are no gameplay, content, migration, configuration or deployment changes. Unrelated dirty-checkout work is preserved. Seven active handoff documents and this protocol/review record the final blocked state.

## Measurements and checks

| Check | Measured result |
| --- | --- |
| Repository test wrapper, including isolated build | **146 passed, 0 failed, 0 skipped**; 36.407 seconds |
| Captured host build | Exit 0; 3.282 seconds; four gameplay DLL hashes unchanged |
| Real-input launch check, external wall time | **57.703 seconds**, exit 0 |
| Native instrumented operation | 57.502 seconds; detailed trace persisted |
| Independent accounting/preservation audit | **57.032 seconds** |
| Predecessor preservation | **12 packages / 14,029 indexed files** |
| Reservation registry | **147 paths / 481,891 values**, including original unused 512 |
| Initial study | **10 files / 16,662,786 bytes**; hashes and creation/modification timestamps unchanged |
| Checkout | **4,307 baseline files**; no observed concurrent UI change |
| Preparations / fights / new values / retries / resumes | **0 / 0 / 0 / 0 / 0** |

The [native trace](../TestResults/balance/tower-complete-family-launch-20260915/control/check-result.json) records detailed timings. Source binding took 18.113 seconds inclusive; nested timings must not be added to that total. The existing performance result remains **622.54x incremental accounting / 4.50x sixteen-write lifecycle at 9,216 archives**. This launch-accounting check supplies no new whole-run speedup or throughput estimate. Test and captured builds ran concurrently, so their timings are verification costs rather than isolated performance comparisons. The test build reported 34 existing compiler/analyzer warnings and zero errors.

## Exact accounting carried forward

Original protocol hash remains `c0b8ccab56ba01002e2d4bd8268a587329b07d7605348117f5a7354af4fba003`. Its base remains **5,391 files / 1,559,084,956 bytes / 2,117.2720002000005 seconds**. The added inventory has **1,396 files / 399,610,603 bytes**, including the complete sealed producing package and prior reservation observer evidence. New producing setup contributes **100.314 seconds**.

The extension reserves an additional **2,400 seconds and 64 MiB upfront** for later checks, audit, reporting, run administration and standalone verification. Within 64 MiB, 62 MiB covers control files and 2 MiB covers whole changed Markdown. Control-record publication retains a 64-KiB failure reserve. This avoids repeatedly rewriting the immutable protocol or omitting future observer files.

For this candidate, effective setup is **4617.586000 seconds** and available native study storage is **66,693,672,313 bytes**. The outer limits remain **2,434,784 attempts / 86,400 seconds / 68,719,476,736 bytes / zero retries**. These are accounting results, not execution authorization. The diagnostic total, including conservative 120-second setup/closeout allowance, is **280.470 seconds**, below 1,800 seconds; final byte totals are in `control/final-verification.json` and remain below 4 GiB. No resource limit was reached.

## Why the final gate failed

`Run` and `Verify` both pass the complete `TowerCompleteAssessment` into `Operation.Complete`. That method calls `PublishControl`, which permits at most **65,536 serialized bytes** per record. The required **43,879 cell hashes alone contain 2,808,256 characters**, before any other fields or JSON punctuation. Therefore the adapter would reject its result after the shared controller had completed the expensive study. The 146 tests passed because they did not exercise a full-size assessment through the outer result adapter.

The [final review finding](../TestResults/balance/tower-complete-family-launch-20260915/control/final-review-failure.json) records the defect. A separate `check-failure.json` explicitly identifies **PostCheckCodeReview** and preserves the earlier successful native input result. The run adapter checks for this marker before loading or entering the study controller, so this request now fails closed without combat. Neither the study nor the sealed producing files were rewritten. No corrective diagnostic or repeated build/test/check was attempted under this frozen scope.

The remaining implementation is small but required: publish a compact control summary with outcome, counts and hashes of `assessment.json` and the completed inventory; keep the full per-cell assessment in the study archive. Exercise both run and verify receipts at the full 43,879-cell size, including size bounds and tampering checks, in a separately frozen zero-combat regression scope. Then build/check a new captured request. Do not retry or resume this sealed request.

## Reproducible command record

From the repository root, these commands completed once:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$package = 'TestResults/balance/tower-complete-family-launch-20260915'
& $python -B "$package/producing/workflow.py" freeze
& $python -B "$package/producing/workflow.py" tests
& $python -B "$package/producing/workflow.py" build
& $python -B "$package/producing/workflow.py" request
& $python -B "$package/producing/workflow.py" native
& $python -B "$package/producing/workflow.py" audit
& $python -B "$package/control/close-stopped.py"
```

The test phase invokes **`build/run-tests.ps1` through PowerShell 7**, using the exact 146-case filter in `producing/test-selection.json` and isolated `producing/test-artifacts`. The native phase invokes the captured DLL with `tower-complete-family-launch-check control/launch-request.json`. Exact argument arrays and exit codes are persisted in the corresponding `*-command.json` and `*-result.json` files. These commands are historical records, not rerun instructions for existing output.

The successful-closeout `workflow.py seal` command was **not run** because final code review failed; `close-stopped.py` records and seals the stopped disposition instead. Full-study run, completed-study verification, reserve-bind, preparation and all combat commands were deliberately not invoked. No requested executed command was blocked by permissions; existing NuGet configuration access was approved for the isolated builds.

Request SHA-256: `a5879c4c684387fb5ed2aa82ceff359eea4f9d68c99ff52b3ddfc0725bd180fe`. Producing seal: `c88f69a4fb9f22932c932ebab8beb44c583064782c30f5f69c7c2315b4a0ed1a`. The full stopped package receives its own final evidence seal after this report is copied.

Reliability remains **Fail 1/3**, adoption **Hold**. V19 itself remains **Unresolved**, retains all **253 required recipes** and contains no confirmation. Full-family second-stage capacity, combat across the 256-cell boundary and full-study throughput remain unverified. No Kharad tuning, gameplay change, deployment or old cap increase occurred.
