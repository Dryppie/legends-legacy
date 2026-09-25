# Affinity preservation: where the pilot converged

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**Preservation changed proposals, surviving beams, nominees and validation challengers. The unchanged validation gate then returned the benchmark at every root.** Final beams differ at **12/12 roots**, nominee sets at **9/12**, and challengers at **7/12**. The saved arithmetic agrees with both published audits; this review found no discrepancy in generation metadata, pruning, nomination or gate application.

The [closed pilot](Tower-Affinity-Preservation-Pilot-01-Execution.md) remains `NoObservedOutputDifferentiation`, with zero paired held-out gain. No policy is promoted. This retrospective review ran **zero fights**, allocated **zero values**, and did not change a search policy, selector, threshold or scientific archive.

## Generation differences reached the final selection stage

| Saved stage, all twelve roots | Original v3 | Preserving v4 |
| --- | ---: | ---: |
| Accepted proposals | 204 | 204 |
| Distinct physical recipes across roots | 57 | 39 |
| Generation attempts | 253 | 260 |
| Already-active rejections | 25 | 27 |
| Duplicate rejections | 24 | 29 |
| Accepted edits removing a completable selected-affinity endpoint | 47 | 0 |
| Newly active authored-route occurrences | 507 | 579 |
| Novel nominee occurrences | 24 | 24 |
| Novel validation challengers | 6 | 9 |
| Validation passes | 0 | 0 |
| Benchmark outputs | 12 | 12 |

All 24 searches filled both waves of nine and eight candidates. Preserving v4 performs its declared structural task: none of its accepted edits removes an existing endpoint that the addition could preserve or complete. Original v3 does so at 47 accepted occurrences. Authored compatibility and route activation counts describe construction; they do not measure combat benefit.

There are **118 changed accepted positions out of 204 paired positions**. Matching positions is different from comparing membership: the pools share **123 same-root recipe occurrences**, with **81 original-only and 81 preserving-only occurrences**. Their same-root union contains **285 generated occurrences**. Across all roots there are 60 distinct recipes: 36 observed under both arms, 21 only under original v3 and 3 only under preserving v4. A recipe can be exclusive within one root while appearing under both arms elsewhere.

Both arms retain four candidates per continuation and nominate two novel candidates plus three fixed references per root. The final beam membership differs at every root. Nominee sets differ at roots 1, 2, 3, 4, 7, 8, 10, 11 and 12. The benchmark remains available throughout. Six original-only pool occurrences and seven preserving-only occurrences reach nomination; one and two respectively reach validation. Shared recipes can also be selected differently because the competing set and preceding observations differ.

Validation challengers differ at roots **2, 3, 4, 8, 10, 11 and 12**. Thus earlier pruning did not erase every proposal difference. At the five roots with the same challenger, the shared physical requests have identical validation observations, as required by the paired design.

## Validation explains the identical outputs

Each nomination uses 16 values. The best eligible nonbenchmark nominee becomes the frozen challenger, even if its nomination score is below the benchmark. It then receives a separate 60-value panel paired with the benchmark. The existing gate requires both more gained than lost wins and an exact one-sided binomial tail at or below **1/20**. Otherwise it returns the benchmark.

In the table, nomination is challenger/benchmark wins out of 16. Validation is gained/lost paired wins followed by the exact tail rounded to three decimals. Complete integer numerators and denominators remain in the [review receipt](../TestResults/affinity-preservation-stage-review-20260924/review.json). Every displayed gate fails.

| Root | Original challenger | Nomination | Validation G/L; tail | Preserving challenger | Nomination | Validation G/L; tail |
| --- | --- | ---: | ---: | --- | ---: | ---: |
| 1 | reference `399bc7760f…` | 14/11 | 10/10; 0.588 | reference `399bc7760f…` | 14/11 | 10/10; 0.588 |
| 2 | reference `399bc7760f…` | 13/12 | 13/12; 0.500 | novel `1ffcb4a472…` | 14/12 | 9/15; 0.924 |
| 3 | novel `482ae6d57e…` | 13/14 | 10/14; 0.846 | novel `d2c2e78c2c…` | 12/14 | 9/8; 0.500 |
| 4 | novel `55445ccdcf…` | 12/12 | 6/5; 0.500 | novel `d2c2e78c2c…` | 14/12 | 8/14; 0.933 |
| 5 | novel `f36adf5f6d…` | 11/13 | 11/16; 0.876 | novel `f36adf5f6d…` | 11/13 | 11/16; 0.876 |
| 6 | novel `b0a2661201…` | 13/11 | 5/12; 0.975 | novel `b0a2661201…` | 13/11 | 5/12; 0.975 |
| 7 | reference `399bc7760f…` | 14/13 | 11/6; 0.166 | reference `399bc7760f…` | 14/13 | 11/6; 0.166 |
| 8 | reference `399bc7760f…` | 11/11 | 10/12; 0.738 | novel `6e7b5c1cc2…` | 15/11 | 9/13; 0.857 |
| 9 | reference `8287f77974…` | 12/10 | 8/9; 0.685 | reference `8287f77974…` | 12/10 | 8/9; 0.685 |
| 10 | novel `1ffcb4a472…` | 13/10 | 12/11; 0.500 | novel `8c033fca14…` | 13/10 | 10/5; 0.151 |
| 11 | reference `399bc7760f…` | 14/13 | 16/12; 0.286 | novel `1ffcb4a472…` | 15/13 | 13/9; 0.262 |
| 12 | novel `a7ade6c9c4…` | 13/13 | 9/12; 0.808 | novel `fc0f301f7e…` | 13/13 | 10/11; 0.668 |

Original v3 has five positive validation contrasts, one zero and six negative; preserving v4 has four positive, one zero and seven negative. Original totals are **121 gains /131 losses**, and preserving totals are **113 /128** across their twelve challenger panels. These are descriptive accounting totals for selected candidates. They are not independent samples of all generated candidates, and shared physical observations across arms must not be counted twice as independent evidence.

The strongest positive original result is root 7, **11 gains /6 losses**, exact tail about **0.16615**. The strongest positive preserving result is root 10, **10 /5**, tail about **0.15088**. Neither is close to the frozen 0.05 pass boundary. The results give no evidence here that loosening the gate would improve independently measured outputs. They also do not establish that every failed challenger is worse than the benchmark.

Two examples locate the transitions:

- **Root 8:** the preserving-only recipe `6e7b5c1cc2…` reaches nomination with 15/16 wins against the benchmark's 11/16. Its fresh validation contrast reverses to 9 gains /13 losses. The original arm nominates the primary reference, which also fails. Both return the benchmark.
- **Root 10:** the preserving-only recipe `8c033fca14…` ties another nonbenchmark nominee at 13/16 and wins the frozen nominee order. It records a positive five-win validation margin, but the exact tail is about 0.15088, so it falls back. The original challenger also has a small positive validation margin and falls back. Neither challenger has a held-out measurement in this root.

The preserving arm sends more novel recipes to validation, but its nine novel challengers together record a descriptive **−19 net wins** against the benchmark on their selected validation panels; the original arm's six record **−17**. This is not a causal comparison of novelty, route counts, preservation or true recipe quality. The root-paired end-to-end endpoint remains zero because both arms actually select the same benchmark.

## What remains unknown and what to implement next

The held-out study measured only the selected benchmark at all twelve roots. **None of the 204 generated occurrences in either arm, and none of either arm's twelve validation challengers, has a same-root held-out outcome.** The review retains nulls for those cells. A recipe's observations in a different root, earlier study, nomination panel or validation panel cannot fill them.

Consequently, the archive establishes where the final outputs converge, but cannot determine whether useful recipes were pruned, whether nomination missed the best candidates, or whether a failed challenger would improve independently measured performance. It does not support promoting preservation or retuning the validation threshold from these observations.

Implement a separately versioned paired-pool recognition-plan adapter for the frozen original/preserving proposal union. Retain all twelve roots; include both arms' nominees and validation challengers, and a probability sample of the remaining shared and arm-exclusive candidates. Freeze membership, inclusion probabilities, fresh panels, endpoints and costs before admission. Keep the current validation gate unchanged; no new combat yet.

The adapter should preserve each root/physical-recipe occurrence and both-arm provenance, deduplicate shared physical evaluations only within a root, and record explicit sampling strata and inclusion probabilities. Its catalogue contains 285 generated root occurrences plus the three reference occurrences per root. Mandatory nominees/challengers and probability sampling of the remainder can make the diagnostic bounded; it should not silently retain only promising observations. Separate sampling uncertainty from combat uncertainty and report all sampled outcomes, with unsampled cells left unknown. The output would diagnose this frozen cohort; a later policy comparison or team confirmation still requires a separate fresh design.

This is a development recommendation. No recognition catalogue sampling, seed reservation, resource admission or new experiment was performed in this review. The completed pilot stays closed, with no retry, extension or promotion.

## Verification and accounting

The new [reviewer](analysis/affinity-preservation-stage-review.py) authenticates the consumed manifests/files, checks both v5/v6 racing contracts and the shared root/panel allocation, recounts saved scores and paired contrasts, reconstructs pruning and nomination, checks selected-affinity endpoint protection, recomputes the exact gate and checks the durable freezes and decisions. It compares shared physical observations without assuming identical proposal trajectories. Native proposal RNG replay, raw battle reconstruction and full live-history verification remain supported by the pinned published audits; this was not a second full battle audit.

**20 tests passed** in [test-affinity-preservation-stage-review.py](analysis/test-affinity-preservation-stage-review.py), using the already sealed literal fixture and corruption cases for both versions, saved pass/fallback behavior, seed reuse, ordering, completeness, scores, contrasts, pruning, beams, nomination, frozen challengers, integer gate boundaries, endpoint protection, shared outcomes, null evidence and manifest drift. During test development, the fixture route mapping and fixture context assumption were corrected before the all-root launch; the initial test logs are retained. No scientific analysis launch failed or was repeated.

Commands, using the bundled Python runtime with `-B -X utf8`:

```text
Balance Harness/analysis/test-affinity-preservation-stage-review.py
Balance Harness/analysis/affinity-preservation-stage-review.py
TestResults/affinity-preservation-stage-review-verification-20260924/finish.py
```

The all-root review completed once in **5.219 seconds**, retaining **1,512,279 bytes**. Its separate **180-second /64-MiB allowance** was [declared before analysis](../TestResults/affinity-preservation-stage-review-20260924/declaration.json) and is fully charged. Cumulative recorded charges are **35,411.656 seconds /29,223,599,622 bytes**; cumulative declared maxima are **89,460 seconds /56,119,787,520 bytes**. Lower actual review use does not reduce those charged ceilings. Fixture tests and documentation verification are separate engineering work.

The last full live-history verification remains **760,272 excluded values across 258 files**, inherited from publication and not rescanned here. All 41 prior historical pins are unchanged. The previous 36-member execution handoff was authenticated before updating only line three of the nine current-status documents; their historical bodies are byte-preserved. The [verification package](../TestResults/affinity-preservation-stage-review-verification-20260924/verification.json) records these checks, and the [new handoff](../TestResults/affinity-preservation-stage-review-handoff-20260924.json) carries the evidence and next step.

Review manifest SHA-256: `dc09dc54929ec14403804e6cc6d97c1af632fcb708eb11ba9d0c512d7ccf5abd`. Scientific manifest: `1c4fb870912fca7b168596cc86645cb54e96361ba8099eac1da5b4b8748d49e4`. Scientific closeout: `f9740a37d49210308261c841115661f774cfbf9e893e84398c138c74fb689d26`. Publication-verification manifest: `d22377f904f4c287cd9a47364ce3843173cc805d16a16cbf102401c1c5027883`.

Changed files are the new reviewer, its tests, this report, nine status lines and the retained review/verification/handoff receipts. No required verification command remains blocked. Backend code and the admitted runtime were unchanged, so no backend rebuild or test rerun was needed. There are **no migrations, application configuration changes, deployments or gameplay-default changes**. Unrelated working-tree changes were preserved.
