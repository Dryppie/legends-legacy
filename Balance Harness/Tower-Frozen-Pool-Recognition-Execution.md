# Frozen-pool recognition: execution and search diagnosis

**Subsequent implementation:** The [versioned proposal policies and candidate export](Tower-Proposal-Policy-Implementation.md) are now implemented. Their captured preview reproduces the legacy first wave and identifies identical benchmark-only arms when no declared parent interactions activate preservation. This follow-up adds no combat and does not revise the diagnostic below.

23 September 2026. The admitted offline Balance Harness diagnostic completed **27,648 fights**, both audits and publication. Its decision is **`CompleteDiagnosticOnly`**. The adaptive pilot remains **`AbandonThisConfiguration`**; no team, search policy or gameplay default was promoted.

**Proposal quality is the next development priority for these frozen pools.** Of 72 measured challenger/root instances, 68 scored below the fixed strongest reference, two tied it and two scored slightly above it. Neither positive contrast had an interval excluding zero. Nominees performed better on average than the discarded groups, but none exceeded the reference's observed wins. This strengthens the case for changing proposal generation while retaining independent evaluation. It does not prove that recognition is adequate or that every unmeasured candidate is weak.

## What was measured

The [immutable plan](Tower-Frozen-Pool-Recognition-Plan.json) fixed twelve historical adaptive-search roots. Each contributed three exact references, two challenger nominees, two final-beam near-misses and two candidates sampled without replacement from the other thirteen generated recipes. All nine teams in a root used the same 256 fresh combat values; panels were disjoint between roots. Repeated recipes retain separate root identities. All roots concern the captured floor-5 scenario, not twelve different encounters.

The study therefore measured 108 team/root cells, including 72 challengers, with 216 signed candidate/reference contrasts. The benchmark is the previously fixed `96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c`, not whichever reference scored highest in the new outcomes. Draws count as non-wins. The approximate Wilson family is 540: 108 rates and two binary components for each contrast. Contrast bounds use paired gains and losses on matching seeds.

The new results are independent of the historical training and held-out panels. Historical nomination and selection labels are joined to the new measurements; old combat observations are not pooled into them. The [native implementation and admission report](Tower-Frozen-Pool-Recognition-Native-Implementation.md) records the complete execution contract and the preserved admission failure.

## Results

All gains below are percentage points relative to the fixed benchmark. Interval counts refer to individual family-540 combat contrasts, not intervals for the group means.

| Group | Measured | Observed above / equal / below | Interval above / below zero | Mean gain |
| --- | ---: | ---: | ---: | ---: |
| Challenger nominees | 24 | 0 / 2 / 22 | 0 / 2 | −8.382 pp |
| Final-beam near-misses | 24 | 2 / 0 / 22 | 0 / 8 | −17.383 pp |
| Sampled lower stratum | 24 | 0 / 0 / 24 | 0 / 12 | −24.202 pp |
| Historically selected challengers, a subset of nominees | 6 | 0 / 1 / 5 | 0 / 0 | −6.250 pp |

Among the 48 measured discarded candidates, 46 scored below the benchmark. Across all 72 measured challengers, 22 contrasts had upper bounds below zero and none had lower bounds above zero. The raw average over the 72 measured challengers is not the all-candidate population estimate: the sampling fractions differ between strata.

For each root, the lower-stratum mean is the mean of its two sampled gains. Each lower-stratum observation has inclusion weight **13/2**. The all-17 candidate mean estimate is `(sum of four nominee/near-miss gains + 6.5 × sum of two sampled lower gains) / 17`.

| Root | R* wins /256 | Nominee mean | Near-miss mean | Lower mean estimate | All-17 mean estimate | Historically selected challenger gain |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 202 | −7.617 | −16.992 | −46.094 | −38.143 | −8.594 |
| 2 | 198 | −5.859 | −17.773 | −40.430 | −33.697 | −2.344 |
| 3 | 195 | −11.328 | −17.773 | −38.086 | −32.548 | −11.328 |
| 4 | 207 | −14.258 | −24.805 | −19.922 | −19.830 | Reference selected |
| 5 | 194 | −14.258 | −19.922 | −12.305 | −13.431 | Reference selected |
| 6 | 201 | −11.133 | −36.133 | −25.586 | −25.126 | Reference selected |
| 7 | 198 | −8.398 | −12.109 | −15.820 | −14.511 | Reference selected |
| 8 | 187 | −5.859 | −25.977 | −14.453 | −14.798 | Reference selected |
| 9 | 201 | −10.352 | −11.328 | −19.531 | −17.486 | −12.109 |
| 10 | 186 | −4.102 | −20.508 | −40.430 | −33.812 | Reference selected |
| 11 | 188 | −4.492 | −2.930 | −8.984 | −7.744 | −3.125 |
| 12 | 187 | −2.930 | −2.344 | −8.789 | −7.341 | 0.000 |

All twelve population mean estimates are negative. Their equal-root average is **−21.539 pp**. This is a descriptive finite-population point estimate, without a population confidence interval. It is not an estimate of the quality of every individual unsampled recipe or of a future search policy's outputs.

Only two measured candidates had positive observed gains:

| Root | Exact candidate prefix | Historical stratum / operator | Candidate / R* wins | Paired gains / losses | Observed gain | Combat interval |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| 11 | `58c7a62d7a84…` | Near-miss / guided-pair | 189 / 188 | 56 / 55 | +0.391 pp | [−19.450, +20.187] pp |
| 12 | `359c97a8f902…` | Near-miss / coordinated | 192 / 187 | 47 / 42 | +1.953 pp | [−16.508, +20.194] pp |

These are uncertain small observed advantages, not demonstrated missed strong teams. Their operator labels do not establish that those operators are better: operator opportunities, parent quality and selection into measured strata differ. The [machine-readable review](../TestResults/recognition-review-20260923/review.json) retains full identities, paired contrasts and historical training metadata for every measured challenger.

## What changes in the diagnosis

The measured nominee group is less weak on average than the near-miss and sampled lower groups. That is consistent with the existing ranking enriching the pool for better candidates. It is not a controlled comparison of ranking rules, and it cannot identify a counterfactual adaptive trajectory: different survivors would change later parents and proposals.

The six historically selected challengers again provide no observed improvement over R*: five were below it and one tied it on these new panels. Their descriptive average deficit is 6.250 points. This reinforces the existing concern about accepting small noisy selection leads, but no individual selected-challenger contrast excludes zero under this diagnostic's wide interval family. The earlier pilot's outcome and its separate uncertainty remain unchanged.

There is now little observed support for a large reservoir of superior teams among the measured discarded candidates. The next effort should target generating more competitive proposals. Simply increasing confirmation of the same nominee population would not answer how to improve the proposals.

The remaining limitations matter:

- **132 candidates remain unmeasured**, with explicit null outcomes. Each member of a thirteen-candidate lower stratum had only a 2/13 inclusion probability. A rare strong discarded recipe can be missed.
- The individual combat intervals are broad at 256 trials and family 540. Absence of a positive lower bound does not exclude modest improvements.
- Lower-stratum sampling uncertainty is additional to combat uncertainty. No individual combat interval is reused as a population confidence interval.
- All evidence diagnoses twelve already generated pools in one captured scenario. It does not establish reliability on future search roots or other encounters.
- The review's across-root averages and historical-selection groups are descriptive post-hoc summaries. They introduce no qualification endpoint, fitted selector or adoption decision.

## Single next implementation step

Add a **separately versioned proposal-policy contract and deterministic candidate-batch export** around `TowerAdaptiveRacingGenerator`. Its current `First`/`Second` schedules and parent ticket mixture are fixed in code. A new experimental contract should make a small declared operator/parent ablation possible while preserving the exact v1 generation streams, recipe identities and archive replay.

Use the existing racing kernel and selector unchanged for that comparison so proposal changes can be assessed independently. Keep all three controls and the fixed strongest-reference benchmark. Before any new combat, export exact accepted recipes, parent identities, effective operators, changed owners, replacement distance, legal-construction attempts and rejection reasons. Exercise the new contract with deterministic, combat-free fixtures.

The first proposed alternatives should test benchmark-centered small edits and edits that preserve known enabler/consumer structures. These are hypotheses, not settings validated by this diagnostic. The previous negative anchored-neighborhood result remains relevant; merely relabeling that policy would not constitute a new justification. Avoid choosing an operator mixture from the two tiny positive observations above.

After implementation, freeze a small matched-budget comparison on fresh roots with common fresh evaluation panels, declared completion limits and independent evaluation. Report both candidate quality and final selected-output performance relative to R*. Do not tune nomination, selection and proposal generation together. No new fight budget, run, qualification or default change is authorized by this report. The pending 52,000-fight exact-team confirmation remains a separate question and is not the next search-design priority.

## Execution and resource accounting

The user's follow-up to proceed initiated exactly one launch through the retained admitted launcher and runtime. There were **zero retries**, replacement roots, resumed panels or entropy refills. The admitted request hash remains `766de5619a0a419ee7270e33fe68d88b94f902b51bcefc3e484179ad0e2e02db`; admission manifest hash remains `1c57b77e14fff65453a33bcc170b0eb7c010cda86c91b53855f7268dcbf19556`.

| Measured phase | Seconds | Retained bytes / notes |
| --- | ---: | --- |
| Native owned execution and cleanup | 1,360.203 | 668,960,345 bytes at the enclosing native/audit boundary |
| Native reconstruction audit | 107.046 | Exit 0, no timeout, zero active processes |
| Independent Python audit | 47.907 | Exit 0, no timeout, zero active processes |
| All audit/publication work through terminal receipt | 211.094 | 6,486,263 additional bytes |
| Scientific launch through terminal receipt | **1,571.297** | **675,446,608 bytes** |
| Including both fully charged admissions | **2,771.297** | **1,749,188,432 charged bytes** |

The scientific launch stayed within its 7,200-second /3.5-GiB allowance and separate native/audit caps. Both admission attempts remain charged at 600 seconds /512 MiB each, including the preserved first failure. The native receipt's earlier storage measurement is 668,803,171 bytes; the enclosing phase boundary includes the subsequent native console/receipt/process bytes. These are operational measurements, not combat-only throughput estimates or guaranteed future bounds.

One 24,576-byte draw exposed 6,144 distinct fresh values. All were permanently reserved: 3,072 used combat values and 3,072 unused tail values. The complete historical union before this run was 666,076. Successful publication does not release unused reservations.

The [terminal receipt](../TestResults/balance/tower-frozen-pool-recognition-repaired-20260923/closeout.json) binds the scientific manifest SHA-256 **`5fd7e9eb38eb4319f41be7a0e874297a897c7a32a73d09030261579b2473b377`**. The scientific archive, admission packages and all prior failure evidence remain unchanged.

The additional [read-only publication verification](../TestResults/recognition-publication-verification-20260923/verification.json) also passed. It authenticated the admitted runtime, invoked the native published-archive verifier, checked the final result and terminal accounting, and independently scanned the complete live history. The union is **672,220 reserved values across 246 ledger files**. Its native process exited 0 with no timeout or active descendants. The separately declared engineering allowance was 600 seconds /64 MiB; verification and sealing took **157.485 seconds**, retaining **214,663 bytes**, with zero fights and zero new values. Its manifest SHA-256 is **`ef4d5d7595d1aaf270cf5d330898b3192c6fee10ed93f573722a87083f4a39a1`**. This read-only work is accounted separately from the scientific launch, and does not extend that launch's budget.

## Review implementation and verification

The new [read-only reviewer](analysis/frozen-pool-recognition-review.py) authenticates every consumed result/audit/stage-review file against external manifest pins, reconstructs group arithmetic and sampling weights, verifies root-specific membership and explicit nulls, joins historical labels and rechecks consumed bytes. It emits JSON and Markdown plus its producing source snapshot. It consumes already audited results; it does not repeat the compressed-battle audit itself.

The [review package](../TestResults/recognition-review-20260923/files.json) has manifest SHA-256 `cf31156c430a0218d8d4056cc964cb12d3aaa6e2120a4076f1165a78eb6b3886`. Its source stage-review manifest is `f4087410b3647ef1294c732f0570459daeb901913617502216345828a2508835`, and the frozen plan remains `f8e8b206cd6b0cf46f420ab4d6d4d4a9568e5e46186ae8a8ae18a552fa0957b8`. The review added zero fights and zero values.

**12 reviewer tests passed**, covering incomplete/promoted results, root collisions, missing or changed contrasts, changed sampling weights, nonfinite estimates, invented unmeasured outcomes, incorrect historical joins, observed-vs-interval sign distinctions and evidence mutation. The complete reviewer CLI also passed against the retained literal non-combat fixture before analyzing the real result. That earlier CLI check preceded the addition of the Markdown renderer; the final real invocation produced both formats successfully.

The [test log](../TestResults/recognition-execution-20260923/review-tests-publication.log), [report arithmetic/link check](../TestResults/recognition-execution-20260923/report-check.log) and [source/whitespace check](../TestResults/recognition-execution-20260923/source-check.log) are retained. The report check validated all twelve root rows, the three stratum rows, all local links, resource totals and all three external publication/review pins.

Commands used (`python` denotes the bundled interpreter):

```powershell
python -B -X utf8 'TestResults/recognition-admission-repaired-20260923/run-frozen-pool-recognition.py' --request 'TestResults/recognition-admission-repaired-20260923/request.json' --harness 'TestResults/recognition-admission-repaired-20260923/runtime/BalanceHarness.dll' --admission-pin 1c57b77e14fff65453a33bcc170b0eb7c010cda86c91b53855f7268dcbf19556
python -B -X utf8 'Balance Harness/analysis/test-frozen-pool-recognition-review.py'
python -B -X utf8 'TestResults/recognition-execution-20260923/verify-publication.py' --scientific-pin 5fd7e9eb38eb4319f41be7a0e874297a897c7a32a73d09030261579b2473b377
python -B -X utf8 'Balance Harness/analysis/frozen-pool-recognition-review.py' --run 'TestResults/balance/tower-frozen-pool-recognition-repaired-20260923' --manifest-sha256 5fd7e9eb38eb4319f41be7a0e874297a897c7a32a73d09030261579b2473b377 --plan 'Balance Harness/Tower-Frozen-Pool-Recognition-Plan.json' --stage-review 'TestResults/adaptive-racing-stage-review-20260923' --output 'TestResults/recognition-review-20260923'
```

The scientific launch and artifact-producing commands above are historical single executions, not instructions to rerun this closed study. Changed repository files are the reviewer and its tests, this report, and status links in the harness guides and preceding assessment/implementation reports. No C# code or captured runtime changed, so backend tests were not rerun. No required verification command remains blocked. There are no migrations, application configuration changes or deployment implications. Unrelated working-tree changes were preserved.
