# Frozen late-allocation experiment plan

Declared **14 September 2026**, before preparation or combat. User authorization covers implementation, testing, preparation and one bounded execution. Target: offline BalanceHarness only.

The distinct opt-in policy is `independent-late-allocation-v18` / `tower-late-allocation-v1`. The master seed is **2026091401**; preparation deterministically allocates the three root seeds and three disjoint trial schedules from that value, excluding the complete latest ledger. Their exact values are retained in the frozen definition and seed ledger before execution.

Implementation verification: **349 distinct relevant tests passed**, including **39 new-policy cases**. Saved-data reconstruction reproduced **13,632 evaluations and 48 feedback probes** across five historical studies with identical complete generation and shortlists, unchanged gameplay assemblies, and zero audit fights. The source, test receipts and captured executable are retained in `TestResults/balance/tower-late-allocation-work-20260914` and the separate campaign package.

Campaign: `TestResults/balance/tower-late-allocation-20260914`. Inputs: the stopped v17 discovery definition, the complete 94-recipe fresh-confirmation family, and the 479,533-reservation fresh-confirmation ledger. Kharad remains Health **3.5366243328** / Power **4.4702934848**. Full content, settings and gameplay identity must match before execution.

**Binding cap:** 114,688 fights, 10,800 execution seconds, 4 GiB, zero retries, no resume, no cap extensions. Discovery is 36,864 fights; screening is 12,288; confirmation includes the entire required family at 512 trials each, up to capacity 128. Overflow preserves the entire family and executes no confirmation. Stop or failure retains all evidence and every reserved seed. Read-only verification after execution is separately timed.

The following design is adopted from the sealed proposal. Its prospective wording records the design requirements; implementation readiness above and the preparation receipt determine whether this one experiment may start. No allocation rule, seed, nominee, family membership, threshold or cap may change after observing campaign outcomes.

# Proposal: defer allocation, then give one isolated search more depth

Proposed **14 September 2026** after the [saved-trajectory diagnosis](Tower-Allocation-Trajectory-Diagnosis-Review.md). This is one experiment design, not an executed or prepared study. No generation roots, combat seeds or fights are allocated by this document.

## Question and evidence

Can a late choice between two independently initialized searches produce reliable nominees within the same **768-candidate budget per restart** used by the deep comparator?

In the saved experiment, the component that produced all four newly confirmed viable recipes initially ranked below its isolated partner. It first overtook that partner at candidate **243**. Its confirmed recipes appeared at candidates **352, 368, 371 and 382** of 384. At **256** evaluated candidates per component, the successful component was preferred by the existing fitness ordering. At 96, 128, 192 and 224 it was not. The four confirmed recipes were already retained by the original top-32 screening rule.

This motivates a **256-candidate common prefix followed by one allocation decision**. The cutoff is an exploratory choice informed by these saved trajectories, rounded to a regular checkpoint; it has no independently established optimality. New roots and trial seeds are necessary to test it. Its unexecuted 385–512 continuation has no measured outcome.

## One change and unchanged comparator

| Unit in each paired restart | Allocation | Total candidate evaluations |
| --- | --- | ---: |
| Comparator | Unchanged deep v13 search, 768 candidates, 192 initial fresh parties | 768 |
| Candidate | Isolated A and B each evaluate 256 candidates; choose one and give it 256 additional evaluations | 768 |

The candidate finishes with **512 evaluations in one component and 256 in the other**. Both components start with exactly **96 fresh parties**, preserving the v17 component construction horizon of 384 even when the final allocation becomes 256 or 512. Initial fresh effort is therefore 192 total for both units. Do not let `candidateBudget / 4` silently change those 96 initial parties to 64 or 128.

The candidate's A and B share only the declared discovery schedule and the allocation decision. Each retains separate parents, measurements, ranked 128-module library, exploration, proposal counter and RNG state. After both prefixes finish, select the component whose best measured party ranks first under the existing lexicographic rule: discovery win rate descending, guardian health ascending, survival descending, victory duration ascending, ordinal party ID. If complete ranking keys tie, prefer component A by its declared label. Selection consumes no random draw. Preserve the exact decision inputs and both prefix rankings.

The chosen component continues its own state to 512; the other remains at 256. Do not reconstruct a fresh 512-budget run, reseed, reset the operator counter, transfer modules between components, or use screening/confirmation outcomes to allocate. This continuation is part of the new, predeclared experiment; no historical stopped archive is resumed.

Keep the existing v13 operators, four elite parents, four exploration parents, ranked 128-module library, every-fourth fresh proposal, legal-build rejection and ordered-recipe identities. Deep uses the unchanged `coverage-joint-<root>` stream. Candidate A/B use their existing v17 named streams, initialized with **new** root seeds. Preserve the 96-initial/384-horizon stream prefix through 384; use only the selected component's own subsequent draws after that. No saved recipe, ingredient list, winning copy count or donor enters construction or ranking.

Keep the existing per-unit proposal envelope of **8,192 attempts**: 8,192 for deep, 4,096 per isolated component. The candidate's component cap does not reset at 256; unused rejected-attempt capacity is not transferred. A component that exhausts its proposal limit before its allocated evaluated count makes the experiment incomplete. Prefix/continuation rows remain separately countable and all actual combat attempts are charged.

The comparator remains **deep v13**, preserving the current reliability comparison and avoiding an additional third experimental arm. This experiment can establish reliability relative to deep v13. It cannot establish superiority over the earlier equal 384+384 allocation; that historical arm is not a fresh matched comparator and its outcomes must not be pooled.

## Family, trials and success criteria

Freeze three fresh paired root seeds and the full new protocol before any combat. Every accepted candidate receives the same eight fresh discovery trials. After all generation completes, group candidate A/B recipes, rank and deduplicate using the existing complete-recipe rule, preserving every charged evaluation and origin. Deep and candidate each supply a top-32 shortlist per root. Screen all **six × 32 recipes on 64 fresh shared trials**, including zero-win groups. Keep the original top two and select the screened top two by wins, tied by original discovery rank.

Import **all 94 recipes** from the [complete fresh confirmation family](../TestResults/balance/tower-allocation-confirmation-work-20260914/family.json) as explicit external controls. Preserve the historical anchor `team-1abe76ca1891d97a91d484f0a3662048` and the existing fixed strongest-prior-control comparator `team-a7e5de669c4a17287d84060e8ab6359b`; do not retarget that comparison after seeing new outcomes. The latter retains its role despite newer recipes having higher observed counts.

Confirmation includes all 94 controls, every original and screened nominee, and every discovery or screening ceiling breach under the existing rule. Freeze **capacity 128**. The ordinary nominee envelope is at most 94 + 24 = 118 before additional breaches; if the complete required family exceeds 128, preserve the entire family and stop as capacity-exceeded without partial confirmation or dropping cells. Otherwise every family member receives **512 fresh shared confirmation trials**. No pooling, confirmation-based nomination changes, retries or optional continuation.

Keep the current gate: candidate rate lower bound >= .10, paired-difference lower bound > 0 against the matching deep primary, and anchor-difference lower bound >= −.10, on **at least two of three** candidate primaries. Use the existing joint alpha split: .025 across all confirmation rates and .025 across nine paired differences (18 discordance intervals). Report the fixed stronger-control comparison separately. Report ordinary and joint family acceptance separately from reliability. No full generated-family, acquisition, near-optimality or lifetime repeated-study coverage claim.

Completion alone is not success. The proposal's primary success criterion is complete evidence plus reliability **Pass 2/3 or 3/3**; a family Pass remains a separate scoped result. A failed gate keeps the policy experimental. Any time/storage/attempt interruption is incomplete, not a reliability Fail or Pass. No automatic default or content promotion follows any result.

## Fixed resource envelope

| Phase | Maximum fights |
| --- | ---: |
| Discovery: (768 + 768) × 3 × 8 | 36,864 |
| Screening: 6 × 32 × 64 | 12,288 |
| Confirmation: 128 × 512 | 65,536 |
| **Total cap** | **114,688** |

Propose **10,800 execution seconds (three hours)** and **4 GiB**, with zero combat retries, no resume after interruption, and no cap extensions. The prior same-sized discovery took 4,754.82 seconds, screens took 328.56 seconds, and the separate 94-recipe confirmation took 903.44 seconds. These measurements motivate a conservative time envelope for a full campaign with a larger family and archive overhead; they do not guarantee its duration. Check complete storage throughout using the existing verified integrity checks. Keep 32-report chunks and dense confirmation batches. Record actual phase timings and final storage. Verification after completion is read-only and separately timed.

Use every array in the [479,533-reservation ledger](../TestResults/balance/tower-allocation-confirmation-20260914/seed-ledger.json) and any later ledger as exclusions. Reserve exactly three new roots, eight discovery values, 64 screening values and 512 confirmation values only when preparing this experiment: **587 new values**, giving **480,120** total if no intervening study reserves seeds. Treat this as a conditional arithmetic total, not the current ledger. Freeze the chosen master seed, actual schedules, plan, content and producing executable before execution. Do not spend separate diagnostic fights under this budget.

## Implementation and readiness requirements

Implement a distinct opt-in policy and explicit allocation state using existing generator patterns. Preserve historical validators, executables, contracts and results. The current generation API has one fixed per-method candidate budget and derives initial size from it; this proposal needs an explicit fixed initial horizon and a verified state-continuation boundary. Updating a budget field alone is insufficient.

Before preparation, backend tests through `build/run-tests.ps1` must establish:

- Identical new-policy and v17 component prefixes under synthetic saved measurements, including the first 96 fresh parties, RNG sequence, rejection counters, parent/library state and operator order; no combat required for this construction parity test.
- Choice after both complete 256 prefixes only, deterministic ties, A or B selection, 512+256 final counts, 768 per unit, unchanged deep construction, and zero cross-component donor/parent leakage.
- Attempt exhaustion and interruption preserve incomplete evidence; no reset, implicit retry, historic archive continuation, or use of screens/controls during allocation.
- All six grouped shortlists, original/screened nominations, complete 94-control retention, duplicate charges, overflow behavior, fresh exclusions, fixed limits, unchanged numerical gate and complete saved-report reconstruction.

Preparation must validate current content/settings/gameplay assemblies and runtime against the latest completed confirmation scope, retain its own executable and full source bindings, and run a zero-combat check. The current 128-family/114,688-fight proposal exceeds the stopped v17 caps and requires its own policy-specific contract; do not widen old policies globally. Execute once only after those checks and the new protocol freeze. Finish with full reconstruction, independent statistics, complete recipe exports, and updated active Markdown whether the result is Pass, Fail, capacity-exceeded or incomplete.

Kharad, player budget, default optimizer, catalogs and game content remain unchanged. No migrations, configuration changes or deployment are proposed. Practical acquisition and floors 6–11 remain separate work.
