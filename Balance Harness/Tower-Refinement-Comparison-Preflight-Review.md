# Refinement comparison: seed-free preflight review

16 September 2026. **FailurePreserved**. Preflight stopped in build; retain the failure and do not retry or run dependent checks.

## Change and verification

Added `LL/tools/BalanceHarness/TowerRefinementComparisonPreflight.cs` and eight focused tests in `LL/tests/EssenceSystem.Tests/BalanceHarnessRefinementPreflightTests.cs`. Added an isolated captured preflight entry point and frozen workflow. The preflight verifies required file pins before and after reads, content/settings identity, the four captured gameplay assemblies, canonical control recipes and generated context, complete reservation count, the sealed registry snapshot and the verified driver receipt. It returns `BoundAwaitingAuthorization`, `runAuthorized=false`, `requiresLiveRegistryRefresh=true`, with 45 required future values and the unchanged 288-attempt comparison limit. It does not allocate values, invoke preparation or expose a combat command. This is a real input-binding component; it is not a complete execution launcher.

Backend results: **0/8 new facts passed**, using `build/run-tests.ps1` with the isolated artifacts and `FullyQualifiedName~BalanceHarnessRefinementPreflightTests`. The existing 117 driver assertions were not rerun; exact unchanged source and sealed prior test evidence are retained. Captured builds use the historical gameplay assemblies, not the dirty gameplay checkout. The six process-wrapper fixtures were reused by exact hash; no process-fixture repetition. The driver, search policy, nominations, combat implementation and ability-order rule are unchanged.

No captured preflight metrics were produced.

| Phase before publication | Charged seconds |
| --- | ---: |
| freeze | 0.656 |

This scope rechecks **consumed pinned inputs and producing binaries**. It does **not** repeat the previous 74-package full audit or scan the live reservation registry. The previous [full audit](../TestResults/balance/tower-refinement-comparison-driver-verification-20260916/control/final-preservation-metrics.json) remains sealed. The registry snapshot is historical evidence, not proof that no later reservation exists. No future allocation may rely on it without live refresh.

Uncompleted phases: **build, test-build, tests, captured, audit**. Failure detail:

```
Traceback (most recent call last):
  File "C:\repos\Legends-Legacy\legends-legacy\TestResults\balance\tower-refinement-comparison-preflight-20260916\workflow.py", line 117, in run
    invoke(mode,['dotnet','build',W/('test-compiler/Harness.csproj' if tests else 'compiler/Harness.csproj'),'-c','Release','--no-restore','-o',output],deadline)
  File "C:\repos\Legends-Legacy\legends-legacy\TestResults\balance\tower-refinement-comparison-preflight-20260916\workflow.py", line 81, in invoke
    assert not result['timedOut'] and result['activeProcesses']==0 and result['exitCode']==0,result
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
AssertionError: {'exitCode': 0, 'timedOut': True, 'rootPid': 63644, 'totalProcesses': 4, 'activeProcesses': 0, 'seconds': 6.155999999988126, 'workAllowanceSeconds': 6.152999999991152, 'cleanupAllowanceSeconds': 0.75, 'mechanism': 'suspended-owned-job-v1'}

```

## Resources and next boundary

Incoming diagnostic usage was **2,977.793469456 / 3,000 seconds**, leaving **22.206530544 seconds**. The [frozen protocol](Tower-Refinement-Comparison-Preflight-Protocol.md) allows 21 seconds / 96 MiB within the unchanged cumulative 4 GiB cap, with four seconds reserved for publication. The [completion receipt](../TestResults/balance/tower-refinement-comparison-preflight-20260916/completion.json) records exact measured cumulative usage, remaining time and output accounting. Shared test output is conservatively charged 1 MiB. No reset or cap increase.

Resolve the recorded failure in a separately frozen scope within the remaining resources. Earlier fresh-seed approvals are exhausted. Do not squeeze a combat comparison into the remaining diagnostic seconds. A future runnable study needs a separate concrete resource proposal and explicit fresh-seed exception. The binding does not establish improved optimizer strength, reliability or throughput.

Reproduction commands (executed once; do not rerun against sealed output):

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-comparison-preflight-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py" failure
```

Exact child commands and completion/timeout receipts, compiler/test logs, TRX, source snapshots, request pins and the independent audit are retained. The dirty checkout was snapshotted before edits and unrelated files checked at publication. Six active Markdown handoffs were updated. Full backend-suite execution, gameplay rebuild, live registry refresh, materialization and combat were not scheduled. **Zero fights, fresh values or retries.** All **482,821 reservations**, including the unused 512 v19 values, remain preserved. V19 retains 253 recipes and Unresolved status; reliability Fail 1/3, deep recovery 0/3 and adoption Hold are unchanged. No migration, configuration change or deployment.
