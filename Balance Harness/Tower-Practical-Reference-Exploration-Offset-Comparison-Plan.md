# Prospective comparison of reference exploration with owner offsets

22 September 2026. Target: offline `LL/tools/BalanceHarness`. This is a **new, single comparison**, `tower-reference-exploration-offset-comparison-v1`, between the [implemented owner-offset policy](Tower-Practical-Reference-Exploration-Offset-Implementation.md) and the unchanged three-reference practical baseline. The [JSON contract](Tower-Practical-Reference-Exploration-Offset-Comparison-Plan.json) freezes the scientific scope and resource envelope. Preparation and engineering checks do not constitute a scientific launch.

The [closed original comparison](Tower-Practical-Reference-Exploration-Comparison-Execution.md) supplied no promotion evidence. Its [diagnosis](Tower-Practical-Reference-Exploration-Diagnosis.md) found that the third reference repeatedly explored only owner slots 1–7 in its short search. The new policy varies each reference's initial owner cursor using the construction root and exact reference ID. Mechanical coverage has passed; the hypothesis here is that this change improves the **selected team's independently confirmed strength**. Coverage, novelty and nomination counts are diagnostics, not substitutes for that endpoint.

## Fixed comparison

| Item | Declaration |
| --- | --- |
| Baseline | `retained-composition-three-references-v1` |
| Candidate | `retained-composition-three-reference-exploration-offset-v1` |
| Selector in both arms | `tower-staged-incumbent-tie-v1` |
| Tie designation | `confirmed-399bc7760fb0cf790a5d8ac4` |
| Paired construction restarts | 12; baseline then candidate for each root |
| Discovery per arm | 46 evaluated recipes; maximum 256 proposals; eight trials each |
| Selection per arm | All three controls plus two ranked challengers; 32 trials each; one output |
| Confirmation per pair | 1,000 common fresh trials for each distinct physical recipe in the union of both outputs and all three controls |
| Context | Captured floor 5; ten level-40 characters; two groups of five; five Essences each; rank 2/tier 1; fixed equipment and actor identities |
| Acquisition/order | `OwnedCopies=null`; unchanged allowed pool; canonical Essence order |
| Additional work | No diagnostics battles, replay, replacement root, retry, resume or extension |

The three exact control recipe hashes are `399bc7760fb0cf790a5d8ac4272b607a440d5f982a17333842f9b3e79f680d5b`, `8287f77974c8c94e8af2fbcdb1b0e42911721d5fb1738fd8f24c5cccfc01ae50` and `96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c`. Both arms receive all three without historical fitness. The third reference is not made tie-designated primary.

Preserve the seed-free `preset/template.json` and gameplay dependencies, effective settings and 16 content files of the reconciled three-reference capture. The JSON binds the original manifest `001eb3e1…`, its appended failure `8e277d2a…`, closeout `65cde20a…` and template `cd12abf2…`. A newly tested harness must match its producing symbols/sources and be compatible with these preserved gameplay dependencies before admission. Current development gameplay cannot silently replace them.

The offset draw changes only the initial owner position of the existing periodic reference-exploration schedule. It does not change opportunity cadence, radii, construction caps, nomination, selection or the old exploration-construction random stream. Both policies receive equal discovery and selection budgets. The experiment tests the complete offset exploration policy against the practical baseline; it does **not** isolate the causal effect of offsets against original exploration v1.

## Pairing and freeze

Both arms within each pair share one construction root, eight discovery values and 32 selection values, with identical scenario identity. Each arm performs its own adaptive search and measurements; there is no cross-arm search cache or outcome sharing. Definitions may differ only in generation policy after binding.

Run all 24 searches before confirmation. Durably freeze their outputs, all twelve ordered physical confirmation unions/panels, and the attempt-journal hash after exactly **12,672 completed search fights**, with no unmatched attempt. A missing candidate target, missing nominee, invalid input or failed freeze makes the comparison incomplete or invalid. No replacement root is allowed.

Then confirm each pair's three-to-five distinct physical recipes in ordinal physical-recipe-hash order, once per recipe on the pair's complete 1,000-value panel. Identity includes ordered owners, equipment, progression, Essences and actor identities in the fixed encounter. Controls and coincident outputs share physical evidence within a pair. There is no sharing across pairs. Logical per-arm family-ten views remain descriptive and cannot authorize team adoption.

## Primary endpoint and abandonment

For pair `r`, let `G_r` count candidate wins/baseline losses and `L_r` the reverse on common confirmation values. All twelve planned pairs remain in the denominator:

```text
net  = sum_r (G_r - L_r)
mean = net / 12000
```

Support requires **net ≥600**, a strictly positive one-sided 95% conditional lower bound, and **at least seven positive pairs**. Gate on integer counts, not rounded percentages. Return `SupportsReferenceExplorationOffsetForFrozenOutputs` only when all gates and both audits pass. Otherwise a complete comparison with differing outputs returns `DoNotPromoteReferenceExplorationOffset`. All identical outputs return `NoSelectedOutputDifferences` and exact zero mean/bound, without implying future equivalence. All confirmation unions still complete even if seven positive pairs have become impossible.

The estimand is the mean difference of these twelve frozen output pairs over the fresh 32-bit population after the search values, conditional on the complete search. It excludes uncertainty across future construction roots. Do not condition the claim on realized confirmation values, unused entropy tail, control-only outcomes or successful completion. Previous outcomes are excluded rather than pooled.

Let `H` be the fully verified pre-draw historical count, `M=2^32-H-492`, `K` the number of differing output pairs, `n=1000*K` and `N=12000`:

```text
depletion = n(n-1) / (M N)
margin    = sqrt(2 n log(20)) / N + depletion
lower     = max(-K/12, mean - margin)
```

For `K=0`, all four quantities are zero. This retains the [prior derivation](Tower-Practical-Reference-Exploration-Comparison-Plan.md#conditional-precision): each frozen paired difference has range length two; sampling without replacement adds the stated depletion allowance. The conditional-range martingale inequality is given in [Kuang Yang's lecture, Theorem 6.2](https://jhc.sjtu.edu.cn/~kuanyang/teaching/CS3341/notes/lec06.pdf). The global freeze prevents confirmation outcomes from changing the functions being measured.

At illustrative `H=567,789` and `K=12`, the margin is about **2.235 percentage points**. Twelve restarts are twelve search replicates, not 12,000 independent search roots. The unchanged trial count has no guaranteed power at exactly the target five-point true mean, and positive coverage is not a strength forecast.

**Abandonment rule:** one comparison only. A failed strength gate or all-identical outputs ends this offset experiment without promotion or extension. Another offset variant is not justified solely by coverage success. Incomplete/invalid evidence supports no performance conclusion and permits no replacement attempt. Defaults and team recommendations require a separate decision even after a positive result.

## Allocation and resource envelope

After passing admission, a separately invoked owned launcher may draw one 16,384-word 32-bit entropy batch. Reject history and duplicate words without refill. Assign the first 12,492 accepted fresh values: twelve 41-value search blocks first, then twelve 1,000-value confirmation blocks. Permanently reserve every fresh value in the whole draw, including unused tail and interrupted assignments. Require `H≤983,616` to stay within the million-value registry ceiling.

The latest sealed engineering history receipt records **567,789 exclusions across 234 files**. It is a source pin for planning, not a replacement for the complete admission and immediate prelaunch scans, including abandoned-reservation recovery. Preserve the closed original comparison and all earlier values. No old allocation or unused allowance is reopened.

```text
search       = 12 * 2 * (46*8 + 5*32) = 12672 fights
confirmation = 1000 * sum of twelve physical union sizes
total        = 48672 .. 72672 fights
```

The new cumulative limit is **10,800 seconds /6 GiB**, including a conservative **600-second /512-MiB admission charge**. The single execution, both audits and publication share **10,200 seconds /5.5 GiB**. The native child gets **10,080 seconds /5.25 GiB**, leaving enclosing headroom. Later audits receive only the remaining deadline; failures retain all attempted work and reservations. Disk accounting includes retained evidence, temporary output and copied runtime files.

For feasibility, retain the earlier conservative linear estimate from the authenticated 3,528-fight execution: scaling its complete cost to 72,672 fights gives approximately **8,587 seconds /3,134 MiB**, within the declared execution envelope. The faster original exploration comparison does not justify lowering the allowance. This estimate is not a guaranteed upper bound; hard limits govern the new attempt.

Engineering work remains separately disclosed under the accepted accounting decision. Complete older engineering totals remain unknown. The **18,180-second /13,584-MiB** prior ledger is preserved. Admission precharges its full allowance; none of this permits a retry or transfers unused budgets from closed experiments.

## Implementation and admission boundary

The existing comparison engine may share its unchanged lower-level mechanics, but must bind the new exact version, candidate policy, plan hash and decision labels throughout request, allocation, study, freeze, scope, launch, receipts and both auditors. Original v1 remains pinned to its original candidate and plan. There is no arbitrary generator or user-supplied gate override.

Before scientific allocation, the new captured package must authenticate the source capture and tested runtime; match the portable symbols to producing sources; resolve native entry paths; validate both policy definitions and all three references under a combat-entry guard; reproduce the complete exclusion inventory natively and independently; bind exact request/content/runtime/source/helper hashes; prove owned process trees empty; and seal the remaining-budget ledger. Validation labels are not scientific values.

Native reconstruction must reproduce both trajectories, nomination, selection, freeze, physical evidence mapping, attempts and result without combat. The independent auditor must recount direct saved outcomes, common panels, all gains/losses, costs, reservation tail and decisions. Engineering fixtures exercise both versions, convergence, unequal outputs, rejected version/plan substitutions, interrupted work, altered archives and shared audit deadlines.

The [comparison admission report](Tower-Practical-Reference-Exploration-Offset-Comparison-Admission.md) records the actual implementation checks and admission outcome separately. No scientific launch follows merely from creating this plan or passing admission.

## Execution follow-up

The [single admitted execution and both audits](Tower-Practical-Reference-Exploration-Offset-Comparison-Execution.md) are verified and closed. All 24 outputs froze after 12,672 search fights; confirmation added 39,000 fights. The result is `DoNotPromoteReferenceExplorationOffset`: +21 net wins /12,000 (**+0.175 percentage points**), one positive pair and a **−0.4700-point** conditional lower bound. All three promotion gates failed. This executes the abandonment rule above: no promotion, retry, extension or coverage-driven offset variant. The JSON contract and scientific scope remain unchanged. Post-execution history is **584,171 exclusions across 236 files**; earlier counts above describe planning and admission.
