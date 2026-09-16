# Composition and order sensitivity diagnostic

Frozen design, 15 September 2026. Target: offline BalanceHarness, the unchanged captured-v19 guardian Health/Power +10% content. This scope prepares and verifies a small diagnostic without combat or seed allocation. A separate explicit exception is required before 64 new values are allocated or 384 fights run. No default optimizer, gameplay or previous experiment changes.

## Fixed cases and question

Use exactly the strongest saved control `team-040e60d3dbc5c127321653c47ed3a9d3` and the best confirmed new challenger `team-63357a2e7176e667b6bcf0a7cf380a1e` from the sealed deep study. Their previous 63/256 and 7/256 results select the probe sources only; they are not pooled into new estimates. These are control-derived sensitivity probes, not independent search candidates.

For each composition, retain the original ordered recipe and create ascending-ID and descending-ID orderings within each character's five Essence slots. Preserve every character's membership, owner placement, equipment, attributes, identity IDs, timestamp, floor and preparation state. Ascending and descending are deterministic ordering policies, not rankings of Essence strength. Use one common scenario ID and identical non-gameplay assumptions across all six cases so scenario-derived encounter identities do not differ between factors. This is a new controlled fixture, not an exact replay of either historical scenario.

| Composition | Ordering | Trials after authorization |
| --- | --- | ---: |
| Saved control | Original | 64 |
| Saved control | Ascending Essence ID | 64 |
| Saved control | Descending Essence ID | 64 |
| New challenger | Original | 64 |
| New challenger | Ascending Essence ID | 64 |
| New challenger | Descending Essence ID | 64 |

Total **384 fights**, all on the same 64-value schedule. No extra controls, replays, screening, adaptive selection, repetitions, retries or resumes. Preserve all six results, including zero-win cases. Duplicate recipes or incompatible contexts fail preparation rather than silently changing the matrix.

## Comparisons and interpretation

Freeze five left-minus-right paired contrasts: control minus challenger under original, ascending and descending ordering; ascending minus descending within control; ascending minus descending within challenger. The original-order contrast measures the original recipe gap, including their different orders; it does not isolate composition. The two common-policy contrasts measure the whole-composition gap under those policies. Composition includes all Essence membership and owner placement differences; it does not isolate Bark Golem or any individual ingredient.

Allocate alpha .025 across six rate intervals and .025 across five paired contrasts, with two Wilson discordance bounds per contrast. Reuse the existing Wilson helper with family sizes 12 and 20 respectively. Report observed wins, rates, intervals, gains, losses and paired differences. Intervals spanning zero are unresolved; equal observed outcomes do not demonstrate ordering invariance. This small schedule can resolve only large effects. It cannot establish global optimality, search reliability, formal interaction significance, viability acceptance, acquisition feasibility or optimizer adoption.

Do not select a preferred ordering from these outcomes and count it as independent confirmation. Do not feed these control-derived recipes into independent generation. Any later search change needs a separate design and verification.

## Seed and resource boundaries

Preparation uses zero fresh values and zero fights. Six native preparations may declare one already reserved historical discovery seed temporarily because the input validator requires it; no random seed is derived, no battle begins, and exported recipes remain seed-free.

After explicit approval only, reserve **64 fresh values**, excluding all **482,222** current reservations. Expected union **482,286**. Keep original unused512 and unused32 excluded. Derivation is the existing stable algorithm with domain `tower-composition-order-diagnostic-v1`, master `2026091502`, stage `diagnostic`, zero-based ordinal and at most 100,000 candidates. A durable Pending intent precedes derivation, each Start precedes its candidate, and accepted/rejected candidates remain recorded. Changed or unresolved historical inputs stop allocation; failed binding is never retried.

Maximum new output **4 GiB**: preparation/control together below 1 GiB, study below 3 GiB. Native combat run maximum **900 seconds**; all diagnostic workloads together maximum **1,800 seconds**, including native preparation, binding, execution, reconstruction and independent audits. Editing, builds, focused unit tests and report publication are measured separately. User decision wait is excluded. No predecessor cap is reset or increased.

Use the existing compact campaign with chunk size 32, zero retry reserve, prepared execution, owned storage accounting and durable per-attempt charging. The outer Start/Complete journal caps execution at 384 attempts and preserves interrupted starts. Reserve output space for outer receipts before setting the campaign cap. Cancellation and time/storage failure retain partial evidence. Complete archive reconstruction and exact file hashes remain mandatory. Do not weaken durability for speed.

## Preparation and verification freeze

Before the once-only native preparation command, pin both source scenarios, the deep-study manifests, the captured content and gameplay assemblies, new helper/adapter source, producing executable, protocol and scripts. Run focused synthetic transformation/paired-statistics tests through `build/run-tests.ps1`. These tests enter no combat and allocate no balance seeds.

The frozen native check constructs all six cases, validates their unchanged non-Essence context, prepares six rosters with a combat-entry guard, exports hashes, refreshes the complete historical reservation registry, and emits all 65 possible win counts for both interval families plus 25 fixed paired fixtures. A separate Python audit independently reconstructs the matrix, validates memberships/order/context, checks roster hashes and verifies every numerical fixture against `statistics.NormalDist`. No fresh seed derivation is permitted in either preparation check.

After successful preparation, seal the package and publish the six recipes, measured checks, limitations and exact gated commands. Authorization of this preparation does not supply the required 64-value exception. Original v19 stays Unresolved with 253 retained recipes and no internal confirmation; historical portfolio reliability remains Fail 1/3, deep recovery 0/3 and adoption Hold.
