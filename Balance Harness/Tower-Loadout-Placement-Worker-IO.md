# Loadout placement: Python worker authentication and result output

Current status: bound Python workers count binding authentication and the independent auditor's result-file operations. Verification passes 154 Python tests. Whole-process coverage remains incomplete; the compressed launch guards and the 1,806/1,800-second resource gate stay closed.

## Implementation

`Balance Harness/analysis/proposal_work_accounting.py` creates the worker collector before reading its binding. The worker counts the binding's actual read and JSON parser input separately, then counts request, producer and accounting-module reads during authentication before and after the action. Authentication attempts, completions and failures are retained. Short-circuit validation charges only reads that actually happened. A failure before receipt reservation still invokes no worker action and creates no receipt; no untrusted binding can select a failure-output destination.

The worker's existing extensible counter receipt remains bound to its original request, producer and phase and retains the same schema. Additional counters do not make the receipt complete process accounting. Its own serialization, writes, flush, fsync and close remain outside its snapshot. The native worker implementation is unchanged and does not acquire these additional Python authentication counters.

`Counters.write_json_output` observes the independent auditor's exclusive UTF-8 text-file output. It records open, write and close attempts and outcomes, plus the UTF-8 length of text accepted by successful writes. These accepted text bytes precede TextIO newline translation and buffering; they are not raw-file or durable bytes. A thrown write marks its progress unknown. A returned short write counts only the accepted prefix and fails rather than claiming a complete result. Closing is always attempted after a successful open; a close failure cannot replace an earlier write or serialization exception.

`Balance Harness/analysis/audit-proposal-affinity-study.py` uses that method only when a bound worker collector is active. Without accounting, the original output branch remains intact. JSON formatting, UTF-8 encoding, platform newline translation, exclusive creation and the existing absence of an explicit fsync are preserved. Console output is unchanged and remains outside these file counters. The worker cannot report completion after a caught result-file operation failure. Final authentication runs after result-file close, so a changed request during close cannot produce a complete receipt.

`build/test-proposal-worker-receipts.py` updates two expected totals to include binding authentication. The new `build/test-proposal-worker-io.py` tests the added boundaries. The scientific launcher, enclosing supervisor, process wrapper, native source, admission rules and defaults are unchanged. Future accounting use must authenticate the changed Python module and auditor in a fresh admission; sealed packages are never silently updated.

## Verification

The fresh [verification package](../TestResults/loadout-placement-worker-io-verification-20260924) retains source snapshots, logs and fixtures. Sixteen new tests cover exact authentication reads and parser input, failed short-circuit authentication, cancellation, result-file byte equivalence, exclusive-open failure, partial serialization, short and failed writes, invalid write counts, close failure, original-error preservation, caught failures, unchanged no-fsync behavior, unchanged stdout and authentication after output close.

The `worker-io` fixture exercises the real binding and independent auditor CLI with its scientific computation replaced by an explicit literal fixture result. It verifies byte-for-byte equality between default and counted result files and seals the bound worker receipt. Its metadata separately records accepted text bytes and final file bytes, preserving Windows newline translation. This is a correctness fixture, not a scientific result, resource forecast or native production-audit success fixture.

The existing worker-receipt (18), owner-supervisor (15), owner-closeout (13), collector/process (38), owner-file (22), owner arithmetic/guard (25) and provenance (7) tests also pass: **154 distinct Python tests**, with no skips. Fresh exports retain the real tiny-process owner fixtures and the separately labeled synthetic supervisor fixture. Historical native exchanges and the isolated DLL are read-only inputs. Backend sources did not change, so no backend rebuild or backend test run was required. No required command remains blocked.

```text
python -B -X utf8 build/test-proposal-worker-io.py -v
python -B -X utf8 build/test-proposal-worker-receipts.py -v
python -B -X utf8 build/test-proposal-owner-supervisor.py -v
python -B -X utf8 build/test-proposal-owner-closeout.py -v
python -B -X utf8 build/test-proposal-work-accounting.py -v
python -B -X utf8 build/test-proposal-owner-files.py -v
python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v
python -B -X utf8 build/test-proposal-selected-members.py SelectedMembers -v
git diff --check -- <changed files>
```

Export variables designated fresh destinations. Worker exchange verification used its external manifest pin. These are completed verification commands; mutation tests must not be rerun inside sealed evidence packages.

## Remaining work and operational effect

Outstanding boundaries include worker receipt publication, native binding authentication, supervisor-observation persistence, console output and process exit, whole-owner memory lifetime, transient and external scratch storage, remaining owner/native reads and parsing, and lease acquisition/handle operations and failures. Python result-file counters cover accepted text and close outcomes, not underlying buffered/raw writes or durable storage. A fresh full native production-audit success fixture is still needed. All observations remain `wholeProcessCoverage=false` and `usableForAdmission=false`.

A separately justified prospective replacement resource model is still required before measurement. No scientific launch, native preparation, qualification, production entropy draw, scientific reservation, timing pair, live-history scan or combat occurred. The first failed pair stays failed; history and cumulative charges are unchanged. No migrations, application configuration changes, deployment or retained-runtime replacement occurred. Only line 3 changes in the nine status documents; their historical bodies remain byte-identical.
