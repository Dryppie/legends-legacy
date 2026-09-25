# Paired affinity-preservation recognition plan

24 September 2026. Target: the offline `LL/tools/BalanceHarness`.

**The paired-pool plan adapter is implemented, tested and used to freeze a concrete diagnostic design.** The [plan](Tower-Affinity-Preservation-Recognition-Plan.json) contains **145 measured root/recipe cells**, chosen from the complete **321-cell catalogue**. It retains **176 explicit unmeasured cells**. Deterministic verification reproduces the catalogue, sampling choices and plan from authenticated evidence and the recorded draw.

The plan status is `FrozenDesignNativeIntegrationRequired`; `runnableRequest` is false. **40 tests passed.** No combat, native preparation, combat entropy or seed reservations occurred. There was one separately recorded **256-byte population-sampling draw**, used only to choose catalogue members.

## Frozen cohort and sampling rules

The [stage review](Tower-Affinity-Preservation-Stage-Review.md) found that preservation changes the final validation challenger at seven of twelve roots, but both arms return the benchmark everywhere. It cannot determine the independent quality of the unselected recipes. The new diagnostic measures the frozen pools without changing that completed pilot's `NoObservedOutputDifferentiation` decision or tuning its gate.

The [builder](analysis/affinity-preservation-recognition-plan.py) authenticates both source arms, their v5/v6 racing contracts, the publication verification and sealed stage review. All twelve roots remain. The union contains **285 generated root/recipe occurrences**, plus three reference occurrences per root. Each original and preserving pool contains seventeen generated recipes. Shared physical recipes are deduplicated within a root, while the same recipe at different roots remains a separate occurrence.

Every cell retains both-arm provenance: generation wave and accepted position, final-beam rank, nominee rank and validation-challenger identity, or explicit absence from that arm. The physical scenario preserves the captured actor, gear, identity, ordering and other fields; only composition is taken from that exact saved recipe and combat seeds are cleared. The source scope with unrestricted owned copies is checked explicitly rather than generalized.

The certainty set contains **all references and the union of both arms' final nominees and validation challengers**. All challengers already belong to those nominee sets. This yields **36 reference cells and 37 distinct generated finalist cells**. A shared recipe becomes a certainty cell for both pool estimators even if only one arm nominated it.

The remaining generated cells are partitioned by same-root source membership:

| Remaining stratum | Population cells | Sampled cells |
| --- | ---: | ---: |
| Shared by both arms | 99 | 24 |
| Original-only | 75 | 24 |
| Preserving-only | 74 | 24 |
| Total | 248 | 72 |

Within each root and remaining stratum, select **min(2, N)** distinct recipes uniformly without replacement. A population of zero, one or two is a census and consumes no sampling bytes. Every other candidate has a positive known inclusion probability. Final-beam near misses and pruned candidates retain their provenance within these groups; they are not selected using observed win counts. The concrete populations all contain at least four members per remaining stratum, so this plan samples two from each of the 36 groups.

Individual inclusion is **2/N**, joint inclusion for two distinct members is **2/[N(N−1)]**, and the total-estimator weight is **N/2**. Certainty cells have probability and weight one. The full catalogue and sampling design were published before drawing. Actual measured-cell and fight counts were [recorded before sampling](../TestResults/affinity-preservation-recognition-builder-20260924/pre-sampling-counts.json); they do not depend on which subsets were drawn.

| Root | Generated certainty cells | Remaining shared /original-only /preserving-only | Recorded subset indices, same order | Measured including 3 references | Unmeasured |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 4 | 9 /6 /4 | 23 /12 /4 | 13 | 13 |
| 2 | 4 | 8 /6 /6 | 26 /11 /7 | 13 | 14 |
| 3 | 3 | 8 /6 /6 | 21 /12 /0 | 12 | 14 |
| 4 | 4 | 5 /9 /9 | 8 /17 /10 | 13 | 17 |
| 5 | 2 | 11 /4 /4 | 22 /1 /1 | 11 | 13 |
| 6 | 2 | 8 /7 /7 | 17 /6 /5 | 11 | 16 |
| 7 | 3 | 10 /4 /5 | 29 /3 /1 | 12 | 13 |
| 8 | 3 | 8 /7 /6 | 19 /17 /2 | 12 | 15 |
| 9 | 2 | 8 /7 /7 | 1 /8 /19 | 11 | 16 |
| 10 | 3 | 8 /7 /7 | 17 /6 /13 | 12 | 16 |
| 11 | 3 | 9 /5 /6 | 18 /4 /10 | 12 | 14 |
| 12 | 4 | 7 /7 /7 | 18 /0 /18 | 13 | 15 |

Subset indices refer to lexicographically ordered unordered pairs of ascending full recipe IDs within each group. Exact IDs, population hashes, inclusion probabilities and byte offsets are in the [sampling receipt](../TestResults/affinity-preservation-recognition-sampling-20260924/choices.json).

## One bounded population sample

The sampler made one OS CSPRNG draw of 256 bytes after catalogue publication and intent recording. It consumes unsigned 16-bit little-endian words in root then stratum order. For M possible pairs, accept a word below `floor(65536/M)*M` and take its remainder modulo M. This avoids modulo bias. Every rejected word and unused byte remains in the package; an exhausted batch cannot produce a partial plan or be refilled.

This draw consumed **72 bytes**, rejected **zero words**, and retained **184 unused bytes**. It allocated no combat values. Existing output directories are refused; failures preserve their intent and available entropy. Literal engineering draws are labeled and rejected by the production publication path. Verification checks the external catalogue/sampling pins, source hashes, recorded entropy, exact choices and plan reconstruction. A hash authenticates the retained bytes but cannot prove that their origin was random; the design states its OS CSPRNG assumption.

## Prospective measurements and estimands

Each root's eleven to thirteen measured recipes would share a fresh 256-value combat panel. The twelve panels must be disjoint and excluded from complete live history. The frozen plan therefore requires **3,072 fresh combat values and 37,120 attempted fights**. Those are future design counts, not an executed experiment, resource admission or power claim.

Report all **145 recipe rates** and **327 generated-recipe/reference paired contrasts**. The prespecified approximate Wilson family is **799**: 145 rates plus the gain and loss rates for each of 327 signed contrasts. The strongest captured reference remains the fixed benchmark. A later native profile and independent audit must verify this arithmetic for variable root sizes.

For a sampled generated recipe i, let `gain_i` be its paired win-rate difference from the benchmark on that root's fresh panel and `pi_i` its inclusion probability. Each arm's seventeen-recipe mean is estimated as:

```text
root arm mean = sum(gain_i / pi_i for measured generated members of that arm) / 17
paired pool difference = preserving root mean - original root mean
all-root pool difference = equal mean of the twelve paired pool differences
```

Shared recipes use the same measurements and inclusion weights, so their contributions cancel in the paired pool difference. Separate-arm uncertainty calculations that treat these cells as independent would be incorrect. Mandatory finalists are intentionally overrepresented in the measured set; an unweighted measured-pool average would not estimate the full pool mean.

The plan specifies the finite-population sampling variance for each declared linear contrast: for a noncensus stratum, `N² × (1−k/N) × sampleVariance(z) / k`, where z includes the estimand coefficient. Census strata contribute zero. Tests exhaustively verify inclusion and weighted expectations across supported population sizes, verify the sampling-variance identity, and check shared-cell cancellation. Sampling uncertainty is conditional on fixed full-panel recipe outcomes. Combat uncertainty must retain per-seed covariance conditional on the selected sample. **Report the two uncertainty components separately; do not naively add their variance estimates or label either one a combined confidence interval.**

Report both frozen nominee means and validation challengers alongside all measured near misses/pruned candidates, strata, weights and nulls. The probability sample can miss a rare strong recipe; it does not identify the full-pool maximum. Unmeasured outcomes stay unknown, and no replacement selector is scored retrospectively. These estimands describe this fixed development cohort and cannot establish general search reliability, qualify a team or promote a policy. The reporting decision remains `CompleteDiagnosticOnly`.

## Versioning and next implementation

The new version is **`tower-affinity-preservation-recognition-plan-v1`**. It binds the preservation comparison source, original racing v5, preserving racing v6, both generator policies and the unchanged benchmark-validation selector. The source runtime execution hash is `3fc51fa4ffbe9fccd1acac7b9a1df131f1915a6b2ef014a25b14992df4cfc988`. A future admission must establish compatibility with that captured runtime and data.

The existing native [recognition profiles](../LL/tools/BalanceHarness/TowerAffinityCreationRecognition.cs) pin their own earlier plans. Their [assessment implementation](../LL/tools/BalanceHarness/TowerFrozenPoolRecognition.cs) assumes 108 cells, nine teams per root and a single thirteen-member lower stratum. Those assumptions cannot execute this 145-cell paired design. Both existing profiles and plans remain unchanged.

Add a separately versioned native execution and independent-audit profile for the frozen 145-cell paired-pool plan, including variable root sizes, both-arm provenance, inclusion weights and covariance-aware reporting. Keep existing recognition profiles unchanged. Complete source-bound tests and then a separate runtime/resource admission before any combat.

The next profile needs explicit root offsets, deduplicated physical cells, both-arm membership, dynamic reporting-family counts, weighted pool summaries and separate uncertainty outputs. Admission must bind the exact new plan hash, complete live exclusion history, captured content/runtime, and cumulative time/storage allowances for execution and both audits. Combat entropy and permanent reservation belong only to that later admitted run. This builder does not provide a runnable path through either old profile.

## Verification, files and accounting

**40 tests passed**: 17 new [paired-pool tests](analysis/test-affinity-preservation-recognition-plan.py) and 23 [legacy builder regressions](analysis/test-affinity-creation-recognition-plan.py). Coverage includes complete union membership, both-arm provenance, mandatory inclusion, small-stratum census, exhaustive uniform subset mapping and weights, variance/covariance identities, prospective counts, null outcomes, changed versions/context/scope, source and package tampering, entropy exhaustion, no redraw, fixture rejection and full recorded-package round trips.

Commands used the bundled Python runtime with `-B -X utf8`:

```text
Balance Harness/analysis/test-affinity-preservation-recognition-plan.py
Balance Harness/analysis/test-affinity-creation-recognition-plan.py
TestResults/affinity-preservation-recognition-builder-20260924/publish.py
TestResults/affinity-preservation-recognition-builder-verification-20260924/finish.py
```

The publisher declared **180 seconds /64 MiB** before authenticating, publishing or sampling. It completed once in **15.547 seconds**, retaining **18,007,486 bytes**, including the canonical plan copy. The full allowance is charged on success or failure. Cumulative recorded charges are **35,591.656 seconds /29,290,708,486 bytes**; cumulative declared maxima are **89,640 seconds /56,186,896,384 bytes**. Development tests and documentation checks are separate engineering work.

The last full live-history verification remains **760,272 excluded values across 258 files**; it is inherited and was not rescanned. The previous 35-member handoff and all 45 historical pins were authenticated. Only line three of the nine current-status documents changed; historical bodies were preserved byte-for-byte. The [final verification receipt](../TestResults/affinity-preservation-recognition-builder-verification-20260924/verification.json) and [handoff](../TestResults/affinity-preservation-recognition-builder-handoff-20260924.json) retain the checks and next step.

External pins:

- Canonical plan: `8456bb84f4f061eba78d35371602ec8a49ac81e5e60a550552c02bda3d3fddf6`.
- Catalogue manifest: `c9b448243b4cd2edb530678740c733e485257550de7810db6fb5296420330529`.
- Sampling manifest: `a3dd8159dc8d4016d5a1e172387816ad8d016e19a933b28c31a5930ae9619ae0`.
- Plan package manifest: `8df81444183e1b335d55af8fe0914dc752724735d5f334638a1ee83416dc90b1`.
- Aggregate builder publication manifest: `1ea3fcae2208afe52c8f34078757a10821e373b038f4a8cada6495c75cb2d161`.

Changed files are the new builder, tests, canonical JSON plan, this report, nine status banners and retained publication/verification receipts. Three new LF rules in `.gitattributes` preserve the plan and reproducing source bytes. No required verification command remains blocked. No backend code or admitted runtime changed, so no backend rebuild or test rerun was needed. There are **no migrations, application configuration changes, deployments or gameplay-default changes**. Unrelated working-tree changes were preserved.
