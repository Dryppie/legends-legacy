# Elite Build Certification Gate

The concrete v1 thresholds, execution profiles, player-fixture rules, and approval checklist are defined in the companion [Elite Build Certification Policy v1](elite-build-certification-policy-v1.md).

**Implementation status:** Implemented in balance schema 15. Certification analyzer algorithm v21 retains the legacy single-seed objective and adds a disabled-by-default nested common-seed audit with E4/E5/E6 reference cohorts, exact seed panels, elite ranking gates, scenario variance, promotion telemetry, and runtime projection. The seed-`8471` 32-seed experiment found no statistically stable submaximal panel compatible with the 15-minute complete-search target, so no robust search objective or larger-population search is promoted. Certification evidence remains unapproved and the checked-in curated fixture is intentionally empty. The complete investigation history is recorded in the [Elite Build Search Investigation Log](elite-build-search-investigation-log.md).

This gate establishes whether generated Essence combinations are strong enough to represent highly optimized and top-player builds. It must be completed before Region 1 scaling is remediated and before Milestone 14 extends the system to additional progression bands.

The purpose is not to make P99 builds the normal progression target. Generic P75 builds remain the default Region 1 reference. Certified P95/P99 and encounter-specialized parties provide a separate elite stress boundary that prevents content from being accidentally trivialized by advanced players.

## Why the current optimizer is not sufficient proof

The production catalog currently contains 80 Essences across 77 source-monster families. Because a legal build cannot equip two Essence variants from the same source monster, the exact legal combination counts are:

| Slots | Legal combinations | Default unique evaluations | Search-space coverage |
| ----: | -----------------: | -------------------------: | --------------------: |
|    E4 |          1,572,574 |                         80 |           0.00508720% |
|    E5 |         23,812,016 |                         80 |           0.00033596% |
|    E6 |        296,229,474 |                         80 |           0.00002701% |

The current genetic/beam search is useful for discovering strong legal builds, but its default sample is far too small to prove a global or near-global optimum. Current P50/P75/P90 labels describe percentiles within that generated population, not percentiles of real players or the complete legal space.

Additional limitations are:

- encounter-specific optimization ranks the same evaluated generic population rather than discovering a larger boss-specific population;
- generic fitness is the equal-weight average of five PvE benchmarks, so a specialist can be hidden by its aggregate score;
- gear, Essence progression state, and five-player party composition are not jointly optimized;
- the optimizer has no convergence requirement across independent restarts;
- top candidates are not exhaustively challenged by all nearby one- and two-Essence substitutions;
- no real or curated top-player builds are currently included as regression fixtures.

The existing optimizer should therefore be treated as a capable search stage, not an elite-build certificate.

## Player populations must remain separate

Content should be evaluated against distinct populations with different purposes:

| Population                        | Purpose                                                                                  |
| --------------------------------- | ---------------------------------------------------------------------------------------- |
| Generic P75                       | Ordinary progression anchors, normal recommended CR, and standard encounter calibration. |
| Certified P95/P99                 | Top-player difficulty, theoretical power ceiling, and high-end clear-speed pressure.     |
| Encounter-specialized elite       | Hard-counter, mechanic-bypass, immunity, burst, sustain, and cheese detection.           |
| Curated or observed player builds | Evidence that automated search reflects builds real advanced players construct.          |

Elite profiles must not silently replace P75 progression profiles. Every report must show the populations independently and apply an explicit target policy to each one.

World Tower holdouts use deterministic score-centered cohorts around the P95 and P99 targets. The artifact retains both cohort membership lists so reviewers can verify that the labels did not collapse into the same upper-tail population.

## Required certification search

### Independent restarts

Run multiple searches from unrelated deterministic seeds. Each restart must retain its complete top cohort and per-scenario component scores. A single lucky or unlucky search is not enough.

The implementation should support configurable:

```text
Restart count
Population size
Generation count
Elite count
Mutation schedule
Random-injection schedule
Candidate evaluation budget
Convergence tolerance
```

The production default should be chosen only after runtime measurement. A smaller developer mode and a larger release-certification mode may share the same orchestration, report contract, and acceptance rules.

### More than one search strategy

Certification should compare at least two complementary strategies:

- the existing diversity-aware genetic/beam search with independent restarts;
- a deterministic local or beam refinement seeded from top builds, strong Essence usage, and measured pair synergies.

Agreement between different strategies is stronger evidence than repeatedly running one algorithm.

### Local-optimum challenge

Every finalist must be challenged by:

- every legal one-Essence substitution;
- a bounded or complete two-Essence substitution search;
- high-synergy pair insertions from the Essence meta analysis;
- replacement of any repeatedly mandatory Essence;
- scenario-specific reranking rather than aggregate-score reranking alone.

A finalist is not locally certified when an allowed neighbor improves its relevant fitness beyond the configured tolerance.

### Pareto retention

Do not retain only the highest equal-weight aggregate. Preserve a Pareto frontier across:

```text
Short single-target damage
Sustained single-target damage
High incoming damage
Multi-target combat
Attrition and sustain
Real encounter performance
Survival consistency
Clear speed
```

This protects elite specialist builds that are legitimately dominant in one role without pretending they are generic progression builds.

### Joint party optimization

Top players optimize teams, not only isolated characters. The elite search must optimize complete parties against real encounters, including:

- complementary Essence kits;
- role coverage;
- buffs, debuffs, summons, and status interactions;
- duplicated versus diversified builds;
- burst timing and sustain loops;
- party-level survival and clear speed.

The encounter-specific optimizer should eventually search party genomes directly instead of only ranking individual builds and assembling a diverse retained pool.

## Top-player reference builds

Automated search can estimate a theoretical ceiling, but certainty about actual top players requires player evidence.

Until live telemetry exists, support a versioned input such as:

```text
top-player-builds.json
```

Each curated entry should retain:

```text
Build or party ID
Source and review date
Gear package or exact equipment
Character level and progression state
Essence IDs and evolution state
Intended role or encounter
Optional observed result
Content-version fingerprint
```

Only builds deliberately supplied by developers or trusted players should enter the repository fixture. Do not collect personal identifiers. When live telemetry becomes available, use anonymized aggregate snapshots with an explicit retention and privacy policy.

Every curated build must be materialized through the same production construction and combat boundaries as generated builds. A content fingerprint mismatch should fail explicitly rather than silently evaluate stale data.

## Certification evidence

Each E4/E5/E6 elite profile and encounter-specialized party should report:

- legal search-space size;
- unique candidates evaluated;
- independent restart count and seeds;
- best score and component scores per restart;
- raw/refined build IDs and Essence genomes, plus distance from the strongest restart genome;
- best-score spread across restarts;
- generations since the last material improvement;
- optional valley-search depth, candidates, budget exhaustion, and best improvement;
- optional valley-prefilter generated and rejected candidate counts;
- optional bridge endpoint, legal-node, maximin-path, and regression evidence kept outside certification populations;
- one-swap and two-swap challengers evaluated;
- best local-neighbor improvement;
- cross-strategy agreement;
- cross-seed clear-rate confidence interval;
- scenario and encounter weaknesses;
- recurring Essences and pairings;
- mean similarity and distinct elite archetypes;
- comparison with every curated top-player build;
- the final certification verdict and warnings.

## Proposed verdicts

| Verdict                      | Meaning                                                                                             |
| ---------------------------- | --------------------------------------------------------------------------------------------------- |
| `CertifiedElite`             | Search convergence, local challenge, holdout stability, and player-reference requirements all pass. |
| `SearchUnstable`             | Independent searches disagree or continue finding material improvements.                            |
| `LocalImprovementFound`      | A one- or two-Essence challenger materially beats a finalist.                                       |
| `ScenarioCoverageFailure`    | The retained elite cohort omits a required role or benchmark frontier.                              |
| `PartyOptimizationRequired`  | Individual builds look strong, but the party ceiling has not been established.                      |
| `HumanBuildOutperformed`     | A curated player build exceeds the automated ceiling beyond tolerance.                              |
| `InsufficientPlayerEvidence` | Automated evidence is strong, but no acceptable top-player fixtures or telemetry exist.             |

`CertifiedElite` should be impossible when any required evidence is missing. A report can still preserve partial results without presenting them as certified.

## Initial acceptance policy

Exact thresholds must be configuration-owned and approved before implementation. A reasonable starting policy for review is:

| Check                        | Draft requirement                                                                                                        |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| Independent search agreement | Best-score spread no more than 0.50 points.                                                                              |
| Search plateau               | No material best-score improvement during the final configured generations.                                              |
| Local challenge              | No legal neighbor improves relevant fitness by more than 1.00 point.                                                     |
| Holdout confidence           | Elite clear-rate interval width no more than 5 percentage points for release certification.                              |
| Human comparison             | No curated player build exceeds the automated elite ceiling by more than 2.00 points without invalidating certification. |
| Diversity                    | Retain multiple materially distinct elite archetypes unless the report explicitly flags a mandatory-Essence meta.        |
| Legality                     | Every build and party must pass production source-family, slot, content-resolution, and progression rules.               |

These values are frozen v1 runtime tolerances, not hidden game-design decisions. They remain subject to explicit game-design approval together with the intended P95/P99 clear-rate bands.

## Content-balance use

For each encounter, the final balance report compares:

```text
Generic P75 clear rate and confidence interval
Certified P95 clear rate and confidence interval
Certified P99 clear rate and confidence interval
Certified encounter-specialized party clear rate and kill time
Best curated top-player result
Hard-counter or cheese evidence
```

Normal progression remains balanced around its approved generic profile. Elite evidence answers a different question: whether an advanced build or party trivializes mechanics, produces an unintended clear-speed ceiling, or invalidates the content's intended challenge.

No encounter scaling should be accepted merely because generic P75 is on target when uncertified elite or curated player builds can bypass it.

## Automated-flow requirement

The certification gate is part of the same repository-level action:

```powershell
.\build\run-balance.ps1
```

It must not require manual transfer of optimizer outputs between commands. A practical implementation can expose a quick default and a more expensive release-certification profile, but both must execute the same stages and produce compatible artifacts.

Implemented controls include:

```text
--elite-restarts <number>
--elite-population <number>
--elite-generations <number>
--elite-max-generations <number>
--elite-crossover <number>
--elite-basin-jump <number>
--elite-explorer-archive <number>
--elite-stratified-portfolio <number>
--elite-quality-island <number>
--elite-mechanic-island <number>
--elite-descriptor-audit
--elite-benchmark-confidence-audit
--elite-confidence-cohort <number>
--elite-confidence-seeds <number>
--elite-confidence-margin <number>
--elite-valley-beam-width <number>
--elite-valley-beam-depth <number>
--elite-valley-budget <number>
--elite-valley-prefilter <number>
--elite-bridge-audit
--elite-local-swap-depth <1|2>
--elite-restart-refinement <number>
--elite-restart-seeds <number>
--elite-restart-two-swap-limit <number>
--elite-finalist-refinement <number>
--elite-holdout-seeds <number>
--elite-simulations <number>
--top-player-builds <path>
--certification-profile <developer|release>
--elite-search-only
```

## Implemented report contract

The runner writes a standalone artifact under both `latest` and immutable history:

```text
elite-build-certification.json
```

`summary.json` contains the complete certification snapshot, while `summary.md` shows:

- certification verdict per E4/E5/E6 elite profile;
- leading generic and scenario-specialized builds;
- party-level encounter ceiling;
- convergence and local-search evidence;
- comparison with curated top-player builds;
- blocking warnings that prevent scaling approval.

Balance schema version 15, optimizer algorithm version 6, and certification analyzer algorithm version 21 implement this contract and write the resolved policy, execution profile, content fingerprint, evidence, verdicts, warnings, search diagnostics, and optional isolated bridge, E5 descriptor/collision, and nested common-seed benchmark-confidence audit sections into the standalone artifact and combined summary.

## Relationship to the current Region 1 gate

The existing [Region 1 Scaling Validation Gate](../region-1-scaling-validation.md) correctly tests whether calibration generalizes for the current P75 representative library. It does not prove that the library contains elite-quality builds.

The required order is therefore:

1. pass elite-build certification with the implemented gate;
2. freeze certified generic and encounter-specialized elite cohorts for the content version;
3. remediate Region 1 calibration using P75 as the normal target;
4. stress every recommendation against certified P95/P99 and top-player parties;
5. rerun Region 1 scaling validation;
6. begin Milestone 14 only after both gates pass or exceptions are explicitly approved.

## Implementation completion and current evidence boundary

The gate implementation is complete because:

- elite candidates are searched with multiple independent strategies and restarts;
- finalists pass deterministic local-neighborhood challenges;
- complete parties are optimized against real encounters;
- P95/P99 profiles are derived from the certified population rather than the current 80-build sample;
- curated player builds can be loaded, validated, and compared automatically;
- holdout confidence and convergence thresholds produce explicit verdicts;
- reports are reproducible and written to latest and immutable history;
- the entire process runs from the one-button balance command;
- automated tests cover legality, determinism, convergence, local challenge, player comparison, report persistence, and failure verdicts;
- no certification result mutates production content automatically.

The evidence gate itself has not passed. The completed algorithm-v11 bridge run used seed `8471`, search-only mode, population `96`, generations `24`-`40`, and `12` elites, with valley search and crossover disabled. It took `1,247.34` seconds, retained `59,006` certification candidates, and separately evaluated `146` bridge nodes. The E5 bridge exhaustively evaluated 70 legal minimum-substitution nodes. Its best maximin path was `85.27 -> 83.97 -> 83.69 -> 83.87 -> 86.21`, with a `1.30` largest step loss and `4.28` total deficit below the source; neither a non-regressing nor frozen-`0.50`-bounded bridge exists. This establishes a genuine shortest-path fitness valley while leaving the `0.94` spread and `SearchUnstable` verdict unchanged. Longer off-endpoint detours were not evaluated.

Algorithm v12 then tested a `20%` restart-local coordinated-mutation rate under the same `96`/`24`-`40`/`12` search-only configuration, with all other experimental search options disabled. It replaced `3,366` ordinary mutation births with legal three/four-gene jumps and completed in `1,407.29` seconds with `62,296` certification candidates. E4 improved to the known `78.61` ceiling at `0.46` spread, but E5 worsened to `1.09` spread (`86.21`, `85.50`, `85.12`) and E6 widened to `0.37`. The result remained `SearchUnstable`, exceeded the runtime criterion, and was not repeated on seed `1337`.

Algorithm v13 then tested that follow-up with the same `20%` rate and a 12-candidate persistent restart-local explorer archive. The run produced `1,664` new jump seeds plus `1,513` continuation births, took `1,386.22` seconds, and evaluated `59,793` certification candidates. E4 converged at `0.17` but retained only `78.32`. E5 converged at `0.15` only because every restart fell into the lower basin (`85.27`, `85.12`, `85.27`) and lost `86.21`. E6 widened to `0.99` while retaining `87.46` in one restart. The overall result remained `SearchUnstable`; seed `1337` was not run and the archive is not approved.

Algorithm v14 then isolated `256` deterministic stratified candidates per restart/profile behind the unchanged baseline optimizer and refinement beam. The corrected seed-`8471` run benchmarked `2,304` direct portfolio candidates, evaluated `80,560` local candidates across the separate baseline and portfolio beams, retained `109,724` unique certification candidates, and completed in approximately `517.35` seconds. It preserved all known ceilings. E4 passed at `0.29` (`78.61`, `78.61`, `78.32`) and E6 passed at `0.34` (`87.46`, `87.46`, `87.12`), but E5 still failed at `1.09` (`86.21`, `85.12`, `85.27`). Its only portfolio gain was `84.91` to `85.12` in restart 2. The run remained `SearchUnstable`; seed `1337` was not run, the option stays disabled, and no budget is promoted.

Algorithm v15 then tested `256` quality-diversity island candidates per restart/profile after the complete unchanged baseline. The corrected run pre-filled at most 25 strongest/weakest-scenario niches from each restart's own authoritative baseline, evaluated 32 fresh candidates and 224 niche-champion descendants, retained `62,865` unique certification candidates, and completed in approximately `256` seconds. Islands occupied 7–11 niches and made 1–13 champion replacements, but none beat a refined baseline. E4 failed at `0.63` (`77.98`, `78.61`, `78.32`), E5 failed at `1.30` (`86.21`, `84.91`, `85.27`), and E6 retained its passing `0.34` baseline spread. The result remained `SearchUnstable`; seed `1337` was not run, the option stays disabled, and no budget is promoted.

Algorithm v18 then tested `256` restart-local mechanic-archetype island candidates per restart/profile after the complete unchanged baseline. The run benchmarked `2,304` island candidates, retained `63,023` unique certification candidates, and completed in approximately `303` seconds. Islands occupied 133–153 niches and made 28–44 champion replacements, but none beat a refined baseline. E4 failed at `0.63`, E5 failed at `1.30` (`86.21`, `84.91`, `85.27`), and E6 retained its passing `0.34` spread. The high E5 coarse niche was already present in every complete baseline and every island reached it, but same-niche candidates scored from `86.21` down into the 70s. The failure is therefore descriptor collision, not insufficient coarse-niche coverage. Seed `1337` was not run, the option stays disabled, and no budget is promoted.

The bridge audit establishes a real minimum-substitution E5 valley, while the v12-v15 and v18 failures show that diversity and retention alone do not reliably cover the stronger basin. A larger random, portfolio, scenario-island, or mechanic-island budget is not justified. If investigation continues, the next bounded work is an audit-only collision study for a hard-bounded authored-mechanic residual descriptor inside the coarse E5 niche; it must pass prospective separability and fragmentation criteria before another search run is authorized. Curated player evidence and a release-profile run remain separate mandatory blockers.

Algorithm v19 then tested a four-axis capped authored-mechanic intensity residual across the finalized E5 collision population. Of 343 candidates in the coarse high niche, one met the `85.71` high floor and 342 were at or below the `85.27` low ceiling. Three residual signatures produced `99.71%` purity but `0%` high accuracy and only `49.85%` balanced leave-one-out accuracy; the high anchor residual collided with low candidates. The descriptor passed its hard 81-niche ceiling but failed separability and map readiness. Seed `1337` was not run and no setting is promoted.

Further descriptor-island work is not justified by current evidence. If investigation continues, the next bounded work should be an audit-only held-out rankability study comparing quantitative authored effect payloads, additive Essence effects, and measured pair interactions. The restart containing a candidate under evaluation must be excluded from model training, and no model may enter search or certification evidence until it prospectively ranks the strong candidate. Curated player evidence and a release-profile run remain separate mandatory blockers.
