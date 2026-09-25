# Three-reference tie comparison: saved-stage diagnosis

**Confirmation verification and accounting — 23 September 2026:** The [complete verifier improvement and prospective resource amendment](Tower-Practical-Three-Reference-Confirmation-Resource-Amendment.md) are ready. Full input verification measured 55.94 seconds with the original helper and 18.41 seconds with the new helper; all 30 relevant tests pass. The proposed 8,400-second /4.5-GiB cumulative allowance includes the failed admission and one new admission. Next is explicit v2 accounting implementation. No new admission, preparation, reservation or combat ran.

23 September 2026. Target: offline `LL/tools/BalanceHarness`. **VerifiedThreeReferenceTieStageReview.** All **24 searches** from the [closed selector comparison](Tower-Practical-Three-Reference-Tie-Comparison-Execution.md) were reconstructed. Its `NoSelectorDifferences` result is explained by the actual selection matrices: **14 unique reference leaders, 5 unique challenger leaders, 3 primary-protected ties and 2 ties already favoring another reference**. The review added **zero fights, values, parties or native preparations**.

The next useful step is **independent fixed-family confirmation of all five selected challengers against all three retained references**. Every selected challenger led the best reference by exactly **one win out of 32**. Their absolute confirmation results are unmeasured because both selectors chose the same recipe. Those one-win leads do not establish either useful improvement or selection error. Measure the existing candidates before using them to justify a wider selection margin.

## Why the broader tie rule made no difference

| Selection situation | Roots | Explanation |
| --- | ---: | --- |
| Unique reference leader | 14 | Both selectors preserve strict leaders. |
| Unique challenger leader | 5 | Every challenger led the best reference by one win; neither tie rule applies. |
| Positive tie including the designated primary | 3 | Both selectors preserve the primary. |
| Positive challenger/nonprimary-reference tie | 2 | The reference already precedes the challenger in frozen nominee order. |

At roots **2 and 24**, reference `96b94357…` tied a challenger at **21/32 and 27/32**, respectively. The primary was below the maximum, so these were relevant opportunities for the broader reference preference. In both cases the existing order already selected that reference. Thus **two relevant ties occurred, but zero baseline choices needed changing**. This is different from the tie condition never occurring.

At roots **11, 14 and 23**, the designated primary `399bc776…` tied the maximum at **26, 23 and 28 wins**, respectively. Its existing preference already handled those ties. Fourteen other roots had a unique reference leader. Reference `96b94357…` was selected **11 times**, the primary **8 times**, and reference `8287f779…` **zero times**. These selection frequencies do not supersede the three previously confirmed team recommendations.

## The five unconfirmed strict leaders

The [candidate inventory](../TestResults/three-reference-tie-stage-review-20260923/selected-challengers.json) includes every selected challenger in source-root order, with full party IDs, recipe hashes and authenticated checkpoint paths. It creates no new recipe and contains no allocated request. The comparison measured no confirmation battles for these recipes; the scores below are selection observations only.

| Root | Selected challenger | Selection wins /32 | Best reference /wins | Paired gains /losses | First /last half selected output | Leave-one-out changes |
| --- | --- | ---: | --- | ---: | --- | ---: |
| 8 | `c4ef379d…` | 26 | `399bc776…` /25 | 6 /5 | `399bc776…` /`c4ef379d…` | 6/32 |
| 13 | `8a0359aa…` | 23 | `8287f779…` /22 | 9 /8 | `6842e008…` /`8287f779…` | 7/32 |
| 15 | `7135da35…` | 25 | `96b94357…` /24 | 6 /5 | `96b94357…` /`7135da35…` | 0/32 |
| 21 | `4286b24f…` | 24 | `399bc776…` /23 | 8 /7 | `399bc776…` /`4286b24f…` | 14/32 |
| 22 | `51de5c79…` | 28 | `96b94357…` /27 | 5 /4 | `51de5c79…` /`51de5c79…` | 0/32 |

“Best reference” here means the largest selection win count, retaining the original primary on a tied reference maximum and otherwise using frozen nominee order. All three reference comparisons remain in the machine-readable review. For example, root 13's references `8287f779…` and `96b94357…` both scored 22; the table uses the former because it occurs first. Its challenger had paired gains/losses **9/8** against that reference and **8/7** against the other. No unique best reference is inferred.

Across these five roots, **4/5 first/last 16-trial selections disagree**, and **3/5 roots change under at least one leave-one-out panel**, with **27 changes across 160 overlapping subsets**. Root 15 has disagreeing half-panel outputs but no leave-one-out change: removing one challenger-only win creates a tie where the challenger still precedes the nonprimary reference. Root 22 is unchanged under both diagnostics. Stability under these subsets is not evidence of independent strength, so neither root is singled out for promotion or omitted from confirmation.

Across all 24 roots, the corresponding figures are **15/24 half-panel disagreements**, **9/24 roots with a leave-one-out change**, and **68 changes across 768 overlapping subsets**. No subset was unassessable. These fixed diagnostics reuse selection observations; they are neither independent trials nor confidence estimates, and no margin threshold was fitted from them.

## Discovery and nomination coverage

The common direct search evaluated **46 recipes per root**: three supplied references and 43 challenger occurrences. It retained all three references and two discovery-ranked challengers for the 32-value selection panel. All **1,104 evaluated occurrences** and **120 selection rows** were reconstructed from their recorded trials and reserved stage panels.

Of **1,032 challenger occurrences**, only **48** received selection measurements. The other **984** remain unmeasured at selection; **33** were excluded while tied on discovery wins at the challenger cutoff, and **951** were below it. The cutoff had more than one tied challenger in **20/24 roots**. Discovery's first and second challengers reversed their relative selection win counts in **11/24 roots**. Four of the five selected challengers were the second discovery-ranked challenger. These observations identify coarse nomination feedback, but do not prove the excluded recipes were stronger or justify reopening the failed screening comparison.

Among the 48 challenger nominees, **5 scored above**, **5 tied**, and **38 scored below** the best reference on selection. These are nominee occurrences, not 48 independent team comparisons. The five tied nominees are not all maximum ties: in some roots another challenger leads, as at roots 13 and 21.

## Complete selection census

R1 is `399bc776…`, R2 is `8287f779…`, and R3 is `96b94357…`. C1/C2 are the two challengers in frozen discovery-rank order. Scores are wins out of 32. The top gap compares the highest and second-highest nominee counts; leave-one-out changes use the existing primary-tie selector. Full identities, paired counts, proposal positions and unmeasured fields are retained in [review.json](../TestResults/three-reference-tie-stage-review-20260923/review.json).

| Root | R1 /R2 /R3 | C1 /C2 | Selected | Explanation | Top gap | Leave-one-out changes /32 |
| --- | --- | --- | --- | --- | ---: | ---: |
| 1 | 21 / 19 / 27 | 23 / 19 | R3 | Unique reference | 4 | 0 |
| 2 | 20 / 20 / 21 | 19 / 21 | R3 | Other reference first | 0 | 10 |
| 3 | 20 / 19 / 24 | 19 / 15 | R3 | Unique reference | 4 | 0 |
| 4 | 23 / 20 / 18 | 17 / 14 | R1 | Unique reference | 3 | 0 |
| 5 | 26 / 20 / 28 | 21 / 23 | R3 | Unique reference | 2 | 0 |
| 6 | 21 / 23 / 28 | 20 / 14 | R3 | Unique reference | 5 | 0 |
| 7 | 26 / 24 / 25 | 21 / 19 | R1 | Unique reference | 1 | 0 |
| 8 | 25 / 20 / 23 | 20 / 26 | C2 | Unique challenger | 1 | 6 |
| 9 | 21 / 21 / 27 | 22 / 24 | R3 | Unique reference | 3 | 0 |
| 10 | 24 / 21 / 29 | 17 / 26 | R3 | Unique reference | 3 | 0 |
| 11 | 26 / 23 / 26 | 23 / 21 | R1 | Primary tie | 0 | 4 |
| 12 | 25 / 22 / 23 | 13 / 24 | R1 | Unique reference | 1 | 0 |
| 13 | 21 / 22 / 22 | 22 / 23 | C2 | Unique challenger | 1 | 7 |
| 14 | 23 / 23 / 22 | 22 / 21 | R1 | Primary tie | 0 | 6 |
| 15 | 19 / 19 / 24 | 19 / 25 | C2 | Unique challenger | 1 | 0 |
| 16 | 21 / 21 / 24 | 18 / 17 | R3 | Unique reference | 3 | 0 |
| 17 | 24 / 17 / 22 | 21 / 21 | R1 | Unique reference | 2 | 0 |
| 18 | 27 / 15 / 26 | 24 / 20 | R1 | Unique reference | 1 | 0 |
| 19 | 23 / 23 / 24 | 22 / 18 | R3 | Unique reference | 1 | 12 |
| 20 | 22 / 20 / 26 | 20 / 17 | R3 | Unique reference | 4 | 0 |
| 21 | 23 / 21 / 20 | 23 / 24 | C2 | Unique challenger | 1 | 14 |
| 22 | 24 / 18 / 27 | 28 / 23 | C1 | Unique challenger | 1 | 0 |
| 23 | 28 / 25 / 26 | 19 / 28 | R1 | Primary tie | 0 | 4 |
| 24 | 22 / 21 / 27 | 21 / 27 | R3 | Other reference first | 0 | 5 |

## Recommended next scope

The [separate prospective confirmation design](Tower-Practical-Three-Reference-Confirmation-Plan.md) is now frozen: **five exact challengers plus three exact controls**, 6,500 values each, 52,000 fights and all fifteen contrasts in family 38. Implementation and captured-runtime admission are next. Its candidate order is roots **8, 13, 15, 21, 22**, followed by the existing three-reference order. The recommendation below records this review's motivation. Use their unchanged captured floor-5, ten-character, level-40 recipes and one fresh common panel across all eight teams. Include all **15 candidate/reference contrasts** and report every candidate that meets the predeclared practical strength requirements against every reference. Preserve the designated primary and report qualifiers in frozen order.

The existing [fixed-family controller](../LL/tools/BalanceHarness/TowerFixedFamilyConfirmation.cs) is pinned to **six candidates and two controls**, even though that also totals eight recipes. Its loops, exact team hash and family size cannot be reused unchanged for five candidates and three controls. If the same win-rate plus gain/loss interval construction is retained, the declared quantity count becomes **8 +2×15 =38**, replacing the old family 32. Sample size, conditional power, resource forecast, admission allowance and exact source bindings need a new design before implementation or execution. The prior [six-candidate study](Tower-Practical-Fixed-Family-Confirmation-Execution.md) remains closed.

This follow-up would answer whether these fixed teams offer practical improvement in the captured cohort. It would not establish the search method's general reliability. A subsequent selector proposal can use the diagnosis as motivation, but its strength must be tested prospectively on fresh roots; confirmation outcomes must not be reused as validation for a margin fitted to these five cases. Keep the current selector and team recommendations while that evidence is absent.

## Verification and accounting

The [analyzer](analysis/three-reference-tie-stage-review.py) authenticated the scientific archive, admission and external execution evidence; reconstructed all discovery nomination orders, supplied recipes, both selectors and saved endpoint identities; checked exact trial partitions and reserved 41-value root blocks; resolved generated ancestry; and computed paired selection counts and fixed sensitivity diagnostics. Native generation/preparation and battle replay remain established by the two closed scientific audits. This review invoked no game executable.

**Nine tests passed**, covering the five strict leaders, both nonprimary ties, three primary ties, unmeasured fields, zero-win subset handling, changed output/recipe/nomination/control/ancestry, invalid measurements and wrong reserved panels. [Tests](analysis/test-three-reference-tie-stage-review.py), [test log](../TestResults/three-reference-tie-stage-review-20260923/tests.log), [analysis log](../TestResults/three-reference-tie-stage-review-20260923/analysis.log) and [publication verification](../TestResults/three-reference-tie-stage-review-20260923/verification.json) are retained. No C# changed; all 203 producing source documents still match admission, so backend tests were not repeated. No required command was blocked.

The review and full history scan took **53.297 seconds**. History remains **633,313 exclusions across 240 files**. All three input archives were authenticated again after the review. Engineering is separately disclosed under the accepted accounting treatment; the **18,180-second /13,584-MiB** older ledger is preserved and complete historical engineering totals remain unknown. This elapsed review duration is not a complete engineering total.

| Input | Manifest SHA-256 |
| --- | --- |
| Closed scientific comparison | `68f7468ba270f1d51a075772a6fed3d800ad627a1376a00c4829c10ef9d806d1` |
| Captured-runtime admission | `90b9a5a9815454ba1169d64f61cc8b7a950597cfa56bc1c9380c53155538f1c5` |
| External execution evidence | `f05051cf90896bb9d9497a07b32ca207c996886608daa3579b828b1f53bc9507` |

Completed commands used bundled Python. The analyzer writes a new external review receipt and refuses to overwrite it.

```powershell
python -B -X utf8 'Balance Harness/analysis/test-three-reference-tie-stage-review.py'
python -B -X utf8 'Balance Harness/analysis/three-reference-tie-stage-review.py' --output 'TestResults/three-reference-tie-stage-review-20260923'
python -B -X utf8 'TestResults/three-reference-tie-stage-review-20260923/verify.py'
```

Changed repository files are the analyzer and its tests, this report and six documentation pointers. Defaults, three confirmed recommendations, the frozen comparison protocol and the captured cohort's separate balance failure remain unchanged. No configuration, migrations, database operations, deployments or infrastructure changes occurred.
