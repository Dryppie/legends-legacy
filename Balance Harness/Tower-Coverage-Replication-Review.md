# Unchanged v4 coverage-policy replication

The separately frozen replication completed **9,224 fights** and returns **Fail: 0/3 passing restarts, with 2 required**. All 12 generated finalists won **0/256** on fresh held-out seeds. Six saved controls, including both successful recipes from the earlier pilot, won **20.31–43.75%**. The original [v4 pilot](Tower-Party-Coverage-Review.md) remains separately **Fail: 1/3**; neither its samples nor its restart gate were pooled with this replication.

The 18-cell validation assessment is **Inconclusive**, under both the ordinary evaluator and the joint-adjusted analysis. The prior v4 secondary won **112/256 (43.75%)**, with a joint-adjusted upper bound of **53.71%**. No discovery or validation party had an observed rate above 50%, but the uncertainty does not support accepting this tested family. The earlier 2,918-party confirmation retains its own family and evidence scope. Near-optimality remains unestablished.

## Frozen allocation and compatibility

Target: the offline `LL/tools/BalanceHarness`, Kharad floor 5 at **Health 3.04881408 / Power 3.85370128**. The budget remains ten level-40, tier-1, rank-2, Standard-quality characters with five level-1 unascended/unevolved Essences each, unchanged fixed gear, no Combat Styles and no prior contributions. Eligible Essences include Rare under hypothetical ownership. Practical acquisition and ownership remain unverified.

The [preflight receipt](../TestResults/balance/tower-coverage-replication-20260912/initial-verification.json) verified all **6,873 sealed pilot files**, **82 current harness source files**, **16 content files**, five current/saved execution assemblies and both retained catalogs. The driver additionally compared current settings/execution identity before freezing and executing. The policy, methods, budgets, inventory, content and gameplay assemblies match the prior pilot. All **66 capability entries** and **48 mechanic-core hypotheses** match exactly.

The [protocol](../TestResults/balance/tower-coverage-replication-20260912/protocol.json) was frozen before any replication battle, after reviewing the [handoff](Tower-Coverage-Replication-Plan.md). The handoff's example was explicitly adopted with six controls and the following bounds:

| Phase | Frozen allocation | Actual fights |
| --- | --- | ---: |
| Discovery | 2 methods × 3 restarts × 96 evaluated complete parties × 8 shared seeds | 4,608 |
| Held-out validation | Top 2 per arm plus all 6 controls; 18 distinct recipes × 256 shared fresh seeds | 4,608 |
| Diagnostics | 4 historical detailed parity replays; first validation seed for each v4 primary and the fixed saved anchor | 8 |
| **Total** | **No retry reserve or automatic resume** | **9,224** |

Each arm permits at most 2,048 proposals; rejected or duplicate proposals consume proposal capacity without running fights. There were **593 proposals: 576 evaluated, 11 duplicates and 6 family violations**. The [seed ledger](../TestResults/balance/tower-coverage-replication-20260912/seed-ledger.json) excludes the union of **every array** in the previous ledger: **468,658 historical/reserved seeds**, including unused selection/confirmation reservations. Three new generation seeds, eight discovery seeds, 256 validation seeds and two new unused reservations are mutually disjoint and outside that union. Combat schedules are shared across methods/restarts; they are not extra independent samples per restart.

`independent-coverage-v4` retains the order `mechanics-joint`, `coverage-joint`. No harness policy code was edited. References, recipes, source identities, ancestry, fitness and held-out outcomes remain outside independent generation. A zero-combat boundary check found identical detached inputs with all six references, no references and reversed references. Production reference-invariance and provenance regression tests also passed. The original four detailed control reports reproduced exactly; the two added controls were loaded by exact saved recipe and measured afresh.

## Preselected comparisons and results

The top two recipes per arm were chosen using unchanged discovery ranking. The complete [18-recipe selection](../TestResults/balance/tower-coverage-replication-20260912/validation-selection.json) was saved before any validation attempt. Rank one is the primary; rank two is exploratory and cannot replace it based on held-out results. Exact recipe deduplication preserves every source association; all 18 were distinct here.

The saved gate anchor remains `team-1abe76ca1891d97a91d484f0a3662048`, preserving the original replication question. Both prior v4 winners are additional measured rate controls; neither becomes an easier replacement anchor after outcomes. A passing v4 primary must satisfy all three original conditions:

1. Its adjusted clear-rate lower bound is at least 10%.
2. Its paired lower difference against the same-restart mechanics primary is above zero.
3. Its paired lower difference against the fixed saved anchor is at least −10 percentage points.

At least two of the three preselected primaries must pass. This is the declared experimental gate, not a confidence bound on success across all possible generation restarts.

| Generation seed | Mechanics primary / secondary wins | Coverage primary / secondary wins | Reliability |
| --- | --- | --- | --- |
| -1988921447 | 0 / 0 | 0 / 0 | Fail: all three checks |
| 134126737 | 0 / 0 | 0 / 0 | Fail: all three checks |
| 345422337 | 0 / 0 | 0 / 0 | Fail: all three checks |

Each entry has 256 held-out trials. Every generated finalist's joint-adjusted interval is **0–3.84%**, which is not proof of a zero true win rate. Each primary's paired interval against its same-restart mechanics primary is **−3.57 to +3.57 pp**. Against the saved anchor, which won 69/256, each interval is **−36.20 to −15.78 pp**. Every primary fails all three gate components.

| Saved recipe | Fresh wins | Fresh rate | Joint-adjusted interval |
| --- | ---: | ---: | --- |
| `team-38248d838d1db9634fd82536c177df0a` | 82/256 | 32.03% | 23.55%–41.89% |
| `team-1a924a7cfff12298633bee909cdea4ad` | 112/256 | 43.75% | 34.27%–53.71% |
| `team-1abe76ca1891d97a91d484f0a3662048` | 69/256 | 26.95% | 19.10%–36.58% |
| `team-3a69c759178064021dc5cf7124d7f4f5` | 76/256 | 29.69% | 21.48%–39.45% |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | 69/256 | 26.95% | 19.10%–36.58% |
| `team-693ffa8ec0b654154a06722aba06a968` | 52/256 | 20.31% | 13.49%–29.42% |

Joint nominal alpha .05 is split .025 across all 18 rate intervals and .025 across the six predeclared paired comparisons. A paired difference subtracts two Wilson discordance intervals with component alpha `.025/(6×2)`. Wilson coverage is approximate. The ordinary evaluator separately uses its unchanged `.05/18` rate allocation and also returns Inconclusive. There is no sample pooling, held-out reranking, optional extension, superiority claim between saved controls or lifetime repeated-study confidence claim.

All six controls support the 10% viability floor, but the prior secondary's upper bound remains unresolved. Dropping it, substituting an easier control or adding samples to this closed run would not resolve the predeclared decision legitimately. The [acceptance policy](Tower-Balance-Acceptance-Policy.md) remains unchanged.

## Diagnostics and implications for search

All 576 discovery parties also won zero of eight. Coverage's best discovery boss-health remainder was lower than mechanics in each restart (52.87% versus 73.46%, 48.12% versus 62.78%, and 60.19% versus 72.48%); those tie-break improvements did not translate into validation victories.

Across 256 reports per primary, median first initial-character death occurred at **51.9, 53.9 and 42.0 seconds** in restart order. Those parties averaged **53.99%, 51.82% and 63.80% guardian health remaining**. All three contain providers in all five capability categories. Coverage counts and broad category presence therefore do not suffice to establish competitive strength in these observed teams. They do not identify a causal ability failure or prove a universal role quota. The [fixed replay diagnostics](../TestResults/balance/tower-coverage-replication-20260912/replay-diagnostics.json) and [named recipes/ancestry](../TestResults/balance/tower-coverage-replication-20260912/trace-findings.json) retain the underlying descriptive evidence; all four new detailed summaries match their compact reports.

The two historical winners remain useful controls: they won **82/256 and 112/256** on the new schedule. Their preserved performance alongside zero new finalists supports keeping independent search reliability open. The next development step should diagnose unsuccessful generated lineages and test a specific improvement to generic construction/exploration or refinement allocation in a separately frozen bounded comparison with v4. Authored targets, recurrence, resource costs and interactions may inform hypotheses; saved recipes, IDs, counts, ancestry and measured fitness must stay outside independent generation. This replication does not isolate which operator should change, and no new policy or follow-up campaign has been started.

Do not rerun seeds until a restart passes, retune Kharad, promote defaults/catalogs or begin the next floor batch from these results. Any precision study of the unresolved control also needs its own frozen fresh schedule and uncertainty allocation. Floors 6–9 remain five-slot targets, floor 10 six and floor 11 at least seven, with separate lower-budget diagnostics.

## Resources, saved evidence and verification

Execution/reconstruction phases totaled **247.07 seconds**; the driver's complete execute invocation measured **250.64 seconds**, within the ten-minute limit. Discovery took 146.08 s, discovery reconstruction 12.09 s, validation 74.16 s and validation reconstruction 3.02 s; the eight replays used about 11.72 s. Preparation, builds, tests, Python analysis and documentation are outside these timings. This is an observation under this workload, not a controlled performance comparison.

About **344 MiB** is retained, within 1 GiB overall and 512 MiB per campaign. The global durable attempt journal and campaign journals account for all 9,224 started/completed fights, with **zero retries, lost/uncommitted attempts, interruptions or budget extensions**. Test simulations are separate from experiment accounting.

Both campaigns reconstructed without additional combat. Independent Python analysis checked manifests, hashes, journals, schedules, recipes, legality, ancestry, discovery ranking, frozen finalist selection, all six controls, outcomes and adjusted intervals. The [final verification receipt](../TestResults/balance/tower-coverage-replication-20260912/final-verification.json) checks these results, current source/content/execution compatibility, unchanged catalogs and sealed historical evidence/reviews. The local driver built with zero warnings/errors and **144 relevant backend tests passed** through `build/run-tests.ps1 -NoBuild -Configuration Release`, covering Coverage, MechanicCore, BossGeneration, BossDiscoveryContract/Run, BalanceEvaluator, Compact and Bulk tests. Existing matching harness/test assemblies were reused; no full gameplay rebuild was claimed. No required command was blocked; Python was absent from PATH, so the bundled runtime was used.

The [saved builds](../TestResults/balance/tower-coverage-replication-20260912/validated-builds.json) contain all **18 seed-free finalist/control recipes**, fresh measurements, source associations and content/execution provenance. Every evaluated party and rejected proposal remains in discovery evidence. Reading recipes needs no rediscovery or battle replay. This is a local ignored evidence package, not a catalog promotion or fresh-checkout distribution; preserve the package with its linked evidence. The [analysis](../TestResults/balance/tower-coverage-replication-20260912/analysis.json), [seed ledger](../TestResults/balance/tower-coverage-replication-20260912/seed-ledger.json), producing driver/assemblies and compact outcomes remain available for reuse and auditing.

Changed files comprise this review, the active replication handoff, six active planning/policy documents, the harness README and the new local replication package. No gameplay code, boss content, fixed gear, dependency, migration, configuration, shared database or deployment changed. Both retained catalogs and default policies remain unchanged.
