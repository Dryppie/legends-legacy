# Affinity preservation comparison: first pilot

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The single admitted pilot completed and passed both audits. Its frozen decision is `NoObservedOutputDifferentiation`.** Preserving v4 minus original v3 is **+0.000 percentage points** across all twelve roots; preserving v4 minus the fixed benchmark is **+0.000 points**. Final outputs differ at **0/12 roots**, and the preserving arm returns a novel recipe at **0/12**. No policy is promoted.

The two proposers returned the same final physical recipe at every root. The paired method contrast is exactly zero because held-out observations are shared for identical outputs. This does not establish an improvement from preservation, or equivalence of the unselected candidate pools.

## Matched comparison and result

The [admitted design](Tower-Affinity-Preservation-Admission.md) changes the proposer from original v3 to endpoint-preserving v4. Both arms retain the exact same racing allocation and benchmark-validation selector, selected authored affinity IDs, captured inventory, benchmark parent and reference set. Both use the same root, four eight-value racing panels, sixteen nomination values and sixty validation values, with separate fight charges. There is no root preview, replacement or refill.

All 24 searches completed before the single global output freeze. Held-out evaluation then used a separate 256-value panel for each root. Identical physical outputs share one archive of held-out battles within a root. The run charged **15,744 fights**: **12,672 search** and **3,072 held-out**, below the fixed 21,888-fight maximum. Every root is retained.

| All-root endpoint | Mean gain, percentage points | Descriptive 95% root interval |
| --- | ---: | ---: |
| Preserving v4 minus original v3 | +0.000 | [+0.000, +0.000] |
| Preserving v4 minus fixed benchmark | +0.000 | [+0.000, +0.000] |

Across the twelve equal-sized panels, the original arm has 2,385/3,072 wins, the preserving arm 2,385/3,072 and the benchmark 2,385/3,072. These role totals can refer to shared physical battles; they must not be added to obtain the fight charge. Root intervals describe these twelve outputs and are not a powered efficacy test. Conditional paired-seed uncertainties and covariance are retained in the [audited result](../TestResults/balance/tower-affinity-preservation-pilot-01-20260924/result.json).

| Root | Original wins | Preserving wins | Benchmark wins | Method gain, pp | Benchmark gain, pp | Original / preserving gate | Final outputs |
| --- | ---: | ---: | ---: | ---: | ---: | --- | --- |
| 1 | 192/256 | 192/256 | 192/256 | +0.000 | +0.000 | fallback / fallback | same |
| 2 | 200/256 | 200/256 | 200/256 | +0.000 | +0.000 | fallback / fallback | same |
| 3 | 193/256 | 193/256 | 193/256 | +0.000 | +0.000 | fallback / fallback | same |
| 4 | 195/256 | 195/256 | 195/256 | +0.000 | +0.000 | fallback / fallback | same |
| 5 | 195/256 | 195/256 | 195/256 | +0.000 | +0.000 | fallback / fallback | same |
| 6 | 198/256 | 198/256 | 198/256 | +0.000 | +0.000 | fallback / fallback | same |
| 7 | 200/256 | 200/256 | 200/256 | +0.000 | +0.000 | fallback / fallback | same |
| 8 | 198/256 | 198/256 | 198/256 | +0.000 | +0.000 | fallback / fallback | same |
| 9 | 211/256 | 211/256 | 211/256 | +0.000 | +0.000 | fallback / fallback | same |
| 10 | 203/256 | 203/256 | 203/256 | +0.000 | +0.000 | fallback / fallback | same |
| 11 | 198/256 | 198/256 | 198/256 | +0.000 | +0.000 | fallback / fallback | same |
| 12 | 202/256 | 202/256 | 202/256 | +0.000 | +0.000 | fallback / fallback | same |

Both proposal waves filled at every root. Of the 204 generated positions per arm, **118 positions differ** between arms. The original arm passed benchmark validation at **0 roots** and fell back at **12**; the preserving arm passed at **0** and fell back at **12**. The original arm selected the fixed benchmark at **12/12 roots**, and the preserving arm at **12/12**. Complete gain/loss counts and exact gate arithmetic for both arms are in the result. A gate pass is a search selection decision, not independent confirmation. Unselected recipes were not assigned held-out outcomes by this study.

## Execution and publication verification

The [execution declaration](../TestResults/affinity-preservation-pilot-01-execution-declaration-20260924.json) fixed one launch, zero retries, the 4,380-value allocation and all scientific limits before execution. The admitted runtime and owner were used without rebuilding or changing the plan. The native worker, native reconstruction, independent Python audit and final publication barrier all completed. Their results agree exactly.

A separately declared **600-second /64-MiB read-only allowance** then ran native post-publication verification and a complete live-history scan through the same Windows Job ownership mechanism. It did not extend the scientific budgets or run new combat. The [publication verification](../TestResults/affinity-preservation-pilot-01-publication-verification-20260924/verification.json) passed. All child processes exited; the scientific archive remained unchanged.

The one entropy batch permanently reserved **16,380 fresh values**, of which **4,380 were assigned** and **12,000 remain unused but permanently excluded**. Complete live history now contains **760,272 excluded values across 258 files**. Only this study's two owned history files extend the prior set, and all preceding history hashes remain unchanged.

Scientific manifest SHA-256: `1c4fb870912fca7b168596cc86645cb54e96361ba8099eac1da5b4b8748d49e4`. Scientific closeout SHA-256: `f9740a37d49210308261c841115661f774cfbf9e893e84398c138c74fb689d26`. Publication-verification manifest SHA-256: `d22377f904f4c287cd9a47364ce3843173cc805d16a16cbf102401c1c5027883`. The [closed handoff](../TestResults/affinity-preservation-pilot-01-execution-handoff-20260924.json) retains these pins and the result.

## Resource accounting

| Partition | Measured seconds | Hard seconds | Retained bytes | Hard bytes |
| --- | ---: | ---: | ---: | ---: |
| Native work | 1,085.672 | 9,000 | 1,856,058,914 | 5,905,580,032 |
| Audit/publication | 230.281 | 1,800 | 2,348,105 | 536,870,912 |
| Scientific total | 1,315.953 | 10,800 | 1,858,407,019 | 6,442,450,944 |

The separate publication check measured **182.781 seconds /90,967 bytes**; its full 600-second /64-MiB maximum remains charged in the declared allowance ledger. Cumulative recorded work is **35,231.656 seconds /29,156,490,758 bytes**. Cumulative declared maxima are **89,280 seconds /56,052,678,656 bytes**. Prior accounting is carried from the authenticated admission handoff; lower actual use never reduces declared ceilings.

## Changed files and checks

- [verify-affinity-preservation-publication.py](analysis/verify-affinity-preservation-publication.py): bounded published-archive and full-history verification for the new study version, with separate recorded and declared accounting.
- [test-affinity-preservation-publication.py](analysis/test-affinity-preservation-publication.py): **13 passing tests** for reservation tails, version/assignment mismatches, history drift, execution limits and accounting.
- This report, the execution declaration and handoff, nine current-status lines and retained scientific/verification receipts. No search, gameplay or selection implementation changed during this execution.

`python -B -X utf8 "Balance Harness/analysis/test-affinity-preservation-publication.py"` passed all 13 tests. The admitted `run-proposal-affinity-study.py` completed once; its native and independent audits passed. `verify-affinity-preservation-publication.py` passed with the external admission, declaration and scientific closeout pins. The existing 167 backend tests, 37 archive tests and 26 admission tests remain authenticated; unchanged backend code did not require another test run. No verification command remains blocked.

The prior 35-member admission handoff was authenticated before changes. All 41 historical pins and nine historical document bodies remain preserved. No migrations, application configuration changes, deployments or default-policy changes are involved. Unrelated working-tree changes were preserved.

## Next step

A bounded review of the saved generation, nomination and validation evidence to locate where changed proposals failed to produce different final outputs; no new combat yet. This pilot is closed. Its observations cannot be turned into a fresh confirmation by relabeling them, and its resource allowance cannot be extended. Earlier pilots and the recognition diagnostic retain their original interpretations.
