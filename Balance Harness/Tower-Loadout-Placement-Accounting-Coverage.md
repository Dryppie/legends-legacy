# Placement accounting: native reads and retained owner lifecycle

The offline Balance Harness now counts its direct contract, manifest, journal and compressed battle reads, and supplies a durable opt-in owner wrapper for phase receipts and cleanup. Verification passes 110 distinct backend tests and 106 distinct Python tests, including fresh plain/compressed study reconstructions. This extends the [previous collector implementation](Tower-Loadout-Placement-Work-Accounting.md); it does not establish whole-process coverage or a current resource forecast.

The inherited necessary gate remains **1,806 seconds against a 1,800-second cap**. Both compressed launch guards remain closed. No cost experiment, scientific reservation, production entropy, preparation, qualification or combat was performed. Implementation-test durations are not resource samples and do not reduce any historical charge or floor.

## Native read coverage

`TowerWorkAccounting` adds counted text and line readers, a string parser, and a generic decoded-stream boundary. Text readers retain UTF-8 defaults and BOM detection. Raw file bytes and UTF-8 bytes supplied through the string parser are separate counters: a UTF-16 file or a journal with line separators need not give equal totals. Buffered bytes returned to the application still count when a later line fails parsing. Parsing attempts, completed parses and successful reconstruction events remain distinct.

`TowerContractJson` keeps its unknown-member and required-constructor-parameter checks while counting contract and owning-manifest reads. `TowerProposalStudyArchive` counts full attempt scans, newline checks and repeated prefix reads; the prefix reader still permits the existing journal writer's handle. `TowerProposalRacingNative` counts charge and input journals. `TowerLoadoutArchive` counts trial-journal reads and compressed battle payloads, inflated bytes and JSON parsing. The real held-out audit also increments its trial-binding counter after successful authentication.

Generic gzip decoding records completion only after a nonempty read observes EOF. Closing a partial stream or issuing a zero-length read does not manufacture a completed pass. This is a decoder-consumption event, not an additional claim of format authentication. The evidence codec retains its own stricter verified-pass completion behavior. Streams keep the collector captured when opened, including asynchronous reads across later scopes.

| Boundary | Coverage after this change |
| --- | --- |
| Harness JSON reads and file hashes | Existing opt-in counters retained |
| Contract and owning-manifest documents | Counted, with strict parsing preserved |
| Proposal attempt, charge, input and trial journals | Actual returned bytes and attempted line parses counted |
| Compressed battle reports | Payload reads, inflated bytes and parsing counted |
| Encoded evidence and independent reconstruction | Existing integrations retained |
| Infrastructure content providers | Still incomplete; several providers call `File.ReadAllText` directly |
| Native writes, copied files, logs and scratch | Not comprehensively instrumented |
| Scientific launch activation | Unchanged; the launcher does not activate these collectors |

## Retained owner lifecycle

`proposal_work_accounting.RetainedOwner` creates a fresh managed directory and requires the accounting module's external SHA-256. It binds the request and owner producer in `binding.json`. Within each ordered phase, it validates the worker's external receipt pin and request/producer/phase bindings, retains the **original receipt bytes**, and attaches their hash to the owner ledger. Process observations carry an explicit producer pin and are saved immediately. Writes use exclusive creation and flush/fsync before returning.

Registered cleanup callbacks run before the enclosing interval ends, including on an exception. Earlier worker files may be removed by cleanup because their receipts are already retained. A complete outcome requires all four phase counter receipts and process observations, successful workers and drained successful processes. Missing evidence, a previously failed phase, cleanup failures and invalid receipts cannot produce a complete owner. The wrapper retains partial intervals and errors for ordinary failure paths. A controller-failure test exercises this with the real Windows owned-process runner, retaining its drained-job observation and a failed partial counter receipt.

`owner-work.json` records the ledger, errors and managed storage snapshot. `files.json` binds all retained members; its SHA-256 is available to an outer caller. Identity checks reject even same-length changes to retained receipts. Flush/write failures are remembered even when the caller catches them, preventing a later complete outcome. If terminal publication fails, no manifest pin is produced and earlier durable receipts remain. An original worker exception is preserved if cleanup or receipt publication also fails.

This wrapper is a working diagnostic API, exercised with fixed-clock lifecycles and small subprocesses. It is **not wired into the scientific launcher**. Its final receipt/manifest serialization and writes are explicitly excluded from its interval and prepublication storage snapshot. A later enclosing process must account for that tail and its own drain. The existing process observation still samples owner memory before handle cleanup; no complete later owner-lifetime memory claim is added. Every receipt remains `wholeProcessCoverage=false` and `usableForAdmission=false`.

## Verification and evidence

The [verification package](../TestResults/loadout-placement-accounting-coverage-verification-20260924) contains before/after source snapshots, logs, TRX results, the native/Python exchange and twelve small diagnostic process observations across the initial and final collector runs, including two sealed retained-owner failure packages. The final observations bind the final accounting module. The [fresh synthetic studies](../TestResults/loadout-placement-accounting-coverage-fixtures-20260924) were generated for this verification. Both packages become immutable when sealed; mutation tests require newly generated directories.

Backend verification covers 108 focused cases and two complete study cases. New cases check UTF-8/UTF-16 BOM semantics, required fields and unknown fields, malformed contracts, journal buffering and failure, compressed battle parsing, zero-length reads, asynchronous EOF, partial disposal and collector isolation. The full study fixtures reconstruct 12 roots, 24 trajectories and 12 placement catalogues with the same `Inconclusive` result. Their native fixture supplies battle/held-out observations directly, so its 12,672 trial bindings are not the independent auditor's 18,816 bindings. The manifest and journal counters are now present in the native fixture. Full native production-audit content loading remains outside that fixture boundary.

Python verification covers 38 collector/process/owner cases, 20 codec cases, 25 owner/arithmetic regressions, seven provenance cases, 14 storage integration cases and two complete-study counter checks. The independent audit reads all 18,816 literal battle reports; the compressed variant performs 18,936 completed decoding passes, including 120 passes over 60 encoded members. Expected logical byte totals are derived from the fresh descriptors, not a previous runtime's byte count. All 60 encoded members match their plain logical bytes and hashes, and both study results match exactly.

```text
build/run-tests.ps1 -Filter '(FullyQualifiedName~BalanceHarnessWorkAccountingTests|FullyQualifiedName~BalanceHarnessEvidenceCodecTests|FullyQualifiedName~BalanceHarnessEvidenceStorageTests|FullyQualifiedName~BalanceHarnessProposalNativeTests)&FullyQualifiedName!~Complete_placement_study' -ArtifactsPath .artifacts/loadout-accounting-coverage-20260924
build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarnessEvidenceStorageTests.Complete_placement_study' -ArtifactsPath .artifacts/loadout-accounting-coverage-20260924
python -B -X utf8 build/test-proposal-work-accounting.py -v
python -B -X utf8 "Balance Harness/analysis/test-proposal-evidence-codec.py" -v
python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v
python -B -X utf8 build/test-proposal-selected-members.py SelectedMembers -v
python -B -X utf8 build/test-proposal-evidence-storage.py --fixtures TestResults/loadout-placement-accounting-coverage-fixtures-20260924 -v
python -B -X utf8 build/test-proposal-study-work.py --fixtures TestResults/loadout-placement-accounting-coverage-fixtures-20260924 --output TestResults/loadout-placement-accounting-coverage-verification-20260924/full-study-work -v
```

These commands record the completed runs; do not rerun them against sealed exports. Test-only `LL_WORK_ACCOUNTING_EXPORT`, `LL_EVIDENCE_STORAGE_EXPORT`, `LL_WORK_ACCOUNTING_EXCHANGE` and `LL_WORK_ACCOUNTING_OBSERVATIONS` named fresh output/exchange directories. The codec regressions used the earlier immutable codec exchange through `LL_EVIDENCE_CODEC_EXCHANGE` without changing it. The first backend build was blocked by sandbox access to NuGet.Config; the approved retry used the existing configuration/cache and passed with existing unrelated warnings. No required command remains blocked.

## Next implementation boundary

Instrument the remaining content-provider reads through a suitable injected I/O boundary without making Infrastructure or Core depend on the harness. Then account for every worker/owner write, copy, log and scratch lifetime, activate bound worker receipts under a separately pinned opt-in owner contract, and measure that owner's final publication/drain from an enclosing process. Native held-out authentication is instrumented, but a fresh full production-audit-path fixture is still needed to verify its complete trial and content coverage. The current synthetic proof deliberately uses narrower native callbacks.

Even completed instrumentation will not reopen the current gate. A separately justified prospective replacement model must identify and bound the retained resource components before any measurement can be considered. Neither compression ratios nor these test durations establish a speedup or qualification.

No migrations, service configuration changes, deployment, live-history rescan or historical accounting adjustment is involved. All 260 inherited evidence pins were authenticated before editing and rechecked at closeout. Historical document bodies remain unchanged; only line 3 of nine status documents points to this report. Changes are confined to the offline harness collectors/read boundaries, their verification, scoped line-ending attributes and these status records.
