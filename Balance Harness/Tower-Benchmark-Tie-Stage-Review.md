# Benchmark tie pilot: saved-stage diagnosis

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The remaining benchmark deficit comes from strict training-score leaders, beyond the reach of a tie preference.** All six candidate-arm outputs that differ from the benchmark won their final 40-observation selection panel outright. Roots 5 and 8 contribute **52 of the 75 net lost held-out wins**. The selector correctly applied its declared rules; this review found no selection implementation error.

The [closed comparison](Tower-Benchmark-Tie-Pilot-01-Execution.md) remains **AbandonThisConfiguration**: +0.814 percentage points against control, −2.441 points against the fixed benchmark. The tie override recovered 25 net wins at roots 1 and 2, but did not establish a search procedure that improves on the benchmark. No fights, entropy draws, reservations, policy promotion or gameplay changes occurred during this review.

The next useful development step is a **separately versioned benchmark-relative validation and fallback contract**. Freeze one challenger before a fresh paired validation panel, then retain the benchmark when the validation gate is unmet. This is a proposal for addressing weak output decisions, with a concrete fixed-budget option below. It is not evidence that this generator produces strong challengers or that another selector will succeed.

## All-root evidence

The [reviewer](analysis/benchmark-tie-stage-review.py) authenticated consumed scientific, admission and publication records against external manifest pins. It recounted all **24 saved search trajectories**, including creation edits, stage scores, pruning, common-panel ranking, nominees and both final selectors. All **204 accepted proposal positions** and **6,336 paired training observations** match across arms. These observations were physically evaluated in each arm; equality does not erase their recorded fight charges.

Candidate outputs below use the benchmark-tie selector. Selection counts are out of 40; held-out counts are out of 256. Positive net counts favor the candidate. Every root remains included.

| Root | Selected recipe | Selection wins / benchmark | Selection reason | Held-out wins / benchmark | Held-out net wins |
| --- | --- | --- | --- | --- | ---: |
| 1 | Benchmark `96b94357…` | 32 /32 | Benchmark positive tie | 200 /200 | 0 |
| 2 | Benchmark | 30 /30 | Benchmark positive tie | 192 /192 | 0 |
| 3 | Novel `420ce347…` | 33 /31 | Unique maximum | 186 /196 | −10 |
| 4 | Novel `f36adf5f…` | 32 /30 | Unique maximum | 198 /197 | +1 |
| 5 | Novel `b0a26612…` | 33 /32 | Unique maximum | 159 /193 | **−34** |
| 6 | Novel `88f3e02f…` | 30 /28 | Unique maximum | 187 /195 | −8 |
| 7 | Benchmark | 31 /31 | Unique maximum | 189 /189 | 0 |
| 8 | Older primary `399bc776…` | 29 /26 | Unique maximum | 180 /198 | **−18** |
| 9 | Benchmark | 34 /34 | Unique maximum | 192 /192 | 0 |
| 10 | Benchmark | 34 /34 | Unique maximum | 203 /203 | 0 |
| 11 | Benchmark | 34 /34 | Unique maximum | 183 /183 | 0 |
| 12 | Novel `a7ade6c9…` | 35 /32 | Unique maximum | 196 /202 | −6 |

At roots 1 and 2 the control selected the older primary on positive ties and recorded 186 and 181 held-out wins. Selecting the benchmark instead gained 14 and 11 wins. The remaining ten outputs are identical between arms. The two changed roots isolate a modest benefit of this selector on these frozen outputs; the unchanged roots expose its limited scope.

| Candidate output category | Roots | Net wins versus benchmark | Contribution to twelve-root mean |
| --- | --- | ---: | ---: |
| Benchmark | 1, 2, 7, 9, 10, 11 | 0 | 0 points |
| Older primary reference | 8 | −18 | −0.586 points |
| Novel recipes | 3, 4, 5, 6, 12 | −57 | −1.855 points |
| All roots | 1–12 | **−75** | **−2.441 points** |

These are retrospective descriptions of selected outputs, not causal estimates of novelty or reference status. Roots 5 and 8 were chosen for closer inspection after seeing their losses. Their combined 69.3% share describes this archive and is not a prediction for future roots.

## Root 5: a one-win lead selected a losing edit

The selected recipe is `b0a26612010696f4a90d3d366bac3bfbd87df58189bb98e27d370cb6d29ba031`. It is the ninth accepted proposal in wave 1, from attempt 10. It changes **owner 5: Enchanted Fairy → Viper**, leaving every other owner unchanged. The authored creation step adds the missing endpoint and records three newly active damage-affinity routes. The recipe is legal and the route metadata is correct; structural compatibility does not establish a net combat benefit after removing another Essence.

| Stage | Recipe wins | Benchmark wins | Paired gains / losses |
| --- | ---: | ---: | --- |
| Wave 1 screen, 8 observations | 6 | 7 | 1 /2 |
| Wave 1 continuation, 8 | 5 | 5 | 2 /2 |
| Wave 2 screen, 8 | 8 | 8 | 0 /0 |
| Wave 2 continuation, 8 | 6 | 4 | 2 /0 |
| Selection, 40 | **33** | **32** | **7 /6** |
| Held-out, 256 | **159** | **193** | **30 /64** |

The recipe survived both waves and reached nomination after a final common score of 14/16, versus the benchmark's 12/16. Final selection then favored it by a single win. This was not an overlooked positive tie: the benchmark was below the unique leader, so both declared selectors chose the edit.

The selection halves choose different outputs. Under the benchmark-tie rule, removing any of seven observations changes the selected output; under the control rule none does, because a resulting tie can still favor the challenger through the frozen nominee order. This difference is a property of the two rules applied to saved training data, not seven new independent failures.

The held-out deficit is −34/256, or **−13.281 points**. The records demonstrate a training-to-held-out reversal for this exact whole-team edit. They do not isolate whether removing Enchanted Fairy, adding Viper, owner-specific interactions or stochastic variation caused the deficit. No Essence ban or damage-affinity blacklist follows from this one selected recipe.

## Root 8: reference status did not protect against a strict noisy lead

The older primary reference `399bc776…` is retained throughout the search and did not depend on a new proposal surviving pruning. All three references remain eligible on the final panel.

| Stage | Older primary wins | Benchmark wins | Paired gains / losses |
| --- | ---: | ---: | --- |
| Wave 1 screen, 8 observations | 6 | 6 | 1 /1 |
| Wave 1 continuation, 8 | 5 | 4 | 3 /2 |
| Wave 2 screen, 8 | 6 | 7 | 1 /2 |
| Wave 2 continuation, 8 | 2 | 5 | 1 /4 |
| Selection, 40 | **29** | **26** | **10 /7** |
| Held-out, 256 | **180** | **198** | **38 /56** |

Despite trailing on the final common training panel, the primary reference became the unique selection leader. Its three-win lead survives all single-observation removals, while the two halves choose different outputs. Held-out performance is −18/256, or **−7.031 points**, relative to the benchmark.

The inherited primary tie preference did not determine this choice. Relabeling the primary reference or adding another positive-tie priority would not change a strict maximum. A benchmark-relative output gate must apply to other retained references as well as novel recipes.

## Limits of the diagnosis

The candidate selector's half-panel choices disagree at **7/12 roots**; single-observation removal changes outputs at **4/12 roots**, with 31 changes across the 480 removals. Control figures are 9/12 and 4/12, with 29 changes. These are dependent sensitivity diagnostics, not confidence intervals, extra replications or policy promotion criteria.

Local stability does not identify winners reliably. Root 12 has a three-win benchmark lead, identical choices in both halves and no leave-one-out changes, yet loses six net held-out wins. Root 4 has a two-win lead and similarly stable choices, then gains one held-out win. A fitted margin or stability threshold would use the evaluation evidence to design its own apparent success. No threshold sweep or retrospective replacement-policy score was performed.

Each arm accepts **204 proposal/root occurrences covering 54 distinct recipes**, with 182 one-slot and 22 two-slot edits. There are 24 novel nominee occurrences but only five selected novel occurrences. Only those five have same-root held-out measurements; **199 generated occurrences and 19 novel nominee occurrences remain unmeasured on their own held-out panels**. Repeated recipes in other roots do not fill these cells. All five selected edits replace one slot; the unselected two-slot recipes remain unknown.

The generator schedules damage-affinity creation from the benchmark at every accepted position. This archive cannot show that a better generated candidate was available and missed, nor that every generated candidate was weak. It supports improving the decision to depart from the benchmark, while leaving proposal quality unresolved. The previous [frozen-pool diagnostic](Tower-Frozen-Pool-Recognition-Execution.md) remains evidence about its own earlier pool, not an evaluation of these 199 missing cells.

## Proposed next contract, before another scientific run

Implement a separate opt-in output-validation policy with a benchmark fallback. Preserve the current generator and both racing waves so the first test changes the final decision stage. Use this concrete **proposed, unproven** allocation as a starting design:

| Work per search arm | Existing | Proposed |
| --- | ---: | ---: |
| Two racing waves | 328 fights | 328 fights, unchanged |
| Five nominees on a fresh selection panel | 5 ×40 =200 | 5 ×16 =80 |
| One frozen challenger versus benchmark on fresh validation | 0 | 2 ×60 =120 |
| Total | **528** | **528** |

The 16/60 split is a fixed-budget arithmetic proposal, not an optimum estimated from these held-out results. No claim of power for small improvements is made. Its cost is less precise challenger nomination and potentially frequent fallback; its purpose is to reserve independent evidence for the decision to replace a strong reference.

1. On the 16-observation nomination panel, choose one non-benchmark nominee with the existing positive-win ranking, primary tie fallback and zero-win health ordering. The two other references are eligible challengers alongside the two novel nominees. Freeze that exact recipe before observing validation.
2. Evaluate the frozen challenger and benchmark on all 60 new common values. Do not inspect intermediate results to stop, replace the challenger or enlarge the panel. Even if the benchmark led nomination, execute the same declared validation allocation.
3. Prospectively define and test a paired superiority gate. A simple candidate is a one-sided exact discordant-pair test at 0.05: with `G` challenger-only wins and `L` benchmark-only wins, require `G > L` and `sum(comb(G+L, k), k=G..G+L) / 2**(G+L) <= 0.05`. Zero discordance and every unmet gate return the benchmark. Freeze the gate before fresh execution; do not evaluate alternatives on this pilot's held-out panels. This is a per-search output rule, not a family-wide adoption guarantee.
4. Keep the eventual held-out output evaluation separate from all nomination and validation values. Compare against both the frozen search control and benchmark-only output. Report fallback frequency, novel-output frequency, paired-root gains and costs. Frequent fallback by itself does not establish useful search improvement.

The immediate implementation target is the **zero-combat policy contract and deterministic synthetic tests**, followed by independent native replay/audit support. `TowerBatchRacing` currently treats the final panel as one five-member selection stage; the new two-stage contract must have an explicit version and separately recorded freezes. Extend the proposal plan and study/audit bindings without changing existing v3/v4 behavior. Verify the 328+80+120 accounting, nominee ordering, frozen challenger identity, paired seeds, exact gate boundaries, zero-discordance fallback and missing/partial observation rejection before designing runtime admission.

Any future comparison needs its own prospective plan, resource admission, fresh permanent value allocation and decision criteria. The current pilot remains closed and cannot be retried or expanded. This review implements the diagnosis only; the proposed policy has not been implemented or tested on combat outcomes.

## Evidence and verification

The primary records are the [published result](../TestResults/balance/tower-benchmark-tie-pilot-01-20260923/result.json), [root 5 pair](../TestResults/balance/tower-benchmark-tie-pilot-01-20260923/study/pair-05.json), [root 8 pair](../TestResults/balance/tower-benchmark-tie-pilot-01-20260923/study/pair-08.json) and [machine review](../TestResults/benchmark-tie-stage-review-20260923/review.json). The saved [admitted selector source](../TestResults/benchmark-tie-admission-20260923/source/LL/tools/BalanceHarness/TowerBatchRacing.cs), lines 208–228, defines strict-score selection and the positive-tie override. The reviewer authenticates that source before interpreting the records.

**40 Python tests passed:** 13 selector-review tests, 15 creation-review regression tests and 12 shared racing-review regression tests. They cover every root, selector identities and boundaries, paired contrast ordering, exact root-5 edits, the stable-but-losing root-12 counterexample, changed trajectories, missing same-root outcomes and evidence tampering. The shared analysis helpers now accept an explicit benchmark-tie diagnostic option; their existing default behavior remains covered by the regression suites. No C# implementation or admitted executable changed.

Commands used the bundled workspace Python runtime with `-B -X utf8`:

```powershell
python -B -X utf8 'Balance Harness/analysis/test-benchmark-tie-stage-review.py'
python -B -X utf8 'Balance Harness/analysis/test-affinity-creation-stage-review.py'
python -B -X utf8 'Balance Harness/analysis/test-adaptive-racing-stage-review.py'
python -B -X utf8 'Balance Harness/analysis/benchmark-tie-stage-review.py'
```

Logs: [selector tests](../TestResults/benchmark-tie-stage-review-tests-20260923.log), [creation regression](../TestResults/benchmark-tie-stage-creation-regression-20260923.log), [racing regression](../TestResults/benchmark-tie-stage-kernel-regression-20260923.log), [formal review](../TestResults/benchmark-tie-stage-review-run-20260923.log). The review output is closed; do not rerun its command against the same directory.

The formal read-only review completed in **3.391 seconds**, retaining **1,356,789 bytes** under a separate **180-second /64-MiB** allowance with a hard watchdog. The full allowance is charged: cumulative recorded charges are **27,191.671 seconds /22,349,413,892 bytes**; cumulative declared maxima are **55,020 seconds /35,769,024,512 bytes**. Prior charges and scientific limits remain intact. Unit tests and exploratory reads are separate engineering work.

The sealed [review manifest](../TestResults/benchmark-tie-stage-review-20260923/files.json) has SHA-256 **`13b860996ca4c31561aea0a3ac9f0252c6e0a98d904b3d85e48d214de58be023`**. It binds the declaration, six producing/test Python files, test log and machine review. Consumed inputs were rechecked before sealing. The pinned [publication verification](../TestResults/benchmark-tie-pilot-01-publication-verification-20260923/verification.json) supplies the complete battle reconstruction and last live-history audit: **721,365 permanently excluded values across 252 files**. This review does not repeat the full battle audit or rescan the registry.

Changed files are the new reviewer, its tests, two shared analysis helpers, this report and seven current-status links in the harness guides and preceding reports. The [document and preservation check](../TestResults/benchmark-tie-stage-review-verification-20260923.json) records source pins, table arithmetic, local links and scoped whitespace checks. No required command was blocked; no backend build was needed for these Python-only analysis changes. There are no migrations, application configuration changes, deployments or gameplay-default changes. The sealed studies, admissions, publication records, execution handoffs and original prospective plan remain untouched.
