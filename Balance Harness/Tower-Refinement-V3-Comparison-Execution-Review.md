# Refinement comparison execution review

16 September 2026. **FailurePreserved**. 0 completed / 0 charged fights; 45 fresh values durably recorded. Not available; no successful quality conclusion.

**Saved-log clarification:** the 150-second binding deadline expired during `TowerRefinementComparisonLaunch.Recheck` while `TowerHistoryRegistry.Scan` enumerated the complete registry for the second time. The first scan and all 45 allocations completed, but binding never published Complete and combat never started. Native elapsed time was **150.0975 seconds**; the complete task charged **151.4530 seconds**. This was the internal binding deadline, not the outer 400-second task limit or a failed combat result. The frozen binding allowance was insufficient for this run's registry validation.

All **482,911 values** now remain reserved, including these **45 unused Pending values**, the prior failed comparison's 40 unused values, and V19's separate 512 unused values. Pending evidence forbids reuse or rebinding; no reservation was released. Study output was **16,091,121 bytes (15.35 MiB)**. The native process exited with zero active descendants; no retry, resume or replay ran. Source/gameplay and 713 unrelated dirty files passed preservation checks. Native reconstruction and the independent combat-result audit were not reached. The earlier 60 passing backend tests remain reused evidence, not newly executed tests.

The immediate unresolved blocker is the cost of reservation-history validation and the retained Pending reservation. Diagnose those using saved evidence before proposing another separately bounded run. No further seeds or fights are authorized. This clarification comes from [native metrics](../TestResults/balance/tower-refinement-v3-comparison-execution-20260916/native-metrics.json) and the sealed study's `binding-failure.json`; the sealed execution package and its original report copy remain unchanged.

| Confirmation family origins | Wins / fights | Mean boss health remaining |
| --- | ---: | ---: |


Boss health is descriptive; it does not replace the frozen win-rate endpoint. Native reconstruction and independent confirmation/statistics checks did not both complete. Pending reservation state: **True**; unresolved allocation start/partial transcript: **False**. Preserve all original 482,866 reservations and every newly derived/recorded value; uncertain Pending state forbids reuse. No retries or replays.

The [frozen execution protocol](Tower-Refinement-V3-Comparison-Protocol.md) binds 45 fresh values, at most 288 attempts, 360 study seconds, 400 execution-task seconds and 84 MiB study output plus 4 MiB execution evidence, with readiness charged separately. The explicit approval raises cumulative diagnostic time to 3,840 seconds and cumulative output to 4 GiB + 128 MiB. Exact usage and the first failure are in the [receipt](../TestResults/balance/tower-refinement-v3-comparison-execution-20260916/completion.json). The [readiness review](Tower-Refinement-V3-Comparison-Readiness-Review.md) preserves input/binary checks, 60 reused passing backend facts and 128 saved-record/four-rate reader checks.

First failure, if any:

```
Traceback (most recent call last):
  File "C:\repos\Legends-Legacy\legends-legacy\TestResults\balance\tower-refinement-v3-comparison-readiness-20260916\execution.py", line 109, in main
    invoke('native',['dotnet',W/'host/EssenceSystem.Tests.dll',W,PRODUCING,'execute',E,'360'],start+364)
  File "C:\repos\Legends-Legacy\legends-legacy\TestResults\balance\tower-refinement-v3-comparison-readiness-20260916\execution.py", line 25, in invoke
    assert result['exitCode']==0 and not result['timedOut'] and result['activeProcesses']==0,result
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
AssertionError: {'exitCode': 1, 'timedOut': False, 'rootPid': 40860, 'totalProcesses': 2, 'activeProcesses': 0, 'seconds': 150.18700000000536, 'workAllowanceSeconds': 363.3220000000001, 'cleanupAllowanceSeconds': 0.5, 'mechanism': 'suspended-owned-job-v1'}

```

The new study and execution evidence are sealed without retry. No gameplay, configuration, migration or deployment changes. Fixed ability order, v19 Unresolved, its 512 unused values and 253 recipes, later reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain. This is a single bounded exploratory policy comparison, not a reliability or optimality claim.

Executed once after the bound approval:

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B 'TestResults/balance/tower-refinement-v3-comparison-readiness-20260916/execution.py'
```
