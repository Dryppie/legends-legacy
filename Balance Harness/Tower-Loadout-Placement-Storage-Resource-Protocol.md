# Placement evidence-storage resource methodology

24 September 2026. Target: the offline `LL/tools/BalanceHarness` placement studies.

The prospective accounting contract is defined and its necessary gate is implemented and independently replayable. **Measurement remains blocked: the preserved audit/publication floor rounds to 1,806 seconds against the unchanged 1,800-second cap.** Storage integration establishes correctness; it does not supply a current-format cost forecast. No timing pair, owned fixture, native preparation, runtime qualification or scientific work ran in this stage.

The [frozen protocol](Tower-Loadout-Placement-Storage-Resource-Protocol.json) defines complete accounting, comparability, evidence reconciliation and the rule that must precede any measurement. Its SHA-256 is `9527ac21c60426cdfac769602860744f5f368c25b2ea6ea4f72103ccf68ded4e`. It preserves the previous storage design and recovery protocol rather than amending their historical contents.

## Necessary gate and historical reconciliation

The [read-only registration](../TestResults/loadout-placement-storage-resource-protocol-20260924) authenticates all 251 inherited historical pins and all 57 current source hashes. It retains the prior recovery package, reproduces that package with its own authenticated implementation and inputs, and then applies the prospective rule:

```text
For every time/storage partition:
    necessary floor = ceil(max(authenticated recovery floor,
                               any separately justified larger floor))
    require necessary floor < unchanged partition cap
```

No compressed-byte ratio enters this calculation. Unknown additional costs are not observations of zero; these numbers remain necessary lower planning bounds, with the missing work listed separately. Passing a necessary bound would still provide no measurement authorization under this version.

| Partition | Preserved necessary floor | Unchanged cap | Result |
| --- | ---: | ---: | --- |
| Native time | 8,020 seconds | 9,000 seconds | Below cap |
| Both audits and publication | **1,806 seconds** | **1,800 seconds** | **Fails** |
| Native retained storage | 5,347,633,246 bytes | 5,905,580,032 bytes | Below cap |
| Audit/publication retained storage | 34,989,702 bytes | 536,870,912 bytes | Below cap |

The unrounded 1,805.080522-second audit bound reconciles to 445.000000 seconds for the probe's whole-worker and independent-audit terms after margin, 1,240.080522 seconds for its scaled additional inventories after margin, and the unchanged 120-second publication reserve. These are terms of the frozen planning formula. They are not a decomposition of measured current compressed performance.

The full failed original pair remains failed and charged **24,420 seconds / 13,019,119,616 bytes**, including its unstarted candidate. Its 17,280-row native receipt remains an incomplete-study denominator ceiling in the old calculation; it is not a completed plain baseline. The completed 18,816-row placement archive remains separate. The completed 16,256-fight pilot, final closeouts, inherited floors, scaling to the 21,888-fight ceiling, margin two and publication reserve all remain binding.

The old probe combines generation, reconstruction and archive scanning. Its inventory extrapolation does not identify a current-format time coefficient. Dropping the whole worker, multiplying the total by a compression ratio, or pairing a newly compressed archive with a historical incomplete one would not justify reducing the floor. A later amendment must identify exactly which term it replaces and provide a comparable, prospectively justified replacement while bounding the retained terms. This version deliberately cannot bootstrap that evidence by running a blocked pair.

## Complete accounting contract

The protocol fixes four contiguous enclosing phases: native work; native audit; independent audit; and publication. They include controller work, startup, receipt retention, hashing, cleanup and process drain. The last three share the existing audit partition. Diagnostic subphase timings overlap their parents and must not be added again.

Stored bytes, application reads and logical processing are different quantities:

- Retained storage counts each physical path once, including payloads, indexes, manifests, sources/runtime, receipts, logs and partial files. Equal-content files remain separate. Overlapping manifests do not multiply retained size.
- Application-read bytes count every returned byte across repeated hashing, scanning and payload reads. These counters do not measure physical-device traffic or cache misses. Hash/parse/decode purposes can overlap and cannot be summed as independent I/O.
- Logical bytes preserve every complete representation, including duplicate pair/search observations, ordinary JSON, catalogues and journals. Decoded and parsed bytes count repeated and partial passes separately, by process and phase.
- Reconstruction counters cover complete trajectories, catalogue construction/verification, held-out observations, input/cache-key/report checks and endpoints. Compression does not remove these duties.
- Scratch accounting covers concurrent retained-plus-scratch high water and failed/deleted temporary output. An endpoint directory inventory cannot establish a scratch peak.
- Memory accounting needs a documented peak process-tree and owner aggregation, plus a justified bound fixed before any future registration. Small decompressor buffers do not bound a materialized JSON document or its object graph.

The pinned synthetic proof contains **1,030,288,532 logical bytes** in its 60 encoded members. One native reconstruction decoded **2,060,577,064 bytes in 120 passes**. Both current readers perform two passes per encoded read, so one complete audit in each language requires at least **4,121,154,128 decoded bytes** for those members alone. This is an exact fixture work count and a reader-contract implication, not a performance sample, full lifecycle receipt or scientific workload bound.

Source inspection locates the remaining collection gaps:

| Current source | Existing behavior and missing coverage |
| --- | --- |
| [`TowerProposalEvidenceStorage.cs:108`](../LL/tools/BalanceHarness/TowerProposalEvidenceStorage.cs#L108) | Aggregates codec payload reads/decoding. Constructor manifest checks at line 124, index hashes and final metadata checks are outside those counters. |
| [`proposal_evidence_storage.py:17`](analysis/proposal_evidence_storage.py#L17) | Independent codec counters likewise omit the `authenticate` callback, metadata and direct hashing. |
| [`audit-proposal-affinity-study.py:78`](analysis/audit-proposal-affinity-study.py#L78) | Ordinary JSON parsing, manifest scans, trial journals and complete reconstruction lack one per-phase work receipt. |
| [`bounded_windows_process.py:157`](../build/bounded_windows_process.py#L157) | Records owned process completion, elapsed time and cleanup. Its returned receipt does not establish the required memory or scratch peak. |
| [`run-proposal-affinity-study.py:300`](../build/run-proposal-affinity-study.py#L300) | Takes the final timestamp before writing `closeout.json` and before owner cleanup. A later enclosing collector must include that tail and distinguish its own separately bounded bookkeeping. |

A missing metric remains unknown. Coverage declarations must identify the collector and its boundary for each phase; successful codec counters cannot stand in for complete process accounting. Partial counters and outputs must survive failures.

## A future matched comparison

The comparison unit is the **same complete placement study** emitted by the same current implementation once as plain JSON and once with explicit gzip selection. It compares encodings, not the former common-v5 and placement policies. Runtime, source/PDB identities, owner/auditors, settings/content, ten actors/five slots, twelve roots, literal schedules, evaluator, request order, complete logical hashes/counts, bounds and phase boundaries must be fixed before registration.

The declared order is plain then compressed, one attempt each, no warmups, retries or cache manipulation. That order has possible cache/order confounding; one pair cannot establish a causal speedup. Every eligible decoded byte, catalogue, trajectory, freeze, attempt, provenance binding, observation and reconstructed result must agree. Only the specified representation, physical bindings and preregistered output metadata may differ. There is no per-report, per-actor, per-slot or post-timing normalization.

Both complete owned lifecycles, their final closeouts and publication must succeed. A failure retains both declared allowances and stops the pair without an alternative output or replacement denominator. The 18,816-row correctness fixture also does not cover every possible 21,888-fight outcome; a later projection must justify its full workload coverage separately. No numerical current-format forecast, expected speedup, memory cap or scratch requirement has been invented here.

**This is a comparability contract, not a registered cost experiment.** The failed necessary gate forbids running the described pair under v1.

## Verification, accounting and next implementation

The new Python helper provides only `register` and `verify` for the read-only gate. It authenticates the retained recovery implementation before importing it, rejects changed packages, unsafe paths and duplicate JSON keys, and has no fixture/codec/process launcher. The sealed registration manifest SHA-256 is **`6ec042f35b55606582f41f9085a36e6e7035b68158318200f7b6200e2c931034`**. Running the retained helper against that external pin reproduces the assessment exactly without consulting mutable working sources.

**22 Python tests pass**, including 100 deterministic monotonicity cases, strict rounding at the cap, floor retention, unchanged resources, missing accounting categories, forbidden normalization/discounts, repeated decoding, evidence tampering and replacement-output rejection. An initial test mistakenly treated 1,801 seconds as larger than the retained audit floor; its corrected input now exceeds both the cap and floor. The initial failure and final passing log remain in the [verification package](../TestResults/loadout-placement-storage-resource-verification-20260924).

```text
python -B -X utf8 "Balance Harness/analysis/test-loadout-placement-storage-resource-protocol.py" -v
python -B -X utf8 "Balance Harness/analysis/loadout-placement-storage-resource-protocol.py" register --output TestResults/loadout-placement-storage-resource-protocol-20260924
python -B -X utf8 TestResults/loadout-placement-storage-resource-protocol-20260924/helper.py verify --output TestResults/loadout-placement-storage-resource-protocol-20260924 --expected-manifest-sha256 6ec042f35b55606582f41f9085a36e6e7035b68158318200f7b6200e2c931034
```

Registration is single-use and is already complete; only the verification command may be replayed against this sealed package. Mutation tests use fresh temporary directories and never alter sealed studies. The read-only gate charged its full predeclared **180 seconds / 67,108,864 bytes** at start. It finished assessment in about 1.31 seconds before sealing; that diagnostic duration is not a study timing sample. Historical charges are unchanged. Cumulative recorded accounting is now **79,339.66095319996 seconds / 51,980,910,910 bytes**; cumulative declared maxima are **168,240 seconds / 107,122,524,160 bytes**. Implementation tests remain outside these experiment-accounting totals, as in earlier stages.

Changed files are the protocol JSON, this report, its Python gate and tests, LF attributes, and only the current-status line in nine existing documents. Backend, owner, codec, auditor and captured runtime code are unchanged, so backend/integration tests were not rerun. Their previous 92 distinct backend and 66 Python results remain pinned. All required commands completed; no migrations, service configuration changes, deployments, live-history scans, entropy, scientific reservations or combat occurred.

The next engineering step is **complete work-accounting collectors and retained receipts**, verified with fresh deterministic synthetic success/failure tests. Cover the omitted manifest/metadata reads, repeated parsing/reconstruction, owner/process memory, scratch lifetime and publication tail at the boundaries above. Preserve both launch guards at `TowerProposalStudyProtocol.cs:146` and `run-proposal-affinity-study.py:113`. Collector correctness alone will not reopen the gate: a separate justified replacement resource model is still required before timing or qualification.
