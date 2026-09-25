# Loadout placement: supervisor observation persistence

Current status: diagnostic owners now persist and bind their final supervisor observation, with separate counters through manifest close and verification. Verification passes 227 Python tests. Owner console/process exit and whole-process coverage remain incomplete; the compressed launch guards and the 1,806/1,800-second resource gate stay closed.

## Implementation

`Balance Harness/analysis/proposal_owner_supervisor.py` now creates a separate managed `terminal` directory within the existing diagnostic accounting root. After closing the worker phases, owner and preceding supervisor publication, it writes `supervisor-observation.json` and a manifest binding those exact bytes. Both files use exclusive creation, checked partial-write loops, flush, file sync and close. Their lengths and hashes are checked before the new observation-manifest pin can be reported.

The new files use the existing combined storage checks and shared deadline before and after each publication, then again after final verification. They remain outside the scientific archive, preserving its exact retained-length contract. The existing watchdog stays active through publication and the diagnostic success output. Existing evidence is never overwritten. Publication, quota, integrity or verification failures suppress success output; a secondary close failure does not replace the original write error, and a supervisor persistence failure does not replace an original worker failure.

The persisted supervisor snapshot retains its original `tower-proposal-supervisor-observation-v1` shape and pre-publication boundary. It truthfully continues to exclude its own persistence from its counters. A separate in-memory `tower-proposal-supervisor-persistence-v1` observation records the new files' application writes, flushes, syncs, closes, verification reads, managed storage and final combined storage sample. Successful publication of a failed supervisor snapshot remains distinct from successful worker execution.

When persistence fails, the enclosing observation retains completed/failed operation counters and the error, leaves unverifiable final storage and combined-byte values null, clears the success pin and marks the failed publication boundary. Failed write progress remains unknown. No physical-disk, durable-byte, transient-scratch or complete-process claim is made. The new enclosing observation itself excludes external persistence, final console output and process exit. Its counters stay separate from worker, owner, preceding supervisor and kernel job totals.

`build/run-proposal-affinity-study.py` adds `accountingObservationManifestSha256` to its existing opt-in diagnostic success object. `accountingManifestSha256` keeps its preceding supervisor-manifest meaning. The default output and CLI are unchanged. Source AST checks verify that this one new output keyword is the launcher's only executable change; native execution, admission validation, the initial compressed rejection, leases, deadlines and existing accounting logic are unchanged. Fresh diagnostic use must authenticate the updated launcher and supervisor sources.

## Verification

The fresh [verification package](../TestResults/loadout-placement-supervisor-persistence-verification-20260924) retains source snapshots, commands, logs and literal fixtures. `build/test-proposal-supervisor-persistence.py` adds **22 tests** for exact persisted bytes and bindings, separate accounting, failed workers, serialization/open/write/flush/sync/close failures, short/zero writes, original-error preservation, manifest and verification failures, same-length tampering, untracked files, existing evidence, shared quotas/deadlines and watchdog lifetime.

The success fixture proves that the newly accepted write bytes equal the two final files' lengths, that both closes and syncs completed, and that the post-persistence combined byte sample equals the earlier sample plus those files. It verifies the diagnostic stdout manifest pin, the saved supervisor observation and all enclosing manifests. The fixture uses real admission checks, Windows leases, file operations and publication, while its worker commands and process observations are intercepted and explicitly labeled synthetic. It is not a native scientific study or full production-audit success fixture.

A deliberate console failure after persistence confirms the remaining boundary: the persisted observation and its in-memory enclosing observation describe completed publication, while console output still raises and the watchdog is cancelled. Their missing-coverage fields explicitly retain final console/process exit; these observations cannot certify the caller's subsequent lifecycle.

The existing supervisor (15), native publication (7), Python receipt publication (28), job I/O (16), worker I/O (16), worker receipts (18), owner closeout (13), work accounting (38), owner files (22), arithmetic/guards (25) and selected-members (7) groups also pass: **227 distinct Python tests**, with no skips. Native exchanges and isolated binaries were authenticated read-only inputs. Native/backend sources did not change, so no backend build or backend test run was needed this turn. No required command remains blocked.

```text
python -B -X utf8 build/test-proposal-supervisor-persistence.py -v
python -B -X utf8 build/test-proposal-owner-supervisor.py -v
python -B -X utf8 build/test-proposal-native-publication.py -v
python -B -X utf8 build/test-proposal-receipt-publication.py -v
python -B -X utf8 build/test-proposal-job-io.py -v
python -B -X utf8 build/test-proposal-worker-io.py -v
python -B -X utf8 build/test-proposal-worker-receipts.py -v
python -B -X utf8 build/test-proposal-owner-closeout.py -v
python -B -X utf8 build/test-proposal-work-accounting.py -v
python -B -X utf8 build/test-proposal-owner-files.py -v
python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v
python -B -X utf8 build/test-proposal-selected-members.py SelectedMembers -v
git diff --check -- <changed files>
```

`test-runs.json` records exact executable paths, commands and fresh export destinations. `runtime-inputs.json` records the historical native input locations and external pins. Mutation tests must not be rerun in sealed destinations. Only line 3 changes in the nine status documents; their historical bodies remain byte-identical.

## Remaining work and operational effect

Next observe the owner from an enclosing process boundary so final console output, process exit and lifetime memory can be covered. The current in-memory terminal observation's persistence remains outside its own counters. Worker observation-persistence application counters, external/transient scratch, remaining owner/native reads and parsing, metadata/path checks and lease-handle operations also remain incomplete. A fresh full native production-audit success fixture is still required.

Both admission guards remain closed. The 1,806-second audit floor still exceeds the 1,800-second limit; a separately justified prospective resource model is required before measurement. No current resource forecast, compression speedup or search improvement is established. The first failed pair stays failed; historical evidence and cumulative charges are unchanged. No scientific launch, native preparation, qualification, timing pair, live-history scan, production entropy draw, scientific reservation or combat occurred. No migrations, application configuration changes, deployments or retained-runtime replacement occurred.
