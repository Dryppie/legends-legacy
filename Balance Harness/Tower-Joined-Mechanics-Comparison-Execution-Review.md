# Joined-mechanics comparison completed and verified

15 September 2026. **512 completed / 512 charged fights**, zero retries/resumes/combat replays. The user approved **45 new values** for this frozen comparison; the ledger now retains **482,416 reservations**, including all earlier unused values. Both policies used the same captured gameplay, content, equipment, neutral identities, timestamp, scenario identity and combat schedules, with fixed ordinal Essence order.

The baseline finalist won **0/32**; the joined-mechanics finalist won **0/32**. The primary adjusted paired interval includes zero: this pilot does not establish that joined-mechanics search improves the selected team. The observed primary difference is **0.00 percentage points**, with adjusted paired bounds **-22.22 to 22.22 points**. Both finalists were fixed before confirmation. Historical reliability Fail 1/3, deep recovery 0/3, sealed v19 Unresolved and adoption Hold remain unchanged.

## Confirmation results

| Role | Wins / trials and seed-free recipe | Observed rate | Adjusted Wilson interval |
| --- | ---: | ---: | --- |
| Baseline finalist | [0/32](../TestResults/balance/tower-joined-mechanics-comparison-execution-20260915/exports/baseline-finalist.json) | 0.00% | 0.00%–18.94% |
| Joined-mechanics finalist | [0/32](../TestResults/balance/tower-joined-mechanics-comparison-execution-20260915/exports/joined-finalist.json) | 0.00% | 0.00%–18.94% |
| Fixed control 1 | [0/32](../TestResults/balance/tower-joined-mechanics-comparison-execution-20260915/exports/team-040e60d3dbc5c127321653c47ed3a9d3.json) | 0.00% | 0.00%–18.94% |
| Fixed control 2 | [0/32](../TestResults/balance/tower-joined-mechanics-comparison-execution-20260915/exports/team-49f6979895354870c89362d4abf214bb.json) | 0.00% | 0.00%–18.94% |

All four confirmation recipes recorded zero wins. This provides no observed win/loss separation, and the wide intervals do not establish equivalence or zero true win probability.

Controls stayed outside both searches, their loadout libraries and screening. These are fresh paired measurements; earlier control results are not pooled. One shared generation-root value produces separate policy-defined random streams. This compares the implemented policies on one shallow restart; it does not isolate joining from stochastic differences or establish optimality/reliability.

| Prespecified contrast | Gained / lost paired wins | Difference (points) | Adjusted paired bounds (points) |
| --- | ---: | ---: | --- |
| joined-finalist-minus-baseline-finalist | 0 / 0 | 0.00 | -22.22 to 22.22 |
| baseline-finalist-minus-team-040e60d3dbc5c127321653c47ed3a9d3 | 0 / 0 | 0.00 | -22.22 to 22.22 |
| baseline-finalist-minus-team-49f6979895354870c89362d4abf214bb | 0 / 0 | 0.00 | -22.22 to 22.22 |
| joined-finalist-minus-team-040e60d3dbc5c127321653c47ed3a9d3 | 0 / 0 | 0.00 | -22.22 to 22.22 |
| joined-finalist-minus-team-49f6979895354870c89362d4abf214bb | 0 / 0 | 0.00 | -22.22 to 22.22 |

The primary is `joined-finalist-minus-baseline-finalist`. All four secondary control contrasts are retained. Rates use Wilson family factor 8; each gained/lost proportion uses factor 20, as frozen. These are conservative approximate intervals; identical deduplicated recipes would have an exact zero difference. No finalist is reselected from confirmation outcomes.

## Search and construction observations

| Policy | Distinct evaluated teams | Proposals | Discovery wins | Best mean guardian health remaining |
| --- | ---: | ---: | ---: | ---: |
| baseline | 44 | 45 | 0/176 | 85.11% |
| joined | 44 | 44 | 0/176 | 78.70% |

Both policies received **44 teams × 4 discovery seeds**. Their top two were frozen before screening on eight shared seeds; the actual screen contained **4 distinct recipes** and recorded **0/32 wins**. One winner per policy plus the two controls formed **4 distinct confirmation recipes** on 32 shared seeds. Exact duplicates would share screen/confirmation measurements without refilling seats; overlapping discovery recipes remain separately charged. All proposals, rejections, recipes, traces and observations above 50% are retained in the [measured results](../TestResults/balance/tower-joined-mechanics-comparison-execution-20260915/measured-results.json) and native archives.

The joined policy saved **19 fresh-proposal traces**, with **71 successful joined-group insertions** and **14 small-core fallbacks** across those traces. These are construction events across proposals/owners, not independent teams, wins or proof of synergy. The new construction path was exercised. Its ability to insert groups does not by itself establish that their selection, repetition across owners or subsequent search is strong enough. A longer search and other policies remain unmeasured.

## Workload and limits

| Phase | Completed fights | Native phase seconds |
| --- | ---: | ---: |
| baseline-discovery | 176 | 11.145 |
| joined-discovery | 176 | 6.473 |
| screen | 32 | 1.255 |
| confirmation | 128 | 3.050 |

Native execution took **22.282 seconds**. The full run command, including registry/input checks, took **63.469 seconds**; binding **41.187 seconds**, native verification **54.766 seconds**, and independent execution audit **17.812 seconds**. Total diagnostic workload including preparation/preservation is **241.030 / 1,800 seconds**. New output before publication is **277.06 MiB / 4 GiB**. Detailed phase timings, CPU, allocations and memory are saved in [performance.json](../TestResults/balance/tower-joined-mechanics-comparison-study-20260915/performance.json). The native snapshot precedes final sealing.

Both arms used incremental storage accounting. Baseline ran first, so its timing includes a different warm-up/cache position. No controlled performance speedup is inferred from these two native durations. No fight/time/storage cap was increased.

## Verification and reproducibility

The frozen preparation passed **40 tests through `build/run-tests.ps1`** and prepared both controls without combat; see the [readiness review](Tower-Joined-Mechanics-Comparison-Readiness-Review.md). Those tests were retained by hash, not rerun against altered code. Native post-run verification reconstructed both search trajectories, nominations, shared screen, finalists, confirmation family and archived results without new fights. The independent audit checked compact records, schedules, compositions, separate nominations, shared contexts, deduplication, all five contrasts, complete seed preservation and the durable **512-attempt** journal.

Commands executed once after approval:

```powershell
$py = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$w = 'TestResults/balance/tower-joined-mechanics-comparison-preparation-20260915'
& $py -B "$w/workflow.py" bind
& $py -B "$w/workflow.py" run
& $py -B "$w/workflow.py" verify
& $py -B "$w/workflow.py" audit-execution
& $py -B 'TestResults/balance/tower-joined-mechanics-comparison-execution-20260915/publish.py'
```

Do not rerun into these sealed directories. Reproduction requires a separate protocol/budget and preserves the exact producing executable, inputs and schedules. [Authorization](../TestResults/balance/tower-joined-mechanics-comparison-execution-20260915/authorization.json), [independent verification](../TestResults/balance/tower-joined-mechanics-comparison-execution-20260915/independent-execution.json), [study seal](../TestResults/balance/tower-joined-mechanics-comparison-study-20260915/final-files.json), [execution seal](../TestResults/balance/tower-joined-mechanics-comparison-execution-20260915/evidence-files.json), [completion](../TestResults/balance/tower-joined-mechanics-comparison-execution-20260915/completion.json).

Only the new study/execution artifacts, seed-free exports, this report and active Markdown were written. Existing harness/gameplay source, captured v19 and previous sealed experiments were preserved. No required command failed or was blocked. The full dirty gameplay build/backend suite was not rerun; execution used the already tested frozen snapshot. No configuration changes, migrations, deployment, Kharad tuning or ability-order search.

The approved comparison is complete. Any follow-up should first use these saved proposals/measurements to decide whether another comparison is justified; no additional allocation or combat is authorized here. Search-quality conclusions remain limited by four discovery fights, eight screen fights, 32 confirmations and one restart. Adoption remains **Hold**.
