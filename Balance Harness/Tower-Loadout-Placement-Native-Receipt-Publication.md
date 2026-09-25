# Loadout placement: native receipt publication accounting

Current status: native workers now support v2 receipt-publication observations, independently validated by Python. Verification passes 160 backend and 205 Python tests. Whole-process coverage remains incomplete; the compressed launch guards and the 1,806/1,800-second resource gate stay closed.

## Implementation

`LL/tools/BalanceHarness/TowerProposalWorkReceipt.cs` accepts the existing v1 binding and the explicit `tower-proposal-worker-binding-v2` contract already used by Python. V2 requires an external, distinct `publicationPath`. Unknown fields, duplicates, missing/null v2 destinations, and publication fields attached to v1 bindings are rejected before work. The observation destination is reserved exclusively before the receipt or worker action. Existing evidence is preserved. V1 retains its original receipt shape, streaming serialization and flush behavior.

The new `TowerReceiptPublication.cs` collector emits the common `tower-proposal-worker-publication-v1` observation. It records receipt reservation, serialization, binary write, flush, sync and close attempts, completions and failures, together with serialized byte length and SHA-256. The v2 receipt is serialized to a byte array before its write so the observation can bind its exact intended bytes. Only a completed write accepts that buffer's bytes; a thrown write has unknown progress. The collector does not infer failed progress from file length or position. The .NET write API accepts a requested buffer without returning a short-write count. [Microsoft Stream.Write documentation](https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.write?view=net-10.0).

V2 explicitly calls `Flush()` and then `Flush(true)`, recording the latter as the sync boundary, before disposing the stream. These are observations of application calls and accepted bytes, not physical-disk traffic or independent proof of durable storage. V1 still makes its original `Flush(true)` call. The v2 byte-array allocation and additional explicit flush belong to this new diagnostic path; no overhead or speedup estimate is inferred. [Microsoft FileStream.Flush documentation](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=net-10.0).

Publication outcome remains separate from worker outcome. A failed or cancelled worker may publish a complete failure receipt. A successful body may suffer a publication failure. Receipt serialization/write errors survive secondary close failures; observation-persistence failures preserve the original worker error. Observation publication occurs after the receipt publication attempt, including close when a receipt was opened. The observation's own persistence remains excluded. Failed receipt reservation leaves serialized size and digest null.

Worker counters, publication counters and kernel job totals remain separate; their scopes overlap and must not be summed. Native accounting restores the enclosing collector before publishing either artifact, including asynchronous success and failure paths. Internal serialization/open/sync delegates allow deterministic failure tests without global hooks or a production test mode.

`Balance Harness/analysis/proposal_owner_supervisor.py` now requests v2 for all four diagnostic worker phases. The Python validator and retained owner required no changes: they validate the same operation relationships, binding, exact successful receipt bytes and non-admitting coverage flags for both languages. Accounting remains opt-in. The scientific launcher, admission rules, archive inventory, default CLI behavior and historical retained runtimes are unchanged. Future use requires fresh authenticated native producer and supervisor bindings.

## Verification

The fresh [verification package](../TestResults/loadout-placement-native-receipt-publication-verification-20260924) contains before/after snapshots, backend TRX, command logs, native exchanges and Python owner fixtures.

`BalanceHarnessReceiptPublicationTests.cs` adds **37 backend tests** for all native phases, strict v1/v2 schema handling, existing-output rejection, async collector restoration, cancellation, changed inputs, exact bytes, failed reservation/serialization/write/flush/sync/close, preservation of primary errors, and observation-persistence failure. The native suite exports 15 independently hashed cases: three successful literal worker boundaries, two failed/cancelled bodies, a real reservation failure, six injected publication failures and three real commands rejecting literal input. Successful literal boundaries do not establish full production-audit success. Injected stream failures are explicitly labeled.

The existing work counters (18), v1 worker receipts (30), file accounting (30), content accounting (23) and write accounting (22) also pass: **160 distinct backend tests**, with no skips. The build used a new isolated `.artifacts/loadout-native-publication-20260924` output and did not replace retained runtimes. Existing repository compiler/analyzer warnings remain; there were no build errors after access was resolved.

`build/test-proposal-native-publication.py` adds **7 Python tests** that authenticate every exported native case and validate them using the unchanged independent reader. It also runs three real native worker processes against invalid literal input under the existing Windows owner. All fail before preparation; the owner retains failed worker receipts, complete receipt-publication observations, drained job observations and kernel I/O totals. The fixtures contain no production entropy, reservations or combat.

The existing 198 Python tests also pass, for **205 distinct Python tests**, with no skips. The routed supervisor fixture now requests v2 in all phases and retains their separate publication observations. Its native commands and process observations remain synthetic; its native-shaped observations use the common Python publication collector and are not presented as native execution evidence. Real native execution evidence comes from the separate native fixtures. Fresh v1 native exports verify backward compatibility against the newly built DLL.

Completed verification commands:

```powershell
./build/run-tests.ps1 -ArtifactsPath .artifacts/loadout-native-publication-20260924 -Filter 'FullyQualifiedName~BalanceHarnessReceiptPublicationTests|FullyQualifiedName~BalanceHarnessWorkerReceiptTests|FullyQualifiedName~BalanceHarnessWorkAccountingTests|FullyQualifiedName~BalanceHarnessWriteAccountingTests|FullyQualifiedName~BalanceHarnessFileAccountingTests|FullyQualifiedName~BalanceHarnessContentAccountingTests'
```

```text
python -B -X utf8 build/test-proposal-native-publication.py -v
python -B -X utf8 build/test-proposal-receipt-publication.py -v
python -B -X utf8 build/test-proposal-owner-supervisor.py -v
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

The first sandboxed build could not read the local NuGet configuration. An authorized retry completed the build and all 160 backend tests. Both logs are retained; no required command remains blocked. Exact Python commands, environments and external exchange pins are in `test-runs.json` and `runtime-inputs.json`. Mutation tests used fresh directories and must not be rerun in these sealed destinations. Only line 3 changes in the nine status documents; their historical bodies remain byte-identical.

## Remaining work and operational effect

Next close the observation-persistence and owner-process lifecycle gaps: worker/supervisor observation publication, final console output and process exit, whole-owner memory lifetime and external/transient scratch. Remaining owner/native reads and parsing, metadata/path checks and lease-handle operations also remain incomplete. A fresh full native production-audit success fixture is still required.

Both admission guards remain closed, and the 1,806-second audit floor still exceeds the 1,800-second limit. A separately justified prospective resource model is required before measurement. This work establishes no current resource forecast, compression speedup or search improvement. The first failed pair stays failed; cumulative historical charges and evidence are unchanged. No scientific launch, native preparation, qualification, timing pair, live-history scan, production entropy draw, scientific reservation or combat occurred. No migrations, application configuration changes or deployments occurred. Only isolated test binaries were rebuilt.
