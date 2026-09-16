# Refinement comparison: seed-free preflight review

16 September 2026. **VerifiedSeedFreePreflight**. Eight new backend tests and the captured seed-free preflight passed. Independent checks confirm identical captured gameplay inputs, both fixed-order controls and the complete 482,821-value ledger. The binding explicitly forbids execution and requires a live registry refresh.

## Change and verification

Corrected `LL/tools/BalanceHarness/TowerRefinementComparisonPreflight.cs` to canonicalize each control before constructing its generated-context choice. The eight regression tests are unchanged. Added a separately frozen verification workflow with build servers disabled; both earlier failed scopes remain sealed. No ability-order search or control-composition change. The preflight verifies required file pins before and after reads, content/settings identity, the four captured gameplay assemblies, canonical control recipes and generated context, complete reservation count, the sealed registry snapshot and the verified driver receipt. It returns `BoundAwaitingAuthorization`, `runAuthorized=false`, `requiresLiveRegistryRefresh=true`, with 45 required future values and the unchanged 288-attempt comparison limit. It does not allocate values, invoke preparation or expose a combat command. This is a real input-binding component; it is not a complete execution launcher.

Backend results: **8/8 new facts passed**, using `build/run-tests.ps1` with the isolated artifacts and `FullyQualifiedName~BalanceHarnessRefinementPreflightTests`. The existing 117 driver assertions were not rerun; unchanged driver source and prior evidence are retained. These eight preflight tests run against the rebuilt corrected harness; no previous failure is relabeled. Captured builds use the historical gameplay assemblies, not the dirty gameplay checkout. The six process-wrapper fixtures were reused by exact hash; no process-fixture repetition. The driver, search policy, nominations, combat implementation and ability-order rule are unchanged.

Captured preflight: **0.3309s**, **0.3281 CPU seconds**, **277,349,952 allocated bytes**, **242,552,832 peak working-set bytes**. Detailed existing-infrastructure stage timings are retained in `native-metrics.json`. This is one latency measurement, not a before/after speedup or build-strength result.

| Phase before publication | Charged seconds |
| --- | ---: |
| audit | 0.485 |
| build | 3.687 |
| captured | 0.609 |
| freeze | 0.750 |
| test-build | 2.469 |
| tests | 1.922 |

This scope rechecks **consumed pinned inputs and producing binaries**. It does **not** repeat the previous 74-package full audit or scan the live reservation registry. The previous [full audit](../TestResults/balance/tower-refinement-comparison-driver-verification-20260916/control/final-preservation-metrics.json) remains sealed. The registry snapshot is historical evidence, not proof that no later reservation exists. No future allocation may rely on it without live refresh.

Uncompleted phases: **none**. Failure detail:

```
None.
```

## Resources and next boundary

Incoming diagnostic usage was **2,992.261469456 / 3,000 seconds**, leaving **7.738530544 seconds** before the separately recorded 30-second extension to **3,030 seconds**. The [frozen protocol](Tower-Refinement-Preflight-Order-Correction-Protocol.md) allows 30 seconds / 96 MiB within the unchanged cumulative 4 GiB cap, with four seconds reserved for publication. The [completion receipt](../TestResults/balance/tower-refinement-preflight-order-correction-20260916/completion.json) records exact measured cumulative usage, remaining time and output accounting. Shared test output is conservatively charged 1 MiB. The explicit authorization receipt binds the 30-second cumulative-cap increase; no output-cap increase or budget reset.

Input binding is verified. A live reservation-registry refresh, authorized durable allocation and the execution launcher still need integration and verification before combat. The existing 117-test controller verification is preserved; actual compact-runtime execution remains untested in this comparison. Earlier fresh-seed approvals are exhausted. Do not squeeze a combat comparison into the remaining diagnostic seconds. A future runnable study needs a separate concrete resource proposal and explicit fresh-seed exception. The binding does not establish improved optimizer strength, reliability or throughput.

Reproduction commands (executed once; do not rerun against sealed output):

```powershell
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$work = 'TestResults/balance/tower-refinement-preflight-order-correction-20260916'
& $python -B "$work/workflow.py" freeze
& $python -B "$work/workflow.py" build
& $python -B "$work/workflow.py" test-build
& $python -B "$work/workflow.py" tests
& $python -B "$work/workflow.py" captured
& $python -B "$work/workflow.py" audit
& $python -B "$work/publish.py"
```

Exact child commands and completion/timeout receipts, compiler/test logs, TRX, source snapshots, request pins and the independent audit are retained. The dirty checkout was snapshotted before edits and unrelated files checked at publication. Six active Markdown handoffs were updated. The authorization receipt is retained with the exact grant and protocol hash. Full backend-suite execution, gameplay rebuild, live registry refresh, materialization and combat were not scheduled. **Zero fights, fresh values or retries.** All **482,821 reservations**, including the unused 512 v19 values, remain preserved. V19 retains 253 recipes and Unresolved status; reliability Fail 1/3, deep recovery 0/3 and adoption Hold are unchanged. No migration, configuration change or deployment.
