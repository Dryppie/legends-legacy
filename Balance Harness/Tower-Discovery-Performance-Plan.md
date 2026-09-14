# Tower discovery performance: implementation and handoff plan

Prepared **14 September 2026** after investigating the v19 discovery slowdown. Target: the offline `LL/tools/BalanceHarness` in the primary LL service. This is an implementation plan, not a frozen combat protocol or permission to repeat a closed experiment. No performance fix has been implemented by this planning task.

**Execution update — 14 September 2026:** the [implementation review](Tower-Discovery-Performance-Review.md) retains the measured 622.54× incremental and 4.50× sixteen-write lifecycle improvements and the stopped 42-fight diagnostic. The separately frozen [parity closure](Tower-Discovery-Parity-Closure-Review.md) now completes all 34 candidate fights, both exact detailed replays, durable completed-campaign reconstruction and nested accounting at 9,216 archives; **129 tests pass**. The engineering completion gate is closed for the explicit opt-in ownership contract. The broader 5× lifecycle target and whole-run throughput remain unestablished; mandatory audits remain. Neither diagnostic changes v19, its reservations or balance conclusions.

## Objective and priority

Remove the discovery harness's growing filesystem overhead while preserving deterministic search, combat outcomes, durable attempt accounting, resource limits and archive verification. Demonstrate the improvement at the **9,216-candidate archive scale**, rather than relying on a tiny empty-directory benchmark.

This work precedes another large discovery or confirmation run. The subsequent scientific task remains separate confirmation of all 253 retained v19 recipes, after resolving gameplay-version compatibility. Do not start that 129,536-fight study during this performance task.

## Evidence already established

| Observation | Evidence and interpretation |
| --- | --- |
| V19: 86,016 fights in 17,561.34 seconds | [Completed review](Tower-Search-Portfolio-Review.md). Discovery used 73,728 fights and 17,017.91 seconds (283.63 minutes); six screens used 12,288 fights and 535.07 seconds (8.92 minutes). Discovery accounts for 96.91% of execution time. |
| One archive per eight-fight discovery candidate | 9,216 candidate archives contain 101,376 files: 11 files per completed archive, according to the sealed [final file inventory](../TestResults/balance/tower-search-portfolio-20260914/final-files.json). |
| Repeated scans of the growing campaign | Captured [TowerBulkCampaign](../TestResults/balance/tower-search-portfolio-work-20260914/before-source/LL/tools/BalanceHarness/TowerBulkCampaign.cs) calls `CheckStorage` before each batch and from its chunk-completion callback. `StorageBytes` recursively enumerates the full campaign. The eight-trial discovery batch contains one chunk. These two checks imply roughly **934 million file visits** across discovery, excluding other checks and scans. This is an estimate from the archive layout, not a recorded I/O counter. |
| Additional campaign-wide checks | Captured [TowerFeedbackBenchmarkRun](../TestResults/balance/tower-search-portfolio-work-20260914/producing-source/LL/tools/BalanceHarness/TowerFeedbackBenchmarkRun.cs) checks the complete outer package every 128 fight starts, as well as at other boundaries. Fixing only the inner check leaves another growing scan. |
| Per-candidate time grew markedly | Differences between consecutive saved `bulk-manifest.json` modification times give a median of **0.1787 seconds** for the first 128 gaps and **3.5459 seconds** for the last 128. The monotonic timestamp span is 16,999.59 seconds, consistent with the discovery phase metric. These are filesystem timestamps, not instrumented candidate timers. |
| Doubling discovery work almost quadrupled time | V18 discovery used 36,864 fights in 4,414.55 seconds; v19 used twice as many fights in **3.855 times** the time. Different recipes and machine load prevent treating this alone as causal proof. |
| A full scan is expensive | A read-only C# reproduction of the captured directory enumeration took **2.1776 seconds** over 101,429 files and 55,313 directories in the completed discovery tree. This is one present-day measurement, not the historical average or a promised speedup. |

The code and growth pattern strongly implicate repeated filesystem traversal. The old run saved phase totals but did not persist the active trace's detailed timing snapshot. Therefore, do not claim an exact historical percentage for storage checks, simulation, preparation, compression or durable writes. Other machine activity may also have contributed.

## 1. Establish the version and preserve existing work

Read repository and applicable directory instructions, then inspect `git status` before editing. This is a shared dirty checkout containing substantial unrelated gameplay, content, UI and harness changes. Do not reset, revert or overwrite them.

Compare the current relevant source against the captured producing/before-source snapshots. Inspect the [concurrent-checkout receipt](../TestResults/balance/tower-search-portfolio-work-20260914/concurrent-checkout-changes.json) and [final preservation receipt](../TestResults/balance/tower-search-portfolio-work-20260914/final-preservation.json). The earlier observation of ten changed content files and five binaries is a point-in-time record; recheck rather than assuming the list is still complete.

Freeze one explicit reference version for the performance comparison. Reference and candidate must use the same content, gameplay assemblies, recipes, seeds, settings and execution mode; only the intended harness changes may differ. Prefer the captured gameplay scope when practical. If the current checkout is used, record it as a new diagnostic scope and do not claim v19 gameplay equivalence. Use an isolated diagnostic build/output if ordinary compilation would disturb another task's binaries.

Read the v19 packages in place; never modify them. Put new evidence in a separate, uniquely named `TestResults/balance/tower-discovery-performance-*` directory. Preserve before/after source hashes and command outcomes there. Never use hard links from writable fixtures to sealed files.

## 2. Make the costs observable

Reuse [TowerPerformanceTrace](../LL/tools/BalanceHarness/TowerPerformanceTrace.cs) and [TowerPerformanceBenchmark](../LL/tools/BalanceHarness/TowerPerformanceBenchmark.cs). Instrument the actual adaptive-discovery/bulk-campaign path; the existing fixed-case benchmark alone does not reproduce thousands of candidate archives.

Persist diagnostic trace snapshots on success and failure in the new workflow, keeping legacy serialization and sealed manifest rules compatible. Measure separately:

- Candidate construction/ranking and reusable combat preparation.
- Combat simulation, excluding archive work where instrumentation permits.
- Attempt-journal writes and durable flushes.
- Serialization/compression, chunk publication and immediate verification.
- Inner and outer storage accounting: calls, directories/files visited and elapsed time.
- End-to-end time, process CPU, allocations/peak memory, file count, bytes and batch latency by position.

Use exclusive timings for breakdowns; nested inclusive values must not be added together. Record cold/warm conditions and competing machine activity where observable. Profiling must not consume random values, change candidate order or influence selection.

## 3. Remove repeated full-tree work without weakening correctness

Start with storage accounting, not parallel search or a new archive format. The preferred design is a small campaign-owned accountant that tracks known writes and newly published files, with explicit full audits at declared lifecycle boundaries. Assess both the inner `TowerBulkCampaign` checks and outer `TowerFeedbackBenchmarkRun` checks.

Before coding, map every relevant write path and ownership boundary. Account for setup content/executables, nested batches, journals, metadata, replacement files, pending/compressed writes and final inventories. Parent totals must include child outputs exactly once. Reservations and reconciliation must remain correct when creation, compression, rename or publication fails or cancellation interrupts a write.

Keep the following guarantees:

- Resource caps remain enforced at the declared boundaries; an underestimated cache cannot publish an oversized package as valid.
- Attempts remain durably charged before combat; no retry or lost-attempt accounting is introduced.
- Existing files, unknown files, hidden/pending artifacts and reparse points retain appropriate validation. A writer lease does not prevent unrelated processes from changing files.
- Completed archives still undergo exact inventory/hash verification. Verification must not trust an execution cache as evidence of integrity.
- Reopening reconstructs state from durable evidence. Historical contracts that forbid resume remain non-resumable.
- Search streams, recipes, evaluation charges, ranks, nominations, full-family retention and combat/report semantics remain unchanged.

Do not silently relax an existing guarantee about externally modified files or storage-check timing. If the optimized accounting requires a different ownership/validation contract, define and test it explicitly in a versioned opt-in execution path while retaining the historical verifier. Do not simply remove checks, raise resource caps, sample directory entries or defer all accounting until completion.

Keep archive layout, per-fight durability and parallelism unchanged in the first optimization unless profiling demonstrates they must be addressed. Treat any later format/durability/concurrency change as a separately justified increment with compatibility tests.

## 4. Prove scaling with a bounded diagnostic workload

First use zero-combat fixtures or saved-report callbacks with the same candidate/archive layout. Exercise empty/small, 1,024-, 4,608- and 9,216-candidate directory states. Measure a predeclared number of candidate writes/checks at each state. Include setup and final audit costs separately. Do not recreate the original five-hour sequence merely to rediscover the bottleneck.

Use counters to establish that full-tree enumeration is no longer performed per candidate, chunk or 128-fight interval. Total bookkeeping work should grow approximately with files written plus a bounded number of full audits, rather than the sum of every historical archive prefix. Report median and tail batch latency at each scale. A practical target is at least **5x less bookkeeping time at the largest scale**; this is a target to measure, not a claimed result or a reason to repeat runs until a pass appears.

Then run a small paired real-combat diagnostic using existing saved recipes and seed values. Compare exact outcomes/report digests under the same frozen gameplay inputs. Label these as performance/parity repetitions; never count them as fresh balance evidence or feed their outcomes back into discovery.

Proposed limits to freeze in a diagnostic definition before running it:

- **Zero fresh balance seeds and zero new candidate-search studies.**
- **At most 512 actual diagnostic fights total**, including reference, candidate, every repetition and detailed replay; zero retries. Prefer fewer. Account for the existing benchmark's repetitions, worker counts and replay costs before launch.
- **At most 30 minutes for the complete measured diagnostic workload**, including fixture preparation and verification, excluding compilation and correctness tests. Existing performance invocations keep their own maximum of 900 seconds; do not raise their cap.
- **At most 4 GiB of new retained diagnostic output/fixtures in total.** Reproduce the directory shape with small neutral fixture data when payload size is irrelevant to the filesystem measurement.
- Declare all scales, cases, ordering, repetitions and limits before measuring. If a cap is reached, preserve the partial result and report the unresolved measurement; no automatic extension or repeated search for a favorable timing.

These are proposed diagnostic limits, not an already prepared protocol. The new task must record exact schedules and input hashes before execution.

## 5. Verify behavior and compatibility

Run backend tests through the repository wrapper. The initial relevant suite is:

```powershell
./build/run-tests.ps1 -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerPerformanceTests|FullyQualifiedName~BalanceHarnessTowerCompact|FullyQualifiedName~BalanceHarnessTowerSearchPortfolioTests|FullyQualifiedName~BalanceHarnessTowerFeedbackBenchmarkTests'
```

Extend the filter if additional changed contracts need coverage. Test meaningful failure cases: cap boundary and overshoot, failed/partial publication, pending writes, journal accounting, restart reconstruction where supported, unexpected/modified artifacts, reparse points, nested accounting, manifest tampering and cancellation. Test old archive reconstruction and deterministic candidate/nomination parity using saved measurements with a no-combat guard. Do not introduce timing-sensitive CI assertions; use deterministic operation counters for the scaling regression and retain wall-clock measurements as diagnostic evidence.

The prior 388-test result applies to the original producing version, not whatever is now checked out. Report new commands and their exact scope. If the Windows sandbox blocks access to the user NuGet configuration, obtain the required tool-level filesystem access; do not change package managers or rewrite NuGet configuration as a workaround.

## 6. Deliver a decision and a usable handoff

Deliver scoped code/tests, a performance review with before/after measurements and limitations, reproducible diagnostic commands, and updated active Markdown. Preserve frozen plans, reviews, source packages and seed ledgers. Report any incomplete or blocked checks and distinguish concurrent edits from this task's changes. No migration, production configuration change, deployment or optimizer/content promotion is intended.

Completion requires unchanged verified outcomes/accounting, historical reconstruction, measured full-scale bookkeeping improvement, and no per-candidate full-tree traversal in the optimized path. If improvement falls short, identify the next measured bottleneck rather than claiming success from a tiny fixture or increasing the combat budget. Any whole-run runtime estimate must be labeled as an estimate and include setup, simulation, publication and final verification costs.

After the performance decision, return to the [253-recipe confirmation handoff](Tower-Coverage-Replication-Plan.md#next-scoped-work). V19 remains **VerifiedCapacityExceeded**, reliability **Unresolved**, adoption **Hold**, ordinary/joint confirmation-family assessment **NotRun / NotRun**. All **480,707 reserved values**, including 512 unused v19 confirmation values, remain excluded from any later fresh balance study. This performance task does not prepare or execute that confirmation.
