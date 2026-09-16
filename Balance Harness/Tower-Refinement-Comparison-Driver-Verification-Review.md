# Refinement comparison driver: verification closure

16 September 2026. **VerifiedRefinementComparisonDriver**. The process wrapper passed six Windows fixtures. All three synthetic controller cases, complete-result reconstruction, independent audit and the 74-package preservation pass succeeded. The unchanged controller retains its 117 passing backend assertions, reused by exact source/binary/TRX verification.

## Change and evidence

Verified the unchanged `build/bounded_windows_process.py` using corrected isolated fixtures. The fixtures now persist their complete process result before assertions and require at least the deliberately launched process count, while retaining the strict zero-active-process requirement and descendant-ready markers. Exact lifetime counts are reported rather than assumed. The preceding failed process fixture remains sealed; its precise failing conjunct was not recorded. Added a separate frozen verification workflow and report. The wrapper creates a hidden suspended process, assigns an owned job before resuming, captures stdout/stderr, terminates the job on timeout, and verifies zero active descendants before returning. Nonzero exit remains nonzero. The job is closed on exceptions and has kill-on-close enabled; breakaway is not enabled. It follows Microsoft's [job-object documentation](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects). No change to C# driver/model/search/test code, gameplay, ranking, recipes, fixed ability order or earlier evidence.

Reused **117/117 passing backend assertions** by verified exact current source, producing harness/test binary and sealed late-TRX identity. The original command used `build/run-tests.ps1`; its 11.2236-second test execution exceeded the old eight-second limit. That [failed scope](Tower-Refinement-Comparison-Driver-Review.md) remains FailurePreserved, fully charged and unchanged. No backend rebuild or test repetition in this closure. New process fixtures passed: **6/6**. Intentional timeout tests are successful negative tests, not retries.

| Process fixture | Timed out as intended | Exit code | Processes / active after return |
| --- | --- | ---: | --- |
| normal | False | 0 | 2 / 0 |
| nonzero | False | 7 | 2 / 0 |
| tree-timeout | True | 124 | 4 / 0 |
| exited-root | True | 0 | 3 / 0 |
| expired | prelaunch rejection | — | — / — |
| missing | prelaunch rejection | — | — / — |

Three-case native fixture cost: **3.8962s**, **2.0000 CPU seconds**, **85,544,968 allocated bytes**, **84,262,912 peak working-set bytes**.

| Controller fixture | Gate | Synthetic charged attempts |
| --- | --- | ---: |
| complete | Ready | 240 |
| partial-baseline | StoppedDiscovery | 56 |
| partial-candidate | StoppedDiscovery | 120 |

Preservation: **74 complete packages**, **27,473 files**, **4,632,271,140 bytes**, **15.047s**. Four hash workers, unchanged legacy assertions and a fresh per-pass cache. This single audit is not a speedup comparison.


The controller fixtures fabricate observations and journal events without invoking the generator, engine or materialization. Complete discovery freezes nominations before screening and finalists before confirmation, preserving ranking/ties and deduplicated origins. Partial baseline stops before the candidate arm; partial candidate stops before screening; charges and rejection diagnostics survive. Complete results reconstruct from saved stage evidence. These checks do not measure stronger builds or end-to-end campaign throughput. **Actual fights, runtime preparations, fresh seeds and retries: zero.**

Uncompleted phases: **none**. Failure detail:

```
None.
```

| Phase before publication | Charged seconds |
| --- | ---: |
| audit | 0.328 |
| captured | 4.078 |
| freeze | 0.157 |
| preserve | 15.235 |
| process-tests | 2.266 |

Incoming usage: **2,954.448469456 / 3,000 seconds**; **45.551530544 seconds** remained. The [frozen protocol](Tower-Refinement-Comparison-Driver-Verification-Protocol.md) capped this scope at 43 seconds / 32 MiB and reserved four seconds for publication, including one closure second. The [completion receipt](../TestResults/balance/tower-refinement-comparison-driver-verification-20260916/completion.json) carries exact added/cumulative/remaining time and output. Prior output includes the failed scope's 16 MiB temporary/shared-test allowance. No cap increase or budget reset.

## Remaining work and reproduction

Controller verification is complete for the injected synthetic transport. Next is a separately frozen launcher/preflight that binds captured content, controls, producing binaries, the historical reservation registry and approved seed schedules. No live compact-runtime comparison or search-strength improvement has been demonstrated. Any fresh balance values still need a separate user exception; earlier approvals are exhausted. Adoption remains Hold. A catastrophic parent exit before job assignment can leave a suspended process; normal failures terminate it before it can run. This wrapper supports ordinary CreateProcess descendants; WMI launchers and breakaway mechanisms are outside this diagnostic contract. No claim of universal process-tree containment.

Executed once under the frozen scope; missing success receipts identify unrun commands. Never repeat against sealed/failed evidence:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-comparison-driver-verification-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" process-tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/workflow.py" preserve
& $python -B "$work/publish.py"
```

Exact commands, process completion/timeout receipts, fixture logs, input hashes, prior test proof, independent audit and preserved inventory are retained. Six active Markdown handoffs are updated and unrelated dirty files preserved. No configuration change, migration or deployment. No ordinary gameplay build, full backend suite, old experiment rerun or new combat comparison. All **482,821 reservations** remain, including the original unused 512 v19 values. V19 retains 253 recipes and Unresolved status; reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged.
