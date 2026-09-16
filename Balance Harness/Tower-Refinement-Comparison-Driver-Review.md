# Discovery refinement comparison driver: measured result

16 September 2026. **FailurePreserved**. Driver verification exceeded its frozen test deadline. Both builds succeeded and all 117/117 tests passed late; this is not a successful bounded scope. No dependent execution or retry.

## Changed behavior

`TowerRefinementComparisonModel.cs` derives the existing paired comparison rules under a new policy/version. `TowerRefinementComparisonRun.cs` integrates discovery, the verified gate, screening, confirmation, archive verification, durable attempt charging, cancellation, campaign-owned storage accounting, performance/failure receipts and final reconstruction. New fixture/test files cover these boundaries without calling generation, preparation or combat. Historical code and sealed evidence remain unchanged.

Discovery retains 16 proposals/candidates and four discovery samples per candidate in each arm. Incomplete discovery stops the whole comparison, keeping rejection counts and charges. Two nominations per complete arm are frozen before the shared eight-sample screen. One finalist per arm plus the two controls form the deduplicated 32-sample confirmation family, retaining all origins. Ranking, tie-breaking, paired intervals and fixed ordinal ability order remain unchanged. The compiled production path is unexercised in this scope. Future execution is bounded at 288 attempts / 900 seconds / 384 MiB; these are controller ceilings, **not authorization to run**. No seed allocator or default CLI dispatch was added.

## Measured verification


Tests passed: **117/117**, through `build/run-tests.ps1`, but only after the eight-second phase limit. TRX start-to-finish: **11.2236 seconds**; the controller recorded its timeout after 8.031 seconds. The child termination attempt did not stop completion promptly. The original timeout-copy TRX contains the preceding 105-test run and is retained alongside the distinct late 117-test TRX; it is not current success evidence. Read-only process inspection found no remaining dotnet/testhost run. The late overrun, teardown allowance and triage are charged separately. Uncompleted dependent phases: **tests, captured, audit, preserve**. No retries. Failure detail, if any:

```
Traceback (most recent call last):
  File "C:\repos\Legends-Legacy\legends-legacy\TestResults\balance\tower-refinement-comparison-driver-20260916\workflow.py", line 47, in invoke
    try:code=child.wait(timeout=max(.1,deadline-time.monotonic()-1))
             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\HrHoe\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\Lib\subprocess.py", line 1264, in wait
    return self._wait(timeout=timeout)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\HrHoe\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\Lib\subprocess.py", line 1593, in _wait
    raise TimeoutExpired(self.args, timeout)
subprocess.TimeoutExpired: Command '['C:/Program Files/PowerShell/7/pwsh.exe', '-NoProfile', '-File', 'build/run-tests.ps1', '-NoBuild', '-ArtifactsPath', 'C:\\repos\\Legends-Legacy\\legends-legacy\\TestResults\\balance\\tower-refinement-comparison-driver-20260916\\tests', '-Filter', 'FullyQualifiedName~BalanceHarnessJointLoadoutTests|FullyQualifiedName~BalanceHarnessJointPartyTests|FullyQualifiedName~BalanceHarnessJointStructuralSearchTests|FullyQualifiedName~BalanceHarnessJointDiverseTests|FullyQualifiedName~BalanceHarnessTeamCoverageTests|FullyQualifiedName~BalanceHarnessFillerDiversityTests|FullyQualifiedName~BalanceHarnessCorePortfolioTests|FullyQualifiedName~BalanceHarnessDiscoveryRefinementTests|FullyQualifiedName~BalanceHarnessDiscoveryComparisonGateTests|FullyQualifiedName~BalanceHarnessRefinementComparisonTests']' timed out after 6.812999999994645 seconds

During handling of the above exception, another exception occurred:

Traceback (most recent call last):
  File "C:\repos\Legends-Legacy\legends-legacy\TestResults\balance\tower-refinement-comparison-driver-20260916\workflow.py", line 65, in run
    try:code=invoke(mode,['C:/Program Files/PowerShell/7/pwsh.exe','-NoProfile','-File','build/run-tests.ps1','-NoBuild','-ArtifactsPath',W/'tests',
             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\repos\Legends-Legacy\legends-legacy\TestResults\balance\tower-refinement-comparison-driver-20260916\workflow.py", line 49, in invoke
    subprocess.run(['taskkill','/PID',str(child.pid),'/T','/F'],capture_output=True);child.wait(timeout=1);raise
                                                                                     ^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\HrHoe\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\Lib\subprocess.py", line 1264, in wait
    return self._wait(timeout=timeout)
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\Users\HrHoe\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\Lib\subprocess.py", line 1593, in _wait
    raise TimeoutExpired(self.args, timeout)
subprocess.TimeoutExpired: Command '['C:/Program Files/PowerShell/7/pwsh.exe', '-NoProfile', '-File', 'build/run-tests.ps1', '-NoBuild', '-ArtifactsPath', 'C:\\repos\\Legends-Legacy\\legends-legacy\\TestResults\\balance\\tower-refinement-comparison-driver-20260916\\tests', '-Filter', 'FullyQualifiedName~BalanceHarnessJointLoadoutTests|FullyQualifiedName~BalanceHarnessJointPartyTests|FullyQualifiedName~BalanceHarnessJointStructuralSearchTests|FullyQualifiedName~BalanceHarnessJointDiverseTests|FullyQualifiedName~BalanceHarnessTeamCoverageTests|FullyQualifiedName~BalanceHarnessFillerDiversityTests|FullyQualifiedName~BalanceHarnessCorePortfolioTests|FullyQualifiedName~BalanceHarnessDiscoveryRefinementTests|FullyQualifiedName~BalanceHarnessDiscoveryComparisonGateTests|FullyQualifiedName~BalanceHarnessRefinementComparisonTests']' timed out after 1 seconds

```

The twelve new tests cover stage ordering/durable selections, reconstruction/tampering, either partial discovery arm, archive mismatch/failure, partial screen/confirmation, cancellation at outcome return, retry prevention, overrun/undercharging, storage before attempt, receipt write failure, changed definitions, invalid deadlines and precancellation. The planned Python/native diagnostic and final full preservation were not run after the timeout. The tests' passing synthetic checks do not substitute for these required phases. This scope does not repeat the earlier storage-scaling or combat-parity diagnostics.

| Phase before publication | Charged seconds |
| --- | ---: |
| build | 3.625 |
| freeze | 0.812 |
| late-test-accounting | 6.365 |
| test-build | 1.938 |

Incoming usage: **2,928.817 / 3,000 seconds**. The [completion receipt](../TestResults/balance/tower-refinement-comparison-driver-20260916/completion.json) records this scope's exact additional and remaining time, output and preservation status. The [frozen protocol](Tower-Refinement-Comparison-Driver-Protocol.md) limits this scope to 65 seconds / 128 MiB within the cumulative 4 GiB cap. It charges a conservative 16 MiB for temporary/shared test artifacts and one second for closure. No budget reset.

## Remaining boundary and reproduction

Preserve the timeout and late pass evidence. Repair reliable child-process termination and freeze a realistic test-phase allocation before any separate verification. Native fixtures, independent audit and final 72-package preservation remain unrun; the controller is compiled and unit-tested but verification is incomplete. A fresh-seed exception is still required before a new combat study; earlier seed approvals are exhausted. No numerical claim about stronger builds follows from fabricated outcomes. The current policy remains opt-in; adoption Hold.

Executed once, sequentially under the frozen protocol; missing success receipts identify unrun commands. Never rerun against a sealed/failed directory:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-comparison-driver-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/workflow.py" preserve
& $python -B "$work/close_failure.py"
```

Exact commands, logs, source/dependency hashes, TRX and receipts are preserved. Active Markdown handoffs are updated. Unrelated dirty work is preserved. No full gameplay build/full backend suite, real runtime preparation, combat comparison, balance tuning, configuration change, migration or deployment. Zero actual fights, fresh values, replays and retries. All **482,821 reservations** remain, including the original unused 512 v19 values. V19 retains all 253 recipes and Unresolved status; reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged.
