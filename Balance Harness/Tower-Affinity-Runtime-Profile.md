# Supported affinity search runtime — 25 September 2026

The supported search now makes private deep copies through compact UTF-8 JSON instead of indented UTF-16 strings. The JSON contract, mutation isolation, saved evidence, essence order, battle schedule and selection rules remain unchanged. This is an allocation improvement with a modest observed effect on total runtime, not a new search algorithm.

## Measured workload

The engineering fixture reused root 1's control search from `TestResults/balance/tower-affinity-nomination-pilot-01-20260925`. Its 528-fight schedule, recipes, settings and captured content were retained. Only the execution identity was rebound to the locally built harness. Each implementation ran twice. All 2,112 replayed fights matched the saved trial ID, stage, recipe hash, seed, input hash and full canonical battle-report hash. Each pass retained the benchmark. No fresh seeds, strength claims or team promotions resulted.

| Measurement | Before | After |
| --- | ---: | ---: |
| First recorded 528-fight pass | 26.832 s | 25.227 s |
| Warm 528-fight pass | 21.990 s | 21.563 s |
| Median time to copy the saved plan | 54.221 ms | 40.736 ms |
| Allocated bytes per plan copy | 57,112,914 | 25,863,666 |

The same-process copy comparison used four rounds of five copies per implementation, alternating their order. The roughly 20 MB saved plan was copied identically by both implementations. Copying used about 25% less time and 55% fewer allocated bytes.

The whole-search timings exclude source verification, setup and post-run parity checks; they include native search, combat, durable evidence and limit checks. The warm difference is approximately 0.43 seconds (1.9%). Runs were sequential, baseline first; OS caches were uncontrolled, and the optimized process also ran the copy microbenchmark before its search passes. These observations do not establish a general speedup or performance on other encounters.

Combat playback accounted for 12.58 seconds of the 21.99-second baseline warm pass (57%). Canonical hashing accounted for another 3.64 seconds (17%). Proposal generation was small in this workload. Exclusive timings avoid counting nested stages twice. There is no evidence here that another proposal algorithm would address the dominant runtime cost.

## Implementation and checks

- `TowerBatchRacingContract.cs`: compact UTF-8 private copies with the same serializer contract and an opt-in copy timing stage.
- `TowerProposalRacingNative.cs`, `TowerAdaptiveRacingGenerator.cs`, `TowerLoadoutArchive.cs`: opt-in timing boundaries for native search, generation, archive evaluation and battle writes. Profiling is inactive during ordinary execution.
- `BalanceHarnessRacingCopyTests.cs`: legacy-copy equivalence for plans and complete/failed histories, plus nested mutation isolation.
- `BalanceHarnessAffinityRuntimeTests.cs`: explicit, bounded replay fixture, copy measurements and all-report parity checks; skipped unless its source, pin and output are configured.
- `AFFINITY-SEARCH.md`: profiling instructions and evidence limitations.

`build/run-tests.ps1 -NoBuild` passed 134 targeted regression tests covering racing, native archives, benchmark validation, the supported profile and copying. The baseline and optimized engineering replay tests also passed. Source content and manifest pins remained unchanged. No verification remains blocked.

The initial build could not read the user NuGet configuration; the isolated artifact path lacked restore assets, and a default dependency rebuild encountered a protected intermediate file. Building the harness and test project with their existing restored references (`--no-restore -p:BuildProjectReferences=false`) resolved these local build issues. The test entry point remained `build/run-tests.ps1`.

Evidence is retained under `TestResults/affinity-runtime-baseline-20260925` and `TestResults/affinity-runtime-optimized-20260925`, with `timing.json` and `parity.json` for each pass. The optimized directory also contains `copy-comparison.json`. Regression and replay logs are `TestResults/affinity-runtime-regression-20260925.log`, `TestResults/affinity-runtime-baseline-run-20260925.log` and `TestResults/affinity-runtime-optimized-run-20260925.log`.

The change is confined to the offline harness. There are no database migrations, gameplay configuration changes or deployments. Existing published runtimes stay unchanged; using this code in a future admitted run requires retaining its new execution identity. The next priority is representative encounter coverage for the supported baseline, rather than further tuning on this single floor.
