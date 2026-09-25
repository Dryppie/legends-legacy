# Native file operations and compact publication accounting

The offline Balance Harness now accounts for additional text writes, content/runtime copies, the root storage descriptor, proposal exports and compact publication. Native file operations retain their original APIs and failure behavior. Accounting is still opt-in: the scientific owner does not activate it, receipts remain `wholeProcessCoverage=false` and `usableForAdmission=false`, and both compressed launch guards stay closed. The inherited 1,806-second floor still exceeds the 1,800-second cap. No current resource forecast or compression speedup is established.

## Changed files and decisions

`TowerWorkAccounting.Files.cs` extends the collector with native text/copy operations and tracked directory publication. Its text helpers call the original `File.AppendAllText` and `File.WriteAllText`, retaining their UTF-8 encoding, BOM handling, sharing, creation and overwrite behavior. Successful calls add their encoded application bytes; failures explicitly leave accepted bytes unknown. Existing and resulting logical file lengths participate in the tracked storage gauges. Missing paths count as absent; inaccessible length observations remain failures rather than being silently treated as zero.

Native `File.Copy` is preserved, including file metadata and overwrite/error behavior. Its completed destination length is reported separately as `fileCopyLogicalBytes.<role>`. This is a logical copy measure, not observed application-stream reads/writes or physical disk traffic; the OS may perform the copy internally. Failed copy progress remains unknown. When the existing bounded copier uses streams, the collector measures actual returned reads and successful writes, including a read whose chunk is subsequently rejected by the byte cap. Destination hashing remains a separate read.

| Changed boundary | Result |
| --- | --- |
| `TowerLoadoutArchive` | Trial JSONL appends use the native text observer |
| `TowerBundle` | Content copies distinguish native copy lengths from subsequent hash reads; scorecard text is observed |
| `TowerBossStudyArchive` | Unbounded executable copies retain File.Copy; bounded copies expose stream reads/writes; study Markdown is observed |
| `TowerProposalEvidenceStorage` | The root descriptor now participates in metadata writes and durable flush accounting |
| `TowerProposalPolicies` | All three exported members participate in writes without changing the exclusive inventory or budget |
| `TowerCompactBundle` and `HarnessJson` | Gzip chunks and their receipts are scratch until publication; existing public WriteNew signature is preserved via an internal scratch overload |
| `TowerCompactPublication` | Successful original directory renames reclassify verified pending members; failures/retries keep scratch and never repeat write charges |
| `TowerCompactResume` | Checkpoint/attempt writes and flushes, pending final-manifest publication, and existing cleanup deletions use the collector |

Compact publication observes both validated pending members even when they predate the current collector. Prefix matching uses a directory separator, so similarly named sibling directories are excluded. Existing retry counts, cancellation, revalidation and no-overwrite rules are unchanged. The actual commit, gzip and attempt functions are internal test seams, allowing literal fixture data to exercise production code without entering combat.

Storage still describes observed logical lengths of instrumented files. Current gauges and historical peaks are distinct; neither is a whole-directory or physical-allocation measurement. Do not add native copy logical bytes to stream I/O and describe the result as physical I/O. Do not sum worker storage gauges/peaks and describe the result as an enclosing process peak.

## Verification and evidence

The [verification package](../TestResults/loadout-placement-native-file-accounting-verification-20260924) retains before/after source snapshots, logs, TRX results, a native/Python file-operation fixture and six small diagnostic process observations. The [new plain/compressed study fixtures](../TestResults/loadout-placement-native-file-accounting-fixtures-20260924) are distinct from every earlier package. Packages become immutable after sealing; future mutation tests require new directories.

Verification passes **208 distinct backend tests** (206 focused and two full studies) and **114 Python tests**. The 30 new backend cases compare native and observed text output for null/empty/multibyte/invalid-surrogate strings, existing BOMs, append/overwrite and locked-file failures; compare File.Copy metadata and errors; check bounded-copy limits/cancellation, partial content-copy failure, both executable retention modes, storage metadata budgets, proposal exports, compact gzip/receipt equivalence, directory rename retries and durable attempt caps. Every new test has a guard forbidding combat. Existing runtime-retention, proposal-policy, read/content/write/codec and native-evidence regressions also pass.

The captured file-operation fixture verifies twenty logical native-copy bytes, twenty observed bounded-copy read bytes, twenty observed bounded-copy write bytes, seventeen appended text bytes, and a final/peak retained length of 57 bytes. The source file is outside the measured destination set. Its export test was repeated against the final build into a new directory; the initial and final fixture manifests are identical. Python independently authenticates the external manifest pin, original file bytes and receipt bindings. The bindings are explicit synthetic values, not production worker attestations.

Five existing write checks are repeated against the immutable earlier replacement fixture and the fresh full studies. They verify full-generation journal bytes and all sixty encoded payload lengths/trailers. Other Python groups cover the new file-operation fixture (3), collector/owner behavior (38), codec behavior (20), owner arithmetic and closed guards (25), provenance (7), storage integration (14), and complete native/independent audit counters (2). Both full results remain `Inconclusive` and identical; all sixty encoded logical members match the plain bytes/hashes; the independent auditor reconstructs all 18,816 literal observations. Generation receipts end before their own serialization, fixture export and audits. The native audit still uses narrower literal callbacks and does not establish complete production-audit-path coverage.

The initial build hit the known sandbox denial for the user's NuGet.Config. The approved retry exposed two fixture mistakes: proposal exports require at least two policies, and altering a serialized descriptor's budget changes its own length. The fixtures now use two valid policies and an unambiguously insufficient descriptor budget. Final runs pass with existing unrelated build warnings. No required verification command remains blocked.

```text
build/run-tests.ps1 -Filter '((FullyQualifiedName~BalanceHarnessFileAccountingTests|FullyQualifiedName~BalanceHarnessWriteAccountingTests|FullyQualifiedName~BalanceHarnessContentAccountingTests|FullyQualifiedName~BalanceHarnessWorkAccountingTests|FullyQualifiedName~BalanceHarnessEvidenceCodecTests|FullyQualifiedName~BalanceHarnessEvidenceStorageTests|FullyQualifiedName~BalanceHarnessProposalNativeTests|FullyQualifiedName~BalanceHarnessProposalPolicyTests|FullyQualifiedName~BalanceHarnessProposalRuntimeRetentionTests)&FullyQualifiedName!~Complete_placement_study)' -ArtifactsPath .artifacts/loadout-native-file-accounting-20260924
build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarnessEvidenceStorageTests.Complete_placement_study' -ArtifactsPath .artifacts/loadout-native-file-accounting-20260924
python -B -X utf8 build/test-proposal-file-work.py --fixture TestResults/loadout-placement-native-file-accounting-verification-20260924/file-fixture-final --manifest-pin <externally-checked-files.json-SHA256> -v
python -B -X utf8 build/test-proposal-write-work.py --fixture TestResults/loadout-placement-native-write-accounting-verification-20260924/write-fixture --manifest-pin <externally-checked-files.json-SHA256> --studies TestResults/loadout-placement-native-file-accounting-fixtures-20260924 -v
python -B -X utf8 build/test-proposal-work-accounting.py -v
python -B -X utf8 "Balance Harness/analysis/test-proposal-evidence-codec.py" -v
python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v
python -B -X utf8 build/test-proposal-selected-members.py SelectedMembers -v
python -B -X utf8 build/test-proposal-evidence-storage.py --fixtures TestResults/loadout-placement-native-file-accounting-fixtures-20260924 -v
python -B -X utf8 build/test-proposal-study-work.py --fixtures TestResults/loadout-placement-native-file-accounting-fixtures-20260924 --output TestResults/loadout-placement-native-file-accounting-verification-20260924/full-study-work -v
```

These are records of completed verification, not permission to mutate sealed packages. Tests using earlier exchanges are read-only. The compact checks use literal write/publication operations; combat-producing compact creation/resume tests were deliberately outside this verification scope. Test export variables name fresh destinations and do not change application configuration.

## Remaining work and operational effect

Next, account for the Python owner's copies, logs and publication files, and emit bound opt-in worker receipts that the scientific owner retains. Complete enclosing receipt/manifest publication, process drain and owner memory coverage. The current owner remains diagnostic and its final publication tail remains uncovered. Review lease/DeleteOnClose lifetime, remaining binary and compact read paths, in-memory parsing/materialization, and any other uninstrumented boundaries. Add a fresh full native production-audit-path fixture. External changes and files outside the active collector remain outside tracked storage coverage.

A separately justified prospective replacement resource model is still required before measurement. This stage performs no timing pair, production entropy draw, scientific reservation, scientific native preparation, qualification, combat, live-history scan, migration, application configuration change or deployment. Historical charges and the permanently failed first pair are unchanged. All 272 inherited historical pins were authenticated before edits and rechecked at closeout. Only line 3 of the nine status documents changes; their historical bodies remain byte-identical. Only an isolated test build was produced; no retained runtime was replaced.
