# Allied-action comparison: first pilot

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The single admitted pilot completed and passed both audits. Its frozen decision is `Inconclusive`.** Allied-action-preserving v5 minus endpoint-preserving v4 is **+1.042 percentage points** across all twelve roots; v5 minus the fixed benchmark is **+0.000 points**. Final outputs differ at **2/12 roots**, and v5 returns a novel recipe at **0/12**. No policy is promoted.

Neither the frozen go rule nor the abandon rule was met. This pilot does not support promoting allied-action preservation. The twelve-root interval is descriptive and does not establish future-root reliability.

The candidate returned the fixed benchmark at all twelve roots. Its relative gain came from roots where the control selected different recipes with fewer held-out wins.

## Matched comparison and result

The [admitted design](Tower-Affinity-Allied-Action-Admission.md) changes only the proposer, from endpoint-preserving v4 to allied-action-preserving v5. Both arms retain the same selected affinity IDs, captured inventory, benchmark parent, reference set, racing allocation and benchmark-validation selector. They share the root, four eight-value racing panels, sixteen nomination values and sixty validation values, with separate fight charges. No preview screening, replacement roots, refill or retry occurred.

All 24 searches completed before the global output freeze. Held-out evaluation then used a separate 256-value panel per root. Identical physical outputs share one archive of held-out battles within that root. The run charged **16,256 fights**: **12,672 search** and **3,584 held-out**, within the fixed 21,888-fight ceiling. Every root is retained.

| All-root endpoint | Mean gain, percentage points | Descriptive 95% root interval |
| --- | ---: | ---: |
| Allied-action v5 minus endpoint-only v4 | +1.042 | [-0.561, +2.644] |
| Allied-action v5 minus fixed benchmark | +0.000 | [+0.000, +0.000] |

Across the twelve equal-sized panels, v4 has 2,316/3,072 wins, v5 has 2,348/3,072 and the benchmark has 2,348/3,072. These role totals may refer to shared physical battles; adding them would overcount fight charges. The root intervals describe these twelve outputs and are not a powered efficacy test. Conditional paired-seed uncertainties and covariance remain in the [audited result](../TestResults/balance/tower-affinity-allied-action-pilot-01-20260924/result.json).

| Root | V4 wins | V5 wins | Benchmark wins | Method gain, pp | Benchmark gain, pp | V4 / v5 gate | Final outputs |
| --- | ---: | ---: | ---: | ---: | ---: | --- | --- |
| 1 | 201/256 | 201/256 | 201/256 | +0.000 | +0.000 | fallback / fallback | same |
| 2 | 187/256 | 199/256 | 199/256 | +4.688 | +0.000 | pass / fallback | different |
| 3 | 178/256 | 198/256 | 198/256 | +7.812 | +0.000 | pass / fallback | different |
| 4 | 190/256 | 190/256 | 190/256 | +0.000 | +0.000 | fallback / fallback | same |
| 5 | 188/256 | 188/256 | 188/256 | +0.000 | +0.000 | fallback / fallback | same |
| 6 | 193/256 | 193/256 | 193/256 | +0.000 | +0.000 | fallback / fallback | same |
| 7 | 197/256 | 197/256 | 197/256 | +0.000 | +0.000 | fallback / fallback | same |
| 8 | 194/256 | 194/256 | 194/256 | +0.000 | +0.000 | fallback / fallback | same |
| 9 | 198/256 | 198/256 | 198/256 | +0.000 | +0.000 | fallback / fallback | same |
| 10 | 192/256 | 192/256 | 192/256 | +0.000 | +0.000 | fallback / fallback | same |
| 11 | 198/256 | 198/256 | 198/256 | +0.000 | +0.000 | fallback / fallback | same |
| 12 | 200/256 | 200/256 | 200/256 | +0.000 | +0.000 | fallback / fallback | same |

Both proposal waves filled at every root. Of the 204 generated positions per arm, **138 positions differ**. V4 passed benchmark validation at **2 roots** and fell back at **10**; v5 passed at **0** and fell back at **12**. V4 selected the fixed benchmark at **10/12 roots**, and v5 at **12/12**. Complete gain/loss counts and exact gate arithmetic for both arms are retained. A gate pass is a search decision, not independent confirmation. Unselected recipes were not assigned held-out outcomes.

## Execution, exposure and verification

The [execution declaration](../TestResults/affinity-allied-action-pilot-01-execution-declaration-20260924.json) fixed one launch, zero retries, 4,380 assigned values and all scientific limits before execution. The admitted runtime, launcher, content and plan were used without rebuilding or substitution. Native execution, native reconstruction, independent Python audit and the final publication barrier all passed with identical results.

A separately declared **600-second /64-MiB read-only allowance** then ran native post-publication verification and a complete live-history scan under Windows Job ownership. It did not extend scientific budgets or run additional combat. The [publication verification](../TestResults/affinity-allied-action-pilot-01-publication-verification-20260924/verification.json) passed; all child processes exited, and the scientific archive remained unchanged.

The one entropy batch permanently reserved **16,381 fresh values**. It assigned **4,380**; **12,001 unused values remain permanently excluded**. Complete live history now contains **782,796 values across 262 files**. Only this study's two owned history files extend the prior set, and all earlier history hashes remain unchanged.

Scientific manifest: `c9d191a8f81439d6bfa947e8c62bd74a6e35352ee5d999b8dcd9ecdfdded54ef`. Scientific closeout: `f58552cbd568e66f6e76090f01cd0be8d450e173b552513d445e97a73379dc82`. Publication-verification manifest: `5a1553cf3c0591086514c632ced8c0513ac31cce5ceb8d4dae559156ea8aa7dc`. These pins and the result are retained in the [closed handoff](../TestResults/affinity-allied-action-pilot-01-execution-handoff-20260924.json).

## Resource accounting

| Partition | Measured seconds | Hard seconds | Retained bytes | Hard bytes |
| --- | ---: | ---: | ---: | ---: |
| Native work | 848.078 | 9,000 | 1,897,674,760 | 5,905,580,032 |
| Audit/publication | 193.266 | 1,800 | 2,405,528 | 536,870,912 |
| Scientific total | 1,041.344 | 10,800 | 1,900,080,288 | 6,442,450,944 |

The separate publication check measured **193.156 seconds / 91,918 bytes**; its full 600-second /64-MiB allowance remains charged. Cumulative recorded work is **41,255.894953 seconds / 33,516,834,007 bytes**. Cumulative declared maxima are **116,220 seconds / 72,225,914,880 bytes**. Lower observed use never reduces declared ceilings; prior charges are carried from the authenticated admission handoff.

## Changed files and checks

- `verify-affinity-allied-action-publication.py` adds bounded archive and complete-history verification for the new study, with separate observed and declared accounting.
- `test-affinity-allied-action-publication.py` passed **13 tests** covering reservation tails, mismatched versions/assignments, history drift, execution limits and accounting.
- This report, the execution declaration and handoff, `.gitattributes`, nine current-status lines and retained receipts document the closed result. No search, gameplay or selection implementation changed.

`python -B -X utf8 "Balance Harness/analysis/test-affinity-allied-action-publication.py"` passed. The admitted `run-proposal-affinity-study.py` completed once and both required audits passed. `verify-affinity-allied-action-publication.py` passed with external admission, declaration and scientific closeout pins. The existing 125 backend, 67 Python implementation and 28 admission tests remain authenticated; backend code was unchanged and was not rebuilt or retested. No verification command remains blocked.

The prior 35-member admission verification package was authenticated before changes. All 122 inherited pins and nine historical document bodies remain unchanged. No migrations, application configuration changes, deployments or default-policy changes occurred. Unrelated working-tree changes were preserved.

## Next step

Review the saved proposal pools, nominations and validation gates under a bounded read-only scope before designing another fresh comparison. This pilot is closed. It cannot be relabeled as fresh confirmation or have its allowance extended. Earlier pilots and the recognition diagnostic retain their original interpretations.
