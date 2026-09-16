# Complete retained-family controller — captured source verification review

Completed **14 September 2026** under the [frozen verification protocol](Tower-Complete-Family-Verification-Protocol.md). Target: offline `LL/tools/BalanceHarness`. Status **CapturedControllerSourceVerifiedParityPending**. The corrected **215-test** suite passed, the unchanged controller compiled against captured v19 gameplay, and its public source inspection verified **all 43,879 original scenarios**. An independent check passed the complete identity/capacity receipt and **all 290 interval rows**. **Zero combat preparations, engine fights, new balance reservations or diagnostic retries/resumes occurred.**

The [previous scope violation](Tower-Complete-Family-Controller-Review.md) remains a separate sealed result: eight unintended test fights and two resumes occurred there. This package does not amend or erase it. The old v19 study, its recipes, caps and unused confirmation reservations remain unchanged. Reliability **Fail 1/3**, adoption **Hold**.

## What was verified

The test filter explicitly excludes both cases of `Cancel_resume_reconstructs_selection_and_committed_work_without_accepting_tampering`. The eight selected classes and their helpers were inspected; selected controller loops use synthetic writers and accounting events, and retained-audit fixtures inject synthetic participant data. The [frozen 215-name allowlist](../TestResults/balance/tower-complete-family-verification-20260914/test-selection.json) matches the [complete passing TRX](../TestResults/balance/tower-complete-family-verification-20260914/tests.trx) exactly. It includes all **52 new complete-family controller tests**. No excluded case ran.

The producing executable uses the completed UTC proof's captured harness source plus the unchanged `TowerCompleteFamily.cs`, `TowerCompleteFamilyInputs.cs` and `TowerCompleteFamilyRun.cs`. A dedicated inspection-only entry point invokes the actual public `TowerCompleteFamilyInputs.Inspect` once. All three implementation files match the reviewed checkout byte-for-byte. Application, Common, Domain and Services.LL DLL hashes match the captured v19 runtime; no gameplay assembly was rebuilt for this diagnostic. The entry point exposes no preparation or execution route.

The native path verifies the sealed UTC package and its **203 external input bindings**, strict original recipes/timestamp identities, content/settings, and the complete source inventory: **43,879 scenarios / 47,834 origins**. The independent receipt confirms unique inventory, legacy, recipe and UTC cell identities, **one context**, **580 anchor reasons / 560 anchors**, with **327 historical** and **253 midpoint** reasons overlapping on **20 cells**. The canonical ordered-cell hash is `c060362a00de3bc2e73766859f5df59b2d16258512b32974c7847244b785d521`.

Whole-family arithmetic remains **43,319 first-stage cells × 32 = 1,386,208 fights**, with at most **4,096 × 256 = 1,048,576** second-stage fights, totaling **2,434,784** for a separately bound future study. Its capacity leaves **3,536 unresolved non-anchor places**. The first stage clears only **0–1 wins / 32**; at maximum second-stage multiplicity, the joint viability/ceiling band is **48–91 wins / 256**. These are arithmetic checks, not combat outcomes or authorization to run that study.

## Measurements and commands

| Check | Measured result |
| --- | --- |
| Corrected backend wrapper | **42.604 s**, 215 passed / 0 failed / 0 skipped; build 18.58 s, test suite 21 s; 34 existing warnings / 0 errors |
| Captured controller build | **2.458 s** wrapper; zero warnings / errors |
| Public source inspection | **18.682 s** native; **18.875 s** complete guarded command |
| Independent receipt check | **1.734 s** verifier; **1.860 s** complete guarded command |
| Native CPU / peak working set | **19.750 CPU s / 1,361,014,784 bytes** |
| Cumulative managed allocation | **32,358,572,312 bytes**; this is allocation traffic, not retained output or simultaneous memory |
| Native output | **45,936 bytes**, including result and performance receipt |
| All 290 interval rows | Maximum absolute difference **5.527861501875009e-10**, tolerance **1e-8** |

The [performance receipt](../TestResults/balance/tower-complete-family-verification-20260914/native/performance.json) retains wall/CPU/allocation/peak-memory/GC counts and an existing `TowerPerformanceTrace` command scope. `Inspect` owns a private nested trace; nested source/hash percentages are unavailable. No whole-study speedup or combat throughput improvement is claimed. The earlier measured **622.54× incremental accounting / 4.50× sixteen-write lifecycle** results remain confined to the [performance review](Tower-Discovery-Performance-Review.md); this source-only command is not a comparable workload.

Executed from the repository root, using the package-local frozen runners:

```powershell
$package = 'TestResults/balance/tower-complete-family-verification-20260914'
& "$package/run-tests.ps1"
& "$package/build-captured.ps1"
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python -B "$package/diagnostic.py" freeze
& $python -B "$package/diagnostic.py" native
& $python -B "$package/diagnostic.py" independent
```

`run-tests.ps1` invokes the required `build/run-tests.ps1` with the exact filter in the protocol and isolated artifacts. `protocol.json` pins all producing files and the fully expanded native/independent commands. The [independent comparison](../TestResults/balance/tower-complete-family-verification-20260914/interval-comparison.json) retains every interval and its error. Execute-once markers prevent repeating diagnostics in this package. To reproduce separately, copy its test/build/diagnostic runner scripts and verifier, compiler entry point and project, reviewed and captured sources, lexical helper, baseline, test-selection and test-filter files into a **new** output root; update the copied project's captured-source path to that root, retain the fixed captured gameplay references, run the same sequence there under a new frozen scope, and preserve this package unchanged. No reproduction was run here.

## Remaining boundary and changed files

The captured source gate is complete. The production full-family preparation path, compact-batch adapter and complete execution/reconstruction path still lack captured orchestration parity. The injected-loop tests do not establish those properties. A separately frozen bounded parity diagnostic is the next engineering gate; it must specify exact existing diagnostic values, cases, repetitions, output and attempt charging before any execution. No fresh schedule, 288-value reservation, confirmation launch or gameplay application follows from this result.

Actual second-stage selection capacity remains unproven. More than 3,536 unresolved non-anchors must stop Inconclusive without trimming recipes or enlarging caps. Complete-study throughput, memory behavior during preparation, practical acquisition, other floors and current-checkout gameplay remain outside this result. The native source inspection's substantial cumulative allocation is recorded; it was not optimized or extrapolated into full-study memory demand.

This task changes documentation only in the checkout: this review, its new frozen protocol, and seven active handoffs (discovery implementation/plan, acceptance policy, coverage plan, strategy reset, retained-family confirmation design, harness README). Producing runners, captures, test/build output and measured evidence reside in the new ignored TestResults package. No implementation, tests, gameplay/content, old cap, migration, dependency, shared configuration or deployment change was made this turn. All authorized verification commands ran; combat preparation/execution and confirmation were deliberately outside this scope.

## Final preservation and accounting

The **11.078-second** preservation audit checked **4,224 baseline checkout files**, all **117 prior reviews**, all **1,494 UTC-package files**, all **2,069 preceding controller-package files**, the **203 external input bindings**, and all **185 producing files** pinned for this diagnostic. Every required artifact remains intact. Twenty-four unrelated frontend files changed concurrently; their paths are recorded, and those changes were preserved.

History membership remains **145 files / 102 distinct hashes**. Independently reconstructing their numeric-array union produces exactly **481,603 reservations**, matching the authoritative midpoint ledger and preserving all **512 unused original v19 values**. The shared TRX matched the captured result at the preservation boundary. No implementation source changed in this task.

Measured diagnostic phases total **32.969 seconds**. Adding the conservative **60-second static-read allowance** and full **120-second sealing allowance** charges **212.969 seconds / 3.55 minutes**, below 30 minutes. Correctness tests and captured compilation are separately timed above. Their output, all captures/receipts, final documentation copies, whole changed checkout files, shared TRX and a 2-MiB metadata allowance total **less than 0.4 GiB**, within 4 GiB. Native output is 45,936 bytes, within its 1-GiB sublimit; allocation traffic and peak memory are separate measurements.

The package's `final-verification.json` records exact charges and remaining gates. `updated-files.json` and `final-documents/` bind the final handoffs; `diff-check.json` records scoped whitespace verification. Its `evidence-files.json` is written last, after new Markdown link checks, with actual sealing time retained in `seal-timing.json`. No further test, preparation, source inspection or combat follows that seal.
