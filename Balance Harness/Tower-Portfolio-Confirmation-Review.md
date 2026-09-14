# Captured-v19 portfolio confirmation — completed and reconstructed

Completed **14 September 2026** under the [frozen study plan](Tower-Portfolio-Confirmation-Plan.md) and [execution protocol](Tower-Portfolio-Confirmation-Execution-Protocol.md). **129,536 fights** completed exactly once for **253 recipes × 512 fresh shared trials**. Reliability is **Fail 1/3**, adoption **Hold**, and ordinary/joint family assessment **Fail / Fail**. All complete-family results were reconstructed without further combat.

## Result and scope

The full family retained 112 controls and 141 new recipes, including all 119 recipes required solely by the ceiling rule. All original and screened nomination groups, primaries/secondaries, roots, recipe order and origins remain frozen. There was no discovery or screening rerun, pooling, selection after confirmation, retry or recipe omission.

**85 recipes** have observed confirmation rates above 50%; **63** have ordinary-adjusted lower bounds above 50%, and **58** have joint-adjusted lower bounds above 50%. **152 / 151** recipes support at least 10% viability under ordinary/joint intervals. Family ceiling acceptance and the original 2/3 search-reliability comparison are separate decisions.

The [complete recipe results](../TestResults/balance/tower-portfolio-confirmation-execution-20260914/confirmation-results.json) retain every count, interval, recipe and origin. The [ranked inventory and 253 exact exports](../TestResults/balance/tower-portfolio-confirmation-execution-20260914/exports/saved-builds.md) make the saved parties available for review without catalog promotion or claims about acquisition.

The fixed anchor won **0/512**; the stronger saved control won **152/512**. Captured Kharad remains Health **3.5366243328** and Power **4.4702934848**. Four gameplay DLLs, bound content/settings and the runtime identity match v19. Current checkout gameplay was not substituted.

## Frozen reliability comparison

Intervals below are joint adjusted; paired differences are percentage points. All three gate components must pass for at least two of the three screened portfolio primaries.

| Root | Portfolio wins | Deep wins | Portfolio rate [lower, upper] | Paired improvement over deep | Paired difference vs anchor | Root gate |
| ---: | ---: | ---: | --- | --- | --- | --- |
| -1038588442 | 37/512 | 359/512 | 7.23% [3.90%, 13.02%] | -62.89 [-69.84, -53.48] pp | 7.23 [2.39, 11.78] pp | Fail |
| -867718476 | 303/512 | 177/512 | 59.18% [50.58%, 67.25%] | 24.61 [12.23, 36.02] pp | 59.18 [50.16, 65.88] pp | Pass |
| -242256866 | 112/512 | 170/512 | 21.88% [15.63%, 29.74%] | -11.33 [-22.00, -0.21] pp | 21.88 [14.66, 28.23] pp | Fail |

- Root **-1038588442**: viability **Fail**, improvement over matching deep primary **Fail**, anchor recovery **Pass**. Portfolio `team-1b3d503fe18c185829cf1d18a135ff29`; deep `team-77416926dedbf2bf6abc7ccb7f782f89`. Stronger-control paired difference: **-22.46 [-31.48, -12.56] pp**, reported separately.
- Root **-867718476**: viability **Pass**, improvement over matching deep primary **Pass**, anchor recovery **Pass**. Portfolio `team-6d05d55b31fc7d2092c447ccd19fd5a2`; deep `team-42385f7b20113d3bf961905843438a72`. Stronger-control paired difference: **29.49 [17.81, 40.02] pp**, reported separately.
- Root **-242256866**: viability **Pass**, improvement over matching deep primary **Fail**, anchor recovery **Pass**. Portfolio `team-697e8a1863114d6c92a757db294776f0`; deep `team-3037558287f9005eeb6ae60dd59587d4`. Stronger-control paired difference: **-7.81 [-18.60, 3.29] pp**, reported separately.

The unchanged joint split is alpha .025 over all 253 rates and .025 over nine paired differences / 18 discordance intervals. Ordinary family intervals use .05 over all 253 rates. These are approximate Wilson intervals for this frozen family, with no lifetime repeated-study, unsearched-party, optimality or practical-acquisition guarantee. The harness adoption field reports the reliability gate; it never automatically promotes gameplay or overrides family acceptance.

## Execution and performance

| Measurement | Result |
| --- | ---: |
| Started / completed / retries | 129,536 / 129,536 / 0 |
| Native summary execution | 3138.653 s (52.31 min) |
| Full run command, including input check and final publication/verification | 3147.695 s (52.46 min) |
| Normal completed reconstruction | 94.515 s; zero fights |
| Independent audit and prior-package preservation command | 404.848 s; zero fights |
| Retained complete study | 1,481,017,559 bytes (1412.4 MiB), 9,027 files |
| Native execution / study limits | 14,400 s / 8 GiB |
| New seed allocation during execution | 0 |

The [saved performance trace](../TestResults/balance/tower-portfolio-confirmation-20260914/performance.json) records simulation/I/O/accounting/journal timing, CPU seconds, allocations and peak working set. Its snapshot ends before final inventory publication; the full-command timer includes that boundary. The largest measured exclusive categories are below. Repeated path suffixes are grouped; nested inclusive durations are not added.

| Exclusive category | Calls | Seconds | Share of performance snapshot |
| --- | ---: | ---: | ---: |
| `engine.simulation-without-checkpoints` | 129,536 | 2070.202 | 65.96% |
| `outer.attempt-flush` | 259,072 | 466.947 | 14.88% |
| `compact.flush-attempt` | 129,536 | 251.814 | 8.02% |
| `hash.compatible-report` | 259,072 | 99.707 | 3.18% |
| `storage.check-owned` | 6,095 | 45.797 | 1.46% |
| `outer.attempt-journal` | 259,072 | 38.649 | 1.23% |
| `hash.file-read` | 63,398 | 36.563 | 1.16% |
| `compact.read-decompress-deserialize` | 8,096 | 35.085 | 1.12% |

This is one complete confirmation workload, **not a paired before/after speedup**. The earlier measured 622.54× incremental / 4.50× sixteen-write lifecycle bookkeeping results retain their original fixture scope; the broader 5× target remains unmet in that historical measurement. Confirmation duration must not be compared directly with v19 discovery as an optimization speedup because recipes, trial counts and archive shapes differ.

## Verification and preservation

The native verifier reconstructed every retained report, complete definition, producing identity, ordinary/joint calculations, durable attempt journal and final inventory. The independent audit required all 253 cells to contain the identical ordered 512-seed schedule; counted all wins/defeats/draws and nine paired discordance comparisons; and reproduced the rate bounds and gate decisions using Python `statistics.NormalDist` within 1e-8 of the harness's Acklam approximation. It checked every final archive file and all six prior sealed packages. Detailed receipts and the [final verification](../TestResults/balance/tower-portfolio-confirmation-execution-20260914/final-verification.json) retain hashes and boundaries.

The original 480,707 exclusions are preserved. All 512 newly prepared values were used by this complete confirmation, leaving **481,219 cumulative reservations**; the 512 unused v19 confirmation values remain excluded. The original v19 experiment remains **VerifiedCapacityExceeded** and was not resumed or rewritten. Its historical NotRun confirmation status is preserved; this separate study provides the new captured-scope inference.

The 201 passing backend tests from [preparation](Tower-Portfolio-Confirmation-Preparation-Review.md) were reused after verifying unchanged reviewed harness/test source and the sealed test evidence. No new code change justified repeating the suite during this measured run. Backend tests were originally invoked through `build/run-tests.ps1`; no required command was blocked.

## Commands and handoff

The [execution definition](../TestResults/balance/tower-portfolio-confirmation-execution-20260914/execution-definition.json) froze these commands and wrapper timeouts before combat. They are provenance commands; never rerun the completed study or write into sealed work paths.

```powershell
$w = 'TestResults/balance/tower-portfolio-confirmation-execution-20260914'
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $py "$w/execute.py" freeze
& $py "$w/execute.py" execute
& $py "$w/postprocess.py"
```

The wrapper invoked the retained executable with `tower-portfolio-confirmation-check`, then `tower-portfolio-confirmation-run`, then `tower-portfolio-confirmation-verify`. The run returned **1**; normal reconstruction returned **0**. Exit 1 from a complete run denotes an unmet adoption gate, not an execution failure. Independent audit returned 0. No additional combat or seeds were used for verification.

A supported ceiling breach requires a separately scoped recalibration and full relevant-family confirmation before balance acceptance. No gameplay or Kharad tuning was performed. Updated-checkout gameplay requires its own explicitly frozen scope; this result belongs to captured v19.

This task added the execution protocol, result review and execution work package, and updated six active handoffs. Producing harness/gameplay/test source was unchanged; dirty and concurrent checkout work is recorded separately and preserved. No migration, production configuration, deployment, default/catalog promotion or old-cap change occurred. Practical acquisition and floors 6–11 remain separate milestones.
