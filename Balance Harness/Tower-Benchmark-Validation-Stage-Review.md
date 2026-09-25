# Benchmark validation pilot: saved-stage diagnosis

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The final-stage changes recovered losses through benchmark fallback, but produced no novel output.** Of 204 accepted candidate/root occurrences, 24 reached nomination, seven became validation challengers, and none of those seven passed the gate. Six of the seven novel challengers lost more paired validation wins than they gained. The one positive novel contrast was too small to pass the declared gate. The reviewer found no disagreement with the declared generation, racing, nomination or validation rules.

The [completed v4-versus-v5 pilot](Tower-Benchmark-Validation-Pilot-01-Execution.md) remains **Inconclusive**: +1.204 percentage points against v4 and −0.423 points against the fixed benchmark. The sole gate pass was an existing reference, which subsequently lost 13 net held-out wins. This review used saved evidence only: **zero fights, entropy draws, reservations, policy changes or promotions**.

**Next: design a versioned frozen-pool recognition diagnostic for the affinity-creation candidates.** Measure nominees and a prospectively chosen sample of discarded candidates on fresh paired panels. This addresses the remaining evidence gap: the current archive cannot distinguish a weak generated pool from useful recipes lost by racing or nomination. Another gate threshold fitted to this pilot would not answer that question.

## What was verified

The separate [reviewer](analysis/benchmark-validation-stage-review.py) authenticated consumed files against the scientific, admission and publication manifest pins. It reused the existing generation/racing checks for v4, verified the candidate arm's identical first four panels and nominees, and independently recounted v5's final panels. All **204 accepted positions**, **3,936 racing observations** and **960 shared nomination observations** agree between arms. Shared observations were physically evaluated in each arm; their original fight charges remain recorded.

New checks cover the exact 16-seed nomination subset, 60 validation seeds disjoint from the entire control selection panel and held-out panel, frozen challenger identity, party/seed order, complete panel lengths, scores, paired contrasts, integer gate arithmetic and selected endpoint. All twelve roots are included. Archived raw battle reconstruction and complete live-history verification remain the responsibility of the already completed publication verification; this review did not repeat them.

## Candidate funnel and missing outcomes

Counts below refer to candidate/root occurrences, not independent distinct recipes. The 204 accepted occurrences contain **57 distinct recipes** across roots; each root has 17 accepted candidates. There were 252 construction attempts and 48 rejected attempts.

| Stage | Novel occurrences | Same-root held-out observations available |
| --- | ---: | ---: |
| Accepted generation | 204 | 3 |
| Final nominees | 24 | 3 |
| Frozen validation challenger | 7 | 1 |
| Passed validation | 0 | 0 |
| Final v5 output | 0 | 0 |

The other five validation challengers were retained references. One passed. Thus **201 generated occurrences, 21 novel nominee occurrences and six novel validation challengers have no same-root held-out measurement**. The three measured novel recipes were selected by v4 at roots 3, 8 and 10; their evidence is available to the diagnosis even though v5 fell back to the benchmark. Only the root-3 recipe was also v5's validation challenger.

There are 176 one-slot and 28 two-slot accepted edits. Nomination retains 18 one-slot and six two-slot occurrences; validation tests six one-slot and one two-slot novel challenger. These are coverage counts, not estimates of either edit class's effectiveness. Authored affinity activation establishes structural compatibility; it does not establish the net benefit of replacing another Essence.

No held-out value was borrowed from a different root, imputed for an unmeasured recipe, or substituted for the actual frozen output. A complete account of missed strong candidates is therefore still unavailable.

## All twelve nomination and validation decisions

Nomination counts are out of 16; validation counts are out of 60. Each pair shows challenger / benchmark wins. Gains and losses count discordant paired outcomes. `Unknown` means that exact challenger was not evaluated on this root's 256 held-out values.

| Root | Frozen challenger | Nomination | Validation | Gains / losses | Gate | Challenger held-out / benchmark |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Novel `55445ccd…` | 13 /12 | 42 /51 | 3 /12 | Fail | Unknown /189 |
| 2 | Primary `399bc776…` | 14 /14 | 47 /51 | 9 /13 | Fail | Unknown /204 |
| 3 | Novel `55445ccd…` | 14 /14 | 43 /44 | 7 /8 | Fail | 180 /187 |
| 4 | Primary `399bc776…` | 15 /11 | 52 /41 | 17 /6 | **Pass** | **185 /198** |
| 5 | Primary `399bc776…` | 15 /16 | 40 /40 | 13 /13 | Fail | Unknown /190 |
| 6 | Novel `b3d2cd5a…` | 14 /12 | 41 /45 | 8 /12 | Fail | Unknown /186 |
| 7 | Novel `91465eec…` | 13 /12 | 42 /44 | 9 /11 | Fail | Unknown /199 |
| 8 | Primary `399bc776…` | 12 /12 | 41 /47 | 9 /15 | Fail | Unknown /192 |
| 9 | Novel `420ce347…` | 12 /15 | 46 /43 | 11 /8 | Fail | Unknown /189 |
| 10 | Novel `fc95321d…` | 14 /11 | 37 /41 | 11 /15 | Fail | Unknown /193 |
| 11 | Other reference `8287f779…` | 11 /11 | 37 /42 | 11 /16 | Fail | Unknown /192 |
| 12 | Novel `a4270a2d…` | 11 /10 | 47 /49 | 10 /12 | Fail | Unknown /184 |

Ten challengers are unique nonbenchmark nomination leaders. Roots 1 and 3 use frozen nominee order to resolve positive ties between novel candidates; neither uses the primary-reference tie preference. The benchmark is deliberately excluded from challenger selection. Consequently roots 5 and 9 still complete validation even though the benchmark leads nomination. That behavior matches the frozen contract.

The seven novel validation contrasts are −9, −1, −4, −2, +3, −4 and −2 net wins. The gate's zero novel passes therefore cannot be attributed solely to rejecting positive validation leads: six contrasts are already negative. These selected, noisy observations do not prove that every novel challenger has a negative true effect, or estimate how many improvements the gate might miss.

## Where the 37 recovered wins came from

| Root | v4 selected recipe | v4 selection / benchmark, out of 40 | v5 validation challenger | Held-out v4 / benchmark | Recovery from v5 fallback |
| --- | --- | --- | --- | --- | ---: |
| 3 | `55445ccd…` | 33 /31 | Same recipe | 180 /187 | +7 |
| 8 | `88f3e02f…` | 28 /25 | Primary reference | 181 /192 | +11 |
| 10 | `7863c579…` | 30 /27 | Different novel `fc95321d…` | 174 /193 | +19 |

All other outputs agree between arms. At root 3, the same challenger reaches validation, fails with 7 gains and 8 losses, and the benchmark fallback avoids its seven-win held-out deficit. At roots 8 and 10, **the shorter nomination panel has already changed the challenger**. Their gains belong to the combined nomination-plus-validation procedure; the saved archive does not isolate the gate's effect on the recipes v4 selected. Those two recipes have no validation measurements in these roots.

Root 3's recipe changes owner 3 from Venomous Spiderling to Viper. It records the declared newly active owner-specific affinity, yet its actual whole-team held-out contrast is −7/256. This demonstrates a legal affinity-creating edit with a negative observed outcome; it does not isolate the causal effect of either Essence or justify banning that edit elsewhere.

Across all roots, v4's three novel outputs contribute −37 net wins against the benchmark and its primary-reference output contributes −13. V5 removes the former losses through fallback but retains the latter: **−13/3,072 = −0.423 percentage points**. These are accounting decompositions of observed outputs, not estimates of the effect of novelty or reference status.

## Root 4: the passing reference reverses on held-out fights

The primary reference remains eligible throughout both searches. It is not a generated candidate that survived pruning.

| Stage | Primary wins | Benchmark wins | Paired gains / losses |
| --- | ---: | ---: | --- |
| Wave 1 screen, 8 | 6 | 8 | 0 /2 |
| Wave 1 continuation, 8 | 7 | 6 | 1 /0 |
| Wave 2 screen, 8 | 5 | 7 | 0 /2 |
| Wave 2 continuation, 8 | 6 | 7 | 0 /1 |
| Nomination, 16 | 15 | 11 | 5 /1 |
| Validation, 60 | **52** | **41** | **17 /6** |
| Held-out, 256 | **185** | **198** | **44 /57** |

The exact one-sided validation tail is `145499 /8388608` (about 0.01735), meeting the fixed 1/20 gate. The held-out contrast then reverses to −13/256, or −5.078 percentage points. The saved arithmetic is correct. An observed reversal does not establish the recipe's true effect or turn this one root into an estimate of the gate's false-positive rate. A gate pass is a provisional search decision, not an independent team confirmation.

V4 also selects the primary here: it ties a novel recipe at 30/40, above the benchmark's 29/40, and wins the primary positive-tie preference. Thus this root contributes the entire remaining v5 benchmark deficit, but no v5-versus-v4 difference.

## Next implementation target

The unresolved question is whether the current generator supplies useful candidates that racing or nomination discards. Only three of 204 generated occurrences have held-out evidence, and the seven validation challengers are selected through noisy training panels. Neither the validation subset nor the earlier diagnostic of a different generator answers the whole-pool question for this cohort.

Build a **new, separately versioned recognition-plan adapter** for this pilot's frozen affinity-creation pool, following the existing [frozen-pool diagnostic approach](Tower-Frozen-Pool-Recognition-Implementation.md). Its design should retain all roots and references, include final nominees and near misses, and define a probability sample of lower-ranked candidates without selecting them by observed outcomes. Preserve original party/scenario identity and root membership. Repeated recipes across roots remain separate occurrences with separate future panels.

Before execution, freeze catalogue membership, sampling rules, endpoints, uncertainty reporting, fight count, resource allowances and fresh-value allocation. Keep sampling uncertainty distinct from combat uncertainty, retain nulls for unsampled recipes, and report the full sampled cohort. Reuse the current validation policy unchanged for provenance; this diagnostic should not tune it or retrospectively score a replacement selector. Any later policy comparison or team confirmation needs its own fresh evidence.

This recommendation is a development diagnosis, not an executed or admitted experiment. No new catalogue sampling, recognition run or larger validation pilot was launched here. The completed pilot's prospective criterion for a larger evaluation remains unmet.

## Verification, accounting and changed files

**27 tests passed**: 14 new [validation-review tests](analysis/test-benchmark-validation-stage-review.py) and 13 legacy selector-review regressions. They cover saved pass/fail cases, integer gate boundaries, altered identities, changed trajectories, seed reuse, incomplete/reordered panels, score/contrast tampering, null evidence and manifest authentication. The all-root review then completed once in **8.047 seconds**, retaining **1,357,585 bytes**. Its **180-second /64-MiB** allowance was declared before analysis and is fully charged; there was no retry.

Commands used the bundled Python runtime with `-B -X utf8`:

```text
Balance Harness/analysis/test-benchmark-validation-stage-review.py
Balance Harness/analysis/test-benchmark-tie-stage-review.py
Balance Harness/analysis/benchmark-validation-stage-review.py
```

The [sealed review](../TestResults/benchmark-validation-stage-review-20260924/review.json) and [declaration](../TestResults/benchmark-validation-stage-review-20260924/declaration.json) retain all-root details, consumed-input hashes, source/test snapshots and accounting. External review-manifest pin: `bde705235252a4afd04cad3ce5256060e5453422656e209da7ccb8d91ed452f9`.

Source pins remain scientific manifest `fd96c4d21a27a32ad17ab750d2972a6b7dab54d09950661042b9f3af56859504`, closeout `15e192e436940e9bea9df5849b0305b02b71ffc33854cf025c5edb71272c46bb`, admission manifest `e6fdb1369df6585fde905523e3190af221ef0dc9323e4b3ae3b1bf37659b5576`, and publication-verification manifest `58d10de4135efa1451b9085c859752129b7a236d139037abd07126f0a75e2911`.

Cumulative recorded charges are **30,225.312 seconds /25,458,095,271 bytes**. Cumulative declared maxima are **67,500 seconds /43,419,435,008 bytes**. The last complete live-history verification remains **737,749 excluded values across 254 files**, inherited from publication and explicitly not rescanned here. Development tests and documentation verification are separate engineering work.

Changed repository files are the new reviewer, its tests, this report and current-status banners in nine harness/assessment/implementation/admission documents. Original document bodies and scientific evidence remain intact. The [final verification record](../TestResults/benchmark-validation-stage-review-verification-20260924/verification.json) records arithmetic, links, preserved pins and document-body checks. No required verification command was blocked. No C# or admitted-runtime change required a backend rebuild or rerun. There are **no migrations, application configuration changes, deployments or gameplay-default changes**.
