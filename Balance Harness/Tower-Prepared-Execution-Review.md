# Reusable Tower preparation and checkpoint-free bulk execution

Date: 12 September 2026. Scope: `LL/tools/BalanceHarness` and its backend tests.

The opt-in **`prepared-v1` execution mode** now prepares each fixed team once, reuses its compiled ability definitions and runs bulk fights without playback checkpoints. On the frozen compact-archive benchmark, total elapsed time fell by **27.09% and 26.96%** in reversed-order comparisons. Allocated memory fell by **58.58% and 58.41%**. Complete result hashes, search ranking/observer inputs and detailed replays matched. All **239 relevant tests passed**.

This is an additional execution improvement after the [compact storage increment](Tower-Compact-Archive-Review.md). Both arms below use compact storage, so the new ratios do not mix execution gains with the earlier archive-size reduction.

## Implementation and state isolation

`TowerBattleRunner.PrepareReusableAsync` takes an owned copy of the full input, validates it against the production materialization path, and calls the existing `WorldTowerCombatRuntimeFactory` and preparation pipeline once. `OfflineContent` owns a copy of threat settings and rejects a prepared worker whose input settings do not match its provider. The handle accepts only seeds from its frozen scenario.

The private `TowerPreparedBattle` worker retains the prepared template, participant description/hash and one production `CombatEngineExecutor`. For every seed it creates a new encounter identity/plan and copies combat actors through the existing `DeepCloneForEncounter`. The executor then creates fresh runtime actors, ability cooldowns, statuses, conditions, summons, stagger state, engine accumulators and RNG. Gear/Essence/ability definitions are privately held and read as fixed input; they are not reused as mutable combat state. A semaphore serializes requests to one worker's mutable compiled-ability lookup; separate case workers remain independent.

Bulk execution calls the existing `ExecuteSimulationAsync` with event logging disabled. It therefore avoids constructing discarded playback checkpoints while retaining all compact telemetry and final statistics. The old playback path is unchanged. Both modes share the existing Tower result mapping, including guardian health, outcome interpretation and displayed duration.

`BattleSummary.From` already removes references to catalog ability definitions from outgoing statistics. Returned team/state/statistics data belong to the current simulation, and prepared descriptions are immutable JSON. Tests mutate incoming recipes/settings and outgoing results, then verify that later fights still match. No extra per-battle serialization copy was added to achieve this isolation.

Reuse lasts **within one recipe's seed batch**. A compact bundle processes cases in order, retaining preparation only for the active case. There is no global preparation cache, cross-bundle cache, resumable worker state or new campaign-wide worker pool. Detailed archive replay deliberately uses fresh production preparation, providing an independent check of the saved optimized result.

## Commands and provenance

```powershell
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll tower-compact --execution-mode prepared-v1 --scenario LL/tools/BalanceHarness/Fixtures/tower-floor-1-user-party.json --content-root LL/src/API/API.LL --output TestResults/balance/tower-prepared-new
dotnet LL/tools/BalanceHarness/bin/Release/net10.0/BalanceHarness.dll tower-performance --archive-format tower-compact-v1 --execution-mode prepared-v1 --content-root LL/src/API/API.LL --output TestResults/balance/tower-performance-prepared-new
```

The benchmark flag requires compact archives. Omitting `--execution-mode` preserves fresh preparation/playback. The archive plan and performance scope/report record the selected mode; verification rejects unknown modes, and comparisons check the scope against each saved case. The optional field is omitted for old-mode output, and current readers retain support for existing archives. Replay still requires the producing executable/runtime; standalone compact archives and benchmark roots retain those binaries.

Use the existing `tower-compact-verify`, `tower-compact-replay`, `tower-balance-evaluate` and `tower-performance-compare` commands. Comparison permits the declared execution modes to differ while requiring the same producing build, recipe/budget/seed schedule, content and settings, and matching verified full-result digests. Execution metadata does not grant statistical acceptance.

## Frozen comparison

The [protocol](../TestResults/balance/tower-prepared-comparison-20260912/protocol.json) and definition copy were frozen before measurement. It used the unchanged four-case [performance fixture](../LL/tools/BalanceHarness/Fixtures/tower-performance.json): weak, retained strong, summon/status-heavy and longer-duration floor-5 teams. The fixed budget remains ten level-40 characters with five untrained Essences each and unchanged gear.

The order was **reference A, prepared A, prepared B, reference B**. Each run used eight seeds per team, three passes and worker limits 1/2/4, plus one detailed replay per team/configuration. Exactly **1,200 diagnostic battles completed: 1,152 ordinary repeats and 48 replays**. There are only 32 distinct ordinary recipe/seed pairs; these repetitions are not independent search or acceptance evidence. Unit-test fights are additional correctness work, outside this diagnostic reservation.

Each run retained the existing 300-combat, 300-second and 2-GiB logical-output caps. No builds, tests or archive maintenance initiated by this task ran concurrently with measurement. Filesystem cache and other machine activity remain uncontrolled. All four runs used the same final producing executable, whose files and relevant source hashes are recorded in the protocol.

| Run, in execution order | Total seconds | Allocated MiB per 32-fight pass | Largest measured process peak MiB | Complete output MiB |
| --- | ---: | ---: | ---: | ---: |
| Reference A: compact, fresh playback | 24.124 | 1,055.51–1,101.69 | 152.86 | 154.714 |
| Prepared A | 17.590 | 424.24–467.11 | 141.21 | 154.815 |
| Prepared B | 17.168 | 424.02–466.62 | 145.06 | 154.815 |
| Reference B: compact, fresh playback | 23.505 | 1,053.28–1,101.63 | 161.97 | 154.714 |

Overall time ratios were **1.372× and 1.369×** in favor of prepared execution. Summed ordinary-pass ratios were **1.481× and 1.505×**; warm-pass ratios were **1.576× and 1.633×**. Every matched pass was faster in these two pairs, but this small benchmark does not establish a universal speedup, an optimal campaign worker count or the duration of another 600,000-battle workload.

Allocation is cumulative memory churn, not resident memory. Across the two warm serial passes (64 ordinary battles), production preparation calls fell from **64 to 8**, and materializations including verification fell from **144 to 32**. No playback stage ran for prepared bulk battles. These counters establish actual reuse and checkpoint removal rather than relying on timing alone.

Warm serial prepared timings now attribute about **37–39%** of measured exclusive time to simulation, **29–31%** to file/canonical/compatible-report hashing and about **13%** to content loading/materialization. These are observations with profiling overhead, not recoverable-time estimates. Full measurements, stage counts, result digests and replay hashes are in [metrics.json](../TestResults/balance/tower-prepared-comparison-20260912/metrics.json).

The [first comparison](../TestResults/balance/tower-prepared-comparison-20260912/comparison-a/comparison.md) and [reversed-order comparison](../TestResults/balance/tower-prepared-comparison-20260912/comparison-b/comparison.md) each revalidated 288 archived trial pairs without executing combat. Full-result digests agree across all four runs, and all 48 complete detailed replay files match their corresponding recipe's replay. Independent comparison reads are outside the originally measured pass times.

Storage remains effectively unchanged: ordinary archives are approximately **28.79 MiB per run**, plus **107.70 MiB of detailed replays** and executable/content/metadata. Execution-mode metadata and additional profiling paths add roughly 0.10 MiB to complete output. The four run directories total **619.06 MiB of logical data**, plus small protocol/comparison/metrics files. Physical NTFS allocation was not measured. No historical archive was deleted or repackaged.

## Verification

The final build passed **239 tests with zero failed/skipped**, retained in the [TRX receipt](../TestResults/tower-prepared-tests-20260912.trx). This includes 12 dedicated prepared-execution tests and prepared-mode extensions to compact/archive tests. Existing independent Tower tests now also compare prepared execution against the normal route that persists and reloads character snapshots. That suite spans floors 1–15, multiple gear/slot budgets, ordered Essence variants and larger deployments; it is parity coverage, not new competitive balance evidence.

Coverage includes full report bytes/hashes; ranking and recovery observations; reversed/repeated/concurrent seed requests; private template stability; incoming and outgoing mutation; cancellation recovery; illegal input/provider settings; unknown modes; complete multi-case chunks; compact verification/evaluator compatibility; selected replay; and exact benchmark accounting. A separate temporary-content test forces the full **6,000-tick draw** and checks repeated result/statistics parity. Its synthetic guardian changes never touch game content or the benchmark fixture.

```powershell
dotnet build LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj --configuration Release --no-restore -p:UseSharedCompilation=false -m:1
./build/run-tests.ps1 -NoBuild -Configuration Release -Filter 'FullyQualifiedName~BalanceHarnessTowerPreparedTests|FullyQualifiedName~BalanceHarnessTowerCompactTests|FullyQualifiedName~BalanceHarnessTowerPerformanceTests|FullyQualifiedName~BalanceHarnessJsonTests|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessTowerBenchmarkTests|FullyQualifiedName~BalanceHarnessTests|FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests'
```

The final build succeeded with five existing warnings. Local permission was needed for generated API build-cache writes; no required command remained blocked. [Final verification](../TestResults/balance/tower-prepared-comparison-20260912/final-verification.json) records the producing binary/source checks, protected historical evidence and unchanged live content. No code changed after the frozen benchmark.

## Remaining work and operational impact

Next implement **bounded-memory, resumable verification and campaign integration**. The current compact reader materializes a whole bundle; completed chunk receipts do not yet support trusted resume. Existing discovery/calibration/retention drivers still emit their current archives and do not automatically select `prepared-v1`. The standalone compact command retains a battle reservation but no separate elapsed-time/storage cap; the bounded benchmark has all three.

After those remaining checks, resume stronger combination searches using saved recipes as controls, confirm challengers on fresh seeds and recalibrate only when warranted. The fixed gear/untrained-Essence budget, intended floor slot progression and unchanged [10–50% policy](Tower-Balance-Acceptance-Policy.md) still apply. Kharad's portfolio has a scoped balance Pass while search quality remains Fail; this performance work does not establish near-optimal teams or broader-floor balance. No staged statistical stopping policy is introduced.

| Changed files | Purpose |
| --- | --- |
| `TowerPreparedBattle.cs` | Private reusable preparation, isolated actors/executor, checkpoint-free simulation and deterministic report mapping |
| `TowerBattleRunner.cs`, `OfflineContent.cs` | Owned input/settings, production preparation validation and shared result mapping |
| `TowerCompactBundle.cs` | Opt-in per-case execution and recorded mode while preserving the compact result format |
| `TowerPerformanceBenchmark.cs`, `TowerPerformanceComparison.cs`, `Program.cs` | Mode selection, frozen provenance, profiling and matched-mode comparison |
| `BalanceHarnessTowerPreparedTests.cs`, `BalanceHarnessTowerTests.cs`, `BalanceHarnessTowerCompactTests.cs` | Independent parity, isolation, cancellation, timeout, archive and benchmark checks |
| Harness README and six active planning/policy documents | Implemented scope, measured gains, commands and remaining prerequisites |

No production engine change, game-content edit, new dependency, database migration, deployment or deployed configuration change was made. Unrelated existing checkout changes were preserved. Saved-team catalogs and historical acceptance evidence remain unchanged.
