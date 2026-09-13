# World Tower: the next substantial search milestone

13 September 2026. Analysis requested after eight continuation comparisons failed independent-search reliability. Target: the offline `LL/tools/BalanceHarness`. This is a recommendation and implementation direction, not a frozen experiment, new combat allocation or claim of improved balance.

## Recommendation

Make the next work package **a sustained complete-party optimizer and one decisive search-quality benchmark**. Test whether greater search depth alone recovers competitive parties, and whether retaining different measured combat behaviors improves on that deeper baseline. Finish that work package with saved parties and a fresh reliability result, including an explicit failure if necessary.

Use the existing simulator, legal-party construction, mutation operators, compact archives, resumable execution and acceptance evaluator. Add one optional search kernel and a reusable benchmark workflow. Avoid another chain of separate provider-rule assessments, implementations and almost identical pilots.

Start the comparison from the unchanged **v4 coverage policy**. It is the simplest policy with independent winners at the current Kharad setting, although its replication failed. That makes it a defensible baseline, not a proven best method. Keep v5–v11 available for historical reconstruction; their accumulated additions have not earned default promotion. Selecting a baseline from published method performance does not authorize copying its saved winning recipes or features into generation.

## What the evidence establishes

The [active milestone log](Tower-Coverage-Replication-Plan.md) records eight separately closed comparisons after the original v4 pilot: replication and v5–v11. Each used 9,224 fights and failed the declared reliability gate at 0/3. The separate 6,010-fight control confirmation brings this continuation's total to 79,802 fights. These totals are resource accounting, not pooled statistical evidence.

The bottleneck is more specific than “we need more useful Essences”:

| Observation | Consequence for the next decision |
| --- | --- |
| Each recent arm evaluates 96 complete parties on eight discovery seeds. The elite beam has four members, with up to four exploration choices. | The experiment tests a shallow search allocation; it does not establish the limit of the existing operators. |
| In the latest six arms, all 576 candidates have zero discovery wins **and zero end-of-battle survival**. Victory-duration fitness is also constant for losses. | The lexicographic objective effectively ranks these candidates by remaining guardian health, with recipe ID resolving exact ties. Other measured combat behavior does not rescue a candidate's main rank. |
| Each latest arm has 42 accepted fresh starts and 54 accepted descendants. New-method primaries have ancestry depths 1, 3 and 8. | Many independent starting points receive little refinement. This is a measured trajectory property, not proof that a particular depth would succeed. |
| V11's four distinct elite loadouts were actually selected, and all six new finalists retained nominal control capacity. | Duplicate elite recipes and missing nominal control do not explain away the failure. Different recipes can still occupy the same narrow region of combat behavior. |
| Saved controls still win at the same fixed budget; the strongest latest result is 131/256 (51.17%). | Competitive parties exist. Weak generated parties do not justify lowering boss difficulty. The observed ceiling breach remains unresolved. |

The first three rows were checked against the current [search kernel](../LL/tools/BalanceHarness/TowerBossGeneration.cs), [survival definition](../LL/tools/BalanceHarness/TowerBenchmark.cs) and [saved v11 discovery record](../TestResults/balance/tower-loadout-diversity-20260913/discovery/discovery.json). Ancestry depth is zero for a fresh party and one plus the maximum parent depth for a descendant; parent links use proposal identities. The last two rows are supported by the [completed v11 diagnosis](Tower-Loadout-Diversity-Diagnosis-Review.md).

There is relevant contrary evidence to “budget cannot matter”: an [older comparison](Tower-Competitive-Build-Search-Review.md#results-and-verification) found a small independent primary at 22/1,000 and a large independent primary at 1,000/1,000, using 128 versus 512 candidates per arm. That study used an earlier boss setting and a different policy. It supports testing depth; it cannot predict performance at today's Kharad setting.

Eight fights also give coarse feedback. For a fixed party with a true 10% win probability under independent trials, the chance of seeing zero wins in eight is 0.9^8, approximately 43%. Therefore zero discovery wins must not, by itself, eliminate a party or stop a search. This arithmetic does not estimate any archived party's true rate.

## Proposed optimizer

Retain a bounded population of complete legal parties with different **observed** combat behaviors, and let those parties produce descendants over a longer run. An initial design target is at most 32 retained parents, including the existing best outcome-ranked party. Capacity is an engineering choice to freeze before testing, not a measured optimum.

Use a small behavior archive based on existing discovery measurements, such as party health deficit and hostile action denial. Keep the original win/progress ordering within comparable archive entries and for final nomination. Behavior descriptors preserve alternative search paths; they are not new balance targets or weighted rewards for healing and stalling. Measure their units, duration dependence, summon scope, missing values and deterministic tie rules before choosing fixed bins or representative rules. Do not derive thresholds from saved-control recipes or held-out outcomes.

This is related to the established idea of keeping good solutions across different behaviors in [MAP-Elites](https://arxiv.org/abs/1504.04909). Applying that idea here is a hypothesis, not an efficacy guarantee. The existing `TowerBossOptimization.Archive` already contains a smaller measured-behavior representative pattern in another search path; inspect and reuse its concepts rather than introducing a general optimization framework. The current independent `Explore` instead uses authored capability patterns and recipe differences.

For the first comparison, hold construction, legal families, inventory, gear, mutation operators and per-candidate discovery sampling constant against v4. Change parent retention and selection in the new method. Existing whole-character, cross-character, recombination and coverage operators already permit coordinated changes. Do not simultaneously add a recovery quota, new Essence ranking, repair system, learned model or curriculum of easier bosses.

More efficient sampling can follow once a search path works. [Successive resource allocation](https://proceedings.mlr.press/v51/jamieson16.html) provides a useful precedent, but noisy combat outcomes and rare wins require their own policy. Do not assume that paper's guarantees transfer, or bundle adaptive per-candidate sampling into this first depth-versus-selection comparison. Separate discovery, screening and untouched validation already provide a useful first boundary.

## One bounded comparison

The following is a concrete planning envelope, **not a frozen protocol**. Reserve fresh schedules only after implementation and verification. All three methods use the same declared eight discovery combat seeds, paired across methods and restarts; generation randomness remains method/restart-specific. Restarts measure initialization sensitivity, not independent combat schedules.

| Method | Candidates per restart | Purpose |
| --- | ---: | --- |
| A: unchanged v4 | 96 | Reproduce the small search allocation as a contemporary baseline. |
| B: unchanged v4 | 384 | Measure the effect of deeper search without a new selection rule. |
| C: v4 construction/operators with a measured-behavior parent archive | 384 | Compare the new selection strategy against an equally deep baseline. |

Use three restarts per method. Budget contrasts A/B are intentionally unequal; B/C is the equal-budget method comparison. Increasing candidate count also changes v4's initial-construction allocation, so A/B tests the existing policy at a larger budget, not pure extra generations or an exact continuation of A. Report unique recipes, actual fights and wall time as well as the candidate cap.

Freeze two finalists per method/restart using discovery alone, keeping rank one primary and rank two exploratory. Include the same six external controls. That gives at most 24 distinct finalist/control cells. No control enters construction, mutation, the parent archive or discovery fitness.

| Phase | Maximum fights |
| --- | ---: |
| Discovery: (96 + 384 + 384) × 3 × 8 | 20,736 |
| Separate development screen: 24 × 64 fresh seeds | 1,536 |
| Untouched validation, only if the predeclared screen permits it: 24 × 256 | 6,144 |
| Fixed diagnostic/parity repeats | 8 |
| Total maximum | **28,424** |

Suggested operational caps are **30 minutes of combat execution, 2 GiB retained output and zero retries**. These are proposed limits, not runtime forecasts; include reconstruction and documentation time separately in the receipt. Do not partition around existing limits, silently increase contract limits, reassign unused confirmation fights to search, or retry an exhausted run with new seeds. The existing policy contract accepts exact two-method combinations, so a three-method benchmark needs an explicit supported contract/orchestrator and verification; it cannot be passed to the existing command unchanged.

Before execution, specify one deterministic screening rule. A reasonable proposal is to continue only when B or C has at least two discovery-frozen primaries with at least 7/64 screen wins and an observed gap no worse than ten percentage points below the fixed anchor. This is an engineering filter with false-negative risk, **not** a confidence-based pass. Do not replace primaries from screen results. If it fails, stop and report the planned early failure; do not spend the validation reserve merely to repeat that the methods are weak. If it passes, validate the complete predeclared family, not only favorable cells. Both primary/comparator comparisons and all rate cells need the declared family adjustment, including both candidate methods if either can be selected.

The final protocol must settle exact archive behavior, comparator/anchor identities, multiple-comparison allocation, incomplete-run handling and all seed exclusions before its first fight. The current exclusion union remains 472,194 seeds, including unused reservations. This analysis adds none.

## What counts as progress, and how the result changes the plan

The next milestone is **repeatable independent recovery of competitive parties**, not another integrity pass. Continue reporting the existing combined reliability rule: at least two of three primaries need supported viability of at least 10%, supported improvement over the declared comparator, and the declared non-inferiority margin against the fixed anchor. Do not retrospectively relabel historical failures or call a screening threshold a reliability pass. Report viability, comparator improvement and anchor recovery separately so a failure has an actionable explanation.

| Result | Decision |
| --- | --- |
| B reliably succeeds; C supplies no supported improvement | Prefer the simpler existing method with a sufficient budget. The new archive has not earned promotion. |
| C reliably succeeds and improves on B | Carry the new optimizer forward with saved recipes and an explicit search budget. |
| Both still produce weak or zero-win primaries | Reject this work package's remedy. Do not start v13 by changing one provider label. Reconsider representation and proposal reachability, or discuss a deliberately different supplied-build workflow with the user. |
| Viability improves but confidence or reference competitiveness remains unresolved | Report partial search progress. Do not retune or broaden the balance claim on that basis. |

Even success here is a first search milestone. Matching one fixed anchor does not establish that the search challenges the best players. Before renewed calibration, independently challenge the **strongest known control portfolio**, assess additional search budget and an alternative search route, and preserve every discovered above-ceiling party. Those steps provide bounded evidence about search coverage, never proof of a global optimum. Practical acquisition remains unverified.

After that prerequisite is met, use the existing separate calibration and fresh-confirmation workflow on the expanded portfolio, then resume floors 6–11 with the approved Essence-slot progression. Kharad stays at Health 3.04881408 / Power 3.85370128 during search development. Keep fixed gear and untrained/unevolved Essences throughout.

## Deliver the work as one package

1. Implement the optional parent archive and explicit benchmark contract, preserving the old kernels and reference boundary. Cover deterministic retention, legal complete parties, exact recipe/order identity, accounting and interruption behavior through `build/run-tests.ps1`.
2. Reuse the existing execution/archive/evaluation components to provide one repeatable prepare/run/report workflow. Record source/content identity, all seeds, proposals, ancestry, phase decisions and recipes once. Keep full-detail replay bounded.
3. Freeze and execute the comparison; reconstruct it and publish one result with the saved teams, costs and success/failure decision. Implementation correctness alone does not complete this milestone.

Use this document as the strategic recommendation and the [handoff](Tower-Coverage-Replication-Plan.md) as the current status index. Sealed historical reviews remain unchanged. Update the small set of active entry points rather than copying the same long status paragraph into every old plan. The death-dependent recovery assessment is deferred as a local hypothesis; it is no longer the next strategic milestone.

## Verification of this analysis

This turn read repository instructions, current search/measurement/selection code, original handoff, historical comparisons, current protocols and saved discovery evidence. A read-only calculation verified all six arm counts, constant win/survival fields and parent depths; budget and probability arithmetic were checked. Local links and `git diff --check` were verified after the Markdown changes. An initial PowerShell pipeline error and an initial parent-identity lookup error were corrected before obtaining the reported counts; no command remains blocked.

Only this analysis and three active Markdown entry points changed in this turn. No backend tests were rerun because no implementation changed; the historical 264-test result is not a new test result for this proposal. No new battles, constructor calls, seeds, migrations, configuration changes, gameplay changes or deployments were made.
