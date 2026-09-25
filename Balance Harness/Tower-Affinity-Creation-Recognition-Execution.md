# Affinity-creation recognition: completed diagnostic

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The diagnostic completed all 27,648 fights and passed both audits. Of 72 measured challengers, 62 were below the fixed benchmark and ten were above it; none had an adjusted combat interval entirely above zero.** Nominees and near misses each averaged **−6.706 percentage points**, while the probability-sampled lower group averaged **−11.458 points**.

The measured pool is weak on average. This does **not** show that recognition is solved: nomination did not separate nominees from near misses on their fresh average scores, and eight of the ten positive observations were discarded candidates. Their uncertainty is substantial. The right next step is a bounded diagnosis of the exact additions, removals and owners behind these results, before choosing another proposal change or tuning a gate.

The decision is **`CompleteDiagnosticOnly`**. The [v5 validation pilot](Tower-Benchmark-Validation-Pilot-01-Execution.md) remains **Inconclusive**. No team qualified, no policy was promoted, and no gameplay or policy default changed.

## What was measured

The [frozen plan](Tower-Affinity-Creation-Recognition-Plan.json) retained all twelve source roots. Each root measured three references, two nominees, two other final-beam candidates (near misses), and two prospectively sampled members of its thirteen-candidate lower stratum. Its nine teams shared 256 fresh values; the twelve panels were disjoint. Draws count as non-wins.

This produces **108 team/root rates and 216 paired candidate/reference contrasts**. All **132 unsampled outcomes remain null**. The catalogue contains 204 generated occurrences and 57 distinct generated recipes; the 72 measured occurrences contain **45 distinct recipes**. Repeated recipes remain separate root instances. The reviewer consumes the new audited results and frozen membership, with no old combat outcomes or fitted selector.

The benchmark is the fixed previously confirmed `96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c`, rather than a retrospectively chosen panel winner. The primary and other references retain their original positions and are included in the complete paired reporting.

## Candidate quality and recognition

Differences are percentage points against the fixed benchmark. Interval counts use the specified approximate Wilson family of 540; these are individual combat intervals, not population-mean intervals.

| Group | Measured | Observed above / equal / below | Interval above / below zero | Mean gain |
| --- | ---: | ---: | ---: | ---: |
| nominee | 24 | 2 / 0 / 22 | 0 / 0 | -6.706 pp |
| near-miss | 24 | 5 / 0 / 19 | 0 / 1 | -6.706 pp |
| lower | 24 | 3 / 0 / 21 | 0 / 5 | -11.458 pp |
| All measured | 72 | 10 / 0 / 62 | 0 / 6 | -8.290 pp |
| Discarded | 48 | 8 / 0 / 40 | 0 / 6 | -9.082 pp |

The all-measured and discarded means are descriptive averages of the measured cells. They are not estimates obtained by treating all seventeen candidates as equally likely to be sampled. For the finite seventeen-candidate mean, each sampled lower member has weight **13/2**, while each nominee and near miss has weight one.

| Root | Benchmark wins /256 | Nominee mean | Near-miss mean | Lower mean estimate | All-17 mean estimate |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 192 | -0.977 | +2.148 | -7.812 | -5.836 |
| 2 | 192 | +0.586 | -9.766 | -5.078 | -4.963 |
| 3 | 202 | -8.203 | -5.078 | -9.180 | -8.582 |
| 4 | 182 | -2.930 | +2.539 | +1.562 | +1.149 |
| 5 | 215 | -15.625 | -24.805 | -15.820 | -16.854 |
| 6 | 184 | -1.172 | -6.641 | -0.781 | -1.517 |
| 7 | 206 | -14.258 | -12.305 | -22.852 | -20.600 |
| 8 | 198 | -3.711 | -10.742 | -6.250 | -6.480 |
| 9 | 199 | -6.445 | -5.078 | -9.961 | -8.973 |
| 10 | 189 | -4.102 | -6.445 | -18.359 | -15.280 |
| 11 | 210 | -12.305 | -5.078 | -28.125 | -23.552 |
| 12 | 194 | -11.328 | +0.781 | -14.844 | -12.592 |

Eleven root population estimates are negative; root 4 is positive. Their equal-root average is **-10.340 pp**. This is a descriptive point estimate with **no population confidence interval**. Only two of thirteen lower candidates were measured per root, so sampling uncertainty remains in addition to combat uncertainty. It does not estimate future search-output quality.

All ten positive observations are retained below, including small ones. Full identities and all negative observations remain in the [machine-readable review](../TestResults/affinity-creation-recognition-review-20260924/review.json).

| Root | Candidate prefix | Stratum | Candidate / benchmark wins | Gains / losses | Gain | Adjusted combat interval |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| 1 | `8c033fca1402…` | near-miss | 194 / 192 | 42 / 40 | +0.781 pp | [-17.087, +18.561] pp |
| 1 | `a7ade6c9c4b9…` | near-miss | 201 / 192 | 41 / 32 | +3.516 pp | [-13.724, +20.359] pp |
| 2 | `fe8316096f16…` | nominee | 196 / 192 | 45 / 41 | +1.562 pp | [-16.655, +19.604] pp |
| 4 | `f4daafdda1f7…` | near-miss | 200 / 182 | 56 / 38 | +7.031 pp | [-11.971, +25.242] pp |
| 4 | `1ffcb4a47284…` | lower | 187 / 182 | 51 / 46 | +1.953 pp | [-17.076, +20.762] pp |
| 4 | `482ae6d57e51…` | lower | 185 / 182 | 52 / 49 | +1.172 pp | [-18.086, +20.298] pp |
| 6 | `b3d2cd5a543f…` | nominee | 185 / 184 | 43 / 42 | +0.391 pp | [-17.690, +18.428] pp |
| 6 | `1ffcb4a47284…` | lower | 206 / 184 | 55 / 33 | +8.594 pp | [-9.993, +26.213] pp |
| 11 | `869592655f8d…` | near-miss | 215 / 210 | 39 / 34 | +1.953 pp | [-15.224, +18.910] pp |
| 12 | `8c033fca1402…` | near-miss | 199 / 194 | 40 / 35 | +1.953 pp | [-15.396, +19.083] pp |

The largest observed gains are a lower-stratum candidate at root 6 (+8.594 points) and a near miss at root 4 (+7.031 points). Both adjusted intervals cross zero substantially. They are exploratory leads, not demonstrated missed winners, qualification results or evidence for promoting their operators. Their ordering must not be reused as a fresh evaluation of a selector.

Three conclusions follow. First, most measured proposals lose to a benchmark already available without search. More final confirmation alone will not improve that proposal distribution. Second, the lower group is weaker on its observed average, but nominee and near-miss means are identical; this diagnostic supplies no observed average enrichment at the final nomination boundary. That is not a controlled estimate of an alternative ranking rule. Third, 132 unmeasured cells and broad combat intervals leave room for rare or modest improvements. Absence of positive interval bounds is not proof that none exists.

This is evidence conditional on twelve frozen pools in one captured encounter. It does not establish that affinity construction, adaptive racing, other operator settings or future roots are ineffective. Comparisons with earlier diagnostic cohorts would involve different generated pools and panels; their outcomes are not pooled here.

## Single next implementation step

Add a **bounded, read-only proposal-edit diagnosis** that joins these exact measured root/party identities to the retained v5 generation records. Extend the existing reviewer/export path, preserving the sealed recognition review as its own version. Retain every measured occurrence and every unknown outcome. Export parent identity, owner and subgroup, the exact removed/added Essences, one- versus two-slot distance, target/newly activated affinities, and training stratum alongside the new benchmark contrast.

This targets a concrete limitation in the current generator. `TowerAffinityCreation.Opportunities` enumerates legal minimal replacements for an authored pair, and `Create` chooses equally among eligible pairs and then legal removals. Structural activation establishes compatibility; it does not measure whether the benefit of the added pair compensates for the removed Essence or fits the owner's equipment and role. The current diagnostic establishes the net result, but its reviewer deliberately does not attribute that result to an individual removal, affinity or owner.

Use the joined data to formulate **one mechanically justified, separately versioned proposal hypothesis**, with raw counts and explicit post-hoc labels. Do not infer causal Essence effects from those groups, impute missing outcomes, or select a mixture by the two largest positive scores. The existing proposal-policy contract and deterministic batch export already exist; they do not need to be rebuilt.

After that diagnosis, test the chosen generator change against the unchanged proposer with matched fresh search budgets, keeping racing and final selection fixed. Continue reporting final output relative to the same benchmark and use independent evidence for any exact-team recommendation. Another tie rule, a gate threshold fitted to this result, a large campaign, or the pending 52,000-fight historical-team confirmation is not the immediate next search-design step. No additional combat campaign was launched in this execution.

## Execution, verification and resources

One launch used the [sealed admission](Tower-Affinity-Creation-Recognition-Admission.md), its retained runtime and launcher. There were **zero retries, resumes, replacement roots or refills**. Captured gameplay assemblies and content were unchanged. The native owner, both auditors and publication process all exited 0 without timeout or active descendants.

| Phase | Seconds | Storage / verification |
| --- | ---: | --- |
| Native owned process | 1,365.859 | 672,217,290 bytes at the enclosing native/audit boundary |
| Native reconstruction audit | 119.015 | Exit 0, no active descendants |
| Independent Python audit | 47.265 | Direct terminal-report reconstruction agreed with native result |
| All audits/publication through terminal closeout | 204.656 | 6,485,522 additional bytes |
| Scientific execution through terminal closeout | **1,570.547** | **678,702,812 bytes** |
| Read-only published archive/history verification | 159.110 | Zero fights and new values; full 600-second /64-MiB allowance charged |
| Descriptive review before sealing | 0.047 | Zero fights and new values; full 120-second /16-MiB allowance charged |

The scientific run stayed within the fixed 7,200-second /3.5-GiB allowance and separate 6,000-second /3-GiB native and 1,200-second /512-MiB audit limits. Phase times are operational observations, not combat-only timings or guaranteed future performance. The admission's 600-second /512-MiB charge was already included in prior accounting and is not added twice.

The one 24,576-byte entropy draw exposed 6,144 words: **6,143 fresh values**, one historical collision and no within-batch duplicate. All fresh values remain reserved: **3,072 used and 3,071 unused**. The independent complete live-history scan verifies **743,892 values across 256 files**, equal to the prior 737,749 union plus the new reservations.

Cumulative recorded charges are **33,295.859 seconds /26,824,663,939 bytes**. Cumulative declared maxima are **76,200 seconds /47,865,397,248 bytes**. The already declared scientific maximum is not added a second time. The two additional verification/review allowances were declared before launch and charged in full at their starts, including failure. Development tests and documentation checks are separate engineering work.

## Evidence map

| Artifact | SHA-256 |
| --- | --- |
| [Scientific manifest](../TestResults/balance/tower-affinity-creation-recognition-20260924/files.json) | `f1bb922ea31e59e8c7f270b3ae05f95fff111a35524d26e70ee22739add2e897` |
| [Terminal closeout](../TestResults/balance/tower-affinity-creation-recognition-20260924/closeout.json) | `cfd116043e4dd279fadee60b0920ab2064e0abb4b856ff7c1d0b36393f952aeb` |
| [Published archive/history verification](../TestResults/affinity-creation-recognition-publication-verification-20260924/files.json) | `35e21eb9f889abb28a3ade80f1f74aa4bca498563283188677285175d1b57115` |
| [Descriptive review](../TestResults/affinity-creation-recognition-review-20260924/files.json) | `54175d22ad3b2e622a4313d14c378e1d0036560ceb92e73d8ce9b709311111de` |
| Admission manifest | `a1bb9417e44fefc9fe596ff0af95b9a6e3418071756498d90b8470e9474270a0` |
| Request | `3d2dfd52c93264875ad22a82720ce8850e82d56b6d6d083aade3806329821ab1` |
| Frozen plan | `170065b593a49609e72142d766443c3a47f7a271b937cc88d52436de2b4792a2` |

The [execution declaration](../TestResults/affinity-creation-recognition-execution-20260924/declaration.json), [single-launch intent](../TestResults/affinity-creation-recognition-execution-20260924/launch-intent.json) and [review implementation freeze](../TestResults/affinity-creation-recognition-execution-20260924/review-implementation.json) predate the scientific publication. The reviewer and collision checks were fixed and tested on literal fixtures before consuming the real result.

## Changed files and checks

Added the [descriptive reviewer](analysis/affinity-creation-recognition-review.py), its [regression tests](analysis/test-affinity-creation-recognition-review.py), this report and retained execution/verification receipts. Nine existing guides and assessment/admission documents receive only a current-status-line update; their historical bodies and earlier evidence pins are preserved.

**20 tests passed:** 15 reviewer integrity/arithmetic tests and five reservation-classification tests. They cover missing or changed cells/contrasts/catalogue membership, wrong study version, incomplete or promoted results, invalid discordance counts, population weighting, nonfinite estimates, invented unsampled outcomes, evidence mutation, historical collisions, batch duplicates, changed panels and dropped unused reservations. The real reviewer CLI also completed successfully.

Commands below are historical completed invocations, not instructions to rerun the single-use study (`python` denotes the bundled interpreter):

```powershell
python -B -X utf8 'TestResults/affinity-creation-recognition-execution-20260924/launch-once.py'
python -B -X utf8 'Balance Harness/analysis/test-affinity-creation-recognition-review.py' -v
python -B -X utf8 'TestResults/affinity-creation-recognition-execution-20260924/test-publication.py' -v
python -B -X utf8 'TestResults/affinity-creation-recognition-execution-20260924/verify-publication.py' --scientific-pin f1bb922ea31e59e8c7f270b3ae05f95fff111a35524d26e70ee22739add2e897
python -B -X utf8 'TestResults/affinity-creation-recognition-execution-20260924/reviewer.py' --run 'TestResults/balance/tower-affinity-creation-recognition-20260924' --manifest-sha256 f1bb922ea31e59e8c7f270b3ae05f95fff111a35524d26e70ee22739add2e897 --plan 'Balance Harness/Tower-Affinity-Creation-Recognition-Plan.json' --catalogue 'TestResults/affinity-creation-recognition-catalogue-20260924' --output 'TestResults/affinity-creation-recognition-review-20260924'
```

The [handoff closeout](../TestResults/affinity-creation-recognition-execution-20260924/closeout.json) records preservation, accounting and local-link checks. No required verification command remains blocked. No C# source changed, so backend tests were not rerun; execution used the previously tested admitted DLL. There are **no migrations, application configuration changes, deployments or gameplay/default-policy changes**. Unrelated working-tree changes were preserved.
