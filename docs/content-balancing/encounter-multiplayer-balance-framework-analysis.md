# Encounter and Multiplayer Balance Framework Analysis

## Historical decision summary and current disposition

Legends Legacy should evolve the existing balance pipeline rather than create a second framework.

The repository already has the expensive and difficult foundations: finalized canonical builds, legal Essence generation, deterministic seeds, five production-engine PvE benchmarks, diversity-aware representative cohorts, multi-restart elite search, common-seed confidence analysis, real World Tower runtime construction, complete-party elite search, bounded calibration, holdout validation, immutable reports, and an Admin Dashboard Essence simulator. Those systems should remain the build-balance layer.

The missing layer identified by the original analysis was encounter analysis. That layer is now implemented for Region 1 through balance schema 46. Its organizing requirements remain:

1. measured build capability profiles with six universal dimensions, derived from controlled production-engine simulations;
2. deterministic party families assembled and cheaply pre-classified from those profiles;
3. mechanic-specific capability checks plus encounter telemetry that separates terminal failure, observed failure modes, and contributing conditions;
4. certification of each floor at its authored `RequiredSlots`, with optional balance-only 5/10/15 scaling probes that do not change the production content model;
5. an authored party-family response profile describing the encounter's intended shape of viability; and
6. constrained calibration that uses authoritative mechanic evidence and sensitivity probes while preserving encounter identity.

Combat Rating should be retained, but its contract should be narrowed to general permanent progression investment. It must not be presented as a universal effectiveness score. The existing overall calculation already fits that purpose better than its current detailed category names do.

The original correctness blocker is resolved. Offline World Tower execution now assigns each friendly `CombatParticipantSlot` through the same five-player `WorldTowerPartyRules.GetPartyNumber` mapping used by live semantics, and compact trial evidence records the resulting party numbers. Party-scoped abilities therefore no longer lose their party boundary in 10/15-player balance simulations.

## 1. Current state

### 1.1 Automated balance pipeline

`ProductionBalanceRunner` is the single offline orchestration path. Balance schema 46 currently runs these stages in order:

```text
Production catalogs
    -> deterministic Essence smoke battle
    -> Region 1 canonical gear packages
    -> legal E4/E5/E6 Essence build generation
    -> five-scenario PvE benchmark suite
    -> Combat Rating health analysis
    -> generic Essence build optimization
    -> P50/P75/P90 representative libraries
    -> physical build-capability profiling
    -> Essence usage/pair meta analysis
    -> progression power anchors and bands
    -> World Tower Floors 1-10 analysis
    -> bounded encounter calibration
    -> encounter-specific build optimization
    -> elite build and party certification
    -> multi-seed scaling validation
    -> party-family construction and encounter evaluation
    -> optional 5/10/15-player scale probes
    -> optional Region 1 neutral-reference reliability study
    -> latest + immutable JSON/Markdown reports
```

Population-replication policy v3 sits immediately outside this single-run chain. It compares diagnostic recovery and family-contract replication separately across at least three completed reliability snapshots without rerunning combat. Compatibility now requires the complete upstream population protocol as well as matching reliability options; artifacts created before schema 46 cannot form a current compatible panel because that provenance is absent. The evaluator remains a programmatic policy layer rather than a CLI stage or persisted aggregate artifact, so it does not change the balance schema or any individual run verdict.

The resulting authored-contract review preserves physical mechanic recovery while revising only unsupported family premises: replace Regeneration's label-based ordering with an authored sustained-damage/self-sustain requirement; retain AddPressure's MultiTarget identity but use add-window reset advantage and normalized uptime instead of saturated clear rate and raw active ticks; and remove DistributedAttrition's Defensive relative-clear promise. Schema-43 follow-ups show that neither the existing AttritionResilience probe nor a preregistered burden-plus-mitigation conjunction produces a replicating cohort. A separately preregistered Regeneration damage-survival conjunction also fails its discovery prerequisite with zero qualifying source builds. Another contract for either family therefore requires an author-defined premise or a materially different production-observable. These recommendations are not implemented automatically.

`ProductionBalanceComposition` loads the same production ability, status, summon, Essence, crafting, creature, creature-ability, progression, and World Tower data used by the game. It composes `CombatSetupService`, `CombatPreparationPipeline`, `WorldTowerCombatRuntimeFactory`, `CombatEngineExecutor`, and `FastCombatEngine`; it does not use a database or mutate player/content state.

The repository entry point is `build/run-balance.ps1`. The report writer emits one combined snapshot plus stage-specific artifacts under `latest` and an immutable history directory. Metadata records the run seed, schema and simulator versions, combat-engine assembly version, timestamp, and Git commit when available.

### 1.2 Build construction and selection

`EssenceBuildGenerator` creates legal builds for four, five, and six unlocked Essence slots. It groups Essence definitions by source monster and prevents selecting two variants from the same source. Each build is materialized against a canonical Region 1 gear package through `CanonicalEquipmentBuildFactory`.

The initial generator is deterministic for a run seed. `EssenceBuildOptimizer` expands and evaluates the population using the PvE benchmark suite. The optimizer is a search stage, not proof of the complete legal space; the existing elite certification documentation correctly makes that limitation explicit.

`RepresentativeBuildLibrary` selects P50, P75, and P90 profiles independently for E4, E5, and E6. Selection is centered on the named score percentile within the evaluated population and applies an Essence-overlap diversity penalty inside a bounded score window. A representative retains its exact Essences, gear package, character level, Combat Rating, aggregate score, per-scenario scores, discovery generation, and source build ID.

The percentile names describe the generated/evaluated population. They are not percentiles of all legal combinations or of live players.

### 1.3 Existing diagnostic PvE scenarios

`PveBenchmarkRunner` directly uses the production `FastCombatEngine` with production-compiled abilities, statuses, summons, canonical gear attributes, and Essence abilities. Its five scenarios are:

| Scenario | Current window | Existing purpose |
| --- | ---: | --- |
| Short single target | 300 ticks / 30 seconds | Opening and burst pressure |
| Sustained single target | 1,200 ticks / 120 seconds | Sustained damage and ramp |
| High incoming damage | 600 ticks / 60 seconds | Mitigation, survival, and some offense |
| Three targets | 600 ticks / 60 seconds | Multi-target damage and target switching |
| Attrition | 1,800 ticks / 180 seconds | Long survival, sustain, and defense |

The runner already records damage dealt/taken, healing, regeneration, barrier generation, blocking, raw incoming damage, prevented damage, enemies defeated, survival, remaining health, duration, and outcome. It converts those measurements into an equal-weight aggregate score used by build balance.

The normal benchmark run derives a deterministic seed per build and scenario. The newer `RunCommonSeedReplicates` path deliberately uses the same scenario seed for every build in a replicate. The elite benchmark-confidence audit builds on this common-random-number path, retains exact seed panels and per-scenario variance, and compares smaller panels with a larger reference. This work is mature and must not be replaced or silently changed by encounter profiling.

### 1.4 Essence simulator and Admin Dashboard tooling

The protected Admin Dashboard balance feature is distinct from the offline content runner:

- `AbilityBalanceSimulator` supports 1v1, larger teams up to 15 participants, fixed candidate teams, random pools, round-robin matchups, canonical role/equipment profiles, repeated seeded battles, parallel battle execution, per-team and per-Essence results, and Wilson-style confidence output.
- `AbilityBalanceAuditService` runs multi-seed screening, finalist round robins, matched Essence replacement validation, content fingerprinting, and aggregate confidence analysis.
- `DiagnosticsController` exposes simulation and audit endpoints.
- The Angular dashboard can configure seeds, equipment, team size, and Essence count; save combinations; compare audits; and export JSON/CSV. Its audit history is currently browser-local, not a server-side balance artifact store.

This remains the correct interactive Essence/build comparison tool. It should gain links or views for encounter reports later, not be replaced with another Essence simulator.

### 1.5 World Tower production combat

World Tower content already contains 5-, 10-, and 15-player floors. `TowerFloorDefinition` currently has one `RequiredSlots` value and one set of Guardian scaling values per floor. Live rosters are divided into parties of at most five by `WorldTowerPartyRules`.

Live combat uses:

```text
Persisted CharacterSnapshots
    -> SnapshotCombatantRequest with PartyNumber
    -> CombatPreparationPipeline
    -> WorldTowerCombatRuntimeFactory
    -> WorldTowerGuardianScaling
    -> CombatEngineExecutor / FastCombatEngine
    -> compact checkpoints, CombatResult, playback, and battle report
```

The combat runtime supports initial hostile participants, static reinforcement waves, and dynamic hostile wave factories. The engine supports summons, threat and attention, stagger, downed/revival states, status/control effects, cleansing, dispelling, party-aware targeting, wave events, and deterministic magnitude/targeting random streams.

World Tower Guardian scaling already treats player-count dimensions differently:

- health uses `participantCount^0.85`;
- offense uses `1 + 0.05 * (participantCount - 1)`;
- durability uses `(participantCount / 5)^0.25`;
- stagger has its own reference count and exponent.

This is a better starting point than linear scaling, but the formulas are not tested through isolated cross-size probes of the same boss. Each authored floor correctly selects only one player count.

### 1.6 Current World Tower balance analysis

`WorldTowerContentAnalyzer` selects the P75 representative profile closest to each Region 1 floor's target benchmark power. For each deterministic trial it shuffles that one profile, fills the authored roster size, and runs the real Guardian through production preparation and combat. It records clear rate, duration, deaths, remaining party health, mean/team displayed CR, and build IDs.

Strengths:

- production content and production combat determine outcomes;
- party and combat seeds are stable and reproducible;
- 5/10/15 participant counts are exercised across Floors 1-10;
- authored Guardian identity is validated;
- the analyzer can materialize both representative and arbitrary generated builds;
- temporary calibration never changes production content.

Limitations:

- parties come from one generic percentile profile without composition constraints;
- a 10/15-player roster can repeat builds when the retained library is smaller than the roster;
- no build capability profile drives selection;
- balance-created participant slots omit live `PartyNumber` assignment;
- only Floors 1-10 are in the current progression-band analysis even though content is released through Floor 15;
- one boss is not evaluated at all three sizes;
- the result has no per-target, add, control, cleanse, or mechanic-failure diagnostics;
- `ObservedClearRate`, average deaths, duration, and remaining health are too coarse to explain a loss sequence.

### 1.7 Existing calibration, specialization, and certification

`EncounterCalibrator` performs an auditable common-seed binary search. It applies one shared factor to Guardian health and offense within 0.25-2.00 and reports convergence, best effort, or exhausted bounds. It never writes content. This is safe and useful, but a shared health/offense factor can erase the distinction between a durability check and a tank-pressure check.

`EncounterSpecificOptimizer` ranks every matching-slot generic candidate against a real calibrated floor, then retains a diverse set and compares it with generic P75. Individual candidate evaluation fills the entire roster from a one-build list, so candidate scores are effectively homogeneous-party scores. The final retained list is mixed, but 10/15-player rosters cycle through the small retained set. This is valuable hard-counter detection, not a complete party model.

`EliteBuildCertificationAnalyzer` is substantially more mature. It has independent deterministic restarts, local challenges, Pareto/scenario evidence, P95/P99 score-centered cohorts, curated top-player fixtures with a content fingerprint, holdout confidence, and a complete-party genome search against real floors. The party optimizer permits repeated finalist builds and evaluates full `RequiredSlots` rosters. It establishes an elite ceiling, but it searches for the best party rather than a distribution of healthy, specialist, awkward, and bad party families.

`ScalingValidationAnalyzer` uses independent holdout seeds, Wilson intervals, cross-seed standard deviation/range, easier/harder probes, health-only and damage-only sensitivity, and P50 <= P75 <= P90 ordering. It is an excellent foundation for encounter certification. Its acceptance population is still a generic percentile profile rather than composition-aware party cohorts.

### 1.8 Combat telemetry already available

`EntityStats` and the engine's balance accumulator expose more than the current World Tower report uses:

- damage done/taken and damage by ability/type;
- healing done/received and regeneration, potential, overheal, and pulses;
- barrier generated and absorbed;
- raw incoming damage and avoided, mitigated, blocked, reduced, redirected, and amplified damage;
- threat generated, targeted attacks, and attention share;
- stagger contribution and breaks;
- deaths, revivals, downed ticks, and final health/barrier state.

The full event vocabulary also includes status application/removal/cleanse/dispel, summon/expiry, death/revive, stagger, and waves. Full event logging is disabled in large balance runs for good performance reasons. The compact accumulator does not yet retain target-split damage, control uptime, cleanse counts, add lifetime/peak active adds, mechanic counters, or the timing needed to order observed failure conditions reliably.

There is no explicit cast/interrupt mechanic in the current combat contract. Silence and hard control can stop actions, but an `InterruptCapability` dimension would currently claim a precision the engine does not support.

## 2. Reuse classification

| Existing component | Classification | Recommendation |
| --- | --- | --- |
| `FastCombatEngine` and `CombatEngineExecutor` | Keep unchanged at the outcome-authority boundary; extend telemetry only | All outcomes must continue to come from production combat. Add generic low-cost observation, not parallel combat formulas. |
| `CombatPreparationPipeline` and `WorldTowerCombatRuntimeFactory` | Keep and extend | Preserve them as the one live/offline World Tower preparation path. Correct offline party-number mapping and allow balance-only player-count overrides without duplicating preparation or changing authored floors. |
| `CanonicalEquipmentBuildFactory`, gear packages, and snapshot materialization | Keep unchanged | They already create finalized, production-faithful builds. |
| `EssenceBuildGenerator` | Keep unchanged for generic populations | Its legality/source-family rules are reusable. Later population sources may add real/curated exact builds alongside it. |
| `PveBenchmarkRunner` scoring v1 | Keep unchanged for build balance | Changing its five scores would invalidate optimizer, representative, elite, and confidence evidence. Extract/reuse execution helpers, but create a separate versioned capability profile contract. |
| Common-seed benchmark replicates and benchmark-confidence audit | Keep unchanged | Reuse seed panels and statistical techniques. Do not fold encounter certification into the build score. |
| `EssenceBuildOptimizer` and `RepresentativeBuildLibrary` | Keep unchanged as generic build-balance inputs | They remain the ordinary build population. Capability-aware clustering should consume their output, not replace percentile selection initially. |
| `EssenceMetaAnalyzer` | Keep unchanged | Useful for explaining recurring combinations and outliers; not a substitute for measured capabilities. |
| Power anchors and progression bands | Keep unchanged | They define investment/progression intent and select relevant cohorts. |
| `WorldTowerContentAnalyzer` | Refactor and extend | Extract reusable encounter execution from sampling/reporting; preserve its production path. Add party numbers, arbitrary party cohorts, balance-probe keys, richer results, and all released bands. |
| `EncounterCalibrator` | Refactor and extend | Preserve bounded common-seed search and non-mutating recommendations. Replace the one shared factor with constrained parameter groups only after failure diagnostics exist. |
| `EncounterSpecificOptimizer` | Extend, then narrow its responsibility | Keep it as build hard-counter discovery. Feed capability profiles into candidate diversity and stop treating it as the representative party distribution. |
| Elite build certification | Keep and extend | Reuse elite cohorts, direct party-genome search, curated fixtures, content fingerprints, and holdouts for optimized/extreme party families. |
| `ScalingValidationAnalyzer` | Extend | Generalize from one P75 profile to party families, authored response profiles, optional player-count probes, observed-failure expectations, and duration/mechanic constraints. |
| `BalanceReportWriter`, CLI, immutable history | Keep and extend | Add capability, party-cohort, encounter-diagnostic, and certification artifacts to the same command and schema. |
| Admin Essence simulator/audit | Keep unchanged for Essence analysis; extend UI later | Do not merge build and encounter questions into one ranking. Add encounter report views/actions alongside it. |
| Combat Rating overall | Keep but redefine | Treat it as permanent progression investment. Validate its progression monotonicity, not universal encounter prediction. |
| Combat Rating detailed offense/control categories | Replace or stop exposing | Single- and multi-target offense are currently identical and control utility is zero. Measured capability profiles should own those claims. |
| Generic World Tower failure string | Replace | The current "Expedition was defeated" message should be backed by a typed failure classification with evidence. |
| Old database-backed balance caches | Keep removed | The first encounter framework should use offline versioned artifacts/caches. Reintroduce production persistence only for a later player-readiness feature with a clear invalidation contract. |

No mature build-balance subsystem should be removed.

## 3. Gaps

### Capability measurement

The five PvE scenarios implicitly measure several capabilities, but the result is collapsed into a generic score. There is no versioned, reusable fingerprint with physical units or normalized dimensions, no target-split priority damage, no party-support benchmark, and no control/cleanse measurement.

### Party populations

The generic analyzer randomizes members from one profile. The encounter-specific stage scores homogeneous copies before retaining a small mixed list. Elite certification searches only the ceiling. There is no deterministic distribution representing healthy balanced parties, specialists, plausible weak rosters, intentionally bad rosters, or capability-defined composition archetypes.

### Encounter requirements

`TowerFloorDefinition` has player count, CR, Guardian multipliers, stagger, and descriptive ability data, but no internal certification envelope, mechanic identity constraints, or measured demand profile. Current UI tags and scouting descriptions explain mechanics but do not summarize capability demands.

### Failure observations

The engine exposes enough raw totals for broad diagnosis but no typed terminal result, observed failure mode, or contributing-condition evidence. Current World Tower balance and live reports cannot distinguish timeout, primary-target collapse, general attrition, boss sustain, add accumulation, priority-target failure, or a failed status mechanic without overstating causal certainty.

### Player-count scaling analysis

The runtime supports rosters of all three sizes across different floors, while each floor intentionally has one authored `RequiredSlots` value. The balance tool cannot yet ask how the same encounter definition would behave at 5/10/15 without changing production content. Formula inputs and stagger scaling exist, but there is no isolated hypothetical player-count probe or authored-size certification policy.

### Calibration

The common-seed binary search is reproducible but moves health and offense together. It has no way to target add cadence, boss regeneration, primary-target damage, or an enrage timer, and no objective penalty for changing encounter identity.

### Persistence and invalidation

Reports are versioned, but reusable capability results are not cached. The existing combat fingerprint is a strong start but does not by itself cover every build, gear, scenario, creature, World Tower definition, scaling-rule, and engine input required for safe encounter-result reuse.

### Live distribution

Curated player fixtures exist but are intentionally empty and current generated percentiles are not real-player percentiles. The architecture can accept real builds later, but it does not yet materialize anonymized progression-band distributions.

## 4. Recommended architecture

Keep two explicit analysis products on one shared execution foundation:

```text
                             PRODUCTION CONTENT + COMBAT ENGINE
                                          |
                    +---------------------+----------------------+
                    |                                            |
              BUILD BALANCE                               ENCOUNTER BALANCE
                    |                                            |
    legal/finalized build population                finalized build population
                    |                                            |
    existing five-scenario PvE scores               diagnostic combat scenarios
                    |                                            |
    optimizer + P50/P75/P90 + elite                  capability profiles (versioned)
                    |                                            |
    Essence rankings, confidence, meta               deterministic party families
                    |                                            |
                    +----------------------+---------------------+
                                           |
                              real World Tower runtime
                                           |
                         outcome + compact encounter telemetry
                                           |
             clear/duration/failure observations/family-response diagnostics
                                           |
                authored-size certification + optional scale probes
                                           |
                              constrained tuning advice
```

### Suggested boundaries

Within `LL/tools/LegendsLegacy.Balance`, add small cohesive namespaces rather than a second executable:

- `Capabilities/DiagnosticScenarioCatalog`: versioned controlled scenario definitions.
- `Capabilities/CombatCapabilityProfiler`: materializes finalized builds and produces raw and normalized profiles.
- `Parties/BenchmarkPartyBuilder`: cheaply creates and pre-classifies deterministic party-family cohorts from progression and character capability profiles.
- `Encounters/WorldTowerEncounterExecutor`: extracted from `WorldTowerContentAnalyzer`; accepts an exact roster, authored floor or balance-only scale probe, ruleset, and seed and returns a rich trial result.
- `Encounters/EncounterOutcomeAnalyzer`: records terminal failure, observed failure modes, contributing conditions, and any explicit authoritative mechanic cause.
- `Encounters/EncounterCertificationAnalyzer`: compares each party family with the encounter's authored response profile at the floor's authored player count.
- `Encounters/EncounterScaleProbeRunner`: optionally evaluates hypothetical 5/10/15 player counts without materializing production variants.
- `Encounters/EncounterParameterCalibrator`: evolves the current bounded search with identity constraints.

The existing `ProductionBalanceRunner`, report writer, CLI, seed infrastructure, content loaders, and immutable artifact layout remain the orchestration shell.

### Data contracts

A capability artifact should contain:

```text
Build fingerprint
Progression band / gear package
Profiler + scenario-catalog versions
Combat-content + engine/ruleset fingerprint
Exact scenario seeds
Raw measurements per scenario
Normalized capability dimensions within the reference progression band
Measurement variance / sample count
```

An encounter trial should contain:

```text
Encounter ID, authored player count, and optional balance-probe ID
Exact party-family and party signature
Combat and selection seeds
Outcome and duration
Entity/party totals
Target/add/mechanic telemetry
Terminal failure
Primary observed failure mode with confidence and evidence
Contributing conditions with evidence
Optional authoritative mechanic cause
```

### Cache safety

Use a composite key, not a timestamp:

```text
SHA256(
  exact finalized-build fingerprint
  + combat/Essence/equipment/creature content fingerprint
  + scenario catalog version and definition hash
  + combat engine and ruleset versions
  + profiler algorithm version
  + exact seed panel
)
```

Persist the first cache under the configured balance output root as immutable JSON or a compact local store. Do not add production database tables in the first implementation. Reuse a cached character profile across party construction and encounters, but never reuse a full encounter outcome when party, authored player count or probe definition, encounter definition, preparation bonuses, seed panel, or content fingerprint changes.

## 5. Combat Rating recommendation

### Recommendation: Option B — retain and redefine

The current overall Combat Rating is a deterministic weighted valuation of permanent direct attributes after base, equipment, and resolved attribute modifiers. It excludes temporary encounter effects and does not attempt to value every active/passive Essence behavior. That makes it suitable for the question:

> Is this character approximately invested/geared for this progression band?

It is not suitable for:

> How effective is this build or party against this encounter?

The current code already documents that it does not predict wins or recommend content, and World Tower's derived CR is based on progression anchors rather than a direct win formula. This strongly supports Option B.

Required changes to its contract:

- call the player-facing value Combat Rating consistently, while keeping historical internal `PowerRating` names where compatibility requires;
- describe it as permanent progression investment, not combat effectiveness;
- keep temporary buffs and encounter effects excluded;
- do not add Essence-synergy or encounter-specific bonuses to the number;
- validate monotonic progression, sensible band separation, and gross outliers rather than requiring high correlation with one equal-weight PvE score;
- stop exposing or rename the detailed `SingleTargetOffense`, `MultiTargetOffense`, and `ControlUtility` claims until they are meaningful. The first two are currently the same attribute sum and control is always zero;
- show encounter demands and party capability readiness separately.

Consequences of the alternatives:

- Option A preserves a useful overall number but also preserves misleading detailed semantics and the temptation to treat CR as a universal predictor.
- Option B retains existing snapshots, rally storage, UI familiarity, progression anchors, and authored recommendations while giving capability profiling the correct responsibility.
- Option C removes a cheap and understandable progression signal, breaks multiple DTO/content/storage contracts, and still leaves the game needing a gear/investment gate. The current implementation is not fundamentally misleading if its scope is stated correctly.

No EF migration is required merely to redefine the label and contract. Removing/renaming stored fields would be a separate compatibility project and is not recommended.

## 6. Minimum useful capability model

Start with six first-class universal character dimensions. Preserve raw measurements and normalize dimensions only against a named progression cohort. These dimensions are broad enough to construct useful parties without pretending that every mechanic can be reduced to one interchangeable scalar.

| Capability | Meaning and measurement | Diagnostic scenario | Level | Encounter use |
| --- | --- | --- | --- | --- |
| SingleTargetBurst | Effective hostile damage in an opening window, with survival/uptime reported separately | Reuse the current short single-target benchmark; add a shorter window only if measured opener variance proves useful | Character | Burst phases, shields, healing-ramp breakpoints |
| SingleTargetSustained | Effective boss damage per second over the current 1,200-tick window, including ramp and downtime | Reuse sustained single target | Character | Boss HP budgets, enrage, regeneration/healing checks |
| MultiTarget | Damage, kills, wave clear time, and unresolved enemy count under repeated groups | Evolve the current three-target scenario into a static panel plus bounded reinforcement waves | Character | Add/wave pressure and cleave encounters |
| FocusSurvivability | TTD, minimum health, mitigation, healing required, attention share, and survival under concentrated heavy hits | Heavy single-target pressure with threat enabled | Character | Tank/primary-target pressure |
| AttritionResilience | TTD, remaining health, self-healing/regeneration, mitigation, and barrier under sustained pressure | Reuse/evolve attrition pressure | Character | Long fights and distributed damage |
| PartySustain | Effective allied healing, burst recovery, barriers, overheal, and deaths prevented under controlled allied damage | Damaged benchmark allies with repeatable incoming patterns | Character support contribution | Party-wide pressure and recovery checks |

Priority-target performance remains a raw diagnostic measurement under multi-target testing or an encounter-specific check. It should become a universal dimension only if real results show that it reliably distinguishes builds beyond `SingleTargetSustained` and `MultiTarget`.

### Mechanic capabilities

Control, stagger, cleanse, and dispel should be explicit mechanic capabilities rather than universal normalized scores. Their physical semantics matter more than a percentile such as `Cleanse = 72`:

- cleanse capacity and time-to-cleanse for named removable-effect types and target counts;
- dispel capacity for named enemy-buff types;
- hard-control, silence, slow, or other action-denial coverage reported separately by effect type, susceptible target count, duration, and cadence;
- stagger contribution and breaks within an authored stagger window;
- priority-objective damage or TTK while specified secondary targets remain active.

An encounter that needs one of these declares a measurable contract such as "cleanse three removable party debuffs within 15 seconds" or "produce the stagger threshold inside this window." Party construction can then test coverage against that contract. It must not treat stun, silence, slow, stagger, cleanse, and dispel as interchangeable points in one utility dimension.

Do not add an interrupt capability until the combat engine has authored casts and explicit interruption outcomes. Do not create a separate shielding dimension initially; retain it as a raw `PartySustain` and `AttritionResilience` submetric. Do not collapse focus survivability and attrition resilience: the existing threat/attention system makes concentrated pressure mechanically distinct from sustained or distributed pressure.

The current five benchmark scores remain unchanged. The profiler can reuse their executions and raw metrics where definitions match, then run only the missing party-support and wave measurements universally. Mechanic diagnostics run only for builds or parties being evaluated for an encounter that declares the corresponding mechanic.

### Character profiles construct parties; encounters test parties

Cached character profiles are the cheap selection layer. Use them to stratify candidates, satisfy minimum coverage, identify specialists, and pre-classify generated parties. Do not run a complete independent diagnostic scenario suite for every generated party.

Party interactions are nonlinear, so summed character scores are not an authoritative prediction. The authoritative party test is the sampled party's actual production-engine simulation against the encounter. Dedicated party-level diagnostic scenarios are reserved for a small frozen sentinel cohort, regression baselines, or a focused calibration investigation where isolating an interaction is worth the additional simulation cost.

## 7. Party sampling strategy

### Capability-based, not class-based

Within each progression band, convert every capability dimension to a robust percentile or z-score using the finalized reference population. Party-building constraints should refer to measured dimensions, not class names or hand-authored Essence tags.

Examples of quantitative member tendencies are allowed for selection and explanation:

- `frontline-capable`: high focus survivability and demonstrated attention share;
- `party-sustain-capable`: high effective allied healing/barrier under the support scenario;
- `single-target specialist`: high sustained/burst single-target and materially lower multi-target rank;
- `multi-target specialist`: the inverse specialization;
- `mechanic specialist`: satisfies a named encounter-specific control, stagger, cleanse, dispel, or priority-objective contract;
- `generalist`: no severe deficit across the core dimensions.

These are derived labels over measurements, not permanent RPG roles.

### Party families

Generate these at each floor's authored player count and progression band. The same builder can generate 5/10/15 rosters when an optional scale probe requests them:

| Family | Construction intent |
| --- | --- |
| Intended balanced | Meets minimum focus-survival and party-sustain coverage, then fills diverse damage capabilities and any declared mechanic coverage without extreme concentration |
| Damage heavy | Meets only the minimum survival/sustain constraints and spends remaining slots on damage |
| Defensive | Exceeds focus-survival and sustain coverage while retaining a minimum damage budget |
| Single-target specialist | Strong single-target capability with legal minimum survival/sustain and deliberately lower multi-target coverage |
| Multi-target specialist | Strong wave coverage with legal minimum survival/sustain |
| Mechanic specialist | Strong coverage of a mechanic the encounter actually declares, while retaining minimum damage and survival; omit when no such mechanic applies |
| Awkward but plausible | Misses one recommended capability threshold, preserves reasonable CR, and avoids deliberately nonsensical low-power selection |
| Poor composition | Explicitly violates one or more essential constraints, such as no focus survivor or negligible party sustain |
| Optimized/extreme | Reuse elite finalists and the existing complete-party encounter search; retain Pareto-diverse ceilings, not only one best roster |

### Authored party-family response profile

Every certifiable encounter should state its intended shape of viability. A family result is meaningful only relative to that design intent. Use a small ordered vocabulary such as:

```text
Advantaged
ShouldSucceed
DisadvantagedButViable
UsuallyFails
NotApplicable
```

For an add boss, the authored profile might mark `Intended balanced = ShouldSucceed`, `Multi-target specialist = Advantaged`, `Single-target specialist = DisadvantagedButViable`, and `Poor composition = UsuallyFails`. A healing-ramp boss can deliberately reverse the specialist relationship. Each disposition owns a configurable clear-rate envelope, duration expectations where relevant, and any required mechanic observations.

Certification compares the measured family response curve with this authored profile; it does not pull every family toward one generic clear rate. A specialist is an unexpected bypass only when its result contradicts the declared disposition, skips an authoritative mechanic requirement, or falls outside a configured extreme-outlier policy.

### Progression cohorts

- Undergeared: use the same party-family constraints from the lower progression/CR band. Do not simulate undergeared by multiplying stats on intended builds.
- Intended: use the floor's target progression band and its P50/P75/P90 or equivalent finalized population.
- Overgeared: use the next approved progression band or exact higher-investment builds.
- Live/curated: later add exact reviewed builds without mixing them into generated percentile labels.

### Controlling combinatorics

Do not enumerate all rosters. Use a deterministic constrained sampler:

1. stratify candidate members by capability percentiles and specialization;
2. seed each family with required capability coverage;
3. fill remaining slots with a greedy diversity objective over build signatures and capability vectors;
4. deduplicate order-independent party signatures;
5. retain a fixed party budget per family and authored-size/probe evaluation;
6. use common combat seeds across comparable families and parameter candidates;
7. reserve direct genome search for optimized/extreme ceilings.

The party budget should be configuration-owned. Begin with performance measurements, not an arbitrary large default. A useful development profile can use tens of parties per family and a release profile can use hundreds only where confidence and runtime justify it. Member profiles construct and pre-classify these parties; only the retained samples run against the boss. Dedicated party diagnostic scenarios are not part of this inner loop.

## 8. Encounter calibration strategy

### Use TTK/TTD as budgets

Before simulation, derive transparent initial values:

- boss effective health from intended effective party DPS and target duration;
- primary-target incoming damage from the intended focus-survivability/TTD band;
- add HP/count/cadence from intended wave throughput and target add lifetime;
- boss healing/ramp from the intended sustained-DPS threshold and desired failure time;
- distributed damage from intended party-sustain and attrition resilience;
- mechanic cadence from the encounter's typed capability contract and desired tolerance.

These are starting budgets and report diagnostics. The production combat engine remains the outcome authority.

### Identity-preserving parameter groups

Each certifiable encounter should declare which groups are tunable and their bounds in balance configuration:

| Parameter group | Examples | Allowed response |
| --- | --- | --- |
| Boss survivability | HP, defense, resistance | Tune when duration/enrage dominates without mechanic failure |
| Primary-target pressure | power, heavy-hit coefficient, attack interval | Tune when primary-target collapse dominates |
| Add pressure | add HP/damage/count/cadence/maximum active | Tune when add accumulation or priority failure dominates |
| Sustain check | regeneration, heal amount, ramp start/rate | Tune when boss healing exceeds effective damage |
| Party attrition | party-wide damage and cadence | Tune when distributed attrition dominates |
| Mechanic cadence | status/control/cleanse timing, enrage | Tune only within authored identity constraints |

An add encounter must give the add-pressure group a nonzero required contribution and must not be "fixed" only by boss HP. A healing-ramp encounter must retain its ramp and threshold. Identity constraints should include the party-family response profile, expected observed failure modes, duration range, add lifetime/peak bounds, authoritative mechanic requirements, and allowed parameter movement.

### Search algorithm

Retain bounded, deterministic, explainable search:

1. evaluate the baseline on frozen party cohorts and common seeds;
2. select a parameter group only when an authoritative mechanic cause or a high-confidence configured observation rule supports it; otherwise run bounded sensitivity probes and return `REVIEW` if attribution remains ambiguous;
3. use binary search for a monotonic continuous knob, a small grid for discrete counts/cadences, and bounded coordinate descent when two groups genuinely interact;
4. re-evaluate all party families after every accepted coordinate step;
5. stop when the target envelope is satisfied, no candidate improves the objective, or bounds are exhausted;
6. report the full trace and never write production content automatically.

The objective should penalize:

- distance outside the intended clear-rate band;
- distance from each family's authored response envelope, including intended advantages and disadvantages;
- undergeared or `UsuallyFails` parties clearing too often;
- overgeared/optimized or `ShouldSucceed` parties failing too often;
- percentile/progression ordering violations;
- duration outside the target range;
- observed failure patterns or authoritative mechanic causes that contradict encounter identity;
- excessive parameter movement from authored values;
- cross-seed instability and wide confidence intervals.

The calibrator must not treat correlation as proof of causation. For example, add accumulation, lost boss damage, boss healing dominance, and timeout may all appear in one loss. Unless encounter logic emits an authoritative cause, those are ordered observations and contributing conditions. Automatic parameter selection should require explicit evidence or a reviewed, versioned rule; ambiguous cases should produce a sensitivity comparison rather than silently adjusting the most correlated knob.

Do not use a machine-learning optimizer. The current common-seed binary search, Wilson confidence, monotonic probes, and holdout design are the correct statistical foundation.

## 9. Failure observations and diagnostics

### Typed result

Separate what ended the trial from what was observed and from what the encounter authoritatively reported:

```text
TerminalFailure
  None | PartyDefeated | Timeout | Enrage | ObjectiveFailed | Other

PrimaryObservedFailureMode
  None | PrimaryTargetCollapse | PartyAttrition | BossSustainDominance
  | AddPressure | PriorityObjectiveUnmet | ControlWindowUnmet
  | CleanseDemandUnmet | Other

ContributingConditions[]
  the same observed-condition vocabulary, each with evidence and confidence

AuthoritativeMechanicCause?
  a typed cause emitted by production encounter/objective logic
```

`PrimaryObservedFailureMode` is the strongest supported description of the loss sequence, not necessarily its root cause. Avoid calling a character a tank solely from an authored role. `PrimaryTargetCollapse` should be supported by measured attention share/targeted attacks, death timing, and the subsequent party health trajectory.

Suppose adds accumulate, damage dealers lose boss uptime, boss healing overtakes incoming damage, and the trial times out. The result can safely state `TerminalFailure = Timeout`, select the earliest or strongest configured observed mode with a confidence value, and record the other conditions as contributors. It must not invent an authoritative cause when encounter logic did not emit one.

### Evidence priority

Record evidence in this order:

1. explicit mechanic cause emitted by production encounter logic;
2. terminal objective state such as max ticks, enrage, party defeat, or a surviving required target;
3. objective telemetry such as active-add cap, priority-target deadline, required cleanses, or control window;
4. temporal association such as high-attention member death followed by rapid party collapse;
5. aggregate heuristic only when no stronger evidence exists;
6. `Other` rather than false precision.

Only item 1 may populate `AuthoritativeMechanicCause`. Items 2-5 support terminal or observed fields and must carry a rule version and confidence. Every classification must serialize its evidence: relevant tick, entity/objective ID, threshold, observed value, and rule version.

### Instrumentation changes

First reuse current totals. Then add generic, low-cost production-combat observation sufficient for balance without capturing full logs:

- target-split damage/healing for tagged benchmark objectives;
- counts and useful timings for status applied, cleansed, dispelled, and action-denial duration;
- summon spawn/death/expiry, lifetime, and peak active hostile count;
- death tick and attention share near death;
- boss healing/regeneration over time, not just totals;
- scale-probe/mechanic counters and explicit success/failure markers;
- a simulation-with-compact-checkpoints executor that accepts the normal `CombatRuleset` and keeps `CaptureEventLog` false.

Keep mechanic-specific interpretation in the encounter analyzer. Add only generic events/counters to Core/Services so production combat is not coupled to the balance tool.

The live Tower battle report can later use the same observation model, but the first implementation should validate it offline before replacing player-facing messages.

## 10. World Tower 5/10/15 scaling

### Preserve the production content model

Keep each production floor's authored `RequiredSlots`. Floor 5 does not need to become a selectable 5/10/15-player encounter merely because the balance tool can investigate scaling. The initial work requires no production `TowerFloorDefinition`, API, rally, persistence, or UI variant model.

Represent cross-size experiments in balance-owned configuration:

```text
EncounterScaleProbe
  encounter/floor ID
  hypothetical player count: 5 | 10 | 15
  scaling formula version
  temporary parameter overrides
  party-family response profile or explicit probe expectations
  seed panel and evidence fingerprint
```

The executor materializes this as an ephemeral runtime input and never writes it into production content. The authored-size evaluation is the release certification. Hypothetical sizes are research evidence, clearly labeled `BALANCE_PROBE`, and do not block a production release unless an explicit balance policy opts into that behavior.

If selectable encounter sizes later become a real gameplay feature, introduce shared encounter identity plus production variants as a separate architecture/content project driven by gameplay requirements. The balance probe results can inform that project, but should not force it prematurely.

### Formula candidates

Shared formulas are allowed as authoring defaults:

- boss health: current sublinear exponent as a starting hypothesis;
- boss primary-target damage: near-flat or mild scaling, since one target receives it;
- party-wide damage: scale by target count/party partitions rather than boss health logic;
- defense/resistance: mild scaling only if larger rosters create multiplicative debuff/penetration effects;
- stagger: retain its independent reference-count/exponent model;
- add count: table or piecewise rule such as 2/4/5, not forced linearity;
- add HP and spawn cadence: separate from count;
- capped/party-scoped effects: respect the live five-player `PartyNumber` layout.

### Evaluation rule

Certify the floor's authored player count on frozen party-family cohorts and seed panels. It passes when:

- every family result fits the encounter's authored response disposition and envelope;
- duration and observed-failure pattern remain within identity bounds;
- authoritative mechanic requirements pass;
- under/intended/over progression ordering holds;
- confidence/stability requirements pass.

Optionally run the same checks as independent 5P, 10P, and 15P scale probes. Similar hypothetical difficulty means preserving the authored response shape and mechanical identity, not equal health-per-player or identical numerical clear rates. A specialist performing exceptionally well is correct when the response profile declares it advantaged; it is a bypass only when it violates that declared shape or skips an authoritative mechanic check.

## 11. Player-facing design

### Floor information

Show:

```text
Recommended Combat Rating: ~8,200
Expedition Size: 10

Encounter Demands
Single Target       Very High
Multi-Target        Low
Primary Pressure    High
Party Sustain       Moderate

Mechanic Demands
Stagger Windows     Not Required
Cleanse             Frequent Party Debuffs
```

Demand labels should be a versioned projection of certified internal requirements into five broad bands. They should not be recalculated from one noisy run at request time. Continue showing plain-language scouting/mechanic descriptions; those explain why a demand exists.

### Roster readiness

Replace CR-only readiness with two separate sections:

- `Progression`: average/member Combat Rating versus the recommendation.
- `Encounter fit`: broad party capability categories such as Insufficient, Low, Adequate, Strong, Exceptional.

Never show an exact win probability. If one or more build profiles are unavailable or stale, show `Unknown` for encounter fit rather than treating CR as a proxy.

Dynamic readiness should not synchronously run combat when a player opens a rally. It requires a versioned cached profile for each exact snapshotted build. That production persistence/background evaluation is a later phase and may require an EF migration. Static encounter demands and corrected CR language can ship earlier without it.

### Naming cleanup

Player-facing World Tower currently says `Power Rating` in several contracts/messages while the system documentation calls it Combat Rating. Change display copy and new DTO fields to Combat Rating. Preserve old serialized/internal names behind mapping during a compatibility window if needed.

## 12. Phased implementation plan

Current version note (2026-08-30): schema 46 supersedes the version-specific Phase-2 details in the historical implementation paragraph below; capability profiler v3 and PvE benchmark v2 add diagnostic average health-deficit telemetry without changing the six dimensions or their rankings, the progression-fidelity report retains every neutral-reference candidate, the matched-genome power panel isolates population composition, and cross-population policy requires explicit matching upstream cohort provenance.

Implementation status (2026-08-29): Phase 0 and the first Phase 1 slice are implemented for Region 1. Compact telemetry now has an independent off switch and an allocation regression budget. Phase 2 is implemented as build-capability profiler algorithm v2: it reuses the five unchanged PvE benchmark executions, adds ally-support and three-wave response probes, emits all six universal dimensions in physical units plus profile-relative percentiles, and keeps cleanse, dispel, individual control types, and stagger as separate mechanic measurements. Support and wave probes can use an optional common-seed panel (`--capability-seeds`, default 1) with sample variance, and CLI runs persist their content/build/scenario/engine-keyed probe cache under the output root. Balance schema 23 completes Phase 3 and the tooling portion of Phase 4. Party-family builder algorithm v2 emits balanced, damage-heavy, defensive, single-target, multi-target, mechanic, awkward, poor, and elite-sourced optimized/extreme P75 families at every floor's authored `RequiredSlots`, plus balanced P50/P75/P90 progression cohorts. Each roster retains its seed, signature, exact source builds, capability vector, and constraint evidence. P75 progression evidence reuses the intended-balanced family execution; only missing P50 and P90 rosters add production-combat work. Party-family encounter evaluator algorithm v3 runs all retained evidence without calibration overrides, records Wilson 95% intervals and observed failure diagnostics, and checks P50 ≤ P75 ≤ P90 with a reviewed five-percentage-point tolerance. A point inversion is `Review` while confidence intervals overlap and becomes `Fail` only when the inversion is confidence-separated. The versioned certification policy requires at least three constraint-passing rosters per regular family and progression cohort, 25 common-seed trials per roster, 100 optimized holdout trials, interval width no greater than 0.25, typed mechanic evidence, complete progression ordering, and certified release-profile elite evidence. Developer runs remain explicitly non-certifying at one trial per roster; release CLI runs default to the reviewed 25-trial budget. Missing samples or dependencies produce `ReviewRequired`, adequately evidenced family or progression violations produce `Failed`, and only every-family plus progression success produces `Certified`. Production floors keep their authored `RequiredSlots`; no selectable size variants or production content mutations are introduced. `--scale-probes` requests isolated 5/10/15 balanced-P75 rosters, clones the encounter definition in memory, changes only the diagnostic roster size plus explicit balance-owned multipliers, and runs the normal production World Tower combat path. The scale-probe artifact reports formula ratios, clear-rate deltas and Wilson intervals, observed failure modes, and exact added combat trials and simulated ticks. Schema 23 adds measured wall time, current-thread allocations, trial/tick throughput, process peak working set, managed-heap high-water estimates, and runtime/host context per executed floor/size batch and in aggregate. Optional CLI thresholds can classify this evidence `WithinBudget` or `OutsideBudget`; all measurements remain host-dependent and non-certifying, and reused authored evidence is not falsely re-measured. The four-run [workstation baseline v1](encounter-scale-probe-performance-baseline-v1.md) freezes a reproducible 450-trial workload and an opt-in 15 ms/trial, 10 MiB/trial, 30,000 ticks/s, 192 MiB process-peak budget. The validation run passed every batch. An actual release run with curated elite evidence remains Phase 4 acceptance work.

Schema 43 advances Phase 2 to build-capability profiler algorithm v3 and PvE benchmark v2. Compact telemetry samples average initial-friendly health deficit once per completed tick and exposes it only as AttritionResilience supporting evidence. Three random-build populations show uncensored spread and stable inverse association with mitigation, but the independent seed-4243 holdout association with first-death timing is weak (`−0.12` by trial and `−0.34` by roster). The metric does not change scoring, normalization, party selection, or the still-insufficient DistributedAttrition family contract.

A preregistered burden-plus-mitigation conjunction also fails replication. Seed 9013 passes with roster-level Spearman `+0.54` and a `+32.0%` high-versus-low exposure timing advantage, but the unchanged seed-11027 replication reverses to `−0.36` and `−0.9%`. Seed 6311 is excluded only because its generated family population is incomplete. No threshold is revised, and no attrition-resilient cohort is formed from the saturated probe.

A preregistered Regeneration damage-survival conjunction fails earlier. Seed 12041 is excluded as a protocol dry run when Floor 7 selects E6 instead of the assumed E5 source pool. The corrected profile-relative seed-14281 discovery has 17 E6 source builds, but its eight above-median sustained-damage builds and eight below-median health-deficit builds have zero overlap. Twelve full-fault rosters consequently have one exposure value, failing the frozen three-value prerequisite before net-damage and clear-outcome gates can be interpreted. No replication is authorized and no named family is created.

A preregistered three-stage progression review then tests the fixed candidate boundaries Floors 1–4 E4, 5–7 E5, and 8–10 E6 without applying them. E5 materially changes available conclusions on every seed, but unanimous adoption prerequisites fail: seed 12041 has non-monotonic P75 power and no neutral Floor-4 reference, and seed 16633 has no neutral Floor-3 reference. Only seed 14281 is complete and power-monotonic. This supports keeping E5 in the author review while rejecting an automatic mapping change from the current generated evidence.

### Phase 0 — Correct and freeze the shared simulation boundary

Objective: ensure offline Tower combat is behaviorally equivalent to live Tower combat before adding measurements.

Affected areas:

- `WorldTowerContentAnalyzer` trial slot construction;
- `WorldTowerCombatRuntimeFactory` tests;
- `BalanceRunnerTests` and World Tower integration tests;
- composite content fingerprint/version metadata.

Work:

- assign `PartyNumber` in offline 10/15-player trials exactly as live `WorldTowerService` does;
- extract exact-roster execution from `WorldTowerContentAnalyzer` without changing production combat;
- freeze existing PvE scoring v1 and common-seed confidence contracts;
- extend fingerprints to include exact scenario definitions, canonical gear/equipment budget, creature/tower definitions and scaling, combat rules, and engine version.

Migration: none.

Verification: a known live-style roster and offline roster produce identical runtime slots, seed, outcome, and entity totals; deterministic replay remains byte-equivalent where contracts require it.

Expected outcome: trustworthy shared execution and safe future caching.

### Phase 1 — Compact encounter telemetry and failure observations

Objective: explain current Tower wins/wipes without changing difficulty.

Affected areas:

- generic combat telemetry/result contracts in Core/Services;
- `FastCombatEngine`, `CombatEngineExecutor`, and stats aggregation;
- extracted World Tower encounter executor;
- report writer and CLI.

Work:

- add compact target, control/cleanse, summon, death-timing, and healing-over-time metrics;
- add simulation checkpoints with event-log capture disabled;
- implement versioned terminal-failure, observed-mode, contributing-condition, and optional authoritative-cause contracts with evidence;
- report P10/P50/P90 duration, boss effective DPS, primary-target intake, party sustain, and observed-failure distributions.

Migration: none for offline reports. Do not change persisted live battle reports yet.

Tests: evidence precedence, ambiguous multi-factor losses, `Other`, timeout, primary collapse, attrition, boss sustain, add accumulation, authoritative mechanic signals, deterministic evidence, and telemetry reconciliation.

Performance: benchmark telemetry off/on; compact observation must avoid full event-log memory in release-size runs.

Expected outcome: existing clear-rate failures become actionable.

### Phase 2 — Versioned diagnostic capability profiles

Objective: produce reusable empirical fingerprints for finalized builds.

Affected areas:

- new capability scenario catalog/profiler in the balance tool;
- shared helpers extracted from `PveBenchmarkRunner`;
- report writer, CLI options, output artifacts, and cache.

Work:

- reuse current five benchmark executions and raw metrics without changing their scores;
- add party-support and wave measurements for the six universal dimensions;
- add typed, physical-unit mechanic diagnostics for priority objectives, control types, stagger, cleanse, and dispel without forcing them into universal scalar scores;
- output the six-dimension character model with raw units, normalized cohort values, seed variance, and cache keys;
- invalidate on content/build/scenario/engine/ruleset changes.

Migration: none; offline artifact/cache only.

Tests: known synthetic specialists rank correctly by the relevant universal dimension; mechanic types remain distinct; common-seed replay; cache hit/miss and invalidation; no change to existing benchmark/elite confidence output.

Performance: record scenario cost and cache reuse rate. Run missing scenarios incrementally rather than re-profiling unchanged builds.

Expected outcome: the large build space becomes a small measured capability space without formula duplication.

### Phase 3 — Deterministic party families

Objective: cheaply construct and pre-classify distributions of plausible parties, then evaluate retained samples against the real encounter.

Affected areas:

- new party builder/family configuration;
- representative and elite cohort adapters;
- encounter-specific optimizer reporting.

Work:

- normalize profiles by progression band;
- generate intended, specialist, awkward, poor, and extreme party families at each authored player count, with the same builder available to optional 5/10/15 probes;
- add an authored party-family response profile and per-disposition envelopes to each certifiable encounter;
- preserve exact signatures/seeds and quantitative family constraints;
- reuse elite complete-party search for optimized ceilings;
- stop using homogeneous-party candidate results as evidence for ordinary composition distributions;
- reserve dedicated party diagnostic scenarios for frozen sentinel parties and focused investigations, not every generated roster.

Migration: none.

Tests: constraint satisfaction, determinism, signature uniqueness, no class-label dependency, progression-band separation, family-response disposition mapping, and bounded sample counts.

Performance: measure party construction, optional sentinel diagnostics, and boss simulation budgets separately; cache member profiles, simulate only retained parties, and never enumerate the Cartesian product.

Expected outcome: clear rates answer "what percentage of appropriately constructed parties clear?"

### Phase 4 — Authored-size certification and balance-only scaling probes

Objective: certify each production floor at its authored `RequiredSlots` and investigate other player counts without changing production content.

Affected areas:

- World Tower analyzer and scaling validator;
- new authored-size certification and `EncounterScaleProbe` configuration/artifacts;
- balance report/CLI.

Work:

- evaluate the authored player count against party-family response profiles, progression cohorts, duration, observed failure modes, mechanic requirements, and capability deficits;
- optionally materialize isolated 5/10/15 runtime probes with temporary scaling inputs and overrides;
- produce PASS/FAIL/REVIEW for the authored size and separately labeled evidence for each optional probe;
- extend analysis to released Floors 11-15 before using the system as the template for later bands.

Migration: none. Production JSON, `TowerFloorDefinition.RequiredSlots`, rally persistence, API, and UI remain unchanged. Selectable variants would be a later gameplay feature with its own design and migration review.

Tests: authored `RequiredSlots` preservation, 5/10/15 probe isolation, no production-content mutation, live party-number semantics, formula defaults plus overrides, confidence containment, family-response envelopes, and immutable artifact persistence.

Performance: required benchmarks for all three sizes at representative sample counts, reporting wall time, allocations, tick throughput, and peak memory.

Expected outcome: a designer receives a release verdict for the authored floor and can request separate evidence-backed 5/10/15 scaling probes without creating gameplay variants.

### Phase 5 — Assisted identity-preserving calibration

Objective: propose the smallest mechanic-appropriate parameter changes.

Implementation status (2026-08-29): balance schema 24 and `EncounterCalibrator` algorithm v2 implement the first conservative assisted-calibration slice. The legacy shared health/offense search remains the diagnostic baseline. The opt-in `--assisted-calibration` path selects only offense for dominant `PrimaryTargetCollapse`/`PartyAttrition` observations and regeneration for dominant `BossSustainDominance`; all successful/too-easy evidence, mixed observations, add pressure, priority, control, cleanse, and other mechanic modes return `Review`. A supported group receives two bounded common-seed sensitivity points and paired baseline/candidate evaluation on one independently derived holdout seed. A proposal is emitted only when the candidate materially improves clear-rate error, lands inside the configured target window on holdout, and changes exactly one temporary parameter. The report returns a range around the selected grid cell, requires human approval, and never changes production content.

Subsequent reliability work through schema 42 physically validates detached Health, Offense, Guardian ability-healing/Regeneration, brood AddPressure, and Garran distributed-attrition faults without widening assisted calibration. AddPressure and distributed attrition still return `Review` because no interchangeable add-count or ability-specific distributed-damage knob exists. A neutral-reference E4/E5/E6 matrix shows that an intermediate population materially changes conclusions on Floors 3, 4, and 8, supporting review of the smallest explicit three-stage progression model. Four-dose Regeneration and DistributedAttrition panels expose physical burden and family survival shape. Censor-aware first-death survival and three complete ten-build master-seed runs reject the proposed Regeneration and DistributedAttrition family envelopes. Balance-only event attribution measures the exact injected `Slam the Gates` effect and proves monotonically increasing DPS with five-target breadth on every seed/dose; aggregate concentration remains diagnostic. Schema 41 separates diagnostic recovery from affected-family replication: all five supported diagnostics are confirmed across seeds 1337, 2029, and 8471; AddPressure's approved MultiTarget reset/normalized-uptime contract is confirmed; Regeneration and DistributedAttrition family evidence remains `InsufficientEvidence`. Schema 42 adds direct Guardian damage/s and net damage-after-self-sustain reporting, then shows that the Regeneration net-margin leader still does not identify the outcome-leading family on seed 8471. It also confirms that the existing AttritionResilience probe is saturated at 180 seconds for every audited roster and that its physical submetrics reverse direction across populations. A bounded pressure-only follow-up rejects simple pressure escalation: temporary `2.2×` and `2.6×` probes leave all 30 frozen random builds capped, while `4.0×` still caps 26/30 and produces only late 162–174 second failures. The checked-in `1.8×` pressure is retained; an uncensored continuous attrition observation is required before another cohort attempt. Population-replication policy v2 preserved these distinctions at that stage and required unanimous passes across at least three distinct enabled populations; policy v3 retains that rule and additionally requires complete, matching upstream population provenance. CleanseDemand remains unavailable behind explicit catalog, physical-capability, and roster prerequisites: the current production catalog has no player Cleanse effect and Floor 8 has no valid cleanse-specialist roster. Discrete mechanic knobs, multi-group coordinate descent, duration/family/progression constraints, and the two missing author-owned family contracts remain later Phase 5 work. See [Assisted Encounter Calibration](assisted-encounter-calibration.md) and the [Region 1 reliability audit](region-one-balance-framework-reliability-audit.md).

Schema 43 completes the attrition-observation follow-up described above but does not widen assisted calibration. Average health deficit produces continuous spread, while the burden-plus-mitigation cohort passes seed 9013 and reverses on seed 11027. The subsequent Regeneration two-axis candidate produces zero qualifiers on its corrected seed-14281 discovery population and stops without replication. Attrition calibration remains `Review`; both DistributedAttrition and Regeneration family identification remain `InsufficientEvidence`. Further empirical combinations from the same measurements are not justified without an author-defined premise or materially different observable.

The following three-population progression review does not widen calibration or change the smooth-step curve. Material E5 conclusion changes replicate 3/3, but complete matrix and monotonic-power prerequisites hold only on seed 14281. The preregistered unanimous gate therefore rejects the proposed explicit three-stage floor boundaries. Region 1 retains the smooth-step curve, and Region 2+ expansion remains blocked unless a new author-owned policy is preregistered and passes a fresh schema-46 protocol-matched panel.

Schema 44 retains the previously discarded neutral-reference search panels and renders them in the report. The two incomplete floors are both lower-bound exhaustion rather than search discontinuities: seed 12041 Floor 4 reaches `35.56%` clear at factor `0.25`, while seed 16633 Floor 3 reaches only `4.44%`; all larger tested factors are below the neutral window. The seed-12041 power inversion is independent of that boundary and comes from comparing small, compositionally different slot-profile populations generated from separate deterministic random streams. No factor bound, population generator, progression mapping, or combat verdict is changed by this diagnostic schema.

Schema 45 validates that causal diagnosis with a preregistered matched-genome benchmark. Ten E6 random genomes per population are exhaustively projected into all 15 E4 subsets, all 6 E5 subsets, and the full E6 build under the normal packages. Seeds 12041, 14281, and 16633 all have strict E4<E5<E6 population means, positive median per-genome steps, and 10/10 strict individual ladders. This confirms the prior P75 inversion was driven by different generated Essence compositions. It does not repair the two lower-bound-exhausted neutral references, change the smooth-step curve, or authorize the candidate fixed mapping.

Schema 46 records the complete upstream population protocol on every reliability artifact and upgrades population-policy compatibility to v3. Matching reliability options alone is no longer sufficient: optimizer, representative, capability, party-family, and World Tower protocol settings must also match and be present. This closes a reporting correctness gap without changing combat or selection and prevents current-default reruns from being used to overturn a differently generated preregistered progression panel.

Affected areas:

- refactored `EncounterCalibrator`;
- balance-owned parameter bounds/identity configuration;
- evaluator adapters for temporary calibration and scale-probe parameters;
- calibration trace/report.

Work:

- retain shared-factor search as a diagnostic baseline;
- add bounded parameter groups, evidence-gated coordinate selection, discrete grids, sensitivity probes, and holdout revalidation;
- enforce duration, observed-failure, progression, authored family-response, specialist, and poor-composition constraints;
- return `REVIEW` instead of selecting a knob when causal attribution remains ambiguous;
- return ranges when Monte Carlo resolution does not support a precise point recommendation.

Migration: none; recommendations remain non-mutating.

Tests: monotonic continuous search, discrete search, ambiguous attribution, sensitivity comparison, bound exhaustion, identity constraint rejection, common-random-number reuse, and no production-content write.

Expected outcome: suggestions such as "reduce primary-target damage 6-9% or lengthen the heavy-hit interval" rather than a generic HP change.

### Phase 6 — Admin encounter workspace

Objective: make reports usable without replacing the existing Essence simulator.

Affected areas:

- Admin Dashboard diagnostics API and Angular balance area;
- offline report discovery/import or explicitly triggered local balance jobs;
- visualization of cohorts, confidence, failures, and calibration traces.

Work:

- add encounter, authored-size certification, and optional scale-probe report views;
- show family response expectations/results, duration distributions, failure observations, capability deficits, and proposed parameter ranges;
- preserve JSON/CSV export and immutable run identifiers;
- keep long release certification out of synchronous HTTP request paths.

Migration: none unless server-side report retention is explicitly chosen.

Tests: DTO mapping, report compatibility, UI category rendering, and cancellation/error behavior.

Expected outcome: one developer-facing workspace for build and encounter questions, with the two layers visibly separate.

### Phase 7 — Player guidance and live-meta inputs

Objective: expose broad encounter demands/readiness and optionally learn from actual build distributions.

Affected areas:

- World Tower content/DTOs/service and Angular UI;
- character snapshot/profile cache service;
- optional anonymized aggregate ingestion and retention policy.

Work:

- ship corrected Combat Rating language and static certified encounter demands first;
- later cache exact-build profiles asynchronously and show broad party readiness;
- accept curated/live distributions as a separate cohort source with explicit content version and privacy rules;
- never expose exact win probability.

Migration: likely required only for production capability-profile persistence/status if a separate durable cache is chosen. Static demands in JSON require no EF migration.

Tests: stale/missing profile behavior, broad category thresholds, snapshot invalidation on gear/Essence/progression changes, privacy/retention rules, and UI accessibility.

Expected outcome: players understand both progression readiness and encounter fit without the simulator becoming an oracle.

## Testing and regression policy

### Unit tests

- capability raw-metric calculations and normalization;
- party-family constraints, diversity, and deterministic signatures;
- failure-observation precedence, confidence, contributing conditions, and authoritative-cause evidence;
- party-family response-profile evaluation;
- balance-only scale formulas and overrides;
- calibration objective/identity constraints;
- CR contract and any category-label changes;
- composite fingerprint and cache invalidation.

### Integration tests

- every diagnostic scenario through the real production engine;
- exact offline/live World Tower party-number equivalence;
- exact finalized build materialization including equipment and Essences;
- deterministic multiplayer simulations with summons, threat, party targeting, stagger, control, and cleanse;
- authored-size execution plus isolated 5/10/15 balance probes of the same encounter definition;
- no production content or database mutation from analysis/calibration.

### Statistical regression tests

Freeze a small known build cohort, sentinel party cohort, scenario catalog, authored encounter/probe definition, party-family response profile, and seed panel. Assert exact replay for deterministic artifacts and policy bands—not fragile equality to a single noisy clear rate—when algorithm/content versions have not changed. Require an intentional version bump and reviewed fixture update when semantics change.

Preserve existing benchmark-confidence tests unchanged. Encounter-confidence tests should use common random numbers for comparisons and independent holdout seeds for acceptance.

### Performance tests

Measure 5-, 10-, and 15-player runs separately at representative combat lengths and sample counts. Record:

- combats and simulated ticks per second;
- wall-clock time by stage;
- allocation and peak-memory estimates;
- event-log-off versus compact-telemetry cost;
- cache hit rate and avoided scenario executions;
- scaling with summons/adds and party-targeted effects;
- deterministic parallel execution equivalence.

Outer encounter trials can be parallelized because they are independent, but report ordering and reductions must remain stable. Avoid nested unbounded parallelism with the already parallel Ability Balance Simulator. Give the runner one shared maximum-degree setting and deterministic result ordering.

## Certification report contract

A useful final artifact should report, per encounter and authored player count, with optional probe sections:

```text
Progression cohort and Combat Rating range
Exact content/scenario/engine fingerprints and seed panel

Clear rate + confidence by party family
Authored family disposition/envelope and whether the observed viability shape matches
P10/P50/P90 fight duration
Terminal failure distribution
Primary observed failure modes and contributing conditions
Authoritative mechanic causes where explicitly emitted
Capability distribution of winners and losers
Most influential measured deficits

Undergeared / intended / overgeared ordering
Optimized/extreme and curated/live results
Specialist or build outliers relative to the authored family response profile

Identity constraints and whether they passed
Parameter sensitivity and bounded tuning suggestions
Certification verdict and blocking reasons
```

`PASS` applies to the production floor at its authored `RequiredSlots` and requires every configured family disposition and mechanic policy to pass. A balance-only scale probe reports its own non-release verdict unless policy explicitly promotes it. `FAIL` must diagnose the violated envelope and evidence. `REVIEW` is appropriate when confidence is insufficient, causal attribution is ambiguous, a search bound is exhausted, or elite/live evidence is missing.

## Recommended first implementation slice

The best first slice is Phase 0 plus Phase 1, applied only to current Region 1 floors:

1. correct offline party numbers;
2. extract exact-roster encounter execution;
3. add compact death timing, target attention, summon/add, boss sustain, and timeout telemetry;
4. classify terminal failures, observed modes, contributing conditions, and explicit mechanic causes with evidence;
5. extend the current World Tower report with duration percentiles and failure-observation distributions.

That slice immediately improves every existing calibration and certification report, has no migration or player-facing risk, and creates the evidence needed to decide the exact capability scenarios and calibratable parameter groups. Capability profiling should follow without altering the existing five-scenario build score or its common-seed confidence work.
