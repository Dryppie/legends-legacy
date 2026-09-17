# Practical policy design: balanced neighbors of a supplied anchor

17 September 2026. **Implemented and evaluated; the paired comparison completed with `DoNotPromoteAnchored`.** See the [prospective protocol and verified result](Tower-Practical-Anchored-Neighborhood-Comparison.md). See the [implementation and verification record](Tower-Practical-Anchored-Neighborhood-Implementation.md). Target: offline BalanceHarness practical supplied-team refinement. Motivated by the [saved coverage analysis](Tower-Practical-Search-Candidate-Coverage-Review.md). The existing incumbent policy remains the default; `retained-composition-racing-v1` remains closed without promotion.

## Hypothesis

At the same 368-fight discovery budget, evaluating more distinct one-change neighbors of an explicitly designated strong supplied team may produce stronger finalists than the incumbent search's mixture of fresh parties and adaptive mutations.

The evidence is limited but concrete: only nine of 3,952 legal neighbors were evaluated across the last six studies, while 37.8% of their discovery fights evaluated fresh recipes that recorded no victories. One existing-search single-change candidate was selected and recorded a 2.2-point confirmation advantage; that observation is insufficient for adoption and is not a performance guarantee for the proposed policy.

## Policy contract

Version: `anchored-neighborhood-v1`. Scope the first version to the same complete ten-character, five-Essence, one-context supplied refinement problem. Reuse existing legality, canonical order, recipe identity, measurement, nomination, selection and archive components.

1. Require an **explicitly designated primary supplied reference**, plus the existing second supplied reference. The primary is an input to this policy, not inferred from archived outcomes, current eight-trial scores or a hardcoded party ID. The comparison must disclose this designation and hold the two starting recipes fixed for both methods.
2. Admit both supplied recipes and **44 distinct legal one-change neighbors of the designated primary**, giving 46 evaluated candidates. Every new recipe differs by exactly one removed and one added Essence on the same character; all other recipe fields remain fixed.
3. Balance coverage across characters. Use four complete passes over a construction-root-shuffled character order, then four positions of that order: each character receives four or five neighbors. The extra positions rotate with the construction root. This rule applies uniformly; character 1 and Ice Harpy have no special treatment.
4. Within each character, choose uniformly without replacement from its legal `(removed Essence, added Essence)` options using a deterministic stream derived from the declared construction root and version. Respect source-family and owned-copy rules; exclude no-ops and supplied/previously admitted recipes before combat. Record the selected edit descriptors and provenance. If the declared scope cannot supply the required quota, reject it explicitly without silently switching operators or redistributing the quota.
5. Freeze the full candidate batch before reading discovery outcomes. All new candidates use the same primary parent; there is no adaptation or reevaluation within this policy.
6. Evaluate all 46 teams on the same eight declared discovery values: **368 fights**. Preserve both supplied teams in the four-nominee shortlist; fill the other two places using the existing discovery ranking. Keep the existing 32-trial selection stage and one-finalist rule: **128 selection fights**.

The frozen candidate batch makes the coverage guarantee inspectable before any scores exist. It also removes within-run exploitation of a newly discovered good parent. That tradeoff is part of the policy being tested, not an incidental implementation detail.

## Required implementation checks

Verify deterministic reconstruction; four/five-character quotas; 44 distinct legal neighbors; exact one-change distance to the designated primary; unchanged equipment/identities/order; no hidden imports of archive fitness; complete provenance; eight shared discovery trials per recipe; protected supplied nominees; and unchanged selection behavior. Cover small or copy-constrained neighborhoods with explicit failure fixtures. Native accounting and archive reconstruction must include every attempted fight and retain partial evidence on cancellation.

Do not alter legacy policy behavior or sealed archive readers. The primary-reference designation must survive serialization, native validation and reconstruction. Existing practical commands should opt into the new version explicitly; the default remains unchanged.

## Subsequent quality test

Use the unchanged incumbent method as comparator with the same two starts, gameplay, 46-candidate/368-fight discovery ceiling and 128-fight selection ceiling. The local method intentionally uses the declared strong reference as its fixed parent; this is a test of the whole supplied-refinement policy, not an isolated test of character balancing.

Before any execution, specify fresh construction roots, paired untouched confirmation panels, a useful effect threshold, precision, restart-level interpretation, total resource ceiling and stopping rule. Freeze all outputs before confirmation. Retain every planned restart, including failures. Report actual costs and selected output strength separately; increased neighbor coverage is an implementation check, not a success endpoint. Reuse the existing comparison machinery where its frozen contract genuinely fits; its current version is specifically bound to racing and must not silently be repurposed.

No fresh scientific values, candidates, battles or operational allowance are allocated here. The optional generator and its fixture/integration checks are documented in the implementation record. A quality experiment remains a separate prospectively specified step. Missing the later declared endpoint must not trigger outcome-dependent sample extension or a switch to favorable secondary metrics.

## Risks and exclusions

The confirmed team may be a local optimum. Useful improvements may require two or more coordinated replacements. Uniform character coverage can spend trials on insensitive positions, and 44 nearby candidates still cover only about 1.11% of this immediate neighborhood. Eight reused discovery trials can still overfit. These limitations are not resolved by the coverage analysis.

This policy does not address independent discovery from scratch, change the final selection rule, fit a surrogate model, add a combat mechanic, change balance thresholds or reopen racing. It requires no gameplay data change, migration or deployment. The existing confirmed team recommendation remains in effect until a separate valid adoption result changes it.
