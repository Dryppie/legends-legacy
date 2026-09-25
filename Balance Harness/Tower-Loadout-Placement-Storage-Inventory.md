# Placement evidence storage inventory and prospective format

**Status: the read-only inventory is complete; the recovery gate remains closed.** Three complete JSON evidence families occupy **91.05%** of the retained placement archive. This supports implementing a lossless, explicitly versioned gzip format for those families. No compression ratio or audit speedup has been measured, and the format is not implemented yet.

The [prospective storage design](Tower-Loadout-Placement-Evidence-Storage-Design.json) specifies the format, integration points, independent verification and cost accounting. It is an engineering design, not a registered timing experiment or an admission. The [earlier recovery protocol](Tower-Loadout-Placement-Recovery-Protocol.md) still has a necessary audit/publication floor of **1,806 seconds** against its unchanged **1,800-second** cap.

## Authenticated findings

The [inventory package](../TestResults/loadout-placement-storage-inventory-20260924/files.json) has SHA-256 `2fbcd48a8e2176a632a64b2921af7fddf6c3b11da4ea72353ec4b5b31e64af8b`. Its helper authenticated all 236 inherited pins and the current source bindings, then streamed and hashed **40,300 files** against the two previously pinned manifests. It recorded exact byte lengths, categories and same-hash duplicate groups without changing either archive.

| Retained evidence | Failed matched baseline | Original completed placement |
| --- | ---: | ---: |
| All files | 19,367 | 20,933 |
| Total bytes | 1,078,787,663 | 1,131,534,583 |
| Search-result JSON | 406,941,995 | 406,864,724 |
| Paired-result JSON | 426,237,679 | 426,157,682 |
| Held-out observation JSON | 147,950,257 | 197,267,378 |
| These three families combined | 981,129,931 (90.95%) | 1,030,289,784 (91.05%) |
| Existing gzip battle reports | 13,256,532 | 14,434,291 |
| Complete placement catalogues | Not applicable | 5,609,328 |
| Exact duplicate bytes within the archive | 25,001,513 (2.32%) | 18,352,866 (1.62%) |

The placement total agrees exactly with its recorded final retained size. The failed baseline's retained size is 47,728 bytes above its native receipt because this inventory includes subsequent retained audit metadata; it is not a replacement native or completed-study measurement. The two runs contain 17,280 and 18,816 literal reports respectively and have different completion states. Their raw size difference is not an estimate of placement overhead or a usable cost ratio.

The duplicate counts are hypothetical gross whole-file savings before any index/reference overhead. Even perfect within-study file deduplication would leave 1,113,181,717 placement bytes. Much of the largest exact duplication is required input/trial journal evidence. Cross-archive shared unique content totals 257,057,253 bytes, but each study must remain self-contained; shared storage across studies is not credited.

Native source inspection explains why large JSON records are a suitable codec boundary: each search saves its complete `search.json`; the study also saves complete paired search results and held-out request/outcome collections. The design retains these representations and their verification relationships. This inventory did not measure repeated subtrees, rewrite JSON, or try compression. Existing battle reports already use gzip and account for only 1.28% of the placement archive.

## Chosen engineering design

The proposed `tower-proposal-json-evidence-gzip-v1` format encodes only complete search reports, paired results and held-out observation files. It preserves the exact bytes the existing serializer emits. Every eligible logical `.json` member has one physical `.json.gz` member, with authenticated physical and decoded lengths and SHA-256 digests. Per-directory indexes are included in the owning and ancestor manifests; a retained root descriptor must agree with the prospective request's explicit format selection.

Complete catalogues, plans, freezes, attempt/charge/trial journals, recipes, summaries, receipts, runtime bindings and existing battle evidence retain their current representation. Every logical observation, duplicate report, source label, order and scientific hash remains present. The native and independent Python auditors must reconstruct the complete study through the selected format and enforce exact membership, bounded decoding, integrity and legacy compatibility. No default switches or historical conversions are part of the design.

This is a narrow codec change with a clear byte-equivalence test. A scenario dictionary or shared content store would require additional reference and provenance rules; the current inventory does not establish that complexity is necessary. Compression remains an engineering hypothesis: its space benefit and CPU cost are unknown until a separately admitted experiment measures the full implementation.

## Cost comparability and gate

The design separates physical stored bytes from logical decoded bytes, repeated decode passes, file/index counts, trial reconstruction and peak scratch. It charges all indexes, headers, failed files and publication work. A reduction in physical bytes cannot serve as a reduction in audit time: the auditors still decode, authenticate and reconstruct the full evidence, potentially more than once.

Any later storage experiment must prospectively fix a matched **complete logical workload** on plain and compressed paths, using the same current implementation, literal inputs, request order and corrected audits. Its phase boundaries, denominator rules, order, limits and failure charges must be fixed before execution. The current failed baseline and completed placement archive cannot become that matched experiment through normalization.

No new numeric forecast is asserted. The inherited floors, margin, publication reserve and resource caps remain in force. A future resource methodology must explain how it handles physical I/O and unchanged logical processing and reconciles the existing probes before any measurement is registered. Smaller files alone cannot reopen the failed recovery route. Both the original failed pair's full charge and its unused candidate allowance remain retained.

## Changes and verification

- `analysis/loadout-placement-storage-inventory.py` implements bounded, read-only membership/hash/byte inventory, within-archive duplicate accounting and retained-package replay. Replay authenticates the recorded inventory using its external package pin and rederives the arithmetic; it does not remeasure source files or infer new audit costs.
- `analysis/test-loadout-placement-storage-inventory.py` has **20 passing tests** covering exact-byte inventory, duplicate accounting, unsafe paths, altered/missing/extra evidence, malformed manifests/JSON and closed execution flags.
- The prospective JSON design and this report record the findings and implementation contract. Nine current-status lines point here; their historical bodies remain byte-for-byte unchanged. `.gitattributes` protects the new Python and design files with LF endings.

Verification commands completed:

```text
python -B -X utf8 "Balance Harness/analysis/test-loadout-placement-storage-inventory.py" -v
python -B -X utf8 "Balance Harness/analysis/loadout-placement-storage-inventory.py" create --output TestResults/loadout-placement-storage-inventory-20260924
python -B -X utf8 "TestResults/loadout-placement-storage-inventory-20260924/helper.py" verify --output TestResults/loadout-placement-storage-inventory-20260924 --expected-manifest-sha256 2fbcd48a8e2176a632a64b2921af7fddf6c3b11da4ea72353ec4b5b31e64af8b
```

The [verification package](../TestResults/loadout-placement-storage-inventory-verification-20260924) retains test logs, source snapshots, the replay and design-to-inventory checks. No required command was blocked. Backend tests were not rerun because backend, fixture, owner, auditor and runtime sources did not change. Their previous verification remains pinned.

This inventory charged its declared **180 seconds and 67,108,864 bytes** at the start and completed before sealing in **15.766 seconds**. That elapsed time describes this read-only operation, not a new native/audit cost sample. No compression trials, archive rewrites, native preparation, qualification, production entropy, scientific reservations, combat or live-history rescan occurred.

The next implementation is the opt-in streaming codec and independent decoder, followed by full logical reconstruction and legacy tests using synthetic evidence. A separate prospective resource protocol must pass its own necessary gate before any measured fixture can run. No migrations, application configuration changes, deployments or external-environment operations occurred.
