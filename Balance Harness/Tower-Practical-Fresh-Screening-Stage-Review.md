# Fresh-screening search-stage diagnosis

23 September 2026. Target: offline `LL/tools/BalanceHarness`. **VerifiedFreshScreeningStageReview.** All **24 saved searches** from the [closed comparison](Tower-Practical-Fresh-Screening-Comparison-Execution.md) were reconstructed. The review explains the large gains in pairs 3/5 and losses in pairs 6/11, and includes pair 8 and the seven identical-output pairs. It added **zero fights, values, parties or native preparations**.

The clearest next hypothesis is **positive-tie preference for every retained reference**. The current selector protects only designated primary `399bc776…`. It can therefore choose a challenger tied with confirmed reference `96b94357…`, as happened in pair 3's baseline and pair 11's screened search. This is the existing documented behavior, not an implementation defect. A broader preference requires a separately versioned and prospectively evaluated selector; this review changes no policy.

The fresh-screening experiment remains **`DoNotPromoteFreshScreening`**: +96/12,000 net wins, +0.8 percentage points, three positive pairs and a −0.6424-point conditional lower bound. Those recorded confirmation results identify which realized trajectories to explain. They were not used to rank an alternative selector, compute hypothetical strength gains or pool earlier experiments.

## What the stages show

Both pipelines used 46 discovery recipes, protected the three references and selected among five nominees on the same 32 fresh values per pair. Baseline discovery used eight trials; screened discovery used four, followed by a separate eight-trial screen of three references plus twenty challengers. Every arm still spent 528 search fights.

The arms shared only **20–37 of 46 evaluated recipes** per pair. Their evaluated-recipe sequences first differed at positions **10–21**. Shared recipes reproduced the same first four discovery outcomes, and shared selection recipes reproduced the same complete measurements. The trajectories diverged because the pipelines had different discovery feedback; the experiment cannot isolate screening on an identical candidate set.

Screening retained both original four-trial discovery challengers in **one pair**, one in **five pairs**, and neither in **six pairs**. Thus **17 of 24 final challenger nominations** came from outside that arm's original top two. Their discovery challenger ranks reached **20**. This demonstrates broader nomination, but does not establish better final teams.

| Saved diagnostic | Direct baseline | Fresh screening |
| --- | ---: | ---: |
| Searches reconstructed | 12 | 12 |
| Reference outputs | 9 | 8 |
| Challenger outputs | 3 | 4 |
| Challenger selected while tied with a nonprimary reference | 1 | 1 |
| Challenger nominees strictly above the best reference on selection | 2 | 4 |
| First/last 16-trial selections disagree | 11/12 | 11/12 |
| Searches with at least one leave-one-out output change | 5/12 | 2/12 |
| Changed outputs across the 384 leave-one-out subsets per arm | 43 | 20 |

The fixed half-panel and leave-one-out diagnostics overlap and use fewer than 32 observations. They are sensitivity checks, not independent replications, calibrated error rates or replacement selectors. No zero-win subset occurred. The lower leave-one-out count in the screened arm does not establish superior strength; pair 6's lower-confirmation-output challenger survived every leave-one-out check.

## Pair 3: a reference tie, with discovery and screening both excluding the baseline winner

Baseline challenger `0b22cc0f…` ranked second among challengers with **6/8 discovery wins**. It tied `96b94357…` at **23/32 selection wins**. Designated primary `399bc776…` had 20, so its tie preference did not apply. The challenger appeared earlier in the frozen discovery shortlist and was selected.

The same challenger existed in candidate discovery, scoring **3/4** at challenger rank **3**, already outside that arm's ordinary top two. It reached the broader screen, scored **6/8**, and finished at screening challenger rank **5**, again outside the final two. Candidate nominees ranked fourth and eleventh in discovery; neither beat the protected reference's **23/32** at final selection. The candidate therefore selected `96b94357…`.

Recorded confirmation was **658 versus 763 wins**, a +105 difference for the candidate. The saved path explains which decisions differed, but does not attribute the gain solely to fresh screening: the shorter discovery stage had already excluded the baseline winner from direct nomination. Eight baseline leave-one-out panels changed the output; no candidate leave-one-out panel did.

## Pair 5: changed discovery nomination, followed by a strict selection lead

Baseline winner `e93ee623…` scored **8/8 discovery**, ranked first among challengers and won selection **25/32**, one win above the best references at 24. It was **absent from the candidate's realized discovery population**.

Candidate winner `1b449348…` existed in both populations. Baseline discovery gave it **7/8**, challenger rank **3**, so it never received baseline selection trials. Candidate discovery gave it **3/4**, challenger rank **2**; it was already an ordinary discovery nominee before screening. It then scored **8/8 screening** and **28/32 selection**, four wins above the best references.

This was the only pair where screening retained both original discovery challengers. It changed their ordering, but did not introduce either finalist. Recorded confirmation was **590 versus 725**, a +135 difference. This supports a diagnosis involving discovery feedback and nomination as well as final selection; it does not identify screening width as the cause. The baseline's missing selection score for `1b449348…` remains unknown in that arm's record. Its candidate score comes from the actual separately measured candidate search, not an invented rerun.

## Pair 6: broad screening admitted a challenger that won selection outright

Candidate winner `298ab0d1…` scored **2/4 discovery** and ranked **20th among challengers**, exactly at the screening boundary. It scored **7/8 screening**, ranked second among screened challengers and advanced to selection. It was **absent from baseline discovery**.

Reference `96b94357…` scored **8/8 screening**, remained protected and scored **24/32 selection** in both arms. The candidate challenger scored **26/32**, a strict two-win lead. Baseline selected the reference; candidate selected the challenger. Recorded confirmation was **762 versus 681**, a −81 difference.

This loss cannot be addressed by changing only equal-win tie preference. The reference was available at every stage and lost the final count comparison. Both 16-trial halves selected references, yet the full panel selected the challenger and none of its 32 leave-one-out panels changed that decision. Small-panel sensitivity and full-panel correctness are different questions; no selection-margin threshold is fitted here.

## Pair 11: screening nomination followed by a nonprimary-reference tie

Candidate winner `b7277336…` was absent from baseline discovery. In candidate discovery it scored **3/4**, challenger rank **5**. The screen gave it **6/8**, challenger rank **2**, promoting it into the final five. It tied `96b94357…` at **25/32 selection wins**. Primary `399bc776…` had 20, so the earlier screening nominee won the tie.

Baseline selected `96b94357…` with the same **25/32** observations. Recorded confirmation was **769 versus 698**, a −71 difference. Nine candidate leave-one-out subsets changed its output. This is the candidate-side counterpart of pair 3's baseline reference tie, with screening order supplying the rank tie-breaker.

## Pair 8 and complete coverage

Pair 8 added **eight net wins**: 774 versus 782. Both selected challengers were absent from the opposite realized discovery population. The candidate's `7c2fe15b…` moved from discovery challenger rank **9**, with **2/4 wins**, to screening challenger rank **1**, with **6/8**, then won selection **26/32**. Its baseline counterpart `c8f0446c…` won selection **24/32**. The other seven pairs selected identical outputs and remain in the original denominator.

The [full review](../TestResults/practical-fresh-screening-stage-review-20260923/review.json) retains every evaluated recipe's operator, ancestry, discovery rank, screening status, nomination, selection score and output status, plus both output paths through the opposite arm. Missing screening and selection measurements are explicitly `null`. No conclusions are assigned to unmeasured alternatives or absent recipes.

## Recommended next step

The [prospective design](Tower-Practical-Three-Reference-Tie-Comparison-Plan.md) and [implementation](Tower-Practical-Three-Reference-Tie-Implementation.md) are now complete, with 241 backend and 18 Python tests passing. The [captured-runtime admission](Tower-Practical-Three-Reference-Tie-Admission.md) is now complete. The subsequent [single prospective comparison](Tower-Practical-Three-Reference-Tie-Comparison-Execution.md) is now verified and closed as **`NoSelectorDifferences`**; the [subsequent saved-stage review](Tower-Practical-Three-Reference-Tie-Stage-Review.md) explains the unchanged outputs and recommends independent confirmation of five strict challenger leaders against all three references. The recommendation below records this earlier review's motivation.

Prepare a narrow, separate **three-reference positive-tie selector** comparison on the existing direct-nomination baseline. Preserve designated-primary preference. When the primary is not tied for the positive maximum but another retained reference is, prefer the first such reference in the already frozen nominee order over a tied challenger. Preserve strict-win leaders and the current zero-win behavior. This adds no search fights and does not require choosing a new primary or changing the generator.

The hypothesis is that a challenger should show a strict observed win-count lead before replacing a confirmed reference. It addresses the demonstrated tie mechanism; it does **not** solve pair 5's one-win baseline lead or pair 6's two-win screened lead. Establish the version, exact behavior, tests and prospective acceptance rule before any fresh allocation. Do not reopen or extend the failed screening experiment, tune a margin from its confirmation outcomes or claim the new rule would have produced a verified strength improvement.

## Verification and accounting

The [analyzer](analysis/practical-fresh-screening-stage-review.py) authenticated the scientific archive, admission package and external execution evidence; reconstructed all discovery shortlists, 23-member screening freezes, five-member nominations and selected outputs; validated saved trial IDs against the reserved stage panels; checked shared observations and generated-parent ancestry; and reproduced the recorded paired confirmation counts. Existing native and independent scientific audits remain the underlying battle/fitness verification. This review did not run either game executable.

It then scanned the entire permanent reservation history, reproducing **600,552 exclusions across 238 files**, and rechecked all three input packages' memberships and hashes. The [review receipt](../TestResults/practical-fresh-screening-stage-review-20260923/review.json), [input pins](../TestResults/practical-fresh-screening-stage-review-20260923/input-manifests.json), [history inventory](../TestResults/practical-fresh-screening-stage-review-20260923/history-files.json) and [helper pins](../TestResults/practical-fresh-screening-stage-review-20260923/helper-pins.json) preserve those checks. The measured analysis took **68.547 seconds**.

**Eleven diagnostic tests passed**, covering the literal tie and strict-lead cases, missing later-stage measurements, changed membership/nomination/output/ancestry, incomplete or nonboolean observations, wrong fitness, altered stage boundaries, reused or misaligned panels and shared discovery prefixes. The [test source](analysis/test-practical-fresh-screening-stage-review.py), [final test log](../TestResults/practical-fresh-screening-stage-review-20260923/tests.log) and [analysis log](../TestResults/practical-fresh-screening-stage-review-20260923/analysis.log) are retained. Publication also checks report figures, local links, Python syntax, scoped whitespace, the frozen plan and all 202 producing source documents.

```powershell
python -B -X utf8 'Balance Harness/analysis/test-practical-fresh-screening-stage-review.py'
python -B -X utf8 'Balance Harness/analysis/practical-fresh-screening-stage-review.py'
```

Python used the bundled runtime. These commands record completed work; analysis refuses to overwrite its review receipt. No required command remains blocked. Backend tests were not repeated because no C# or captured runtime changed. Engineering remains separately disclosed: this measured analysis time is not a complete engineering total, older incomplete totals remain unknown, and the **18,180-second /13,584-MiB** prior ledger is unchanged.

The [publication verification](../TestResults/practical-fresh-screening-stage-review-20260923/verification.json) binds the final documentation and helper sources. Changed repository files are this report, the read-only analyzer and its tests, and seven documentation follow-ups. Search defaults, confirmed-team recommendations and the captured cohort's separate balance failure are unchanged. No application configuration changes, migrations, database actions or deployments occurred.
