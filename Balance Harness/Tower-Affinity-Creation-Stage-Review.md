# Affinity creation: saved-stage review

23 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**Reference selection accounts for almost all of this pilot’s net benchmark shortfall.** The creation arm lost 41 net held-out wins against the benchmark across twelve roots. Selecting the older primary reference accounts for 40 of those lost wins; the five selected novel-output occurrences account for the remaining one. This is a retrospective decomposition of the observed result, not an estimate of how a different policy would perform.

The [closed pilot](Tower-Affinity-Creation-Pilot-01-Execution.md) remains `Inconclusive`: +2.246 percentage points against control and −1.335 against the benchmark. No root was removed, no outcome was reclassified, and no policy was promoted. This review ran no fights, drew no entropy and allocated no values.

The new [reviewer](analysis/affinity-creation-stage-review.py) authenticates consumed records against the pinned scientific, admission and publication-verification manifests. It recounts all **24 search trajectories**: saved panel scores, pruning, common-panel ranking, nomination, final selection and held-out identity. It also verifies **2,600 shared training observations**, the literal creation edits, authored pair identities and complete newly activated route lists. The prior publication verification remains responsible for the full compressed-battle and live-history audit; this review does not repeat those operations or regenerate random trajectories.

## Where the benchmark deficit came from

The benchmark is `96b94357…`. The older primary reference is `399bc776…`; its role still controls positive selection ties in the frozen racing plan. All three references remain on every final 40-observation selection panel.

| Creation-arm selected category | Roots | Held-out net wins versus benchmark | Contribution to twelve-root mean |
| --- | --- | ---: | ---: |
| Benchmark itself | 5, 6, 10 | 0 | 0 points |
| Older primary reference | 2, 7, 9, 12 | **−40** | **−1.302 points** |
| Novel proposal | 1, 3, 4, 8, 11 | **−1** | **−0.033 points** |
| All roots | 1–12 | **−41** | **−1.335 points** |

Category membership is determined by the selected recipe. These are selected, unequal groups and cannot establish a causal effect of being novel or a reference. In particular, the novel-output total conceals both gains and losses.

All creation-arm selections are retained below. Selection counts use 40 observations; held-out differences are net wins over 256 observations. A positive tie can involve another challenger even when the benchmark is below the leaders.

| Root | Selected output | Selection wins / benchmark wins | Selection rule | Held-out net wins versus benchmark |
| --- | --- | --- | --- | ---: |
| 1 | Novel `8c033fca…` | 31 /26 | Unique maximum | +9 |
| 2 | Primary `399bc776…` | 29 /29 | Primary positive tie | **−29** |
| 3 | Novel `1ffcb4a4…` | 36 /28 | Unique maximum | −9 |
| 4 | Novel `420ce347…` | 34 /30 | Unique maximum | +1 |
| 5 | Benchmark | 31 /31 | Frozen nominee order | 0 |
| 6 | Benchmark | 36 /36 | Unique maximum | 0 |
| 7 | Primary | 33 /32 | Unique maximum | −6 |
| 8 | Novel `a7ade6c9…` | 34 /28 | Unique maximum | −2 |
| 9 | Primary | 30 /28 | Primary positive tie | −14 |
| 10 | Benchmark | 33 /33 | Unique maximum | 0 |
| 11 | Novel `a7ade6c9…` | 33 /31 | Unique maximum | 0 |
| 12 | Primary | 30 /28 | Primary positive tie | +9 |

Root 2 makes the reference-priority issue concrete. Both methods saw the primary and benchmark win **29/40** and selected the primary through the inherited positive-tie rule. The paired held-out panel then gave the primary **172/256** and the benchmark **201/256**. This one root accounts for 29 of the candidate’s 41 net lost wins. The rule was executed as declared; the review identifies its interaction with the benchmark objective.

Reference preference alone cannot explain the other losses. Root 7’s primary led the benchmark by one selection win, and root 9’s by two. Root 3 selected a novel proposal with an eight-win lead yet lost nine held-out wins to the benchmark. Conversely, root 12’s primary exceeded the benchmark by nine held-out wins. All of these outcomes remain in the assessment.

## Creation coverage and the selected recipes

Creation changed all **204 accepted positions**. Its 252 attempts comprised 204 acceptances, 25 `affinities-already-active` rejections and 23 duplicate-recipe rejections. There were 227 construction checks. Control accepted all 204 attempts with 204 construction checks. Rejected attempts are retained; accepted positions are not assumed to share attempt numbers or random-choice trajectories between policies.

The 204 creation occurrences cover **58 distinct recipes across nine owners**, compared with 200 distinct control recipes. Owner 8 already activates the selected pairs and supplied no accepted creation. Target selection covers 108 Venomous Spiderling/Viper placements and 96 Royal Venom/Viper placements. The two Spiderling routes share one essence pair and do not create an extra sampling ticket.

| Creation edit distance | Accepted occurrences | Distinct recipes | Survived initial screen | Nominated | Selected | Same-root held-out unknown |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| One replacement | 179 | 42 | 66 | 20 | 5 | 174 |
| Two replacements | 25 | 16 | 15 | 4 | 0 | 25 |

Counts are recipe/root occurrences unless explicitly marked distinct. Initial survival means reaching the continuation panel of the proposal’s birth wave. Four two-slot proposals reached final nomination, but none was selected. **All 25 two-slot same-root held-out outcomes remain unknown.** Their absence from selection does not establish poor held-out performance.

The five selected creation occurrences comprise **four exact recipes**, all obtained by adding Viper in one slot:

| Root | Birth wave / accepted position | Owner | Removal → addition | Final common wins /16 | Selection wins /40 | Held-out wins /256 |
| --- | --- | ---: | --- | ---: | ---: | ---: |
| 1 | 2 /7 | 6 | Bark Golem → Viper | 16 | 31 | 201 |
| 3 | 1 /1 | 7 | Elder Treant Thornstorm → Viper | 16 | 36 | 192 |
| 4 | 1 /2 | 10 | Cinder Beetle → Viper | 13 | 34 | 197 |
| 8 | 2 /3 | 9 | Flame Imp → Viper | 15 | 34 | 195 |
| 11 | 2 /1 | 9 | Flame Imp → Viper | 14 | 33 | 197 |

Root 8’s accepted position 3 came from attempt 6; the intervening rejected attempts are preserved. Roots 8 and 11 selected the same physical recipe on different panels. Its root-6 nomination was not selected and has no root-6 held-out measurement. The reviewer deliberately leaves that cell null rather than substituting 195 or 197 from another root.

Root 1’s sole promising novel recipe is `8c033fca1402ef07c33e155e4abf62ac5774f87929d50168196ae76b01cd9f7e`. It was generated in wave 2, won **8/8 screen +8/8 continuation**, ranked first among challengers, and won **31/40** against the benchmark’s **26/40** in selection. Held-out counts were **201/256 versus 192/256**, the predeclared +3.516-point promising result. The edit adds the missing Viper endpoint and activates all three selected authored routes on owner 6. Those are structural compatibility facts; the observation does not isolate the contribution of Poison, Viper or removing Bark Golem, and it does not qualify this exact team for adoption.

Root 12 supplies a useful contrasting path. Control selected `21e49b61…`, **owner 6: Bark Golem → Dire Wolf**, after a final common score of 15/16 and selection scores of **31/40 versus primary 30/40 and benchmark 28/40**. Its held-out score was **208/256**. Creation selected the primary through a tie with its own challenger at 30/40, and the primary scored **188/256**, still above the benchmark’s 179/256. The −20 method contrast at this root cannot be attributed to a losing selected creation edit: creation selected a reference, and its unselected challenger’s same-root held-out outcome is unknown.

## What the selection diagnostics can and cannot show

Selection halves disagree at **9/12 roots in each arm**. Removing a single observation changes the selected output at **7/12 control roots and 5/12 creation roots**. These are descriptive sensitivity checks on existing training observations, not extra independent trials or proposed selection gates.

Root 1’s five-win benchmark lead is stable in both halves and all leave-one-out checks. Root 3’s eight-win lead is also stable in both halves and all leave-one-out checks, yet its held-out benchmark contrast is negative. Thus, even a wide, locally stable training lead did not guarantee a positive held-out contrast in this archive. No margin or stability threshold was fitted here.

Each arm generated 204 proposal/root occurrences, but only **five in each arm** have same-root held-out observations. The other **199 per arm remain unknown**, including nominated losers. This review can trace observed selection errors and concentration of the measured shortfall; it cannot rank all generated proposals by generalization quality or separate all generation effects from selection effects.

## Recommended next implementation

Prioritize a small, separately versioned final-selector experiment before broadening the generator again. The first isolated hypothesis is **benchmark preference on positive maximum-score ties**, followed by the existing selector for every other case. Keep the legacy default, zero-win health rule, nominee order, non-benchmark tie fallback, generation and panel allocation unchanged. Bind the opt-in selector identity through plan, request, replay and audit records, and test its exact boundaries before designing a fresh comparison.

Do not implement this by simply relabeling the current primary reference. `TowerBatchRacing.Select` gives its primary argument a special override, then falls back to frozen nominee order. Changing that argument can also change a tie when the benchmark is not a leader. Root 9 illustrates this distinction: its primary and challenger both score 30 while the benchmark scores 28, and the challenger precedes the primary in nominee order. That challenger’s same-root held-out outcome is unknown. An explicit benchmark-tie override with legacy fallback isolates the intended change.

This is an engineering hypothesis, not a demonstrated policy improvement. It addresses the root-2 mechanism and does not solve the broad-lead reversal at root 3. Preserve the current `Inconclusive` decision and the unmet larger-evaluation gate. Any new scientific comparison requires a separate prospective design and admission; these held-out observations must not choose a margin or be reused as confirmation evidence.

## Verification and retained evidence

**27 Python tests passed:** 15 new creation-review cases and 12 shared stage-review cases. They exercise all 24 trajectories, exact root-1 edits, authored-route metadata, locality and minimal edit distance, duplicate handling, policy versions, score/seed/membership/ranking tampering, fixed reference priorities, reference-loss arithmetic, shared outcomes and unknown same-root cells. The [creation tests](analysis/test-affinity-creation-stage-review.py) include the counterexample of a stable training lead followed by a held-out loss.

```powershell
python -B -X utf8 'Balance Harness/analysis/test-affinity-creation-stage-review.py'
python -B -X utf8 'Balance Harness/analysis/test-adaptive-racing-stage-review.py'
python -B -X utf8 'Balance Harness/analysis/affinity-creation-stage-review.py'
```

Commands used the bundled workspace Python runtime. Logs: [creation tests](../TestResults/affinity-creation-stage-review-tests-20260923.log), [shared tests](../TestResults/affinity-creation-stage-kernel-tests-20260923.log), [formal review](../TestResults/affinity-creation-stage-review-run-20260923.log).

The single formal review completed in **3.266 seconds**, retaining **1,135,463 bytes**, under a separate **180-second /64-MiB** allowance with a hard watchdog. The full allowance is charged: cumulative recorded charges are **24,094.359 seconds /19,195,820,950 bytes**; cumulative declared maxima are **42,540 seconds /28,118,614,016 bytes**. Prior charges and scientific limits remain intact. Unit tests and exploratory development reads remain separate engineering work.

The [machine review](../TestResults/affinity-creation-stage-review-20260923/review.json) retains every root, accepted proposal path, selection diagnostic, same-root held-out null, source pin and consumed-file hash. The sealed [artifact manifest](../TestResults/affinity-creation-stage-review-20260923/files.json) has SHA-256 **`e5ca00fc0f43c10693908281a77213074a77b384ba8c8df8b02689617a1fc21d`**. It binds the declaration, five producing Python files, creation test log and review. Inputs were rechecked before sealing. The last verified reservation inventory remains **704,982 values across 250 files**; this review did not rescan the live registry.

The [final document and preservation check](../TestResults/affinity-creation-stage-review-verification-20260923.json) verifies the sealed review, consumed-source hashes, table arithmetic, links and unchanged preceding evidence. Scoped whitespace checks pass. The repository-wide check found existing trailing whitespace in the unrelated `LL/docs/strongholds-mechanical-progression-revision.md`; that file was left untouched.

Changed files are the new reviewer, its tests, this report and eight current-status links in the harness guides and preceding implementation/admission/execution reports. No command was blocked. No backend build was needed for these Python-only analysis changes. There are no migrations, application configuration changes, deployments or gameplay-default changes. The closed study, admission, publication verification and execution handoff remain untouched.
