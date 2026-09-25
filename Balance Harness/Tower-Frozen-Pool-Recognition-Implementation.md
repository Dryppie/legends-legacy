# Frozen-pool recognition study builder

**Subsequent execution:** [Native execution and admission](Tower-Frozen-Pool-Recognition-Native-Implementation.md) were implemented, and the [27,648-fight diagnostic](Tower-Frozen-Pool-Recognition-Execution.md) completed both audits and publication verification. The following records the earlier sealed builder step; its plan and package bytes remain unchanged.

23 September 2026. The offline diagnostic builder is implemented, tested and used to freeze a concrete [recognition plan](Tower-Frozen-Pool-Recognition-Plan.json). It preserves the completed adaptive pilot's exact candidates and physical scenarios, records one lower-stratum probability sample, and reproduces the resulting plan from authenticated evidence.

**This completes the builder step. Native execution support is the next implementation step.** The plan explicitly says `FrozenDesignNativeIntegrationRequired` and `runnableRequest: false`. No combat, native preparation, combat-seed allocation, database change or deployment occurred. The adaptive pilot's `AbandonThisConfiguration` decision and practical defaults remain unchanged.

## Frozen membership

The [preceding diagnosis](Tower-Adaptive-Racing-Stage-Review.md) could not establish whether racing discarded stronger teams because most candidates lacked independent measurements. This plan measures strata from all twelve completed adaptive roots, rather than selecting roots or recipes using held-out results.

| Stratum | Population per root | Planned measurements per root | Total planned measurements |
| --- | ---: | ---: | ---: |
| Exact retained references | 3 | 3 | 36 |
| Final challenger nominees | 2 | 2 | 24 |
| Other members of the final four-candidate beam | 2 | 2 | 24 |
| Remaining generated candidates | 13 | 2 | 24 |
| **Total** | **20** | **9** | **108** |

The source catalogue freezes all **240 root/recipe instances**. The plan contains **108 complete seed-free scenarios** and explicitly records **132 unsampled lower-stratum instances with null independent outcomes**. None has a new measured outcome yet. Identical party identities in different roots remain separate instances with separate future panels; global recipe deduplication would change the experiment.

Every scenario preserves the producing archive's actor identities, equipment, progression, quality, fixed time, encounter assumptions and per-owner Essence ordering. Only the corresponding frozen composition is substituted into its authenticated physical template, and the old combat seed list is removed. Validation checks exact party/scenario hashes, ten ordered owners, five canonical Essences per owner, allowed-pool membership and distinct case-insensitive source-monster families within each owner. The captured cohort has no owned-copy limit; the builder rejects an altered inventory scope instead of silently generalizing its legality rules.

The three references retain their original order. `399bc776…` remains the designated primary in the source policy, and `96b94357…` remains the fixed strongest benchmark. The two nominees retain frozen nomination order; the near misses retain final beam order; the thirteen lower candidates are sorted by full party identity. Neither the final selected winner nor its held-out win count determines catalogue membership.

## Recorded probability sample

The catalogue, sampling protocol and producing source snapshot were published before the actual sample. A separate `sample` command then wrote its intent record before drawing **one 128-byte OS CSPRNG batch**. The `build` command consumes that externally retained sampling package and its manifest pin. It does not choose lower candidates itself.

For each root in order, there are `C(13,2) = 78` unordered candidate pairs. The sampler rejects bytes 234–255 and maps the next accepted byte to `byte % 78`, indexing the lexicographically ordered pairs. Each pair has exactly three byte representations among 0–233. Thus, under the stated uniform-byte model, each pair has probability **1/78** and each candidate inclusion probability **2/13**. Conditional on the batch containing enough accepted bytes, the same symmetry holds; completion does not favor a particular pair.

The actual sample consumed **fourteen bytes**, including **two rejected bytes**, and retained the **114-byte unused tail**. All 128 bytes and every accepted/rejected offset are preserved. These bytes choose recipes; they are not combat seeds, are not entered into the combat reservation registry, and must not be recycled for combat. A cryptographic hash authenticates recorded bytes but cannot independently prove their entropy origin; that assumption and the producing sampler are explicit in the receipt.

| Root | First sampled lower candidate | Second sampled lower candidate | Pair index |
| --- | --- | --- | ---: |
| 1 | `7fac9946238f` | `fa9b97fb9a8d` | 40 |
| 2 | `820c2b144ce4` | `a9aa6bbc99dd` | 44 |
| 3 | `83412b2664f4` | `a44ac530da0d` | 44 |
| 4 | `a1b50da6abdc` | `a1c238c32d08` | 63 |
| 5 | `5b8c8da0e768` | `b7e583739d2f` | 58 |
| 6 | `4d20880b378b` | `afed01f406d3` | 39 |
| 7 | `1bc0426eaf04` | `6f0d7ed72e4e` | 29 |
| 8 | `27b856e9c9e6` | `3ec4af61f22a` | 12 |
| 9 | `73c1aee04b02` | `dfc45ef0c59d` | 62 |
| 10 | `743322929330` | `b76c12493588` | 54 |
| 11 | `95e732c4ffd5` | `98677b4f441f` | 77 |
| 12 | `5d23ecec730d` | `8961b2a3ff85` | 36 |

Identifiers are abbreviated here; the [choices receipt](../TestResults/frozen-pool-recognition-sampling-20260923/choices.json) and plan retain full identities. Existing output directories are refused. If the batch is exhausted or a draw fails, the intent, bytes available and failure remain, and no partial plan is produced. No refill or same-directory retry is supported. Literal test entropy is marked `LiteralEngineeringFixture` and rejected by the real plan-publication path. A failed receipt cannot be relabeled complete by adding it to a resealed package.

## Planned evaluation and interpretation

Each root's nine teams will share **256 new combat values**. The twelve panels must be disjoint from each other and the complete live reservation history. The fixed design therefore requires **3,072 fresh combat values** and **27,648 attempted fights**, with no new search or selection fights. These numbers are exact design counts, not runtime, storage or power estimates.

Reporting is prospective: all 108 recipe rates and all 216 candidate/reference paired contrasts, retaining each root and stratum. The intended reuse of the existing approximate family-adjusted Wilson arithmetic has family **540**: 108 recipe rates plus 432 gain/loss rates. That arithmetic must be implemented and independently tested in the new native/audit profile before execution. It will remain approximate and supplies no claim of search reliability or automatic team qualification.

The lower-stratum probability sample also fixes how to summarize that finite population. Each sampled lower candidate receives weight **13/2** when estimating its thirteen-member stratum total; its stratum mean is the mean of the two sampled gains against R*. For the entire seventeen-candidate pool in one root, the planned estimate is:

```text
(sum of the four measured nominee/near-miss gains
 + (13/2) × sum of the two sampled lower-candidate gains) / 17
```

An unweighted average of the six measured challengers would overrepresent nominees and near misses. The sampling tests exhaustively verify the weighted identity across all 78 pairs for arbitrary literal finite-population values. This verifies the sampling arithmetic, not an achieved precision. Sampling uncertainty and combat uncertainty remain separate; measured individual intervals are not population-mean intervals. The thirteen-member stratum is deliberately small-sampled, so estimates can be imprecise. Null outcomes remain null and must never become losses or fitted predictions.

The study is `DevelopmentDiagnosisOnlyNoQualificationOrPolicyPromotion`, with a completed-study status of `CompleteDiagnosticOnly`. It describes these historical pools and can inform a subsequent algorithm design. It cannot reconstruct an alternative adaptive trajectory, establish performance on future roots, select a new primary or promote whichever measured recipe happens to score highest.

## Native integration boundary

The existing [fixed-family definition](../LL/tools/BalanceHarness/TowerFixedFamilyConfirmation.cs#L37) fixes eight teams, and [its validator](../LL/tools/BalanceHarness/TowerFixedFamilyConfirmation.cs#L56) binds exact family hashes. The current [protocol dispatch](../LL/tools/BalanceHarness/TowerFixedFamilyConfirmationProtocol.cs#L27) supports the 44,000- and 52,000-fight confirmation profiles. Neither is a valid runner for these twelve nine-team families. Reusing their request versions or weakening their validators would misrepresent the study.

The next implementation should add a distinct native diagnostic profile while preserving those historical versions. Reuse the captured-scenario battle executor, owned process, reservation and archive components; bind root identity in recipes, attempts, cache scope and reported rates. Add literal-outcome tests for complete 108-cell reconstruction, all root/seed pairings, stratum reporting, null unsampled entries, partial-family rejection and the absence of adoption behavior. Run backend tests through `build/run-tests.ps1` at that step.

Before real execution, the new profile also needs captured-runtime compatibility, a current full-history scan, a fresh admitted request, one declared combat entropy batch and an explicit time/storage allowance covering admission, execution, both audits and publication. This builder supplies no live history or launch allowance. Earlier unused allowances are not transferred. The pending 52,000-fight exact-team confirmation and the closed adaptive campaigns remain separate.

## Retained evidence and reproducibility

The producing implementation is [frozen-pool-recognition-plan.py](analysis/frozen-pool-recognition-plan.py), SHA-256 `65037fc07feb67706d5d3bd5bf9f39587e30d46d6d496558a0bf71da0acb89b8`.

| Artifact | SHA-256 |
| --- | --- |
| [Catalogue package manifest](../TestResults/frozen-pool-recognition-catalogue-20260923/files.json) | `a46551edc4c2a665b69ec470fbac2e04b53b9480825524797092d0aedefbb3c1` |
| [Catalogue](../TestResults/frozen-pool-recognition-catalogue-20260923/catalogue.json) | `b8f214a7f8de3153afe8a6e7003807ba23025ad3fa422ba10d2d81289177f830` |
| [Sampling package manifest](../TestResults/frozen-pool-recognition-sampling-20260923/files.json) | `6e99f9e5d56ec68198b92bb79f80435b3ec2abd80d01544e0c61b3eabecc14a1` |
| [Plan package manifest](../TestResults/frozen-pool-recognition-plan-20260923/files.json) | `3c6a0afc2e376ef0ea582cdc1e17b6f613e62b24838f32e9266df889dfbd209a` |
| [Portable plan](Tower-Frozen-Pool-Recognition-Plan.json) | `f8e8b206cd6b0cf46f420ab4d6d4d4a9568e5e46186ae8a8ae18a552fa0957b8` |

The catalogue package retains 6,762,474 bytes, the sampling package 27,641 bytes and the plan package 3,098,467 bytes. These are terminal local artifact sizes, not measured peak storage or a future execution allowance. The portable plan is a byte-identical copy of the package's `plan.json`; `.gitattributes` preserves its LF bytes. Package bindings with local machine paths stay under `TestResults`.

`verify` reauthenticates the packages, rebuilds the catalogue from the pinned source pilot, reconstructs every sampled choice from the full recorded batch and compares the complete plan. It passed on the actual frozen package: [verification receipt](../TestResults/frozen-pool-recognition-plan-verification-20260923.log). Consumed source files are hash-checked and rechecked; this does not claim a new full compressed-battle audit or live-history scan.

**17 tests passed**, including all 78 pair choices, exact inclusion probabilities and weighted expectations, all 240 real source recipes, full plan round-trip verification, reordered/missing/cherry-picked choices, resealed tampering, failed draw retention, forbidden redraws, test/live entropy separation, illegal recipes and unchanged membership when outcome values are perturbed. An initial test exposed a missing-control check in final nominees; the validator was corrected and the complete final suite passed. See the [tests](analysis/test-frozen-pool-recognition-plan.py) and [final test log](../TestResults/frozen-pool-recognition-builder-tests-final-20260923.log).

Commands executed, with `python` denoting the bundled Python interpreter's absolute path:

```powershell
python -B -X utf8 'Balance Harness/analysis/test-frozen-pool-recognition-plan.py'
python -B -X utf8 'Balance Harness/analysis/frozen-pool-recognition-plan.py' catalogue --output 'TestResults/frozen-pool-recognition-catalogue-20260923'
python -B -X utf8 'Balance Harness/analysis/frozen-pool-recognition-plan.py' sample --catalogue 'TestResults/frozen-pool-recognition-catalogue-20260923' --catalogue-pin a46551edc4c2a665b69ec470fbac2e04b53b9480825524797092d0aedefbb3c1 --output 'TestResults/frozen-pool-recognition-sampling-20260923'
python -B -X utf8 'Balance Harness/analysis/frozen-pool-recognition-plan.py' build --catalogue 'TestResults/frozen-pool-recognition-catalogue-20260923' --catalogue-pin a46551edc4c2a665b69ec470fbac2e04b53b9480825524797092d0aedefbb3c1 --sampling 'TestResults/frozen-pool-recognition-sampling-20260923' --sampling-pin 6e99f9e5d56ec68198b92bb79f80435b3ec2abd80d01544e0c61b3eabecc14a1 --output 'TestResults/frozen-pool-recognition-plan-20260923'
python -B -X utf8 'Balance Harness/analysis/frozen-pool-recognition-plan.py' verify --package 'TestResults/frozen-pool-recognition-plan-20260923' --manifest-pin 3c6a0afc2e376ef0ea582cdc1e17b6f613e62b24838f32e9266df889dfbd209a
python -B -X utf8 'TestResults/frozen-pool-recognition-report-check.py'
```

The [static report check](../TestResults/frozen-pool-recognition-report-check.py) and [receipt](../TestResults/frozen-pool-recognition-report-check-20260923.log) verify all twelve sampled-root rows, portable-plan identity, six published hash bindings, package sizes, null outcomes, budget arithmetic, source references and local links.

The publication commands above are historical and their existing outputs cannot be overwritten. Use the final read-only `verify` command to reproduce this plan. No command for the current builder remains blocked. Native execution was not invoked because the new profile and admission are not implemented yet; backend tests were not run because this step changes Python planning and documentation only.

Changed repository files are the builder and tests, portable plan, this report, `.gitattributes` and status links in the preceding review and harness documentation. Python syntax, scoped whitespace, report arithmetic, local links and source references were checked. There are no database migrations, application configuration changes or deployment implications. Unrelated uncommitted work and all sealed scientific archives were preserved.
