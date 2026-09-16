# Supplied-composition comparison: concrete execution proposal

16 September 2026. Target: offline `LL/tools/BalanceHarness`. **Prepared proposal; not an executable-ready or authorized allocation receipt.** The user asked to proceed after implementation. That authorizes preparing the comparison; the original instruction against increasing old caps still requires a specific budget exception. No old study is reopened.

## Question and target

Compare `retained-composition` and `supplied-block` under `supplied-composition-block-v1`, giving both exactly the same two starting compositions, current legal Essence pool, equipment, fixed ordinal order, discovery panels and evaluation budget. The target is **current gameplay/content at the subsequently pinned build**, not the captured V19 balance candidate. Historical strengths are not imported as current measurements.

Start from the two recipes in `TestResults/balance/tower-deep-challenger-study-20260915/definition.json`: `team-040e60d3dbc5c127321653c47ed3a9d3` and `team-49f6979895354870c89362d4abf214bb`. Preserve their recipe ancestry/evidence hashes. Use floor 5, ten characters in two fixed five-character subgroups, five level-1 unascended/unevolved Essences per level-40 character, tier 1/rank 2, Standard Essence quality, fixed existing Uncommon equipment and neutral character identities. Canonicalize Essence order ordinally. Repetition across characters remains legal; families remain distinct within each character. Copy inventory is unrestricted in this experimental cohort; it is not a claim about a particular player's possessions or acquisition costs.

Before allocation, native current-runtime materialization must admit both anchors, match gear/identity/family legality and validate every content/settings/executable hash. Static JSON admission is useful preparation but cannot substitute for this check. If either start fails, stop; do not silently repair it or fall back to another recipe. Freeze a clean snapshot of the chosen dirty-checkout source while preserving all unrelated work. Do not change gameplay to make admission succeed.

## Fixed workload

Three independent construction roots, with disjoint discovery/selection/confirmation panels across roots. Within a root, both methods share all starting information and combat panels. Each root has one generation label, eight discovery seeds, 32 selection seeds and 256 confirmation seeds: **297 values/root, 891 fresh values total** (three construction labels plus 888 combat seeds). No values are derived or reserved during this planning task. All **483,046** prior reservations, including Pending/failed allocations and V19's 512 unused values, remain excluded. Successful allocation would raise the reservation count to **483,937**; uncertain partial allocations must remain excluded.

| Stage | Maximum schedule | Fight cap |
| --- | --- | ---: |
| Discovery | 2 methods × 3 roots × 64 complete parties × 8 trials | 3,072 |
| Selection | 4 nominees × 2 methods × 3 roots × 32 trials | 768 |
| Confirmation, only if the futility gate passes | 3 roots × (2 method finalists + 2 anchors) × 256 trials | 3,072 |
| Total | No combat retries, replays or optional extensions | **6,912** |

Each method/root has a **256 emitted-proposal cap** in addition to its 64-party cap. Include supplied-start evaluations in that budget. Initial construction is two starts plus six fresh opportunities; every fourth subsequent opportunity is fresh. Duplicate/rejected proposals consume their opportunities; there is no refill beyond the cap. Discovery charges both arms' scheduled evaluations even for common recipes. Selection/confirmation may merge exact same-context recipes within one root, retaining every origin; saved fights do not fund extra proposals or samples. Never merge outcomes across roots.

Require all six discovery arms to complete before selection. Nominate independently for every method/root: the top two by frozen discovery rank, then two distinct representatives from the deterministic composition-diversity retention rule, with ranked fallback only if necessary. Freeze the complete nomination map before reading selection outcomes. The generic combined study shortlist is not sufficient for this comparison.

For each method/root, select one winner using the implemented rule: more selection wins; only zero-win ties use lower arithmetic mean guardian health; exact ties use frozen discovery rank then ordinal recipe ID. Positive-win ties keep the discovery-rank/ID tie-break. The complete selection matrix is required, and confirmation never affects selection.

## Stop and promotion rules

After all selection finishes, stop **before all confirmation** unless at least two of the three block-search roots select a recipe different from both admitted anchors with at least one selection win. This futility rule is an engineering screen with false-negative risk. On a stop, record a complete discovery/selection result and unrun confirmation, without inventing confirmation outcomes.

If the gate passes, confirm the entire frozen family in all roots. Promotion requires at least two roots in which the block finalist both (a) has an adjusted lower viability bound of at least 10%, and (b) improves observed win rate by at least five percentage points with adjusted paired lower bound above zero versus the comparator **and each anchor**. Preallocate two-sided family error .025 to the twelve rate cells and .025 to the nine paired contrasts, using the existing Wilson/discordance implementation only after verifying its parameter semantics with fixtures. Merged recipes retain all conceptual comparisons; identical-recipe contrasts are zero and cannot establish improvement. Report every root and contrast. Health is descriptive, not a substitute endpoint.

If validation fails, discovery exhausts its cap, the futility gate fails, or confirmation fails the promotion gate, stop this proposal without another ratio/seed/archive-size variant. An inconclusive result is no promotion under this budget. Keep any unusually strong team and report its separate balance implication; do not discard evidence because it exceeds 50% wins. Adoption remains Hold until evidence justifies a separately reviewed change.

## Required engineering before any allocation

The kernel is tested; the new multi-root comparison controller is not implemented. The older refinement controller is hard-bound to one root, 45 values and 288 fights. Comparison v5 changes its selector only and must not be used as a shortcut to this study.

Implement a separately versioned model/runner that composes existing compact discovery and balance campaigns, reconstructs per-arm nominations, applies the global futility gate, computes the complete confirmation family and verifies all records from saved evidence. Reuse durable battle charging, Job Object time limits, storage guards and immutable source/content/executable identity. Add a dedicated reservation/launch binding for the exact 891-value/6,912-fight contract without relaxing older version limits. New request, authorization, seed and result hashes must bind that version.

Verify through `build/run-tests.ps1` with explicit synthetic-only filters. Cover per-arm/root quota isolation; separated seed matrices; equal starting information; deterministic nomination ties; shared-recipe origins; global futility before any confirmation; all-roots confirmation after passing; adjusted intervals; exact charging; cancellation; content/version mismatch before allocation; partial reservations; saved reconstruction; and compact-health extraction from fabricated compact records. Reuse the existing 35 passing mechanics/selection cases where source and binaries match; rerun affected cases when source changes. Also strengthen the integrated deceptive-family fixture: the earlier equal-budget toy already contained the optimum in its initial batch, so it is not evidence of scheduler escape.

Build the current gameplay dependencies as well as the harness, rather than treating archived gameplay DLLs as proof of current parity. Freeze sources, build configuration, binaries, content, effective settings, histories and both admitted anchors before binding. Preparation/verification performs zero combat and zero fresh allocation. Permit at most two engineering compile/test attempts per affected phase, retaining every failure, within the fixed task cap; this does not permit changing a negative toy-strength result into a passing one. Combat/binding execute once only after every readiness check passes. If implementation cannot satisfy the contract within budget, stop without allocating or fighting.

## Concrete budget request

The latest [implementation receipt](../TestResults/balance/tower-supplied-composition-20260916/completion.json) leaves **278.50172764434956 seconds and 751,528 bytes** before this planning task. A single retained harness compilation in that package added approximately 3.69 MB, already more than the remaining output allowance. This comparison cannot be prepared and run under the old limits.

Request an additional **3,600 diagnostic seconds (60 minutes) and 1 GiB (1,073,741,824 bytes)**, plus the exact **891 fresh values** above. The proposed new cumulative ceilings are **7,980 seconds** and **5,804,916,736 bytes**. Approval would grant a maximum, not require consuming it. No cap of any sealed historical study changes.

| New-work component | Time ceiling | Output ceiling |
| --- | ---: | ---: |
| Controller implementation verification, current-runtime builds, native admission, history/preflight | 600 seconds | 224 MiB |
| One binding/discovery/selection/conditional-confirmation run and reconstruction | 2,700 seconds | 768 MiB |
| Independent final audit, failures, publication and closure | 300 seconds | 32 MiB |
| Total | **3,600 seconds** | **1,024 MiB** |

For scale only, the prior 280-fight comparison used 65.5471427 native seconds: linear extrapolation to 6,912 fights is about **1,618 seconds (27 minutes)**. Its 85,113,082 study bytes included a 73.04 MiB counted component minimum; a rough split of three such fixed components plus scaled variable output is approximately **420 MiB**, before new-runtime/build outputs. These are estimates, not measured current-content performance or guarantees. Before allocation, count exact fixed archive components and refuse launch if the bounded remaining variable allowance is inadequate. Changed current content can alter durations and report sizes. No performance benchmark is needed to justify a guaranteed runtime claim, because no such claim is made.

## Work performed in this planning task

Only source/evidence inspection, static JSON compatibility and arithmetic checks, and these planning artifacts. A new planning package records input hashes, recipe-level findings and a bounded resource receipt. Its scope is at most 20 charged seconds and 64 KiB, from the remaining old allowance. It invokes no harness, build, backend test, preparation service, allocator, combat or replay. No migrations, configuration or deployment changes.
