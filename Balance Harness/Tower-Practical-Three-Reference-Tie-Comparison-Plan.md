# Three-reference positive-tie comparison

23 September 2026. This prospective design tests `tower-staged-three-reference-tie-v1` against the current `tower-staged-incumbent-tie-v1` on the same direct three-reference searches. The [implementation](Tower-Practical-Three-Reference-Tie-Implementation.md) is a separate engineering step; the [captured-runtime admission](Tower-Practical-Three-Reference-Tie-Admission.md) and [single scientific execution](Tower-Practical-Three-Reference-Tie-Comparison-Execution.md) are now complete; the result is **`NoSelectorDifferences`**. The protocol below is the unchanged prospective design, not permission to rerun it. The [frozen JSON](Tower-Practical-Three-Reference-Tie-Comparison-Plan.json) has SHA-256 **`2ae905286313000e04170f45acd849c552251954487c99186d4cea1b96fcdf24`**.

## Hypothesis and exact change

The [fresh-screening stage review](Tower-Practical-Fresh-Screening-Stage-Review.md) identified positive challenger/reference ties where the primary-only preference did not protect another confirmed reference. That motivates this test; old confirmation results are not evidence for the new selector. The screening experiment remains closed as `DoNotPromoteFreshScreening`.

Both selectors first rank by the same 32 selection wins. The candidate:

1. Preserves any unique win-count leader.
2. On a positive maximum tie, retains the designated primary if it is tied.
3. Otherwise prefers a tied supplied reference over a tied challenger. If multiple references tie, their existing frozen nominee order decides.
4. Uses the existing frozen order when no reference is tied. At zero wins, preserves mean guardian health, frozen nominee order and stable ID.

The reference set and primary must come from the validated definition and exact bound recipes. Historical fitness and confirmation outcomes never enter selection. All three original references remain supplied; no new primary is chosen. Strict challenger leads remain eligible, so the rule does not address the strict-lead failures in the earlier review.

## Shared search and fresh confirmation

Protocol: **`tower-three-reference-tie-comparison-v1`**. Use 24 fresh construction roots, the existing `retained-composition-three-references-v1` generator, 46 candidates and at most 256 proposal attempts per root. Retain the captured floor-5, ten-character, level-40, five-Essence, canonical-order context, gameplay dependencies, content and effective settings from the reconciled [three-reference capture](Tower-Practical-Three-Reference-Admission.md).

Each root runs one common search: **46 ×8 discovery +5 ×32 selection =528 fights**. The five nominees are the three supplied references and two challengers. Both selectors receive the exact same measurements. There is no fresh-screening stage or separate candidate search.

Freeze all **48 output choices** durably before the first confirmation fight. If K roots choose different physical recipes, confirm both recipes on the same 1,000 fresh values at each active root. Identical outputs contribute exact zero paired difference; their absolute win counts remain **null/unmeasured**. No additional reference controls are measured.

Total fights are **12,672 +2,000K**, at most **60,672**. An incomplete search, failed freeze or incomplete confirmation terminates the comparison; roots cannot be dropped or replaced.

## Allocation and acceptance

Rescan the complete authoritative history immediately before reservation. The latest closed review records **600,552 excluded values across 238 files**; this is illustrative, not an admission-time substitute. Draw one 32,768-word signed 32-bit entropy batch. Permanently retain every fresh word, including unused tail values. Assign the first **24,984** unique fresh values in order: 24 search blocks of one root, eight discovery and 32 selection values (**984** total), then 24 fixed 1,000-value confirmation panels. Inactive panels remain excluded. At most 967,232 historical values are supported. No refill, retry or resumed allocation is allowed.

Use the earlier [selector-comparison acceptance rule](Tower-Practical-Incumbent-Tie-Comparison-Plan.md): at least **240 net gained wins /24,000 =+1 percentage point**, at least **three positive roots**, and a strictly positive one-sided 95% conditional lower bound. The denominator remains 24,000 regardless of K. This is a selector-only comparison; the five-point generator-comparison threshold is not substituted.

For H historical exclusions, let n=1,000K and M=2^32−H−984. With bound version `conditional-range-hoeffding-depletion-v1`, depletion=n(n−1)/(M×24,000), margin=√(2n ln20)/24,000+depletion, and lower=max(−K/24, netWins/24,000−margin). This conclusion is conditional on the frozen outputs, not a claim about future random roots. Outcomes are `SupportsThreeReferenceTieForFrozenOutputs`, `DoNotPromoteThreeReferenceTie`, or `NoSelectorDifferences` when K=0. No sample extension or automatic default change follows any outcome.

## Resource and execution boundary

The cumulative allowance is **10,800 seconds /6 GiB**. Precharge **600 seconds /512 MiB** for a separate captured-runtime admission, leaving **10,200 seconds /5.5 GiB** for one owned execution, both audits and publication. Native execution has a nested ceiling of **10,080 seconds /5.25 GiB**. These are ceilings, not runtime estimates; unused earlier experiment allowances do not transfer.

Use [the new owned launcher](../build/run-three-reference-tie-comparison.py) after admission. The native commands remain `tower-incumbent-tie-comparison-check`, `-run`, `-audit` and `-verify`, with the explicit new request version. The request additionally binds the reconciled capture, this plan, the auditor and prior charges. A hidden Windows Job owns each process tree before execution; both audits share the remaining execution deadline. Any failure retains evidence and exposed reservations without a completion seal.

Engineering is separately disclosed as authorized. Historical engineering totals remain unknown; the reconciled **18,180-second /13,584-MiB** ledger is a prior-charge ledger, not a total. This plan does not change application configuration, migrations, gameplay defaults, confirmed-team recommendations or the captured cohort's separate balance failure.
