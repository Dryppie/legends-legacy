# Compact Tower archive implementation and comparison

Date: 12 September 2026. Scope: the offline `LL/tools/BalanceHarness` tool and its backend tests.

The opt-in **`tower-compact-v1`** archive is implemented. On the fixed diagnostic benchmark, ordinary archives fell from **254.50 MiB to 28.79 MiB (88.69% smaller)**. Overall benchmark elapsed time fell by **10.31% and 15.17%** in the two comparisons. Full non-event report digests and all selected detailed replays matched. This completes the compact-format increment; it does not complete the broader campaign-performance prerequisite.

## What is stored

One archive can hold multiple named cases. It freezes shared content/settings/execution identity once, stores each distinct scenario recipe and materialized input template once, and shares prepared-participant descriptions by hash. Ordered character and Essence recipes, exact seeds, floor settings and the producing build remain identifiable.

Each battle contributes a record to a compressed chunk. Records retain the **entire non-event `BattleSummary`**: outcomes, termination, ticks/seconds, final state, per-entity/ability statistics and compact telemetry, plus Tower success/guardian-health/display-duration fields. This deliberately preserves healing, mitigation, survival, summons and status observations used by discovery ranking. Keeping only a win/loss or damage total would change search behavior.

The reader reconstructs the exact original non-event `TowerBattleReport`, including shared preparation, and checks its SHA-256 against the original JSON report hash. Existing `TowerPartySelection`, `TowerBossOptimization` and `TowerBossSearch.Observe` calculations therefore receive the same data. Compatibility hashing streams into SHA-256 rather than writing a duplicate report file. Compression uses .NET's existing gzip support; no dependency was added.

| Artifact | Purpose |
| --- | --- |
| `bulk-plan.json`, `bulk-scope.json` | Versioned case references, complete reservation, settings/content and execution identity |
| `content/Data/`, `recipes/`, `inputs/`, `prepared/` | Shared frozen content, complete recipes, materialized templates and compressed prepared participants |
| `chunks/000000/records.json.gz`, `receipt.json` | Ordered battle records and a count/index/digest receipt |
| `bulk-status.json`, `bulk-manifest.json` | Complete count and exact file/hash inventory; final manifest is the completion marker |
| Optional `executable/`, `executable-files.json` | Producing binaries; the standalone command retains them, while the benchmark shares its parent executable |

Ordinary compact archives contain no repeated `tower-input.json` arrays or scorecards embedding every full report. Detailed event logs remain available through explicitly selected deterministic replay. The performance benchmark keeps the same four replay selections per worker configuration in both formats.

## Verification and integration

Chunks publish from a temporary directory by rename, followed by a final manifest when the full declared schedule completes. Verification checks the exact inventory, file and chunk hashes, recipe/input identities, frozen content, legal materialization, exact case/seed/order/count, reconstructed report hashes and outcome/duration consistency. It rejects missing/modified records, extra or unreferenced data, inconsistent receipts and incomplete runs. Linked directories/files are rejected. A caller can supply a previously trusted manifest digest; self-contained hashes detect inconsistency but are not an external authenticity signature.

Cancellation preserves already committed chunks and writes failure diagnostics, but an incomplete archive cannot supply accepted evidence. Existing output directories are never resumed or overwritten. Atomic publication is not a guarantee against every power-loss/filesystem failure.

`TowerBalanceRuns` accepts compact sources, with `compactCaseId` required for multi-case bundles. Batched evaluation verifies each compact directory once and derives each mapped cell from that verified snapshot. It retains no cache between evaluations; changed artifacts are rechecked on the next call. Legacy archives keep their existing default path and reader. The compact command reports technical completion only: all statistical acceptance still belongs to the unchanged [10–50% policy](Tower-Balance-Acceptance-Policy.md).

## Frozen measurement

The [protocol](../TestResults/balance/tower-compact-comparison-20260912/protocol.json) was written before any comparison fight. Its unchanged [fixture](../LL/tools/BalanceHarness/Fixtures/tower-performance.json) uses four saved floor-5 recipes: weak, strong retained leader, summon/status-heavy and a longer-duration specimen. Each has ten level-40 characters, five untrained Essences per character and the fixed gear budget. The longer-duration label is specimen selection, not a 6,000-tick stress test.

The predeclared order was **legacy A, compact A, compact B, legacy B**, sequentially on one producing executable/content/settings identity. Each run used eight seeds per recipe, three passes, worker limits 1/2/4, and four detailed replays per worker configuration. Exactly **1,200 diagnostic combats completed: 1,152 ordinary repeats plus 48 replays**. Only 32 distinct ordinary recipe/seed pairs are represented; repetitions cannot become independent balance evidence. Unit-test fights are additional correctness work, outside this diagnostic reservation.

Each run retained its 300-combat, 300-second and 2-GiB logical-output limits. In-flight writes/final receipts can exceed a polled resource limit. No builds, tests or archive maintenance initiated by this task ran concurrently with these measurements. OS cache and other machine activity remain uncontrolled.

| Run, in execution order | Total seconds | Ordinary archives MiB | Complete output MiB | Largest measured process peak MiB |
| --- | ---: | ---: | ---: | ---: |
| Legacy A | 23.757 | 254.502 | 380.368 | 450.19 |
| Compact A | 21.307 | 28.789 | 154.702 | 169.10 |
| Compact B | 21.476 | 28.789 | 154.702 | 158.68 |
| Legacy B | 25.316 | 254.502 | 380.368 | 396.75 |

Complete output includes **107.70 MiB of identical detailed replays in every run**, the producing executable, content and metadata. Thus total output fell **59.33%**, while ordinary archive storage fell **88.69%**. The four run directories together occupy approximately **1.045 GiB of logical data**, plus small protocol/comparison/metrics files; this is not measured NTFS allocation. No old archive was converted or deleted.

The [first comparison](../TestResults/balance/tower-compact-comparison-20260912/comparison-a/comparison.md) and [reversed-order comparison](../TestResults/balance/tower-compact-comparison-20260912/comparison-b/comparison.md) each independently revalidated 288 stored trial pairs without executing combat. Result digests also agree across all four runs, and all 48 full detailed replay files match their corresponding recipe's replay. The earlier [23.71-second baseline](Tower-Performance-Benchmark-Review.md) remains historical evidence; the performance ratios above compare two formats on the same newly frozen build.

Overall time ratios were **1.115× and 1.179×** in favor of compact archives. Summed ordinary pass ratios were 1.144× and 1.224×; warm-pass ratios were 1.053× and 1.207×. Three individual warm passes in pair A were slower with compact storage. Both complete runs improved, but this small workload does not establish universal throughput, a best worker count, or the duration of another 600,000-fight campaign.

Allocated memory per 32-fight pass fell from roughly **1,476–1,644 MiB to 1,054–1,100 MiB**. Allocation is cumulative churn, not resident memory. Warm serial compact timing still spends about **41%** in engine/playback including checkpoints, **25–27%** in the measured file/canonical/compatible-report hash stages, and **16–20%** in materialization/content loading. These exclusive stage shares support the next preparation/checkpoint work; they are not promises of recoverable time. Full data and source hashes are in [metrics.json](../TestResults/balance/tower-compact-comparison-20260912/metrics.json).

## Tests and producing builds

The initial comparison build passed **223 relevant tests** through `build/run-tests.ps1`. Coverage includes exact report bytes/digests and ranking/observer parity, multiple cases across chunks, recipe/preparation deduplication without dropping trials, detailed replay, retained executable integrity, invalid definitions, modified/missing artifacts, rehashed schedule/summary changes, interrupted chunks, and compact/legacy performance comparison.

After that frozen measurement, a final integration fix removed repeated whole-bundle reads when multiple balance cells reference the same compact archive. Its regression checks verify one read per directory, preserved cell order, invalid case handling, and corruption detection on a subsequent evaluation. The benchmark worker does not call this batched evaluator method. Measurements belong to the retained comparison executable; replay that evidence with its retained binary. The final test receipt is recorded below rather than attributing those timings to a later rebuilt executable.

Verification commands:

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -m:1
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerCompactTests|FullyQualifiedName~BalanceHarnessTowerPerformanceTests|FullyQualifiedName~BalanceHarnessJsonTests|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerBenchmarkTests|FullyQualifiedName~BalanceHarnessTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests'
```

Both builds succeeded with five existing warnings. Local build permission was needed for generated API build-cache writes. No required command remained blocked. The [CLI reference](../LL/tools/BalanceHarness/README.md#compact-tower-archives) contains create, verify, replay and comparison commands.

The final build passed **224 tests, zero failed/skipped**, including 29 compact archive tests. Its [TRX receipt](../TestResults/tower-compact-final-tests-20260912.trx) and [final verification record](../TestResults/balance/tower-compact-comparison-20260912/final-verification.json) are retained separately from the pre-comparison test receipt and producing executable.

## Limits and next work

- This is **opt-in** standalone/evaluator/benchmark support. Existing discovery, campaign, calibration and retention drivers do not automatically emit compact bundles. Saved best-build catalogs remain unchanged.
- Chunks contain at most 32 records; each compressed JSON file has a 64-MiB uncompressed limit. Definitions permit at most 256 cases, 1,000 seeds per case and 100,000 explicitly reserved battles. Those are validation ceilings, not a recommended workload. The standalone command has a battle reservation but no separate overall elapsed-time/storage cap; the bounded benchmark has all three.
- The reader materializes a complete bundle's reports. It is not a streaming, resumable verifier. Sharing one verified snapshot within a balance evaluation avoids repeated scans, but does not implement persistent trust receipts or bounded-memory campaign consumption.
- Combat still uses fresh production preparation, mutable state and RNG per fight, including playback checkpoints. Prepared data is deduplicated **on disk**, not reused to execute battles.
- No staged statistical stopping policy, campaign migration, historic repackaging or automatic cleanup is introduced. Repeated diagnostics do not establish stronger candidate combinations or a new balance Pass.

Next implement safe reuse of immutable preparation and remove unused bulk playback checkpoints, with independent parity checks. Then complete bounded/resumable verification and campaign integration before a larger combination search. Keep the fixed character/gear/untrained-Essence budget, intended floor slot progression and existing statistical policy. The Kharad portfolio's scoped balance Pass and search-quality Fail remain unchanged.

## Changed files and operational impact

| Files | Change |
| --- | --- |
| `LL/tools/BalanceHarness/TowerCompactBundle.cs` | Versioned writer, verifier, evaluator evidence and replay adapter |
| `LL/tools/BalanceHarness/TowerPerformanceComparison.cs` | Artifact-backed matched-format comparison without additional fights |
| `LL/tools/BalanceHarness/TowerBalanceRuns.cs` | Optional compact case selection and one verification per bundle within batched evaluation |
| `LL/tools/BalanceHarness/TowerPerformanceBenchmark.cs`, `Program.cs` | Opt-in compact benchmark path and CLI commands |
| `LL/tests/EssenceSystem.Tests/BalanceHarnessTowerCompactTests.cs` | Compact parity, integrity, cancellation, shared verification and comparison checks |
| Harness README and six active planning/policy documents | Commands, implemented scope, measured results and remaining work |

No game-content edit, database migration, deployment, new dependency or deployed configuration change was made in this increment. Previously modified files elsewhere in the checkout were preserved. Historical accepted evidence and retained-build catalogs were checked against the cleanup audit's protected hashes.
