# Loadout placement: Python receipt publication accounting

Current status: opt-in Python workers retain separate application counters for receipt reservation, serialization, writes, flush, sync and close. Verification passes 198 Python tests. Native receipt publication and whole-process coverage remain incomplete; both compressed launch guards and the 1,806/1,800-second resource gate stay closed.

## Implementation and boundaries

`Balance Harness/analysis/proposal_work_accounting.py` accepts an explicit `tower-proposal-worker-binding-v2` binding with an external `publicationPath`. The binding, request, producer and accounting module remain authenticated before work. The observation file is reserved exclusively before reserving the receipt or invoking the worker. Both destinations must be outside the study archive and distinct; existing files are preserved. Legacy v1 bindings retain their existing receipt format and publication behavior.

The separately versioned `tower-proposal-worker-publication-v1` observation records attempted, completed and failed application operations for receipt reservation, canonical serialization, binary writes, flush, file sync and close. It also binds the intended serialized receipt's byte length and SHA-256. Short writes are retried and only returned accepted bytes are counted. A thrown or invalid write records unknown progress for that call. Successful close is distinguished from a close attempt. Failed serialization or reservation leaves the unavailable serialized size and digest null.

Receipt publication outcome is separate from worker outcome: a failed worker can publish a complete failure receipt, and a successful worker body can suffer a publication failure. Publication errors propagate; secondary close or observation-persistence failures do not replace an original worker error. These counters describe application calls and accepted binary bytes. They are not physical-disk or durable-byte measurements.

The observation is serialized after the receipt publication attempt, including close where a receipt stream was opened. Its own creation, serialization, writes, flush, sync and close are explicitly excluded. It therefore avoids claiming that a receipt measured its own final write. Existing kernel job totals cover a different, overlapping scope; they must not be summed with these counters. No complete I/O, process lifetime, memory or scratch claim follows from this observation.

`RetainedOwner.run_worker` can request the new binding, validate the observation's schema, binding, operation relationships and coverage flags, and retain its bytes in the owner manifest. Successful publication must match the retained receipt's exact bytes and digest. The two artifacts are retained independently on failure, so a missing or malformed observation does not discard a valid worker receipt. Missing, failed or inconsistent observations cannot produce successful owner accounting, even if an invocation mistakenly returns normally. Publication counters remain separate from worker/application totals.

`Balance Harness/analysis/proposal_owner_supervisor.py` requests v2 only for the independent Python auditor when diagnostic accounting is explicitly active. The native worker phases continue to use v1. The existing auditor CLI already passes the authenticated binding to `worker_receipt`, so its flags and result format need no change. Default scientific launch behavior, admission logic, captured historical runtimes and archive inventories are unchanged. Future diagnostic use must authenticate the changed module and supervisor sources.

## Verification

The fresh [verification package](../TestResults/loadout-placement-python-receipt-publication-verification-20260924) retains before/after snapshots, commands, environments, logs and fixtures. `build/test-proposal-receipt-publication.py` adds 28 tests covering exact accepted bytes and digest binding, legacy compatibility, reservation/path rejection, malformed observations, short and failed writes, serialization/flush/sync/close failures, cancellation, error preservation, owner retention and unchanged auditor result bytes.

The retained `publication` fixture runs four real, bounded Python worker processes under the existing Windows process owner. They use literal input, perform no combat and consume no production entropy. Each closes its receipt before publishing its separate observation. The owner retains both artifacts, validates their bindings, completes all four phase intervals and deletes the external copies during cleanup. Its sealed manifest and final observation retain the evidence. All four workers are Python fixture workers; their phase names do not establish native implementation or production-audit success.

The routed supervisor fixture exercises the real owner path with intercepted worker commands. Its independent-audit receipt publication now uses the real Python binding and publication code. Native commands and process observations in that fixture remain synthetic and explicitly labeled. Verification asserts v2 for the independent auditor and v1 for all three native phases, with the new sidecar counted by the existing storage/deadline checks.

The initial new suite passed 24 of 25 tests; one path-isolation test reused paths from its preceding fixture directory, causing two subcase failures. The test now derives each path from its current fixture. The failed log is retained. The corrected suite adds three further failure checks and passes all 28 tests. Final exported fixtures were generated only after that correction in fresh directories.

Final verification passes **198 distinct Python tests**, with no skips:

```text
python -B -X utf8 build/test-proposal-receipt-publication.py -v             # 28
python -B -X utf8 build/test-proposal-owner-supervisor.py -v                # 15
python -B -X utf8 build/test-proposal-job-io.py -v                          # 16
python -B -X utf8 build/test-proposal-worker-io.py -v                       # 16
python -B -X utf8 build/test-proposal-worker-receipts.py -v                 # 18
python -B -X utf8 build/test-proposal-owner-closeout.py -v                  # 13
python -B -X utf8 build/test-proposal-work-accounting.py -v                 # 38
python -B -X utf8 build/test-proposal-owner-files.py -v                     # 22
python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v  # 25
python -B -X utf8 build/test-proposal-selected-members.py SelectedMembers -v # 7
git diff --check -- <changed files>
```

Exact executable paths and export variables are recorded in `test-runs.json`. The historical native DLL and exchanges were read-only regression inputs, including their external pins. Native/backend sources did not change, so no backend rebuild or backend test run was needed. Source AST checks limit existing production method changes to `worker_receipt`, `RetainedOwner.run_worker` and `StudyWorkSupervisor.run`; the publication collector/validator are new. Only line 3 changes in the nine status documents; their historical bodies are byte-identical. No required command remains blocked. Do not rerun mutation tests in these sealed destinations.

## Remaining work and operational effect

Next implement native receipt-publication accounting with equivalent success/failure guarantees and cross-language verification. Remaining boundaries also include the new observation's own persistence, supervisor-observation persistence, final owner console output and process exit, whole-owner memory lifetime, external/transient scratch, remaining owner/native reads and parsing, metadata/path checks and lease-handle operations. A fresh full native production-audit success fixture remains outstanding.

All observations remain incomplete and unusable for admission. This work establishes no current resource forecast, compression speedup or search improvement. The first failed pair stays failed; cumulative historical charges and evidence are unchanged. The 1,806-second audit floor still exceeds the 1,800-second limit. A separately justified prospective resource model is required before measurement. No scientific launch, native preparation, qualification, new timing pair, live-history scan, production entropy draw, scientific reservation or combat occurred. No migrations, application configuration changes, deployment or retained-runtime replacement occurred.
