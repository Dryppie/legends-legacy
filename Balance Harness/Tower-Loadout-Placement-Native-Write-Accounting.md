# Native write and scratch accounting

The offline Balance Harness now records successful writes and observed file lifetimes at its shared JSON, battle-gzip, evidence-codec, durable-journal and atomic-publication boundaries. Accounting remains opt-in. The scientific owner does not activate it, every receipt remains `wholeProcessCoverage=false` and `usableForAdmission=false`, and both compressed launch guards remain closed. The inherited 1,806-second floor still exceeds the 1,800-second cap. This work establishes no current resource forecast or compression speedup.

## Changed boundaries and semantics

`TowerWorkAccounting.Writes.cs` extends the existing collector. A stream captures the collector when wrapped, so a later nested activation cannot take its writes. With accounting disabled, the original stream is returned. Instrumentation preserves the writers' original file modes, sharing, compression levels, durable flushes, byte allowances and rename behavior.

| Writer | Integration |
| --- | --- |
| `HarnessJson.WriteNew` | Count bytes accepted by the file stream, including partial serialization output; retain CreateNew and performance tracing |
| `TowerLoadoutArchive.WriteBattle` | Count compressed file bytes through final gzip trailer disposal; plain reports use HarnessJson |
| `TowerProposalEvidenceCodec.WriteRawNew` | Count physical file writes beneath the existing codec allowance/digest streams; keep charge-before-dispatch and durable flush |
| `TowerAdaptiveRacingNative.Evidence` | Count each JSON write and journal append; retain WriteThrough, sharing, caps and flush-before-return |
| `TowerPracticalSearch.Attempts` | Count durable Started/Completed journal lines without changing attempt validation or reader sharing |
| `TowerCompleteReservation.Storage` | Observe the existing target and pending scratch together, then reclassify after a successful atomic rename; count allocation journal appends without closing caller-owned handles |

Successful stream writes add `applicationWriteBytes.<role>`. A failed write can partially overwrite a file without changing its length or position, so the collector never infers its accepted bytes. It separately records the failed operation, requested bytes and `failedWriteBytesUnknown`, then attempts to observe the retained length. Flush, close, move, delete and length-observation failures remain explicit. An observation failure does not replace the original I/O exception. Existing codec budget charges remain distinct from successful application writes and are never refunded by this accounting.

Storage fields describe **observed logical lengths of tracked files**, not physical disk allocation or a complete directory inventory. Current retained, scratch and combined byte gauges can decrease. Their peaks remain, including simultaneous old-target/pending-replacement bytes and scratch later deleted. Atomic renaming does not count the same bytes as a second write. Replaced and deleted bytes are recorded separately. Updates are incremental rather than rescanning a growing directory after each write.

The gauges and peaks are not additive work totals. They must not be summed across worker receipts and presented as an enclosing storage peak. Uninstrumented/external file changes and gaps between observations remain outside coverage. The deletion helper is exercised by the scratch fixture; it does not change existing production cleanup behavior.

## Verification

The [verification package](../TestResults/loadout-placement-native-write-accounting-verification-20260924) retains before/after source snapshots, logs, TRX results, a native/Python replacement fixture and six small diagnostic process observations. The [fresh full study fixtures](../TestResults/loadout-placement-native-write-accounting-fixtures-20260924) contain separate plain and compressed studies. All packages become immutable after sealing; subsequent mutation tests require new generated directories.

The 22 new backend cases cover captured collector ownership, synchronous/asynchronous writes, overwrites and truncation, partial failures, unavailable lengths, flush/close failures, atomic replacement, failed rename, scratch deletion, untracked operations, byte caps, exclusive creation, durable journal sharing, caller-owned journal handles, adaptive evidence appends, serialization failures and gzip trailer parity. The standalone replacement fixture starts with seven retained bytes, publishes fifteen new bytes, creates and deletes twenty scratch bytes, and independently verifies 35 successful write bytes, a 35-byte combined peak and fifteen final retained bytes.

The two complete studies now activate a separate native write collector through synthetic generation and provisional publication, before the existing independent native-audit collector starts. These generation receipts use explicit synthetic test bindings. They exclude their own serialization, later fixture export, mutation tests and independent audits. All battle outcomes and entropy input are fixed fixture values; combat is forbidden by the existing guard. The native audit still uses narrow literal callbacks, so this is not the missing whole production-audit-path coverage proof.

Verification passes **191 distinct backend tests** (153 focused, 36 shared reservation regressions and two full studies) and **111 Python tests**. The five write checks independently verify the standalone fixture, full-generation journal byte totals, and all sixty physical encoded members including gzip trailers. Remaining Python groups cover collector/owner behavior (38), codec behavior (20), owner arithmetic and closed guards (25), provenance (7), storage integration (14), and complete native/independent audit counters (2). Both full results match with `Inconclusive`; all sixty encoded logical members match the plain bytes/hashes; the independent auditor reconstructs all 18,816 literal observations.

The first build encountered the known sandbox denial for the user's NuGet.Config. The approved retry built successfully and exposed three fixture issues: Windows reports directory rename/delete failures as UnauthorizedAccessException, and the unavailable-length fake stream needed to preserve its intended injected write exception. Tests now compare native filesystem exception behavior and preserve the injected exception. The final runs pass with existing unrelated build warnings. No required verification command remains blocked.

```text
build/run-tests.ps1 -Filter '((FullyQualifiedName~BalanceHarnessWriteAccountingTests|FullyQualifiedName~BalanceHarnessContentAccountingTests|FullyQualifiedName~BalanceHarnessWorkAccountingTests|FullyQualifiedName~BalanceHarnessEvidenceCodecTests|FullyQualifiedName~BalanceHarnessEvidenceStorageTests|FullyQualifiedName~BalanceHarnessProposalNativeTests)&FullyQualifiedName!~Complete_placement_study)' -ArtifactsPath .artifacts/loadout-native-write-accounting-20260924
build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessEvidenceStorageTests.Complete_placement_study' -ArtifactsPath .artifacts/loadout-native-write-accounting-20260924
build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarnessTowerCompleteBindingTests' -ArtifactsPath .artifacts/loadout-native-write-accounting-20260924
python -B -X utf8 build/test-proposal-write-work.py --fixture TestResults/loadout-placement-native-write-accounting-verification-20260924/write-fixture --manifest-pin <externally-checked-files.json-SHA256> --studies TestResults/loadout-placement-native-write-accounting-fixtures-20260924 -v
python -B -X utf8 build/test-proposal-work-accounting.py -v
python -B -X utf8 "Balance Harness/analysis/test-proposal-evidence-codec.py" -v
python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v
python -B -X utf8 build/test-proposal-selected-members.py SelectedMembers -v
python -B -X utf8 build/test-proposal-evidence-storage.py --fixtures TestResults/loadout-placement-native-write-accounting-fixtures-20260924 -v
python -B -X utf8 build/test-proposal-study-work.py --fixtures TestResults/loadout-placement-native-write-accounting-fixtures-20260924 --output TestResults/loadout-placement-native-write-accounting-verification-20260924/full-study-work -v
```

These commands describe completed verification, not authorization to mutate sealed evidence. The work/codec tests read earlier immutable native exchanges; only the newly generated full fixtures were used by mutation tests. Test export variables name fresh test destinations and are not application configuration.

## Remaining work and operational effect

Next, instrument the remaining native direct writes (including the `TowerLoadoutArchive` trial append and compact gzip path), content/runtime copies, owner logs and publication, and scratch cleanup. Preserve their native failure and durability behavior. Add bound opt-in worker receipt emission and scientific-owner integration, then close the owner receipt/manifest publication tail, process drain and enclosing owner memory coverage. Audit remaining binary reads, in-memory parsing/materialization and full native production-audit coverage. The current collector cannot detect all external changes or establish a whole-process peak.

A separately justified prospective replacement resource model remains necessary before measurement. No timing pair, production entropy, scientific reservation, native scientific preparation, qualification, combat, deployment, migration or application configuration change was performed or authorized by this stage. Historical resource totals and the permanently failed first pair remain unchanged. All 268 inherited historical pins were authenticated before editing and checked again at closeout. Only line 3 of the nine status documents changes; their historical bodies remain byte-identical. No retained runtime was replaced.
