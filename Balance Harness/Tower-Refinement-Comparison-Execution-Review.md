# Refinement comparison execution review

16 September 2026. **FailurePreserved**. 120 completed / 120 charged fights; 45 fresh values durably recorded. Not available; no successful quality conclusion.

**Saved-receipt clarification:** Baseline completed 16/16 team evaluations (64 fights). Refinement completed 14/16 (56 fights): two proposals were rejected for missing team roles, exhausting its frozen 16-proposal allowance. The completeness gate stopped before nominations, selection and confirmation. No time or output cap was hit, and no retry ran.

| Discovery policy | Valid evaluated teams | Completed fights | Stop reason |
| --- | ---: | ---: | --- |
| Baseline | 16/16 | 64 | Candidate budget reached |
| Refinement | 14/16 | 56 | Two missing-team-roles rejections; proposal budget exhausted |

Native binding/launch elapsed 144.5401 seconds; the controller's discovery/archive phase was 8.8577 seconds, including its setup and verification. The difference includes binding, registry refreshes and other launcher work; no exact internal timing attribution or whole-run speedup is inferred. Study output was 64,967,358 bytes (61.96 MiB), below the 84 MiB cap.

All **482,866 reservations** remain retained. The 8 selection and 32 confirmation values allocated by this run remain unused and must not be reused; v19's separate 512 unused values remain untouched. The native discovery archives were reconstructed before the gate stopped the comparison; the final independent audit was skipped under the frozen first-failure rule. The previously passed 32 backend tests are reused evidence, not new test executions.

Next: inspect the two saved missing-team-roles rejections and repair refinement role-validity handling in a separately frozen zero-combat scope. Do not retry or resume this study, reuse its reserved values, alter ability order, tune the boss or increase old caps. The comparison remains incomplete; no strength conclusion is supported.

This clarification reads the [sealed gate receipt](../TestResults/balance/tower-refinement-comparison-study-20260916/run/discovery-gate.json). The original execution package and its captured report remain unchanged. The separate [closeout receipt](../TestResults/balance/tower-refinement-comparison-closeout-20260916/completion.json) adds only documentation-verification cost to the exact cumulative resources.

Boss health is descriptive; it does not replace the frozen win-rate endpoint. Native reconstruction and independent confirmation/statistics checks did not both complete. Pending reservation state: **False**; unresolved allocation start/partial transcript: **False**. Preserve all original 482,821 reservations and every newly derived/recorded value; uncertain Pending state forbids reuse. No retries or replays.

The [frozen execution protocol](Tower-Refinement-Comparison-Execution-Protocol.md) binds 45 fresh values, at most 288 attempts, 360 study seconds, 400 execution-task seconds and 84 MiB study output plus 4 MiB combined readiness/execution evidence. The explicit approval raises cumulative diagnostic time to 3,600 seconds and leaves cumulative output at 4 GiB. Exact usage and the first failure are in the [receipt](../TestResults/balance/tower-refinement-comparison-execution-20260916/completion.json). The [readiness review](Tower-Refinement-Comparison-Readiness-Review.md) preserves input/binary checks, 32 reused passing backend facts and 128 saved-record/four-rate reader checks.

First failure, if any:

```
Traceback (most recent call last):
  File "C:\repos\Legends-Legacy\legends-legacy\TestResults\balance\tower-refinement-comparison-readiness-20260916\execution.py", line 109, in main
    invoke('native',['dotnet',W/'host/EssenceSystem.Tests.dll',W,PRODUCING,'execute',E,'360'],start+364)
  File "C:\repos\Legends-Legacy\legends-legacy\TestResults\balance\tower-refinement-comparison-readiness-20260916\execution.py", line 25, in invoke
    assert result['exitCode']==0 and not result['timedOut'] and result['activeProcesses']==0,result
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
AssertionError: {'exitCode': 1, 'timedOut': False, 'rootPid': 17204, 'totalProcesses': 2, 'activeProcesses': 0, 'seconds': 144.60899999999674, 'workAllowanceSeconds': 363.32199999998556, 'cleanupAllowanceSeconds': 0.5, 'mechanism': 'suspended-owned-job-v1'}

```

The new study and execution evidence are sealed without retry. No gameplay, configuration, migration or deployment changes. Fixed ability order, v19 Unresolved, its 512 unused values and 253 recipes, later reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain. This is a single bounded exploratory policy comparison, not a reliability or optimality claim.

Executed once after the bound approval:

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B 'TestResults/balance/tower-refinement-comparison-readiness-20260916/execution.py'
```
