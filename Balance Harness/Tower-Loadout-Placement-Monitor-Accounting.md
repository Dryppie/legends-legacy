# Loadout placement: monitor-call accounting and resource boundaries

Current status: opt-in monitor-call accounting covers per-call preparation, selected owner-process/log observations, publication and verification through the point before terminal snapshot construction. Verification passes 340 Python tests. Unchanged native sources and runtime retain the authenticated prior 212-test backend verification. Scratch confinement and the final observer boundary remain unresolved; both compressed guards and the 1,806/1,800-second resource gate remain closed.

The target is the offline Balance Harness diagnostic monitor. `proposal_owner_process.py` adds the API option `OwnerProcessMonitor(..., monitor_accounting=True)`. The default remains disabled, and the existing `tower-proposal-owner-process-v1` package retains its original boundary, schema and publication exclusions. No launcher, supervisor, codec, native source, game service or admission rule changes.

The opt-in `monitor_terminal_observation` is an in-memory `tower-proposal-monitor-call-v1` record. It contains source bindings, the verified owner-observation manifest pin, elapsed call time, failure details and separate counters for preparation, owner process, observation, publication and verification. Preparation includes driver/monitor/helper/accounting-module/interpreter hashing and selected path validation. Publication counts exclusive opens, actual accepted binary-write lengths, flush, sync and close outcomes. Verification counts inventory, selected metadata and file hashing. Caller resource-check invocations are counted as logical calls; their internals are not inferred.

Partial returned writes accumulate their actual accepted lengths. A thrown write may have consumed an invisible prefix, so its progress is marked unknown. A failed close preserves an earlier write error. Publication errors preserve an earlier process error, and failure to construct the terminal snapshot also preserves the primary error. A second opt-in attempt cannot replace the first record, including a preparation failure that created no files. The default API's prior behavior remains covered by regression tests.

These are selected API observations. Path validation may inspect multiple ancestors; its count is not the number of filesystem calls. Existing metadata return/error-suppression semantics remain intact. File sizes are samples, not a complete storage high-water mark. Flush/sync completion is an observed API result, not a physical or durable byte measurement. Kernel job totals overlap application observations and must remain separate.

The new record stops **before its own construction**. Constructor work and earlier imports, terminal snapshot construction/serialization/persistence, caller console, monitor process exit and full monitor memory lifetime remain excluded. A complete monitor call means the call returned and its publication/verification path completed; it does not mean the child exited zero, the owner observation is complete, or a scientific study succeeded. The record keeps the owner's observation outcome separate. All whole-process, admission and qualification flags remain false.

The [resource-boundary specification](Tower-Loadout-Placement-Resource-Boundaries.json) identifies 11 named domains with 25 source references:

| Domain | Lifetime or unresolved obligation |
| --- | --- |
| Admission and captured runtime | Authenticate reads through final verification; input hashes do not confine runtime writes. |
| Registry and historical evidence | Preserve pre-existing and failed evidence; current output samples do not cover every registry lifetime. |
| Study retained members | Creation through audits and permanent retention; samples do not capture all intervening growth/deletion. |
| Native pending publication | Creation through rename/delete or failed retention, including targets outside output roots. |
| Writer leases | Open through release/process cleanup; failed release is unknown. Only specific racing leases are required to be empty. |
| Worker receipt sidecars | Publication, authentication, import and retention; terminal receipts still exclude their own persistence. |
| Managed owner/supervisor evidence | Exactness depends on exclusive managed membership; this API assumption is not OS confinement. |
| Redirected console and worker logs | Creation through descendant drain, handle close, hash and retention. |
| Outer-monitor publication | Selected calls through verification are now observed; terminal construction and later work are excluded. |
| Caller terminal persistence and console | Destinations and lifetimes are caller-selected and not bound by this monitor API. |
| External runtime/temp/unclassified files | Inherited environment paths and transient files require enforcement or justified finite bounds. |

This is a source-bound specification, not implemented confinement or an exhaustive dynamic path inventory. A future implementation must assign each canonical path to exactly one leaf before summing; pending, lease, log and sidecar roles take precedence over enclosing retained roots. Unclassified paths stay unknown. Logical file bytes are distinct from allocated disk blocks and operating-system backing storage. No new limits, coefficients, observer bounds or qualified forecast are invented.

The observer-closure rule is finite: declare the monitored process set, identify its external observer, and separately justify a conservative bound for that observer's setup, publication, verification, cleanup and exit. Nesting another observer does not make the remaining observer tail disappear. Until both parts and the scratch domains have defensible bounds, the resource gate remains closed. The [updated accounting inventory](Tower-Loadout-Placement-Monitor-Boundaries.json) pins 59 references across the existing 16 boundary families and retains both earlier inventories unchanged.

The [verification package](../TestResults/loadout-placement-monitor-boundary-verification-20260925) and [handoff](../TestResults/loadout-placement-monitor-boundary-handoff-20260925.json) retain source snapshots, logs, fresh correctness observations, domain references and authenticated native inputs. **340 Python tests passed with no skips**: 24 new monitor-boundary tests, 20 lease tests, 13 read tests, 280 prior diagnostic regressions and three actual native audit command cases.

The new suite checks ordinary/default behavior; failed preparation/hash/open/write/flush/sync/close/verification; hidden partial writes; preservation of primary errors; missing kernel observations; nonzero child exit; and repeat-call rejection. A real enclosing owner job starts four nested literal jobs and creates/deletes a 131,072-byte late scratch file. Its disappearance from the final inventory demonstrates the sample boundary. Scientific workers are routed, outcomes are literal, and the caller persists the terminal observation outside the sealed v1 monitor package.

The existing native process cases again pass full production audit and reject changed content or a resealed false input identity. They use the authenticated unchanged native fixture/runtime; no native rebuild or backend test run was needed. The prior 212 backend results and all source/runtime hashes were verified. The literal audit still reconstructs 15,744 bindings without combat or encounter preparation.

```text
python -B -X utf8 build/test-proposal-monitor-boundary.py -v
python -B -X utf8 build/test-proposal-lease-accounting.py -v
python -B -X utf8 build/test-proposal-remaining-reads.py -v
python -B -X utf8 TestResults/loadout-placement-monitor-boundary-verification-20260925/run-verification.py
python -B -X utf8 build/test-proposal-native-audit-success.py -v
git diff --check -- <changed files>
```

Exact commands, exports and pins are retained with the evidence. No required command remains blocked. Backend tests were not rerun because backend sources and runtime are unchanged; they remain linked to the prior required-runner verification. Correctness-test elapsed observations are not qualification timing samples.

Changed files comprise the monitor module, one new Python test, this report, two new JSON specifications/inventories, scoped LF attributes and only line 3 of nine existing status documents. Historical document bodies, prior reports/specifications, sealed packages, runtimes and cumulative accounting remain intact. Recorded charges stay **79,339.66095319996 seconds** and **51,980,910,910 bytes**; declared maxima stay **168,240 seconds** and **107,122,524,160 bytes**. The first failed pair remains failed.

Next implement and verify a closed diagnostic path/scratch ownership contract, with unknown external runtime/cache paths explicitly rejected for qualification or conservatively bounded. Then justify the remaining observer tail and full authentication/audit/publication boundary before proposing measurement. No scientific launch, qualification, cost experiment, timing pair, live-history rescan, production entropy, scientific reservation, encounter preparation or combat occurred. There are no migrations, application configuration changes or deployments. Future opt-in diagnostic packages must bind the updated monitor source; historical runtimes must not be replaced.
