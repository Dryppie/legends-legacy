# Current Balancing Functionality

Last reconciled with the implementation: **2026-08-30**  
Current combined report schema: **48**

This document describes the balancing functionality that currently exists in the LegendsLegacy repository. It is an implementation reference, not a future design proposal. Historical measured examples are identified as such and must not be treated as permanent balance targets.

## 1. Executive summary

LegendsLegacy has one offline, deterministic balance pipeline that connects production content and production combat behavior to build analysis, encounter diagnosis, calibration recommendations, stress testing, and immutable reports.

The pipeline currently supports:

- deterministic Region 1 gear anchors;
- legal E4, E5, and E6 Essence build generation;
- five production-engine PvE benchmark scenarios;
- Combat Rating predictive-health analysis;
- diversity-aware Essence build optimization;
- generated-population P50, P75, and P90 representative libraries;
- six-dimensional build capability profiling plus typed mechanic measurements;
- Essence usage, pairing, and conditional-performance diagnostics;
- measured Region 1 power anchors and a ten-floor progression curve;
- authored World Tower simulation at each floor's production `RequiredSlots`;
- physical combat telemetry, including Guardian passive regeneration, ability healing, and total self-sustain, plus evidence-based failure observations;
- bounded shared health/offense calibration recommendations;
- opt-in, identity-preserving single-parameter assisted calibration;
- encounter-specific build optimization and hard-counter/cheese diagnostics;
- multi-restart P95/P99 elite search, local challenges, party search, holdouts, curated-player comparison, and experimental audit modes;
- independent Region 1 scaling holdout validation;
- authored encounter-specific party-family response profiles and progression-cohort evaluation;
- versioned Floor 1 and Floor 7 progression policies with simulation-free hard-constraint evaluation;
- opt-in constrained Floor 1 and Floor 7 health/offense/ability-healing search with independent holdouts and unapplied proposed patches;
- opt-in 5/10/15-player balance-only scale probes with performance measurements;
- opt-in neutral-reference Region 1 health/offense/regeneration/add-pressure/distributed-attrition fault injection, an always-emitted CleanseDemand prerequisite audit, and a diagnostic E4/E5/E6 progression-fidelity matrix;
- a simulation-free population-replication policy evaluator for comparing completed reliability snapshots; and
- a combined Markdown report plus dedicated JSON artifacts in mutable `latest` and immutable run history.

The system does **not** automatically edit production content. It does not make 5/10/15-player variants into selectable gameplay modes, apply database migrations, update player state, deploy services, or approve Region 1 scaling merely because a search found a plausible multiplier.

## 2. System boundary

### 2.1 What the balance runner is

The runner is an offline .NET tool at:

```text
LL/tools/LegendsLegacy.Balance
```

The repository-level entry point is:

```powershell
.\build\run-balance.ps1
```

The script runs the balance project in `Release` mode and forwards all additional arguments to the CLI.

It also supplies the compatibility marker `--full`. The current parser accepts that marker as a no-op because the implemented stages already run through the single combined pipeline; it does not select a second or broader workflow.

### 2.2 What the runner treats as authoritative

The runner reuses production construction and combat paths wherever outcome authority matters:

- production ability, status, summon, Essence, creature, creature-ability, crafting, progression, and World Tower catalogs;
- canonical character and equipment materialization;
- production tempering, attribute projection, and Combat Rating calculation;
- production Essence and ability resolution;
- `CombatPreparationPipeline`;
- World Tower Guardian scaling and authored Guardian ability profiles;
- `CombatEngineExecutor` and `FastCombatEngine`;
- production party numbering semantics; and
- production combat telemetry emitted by the engine and stats aggregation.

Synthetic PvE and capability scenarios are balance-owned test fixtures, but they execute through the production combat engine. Real encounter stages use the authored World Tower definitions and production encounter runtime.

### 2.3 What remains detached

The tool materializes detached canonical player builds. It does not read live player accounts or require database-backed inventory lookup. It does not persist simulation builds, encounter variants, or calibration candidates.

### 2.4 Safety rules enforced by the implementation

- Production content is read-only during analysis.
- Calibration and scale-probe floor definitions are temporary in-memory clones.
- Reports explicitly record `productionContentModified: false` where relevant.
- Scale-probe reports explicitly record `releaseEligible: false`.
- Authored floor `RequiredSlots` remain unchanged.
- Suggestions require developer review.
- Exhausted or ambiguous searches request review instead of widening bounds or selecting an unsupported knob automatically.

## 3. End-to-end pipeline

The current `ProductionBalanceRunner` executes these stages in one request:

| Order | Stage | Main output | Role |
| ---: | --- | --- | --- |
| 1 | Content load and smoke combat | Run metadata and smoke result | Validate production catalog/combat composition |
| 2 | Region 1 Gear Packages | `gear-packages.json` | Establish deterministic equipment anchors |
| 3 | Random Essence builds | `essence-builds.json` | Produce legal unbiased E4/E5/E6 baseline samples |
| 4 | PvE benchmark suite | `benchmarks.json` | Measure complete-build PvE behavior |
| 5 | Combat Rating health | `combat-rating.json` | Test whether CR predicts measured performance |
| 6 | Generic Essence optimizer | `optimizer.json` | Search stronger legal builds with diversity pressure |
| 7 | Representative library | `representative-builds.json` | Freeze P50/P75/P90 cohorts from all evaluated candidates |
| 8 | Build capability profiler | `build-capabilities.json` | Measure six universal dimensions and typed mechanics |
| 9 | Essence meta analysis | `essence-meta-analysis.json` | Diagnose usage, conditional performance, and pair signals |
| 10 | Power anchors | `power-anchors.json` | Measure Region 1 endpoint build power |
| 11 | Progression band | `progression-bands.json` | Interpolate Floor 1–10 target benchmark power |
| 12 | World Tower analysis | `world-tower-analysis.json` | Simulate authored encounters against selected P75 cohorts |
| 13 | Encounter calibration | `encounter-calibration.json` | Search bounded shared factors and optionally propose evidence-gated single-parameter changes |
| 14 | Encounter-specific optimization | `encounter-specific-optimization.json` | Find boss specialists and hard-counter/cheese risks |
| 15 | Elite build certification | `elite-build-certification.json` | Establish P95/P99, local, party, holdout, and curated-player evidence |
| 16 | Scaling validation | `scaling-validation.json` | Revalidate shared calibration on independent seeds |
| 17 | Party-family construction | `party-families.json` | Build authored-size composition families and P50/P75/P90 cohorts |
| 18 | Party-family encounter evaluation | `party-family-evaluation.json` | Test the intended shape of encounter viability |
| 19 | Floor-to-progression policy evaluation | `floor-progression-policy-evaluation.json` | Resolve frozen primary/guardrail cohorts and evaluate authored hard constraints without search |
| 20 | Automatic floor-to-progression calibration | `automatic-floor-progression-calibration.json` | Optionally search one policy-approved continuous knob and hold out the selected candidate |
| 21 | Encounter scale probes | `encounter-scale-probes.json` | Optionally test hypothetical 5/10/15-player scaling and performance |
| 22 | Region 1 reliability study | `region-one-reliability-study.json` | Optionally recover supported faults on neutral temporary references and always audit CleanseDemand prerequisites |
| 23 | Report persistence | `summary.md` and `summary.json` | Publish combined latest and immutable evidence |

The ordering matters. For example, encounter calibration consumes the immutable authored-content baseline, elite certification consumes the generic population and calibration, and party-family construction can use elite complete-party results without replacing ordinary P75 progression.

## 4. Detailed functionality

### 4.1 Production-content smoke test

Every run:

- loads the production combat and Essence catalogs;
- excludes the training Essence from the usable production Essence set;
- requires at least two usable Essences;
- runs one deterministic 1v1 production simulator battle using the first two stable Essence IDs; and
- records the seed, result, duration, damage exchange, catalog counts, simulator version, combat-engine assembly version, optional Git commit, UTC timestamp, and run ID.

This is a composition and reproducibility smoke check. It is not a balance verdict.

### 4.2 Region 1 Gear Packages

Two deterministic equipment-only anchors are built through production crafting and equipment rules:

| Progression anchor | Package | Contents |
| --- | --- | --- |
| Region 1 Floor 1 | `T1_Rare_Exceptional_Balanced` | Tier 1, Rare, Exceptional, Balanced; seven canonical combat slots |
| Region 1 Floor 10 | `T1_Epic_Exceptional_Balanced` | Tier 1, Epic, Exceptional, Balanced; seven canonical combat slots |

Gear Packages contain no Essences. This keeps equipment power and Essence-kit performance separable.

Each snapshot includes the exact package definition, character level, items, recipes, modifiers, projected attributes, raw/displayed CR breakdown, and construction algorithm versions exposed by the production systems.

### 4.3 Legal random Essence build generation

The default CLI produces ten builds for each profile:

| Profile | Essence slots | Character level | Reference gear |
| --- | ---: | ---: | --- |
| `E4_RANDOM` | 4 | 30 | Region 1 start package |
| `E5_RANDOM` | 5 | 40 | Region 1 start package |
| `E6_RANDOM` | 6 | 50 | Region 1 end package |

The generator enforces:

- every selected Essence exists in production content;
- no duplicate Essence definition in a build;
- at most one Essence variant from each source monster;
- sufficient character-level slot unlocks; and
- unique, deterministic build signatures within a profile.

Builds are complete canonical characters with production equipment, attributes, Essence tags, and Combat Rating. This stage intentionally performs no optimization.

### 4.4 Five-scenario PvE benchmark suite

PvE benchmark scoring version 2 evaluates every build through five deterministic synthetic scenarios. Version 2 preserves the scoring formulas and adds average initial-friendly health-deficit ratio sampled once per completed combat tick:

| Scenario | Limit | Primary measurement |
| --- | ---: | --- |
| `pve.short-single-target` | 300 ticks | Burst and opening pressure |
| `pve.sustained-single-target` | 1,200 ticks | Sustained damage, ramp, and long cooldown value |
| `pve.high-incoming-damage` | 600 ticks | Focus survival, mitigation, healing, and shielding |
| `pve.three-targets` | 600 ticks | AoE, cleave, and target switching |
| `pve.attrition` | 1,800 ticks | Long-duration sustain, defense, and resource efficiency |

Each component retains outcome, duration, objective progress, damage dealt/taken, enemies defeated, survival, remaining health, healing, shielding, status/mechanic observations, and its 0–100 score.

The aggregate Benchmark Performance Score is the equal-weight mean of the five component scores. Component-specific weights convert raw scenario results into each component score; the raw metrics remain available so the aggregate cannot hide a severe weakness.

The legacy benchmark seed includes the build ID and scenario ID. It is deterministic, but directly competing builds do not receive identical random streams. The elite analyzer's optional confidence audit measures the consequences of this limitation with common seed panels; no replacement robust objective has been promoted.

### 4.5 Combat Rating predictive-health analysis

The CR analyzer compares displayed/raw CR with aggregate PvE benchmark performance. It reports:

- Spearman rank correlation with tie handling;
- ordinary least-squares slope/intercept and R²;
- mean absolute error and root mean square error;
- fixed occupied ten-point displayed-CR bands;
- band P10, median, P90, variance, standard deviation, minimum, and maximum;
- per-build predictions and residuals; and
- high- and low-performing CR outliers.

The default outlier rule requires both an absolute residual of at least five score points and at least two residual standard deviations.

The overall classifications are `Excellent`, `Good`, `Concerning`, and `Poor`, based on combined correlation, R², error, and within-band spread thresholds. This is diagnostic; it does not rewrite CR.

### 4.6 Generic Essence optimizer

Optimizer algorithm version 6 performs deterministic, bounded, diversity-aware search for each slot profile.

The default search:

- begins with the random build population;
- fills to 20 candidates per profile when necessary;
- runs four generations;
- preserves five elites;
- mutates Essence slots at a 0.25 rate;
- injects 10% fresh random candidates;
- penalizes maximum overlap with already selected candidates by up to eight score points; and
- retains ten diversity-selected candidates per profile in the compact report.

Fitness is the aggregate five-scenario PvE score. Search mutates only legal Essence selection. It does not change gear, level, CR rules, benchmark definitions, or production content.

Every unique evaluated candidate remains available transiently to the representative, meta, encounter-specific, and elite stages even though the full search population is not written into `optimizer.json`. Capability profiling is intentionally narrower: it profiles the original generated builds plus every source build selected into the representative library.

### 4.7 P50/P75/P90 representative library

Representative library algorithm version 1 uses every unique generic optimizer candidate evaluated during the run, not only the final generation.

It creates nine profiles:

```text
E4_P50  E4_P75  E4_P90
E5_P50  E5_P75  E5_P90
E6_P50  E6_P75  E6_P90
```

For each profile it:

1. interpolates the target score within the generated search population;
2. builds a bounded window around that target;
3. selects the closest candidate first; and
4. fills the configured cohort with a target-distance plus Essence-overlap diversity objective.

The default retains ten builds per profile. These percentiles describe the generated balance population, not actual player percentiles or the complete legal Essence space.

### 4.8 Build capability profiling

Build capability profiler algorithm version 2 measures six universal dimensions:

| Dimension | Main evidence |
| --- | --- |
| `SingleTargetBurst` | Short single-target damage behavior |
| `SingleTargetSustained` | Sustained single-target behavior |
| `MultiTarget` | Three-target benchmark plus staged wave-response probe |
| `FocusSurvivability` | High-incoming-damage survival without folding sustain into the label |
| `AttritionResilience` | Attrition survival, mitigation, sustain, and diagnostic average health deficit |
| `PartySustain` | Dedicated ally-support probe |

Each dimension records:

- a physical raw value and unit;
- supporting metrics;
- a profile-relative percentile score from 0–100; and
- seed minimum, maximum, and standard deviation where the support/wave seed panel applies.

Mechanic capabilities are deliberately not compressed into universal scalar dimensions. The profiler separately records:

- cleanses and dispels, including rates per 15 seconds;
- stun, freeze, silence, and slow applications; and
- stagger contribution.

The CLI uses a persistent probe cache at:

```text
balance-output/cache/build-capability-probes.v1.json
```

Cache keys include content/build/scenario/engine evidence. Capability seed count defaults to one and can be increased to 32.

Capability profiles help construct and pre-classify parties cheaply. They do not replace actual encounter simulation as the authoritative party test.

### 4.9 Essence meta analysis

Essence meta analyzer algorithm version 2 uses the complete unique generic optimizer population.

For every production Essence it reports:

- total appearances and overall usage;
- P50/P75/P90/P95/P99 upper-cohort usage;
- mean build performance when present and absent;
- conditional performance difference;
- common partners; and
- complementary simulator evidence.

For every unordered pair with at least three appearances it calculates:

```text
expected pair score = mean(A) + mean(B) - population mean
synergy delta       = mean(A+B) - expected pair score
```

Default warnings are:

| Warning | Default rule |
| --- | --- |
| Potentially mandatory | P95 usage at least 80% |
| Underused | Overall usage at most 2% |
| Suspicious synergy | Absolute pair delta at least 5 points |

Pair evidence is correlational, not causal. Warnings never automatically nerf or buff an Essence.

The complementary singleton simulator defaults to 2,000 battles. A balanced all-Essence round robin is available, but the measured 80-Essence pilot produced identical `0.5000` scores and therefore failed the discrimination gate. The analyzer reports `NoDiscrimination` rather than presenting uniform scores as healthy evidence. No factorial pair-balance conclusion is authorized from that endpoint.

### 4.10 Region 1 power anchors

Power anchor analyzer algorithm version 1 defines:

| Anchor | Gear | Representative profile |
| --- | --- | --- |
| `WorldTower.Region1.Start` | Region 1 start package | `E4_P75` |
| `WorldTower.Region1.End` | Region 1 end package | `E6_P75` |

Anchor benchmark power is the mean aggregate PvE score of the retained representative cohort. The analyzer also reports power range, population variance/standard deviation, mean component scores, and raw/displayed CR distribution.

CR remains a diagnostic label. Measured benchmark performance defines the power anchor.

### 4.11 Region 1 progression band

Progression band builder algorithm version 1 interpolates target benchmark power for Floors 1–10 between the two measured anchors.

Supported curves are:

| CLI value | Weight at normalized position `t` |
| --- | --- |
| `linear` | `t` |
| `ease-in` | `t²` |
| `ease-out` | `1 - (1 - t)²` |
| `smooth-step` | `3t² - 2t³` |

`smooth-step` is the default. No intermediate character builds are authored or persisted; the curve contains target power only.

### 4.12 Authored World Tower analysis

World Tower analyzer algorithm version 8 evaluates every Region 1 floor using:

- the authored production floor definition;
- the authored Guardian and ability-profile identity;
- the authored 5-, 10-, or 15-player `RequiredSlots`;
- the P75 profile whose measured mean power is closest to the floor target;
- deterministic varied parties from that representative cohort; and
- the production 6,000-tick encounter path.

The default performs ten trials per floor. For each trial the report retains:

- party-selection and combat seeds;
- exact build IDs and party numbers;
- outcome and duration;
- friendly deaths and surviving-health ratio;
- mean-player and team CR;
- Guardian remaining-health ratio;
- hostile DPS and primary-target incoming damage;
- party sustain per second;
- first friendly death;
- peak/final hostile and summon pressure;
- cleanse, dispel, and action-denial observations;
- Guardian regeneration checkpoints; and
- typed failure diagnostics.

The default clear-rate intent is 65% with a ±10 percentage-point window:

| Clear rate | Classification |
| --- | --- |
| Below 55% | `TooHard` |
| 55%–75% | `OnTarget` |
| Above 75% | `TooEasy` |

Recommended display CR is interpolated from measured anchor CR using the same progression weight. It does not drive encounter scaling.

### 4.13 Failure telemetry and diagnostics

The combat path emits compact telemetry without requiring full event logs, including active combatant/summon peaks and final counts. Entity stats also expose damage, healing, shielding, regeneration, targeting attention, death timing, mechanic applications, and denied-action evidence used by offline analysis.

World Tower diagnostics deliberately separate:

| Field | Meaning |
| --- | --- |
| Terminal failure | How the trial ended: `PartyDefeated`, `Timeout`, or `Other` |
| Primary observed failure mode | Strongest evidence-based observation |
| Contributing conditions | Additional observations that may have contributed |
| Authoritative mechanic cause | Optional explicit cause emitted by encounter logic |

Observed failure modes are:

```text
PrimaryTargetCollapse
PartyAttrition
BossSustainDominance
AddPressure
PriorityObjectiveUnmet
ControlWindowUnmet
CleanseDemandUnmet
Other
```

Every diagnostic retains a confidence value, rule version, physical evidence metrics, thresholds where applicable, and relevant entity IDs. An observed mode is not automatically described as a proven cause.

### 4.14 Shared-factor encounter calibration

Encounter calibrator algorithm version 2 retains the original shared-factor search as the main downstream calibration baseline.

For each floor it temporarily applies one factor equally to:

```text
authored guardian health
authored guardian offense
```

The default bounds are 0.25–2.00 with ten binary-search iterations. The search reuses the baseline party cohort and common random conditions. Results are:

- `AlreadyOnTarget`;
- `Converged`;
- `BestEffort`;
- `LowerBoundExhausted`; or
- `UpperBoundExhausted`.

Every evaluated factor and combat summary is retained. Exhausted-bound results do not recommend a write. Converged/best-effort values are still recommendations requiring developer approval.

Existing encounter-specific optimization, elite analysis, and scaling validation continue to consume this shared health/offense baseline. Assisted proposals do not silently replace it.

### 4.15 Assisted identity-preserving calibration

Assisted calibration is disabled by default and enabled with:

```text
--assisted-calibration
```

It is a conservative layer beside the shared-factor baseline. The current evidence mapping is:

| Dominant observed failure | Supported temporary group |
| --- | --- |
| `PrimaryTargetCollapse` | Guardian offense |
| `PartyAttrition` | Guardian offense |
| `BossSustainDominance` | Guardian regeneration |

The dominant mode must represent at least 60% of non-success observed failures. Mixed evidence, too-easy results, add pressure, priority/control/cleanse failures, and other unsupported modes return `Review` without selecting a knob.

For a supported hard floor the stage:

1. probes factors 0.85 and 0.70 for exactly one parameter group on the original common seed;
2. requires at least a five-point improvement in absolute clear-rate error;
3. derives an independent deterministic holdout seed;
4. compares factor 1.0 and the candidate on that same holdout seed;
5. requires material paired improvement and an on-target holdout result; and
6. reports a bounded range around the selected grid cell.

Verdicts are `Disabled`, `KeepAuthored`, `Proposal`, or `Review`. All proposals require human approval. The temporary evaluator supports health, offense, defense, resistance, and regeneration factors, but the current evidence gate only selects offense or regeneration.

### 4.16 Encounter-specific build optimization

Encounter-specific optimizer algorithm version 1 evaluates every unique generic optimizer candidate with the matching slot count against each calibrated Guardian.

The default uses three trials per candidate, retains five diversity-aware specialists, and then re-simulates the mixed retained party using the normal World Tower trial count.

Candidate score is:

```text
clear rate × 100
+ remaining-health ratio × 10
- friendly deaths × 2
- normalized duration × 5
```

Findings are:

| Finding | Rule |
| --- | --- |
| `HardCounter` | Specialist clear at least 80% and at least 25 points above generic P75 |
| `CheeseRisk` | Clear at least 90%, advantage at least 25 points, generic PvE delta at most -5, and one Essence in at least 80% of retained builds |
| `None` | Neither complete rule passes |

This stage diagnoses narrow strengths. It does not replace the representative library, progression curve, CR, calibration baseline, or scaling-validation verdict.

### 4.17 Elite build certification

Elite certification analyzer algorithm version 21 is a separate stress boundary from ordinary P75 progression.

It supports:

- independent E4/E5/E6 search restarts;
- adaptive generations and plateau checks;
- P95 and P99 cohort derivation;
- diversity/Pareto-style finalist retention across benchmark scenarios;
- restart-local refinement;
- legal one- and bounded/complete two-Essence substitution challenges;
- cross-strategy agreement checks;
- complete-party genome optimization against real encounters;
- deterministic multi-seed encounter holdouts;
- generic P75 versus P95, P99, specialized-party, and curated-party comparisons;
- content and policy fingerprints;
- validated curated top-player fixtures; and
- explicit verdicts that prevent missing evidence from becoming certification.

Possible verdicts include:

```text
CertifiedElite
DeveloperProfileOnly
SearchUnstable
LocalImprovementFound
ScenarioCoverageFailure
PartyOptimizationRequired
HumanBuildOutperformed
InsufficientPlayerEvidence
```

The default developer profile uses three restarts, population 64, 12–24 generations, eight elites, six finalists, local swap depth two, four holdout seeds × 25 simulations, and 2,000 party genomes per floor. Developer runs cannot produce release certification.

The release profile increases this to eight restarts, population 256, 60–100 generations, 32 elites, 12 finalists, complete finalist two-swap search, eight holdout seeds × 200 simulations, and 25,000 party genomes per floor.

The checked-in curated fixture is intentionally empty. Therefore current evidence cannot satisfy the curated-player requirement, and no current Region 1 run is approved as `CertifiedElite`.

#### Isolated elite investigation modes

The analyzer also contains opt-in, verdict-isolated or carefully isolated research modes:

- elite-parent crossover;
- coordinated three/four-gene mutation;
- persistent explorer archive;
- stratified portfolio candidates;
- quality-diversity scenario islands;
- mechanic-archetype islands;
- restart valley beam search and optional prefiltering;
- minimum-substitution bridge audit;
- descriptor separability/collision audit; and
- nested common-seed benchmark-confidence audit.

These controls exist to measure search behavior. They are disabled by default, have mutual-exclusion validation where experiments would contaminate one another, and are not automatically promoted into certification evidence. The current benchmark-confidence study found no statistically adequate submaximal seed panel within the target complete-search runtime, so the legacy single-seed search objective remains in use.

### 4.18 Independent Region 1 scaling validation

Scaling validation analyzer algorithm version 1 tests whether the shared-factor calibration generalizes.

Defaults per floor are:

- eight independently derived holdout seeds;
- 50 calibrated P75 trials per seed;
- 25 trials per sensitivity probe per seed;
- 400 primary holdout trials; and
- 1,800 total trials across calibrated, easier, harder, health-only, damage-only, P50, P75, and P90 evidence.

A floor is `Validated` only when:

- the full 95% Wilson interval fits inside the target clear-rate window;
- cross-seed standard deviation is at most 0.10;
- seed range is at most 0.25;
- easier/calibrated/harder shared scaling is monotonic within 0.03;
- P50/P75/P90 clear rates retain expected order within 0.03; and
- calibration did not end as best effort or at an exhausted bound.

Other verdicts are `Unstable` and `MechanicReviewRequired`. Health-only and damage-only probes are reported as sensitivity evidence but do not select a calibration knob.

The documented canonical seed-8471 evidence validated only Floor 8. That is historical evidence for the then-current content, not a permanent assertion about future runs.

### 4.19 Party-family construction

Party-family builder algorithm version 4 constructs deterministic authored-size parties from capability-profiled representative builds. Balanced, damage-heavy, defensive, single-target, and mechanic families retain focus-survivability and party-sustain coverage anchors. MultiTargetSpecialist omits those generalist anchors so its unchanged physical specialization constraints can be reached by the measured multi-target tail rather than being preloaded with opposing zero-multi builds.

Families are:

```text
IntendedBalanced
DamageHeavy
Defensive
SingleTargetSpecialist
MultiTargetSpecialist
MechanicSpecialist
AwkwardButPlausible
PoorComposition
OptimizedExtreme
```

It also builds balanced progression cohorts:

```text
LowerPowerP50
IntendedP75
UpperPowerP90
```

Every retained generated party is unique and passes the defining family constraints. The builder records its order-independent signature, deterministic selection seed, exact members and source cohort, capability cache keys, mean capability vector, relevant mechanic percentile, and constraint results. If the frozen candidate pool cannot supply the requested number, the family or progression cohort is typed `InsufficientFamilyMaterial`; invalid rosters are not retained or simulated.

Examples of construction constraints include:

- balanced parties require meaningful damage, focus survival, and party sustain;
- damage-heavy parties retain minimum survival and sustain floors;
- single-target and multi-target specialists require both a high relevant score and a positive specialization gap;
- mechanic specialists require a high typed mechanic percentile;
- awkward parties require a real weak dimension but a plausible overall floor; and
- poor parties require an essential focus-survival or sustain deficit.

The optimized/extreme family comes from elite complete-party search when that evidence exists.

### 4.20 Authored party-family response shapes

Every Region 1 floor has an explicit response profile. Generic dispositions map to clear-rate envelopes:

| Disposition | Envelope |
| --- | --- |
| `Advantaged` | 75%–100% |
| `ShouldSucceed` | 55%–90% |
| `DisadvantagedButViable` | 15%–70% |
| `UsuallyFails` | 0%–35% |
| `NotApplicable` | No numeric envelope |

Default intent is:

- intended balanced: `ShouldSucceed`;
- poor composition: `UsuallyFails`;
- optimized extreme: `Advantaged`;
- ordinary specialists: `DisadvantagedButViable`; and
- mechanic specialist: `NotApplicable` unless the floor declares a reviewed mechanic response.

Current Region 1 specializations are:

| Floor | Deliberately advantaged family | Identity |
| ---: | --- | --- |
| 3 | Multi-target specialist | Brood waves |
| 5 | Multi-target specialist | Twin-pillar pressure |
| 7 | Single-target specialist | Healing-ramp pressure |
| 8 | Mechanic specialist requiring cleanse | Poison pressure |

These response shapes prevent the evaluator from treating an intended specialist advantage as an accidental bypass.

### 4.21 Party-family encounter evaluation and certification

Party-family encounter evaluator algorithm version 4 runs retained parties unchanged against authored production World Tower content. It does not apply calibration overrides.

For each party and family it reports:

- trial and clear counts;
- point clear rate, a roster-effective Wilson 95% interval, and pooled-trial Wilson as a diagnostic;
- a nested roster/seed stability grid using deterministic prefixes of the frozen run;
- duration distribution;
- deaths and remaining health;
- terminal failures;
- primary observed modes and contributing conditions; and
- authoritative mechanic causes when present.

Family verdicts are `Pass`, `Fail`, `Review`, `NotApplicable`, `Unavailable`, or `Disabled`. The roster is the primary sampling unit for envelope, progression, and certification decisions; repeated combat seeds reduce within-roster combat uncertainty but do not manufacture independent composition evidence. Point estimates inside an authored envelope can pass; authoritative confidence overlap can produce review rather than a false hard failure.

The stability grid reports every available combination of roster checkpoints 3/5/10/15 and seed checkpoints 5/10/15, plus the actual maximum when it is not already a checkpoint. It reuses trials already executed in the run and adds no combat work.

The evaluator also checks P50 ≤ P75 ≤ P90 progression shape. A point inversion within the frozen five-point tolerance or with overlapping confidence produces review; a confidence-separated inversion can fail.

Release family certification policy v1 requires:

- at least three constraint-passing parties per regular family and progression cohort;
- at least 25 common-seed trials per party;
- at least 100 optimized holdout trials;
- confidence-interval width no greater than 0.25;
- required typed mechanic evidence;
- complete progression ordering; and
- certified elite evidence.

CLI developer runs use one trial per roster and therefore report `DeveloperProfileOnly` or another non-certifying result. A release-profile run uses 25 by default but still cannot certify while elite/curated-player evidence is incomplete.

### 4.22 Floor-to-progression policy evaluation

Floor progression policy evaluator algorithm version 1 loads the versioned author-owned policy before combat begins and fingerprints its canonical JSON for report provenance. Schema 47 pilots Floor 1's general health/offense pressure policy and Floor 7's health/ability-healing policy.

The evaluator reuses evidence already produced by the same run. It resolves the authored P75 primary profile from the representative library and World Tower trials, the matching P50/P90 guardrails from party-family progression evidence, the certified P95 guardrail from elite holdouts, and required family responses from party-family evaluation. It does not change cohort selection or run additional combat.

Each floor reports cohort-resolution status; primary clear-rate, duration, median-death, and median-remaining-health constraints; undergeared, strong, and elite clear-rate guardrails; P50 ≤ P75 ≤ P90 ordering; dominant-failure identity; required family responses; allowed continuous calibration knobs and their bounds; violations; and unavailable evidence. A floor passes only when every applicable constraint is satisfied and all required evidence is present. Any violation, policy mismatch, or evidence gap returns `Review`; there is no least-bad fallback or automatic bound widening. The evaluation records `productionContentModified: false` and does not search candidates or generate a production patch.

### 4.23 Automatic Floor-to-Progression calibration pilot

Automatic floor progression calibrator algorithm version 1 is disabled by default and enabled with `--floor-progression-calibration`. It operates only on policy-enabled pilot floors and changes one typed continuous parameter group at a time on detached runtime definitions: Guardian health, Guardian offense, or Guardian ability healing.

Physical failure evidence selects offense for primary-collapse or party-attrition pressure and ability healing for boss-sustain pressure. Health is available for a duration-only violation when the primary clear-rate target already passes. If the physically supported knob is not policy-approved, the floor returns `Review` without searching a substitute parameter.

The sensitivity grid uses common seeds, stops at the nearest candidate satisfying every hard constraint, and performs bounded midpoint refinement toward factor `1.0`. Every candidate independently evaluates the authored P75 primary cohort, P50/P90 progression guardrails, certified-P95 builds, required exact party families, progression ordering, outcome targets, and dominant-failure identity. Candidate and family rosters remain frozen throughout the comparison.

The selected factor and a neutral baseline are then evaluated on an independently derived holdout seed. A patch is proposed only when the holdout candidate also satisfies every hard constraint. The artifact records every evaluated factor and rejection reason, normalized change distance, policy/content fingerprints, combat counts, and a machine-readable one-field patch. Patches require human approval, have `applied: false`, and are never written to production content by the balance command.

### 4.24 Balance-only 5/10/15-player scale probes

Encounter scale-probe analyzer algorithm version 5 is disabled by default. Its detached override also carries the analysis-only Guardian ability-healing multiplier, bounded additional-summon-copy count, and injected-copy health/power potency used by the reliability study; normal scale probes leave these controls neutral.

When enabled, it:

- keeps production floors at their authored `RequiredSlots`;
- creates isolated balanced-P75 parties at requested sizes 5, 10, and 15;
- clones the floor definition in memory;
- changes only diagnostic player count and explicitly supplied balance-owned multipliers;
- runs the normal production World Tower combat path;
- reuses compatible authored-size party-family evidence when possible; and
- reports comparisons without creating gameplay variants.

Optional overrides can temporarily vary health, offense, defense, resistance, and regeneration from 0.25–4.00 for one floor/size probe.

Each variant reports:

- party/trial counts and simulated ticks;
- clear rate and Wilson interval;
- delta from authored size;
- duration;
- health, offense, and durability formula ratios;
- terminal and observed failure distributions;
- evidence source and applied override; and
- assessment as authored baseline, within tolerance, outside tolerance, inconclusive, or unavailable.

Scale probes are always balance-only and non-release. They do not change party-family certification or encounter release verdicts.

### 4.25 Scale-probe performance instrumentation

Executed scale-probe batches measure:

- wall time;
- current-thread allocations and allocation per trial;
- working set before/after;
- process peak working set;
- managed-heap high-water estimate;
- trials per second;
- simulated ticks per second; and
- runtime, OS, architecture, processor count, GC mode, and stopwatch context.

Optional thresholds can classify a batch and suite as `WithinBudget` or `OutsideBudget`. No thresholds are configured by the normal CLI default.

The checked-in workstation baseline uses a 450-trial workload and diagnostic thresholds of 15 ms/trial, 10 MiB/trial, 30,000 ticks/s, and 192 MiB process peak. Those numbers are host-specific regression evidence, not server capacity or release policy.

### 4.26 Region 1 neutral-reference reliability study

Region One reliability-study analyzer version 15 is disabled by default. When enabled, it:

- uses only unique constraint-passing IntendedBalanced, Defensive, and SingleTargetSpecialist rosters;
- uses Floor 1 as the explicitly add-free health/offense reference, Floor 7 as the regeneration reference, and Floor 3 as the brood/add-pressure reference;
- searches temporary shared health/offense factors for an IntendedBalanced clear rate between 40% and 80%;
- deterministically refines a bracketed step transition for at most eight midpoint evaluations;
- injects exactly one 1.40× Guardian Health, Guardian Offense, detached Guardian ability-healing, or one-copy brood-summon fault using the same rosters and common seeds;
- persists full per-roster trial diagnostics and roster-primary uncertainty;
- combines dominant failure observations with paired hostile-DPS and Guardian end-health telemetry to distinguish longer health-driven exposure from increased offense;
- verifies that the injected knob reached its expected physical telemetry before judging diagnostic recovery;
- records the first additional-hostile tick and the first later tick with zero hostile summons, then reports per-family add-window clear rate and average clear duration for the Floor 3 reference and brood fault;
- counts distinct hostile-summon spawn ticks, total summons created, continuous add windows, cleared windows, active-summon ticks, wave spacing, and peak summons for the complete combat;
- runs a diagnostic `0.25`, `0.50`, `0.75`, and `1.00` detached duplicate-brood payload panel, scaling only the injected copy's health and power while preserving authored cadence and content;
- requires MultiTargetSpecialist to have the strongest add-window reset rate, retain at least a ten-point reset advantage over IntendedBalanced, increase normalized summon uptime, and remain coherent in reset rate and normalized uptime across the graded payload panel within a five-point reversal tolerance;
- runs diagnostic `0.25`, `0.50`, `0.75`, and `1.00` excess-multiplier panels for Guardian ability healing and distributed party damage, reporting per-family clear, duration, deaths, remaining health, self-sustain, non-primary damage, concentration, and party sustain;
- reports first-death event rate, observed first-death timing, and Kaplan–Meier restricted mean first-death-free ticks for every distributed-attrition dose, using the common combat limit so faster death-free victories are not scored as worse survival;
- records physical Guardian damage taken per second on every World Tower trial and reports realized damage-minus-self-sustain per second for every graded Regeneration family row;
- enables event logging only on detached distributed-fault executions, attributes damage to the exact injected `Slam the Gates` effect ID, and reports injected DPS, hits, activation waves, and peak distinct targets per wave;
- reuses each mechanic's unchanged full-strength fault at dose `1.00`; intermediate regeneration and distributed-attrition doses do not alter their verdict gates;
- retains Regeneration and DistributedAttrition family telemetry as diagnostic evidence but configures no affected-family contract until an independently replicated authored premise exists;
- adds only the configured excess share of Garran's authored all-party `Slam the Gates` damage on the detached runtime (40% at the default fault multiplier), leaving his basic attacks and production content unchanged;
- requires the distributed-attrition fault to increase non-primary friendly damage by at least 1.10×, directly attribute positive injected damage to at least two distinct targets in one wave, and produce `PartyAttrition` evidence in at least 60% of failures; highest-character damage concentration and all Defensive comparisons remain diagnostic;
- evaluates deterministic IntendedBalanced E4, E5, and E6 P75 rosters on selected Floors 3–8 at the same temporary 40–80% current-profile neutral reference, with common seeds and no authored-content changes;
- reports each P75 population's level, Essence slots, gear package, benchmark power, and physical capability P10/P50/P90, plus per-floor target gap, clear rate, roster interval, duration, deaths, remaining health, and primary failure mode;
- marks a profile comparison material when clear rate changes by at least fifteen points, median duration changes by at least ten percent, or the dominant failure mode changes, but does not treat generated P75 cohorts as player percentiles or release evidence;
- compares the expected injected parameter group with the recovered parameter group, or requires the reviewed observed-failure and physical signatures for discrete add-pressure and distributed-attrition faults;
- reports diagnostic recovery and family-contract replication separately, then derives the composite `Pass`, `Fail`, `Inconclusive`, or `Unavailable` result without modifying content or contributing directly to release certification.

Whether enabled or disabled, the reliability artifact also records the CleanseDemand prerequisites from the loaded production ability catalog, measured build-capability population, and Floor 8 party-family material. It reports catalog Cleanse/Dispel effect counts, physically observed cleansers and maximum cleanse rate, requested/retained MechanicSpecialist rosters, and whether a controlled injection has been implemented. Engine support or a percentile tied at zero cannot satisfy the prerequisite.

A fault is observable only when IntendedBalanced clear rate drops by at least ten percentage points. Diagnostic recovery additionally requires the injection to reach its expected physical signature, one observed failure mode to reach 60% dominance, and the combined evidence to recover the injected parameter group. Health recovery requires stable hostile DPS plus a material Guardian end-health increase; offense recovery requires at least a 1.10× hostile-DPS ratio. The healing-ramp experiment requires at least a 1.10× increase in total Guardian self-sustain per second and maps that physical response to the existing Regeneration calibration dimension. If the ability-healing override remains physically absent, the diagnostic is `Unavailable`, not an inferred pass or failure. A composite pass additionally requires any applicable family contract to pass; missing authored family evidence is `InsufficientEvidence` and leaves the composite result `Inconclusive`. The inverse correction is represented by the already-executed frozen neutral reference; the study does not duplicate identical combat executions.

The add-pressure experiment attaches one temporary duplicate of Morrowmaw's authored summon effect to the detached Guardian. It requires at least a 1.50× peak-add ratio and `AddPressure` evidence in at least 60% of failed trials. Its reviewed response contract is physical: MultiTargetSpecialist must have the strongest add-window reset rate, retain at least a ten-point reset advantage over IntendedBalanced, increase normalized summon uptime, and show no greater than a five-point adverse reversal in reset rate or normalized uptime as payload rises. The unchanged `1.00` full-strength fault supplies the verdict evidence and is reused rather than rerun. Potency `0.00` in the report denotes the frozen authored reference; higher doses add one duplicate per authored wave and scale only that duplicate's health and power. Absolute and relative clear ordering plus raw active ticks remain reported as diagnostics but do not control the contract because terminal clear floors and shorter victories distort them. Unresolved windows remain in the clear-rate denominator, while average first-window duration uses only resolved windows and is always presented beside the rate. A spawn wave is one or more hostile summons created on the same combat tick; an add window is a continuous period with at least one living hostile summon; normalized uptime counts a tick once regardless of summon count and divides by observed encounter duration. If the specialist population is absent, the diagnostic may match while family evidence is `InsufficientEvidence` and the composite experiment remains `Inconclusive`.

The distributed-attrition experiment targets Floor 1's authored `Slam the Gates` effect. The temporary modifier adds only the multiplier's excess damage coefficient to that all-enemy effect; cadence, targeting, Garran's basic attack, and production JSON remain fixed. Diagnostic recovery requires damage outside the attention-defined primary target to rise, direct injected-effect damage to reach multiple targets in a wave, dominant/contributing `PartyAttrition`, and an observable IntendedBalanced clear-rate drop. Concentration, Defensive relative clear, party sustain, and censor-aware survival remain reported but are not an authored family contract. Assisted calibration returns `Review` for both this fault and AddPressure because it has neither an ability-specific distributed-damage group nor an add-count group. Cleanse-demand injection remains `Unavailable` until all recorded physical prerequisites exist.

The progression-fidelity matrix is diagnostic-only and reuses the same detached encounter override path. It first evaluates the currently selected P75 population to find a neutral reference, then holds that factor fixed while changing only the generated E4/E5/E6 population. In the schema-37 ten-build seed-1337 panel, all selected Floors 3–8 retained three valid rosters per population. E5 materially changed conclusions relative to the current nearest profile on Floors 3, 4, and 8, supporting review of the smallest explicit three-stage progression model rather than ten floor-specific optimizers. The same broader population also made the Regeneration and DistributedAttrition relative family-response gates inconclusive even though their physical diagnostic recovery remained correct; therefore a single narrow-population 5/5 fault pass is not a population-robust release claim.

Schema 38 tests possible physical replacements with graded common-seed panels. The ten-build panel shows coherent Regeneration self-sustain with SingleTargetSpecialist strongest at every dose, and coherent distributed damage with Defensive highest in party sustain and average duration. The five-build replication cannot run Regeneration because Floor 7 retains only 2/3 required unique IntendedBalanced rosters; its available attrition panel gives Defensive higher sustain but shorter average duration than IntendedBalanced. The framework therefore keeps both existing family-response gates and exposes the graded rows as diagnostic evidence. No cross-population replacement envelope is currently certified.

Schema 39 adds censor-aware first-death survival and repeats the complete ten-build study on seeds 1337, 2029, and 8471. Neither candidate family envelope replicates: SingleTargetSpecialist is the strongest Regeneration family only on seed 1337, while Defensive attrition restricted-mean survival is below IntendedBalanced at every dose on seeds 2029 and 8471. Distributed non-primary DPS increases by at least 1.1491× on all three seeds, but the required concentration decrease passes only seed 1337. Aggregate concentration is therefore not a population-robust causal signature for the injected all-party effect.

Schema 40 replaces only that physical-reach proxy with direct balance-only event attribution. It requires non-primary DPS to rise by at least 1.10× and the exact injected effect to deal positive damage to at least two distinct targets in one wave. All three master seeds show monotonically rising injected DPS and five-target breadth at every dose. DistributedAttrition now passes physical reach on all populations; seeds 2029 and 8471 pass the complete fault, while seed 1337 remains `Inconclusive` on the unchanged Defensive-family response. Concentration, censor-aware survival, and all legacy family evidence remain visible diagnostics.

Schema 41 implements the approved authored-contract review. It exposes `diagnosticVerdict` and `familyContractVerdict` on each fault and prevents a missing family premise from being mistaken for either a failed diagnostic or a full pass. Regeneration drops the non-replicating SingleTarget ordering and DistributedAttrition drops the non-replicating Defensive relative-clear promise; both keep their physical diagnostic contracts and report family `InsufficientEvidence`. AddPressure retains its MultiTarget identity through strongest reset rate, material reset advantage, increased normalized uptime, and graded reset/uptime coherence. All three unchanged master populations confirm every supported diagnostic and the AddPressure family contract; Regeneration and DistributedAttrition remain composite `Inconclusive` only because their replacement family contracts have not been authored.

Schema 42 adds direct Regeneration contract evidence without changing a verdict. Guardian damage taken per second comes from the Guardian's production combat stats, and the report subtracts realized self-sustain to expose the physical net-damage margin by family and dose. The three-population follow-up rejects using either generic SingleTargetSustained capability or this margin alone as a family identity: generic capability correlations reverse on seed 8471, and the full-dose net-margin leader there is MultiTargetSpecialist while Defensive has the highest clear rate. The corresponding attrition review finds every audited roster tied at the 180-second AttritionResilience probe ceiling; prevented damage, self-sustain, and PartySustain submetrics also change predictive direction across populations. Both missing family contracts therefore remain `InsufficientEvidence`.

A bounded post-schema-42 AttritionResilience pressure audit keeps the checked-in benchmark unchanged. On the frozen seed-1337 random-build cohorts, both temporary `2.2×` and `2.6×` pressure leave all 30 builds at the 180-second ceiling. A `4.0×` upper bound still leaves 26/30 capped, with the other four surviving 162.0–174.0 seconds. Because the candidate fails to create a useful range before population replication, the runner restores and retains the authored `1.8×` pressure. Any next attrition-contract study must first validate an uncensored continuous observation within the existing dimension; this audit does not authorize a new universal capability, cohort, or threshold.

Schema 43 adds that observation without creating a new dimension or changing a score. Compact telemetry averages each initial friendly combatant's missing-health ratio once per completed tick; PvE benchmark v2 exposes it, and capability profiler v3 reports it under AttritionResilience as `average_health_deficit_ratio`. At unchanged `1.8×` pressure, seed 1337/2029/8471 random panels produce 23/26/26 distinct values among 30 builds, with ranges `0.0018–0.0151`, `0.0027–0.0150`, and `0.0040–0.0149`, despite 30/29/30 builds retaining the 180-second cap. Lower deficit aligns with higher mitigation on every population (Spearman `−0.57/−0.59/−0.66`) and higher final health in the same direction (`−0.41/−0.17/−0.37`). Sustain is positively associated (`+0.58/+0.65/+0.65`), showing reactive healing opportunity rather than contradicting the burden measure. A separate seed-4243 reliability holdout gives only weak prediction of full-fault first-death timing (`−0.12` across 180 trials; `−0.34` across 12 roster clusters), with zero clears in that panel. The observation is therefore useful diagnostic telemetry but not an independently validated cohort rule; AttritionResilience ranking and DistributedAttrition family verdicts remain unchanged.

The preregistered follow-up combines below-median health deficit with above-median prevented-damage ratio, using strict within-population E4 medians and no outcome-fitted weights. Seed 6311 is unavailable because its family population is incomplete. Seed 9013 passes the frozen discovery gates with four roster-exposure levels, Spearman `+0.54`, and `+32.0%` top-tertile first-death timing. The unchanged seed-11027 replication reverses to `−0.36` and `−0.9%`, so the candidate is rejected without threshold revision. This confirms that uncensored telemetry alone does not supply an affected-family contract; further combinations from the same probe are out of scope absent an author-defined premise or materially different observable.

The preregistered Regeneration follow-up tests a cross-family damage-survival cohort without changing the runner. A source build must be strictly above its selected P75 profile's median `SingleTargetSustained` raw value and strictly below its median `average_health_deficit_ratio`; roster exposure must then predict both full-fault Guardian net damage and clear outcome. Seed 12041 is excluded as a protocol-unavailable dry run because Floor 7 selects E6 rather than the initially assumed E5 source pool. Under the corrected profile-relative rule, seed 14281 completes 7,425 reliability combats but yields zero qualifiers among 17 E6 source builds: the eight above-median damage builds and eight below-median-deficit builds do not overlap. All 12 Regeneration rosters therefore have zero exposure, failing the frozen minimum-spread gate before outcome testing. No replication, family threshold, scoring change, or party-selection change follows.

A subsequent diagnostic review freezes a candidate three-stage progression policy—Floors 1–4 E4, Floors 5–7 E5, Floors 8–10 E6—but does not apply it. Across protocol-compatible seeds 12041, 14281, and 16633, E5 materially changes at least two available floor conclusions in every matrix. Adoption prerequisites do not replicate: only seed 14281 has all six tested floors plus monotonic P75 power; seed 12041 has non-monotonic `67.58/70.46/68.33` E4/E5/E6 power and no neutral Floor-4 reference, while seed 16633 has no neutral Floor-3 reference. Complete prerequisites therefore pass 1/3 even though E5 relevance passes 3/3. Current interpolation, population selection, gear packages, and production content remain unchanged.

Schema 44 makes the missing neutral-reference evidence inspectable. Each progression floor serializes every tested factor, trial count, IntendedBalanced clear rate, roster-cluster confidence interval, and whether it entered the frozen 40–80% window; `summary.md` renders the same panel. Seed 12041 Floor 4 stays at 0% through factor `0.30` and reaches `35.56%` at the minimum `0.25`. Seed 16633 Floor 3 reaches only `4.44%` at `0.25`. Both are lower-bound exhaustion, not failed midpoint refinement. The separate seed-12041 power inversion is a generated-population confound: each slot count uses a different deterministic random stream, and the P75 profiles select ten representatives from only 17 evaluated, compositionally different genomes. No reference bound, build generator, representative selector, progression curve, or verdict changes in schema 44.

Schema 45 isolates that power confound with a diagnostic-only matched-genome panel. For each of ten E6 random genomes it benchmarks every 4-of-6 subset, every 5-of-6 subset, and the full genome through the normal E4/E5/E6 packages with common scenario seeds. Seeds 12041, 14281, and 16633 unanimously produce strict population-mean ordering and positive median per-genome step deltas; all 30 individual ladders are strict. The earlier E5>E6 P75 mean inversion is therefore generated-population composition, not a package reversal. The panel cannot affect representative selection, progression targets, calibration, certification, or content, and it does not resolve the two missing neutral-reference floors or authorize the rejected fixed mapping.

Schema 46 adds explicit upstream population-protocol provenance to every reliability snapshot. It records all semantic settings that construct the tested cohorts and encounter baseline, including initial build count, optimizer and representative options, capability content fingerprint and probe budget, party-family options, and World Tower options. Population-policy analyzer v3 requires that descriptor to be present and identical alongside the reliability analyzer/options; missing or mismatched provenance returns `InsufficientEvidence`. This fixes the prior possibility that differently generated cohort panels could be labeled protocol-compatible merely because their reliability-study options matched. It changes no combat, population selection, threshold, or content.

Population-replication policy analyzer v3 consumes completed reliability-study snapshots without running combat or changing their verdicts. It requires at least three distinct enabled master populations, complete matching upstream population provenance, and evaluates all supplied populations. It aggregates diagnostic verdicts separately from applicable family contracts. Either layer is `Confirmed` only with unanimous passes, `PopulationSensitive` for mixed complete outcomes, `Rejected` for a complete panel containing failure or no pass, and `InsufficientEvidence` when provenance, a population, fault row, physical prerequisite, or family contract is missing. Unsupported faults remain separate and block expansion eligibility. The reviewed policy-v2 application to schema-42 evidence established all five supported diagnostics and AddPressure's family contract at 3/3; policy v3 preserves that history without pretending the pre-provenance artifacts form a new schema-46 panel. Regeneration and DistributedAttrition family contracts plus CleanseDemand remain insufficient, and expansion eligibility remains false.

This evaluator is currently a programmatic policy layer, not stage 21 of `ProductionBalanceRunner`. The normal CLI still emits one reliability artifact per master seed; it does not load several historical artifacts, persist a population-policy JSON file, or change the per-run `releaseEligible: false` field. The audit's three-population confirmation records the reviewed policy-v2 application to schema-42 artifacts; schema-43 measurement/holdout, schema-44 reporting, and schema-45 matched-genome runs are explicitly non-promoting follow-ups. Policy v3 will not reclassify those historical artifacts as a new compatible panel because they predate explicit upstream provenance. A future aggregate CLI/report entry point would be presentation plumbing, not permission to weaken the unanimous replication policy.

## 5. Configuration and CLI controls

Run help with:

```powershell
.\build\run-balance.ps1 --help
```

### 5.1 Core pipeline controls

| Option | Default | Purpose |
| --- | ---: | --- |
| `--seed` | 1337 | Root deterministic seed |
| `--build-count` | 10/profile | Random E4/E5/E6 builds |
| `--optimizer-population` | 20 | Generic candidates per profile |
| `--optimizer-generations` | 4 | Generic search generations |
| `--optimizer-elites` | 5 | Preserved generic elites |
| `--optimizer-mutation` | 0.25 | Per-slot mutation rate |
| `--optimizer-random` | 0.10 | Fresh random injection |
| `--optimizer-diversity` | 8 | Similarity penalty |
| `--optimizer-retained` | 10 | Compact retained candidates/profile |
| `--representative-count` | 10 | Builds per P50/P75/P90 profile |
| `--capability-seeds` | 1 | Common support/wave probe seeds |
| `--meta-simulator-battles` | 2,000 | Legacy complementary singleton simulator battles |
| `--meta-simulator-rounds-per-matchup` | 0 | Balanced all-Essence round robin; even, zero disables |
| `--progression-curve` | `smooth-step` | Region 1 interpolation curve |
| `--tower-simulations` | 10/floor | Authored World Tower and final specialist trials |
| `--calibration-iterations` | 10 | Shared-factor binary-search iterations |
| `--assisted-calibration` | Off | Evidence-gated single-parameter calibration |
| `--assisted-calibration-simulations` | 0 | Assisted trials/evaluation; zero inherits Tower trials |
| `--floor-progression-calibration` | Off | Run the Floor 1/7 constrained continuous-knob pilot |
| `--floor-progression-simulations` | 10 | Common-seed trials per search candidate |
| `--floor-progression-holdout-simulations` | 25 | Independent holdout trials per candidate |
| `--floor-progression-sensitivity-points` | 5 | Ordered points between authored value and approved bound |
| `--floor-progression-refinement-iterations` | 4 | Boundary refinements toward the authored value |
| `--encounter-candidate-simulations` | 3 | Trials per boss-specialist candidate |
| `--encounter-retained` | 5 | Specialized builds retained per floor |
| `--validation-seeds` | 8 | Independent scaling holdout seeds |
| `--validation-simulations` | 50 | Primary holdout trials/seed |
| `--validation-probe-simulations` | 25 | Sensitivity trials/seed |
| `--content-root` | Auto-detected | Production `API.LL` content root |
| `--output` | `balance-output` | Report root |

### 5.2 Party-family controls

| Option | Developer default | Release default | Purpose |
| --- | ---: | ---: | --- |
| `--party-family-samples` | 3 | 3 | Requested parties per family/cohort |
| `--party-family-simulations` | 1 | 25 | Common-seed encounter trials per retained roster |

The CLI enables party-family evaluation in both profiles. Direct programmatic use can disable the evaluator through its options.

### 5.3 Scale-probe controls

| Option | Default | Purpose |
| --- | ---: | --- |
| `--scale-probes` | Off | Enable isolated 5/10/15 probes |
| `--scale-probe-parties` | 1 | Balanced rosters per size |
| `--scale-probe-simulations` | 1 | Trials per probe roster |
| `--scale-probe-max-ms-per-trial` | Unset | Diagnostic wall-time ceiling |
| `--scale-probe-max-allocated-mb-per-trial` | Unset | Allocation ceiling |
| `--scale-probe-min-ticks-per-second` | Unset | Throughput floor |
| `--scale-probe-max-peak-memory-mb` | Unset | Process peak-working-set ceiling |

The CLI exposes performance thresholds but not per-floor content multiplier overrides. Overrides are available through the programmatic `EncounterScaleProbeOptions` contract.

### 5.4 Reliability-study controls

| Option | Default | Purpose |
| --- | ---: | --- |
| `--reliability-study` | Off | Enable the optional neutral-reference fault-injection study |
| `--reliability-rosters` | 3 | Exact valid rosters per tested family |
| `--reliability-simulations` | 10 | Common-seed trials per reliability roster |
| `--reliability-fault-multiplier` | 1.40 | Health, offense, ability-healing, or distributed-damage multiplier for a one-control fault |

### 5.5 Elite execution profiles

| Setting | Developer | Release |
| --- | ---: | ---: |
| Restarts | 3 | 8 |
| Population | 64 | 256 |
| Minimum generations | 12 | 60 |
| Maximum generations | 24 | 100 |
| Elites | 8 | 32 |
| Finalists/profile | 6 | 12 |
| Local swap depth | 2 | 2 |
| Two-swap limit/finalist | 250 | Complete (`0`) |
| Restart refinement passes | 6 | 12 |
| Restart refinement seeds | 4 | 8 |
| Restart two-swap limit/pass | 250 | 1,000 |
| Finalist refinement rounds | 3 | 5 |
| Holdout seeds | 4 | 8 |
| Simulations/seed | 25 | 200 |
| Party genomes/floor | 2,000 | 25,000 |

Select the profile with `--certification-profile developer|release`. `--elite-search-only` skips holdouts and party search and can never certify.

All implemented elite controls are:

```text
--certification-profile
--elite-search-only
--elite-restarts
--elite-population
--elite-generations
--elite-max-generations
--elite-elites
--elite-finalists
--elite-local-swap-depth
--elite-two-swap-limit
--elite-restart-refinement
--elite-restart-seeds
--elite-restart-two-swap-limit
--elite-finalist-refinement
--elite-holdout-seeds
--elite-simulations
--elite-party-genomes
--elite-crossover
--elite-basin-jump
--elite-explorer-archive
--elite-stratified-portfolio
--elite-quality-island
--elite-mechanic-island
--elite-valley-beam-width
--elite-valley-beam-depth
--elite-valley-budget
--elite-valley-prefilter
--elite-bridge-audit
--elite-descriptor-audit
--elite-benchmark-confidence-audit
--elite-confidence-cohort
--elite-confidence-seeds
--elite-confidence-margin
--elite-policy
--top-player-builds
```

## 6. Determinism, common seeds, and caching

The pipeline derives stable seeds from the root run seed plus stage-specific identities such as profile, build, floor, party signature, trial, candidate, and scenario.

Current comparison boundaries are:

- generic benchmarks are deterministic but use build-specific random streams;
- shared calibration reuses parties and combat seeds across factors;
- scaling validation uses independent holdout seeds and common samples within each comparison;
- party-family evaluation uses common combat seeds for retained rosters;
- assisted calibration uses common sensitivity seeds and a separate paired holdout seed;
- scale probes derive deterministic party and combat samples; and
- elite benchmark-confidence audits use explicit nested common seed panels.

The build-capability support/wave probes are cached by a SHA-256 key containing content, engine, scenario, build, and seed evidence. Generated reports retain seeds, content/policy fingerprints, and algorithm versions needed to reproduce or compare results.

## 7. Report and artifact contract

Every successful CLI run writes the same file set twice:

```text
balance-output/
├── latest/
│   ├── summary.md
│   ├── summary.json
│   └── dedicated stage JSON files
└── history/
    └── <timestamp-and-guid-run-id>/
        ├── summary.md
        ├── summary.json
        └── dedicated stage JSON files
```

`latest` is overwritten by the newest successful report. Each history directory is immutable; the writer refuses to overwrite an existing run ID.

Dedicated JSON artifacts are:

```text
gear-packages.json
essence-builds.json
benchmarks.json
build-capabilities.json
party-families.json
party-family-evaluation.json
encounter-scale-probes.json
region-one-reliability-study.json
combat-rating.json
optimizer.json
representative-builds.json
essence-meta-analysis.json
power-anchors.json
progression-bands.json
world-tower-analysis.json
encounter-calibration.json
encounter-specific-optimization.json
elite-build-certification.json
scaling-validation.json
floor-progression-policy-evaluation.json
automatic-floor-progression-calibration.json
```

JSON uses camel-case property names, indented formatting, and string enum values. `summary.json` contains the complete combined report; dedicated artifacts provide smaller stable consumption boundaries. `summary.md` is the human review surface.

Run metadata records:

- run ID and UTC creation time;
- root seed;
- balance schema version;
- simulator algorithm version;
- combat-engine assembly version;
- optional Git commit hash; and
- production catalog counts.

Generated `balance-output` is ignored by Git.

## 8. Which outputs are authoritative, diagnostic, or certifying

| Output | Current role | Can approve production content by itself? |
| --- | --- | --- |
| PvE benchmark score | Authoritative for the current synthetic benchmark objective | No |
| Capability profile | Construction/pre-classification evidence | No |
| CR health | Diagnostic model-health evidence | No |
| Essence meta warnings | Correlational investigation evidence | No |
| World Tower trials | Authoritative outcomes for the tested parties/seeds/content | No; sample and policy still matter |
| Shared calibration | Search recommendation | No |
| Assisted calibration | Human-review proposal or review verdict | No |
| Encounter-specific findings | Hard-counter/cheese diagnostics | No |
| Scaling validation | Region 1 calibration acceptance gate for generic cohorts | Only as one required gate |
| Elite certification | High-end search/party/player-evidence gate | Yes only when `CertifiedElite`, which current evidence does not reach |
| Party-family certification | Encounter viability-shape gate | Yes only when release evidence and dependencies all pass |
| Scale probes | Non-release hypothetical scaling evidence | Never |
| Performance budget | Host-dependent regression diagnostic | Never |

No report bypasses explicit developer approval and application of production changes.

## 9. Current evidence status

The implementation is substantially complete for Region 1 analysis, but the content is not release-certified by the current evidence chain.

Current blockers and qualifications are:

- the checked-in curated top-player fixture contains no builds or parties;
- elite certification therefore lacks mandatory human/player evidence;
- historical elite searches found an unstable E5 search basin under the frozen convergence tolerance;
- the common-seed benchmark audit found no smaller statistically reliable panel suitable for promotion into the main search objective;
- the balanced singleton Essence simulator endpoint showed no discrimination;
- the documented canonical Region 1 scaling run validated only Floor 8;
- developer party-family runs are intentionally non-certifying;
- release party-family certification depends on certified elite evidence;
- scale probes remain diagnostic and cannot create or certify gameplay variants; and
- schema-46 evidence confirms all five supported physical diagnostics and the AddPressure family contract, shows that Regeneration and DistributedAttrition family proxies remain insufficient, confirms the earlier P75 power inversion was a generated-population-composition confound, and prevents cross-population claims from silently mixing upstream cohort protocols; fixed three-stage progression adoption remains unsupported, and the uncensored attrition burden metric has stable internal direction but only weak holdout outcome prediction;
- assisted calibration currently supports only conservative offense/regeneration mappings and does not yet enforce complete author-owned identity constraints.

These are evidence blockers, not missing safety behavior: the system reports non-certification rather than silently approving incomplete evidence.

## 10. Known functional limitations

### 10.1 Scope limitations

- Only World Tower Region 1 Floors 1–10 have a complete progression-band and authored-response implementation.
- Additional progression bands are not implemented.
- There is no Admin Dashboard encounter-balancing workspace yet.
- No production write/apply workflow is implemented.

### 10.2 Modeling limitations

- P50/P75/P90 are percentiles of the generated search population, not live players.
- The default generic benchmark objective uses build-specific RNG streams.
- Generic build fitness is an equal-weight average of five scenarios.
- Party capability is not predicted by summing member profiles; real encounter simulation remains required.
- Party-family response profiles and constraint thresholds are currently code-owned rather than loaded from an author-editable balance policy.
- Some observed failure modes do not have authoritative encounter causes.
- The current terminal-failure enum distinguishes party defeat, timeout, and other; explicit enrage/objective failure requires encounter support.
- Encounter-specific optimization searches the already evaluated generic candidate population rather than a larger boss-specific genome space.
- Assisted calibration does not yet tune discrete add counts, cadences, heal intervals, control windows, cleanse timing, or multiple interacting parameter groups.

### 10.3 Statistical and performance limitations

- Default developer sample counts are useful for iteration but often too small for certification.
- Release elite and validation profiles are intentionally expensive.
- Scale-probe allocation metrics cover the synchronous combat thread; process peak working set covers the entire process.
- Performance budgets are host-specific and have no balance-verdict authority.
- Historical seed-8471 measurements can change when content, combat rules, algorithms, or policy change.
- The population-replication policy evaluator is not yet wired to a multi-artifact CLI or persisted aggregate report; it is consumed programmatically and covered by deterministic tests.

## 11. Recommended usage patterns

### 11.1 Fast developer diagnosis

Use the default developer profile and reduce explicitly expensive search/validation budgets only when the run is clearly labeled diagnostic. Keep the same root seed when comparing code or content changes.

### 11.2 Calibration investigation

Increase `--tower-simulations`, run shared calibration, enable assisted calibration when failure telemetry is meaningful, and inspect:

- baseline trial diagnostics;
- complete shared-factor trace;
- assisted sensitivity/holdout trace;
- encounter-specific specialists;
- independent scaling validation; and
- party-family response results.

Do not copy a suggested multiplier into production solely because the calibration search converged.

### 11.3 Release evidence

A release attempt should:

1. use `--certification-profile release`;
2. provide reviewed, content-fingerprint-compatible top-player fixtures;
3. retain the default or explicitly approved release party-family sample policy;
4. preserve independent scaling-validation holdouts;
5. review every non-pass verdict and confidence interval;
6. treat scale probes as separate non-release evidence; and
7. archive the immutable history directory used for approval.

### 11.4 Scale performance regression

Use:

```powershell
.\build\run-scale-probe-performance-baseline.ps1
```

This runs the frozen workstation diagnostic workload. Establish a separate baseline before adopting its thresholds on materially different hardware.

## 12. Algorithm and policy versions

| Component | Current version |
| --- | ---: |
| Combined balance schema | 46 |
| PvE benchmark scoring | 2 |
| Generic Essence optimizer | 6 |
| Representative library | 1 |
| Build capability profiler | 3 |
| Capability normalization | `profile-relative-percentile-v1` |
| Essence meta analyzer | 2 |
| Power anchor analyzer | 1 |
| Progression band builder | 1 |
| World Tower analyzer | 8 |
| Failure observation rules | `world-tower-failure-observation-v3` |
| Encounter calibrator | 2 |
| Encounter-specific optimizer | 1 |
| Elite certification analyzer | 21 |
| Elite certification policy | `WorldTowerEliteCertificationV1`, version 1 |
| Scaling validation analyzer | 1 |
| Party-family builder | 4 |
| Party-family evaluator | 4 |
| Party-family certification policy | `WorldTowerPartyFamilyCertificationV1`, version 1 |
| Encounter scale-probe analyzer | 5 |
| Region 1 reliability-study analyzer | 18 |
| Region 1 population-replication policy analyzer | 3 |

## 13. Primary implementation references

- Pipeline orchestration: `LL/tools/LegendsLegacy.Balance/ProductionBalanceRunner.cs`
- CLI and defaults: `LL/tools/LegendsLegacy.Balance/BalanceCommandOptions.cs`
- Dependency composition: `LL/tools/LegendsLegacy.Balance/ProductionBalanceComposition.cs`
- Report writer: `LL/tools/LegendsLegacy.Balance/BalanceReportWriter.cs`
- PvE benchmarks: `LL/tools/LegendsLegacy.Balance/PveBenchmarkRunner.cs`
- Capability profiles: `LL/tools/LegendsLegacy.Balance/BuildCapabilityProfiler.cs`
- Party construction: `LL/tools/LegendsLegacy.Balance/PartyFamilyBuilder.cs`
- Party evaluation: `LL/tools/LegendsLegacy.Balance/PartyFamilyEncounterEvaluator.cs`
- World Tower execution/analysis: `LL/tools/LegendsLegacy.Balance/WorldTowerEncounterExecutor.cs` and `WorldTowerContentAnalyzer.cs`
- Shared and assisted calibration: `LL/tools/LegendsLegacy.Balance/EncounterCalibrator.cs`
- Scale probes: `LL/tools/LegendsLegacy.Balance/EncounterScaleProbeAnalyzer.cs`
- Region 1 reliability study: `LL/tools/LegendsLegacy.Balance/RegionOneReliabilityStudyAnalyzer.cs`
- Region 1 population-replication policy: `LL/tools/LegendsLegacy.Balance/RegionOneReliabilityPopulationPolicyAnalyzer.cs`
- Elite policy: `LL/tools/LegendsLegacy.Balance/Configuration/elite-certification-policy.v1.json`
- Elite analyzer: `LL/tools/LegendsLegacy.Balance/EliteBuildCertificationAnalyzer.cs`
- Scaling validation: `LL/tools/LegendsLegacy.Balance/ScalingValidationAnalyzer.cs`
- Compact production telemetry: `LL/src/Core/Domain/Models/Combat/CompactCombatTelemetry.cs`

## 14. Related design and evidence documents

- [Automated balance system implementation plan](legendslegacy-automated-balance-system-implementation-plan.md)
- [Encounter and multiplayer balance framework](encounter-multiplayer-balance-framework-analysis.md)
- [Assisted encounter calibration](assisted-encounter-calibration.md)
- [Region 1 scaling validation](region-1-scaling-validation.md)
- [Elite build certification gate](elite-build-certification/elite-build-certification-gate.md)
- [Elite certification policy v1](elite-build-certification/elite-build-certification-policy-v1.md)
- [Elite search investigation log](elite-build-certification/elite-build-search-investigation-log.md)
- [Encounter scale-probe performance baseline](encounter-scale-probe-performance-baseline-v1.md)
- [Region 1 balance-framework reliability audit](region-one-balance-framework-reliability-audit.md)
- [Region 1 affected-family contract decision](region-one-family-contract-decision.md)
- [Automatic floor-to-progression calibration plan](automatic-floor-progression-calibration-plan.md) — Slices 1–2 implemented
