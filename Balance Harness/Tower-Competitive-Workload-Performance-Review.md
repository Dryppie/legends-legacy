# Competitive Tower workload performance review — 12 September 2026

The workload was inefficient. The largest confirmation phase combined combat with hundreds of gigabytes of redundant JSON, repeated preparation and validation, and competing archive readers. The complete campaign also ran substantially more than 600,000 battles. Some of that additional work answered useful questions; some was avoidable orchestration and contract-validation overhead.

This review concerns the offline `LL/tools/BalanceHarness` workflow, principally Kharad on floor 5. The character/gear budget and untrained Essences remained fixed. It does not change combat, boss scaling, acceptance decisions or historical evidence.

**What actually ran**

| Phase | Actual battles | Purpose and result |
| --- | ---: | --- |
| Four comparative searches | 177,084 | Independent versus retained-build search, at two budgets, with method restarts, selection, confirmation and 12 replays. Larger searches found much stronger teams. |
| Original audit | 70,000 | Invalid for acceptance: exported scenarios used `notes` instead of required `assumptions`. All fights were preserved. |
| Replacement audit | 70,000 | Same 70 recipes, corrected contract and fresh seeds. Search-quality conclusion: **Fail**. |
| First calibration discovery | 31,488 | Coarse and fine linked Health/Power sweeps. Selected multiplier 1.20 relative to the starting scaling. |
| First calibration confirmation | 387,000 | 1,548 recipes × 250 battles. **Fail**; strongest 162/250. This setting was never applied. |
| Challenger searches | 176,980 | Independent and retained searches at the rejected setting, including four replays. Retained search found a 985/1,000 team and expanded the portfolio. |
| Expanded calibration discovery | 56,064 | Seven coarse points and eleven fine points against 48 screening recipes. Selected absolute multiplier 1.392. |
| Expanded calibration confirmation | **609,500** | **2,438 recipes × 250 battles**. Strongest 119/250; simultaneous uncertainty left the decision **Inconclusive**. |
| First precision follow-up | 20,000 | Two unresolved recipes × 10,000 fresh battles. Strongest 48.76%; its adjusted upper bound was 50.00885%, still **Inconclusive**. |
| Final precision follow-up | 50,000 | One unresolved recipe on another fresh schedule. 23,835 wins, 47.67%; completed family decision **Pass**. |
| Diagnostic and application checks | 763 | Current-engine and live-content comparisons, unaffected-floor pairs and detailed replays. These are not additional independent acceptance samples. |
| **Main workload total** | **1,648,879** | Includes the invalid audit and diagnostic/replay executions. |

This total excludes the small separate UI smoke run and simulations inside automated tests. Re-reading historical archives did not rerun combat and is not counted as additional battles. The 609,500 phase was only 37% of the main workload's battles.

Counts come from the search completion receipts, calibration assessments and final completion index; the [extracted metrics](../TestResults/balance/tower-competitive-performance-review-20260912/metrics.json) preserve their accounting. The existing [competitive search review](Tower-Competitive-Build-Search-Review.md) contains the experimental conclusions.

**Where the elapsed time went**

Times below are Copenhagen local time (UTC+02:00). They are reconstructed from artifact creation/write times, not a profiler. Phases overlapped, so their durations must not be added as sequential elapsed time.

| Activity | Recorded interval | Interpretation |
| --- | --- | --- |
| Initial comparative searches | 11 Sep 23:53 → 12 Sep 00:31 | About 38 minutes through search completion/reconstruction. |
| Gap before audit freeze | 00:31 → 08:31 | About eight hours without a subsequent campaign stage in these records. The artifacts do not explain the gap or establish that combat was running throughout it. |
| Invalid audit fights and archives | 08:31 → 08:52 | About 21 minutes; replacement preparation began before this process finished. |
| Replacement audit fights and archives | 08:42 → 09:06 | About 25 minutes. Full archive assessment was written at 09:45; quality report at 09:59. |
| First calibration | 08:47 → 10:01 | Discovery about seven minutes; 387,000 confirmation fights and archives about 45 minutes; verification continued afterward. |
| Challenger searches | 09:06 → 10:08 | About 62 minutes, overlapping the first calibration. |
| Expanded discovery | 10:08 → 10:20 | About eleven minutes of execution. |
| **609,500 confirmation fights and archives** | **10:21:43 → 13:55:55** | **3 h 34 min 12 s**, averaging **47.4 battles/s** across eight combat workers. Includes preparation, process startup and archive output. |
| Final verification tail | 13:55:56 → 14:35:08 | **39 min 12 s after combat/archive completion**. Verification also consumed resources while combat was running. |
| First precision follow-up | 14:35:11 → 14:39:13 | About four minutes from freeze to verified assessment. |
| Final precision follow-up | 14:46:35 → 14:55:15 | About nine minutes from freeze to verified assessment. |
| Application, publication and final index | After acceptance → 15:10 | Local content application, checks, retained-build publication and documentation. |

The first frozen comparison to final integrity receipt spans about 15 h 18 min, but includes the unexplained eight-hour gap. It is incorrect to attribute that entire interval to simulating 600,000 battles. Hardware inventory queries were denied by the sandbox, and a bounded Windows power-event query returned no usable output; sleep/suspend or agent scheduling cannot be established from this review.

Within the 609,500 phase, completion throughput changed substantially:

| Successive completed-run group | Battles in group | Battles/s, including normal run output |
| --- | ---: | ---: |
| First 350 recipes | 87,500 | 116.5 |
| Next 350 | 87,500 | 129.4 |
| Next 350 | 87,500 | 52.1 |
| Next 350 | 87,500 | 35.9 |
| Next 350 | 87,500 | 30.6 |
| Next 350 | 87,500 | 27.7 |
| Final 338 | 84,500 | 65.9 |

These groups use actual run-completion timestamps, avoiding the Python driver's input-order progress messages. They are different recipes, not controlled repeated measurements. Nevertheless, the deterioration coincides with overlapping readers, writers and maintenance work. A [saved performance-counter snapshot](../TestResults/balance/tower-competitive-buffer-verification-20260912/concurrent-performance.json) recorded about 9.1 MB/s disk reads, 4.8 MB/s writes, disk queue length 8 and total CPU at 46%. That supports storage contention; it does not allocate an exact percentage of time to disk, CPU, compression or garbage collection. The counter's reported disk-time value above 100% is not a literal disk-utilization percentage.

**Largest engineering costs**

1. **The ordinary Tower archive format was used at campaign scale.** Metadata from every one of the 2,438 confirmation runs gives exactly 117.71 GB of `tower-input.json` and 216.25 GB of `scorecard.json`. The scorecard embeds every full battle report even though each report is also written separately. Four deterministic archive samples contained 72.49–77.15 MB of separate battle reports per 250 fights. Scaling their mean projects another 183.71 GB, putting these major files near **518 GB of logical JSON for this one phase**. The report-file projection is an estimate; the 333.96 GB of inputs plus scorecards is an exact file-length sum. These are uncompressed logical lengths, not measured physical disk allocation or physical device traffic.

2. **Inputs and prepared character descriptions are repeated per seed.** `TowerBundle.CreateAsync` writes an array of fully materialized inputs. Each input also includes the scenario's whole seed schedule. It calls `CreateInput` for every seed; `TowerBattleRunner.PrepareAsync` calls it again before every battle. For 609,500 fights this means **1,219,000 `CreateInput` calls before verification**, including floor-catalog loading/validation, creature-file parsing and rebuilding the ten-character recipe. With ten characters, that is 12.19 million build-factory invocations at these two call sites. Safe immutable preparation can be shared; battle health, statuses, RNG and other mutable state still require isolation.

3. **Bulk combat builds playback checkpoints and discards them.** `TowerBattleRunner.RunAsync` calls `ExecuteTowerPlaybackAsync(...).Result`. The executor collects checkpoints and the engine maintains checkpoint statistics, but the harness keeps only the final result. The frozen checkpoint interval was ten ticks. Detailed event logs are already disabled for ordinary fights, so simply saying “turn logging off” misses this cost. The remaining reports still contain large prepared-participant descriptions and per-entity/per-ability statistics. In the four inspected first reports, compact prepared descriptions occupied about 84 KB and compact statistics about 66–77 KB each.

4. **Verification repeatedly parses, reconstructs and canonicalizes large objects.** `TowerBundle.ReadSaved` reads the whole input array and scorecard, hashes each battle file, then opens each again to deserialize it. It reconstructs a second scorecard and hashes both full scorecards. `TowerBalanceRuns.Read` reconstructs the recipe and hashes both expected and saved full inputs for every seed. `HarnessJson.Hash` serializes into a JSON element, sorts object properties, writes a memory stream, then copies that stream to an array before hashing. These operations add allocations and CPU work to the large logical read volume. Larger read buffers address only part of this problem.

5. **Thousands of short-lived processes repeat initialization.** The calibration driver launches a new `dotnet ... tower` process for each recipe at each tested setting: 2,438 processes for confirmation and another 864 for the 48-recipe coarse/fine sweep. It reloads content and writes a normal archive each time. Python then parses the entire scorecard merely to extract a few counters. Startup is particularly wasteful for 16-trial discovery batches. Startup's measured share is unknown; it should be profiled rather than assumed to dominate the very large JSON cost.

The relevant implementation is [TowerBundle.cs](../LL/tools/BalanceHarness/TowerBundle.cs), [TowerBattleRunner.cs](../LL/tools/BalanceHarness/TowerBattleRunner.cs), [TowerBalanceRuns.cs](../LL/tools/BalanceHarness/TowerBalanceRuns.cs), [HarnessJson.cs](../LL/tools/BalanceHarness/HarnessJson.cs) and the [calibration driver](../LL/tools/BalanceHarness/Scripts/calibrate-tower-search-portfolio.py). Their four core harness files match the completion index's recorded source hashes. The current executor's playback implementation is in [CombatEngineExecutor.cs](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatEngineExecutor.cs).

There is already a better foundation in [TowerLoadoutArchive.cs](../LL/tools/BalanceHarness/TowerLoadoutArchive.cs): shared frozen content, recipes referenced by hash, a trial ledger and optional fast gzip report storage. Search uses this abstraction. Calibration used the separate normal-run envelope with repeated inputs and scorecards. A follow-up should extend the existing archive patterns instead of creating another unrelated campaign framework. Gzip alone still leaves serialization, repeated preparation and per-battle file overhead.

**Avoidable orchestration and experimental costs**

- **The 70,000 invalid audit fights were an implementation mistake.** Strict contract validation should have run before the first fight. Mandatory preflight was added for the replacement, but that does not recover the initial cost. Preserving the invalid run was correct; producing it was avoidable.
- **Verification restarted without durable per-archive progress.** Four original expanded-calibration readers started between 10:34 and 11:54 and were stopped at 12:26. Their source-file timestamps imply approximately **319 aggregate worker-minutes elapsed**, not 319 minutes of CPU or serial delay. No partition had completed. The replacement readers could not reuse their partial work. Four buffered partitions then took about 103 minutes each under overlap; the last three took about 23–24 minutes each after combat writers had finished. The original first-calibration driver also performed redundant sequential verification before handing off to parallel readers.
- **A small timing probe was insufficient evidence for restarting large readers.** The recorded original-then-buffered comparison was 14.656 versus 7.235 seconds with matching evidence. It was one ordered pair under changing cache/load conditions. It did not prove that restarting several hours of in-progress work would shorten completion time. The buffering change preserved validation, but the decision to discard partial progress was not supported by a measured break-even calculation.
- **Compression competed for storage bandwidth.** Standard NTFS compression and a later optional LZX pass overlapped the campaign. The latter added before/after hashing and stopped on a native disk-space error despite reported free space. Receipts record 2,463 successful operations and about 22 GiB recovered; additional historical checks verified affected archives. This preserved evidence but added avoidable I/O to an already busy critical path. Compression should be chosen at write time or deferred until the compute stage is idle.
- **Search and final calibration were sequenced poorly.** Expensive 387,000-fight confirmation was running while challenger search could still invalidate the chosen boss setting. Finishing a frozen experiment preserved its interpretation, but future protocols should explicitly allow early rejection and should run substantial challenger discovery before final confirmation. The 48-team fine screen later estimated a maximum of 25%, while full-portfolio confirmation found 47.6%. A boss change can reorder which recipes are strongest; a shortlist based on the previous setting can miss that.
- **Uniform sampling spent most battles on clearly weak recipes.** Of the expanded portfolio, **1,890 recipes won zero of 250**, consuming **472,500 battles (77.5%)**. **2,326/2,438** finished at or below 10%; only nine exceeded 30%. This is hindsight evidence of poor allocation, not permission to discard weak recipes from the accepted family. A future predeclared staged policy can retain every recipe while allocating more samples to unresolved ceiling/viability cases. Repeatedly checking ordinary fixed-sample intervals and stopping opportunistically would needlessly compromise the evidence.
- **Operating near the 50% ceiling required additional precision.** The two fresh follow-ups added 70,000 fights because 250 per recipe was insufficient for the strongest candidates and the first 10,000-trial bound narrowly crossed 50%. They were separately frozen; historical samples were not silently pooled or extended. Better broad screening and an explicit uncertainty budget should precede the final campaign. The intended 10–50% rule should remain intact.
- **Tests and builds added contention and retries.** An earlier broad test run reached 386 passes but had three dashboard timeouts and was aborted during heavy load. Later, all 469 tests passed in 6 min 57 s; the final focused 11 passed too. Build retries also hit sandbox cache access and overlapping build-file locks. These were real costs, although they do not explain hours on their own. Tests, builds and archive maintenance should not compete with bulk combat by default.

I should have investigated the archive volume and throughput earlier, avoided redundant verification, and introduced a performance gate before expanding the workload. A succession of individually bounded experiments still became a large, poorly coordinated overall workload. A campaign needs an overall time/storage budget as well as a battle-count cap.

**What the work established, and what it did not**

The work produced useful retained recipes, exposed very large weaknesses in the earlier benchmark, rejected an insufficient boss adjustment, and reached a passing balance decision for the completed 2,438-recipe portfolio. The final strongest measured recipe won 47.67%, and the existing final assessment covers the family under its declared approximate interval policy.

However, **search quality remains Fail**. Larger independent searches still produced large improvements, and retained-derived search found strong teams that independent search missed. The 1.65 million executions do not establish that these are globally optimal or sufficiently close to the best possible teams. Much of the cost was repeatedly measuring known recipes, not discovering new combinations. Faster simulation should support stronger discovery and adversarial challenges, rather than simply increasing confirmation counts.

**Recommended implementation order**

| Priority | Change | Evidence required before scaling up |
| --- | --- | --- |
| 1 | Add stage timings and a bounded benchmark command. Separate content loading, preparation, engine execution, checkpoint/report construction, serialization, writes, hashes and verification. Record allocations, worker counts and logical output bytes. | A fixed mix of weak, strong, summon/status-heavy and long-duration recipes. Report cold/warm runs and repeated measurements; freeze seeds and executable identity. |
| 2 | Add a versioned compact bulk archive using existing shared-content/recipe-ledger patterns. Keep per-trial seed, outcomes, duration, required search-fitness fields and integrity information; keep full reports for selected deterministic replays and diagnostics. Store chunks with atomic completion receipts. | Same production combat outcomes and required fitness/acceptance inputs as the legacy path. Tampering, incomplete-chunk and replay checks. Preserve historical readers. |
| 3 | Reuse immutable recipe/content preparation in bounded persistent workers. Avoid rebuilding seed-independent inputs and repeated canonical hashes. Remove unused playback checkpoint collection from the bulk path. | Seed-order, worker-count and replay parity; no mutable state or RNG leakage. Verify Tower result mapping and final state as well as win counts. |
| 4 | Verify completed chunks once per explicit verification pass and save resumable receipts tied to content/engine/schema/validator identity and immutable artifact digests. Bound memory and I/O concurrency. | Resume must reject modified inputs/results. A receipt must not blindly trust writable paths or file timestamps. Independent verification must remain possible. |
| 5 | Add one scheduler for fights, verification and maintenance, plus an overall elapsed-time/storage ceiling and meaningful completion-rate reporting. | Benchmark several worker counts; ensure more workers actually improve throughput. No overlapping compression or broad tests by default. |
| 6 | Improve broad screening and use predeclared staged confirmation, then challenger search before final calibration acceptance. | Preserve all ceiling breaches and family membership; allocate uncertainty across planned stages. Show search-strength improvement independently of balance acceptance. |

Do not begin another 600,000-fight campaign to validate these changes. Start with a small fixed parity/performance set, compare legacy and optimized paths, then increase the sample only after equivalence and throughput gains are demonstrated. Extending statistical rules and changing storage formats should be separate reviewable changes.

**How much faster could it be?**

The existing workflow already completed 387,000 normal confirmation fights and archives in about 45 minutes (143/s), and the final 50,000-fight follow-up reached its verified assessment in about nine minutes. Those observations make a large improvement over 47/s credible. They are different recipes/settings and resource conditions, so they are not an A/B speedup measurement.

At a sustained **100 battles/s**, 609,500 battles take **about 102 minutes**; at **200/s**, **about 51 minutes**, before any separate final verification. These are arithmetic scenarios, not promised optimized runtimes. The current evidence does not measure pure engine time or justify claiming that 600,000 full Tower fights should take only seconds. Removing hundreds of gigabytes of unnecessary report data is the clearest first target; its actual end-to-end benefit needs the bounded benchmark.

**Review verification and changes**

- Added this report and a [reproducible read-only extraction script](../TestResults/tower-competitive-performance-review-20260912.py), with [metrics](../TestResults/balance/tower-competitive-performance-review-20260912/metrics.json) and [per-run metadata](../TestResults/balance/tower-competitive-performance-review-20260912/run-metadata.json) under ignored `TestResults`.
- Read metadata for all 2,438 confirmation runs; inspected four deterministic battle-directory samples and four individual report payloads. Did not resimulate fights, recursively revalidate the full archive corpus, or recompress evidence.
- Reconciled the 1,648,879 battle total and checked the four principal harness source hashes against the previous completion index. Verified report links and JSON consistency locally.
- Hardware CIM inspection was denied; no new runtime benchmark or backend suite was run for this documentation/analysis-only change. Existing final test results above describe the preceding workload, not new tests performed by this review.
- No migrations, game-content/configuration changes, deployment or external-environment actions. The sealed preceding reports and evidence were left intact.
