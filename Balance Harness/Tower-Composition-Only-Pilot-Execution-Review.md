# Composition-only pilot completed and verified

**Composition-only trajectory diagnosis complete - 15 September 2026:** the [saved-data review](Tower-Composition-Only-Trajectory-Review.md) verifies all 48 candidates, 50 proposals, 16 library hashes and 49 parent references. All 80 Essences appeared, but no generated loadout contained even three of the controls' four shared ingredients or any complete control loadout. Selection/library reconstruction passes; the gap precedes confirmation. Next target: generic joining of compatible overlapping mechanic groups, with zero-combat verification first. **Zero new fights/seeds**; all **482,371 reservations** preserved. Reliability and adoption Hold unchanged.

15 September 2026. **512 completed / 512 charged fights**, zero retries/resumes/replays. The user explicitly approved the frozen **85-value exception**; all 85 values were reserved and used for their declared generation/discovery/screen/confirmation roles. The full ledger now preserves **482,371 reservations**, including all previously unused reservations. Target: offline BalanceHarness, unchanged captured-v19 guardian Health/Power +10% candidate, fixed ordinal Essence order.

The prespecified new primary won **0/64** and the second finalist **0/64**. No paired contrast establishes an improvement over its control. These results do not establish search reliability, improvement over the old search policy, complete-family balance acceptance or optimality. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption Hold remain unchanged.

## Confirmed measurements

| Recipe role | Wins / trials and seed-free recipe | Observed rate | Adjusted Wilson interval |
| --- | ---: | ---: | --- |
| Primary | [0/64](../TestResults/balance/tower-composition-only-pilot-execution-20260915/exports/finalist-1.json) | 0.00% | 0.00%–10.46% |
| Second finalist | [0/64](../TestResults/balance/tower-composition-only-pilot-execution-20260915/exports/finalist-2.json) | 0.00% | 0.00%–10.46% |
| Saved control 1 | [7/64](../TestResults/balance/tower-composition-only-pilot-execution-20260915/exports/team-040e60d3dbc5c127321653c47ed3a9d3.json) | 10.94% | 4.13%–25.91% |
| Saved control 2 | [4/64](../TestResults/balance/tower-composition-only-pilot-execution-20260915/exports/team-49f6979895354870c89362d4abf214bb.json) | 6.25% | 1.76%–19.89% |

All recipes use the same captured context, neutral identities, equipment and fixed Essence order. Controls stayed outside search, parents, modules and screening. The primary was selected before confirmation and is not retrospectively replaced by whichever team had more confirmation wins. Old ordered-control results are not pooled with these measurements.

| Finalist | Fixed control ID | Gained / lost paired wins | Observed difference | Adjusted paired bounds |
| --- | --- | ---: | ---: | --- |
| finalist-1 | team-040e60d3dbc5c127321653c47ed3a9d3 | 0 / 7 | -10.94 points | -27.42 to 8.17 points |
| finalist-1 | team-49f6979895354870c89362d4abf214bb | 0 / 4 | -6.25 points | -21.40 to 10.40 points |
| finalist-2 | team-040e60d3dbc5c127321653c47ed3a9d3 | 0 / 7 | -10.94 points | -27.42 to 8.17 points |
| finalist-2 | team-49f6979895354870c89362d4abf214bb | 0 / 4 | -6.25 points | -21.40 to 10.40 points |

The frozen alpha allocation is .025 across up to four rates and .025 across the four paired contrasts. Paired intervals use Wilson bounds for gained/lost wins; coverage is approximate. Bounds spanning zero are unresolved. One shallow restart with four discovery trials can miss useful compositions and is vulnerable to early sampling luck. No historical-policy comparator ran, so there is no measured before/after optimizer-quality effect.

## Search and resources

The search evaluated **48 distinct compositions** using **50 proposals**, screened four builds on 16 separate values, then confirmed two frozen finalists plus two controls on 64 separate shared values. Exact duplicate confirmation recipes would merge with all origins retained; the actual confirmed family contains 4 distinct recipes. All evaluated/rejected/duplicate proposals and exact per-character memberships remain in the [discovery archive](../TestResults/balance/tower-composition-only-pilot-study-20260915/discovery/discovery.json). Fixed ability order was never optimized.

Discovery recorded **0/192 wins**, and screening **0/64**. The best discovery candidate left the guardian at **83.99% health** on average. Thus this run did not find a winning discovery or screen candidate; the weak observed result preceded confirmation. The next useful investigation is the saved candidate compositions and proposal history, before allocating another combat study. This pilot alone does not distinguish insufficient search depth from inadequate construction/mutation guidance.

Discovery produced **0** observations above 50%; screening produced **0**. All are retained in the [measured results](../TestResults/balance/tower-composition-only-pilot-execution-20260915/measured-results.json). A recipe without confirmation is not accepted or cleared by this pilot.

| Phase | Completed fights | Native phase seconds |
| --- | ---: | ---: |
| discovery | 192 | 13.440 |
| screen | 64 | 1.980 |
| confirmation | 256 | 5.907 |

Native execution was **21.712 seconds**; the complete run command, including input/registry checks, took **62.156 seconds**. Binding took **55.016 seconds**, native reconstruction **44.984 seconds**, and independent execution audit **25.750 seconds**. Cumulative diagnostic workload including preparation and initial preservation is **257.452 / 1,800 seconds**. New output before publication is **222.32 MiB / 4 GiB**. Detailed phase/CPU/allocation/memory/timing data are persisted; the native performance snapshot precedes final manifest publication. No runtime speedup against an equal-input old-policy run is claimed.

## Verification, commands and preservation

The captured adapter build and **24/24 tests** passed through `build/run-tests.ps1` before execution; the [readiness review](Tower-Composition-Only-Pilot-Readiness-Review.md) records the exact commands and two control preparations. Native post-run verification reconstructed the complete discovery trajectory, shortlist, screen, frozen finalists, confirmation family and all archived outcomes without new combat. The independent Python audit checked all 512 compact records against schedules/evidence, unique compositions, selection order, complete family origins, confirmation intervals, paired contrasts, durable attempts and the reservation union. No build, test or diagnostic execution failed.

Commands executed once from the repository root after the user's explicit authorization:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-composition-only-pilot-preparation-20260915'
& $py -B "$w/workflow.py" bind
& $py -B "$w/workflow.py" run
& $py -B "$w/workflow.py" verify
& $py -B "$w/workflow.py" audit-execution
& $py -B 'TestResults/balance/tower-composition-only-pilot-execution-20260915/publish.py'
```

Do not rerun or resume the sealed study. Reproduction needs a separate frozen workspace; further combat requires a separately bounded scope and seed authorization. [Frozen protocol](Tower-Composition-Only-Pilot-Protocol.md), [authorization](../TestResults/balance/tower-composition-only-pilot-execution-20260915/authorization.json), [independent execution audit](../TestResults/balance/tower-composition-only-pilot-execution-20260915/independent-execution.json), [study seal](../TestResults/balance/tower-composition-only-pilot-study-20260915/final-files.json), [completion receipt](../TestResults/balance/tower-composition-only-pilot-execution-20260915/completion.json), [execution evidence seal](../TestResults/balance/tower-composition-only-pilot-execution-20260915/evidence-files.json).

Only the offline pilot package, evidence/recipe exports and active Markdown were added/updated. The producing harness used the previously verified composition-only source snapshot with the exact captured gameplay dependencies. Existing checkout harness files stayed unchanged; unrelated dirty work remains intact. The full backend suite and dirty gameplay rebuild were not run. No migrations, application configuration changes, deployment, Kharad tuning, default adoption or increases to old experiment caps occurred.
