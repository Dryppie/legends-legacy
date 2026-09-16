# Discovery comparison gate: verification stopped

16 September 2026. **FailurePreserved**. The gate and ten new tests are implemented as **unverified source**. The prerequisite preservation scan reached its frozen 20-second deadline; no successful input freeze, build or verification followed. Zero fights, runtime preparations, fresh values, retries and replays. All **482,821 reservations** retained.

## Proposed behavior, not yet verified

`TowerDiscoveryComparisonGate` keeps the existing 16-evaluation requirement for both baseline and refinement. Its intended behavior is to write a durable receipt with statuses, proposal/evaluation counts and rejection reasons, then stop the entire pair if either arm is incomplete. Complete pairs retain the existing top-two ranking. No refill, cap increase, legality relaxation or partial-arm quality claim is introduced. The saved 14/16 fixture remains evidence from the preceding refinement scope; it was not processed by this new gate.

New source: `LL/tools/BalanceHarness/TowerDiscoveryComparisonGate.cs` and `LL/tests/EssenceSystem.Tests/BalanceHarnessDiscoveryComparisonGateTests.cs`. The planned 105-test run comprises the earlier 95 plus ten gate tests. **None ran in this scope.** The gate has not compiled, and the historical 95-test success does not validate these new files. No search, allocation, default or gameplay change was made.

## Failure and measured limits

The [frozen protocol](Tower-Discovery-Comparison-Gate-Protocol.md) allowed 80 diagnostic seconds / 128 MiB for this scope, within the cumulative 3,000-second / 4 GiB cap. Its first command was:

```powershell
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B 'TestResults/balance/tower-discovery-comparison-gate-20260916/workflow.py' freeze
```

The [failure receipt](../TestResults/balance/tower-discovery-comparison-gate-20260916/control/freeze-failure.json) records **20.000 seconds** and `AssertionError: Preservation deadline` in the inherited package-content scan. It does not establish a damaged archive or explain the I/O slowdown. Verification of all 69 predecessor packages did not complete. Do not rerun this command or reuse the directory.

Build, test-build, `build/run-tests.ps1`, saved-fixture gate processing, independent audit and the success publisher were **not run**. No new measured gate result or speedup is claimed. The [failure closure](Tower-Discovery-Comparison-Gate-Failure-Closure.md) records a separate, bounded publication path without repeating the failed scan.

Incoming usage was **2,843.503 diagnostic seconds**, leaving **156.497**. The [completion receipt](../TestResults/balance/tower-discovery-comparison-gate-20260916/completion.json) carries the failed 20 seconds plus measured closure cost and exact remaining allowance. New output and the conservative 1 MiB shared-artifact allowance remain charged against the unchanged cumulative cap. Source editing is excluded as before.

## Remaining work

Preserve this failed package. Before a new verification attempt, explicitly address the preservation scan's deadline and freeze a separate scope within the remaining allowance; do not silently increase old caps or skip required checks. The new gate still needs compilation, tests and saved-fixture verification. Full comparison-driver integration and combat preparation/execution also remain unrun. Prior fresh-seed approvals are exhausted; no new combat is authorized.

Closure checks cover the scoped source snapshots, unchanged pre-existing harness/dirty-file hashes outside active Markdown, ledger hash against the preceding freeze, changed-file whitespace and new local links. They are not a replacement for the unfinished predecessor-content audit. Old protocols, scripts and captured evidence are unchanged. No configuration change, migration or deployment.

V19 remains Unresolved with all 253 recipes and the original unused 512 confirmation values preserved. Reliability Fail 1/3, deep recovery 0/3 and adoption Hold remain unchanged. Fixed ability order remains required; no Kharad tuning or large confirmation.
