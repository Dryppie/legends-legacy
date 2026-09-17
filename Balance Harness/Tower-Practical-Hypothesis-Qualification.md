# Practical Tower search: hypothesis qualification after native verification

17 September 2026. Target: offline BalanceHarness source and retained evidence. **No search change qualifies for implementation or a quality experiment yet.** The completed workflow and allocation recovery remove the identified integration gaps. They do not establish that another constructor, operator or allocation policy will improve independently measured teams.

This review adds a concrete examination of the two completed incumbent-preserving runs. It reconstructs their saved nomination and selection arithmetic, discovery distributions and available confirmation coverage. The [read-only script](analysis/practical-hypothesis-qualification.py) and [results with source hashes](Tower-Practical-Hypothesis-Qualification.json) reproduce the findings. No experiment, model fit, candidate generation, native reconstruction or gameplay execution is involved. The two runs remain separate observations; their outcomes are not pooled into an improvement or reliability estimate.

**Subsequent evidence:** the [four-nominee diagnostic](Tower-Practical-Selection-Diagnostic-Execution-Review.md) completed 4,640 fights and both audits, returning `NoSelectionMissDemonstrated`. Its new frozen primary won 712/1,000 versus anchors 632/1,000 and 628/1,000, with 598/1,000 for its other challenger. This later pool does not fill the missing confirmation cells in the two historical runs analyzed below. The [adoption review](Tower-Practical-Primary-Adoption-Readiness.md) keeps Hold, and the [fixed-team plan](Tower-Practical-Fixed-Team-Confirmation-Plan.md) specifies the [implemented and fixture-tested controller](Tower-Practical-Fixed-Team-Confirmation-Implementation-Review.md); the subsequent [completed fixed-team confirmation](Tower-Practical-Fixed-Team-Confirmation-Execution-Review.md) completed 16,500 trials and both audits, returning AdoptFixedTeam for exact candidate `399bc776…`. The earlier diagnostic retains its original endpoint. No new search algorithm is qualified. The original analysis/JSON and the then-proposed 1,024-trial illustration below retain their historical scope.

## What the additional evidence establishes

At this analysis step, the native run's producing source pins matched the then-current [supplied search](../LL/tools/BalanceHarness/TowerSuppliedCompositionSearch.cs#L145), [discovery rank](../LL/tools/BalanceHarness/TowerBossGeneration.cs#L196), [stage selection](../LL/tools/BalanceHarness/TowerBossStudyPolicy.cs#L49) and [zero-win tie rule](../LL/tools/BalanceHarness/TowerZeroWinSelection.cs#L7). The current kernel still evaluates both supplied anchors, six initial fresh proposals, then a fixed mixture of fresh construction and five mutation/recombination operators. Parents mostly come from the top four discovery rows, with explicit anchor access. Fixed canonical ability order remains outside the tuning space.

Observed discovery accounting:

| Saved run | Evaluated /proposed parties | Fresh parties | Fresh discovery wins | Fresh discovery fights /512 | Other evaluated descendants with fresh ancestry |
| --- | ---: | ---: | ---: | ---: | ---: |
| 16 September incumbent pilot | 64 /67 | 20 | 0/160; every party 0/8 | 160 /512 (31.25%) | 0 |
| 17 September native workflow | 64 /70 | 21 | 0/168; every party 0/8 | 168 /512 (32.81%) | 3, all 0/8 |

The remaining evaluated parties include the anchors and their descendants. The pilot had three duplicate proposals; the native run had three duplicates and three duplicate-family rejections. Both reached their 64-candidate budget well below the 256-proposal ceiling. **Proposal exhaustion was not the observed bottleneck.** This says nothing about all possible construction roots.

Fresh construction had no observed winning output or winning descendant in these two pools. That makes its practical return a reasonable question. These shared-panel, adaptively generated observations are not independent Bernoulli trials or a randomized operator comparison. Different operators receive different parents and opportunities. The totals cannot estimate the improvement from removing exploration, choose a new fresh/edit ratio, or establish that exploration never helps. The earlier [fresh-first trajectory](Tower-Fresh-First-Trajectory-Review.md) also does not authorize another ratio adjustment.

The four nominees and their separate stage measurements were:

| Run /nominee | Full discovery rank | Discovery wins /8 | Selection wins /32 | Confirmation wins /256 |
| --- | ---: | ---: | ---: | ---: |
| Pilot selected `a0f9ffe3bccc` | 1 | 7 | 26 | 167 |
| Pilot other challenger `5645554b96d2` | 2 | 7 | 20 | Not measured |
| Pilot anchor 040e | 26 | 4 | 23 | 161 |
| Pilot anchor 49f6 | 20 | 4 | 23 | 161 |
| Native selected `0ca7c00b2918` | 2 | 5 | 26 | 169 |
| Native other challenger `897432640ced` | 1 | 6 | 25 | Not measured |
| Native anchor 040e | 8 | 4 | 22 | 155 |
| Native anchor 49f6 | 27 | 2 | 20 | 150 |

All membership and selection decisions match the frozen rules. Both anchors remain eligible despite their discovery ranks. That directly addresses the earlier [retained pilot's omission of strong incumbents](Tower-Retained-Practical-Pilot-Execution-Review.md); the same nomination defect is not still outstanding.

In the native run, the selected challenger had **six paired selection gains and five losses** against the other challenger: a one-win margin in 32 trials. The pilot's corresponding counts were nine gains and three losses. Neither choice depended on the zero-win tie-break, so another zero-win tie change would not change these recorded selections. The native rank reversal is compatible with sampling variation, but does not prove that the selected candidate was the worse choice.

Only three of each run's 64 evaluated parties were confirmed. The other **61**, including the unselected challenger, have no confirmation outcome. Consequently the records cannot identify the strongest member of either complete pool, quantify selection regret, or determine whether more accurate selection would improve the final result. In particular, **25/32 does not imply a higher true rate than the selected party's 169/256**: those are different stages, panels and sample sizes.

Both selected parties were produced by recombination. The pilot selected party differs from 040e by three of 50 Essence assignments on two owners; the native selected party differs from 49f6 by three assignments on two owners and from 040e by four on three owners. Coordinated changes are therefore representable and occurred. This does not establish that the available operators cover every useful interaction or cross low-fitness intermediate states reliably.

The existing independent audits remain authoritative for strength: **ImprovementNotDemonstrated** in both runs. Native observed gains of 5.47 and 7.42 points still have negative adjusted paired lower bounds. A failed gate is neither proof of no true gain nor evidence that the selector must be broken. Confirmation was not used to replace either primary.

## Qualification of the plausible directions

| Direction | Evidence supporting investigation | Missing causal or decision evidence | Decision |
| --- | --- | --- | --- |
| Change fresh construction or its budget share | Around one third of discovery fights went to fresh parties with zero observed wins in each inspected run; no winning fresh descendant appeared. | No distinct replacement constructor or equal-budget counterfactual has been justified. Adaptive logs do not identify the benefit of reallocating those fights or the cost of losing exploration. | Low observed yield is established locally; a new search policy is not qualified. Do not tune the ratio from these results. |
| Improve selection precision or redistribute stage effort | Native finalists reversed discovery order, with only a 26–25 selection margin. | The runner-up has no independent confirmation. We do not know whether final selection discarded a better party, or whether the pool lacked a useful improvement. Earlier independent feedback/allocation protocols test different contracts and do not supply this missing outcome. | Highest-value uncertainty to resolve before changing the practical selector. This is a measurement question, not proof that extra selection fights help. |
| Add coordinated mutation, reuse the archive model, or repeat a mechanism swap | The representation supports coordinated edits, and source timing offers possible mechanism leads. | [Block search](Tower-Block-Search-Closure.md) failed its quality gate; the [archive model](Tower-Archive-Model-Execution-Review.md) failed its frozen ranking gate; the [Seal review](Tower-Second-Seal-Mechanism-Design-Review.md) lacks a justified complete replacement and its opportunity costs. This review supplies no contradictory evidence. | Keep those proposals closed. Do not refit on the new native pool or rename a stopped variant. |

The most defensible inference is that construction yield and measurement quality remain entangled. The evidence does not isolate a winning intervention. Another optimizer would be premature, and another integration wrapper would not resolve this scientific uncertainty.

## Smallest useful future observation

If a further scientific scope is chosen, first design a **prospective selection diagnostic using the unchanged practical policy**. Its narrow question would be: does the frozen selection primary lose to another already nominated party on independent evaluation? This can resolve the last selection step; it cannot establish that discovery retained the best of all 64 candidates or that a new search method is reliable.

The minimum proposed scope is one future search with the same cohort, anchors, legal pool, canonical order, proposal limits, 64-by-eight discovery and four-by-32 selection. Freeze the original primary and **all four nominees** before independent outcomes are opened. Independently measure those four on one new, prospectively specified paired panel. Keep the selected primary fixed for reporting; do not replace it with the best confirmed nominee. This would be a new diagnostic, never an extension or replay of either closed run or a test of their old runner-ups on reserved values.

For a concrete decision specification, retain all four recipe-rate quantities and the gain/loss quantities for the primary versus each of the other three nominees: a fixed family of **10** quantities. Use the existing conservative Wilson-difference construction, with its prospective multiplicity setting. A positive diagnostic would require at least one other nominee to have supported viability of at least 10%, an observed gain of at least five points against the frozen primary, and a strictly positive adjusted paired lower bound. It would establish a missed useful nominee on that run, **not adoption of another selector or a general reliability result**. A zero/negative result can be inconclusive; it must not be presented as proof that selection is adequate.

This is a design lead, **not launch-ready work**. Before implementation or allocation, specify the independent sampling model, the true alternative and discordance at which the diagnostic must have adequate power, and a fixed sample count that meets a prospective target such as 80%. The five-point observed gate is not itself an 80%-power alternative. Do not choose sample size or effect assumptions from the unconfirmed candidate's 25/32 score. If no useful question can meet the practical cost and precision requirements, stop the diagnostic proposal here.

For accounting only, four distinct nominees require **640 + 4N** fights including discovery and selection: 1,664 at `N=256`, or 4,736 at a planning ceiling of `N=1,024`. These figures specify neither adequate precision nor granted capacity. A separate cumulative wall/storage envelope must cover admission, search, all confirmation, publication and audit. The observed 1,408-fight native workload used 166.547 operational seconds; it is not a throughput model for 4,736 or 12,032 fights. Its full 600-second /256-MiB allowance is closed and unavailable.

Stop a future diagnostic at its frozen end or any validity/resource boundary. No extra panel, nominee, ratio change, new endpoint, or automatic policy adoption follows either result. A supported selection miss would justify designing one explicit selector change and an equal-information/equal-cost comparison. It would not authorize that comparison. The larger [three-restart quality proposal](Tower-Practical-Evaluation-Design.md) remains **no-go** because its distinct intervention, sampling model and resource feasibility are still missing; the conditional +18-point power calculation supplies none of them.

## Verification and preservation

The reader verifies selected source hashes through the sealed native execution manifest and its pinned predecessor manifest, checks aggregate saved study consistency, reconstructs nomination and selection, recounts confirmation outcomes on their recorded paired panels, and checks agreement with the retained independent audits. It checks the same hashes again after reading. This is file integrity and arithmetic inspection, **not new native authentication**. Four inspected search-source files match the producing native source pins. No general cross-build equivalence is inferred.

Run from the checkout with Python 3; stdout reproduces the JSON artifact:

```text
python -B "Balance Harness/analysis/practical-hypothesis-qualification.py"
```

Verification includes Python syntax, exact JSON reproduction, a separate saved-matrix count/selection check, changed-document local links and whitespace, and preservation of unrelated dirty files and sealed manifests. No backend build/test, benchmark, combat, replay, native audit, seed derivation or reservation command ran in this source/evidence-only follow-up. No required inspection is blocked. The preceding 316 passing backend cases retain their engineering scope and were not rerun.

Changed files are this review, the read-only analysis script and JSON, plus links/current conclusions in the assessment, evaluation design, readiness, practical guide and README. No gameplay or backend implementation, migration, persistent configuration or deployment changes. **484,285** exclusions, both recommended anchors, V19's **512 unused values /253 required recipes /Unresolved**, and adoption **Hold** remain unchanged. No historical allowance is reopened and no experiment is queued.
