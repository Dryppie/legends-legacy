# Practical Tower evaluation: prospective design and launch decision

16 September 2026. Target: the offline BalanceHarness. This completes the statistical design follow-up to the [readiness review](Tower-Practical-Workflow-Readiness.md). It is a planning specification, not a runnable study request or an allocation.

**Decision: no-go for a new quality experiment.** The proposed 12,032-fight ceiling can support a conservative 80% conditional confirmation-power target at a true **+18 percentage-point** gain. That requires at least two of the three selected candidate outputs to truly beat both anchors and their same-restart comparator by that amount. Existing evidence does not establish that a proposed change can produce such outputs. A five-point observed usefulness threshold remains unchanged; the larger power alternative is an assumption to assess, not a new acceptance threshold or a forecast.

The calculation resolves a statistical design question, not the missing search hypothesis. Do not launch the experiment merely because a sufficiently large assumed effect makes its confirmation power adequate. No new optimizer or candidate variant is justified by this calculation.

The later [hypothesis qualification](Tower-Practical-Hypothesis-Qualification.md) motivated a separate [selection diagnostic](Tower-Practical-Selection-Diagnostic-Execution-Review.md), which has now completed 4,640 fights and both audits with `NoSelectionMissDemonstrated`. Its frozen primary won 712/1,000; the [adoption review](Tower-Practical-Primary-Adoption-Readiness.md) preserves Hold because adoption was outside that diagnostic. The [fixed-team plan](Tower-Practical-Fixed-Team-Confirmation-Plan.md) specifies a prospective three-team confirmation whose [controller is now implemented and fixture-tested](Tower-Practical-Fixed-Team-Confirmation-Implementation-Review.md). The subsequent [completed fixed-team confirmation](Tower-Practical-Fixed-Team-Confirmation-Execution-Review.md) completed 16,500 trials and both audits, returning AdoptFixedTeam for exact candidate `399bc776…`. The earlier diagnostic retains its original endpoint. This does not amend or authorize the 12,032-fight three-restart experiment below. Its calculations, JSON and original no-go decision remain unchanged.

## Proposed comparison and frozen decision

Compare one materially distinct, justified complete search policy with the unchanged fixed-order incumbent-preserving practical comparator. Both receive the same two admitted anchors, content, legal Essence pool, equipment and progression assumptions: ten level-40 characters, five Essences each, two five-character subgroups, one equipment context, and outcome-independent canonical ability order. This tests practical improvement using supplied knowledge, not discovery from scratch. Account inventory is not a prerequisite; preserve the declared `OwnedCopies=null` acquisition assumption and all gameplay legality checks.

| Design component | Proposed rule |
| --- | --- |
| Restarts | Three predeclared construction restarts per method. Pair methods within each restart with equal starting information and the same stage conditions. Preserve each restart's origin even when outputs duplicate. |
| Discovery | 64 evaluated parties on eight discovery trials: a 512-fight ceiling per method/restart; at most 256 proposals, including invalid and duplicate proposals. Charge anchor evaluations too. Exhausting proposals before the required candidate count is incomplete, not permission to refill. |
| Selection | Four nominees, including both incumbents, on 32 separate selection trials: 128 fights per method/restart. Use the existing fixed nomination, selection and tie rules; freeze exactly one output. |
| Stage separation | Discovery, selection and confirmation are disjoint and exclude the full historical union. A future protocol must specify and justify the sampling model before allocation; no values or schedules are created here. |
| Final family | Six selected output slots plus two anchors, all frozen before any confirmation outcome is available. Merge exact prepared-equivalent recipes for execution, retain all origins, and keep the planned interval family fixed at 26. No replacement nominee or extra trial is bought with duplicate savings. |
| Confirmation | 1,024 paired trials per distinct recipe on one shared panel. All recipes use the same conditions at each trial. Outcomes are win/non-win; draws count as non-wins. |
| Required contrasts | For each restart `r`, candidate `C_r` versus anchor `A`, anchor `B`, and unchanged comparator output `P_r`: nine directional contrasts in total. No best-of-restarts comparison or post-confirmation reselection. |
| Approximate interval family | Eight recipe win-rate quantities plus gain/loss rates for each of nine contrasts: `8 + 2*9 = 26`. Two-sided Wilson intervals use `z = Phi^-1(1 - 0.05/(2*26))`. |
| One restart passes | A mechanically legal candidate distinct from both anchors; its adjusted win-rate lower bound is at least 10%; each of its three observed gains is at least five percentage points and each adjusted paired lower bound is strictly positive. An output equal to its same-restart comparator cannot pass. |
| Study passes | At least two of three restarts pass, with complete, valid, authenticated evidence for the entire planned study. Report all restarts, duplicate identities, costs and failed contrasts. |

For a contrast with `g` candidate-only wins and `l` reference-only wins among `n` trials, the gain estimate is `(g-l)/n`, and its lower bound is `WilsonLower(g,n,26) - WilsonUpper(l,n,26)`. This supports a positive true gain with a five-point observed threshold; it does **not** certify that the true gain exceeds five points. Wilson coverage remains approximate despite the family adjustment; the calculation below does not convert it to an exact 95% simultaneous guarantee. The [NIST description](https://www.itl.nist.gov/div898/handbook/prc/section2/prc241.htm) explains the score-interval construction.

The [implemented practical gate](../LL/tools/BalanceHarness/TowerPracticalSearch.cs#L26) still uses seven quantities and two anchor contrasts for one output. This proposed 26-quantity, comparator-inclusive decision is not implemented. Repeating the current command and choosing successful outputs would not implement this design. Any future controller needs a separate versioned contract; no current gate or historical decision changes here.

## Cost and stop rule

| Component | Maximum fights |
| --- | ---: |
| Discovery: `3 * 2 * 512` | 3,072 |
| Selection: `3 * 2 * 128` | 768 |
| Confirmation: `8 * 1,024` | 8,192 |
| Total | **12,032** |

These are prospective ceilings, not balances available to spend. Freeze the cumulative wall-time and storage limits from compatible retained costs before launch, including setup, complete history scanning, admission, export and verification. Those limits have not been established here; earlier filesystem parity does not establish whole-run throughput. Count all attempted combat, including failed attempts, against the ceiling. There is no retry, replay or extension allowance and no transfer from historical budgets.

Stop at the first invalidity or resource boundary, or at the fixed end. Incomplete evidence cannot produce study success, even if two available outputs look favorable. Preserve attempts and reservations; never delete or relabel Pending to continue. A complete study that misses its gate yields no adoption and closes the proposal. Failure is not equivalence, and does not authorize changed thresholds, another restart, another candidate, or additional confirmation. Above-50% performance remains a separate encounter-balance concern, not a reason to reject team strength.

## Conditional confirmation power

The [calculation script](analysis/practical-evaluation-design.py) and [machine-readable results](Tower-Practical-Evaluation-Design.json) use the same Wilson arithmetic as the [harness](../LL/tools/BalanceHarness/TowerBalanceEvaluator.cs#L172). They use no battle results, historical archives, random draws or evaluation-seed values. The earlier [single-contrast sensitivity calculation](Tower-Practical-Confirmation-Precision.json) remains unchanged.

Condition on frozen, eligible outputs. Assume a complete prescribed confirmation panel without outcome-dependent truncation, with independent trials under the target distribution, and that at least `k` of the three candidates truly improve by at least `delta` against **each** of their three required references. Dependence among recipes, comparisons and restarts sharing a trial is unrestricted. The candidate's true win probability is then at least `delta`, because reference win probabilities cannot be negative.

For `n=1,024` and `F=26`, `z=3.101861832`. The maximum Wilson half-width is `z/(2*sqrt(n+z^2))`. Subtracting two Wilson centers shows that an observed gain strictly above

```text
t = max(0.05, z*sqrt(n+z^2)/n) = 0.097387512
```

is sufficient to pass a contrast for every possible discordance. This is a sufficient event, not an added gate: observed gains below it can still pass the actual gate. Score-interval inversion gives a viability threshold of

```text
a = 0.10 + z*sqrt(0.10*0.90/n) = 0.129079955
```

or at least **133 wins out of 1,024**. The script checks this equivalence at every possible win count.

The paired difference takes values in `[-1,1]`, while a win indicator is in `[0,1]`. Applying the bounded-variable tail inequality from [Hoeffding, Theorem 2](https://www.cs.rpi.edu/academics/courses/spring06/random/hoefding.pdf) gives conservative failure bounds:

```text
epsilon = exp(-n*(delta-t)^2/2)  if delta > t; otherwise 1
eta     = exp(-2*n*(delta-a)^2)  if delta > a; otherwise 1
r       = min(1, 3*epsilon + eta)
joint confirmation pass probability >= max(0, 1 - k*r/(k-1)), k = 2 or 3
```

Here `r` bounds failure of one strong restart by a union bound over three contrasts and viability. Failing the two-of-three study rule requires at least `k-1` of the `k` strong restarts to fail. Their expected failure count is at most `k*r`; the displayed count bound needs no independence among restarts. It covers every feasible discordance without forecasting it from the pilot.

| Assumed true gain against every required reference | Lower bound if at least two outputs are strong | Lower bound if all three are strong |
| --- | ---: | ---: |
| +5 pp | 0% (uninformative bound) | 0% (uninformative bound) |
| +10 pp | 0% (uninformative bound) | 0% (uninformative bound) |
| +15 pp | 0% (uninformative bound) | 0% (uninformative bound) |
| **+18 pp** | **80.8%** | **85.6%** |
| +20 pp | 97.3% | 97.9% |

A zero lower bound does not mean actual power is zero or that smaller effects are impossible to detect. Eighteen points is a sufficient planning alternative from this conservative calculation, **not** a minimum detectable effect estimate. The five-point observed gate remains the same. The earlier precision review separately explains why a true five-point effect at the observed threshold cannot simply be assigned 80% power by increasing the sample.

This is not an 80% chance that the search succeeds. Let `H` mean that at least two searches return eligible outputs with all true gains at least 18 points. For the hypothetical complete panel, `P(statistical pass | H) >= 0.8079` under the stated sampling model. No estimate of `P(H)` or operational completion probability is available. Conditioning on completion after an outcome-dependent timeout could alter the trial distribution; this bound must not be applied to that selected subset of runs. Even a qualifying result from three construction restarts is finite evidence, not a general method-reliability guarantee. Confirmation must be independent of all adaptive training and selection; a successful output cannot be selected retrospectively from confirmation.

## Go/no-go conditions and next work

| Condition | Current status |
| --- | --- |
| A materially distinct hypothesis with evidence explaining why it could produce the required practical improvement | **Not qualified after the saved-evidence follow-up.** [The review](Tower-Practical-Hypothesis-Qualification.md) identifies construction-yield and selection uncertainties without a supported intervention. Closed coverage, block, allocation and mechanism proposals do not become new hypotheses by renaming them. |
| Useful observed threshold and comparison family | Specified here as a proposal: +5 pp, two anchors plus matched comparator, 26 quantities, two of three restarts. |
| Adequate conditional confirmation power within the fight ceiling | Demonstrated arithmetically at +18 pp under the stated assumptions. No evidence that the search can reach that alternative; no unconditional power claim. |
| Prospectively justified confirmation sampling model and frozen implementation | Not established. The existing single-output practical command is insufficient for this study. |
| Cumulative wall/storage feasibility | Not established for this 12,032-fight design. The separate [native verification](Tower-Practical-Native-Verification-Execution-Review.md) completed one 1,408-fight workflow and both audits in 166.547 seconds, within 600 seconds /256 MiB. It is one operational observation, not a throughput multiplier or evidence that this larger design fits its allowance. |
| Practical Pending recovery and successful gameplay workflow evidence | Recovery is implemented for authenticated declared-input pre-combat registration and [closed recorded allocation prefixes](Tower-Practical-Allocation-Recovery-Review.md). Real process fixtures cover failure/recovery controls; the separate native run verifies the successful gameplay path. Older unauditable records and unresolved derivations remain blocked. See [process verification](Tower-Practical-Workflow-Readiness.md#process-lifecycle-verification) and the [recovery boundary](Tower-Practical-Workflow-Readiness.md#operational-boundary-that-must-remain-explicit). |
| Allocation and launch of this quality comparison | **Not authorized or performed.** The separately authorized operational verification is closed; no further experiment is queued. |

Keep the scientific proposal at **no-go**. Do not invent a high-effect hypothesis to fill the template. If retained evidence later justifies a distinct change, assess whether its plausible effect fits this envelope before implementing or allocating an experiment. A sharper justified power calculation may replace this conservative bound prospectively; lowering a success criterion after results is not an option. Declared-input recovery, process fixtures and the native success observation improve operational confidence within their documented boundaries. They do not establish better search quality or feasibility for this larger study. The native run itself returned ImprovementNotDemonstrated.

All previous **483,988 exclusions** are preserved; the separate native run adds 297, for **484,285** permanent exclusions. V19's **512 unused values**, **253 required recipes**, **Unresolved** reliability and **Hold** adoption remain unchanged. Sealed historical experiments and receipts are untouched.

## Verification and files

This section records the original planning-only verification. The later recovery, process-lifecycle and owned-allocation increments and their 257-check backend result are recorded in the [readiness review](Tower-Practical-Workflow-Readiness.md#verification-and-changed-files); they do not change this design's calculation or no-go decision.

This increment adds this design, its deterministic Python calculation and JSON results; it links the design from the readiness review, practical guide and README. No backend or gameplay code changes.

The arithmetic checks all **525,825** feasible gain/loss count pairs at `n=1,024`, including **214,369** pairs satisfying the sufficient event; all **1,025** viability counts; and all **12** outcome patterns for two or three strong restarts. Numerical multinomial sensitivity checks at +18 and +20 points agree with the conservative contrast bounds. The earlier arithmetic self-checks also pass. These are mathematical/model checks, not combat or workflow evidence. The output is reproducible with:

```text
python -B "Balance Harness/analysis/practical-evaluation-design.py"
```

The bundled Python interpreter was used because `python` is absent from PATH. JSON reproduction, changed-document local links and whitespace checks passed. No verification command remained blocked. Backend tests were not repeated for this planning-only change; the prior recorded result remains 164 focused checks passed, with its original verification limits. No migrations, configuration changes, deployment, native reconstruction, combat or seed allocation occurred.
