# Refinement comparison readiness review

16 September 2026. **ReadyAwaitingSpecificApproval**. The exact 45-value / 288-attempt comparison request, thin execution host, once-only launcher and independent result checker are prepared. Native pinned preflight and independent readiness checks passed; the reader reproduced 128 existing confirmation records and four adjusted rates. Zero new values or fights.

The [execution protocol](Tower-Refinement-Comparison-Execution-Protocol.md) freezes the two search policies, identical captured gameplay/context/settings, fixed ordinal ability order, nomination/tie-breaking rules, schedules, control family, outputs and first-failure behavior. Request: [exact JSON](../TestResults/balance/tower-refinement-comparison-readiness-20260916/request.json). The authorization template is an unsigned request artifact, not permission; no production authorization or approval file exists.

| Stage | Maximum teams | Fights each | Maximum fights |
| --- | ---: | ---: | ---: |
| Baseline discovery | 16 | 4 | 64 |
| Refinement discovery | 16 | 4 | 64 |
| Shared selection | 4 | 8 | 32 |
| Two finalists plus two controls | 4 | 32 | 128 |

Total **288 attempts**, **45 fresh values** (one generation, four discovery, eight selection, 32 confirmation). Duplicates retain all origins and may reduce total fights. The pilot asks whether refinement selects a stronger team on this captured boss/context. It cannot establish global optimality or reliability; adoption remains Hold.

The study cap is **84 MiB**, compared with the measured **73.01 MiB component minimum**. The **10.99 MiB difference** covers remaining study output within the enforced cap; it is not a proven bound on that output. A **4 MiB combined readiness/execution allowance** makes the full proposed new-output reservation **88 MiB**, below the available **90.17 MiB** at preparation start. No output-cap increase is requested. Completion remains uncertain: any cap, cancellation, incomplete discovery or failed verification ends the run with evidence retained and no retry.

Study deadline: **360 seconds**, with **150 seconds for binding conservatively charged in full**, leaving **210 seconds for launch, its live refresh, combat and native reconstruction**. The complete post-approval task is at most **400 seconds**, including process cleanup, a 30-second independent audit and publication. It fits only after the requested **360-second extension** to the cumulative ceiling. This increases no old experiment cap. Three full registry traversals remain mandatory; the previous 56.8624-second Check is context, not a runtime guarantee.

The preparation host executed only request validation and pinned preflight. It did not call global registry Check, an allocator, actor preparation, batch creation or combat. All production seed derivation is deferred until approval and durable binding. The future execution host uses the tested public ReserveAndBind/Run entry points; Run includes native reconstruction before successful publication. The independent checker recounts raw records, attempts, the exact allocation transcript, histories, selection winners, family origins and adjusted confirmation statistics.

**Verification:** reuse the pinned **32/32 backend facts** previously run through `build/run-tests.ps1`. No harness or gameplay implementation changed, no backend tests were added and none were rerun. Independent reader self-check: **128 saved records / four rates passed**; this reads evidence without fights or replays. Full adaptive/combat integration remains the proposed bounded execution; it was not simulated or claimed complete here.

Await explicit approval for 45 fresh values and a 360-second cumulative diagnostic extension (3,240 to 3,600 seconds). Keep cumulative output at 4 GiB, zero retries and the sealed 360-second / 84 MiB study cap. After approval, execute the prepared command once; no more standalone setup/registry checks.

| Completed phase before publication | Charged seconds |
| --- | ---: |
| audit | 0.2970 |
| build | 1.7340 |
| freeze | 0.2650 |
| inspect | 0.5310 |

Uncompleted phases: **none**. First failure:

```
None.
```

Preparation is capped at **20 seconds / 3 MiB**, inside the combined 4 MiB evidence allowance and charged against the existing 3,240-second ceiling. The [completion receipt](../TestResults/balance/tower-refinement-comparison-readiness-20260916/completion.json) records exact usage/remaining resources. The requested extension is not applied by readiness. Preserve all **482,821 reservations**, including v19's unused **512 values** and **253 recipes**. V19 Unresolved, later reliability Fail 1/3, deep recovery 0/3, fixed ability order and adoption Hold remain.

Changed files: new request/protocol/readiness review, thin host and diagnostic/execution scripts; six active Markdown handoffs. Source/gameplay, configuration and migrations are unchanged; nothing was deployed. Unrelated dirty files and consumed sealed artifacts were hash-checked.

Executed once; never rerun this prepared path:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-comparison-readiness-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" inspect
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
# Still unrun; requires the exact recorded user approval and authorization:
& $python -B "$work/execution.py"
```
