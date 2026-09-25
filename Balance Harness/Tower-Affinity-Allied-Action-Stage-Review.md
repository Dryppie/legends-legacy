# Allied-action preservation: saved-stage diagnosis

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The rule preserved the allied-action provider, but this pilot did not discover a stronger selected team.** All twelve candidate searches returned the benchmark. The candidate's **+1.041667 percentage-point** advantage over control came from avoiding two control selections that lost held-out wins. The [closed pilot](Tower-Affinity-Allied-Action-Pilot-01-Execution.md) remains **`Inconclusive`**; its interval is **−0.560984 to +2.644317 points**, and its candidate-minus-benchmark endpoint remains exactly zero.

The [versioned reviewer](analysis/affinity-allied-action-stage-review.py) reconstructed proposal metadata, pruning, nomination, exact validation gates and saved held-out contrasts. All saved arithmetic agrees. It ran **zero fights**, drew or reserved **zero values**, and changed no scientific rule, runtime or gameplay default.

## What changed in the proposal pools

Counts below describe this fresh pilot, not the earlier generation-only preview. An occurrence is one accepted recipe within one root and arm; repeated physical recipes across roots are counted separately.

| Saved stage, twelve roots | Endpoint-preserving v4 | Allied-action-preserving v5 |
| --- | ---: | ---: |
| Accepted occurrences | 204 | 204 |
| Distinct recipes across roots | 39 | 26 |
| Generation attempts | 255 | 296 |
| Already-active rejections | 26 | 27 |
| Duplicate rejections | 25 | 65 |
| Edits removing completable affinity endpoints | 0 | 0 |
| Edits removing the typed allied-action provider | 74 | 0 |
| Enchanted Fairy removal occurrences | 69 | 89 |
| Newly activated authored-route occurrences | 574 | 569 |
| Novel nominee occurrences | 24 | 24 |
| Novel validation challengers | 7 | 6 |
| Validation passes | 2 | 0 |
| Selected benchmark roots | 10 | 12 |

All 24 searches filled both nine/eight-candidate waves. Typed inventory evidence identifies Pack Howler's direct equipped `PerformBasicAttack` / `NonSummonedAllies` effect as the protected provider. Names are descriptive here; the policy uses the authored relationship. Avoiding provider removal is verified construction behavior. It does not establish a combat benefit. Fairy removals increase by twenty occurrences; this archive cannot isolate their causal cost.

There are **138 changed positions out of 204**, **115 shared same-root generated occurrences**, and **89 exclusive occurrences per arm**. Their union contains **293 root/recipe occurrences**. Across roots, the observed union has **40 distinct recipes**: 25 seen in both arms, 14 only under control and one only under candidate. Same-root exclusivity is not global novelty.

Final beam membership and nominee membership differ at **12/12 roots**. Challengers differ at **7/12**, roots 1, 2, 3, 5, 7, 9 and 12. Fourteen control-exclusive and seventeen candidate-exclusive pool occurrences reach nomination; three and six respectively reach validation. None is selected. At the five roots with the same challenger, shared validation observations agree exactly.

## The validation gates were applied correctly

The unchanged selector freezes the best eligible nonbenchmark nominee after sixteen nomination values, including when it trails the benchmark. It then uses a separate sixty-value paired validation panel. Passing requires more gained than lost wins and an exact one-sided binomial tail at or below **1/20**. All other cases return the benchmark.

In this table, nomination is challenger/benchmark wins out of sixteen. Validation shows gained/lost wins and the exact tail rounded to five decimals. Full integers remain in the [review receipt](../TestResults/affinity-allied-action-stage-review-20260924/review.json).

| Root | Control challenger | Nomination | Validation G/L; tail | Candidate challenger | Nomination | Validation G/L; tail |
| --- | --- | ---: | ---: | --- | ---: | ---: |
| 1 | novel `2a2317419e…` | 15/11 | 10/7; 0.31453 | novel `1ffcb4a472…` | 14/11 | 10/9; 0.50000 |
| 2 | novel `869592655f…` | 14/12 | 13/5; 0.04813 **pass** | other-reference `399bc7760f…` | 13/12 | 14/10; 0.27063 |
| 3 | novel `2a2317419e…` | 11/14 | 14/5; 0.03178 **pass** | novel `8c033fca14…` | 14/14 | 11/12; 0.66118 |
| 4 | other-reference `399bc7760f…` | 14/12 | 6/13; 0.96822 | other-reference `399bc7760f…` | 14/12 | 6/13; 0.96822 |
| 5 | novel `88f3e02f66…` | 15/11 | 9/12; 0.80834 | novel `1ffcb4a472…` | 14/11 | 9/13; 0.85686 |
| 6 | other-reference `399bc7760f…` | 13/14 | 15/14; 0.50000 | other-reference `399bc7760f…` | 13/14 | 15/14; 0.50000 |
| 7 | novel `420ce347be…` | 11/13 | 9/8; 0.50000 | novel `2a2317419e…` | 13/13 | 7/7; 0.60474 |
| 8 | other-reference `8287f77974…` | 16/12 | 11/15; 0.83653 | other-reference `8287f77974…` | 16/12 | 11/15; 0.83653 |
| 9 | novel `f36adf5f6d…` | 15/9 | 6/12; 0.95187 | novel `88f3e02f66…` | 13/9 | 6/14; 0.97931 |
| 10 | other-reference `8287f77974…` | 13/9 | 7/14; 0.96082 | other-reference `8287f77974…` | 13/9 | 7/14; 0.96082 |
| 11 | other-reference `399bc7760f…` | 14/15 | 8/17; 0.97836 | other-reference `399bc7760f…` | 14/15 | 8/17; 0.97836 |
| 12 | novel `b4f2caf699…` | 11/15 | 6/14; 0.97931 | novel `a7ade6c9c4…` | 12/15 | 7/12; 0.91647 |

Control validation panels total **114 gains /136 losses**; candidate panels total **111 /150**. Control has five positive and seven negative margins. Candidate has three positive, one zero and eight negative. Its six novel challengers total **50 gains /67 losses**. These are descriptive totals for selected challengers; panels and shared physical observations must not be treated as independent evidence about all generated recipes. A failed gate also does not prove that a challenger is inferior.

The strongest candidate tail is root 2's **0.27063**, well above the frozen 0.05 boundary. The saved results do not justify relaxing that boundary. The two control passes followed the rule correctly; a validation pass is not a guarantee of held-out improvement.

## Why the two output differences occurred

**Both losing control selections were generated by both arms and retained the protected provider.** Candidate fallback therefore cannot be credited simply to blocking those recipes. Shared physical requests have identical outcomes; different competitors and nomination paths changed which recipe advanced.

| Root | Shared control selection | Control validation | Held-out selection / benchmark | Paired held-out gains / losses |
| --- | --- | ---: | ---: | ---: |
| 2 | `869592655f…`, removes Elder Treant Thornstorm | 13/5; tail 0.04813 | 187/199 out of 256 | 40/52 (−4.6875 points) |
| 3 | `2a2317419e…`, removes Illusion Fox | 14/5; tail 0.03178 | 178/198 out of 256 | 37/57 (−7.8125 points) |

At **root 2**, the shared recipe appears in wave two at position four in both arms, with the same 7/8 screen and 5/8 continuation wins. It ranks second in control's final beam but third in candidate's, so only control nominates it. It wins 14/16 in nomination and passes validation. Candidate instead challenges with the primary reference, which fails and returns the benchmark.

At **root 3**, the shared recipe appears in wave two at position two and reaches nomination in both arms. It wins 11/16 against the benchmark's 14/16 in both. It is control's best nonbenchmark nominee, then passes validation. Candidate has another nominee, `8c033fca14…`, with 14/16; that nominee becomes challenger, loses its fresh validation contrast 11/12, and returns the benchmark. The gate did not require a nomination win over the benchmark, so this is expected selector behavior.

These two observed reversals explain the entire +1.041667-point end-to-end mean. They do not establish true inferiority of either recipe or future superiority of either search policy. They also provide no basis for fitting a new gate to these roots.

## What is still unknown, and the next useful implementation

Each arm has **two generated occurrences with same-root held-out measurements**, the shared recipes above, and **202 without**. In the union, **291/293 generated occurrences remain unmeasured** on their root's held-out panel. All twelve candidate validation challengers lack a same-root held-out outcome. Candidate's known generated outcomes come from shared physical recipes selected by control, not from candidate selections. The export preserves these distinctions and all unknowns as nulls.

This archive cannot decide whether better candidates existed in the pool, were pruned, or lost nomination. It cannot estimate the whole candidate pool from its selected validation panels. Earlier [paired-pool recognition](Tower-Affinity-Preservation-Recognition-Execution.md) addressed the v3/v4 cohort; its outcomes cannot fill these v4/v5 root-specific cells.

**Next: build a complete finite-neighborhood diagnostic plan, before any more combat.** The [generation-only preview](Tower-Affinity-Allied-Action-Preservation-Implementation.md) already establishes a small legal neighborhood: 41 endpoint-preserving recipes and 26 allied-action-preserving recipes. Reconstruct and authenticate the complete catalogue and the expected subset relation, using exact builds, equipment, context and authored evidence. Include the three fixed references. This would evaluate the complete legal family rather than repeatedly sampling another search trajectory.

Build a combat-free, separately versioned exhaustive-neighborhood diagnostic plan from the captured v4/v5 legal recipes and three fixed references. Freeze complete physical membership, common fresh panels, simultaneous uncertainty, a practical-benefit/stop rule and resource ceilings before admission. Treat it as a finite-family diagnosis, not a rerun, policy promotion or estimate of future-root search performance.

The plan should distinguish three questions: whether any legal recipe offers a practically useful benchmark gain, how the complete protected subset compares with the larger neighborhood, and whether the neighborhood should be retired if evidence is insufficient. Fix the meaningful effect size, sample size, simultaneous comparisons and stop decision before new observations. Do not select the family or tune sample counts using the current winners. Do not reuse old outcomes as fresh confirmation. With complete inclusion there is no pool-sampling uncertainty; combat uncertainty and selection across recipes still require explicit treatment. A successful finite-family result would inform a later fresh search comparison or team confirmation, without promoting a policy by itself.

This recommendation avoids another blind proposer iteration while leaving the current study closed. No catalogue builder, new protocol, admission, reservation or combat execution was performed in this read-only review.

## Verification, changed files and accounting

**25 tests passed**, covering both racing versions, literal pass/fallback cases, seed partitioning, scores, contrasts, pruning, beams, nomination, frozen challengers, exact gate boundaries, typed provider protection, shared observations, saved held-out recounts, unknown outcomes, occurrence counting and evidence tampering. An initial test expected the older endpoint-protection error message when the new provider check rejected the corruption first; that test expectation was corrected. Both logs are retained. The real all-root review completed once, with no failed launch or retry.

Commands used the bundled Python runtime with `-B -X utf8`:

```text
Balance Harness/analysis/test-affinity-allied-action-stage-review.py
Balance Harness/analysis/affinity-allied-action-stage-review.py
TestResults/affinity-allied-action-stage-review-verification-20260924/finish.py
```

The separate review allowance was **180 seconds /64 MiB**, declared before processing the pilot and fully added to the declared maxima, including on failure. Recorded execution was **5.781 seconds /1,821,542 bytes**. Cumulative recorded work is **41,261.676 seconds /33,518,655,549 bytes**; cumulative declared maxima are **116,400 seconds /72,293,023,744 bytes**. Unused allowance is not refunded. Fixture tests and documentation checks are separate engineering work.

All **128 inherited historical pins** and all **34 members** of the preceding verification package were authenticated. Consumed evidence was checked before and after the review and again at publication. Native RNG replay, direct battle reconstruction and live-history validation remain supported by the published audits; this review recounts saved stages and does not repeat those full audits. The last complete history count remains **782,796 excluded values across 262 files**, not rescanned here.

Changed files: the new reviewer, its [regression tests](analysis/test-affinity-allied-action-stage-review.py), this report, two LF-preservation entries in `.gitattributes`, only line three of nine current-status documents, and retained review/verification/handoff artifacts. Historical document bodies are byte-preserved. The [verification receipt](../TestResults/affinity-allied-action-stage-review-verification-20260924/verification.json) and [handoff](../TestResults/affinity-allied-action-stage-review-handoff-20260924.json) retain the checks and next step.

No required verification command was blocked. C# and the admitted runtime were unchanged, so backend tests were not rerun; prior backend checks remain inherited. There are **no migrations, application configuration changes, deployments or default-policy changes**. Unrelated working-tree edits were preserved.

Review manifest SHA-256: `84226a6dd0eb2087ac8df8a37fc0e90c5c4cfb990d28547ee48bdf0fa6900f1d`. Scientific manifest: `c9d191a8f81439d6bfa947e8c62bd74a6e35352ee5d999b8dcd9ecdfdded54ef`. Scientific closeout: `f58552cbd568e66f6e76090f01cd0be8d450e173b552513d445e97a73379dc82`.
