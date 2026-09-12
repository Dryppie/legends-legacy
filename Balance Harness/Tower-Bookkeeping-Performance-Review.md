# Tower archive bookkeeping: bounded performance follow-up — 12 September 2026

**The targeted overhead is reduced; whole-campaign speedup is not established.** Full scans of the existing large archive are **77.02% faster** in the matched measurements. Validating 256 teams against 467,307 exclusions is **78.43% faster** with **96.16% less allocation**. The small four-team campaigns were **5.01% slower overall** in these samples. All **520 diagnostic fights** completed, full outcomes and old detailed replays matched, and **289 relevant tests passed**.

This increment targets only the offline `LL/tools/BalanceHarness`. Kharad's locally applied **Health 3.04881408 / Power 3.85370128**, the fixed gear/untrained-Essence budget, saved builds and the 10–50% acceptance rules are unchanged. The [checked application](Tower-Kharad-Checked-Application-Review.md) and [2,918-party confirmation](Tower-Staged-Confirmation-Review.md) retain their scopes. Competitive search quality remains **Fail**, and near-optimality is unestablished.

## What changed and why

The preceding staged run left **1,952.29 seconds** inside exclusive archive creation outside separately measured children. Source inspection suggested storage scans, journal work and shared-file handling. This follow-up measures those operations; it does not retroactively assign all that historical time to one function.

In [TowerBulkCampaign.cs](../LL/tools/BalanceHarness/TowerBulkCampaign.cs), every storage boundary previously enumerated paths, read each entry's attributes and later constructed another `FileInfo` to retrieve each file's length. The new scan uses metadata supplied by `DirectoryInfo.EnumerateFileSystemInfos` for that scan's attributes and lengths. The full tree is still enumerated at **every existing batch/chunk boundary**. It includes hidden files and pending writes, rejects reparse points and checks cancellation within the walk. The byte total uses checked arithmetic. No persistent byte counter, skipped scan, reduced storage cap enforcement or durable verification trust cache was introduced.

In [TowerBalanceEvaluator.cs](../LL/tools/BalanceHarness/TowerBalanceEvaluator.cs), each cell's `Intersect` constructed another hash set containing the entire exclusion history. The evaluator now builds one local set per validation call, uses it to reject duplicate history, and tests every cell's schedule against it. A later call rebuilds the set, so changes to a caller's list cannot bypass freshness checks. Family, cohort, seed, budget, recipe and acceptance checks remain in place. Staged validation benefits through its existing bounded ordinary-validation partitions; no statistical policy or experiment format changed.

Timing scopes were added for campaign scans, compact file enumeration, shared-content hashing, shared-file writes, attempt validation, append and durable flush. They use the existing opt-in `TowerPerformanceTrace`; nested inclusive times must not be added together. [TowerCompactResume.cs](../LL/tools/BalanceHarness/TowerCompactResume.cs) still writes and calls `Flush(true)` **before every battle**. [TowerCompactBundle.cs](../LL/tools/BalanceHarness/TowerCompactBundle.cs) has instrumentation additions only in this increment. The producing combat assemblies are byte-identical across all variants.

## Frozen workload

The [protocol](../TestResults/balance/tower-bookkeeping-profile-20260912/protocol.json) was frozen before benchmark combat. Baseline instrumentation and its binaries were preserved; the measured baseline identified the two optimization targets before their implementation. The optimized build was then frozen before its first run. The same diagnostic-driver binary was used for both versions.

| Component | Frozen allocation |
| --- | --- |
| Campaign cases | Four saved floor-5 parties: previous control, previous leader, current top observed team, and first canonical candidate-independent party |
| Seeds | Same 32 already-used second-stage seeds per party; diagnostic repeats |
| Run order | Baseline 1, optimized 1, optimized 2, baseline 2; separate process for each |
| Campaign fights | 4 × 32 × 4 = **512** |
| Detailed parity replays | Two per run = **8**, checked against sealed application/confirmation reports |
| Total cap / actual | **520 / 520** starts and completions; zero retries or lost attempts |
| Per-run time / total package cap | **120 seconds / 1 GiB**, cooperative checks; no automatic retry or resume |
| Validation microbenchmark | 256 canonical saved parties, 32 seeds, **467,307 historical exclusions** |
| Storage microbenchmark | Read-only scan of the real **18,769-file / 2,347,896,614-byte** completed campaign |
| Microbenchmark repetitions | One warmup plus five measured repetitions per operation and process |

The large archive was neither copied nor altered. Only 16 allowlisted content files were copied into the small diagnostic input root, with the existing minimal nonsecret Tower settings. No application configuration was copied. The selected recipes, exact schedules, provenance and both producing builds remain in the ignored [output package](../TestResults/balance/tower-bookkeeping-profile-20260912).

Total timed run phases were **49.35 seconds**, including microbenchmarks, campaign creation, full reconstruction and replay. Preparation, code changes, builds, backend tests, independent analysis and documentation are outside that timing; backend test simulations are outside the 520 diagnostic-fight accounting. The package retains approximately **241 MiB**, including producing binaries and diagnostic evidence, with the exact final byte count in the receipt. This is not the elapsed duration of the entire development task.

## Measurements

The [independent analysis](../TestResults/balance/tower-bookkeeping-profile-20260912/analysis.json) keeps every individual sample and warmup metric. Microbenchmark medians below use ten measured samples per version, clustered within two processes. Campaign and reconstruction medians use two samples per version. These are descriptive results from this Windows machine, not confidence intervals or an operating-system cold-cache guarantee.

| Operation | Baseline median | Optimized median | Observed time change |
| --- | ---: | ---: | ---: |
| Full accumulated-archive byte count | 624.43 ms | 143.51 ms | **77.02% less** |
| Strict 256-team family validation | 689.94 ms | 148.85 ms | **78.43% less** |
| Create and assess 128-fight campaign | 5.260 s | 5.523 s | **5.01% more** |
| Full no-combat campaign reconstruction | 513.01 ms | 477.86 ms | **6.85% less** |

Validation's median cumulative allocation fell from **2,276.20 MiB to 87.35 MiB** (**96.16% less**). Storage scanning fell from **28.94 MiB to 14.77 MiB** (**48.95% less**). These numbers describe allocation traffic, not retained disk space or simultaneous RAM usage.

The small campaign builds only four chunks, so it does not experience repeated scans of a large accumulated tree. Its complete timing did not improve: most of the approximately 263 ms difference is in the unmodified simulation stage, whose mean measured time rose by about 248 ms. These two samples per version do not establish the cause. The correct conclusion is a repeatable improvement in the measured metadata and exclusion operations, with **no established end-to-end campaign speedup**. Do not convert the microbenchmark percentages into a promise about the old many-hour workload.

In the first baseline campaign, the 128 attempt appends consumed **220.21 ms**, including **199.68 ms** in durable flushes. Shared-file handling consumed **20.01 ms**. Complete storage checks consumed roughly **15.84 ms** across the small campaign's six boundaries. The real accumulated-tree scan instead consumed around **624 ms** per check. This confirms that archive size affects scan overhead, while the per-attempt durability cost remains visible and unchanged. All timing paths are retained for further attribution.

## Correctness and integrity

Each of the four campaigns fully reconstructed without combat. Their **128 complete decompressed compact rows** match across versions, including seeds, participants' hashes, summaries, outcomes, duration and full-report hashes. Result digests are identical. All eight detailed old-report comparisons match, including event logs. The independent Python analysis verifies chunk and manifest hashes, exact inventories, ordered schedules and all durable journal rows. Assessments match after excluding only their definition and artifact hash fields, which necessarily reflect the different producing harness identities. This is technical parity, not a new win-rate sample or search-quality finding.

The required wrapper passed **289 tests**:

```powershell
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerStagedTests|FullyQualifiedName~BalanceHarnessTowerBulkTests|FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~BalanceHarnessTowerCompactTests|FullyQualifiedName~BalanceHarnessTowerPerformanceTests|FullyQualifiedName~BalanceHarnessJsonTests|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerBenchmarkTests|FullyQualifiedName~BalanceHarnessTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryRunTests|FullyQualifiedName~BalanceHarnessTowerBossStudyTests|FullyQualifiedName~BalanceHarnessTowerBossImprovementTests'
```

The [TRX receipt](../TestResults/balance/tower-bookkeeping-profile-20260912/regression-tests.trx) records zero failures or skips. Three added tests cover hidden/pending files, growth/deletion between authoritative scans, cancellation without returning a partial count, duplicate exclusions and mutations between validation calls. Existing tests cover storage caps before combat, partial campaign resume, durable charged attempts, changed content and corrupted evidence, deterministic replay, ordinary acceptance and staged selection. Backend tests ran before the optimized benchmarks, so those timed runs did not compete with the test process.

A separate [Windows junction smoke](../TestResults/balance/tower-bookkeeping-profile-20260912/junction-checks.json) verifies that both file inventory and byte counting reject root and descendant directory junctions: **four checks, zero fights**. Only the two temporary junctions were removed; their target file remains intact. No historical evidence was deleted.

Harness and diagnostic builds succeeded with zero warnings/errors. The complete backend test build succeeded with five preexisting unrelated warnings. Its first sandboxed attempt could not write the SDK-generated static-web-assets cache; approved local build access resolved that. Package-free diagnostic restores used empty sources and approved access to local NuGet configuration. No required command remains blocked. `git -c core.safecrlf=false diff --check` passed.

The [final verification receipt](../TestResults/balance/tower-bookkeeping-profile-20260912/final-verification.json) records immutable prior evidence, content/catalog hashes, source changes, unchanged combat assemblies, optimized producing files, test results and this review. Prior sealed application and confirmation reviews remain unchanged.

## Changed files and next work

Runtime scope is the four harness files described above. Regression changes are [BalanceHarnessTowerBulkTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBulkTests.cs) and [BalanceHarnessTowerBalanceEvaluatorTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBalanceEvaluatorTests.cs). Documentation changes are this review, the six active discovery/loadout/harness/policy plans and the harness README. Other preexisting working-tree changes were preserved. There are no game-content edits, catalog promotions, migrations, dependency additions, configuration changes, deployments or service restarts in this increment.

Next, freeze a small independent-search reliability experiment using the saved current leaders as controls and keeping supplied-team improvement separately labeled. Diagnose why independent generation misses combinations already known to work, and evaluate improvements on fresh held-out seeds without using confirmation outcomes as search fitness. Saved recipes can be loaded directly; no repeat of the old search is needed to recover them. Workload-level performance remains a prerequisite before another large campaign. Floors 6–11, post-calibration challenges on floors 2–4 and practical acquisition coverage remain open.
