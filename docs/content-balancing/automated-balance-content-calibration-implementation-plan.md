# Automated Balance & Content Calibration System

## Implementation Plan

# 0. Remove Existing Balance/Test Infrastructure Before Implementation

Before implementing the system described in this document, first inspect the repository for the existing combat balancing, simulation, benchmark, character-generation, and automated balance-testing infrastructure.

The intention is to **replace the existing balance-testing system rather than extend it**.

Most of the current balancing/test infrastructure should be removed so that the new system can be built around a clean and coherent architecture without maintaining obsolete concepts, duplicate abstractions, or compatibility layers.

## The One Explicit Exception

The existing **Admin Dashboard Essence Simulator must remain functional**.

This is the simulator exposed through the admin interface that is currently used to run Essence-focused simulations such as 1v1 and 3v3 comparisons and inspect Essence win rates.

Do not remove this feature.

Do not unnecessarily redesign or rewrite it as part of this cleanup unless changes are required to preserve functionality after obsolete infrastructure is removed.

The Admin Dashboard Essence Simulator may continue to reuse:

- the production combat engine;
- shared combat simulation primitives;
- character/combat snapshot creation;
- Essence definitions;
- stat calculations;
- other genuinely reusable low-level infrastructure.

The goal is to preserve the feature, not necessarily every implementation detail currently underneath it.

---

## Cleanup Objective

Remove obsolete infrastructure related to things such as:

- previous automated balance runners;
- old benchmark systems;
- old generated-character systems;
- previous content difficulty testing;
- experimental Combat Rating validation;
- obsolete simulation orchestration;
- abandoned balance reports;
- old balancing commands;
- unused simulator DTOs;
- balance-specific repositories or services that no longer have a purpose;
- old test fixtures created specifically for the previous balancing approach;
- dead frontend/admin balance functionality other than the Essence Simulator;
- duplicate abstractions that would compete with the architecture described in this document.

Do not preserve old infrastructure merely because it might theoretically be reusable.

Prefer rebuilding small balance-specific abstractions cleanly when the old abstraction was designed around a different approach.

---

## Required Audit Before Deletion

Before deleting code, map the existing balance/simulation infrastructure and classify each relevant component into one of these categories:

```text
KEEP
Required by the Admin Dashboard Essence Simulator or production systems.

REUSE
A low-level abstraction that cleanly fits the new architecture.

DELETE
Existing balance infrastructure that is being superseded.

INVESTIGATE
Ownership or dependency is unclear and removal could affect production behavior.
```

Pay particular attention to code shared between:

```text
Admin Essence Simulator
Production combat
PvP combat
PvE combat
Automated tests
Existing balance tooling
```

Do not delete shared combat-engine functionality merely because balance tooling references it.

The cleanup target is the **balance/testing orchestration and obsolete balance-specific architecture**, not the production combat engine.

---

## Preserve Production Behavior

This cleanup must not alter gameplay behavior.

In particular, preserve:

- combat resolution;
- Essence behavior;
- stat calculations;
- equipment calculations;
- PvE combat;
- PvP combat;
- Combat Rating calculation;
- Admin Dashboard Essence Simulator behavior.

If existing balance code contains logic that production code unexpectedly depends upon, move or extract that logic into the appropriate production/shared layer before deleting the obsolete balance component.

Do not keep an obsolete balance service alive solely because production code accidentally depends on it.

Fix the dependency direction instead.

---

## Preserve the Admin Dashboard Essence Simulator End-to-End

After cleanup, verify that the Admin Dashboard Essence Simulator can still:

1. load the available Essences;
2. construct its required combatants;
3. run its existing 1v1 simulations;
4. run its existing 3v3 simulations;
5. execute repeated combat simulations correctly;
6. calculate its existing Essence results/win rates;
7. return results through its backend API;
8. display those results through the admin frontend.

If it currently provides additional legitimate Essence-analysis functionality, preserve that functionality as well.

The exact internals may be refactored if necessary, but externally visible behavior should remain intact.

---

## Do Not Build Compatibility Layers

Do not introduce adapters whose only purpose is to allow the new balance architecture to coexist with obsolete balance infrastructure.

For example, avoid creating structures such as:

```text
NewBalanceRunner
    ↓
LegacyBalanceAdapter
    ↓
OldSimulationCoordinator
```

when the old coordinator should simply be deleted.

Likewise, do not retain obsolete DTOs, interfaces, repositories, or service layers merely to minimize the size of the diff.

The desired outcome is a smaller and clearer codebase.

---

## Tests

Remove tests whose only purpose is validating functionality that is intentionally being deleted.

Preserve or update tests covering:

```text
Production combat behavior
Shared combat infrastructure
Admin Dashboard Essence Simulator
Combat Rating calculation
Equipment/stat calculation
```

If the Essence Simulator does not currently have sufficient automated coverage, add targeted tests around its preserved behavior before or during the cleanup.

Do not recreate the old balance test suite under different names.

---

## Cleanup Deliverable

Before beginning Phase 1 of the new balance system, produce a short cleanup summary containing:

```text
Deleted:
- major obsolete systems/components removed

Preserved:
- components required by the Admin Essence Simulator
- reusable production/shared simulation infrastructure

Refactored:
- dependencies moved out of obsolete balance infrastructure

Remaining:
- any questionable legacy components intentionally left in place and why
```

Also confirm explicitly that the Admin Dashboard Essence Simulator remains operational.

---

## Definition of Done

This preliminary phase is complete when:

- obsolete balance-testing infrastructure has been removed;
- there is no parallel legacy balance architecture competing with the system described below;
- production combat behavior remains unchanged;
- the Admin Dashboard Essence Simulator remains functional;
- shared simulation infrastructure has clear ownership;
- obsolete balance-specific dependencies have been removed from production code;
- relevant tests pass;
- the repository is in a clean state from which the new balance system can be implemented.

Only after this cleanup should implementation continue with **Phase 1 — Balance Runner Infrastructure**.

## 1. Objective

Build a one-click balance pipeline for LegendsLegacy that can:

1. Evaluate the relative strength of Essences.
2. Generate strong and diverse character builds at defined progression milestones.
3. Establish representative player-power populations.
4. Validate whether Combat Rating accurately reflects real combat power.
5. Measure content difficulty against those populations.
6. Derive recommended Combat Rating values for PvE content.
7. Detect overtuned Essences, broken synergies, weak builds, and exploitable encounters.
8. Re-run the entire process after balance changes with minimal manual work.

The system should treat **simulation results as the source of truth for actual combat performance**.

Combat Rating should remain an estimate of player strength, but the simulator should continuously verify whether the estimate is accurate.

---

# 2. Core Design Principle

Do not define content primarily as:

```text
World Tower Floor 11
Recommended CR: 4,350
```

Instead define the intended progression target:

```yaml
WorldTowerFloor11:
  PartySize: 3

  IntendedPower:
    EquipmentTier: 2
    EquipmentRarity: Epic
    EquipmentQuality: Fine
    EssenceStrengthPercentile: 90

  TargetClearRate: 0.65
```

The simulator should then determine what Combat Rating currently corresponds to that intended progression level.

This ensures that content remains tied to design intent even when:

- equipment is rebalanced;
- Essence strength changes;
- combat formulas change;
- new Essences are added;
- stat scaling changes;
- Doctrines or other build systems are introduced.

---

# 3. High-Level Pipeline

The final system should expose a single entry point such as:

```bash
dotnet run balance --full
```

or an internal development/admin command:

```text
Run Full Balance Analysis
```

The pipeline should execute:

```text
1. Load game balance data
2. Run Essence analysis
3. Generate candidate character builds
4. Optimize builds
5. Build milestone populations
6. Run PvE benchmark suite
7. Calculate real performance scores
8. Validate Combat Rating
9. Run content simulations
10. Calibrate recommended ratings
11. Detect balance anomalies
12. Produce reports
```

---

# 4. Proposed Architecture

```text
BalanceRunner
│
├── BalanceConfiguration
│
├── EssenceAnalyzer
│   ├── Essence 1v1 analysis
│   ├── Essence 3v3 analysis
│   └── Essence usage statistics
│
├── CharacterFactory
│   ├── Equipment generator
│   ├── Essence loadout generator
│   ├── Progression constraints
│   └── Character snapshot builder
│
├── BuildOptimizer
│   ├── Candidate generation
│   ├── Mutation
│   ├── Evaluation
│   ├── Selection
│   └── Diversity enforcement
│
├── BenchmarkRunner
│   ├── Benchmark encounter definitions
│   ├── Combat simulations
│   └── Performance scoring
│
├── PopulationBuilder
│   ├── Percentile calculation
│   ├── Archetype grouping
│   └── Benchmark profile persistence
│
├── CombatRatingAnalyzer
│   ├── CR correlation
│   ├── Performance prediction
│   └── Outlier detection
│
├── EncounterAnalyzer
│   ├── Content simulation
│   ├── Clear-rate analysis
│   ├── Composition analysis
│   └── Cheese detection
│
├── EncounterCalibrator
│   ├── Power-anchor lookup
│   ├── Difficulty search
│   └── Recommended CR derivation
│
└── BalanceReportGenerator
    ├── Markdown report
    ├── JSON output
    └── Historical comparison
```

The balance system should reuse the production combat engine rather than implement a second combat model.

---

# 5. Phase 1 — Balance Runner Infrastructure

## Goal

Create the orchestration layer before introducing optimization.

## Implement

Create a dedicated project or executable such as:

```text
LegendsLegacy.Balance
```

Possible alternatives:

```text
Tools.LL.Balance
Simulation.LL
LegendsLegacy.BalanceRunner
```

The project should reference:

- combat engine;
- game definitions;
- Essence definitions;
- equipment definitions;
- stat calculations;
- Combat Rating calculation;
- encounter definitions where practical.

Avoid duplicating production balance logic inside the balance tool.

## Initial command interface

Support commands such as:

```bash
balance --full
balance --essences
balance --builds
balance --benchmarks
balance --content
balance --content WorldTower
balance --encounter WorldTowerFloor11
```

## Output directory

For example:

```text
/balance-output/
    latest/
    history/
```

Each run should have an ID:

```text
2026-08-27_214500
```

Store:

```text
summary.md
summary.json
essences.json
builds.json
combat-rating.json
content.json
warnings.json
```

## Completion criteria

Phase 1 is complete when one command can:

1. start a deterministic balance run;
2. load current production balance data;
3. execute a trivial simulation;
4. save structured output;
5. produce a Markdown summary.

---

# 6. Phase 2 — Deterministic Simulation Support

Reliable balance testing requires repeatable simulations.

## Requirements

Every simulation should accept a random seed.

Example:

```csharp
new CombatSimulationOptions
{
    Seed = 123456,
    MaxDuration = TimeSpan.FromMinutes(5)
};
```

Running the same battle with the same:

- character snapshots;
- encounter;
- rules;
- seed;

should produce the same result.

## Store simulation metadata

Every result should record:

```csharp
public sealed record SimulationMetadata(
    int Seed,
    string GameBalanceVersion,
    string CombatEngineVersion,
    DateTimeOffset RunAt);
```

A Git commit hash can be used as the balance version if convenient.

## Why

Without deterministic simulation, investigating regressions becomes unnecessarily difficult.

---

# 7. Phase 3 — Standard Character Snapshot

Create a balance-specific representation of a character that contains everything required by the combat engine.

Example:

```csharp
public sealed record BalanceCharacterProfile
{
    public required string Id { get; init; }

    public required int CharacterLevel { get; init; }

    public required IReadOnlyList<EquipmentSnapshot> Equipment { get; init; }

    public required IReadOnlyList<EssenceSnapshot> Essences { get; init; }

    public DoctrineSnapshot? Doctrine { get; init; }

    public required CombatStats Stats { get; init; }

    public required double CombatRating { get; init; }

    public required ProgressionAnchor Progression { get; init; }
}
```

The profile should be independent of a real database character.

It should be cheap to create thousands of profiles in memory.

---

# 8. Phase 4 — Progression Anchors

Introduce explicit progression definitions.

Example:

```csharp
public sealed record ProgressionAnchor
{
    public required string Id { get; init; }

    public required int EquipmentTier { get; init; }

    public required ItemRarity Rarity { get; init; }

    public required ItemQuality Quality { get; init; }

    public required int CharacterLevel { get; init; }

    public EssenceProgressionRules EssenceRules { get; init; }
}
```

Example definitions:

```yaml
anchors:
  T2_Rare_Fine:
    equipmentTier: 2
    rarity: Rare
    quality: Fine

  T2_Epic_Fine:
    equipmentTier: 2
    rarity: Epic
    quality: Fine

  T2_Epic_Exceptional:
    equipmentTier: 2
    rarity: Epic
    quality: Exceptional
```

The exact names should follow existing project terminology.

## Important

A progression anchor describes what the player owns.

It does **not** describe how intelligently the player has built their character.

Build quality is handled separately through percentiles.

---

# 9. Phase 5 — Equipment Generator

Implement a generator capable of creating valid equipment configurations for a progression anchor.

Inputs:

```text
Equipment tier
Rarity
Quality
Allowed blueprints
Tempering assumptions
Slot rules
Character restrictions
```

The generator should initially support two modes.

## Fixed benchmark equipment

Generate standardized equipment for initial testing.

This makes early results easier to understand.

## Optimized equipment

Later allow the optimizer to choose among legal equipment configurations.

Do not introduce full equipment optimization in the first version unless necessary.

Start with Essence optimization against standardized gear.

This substantially reduces the search space.

---

# 10. Phase 6 — PvE Benchmark Encounter Suite

Do not use PvP win rate as the only measure of Essence strength.

Create synthetic benchmark encounters representing different combat demands.

Initial suite:

## Benchmark A — Short Single Target

```text
Duration target: ~30 seconds
Purpose: burst damage
```

## Benchmark B — Sustained Single Target

```text
Duration target: ~120 seconds
Purpose: sustained DPS and ramp mechanics
```

## Benchmark C — High Incoming Damage

```text
Purpose:
defense
healing
shielding
sustain
```

## Benchmark D — Three Targets

```text
Purpose:
AoE
cleave
multi-target effects
```

## Benchmark E — Attrition Fight

```text
Duration: long
Purpose:
resource efficiency
healing
defensive scaling
long cooldown abilities
```

## Benchmark F — Crowd-Control Resistant Boss

```text
Purpose:
ensure CC-dependent builds are not incorrectly rated
```

## Benchmark G — High Armor

```text
Purpose:
physical penetration
magic alternatives
mixed damage
```

## Benchmark H — High Magic Resistance

```text
Purpose:
physical alternatives
resistance interactions
```

More benchmark types can be introduced later.

---

# 11. Phase 7 — Performance Score

Each build needs a comparable performance score.

Do not use only DPS.

A candidate build could be evaluated using:

```text
Damage dealt
Damage taken
Survival
Fight duration
Healing
Shielding
Deaths
Successful clears
Performance across different benchmark types
```

Example conceptual score:

```text
BuildPerformanceScore =
    WeightedAverage(
        ShortSingleTargetScore,
        SustainedSingleTargetScore,
        DefensiveScore,
        MultiTargetScore,
        AttritionScore,
        ResistantBossScore
    )
```

Avoid permanently hard-coding arbitrary weights before observing the data.

Initially expose weights through configuration.

Example:

```yaml
benchmarkWeights:
  shortSingleTarget: 1.0
  sustainedSingleTarget: 1.0
  defensive: 0.8
  multiTarget: 0.8
  attrition: 0.8
  ccResistant: 0.6
```

Also retain each individual benchmark score.

A single aggregate number should never hide why a build is strong.

---

# 12. Phase 8 — Initial Build Optimizer

Because 10 Essence slots across hundreds of Essences creates an enormous search space, exhaustive search is impractical.

Implement a heuristic optimizer.

Recommended starting approach:

**Genetic algorithm with elitism and mutation.**

Beam search is also viable, but a genetic algorithm maps naturally to this problem.

## Candidate representation

```csharp
public sealed record BuildGenome
{
    public required IReadOnlyList<EssenceId> EssenceIds { get; init; }

    public EquipmentGenome? Equipment { get; init; }
}
```

Initially optimize only Essences.

## Generation process

```text
1. Generate N random valid builds
2. Evaluate every build
3. Rank candidates
4. Keep strongest candidates
5. Create mutations
6. Add some random candidates
7. Evaluate next generation
8. Repeat
```

Example configuration:

```yaml
optimizer:
  populationSize: 500
  generations: 100
  eliteCount: 50
  mutationRate: 0.25
  randomInjectionRate: 0.10
```

These values should be configurable rather than fixed.

---

# 13. Build Mutation Operations

Initial mutation operators:

```text
Replace one Essence
Replace two Essences
Swap one offensive Essence for defensive
Swap one defensive Essence for offensive
Randomize one Essence slot
Randomize several Essence slots
```

Later:

```text
Change equipment blueprint
Change stat allocation
Change Doctrine
Change equipment affix configuration
```

Mutation must always respect gameplay legality.

---

# 14. Phase 9 — Build Diversity

Without diversity controls, the optimizer may produce hundreds of nearly identical builds.

Implement Essence-set similarity.

One simple metric:

```text
Similarity =
SharedEssenceCount / MaxEssenceCount
```

Example:

```text
Build A:
A B C D E F G H I J

Build B:
A B C D E F G H I X

Similarity = 90%
```

Possible policy:

```yaml
diversity:
  maximumSimilarity: 0.70
```

Rather than always rejecting similar builds, consider applying a penalty.

```text
AdjustedFitness =
RawFitness
-
SimilarityPenalty
```

This lets exceptionally strong builds survive while encouraging exploration.

---

# 15. Phase 10 — Archetype-Aware Optimization

Generate strong characters representing different strategic approaches.

Initial archetypes:

```text
Damage
Defense
Sustain
Hybrid
Unrestricted
```

Later:

```text
Crit
Bleed
Burn
Poison
Summoner
Shield
Basic Attack
Ability Burst
Execute
Healing
Control
```

An archetype should modify optimizer fitness rather than enforce arbitrary Essence lists.

Example:

```text
Damage archetype:
high damage weight
normal survivability weight

Defense archetype:
high survival weight
high mitigation weight

Hybrid:
balanced weighting
```

This helps ensure that the benchmark population contains multiple viable strategies.

---

# 16. Phase 11 — Benchmark Populations

For each progression anchor, generate a population of characters.

Example:

```text
T2 Epic Fine

Damage:       100 builds
Defense:      100 builds
Sustain:      100 builds
Hybrid:       100 builds
Unrestricted: 100 builds
```

Persist the evaluated profiles.

Then classify them into power percentiles.

Example:

```text
P25
P50
P75
P90
P95
P99
```

Meaning:

```text
P50 = approximately median optimized/valid player build in the generated population
P90 = stronger than roughly 90% of the population
P99 = extreme optimization territory
```

Care should be taken with naming.

If the generated population is itself heavily optimized, P50 does not mean an average real player.

The report should distinguish between:

```text
Generated population percentile
Observed live-player percentile
```

if live telemetry is introduced later.

---

# 17. Recommended Default Content Targets

Normal progression should generally not be balanced around the theoretical maximum build.

Suggested conceptual targets:

```text
P50 — ordinary progression
P75 — competent build
P90 — well-optimized build
P95 — highly optimized
P99 — theoretical/extreme
```

For challenging milestone content, P75-P90 is usually a more useful anchor than P99.

The exact values should remain a design choice per encounter.

---

# 18. Phase 12 — Combat Rating Validation

Once builds have:

```text
Combat Rating
Actual simulated performance
```

measure the relationship between the two.

## Required analysis

Calculate:

```text
Correlation between CR and benchmark performance
Performance variance within CR bands
Prediction error
High-performing CR outliers
Low-performing CR outliers
```

Example CR bands:

```text
4000-4099
4100-4199
4200-4299
4300-4399
```

For each band report:

```text
Median performance
P10 performance
P90 performance
Performance spread
```

## Example warning

```text
CR OUTLIER

Build:
T2-Epic-Fine-00418

Combat Rating:
4,221

Expected performance at CR:
9,800

Observed performance:
15,340

Difference:
+56.5%

Potential causes:
- Bloodfang Essence
- Executioner Essence
- Crit interaction
```

The first implementation does not need automatic causal analysis.

Initially list distinguishing Essences and equipment shared by outliers.

---

# 19. Combat Rating Predictive Accuracy

Produce a simple health assessment:

```text
Combat Rating Predictive Accuracy

T1: Excellent
T2: Good
T3: Poor
```

Internally, use numeric metrics.

For example:

```text
R²
Spearman correlation
Mean absolute prediction error
```

Do not rely on only one statistic.

The goal is not statistical perfection.

The goal is detecting when two characters with similar CR routinely have radically different combat effectiveness.

---

# 20. Phase 13 — Essence Usage Analysis

Build optimization produces useful metadata beyond win rates.

For each Essence track:

```text
Overall usage rate
P50 usage
P75 usage
P90 usage
P95 usage
P99 usage
Usage by archetype
Average build performance when present
Average build performance when absent
Common partners
```

Example:

```text
Bloodfang Essence

Overall usage: 18%
P75 usage:     31%
P90 usage:     67%
P95 usage:     82%
P99 usage:     94%
```

This should generate a balance warning.

Possible classifications:

```text
Potentially mandatory
Potentially overtuned
Potentially undertuned
Niche
Healthy
```

Do not automatically rebalance based on these labels.

They are investigation signals.

---

# 21. Phase 14 — Synergy Detection

Analyze Essence pairs and eventually larger combinations.

Initial pair analysis:

```text
Observed performance of A + B
Expected performance based on A and B individually
Difference
```

Example:

```text
Bloodfang + Executioner

Expected:
+8%

Observed:
+31%

Synergy delta:
+23%
```

Flag combinations with unusually high positive or negative interaction.

Do not initially attempt every possible 10-Essence combination.

Start with:

```text
Pairs
Frequently occurring triples
```

---

# 22. Phase 15 — Content Power Anchors

Add a balance metadata definition to PvE content.

Example:

```csharp
public sealed record EncounterPowerTarget
{
    public required string ProgressionAnchorId { get; init; }

    public required int BuildPercentile { get; init; }

    public required int PartySize { get; init; }

    public required double DesiredClearRate { get; init; }
}
```

Example:

```yaml
worldTowerFloor11:
  progressionAnchor: T2_Epic_Fine
  buildPercentile: 90
  partySize: 3

  desiredClearRate: 0.65
```

Additional useful values:

```yaml
desiredFightDurationSeconds:
  min: 90
  max: 150

allowedClearRate:
  min: 0.55
  max: 0.75
```

---

# 23. Phase 16 — Encounter Simulation

For every content definition:

1. Resolve its progression anchor.
2. Resolve appropriate benchmark profiles.
3. Generate teams.
4. Run many battles.
5. Calculate encounter statistics.

Report:

```text
Clear rate
Average fight duration
Median fight duration
Remaining party health
Deaths per attempt
Damage taken
Failure reason
CR distribution
```

For team content also report:

```text
Team CR
Lowest player CR
Highest player CR
CR variance
Archetype composition
```

---

# 24. Team Generation

Do not test only three identical characters.

Generate different team compositions.

Example:

```text
Damage + Damage + Damage
Damage + Damage + Sustain
Damage + Defense + Sustain
Hybrid + Hybrid + Hybrid
Random valid composition
```

The default benchmark population should include many team structures.

The system should detect if an encounter requires one specific team composition despite being intended as general progression content.

---

# 25. Recommended Combat Rating

After simulating the intended population, derive:

```text
Recommended Player CR
Recommended Team CR
```

For multiplayer content, **Team CR should be the more fundamental internal value**.

Example:

```text
Recommended Team CR:
13,140

Approximate individual recommendation:
4,380
```

The frontend can still show the simpler per-player recommendation where appropriate.

---

# 26. Phase 17 — Encounter Calibration

Do not allow the optimizer to freely rewrite all boss stats.

Boss mechanics and identity should remain manually designed.

Instead expose controlled calibration parameters.

Recommended first version:

```text
HealthMultiplier
DamageMultiplier
```

A later alternative:

```text
EncounterPowerMultiplier
```

which maps onto predefined scaling curves.

Example:

```text
Encounter Power 1.20

Health:  x1.20
Damage:  x1.12
Armor:   x1.06
```

This maintains encounter identity while allowing numerical calibration.

---

# 27. Automated Difficulty Search

If an encounter should have:

```text
Target clear rate: 65%
```

the calibrator can search for the appropriate multiplier.

Use binary search or another bounded search algorithm.

Example:

```text
1.00 -> 91% clear
1.50 -> 39% clear
1.25 -> 72% clear
1.375 -> 59% clear
1.31 -> 66% clear
```

Result:

```text
Suggested Encounter Power:
1.31
```

Do not automatically apply the result to production data.

Output it as a proposed adjustment.

Manual approval should remain required.

---

# 28. Phase 18 — Generic vs Encounter-Specific Optimization

Maintain two optimization modes.

## Generic Builds

Characters are optimized against the benchmark suite.

They do not know which real boss they will fight.

Use these to determine intended content difficulty.

## Encounter-Specific Builds

Characters are optimized directly against a specific encounter.

Use these to detect:

```text
Cheese
Hard counters
Broken interactions
Immunity abuse
Infinite sustain
Burst exploits
```

Example report:

```text
World Tower Floor 11

Generic P90 clear rate:
64%

Encounter-optimized P90 clear rate:
100%

Median optimized kill time:
14 seconds

Result:
CRITICAL CHEESE WARNING
```

---

# 29. Phase 19 — Content Difficulty Report

Generate one row per encounter.

Example:

| Encounter      | Anchor           | Target | Actual | Result   |
| -------------- | ---------------- | -----: | -----: | -------- |
| Tower Floor 9  | T2 Epic Fine P75 |    70% |    73% | Healthy  |
| Tower Floor 10 | T2 Epic Fine P75 |    65% |    67% | Healthy  |
| Tower Floor 11 | T2 Epic Fine P90 |    65% |    42% | Too Hard |
| Tower Floor 12 | T3 Rare Fine P75 |    60% |    81% | Too Easy |

Include:

```text
Recommended Player CR
Recommended Team CR
Observed CR range
Clear-rate confidence
Suggested multiplier
```

---

# 30. Phase 20 — Full Balance Report

The one-click run should produce a human-readable report.

Suggested structure:

```text
BALANCE RUN
================================

SUMMARY

ESSENCE BALANCE

BUILD POPULATIONS

COMBAT RATING HEALTH

CONTENT BALANCE

SYNERGY WARNINGS

CHEESE WARNINGS

REGRESSIONS

RECOMMENDED INVESTIGATIONS
```

Example:

```text
ESSENCE BALANCE
--------------------------------

Potentially overtuned: 7
Potentially undertuned: 13
Potentially mandatory: 3
Suspicious synergies: 4
```

Example:

```text
COMBAT RATING
--------------------------------

T1 predictive accuracy: Excellent
T2 predictive accuracy: Good
T3 predictive accuracy: Poor

Largest CR outlier:
+46% performance above expected
```

Example:

```text
CONTENT
--------------------------------

Healthy:   83 encounters
Too Easy:  11
Too Hard:   7
Unstable:   3
```

---

# 31. Historical Regression Tracking

Persist summaries from each run.

Compare:

```text
Current run
vs
Previous run
```

Track:

```text
Essence usage changes
Build performance changes
CR accuracy changes
Encounter clear-rate changes
Recommended CR changes
```

Example:

```text
REGRESSION WARNING

World Tower Floor 11

Previous clear rate:
66%

Current clear rate:
48%

Change:
-18 percentage points

Likely related changes:
- Bloodfang passive nerf
- T2 armor scaling change
```

The first implementation can simply report which balance definitions changed between Git revisions.

Automatic attribution can come later.

---

# 32. Statistical Sampling

A single combat simulation is not enough when combat contains randomness.

For every matchup, run multiple seeds.

Example:

```yaml
simulation:
  seedsPerMatchup: 100
```

For expensive optimization phases, use fewer simulations initially.

Example:

```text
Optimizer evaluation:
10-20 seeds

Final benchmark evaluation:
100-500 seeds
```

This creates a useful performance optimization:

```text
Cheap approximate evaluation during search
Expensive accurate evaluation for finalists
```

---

# 33. Performance Optimization

The system may eventually execute millions of battles.

Design for parallelism early.

Potential approach:

```csharp
Parallel.ForEachAsync(...)
```

or a worker-pool architecture.

Requirements:

```text
Simulation state must be isolated.
Combat engine must not rely on shared mutable global state.
Random generators must be per simulation.
```

Cache deterministic calculations such as:

```text
Equipment stat calculation
Essence static modifiers
Combat Rating
Character derived stats
```

Do not prematurely introduce distributed execution.

A fast local multi-core runner should be the first target.

---

# 34. Result Persistence

Store structured output independently from presentation.

Example:

```csharp
public sealed record BalanceRunResult
{
    public required BalanceRunMetadata Metadata { get; init; }

    public required IReadOnlyList<EssenceBalanceResult> Essences { get; init; }

    public required IReadOnlyList<BuildBalanceResult> Builds { get; init; }

    public required CombatRatingAnalysis CombatRating { get; init; }

    public required IReadOnlyList<EncounterBalanceResult> Encounters { get; init; }

    public required IReadOnlyList<BalanceWarning> Warnings { get; init; }
}
```

Serialize to JSON.

Generate Markdown from the JSON result.

This makes it possible to later add:

```text
Web dashboard
Charts
CI reports
Admin UI
Historical database
```

without changing the simulation pipeline.

---

# 35. Warning System

Introduce standardized warning severity.

Example:

```csharp
public enum BalanceWarningSeverity
{
    Info,
    Warning,
    Critical
}
```

Warning categories:

```text
EssenceOverperformance
EssenceUnderperformance
MandatoryEssence
SuspiciousSynergy
CombatRatingOutlier
CombatRatingPoorAccuracy
EncounterTooEasy
EncounterTooHard
EncounterCheese
LowBuildDiversity
SimulationInstability
```

Each warning should contain enough data for investigation.

---

# 36. Configuration

Most tuning parameters should live outside source code.

Example:

```yaml
balance:
  optimizer:
    populationSize: 500
    generations: 100
    eliteCount: 50

  simulation:
    optimizerSeeds: 10
    validationSeeds: 100

  diversity:
    maximumSimilarity: 0.70

  thresholds:
    essenceMandatoryUsage:
      p95: 0.80

    crOutlier:
      performanceDifference: 0.25

    contentClearRateTolerance:
      absolute: 0.10
```

Thresholds should generate warnings, not automatically modify gameplay.

---

# 37. Suggested Delivery Order

Do not attempt to implement the entire system in one pass.

## Milestone 1 — Balance Runner

Implement:

```text
Balance CLI
Production data loading
Deterministic combat simulation
JSON result output
Markdown report
```

---

## Milestone 2 — Progression Profiles

Implement:

```text
Progression anchors
Standard equipment generation
Random Essence loadouts
Combat Rating calculation
Character snapshot creation
```

At this stage, generate thousands of random characters and inspect CR distributions.

---

## Milestone 3 — Benchmark Suite

Implement:

```text
Synthetic benchmark encounters
Performance scoring
Repeated seeded simulation
```

Now every character can be assigned:

```text
CR
Real performance score
```

---

## Milestone 4 — CR Validation

Implement:

```text
Correlation
CR bands
Outlier detection
Predictive accuracy report
```

This already provides substantial value before optimization exists.

---

## Milestone 5 — Build Optimizer

Implement:

```text
Genetic optimizer
Mutation
Selection
Elitism
Basic diversity penalty
```

Initially optimize only Essences.

---

## Milestone 6 — Population Percentiles

Implement:

```text
Archetypes
P50
P75
P90
P95
P99
```

Persist representative benchmark characters.

---

## Milestone 7 — Essence Meta Analysis

Implement:

```text
Essence usage by percentile
Pair synergy analysis
Mandatory-Essence detection
Underused Essence detection
```

---

## Milestone 8 — Real Content Analysis

Integrate:

```text
World Tower
Dungeons
Bosses
Other fixed PvE encounters
```

Generate:

```text
Clear rates
Recommended CR
Difficulty warnings
```

---

## Milestone 9 — Encounter Calibration

Implement:

```text
Controlled stat multipliers
Binary-search difficulty calibration
Suggested multiplier output
```

Keep manual approval.

---

## Milestone 10 — Encounter-Specific Optimization

Implement:

```text
Boss-targeted optimization
Cheese detection
Hard-counter detection
```

---

## Milestone 11 — Equipment Optimization

Only now expand the optimizer to choose:

```text
Blueprints
Equipment variants
Tempering configurations
Stat distributions
```

This substantially increases search complexity and should not block the useful first versions.

---

## Milestone 12 — Historical Regression Analysis

Implement:

```text
Previous-run comparison
Balance regression warnings
CI integration
```

---

# 38. First Practical Version

The first genuinely useful release does not need the entire plan.

Target this scope:

```text
1. Define T1/T2/T3 progression anchors.
2. Generate fixed equipment for each anchor.
3. Generate random Essence builds.
4. Run benchmark encounters.
5. Calculate real performance score.
6. Compare performance against CR.
7. Implement an Essence-only optimizer.
8. Produce P50/P75/P90/P95 benchmark characters.
9. Test World Tower bosses against them.
10. Output recommended CR and clear-rate reports.
```

This would already answer questions such as:

> How strong is a well-built character in full T2 Epic Fine gear?

> What Combat Rating does that character usually have?

> How much stronger is a P90 Essence setup than a mediocre one?

> Is World Tower Floor 11 appropriately difficult for that progression point?

> Which Essences dominate optimized builds?

> Does Combat Rating actually predict combat performance?

---

# 39. Example World Tower Workflow

Suppose Floor 11 is designed as:

```yaml
progressionAnchor: T2_Epic_Fine
buildPercentile: 90
partySize: 3
desiredClearRate: 0.65
```

The balance runner:

```text
1. Loads the T2 Epic Fine population.
2. Selects representative P90 builds.
3. Generates diverse three-player teams.
4. Runs 10,000 seeded battles.
5. Measures clear rate.
```

Result:

```text
Clear rate:
91%

Median Team CR:
13,160

Median fight duration:
48 seconds
```

The calibrator determines that the encounter is too easy.

It searches encounter power:

```text
1.00 -> 91%
1.20 -> 78%
1.30 -> 69%
1.34 -> 65%
```

Output:

```text
WORLD TOWER FLOOR 11

Design Target:
T2 / Epic / Fine / P90

Target Clear Rate:
65%

Current Clear Rate:
91%

Suggested Encounter Power:
1.34

Recommended Player CR:
4,390

Recommended Team CR:
13,170
```

No production value should change automatically.

---

# 40. Future Live-Telemetry Integration

Eventually compare simulations with real players.

Possible telemetry:

```text
Character CR when encounter attempted
Equipment
Essences
Doctrine
Party composition
Result
Fight duration
```

Then compare:

```text
Simulated clear rate
vs
Actual clear rate
```

Example:

```text
Floor 11

Simulator:
67%

Live players:
61%
```

This provides a way to calibrate not only combat values but also assumptions about player optimization.

It can reveal that generated P75 builds may actually behave more like real P95 players, for example.

This should be considered a later phase and must not block development of the offline system.

---

# 41. Important Guardrails

## Do not make Combat Rating the authority

CR predicts strength.

Simulation measures strength.

---

## Do not balance around P99 by default

Extreme optimization should not become mandatory progression.

---

## Do not use PvP performance as PvE performance

Maintain separate evaluation contexts.

---

## Do not permit unlimited encounter auto-tuning

Boss identity and mechanics remain manually authored.

Only controlled numerical calibration should be automated.

---

## Do not optimize only for aggregate score

Keep benchmark-specific results visible.

Otherwise severe weaknesses can be hidden by one high score.

---

## Do not let the optimizer collapse onto one build

Diversity is a first-class requirement.

---

## Do not automatically apply balance recommendations

The tool should identify problems and propose changes.

A developer should decide whether those changes are desirable.

---

# 42. Long-Term End State

The final workflow should be:

```text
Change Essence
Change equipment
Add boss
Modify combat formula
        ↓
Run balance
        ↓
Receive report
```

The report tells you:

```text
Which Essences moved in power
Which combinations became dominant
Which builds became stronger or weaker
Whether CR still predicts power
Which encounters changed difficulty
Which recommended CR values changed
Which bosses became exploitable
Which progression anchors are affected
```

At that point the system becomes more than a simulator.

It becomes the automated regression-test suite for the numerical design of the game.

---

# 43. Recommended Immediate Implementation Task

Start with the following vertical slice:

```text
Progression Anchor
        ↓
Fixed Gear Generator
        ↓
Random Essence Build Generator
        ↓
PvE Benchmark Suite
        ↓
Performance Score
        ↓
Combat Rating Comparison
        ↓
Markdown Report
```

Do not begin with encounter auto-tuning or sophisticated genetic optimization.

First establish the fundamental pipeline:

> Given a legal character build, can the system reliably measure its actual combat strength and compare that strength to its Combat Rating?

Once that works, optimization, percentile populations, content calibration, and automated difficulty recommendations become incremental additions rather than architectural experiments.
