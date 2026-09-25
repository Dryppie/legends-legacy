# Loadout placement: worker observation-persistence accounting

Current status: opt-in v3 worker bindings now retain separate Python/native accounting for publication-observation persistence through close. Verification passes 192 backend tests and 280 Python tests. Whole-process accounting and native production-audit success verification remain incomplete; both compressed guards and the 1,806/1,800-second resource gate stay closed.

## Implementation

`tower-proposal-worker-binding-v3` adds `publicationPersistencePath` alongside the existing receipt and publication paths. All three outputs must be distinct, absolute, unlinked and outside the scientific archive. The terminal output is reserved before the publication observation, receipt or worker action, so existing evidence cannot be discovered only after work. V1/v2 bindings reject the new field, including explicit null; v3 requires both publication paths. Request, producer and module authentication remain in place.

Python `proposal_work_accounting.py` and native `TowerProposalWorkReceipt.cs`/`TowerReceiptPublication.cs` implement the same `tower-proposal-worker-observation-persistence-v1` contract. Its counters cover the publication-observation file's reservation, serialization, accepted writes, flush, sync and close. It binds the exact serialized observation hash and length to the existing request/producer/phase/binding identities. Python retries short writes and counts only the accepted prefix. Failed writes retain unknown remaining progress. Native `Stream.Write` counts the full buffer only after a successful call. Neither implementation claims physical or durable disk bytes.

The work receipt, receipt-publication observation and new persistence receipt remain separate. Each snapshot preserves its own boundary: the receipt-publication observation still excludes its own persistence, while the new receipt measures those calls and explicitly excludes its own terminal persistence. No recursive complete-accounting claim is made. An enclosing owned job can observe kernel I/O through the terminal file, but kernel and application totals overlap and must not be added together.

A failed worker or cancellation can still have completely observed publication. Publication failure cannot become a successful owner result. Exceptions preserve the original worker error and completed/failed call counters. Terminal publication failures append separate error details; native terminal errors no longer overwrite an earlier publication-persistence error field.

`RetainedOwner.run_worker` validates the new receipt, retains its exact bytes and checks its hash/length against the publication observation before attaching the work receipt. Missing, failed, changed or malformed evidence fails owner retention even when a worker returns successfully. `StudyWorkSupervisor(root, worker_observation_persistence=True)` opts all four diagnostic phases into v3. Its default remains v2. The scientific launcher CLI, admission rules, archive resource arithmetic and process helper are unchanged. Updated routed fixtures support both diagnostic binding versions.

## Verification

The [verification package](../TestResults/loadout-placement-worker-observation-persistence-verification-20260924) retains commands, logs, TRX files, source snapshots, fresh cross-language exchanges and diagnostic process fixtures. Initial authentication covered 336 historical pins, 133 current source hashes and 31 isolated native runtime files.

The final backend run passes **192 tests**, including 32 new native cases covering every native phase, cancellation/failure, strict binding fields, overlapping paths, reservation order, existing evidence, serialization/write/flush/sync/close faults, primary-error preservation, terminal error details and real native command rejection on literal input. The existing 160 publication/worker/accounting tests also pass. A combat guard prohibits fights in the new native fixture cases. Builds use fresh isolated artifacts directories and do not replace retained runtimes.

`build/test-proposal-observation-persistence.py` adds **20 tests** for the Python contract, failure progress, short/zero writes, strict validation, four-phase supervisor integration and retention failures. `build/test-proposal-persistence-exchange.py` adds **6 tests** that independently validate native success/failure receipts with the Python reader and run three real native command failures plus one real Python worker under owned Windows jobs. The process tests verify that terminal-file writes are within the job I/O boundary. The retained v3 supervisor fixture has real owner/publication/lease behavior with explicitly routed scientific workers and synthetic inner process observations.

The 254 preceding Python tests pass again against fresh exports and the new isolated native inputs, for **280 distinct Python tests**, with no skips in the final run. This includes the enclosing owner-process fixtures, v1/v2 compatibility, supervisor persistence, publication, worker/job I/O, owner accounting and unchanged admission guards. No fresh full native production-audit success fixture or scientific study is claimed.

```text
./build/run-tests.ps1 -ArtifactsPath .artifacts/loadout-worker-persistence-final-20260924 -Filter 'FullyQualifiedName~BalanceHarnessReceiptPublicationTests|FullyQualifiedName~BalanceHarnessWorkerReceiptTests|FullyQualifiedName~BalanceHarnessWorkAccountingTests|FullyQualifiedName~BalanceHarnessWriteAccountingTests|FullyQualifiedName~BalanceHarnessFileAccountingTests|FullyQualifiedName~BalanceHarnessContentAccountingTests'
python -B -X utf8 build/test-proposal-observation-persistence.py -v
python -B -X utf8 build/test-proposal-persistence-exchange.py -v
python -B -X utf8 TestResults/loadout-placement-worker-observation-persistence-verification-20260924/run-verification.py
git diff --check -- <changed files>
```

The first sandboxed backend build could not read the user-level NuGet configuration. The authorized retry passed 191 tests; after adding the secondary-error regression, a fresh final build passed 192. The final build reports 44 warnings and no errors. During exchange setup, a create-new check found that the v1 fixture had already produced its own manifest; that manifest was verified and preserved. The premature exchange-test invocation skipped because its input environment was not yet available; subsequent configured runs passed all six tests, and the final regression run contains no skips. No required command remains blocked.

`test-runs.json`, `backend-runs.json` and `runtime-inputs.json` record exact commands, destinations and pins. Prior exchanges, initial test exchanges and their runtime binaries remain immutable. Export commands must use fresh directories. Only line 3 changes in the nine status documents; their historical bodies remain byte-identical.

## Remaining work and operational effect

Next exercise a fresh full native production-audit success fixture under the enclosing owner boundary, then resolve remaining reads/parsing, metadata/lease operations and external/transient scratch in a prospective resource model. Terminal receipt persistence remains excluded from its own application counters; the enclosing kernel observation provides a different, overlapping scope. The outer monitor still excludes its own later publication/verification, console and exit. These limitations remain explicit and non-admitting.

Using v3 in a future diagnostic package requires the updated Python modules and a rebuilt, authenticated native producer; existing v1/v2 paths remain supported. No application configuration or database migration changed, and nothing was deployed. The first failed pair stays failed; historical evidence and cumulative charges are unchanged. The 1,806-second audit floor still exceeds the 1,800-second limit, so measurement still requires a separately justified prospective replacement resource model. No scientific launch, native preparation, qualification, timing pair, live-history scan, production entropy draw, scientific reservation or combat occurred. No current resource forecast, compression speedup or search improvement is established.
