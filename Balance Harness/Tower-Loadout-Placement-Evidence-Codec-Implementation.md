# Opt-in proposal evidence codec implementation

**Status: the native codec and independent Python decoder are implemented as isolated modules.** The current study writer, requests and auditors still use their existing paths. The next integration stage must bind the format to the request, publish and authenticate the indexes, and reconstruct complete synthetic studies through both auditors before any resource experiment is considered.

This implements the first step of the [frozen storage design](Tower-Loadout-Placement-Evidence-Storage-Design.json). The [inventory](Tower-Loadout-Placement-Storage-Inventory.md) remains unchanged. No retained study archive was rewritten or compressed, and no study cost measurement or admission took place. The earlier recovery protocol still fails its necessary audit gate: **1,806 seconds against 1,800 seconds**.

## Implemented boundary

`LL/tools/BalanceHarness/TowerProposalEvidenceCodec.cs` writes the existing serializer's exact UTF-8 bytes through SHA-256 counting streams and a gzip encoder. Callers must supply the explicit `tower-proposal-json-evidence-gzip-v1` version, positive trusted logical/physical byte limits and a physical-byte charging callback. Only the three eligible logical filename families are accepted. Writes use create-new semantics, charge before writing and flush the complete payload before returning its descriptor. Failures retain any partial file and return no successful descriptor.

Descriptors bind the logical name to its `.gz` physical member and record both lengths and digests. `ValidateEntries` compares entries with caller-derived expected membership and order, checks member and aggregate byte limits, and rejects duplicates. It does not derive the expected members from the index itself. Directory/ancestor link checks and plain/compressed collision checks apply before access.

Both readers require an authenticated descriptor from the future owning manifest layer. They check stored bytes, a single gzip member, trailer integrity, strict UTF-8, decoded length and digest, and duplicate-free JSON. They reject truncated streams, extra members, trailing data, altered descriptors, unsupported versions and limits derived solely from an untrusted descriptor. The native parser uses the existing serializer options with duplicate properties disallowed; it does not mutate `HarnessJson.Options`.

`analysis/proposal_evidence_codec.py` implements the decoder independently using Python's standard `zlib` and JSON libraries. It does not call the C# codec. Both readers authenticate the full decoded content before parsing it and recheck physical content before returning a result.

The native reader performs two decoding passes. The first locates the final compressed input window; the second feeds that window bytewise to identify the exact end of DEFLATE despite read-ahead. This rejects bytes that a permissive gzip reader could otherwise ignore. Both passes count toward `DecodedBytesProcessed`; all physical reads, including repeated hashing, count toward `PhysicalBytesRead`. Python likewise records both authentication and parsing passes.

The modules stream compression/decompression with bounded working buffers and do not stage an expanded archive. JSON values still consume memory: native callers materialize the requested object graph, and Python's standard parser also materializes a decoded Unicode document. Logical-byte limits constrain accepted input; this is not a measured peak-memory guarantee or evidence of an audit speedup.

## Integration still required

The modules deliberately have no command, default switch, worker launch, reservation or resource-admission path. Existing `HarnessJson`, native study/search writers, the independent study auditor and previously pinned runtime archives are unchanged. A fresh isolated backend test build exercises the new source; it is not a captured or qualified study runtime.

The next implementation must:

1. Add explicit prospective request/root format binding and trusted limits, retaining legacy defaults and rejecting downgrade or mixed-format evidence.
2. Use the codec for complete search reports, paired reports and held-out observation records; publish per-directory indexes before sealing the owning manifests and authenticate exact physical/logical membership.
3. Extend both complete-study auditors and add full synthetic reconstruction tests, preserving complete catalogues, provenance, trajectories, logical identities, attempts and freeze barriers.
4. Record a separate prospective resource methodology accounting for repeated decoded work, physical storage, scratch and memory before any measured fixture. Keep the failed recovery protocol and every inherited floor, margin, cap and charge intact.

Codec round trips and existing plain-archive regressions do not establish new-format complete-study equivalence. No compressed studies have been produced. Savings, runtime costs and a new forecast remain unmeasured.

## Verification and changes

The [verification package](../TestResults/loadout-placement-evidence-codec-verification-20260924) retains backend and Python logs, test results, cross-language synthetic samples, before/after source snapshots and preservation checks. Native tests cover exact serializer bytes, Unicode and number forms, large compressed inputs, read-ahead, malformed gzip with matching physical hashes, decoded limits, partial writes, charging/cancellation, index constraints and unchanged plain/compact JSON behavior. Python tests independently cover those decoding boundaries and read native-produced samples; a Python-produced gzip sample is authenticated by the native tests.

Final verification passed **74 backend tests (40 codec tests and 34 existing proposal-native regressions)** and **20 Python tests**, with no skips. The final native sample-export test also passed separately without rebuilding, and all three exported samples passed the independent Python reader. The verification record retains hashes of the isolated test build; it is not a runtime qualification.

```text
build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarnessEvidenceCodecTests|FullyQualifiedName~BalanceHarnessProposalNativeTests' -ArtifactsPath .artifacts/loadout-evidence-codec-20260924
build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~BalanceHarnessEvidenceCodecTests.Native_samples_can_be_retained_for_the_independent_python_reader' -ArtifactsPath .artifacts/loadout-evidence-codec-20260924
python -B -X utf8 "Balance Harness/analysis/test-proposal-evidence-codec.py" -v
```

For the final export and Python commands, `LL_EVIDENCE_CODEC_EXCHANGE` pointed to `TestResults/loadout-placement-evidence-codec-verification-20260924/native-exchange-final`. This is a test-only output location, not application configuration. No required command remains blocked.

Backend verification uses `build/run-tests.ps1` with an isolated artifacts directory and a filter for `BalanceHarnessEvidenceCodecTests` and existing `BalanceHarnessProposalNativeTests`. The first sandboxed build could not read the user's NuGet configuration. The approved retry used the existing configuration/cache. Unrelated compiler warnings remain; no service configuration was changed.

New source files are the native codec, its xUnit tests, the Python decoder and its tests. This report and nine current-status lines record the implementation; historical document bodies remain unchanged. `.gitattributes` preserves the new source files' line endings. Historical manifests and source bindings are rechecked before sealing the handoff.

No migrations, application configuration changes, deployments, external-environment operations, production entropy, scientific reservations or combat occurred. Synthetic codec and plain-archive tests are engineering verification, not study timing samples; historical resource accounting is unchanged.
