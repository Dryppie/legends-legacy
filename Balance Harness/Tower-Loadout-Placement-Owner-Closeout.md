# Loadout placement: owner accounting through publication

Current status: opt-in owner accounting now includes its managed receipt/manifest publication and final verification. Verification passes 123 Python tests. Whole-process coverage is incomplete; the compressed launch guards and the 1,806/1,800-second resource gate remain closed.

## Implementation

`Balance Harness/analysis/proposal_work_accounting.py` now accepts a fresh `OwnerFileCounters` in `RetainedOwner`. That collector accounts for owner module/request/producer authentication reads, worker receipt reads and parsing, retained receipt writes, binding and process-observation publication, final manifests and their verification. Existing callers without a collector preserve the original receipt contract and behavior. The scientific launcher remains unchanged and does not activate this integration.

`ManagedStorage` reports successful raw-file write bytes, flushes, fsyncs and closes to the optional collector. Short writes charge only the returned byte count; thrown writes mark partial progress unknown. A failed operation remains recorded even if its caller catches the exception. An accounted owner cannot report successful completion after a caught managed-storage failure. Its exact storage totals cover only files exclusively created through that managed directory; they are not combined with sampled external file lengths, child logs, worker I/O or memory peaks.

Worker receipt validation optionally counts its actual file reads and in-memory JSON parser input. The retained owner's repeated authentication reads remain separate real operations. Worker counters remain in their individual bound receipts and are never added to the owner's counters.

With accounting enabled, `final_observation` becomes available after owner cleanup, the terminal owner receipt, the manifest and all their file closes. It verifies the managed inventory and every retained file hash again before recording its final counters and duration. It binds the final manifest, request and producer. It includes managed storage after publication and a separate duration for the work after the earlier ledger closed. The original `owner-work.json` still truthfully states that its own publication was excluded from its ledger.

The new `tower-proposal-owner-final-observation-v1` object is returned in memory for a caller to retain outside the managed directory. Its own persistence remains explicitly excluded, avoiding a claim that a receipt has measured its own serialization and close. A verification or clock failure makes the affected measurements unknown, clears the manifest pin and marks the observation failed. A secondary observation failure does not replace an original worker exception. Calling close twice cannot overwrite a completed observation.

## Verification

The fresh [verification package](../TestResults/loadout-placement-owner-closeout-verification-20260924) retains before/after source snapshots, logs, a complete literal owner fixture, separate worker fixtures and process observations. All older packages remained immutable.

Thirteen new tests cover final receipt/manifest bytes and hashes, cleanup and publication durations, default behavior, fresh-collector requirements, double close, original errors during publication failure, cleanup failure, post-publication manifest tampering, observation failure, backwards clocks, caught fsync failure, short writes and unknown partial-write progress. Deterministic clock checks distinguish seven seconds through cleanup from four further seconds of terminal publication, for eleven total; these are literal test inputs, not performance measurements.

A real four-worker fixture combines bound receipts, process observations and child-log observations with owner I/O accounting. It creates and deletes 2,048 scratch bytes during cleanup and releases the launcher's actual Windows exclusive DeleteOnClose lease. Reacquiring that lease confirms release. The fixture retains 14,509 bytes in the owner's managed directory, records 16,557 managed write bytes including deleted scratch, and separately observes 52 child-log bytes. The 1,296-byte manifest is included in both final write and verification-read counters. All four worker receipts and process observations are retained. The final observation is sealed externally and binds the owner's manifest; its own persistence is excluded.

The existing bound-worker tests (18), collector/retained-owner/process tests (38), owner-file tests (22), owner arithmetic/guard tests (25), and provenance tests (7) also pass: **123 distinct Python tests**, with no skips. The native exchange and isolated DLL from the previous stage were read-only inputs. Backend sources did not change, so no backend rebuild or test run was needed. No required command remains blocked.

```text
python -B -X utf8 build/test-proposal-owner-closeout.py -v
python -B -X utf8 build/test-proposal-worker-receipts.py -v
python -B -X utf8 build/test-proposal-work-accounting.py -v
python -B -X utf8 build/test-proposal-owner-files.py -v
python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v
python -B -X utf8 build/test-proposal-selected-members.py SelectedMembers -v
git diff --check -- <changed files>
```

Export variables designated fresh destinations. Worker exchange verification used its external manifest pin. These are records of completed verification, not instructions to rerun mutation tests inside sealed packages.

## Remaining work and operational effect

The diagnostic owner now observes its instrumented operations through terminal publication; this does not close the entire scientific owner's lifecycle. Scientific launcher integration, observation persistence in an enclosing supervisor, whole-owner memory, transient scratch outside managed storage, worker binding-authentication and receipt-publication I/O, remaining owner/native reads and parsing, independent CLI output writes, and a full native production-audit success fixture remain outstanding. Lease release is exercised, but lease acquisition/handle-operation I/O and failures are not a complete accounting boundary. Every observation remains `wholeProcessCoverage=false` and `usableForAdmission=false`.

A separately justified prospective replacement resource model is still required before measurement. No scientific launch, native preparation, qualification, production entropy draw, scientific reservation, timing pair, live-history scan or combat occurred. The first failed pair stays failed; history and cumulative charges are unchanged. No migrations, application configuration changes, deployment or retained-runtime replacement occurred. Only line 3 changes in the nine status documents; their historical bodies remain byte-identical.
