# Seed-registry traversal performance

15 September 2026. **Diagnostic stopped; evidence preserved.** This scope changes only offline seed-registry traversal and its measurements. **Zero fights, control preparations, fresh seed values or retries.** The failed completion-versus-allocation comparison preparation remains sealed and was not rerun. Adoption stays Hold.

## Implementation and safeguards

`TowerHistoryRegistry` uses `FileSystemEnumerable` to read entry metadata without constructing `FileSystemInfo` objects for ordinary files. It builds paths only for directories and the three exact ledger filenames. `TowerCompleteFamilyInputs.HistoryRegistry` delegates to it; all existing callers still perform complete registry scans, hashes and reservation validation.

Every entry is inspected, including hidden/system files and irrelevant files that might be links. Reparse rejection, inaccessible-path errors, queued-directory attribute rechecks, the exact excluded subtree and the 2,000,000-directory bound remain. Cancellation is checked for entries discarded by the filename filter; aggregate trace counters survive failure. There is no cached registry, archive pruning or reduced hash coverage. A lower test-only directory limit cannot exceed the production ceiling.

The [frozen protocol](Tower-History-Registry-Performance-Protocol.md) bounded all new diagnostic work before execution. The two isolated builds used captured gameplay DLLs and cached restore metadata. No dirty gameplay assembly was rebuilt.

## Verification and measurements

Backend results: **131/131 passed**, via `build/run-tests.ps1`. The 131-case suite contains the preceding 118 cases and 13 registry cases, including a real directory junction, hidden files, new/removed ledgers, cancellation and count limits. Build logs retain warnings and errors.

| Measurement | Wall seconds | CPU seconds | Allocated MiB |
| --- | ---: | ---: | ---: |
| archive-candidate | 4.028556 | 4.078125 | 27.757 |
| archive-reference | 3.555932 | 3.562500 | 96.098 |
| full-registry-enumeration | 57.467132 | 56.078125 | 370.914 |
| full-registry-hashes | 0.406391 | 0.390625 | 20.636 |
| reservation-union | 3.200305 | 3.562500 | 3447.669 |

The matched archive pair returned identical membership and matched the sealed **9,216 archives / 101,429 files / 55,313 directories**. The candidate was **13.29% slower** in this single reference-first pair, despite **71.12% fewer managed allocated bytes**. This is not evidence of lower peak memory or faster traversal. The performance problem is not declared solved.

The native full-registry traversal visited **702,169 directories**, **4,639,564 files** and **5,341,732 entries** in **57.467 seconds**. It found **164 ledgers** and rebuilt all **482,596 reserved values**. Hashing and union timings are separately recorded above.

The independent Python audit reached its **39-second inner deadline** during its own full-tree traversal, within the frozen 45-second phase cap. Its recorded phase time was **39.125 seconds**. The allotted time was insufficient; the audit did not finish membership comparison, hash comparison or the independent union check. Partial Python entry counters were not persisted, so its completion fraction and full runtime are unknown. No audit retry, timeout extension or comparison preflight followed. Native completion and backend test results remain valid evidence, but **independent full-registry verification is unresolved**.

The dependent diagnostic sequence stopped. No readiness or performance-improvement conclusion is asserted. Preserved failure details:

- `audit-failure`: 39.125 seconds. See its full error receipt under `control/`.

All **15,512 indexed files across 38 predecessor packages** verified unchanged before/after, including the failed preparation. Owned source and fixture hashes matched their frozen copies. Unrelated dirty checkout work was left in place.

New diagnostic time before publication: **117.704 seconds**; cumulative before publication: **1405.755 seconds**. Compilation: **6.485 seconds**, reported separately. New output before publication: **70.74 MiB**, plus a conservative **1 MiB** charge for temporary test fixtures. Carried output: **1,740,924,701 bytes**. The [completion receipt](../TestResults/balance/tower-history-registry-performance-20260915/control/completion.json) includes measured publication and a conservative one-second receipt/seal charge. The cumulative 1,800-second and 4-GiB caps are unchanged.

## Commands and remaining work

Commands describe the once-only frozen attempt. Inspect the saved command/exit/measurement receipts; do not rerun sealed paths.

```powershell
$registry = 'TestResults/balance/tower-history-registry-performance-20260915'
$python = 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $python -B "$registry/workflow.py" freeze
& $python -B "$registry/workflow.py" build
& $python -B "$registry/workflow.py" test-build
& $python -B "$registry/workflow.py" tests
& $python -B "$registry/workflow.py" measure
& $python -B "$registry/workflow.py" audit
& $python -B "$registry/close_failure.py"
```

`tests` invokes `build/run-tests.ps1 -NoBuild`; the exact filter and isolated artifact path are in `control/tests-command.json`, and `control/tests.trx` retains results. The native measurement has a cooperative cancellation deadline inside its outer timeout; stage records persist partial timings on failure. The wrapper uses a bounded direct-child termination fallback. It does not claim to cancel an unresponsive filesystem syscall instantly or provide an atomic snapshot against concurrent external writes.

Uncompleted commands remain gated by the preserved failure. Resolve the recorded defect before freezing any distinct follow-up; no diagnostic retry, larger old cap or combat is authorized by this report.

Changed files: `TowerHistoryRegistry.cs`, its caller in `TowerCompleteFamilyInputs.cs`, `BalanceHarnessHistoryRegistryTests.cs`, the protocol/review, six active Markdown handoffs and the separate evidence package. No game content, gameplay logic, search policy, ability order, configuration or migrations changed; no deployment implications. V19 retains 253 recipes and its unused 512 confirmation values. Historical reliability Fail 1/3, deep recovery 0/3, v19 Unresolved and adoption Hold remain unchanged.
