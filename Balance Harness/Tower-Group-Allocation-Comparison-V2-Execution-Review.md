# Completion versus allocation: execution review

15 September 2026. Status **VerifiedExploratoryComparison**. **512 completed / 512 charged fights**, **45 fresh values retained**, total **482641 reservations**. Zero retries, resumes or replays. Completion won **0/32**, allocation **0/32**. The adjusted primary interval includes zero; improved win probability is unestablished. The primary difference is **0.00 percentage points**, with adjusted paired bounds **-22.22 to 22.22**.

Identical captured gameplay DLLs/content, floor 5, equipment, attributes, neutral identities and UTC timestamp were retained for both policies. The existing captured guardian Health/Power +10% scenario was reused; no gameplay tuning occurred.

## Confirmation and uncertainty

| Recipe | Wins | Mean boss health remaining | Mean duration | Characters with any complete group | Largest repeated group count |
| --- | ---: | ---: | ---: | ---: | ---: |
| Completion finalist | 0/32 | 79.87% | 57.88 s | 1/10 | 1 |
| Allocation finalist | 0/32 | 80.44% | 55.93 s | 5/10 | 3 |
| Fixed control 1 | 0/32 | 27.40% | 92.53 s | 8/10 | 7 |
| Fixed control 2 | 2/32 | 31.50% | 91.05 s | 7/10 | 7 |

These are ten characters in two five-player parties. A complete group requires every ingredient on the same character. Group counts are construction descriptors, not independent combat samples. Health and duration are descriptive, with missingness retained, and did not select confirmation winners. Mean paired health difference, allocation minus completion: **0.57 points**; lower remaining health is better.

| Recipe | Observed win rate | Adjusted Wilson interval |
| --- | ---: | --- |
| Completion finalist | 0.00% | 0.00% to 18.94% |
| Allocation finalist | 0.00% | 0.00% to 18.94% |
| Fixed control 1 | 0.00% | 0.00% to 18.94% |
| Fixed control 2 | 6.25% | 1.13% to 27.94% |

| Prespecified paired contrast | Gained / lost wins | Difference (points) | Adjusted bounds (points) |
| --- | ---: | ---: | --- |
| group-allocation-finalist-minus-baseline-finalist | 0 / 0 | 0.00 | -22.22 to 22.22 |
| baseline-finalist-minus-team-040e60d3dbc5c127321653c47ed3a9d3 | 0 / 0 | 0.00 | -22.22 to 22.22 |
| baseline-finalist-minus-team-49f6979895354870c89362d4abf214bb | 0 / 2 | -6.25 | -30.96 to 21.24 |
| group-allocation-finalist-minus-team-040e60d3dbc5c127321653c47ed3a9d3 | 0 / 0 | 0.00 | -22.22 to 22.22 |
| group-allocation-finalist-minus-team-49f6979895354870c89362d4abf214bb | 0 / 2 | -6.25 | -30.96 to 21.24 |

The primary is `group-allocation-finalist-minus-baseline-finalist`; all four secondary control contrasts are retained. Frozen rate family factor 8 and paired discordance factor 20 apply. Draws are non-wins; identical merged recipes have exact zero difference. No historical combat pooling or finalist reselection. Zero wins establish neither equivalence nor zero underlying probability.

## Discovery

| Policy | Evaluated teams | Proposals | Wins | Best mean discovery boss health | Guided / uniform requests | Requested groups |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| baseline | 44 | 48 | 0/176 | 79.25% | 18 / 2 | 18 |
| group-allocation | 44 | 51 | 0/176 | 81.97% | 21 / 3 | 8 |

Each policy evaluated 44 teams on four shared seeds, with 704 proposals maximum, 11 initial fresh teams and FreshEvery 4. Top two per policy entered an eight-seed screen (4 unique recipes), followed by one frozen finalist each plus two controls on 32 new paired seeds (4 unique recipes). Stage-only exact-context deduplication retains all origins and never refills seats. Controls were excluded from search and screening.

Baseline uses `independent-group-completion-v1`; candidate uses `independent-group-allocation-v1`. The latter varies the allocation of authored mechanic groups across characters. Both retain completion behavior and fixed ordinal Essence-ID ability order. This single shallow restart, separate policy RNG streams and limited group coverage do not establish general search reliability or isolate every construction effect.

## Measurements and verification

| Workflow phase, including registry/input checks | Seconds |
| --- | ---: |
| bind | 50.453 |
| run | 75.234 |
| verify | 58.016 |
| audit-execution | 8.907 |

| Native phase | Charged fights | Completed fights | Seconds |
| --- | ---: | ---: | ---: |
| metric-baseline-discovery | 176 | 176 | 12.845 |
| metric-confirmation | 128 | 128 | 3.465 |
| metric-group-allocation-discovery | 176 | 176 | 7.671 |
| metric-screen | 32 | 32 | 1.380 |

Cumulative diagnostic time before publication: **1733.552 / 1,800 seconds**; **1,537.333 seconds** were carried into execution. The [completion receipt](../TestResults/balance/tower-group-allocation-comparison-v2-execution-20260915/completion.json) includes measured authorization/publication and one conservative closure second. Output carries **1,818,924,029 bytes** before preparation, plus preparation, this study/execution and the preparation's conservative 1-MiB fixture charge, under the unchanged cumulative 4-GiB cap.

The [sealed preparation](Tower-Group-Allocation-Comparison-V2-Readiness-Review.md) passed seven new backend tests through `build/run-tests.ps1`, reusing 131 unchanged backend cases, four metric fixtures and six independent-reader fixtures by hash. No test or gameplay rebuild was repeated during execution. Exact live registry membership/hashes remain required before binding, run and native verification; the preparation snapshot does not replace those checks.

Native verification and independent audit both passed. They reconstructed search/proposal traces, fixed order, completion/allocation construction, nominations, finalist selection, archived records, seed disjointness, all paired contrasts and durable attempts. All **16,407 preceding indexed files across 41 sealed packages** were checked unchanged; new study seal membership count: **1245**. Existing storage accounting, cancellation and durable charging remain active. Detailed CPU, allocations, peak memory and trace metrics are retained where execution reached measurement.

These are workload measurements, not a controlled whole-run speedup comparison. The earlier registry candidate reduced measured managed allocations 71.12% but was 13.29% slower on its archive fixture; it has no established latency improvement. The original incremental-accounting evidence remains separate from this search-policy experiment.

## Exact commands and disposition

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$preparation = 'TestResults/balance/tower-group-allocation-comparison-v2-preparation-20260915'
$execution = 'TestResults/balance/tower-group-allocation-comparison-v2-execution-20260915'
& $python -B "$execution/authorize.py"
& $python -B "$preparation/workflow.py" bind
& $python -B "$preparation/workflow.py" run
& $python -B "$preparation/workflow.py" verify
& $python -B "$preparation/workflow.py" audit-execution
& $python -B "$execution/publish.py"
```

The sequence stops on the first failure; only successful phase receipts above certify completion. Do not rerun these sealed output directories. Reproduction requires a separately frozen scope and authorized budget, retaining the exact executable, inputs and schedules. See [execution seal](../TestResults/balance/tower-group-allocation-comparison-v2-execution-20260915/evidence-files.json) and the saved command, outcome and preservation receipts.

Changed files: new execution/study evidence, this report and six active Markdown handoffs. Harness/gameplay source and unrelated dirty work are preserved. The frozen backend verification was reused; the full dirty gameplay build/suite was not rerun. No configuration changes, migrations or deployment implications. No default policy promotion, ability-order optimization, Kharad tuning, old-cap increase or v19 modification.

Next inspect saved allocation/completion proposals and screening decisions against the controls, using existing evidence without combat, before selecting any further search change. This approval does not fund further fights or seed allocations. Historical reliability **Fail 1/3**, deep recovery **0/3**, sealed v19 **Unresolved**, and adoption **Hold** remain unchanged. V19 retains all 253 recipes and unused 512 confirmation values; the 129,536-fight confirmation remains unstarted.
