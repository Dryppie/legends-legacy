# Elite Build Certification Gate

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
- best-score spread across restarts;
- generations since the last material improvement;
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

These values are draft engineering tolerances, not hidden game-design decisions. The intended P95/P99 clear-rate band for each content type must be chosen explicitly by design.

## Content-balance use

For each encounter, the final balance report should compare:

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

The certification gate must become part of the same repository-level action:

```powershell
.\build\run-balance.ps1
```

It must not require manual transfer of optimizer outputs between commands. A practical implementation can expose a quick default and a more expensive release-certification profile, but both must execute the same stages and produce compatible artifacts.

Suggested future controls include:

```text
--elite-restarts <number>
--elite-population <number>
--elite-generations <number>
--elite-local-swap-depth <1|2>
--elite-holdout-seeds <number>
--elite-simulations <number>
--top-player-builds <path>
--certification-profile <developer|release>
```

## Proposed report contract

The implementation should add a standalone artifact under both `latest` and immutable history:

```text
elite-build-certification.json
```

`summary.json` should contain the complete certification snapshot, while `summary.md` should show:

- certification verdict per E4/E5/E6 elite profile;
- leading generic and scenario-specialized builds;
- party-level encounter ceiling;
- convergence and local-search evidence;
- comparison with curated top-player builds;
- blocking warnings that prevent scaling approval.

The schema version should be incremented when this contract is implemented. Creating this design document does not itself change the current schema-14 runtime.

## Relationship to the current Region 1 gate

The existing [Region 1 Scaling Validation Gate](region-1-scaling-validation.md) correctly tests whether calibration generalizes for the current P75 representative library. It does not prove that the library contains elite-quality builds.

The required order is therefore:

1. implement and pass elite-build certification;
2. freeze certified generic and encounter-specialized elite cohorts for the content version;
3. remediate Region 1 calibration using P75 as the normal target;
4. stress every recommendation against certified P95/P99 and top-player parties;
5. rerun Region 1 scaling validation;
6. begin Milestone 14 only after both gates pass or exceptions are explicitly approved.

## Completion criteria

This gate is complete when:

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
