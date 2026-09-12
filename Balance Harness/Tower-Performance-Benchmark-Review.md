# Bounded Tower performance benchmark — 12 September 2026

The first performance deliverable is implemented and measured. `tower-performance` completed **300/300 diagnostic battles in 23.71 seconds**, retaining **380.30 MiB of logical files**. All full report digests matched across repetitions and worker counts; all **12 detailed replays** matched. **195 relevant backend tests passed**, including 17 new performance tests. This establishes a reproducible measurement baseline; compact archives and execution optimizations remain next.

This work targets the offline `LL/tools/BalanceHarness` tool. It changes no game content, boss scaling, character budgets or statistical acceptance rules. The completed Kharad portfolio balance Pass and search-quality Fail remain separate historical findings.

## Command and frozen workload

```powershell
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll tower-performance --content-root LL/src/API/API.LL --output TestResults/balance/tower-performance-new
```

An optional `--definition <json>` replaces the [default fixture](../LL/tools/BalanceHarness/Fixtures/tower-performance.json). Output directories must be new. The default freezes four complete ten-character floor-5 recipes from the expanded Kharad portfolio, with the existing level-40 gear budget and five ordered, untrained Essences per character:

| Specimen | Selection evidence |
| --- | --- |
| Weak | Historical zero-win recipe; a first archived defeat took 561 ticks. |
| Strong | The retained leader whose historical final precision result was 47.67%. |
| Summon/status | A first archived battle recorded ten summons and 896 status applications. |
| Longer duration | A first archived battle took 1,229 ticks, the longest among the inspected top 24 and first eight zero-win recipes. This is not a tick-limit stress case. |

These are performance specimens, not universal team templates or new claims about current strength. Recipes are copied unchanged; diagnostic scenario names and an explicit eight-seed schedule are frozen separately. The tiny schedule produced five wins out of eight for two specimens. Those repeated diagnostic observations are not a fresh balance assessment and must not be pooled with acceptance samples.

The reservation is **4 recipes × 8 seeds × 3 passes × 3 worker configurations = 288 ordinary battles**, plus **4 detailed replays × 3 configurations = 12**, totaling **300**. There are no uncounted warmup fights. The default limits are **300 battles, 300 seconds overall and 2 GiB logical output**. Contract ceilings are eight cases, 32 seeds each, five passes, four worker configurations, eight concurrent cases, 2,000 total battles, 900 seconds and 4 GiB. Every repetition and replay is charged before execution.

The parent freezes the definition, seed ledger, settings, content and producing executable/dependencies. It materializes every case before launching combat. Each worker configuration runs in a separate retained-executable process, with a first pass and two warm repeats. Worker counts are maximum concurrent recipe counts, not separate application hosts or modified combat rules. Historical build catalogs are not updated by this command.

## Measurements

The [complete report](../TestResults/balance/tower-performance-20260912/performance.md), [timing JSON](../TestResults/balance/tower-performance-20260912/performance.json) and [extracted metrics](../TestResults/balance/tower-performance-review-20260912/metrics.json) retain the observations.

| Maximum workers | First process pass | Warm repeat 1 | Warm repeat 2 |
| ---: | ---: | ---: | ---: |
| 1 | 5.019 s | 2.058 s | 1.430 s |
| 2 | 3.090 s | 2.100 s | 1.664 s |
| 4 | 2.095 s | 1.804 s | 1.663 s |

Every cell measures the same **32 battles plus ordinary archive creation and strict verification**. Overall elapsed time also includes snapshot/executable retention, preflight, process startup, monitoring and detailed replays. The filesystem cache is uncontrolled; configurations run in the declared order. These observations do not establish a winning worker count, an optimization speedup or a throughput estimate for a 600,000-battle campaign. On this small workload, additional workers did not consistently improve warm throughput.

The two warm single-worker passes account for 64 battles and approximately 3.481 seconds of instrumented exclusive time:

| Stage | Exclusive time | Share |
| --- | ---: | ---: |
| Combat execution, including playback checkpoints | 1,259 ms | 36.2% |
| Canonical JSON hashing | 806 ms | 23.2% |
| File hashing, including reads | 437 ms | 12.5% |
| Input materialization, excluding nested measured work | 311 ms | 8.9% |
| JSON deserialization | 134 ms | 3.8% |
| Offline content initialization | 121 ms | 3.5% |
| JSON serialization/write envelope, excluding nested writes | 108 ms | 3.1% |
| Other instrumented work | 305 ms | 8.8% |

There were **136 input materializations and 496 canonical JSON hashes for those 64 battles**, including verification. Each warm 32-battle pass allocated approximately **1.44–1.50 GiB process-wide**, depending on configuration and pass. Allocation totals are not peak resident memory. Recorded process lifetime peak working sets ranged up to **414.9 MiB**.

The archive occupies **380.30 MiB in logical lengths**: approximately **254.50 MiB ordinary archives**, **107.70 MiB detailed replays**, **16.27 MiB producing executable**, **0.53 MiB root content**, and **1.30 MiB metadata**. These are not physical disk-allocation measurements. The final report/index account for the small difference from the report's size measured immediately before its own final output.

The baseline supports the next change: reduce duplicated archive representations and their hashing/verification cost, while preserving required outcomes, search fitness and replay identity. About 64% of the measured exclusive time was outside the combined combat/checkpoint stage. This does not establish the portion of the previous many-hour workload that was recoverable.

## Implementation and limits

- [TowerPerformanceBenchmark.cs](../LL/tools/BalanceHarness/TowerPerformanceBenchmark.cs) implements strict bounded definitions, full preflight, frozen child execution, pass/resource accounting, result comparisons, selected detailed replays and JSON/Markdown reports. It reuses the existing executable-retention helper and normal Tower archive reader/evaluator.
- [TowerPerformanceTrace.cs](../LL/tools/BalanceHarness/TowerPerformanceTrace.cs) collects opt-in async-context-local inclusive/exclusive timings. Small hooks in `HarnessJson`, `OfflineContent`, `TowerBattleRunner`, `TowerBundle` and `TowerBalanceRuns` identify input preparation, content work, production execution/report mapping, serialization, synchronous writes, hashing and verification. Normal archive schemas and combat choices remain unchanged.
- Nested inclusive times overlap; sum exclusive times instead. Parallel worker totals can exceed wall time. The streaming serializer remains streaming. Its nested write timings include buffering effects; canonical/file hashing retain their existing algorithms. Instrumentation and the durable battle counter add overhead, so the command does not measure entirely uninstrumented throughput.
- Production combat and checkpoint construction remain one measured stage. Separating or eliminating discarded checkpoints requires a later implementation and parity check. This first fixture does not cover 6,000-tick exhaustion or every floor/party size.
- Cancellation and budget checks preserve completed ordinary archives, pass receipts, and a battle-start/completion ledger. The parent polls time/storage at 500 ms during child execution and between setup/configurations, requests cooperative cancellation, then waits up to five seconds before terminating only its owned child. Limits can be exceeded by setup work, in-flight writes and final diagnostic receipts. After forced termination, ledger counts are recorded lower bounds; an unfinished battle cannot become complete evidence.
- There is no automatic resume, compact archive mode, immutable preparation reuse, checkpoint removal or new statistical stopping policy in this increment. Full report parity across workers/repeats, legacy validation and replay establish correctness of this instrumented baseline, not completion of those future optimizations.

## Verification and next work

[BalanceHarnessTowerPerformanceTests.cs](../LL/tests/EssenceSystem.Tests/BalanceHarnessTowerPerformanceTests.cs) covers bounded accounting, invalid definitions, complete preflight, async-context isolation, identical JSON bytes/hashes and battle reports with profiling enabled/disabled, worker/repetition parity, legacy tamper rejection, frozen-definition mutation, storage limits, pre-cancellation and cancellation after a running pass.

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -m:1
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerPerformanceTests|FullyQualifiedName~BalanceHarnessJsonTests|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerBenchmarkTests|FullyQualifiedName~BalanceHarnessTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests'
```

The [retained test record](../TestResults/tower-performance-tests-20260912.trx) contains **195 passes, zero failures**, completing in about 69 seconds. The harness build passed with zero warnings/errors. The full test-project build succeeded with five existing unrelated warnings after a sandbox cache-write denial was resolved by retrying with normal filesystem access. The relevant build, tests and benchmark all completed; no required verification remains blocked. Tests finished before the performance measurement began. Automated-test combats are separate from the command's exact 300-combat reservation.

Next, implement a versioned compact bulk archive using the existing shared-content/recipe-ledger patterns. Keep exact trial identities, required fitness/outcome fields, tamper detection and selected full replays; preserve the historical readers. Compare that path against this fixed baseline before adding immutable preparation reuse or changing sampling policy. Large searches and broader-floor balancing remain behind the performance gate.

No game configuration, package dependency, migration, database or deployment changes. The earlier competitive search review, performance postmortem and final acceptance evidence remain unchanged.
