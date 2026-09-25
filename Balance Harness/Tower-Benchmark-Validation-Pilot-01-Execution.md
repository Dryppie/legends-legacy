# Benchmark validation pilot 01: Inconclusive

24 September 2026. Target: the offline Balance Harness and captured floor-5 scope.

**The single admitted v4-versus-v5 comparison completed 16,768 physical fights and reached Inconclusive.** All twelve paired searches and all held-out panels completed. Native reconstruction, the independent Python audit, publication and separately bounded verification after publication passed. There was one launch, with no retry, replacement, refill or resume. No gameplay or search default was promoted.

The v5 candidate averaged **+1.204 percentage points versus the v4 benchmark-tie selector** and **-0.423 points versus the fixed benchmark**. Outputs differed at **3/12 roots**. The validation gate retained a challenger at **1/12 roots** and fell back to the benchmark at **11/12**.

Of the 1 gate-passing outputs, 1 were existing retained references. The candidate selected 0 novel recipes in this run. A positive comparison with v4 alone does not establish improvement over the fixed benchmark.

| Held-out measure | Result |
| --- | ---: |
| v4 control win rate | 73.340% |
| v5 candidate win rate | 74.544% |
| Fixed benchmark win rate | 74.967% |
| Candidate minus control, equal-root mean | +1.204 points |
| Descriptive 95% root interval | -0.324 to +2.732 points |
| Candidate minus benchmark, equal-root mean | -0.423 points |
| Descriptive 95% root interval | -1.355 to +0.508 points |
| Different final recipes | 3/12 |
| Novel candidate outputs / promising novel outputs | 0 / 0 |

Role totals are 2,253 control wins, 2,290 candidate wins and 2,303 benchmark wins, each out of 3,072 role observations. Identical recipes share physical measurements within a root. The intervals describe variation across these twelve retained roots, using 11 degrees of freedom. This development pilot does not establish future-root reliability.

## Interpretation and all-root results

The prospective decision rules are unchanged: abandon at or below -2 points for either mean; otherwise report no observed differentiation when no roots differ; otherwise require at least +2 method points, a nonnegative benchmark mean and at least three differing roots for a larger fresh evaluation. Novelty does not determine this comparison's decision. The independently recounted result is **Inconclusive**: The larger-evaluation conditions are not all satisfied, and neither abandonment threshold is crossed.

Both arms used the same affinity-creation generator, roots and first four racing panels, with all 204 proposal positions matching. The v5 nomination panel uses the first sixteen of v4's forty selection values; its sixty validation values are disjoint from the entire v4 selection panel and all held-out panels. This compares the **whole final-stage change**, including the smaller nomination sample and the validation gate. It does not isolate the gate alone.

Wins below are out of 256; differences are percentage points. G/L are gained/lost wins on the separate sixty-pair validation panel. Every root remains included.

| Root | v4 wins | v5 wins | Benchmark wins | vs v4 | vs benchmark | Validation G/L | v5 output |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | 189 | 189 | 189 | +0.000 | +0.000 | 3/12 | Benchmark |
| 2 | 204 | 204 | 204 | +0.000 | +0.000 | 9/13 | Benchmark |
| 3 | 180 | 187 | 187 | +2.734 | +0.000 | 7/8 | Benchmark |
| 4 | 185 | 185 | 198 | +0.000 | -5.078 | 17/6 | Challenger |
| 5 | 190 | 190 | 190 | +0.000 | +0.000 | 13/13 | Benchmark |
| 6 | 186 | 186 | 186 | +0.000 | +0.000 | 8/12 | Benchmark |
| 7 | 199 | 199 | 199 | +0.000 | +0.000 | 9/11 | Benchmark |
| 8 | 181 | 192 | 192 | +4.297 | +0.000 | 9/15 | Benchmark |
| 9 | 189 | 189 | 189 | +0.000 | +0.000 | 11/8 | Benchmark |
| 10 | 174 | 193 | 193 | +7.422 | +0.000 | 11/15 | Benchmark |
| 11 | 192 | 192 | 192 | +0.000 | +0.000 | 11/16 | Benchmark |
| 12 | 184 | 184 | 184 | +0.000 | +0.000 | 10/12 | Benchmark |

The 3 differing benchmark fallbacks contribute +37 net wins against v4. Gate-passing outputs had these held-out results: root 4: 185/256 candidate wins versus 198/256 benchmark wins. These are descriptive outcomes of the frozen outputs, without substituting unmeasured alternatives.

The gate compares the exact one-sided binomial tail with 1/20, requiring more gains than losses. Gate passes are provisional search decisions, not independent team confirmations. The report recounts all twelve integer tails and both held-out endpoint summaries. Unselected recipes have no new same-root held-out observations; their outcomes remain unknown.

Review the saved nomination and validation decisions before designing another experiment. The prospective gate for a larger fresh evaluation is unmet. No decision threshold was changed after observing results, and this study's values cannot become fresh evidence for a later comparison.

## Execution and reservations

The [admission](Tower-Benchmark-Validation-Admission.md), [qualified plan](../TestResults/benchmark-validation-admission-20260924/plan.json), captured dependencies and policies were used unchanged. The study version is `tower-benchmark-validation-comparison-v1`. The original prospective plan and all earlier scientific archives remain intact.

Search used **12 ×2 ×528 =12,672 fights**. All 24 outputs were durably frozen before held-out evaluation. Held-out work used **16 physical recipe/root cells ×256 =4,096 fights**, for **16,768 total** under the 21,888-fight maximum.

The single 16,384-word entropy draw contained **0 historical collisions** and **0 within-batch duplicates**. It created **16,384 permanent fresh reservations**. Exactly **4,668** values were assigned; **11,716 unused fresh values remain permanently excluded**. Collisions did not trigger replacement draws. The reservation is complete.

| Scientific resources through terminal receipt | Actual | Limit |
| --- | ---: | ---: |
| Native phase, including enclosing work | 1,115.562 seconds | 9,000 seconds |
| Both audits, publication and terminal sealing | 238.079 seconds | 1,800 seconds |
| Scientific total | 1,353.641 seconds (22.56 minutes) | 10,800 seconds |
| Native retained bytes | 1,898,293,179 | 5,905,580,032 |
| Audit/publication retained bytes | 2,428,648 | 536,870,912 |
| Total retained bytes | 1,900,721,827 | 6,442,450,944 |

All four owned scientific processes exited successfully without timeout or active descendants. Native audit took 154.844 seconds and independent audit took 53.390 seconds within the enclosing audit phase. Neither audit ran new fights or allocated values.

The [result](../TestResults/balance/tower-benchmark-validation-pilot-01-20260924/result.json), [completion](../TestResults/balance/tower-benchmark-validation-pilot-01-20260924/completion.json) and [terminal closeout](../TestResults/balance/tower-benchmark-validation-pilot-01-20260924/closeout.json) retain authoritative data. External pins:

- Scientific closeout: `15e192e436940e9bea9df5849b0305b02b71ffc33854cf025c5edb71272c46bb`.
- Scientific archive manifest: `fd96c4d21a27a32ad17ab750d2972a6b7dab54d09950661042b9f3af56859504`.
- Consumed admission manifest: `e6fdb1369df6585fde905523e3190af221ef0dc9323e4b3ae3b1bf37659b5576`.
- Execution declaration: `cfbcbd2b364f8c0f3ccc6cb4bcfc5a9db2a9ef9ae807b8fd217ef5805fe648ea`.

The [execution declaration](../TestResults/benchmark-validation-pilot-01-execution-declaration-20260924.json) preceded the single launch; the [launch log](../TestResults/benchmark-validation-pilot-01-execution-20260924.log) records its closeout pin. This output and admission are consumed and cannot be resumed or relaunched.

## Final verification and accounting

The separate [publication verification](../TestResults/benchmark-validation-pilot-01-publication-verification-20260924/verification.json) authenticated the admission and published archive, invoked the admitted native verifier against the external pin, matched the reconstructed result and independently scanned complete live history under registry/output leases. Verification and sealing took **182.031 seconds**, retaining **82,091 bytes**, with zero new fights or values.

Live history now contains **737,749 permanently excluded values across 254 files**. The increase from 721,365 values across 252 files is exactly this study's reservations and two ledgers. Every preceding history pin matched.

The **600-second /64-MiB** verification allowance was declared before launch and is fully charged. It never extends either scientific partition. Cumulative recorded charges are **30,045.312 seconds /25,390,986,407 bytes**; cumulative declared maxima are **67,320 seconds /43,352,326,144 bytes**. All prior charges remain included. Development tests and report checks are separate engineering work.

**18 publication-verifier tests passed**: ten new v5 cases and eight legacy regressions. The separate [v5 verifier](analysis/verify-benchmark-validation-publication.py) requires the new study identity and 4,668-value allocation; [tests](analysis/test-benchmark-validation-publication.py) reject legacy allocations, altered history, lost unused values, incomplete reservations and inconsistent accounting. The legacy verifier remains byte-for-byte unchanged. The admitted implementation and its existing backend evidence were authenticated; no C# source or admitted runtime changed, so no rebuild or backend rerun was needed.

Both scientific audits, publication verification and report arithmetic checks passed. No required verification command was blocked. Evidence includes the [verification log](../TestResults/benchmark-validation-pilot-01-publication-verification-20260924.log), [v5 test log](../TestResults/benchmark-validation-publication-tests-20260924.log), [legacy test log](../TestResults/benchmark-validation-legacy-publication-tests-20260924.log) and [machine summary](../TestResults/benchmark-validation-pilot-01-execution-review-20260924/summary.json). Publication-verification manifest: `58d10de4135efa1451b9085c859752129b7a236d139037abd07126f0a75e2911`.

Changed repository files are the separate verifier and tests, this report and current-status banners in nine harness/assessment/implementation/admission documents. New TestResults artifacts retain declarations, logs, the sealed study, verification and report checks. There are no migrations, application configuration changes, deployments or gameplay-default changes.

The [closed execution handoff](../TestResults/benchmark-validation-pilot-01-execution-handoff-20260924.json) records the result and accounting. The pilot is closed. Review the saved nomination and validation decisions before designing another experiment. The prospective gate for a larger fresh evaluation is unmet.
