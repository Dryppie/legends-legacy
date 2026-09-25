# Adaptive racing: diagnosis after the negative pilot

**Subsequent evidence:** The [frozen-pool diagnostic](Tower-Frozen-Pool-Recognition-Execution.md) completed 27,648 fresh fights and both audits. Of 72 measured challengers, 68 scored below the benchmark, two tied and two had small uncertain positive gains. Proposal quality is now the next development priority. The following preserves the earlier saved-stage review; its original panels and missing outcomes are not rewritten or pooled with the new measurements.

23 September 2026. Target: the offline `LL/tools/BalanceHarness`. This review implements the next diagnostic step after [pilot 02](Tower-Adaptive-Racing-Pilot-02.md). It reads saved evidence and adds no combat, seed allocation or gameplay changes.

**Keep `AbandonThisConfiguration`.** The records demonstrate unproductive fully fresh proposals and fragile final selection. They do **not** establish whether racing discarded genuinely stronger teams: 198 of 204 adaptive candidate instances, including 18 final nominees, have no held-out measurement. The next useful implementation is a frozen-pool recognition diagnostic that measures retained and discarded candidates on fresh paired panels. Another run of the unchanged search, a broad parameter sweep or a tie-rule-only change would leave that question unresolved.

The negative pilot does not disprove beam search or adaptive allocation in general. It rejects this combined generator, budget allocation and selector in this captured scenario. Its original result remains −0.944 percentage points against the baseline search and −4.199 points against the strongest reference R* (`96b94357…`). No retrospective calculation here changes its endpoint or promotion status.

## What the saved records establish

The new [analysis script](analysis/adaptive-racing-stage-review.py) reconstructs all twelve baseline and adaptive searches. It recounts training panels, checks adaptive pruning and common-rung scores, reproduces nominations and final selection, and joins only actually measured held-out outputs. Counts below refer to root/recipe instances unless explicitly labeled distinct recipes. A repeated recipe in another root has a different panel and is not another measurement for its earlier root.

| Quantity across twelve roots | Baseline B | Adaptive N |
| --- | ---: | ---: |
| Search fights | 6,336 | 6,336 |
| Generated candidate instances | 516 | 204 |
| Distinct generated recipe identities | 507 | 195 |
| Challenger nominees | 24 | 24 |
| Novel selected outputs | 1 | 6 |
| Generated instances with held-out measurements | 1 | 6 |
| Generated instances without held-out measurements | 515 | 198 |
| Reference fights during search | 1,440 | 2,592 |
| Challenger fights during search | 4,896 | 3,744 |
| Final selections tied with a nonprimary reference | 0 | 2 |
| Roots whose first/second selection halves disagree | 7 | 9 |
| Roots with any leave-one-out selection change | 4 | 3 |

Adaptive racing samples 17 new candidates per root versus 43 for the baseline. It uses 216 of 528 search fights per root on references (40.91%), versus 120 (22.73%) for the baseline. Those are legitimate common-panel controls, not cache misses: their repeated trials use different seeds. Nevertheless, the allocation sacrifices breadth and challenger measurements. Whether that sacrifice is worthwhile must be judged by output quality; this pilot did not show a benefit.

### Generation and coverage

Adaptive generation filled every batch with **204 accepted proposals out of 205 attempts**. The only rejected proposal was a duplicate recipe. Six requested recombinations fell back to the documented partial-edit operator because their parents differed at fewer than two owners. No observed batch was limited by exhausting legal construction attempts. These counts do not measure the quality of the broader legal neighborhood.

| Effective adaptive operator | Accepted | Zero wins on initial 8-trial panel | Nominated | Selected | Held-out measured |
| --- | ---: | ---: | ---: | ---: | ---: |
| Single edit | 85 | 0 | 13 | 3 | 3 |
| Coordinated edits | 24 | 0 | 1 | 0 | 0 |
| Partial owner rebuild | 30 | 0 | 2 | 0 | 0 |
| Recombination | 17 | 0 | 6 | 2 | 2 |
| Guided pair | 24 | 1 | 2 | 1 | 1 |
| Fully fresh team | 24 | 24 | 0 | 0 | 0 |

All **24 fresh adaptive teams scored 0/8**, totaling **0/192** initial wins. The baseline's **193 fresh-legal instances also scored 0/8**, totaling **0/1,544**. None reached nomination. This independently repeats the original assessment's observation that broad fresh generation has low immediate yield in this captured cohort. It does not prove zero underlying win probability or that all forms of diverse initialization are ineffective.

The adaptive policy reduced fresh-team trials by 1,352 relative to the baseline, while reference trials increased by 1,152. This is an accounting comparison between complete policies, not an isolated allocation experiment. Likewise, nomination rates by operator are descriptive: parent quality, opportunity counts, adaptive survival and panel difficulty differ. Recombination's six nominees from seventeen accepted proposals do not establish its superiority.

The recorded main-parent sources were 85 benchmark, 46 other-reference, 22 first-wave reference fallback and 27 prior-beam proposals, plus 24 fresh teams. Recombination may also have a separate donor. Five selected novel recipes had a benchmark main parent; one had another-reference main parent. A small legal edit around R* therefore does not by itself protect output quality.

### Racing and elimination

**22 of 24 screen decisions had multiple challengers tied in win count at the third-ranked challenger**, the elite cutoff. Secondary fitness and the competitive diversity rule therefore had substantial influence on which candidates received another eight trials. This is a count of ties on small training panels, not proof that any specific elimination was wrong.

The diversity choice was outside the top four fitness-ranked challengers on **12 of 24 decisions**. Only one diversity-selected candidate eventually reached final nomination: root 12's first-wave guided-pair proposal, initially fifth in the screen ranking. It was ultimately selected and scored 196/256 versus R*'s 199/256. That demonstrates a changed trajectory, not a supported advantage for or against diversity. A no-diversity counterfactual would change later parents, proposals and measurements, most of which are missing.

The first wave supplied 13 of the 24 challenger nominations and three of six novel outputs; the second wave supplied eleven nominations and the other three outputs. The saved data cannot justify dropping a whole wave. It also cannot establish the true quality of the thirteen candidates per root outside the final four-candidate beam: their later measurements do not exist.

### Final selection and held-out reversals

Selection used 40 fresh trials per nominee. R* below is the fixed benchmark; “best-reference margin” compares the selected team with whichever of the three references scored highest on that selection panel. These are different quantities and must not be substituted for each other.

| Root | Output | Selected wins /40 | R* wins /40 | Best-reference margin, wins | Held-out N−R*, pp | Half panels disagree | Leave-one-out changes /40 |
| --- | --- | ---: | ---: | ---: | ---: | --- | ---: |
| 1 | Novel | 31 | 31 | +0 | -13.672 | Yes | 7 |
| 2 | Novel | 33 | 32 | +1 | -5.859 | Yes | 0 |
| 3 | Novel | 29 | 24 | +2 | -7.031 | Yes | 0 |
| 4 | R* | 30 | 30 | +0 | +0.000 | Yes | 0 |
| 5 | R* | 37 | 37 | +0 | +0.000 | No | 0 |
| 6 | R* | 33 | 33 | +0 | +0.000 | No | 0 |
| 7 | R* | 35 | 35 | +0 | +0.000 | Yes | 0 |
| 8 | R* | 31 | 31 | +0 | +0.000 | Yes | 0 |
| 9 | Novel | 31 | 28 | +2 | -11.719 | Yes | 0 |
| 10 | R* | 32 | 32 | +0 | +0.000 | No | 0 |
| 11 | Novel | 33 | 33 | +0 | -10.938 | Yes | 5 |
| 12 | Novel | 31 | 28 | +1 | -1.172 | Yes | 7 |

Roots 1 and 11 tied R* at the selection maximum. The incumbent tie rule protects the designated primary `399bc776…`; it does not protect every reference. Frozen nominee order selected the challenger in both cases. Their held-out deficits were 35 and 28 wins. The four strict novel winners led the best reference by only one or two wins during selection and subsequently lost 15, 18, 30 and three wins against R*. Thus **63 of the 129 total held-out lost wins occurred in the tie-selected outputs, and 66 in strict winners**. This decomposition explains why a tie-rule change alone does not address the entire observed regression. It is not a fresh evaluation of an alternative selector.

All six novel-output roots selected different teams on the first versus second 20-trial halves. Removing one trial changed the full-panel choice at roots 1, 11 and 12, in nineteen of 480 leave-one-out subsets overall. The baseline changed in thirty of 384 subsets, spread across four roots. Neither perturbation is a reliability estimate: the subsets overlap and reuse training data. A selection unchanged by removing one observation can still be wrong, as roots 2, 3 and 9 illustrate. The diagnostics must not become retrospectively fitted decision thresholds.

## Next implementation decision

The unresolved question is whether these searches **generate useful teams but eliminate or misrank them**, or whether most available proposals are weaker than R* even with reliable measurement. More confirmation of only selected winners cannot answer it. The separate pending 52,000-fight exact-team confirmation remains useful for its original purpose, but is still not the most informative next study for search design.

Implement a deterministic **frozen-pool recognition study builder** using the existing fixed-family evaluation and reservation machinery. It should freeze candidate membership before any new outcome is collected, separate strata in its reports and accept an explicit externally supplied sampling choice for lower-ranked candidates. A concrete proposed scope is all twelve completed adaptive roots, with nine teams per root:

- The three exact references.
- Both final challenger nominees, including the one not selected.
- The two remaining members of the final four-candidate beam, which missed nomination.
- Two sampled candidates from the other thirteen generated teams, with the sampling procedure and inclusion probabilities declared before fresh allocation.

At 256 new common trials per team/root, this would require **27,648 fights** (`12 × 9 × 256`). This is a prospective size calculation, not an approved request, power claim or runtime forecast. The builder should distinguish measured training ranks from independent performance, report every sampled team, and retain nulls for unsampled candidates. Claims about the thirteen-member lower stratum require the declared probability sample; two attractive hand-picked misses cannot stand in for it. Fresh values, current admission, an explicit resource envelope and both audits remain necessary before execution.

This follow-up would diagnose those historical pools, not validate a new search policy or reconstruct an alternative adaptive trajectory. If measured discarded candidates are promising, prioritize allocation/recognition. If nominees and discarded strata are broadly below R*, prioritize proposal quality and reference-centered neighborhoods. Keep the three controls and the existing independent recommendation gate throughout. Do not promote a newly fitted selector using these development outcomes.

## Evidence and verification

The review authenticated all **41 consumed scientific files, including the manifest**, and **two publication-closeout files** against their published external pins, then rechecked their bytes after analysis. It used saved panel observations and published held-out endpoints; it did not repeat the complete compressed-battle audit, native preparation, combat execution or live-history scan. The successful prior native and independent scientific audits remain the source of full battle/proposal validation.

- Scientific archive manifest: `f2327de7f9382f3a3ac3213a25cdb590e81e676ddd6e622fb91e3615ea8dd95a`.
- Publication verification manifest: `3e11948b3892f4d813a6b992d12a242c8c10342df34c6ee91996604045d2d798`.
- New diagnostic package manifest: `f4087410b3647ef1294c732f0570459daeb901913617502216345828a2508835`.

The [machine-readable review](../TestResults/adaptive-racing-stage-review-20260923/review.json) retains every candidate's operator, parent source, measured stages, nomination and selection, and available held-out wins. Missing values are JSON nulls. Its [source snapshot](../TestResults/adaptive-racing-stage-review-20260923/analysis.py) and [manifest](../TestResults/adaptive-racing-stage-review-20260923/files.json) bind the producing analysis. The pass completed in **1.234 seconds**, retaining **516,310 bytes**, within its 180-second /16-MiB output limits. There were zero new fights and zero new reserved values.

Relevant implementation locations:

| Behavior | Source |
| --- | --- |
| Frozen batches, fresh common rungs and five nominees | [TowerBatchRacing.cs](../LL/tools/BalanceHarness/TowerBatchRacing.cs#L23), `RunCoreAsync` |
| Elite and competitive diversity pruning | [TowerBatchRacing.cs](../LL/tools/BalanceHarness/TowerBatchRacing.cs#L192), `Prune` |
| Positive incumbent ties and frozen-order fallback | [TowerBatchRacing.cs](../LL/tools/BalanceHarness/TowerBatchRacing.cs#L208), `Select` |
| Opportunity schedule and parent mixture | [TowerAdaptiveRacingGenerator.cs](../LL/tools/BalanceHarness/TowerAdaptiveRacingGenerator.cs#L18), `First`/`Second` and `Generate` |
| Recombination-to-partial fallback | [TowerAdaptiveRacingGenerator.cs](../LL/tools/BalanceHarness/TowerAdaptiveRacingGenerator.cs#L115), `Construct` |
| Fixed endpoint, all root outcomes and uncertainty | [Pilot result](../TestResults/balance/tower-adaptive-racing-pilot-02-20260923/result.json) and [execution report](Tower-Adaptive-Racing-Pilot-02.md) |

**12 tests passed**, including all twelve saved search pairs, complete panel and score reconstruction, missing-outcome preservation, incumbent and nonprimary ties, zero-win handling, manifest tampering, reordered/partial panels, reused seeds, altered pruning/nomination, unresolved parents and duplicate accepted recipes. See the [tests](analysis/test-adaptive-racing-stage-review.py), [test log](../TestResults/adaptive-racing-stage-review-tests-20260923.log) and [execution log](../TestResults/adaptive-racing-stage-review-execution-20260923.log).

Commands executed, with `python` denoting the bundled Python interpreter's absolute path:

```powershell
python -B -X utf8 'Balance Harness/analysis/test-adaptive-racing-stage-review.py'
python -B -X utf8 'Balance Harness/analysis/adaptive-racing-stage-review.py'
python -B -X utf8 'TestResults/adaptive-racing-stage-report-check.py'
```

The [report check](../TestResults/adaptive-racing-stage-report-check.py) and its [receipt](../TestResults/adaptive-racing-stage-report-check-20260923.log) verify all twelve root rows, six operator rows, twelve comparison rows, source-line references, local links and the diagnostic package's hashes.

Changed files are the new analysis script and tests, this report, and status links in the original assessment, pilot report and harness documentation. Python syntax, scoped whitespace, report arithmetic, source-line references and local links were checked. Backend tests were not run because C# and executable search behavior did not change. No required command remains blocked. There are no migrations, application configuration changes or deployment implications; unrelated working-tree changes and all sealed scientific archives were preserved.

The subsequent [recognition builder implementation](Tower-Frozen-Pool-Recognition-Implementation.md) froze all 240 source recipe instances, recorded one probability sample and published the exact 108-team-instance plan. Its 17 tests and read-only reproduction passed, with no combat during planning. The later [native implementation and admission](Tower-Frozen-Pool-Recognition-Native-Implementation.md) and [completed diagnostic](Tower-Frozen-Pool-Recognition-Execution.md) record the separate execution steps.
