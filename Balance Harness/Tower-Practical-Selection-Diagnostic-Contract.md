# Practical Tower selection diagnostic: proposed execution and audit contract

17 September 2026. Target: the offline BalanceHarness. **Design-step status: the four-nominee freeze, sampling, audit and interruption contracts were specified before implementation.** This closes the design work identified by the [precision assessment](Tower-Practical-Selection-Diagnostic-Design.md); further documentation cannot substitute for implementation evidence or compatible resource measurements.

The [read-only accounting script](analysis/practical-selection-diagnostic-contract.py) produces the [JSON appendix](Tower-Practical-Selection-Diagnostic-Contract.json). The appendix is explicitly not a runnable request: its new time/byte allowances are null. All names and fields below describe a proposed `tower-practical-selection-diagnostic-v1` protocol. No command, sampler, search variant or gameplay change was added. No experiments, native reconstruction or allocation ran.

The subsequent [implementation review](Tower-Practical-Selection-Diagnostic-Implementation-Review.md) records the separate versioned commands, two reservation phases, literal archive audits and process checks. This document and its unchanged JSON/script retain the design-step evidence and resource illustrations. Implementation is now verified within the fixture scope; compatible gameplay resource evidence and a fresh frozen allowance are still missing, so launch remains **NoGo**.

## Fixed scope and source constraints

Retain one episode of `retained-composition-incumbents-v1`, both supplied anchors, the same declared ten-character cohort and legality rules, `OwnedCopies=null`, fixed canonical ability order, 64 evaluated parties, at most 256 proposals, eight discovery trials and 32 selection trials. Freeze the original primary and all four distinct nominees, then evaluate all four on 1,000 new paired conditions. The fight ceiling is **512 + 128 + 4,000 = 4,640**, including every attempted fight. No diagnostics, replay, sample extension, automatic replacement or second search follows.

The source requires a distinct protocol boundary, not a request-field workaround:

| Current source | Observed restriction | Required diagnostic behavior |
| --- | --- | --- |
| [Discovery contract](../LL/tools/BalanceHarness/TowerBossDiscoveryContract.cs#L196) | All stage schedules must already exist; incumbent mode requires `GeneratedFinalists=1` at line 253. | A separately versioned pre-confirmation binding with no confirmation values yet. Keep the existing definition validator strict. Do not insert placeholder schedules or relax the old incumbent contract. |
| [Allocator](../LL/tools/BalanceHarness/TowerPracticalAllocation.cs#L26) and [worker](../LL/tools/BalanceHarness/TowerPracticalSearchRun.cs#L184) | Allocate/register all stages before entering the study. | Preserve the pre-search derivation rules for construction/discovery/selection; make confirmation a later, separately journaled reservation under the same owner and registry lease. |
| [Study state machine](../LL/tools/BalanceHarness/TowerBossStudy.cs#L68) | Freezes shortlist, selection, filtered finalists, then confirms finalists plus references. | Keep discovery, nomination and primary selection unchanged; freeze all four nominees without the alternative-finalist filters. |
| [Primary selection](../LL/tools/BalanceHarness/TowerBossStudyPolicy.cs#L33) | Validates all nominees, chooses a primary, then filters possible alternatives. | Invoke the same primary rule with maximum one, and join its selected ID back to the complete frozen shortlist. Do not obtain membership by raising the maximum. |
| [Practical assessment](../LL/tools/BalanceHarness/TowerPracticalSearch.cs#L26) | Family seven; two or three distinct confirmed cells; recommends from a separate strength gate. | A distinct diagnostic result with family ten and exactly four cells. Existing practical results and recommendations retain their original semantics. |
| [Saved-study verifier](../LL/tools/BalanceHarness/TowerBossStudyArchive.cs#L71) | Reconstructs the existing state machine and archive with the producing runtime. | A diagnostic verifier must reconstruct its own two reservation phases, freeze and four-cell order from retained evidence, without calling the entropy source or combat. |

The smallest implementation would reuse the existing generation, ranking, legality, primary selection, archive and watched-process primitives through explicit phase inputs. Any shared extraction needs old-path parity. There is no need for another optimization algorithm or a general orchestration framework.

## State transitions and durable evidence

One parent holds the registry/output leases throughout; one watched worker owns the run. The prospective request fixes content/settings/runtime identities, complete history inventory, reference hashes, construction master/domain, sample counts, interval family, gates, trial order and all phase resource limits before any work. Its run identity and output path are unique. Unknown schema versions, fields or invalid hashes fail admission. Empty resource fields are invalid in an executable request.

| Transition | Evidence that must be durable before the next action |
| --- | --- |
| Admitted → SearchReserved | Record request, history pins and worker identity; write Pending before derivation. Derive exactly 1 construction, 8 discovery and 32 selection values using the existing stage/domain/ordinal rule and rejection cap. Persist all closed derivation events, the 41-value search binding and complete reservation ledger before any fight. No confirmation derivation occurs here. |
| SearchReserved → NomineesFrozen | Complete exactly 512 discovery and 128 selection fights. Freeze discovery-shortlist and selection-results. Recompute the original primary and write the complete nominee freeze described below. Fewer than four legal distinct recipes or incomplete earlier stages stop the diagnostic. |
| NomineesFrozen → EntropyPending | Persist the freeze hash, existing reservation union, entropy contract and intended byte count. Replace the current history sentinel with Pending while retaining all 41 known reservations. Durably record the sole entropy-start event before requesting bytes. |
| EntropyPending → ConfirmationReserved | Persist the complete entropy artifact and completion event, then deterministic classification and panel binding. Recheck the outside history inventory, persist the complete permanent union, and publish Complete only after all binding dependencies are durable and verified. No fight may occur while entropy/reservation is Pending. |
| ConfirmationReserved → Measured | Perform exactly four cells × 1,000 conditions in frozen order, with a durable Started event before each attempt and Completed only after its report is retained. No cache reuse, repeated attempt, omitted cell or result-based early stop. |
| Measured → Closed | Finish archive publication, producing-runtime reconstruction, a separate arithmetic/integrity audit, result and seed-free exports, within the enclosing limits. A terminal receipt records all phases, attempts, reservations and the complete inventory. Any failed requirement prevents a positive or negative completed diagnostic. |

Stage inputs must not expose confirmation values or outcomes to candidate generation or primary selection. Drawing confirmation only after the nominee freeze makes that separation explicit. This does require a new phase boundary in the implementation; the current `TowerBossDiscoveryDefinition` cannot represent it by itself.

The nominee freeze contains the protocol/request/search-binding hashes; content, settings, execution and cohort identities; discovery/selection artifact hashes; the complete ordered four-member shortlist; exactly one primary ID; and both reference-to-recipe mappings. Each member retains its original ID, nomination ordinal, canonical seed-free scenario and recipe hash, provenance, discovery rank and selection measurement hash. Distinctness is checked on complete recipe/context identity, not merely ID spelling. Equipment, owner slots, subgroup assignments and canonical ability order remain exact. The freeze records **640 completed attempts and zero confirmation attempts** and links the corresponding journal prefix.

An ordered phase-event record links the freeze, entropy start/completion, reservation completion and first confirmation attempt. File timestamps alone are insufficient evidence of that ordering. The auditor checks the recorded order and pinned producer behavior; self-consistent hashes are not external attestation that no unrecorded process existed.

## Concrete sampling proposal

The statistical target is the uniform distribution over signed Int32 conditions absent from the complete historical union and the 41 search reservations **immediately before the entropy batch**. It is conditional on the frozen four recipes, content and runtime. Do not redefine that target after seeing the batch or outcomes.

Use exactly one **8,192-byte batch**, interpreted as **2,048 four-byte words**, obtained in the owned worker after the nominee freeze through [`RandomNumberGenerator.Fill`](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator.fill?view=net-10.0). Microsoft documents cryptographically strong bytes; treating them as independent uniform bits is the explicit engineering sampling assumption, not something a receipt can prove. There is no user-chosen confirmation master, clock-based seed, pre-screening, fallback PRNG or extra batch.

Decode each word with [`ReadInt32LittleEndian`](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives.readint32littleendian?view=net-10.0). This maps the 32-bit patterns bijectively onto the signed range. Do not use modulo, absolute value, sign masking or reject negative values. Scan words in stored order:

1. A value already in history or the 41 reservations is classified `AlreadyReserved`.
2. A repeat of an earlier eligible word in this batch is `DuplicateBatch`.
3. The first 1,000 distinct eligible values are the ordered confirmation panel.
4. Every subsequent distinct eligible value is `ReservedUnused`. Preserve it permanently too.

Record every ordinal and classification, the raw-byte hash, initial history hash and ordered panel hash. The raw artifact is exactly 8,192 bytes, written through a bounded temporary file, flushed and published once. Persist the entropy-complete marker before honoring cancellation. A missing/torn artifact, unmatched entropy-start or publication failure leaves Pending; do not regenerate lost random bytes or infer a completed draw from a file length alone. The RNG must never be called by a verifier.

The fixed batch bounds work and makes the entire exposure auditable. It can create at most **2,089 new reservations**: 41 search values plus 2,048 distinct eligible batch values. A completed panel creates at least 1,041; at most 1,048 new tail values remain unused. Thus the current 484,285 historical exclusions would become at most **486,374**. These are proposed ceilings, not allocations or increases to a previous allowance. Validate room under the existing million-value history limit before the run. All exposed eligible values remain excluded even if the panel is incomplete or the diagnostic fails.

Under the stated independent-uniform-byte model, rejection sampling selects an ordered uniform sample without replacement. The number of rejected draws does not favor particular eligible identities. With `H=484285`, `B=41`, `W=2048`, `N=1000`, a conservative per-position rejection bound is `r=(H+B+W-1)/2^32`. A short batch needs at least `W-N+1=1049` rejected positions, so a union bound gives:

```text
P(batch shortfall) <= 2^W * r^(W-N+1) < 10^-3522
```

There is no additional batch after a shortfall. Combining this failure term with the earlier finite-population coupling penalty preserves a conservatively rounded **81.09% conditional detection lower bound** for a true gain of at least 16 points. The five-point observed gate remains unchanged. Recompute these quantities if the admitted history changes; require the rounded bound to remain at least 80% before execution.

This calculation includes the finite entropy-batch cap, but **does not include an unknown probability of operational failure**. Power concerns a complete prescribed measurement; do not condition on successful completion and then claim the same unconditional chance of a usable run. Recorded bytes establish mapping, exclusions and absence of a recorded redraw. Producer review and operating-system entropy quality remain assumptions, and a statistical randomness test of one tape cannot certify them.

## Audit and result contract

The producing-runtime verifier and independent auditor must agree on these invariants:

| Area | Required check and failure behavior |
| --- | --- |
| Scope and membership | Match request, source/content/runtime hashes, legal recipes, subgroup/canonical ordering and exact anchor identities. Reconstruct all 64 evaluated candidates and the four-member nomination set using saved discovery evidence. Recompute primary selection from all 128 selection trials. Reject substitutions, alias duplicates, omitted anchors and confirmation-driven reselection. |
| Sampling and history | Validate the two reservation phases, all recorded derivations, raw tape length/hash, every classification, exact first-1,000 panel, and all unused-tail exclusions. Reject a second entropy event, dropped word, changed byte order, different panel order, history overlap or unrecorded binding. Never sample during audit. |
| Trial evidence | Require the exact ordered 4,640 attempts/reports, including 4,000 confirmation records, with matching stage, member, condition, scenario/input hashes and content outcome. Use nomination ordinal, then panel ordinal for confirmation order. Every cell has the identical ordered panel. Invalid enum values, duplicates, missing records and unmatched attempts invalidate completion. |
| Rates and contrasts | Count `Victory` as one and other valid completed combat outcomes as zero. A process timeout or missing report is incomplete, not a loss. Compute four Wilson rate intervals and three paired comparisons, each `G = other wins/primary does not`, `L = primary wins/other does not`, with family ten. No normal-Tower replay is part of this audit. |
| Decision | For another nominee, require its Wilson lower rate ≥0.10, `20*(G-L) >= 1000`, and `WilsonLower(G)-WilsonUpper(L) > 0`. Any qualifying nominee yields `SelectionMissDemonstrated`; otherwise a fully valid completed run yields `NoSelectionMissDemonstrated`. An incomplete or invalid run yields no completed diagnostic decision. |
| Publication | Retain every nominee, all three comparisons, raw counts, adjusted bounds, nominal/approximate interval wording, frozen primary, full accounting and source hashes. Export all four seed-free recipes with roles/provenance. Leave both historical anchor recommendations and adoption Hold unchanged. No “best confirmed team” promotion. |

Execution status, integrity status, sampling assumptions, diagnostic decision and encounter-balance assessment are separate fields. The diagnostic does not apply the existing practical strength gate to this panel as an extra endpoint. Its nominal family coverage is approximate, as in the precision design. Auditors must reconstruct the decision from evidence rather than trusting the published booleans or each other's summaries. Neither auditor performs combat.

An execution failure is terminal for this run. No resume, retry, panel refill or replacement run is part of the protocol. Existing declared-input/allocation recovery receipts must reject this new protocol version. A complete retained entropy batch permits deterministic exclusion accounting in a future dedicated recovery auditor, including the unused tail, but never resumption. An unresolved entropy draw remains Pending and blocks other allocation. No existing recovery command is claimed to handle a post-search entropy interruption.

## Resource admission: evidence and remaining gap

The reader revalidates the same **53 package and 1,564 run manifest entries**, exact inventories and hashes. The predecessor's worker process used 99.547 seconds; the separate producing-runtime audit used 10.250 seconds and the independent audit 36.562 seconds. The enclosing phase times include their additional overhead. These are measurements of the completed 1,408-fight workflow, not of the proposed protocol.

Holding admission fixed and scaling worker and separate audit times by `4640/1408` gives this additional illustration:

| Phase | Measured predecessor seconds | Illustrative diagnostic seconds | Historical phase ceiling |
| --- | ---: | ---: | ---: |
| Admission | 19.984 | 19.984 | 120 |
| Worker, including internal verification/publication | 99.578 | 328.155 | 360 |
| Separate producing-runtime and independent audits | 46.937 | 154.679 | 120 |
| Total | 166.547 | 502.818 | 600 |

The audit illustration exceeds its historical phase ceiling even though the total is below 600. This is not a forecast or an upper bound. It shows that a total-time comparison alone misses a possible phase constraint. The [earlier storage illustration](Tower-Practical-Selection-Diagnostic-Design.md#resource-evidence-and-limits) also held non-battle output constant; that assumption cannot cover larger ledgers, four nominees, the new transcript, report serialization or temporary publication files. Final retained bytes are not peak temporary storage.

A future admission record must contain a justified model for each phase, its compatible source/runtime/content/workload measurements, planned worst-case or explicitly qualified margins, and actual cumulative ceilings. It must account for both audit passes, internal verification, archive reads, temporary writes, publication and termination/closeout. Resource measurements cannot be charged to a nonexistent allowance. The old 600-second /256-MiB envelope is fully closed; no phase transfer, larger limit or new budget was granted here.

No existing record measures this protocol, and its implementation does not yet exist. The smallest later engineering evidence is a bounded exercise of the implemented four-cell/4,000-record publication and both-auditor path using literal reports, with process/deadline/storage instrumentation. That could qualify archive/audit overhead; it would **not** measure future combat time or guarantee completion. A full resource decision also needs compatible combat-time evidence or an explicitly accepted bounded operational risk. Any proposed gameplay calibration needs its own frozen purpose, accounting and authorization; it is not silently added to this diagnostic. No performance or gameplay work ran in this design step.

## Implementation acceptance and stop rule

If an engineering scope is later chosen, the minimum verification is meaningful fixture coverage of: unchanged discovery/nomination/primary selection; an incumbent primary; four-member freeze independent of alternative-finalist filters; all three contrast orientations and exact thresholds; negative signed words, historical collisions, duplicates and unused tails; capped shortfall; every interruption boundary around entropy and binding publication; stage-order tampering; incomplete cells and attempts; parent/worker death; and deadline/storage termination. Verifiers must reject an entropy callback and combat callback. Old practical commands must preserve their prior results, and unknown diagnostic versions must fail rather than fall back to the seven-quantity assessor. Such backend checks must use `build/run-tests.ps1`; none ran here.

The proposed diagnostic's scientific value is still limited to finding a large missed nominee in one future pool. No existing outcome establishes that such a miss is likely. **Stop this proposal if the implementation cannot preserve stage separation and exclusions, if its operational cost cannot be justified, or if this narrow question is not useful at that cost.** Do not change sample size, usefulness thresholds or the assumed true effect merely to make admission pass. Do not reopen closed search variants or V19 confirmation.

Design work for this increment is complete. Remaining work is empirical engineering/resource qualification, not another refinement of the conditional power calculation. No implementation or execution is queued by this document.

## Verification and changes

Reproduce the JSON using Python 3 from the checkout:

```text
python -B "Balance Harness/analysis/practical-selection-diagnostic-contract.py"
```

Verification covers syntax, exact JSON reproduction, independent integer/probability/phase arithmetic, 14 inspected source hashes, sealed resource inventories, changed-document links/whitespace and preservation of unrelated dirty files. These are read-only arithmetic and file-integrity checks; they do not execute the proposed sampler or validate its future implementation.

This increment adds this contract, its deterministic accounting reader and JSON appendix, and updates current conclusions in the precision design, hypothesis review, assessment, evaluation design, readiness review, practical guide and README. No backend build/test, benchmark, combat, replay, native reconstruction, entropy draw or seed derivation ran. No required analysis check was blocked. There are no migrations, persistent application configuration changes or deployment implications. All **484,285 existing exclusions**, V19's **512 unused values /253 required recipes /Unresolved**, both recommended anchors and adoption **Hold** remain preserved.
