# World Tower mechanic diagnostic: prospective execution scope

**Measure Seal duration and pulse exposure, pillar end reasons, and Resonance lock timing on the two unchanged anchors and the measured challenger. First require exact report parity across the old runtime, the new runtime without observation, and the new runtime with observation.** This is one fixed diagnostic panel, not another search or a confirmation extension. It cannot promote a team or reverse the closed pilot's `ImprovementNotDemonstrated` result.

The [telemetry implementation](Tower-Mechanic-Telemetry-Implementation-Review.md) passed 16 synthetic checks. Full combat integration has not been exercised. Because both `Services.LL.dll` and `BalanceHarness.dll` changed, an observer-on/off comparison alone would miss a change shared by both new paths. The retained pre-telemetry runtime supplies the third parity arm.

## Fixed inputs

Use floor-5 Kharad, the pilot's captured content/settings, ten level-40 characters, five Essences per character, existing subgroups, neutral identities and fixed equipment. Keep ordinal, outcome-independent Essence/ability order. Preserve the 85-Essence /82-family pool and `OwnedCopies=null` inventory assumption. No recipe, order, content or rule changes are allowed in this scope.

| Panel member | Exact saved scenario |
| --- | --- |
| Anchor 040e | [cell-cc1f4a8c1bea7fde79d5cc317c2da802858b5260619d0069ea2684fe87200730](../TestResults/balance/tower-incumbent-practical-pilot-20260916/study/exports/cell-cc1f4a8c1bea7fde79d5cc317c2da802858b5260619d0069ea2684fe87200730.json) |
| Anchor 49f6 | [cell-f7315130ac1ae89b8d5ec0839f9cd35b2e6d2b7508ed234bf02730e74a361f30](../TestResults/balance/tower-incumbent-practical-pilot-20260916/study/exports/cell-f7315130ac1ae89b8d5ec0839f9cd35b2e6d2b7508ed234bf02730e74a361f30.json) |
| Measured challenger a0f9 | [cell-50ab9e8905d1d211df904c1100df16fa4680f96022a62ad1879234a74e0d19ec](../TestResults/balance/tower-incumbent-practical-pilot-20260916/study/exports/cell-50ab9e8905d1d211df904c1100df16fa4680f96022a62ad1879234a74e0d19ec.json) |

The saved scenario bytes and party definitions are pinned in the [proposal](../TestResults/balance/tower-mechanic-diagnostic-planning-20260916/proposal.json). Readiness may substitute only the prospective diagnostic scenario identity and seed panels; all three runtime arms must receive the identical resulting input for each team/value. It must verify native legality and input equality without entering combat. Reuse the complete old runtime in `tower-incumbent-selection-20260916/runtime` and new runtime in `tower-mechanic-telemetry-20260916/runtime`; no production rebuild is requested. Run the two runtimes in separate processes to avoid assembly-resolution mixing.

## Requested fights and values

| Stage | Fixed panel | Maximum executions |
| --- | --- | ---: |
| Parity gate | 3 teams ×4 shared fresh values ×3 runtime modes | 36 |
| Mechanic diagnostic, conditional on passing parity | 3 teams ×12 separate shared fresh values, observed new runtime only | 36 |
| **Total** | No search, historic replay or additional samples | **72** |

Request exactly **16 fresh values**, split into four parity and twelve diagnostic values. No construction root is needed. The panels must be disjoint and absent from the complete **483,640**-value historical exclusion union. Within each panel the same values are shared across the three teams. Repeated execution of each parity input is explicitly counted three times; it is not free verification. There are no archived combat replays.

After execution authorization, pin one master and domain `tower-mechanic-observer-diagnostic-v1` before deriving any candidate. Reuse the existing durable two-panel reservation adapter with counts 4/12 and a fixed 100,000-candidate ceiling per panel. Readiness must use only literal synthetic allocator callbacks. Production reservation writes Pending before derivation and a durable Start/Candidate journal, retaining every returned value and interruption. Reserve once, with no retry or resume. All 16 values stay excluded even if parity fails before the second panel runs.

Use the latest pilot's `binding/seed-ledger.json` as the complete authoritative ledger. Perform recovery-aware production `Refresh`/`Recheck` over the entire live registry, retain all old required history files and the original explicit Pending/recovery mapping, and require exactly 483,640 exclusions before allocation. Recheck membership and bytes immediately before derivation. A known-file hash check alone is insufficient. Never reuse the older adapter's 483,343 count or authoritative-ledger path unchanged.

## Gates and interpretation

Readiness must bind this protocol/proposal, both runtime inventories, exact content/settings/recipes, collector filters/cap, frozen order, counters and limits. Add isolated launcher tests for the three-mode schedule, authorization mismatch, once-only launch, durable attempt/completion bounds, parity failure, truncated/incomplete traces and saved-evidence reconstruction. Reuse the 16 telemetry tests while their pins match. Run new backend checks through `build/run-tests.ps1`; no combat is allowed in readiness.

The parity gate requires equality of the complete ordinary compact battle report across all three modes for every parity input, using identical serializer settings and canonical JSON hashes. Do not strip fields, round values or ignore a mismatch. Also verify serialized ordinary-report bytes under the same compact serializer. Old and new execution identities intentionally differ and are recorded externally; input content and ordinary report semantics must agree. Stop on the first detected mismatch and preserve evidence. A finite passing panel establishes sampled parity only.

Use the existing Seal barrier/status/pillar-group filters with a prospectively fixed cap of **65,536 events per fight**. Require complete, untruncated traces; do not rerun at a larger cap. Check event order, barrier activation/application IDs, spawn/member/group relationships, scheduled versus observed group resolution, terminal reasons, and stack bounds/lock state. Reconcile pillar spawn/kill/non-kill-end totals with the saved ordinary report where represented. A failed reconciliation is an integrity failure, not a mechanic finding.

Publish per team and per fight, with the parity and diagnostic panels explicitly labeled:

- Seal starts, breaks, timeouts and unresolved starts at combat end; completed barrier durations and censored durations separately.
- Linked target application attempts and distinct activation/effect/tick pulse waves. Neither count is damage dealt.
- Pillar spawns, kills, timed expiry, owner-death cleanup, unresolved members, and group resolutions with zero/one/two survivors.
- First Resonance lock tick, whether lock occurs, and time spent at each stack count. Same-tick transitions add no duration; time after combat ends is excluded.
- Descriptive outcome and survival context. Never treat individual pulses or summons as independent experimental samples.

All 16 observed fights per team may contribute to labeled mechanic summaries after parity passes. Do not pool their outcomes with previous confirmation, calculate a new superiority decision, rank a winner, alter a recipe, or select more values from the observed results. These small samples can identify a concrete timing problem worth investigating; they cannot validate a stronger team or identify causal Essence contributions.

Run the fixed panel once. Stop on input/identity/history changes, parity mismatch, invalid/truncated telemetry, unmatched counters, cancellation or any time/storage ceiling. No combat retry, resume, optional extension, automatic sample increase or renamed follow-up is authorized. If evidence is unusable, close as `DiagnosticInvalid`; if complete, close as `DescriptiveDiagnosticComplete`. A later team hypothesis requires a separate prospective decision.

## Resource envelope and approval

The prior [completion receipt](../TestResults/balance/tower-mechanic-telemetry-20260916/completion.json) leaves **2,447.3106077828224 diagnostic seconds /303,116,887 bytes overall**, including **114.91688013849489 engineering seconds /57,180,698 engineering bytes**. This planning scope is capped and charged at **10 seconds /1 MiB** to engineering; its [receipt](../TestResults/balance/tower-mechanic-diagnostic-planning-20260916/completion.json) carries the balance forward.

| Proposed work | Time ceiling | New-output ceiling |
| --- | ---: | ---: |
| Focused launcher readiness, including bounded corrections and failed attempts | 90 seconds | 24 MiB |
| Fresh binding and one parity/diagnostic run, including cleanup | 180 seconds | 64 MiB |
| Independent saved-evidence verification and publication | 60 seconds | 16 MiB |

No transfer or cap increase is needed. Component caps remain engineering **1,225 seconds /624 MiB**, run **2,075 seconds /368 MiB**, audit **300 seconds /32 MiB**; overall **7,980 seconds /5,804,916,736 bytes**. Pin verified runtime/content in their existing sealed locations rather than copying them into readiness. Count transient/overwritten output and reserve publication/cleanup space inside each ceiling. Prior measurements are context, not a prediction of diagnostic cost. Spare capacity cannot be moved between phases after outcomes.

**Approval requested:** the bounded readiness work, then one allocation of 16 fresh values and at most 72 combat executions only after every gate passes, followed by the fixed saved-evidence audit. The completed telemetry scope permitted zero combat and zero fresh values; this document makes the new numerical allowance concrete. No master/value derivation, readiness build/test, native preparation, combat or replay occurred in this planning step.

Planning changes are this protocol, its non-executable proposal/preservation/accounting package, and README/five current handoff notices. Static checks verify the three saved recipes, unchanged sealed packages and source/runtime pins, ledger cardinality, budget arithmetic, document links, unrelated dirty-file preservation and scoped whitespace. Full live registry reconciliation and execution remain readiness work; they are not claimed complete here. There are no production changes, migrations, persistent configuration changes or deployments. The pilot remains **Closed**, adoption **Hold**, V19 **Unresolved**, and its 253 required recipes/512 unused confirmation values remain preserved.
