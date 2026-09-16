# Refinement comparison execution review

16 September 2026. **VerifiedRefinementComparison**. 288 completed / 288 charged fights; 45 fresh values durably recorded. Primary paired difference: 0.00 percentage points, adjusted interval [-22.22, 22.22]. This pilot does not establish improvement over the baseline.

| Confirmation family origins | Wins / fights | Mean boss health remaining |
| --- | ---: | ---: |
| baseline-finalist | 0/32 | 62.485% |
| discovery-refinement-finalist | 0/32 | 72.2175% |
| team-040e60d3dbc5c127321653c47ed3a9d3 | 0/32 | 30.73% |
| team-49f6979895354870c89362d4abf214bb | 0/32 | 32.95% |

Boss health is descriptive; it does not replace the frozen win-rate endpoint. Native reconstruction and independent confirmation/statistics checks completed. Pending reservation state: **False**; unresolved allocation start/partial transcript: **False**. All 482,956 pre-existing reservations and all 45 new values remain reserved: **483,001 total**. The current study is Complete; older Pending reservations remain excluded and cannot be reused. No retries or replays.

The [frozen execution protocol](Tower-Local-Refinement-Comparison-Protocol.md) binds 45 fresh values, at most 288 attempts, 360 study seconds, 400 execution-task seconds and 84 MiB study output plus 24 MiB producing runtime and 4 MiB execution evidence, with readiness charged separately. The explicit approval raises cumulative diagnostic time to 4,260 seconds and cumulative output to 4 GiB + 304 MiB. Exact usage and the first failure are in the [receipt](../TestResults/balance/tower-local-refinement-comparison-execution-20260916/completion.json). The [readiness review](Tower-Local-Refinement-Comparison-Readiness-Review.md) preserves input/binary checks, 69 reused passing backend tests and 128 saved-record/four-rate reader checks.

Measured completion: native execution **77.2364 seconds**, complete sealed wrapper **80.718 seconds**. Final study output **85,264,891 bytes** (81.315 MiB); execution output including runtime and seal **19,104,346 bytes** (18.219 MiB). Runtime accounts for 18,392,054 bytes; execution evidence accounts for 712,292 bytes. Every allocation, attempt and file remains within the approved caps.

A final once-only publication check verified exact membership and hashes of 71 readiness files, 50 execution files and 556 study files, with zero fights, seeds or replays. It charges ten seconds conservatively in full; execution plus publication is 90.718 seconds. Cumulative diagnostic usage is **3874.827272 / 4,260 seconds** and **4,598,743,362 / 4,613,734,400 bytes**, including the [publication receipt](../TestResults/balance/tower-local-refinement-comparison-publication-20260916.json). The sealed execution snapshot retains its original prose; this live review corrects its stale 482,911 count to the 482,956 actual pre-run reservations checked by both verifiers.

V4 left **9.7325 percentage points more boss health** than the baseline in confirmation. This descriptive result and the primary adjusted interval provide no reason to replace the baseline. The next useful analysis is to inspect the already saved nine local edits and nominations; no new seed allocation or combat is authorized. No earlier study was modified, resumed or replayed.

Verification: the production execution and independent reader both exited successfully; archive reconstruction, durable attempt charges, identical input bindings, fixed order, one-slot/one-Essence edits, nominations and paired statistics passed. The 69 existing passing backend tests were reused as frozen; no backend tests or builds were rerun. Checkout preservation checked 764 pre-existing files with no concurrent differences. Changed files are the new study/execution evidence and review plus the six active Markdown handoffs. No command required by this frozen execution was skipped. A live progress read encountered the attempt log's exclusive write lock; final counts were read successfully after process completion and both verifiers agreed on all 288 charges.

First failure, if any:

```
None.
```

The new study and execution evidence are sealed without retry. No gameplay, configuration, migration or deployment changes. Fixed ability order, v19 Unresolved, its 512 unused values and 253 recipes, later reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain. This is a single bounded exploratory policy comparison, not a reliability or optimality claim.

Executed once after the bound approval:

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B 'TestResults/balance/tower-local-refinement-comparison-readiness-20260916/execution.py'
```
