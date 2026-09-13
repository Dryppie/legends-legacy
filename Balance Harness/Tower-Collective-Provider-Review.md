# Collective-provider search: implementation and bounded comparison

The opt-in **`independent-collective-v6`** policy completed its frozen comparison against unchanged v5 with **Fail: 0/3 passing restarts, with 2 required**. Of 12 generated finalists, **0 recorded a held-out win**. The six saved controls measured **25.39–51.17%**. Both ordinary and joint-adjusted tested-family assessments are **Fail** and **Fail**, respectively. The strongest control measured **131/256 (51.17%)**, with joint-adjusted upper bound **60.92%**. Observed validation ceiling breaches: **1**; discovery observations above 50%: **0**. All observations remain saved.

This completes the authorized collective-mutation change and one capped comparison. It does not establish reliable discovery or near-optimality. The v4 pilot (1/3), its replication (0/3) and v5 pilot (0/3) remain sealed, separate experiments. No samples, gates or secondary results were pooled to replace a preselected primary. V1 remains the default.

The ceiling failure follows the policy's **observed-rate rule**. The strongest control's joint-adjusted interval is **41.33–60.92%**, which includes 50%; this sample does not statistically establish that its underlying win rate exceeds 50%. That uncertainty does not erase the observed breach or permit acceptance. Earlier scoped Pass results retain their original families and samples; this newly tested family is Fail.

## Tested change and scope

The [sealed v5 findings](Tower-Coverage-Provider-Review.md) showed that 10 of 18 provider substitutions changed one position. V6 requires a source provider appearing on **multiple characters in the current generated parent**. This is the definition of a collective move, not a target recipe count. The generator still chooses a content-derived category with legal options uniformly, then a legal source/replacement pair uniformly. It replaces every occurrence of that source at its current position and preserves every unrelated slot. If no legal repeated-source pair exists, the proposal records `no-compatible-collective-provider-substitution`; there is no singleton fallback or hidden repair.

[TowerCoverageProvider.cs](../LL/tools/BalanceHarness/TowerCoverageProvider.cs) shares legality and replacement logic with v5, while the v5 public operation retains its original behavior. [TowerBossGeneration.cs](../LL/tools/BalanceHarness/TowerBossGeneration.cs) recognizes exactly `["provider-joint", "collective-joint"]` for the new policy. Only the scheduled provider operation changes. Fresh coverage construction, the uniform route, mutation frequency, ranking, beam/exploration, placement and other operators remain unchanged. Input/feature validation and provenance contracts recognize v6 without changing older policies. Saved IDs, recipes, reference ancestry, prior fitness and held-out measurements never enter fresh generation.

The two methods have separate deterministic generation streams and shared combat schedules. This compares the resulting search policies; it is not an identical-parent causal experiment for one mutation. Broad coverage categories are hypotheses about function, not proof of equivalent timing, magnitude or combat efficacy.

Target: offline `LL/tools/BalanceHarness`, Kharad floor 5 at **Health 3.04881408 / Power 3.85370128**. Budget: ten level-40, tier-1, rank-2 Standard characters, five level-1 unascended/unevolved Essences each, fixed gear, no Combat Styles/contributions. The entire eligible pool, including Rare, remains available under hypothetical ownership; practical acquisition is unverified. No gameplay source, content value, gear, catalog, default, migration, configuration, database or deployment changed.

## Verification and frozen protocol

Before combat, [initial verification](../TestResults/balance/tower-collective-provider-20260913/initial-verification.json) checked all three previous packages and sealed reviews. All four gameplay assemblies remain **byte-for-byte unchanged**, as shown in [build compatibility](../TestResults/balance/tower-collective-provider-20260913/build-compatibility.json). The changed harness assembly has its own frozen identity.

All six historical v5 arms [reconstructed exactly without combat](../TestResults/balance/tower-collective-provider-20260913/v5-comparator-reconstruction.json) using only their original discovery score lookup in a separate compatibility check. Removing or reversing all six references produced [identical fresh-generation inputs](../TestResults/balance/tower-collective-provider-20260913/generation-boundary-verification.json). The 66 coverage features and 48 mechanic cores are unchanged. The [163 passing regression tests](../TestResults/balance/tower-collective-provider-20260913/regression-tests.trx) cover multi-character substitutions across four floor/slot budgets, singleton rejection, ownership/family constraints, parent preservation, determinism, missing/empty features, uniform fallback, v5 comparator equality, restricted ancestry, reference invariance and compact interrupted/resumed parity.

The [protocol](../TestResults/balance/tower-collective-provider-20260913/protocol.json) and [experiment design](../TestResults/balance/tower-collective-provider-20260913/experiment-design.json) froze before any pilot combat. Protocol SHA-256: `806f9639e82b512456a5354759e872e76ff4c6153ec6de35e61a1a960ba07697`.

| Phase | Frozen allocation | Maximum fights |
| --- | --- | ---: |
| Discovery | 2 methods × 3 restarts × 96 evaluated parties × 8 shared fresh seeds | 4,608 |
| Validation | Top 2 per arm + all 6 controls; maximum 18 recipes × 256 fresh seeds | 4,608 |
| Diagnostics | 4 historical detailed parity replays + first validation seed for each collective primary and the fixed anchor | 8 |
| **Total** | **Zero combat retries; no automatic resume** | **9,224** |

Each arm allows at most 2,048 proposals. The entire execute invocation is capped at 600 seconds and the package at 1 GiB, with each campaign capped at 512 MiB. The [seed ledger](../TestResults/balance/tower-collective-provider-20260913/seed-ledger.json) excludes **469,196** historical seeds: every array of the v5 ledger, including unused reservations. New generation, discovery, held-out validation and unused selection/confirmation reservations are mutually disjoint. Repeated use across arms is paired sampling, not additional independent samples per recipe.

The [complete family](../TestResults/balance/tower-collective-provider-20260913/validation-selection.json) was frozen before validation. Discovery rank one remains each arm's primary; rank two is exploratory. All six controls are retained, with `team-1abe76ca1891d97a91d484f0a3662048` fixed as the gate anchor. At least two of three collective primaries must satisfy all: adjusted rate lower ≥10%, paired lower difference against the same-restart v5 primary >0, and paired lower difference against the fixed anchor ≥−10 pp. Other controls and secondaries cannot replace these comparisons after outcomes.

Joint nominal alpha .05 is split .025 over the complete rate family and .025 over six paired comparisons. Each paired difference subtracts two Wilson discordance bounds using component alpha `.025/12`. Approximate coverage applies within this fixed experiment, without a lifetime repeated-study guarantee. The ordinary evaluator independently retains `.05/18`. No historical pooling, held-out reranking or optional extension is allowed. The 10–50% policy still preserves every observed ceiling breach and requires supported ≥10% viability in every intended-budget cohort.

## Results

The table gives held-out wins out of 256 for each primary / secondary:

| Generation seed | Unchanged v5 wins | Collective v6 wins | Primary gate |
| ---: | ---: | ---: | --- |
| 37241145 | 0 / 0 | 0 / 0 | Fail |
| 617558519 | 0 / 0 | 0 / 0 | Fail |
| 991842015 | 0 / 0 | 0 / 0 | Fail |

All per-cell adjusted rates, six paired differences and three-component restart gates are in [analysis.json](../TestResults/balance/tower-collective-provider-20260913/analysis.json). Search reliability and tested-family assessment remain separate; neither yields a new complete Tower-family acceptance claim.

| Saved control | Wins | Observed rate | Joint-adjusted interval |
| --- | ---: | ---: | ---: |
| `team-1a924a7cfff12298633bee909cdea4ad` | 131/256 | 51.17% | 41.33–60.92% |
| `team-1abe76ca1891d97a91d484f0a3662048` | 77/256 | 30.08% | 21.82–39.86% |
| `team-38248d838d1db9634fd82536c177df0a` | 88/256 | 34.38% | 25.65–44.30% |
| `team-3a69c759178064021dc5cf7124d7f4f5` | 80/256 | 31.25% | 22.86–41.08% |
| `team-693ffa8ec0b654154a06722aba06a968` | 65/256 | 25.39% | 17.76–34.92% |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | 81/256 | 31.64% | 23.21–41.49% |

Discovery evaluated 576 complete parties from **595 proposals**, recording **0 victories** across discovery trials. Its **255 fresh constructions** contained **0 parties with any discovery win**. [Search diagnostics](../TestResults/balance/tower-collective-provider-20260913/search-diagnostics.json) retain the per-arm construction and mutation counts, all provider proposal outcomes and every changed-position count.

The new arm evaluated **18 collective substitutions**, all changing multiple characters. Changed-position distribution:

| Positions changed | Evaluated substitutions |
| ---: | ---: |
| 2 | 4 |
| 3 | 1 |
| 4 | 1 |
| 6 | 2 |
| 7 | 2 |
| 8 | 6 |
| 9 | 1 |
| 10 | 1 |

The median was **7.0 positions**. **2** substitutions ranked above their selected parent; median guardian-health improvement was **-5.7350 pp** (positive means less remaining health). **4 of six** collective finalists carried this mutation in their ancestry. These [descriptive findings](../TestResults/balance/tower-collective-provider-20260913/provider-findings.json) come from adaptive parents and a small operator allocation; they do not establish a causal mutation benefit. The collective hypothesis was exercised, but weak fresh construction and the broader refinement process remain unresolved.

## Resource use, retained recipes and next boundary

All **9,224** fights started and completed, with no retries or lost attempts. Measured phases totaled **244.94 seconds**; the execute invocation took **248.61 seconds**. The retained package is approximately **341 MiB**, with exact pre-receipt bytes in [final verification](../TestResults/balance/tower-collective-provider-20260913/final-verification.json). Both complete campaigns reconstructed without combat, and all eight detailed parity checks passed. Independent manifest, journal, schedule, selection, recipe and native-interval checks passed.

Build commands succeeded: backend test-project `dotnet build --configuration Release --no-restore` (five existing unrelated warnings, zero errors), and driver build (zero warnings/errors). Backend tests used `build/run-tests.ps1 -NoBuild -Configuration Release` with the ten relevant Tower test-class filters and passed **163/163**. One zero-combat driver restore failed because sandboxed NuGet attempted to read its user configuration. The same source-free restore succeeded with scoped approved access; [setup attempts](../TestResults/balance/tower-collective-provider-20260913/setup-attempts.json) records this separately. No required command remains blocked.

All 18 complete, seed-free recipes, measurements and source associations are saved in [validated-builds.json](../TestResults/balance/tower-collective-provider-20260913/validated-builds.json), with [named recipes and ancestry](../TestResults/balance/tower-collective-provider-20260913/trace-findings.json). Load these directly without rediscovery; neither retained catalog was promoted. Detailed replay summaries are in [replay diagnostics](../TestResults/balance/tower-collective-provider-20260913/replay-diagnostics.json).

Do not extend this closed experiment or launch another search variant automatically. The observed 51.17% control result makes a separately frozen ceiling confirmation the concrete balance follow-up, with no immediate retuning. Meanwhile, the saved search evidence can support zero-combat diagnosis of weak initial construction and refinement; another search hypothesis needs its own bounded protocol. Any future preparation must exclude every array of this experiment's ledger and register all six controls outside generation. Boss retuning, default/catalog promotion and floors 6–11 remain deferred; floor targets remain five Essences through floor 9, six at floor 10 and at least seven at floor 11.
