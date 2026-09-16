# Refinement preflight: verification closure

16 September 2026. **FailurePreserved**. Closure stopped in tests; preserve the failure and do not retry or run dependent diagnostics.

## What changed and what was verified

Added a separate frozen diagnostic workflow with build servers disabled using `--disable-build-servers -nr:false -m:1 -p:UseSharedCompilation=false`, plus process-local `DOTNET_CLI_USE_MSBUILD_SERVER=0` and `MSBUILDDISABLENODEREUSE=1`. The setting follows Microsoft's [MSBuild server documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-server) and [dotnet build documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build). No machine-wide server shutdown, process-containment relaxation or repository build-default change.

The previous harness compilation succeeded in **2.95 seconds**, zero warnings/errors, root exit 0. Its owned process group timed out after **6.156 seconds** and was emptied. Its descendant identity was not captured; persistent server reuse is a plausible explanation, not a proven diagnosis. The [failed scope](Tower-Refinement-Comparison-Preflight-Review.md) remains FailurePreserved. This closure reuses its exact sealed binary after source, inventory, compile-log and cleanup verification; it does not rerun the harness build or relabel the failed envelope as a success.

New test build: **1.6090s**, exit **0**, timed out **False**, lifetime processes **4**, active on return **0**. The old harness build and new test build compile different projects, so their timings are **not** a speedup comparison.

Backend results: **7/8 new preflight tests passed**. Missing test success receipts mean not verified; unrun tests are not failures. The command is `build/run-tests.ps1 -NoBuild -ArtifactsPath <this-scope>/tests -Filter FullyQualifiedName~BalanceHarnessRefinementPreflightTests`. The existing 117 driver assertions and six process-wrapper fixtures are prior evidence, not newly executed tests. All C# implementation, tests, driver, search and gameplay code remain unchanged in this closure.

Captured preflight was not executed successfully. These checks establish input identity only; no improved build strength, campaign throughput or balance acceptance is demonstrated.

The binding requires complete file pins, captured content/settings and four gameplay assemblies, both canonical controls sharing the generated context, the historical registry snapshot, the verified driver receipt and all **482,821 reservations**. It records `BoundAwaitingAuthorization`, `runAuthorized=false`, `requiresLiveRegistryRefresh=true`, 45 required future values and the unchanged 288-attempt comparison limit. It cannot reserve seeds, prepare combatants or launch fights. Independent checks reconstruct controls, reservation union and actual producing identities.

| Phase | Receipt | Charged seconds |
| --- | --- | ---: |
| freeze | result | 0.656 |
| test-build | result | 2.422 |
| tests | failure | 1.890 |

The entire immediate failed package and frozen consumed inputs were checked. The old 74-package full historical audit was **not repeated**, and the live registry was **not refreshed**. Earlier sealed evidence remains preserved. A historical registry snapshot does not exclude later reservations.

Unrun phases: **captured, audit**. Failed phase: **tests**. Failure detail:

```
Traceback (most recent call last):
  File "C:\repos\Legends-Legacy\legends-legacy\TestResults\balance\tower-refinement-comparison-preflight-closure-20260916\workflow.py", line 74, in run
    try:invoke(mode,['C:/Program Files/PowerShell/7/pwsh.exe','-NoProfile','-File','build/run-tests.ps1','-NoBuild','-ArtifactsPath',W/'tests','-Filter','FullyQualifiedName~BalanceHarnessRefinementPreflightTests'],deadline)
        ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\repos\Legends-Legacy\legends-legacy\TestResults\balance\tower-refinement-comparison-preflight-closure-20260916\workflow.py", line 59, in invoke
    assert not result['timedOut'] and result['activeProcesses']==0 and result['exitCode']==0,result
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
AssertionError: {'exitCode': 1, 'timedOut': False, 'rootPid': 27860, 'totalProcesses': 8, 'activeProcesses': 0, 'seconds': 1.7969999999913853, 'workAllowanceSeconds': 3.8219999999855645, 'cleanupAllowanceSeconds': 0.5, 'mechanism': 'suspended-owned-job-v1'}

```

## Resources and next boundary

Incoming usage was **2,985.933469456 / 3,000 seconds**, leaving **14.066530544 seconds**. The [frozen protocol](Tower-Refinement-Comparison-Preflight-Closure-Protocol.md) allows 13 seconds / 96 MiB within the unchanged cumulative 4 GiB cap. The [completion receipt](../TestResults/balance/tower-refinement-comparison-preflight-closure-20260916/completion.json) records exact added/cumulative/remaining time and output. Publication includes one conservative closure second; shared test output is charged 1 MiB. No reset or cap increase.

Resolve the recorded failure before separately frozen remaining verification; retain all earlier failures. Fresh-seed approvals are exhausted. A runnable future study needs concrete resource limits, explicit fresh-seed authorization and verified live reservation/allocation handling. No additional combat is authorized by this closure. Reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged.

Reproduction commands, each executed at most once. A started/result receipt distinguishes executed commands from the unrun phases above. Never repeat against sealed evidence:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-comparison-preflight-closure-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py" failure
```

All evidence actually produced is retained, including process receipts, logs, available TRX/binding/audit output and exact input hashes. Missing artifacts correspond to unrun or failed phases, not successful verification. Six active Markdown handoffs are updated; unrelated dirty files are preserved. Full backend suite, gameplay rebuild, live registry refresh and combat were not scheduled. **Zero fights, preparations, fresh values and retries.** All 482,821 reservations remain, including the unused 512 v19 values. V19 retains 253 recipes and Unresolved status. No gameplay/content changes, ability-order tuning, migration, configuration change or deployment.
