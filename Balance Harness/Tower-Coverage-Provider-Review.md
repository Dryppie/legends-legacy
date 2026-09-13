# Coverage-provider mutation: implementation and bounded pilot

The new opt-in **`independent-provider-v5`** policy completed its frozen comparison with unchanged v4 and **failed reliability: 0/3 passing restarts, with 2 required**. All 12 generated finalists won **0/256** on held-out seeds. Six saved controls won **21.88–45.31%**. The 18-cell balance assessment is **Inconclusive**: the strongest control measured **116/256 (45.31%)**, but its joint-adjusted upper bound is **55.25%**. No discovery or validation result observed more than 50% wins.

This completes one targeted search change and one bounded comparison. V1 remains the default. V4's original **1/3** pilot and separate **0/3** replication remain sealed, separate results. No samples or restart gates were pooled, and no stronger secondary replaced a preselected primary.

## Diagnosis and the tested change

The [zero-combat diagnosis](../TestResults/balance/tower-coverage-provider-20260912/diagnosis.json) inspected six prior v4 coverage arms from the [pilot](Tower-Party-Coverage-Review.md) and [replication](Tower-Coverage-Replication-Review.md). Their **255 fresh parties** all had zero discovery wins. Only three later refinements won in one arm; its first winning candidate appeared at evaluation **85**, at ancestry depth **8**. Weak starting parties and the difficulty of refining combinations both remain plausible limitations; this is not a clean causal separation.

The old coverage-count mutation changed a median of **six ordered positions**, displacing **four distinct Essences**, and improved on its parent in **5 of 35** recorded evaluations. Its median change in guardian health remaining was a deterioration of **6.52 percentage points**. These are descriptive results under adaptive parent selection, not a randomized operator comparison.

The targeted hypothesis was to change a coverage provider while retaining its current number and placement. [TowerCoverageProvider.cs](../LL/tools/BalanceHarness/TowerCoverageProvider.cs) now implements `coverage-provider`:

- Read the unchanged, content-derived coverage categories; enumerate legal source/replacement pairs within each category using only the current generated parent.
- Give each category with legal pairs equal selection opportunity, then choose a legal pair uniformly. No historical result, Essence ID, saved copy count or recipe determines its probability.
- Replace **every occurrence** of the selected source with the replacement in the same ordered positions. Preserve every unrelated position and character budget. Reject all pairs that would violate family uniqueness or owned-copy limits; do not repair conflicts by disturbing other positions.
- Record the category, source, replacement and number of changed positions. A missing legal pair becomes a recorded rejected proposal.

`independent-provider-v5` requires exactly `coverage-joint` followed by `provider-joint`. The first arm preserves v4. The second replaces **only the scheduled coverage-count operator** with coverage-provider; fresh construction, every-fourth refinement restart, ranking, beam/exploration selection, other mutations and the one-in-eight uniform construction route remain unchanged. Categories are broad structural hypotheses, not equivalent timing, magnitude or combat value. Parent recipes are copied; references and their ancestry/fitness remain outside fresh generation.

Both methods use their own stable generation streams and paired combat schedules. The comparison tests the resulting search policies; it does not isolate individual mutation effects on identical initial parties. No existing policy or archive semantics were changed, and no new CLI command or dashboard default was introduced.

## Verification and frozen protocol

Target: the offline BalanceHarness, Kharad floor 5 at **Health 3.04881408 / Power 3.85370128**. The budget is unchanged: ten level-40, tier-1, rank-2, Standard-quality characters; five level-1 unascended/unevolved Essences each; fixed gear; no Combat Styles or prior contributions. The full eligible pool includes Rare Essences under hypothetical ownership. Practical ownership and acquisition remain unverified.

Preflight checked the sealed replication package/review, current content and source compatibility, all six control recipes and both catalogs. Rebuilding changed gameplay assembly hashes because their source-revision/build metadata changed. The [byte-level compatibility receipt](../TestResults/balance/tower-coverage-provider-20260912/build-compatibility.json) verifies all other bytes are identical across the four gameplay assemblies; the new harness identity is frozen separately. This is not a claim that the full DLL hashes stayed unchanged.

All six prior v4 generation arms were [reconstructed exactly without combat](../TestResults/balance/tower-coverage-provider-20260912/v4-comparator-reconstruction.json) from their original discovery measurements. This separate compatibility replay never supplies historical measurements to the new search. Removing or reversing all six references also produced identical fresh generation inputs. The 66 coverage features and 48 mechanic-core hypotheses remain unchanged.

The [protocol](../TestResults/balance/tower-coverage-provider-20260912/protocol.json) and [experiment design](../TestResults/balance/tower-coverage-provider-20260912/experiment-design.json) froze before any pilot combat:

| Phase | Allocation | Fights |
| --- | --- | ---: |
| Discovery | 2 methods × 3 new restarts × 96 evaluated complete parties × 8 shared seeds | 4,608 |
| Validation | Top 2 per arm plus all 6 controls; 18 distinct recipes × 256 fresh seeds | 4,608 |
| Diagnostics | 4 historical detailed replays; first validation seed for each v5 primary and the fixed saved anchor | 8 |
| **Total** | **Zero combat retry reserve; no automatic resume** | **9,224** |

Each arm has at most 2,048 proposals. Actual proposals totaled **596**: 576 evaluated, nine duplicates, ten family violations and one bounded placement-sampling rejection. The [new seed ledger](../TestResults/balance/tower-coverage-provider-20260912/seed-ledger.json) excludes **468,927** seeds: every array in the preceding ledger, including unused reservations. New generation, discovery, validation and unused selection/confirmation reservations are mutually disjoint. Methods/restarts share combat schedules; their repetitions do not enlarge the independent validation sample per recipe.

The complete [selected family](../TestResults/balance/tower-coverage-provider-20260912/validation-selection.json) was saved before validation. Each primary is rank one under discovery ranking; rank two remains exploratory. The original gate anchor is still `team-1abe76ca1891d97a91d484f0a3662048`. A passing v5 primary needs an adjusted clear-rate lower bound of at least 10%, a positive paired lower difference against its same-restart v4 primary, and a paired lower difference of at least −10 pp against that anchor. At least two of three primaries must pass. The other five controls are measured rate controls, never replacement gate anchors.

Joint nominal alpha .05 is split .025 across all 18 rate intervals and .025 across the six predeclared paired comparisons. Each paired difference subtracts two Wilson discordance intervals using component alpha `.025/(6×2)`. Coverage is approximate, within this fixed experiment, with no lifetime repeated-study claim, historical pooling, post-validation reranking or optional extension. The ordinary evaluator separately retains its unchanged `.05/18` allocation. Search reliability and the tested-family assessment answer different questions.

## Results and limits

| Generation seed | V4 primary / secondary wins | V5 primary / secondary wins | Reliability |
| --- | --- | --- | --- |
| -1165413435 | 0 / 0 | 0 / 0 | Fail |
| 1363816588 | 0 / 0 | 0 / 0 | Fail |
| -1027756932 | 0 / 0 | 0 / 0 | Fail |

Every entry has 256 held-out trials. All generated intervals are **0–3.84%** under the joint adjustment, not proof of a zero true win rate. Each v5 primary's paired interval against v4 is **−3.57 to +3.57 pp**; against the saved anchor it is **−39.07 to −18.18 pp**. All three primaries fail every gate component.

| Saved recipe | Fresh wins | Fresh rate | Joint-adjusted interval |
| --- | ---: | ---: | --- |
| `team-1a924a7cfff12298633bee909cdea4ad` | 116/256 | 45.31% | 35.74–55.25% |
| `team-1abe76ca1891d97a91d484f0a3662048` | 76/256 | 29.69% | 21.48–39.45% |
| `team-38248d838d1db9634fd82536c177df0a` | 87/256 | 33.98% | 25.30–43.90% |
| `team-3a69c759178064021dc5cf7124d7f4f5` | 62/256 | 24.22% | 16.76–33.66% |
| `team-693ffa8ec0b654154a06722aba06a968` | 56/256 | 21.88% | 14.78–31.13% |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | 68/256 | 26.56% | 18.76–36.16% |

All six controls support the 10% viability floor. The prior v4 secondary's upper bound remains above 50%, so both the ordinary and joint-adjusted family assessments are **Inconclusive**. Do not drop that control or extend this closed sample into a pass. The earlier 2,918-party confirmation retains its original scope; there is no new complete-family acceptance or near-optimality claim.

The [provider findings](../TestResults/balance/tower-coverage-provider-20260912/provider-findings.json) independently verify **18 new substitutions**. Ten changed one position; the other eight changed two to nine positions. Thus the median was **one position**, despite replacing all copies of the selected source. Eight mutations improved their discovery ranking relative to the parent, but none won. Five of the six v5 finalists have a provider mutation in their ancestry. These adaptive observations do not establish operator superiority.

Uniform choice among legal provider pairs often chose singly represented sources, so this pilot provides limited coverage of substitutions spanning several characters. The v5 result fails the declared gate; it does not establish that such substitutions can never help. No copy-count preference was added after seeing this outcome. Further work should address weak construction/refinement with a new explicit hypothesis; if collective substitution is tested again, predeclare how it will actually sample repeated providers using current generated-party structure. Preserve uniform reachability and the independent reference boundary. No additional policy or campaign has been started.

## Resources, files and completion checks

Measured execution/reconstruction phases totaled **241.11 seconds**; the execute invocation took **244.69 seconds**. Discovery took 142.67 s, reconstruction 12.01 s, validation 71.98 s, validation reconstruction 2.97 s, and eight diagnostic replays 11.47 s. Preparation, builds, tests, analysis and documentation are outside these timings. This is not a controlled speed comparison.

About **339 MiB** is retained, within the **600-second / 1-GiB** overall limits and 512-MiB per-campaign cap. All 9,224 fights have durable started/completed accounting, with no combat retries, lost attempts, interruptions or budget extension. Two zero-combat NuGet setup commands were blocked from reading user configuration; an approved local restore resolved this before freezing. The driver then built with zero warnings/errors. These setup failures are [recorded separately](../TestResults/balance/tower-coverage-provider-20260912/setup-attempts.json), not concealed as combat retries.

The relevant test project rebuilt successfully with 33 existing unrelated warnings and zero errors. **153 backend tests passed**, run through `build/run-tests.ps1 -NoBuild -Configuration Release`, covering Provider, Coverage, MechanicCore, BossGeneration, BossDiscoveryContract/Run, BalanceEvaluator, Compact and Bulk tests. New checks cover all-copy/position invariants across floor budgets, determinism, family/ownership rejection, empty-feature fallback, restricted ancestry, exact v4 comparator preservation, production reference invariance, reconstruction and compact resume. No required command remains blocked.

Both new campaigns reconstructed without extra combat. All four historical detailed reports match exactly; all four new detailed replay summaries match their compact reports. Independent Python analysis checks manifests, journals, seeds, legality, all six controls, provider substitutions, frozen ranking/selection, raw outcomes and confidence calculations. [Final verification](../TestResults/balance/tower-coverage-provider-20260912/final-verification.json) checks current source/content/build identities, unchanged catalogs and sealed historical evidence/reviews.

Changed harness files: `TowerCoverageProvider.cs` adds the mutation; `TowerBossGeneration.cs`, `TowerBossPartyGenerator.cs`, `TowerPartyCoverage.cs` and `TowerBossDiscoveryContract.cs` integrate the opt-in policy and provenance. `BalanceHarnessTowerProviderTests.cs` adds focused tests; the existing discovery-run and bulk tests now also exercise v5. This review, active plans/policy/handoff and the harness README record the result. The existing working-tree work and sealed reviews were preserved.

All **18 seed-free selected/control recipes**, measurements and provenance are [saved for direct reuse](../TestResults/balance/tower-coverage-provider-20260912/validated-builds.json); every evaluated recipe and rejected proposal remains in discovery evidence. Named v5 recipes and ancestry are in [trace findings](../TestResults/balance/tower-coverage-provider-20260912/trace-findings.json). Reading builds needs no rediscovery or replay. This ignored local package was not promoted into either catalog or distributed to fresh checkouts.

No boss, gear, gameplay source, dependency, migration, configuration, shared database or deployment changed. Defaults and catalogs stay unchanged. Floors 6–9 remain five-slot targets, floor 10 six and floor 11 at least seven; their new progression studies and separate lower-budget diagnostics remain deferred.
