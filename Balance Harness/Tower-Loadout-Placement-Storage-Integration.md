# Explicit evidence storage and complete-study audit integration

**Status: the opt-in compressed format is integrated into study writing and both audit implementations.** Fresh synthetic plain and compressed studies exercise all twelve placement roots. Default requests retain the existing plain JSON representation. Compressed production preparation and owned launching remain explicitly blocked until a separate prospective resource protocol is established.

This builds on the [codec implementation](Tower-Loadout-Placement-Evidence-Codec-Implementation.md) and [frozen storage design](Tower-Loadout-Placement-Evidence-Storage-Design.json). It does not rewrite the original archives, amend the failed matched experiment or replace its resource evidence. The existing recovery gate remains closed at **1,806 necessary audit seconds against 1,800 allowed seconds**.

## Request, writer and index binding

`ProposalStudyRequest` now has an optional `evidenceStorage` selection containing the explicit format version, trusted aggregate encoded-logical and physical byte limits, member limit and hashes of the independent reader modules. The property is omitted when null, preserving legacy request serialization. Selections reject unknown versions, nonpositive or excessive limits, more than 72 encoded members and invalid module pins.

`TowerProposalEvidenceStorage` binds that selection to a retained root descriptor listing exactly 25 index locations: both arms for twelve roots and the study directory. Its writer encodes complete `search.json`, `pair-*.json` and `heldout-*.json` members, with no content deduplication or change to logical serialization. Each directory's index records the codec, physical mapping, lengths and both SHA-256 digests. Indexes are complete before their owning manifests are sealed. Partial work cannot finish the storage writer.

The writer enforces aggregate encoded logical bytes, physical bytes including descriptors/indexes, and member counts. The search adapter also charges compressed writes against its existing per-arm evidence allowance; the original whole-study native storage and deadline checks remain in place. Other evidence continues through the existing writer, including complete placement catalogues, freeze barriers, attempts, charge/input/trial journals, recipes and existing gzip battle reports.

`RunStudy` passes the same explicit writer through both search arms and the study output callback. All 24 search outputs and their indexes finish before the existing all-root freeze and first held-out attempt. The study index closes after its held-out records, before the study manifest. Logical plan, scenario, result and freeze identities do not incorporate the physical encoding.

## Native and independent verification

The native reader checks request/root agreement, the fixed directory set, owning manifests, index versions, exact caller-derived logical membership, physical digest/length agreement, aggregate limits and plain/compressed collisions. Native racing and study reconstruction then read the encoded logical records through the codec and apply their existing complete reconstruction and membership checks. Metadata is rechecked before the audit finishes.

The independent auditor loads the two retained Python reader modules only after checking their request-pinned hashes. `proposal_evidence_storage.py` independently validates the same root/index contract and supplies records to the existing search, catalogue, provenance, held-out, attempt and endpoint checks. Plain requests reject a storage descriptor; legacy search verification does not silently accept compressed replacements. The captured study path retains and verifies both reader modules before any entropy boundary.

The codec's repeated decode passes remain visible. The storage reader's physical-read counter covers codec payload reads; manifest and metadata scans are additional work and must be included in future end-to-end phase measurements. No physical-byte reduction is interpreted as an audit-time reduction. Python still materializes decoded Unicode JSON and its object graph, so memory is also a future resource-model concern.

## Verification boundary

The new complete fixtures are generated from deterministic literal inputs in unit tests, with a guard that prevents the combat engine from running. Their synthetic reservation files are local test data. They are neither owned cost fixtures nor production/scientific reservations, and their test durations and storage sizes are not forecast samples.

The native tests reconstruct each complete study and reject resealed freeze changes. Independent tests compare full plain/compressed results, every encoded logical digest with its plain counterpart, all twelve catalogues, the freeze and attempt journal. They also exercise resealed missing/repeated entries, mappings, codecs, lengths, collisions, versions, changed module pins, request downgrade and catalogue tampering. Small native storage fixtures independently exercise resealed index corruption. Existing codec, arithmetic, provenance and native proposal regressions verify the legacy paths.

Both studies retain 18,816 literal reports, including 6,144 held-out observations. All 60 encoded logical members match the corresponding plain file's exact SHA-256 and length. Both reconstructed results remain `Inconclusive`, with six differing and six novel roots and the same validation/fallback counts. These are correctness fixtures, not an efficacy result. The full owned process/publication lifecycle and current-runtime resource qualification have not been exercised for the new format.

The [verification package](../TestResults/loadout-placement-storage-integration-verification-20260924) retains source snapshots, logs, test results and evidence pins. The newly generated [synthetic fixture package](../TestResults/loadout-placement-storage-integration-fixtures-20260924) is kept separately from historical studies. No frozen historical document body is updated; nine current-status lines point to this report.

Verification passed **92 distinct backend tests** across two runs (52 initial tests including both complete studies; 90 final codec/index/legacy tests, with overlap) and **66 Python tests** (14 full integration, 20 codec, 25 arithmetic/owner and seven provenance tests). The final selected runs have no skips. Backend tests used `build/run-tests.ps1` and the isolated `.artifacts/loadout-storage-integration-20260924` directory; the existing NuGet configuration/cache required approved access. Builds have no errors; unrelated warnings remain. No required command remains blocked.

```text
build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessEvidenceStorageTests|FullyQualifiedName~BalanceHarnessEvidenceCodecTests' -ArtifactsPath .artifacts/loadout-storage-integration-20260924
build/run-tests.ps1 -Filter '(FullyQualifiedName~BalanceHarnessEvidenceStorageTests&FullyQualifiedName!~Complete_placement_study)|FullyQualifiedName~BalanceHarnessEvidenceCodecTests|FullyQualifiedName~BalanceHarnessProposalNativeTests' -ArtifactsPath .artifacts/loadout-storage-integration-20260924
python -B -X utf8 build/test-proposal-evidence-storage.py --fixtures TestResults/loadout-placement-storage-integration-fixtures-20260924 -v
python -B -X utf8 "Balance Harness/analysis/test-proposal-evidence-codec.py" -v
python -B -X utf8 build/test-proposal-affinity-study.py ArithmeticTests -v
python -B -X utf8 build/test-proposal-selected-members.py SelectedMembers -v
```

The first native run set `LL_EVIDENCE_STORAGE_EXPORT` to the new synthetic fixture directory; later runs cleared it. The final Python codec run used the prior sealed `native-exchange-final` synthetic samples through `LL_EVIDENCE_CODEC_EXCHANGE`. These are test-only environment variables, not service configuration. Initial optional tests without those samples were superseded by the explicit no-skip selections above.

## Remaining work and operational effect

The next step is a **separate prospective resource methodology**, not another timing run. It must state how full matched logical workloads, repeated decoding, physical scans, memory, scratch, both audits and publication contribute to its forecast, and explicitly reconcile the earlier pilot/probe evidence and failed gate. Until that protocol passes its necessary gate, the native `Inspect` path and Python owner reject compressed preparation/launch. Existing failed experiments and all prior charges remain intact.

Changed application-side files are limited to the offline BalanceHarness storage adapter, request/worker integration, racing/study writers and audit readers, plus a small external-byte charging hook in the existing evidence writer. Python changes add the storage reader and connect it to the independent auditor; the owner gains the closed-gate check. Tests and fixture-export helpers cover the integration. The standalone byte codec remains unchanged.

No migrations, service configuration changes, deployments or external-environment operations occurred. Existing requests keep their default representation. There is no new runtime qualification, scientific admission, production entropy or combat, and no resource-accounting adjustment is inferred from synthetic tests.
