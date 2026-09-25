# Placement work-accounting collectors

24 September 2026. Target: the offline BalanceHarness and its Python audit/process support.

Opt-in collectors now retain application-read, decoding, parsing and reconstruction counters, managed storage lifetime, enclosing owner intervals and Windows process-memory observations. Receipts bind their request, producer and phase, preserve partial work on failure and explicitly state their coverage. **They do not change admission: the preserved necessary audit floor remains 1,806 seconds against 1,800.**

This implements the accounting mechanisms from the [prospective resource methodology](Tower-Loadout-Placement-Storage-Resource-Protocol.md). Complete process-wide coverage remains unfinished. There is no new current-format resource forecast, compression speedup estimate or authorization to prepare or launch a compressed study.

## What changed

`TowerWorkAccounting.cs` adds an async-context-local collector, matching the harness's existing opt-in tracing pattern. Nested scopes restore their parents; parallel contexts keep separate collectors. Snapshots copy counters, increments reject negative values and overflow, and file-read wrappers preserve seeking and synchronous/asynchronous reads. File role partitions separate manifest, metadata, journal, payload, ordinary JSON and other reads. Parse/decode counters describe overlapping processing and are not added to application-read bytes.

`HarnessJson` now routes file hashes and JSON reads through the collector when active. `TowerProposalEvidenceStorage` records its metadata reads/parsing and the payload hashes performed while authenticating manifests. The owning manifest document itself is parsed through `TowerContractJson`, which remains outside this collector; that missing coverage is not reported as zero reads. `TowerProposalEvidenceCodec` records payload reads and decoded chunks as they are returned, including work before an exception. It distinguishes started and successfully verified decode passes, and parse attempts from completed parses. Neither a digest failure nor a parser exception erases earlier work. Existing per-call codec results remain unchanged.

The racing and study verifiers count completed trajectory, trial-binding, root, catalogue, held-out member and endpoint reconstruction events. These events describe the code paths actually exercised; they do not stand in for uninstrumented work elsewhere. No collector runs by default and no study-format default changes.

`proposal_work_accounting.py` supplies the independent Python counters and receipt handling. The codec and storage reader accept an optional collector without adding an ambient module import to retained evidence. The independent auditor's optional context collects ordinary JSON, journals, hashes, compressed battle-report work, encoded evidence, and completed reconstruction events. Context restoration is guaranteed on failure. An uninstrumented audit preserves its previous behavior and continues to accept the older pinned readers.

Counter receipts use `tower-proposal-work-counters-v1` in both languages. They include phase, request SHA-256, producer SHA-256, outcome, counters and `InstrumentedOperationsOnly` coverage. Every receipt sets `wholeProcessCoverage` and `usableForAdmission` to false. Python verification checks an external file pin, exact request/producer/phase bindings, integer counters, strict JSON and the coverage declaration. Receipt files are created once and flushed; their own write belongs to an enclosing phase rather than a circular self-charge.

## Storage, owner intervals and memory

The managed-storage collector creates a fresh directory and accounts for every write through its managed handles. It records retained and scratch bytes, concurrent retained-plus-scratch high water, scratch peak, total written bytes and deleted bytes. Deleting scratch does not erase its peak or lifetime charge. Equal-content files count separately. Open handles, unexpected members, changed lengths, case collisions and unsafe paths prevent a complete snapshot. This coverage assumes exclusive use of the managed directory; it is not a filesystem-wide monitor and is not yet wired to every study writer.

`OwnerLedger` enforces the four ordered phases from the protocol. Adjacent intervals share boundaries and sum to the enclosing interval. The caller finishes only after publication and cleanup return, so the collector can include the tail omitted by the existing closeout timestamp. Worker counter receipts are externally pinned before attachment. Duplicate receipts, wrong phase/producer/request bindings, failed workers, timeouts and undrained process observations cannot yield a successful owner outcome. A partial owner failure retains its completed intervals and attached evidence. The ledger's own final receipt write still needs the explicitly separate outer bookkeeping boundary.

The existing `bounded_windows_process.py` runner accepts an optional observer. It reports normal completion, timeout and controller-failure cleanup after closing process/job handles. It queries the kernel job memory high-water value and the owner's lifetime peak commit. Their sum is a **conservative sum of separate peaks**, not a simultaneously sampled peak or incremental owner allocation. The owner-memory query occurs before handle cleanup; the receipt identifies that boundary. It does not establish the entire owner's subsequent lifetime peak. The Windows definitions come from Microsoft's [job-limit structure documentation](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_extended_limit_information) and [process memory counters](https://learn.microsoft.com/en-us/windows/win32/api/psapi/ns-psapi-process_memory_counters).

An unavailable memory query returns null values with `Unknown` coverage, retaining cleanup and failure evidence. It never becomes a zero-byte claim. The default runner retains its existing return contract. These observations introduce no memory limit or resource-model coefficient.

## Verification boundary

All new verification uses fresh synthetic files, tiny owned Python subprocesses or literal study fixtures. It does not run the owned study launcher, a cost experiment, combat, production entropy, qualification or scientific reservation. Existing sealed synthetic studies and historical packages remain immutable.

The tests cover repeated seek/read/hash work, exact native/Python logical decoding, corrupt logical digests, malformed JSON, cancellation, scope isolation, receipt rebinding/tampering, invalid counts, deleted scratch, partial writes, extra files, phase overlap/order, failed-worker completion, kernel memory observations, unknown memory queries, timeout and descendant cleanup. Full native and independent study reconstruction is also checked with accounting enabled; the synthetic native fixture supplies held-out observations directly, while the independent audit reads every retained battle report. Their trial counters therefore describe different collection boundaries and are not comparable throughput denominators.

The [verification package](../TestResults/loadout-placement-work-accounting-verification-20260924) retains source snapshots, native exchange samples, process observations, test logs and TRX results. The [fresh full fixtures](../TestResults/loadout-placement-work-accounting-fixtures-20260924) are kept separately and are sealed after all tests. Mutation tests must never be rerun against either sealed package.

Verification passed **100 distinct backend tests** (98 focused codec/storage/native/counter tests and two complete study cases) and **92 distinct Python tests** (24 collector/process tests, 20 codec tests, 25 owner/arithmetic regressions, seven provenance tests, 14 storage integration tests and two full-study counter checks). Both plain and compressed full studies retain 18,816 literal observations, 12 catalogues, 24 trajectories and the same `Inconclusive` result. The independent collector records all 18,816 trial bindings and 24 held-out members. Its compressed audit records 18,936 completed decode passes: 18,816 battle reports plus 120 passes over the 60 eligible encoded records.

The current native fixture records 2,060,576,832 decoded encoded-evidence bytes; the independent compressed audit records 2,088,017,052 decoded bytes including battle reports. These describe different collection boundaries. Neither is used as a time multiplier or forecast. The tests derive logical byte expectations from the freshly produced descriptors instead of an earlier runtime's fixture byte count.

The initial build needed approved access to the existing NuGet configuration/cache. Both final builds succeeded with existing unrelated warnings and no errors. Initial Python test failures exposed an overly exact subprocess-count assumption, nested fixture-command quoting, and the stale historical byte-count assertion; those test inputs/assertions were corrected and their original logs retained. The independent full-study counter test passed in the initial two-test run; the corrected native counter assertion then passed separately. No required verification command remains blocked.

```text
build/run-tests.ps1 -Filter '(FullyQualifiedName~BalanceHarnessWorkAccountingTests|FullyQualifiedName~BalanceHarnessEvidenceCodecTests|FullyQualifiedName~BalanceHarnessEvidenceStorageTests|FullyQualifiedName~BalanceHarnessProposalNativeTests)&FullyQualifiedName!~Complete_placement_study' -ArtifactsPath .artifacts/loadout-work-accounting-20260924
build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessEvidenceStorageTests.Complete_placement_study' -ArtifactsPath .artifacts/loadout-work-accounting-20260924
python -B -X utf8 build/test-proposal-work-accounting.py -v
python -B -X utf8 "Balance Harness/analysis/test-proposal-evidence-codec.py" -v
python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v
python -B -X utf8 build/test-proposal-selected-members.py SelectedMembers -v
python -B -X utf8 build/test-proposal-evidence-storage.py --fixtures TestResults/loadout-placement-work-accounting-fixtures-20260924 -v
python -B -X utf8 build/test-proposal-study-work.py --fixtures TestResults/loadout-placement-work-accounting-fixtures-20260924 --output TestResults/loadout-placement-work-accounting-verification-20260924/full-study-work -v
python -B -X utf8 build/test-proposal-study-work.py --fixtures TestResults/loadout-placement-work-accounting-fixtures-20260924 --output TestResults/loadout-placement-work-accounting-verification-20260924/native-study-work-check CompleteWork.test_native_reconstructions_preserve_complete_counts_and_partial_coverage -v
```

The first backend run set `LL_WORK_ACCOUNTING_EXPORT` to the new native exchange directory; the full-study run set `LL_EVIDENCE_STORAGE_EXPORT` to the fresh fixture directory. Python used that exchange through `LL_WORK_ACCOUNTING_EXCHANGE`, saved small diagnostic process observations through `LL_WORK_ACCOUNTING_OBSERVATIONS`, and read the previous immutable codec samples through `LL_EVIDENCE_CODEC_EXCHANGE`. These variables are test-only. Subsequent runs need fresh output directories; the full-study mutation commands above record completed verification and must not be rerun against the now-sealed fixtures.

## Remaining coverage and operational effect

The collectors deliberately do not label a successful operation as a complete resource observation. Remaining integration includes direct native journal/contract/content I/O, full write/scratch lifetime across all worker and owner paths, and a producing owner/enclosing controller that pins and retains the receipts through every success/failure boundary. The current scientific owner is unchanged and does not activate this instrumentation. The memory observation excludes later owner work; a complete enclosing receipt must account for that separately.

The next step is to close those integration gaps using fresh synthetic lifecycle tests and an explicit coverage inventory, while keeping both compressed launch guards closed. After that, a separately justified prospective model must reconcile the retained probe/pilot terms before any measurement is considered. Collector correctness and small subprocess durations supply no downward credit against the existing floor.

There are no migrations, service configuration changes, deployments, live-history scans or accounting adjustments. All previous failed experiments, limits, charges and historical document bodies are preserved. Only line 3 of nine current-status documents is updated to point here.
