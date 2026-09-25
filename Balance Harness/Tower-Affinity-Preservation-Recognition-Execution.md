# Paired-pool affinity-preservation recognition: completed diagnostic

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**All 37,120 fights completed and both audits passed.** The equal-root weighted pool estimate is **-9.025 percentage points** against the benchmark for the original proposer and **-9.009 points** for the preserving proposer. Preservation minus original is **+0.015 points**, with separate **0.807-point sampling SE** and **0.300-point combat SE**. These are conditional uncertainty components, not a combined confidence interval.

The decision is **`CompleteDiagnosticOnly`**. All **176 unsampled outcomes remain unknown**. The [source pilot](Tower-Affinity-Preservation-Pilot-01-Execution.md) retains its `NoObservedOutputDifferentiation` decision; this diagnostic does not change its identical final outputs or qualify a team or policy.

## Frozen design and interpretation limits

The [frozen paired-pool plan](Tower-Affinity-Preservation-Recognition-Implementation.md) retains all twelve roots and all 321 root/recipe cells. Each original and preserving arm has seventeen generated recipes. The same physical recipe in both arms is measured once within its root. Repeated recipes across roots remain separate occurrences.

Measurements cover 36 references, 37 generated cells in the union of both arms' finalists, and 72 probability-sampled remaining cells: two each from the shared, original-only and preserving-only stratum per root. This produces **145 rates and 327 paired nonreference/reference contrasts**. Each root uses one 256-value common combat panel, disjoint from the other roots and prior history. Draws count as non-wins.

The benchmark is fixed recipe `96b943571150684df3a5be5352c94d60b32485b5797bb7b763b74f73faead78c`. It is not selected from the new observations. Every reported pool mean uses the prespecified Horvitz-Thompson sum: generated members' benchmark gains divided by their inclusion probabilities, then divided by seventeen. Mandatory cells have probability one; remaining cells have probability 2/N. An unweighted mean of the sampled union would not estimate either pool.

Shared recipes have identical measurements and weights, so their contributions cancel from the paired pool difference. Sampling variance conditions on fixed full-panel recipe outcomes; combat variance conditions on the selected sample and retains per-seed covariance. Neither component is a combined uncertainty interval and they must not be added. The all-root estimates average twelve fixed roots; their variance terms sum across roots and divide by 144. They do not estimate future-root performance.

## Complete prespecified summaries

All values in the next tables are percentage points. The SE columns refer to the paired pool difference.

| Root | Benchmark wins /256 | Original pool | Preserving pool | Difference | Sampling SE | Combat SE |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 198 | -8.444 | -9.088 | -0.643 | +2.287 | +0.849 |
| 2 | 199 | -16.682 | -12.983 | +3.699 | +3.456 | +1.105 |
| 3 | 197 | -7.169 | -9.444 | -2.275 | +2.723 | +0.979 |
| 4 | 183 | -5.113 | -4.504 | +0.609 | +2.509 | +1.411 |
| 5 | 189 | -5.630 | -6.549 | -0.919 | +0.847 | +0.711 |
| 6 | 208 | -6.951 | -11.937 | -4.986 | +2.403 | +1.165 |
| 7 | 197 | -13.028 | -12.144 | +0.885 | +3.702 | +0.771 |
| 8 | 201 | -11.294 | -6.411 | +4.883 | +4.284 | +1.092 |
| 9 | 193 | -6.009 | -4.481 | +1.528 | +4.382 | +1.138 |
| 10 | 192 | -10.409 | -9.191 | +1.218 | +1.088 | +1.092 |
| 11 | 197 | -7.893 | -7.181 | +0.712 | +0.338 | +0.809 |
| 12 | 197 | -9.674 | -14.200 | -4.527 | +1.982 | +1.149 |
| Equal-root mean | — | -9.025 | -9.009 | +0.015 | +0.807 | +0.300 |

| Frozen endpoint, equal-root average | Mean gain vs benchmark | Sampling SE | Combat SE |
| --- | ---: | ---: | ---: |
| control | -9.025 | 0.810 | 0.818 |
| candidate | -9.009 | 0.711 | 0.826 |
| difference | +0.015 | 0.807 | 0.300 |
| controlNominees | -5.332 | 0.000 | 0.671 |
| candidateNominees | -5.098 | 0.000 | 0.682 |
| controlChallenger | -5.762 | 0.000 | 1.085 |
| candidateChallenger | -4.753 | 0.000 | 1.087 |

`control` means original, and `candidate` means preserving. Nominee means include **all five** frozen nominees, including reference recipes. Challenger means use the single frozen validation challenger per arm/root. These certainty-set endpoints have no population-sampling uncertainty. The difference row is preserving minus original pool mean, rather than a contrast against the benchmark.

The next table counts the **109 measured nonreference cells**. It is descriptive; unequal inclusion probabilities mean these counts must not be interpreted as pool proportions. Individual interval signs use the prespecified approximate Wilson family of 799. Full rates, all three-reference contrasts, per-root endpoints, covariance terms and both-arm provenance remain in the [native result](../TestResults/balance/tower-affinity-preservation-recognition-20260924/result.json) and [authenticated review](../TestResults/affinity-preservation-recognition-review-20260924-v2/review.json).

| Group | Measured | Observed above / equal / below benchmark | Individual interval above / below zero |
| --- | ---: | ---: | ---: |
| All nonreferences | 109 | 11 / 1 / 97 | 0 / 10 |
| shared | 48 | 7 / 1 / 40 | 0 / 2 |
| control-only | 30 | 2 / 0 / 28 | 0 / 4 |
| candidate-only | 31 | 2 / 0 / 29 | 0 / 4 |
| mandatory | 37 | 8 / 0 / 29 | 0 / 0 |
| remaining-shared | 24 | 2 / 1 / 21 | 0 / 2 |
| remaining-control-only | 24 | 0 / 0 / 24 | 0 / 4 |
| remaining-candidate-only | 24 | 1 / 0 / 23 | 0 / 4 |

## What the diagnostic establishes

The two weighted pool estimates are almost identical: **−9.025 points for original v3 and −9.009 for preserving v4**. Their **+0.015-point** difference is small relative to both separately reported uncertainty components. Seven root point estimates favor preservation and five favor the original proposer, ranging from **−4.986 to +4.883 points**. Both arms' pool estimates are below the benchmark in every root. These results provide no material observed average pool improvement from endpoint preservation in this frozen cohort. They do not establish equivalence or rule out improvements on other roots, contexts or proposal rules.

The main demonstrated problem is the quality of the generated pools relative to the available benchmark. Of the 109 measured nonreference cells, **97 are below, one ties and eleven are above** the benchmark. No individual adjusted interval lies entirely above zero; ten lie entirely below. The probability sample leaves 176 outcomes unknown, so this does not identify the best full-pool recipe or prove that all useful recipes were recognized.

The original and preserving frozen challenger means are **−5.762 and −4.753 points** against the benchmark. The corresponding all-five nominee means are **−5.332 and −5.098 points**. These are prospective, fully measured identities, but their average performance remains negative. The nominee sets contain the three retained references; their better averages than the generated-pool averages cannot by themselves establish that ranking enriches the *novel* candidates. The diagnostic does not evaluate a replacement selector or justify lowering the validation threshold. The source pilot's all-root benchmark fallback remains its original result.

Structural preservation succeeded mechanically in the earlier implementation, but that alone has not translated into better measured average pool quality here. This distinction matters: protecting authored affinity endpoints can prevent one kind of destructive edit while leaving losses from other removed abilities or poor fit to the owner unchanged. The latter explanations are **hypotheses**, not causal findings from these aggregate outcomes.

## Single next implementation step

Extend the existing [affinity-creation edit diagnosis](Tower-Affinity-Creation-Edit-Diagnosis.md) to join this **paired v3/v4 cohort** to its exact saved proposal edits. Reuse the existing exporter and provenance machinery. Retain every measured root/recipe and both-arm occurrence, all unknown outcomes, generation wave, parent, owner/subgroup, removed/added Essences, target/newly activated routes and preservation metadata. Join by full root and recipe identity; do not substitute an outcome from another root or older cohort.

The immediate question is whether preservation merely shifts losses to other removals, or whether proposed additions have poor mechanical fit to the owner's fixed equipment and role. `TowerAffinityCreation.Create` still samples equally among eligible authored pairs and then legal minimal edits; legality and endpoint protection do not measure net combat value. Add a bounded, read-only explanation of those exact edits using captured definitions, with complete raw counts and explicitly post-hoc comparisons. Shared recipes and arm-exclusive recipes must keep their different membership and sampling probabilities; no outcome-selected subset, imputed maximum or fitted selector is appropriate.

Use that joined diagnosis to choose **one mechanically justified generator change** for a separately versioned comparison with fresh evidence. Keep racing, final selection and the benchmark fixed while testing the proposer. This is a development recommendation, not an implemented or proven new operator. The present result does not justify promoting preservation, loosening the gate, expanding this closed diagnostic, or prioritizing the pending historical 52,000-fight confirmation. No additional combat campaign was started.

## Corrected descriptive export

The scientific run, native audit and independent audit all succeeded on their first attempts. The first descriptive export failed before producing a review because it compared the pure endpoint object with the published result envelope, which also contains `studyHash` and `archiveHash`. Its fixture exercised the pure arithmetic without that envelope.

The corrected reviewer explicitly authenticates both hashes before comparing the endpoint body. A retained published native fixture and two hash-corruption regressions now exercise that boundary. The statistical endpoints, membership, weights, uncertainty calculations and interpretation rules are unchanged. The original exporter, tests, failure receipt and **full 180-second /32-MiB charge** remain preserved in the [failed review](../TestResults/affinity-preservation-recognition-review-20260924/failure.json).

The separately declared [v2 correction allowance](../TestResults/affinity-preservation-recognition-execution-20260924/review-correction-declaration.json) adds another **180 seconds /32 MiB**, charged in full. Its [implementation freeze](../TestResults/affinity-preservation-recognition-execution-20260924/review-implementation-v2.json) follows the failed export; the first implementation freeze preceded scientific publication. The corrected export completed once in its new versioned directory. There were two descriptive export attempts and **one scientific launch, with zero scientific retries**. The original failed output was not overwritten, and no fight, seed draw or reservation was repeated.

## Execution, verification and resources

One launch used the [sealed admission](Tower-Affinity-Preservation-Recognition-Admission.md), captured runtime and retained launcher. There were **zero retries, resumes, replacement roots or refills**. No captured gameplay assembly or content changed. Native execution, native reconstruction, independent direct-report reconstruction and final publication each completed in owned Windows Jobs, with exit 0, no timeout and no active descendants.

| Phase | Seconds | Retained storage / verification |
| --- | ---: | --- |
| Native owned process | 2,112.547 | 877,458,140 bytes at the native/audit boundary |
| Native reconstruction audit | 186.719 | Exit 0 |
| Independent Python audit | 69.391 | All direct terminal reports reconstructed |
| Audits/publication through terminal closeout | 316.500 | 9,838,397 additional bytes |
| Scientific total through terminal closeout | **2,429.062** | **887,296,537 bytes** |
| Published archive and complete live-history verification | 246.922 | Zero new fights/values; full 600-second /64-MiB allowance charged |
| Failed descriptive export | 0.218 | Original full 180-second /32-MiB allowance charged |
| Corrected v2 review before sealing | 0.281 | Zero new fights/values; additional full 180-second /32-MiB allowance charged |

The run stayed within its **10,800-second /6-GiB** scientific allowance, including separate 9,000-second /5,632-MiB native and 1,800-second /512-MiB audit/publication caps. Operational timings include verification and process overhead; they are not pure combat benchmarks or guaranteed future performance.

The one 24,576-byte entropy draw exposed 6,144 signed words. It produced **6,143 fresh permanent reservations**, with **1 historical collisions** and **0 within-batch duplicates**. All fresh values remain excluded: **3,072 used** and **3,071 unused**. The complete live-history scan verifies **766,415 values across 260 files**, equal to the prior 760,272 plus fresh reservations.

Cumulative recorded charges are **39,580.718 seconds /30,849,093,663 bytes**. Cumulative declared maxima are **102,000 seconds /63,300,435,968 bytes**. Prior accounting already includes the 600-second /512-MiB admission charge and the declared scientific ceiling, so neither is charged twice. The publication and initial review allowances were declared before launch; the corrected review received a separate prospective allowance. Every allowance is charged in full, including the failed export. Engineering tests and documentation checks are separate work.

## Evidence and changed files

| Artifact | SHA-256 |
| --- | --- |
| [Scientific manifest](../TestResults/balance/tower-affinity-preservation-recognition-20260924/files.json) | `dcea52f2ed251c04d3985679cfb31e71285814fa6e97199fc35379f4e82a6c39` |
| [Terminal closeout](../TestResults/balance/tower-affinity-preservation-recognition-20260924/closeout.json) | `c7e197010b7611afc1fcd814c731faefb285834f7d170225fc95691887a714ce` |
| [Published archive/history verification](../TestResults/affinity-preservation-recognition-publication-verification-20260924/files.json) | `5fffafb823e3d17bb4bc301f51d9b691da393c8ab48a6a0d01caa1b2ddd91c2c` |
| [Descriptive review](../TestResults/affinity-preservation-recognition-review-20260924-v2/files.json) | `498669c35f09c67f8bb9ef1d7e202b966bc83663deee1df1b6875ae3170ed4b6` |
| Admission manifest | `f96631cc46058b8730246304106beb067957f7480cc5bc6bb4270e339579f622` |
| Frozen plan | `8456bb84f4f061eba78d35371602ec8a49ac81e5e60a550552c02bda3d3fddf6` |

Added the [descriptive reviewer](analysis/affinity-preservation-recognition-review.py), its [tests](analysis/test-affinity-preservation-recognition-review.py), this report, and retained execution/verification receipts. Two `.gitattributes` LF rules preserve the new source hashes across Windows checkouts. Nine existing guides and assessments receive only a line-three status update; their historical bodies remain byte-identical. The [execution declaration](../TestResults/affinity-preservation-recognition-execution-20260924/declaration.json) and [review implementation freeze](../TestResults/affinity-preservation-recognition-execution-20260924/review-implementation.json) predate scientific publication.

**24 tests passed:** nineteen reviewer tests cover complete membership, unequal sampling weights, shared-cell cancellation, per-root and all-root covariance/variance arithmetic, all-five nominee summaries, changed trial/seed order, changed provenance, invented unknowns, promotion rejection and source mutation. Five reservation tests cover historical collisions, batch duplicates, unused tail preservation, panel changes and torn batches. The three additional regression cases cover a published native fixture with its provenance envelope, a changed study hash and a changed archive hash. The production reviewer reused the admitted independent auditor to reconstruct the complete prespecified endpoints from authenticated published trial vectors. Both preceding scientific audits reconstruct direct battle reports.

Completed commands below are historical single-use invocations, not instructions to rerun the study (`python` denotes the bundled interpreter):

```powershell
python -B -X utf8 'TestResults/affinity-preservation-recognition-execution-20260924/launch-once.py'
python -B -X utf8 'Balance Harness/analysis/test-affinity-preservation-recognition-review.py'
python -B -X utf8 'TestResults/affinity-preservation-recognition-execution-20260924/test-publication.py'
python -B -X utf8 'TestResults/affinity-preservation-recognition-execution-20260924/verify-publication.py' --scientific-pin dcea52f2ed251c04d3985679cfb31e71285814fa6e97199fc35379f4e82a6c39
python -B -X utf8 'TestResults/affinity-preservation-recognition-execution-20260924/reviewer-v2.py' --run 'TestResults/balance/tower-affinity-preservation-recognition-20260924' --manifest-sha256 dcea52f2ed251c04d3985679cfb31e71285814fa6e97199fc35379f4e82a6c39 --plan 'Balance Harness/Tower-Affinity-Preservation-Recognition-Plan.json' --catalogue 'TestResults/affinity-preservation-recognition-catalogue-20260924' --auditor 'TestResults/affinity-preservation-recognition-admission-20260924/auditor.py' --output 'TestResults/affinity-preservation-recognition-review-20260924-v2'
```

The [handoff closeout](../TestResults/affinity-preservation-recognition-execution-20260924/closeout.json) records preserved history, document bodies, local links and accounting. No required command remains blocked. No C# source changed, so backend tests were not rerun; the admitted runtime carries the preceding 142 passing backend tests and native verification. There are **no migrations, application configuration changes, deployments, gameplay edits or policy-default changes**. Unrelated working-tree changes were preserved.
