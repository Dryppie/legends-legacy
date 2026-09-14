# Discovery accounting: measured parity closure

Completed **14 September 2026** in the offline `LL/tools/BalanceHarness`. **The opt-in accounting implementation's bounded engineering gate is now closed:** all **34 candidate fights** matched retained reference evidence, both detailed replays matched, the completed campaign reconstructed without combat, and nested accounting passed at **9,216 existing archives**. **129 relevant tests passed.**

This was the single execution of a [separately frozen closure protocol](Tower-Discovery-Parity-Closure-Protocol.md). The [earlier review](Tower-Discovery-Performance-Review.md), its stopped 42-fight diagnostic and the sealed v19 experiment remain unchanged. The broader 5× lifecycle target remains unmet; no whole-study speedup or balance acceptance is claimed.

## Performance evidence and decision

The original matched sixteen-write fixtures established the growing-prefix bottleneck:

| Existing archives | Legacy median | Owned median | Incremental ratio |
| ---: | ---: | ---: | ---: |
| 0 | 7.688 ms | 4.036 ms | 1.91× |
| 1,024 | 421.218 ms | 7.080 ms | 59.50× |
| 4,608 | 1,750.846 ms | 5.521 ms | 317.14× |
| 9,216 | 3,424.920 ms | 5.502 ms | **622.54×** |

These are retained [original measurements](../TestResults/balance/tower-discovery-performance-20260914-implementation/scaling-summary.json), made before the final writer-lease correction. At 9,216 archives, the broader sixteen-write lifecycle, including initialization and final audit, was **69.322 s legacy versus 15.421 s owned: 4.50×**, below 5×. Hash initialization/audits dominated the short owned lifecycle and remain required. The legacy fixture's final audit measured size; owned accounting additionally established/checked hashes.

The closure measured the corrected executable with a richer nested fixture: actual campaign and batch leases containing neutral bytes, two growing journals, parent/child checks, pending/final publication and multiple mandatory audits. Every fixture had sixteen new eleven-file archives. [Individual samples and counters](../TestResults/balance/tower-discovery-performance-20260914-parity-closure/scaling-summary.json) are retained.

| Existing archives | Median write/check/seal | Nearest-rank p95 | Sample file visits | Sample directory visits |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 10.198 ms | 13.948 ms | **864** | **480** |
| 9,216 | 11.720 ms | 14.995 ms | **864** | **480** |

At the larger scale, preparation took **52.722 s**, initialization **11.049 s**, all sixteen writes **0.195 s**, and finalization **32.803 s**. Finalization includes repeated child/parent hash audits and independent byte/inventory checks. These bounded full scans grow with total archive size; candidate checks do not revisit the growing closed prefix. No new legacy run was scheduled, so this different fixture does **not** supply a new paired speedup ratio. With sixteen samples, the stated p95 is the maximum, not an estimate of production tail latency. Cache state and other machine activity were uncontrolled; no cache flush or favorable-result repetition was used.

Decision: the original incremental target is amply demonstrated, final-code scaling is supported by deterministic visit counts, and exact outcome/durability checks now pass. That is sufficient to complete this opt-in engineering increment. It does not establish the broader 5× lifecycle target or a revised v19 runtime. Full audits are the next measured bookkeeping cost if further optimization is needed; removing them is not justified. Historical v19 still took **292.69 minutes**, including **283.63 minutes of discovery**. Its missing detailed trace cannot be reconstructed into historical timing percentages.

## Exact gameplay, search and completion checks

The [frozen definition](../TestResults/balance/tower-discovery-performance-20260914-parity-closure/diagnostic-definition.json) reuses the completed reference definition verbatim: two saved recipes, eight existing seeds each, one worker, two passes, then two detailed replays. No reference combat was rerun. The isolated compiler referenced retained reference assemblies directly; execution copied its runtime dependencies and replaced only the harness DLL/PDB. Preflight verified identical Application, Common, Domain and Services.LL DLL hashes, runtime/OS/architecture, content, settings, recipes, schedules and prepared mode. The [freeze receipt](../TestResults/balance/tower-discovery-performance-20260914-parity-closure/freeze-receipt.json) binds source and inputs before execution.

This is the **previous diagnostic's retained dirty-checkout gameplay scope**, not v19 gameplay equivalence. Four unrelated baseline files changed concurrently during closure; [their receipt](../TestResults/balance/tower-discovery-performance-20260914-parity-closure/concurrent-checkout-changes.json) records them separately. They were preserved and did not enter the retained gameplay DLLs.

The [candidate report](../TestResults/balance/tower-discovery-performance-20260914-parity-closure/measured/candidate/performance.json) records:

- **34 started / 34 completed**, zero retry or uncommitted attempts. Its durable outer journal contains exactly 34 alternating start/completion pairs. The compact campaign separately charges exactly **32/32** trials; the other two are detailed replays.
- Four matching eight-fight full-report digest comparisons, covering both cases and both repetitions, plus two exact complete detailed-report hashes.
- Successful normal inventory/hash reconstruction of all four completed compact archives and their campaign under a no-combat guard. Both nested fixtures reconcile exact final bytes and leave no writer leases behind.
- Detailed exclusive/inclusive stage timings, durable journal/flush timings, storage visit counts, CPU, allocation and memory data. Candidate work took **5.681 s**, including audits, reconstruction and replays. Its 32 prepared simulations account for **3.222 s** of exclusive trace time. The reference fixed-case runner has a different lifecycle, so their elapsed times are not a matched campaign-speed comparison.

The earlier zero-combat reconstruction of all **9,216 v19 evaluations**, exact recipes, proposal/evaluation charges, rankings, shortlist and screened nominations remains verified evidence. [Source continuity](../TestResults/balance/tower-discovery-performance-20260914-parity-closure/search-source-continuity.json) confirms that the search/generation/selection code is unchanged from that measured source. This closure changed only diagnostic orchestration and reference-verification visibility; the accountant matches the previously tested lease correction.

## Budgets and preservation

The new command returned **0 in 123.586 s**, including final diagnostic inventory publication and verification. Its internal performance snapshot ends earlier at **107.067 s**; use the command receipt for the whole workload. The preflight took **0.756 s** and independent preservation checks **31.410 s**. The [resource snapshot](../TestResults/balance/tower-discovery-performance-20260914-parity-closure/resource-summary.json) records **60,475,450 measured bytes / 101,837 files**. New build/source/evidence output was approximately **503 MB** before final documentation/sealing, within its 1-GiB envelope. Both diagnostics together retain approximately **1.1 GiB**, comfortably below 4 GiB; the final receipt records the sealing-time bound. These are logical file bytes, consistent with existing infrastructure; filesystem allocation overhead was not separately measured.

Together, the old stopped diagnostic and this closure used **76 fights**, zero fresh seed values and zero combat retries, within the original 512-fight limit. Recorded measured execution and preservation work remains well below 1,800 seconds. Compilation and correctness tests are excluded from that workload clock. No failed diagnostic stage was retried.

The [preservation receipt](../TestResults/balance/tower-discovery-performance-20260914-parity-closure/sealed-preservation.json) independently matches **103,193 v19 campaign files**, **892 v19 work-package files**, and **104,971 prior diagnostic evidence files**, with no missing/extra/changed entries. All **480,707 distinct reservations**, including **512 unused confirmation values**, remain unchanged. Final evidence and budget checks are recorded in [final verification](../TestResults/balance/tower-discovery-performance-20260914-parity-closure/final-verification.json).

## Commands and changed files

Exact commands, from the repository root:

```powershell
$w = 'TestResults/balance/tower-discovery-performance-20260914-parity-closure'
dotnet build "$w/compiler/FrozenBalanceHarness.csproj" -c Release --artifacts-path "$w/build/frozen"
dotnet build "$w/compiler/FrozenBalanceHarness.csproj" -c Release --no-restore --artifacts-path "$w/build/frozen"

./build/run-tests.ps1 -Configuration Release -ArtifactsPath "$w/build/tests" -Filter 'FullyQualifiedName~BalanceHarnessTowerPerformanceTests|FullyQualifiedName~BalanceHarnessTowerCompact|FullyQualifiedName~BalanceHarnessTowerSearchPortfolioTests|FullyQualifiedName~BalanceHarnessTowerFeedbackBenchmarkTests|FullyQualifiedName~BalanceHarnessTowerStorageTests|FullyQualifiedName~BalanceHarnessTowerDiscoveryParityTests'

& "$w/freeze.ps1"
dotnet "$w/execution/BalanceHarness.dll" tower-discovery-parity-check --definition "$w/diagnostic-definition.json"
dotnet "$w/execution/BalanceHarness.dll" tower-discovery-parity --definition "$w/diagnostic-definition.json" --output "$w/measured"
& "$w/verify-preservation.ps1"
& "$w/finalize.ps1"
```

These are provenance commands, not permission to repeat combat or write into sealed evidence. Reproduction needs a new authorized protocol/output and explicitly charged repetitions; the freeze and run commands reject existing destinations. The compiler builds captured `measured-source` against retained gameplay assemblies. The second build bound that snapshot after the initial source compilation. PowerShell used `-DateKind String` when copying the reference definition to preserve timestamp offsets.

The initial sandbox build was denied access to the existing user NuGet configuration. Tool-level filesystem access resolved it; both isolated builds passed without warnings. The test wrapper returned **129 passed, 0 failed, 0 skipped**, retaining [the log](../TestResults/balance/tower-discovery-performance-20260914-parity-closure/tests.log) and [TRX](../TestResults/balance/tower-discovery-performance-20260914-parity-closure/tests.trx). The broader checkout build emitted 34 existing warnings. No required command remains blocked. New tests cover the full nested lease/journal/finalization fixture, constant visit counts, fixed fight/time/storage budgets, rejected expansion, cancellation and output reuse.

Closure source changes: new `TowerDiscoveryParity.cs`; CLI registration in `Program.cs`; reuse and completion/replay checks in `TowerDiscoveryPerformance.cs`; internal access to the existing strict reader in `TowerPerformanceComparison.cs`; and `BalanceHarnessTowerDiscoveryParityTests.cs`. Active README, performance/replication plans and discovery/acceptance handoffs point here. The original implementation review documents the accounting/instrumentation changes. Before/snapshot source hashes, a task-only source diff and concurrent-change receipts preserve the dirty-checkout boundary.

## Remaining scientific boundary

V19 remains **VerifiedCapacityExceeded**, all **253 recipes retained**, **zero confirmation fights**, reliability **Unresolved**, adoption **Hold**, ordinary/joint **NotRun / NotRun**. Next is the [gameplay-version audit and separate confirmation contract](Tower-Coverage-Replication-Plan.md#next-scoped-work). The proposed **129,536-fight confirmation remains unprepared and unstarted**. This work changed no gameplay/content, Kharad settings, old caps, migrations, production configuration, deployment or catalog/default adoption.
