# Refinement comparison driver: verification closure

16 September 2026. **FailurePreserved**. Closure stopped in process-tests: the first normal-process fixture assertion failed. Preserve its logs and the outer zero-active-process receipt; timeout behavior has not yet been verified. No retry or dependent execution.

## Change and evidence

Added `build/bounded_windows_process.py` and isolated verification scripts. The wrapper creates a hidden suspended process, assigns an owned job before resuming, captures stdout/stderr, terminates the job on timeout, and verifies zero active descendants before returning. Nonzero exit remains nonzero. The job is closed on exceptions and has kill-on-close enabled; breakaway is not enabled. It follows Microsoft's [job-object documentation](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects). This scope did not verify the wrapper. No change to C# driver/model/search/test code, gameplay, ranking, recipes, fixed ability order or earlier evidence.

The first fixture printed the expected quoted/Unicode argument and captured stderr, then failed the combined assertion for exit code, timeout flag and exact one-process count. Its inner result was not persisted before the assertion, so the precise failing conjunct is unresolved. The outer job recorded exit 1, four lifetime processes and zero active processes in 0.094 seconds. Extra process accounting is a hypothesis, not an established diagnosis. Read-only executable metadata identifies Python 3.12.14; the checked pyvenv.cfg path does not exist. No subsequent fixture or native/audit/preservation phase ran. The first generated failure-publication script also had an indentation syntax error before execution; that file is retained and one conservative second is charged. The corrected publication script only closes this failed scope.

An **unexecuted** fixture correction is retained in `unrun-correction/process_fixtures.py`: persist every result before assertions and require at least the deliberately launched process count while still demanding zero active processes and the descendant-ready markers. The frozen failing script remains unchanged. This proposed correction does not establish that the wrapper is correct.

Reused **117/117 passing backend assertions** by verified exact current source, producing harness/test binary and sealed late-TRX identity. The original command used `build/run-tests.ps1`; its 11.2236-second test execution exceeded the old eight-second limit. That [failed scope](Tower-Refinement-Comparison-Driver-Review.md) remains FailurePreserved, fully charged and unchanged. No backend rebuild or test repetition in this closure. New process fixtures passed: **0/6**. Intentional timeout tests are successful negative tests, not retries.


The controller fixtures fabricate observations and journal events without invoking the generator, engine or materialization. Complete discovery freezes nominations before screening and finalists before confirmation, preserving ranking/ties and deduplicated origins. Partial baseline stops before the candidate arm; partial candidate stops before screening; charges and rejection diagnostics survive. Complete results reconstruct from saved stage evidence. These checks do not measure stronger builds or end-to-end campaign throughput. **Actual fights, runtime preparations, fresh seeds and retries: zero.**

Uncompleted phases: **process-tests, captured, audit, preserve**. Failure detail:

```
Traceback (most recent call last):
  File "C:\repos\Legends-Legacy\legends-legacy\TestResults\balance\tower-refinement-comparison-driver-closure-20260916\workflow.py", line 59, in run
    if mode=='process-tests':invoke(mode,[sys.executable,'-B',W/'process_fixtures.py','{remaining}'],deadline)
                             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
  File "C:\repos\Legends-Legacy\legends-legacy\TestResults\balance\tower-refinement-comparison-driver-closure-20260916\workflow.py", line 48, in invoke
    assert not result['timedOut'] and result['activeProcesses']==0 and result['exitCode']==0,result
           ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
AssertionError: {'exitCode': 1, 'timedOut': False, 'rootPid': 27180, 'totalProcesses': 4, 'activeProcesses': 0, 'seconds': 0.09399999999732245, 'workAllowanceSeconds': 6.1379999999917345, 'cleanupAllowanceSeconds': 0.75, 'mechanism': 'suspended-owned-job-v1'}

```

| Phase before publication | Charged seconds |
| --- | ---: |
| failure-publication-syntax | 1.000 |
| failure-triage | 1.000 |
| freeze | 0.187 |

Incoming usage: **2,950.823469456 / 3,000 seconds**; **49.176530544 seconds** remained. The [frozen protocol](Tower-Refinement-Comparison-Driver-Closure-Protocol.md) capped this scope at 45 seconds / 32 MiB and reserved four seconds for publication, including one closure second. The [completion receipt](../TestResults/balance/tower-refinement-comparison-driver-closure-20260916/completion.json) carries exact added/cumulative/remaining time and output. Prior output includes the failed scope's 16 MiB temporary/shared-test allowance. No cap increase or budget reset.

## Remaining work and reproduction

The first fixture must persist its own result before assertions and must not assume an exact lifetime process count. A proposed fixture correction is saved unexecuted; independently verify that correction and timeout behavior in a separately frozen scope before the driver fixtures and full preservation audit. No retry of this failed scope. Any fresh balance values still need a separate user exception; earlier approvals are exhausted. Adoption remains Hold. A catastrophic parent exit before job assignment can leave a suspended process; normal failures terminate it before it can run. This wrapper supports ordinary CreateProcess descendants; WMI launchers and breakaway mechanisms are outside this diagnostic contract. No claim of universal process-tree containment.

Executed once under the frozen scope; missing success receipts identify unrun commands. Never repeat against sealed/failed evidence:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-comparison-driver-closure-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" process-tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/workflow.py" preserve
& $python -B "$work/close_failure_final.py"
```

Exact commands, process completion/timeout receipts, fixture logs, input hashes, prior test proof, independent audit and preserved inventory are retained. Six active Markdown handoffs are updated and unrelated dirty files preserved. No configuration change, migration or deployment. No ordinary gameplay build, full backend suite, old experiment rerun or new combat comparison. All **482,821 reservations** remain, including the original unused 512 v19 values. V19 retains 253 recipes and Unresolved status; reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged.
