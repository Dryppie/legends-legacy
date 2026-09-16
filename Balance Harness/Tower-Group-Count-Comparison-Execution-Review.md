# Group/count comparison completed and verified

15 September 2026. **512 completed / 512 charged fights**, with zero retries, resumes or combat replays. The approved **45 fresh values** are durably retained, bringing the ledger to **482,461 reservations**, including all earlier unused values. Both policies used identical captured gameplay/content, equipment, attributes, neutral identities, timestamp, scenario context and paired combat schedules. Ability order stayed fixed.

The joined baseline finalist won **0/32**, and the group/count finalist won **0/32**. The primary adjusted paired interval includes zero; this pilot does not establish improved win probability. The primary observed difference is **0.00 percentage points**, adjusted paired bounds **-22.22 to 22.22 points**. The prespecified mean boss-health difference, group/count minus baseline, is **-6.00 points**; lower remaining health is better. This health comparison is descriptive, with no additional significance claim.

## Confirmation and retained groups

| Recipe / role | Wins / trials and seed-free recipe | Mean boss health remaining | Mean duration | Characters with a complete catalogue group | Largest repeated group count |
| --- | ---: | ---: | ---: | ---: | ---: |
| Joined baseline finalist | [0/32](../TestResults/balance/tower-group-count-comparison-execution-20260915/exports/baseline-finalist.json) | 87.86% | 53.55 s | 2/10 | 1 |
| Group/count finalist | [0/32](../TestResults/balance/tower-group-count-comparison-execution-20260915/exports/group-count-finalist.json) | 81.86% | 53.20 s | 2/10 | 2 |
| Fixed control 1 | [2/32](../TestResults/balance/tower-group-count-comparison-execution-20260915/exports/team-040e60d3dbc5c127321653c47ed3a9d3.json) | 28.51% | 90.08 s | 8/10 | 7 |
| Fixed control 2 | [0/32](../TestResults/balance/tower-group-count-comparison-execution-20260915/exports/team-49f6979895354870c89362d4abf214bb.json) | 31.84% | 89.42 s | 7/10 | 7 |

All four role recipes were fixed before confirmation. A group is counted only when every Essence in that catalogue group is present on a character. The last column is the largest count for any one group, while the preceding column counts the union of characters with any complete group; overlapping groups do not count a character twice. Ten characters form two five-player parties. These counts describe recipes, not independent combat samples. Full per-group counts, per-seed paired health differences, durations and telemetry missingness are retained in [descriptive-metrics.json](../TestResults/balance/tower-group-count-comparison-execution-20260915/descriptive-metrics.json).

| Role | Observed win rate | Adjusted Wilson interval |
| --- | ---: | --- |
| Joined baseline finalist | 0.00% | 0.00%–18.94% |
| Group/count finalist | 0.00% | 0.00%–18.94% |
| Fixed control 1 | 6.25% | 1.13%–27.94% |
| Fixed control 2 | 0.00% | 0.00%–18.94% |

Zero wins do not establish zero true win probability or equivalence. One shallow restart with 32 confirmations has limited precision. Control results from earlier studies are not pooled with these fresh measurements.

| Prespecified paired contrast | Gained / lost wins | Difference (points) | Adjusted paired bounds (points) |
| --- | ---: | ---: | --- |
| group-count-finalist-minus-baseline-finalist | 0 / 0 | 0.00 | -22.22 to 22.22 |
| baseline-finalist-minus-team-040e60d3dbc5c127321653c47ed3a9d3 | 0 / 2 | -6.25 | -30.96 to 21.24 |
| baseline-finalist-minus-team-49f6979895354870c89362d4abf214bb | 0 / 0 | 0.00 | -22.22 to 22.22 |
| group-count-finalist-minus-team-040e60d3dbc5c127321653c47ed3a9d3 | 0 / 2 | -6.25 | -30.96 to 21.24 |
| group-count-finalist-minus-team-49f6979895354870c89362d4abf214bb | 0 / 0 | 0.00 | -22.22 to 22.22 |

The primary is `group-count-finalist-minus-baseline-finalist`. All four secondary contrasts are retained, including non-improvements. Rate family factor 8 and discordance factor 20 are the frozen Wilson settings; identical merged recipes would have an exact zero difference. No recipe was reselected after confirmation.

## Discovery and construction

| Policy | Distinct evaluated teams | Proposals | Discovery wins | Best mean discovery boss health | Characters with groups across all 44 teams | Largest repeated group count |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| baseline | 44 | 45 | 0/176 | 87.09% | 88/440 | 2 |
| group-count | 44 | 45 | 0/176 | 80.72% | 156/440 | 10 |

Both policies received **44 teams × four shared discovery seeds**, at most 704 proposals, initial 11 fresh teams and FreshEvery 4. Their independent top-two lists were frozen before screening. The screen contained **4 distinct recipes** and recorded **0/32 wins** on eight shared seeds. The two policy winners and two fixed controls formed **4 distinct confirmation recipes**. Full-context duplicates would share later measurements without refilling seats; discovery overlaps remain independently charged. Controls never entered generation or screening.

The group/count policy recorded **19 fresh construction requests**, comprising **17 guided** and **2 uniform** requests. Guided requests covered **17 distinct catalogue groups** and owner counts **1, 2, 3, 4, 5, 6, 7, 8, 9, 10**. Fresh outcomes: `{"complete": 19}`. All proposal outcomes, traces, requests and retained recipes are saved, including rejected or duplicate proposals. This is a short trajectory through the 214-group catalogue, not the 2,140-pair reservation fixture or a complete search.

The construction path reached a repeated-group count of **10** during discovery; the group/count finalist retained a maximum of **2**, versus **1** for the baseline finalist. Existing mutations and team-level ranking can remove or replace groups. Increased repetition does not itself establish beneficial synergy. The generated finalists still leave much more boss health than either control in this batch. The policies keep separate defined random streams; this study does not isolate each construction choice or establish general search reliability.

## Workload and verification

| Native phase | Completed fights | Seconds |
| --- | ---: | ---: |
| baseline-discovery | 176 | 10.604 |
| group-count-discovery | 176 | 6.396 |
| screen | 32 | 1.162 |
| confirmation | 128 | 2.903 |

Native execution took **21.357 seconds**. The full run command, including registry/input checks, took **61.312 seconds**; binding **40.125**, native verification **46.984**, and independent execution audit **17.157**. Diagnostic workload including preparation/preservation before this publication is **247.313 / 1,800 seconds**; new output **279.58 MiB / 4 GiB**. Final publication accounting is in [completion.json](../TestResults/balance/tower-group-count-comparison-execution-20260915/completion.json). Preparation compilation was separately recorded at 4.547 seconds.

The [frozen preparation](Tower-Group-Count-Comparison-Readiness-Review.md) passed **56 backend tests through `build/run-tests.ps1`**, four metric fixtures and 66 numerical interval fixtures. Those exact sources/executables/tests were retained by hash, not rerun against altered code. Native verification reconstructed both search trajectories, nominations, screen selections, final family, compact archives, results and durable attempt journal **without fighting again**. The independent audit separately checked records, schedules, canonical recipes, policy nominations, group/count trace counts, deduplication, all five paired contrasts, complete seed preservation and the **512-attempt** journal.

Final preservation checked **4547 prior indexed files**, including the sealed preparation and ten earlier packages, plus **1245 new study files**. Incremental storage accounting and existing archive verification stayed enabled. Baseline ran first; timings have different warm-up/cache positions and are not a controlled speedup comparison. No fight, time or storage cap was increased.

## Reproduction and changed files

Commands executed once after the user's “Please proceed” response to the specific 45-value request:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-group-count-comparison-preparation-20260915'
$e = 'TestResults/balance/tower-group-count-comparison-execution-20260915'
& $py -B "$e/authorize.py"
& $py -B "$w/workflow.py" bind
& $py -B "$w/workflow.py" run
& $py -B "$w/workflow.py" verify
& $py -B "$w/workflow.py" audit-execution
& $py -B "$e/publish.py"
```

Do not rerun into these sealed directories. Reproduction requires its own authorized budget and output path while retaining the exact producing executable, inputs and schedules. Evidence: [authorization](../TestResults/balance/tower-group-count-comparison-execution-20260915/authorization.json), [independent execution check](../TestResults/balance/tower-group-count-comparison-execution-20260915/independent-execution.json), [measured results](../TestResults/balance/tower-group-count-comparison-execution-20260915/measured-results.json), [performance](../TestResults/balance/tower-group-count-comparison-study-20260915/performance.json), [study seal](../TestResults/balance/tower-group-count-comparison-study-20260915/final-files.json), [execution seal](../TestResults/balance/tower-group-count-comparison-execution-20260915/evidence-files.json).

Changed files are the new study/execution artifacts and seed-free exports, this report and the four active Markdown handoffs. Existing harness/gameplay source and all earlier sealed packages remain unchanged by this task. No required command failed or was blocked. The full dirty gameplay build/backend suite was not rerun; this execution uses the already tested frozen scope. No configuration changes, migrations, deployment, Kharad/content tuning, ability-order search, old-cap increase or sealed-v19 modification.

The authorized comparison is complete; no more seeds or fights are authorized by it. Next inspect the saved fresh proposals, mutations and rankings to locate the remaining gap: construction now reaches concentrated groups, but the selected teams still perform poorly against the controls. Do not assume that retaining more copies alone solves that gap or automatically fund a larger run. The 129,536-fight confirmation remains unstarted. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption **Hold** remain unchanged.
