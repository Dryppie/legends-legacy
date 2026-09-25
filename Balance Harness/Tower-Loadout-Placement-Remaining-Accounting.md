# Loadout placement: remaining reads and accounting boundaries

Current status: native reservation-byte reads and the diagnostic owner's four result JSON reads now have operation accounting. Verification passes 202 backend tests and 296 Python tests. A source-pinned inventory describes 16 boundary families and a prospective resource-model draft. Whole-process coverage remains incomplete; both compressed guards and the 1,806/1,800-second resource gate remain closed.

The target is the offline Balance Harness and its diagnostic owner. No game service, combat rule, scientific search policy or deployment changes.

`TowerWorkAccounting.Files.cs` adds a wrapper around the existing `File.ReadAllBytes` call. `TowerProposalStudyProtocol.cs` uses it for the reservation audit's `entropy.bin` read. Successful calls count the returned array length and completed operation. Failed calls preserve the original exception and record unknown read progress; the API does not expose a failed read's consumed prefix. A later allocation mismatch does not turn a completed byte read into a failed read. Sharing, allocation and native error behavior remain those of the original API.

`proposal_work_accounting.py` adds `OwnerFileCounters.read_json_text`; `run-proposal-affinity-study.py` routes its JSON reads through a helper that preserves the inactive `Path.read_text`/`json.loads` path. Active diagnostic scopes count raw returned bytes, text-read attempts/completion/failure, and JSON parse attempts/completion/failure. The parser-byte measure is the UTF-8 size of decoded, newline-normalized text, separate from raw file bytes. These units overlap and must not be added. Ordinary JSON parsing still accepts its existing duplicate-key and nonfinite-value behavior; the stricter receipt parser is not substituted. Each call retains its original encoding, including UTF-8-sig and the default locale. Open/decode/read failure starts no parse and records unknown read progress.

The four owner result reads are `native-receipt.json`, `independent-audit.json`, `native-audit.log` and `provisional-result.json`. Initial request/admission reads share the helper but still precede accounting activation. Their inclusion in the helper does not establish coverage. The supervisor, outer monitor, bounded process helper, independent auditor and codecs are unchanged. Both first compressed guards, default launch mode and all pre-existing accounting methods are preserved.

The [boundary inventory](Tower-Loadout-Placement-Accounting-Boundaries.json) pins source hashes and exact line anchors for 47 references across 16 selected families. It is a source review, not an exhaustive static or dynamic call graph. Its admitting, whole-process and measurement-authorized flags are false. Remaining work includes:

| Boundary | Finding and required treatment |
| --- | --- |
| Reads and in-memory parsing | Selected stream/content/archive reads are counted. In-memory copy/serialization and transitive library work are not fully attributed. Use a complete enclosing duration and memory boundary; parse counts alone cannot forecast cost. |
| Metadata and inventories | Exists, attributes, link checks, enumeration, path resolution and length queries lack a complete operation series. Visited entries and sampled sizes are not transferred bytes. |
| Writer leases | Python `writer_lease` ignores `CloseHandle`'s return value. Add checked release and opt-in lifecycle observations, preserving a body exception when release also fails. Native ownership probes intentionally expect sharing/lock rejection; classify that separately from failed ownership. |
| Pending and managed files | Selected pending-file lifetimes and exclusive managed directories are tracked. Unregistered files and failed length observations prevent complete storage claims. |
| External and transient scratch | Files can grow and disappear between scans. Maximum sampled size is not a true high-water mark. Declare retained, pending, lease, log, scratch and runtime-cache domains, with confinement or defensible external bounds. Redirecting TEMP alone is insufficient. |
| Bootstrap and terminal work | Initial authentication precedes application accounting. Final receipts exclude their own persistence. The enclosing owner observes child tails but overlaps nested job/application totals. The outer monitor's preparation and final persistence, verification, console and exit remain outside that owner boundary. |
| Live history and full pipeline | No live-history rescan or complete scientific owner pipeline has been executed. Literal fixtures establish only the behavior they exercise. |

The prospective model is a design draft. Elapsed time would use one enclosing monotonic interval beginning before authentication and ending after driver publication, verification, console and exit, with separately bounded outer-monitor work and cleanup. Memory needs an explicit process/job commit high-water boundary and monitor treatment. Storage means logical retained-plus-scratch file bytes over complete declared lifetimes, not allocated disk blocks. Kernel transfer totals and application/raw/decoded/parser units remain separate diagnostics. Unknown observations, missing membership, process escape, unresolved scratch or failed release prevent qualification; missing observations never mean zero. Failed admitted attempts remain charged and historical totals remain immutable.

No new limit, coefficient or qualified forecast is supplied. Before any measurement, implement checked leases, establish complete path/scratch ownership or conservative bounds, bound the outer monitor, and authenticate the full owner/native audit/independent audit/publication path. Any replacement resource protocol needs its own justification and authorization. This draft does not admit a run.

The [verification package](../TestResults/loadout-placement-remaining-reads-verification-20260924) contains source snapshots, the inventory generator, logs, TRX results, literal native fixtures, process observations and immutable runtime captures. The [handoff](../TestResults/loadout-placement-remaining-reads-handoff-20260924.json) records their hashes and continuation constraints. Verification passed **202 backend tests and 296 Python tests**, with no skips:

- Nine new backend cases cover returned-byte lengths, missing paths, directory/locked-file errors, handle release, and valid/altered literal reservation evidence. The previous 193 backend cases also pass.
- Thirteen new Python cases cover codecs, BOM/newline behavior, default locale, ordinary JSON semantics, read/decode/parse failures, nested scopes, unknown failed-read progress and the four actual owner result-read paths. That owner fixture routes scientific workers and labels its process results synthetic.
- All 280 preceding Python regressions pass using the fresh runtime and authenticated exchanges. Three real native process cases pass: complete production audit, rejection of changed captured content, and rejection of a resealed false input identity.

The fresh full native audit counts one completed 65,536-byte entropy read and reconstructs 12 roots, 24 trajectories, 12 catalogues, 12 physical held-out members, 15,744 trial bindings and one endpoint. It uses real input identities and fabricated draw outcomes. The native audit itself has no injected audit callback. The success fixture still covers 12 physical held-out members rather than the maximum 36-member layout. Fixture construction and mutation-copy preparation remain outside the enclosing audit-owner observation.

Commands run, with exact export paths and pins retained in `commands.json`, `runtime-inputs.json` and `test-runs.json`:

```text
./build/run-tests.ps1 -ArtifactsPath .artifacts/loadout-remaining-reads-20260924 -Filter 'FullyQualifiedName~BalanceHarnessRemainingReadTests|FullyQualifiedName~BalanceHarnessNativeAuditSuccessTests|FullyQualifiedName~BalanceHarnessReceiptPublicationTests|FullyQualifiedName~BalanceHarnessWorkerReceiptTests|FullyQualifiedName~BalanceHarnessWorkAccountingTests|FullyQualifiedName~BalanceHarnessWriteAccountingTests|FullyQualifiedName~BalanceHarnessFileAccountingTests|FullyQualifiedName~BalanceHarnessContentAccountingTests'
python -B -X utf8 build/test-proposal-remaining-reads.py -v
python -B -X utf8 TestResults/loadout-placement-remaining-reads-verification-20260924/run-verification.py
python -B -X utf8 build/test-proposal-native-audit-success.py -v
git diff --check -- <changed files>
```

The required backend runner used authorized configuration access and completed with 45 warnings and zero errors. No required command remains blocked. One evidence-packaging attempt included the new outer exchange manifest in its own file list, causing validation to fail before tests used it. That invalid package is preserved. A fresh copy of its authenticated case files has a valid manifest; all final cross-language tests use the corrected copy. Correctness-test durations are not resource qualification samples.

Changed files comprise the two native production files, two Python production files, two new test files, this report, the boundary inventory, scoped LF attributes, and only line 3 of the nine existing status documents. Historical document bodies, sealed packages and captured runtimes remain unchanged.

Recorded cumulative charges remain **79,339.66095319996 seconds** and **51,980,910,910 bytes**; declared maxima remain **168,240 seconds** and **107,122,524,160 bytes**. The first failed pair stays failed. The existing audit floor remains 1,806 seconds against a 1,800-second limit. No scientific launch, qualification, cost experiment, timing pair, live-history rescan, production entropy draw, scientific reservation, encounter preparation or combat occurred. Literal `CreateInput` materialization did occur. No compression speedup, search improvement or current qualified forecast is established.

Next implement checked Python lease release with original-error preservation and scoped owner/native metadata and lease lifecycle observations. Then resolve scratch and enclosing-boundary requirements. There are no migrations, application configuration changes or deployments. Future diagnostic packages must bind the updated Python sources and rebuilt native producer; historical retained runtimes must not be replaced.
