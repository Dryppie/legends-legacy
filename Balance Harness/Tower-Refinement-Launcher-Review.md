# Refinement launcher: measured integration review

16 September 2026. **VerifiedSyntheticRefinementLauncher**. Ten backend tests and the complete synthetic reservation/launch/reconstruction case passed. The independent audit verified the literal schedules, durable journal, binding and archive inventories. Production allocation and combat were not invoked.

## Implementation and validation

Added `TowerRefinementComparisonLaunch.cs`, `TowerRefinementReservation.cs`, `BalanceHarnessRefinementLaunchFixture.cs` and `BalanceHarnessRefinementLaunchTests.cs`. The public APIs connect the verified preflight, complete registry scanning, durable 1+4+8+32 reservation, existing comparison controller and final archive reconstruction. Both reservation and launch require the exact externally authorized request/protocol, 45 fresh values, 288 maximum attempts and zero retries. No CLI/default routing or gameplay change.

Pending history is written before deriving a candidate. Start and returned Candidate events are flushed durably; returned labels are recorded before cancellation is observed. Definitions and external history are checked before Complete/binding publication. Failures retain all labels and prohibit retry. Launch rechecks history and immutable binding, deducts the entire binding-time allowance, passes the remaining time/output limits to the unchanged controller, verifies returned results against archives and checks the final inventory. Storage scans occur at operation boundaries, never on each fight. The shared complete-family allocation lease coordinates participating allocators; unrelated legacy allocators still require exclusive operation. This does not claim an atomic snapshot against arbitrary writers.

Backend result: **10/10 tests passed** through `build/run-tests.ps1 -NoBuild` with the isolated artifacts and `FullyQualifiedName~BalanceHarnessRefinementLaunchTests`. The native case uses the same binding/launch code with literal labels and the existing fabricated controller transport. No production seed derivation, combat engine, preparation or complete live-registry traversal is invoked. Existing eight preflight tests, 117 driver tests and six process-wrapper fixtures remain prior evidence. Builds use captured gameplay assemblies and disabled build-server reuse.

Synthetic binding/launch/reconstruction: **2.7434s**, **1.6719 CPU seconds**, **88,235,688 allocated bytes**, **82,341,888 peak working-set bytes**. This is a fixture latency measurement, not a throughput or search-strength comparison.

| Phase before publication | Charged seconds |
| --- | ---: |
| audit | 0.453 |
| build | 3.781 |
| captured | 3.109 |
| freeze | 0.828 |
| test-build | 2.594 |
| tests | 7.875 |

Uncompleted phases: **none**. Failure detail:

```
None.
```

Synthetic ledgers are confined to a dedicated temporary root outside the production registry. The retained case is a ZIP, so its fake ledger names cannot register as real history. All temporary bytes and a 1 MiB shared-test allowance are charged. The independent audit checks all ZIP hashes, four schedules, durable journal, two definitions, binding allowance and final archived results. The current 482,821-value real ledger is pinned unchanged. The old full historical audit and real live-registry refresh were not repeated.

## Resources and next boundary

Incoming usage: **3,003.574469456 / 3,030 seconds**, **26.425530544 seconds remaining**. The [frozen protocol](Tower-Refinement-Launcher-Protocol.md) caps this scope at 25 seconds / 96 MiB inside the unchanged cumulative 4 GiB cap, with four seconds reserved for publication. The [completion receipt](../TestResults/balance/tower-refinement-launcher-20260916/completion.json) records exact added/cumulative/remaining time and output, including temporary evidence. No new cap increase or reset.

Launcher integration is verified on synthetic inputs. Next is a separately budgeted complete live-registry check and a frozen real request/authorization package; no fresh values or combat are authorized. The prior complete registry traversal took 57.467 seconds, beyond the remaining allowance. The previous [registry measurement](Tower-History-Registry-Performance-Review.md) is the timing basis. Actual compact-runtime integration, production allocation and build-strength improvement remain unmeasured in this comparison. Adoption Hold, reliability Fail 1/3 and deep recovery 0/3 remain unchanged.

Executed commands (each at most once; missing receipts identify unrun phases). Never rerun sealed/failed paths:

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-launcher-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

Six active Markdown handoffs are updated and unrelated dirty files preserved. Logs, test counters/TRX, process receipts, captured sources and produced audits are retained; missing outputs are not success evidence. Full backend suite, ordinary gameplay build, complete registry traversal, actual allocation and combat were not scheduled. **Zero real fights, preparations, new seeds or retries.** Preserve all 482,821 reservations and v19's unused 512 values; v19 retains 253 recipes and Unresolved status. No configuration, migration or deployment changes.
