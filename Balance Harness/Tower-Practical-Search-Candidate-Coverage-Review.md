# Practical search: candidate coverage diagnosis

17 September 2026. **VerifiedSavedCoverage.** Analysis of the six sealed studies from the [allocation comparison](Tower-Practical-Search-Allocation-Comparison.md), covering **207 proposal attempts, 186 evaluated occurrences and 149 distinct recipes**. Zero new battles, seeds, generated parties or native preparations. The existing search remains the default and the confirmed `399bc776…` team remains the recommendation.

**Recommendation: implement one separate, balanced one-change search around an explicitly designated strong supplied team.** The evidence supports testing this narrower hypothesis; it does not establish that untested nearby teams are stronger. The [proposed policy](Tower-Practical-Anchored-Neighborhood-Proposal.md) reserves 44 nearby candidates plus the two supplied teams at the existing 368-discovery-fight ceiling. Racing remains closed, with no extension or retuning.

## The gap is specifically near the confirmed team

A one-change neighbor replaces one Essence on one character while preserving every other assignment, fixed equipment, identities and canonical order. Changes are measured as set differences on the same character. Editing a different parent can be a `single` operation without being one change away from the confirmed team.

| Restart / method | Attempts | Unique evaluated teams | Duplicate / illegal attempts | One-change neighbors of confirmed team | Characters covered by those neighbors |
| --- | ---: | ---: | ---: | ---: | --- |
| 1 existing | 52 | 46 | 5 / 1 | 3 | 1, 4, 8 |
| 1 racing | 16 | 16 | 0 / 0 | 1 | 4 |
| 2 existing | 54 | 46 | 8 / 0 | 0 | None |
| 2 racing | 17 | 16 | 1 / 0 | 1 | 6 |
| 3 existing | 52 | 46 | 5 / 1 | 5 | 2, 4, 5, 7, 9 |
| 3 racing | 16 | 16 | 0 / 0 | 0 | None |

The 85-Essence pool and 82 source families permit **3,952 distinct legal one-change neighbors** of the confirmed team. This is a combinatorial count of allowed replacements, with no new complete candidates generated. Owned-copy limits are absent in this captured scope. Counts per character are 396, 392, 396, 392, 396, 396, 392, 396, 396 and 400.

Only **nine distinct neighbors (0.23%)** were evaluated across all six studies: eight in the existing search and two in racing, with one shared recipe. No direct neighbor changed character 3 or 10. This is not a claim that those characters were globally ignored: existing-search mutations collectively touched all ten characters in every restart. The sparse coverage concerns proximity to this exact confirmed team.

Both methods retain access to both supplied parents. The existing search generated 7, 3 and 9 evaluated children with the confirmed team as primary parent, including larger edits. Many single edits instead originated from the second supplied team or an adaptive descendant. Historical superiority is not supplied as a fitness score. For example, in restart two the confirmed team scored 3/8 in discovery while the second supplied team scored 6/8; these small-panel observations guide the existing parent population.

## Fresh construction consumed measurable capacity

| Method, three restarts | Fresh candidate evaluations | Distinct fresh recipes within method | Discovery fights spent on fresh recipes | All discovery fights | Fresh share |
| --- | ---: | ---: | ---: | ---: | ---: |
| Existing | 51 | 51 | 408 | 1,104 | 37.0% |
| Racing | 24 | 24 | 336 | 864 | 38.9% |

The methods share all 24 racing fresh recipes with the existing search, so there are **51 distinct fresh recipes overall**, not 75 independent recipes. The 75 initial-screen evaluation occurrences all recorded 0/8 wins. Racing additionally screened/promoted some of those same recipes; those recorded fights also contained no wins. Across the two methods, **744 of 1,968 discovery fights (37.8%)** evaluated fresh recipes, with zero observed wins. These adaptive and sometimes repeated observations are descriptive accounting, not a pooled population-rate estimate or a significance test.

Fresh recipes were **43–50 assignment replacements** away from the confirmed team. None was nominated in any study. No fresh recipe was used as an actual parent in the three existing-search traces. Racing retained two zero-win fresh recipes alongside the two supplied teams after each first-round promotion; its frozen second-round parent population then produced **eight evaluated children involving a fresh parent**. All eight also recorded 0/8 initial-screen wins.

This is evidence that the implemented broad-construction path contributed no selected output in these runs. It does not prove fresh construction can never discover a better region, or justify changing the independent-from-scratch search objective.

## Operators and duplicate handling

Each entry below is evaluated candidates / challenger nominations, aggregated over three restarts. Supplied incumbents are excluded from this table. These are adaptive counts, not causal estimates of operator quality.

| Operator | Existing search | Racing |
| --- | ---: | ---: |
| Fresh legal construction | 51 / 0 | 24 / 0 |
| Single change | 20 / 4 | 6 / 5 |
| Double change | 20 / 2 | 4 / 0 |
| Cross-character change | 19 / 0 | 3 / 0 |
| Whole-character replacement | 19 / 0 | 3 / 1 |
| Recombination | 3 / 0 | 2 / 0 |

The existing search rejected 18 duplicate attempts and two family-invalid attempts; racing rejected one duplicate. **No duplicate or illegal proposal consumed combat.** All studies reached their candidate allowance well before the 256-attempt ceiling. Sixteen of the existing search's 19 recombination attempts duplicated earlier evaluated recipes. That may warrant a later engineering improvement, but it did not reduce the declared candidate count or explain the 136 fights per restart spent evaluating fresh recipes. It is not the priority proposed here.

## Where the Ice Harpy candidate went

The selected `7a249fb5…` candidate first appeared in existing-search restart one at **proposal index 28 (zero-based), evaluated candidate 26 (one-based)**. It directly changed the confirmed team's character 1 from Venomous Spiderling to Ice Harpy, keeping the other 49 assignments fixed.

| Stage | Candidate | Confirmed parent |
| --- | ---: | ---: |
| Discovery, paired panel | 6 / 8 | 5 / 8 |
| Selection, paired panel | 25 / 32 | 22 / 32 |
| Confirmation, paired panel | 730 / 1,000 | 708 / 1,000 |

The candidate entered the four-parent population immediately, remained third in the final discovery ranking, was used in three subsequent parent references, was nominated and then selected. It was **never proposed by any racing restart**. Its absence from racing is therefore a generation/coverage observation, not an instance of that candidate being discarded by racing's screen or selector. Its 2.2-point confirmation advantage remains below the completed experiment's five-point target and does not authorize fixed-team adoption.

It is tempting to conclude that racing merely stopped too soon. The traces do not establish that counterfactual. The methods share their first eight candidate recipes and paired scores, then first diverge at proposal indices **9, 9 and 8** respectively. They use different parent populations and later random draws. Each pair shares 11 evaluated recipes overall, with 35 existing-only and five racing-only recipes. Extending racing to a 26th candidate would not necessarily reproduce the existing search's 26th recipe.

## Retention, nomination and selection reconstructed correctly

The saved-evidence analyzer checked baseline parent populations against the earlier completed measurements; racing's frozen parent sets, screening membership, promotion membership and next-round parents; exact nominee order; and the existing selection tie rules. All six selected IDs agree with the frozen finalists and confirmation members.

Each existing-search run evaluated 46 teams, nominated both supplied teams plus its two highest-ranked challengers, and left 42 teams unconfirmed through that output path. Each racing run admitted 16 teams: four first-round recipes were not carried forward, eight candidates in its final screen were not promoted, and four reached final promotion and nomination. The selected finalist in every racing run was the confirmed team.

Discarded candidates should not be treated as known selection mistakes. Apart from the frozen selected output and supplied controls, discarded candidates generally have no independent confirmation result. Their true strength is unknown. The strongest measured candidate missing from racing was never generated there; the analysis found no ranking or membership mismatch that warrants changing the selector.

## Decision and limits

Test **coverage of the immediate neighborhood around a known strong supplied team** as one distinct policy. Its initial implementation should be simple and auditable: one explicit anchor, uniform legal one-change sampling with balanced character coverage, fixed original discovery samples and the existing downstream selector. No Essence or character receives a special weight because it appeared in this post hoc analysis.

The proposal deliberately trades fresh exploration and multi-change adaptation for local coverage. The current records do not establish that this tradeoff improves outputs; better teams may require coordinated changes or lie outside the immediate neighborhood. Earlier [V4 local refinement](Tower-Local-Refinement-Trajectory-Review.md) and [V5 fresh-first refinement](Tower-Fresh-First-Trajectory-Review.md) did not validate a local-search improvement. Those policies edited weak freshly constructed teams and are not affirmative evidence for this supplied-anchor variant. Their closed decisions remain unchanged.

The completed comparison's held-out panel has now informed this hypothesis. It cannot validate the proposed policy. A later comparison needs new prospectively declared roots, held-out values, effect threshold, precision and stopping rule; historical outcomes cannot be reused as fresh evidence. Current-family calibration remains a separate scope. No new search is launched by this review.

## Verification and changed files

The new [analysis script](analysis/practical-search-coverage.py) uses Python's standard library only. It verified hashes for all **31 consumed archive files** against the sealed native inventory, reconstructed the decisions above, reconciled discovery/selection costs and rechecked all consumed hashes after analysis. Full native archive verification from the completed experiment was not rerun. The definitive [summary](../TestResults/allocation-coverage-analysis-final-20260917/summary.json), [per-proposal trajectories](../TestResults/allocation-coverage-analysis-final-20260917/trajectories.json), [candidate trace](../TestResults/allocation-coverage-analysis-final-20260917/challenger.json) and [completion receipt](../TestResults/allocation-coverage-analysis-final-20260917/completion.json) are retained with input/source hashes and a copy of the analyzer. The initial analysis package is superseded by this final recount, which restricts parent-population appearance counts to actual mutation attempts and adds method totals; candidate, cost and outcome counts agree.

Three arithmetic tests passed: character-aware composition distance, family legality and neighborhood counting, and positive/zero-win selection ties. The final saved-evidence recount passed in 0.58 seconds. Commands, using the configured Python executable:

```text
python "Balance Harness/analysis/practical-search-coverage.py" --self-test
python "Balance Harness/analysis/practical-search-coverage.py" --output TestResults/allocation-coverage-analysis-final-20260917
```

Changed files are this review, the separate policy proposal, the analyzer and links/status in the comparison review and practical usage guide. Backend code and tests were not changed, so the backend wrapper was not rerun for this analysis/documentation task. No verification command remains blocked. No migrations, application configuration changes or deployment are required. Permanent exclusions remain **501,467**; no historical evidence package was modified.
