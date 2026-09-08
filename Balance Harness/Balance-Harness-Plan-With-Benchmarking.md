# Balance Harness Plan

Status: offline idle execution, the original 12-cell controls, a separate 36-cell [First Hunt cohort](First-Hunt-Cohort.md), explicit baseline acceptance, paired comparisons, versioned goals and advisory CI configuration are implemented in [BalanceHarness](../LL/tools/BalanceHarness/README.md). The new cohort covers all three authored starter Essences, quest-reward equipment counts and fixed two-enemy later encounters. All goals remain draft: training/Forge states and exact reward outcomes still need gameplay review. Gameplay-target approval, first hosted CI validation, broader content coverage, analysis and tuning remain open.

Target: offline balance tooling for the primary game service in `LL/`. This work does not require an API endpoint, Angular UI, chat-service changes, infrastructure changes, or a deployed service.

The harness should answer three questions:

1. Can representative players clear the content intended for their progression stage?
2. Which builds, essences, and party compositions perform unusually well or poorly, and why?
3. Did a code or content change move those results outside the intended balance goals?

The first useful deliverable is a reproducible report for a small set of real encounters. Build search, a universal essence score, and automatic tuning should follow only after that report is trustworthy.

The implementation covers individual encounters with one to three fixed enemies, three progression stages (levels 1, 5 and 10), paired encounter seeds, saved content/inputs, battle replay, scorecards and parity tests against the normal idle-session path. The original controls have 12 cells/60 draft checks; the First Hunt cohort has 36 cells/180 draft checks. Complete suites can be explicitly accepted as comparison baselines and evaluated against their matching policies. See the tool README for exact commands and limits; the sections below distinguish implemented idle support from the wider design.

## 1. Existing Building Blocks and Gaps

The repository already contains useful foundations. Extend their preparation and execution paths instead of building a second combat implementation.

| Existing component | Reuse | Gap to address |
| --- | --- | --- |
| `AbilityBalanceSimulator` | Seeded team discovery, saved-candidate round robins, side swapping, parallel batches, matchup and essence summaries | It constructs runtime combatants directly. It is not yet a general progression or PvE benchmark harness. Its request has no per-participant essence level or ascension settings. |
| `CanonicalEquipmentBuildFactory` | Deterministic equipment references, real essence loadouts, progression rungs, role profiles, and no-essence controls | Add explicit benchmark fixtures and acquisition assumptions; a canonical reference is not automatically a typical player's build. |
| `CanonicalCooperativeRosterCatalog` | Stable roles and five-character cells: Guardian, Restorer, two Strikers, Controller | Include viable alternative compositions; these roles are benchmark conventions, not player classes. |
| `CombatPreparationPipeline` | Live/snapshot preparation, equipment and essence setup, content-specific activity, stable participant identity | Offline idle composition is implemented without an API or database; each additional content adapter still needs preparation parity. |
| `CombatEngineExecutor.ExecuteSimulationAsync` | Real ability compilation, progression modifiers, equipment behavior, and isolated execution | Supply the actual content rules explicitly and reproduce any content-specific outcome interpretation. |
| `WorldTowerCombatRuntimeFactory` | Shared Tower preparation, guardian scaling, stagger, party identity, and scouting modifiers | Benchmark party size from each floor's `RequiredSlots`, including multiple five-character cells. |
| `CombatResult` and `CompactCombatTelemetry` | Engine/content outcomes, ticks, participant statistics, summon/add windows, health-pressure samples | Inventory available measurements before promising per-essence damage, effective healing, or control attribution. |
| Existing correctness and parity tests | Combat preparation, canonical snapshot rehydration, telemetry behavior, outcomes | Harness execution, suite, comparison and goal tests now exist; extend parity coverage with each new content adapter. |

A concrete parity risk exists today: `AbilityBalanceSimulator` does not override `FastCombatEngineOptions.StartActiveAbilitiesOnCooldown`, whose default is `false`. Normal executor and Tower playback paths set it to `true`. The same underlying engine can therefore produce different results because its setup differs. Resolve and test this distinction before presenting simulator results as gameplay balance.

The executor also has a mutable compiled-essence cache. Start with a separate executor/dependency scope per worker; do not assume an existing service instance is safe to share across parallel battles.

## 2. Balance Goals and Metric Classification

Each scenario needs a written player-experience goal, an expected progression cohort, and measurable acceptance rules.

| Content | Proposed primary goal | Useful diagnostics |
| --- | --- | --- |
| Normal idle enemies | Reliable victories and, where relevant, an acceptable kill-time band | Health remaining, damage intake, ability usage |
| Elites | Intended victory rate at the expected progression point | Death timing, sustain, duration distribution |
| Dungeon encounter | Encounter clear rate under the specified starting state | Attrition, resources, wave performance |
| Complete dungeon run | Run completion rate with actual carryover/recovery rules | Failure encounter, remaining health/resources |
| Bosses | Clear rate and explicitly required mechanic checks | Time to kill, first death, add clearance, stagger |
| World Tower | Floor clear rate by progression, party composition, and scouting state | Role survival, mechanic pressure, difficulty curve |
| PvP | Matchup score distribution and bounded draw/side advantage rates | Duration, counters, composition coverage |

Rules:

- A primary metric or explicit guardrail can fail a benchmark. A diagnostic cannot silently become a failure condition.
- Duration is primary only when pacing, an enrage, or another content goal makes it relevant.
- Goals apply to named cohorts. An optimized build and an ordinary progression build need not have the same target.
- Do not require every essence or build to be equally good everywhere. Reward meaningful strengths and counters.
- Record both absolute target compliance and movement from a baseline. A stable result can still be badly balanced.
- Treat initial numerical targets as **draft hypotheses** until reviewed against intended gameplay. Observed results alone do not establish the desired target.
- Track intended exceptions explicitly, such as a boss difficulty spike or an intentionally weak negative control.

## 3. Scenario Definitions

A scenario is a versioned contract describing one combat situation, not just a pair of stat blocks.

Required fields:

| Field | Meaning |
| --- | --- |
| Identity | Stable ID, revision, description, tags, and content adapter |
| Fixture references | Character/party and encounter IDs with pinned revisions |
| Progression context | Level, accessible gear/essences, unlock state, and content position |
| Starting state | Health, resources, cooldown policy, buffs, party order, scouting/modifiers |
| Rules | Production ruleset reference, resolved tick limit, recovery, waves, overtime/revival where applicable |
| Sampling | Explicit seed schedule, orientation policy, repetition count |
| Outcome policy | Definition of victory/clear, draw, timeout, and mechanic failure |
| Goals | Metric, unit, bound, direction, confidence policy, and enforcement status |
| Diagnostics | Requested measurements and telemetry level |

Two scenario classes should stay visibly separate:

- **Gameplay scenarios:** legal, progression-appropriate characters against real content; eligible for gameplay balance checks after parity validation.
- **Mechanic probes:** controlled targets or intentionally isolated abilities; useful for explanation and engine verification, but not evidence of general player viability.

Coverage tags can include single target, adds, burst, sustained damage, armor, resistance, sustain, control immunity, stagger, and attrition. These are selectors over scenarios; they do not require separate simulator implementations.

An isolated dungeon fight must not be reported as a complete dungeon clear. Likewise, a single Tower floor does not model a multi-floor progression journey.

## 4. Benchmark Suite and Runtime Benchmarks

Use three balance suites with different purposes:

| Suite | Contents | Expected use |
| --- | --- | --- |
| Smoke | Small, fixed, representative cases and deterministic invariants | Local iteration and pull requests |
| Reference | More seeds, progression boundaries, alternative builds and parties | Intentional balance review and baseline comparisons |
| Exploration | Generated candidates and broad matchup coverage | Finding combinations and scenarios worth investigating |

The implemented [idle reference fixture](../LL/tools/BalanceHarness/Fixtures/idle-reference.json) contains **12 fixed idle cells**: levels 1, 5 and 10 × two legal builds × two real encounters. It uses 100 distinct seeds per cell, or 1,200 battles total. Alternative builds share encounter seeds for paired comparisons. Lumo Ruins, Blood Grove and Crystal Creek cover initial weapon ownership, the next area entry and the second essence-slot unlock. These are fixed-spawn measurements, not area-wide spawn-distribution estimates.

This is an initial sampling budget, not a claim that 100 seeds can prove narrow win-rate differences. The suite reports per-cell 95% Wilson clear-rate intervals, separate win/non-win duration distributions, remaining health and completeness. It remains advisory until the written ownership hypotheses, targets and sampling are suitable. A `--samples 3` override provides a 36-battle smoke run with the same fixtures.

The separate [First Hunt fixture](../LL/tools/BalanceHarness/Fixtures/idle-first-hunt.json) has 36 cells and 3,600 battles by default, or 108 battles at three samples per cell. It crosses the three actual First Hunt Essence choices with mace/wand, uses quest-reward equipment counts and fixes duplicate/mixed two-enemy encounters at levels 5 and 10. Random box outcomes are explicit conditions, and Essences remain untrained. It improves cohort coverage without silently replacing the original controls or claiming an area-wide spawn estimate.

Performance benchmarking is a separate track:

- Record load/validation, preparation, combat execution, aggregation, and reporting time separately.
- Distinguish cold startup from warmed repeated execution.
- Record battles/second, simulated ticks/second, allocation/peak-memory measurements where supported, worker count, telemetry mode, build configuration, runtime, and machine identity.
- Compare timings on a controlled machine/configuration. Do not fail a gameplay balance assertion because a developer laptop was busy.
- First measure realistic budgets; provisional goals are under one minute for smoke and under ten minutes for a local reference run.
- Keep the existing database-backed `build/measure-idle-combat.ps1` benchmark separate. It measures a broader idle-processing path and has external local dependencies.

## 5. Character Profiles and Progression

A profile must describe a build that can exist at the stated progression point.

Pin:

- Character level and base attributes.
- Equipment identities, occupied slots, tier, rarity, quality, upgrades, and relevant behavior/set effects.
- Essence identities, equipped slots/order, levels, ascension, and other combat-affecting progression.
- Relevant passive unlocks, persistent bonuses, temporary effects, and their explicit inclusion or exclusion.
- Acquisition assumptions: what this player could reasonably own at this stage.
- Expected intent, such as balanced solo, sustain, damage, guardian, or control.

Use three cohorts:

1. **Entry:** newly eligible, incomplete but legal equipment/loadout.
2. **Reference:** an attainable build chosen to represent the intended content audience.
3. **Optimized:** a strong legal build within the same acquisition/resource budget.

An intentionally poor build is a separate negative control. It should not lower the expected standard for entry players.

Use the canonical factories as fixture-building inputs, then store the resolved builds. A fixture ID must not silently acquire new equipment or essences when a factory changes. Report both the source recipe and the materialized result.

Power rating is useful metadata and a coarse comparison aid. Do not use it as the sole definition of equivalent strength or as proof that an encounter is correctly tuned.

## 6. Build Generator

Build generation should follow fixed fixtures, not precede them.

Implement in increasing order of complexity:

1. Hand-authored fixtures.
2. One-slot legal substitutions around those fixtures.
3. Seeded random sampling from the progression-eligible catalog.
4. Archetype-constrained sampling.
5. A bounded optimization/search method only if earlier methods miss useful combinations.

Reuse gameplay validation for slot limits, essence-family restrictions, equipment hand rules, unlocks, and progression limits. Reject invalid fixtures with an actionable error; do not silently truncate a loadout or substitute fallback content.

Specify the sampling distribution. Uniform sampling of essences is not uniform sampling of archetypes or acquisition cost. Save generator version, constraints, seed, and every materialized candidate.

Define duplicate identity carefully: preserve slot/party ordering where it affects gameplay. Only deduplicate permutations proven to be equivalent.

Evaluate candidate builds on multiple encounter types. Reserve separate encounter/seed sets for evaluating search winners so that optimization does not merely memorize the discovery suite.

## 7. Party Creator

Use five-character cells as the starting composition unit, with total participants determined by the encounter.

For Tower:

- Read `RequiredSlots` from the floor definition.
- Preserve `PartyNumber` and stable slot order.
- Pin scouting bonuses and distinguish first-clear conditions from boosted repeat attempts.
- Test guardian scaling, stagger participant counts, and party-local targeting with the real runtime factory.

Recommended reference compositions:

| Composition | Purpose |
| --- | --- |
| Canonical balanced | Stable comparison point |
| Attainable mixed | A plausible group without perfect specialization |
| Damage-heavy | Finds burst strategies and missing sustain requirements |
| Sustain-heavy | Finds indefinite survival and timeout/draw behavior |
| Control-light | Tests how strongly success depends on control/stagger |
| Duplicate-heavy | Tests stacking and mandatory combinations where duplicates are legal |

Changing composition should initially preserve progression and acquisition budgets. Study gear improvements separately from role changes so their effects remain interpretable.

Keep multiple viable party templates; do not define balance as one required Guardian/Restorer composition.

## 8. Enemy and Encounter Profiles

Gameplay encounters should resolve real content IDs through the existing catalog/scaling logic.

The resolved snapshot must include:

- Enemy identity, level/scaling inputs, stats, native abilities, resistances, immunities, and target behavior.
- Adds, summons, waves, reinforcements, and recovery rules.
- Stagger, revival, fury, enrage/overtime, or other applicable mechanics.
- Content-specific outcome rules and exact simulation time limit.
- Party size, lane/party grouping, modifiers, and relevant starting state.

Select encounters at ordinary progression points and transitions: new gear tiers, new mechanics, bosses, and party-size changes. These boundaries are more informative than uniformly testing every possible level.

Use artificial high-armor, high-damage, and healing targets only as labeled mechanic probes.

Add content adapters one at a time. Tower already has a shared runtime factory. For content whose preparation or rules still live in a larger service, extract the smallest shared pure preparation/rules seam when implementing that adapter; do not copy formulas into the harness.

## 9. Combat Simulator and Gameplay Parity

Execution flow:

`Resolved fixtures → production preparation → content runtime/rules → CombatEngineExecutor → raw result → content outcome → metrics`

Use `ExecuteSimulationAsync` where it matches the adapter's needs. It accepts a ruleset and returns combat results without the normal entity-state synchronization. Multi-encounter simulations must explicitly reproduce gameplay state carryover using shared logic.

Important checks:

- Match cooldown initialization and basic-attack timing.
- Preserve equipment behavior and granted abilities, essence evolution/ascension, content-specific essence activity, and threat/tanking settings.
- Distinguish `EngineOutcome` from `ContentOutcome`; the harness must use the gameplay clear/win rule.
- Resolve all time-limit defaults explicitly. Current `CombatRuleset` and normal executor paths have different default/explicit limits.
- A gameplay draw or tick-limit result is a completed simulation. An exception or harness wall-clock cutoff is an invalid/incomplete run, not a combat loss.
- Each battle gets fresh mutable combat state. Never reuse entities mutated by a previous run.
- Detailed logging must not change outcomes or consume gameplay RNG.

Parity validation should exercise **both setup and execution**. Comparing two calls against the same already-prepared runtime cannot catch a broken profile or encounter adapter.

Use existing preparation and snapshot tests as foundations. For each gameplay adapter, compare independently prepared equivalent inputs through the normal content path and the harness path, then compare rules, participants, outcomes, duration, terminal state, and appropriate deterministic telemetry.

## 10. Simulation Matrix and Orchestration

The orchestrator owns workflow; it should not own damage, targeting, or scaling formulas.

1. Load suite and validate references.
2. Resolve and freeze content, builds, encounter rules, and seeds.
3. Expand selectors into concrete cells.
4. Print cell/battle counts and estimated cost before execution.
5. Execute with bounded workers, cancellation, and fresh state.
6. Persist per-battle summaries as work completes.
7. Aggregate, evaluate goals, compare a compatible baseline, and write the report.
8. Rerun selected cases with detailed telemetry when explanation is needed.

Avoid the full Cartesian product of every build, essence, party, encounter, and tier. Use fixed references, targeted substitutions, stratified exploration, and then deeper sampling of suspicious cells.

Record declared omissions. A cancelled or budget-exhausted run must be visibly incomplete and cannot establish a passing baseline.

A future resume capability should reuse only completed battle keys from the exact same experiment identity. Resuming under different content or rules starts a new experiment.

## 11. Determinism and Content Snapshots

A seed alone is insufficient.

Capture:

- Harness/report/schema versions and source revision.
- Whether the checkout was dirty, plus an immutable source snapshot reference or retained relevant patch sufficient to reproduce local code changes.
- Exact resolved content and hashes: combat abilities/statuses/summons, essences, equipment/progression, creatures, and the encounter catalogs/settings used.
- Profile/suite revisions, prepared-input hashes, resolved rules, engine options, and tick rate.
- Runtime/SDK, platform/architecture, build configuration, seed-algorithm version, and worker count.
- Fixed logical start time and deterministic participant/encounter identities.

Use a documented stable hash to derive seeds from master seed, scenario/cell identity, trial index, and random-stream purpose. Do not use process-randomized `GetHashCode()`, task order, or worker number.

Keep seed-schedule identity separate from content/code hashes: changing a damage coefficient should rerun the same seeds for a paired comparison. Use separate seed streams for build selection, encounter generation, and combat.

For PvP, run both orientations for each matchup seed and retain the pair identifier. Side swaps are paired observations, not twice as many independent samples.

Share immutable snapshots/catalogs only after confirming they are immutable in practice. Load no changing content during a run. Validate that one-worker and multiple-worker runs produce the same ordered battle results.

Default to bundled file-backed content and synthetic legal players. Avoid real account snapshots. If hydration currently requires persistence, factor reusable hydration from data access or use a disposable local fixture store while proving parity; never replace preparation with approximate stat calculations.

## 12. Metrics Collector and Outcome Semantics

Store one compact record per battle, not just the best combinations or a capped list of examples.

Minimum record:

- Run, cell, fixture, seed, and orientation identities.
- Completion status, engine outcome, content outcome, and termination reason.
- Duration in ticks and seconds using the recorded engine tick rate.
- Available participant damage/healing statistics and terminal health/alive state.
- Available mechanic measurements and telemetry capability/version.
- Reference to a replay bundle or failure details when retained.

Define metrics before implementing charts:

| Metric | Definition and caveat |
| --- | --- |
| PvE clear rate | Cleared attempts / completed valid attempts; report failures, draws, and tick-limit endings separately. Invalid simulations block acceptance rather than disappearing from the report. |
| PvP win rate | Wins / completed valid battles. |
| PvP matchup score | `(wins + 0.5 × draws) / completed valid battles`; label it as score, not win rate. |
| Duration | Show successful-clear and failed-attempt distributions separately, including median and tail percentiles where sample counts support them. |
| Effective healing | Health actually restored, excluding overheal; unavailable until telemetry supports that distinction. |
| Damage contribution | Distinguish actual health/barrier damage, overkill, and any raw attempted damage before ranking abilities. |
| Control contribution | Accepted control duration/interrupts or mechanic progress, accounting for immunity and overlap; do not equate casts with successful control. |
| Death/survival | Separate first downing, revival, permanent death, and final state where those mechanics exist. |
| Resource use | Record only resources actually modeled and exposed by the relevant content path. |

Missing instrumentation is `unavailable`, never zero. A required metric without support makes a scenario invalid. An optional diagnostic should display its limitation.

Duration on victories alone has selection bias: a build that loses its difficult fights can appear faster. Always present it beside clear rate and sample count. Tick-limit endings are capped observations, not genuine kill times.

Use compact telemetry for bulk runs. Retain a bounded, explicitly selected set of detailed examples: representative victories, failures, timeouts, and large changes. Store the selection policy and all their seeds; the first few fights are not necessarily representative.

## 13. Statistical Interpretation and Balance Assertions

Keep two different claims separate:

1. A fixed seed suite changed. This is a reproducible regression observation.
2. The underlying chance of winning is outside the desired range. This is an estimate with sampling uncertainty.

For independent binary PvE outcomes, report a 95% Wilson interval. As a rough worst-case guide near a 50% clear rate, 100, 400, and 1,600 independent battles provide uncertainty of approximately ±10, ±5, and ±2.5 percentage points. These are planning estimates, not guarantees for paired PvP samples or every metric.

Use predeclared sample sizes for acceptance runs. Exploratory resampling may investigate an outlier, but do not repeatedly sample until an ordinary confidence interval happens to pass. Confirm discoveries with a fixed-size run on reserved seeds.

For a target band:

- **Pass:** the interval lies fully inside the band.
- **Fail:** the interval lies entirely outside the band on a disallowed side.
- **Inconclusive:** the interval overlaps a boundary; the estimate does not support a decisive claim.
- **Invalid:** required data, metrics, or simulations are missing or erroneous.

Use the corresponding one-sided rule for a minimum or maximum. Equality belongs to an inclusive bound. Report inconclusive acceptance runs as needing review, never as passes.

For baseline comparisons, retain matched seed-level results and evaluate paired differences. A side-swapped PvP pair is one sampling cluster. Do not apply a binary-win interval directly to draw-adjusted scores or treat the two orientations as independent.

Every regression rule needs a practical effect threshold as well as uncertainty handling. For example, a five-percentage-point clear-rate decline is different from a five-percent relative decline. State the units and which directions are undesirable; easier content can also violate a difficulty goal.

Keep a small, declared set of gating assertions. Large exploratory rankings create many chances for false alarms; discoveries remain advisory until confirmed on reserved seeds/encounters.

The implemented `evaluate` command reads [draft idle goals](../LL/tools/BalanceHarness/Fixtures/idle-goals.json), validates exact cell coverage and the pinned fixture contract, and evaluates absolute clear-rate/mean-duration/mean-health goals or compatible paired baseline changes. Each check records inclusive bounds, units, eligible sample minimum, rationale and primary/guardrail/diagnostic role. Results are pass, fail, inconclusive or invalid.

The shipped goals are all `Draft`. Optional `Enforced` primary/guardrail goals require a written review reason; diagnostics cannot gate. Assessment includes draft findings while command enforcement considers reviewed checks. Invalid evidence always returns 2; enforced failure returns 1, enforced inconclusive returns 3, and a valid advisory or passing enforced evaluation returns 0. Reports freeze the policy and evidence hashes. The normal-approximation and per-check interval limitations remain visible; no multiple-testing correction or automatic policy promotion is applied.

## 14. Essence Analysis and Scoring

Use scenario-specific evidence before introducing an overall score.

Three levels of analysis:

1. **Observed performance:** results of builds containing an essence. Useful for discovery, but confounded by teammates, opponents, progression, and sampling frequency.
2. **Direct attribution:** damage, effective healing, statuses, summons, and procs attributed to their source when telemetry supports it. This explains events, but does not capture all indirect value.
3. **Marginal contribution:** rerun a matched build with one legal substitution, using the same seed schedule and encounters. This estimates the value of choosing that essence in that context.

Use replacements with comparable slot and acquisition/progression cost. A no-essence control measures the value of filling a slot; it does not establish that the tested essence is better than its alternatives.

Shared-seed comparisons reduce some noise but cannot guarantee identical random events after changing a build. Record paired results without claiming identical fight trajectories.

For synergy, compare four legal variants around the same reference: neither A nor B, A only, B only, and both. On a declared outcome scale, estimate the interaction as `M(A,B) - M(A,0) - M(0,B) + M(0,0)`, where `0` denotes the chosen legal replacement. Report the uncertainty and the reference choices; this is context-dependent and can be distorted by clear-rate ceilings.

Prefer a scorecard of damage, sustain, support, control, and matchup results. If an overall score is added later, publish its scenario weights, normalization, uncertainty, and version. Do not add raw DPS, healing, and control seconds together or count attributed damage again as separate marginal value.

Avoid declaring an essence useless merely because it has low damage or appears infrequently in generated builds. Check its intended role, exposure count, and measured substitutes.

## 15. Baselines and Historical Comparisons

A baseline is an immutable accepted run plus its exact experiment inputs. It is evidence of a reference state, not a replacement for balance targets.

Store small reviewed manifests, fixtures, targets, and accepted summaries in source control. Keep full battle records and replay bundles in an ignored output directory or retained CI artifacts, with checksums and locations recorded in the baseline manifest.

Comparison rules:

- Match stable cell IDs, seed schedules, scenario semantics, units, and scoring versions.
- Allow the intended code/content differences being evaluated and show their hashes/diffs prominently.
- Report changed profile definitions or target policies as experiment changes; compare unchanged cells where possible and mark the rest non-comparable.
- Do not reject every comparison because the content hash changed; testing such changes is the purpose of the harness.
- Keep separate views for stable fixture comparisons and newly generated current-progression fixtures. A generator update must not silently replace the comparison population.
- List added, removed, skipped, invalid, and non-comparable cells explicitly.

Baseline promotion must be an explicit local developer action after reviewing the report and stating why the new reference is accepted. Never overwrite a baseline automatically after a failure. This is a workflow safeguard for future tooling, not a requirement to obtain approval before editing this plan.

The implemented `baseline accept` command validates a complete saved suite and writes a new manifest with its reason, metrics version, relative bundle location, accepted summary and evidence fingerprint. `compare` validates both archives without requiring their old binaries, compares matching complete cells, lists added/removed/incompatible/incomplete cells, and emits advisory JSON/Markdown. Changed code/content is allowed and shown; changed fixture selections, rules, seeds or runtime/platform are excluded. Replay retains strict executable identity checks.

Clear-rate changes use paired gained/lost wins with a conservative approximate 95% interval from Bonferroni-adjusted Wilson intervals. Duration changes use only seeds won in both runs; health uses all matched attempts. Continuous mean intervals are exploratory normal approximations with at least 30 nonconstant paired differences. Versioned goals express draft targets and practical change thresholds. An advisory CI workflow verifies execution and evidence; gameplay-target approval and a multiple-testing policy remain open after the initial cohort review.

When an intentional engine fix changes results, preserve the old report, explain the behavior change, and review targets independently. Rebaselining should not hide an unexplained parity defect.

## 16. Outlier and Dominance Detection

Start with interpretable warnings:

- A progression-reference build misses its content goal.
- A floor or encounter transition produces an unexplained difficulty spike.
- An essence substitution is consistently harmful across its intended use cases.
- A combination outperforms alternatives across several encounter families at the same budget.
- Duplicate stacking sharply improves outcomes or removes a mechanic requirement.
- Sustain creates frequent tick-limit endings or unusually high draw rates.
- A role disappears from successful parties, or only one composition reliably succeeds.

Always attach sample size, effect size, coverage, and reproducible examples. A high rank in one sampled pool does not prove global dominance, and low usage in generated builds is not player pick-rate evidence.

Report tradeoffs: a build that is best against armor but weak against adds may be healthy specialization. Search for alternatives on the damage/survival/control tradeoff frontier instead of requiring one universal ranking.

## 17. Parameter Experiments and Automatic Tuning

Keep production catalogs and existing formulas authoritative. Avoid creating a second balance configuration containing copies of every game value.

Introduce a bounded local overlay only when manual experimentation is useful:

- Explicit allowlist of tunable fields with units, legal bounds, and step size.
- Source content hash, exact old/new values, and experiment rationale.
- Overlay applied to a private snapshot before compilation; cached results keyed by that snapshot.
- Sensitivity experiments changing one parameter or a small related group first.
- Multi-scenario objective with guardrails for progression, build diversity, and unaffected content.

Later search may rank candidate overlays using derivative-free search or another justified method. Penalize unnecessary magnitude/number of changes, use a fixed evaluation budget, and confirm candidates on held-out scenarios and seeds.

Output a proposed patch and before/after report. Never automatically change production files, rewrite targets to fit candidates, promote a baseline, apply migrations, or deploy a suggested adjustment.

## 18. Reporting and Developer Workflow

Start with JSON for tools and Markdown for review. Add CSV exports for cell/matchup tables if useful; a dashboard is optional later work.

The report should show:

1. Run identity, completeness, validity, input changes, and parity/coverage limitations.
2. Primary target results: pass, fail, inconclusive, and draft/advisory cases.
3. Largest practical baseline changes with uncertainty and sample counts.
4. Progression/encounter tables and, when coverage exists, Tower curves and PvP matchup matrices.
5. Diagnostics and concrete replay instructions for selected cases.
6. Runtime/memory results in a separate section.
7. Suggested investigations or parameter experiments with their evidence.

The current CLI supports the following workflow. Use the [tool README](../LL/tools/BalanceHarness/README.md) for runnable PowerShell examples and required options.

| Command | Implemented purpose |
| --- | --- |
| `run` | Save one fixed idle fight |
| `suite` | Run the reference fixture or an explicit suite JSON; `--samples 3` provides a smoke run |
| `replay` | Reproduce a saved fight; suite replay requires `--battle`, and `--detailed` enables the event log |
| `baseline accept` | Validate a complete suite and write a new reference manifest with a reason |
| `compare` | Write an advisory paired comparison against an accepted baseline |
| `evaluate` | Evaluate a saved suite against versioned goals and, for change metrics, a compatible baseline |

Preflight validation is part of run creation; there is no standalone `validate` command or named `smoke` suite selector. [smoke-balance.ps1](../build/smoke-balance.ps1) now wraps two identical suites, disposable repeatability evidence, comparison, replay and draft evaluation for local/CI verification. Resume and broader content commands remain proposed.

Evaluation returns distinct exit statuses for valid advisory/passing enforcement (0), enforced balance failure (1), invalid evidence/configuration/coverage (2), and enforced inconclusive checks (3). Cancellation returns 130. Draft findings remain advisory, and successful execution does not certify unimplemented content types.

The minimum actionable finding is: which case changed, by how much, against which target/baseline, how certain the finding is, and how to reproduce it.

## 19. Suggested Project Boundaries

Keep the first implementation small enough for a solo developer to maintain.

| Location | Responsibility and status |
| --- | --- |
| `LL/tools/BalanceHarness/` | Implemented console entry point, offline composition, execution, metrics, baseline comparison, goal evaluation and reports |
| `LL/tools/BalanceHarness/Fixtures/` | Implemented starter control, original idle reference and First Hunt suites, each with its own draft goals; reviewed baseline manifests can be versioned separately when accepted |
| Existing `Services.LL` combat/content code | Shared preparation, execution, rules, and minimal reusable seams needed by real content adapters |
| `LL/tests/EssenceSystem.Tests/` | Implemented harness correctness, parity, determinism, comparison and goal-evaluation tests |
| `build/smoke-balance.ps1` | Implemented local/CI workflow verification using a disposable same-revision reference; no gameplay baseline promotion |
| `.github/workflows/balance-harness.yml` | Implemented PR/manual harness tests and advisory smoke configuration, job summary and seven-day evidence retention; hosted execution pending |
| Ignored `TestResults/balance/` | Current local output convention for summaries, battle records, snapshots, replay bundles and baseline/evaluation artifacts |

The tool may reference Core and Infrastructure. Core must not depend on the tool, API, Infrastructure, or Presentation. Keep harness-only schemas and scoring policies in the tool until a second genuine consumer requires shared contracts.

Load API-owned content files through an explicit content root without referencing the API assembly or starting its host. Register only required dependencies; do not start hosted services, reward writers, outboxes, or network integrations.

Do not build separate libraries/services for every conceptual box in the original plan. Begin with a few cohesive modules and extract boundaries only when implementation needs them.

## 20. Delivery Phases and Acceptance Criteria

| Phase | Deliverable | Complete when |
| --- | --- | --- |
| 0 — Trustworthy execution | Offline composition, one real idle encounter, explicit rules, parity fixture | Equivalent gameplay and harness inputs produce matching outcomes/state; repeated seeds and logging modes preserve results; no running API/shared database is needed. |
| 1 — Useful vertical slice | Twelve control cells plus 36 First Hunt cells, fixed fixtures/seeds, compact records and Markdown/JSON reports | Every battle is identifiable and replayable; errors/cancellation are visible; a local change produces an explainable report. |
| 2 — Regression workflow | Targets, baseline manifests, paired comparisons, sample/uncertainty rules, small CI suite | A deliberate behavior/fixture perturbation is detected by the appropriate check; unchanged inputs are stable; incompatible comparisons and inconclusive results cannot pass silently. |
| 3 — Content coverage | Tower first, then boss, dungeon, and PvP adapters in separate increments | Each adapter has setup/execution/outcome parity tests and representative progression/mechanic cases; multi-party Tower and content-specific timeout/recovery rules are covered. |
| 4 — Build/essence investigation | Legal substitutions, bounded generation, attribution where supported, synergy analysis | Findings include matched controls and reserved-seed confirmation; different viable builds and role tradeoffs can be inspected. |
| 5 — Tuning assistance | Bounded overlays and ranked proposed patches | Suggestions improve declared objectives without violating guardrails on held-out evaluation; acceptance and production edits remain explicit. |

Do not block phases 0–2 on a general build optimizer, complete telemetry attribution, UI work, economy simulation, or support for all content. These early phases should already answer whether representative idle progression changed.

Phases 0 and 1 are implemented for fixed idle encounters, including group combat, victory/defeat replay, legal partial equipment, paired seeds, scorecards and visible cancellation. Phase 2 includes explicit baselines, compatible paired comparisons, draft goals and distinct evaluation outcomes with optional reviewed enforcement. Both cohorts have advisory CI configuration and local smoke/reference verification. The First Hunt extension addresses starter choices and enemy counts; measured training/Forge states, reward outcomes and gameplay-target approval remain open. The first hosted CI run also needs observation. Do not derive approved targets automatically from observed results.

### Next work to complete Phase 2

1. Review the [First Hunt results](First-Hunt-Cohort.md): all tested untrained level-5 builds lost both Blood Grove pairings. Establish plausible Essence training and Forge states, then run controlled variations before deciding whether the gap belongs in progression or enemy tuning.
2. Review the new cohort's draft minimum-clear and pacing proposals against the intended experience. Its reference evaluation has 63 pass, 26 fail and 91 inconclusive checks, with no invalid evidence. Preserve the old controls and keep reward-box conditions explicit.
3. Give any changed progression recipe a reviewed policy hash and compatible reference, then confirm with predeclared sample budgets/reserved seeds. The 36-cell cohort and its own versioned goals are already implemented; their numerical values remain proposals.
4. Observe the first hosted CI run and adjust operational budgets if needed. CI now smoke-tests both cohorts using three samples per cell, two identical runs each, disposable repeatability references and seven-day retention. Gameplay enforcement remains a separate decision using reviewed primary/guardrail goals with a written `reviewReason`.

This is policy review and workflow integration. Full dungeon runs, Tower, PvP, the complete Beta build matrix, acquisition pacing and automatic tuning still need their own coverage and validation.

## 21. Verification Strategy

When implementing, run backend tests through the repository entry point:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~BalanceHarness|FullyQualifiedName~CombatPreparationPipelineTests|FullyQualifiedName~CanonicalEquipmentBuildFactoryTests|FullyQualifiedName~EquipmentHandRuleTests|FullyQualifiedName~CompactCombatTelemetryTests|FullyQualifiedName~FastCombatEngineOutcomeTests|FullyQualifiedName~AbilityBalanceSimulatorRegressionTests'
```

The filter includes all five harness test classes. Add affected content-specific tests for each new adapter. Use the complete `./build/run-tests.ps1` suite when a change affects shared combat/preparation behavior.

Meaningful new verification should cover:

- Invalid or inaccessible loadouts and unresolved IDs are rejected.
- Identical input snapshots/seeds produce identical results across repeated runs and supported worker counts.
- Each adapter matches production preparation, rules, and content outcome interpretation.
- Summary/detail telemetry preserves gameplay and units, including draw/timeout and revival cases.
- Comparator behavior for improvement, regression, unchanged, missing, incompatible, and inconclusive data.
- A deliberately changed coefficient or broken setup causes the expected result/check to change.
- Cancellation and bounded execution produce partial artifacts without a false passing status.
- Performance budgets on representative content without treating noisy timings as gameplay failures.

`BalanceHarnessTests` covers idle-path parity across progression builds and duplicate/mixed enemy groups, repeatability, logging-mode independence, replay integrity and invalid/cancelled execution. `BalanceHarnessSuiteTests` covers the original schedule, paired/reordered seeds, partial/two-handed loadouts, statistics and cancellation. `BalanceHarnessComparisonTests` covers accepted-evidence integrity, paired statistics, content changes, incompatible/incomplete runs and historical comparison. `BalanceHarnessGoalTests` covers interval boundaries, small samples, policy validation and enforcement outcomes. `BalanceHarnessFirstHuntTests` covers the 36-cell/180-check contract, authored starter choices, reward budgets, independent duplicates, group validation/replay/comparison and legacy hash compatibility. All 57 harness tests and both local smoke workflows passed; two 3,600-battle First Hunt runs also matched. See the cohort report for evidence and limits. Hosted CI, other-adapter and controlled performance checks remain open.

## 22. Decisions to Set Before Enforcing Balance Gates

The following are design choices still to be established from intended gameplay:

1. Do the initial levels 1, 5 and 10 represent the intended player journey well enough for enforcement?
2. Are the fixture's stated gear/essence ownership assumptions attainable at each point, and what acquisition budget should become authoritative?
3. Which clear-rate and pacing bands describe the desired experience, with what practical change tolerances?
4. Which Tower scouting states and alternative party compositions should be considered ordinary?
5. Are the initial 15-minute CI job limit, three-minute smoke limit and seven-day artifact retention suitable after hosted measurement?
6. Which measurements are worth adding to telemetry for the first useful essence analysis?

These decisions can be recorded incrementally while using the implemented runner and advisory evaluator. They prevent treating initial numerical proposals as established balance requirements.

## Source Map

Repository references used to ground this plan. The idle tool, reference suite, baseline workflow, goal evaluator, initial policy review and advisory smoke/CI configuration exist. Reviewed player-cohort enforcement, first hosted CI validation and broader analysis remain future work.

- [Ability simulator](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityBalanceSimulator.cs) and [request/report contract](../LL/src/Core/Application/Interfaces/Services/LL/Essences/IAbilityBalanceSimulator.cs).
- [Combat engine options](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs), [executor](../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/CombatEngineExecutor.cs), and [ruleset contract](../LL/src/Infrastructure/Service/Services.LL/Interfaces/Combat/Resolution/ICombatEngineExecutor.cs).
- [Preparation pipeline](../LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Resolution/CombatPreparationPipeline.cs).
- [Canonical build factory](../LL/src/Infrastructure/Service/Services.LL/PowerRatings/CanonicalEquipmentBuildFactory.cs) and [cooperative rosters](../LL/src/Infrastructure/Service/Services.LL/PowerRatings/CanonicalCooperativeRosterCatalog.cs).
- [Tower runtime factory](../LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerCombatRuntimeFactory.cs), [floor definitions](../LL/src/API/API.LL/Data/world-tower/tower-floors.json), and [region-boss resolver](../LL/src/Infrastructure/Service/Services.LL/RegionBosses/RegionBossCombatResolver.cs).
- [Combat result](../LL/src/Core/Domain/Models/Combat/CombatResult.cs) and [compact telemetry](../LL/src/Core/Domain/Models/Combat/CompactCombatTelemetry.cs).
- [Canonical build and snapshot parity tests](../LL/tests/EssenceSystem.Tests/CanonicalEquipmentBuildFactoryTests.cs), [telemetry tests](../LL/tests/EssenceSystem.Tests/CompactCombatTelemetryTests.cs), and [backend test entry point](../build/run-tests.ps1).
