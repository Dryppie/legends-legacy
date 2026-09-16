# Team coverage versus structural diversity: execution

**VerifiedExploratoryComparison**. 288 fights verified. Team-coverage-minus-diversity win-rate difference: 0.00 percentage points; adjusted paired bounds -22.22 to 22.22. Improved win probability is unestablished.

| Frozen confirmation origin | Wins | Mean boss health remaining |
| --- | ---: | ---: |
| baseline-finalist | 0/32 | 79.30% |
| team-coverage-finalist | 0/32 | 63.06% |
| team-040e60d3dbc5c127321653c47ed3a9d3 | 0/32 | 30.58% |
| team-49f6979895354870c89362d4abf214bb | 0/32 | 29.90% |

Lower remaining boss health is better. The team-coverage finalist left **16.24 percentage points less health** than the diversity finalist on these confirmation seeds, but both controls left substantially less health than either generated finalist. This is a descriptive difference between the selected finalists, not an established win-rate improvement or evidence that the whole candidate pool improved. All four confirmation teams lost every fight, and the adjusted primary bounds still span -22.22 to +22.22 percentage points. The result does not justify adoption or boss tuning.

Executed under the [frozen protocol](Tower-Team-Coverage-Comparison-Protocol.md) and explicit fresh-seed exception recorded in [authorization](../TestResults/balance/tower-team-coverage-comparison-execution-20260915/authorization.json). Fresh values: 45; total reserved: 482731. Unresolved partial counts remain null rather than being guessed. Zero retries/resumes/replays. All old experiments and their caps are unchanged.

The [independent audit](../TestResults/balance/tower-team-coverage-comparison-execution-20260915/independent-execution.json) reconstructs archived records, nominations, screen winners, complete confirmation family, attempt/seed charges and adjusted intervals. Native verification independently validates durable archives and selection. The [descriptive metrics](../TestResults/balance/tower-team-coverage-comparison-execution-20260915/descriptive-metrics.json) retain boss health, duration, missingness and paired health differences. These secondary measurements never influence confirmation selection. No historical pooling, reliability pass, global optimality or default adoption claim.

## Measured time and remaining budget

| Execution phase | Charged seconds |
| --- | ---: |
| Authorization receipt | 0.078 |
| Registry recheck and durable binding | 39.453 |
| Run command, including checks | 53.797 |
| Native archive verification | 44.156 |
| Independent execution audit | 6.844 |
| Publication, including one-second closure allowance | 5.343 |
| **Execution total** | **149.671** |

The [native study measurement](../TestResults/balance/tower-team-coverage-comparison-study-20260915/performance.json) records **14.901 seconds** for its 288-fight phase, including preparation and archive work. That time is contained within the 53.797-second run command, not an additional charge or isolated combat-simulation time. These are single-run timings; this comparison does not establish a filesystem speedup or whole-campaign throughput improvement.

Preparation previously charged **80.438 seconds**, so this comparison's preparation plus execution charged **230.109 seconds**. The [sealed completion receipt](../TestResults/balance/tower-team-coverage-comparison-execution-20260915/completion.json) records **2,291.022 / 2,400 cumulative diagnostic seconds**, leaving **108.978 seconds (1m49s)** at publication. Its cumulative output accounting is **2,990,065,349 bytes before the final seal**, approximately **2.78 GiB / 4 GiB**, including the shared-test allowance. Future diagnostic work must carry these totals forward and reserve its own closure time; the completed 45-value approval is exhausted. This Markdown clarification runs no diagnostics or combat and changes no reservation or sealed receipt.

Failure receipt names: none. All 54 predecessor packages and the sealed comparison preparation were verified unchanged. All 32 generated teams and construction traces matched the sealed fixtures; all 288 starts/completions and the complete 482,731-value reservation union reconciled. Existing preparation's eight tests through `build/run-tests.ps1` were reused, not rerun. No full gameplay build/backend suite was run. No gameplay, configuration, migration or deployment changes.

## Next scoped work

Prepare one bounded **saved-data diagnosis**, with zero fights and zero fresh values, before changing the search policy. Compare the strongest generated recipes and their construction/allocation traces with both fixed controls. Trace missing compatible Essence combinations back to the bounded recipe pool and distinguish pool omissions from allocation choices; the earlier [implementation review](Tower-Team-Coverage-Review.md) already established that no exact control loadout appears in the 560-recipe pool. Role coverage and legal repetition alone have not closed the combat gap.

Freeze the exact reads, comparisons, diagnostic deadline and output allowance before execution, using the remaining cumulative budget. The diagnosis should identify whether the saved evidence supports a generic construction change; it must not insert control recipes, optimize ability order, alter caps or launch more fights. Neither a larger experiment nor a new implementation is authorized by this documentation update.

Reproduction commands and limits remain in the [historical readiness report](Tower-Team-Coverage-Comparison-Readiness-Review.md); all listed comparison phases have now completed, and no sealed or failed directory may be rerun. Exported exact finalist/control scenarios retain all merged origins. The original report snapshot remains unchanged inside the sealed execution package; this working Markdown adds the measured completion details and next handoff.

V19 retains all 253 recipes and unused 512 confirmation values. Reliability Fail 1/3, deep recovery 0/3, v19 Unresolved, adoption Hold. No Kharad tuning, ability-order optimization or 129,536-fight confirmation.
