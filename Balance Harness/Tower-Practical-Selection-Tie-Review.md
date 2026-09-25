# Practical Tower selection tie audit

**22 September follow-up:** The optional selector is implemented; see [configuration, compatibility and verification](Tower-Practical-Incumbent-Tie-Implementation.md). Its separately declared [prospective comparison completed](Tower-Practical-Incumbent-Tie-Comparison-Execution.md), with both audits passing and **SupportsIncumbentTieForFrozenOutputs** (+2.575 points; conditional lower bound +1.930). This retrospective audit's historical evidence and conclusions remain unchanged. Defaults remain unchanged and the prospective scope is closed.

17 September 2026. Target: offline BalanceHarness. **VerifiedRetrospectiveSelectionAudit.** All **12 selected outputs** from the two completed paired comparisons, plus the earlier selection diagnostic's primary, match an independent reconstruction of the existing selector. Three comparison outputs had a tie for the highest selection win count. A hypothetical rule retaining the designated supplied primary on positive win ties would change **two outputs**, whose observed confirmation scores would improve by **7.9** and **2.8 percentage points**. This is an outcome-informed hypothesis, not prospective evidence that the rule improves future searches.

The engineering step identified by this audit was an **optional, separately versioned incumbent tie policy**, with an explicit primary designation frozen before search; its subsequent implementation is linked above. The default selector, incumbent search default, confirmed `399bc776…` recommendation and both closed comparison decisions remain unchanged. This audit allocated **zero values**, generated **zero parties**, prepared **zero native parties** and ran **zero fights**.

## Evidence and scope

The main cohort includes every output from the [racing allocation comparison](Tower-Practical-Search-Allocation-Comparison.md) and the [anchored-neighborhood comparison](Tower-Practical-Anchored-Neighborhood-Comparison.md): six outputs from each, comprising three paired restarts per comparison. Each received the same two supplied recipes, confirmed `399bc776…` and Anchor040e (`8287f779…`), four nominees and 32 selection trials. The six restart panels are disjoint; the two methods within a restart share confirmation values. The 12 outputs are therefore not 12 independent confirmation panels.

The [earlier selection diagnostic](Tower-Practical-Selection-Diagnostic-Execution-Review.md) is separate context. It used the two original anchors, and the now-confirmed team was then a newly generated challenger. All four nominees received confirmation there. We do not retroactively describe that challenger as an already designated incumbent.

This is a complete census of these two comparisons, not an exhaustive audit of every historical Tower experiment. Fixed-team confirmation has no selection episode. Older pilots, other gameplay captures, admission work and resource probes are outside this cohort. Historical results remain intact.

The [analyzer](analysis/practical-selection-ties.py) reads only saved JSON. It authenticates package manifests against captured SHA-256 pins, then authenticates each consumed file against the native inventory and the study inventory. It reconstructs nomination order, selection, frozen primary identity, paired confirmation counts and shared-output deduplication. Its final run checked **111 input files** in **1.50 seconds** and rechecked their hashes before publication. This is a targeted saved-evidence audit; it does not rerun the full native archive verifier or decompress every battle. Both original experiments already passed their native and separate audits.

Final artifacts: [summary](../TestResults/selection-tie-audit-final-20260917/summary.json), [all comparison nominees](../TestResults/selection-tie-audit-final-20260917/studies.json), [earlier diagnostic](../TestResults/selection-tie-audit-final-20260917/earlier-diagnostic.json), [input hashes](../TestResults/selection-tie-audit-final-20260917/inputs.json), and [completion receipt](../TestResults/selection-tie-audit-final-20260917/completion.json). The output also retains the exact analyzer and its file inventory.

## What the current selector does

`tower-staged-zero-win-health-v1` first ranks nominees by total selection victories. For a zero-win tie, it uses mean guardian health weighted by trial count, then frozen shortlist position and stable ID. For positive win ties, guardian health, survival and victory duration do not enter selection; frozen shortlist position decides.

The shortlist preserves the discovery ranking among the two protected supplied teams and the top two challengers. In the incumbent and anchored searches, that order comes from the eight-trial discovery panel. In racing, it comes from the last **16-trial promotion panel**. The selector uses position within the four-member shortlist, not the candidate's numeric rank among the full discovery population. Those positions preserve the same relative order. Since positions are unique, stable ID does not actually resolve these cases.

There is no implementation mismatch here. The audit identifies a choice in the current policy: a challenger can replace a supplied team without more selection wins, based on its earlier discovery position. A tie on 32 observations does not establish equal underlying strength.

## Complete comparison census

“Primary” below means the previously confirmed `399bc776…` supplied team. Confirmation counts are the actual chosen output's wins out of 1,000. Study indexes are zero-based archive directory suffixes.

| Comparison | Study / method / restart | Top selection wins | Actual selected output | Confirmation wins | Hypothetical change |
| --- | --- | ---: | --- | ---: | --- |
| Allocation | 0 / incumbent / 1 | 25, unique | `7a249fb5…` | 730 | None |
| Allocation | 1 / racing / 1 | 22, primary + Anchor040e | Primary | 708 | None |
| Allocation | 2 / incumbent / 2 | 22, unique | Primary | 687 | None |
| Allocation | 3 / racing / 2 | 22, unique | Primary | 687 | None |
| Allocation | 4 / incumbent / 3 | 25, unique | Primary | 705 | None |
| Allocation | 5 / racing / 3 | 25, unique | Primary | 705 | None |
| Anchored | 0 / incumbent / 1 | 22, challenger + primary | `37918562…` | 613 | Primary: 692 |
| Anchored | 1 / anchored / 1 | 22, unique | Primary | 692 | None |
| Anchored | 2 / incumbent / 2 | 23, unique | Primary | 727 | None |
| Anchored | 3 / anchored / 2 | 23, challenger + primary | `8d87215b…` | 699 | Primary: 727 |
| Anchored | 4 / incumbent / 3 | 26, unique | Primary | 739 | None |
| Anchored | 5 / anchored / 3 | 26, unique | Primary | 739 | None |

Nine outputs had a unique leader. Of three selected challengers, one had a strict lead and two won a tie. There were **48 nominee occurrences**: 27 had confirmation evidence and 21 did not. After merging identical recipes evaluated by both methods on the same restart panel, the available comparison evidence contains **15 distinct recipe/panel combinations**. These counts describe coverage, not selection accuracy or independent samples.

## The three ties

| Episode | Tied nominees in frozen shortlist order | Earlier score | Holdout wins |
| --- | --- | --- | --- |
| Allocation, racing restart 1 | Primary, position 2; Anchor040e, position 3 | Promotion: 11/16 vs 10/16 | 708 vs 631 |
| Anchored comparison, incumbent restart 1 | `37918562…`, position 1; primary, position 4 | Discovery: 6/8 vs 3/8 | 613 vs 692 |
| Anchored comparison, anchored restart 2 | `8d87215b…`, position 1; primary, position 3 | Discovery: 8/8 vs 6/8 | 699 vs 727 |

The first tie already retained the primary. In the two displacement cases, the primary ranked 15th and 18th in the full discovery populations, respectively; protection kept it on each shortlist. Its later tied selection score could not overcome the earlier ordering under the existing selector.

Retaining the primary in the second row yields **256 gained /177 lost** paired wins, net **+79/1,000**. In the third row it yields **197 gained /169 lost**, net **+28/1,000**. Gains mean primary-only victories; losses mean challenger-only victories. Draws and defeats count as non-wins. These are descriptive contrasts of previously confirmed recipes, not fresh tests of a selector.

Using selection guardian health for every positive tie would also reverse those two choices, but it would additionally favor Anchor040e over the primary in the racing tie: health **4.77 vs 7.95**, with **631 vs 708** holdout wins. That observed counterexample argues against treating lower selection health as a generally validated positive-win tie-breaker.

The strict challenger lead in allocation restart one is preserved: `7a249fb5…` won **25/32** versus the primary's **22/32**, then **730/1,000** versus **708/1,000**. This observed difference is not a separate adoption result. In the earlier diagnostic, the primary challenger won **23/32**, ahead of the other challenger at **22/32** and the two supplied anchors at **18/32** and **16/32**. Its **712/1,000** confirmation score exceeded all three others. Designating either historical anchor would leave that strict winner unchanged under the hypothetical rule.

## Hypothesis and limits

The counterfactual uses `399bc776…` as the fixed supplied primary in all 12 comparison outputs because it was adopted before those comparisons and explicitly designated for anchored generation. It does not choose the best reference using confirmation outcomes. The other methods did not carry an incumbent designation for selection; applying one to them here remains retrospective.

The rule is narrow: retain a predesignated supplied primary only when it shares a **positive maximum selection win count** with another nominee. Otherwise use the existing selector, including zero-win health ordering. An undesignated run keeps existing behavior. An explicit designation must identify a supplied, nominated team; it must not silently fall back to an inferred primary.

These two favorable changes motivated the hypothesis, so this evidence cannot validate it. Ties against a stronger challenger could harm future results. Twenty-one nominee occurrences lack confirmation, so most episodes cannot establish the best nominee, much less the best discovery candidate. We do not combine old panels into a strength estimate, infer a success rate, recompute the closed comparison gates under changed outputs, or claim that either search policy passed. `DoNotPromoteRacing` and `DoNotPromoteAnchored` stand.

## Next implementation contract

Implement an opt-in selector such as `tower-staged-incumbent-tie-v1` while retaining the existing version's exact behavior and default. Initially restrict the new contract to the practical single-context, four-nominee, one-finalist supplied-team workflow audited here.

1. Add an explicit primary reference ID to the selection request/definition contract. Validate it against the supplied recipe before allocation. Freeze and hash its canonical party identity before generation or measurement; do not infer it from start ordering, file names, historical win rates or the confirmation winner. A designation used for anchored generation does not implicitly opt into a new selection policy.
2. Preserve nomination and strict selection-win ordering. On a positive top tie containing the designated primary, retain that primary. Preserve the existing zero-win and remaining tie behavior. Reject invalid or missing designations when the new version is explicitly requested.
3. Persist the version and designation in the saved definition and selection freeze. Explain the actual selection reason in the finalist/report. Extend archive reconstruction and practical request binding so live execution and saved verification use the same immutable rule. Maintain compatibility with existing archives and requests.
4. Add meaningful backend fixtures for positive ties, strict challenger leads, zero-win health, ties that exclude the primary, ambiguous/missing references, nomination protection, archive tampering and reconstruction. Run these through `build/run-tests.ps1`. First deliver this engineering change without combat or a default switch.

After implementation, design a separate prospective comparison isolating selector behavior: hold the generator and budget fixed, provide both selectors the same new search observations and nomination panel, freeze both outputs before confirmation, and deduplicate converged recipes. Declare fresh roots, endpoint, useful-effect threshold, resource cap and stopping rule before seeing results. Do not repurpose the closed generator comparisons or pick another generator based on this audit.

## Verification and changes

Nine analyzer tests passed, covering positive/zero-win ties, a strict challenger lead, a tie above the primary, designation validation, incomplete measurements, paired outcomes including draws, missing holdouts and changed input hashes. The final audit reproduced all 13 selected identities and the retained comparison/diagnostic confirmation counts. An initial development run rejected a cell naming the same recipe as both finalist and control; the reader was corrected to deduplicate identities within that frozen cell, matching the archive contract. No source evidence was edited. An intermediate successful audit remains in `TestResults/selection-tie-audit-20260917`; the final artifact linked above uses clearer panel terminology and the ninth fixture.

Commands use the bundled interpreter because `python` is absent from PATH:

```powershell
& 'C:\Users\HrHoe\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -B -X utf8 'Balance Harness/analysis/practical-selection-ties.py' --self-test
& 'C:\Users\HrHoe\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' -B -X utf8 'Balance Harness/analysis/practical-selection-ties.py' --output 'TestResults/selection-tie-audit-final-20260917'
```

The analyzer requires a new output directory; use another new directory for a later read-only reproduction. Added this review and analyzer, and updated the harness README, practical guide and anchored implementation/comparison follow-up links. Backend tests were not rerun because backend code did not change. No required command was blocked. There are no migrations, service configuration changes or deployment steps.
