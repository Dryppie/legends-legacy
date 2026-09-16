# Group-completion comparison completed and verified

15 September 2026. **512 completed / 512 charged fights**, zero retries, resumes or combat replays. The approved 45 fresh values are durably retained; the ledger now contains **482,596 reservations**, including all preceding unused values. Both policies used identical captured gameplay/content, equipment, attributes, identities, timestamp and paired schedules. Ability order stayed fixed.

The group-diversity finalist won **0/32** and the group-completion finalist won **0/32**. The adjusted primary interval includes zero; this pilot does not establish improved win probability. The observed difference is **0.00 percentage points**, adjusted paired bounds **-22.22 to 22.22 points**. Mean paired boss-health difference, completion minus baseline, is **-2.02 points**; lower remaining health is better. Health and duration are descriptive and did not influence confirmation selection.

## Confirmation

| Role / seed-free recipe | Wins | Mean boss health remaining | Mean duration | Characters with any complete group | Largest repeated group count |
| --- | ---: | ---: | ---: | ---: | ---: |
| Group-diversity finalist | [0/32](../TestResults/balance/tower-group-completion-comparison-execution-20260915/exports/baseline-finalist.json) | 83.79% | 53.50 s | 7/10 | 3 |
| Group-completion finalist | [0/32](../TestResults/balance/tower-group-completion-comparison-execution-20260915/exports/group-completion-finalist.json) | 81.77% | 55.62 s | 5/10 | 5 |
| Fixed control 1 | [0/32](../TestResults/balance/tower-group-completion-comparison-execution-20260915/exports/team-040e60d3dbc5c127321653c47ed3a9d3.json) | 26.58% | 92.87 s | 8/10 | 7 |
| Fixed control 2 | [1/32](../TestResults/balance/tower-group-completion-comparison-execution-20260915/exports/team-49f6979895354870c89362d4abf214bb.json) | 31.59% | 90.14 s | 7/10 | 7 |

A complete group requires all of its Essences on the same character. Counts describe ten characters in two five-player parties; they are not independent combat samples. Overlapping groups do not count a character twice in the union column. All per-group counts and missingness are retained in [descriptive metrics](../TestResults/balance/tower-group-completion-comparison-execution-20260915/descriptive-metrics.json).

| Role | Observed win rate | Adjusted Wilson interval |
| --- | ---: | --- |
| Group-diversity finalist | 0.00% | 0.00% to 18.94% |
| Group-completion finalist | 0.00% | 0.00% to 18.94% |
| Fixed control 1 | 0.00% | 0.00% to 18.94% |
| Fixed control 2 | 3.12% | 0.33% to 23.67% |

| Prespecified paired contrast | Gained / lost wins | Difference (points) | Adjusted bounds (points) |
| --- | ---: | ---: | --- |
| group-completion-finalist-minus-baseline-finalist | 0 / 0 | 0.00 | -22.22 to 22.22 |
| baseline-finalist-minus-team-040e60d3dbc5c127321653c47ed3a9d3 | 0 / 0 | 0.00 | -22.22 to 22.22 |
| baseline-finalist-minus-team-49f6979895354870c89362d4abf214bb | 0 / 1 | -3.12 | -26.80 to 21.93 |
| group-completion-finalist-minus-team-040e60d3dbc5c127321653c47ed3a9d3 | 0 / 0 | 0.00 | -22.22 to 22.22 |
| group-completion-finalist-minus-team-49f6979895354870c89362d4abf214bb | 0 / 1 | -3.12 | -26.80 to 21.93 |

The primary is `group-completion-finalist-minus-baseline-finalist`. All four secondary finalist/control contrasts are retained. Rate family factor 8 and paired discordance factor 20 are frozen. Draws are non-wins; identical merged recipes would have exact zero difference. No prior combat evidence was pooled and no finalist was reselected after confirmation. Zero wins do not establish zero true win probability or equivalence.

## Discovery and construction

| Policy | Evaluated teams | Proposals | Discovery wins | Best mean discovery boss health | Guided / uniform requests | Requested groups | Requested owner counts |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| baseline | 44 | 45 | 0/176 | 81.86% | 17 / 2 | 17 | 5 |
| group-completion | 44 | 47 | 0/176 | 80.13% | 18 / 2 | 18 | 5 |

Each policy evaluated 44 teams on four shared discovery seeds, with at most 704 proposals, 11 initial fresh teams and FreshEvery 4. The top two from each policy were frozen before the **4-recipe** screen on eight shared seeds. One winner per policy plus the two fixed controls formed **4 confirmation recipes**. Exact-context deduplication retains origins and never refills seats. Controls remained outside discovery/screening.

Both policies use the same diversity order of groups, based on newly covered evidence keys, source cores and Essences, beginning with a five-owner sweep. Completion additionally fills missing authored coverage on group-bearing characters before the existing filler. Both retain every eighth fresh request as uniform. Raw requested/placed/final counts, group identities, bundle/filler metadata, rejections, duplicates and recipes remain saved. This short trajectory does not cover the 214-group catalogue, establish general search improvement or isolate a construction mechanism from the policies' separate random streams.

## Workload, verification and preservation

| Native phase | Completed fights | Seconds |
| --- | ---: | ---: |
| baseline-discovery | 176 | 12.637 |
| group-completion-discovery | 176 | 7.190 |
| screen | 32 | 1.258 |
| confirmation | 128 | 3.270 |

Native execution took **24.755 seconds**. Including registry/input checks, run took **71.063 seconds**, binding **47.000**, native verification **54.375**, and independent audit **19.875**. Cumulative diagnostics before publication are **1168.680 / 1,800 seconds**, including 973.492 seconds carried into execution. The final [completion receipt](../TestResults/balance/tower-group-completion-comparison-execution-20260915/completion.json) includes publication and a conservative one-second closure allowance. The prior failed fixture and corrected accounting remain charged; output includes the preceding 1,286,901,467-byte chain plus this comparison preparation package.

The [frozen preparation](Tower-Group-Completion-Comparison-Readiness-Review.md) passed **103 backend tests through `build/run-tests.ps1`**, four metric fixtures and 66 interval fixtures. Those exact frozen sources/executables were retained by hash; tests and gameplay were not rebuilt or replayed. Native verification reconstructed the two searches, nominations, screen/finalist decisions, compact archives, quality and durable attempts. The independent audit separately checked archived records, seeds, recipes, both diversity schedules, seeded order, placement/filler derivation and every completion insertion, deduplication and all five paired contrasts.

All **12,736 prior indexed files across 31 sealed packages**, plus **1,245 new study files**, verified unchanged. The existing incremental storage accountant, cancellation, durable attempt charging and archive verification remained active. Persisted performance includes CPU, allocations, peak memory and detailed trace. Baseline ran first; warm-up/cache differences mean these timings are not a controlled speedup comparison.

## Commands and remaining work

Executed once after the user's direct “Please proceed” reply to the specific 45-value request:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$preparation = 'TestResults/balance/tower-group-completion-comparison-preparation-20260915'
$execution = 'TestResults/balance/tower-group-completion-comparison-execution-20260915'
& $python -B "$execution/authorize.py"
& $python -B "$preparation/workflow.py" bind
& $python -B "$preparation/workflow.py" run
& $python -B "$preparation/workflow.py" verify
& $python -B "$preparation/workflow.py" audit-execution
& $python -B "$execution/publish.py"
```

Do not rerun into sealed directories. Reproduction needs a separately frozen output and authorized budget while retaining exact executable, inputs and schedules. See [measured results](../TestResults/balance/tower-group-completion-comparison-execution-20260915/measured-results.json), [performance](../TestResults/balance/tower-group-completion-comparison-study-20260915/performance.json), [study seal](../TestResults/balance/tower-group-completion-comparison-study-20260915/final-files.json) and [execution seal](../TestResults/balance/tower-group-completion-comparison-execution-20260915/evidence-files.json).

Changed files: the new study/execution evidence, seed-free recipe exports, this report and six active Markdown handoffs. Existing harness/gameplay source and old sealed packages were preserved. No required execution command failed or was blocked. The full dirty gameplay build/backend suite was not rerun; this used the already tested frozen scope. No configuration changes, migrations, deployment, default promotion, Kharad/content tuning, ability-order search, old-cap increase or v19 modification.

The authorized comparison is complete; its 45-value exception and fight allowance are exhausted. Next inspect the saved completion/diversity proposals, mutation ancestry and screening decisions to understand the observed difference and remaining control gap, using archived evidence and no new combat. Do not infer reliability from one shallow restart or automatically fund a larger run. Historical reliability **Fail 1/3**, deep recovery **0/3**, sealed v19 **Unresolved**, and adoption **Hold** remain unchanged. Its 253 recipes and unused 512 confirmation values remain preserved; the 129,536-fight confirmation is unstarted.
