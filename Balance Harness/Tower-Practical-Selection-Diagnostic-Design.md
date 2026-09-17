# Practical Tower selection diagnostic: precision and resource admission

**Historical design snapshot:** the diagnostic described below has since [completed its native run](Tower-Practical-Selection-Diagnostic-Execution-Review.md) with `NoSelectionMissDemonstrated`. The [adoption review](Tower-Practical-Primary-Adoption-Readiness.md) and [fixed-team plan](Tower-Practical-Fixed-Team-Confirmation-Plan.md) record the current follow-up. The [fixed-team implementation review](Tower-Practical-Fixed-Team-Confirmation-Implementation-Review.md) records its controller and fixtures; the subsequent [completed fixed-team confirmation](Tower-Practical-Fixed-Team-Confirmation-Execution-Review.md) completed 16,500 trials and both audits, returning AdoptFixedTeam for exact candidate `399bc776…`. The earlier diagnostic retains its original endpoint. Earlier NoGo statements below describe design-time admission, not the completed run. The calculations and JSON remain unchanged.

17 September 2026. Target: the offline BalanceHarness. This completes the analysis follow-up to [hypothesis qualification](Tower-Practical-Hypothesis-Qualification.md). **The concrete planning specification is four frozen nominees, 1,000 independent trials per nominee and 4,640 total fights. Launch remains NoGo.** The mathematical model supports an 80% conditional detection target for a true selection miss of at least 16 percentage points. Retained evidence does not establish that such a miss exists, that the current sampler satisfies the model, or that this workload fits a practical resource envelope.

The [read-only calculation](analysis/practical-selection-diagnostic-design.py) produces the [JSON results and evidence hashes](Tower-Practical-Selection-Diagnostic-Design.json). It performs deterministic probability arithmetic and reads sealed resource records. It generates no parties, seeds, random draws or gameplay outcomes. No implementation, experiment or allocation is included.

The subsequent [execution and audit contract](Tower-Practical-Selection-Diagnostic-Contract.md) fixed the four-nominee freeze, post-freeze entropy and interruption rules. Implementation, resource qualification and [native execution](Tower-Practical-Selection-Diagnostic-Execution-Review.md) later completed under separately recorded scopes. The single entropy batch reserved 2,048 values plus 41 search values, and all 4,640 fights passed their built-in audits. This design's conditional 81.09% rounded detection bound and historical resource illustrations remain unchanged; they are not measurements or guarantees for the later fixed-team plan.

## Question and proposed decision

The question is whether the unchanged practical policy selects a meaningfully worse party than another party already in its four-member nomination set. The native run's 26–25 selection margin and unconfirmed runner-up motivate measuring that uncertainty. They do not estimate its size or establish that the runner-up is stronger.

| Component | Proposed fixed rule |
| --- | --- |
| Search | One future episode of the unchanged incumbent-preserving policy: same cohort, two anchors, legal pool, canonical order and proposal limit; 64 parties on eight discovery trials, four nominees on 32 selection trials. |
| Freeze | Preserve all four distinct nominees and the original selection primary before opening independent outcomes. If four valid distinct nominees cannot be frozen, the diagnostic is incomplete; do not refill or alter nomination. |
| Measurement | All four nominees on the same new 1,000-trial paired panel, separate from construction, discovery, selection and all historical reservations. No choice of nominees or panel size from confirmation results. |
| Family | Four recipe-rate intervals and six directional gain/loss intervals, two for each other nominee versus the frozen primary: **10 quantities**. Bonferroni-adjusted Wilson intervals use nominal family coverage 95%; their finite-sample coverage remains approximate. |
| Positive | At least one other nominee has a recipe-rate lower bound of at least 10%, an observed win-rate gain of at least five points against the primary, and a strictly positive adjusted paired lower bound. Report `SelectionMissDemonstrated`. |
| Complete negative | Report `NoSelectionMissDemonstrated`, with all four rates and all three paired comparisons. This is not an equivalence result or proof that selection is adequate. |
| Interrupted or invalid | Report incomplete/invalid with spent resources and retained reservations. No strength conclusion from a partial panel. Stop at the frozen end or any validity/resource boundary. |

The primary remains the selected output even after a positive diagnostic. A positive result would motivate designing an explicit selector change and an equal-information/equal-cost comparison; it would not establish that change's benefit or authorize its execution. A single episode cannot establish method reliability or locate the best party among the 60 non-nominees. No new panel, candidate, endpoint or automatic adoption follows either result.

The current [allocation contract](../LL/tools/BalanceHarness/TowerPracticalAllocation.cs#L23) caps confirmation at 1,000 trials. This planning count respects that cap; the earlier 1,024-trial illustration was not a valid current allocation request. The [practical workflow](../LL/tools/BalanceHarness/TowerPracticalSearch.cs) confirms one selected output plus the references, with its existing seven-quantity gate. It does **not** implement this diagnostic. Increasing `GeneratedFinalists` is insufficient: [finalist selection](../LL/tools/BalanceHarness/TowerBossStudyPolicy.cs#L33) also applies alternative/capability/behavior rules. All four nominees and family 10 need a separate explicit diagnostic contract and verifier, without changing current command semantics.

## Conditional precision, including viability

For a fixed other nominee and paired trial, let `X = other win - primary win`, so `X` is -1, 0 or 1. Let its true mean be `delta` and its discordance probability be `q`. The positive gate uses observed `delta_hat >= 0.05` and `WilsonLower(gains) - WilsonUpper(losses) > 0`, as well as the nominee's viability bound. This follows the existing interval construction; [NIST describes Wilson score inversion](https://www.itl.nist.gov/div898/handbook/prc/section2/prc241.htm).

With `z = Phi^-1(1 - 0.025/10) = 2.8070337701`, a sufficient contrast event at `N` trials is:

```text
delta_hat > t(N) = max(0.05, z * sqrt(N + z*z) / N)
```

Each Wilson half-width is at most `z / (2 * sqrt(N + z*z))`; the two interval centers differ by `N * delta_hat / (N + z*z)`. The displayed event therefore clears both contrast requirements for every feasible gain/loss pair. Wilson score inversion also makes viability equivalent to an observed win rate of at least:

```text
a(N) = 0.10 + z * sqrt(0.10 * 0.90 / N)
```

At `N=1,000`, `t=0.08911523` and `a=0.12662986`; viability requires at least **127 wins**. These sufficient events are arithmetic tools, not replacements for the actual frozen gate.

Conditional on independent identically distributed trials and the frozen nominees, [Hoeffding's bounded-variable inequalities](https://www.cs.rpi.edu/academics/courses/spring06/random/hoefding.pdf) give failure bounds `exp(-N*(delta-t)^2/2)` for the contrast and `exp(-2*N*(p-a)^2)` for viability, when the corresponding true mean exceeds its cutoff. A true gain `delta` implies the other nominee's true win rate `p >= delta`, since the primary's rate cannot be negative. A union bound covers both failures without assuming independence between them.

If **at least one fixed other nominee truly gains 16 points or more**, at 1,000 trials these two failure bounds are 0.08107949 and 0.10783722. Thus:

```text
P(SelectionMissDemonstrated | frozen nominees, stated alternative, IID model)
    >= 1 - 0.08107949 - 0.10783722
    >= 0.81108329
```

This **81.11% lower bound is uniform over feasible discordances** under that model. One qualifying nominee suffices for the diagnostic's OR decision, so there is no additional factor of three or assumption that the three contrasts are independent. The interval family already contains every reported quantity.

The bound first reaches 80% at 988 trials in the inspected 256–1,000 range. That is the minimum for this sufficient bound and alternative, not an optimal sample-size result. The concrete 1,000-trial specification leaves a small arithmetic margin. Sixteen points is a conservative power alternative, **not a minimum detectable effect, a changed five-point usefulness gate, or a prediction about the search**. At a true 15-point gain the same assumption-free marginal bound is only 50.79%; adding a true nominee win rate of at least 25% gives a separate 84.33% bound. That extra assumption is sensitivity analysis only.

Exact multinomial calculations show why a modest effect cannot be assigned one power figure without specifying discordance:

| Trials per nominee | True +10 points, q=50% | True +10 points, q=100% | True +16 points, q=50% | True +16 points, q=100% |
| --- | ---: | ---: | ---: | ---: |
| 256 | 11.65% | 11.13% | 57.89% | 40.03% |
| 512 | 40.64% | 30.05% | 95.93% | 80.17% |
| 1,000 | 85.30% | 63.70% | 99.99% | 98.83% |

These are probabilities of **one contrast passing**, before its viability gate; they are not unconditional diagnostic power or proof over all discordances. The JSON also reports a diagnostic lower bound at each grid point under the additional `p >= 25%` assumption. That lower bound subtracts viability failure without treating the two events as independent. No retained outcome was used to fit these model parameters, and no Monte Carlo simulation was run.

## Sampling condition still needs an executable contract

The target probability must refer to a defined distribution of unused conditions for fixed recipes, content and runtime. Reproducibility alone does not supply that distribution. The current [allocator](../LL/tools/BalanceHarness/TowerPracticalAllocation.cs#L56) derives deterministic values from a domain, master and ordinal through [SHA-256 to signed Int32](../LL/src/Core/Common/Randomness/StableRandom.cs#L13). This source path establishes deterministic derivation, not a statistical proof of uniform independent sampling.

For an **actually uniform sample without replacement** from the eligible Int32 population, comparison to IID uniform sampling loses at most the probability of a repeated value in the IID sample. With the present 484,285 exclusions and 41 new pre-confirmation values (one construction, eight discovery and 32 selection), the eligible population would be `M = 2^32 - 484285 - 41 = 4,294,482,970`. The coupling loss at 1,000 trials is at most `N*(N-1)/(2*M) = 0.000116313`. Subtracting it leaves a conditional lower bound of **81.0967%**. This is a mathematical illustration for uniform sampling, not a certification of the existing hash derivation. Recompute the population if history changes.

A future versioned contract must specify and justify the sampling/randomization mechanism, freeze it before outcomes, keep confirmation independent of earlier stages, and preserve all exclusions and attempted-work accounting. The power statements concern a complete prescribed panel; completion must not be assumed independent of outcomes or resource use. A stopped run remains incomplete. No sampler, random master or confirmation values have been created in this design step.

## Resource evidence and limits

The reader verifies all **53 package entries and 1,564 run entries** in the native verification's sealed manifests, including exact file inventories and hashes. Its measured 1,408-fight operational time was **166.547 seconds**: admission 19.984, run 99.578 and audit 46.937. These are aggregate phase measurements, not per-stage throughput estimates.

The exact final retained size across both roots, including their manifests, is **127,739,696 bytes**. The 1,408 compressed battle records account for 28,206,827 bytes; all other files account for 99,532,869 bytes. The largest observed battle record is 21,963 bytes. The completion receipt's earlier size sample predates final publication; it is not substituted for this final inventory count.

The proposed work is `64*8 + 4*32 + 4*1000 = 4,640` fights. For a transparent stress illustration only, scaling the entire measured time by the fight ratio yields 548.848 seconds. Holding all non-battle output fixed and assigning every proposed fight the largest previously observed battle record yields 201,441,189 bytes. Neither construction is an upper bound or a validated forecast: admission/audit scale differently, non-battle output grows, and future fights can be slower or produce larger reports.

| Illustrative multiplier on both assumptions | Seconds | Bytes | Relative to the old 600-second /256-MiB envelope |
| --- | ---: | ---: | --- |
| 1.00 | 548.848 | 201,441,189 | Both below the historical reference |
| 1.25 | 686.060 | 251,801,487 | Time exceeds the historical reference |
| 2.00 | 1,097.696 | 402,882,378 | Both exceed the historical reference |

Only **9.32%** slowdown over the first timing illustration reaches 600 seconds. This is insufficient evidence to claim resource feasibility. The launcher can terminate work at deadlines or sampled storage limits; that control does not show the intended panel will finish, and storage sampling is not an OS disk quota.

The old 600-second /268,435,456-byte allowance is **fully charged and closed**. It appears here solely as a historical reference. No remaining capacity, refund, transfer or fresh wall/storage allowance exists for this proposal. The arithmetic does not justify increasing an envelope until the workload fits.

## Decision and smallest remaining work

**Conditional precision and the [freeze/audit/sampling design](Tower-Practical-Selection-Diagnostic-Contract.md) are specified; implementation and resource admission are unresolved.** The remaining evidence must come from the implemented path and compatible phase measurements, including both audits and closeout. The current retained records cannot establish that requirement. No calibration fights, benchmark, larger allowance or implementation is authorized by this document.

Before any execution proposal, its scientific value must also stand on its narrow question: detecting a potentially large missed nominee in one future pool. Nothing here establishes that 16-point misses are plausible or frequent. If that question is not useful at this cost, or feasible resources cannot support it, stop this diagnostic proposal. Do not dilute the gate, reuse a closed panel, or rename it as method validation. The separate [three-restart quality experiment](Tower-Practical-Evaluation-Design.md) remains NoGo.

## Verification and preservation

From the checkout, with Python 3, stdout reproduces the JSON:

```text
python -B "Balance Harness/analysis/practical-selection-diagnostic-design.py"
```

Checks cover syntax, critical values, all **501,501 feasible gain/loss pairs** at 1,000 trials, all **1,001 viability counts**, independent small multinomial summation, 48 sensitivity cases, exact JSON reproduction, sealed file hashes/inventories, changed-document links/whitespace, and preservation of unrelated dirty files. These verify the calculation and retained bytes, not the sampling model or native gameplay compatibility.

Changed files are this design, its Python reader and JSON result, plus current-status links in the hypothesis review, assessment, evaluation design, readiness review, practical guide and README. No backend build/test, benchmark, native reconstruction, combat, seed derivation or allocation command ran. Backend tests were not needed for this analysis-only change; the earlier 316-case engineering result keeps its original scope. No required read-only check was blocked. No migration, application configuration change or deployment is involved.

Both recommended anchors, **484,285 exclusions**, V19's **512 unused values /253 required recipes /Unresolved**, and adoption **Hold** remain unchanged. Prior receipts and calculations retain their original bytes. No experiment is queued.
