# Refinement comparison execution review

16 September 2026. **VerifiedRefinementComparison**. 288 completed / 288 charged fights; 45 fresh values durably recorded. Primary paired difference: 0.00 percentage points, adjusted interval [-22.22, 22.22]. This pilot does not establish improvement over the baseline.

| Confirmation family origins | Wins / fights | Mean boss health remaining |
| --- | ---: | ---: |
| baseline-finalist | 0/32 | 62.66% |
| discovery-refinement-finalist | 0/32 | 72.62% |
| team-040e60d3dbc5c127321653c47ed3a9d3 | 1/32 | 27.18% |
| team-49f6979895354870c89362d4abf214bb | 3/32 | 28.30% |

Boss health is descriptive; it does not replace the frozen win-rate endpoint. Native reconstruction and independent confirmation/statistics checks completed. Pending reservation state: **False**; unresolved allocation start/partial transcript: **False**. Preserve all original 482,911 reservations and every newly derived/recorded value; uncertain Pending state forbids reuse. No retries or replays.

The [frozen execution protocol](Tower-Refinement-V3-Recovered-Comparison-Protocol.md) binds 45 fresh values, at most 288 attempts, 360 study seconds, 400 execution-task seconds and 84 MiB study output plus 4 MiB execution evidence, with readiness charged separately. The explicit approval raises cumulative diagnostic time to 4,080 seconds and cumulative output to 4 GiB + 192 MiB. Exact usage and the first failure are in the [receipt](../TestResults/balance/tower-refinement-v3-recovered-comparison-execution-20260916/completion.json). The [readiness review](Tower-Refinement-V3-Recovered-Comparison-Readiness-Review.md) preserves input/binary checks, 45 reused passing backend tests and 128 saved-record/four-rate reader checks.

First failure, if any:

```
None.
```

The new study and execution evidence are sealed without retry. No gameplay, configuration, migration or deployment changes. Fixed ability order, v19 Unresolved, its 512 unused values and 253 recipes, later reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain. This is a single bounded exploratory policy comparison, not a reliability or optimality claim.

Executed once after the bound approval:

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B 'TestResults/balance/tower-refinement-v3-recovered-comparison-readiness-20260916/execution.py'
```
