# Automated Balance & Content Calibration System
## Consolidated Implementation Plan

---

# 0. Purpose

Build a one-click numerical balance and content-calibration system for LegendsLegacy.

The system should make it practical to answer questions such as:

- How strong is a character expected to be at a particular progression point?
- What Combat Rating normally corresponds to that progression point?
- How much does Essence selection affect real combat performance?
- Which Essences or Essence combinations are disproportionately strong?
- How difficult should a boss be if it is intended for a particular stage of gear and Essence progression?
- Is an encounter too easy, too hard, or overly sensitive to specific builds?
- Does Combat Rating actually predict real combat effectiveness?
- Did a balance change unexpectedly alter existing PvE difficulty?

The system should use the **production combat engine** and treat **simulation results as the authority on real combat power**.

Combat Rating remains a useful player-facing estimate, but the simulator validates whether that estimate corresponds to actual performance.

The final workflow should eventually be close to:

```text
Change Essence / equipment / combat formula / boss
                         ↓
                  Run Balance
                         ↓
                 Receive Report
```

The report should identify:

```text
Essence balance changes
Dominant Essence combinations
Representative build power
Combat Rating accuracy
Content difficulty changes
Recommended Combat Rating changes
Potential encounter exploits
Balance regressions
```

---

# 1. Preliminary Cleanup — Remove Existing Balance Infrastructure

**Status: Complete.** See [P0 Balance Infrastructure Cleanup](p0-cleanup-summary.md) for the audit classification, removals, preserved systems, and verification boundary.

Before implementing the system described in this document, inspect the repository for existing combat balancing, simulation, benchmark, generated-character, and automated balance-testing infrastructure.

The intention is to **replace the existing balance-testing architecture rather than extend it**.

Most existing balance-specific infrastructure should be deleted so that the new system is built around a coherent architecture without obsolete concepts, compatibility layers, or duplicate abstractions.

## 1.1 Explicit Exception — Keep the Admin Dashboard Essence Simulator

The existing **Admin Dashboard Essence Simulator must remain functional**.

This is the existing admin feature used to run Essence-focused simulations such as:

```text
1v1 Essence simulations
3v3 Essence simulations
Essence win-rate analysis
```

Do not delete this feature.

Do not unnecessarily redesign or rewrite it during cleanup unless a refactor is required to preserve it after obsolete balance infrastructure is removed.

It may continue to reuse:

```text
Production combat engine
Shared simulation primitives
Character/combat snapshots
Essence definitions
Stat calculations
Random-seed infrastructure
Other genuinely reusable low-level combat infrastructure
```

The goal is to preserve the feature, not necessarily every implementation detail currently underneath it.

---

## 1.2 Cleanup Classification

Audit relevant components and classify each as:

```text
KEEP
Required by production systems or the Admin Dashboard Essence Simulator.

REUSE
A clean low-level component that fits the new architecture.

DELETE
Legacy balance-specific infrastructure superseded by this document.

INVESTIGATE
Ownership or dependency is unclear and removal could affect production behavior.
```

Pay particular attention to dependencies between:

```text
Admin Essence Simulator
Production PvE combat
Production PvP combat
Combat Rating
Equipment/stat calculations
Automated tests
Legacy balance tooling
```

Do not delete production combat functionality merely because old balance tooling references it.

The cleanup target is the old **balance/testing orchestration and balance-specific architecture**.

---

## 1.3 Cleanup Targets

Remove obsolete infrastructure related to things such as:

```text
Old automated balance runners
Old benchmark systems
Old generated-character systems
Old content-difficulty testing
Old Combat Rating validation experiments
Obsolete simulation orchestration
Abandoned balance reports
Legacy balancing commands
Unused simulator DTOs
Balance-specific repositories/services that no longer have a purpose
Dead admin balance functionality other than the Essence Simulator
Duplicate abstractions that compete with the new architecture
Tests whose only purpose is validating intentionally deleted balance features
```

Do not preserve obsolete infrastructure merely because it might theoretically be reusable.

Prefer a small clean replacement over carrying forward an abstraction designed around a different model.

---

## 1.4 Preserve Production Behavior

The cleanup must not alter:

```text
Combat resolution
Essence behavior
Equipment behavior
Stat calculations
PvE combat
PvP combat
Combat Rating calculation
Admin Dashboard Essence Simulator behavior
```

If production code unexpectedly depends on balance-specific infrastructure, move the reusable logic into the correct shared/production layer before removing the obsolete component.

Do not keep an obsolete balance service alive solely because the dependency direction is currently wrong.

Fix the dependency direction.

---

## 1.5 Verify the Existing Essence Simulator

After cleanup, verify that the Admin Dashboard Essence Simulator can still:

1. Load available Essences.
2. Construct required combatants.
3. Run existing 1v1 simulations.
4. Run existing 3v3 simulations.
5. Execute repeated seeded/random simulations correctly.
6. Calculate its current Essence results and win rates.
7. Return results through its backend API.
8. Display results through the admin frontend.

If it currently provides additional legitimate Essence-analysis functionality, preserve that as well.

---

## 1.6 Avoid Compatibility Layers

Do not create structures such as:

```text
NewBalanceRunner
      ↓
LegacyBalanceAdapter
      ↓
OldSimulationCoordinator
```

when the old coordinator should simply disappear.

Likewise, do not retain obsolete:

```text
DTOs
Interfaces
Repositories
Service layers
Commands
Persistence structures
```

only to reduce the size of the diff.

The desired outcome is a smaller and clearer codebase.

---

## 1.7 Cleanup Deliverable

Before implementing the new balance system, produce a short summary:

```text
Deleted:
- obsolete systems/components

Preserved:
- Admin Essence Simulator
- production/shared simulation infrastructure

Refactored:
- dependencies moved out of obsolete balance infrastructure

Remaining:
- legacy components intentionally retained and why
```

Explicitly confirm that the Admin Dashboard Essence Simulator remains operational.

---

# 2. Core Design Principles

The implementation should follow these principles throughout.

## 2.1 Simulation Measures Power

Combat Rating predicts strength.

Simulation measures strength.

Do not make the Combat Rating formula the authority used to decide whether content is correctly balanced.

---

## 2.2 Balance Against Progression Intent

Do not define a boss primarily as:

```text
Recommended Combat Rating = 4,350
```

Instead define the intended player progression.

Example:

```text
Tier 1 gear
Rare rarity
Exceptional quality
6 Essences
Competently optimized Essence build
```

Then determine through simulation what Combat Rating currently corresponds to that progression point.

---

## 2.3 Avoid Explicit Profiles for Every Possible Combination

Do not maintain every combination of:

```text
Tier
× Rarity
× Quality
× Essence count
× Essence selection
× Equipment selection
× Build archetype
```

as explicit benchmark characters.

That would create a balance model nearly as complicated as the game itself.

Instead use:

```text
Progression Bands
    ↓
Start / End Power Anchors
    ↓
Gear Packages + Essence Profiles
    ↓
Small representative build library
    ↓
Measured benchmark power
```

---

## 2.4 Persist Representative Builds, Not the Whole Search Space

The optimizer may explore thousands or millions of candidate builds internally.

The balance system should retain only a relatively small set of strong, diverse, representative builds for future content testing.

For example:

```text
4 Essences / P50 → 10 representative builds
4 Essences / P75 → 10 representative builds
4 Essences / P90 → 10 representative builds

5 Essences / P50 → 10 representative builds
...

6 Essences / P90 → 10 representative builds
```

The exact number should be configurable.

The important point is that **candidate search volume and retained benchmark-profile count are different concepts**.

---

## 2.5 Keep Equipment and Essence Power as Separate Knobs

For content calibration, model a character as:

```text
Gear Package
+
Essence Profile
+
Optional archetype
```

Example:

```text
Gear Package:
T1 / Rare / Exceptional

Essence Profile:
6 slots / P75
```

This is substantially simpler than maintaining complete bespoke character profiles for each encounter.

---

## 2.6 Do Not Balance Normal Progression Around the Theoretical Best Build

Do not make P99 or a single discovered meta build the default content target.

Typical conceptual meanings:

```text
P50 = ordinary/acceptable build quality
P75 = competent and intentionally constructed build
P90 = strongly optimized build
P95 = highly optimized
P99 = extreme/theoretical territory
```

The exact interpretation must be documented based on how the generated population is constructed.

For normal progression, P50-P75 may be appropriate.

For challenging milestone content, P75-P90 may be appropriate.

P95-P99 should generally be used for stress testing and exploit detection rather than ordinary progression requirements.

---

# 3. High-Level Architecture

```text
BalanceRunner
│
├── BalanceConfiguration
│
├── EssenceAnalyzer
│   └── integration with existing Admin Essence Simulator primitives where useful
│
├── ProgressionModel
│   ├── ProgressionBands
│   ├── PowerAnchors
│   ├── GearPackages
│   └── EssenceProfiles
│
├── GearPackageFactory
│
├── EssenceBuildOptimizer
│   ├── CandidateGeneration
│   ├── Mutation
│   ├── Evaluation
│   ├── Selection
│   └── DiversityEnforcement
│
├── RepresentativeBuildLibrary
│
├── BenchmarkRunner
│   ├── Synthetic PvE benchmarks
│   └── Build performance scoring
│
├── PowerModel
│   ├── Anchor measurement
│   └── Progression interpolation
│
├── CombatRatingAnalyzer
│   ├── CR correlation
│   ├── CR prediction error
│   └── Outlier detection
│
├── EncounterAnalyzer
│   ├── Real content simulations
│   ├── Party generation
│   └── clear-rate analysis
│
├── EncounterCalibrator
│   ├── Target power lookup
│   ├── Difficulty search
│   └── Recommended CR derivation
│
├── ExploitAnalyzer
│   └── encounter-specific optimization
│
└── BalanceReportGenerator
    ├── Markdown
    ├── JSON
    └── historical comparison
```

The balance runner must use the real combat engine.

Do not implement an independent simplified combat model to estimate outcomes.

---

# 4. Balance Runner Infrastructure

Create a dedicated executable/project such as:

```text
LegendsLegacy.Balance
```

or equivalent naming consistent with the repository.

It should reference production/shared projects containing:

```text
Combat engine
Essence definitions
Equipment definitions
Stat calculations
Combat Rating
Encounter definitions
Game rules
```

Avoid copying production balance logic into the balance tool.

---

## 4.1 Suggested Commands

Support commands such as:

```bash
balance --full
balance --essences
balance --builds
balance --benchmarks
balance --combat-rating
balance --content
balance --content WorldTower
balance --encounter WorldTowerFloor11
```

A single full command should eventually execute the entire pipeline.

---

## 4.2 Output

Use a directory such as:

```text
/balance-output/
    latest/
    history/
```

Each run should include:

```text
summary.md
summary.json
essences.json
representative-builds.json
power-anchors.json
combat-rating.json
content.json
warnings.json
```

Each balance run should have a unique ID and metadata.

---

# 5. Deterministic Simulation

Every combat simulation should support a deterministic seed.

Example:

```csharp
new CombatSimulationOptions
{
    Seed = 123456,
    MaxDuration = TimeSpan.FromMinutes(5)
};
```

Given identical:

```text
Character snapshots
Encounter
Combat rules
Seed
```

the result should be reproducible.

Store:

```text
Seed
Balance version
Combat engine version
Run timestamp
Git commit hash if practical
```

This is important for debugging regressions.

---

# 6. Gear Packages

A **Gear Package** represents a standardized equipment state for balancing purposes.

Example:

```csharp
public sealed record GearPackageDefinition
{
    public required string Id { get; init; }

    public required int Tier { get; init; }

    public required ItemRarity Rarity { get; init; }

    public required ItemQuality Quality { get; init; }

    public GearArchetype Archetype { get; init; }
}
```

Example IDs:

```text
T1_Rare_Exceptional_Balanced
T1_Rare_Exceptional_Offensive
T1_Rare_Exceptional_Defensive

T1_Epic_Exceptional_Balanced
T1_Epic_Exceptional_Offensive
T1_Epic_Exceptional_Defensive
```

---

## 6.1 Keep Gear Package Count Small

Do not generate every possible item combination during the initial implementation.

Start with:

```text
Balanced
Offensive
Defensive
```

for relevant tier/rarity/quality combinations.

If the game's equipment structure makes one of these meaningless, simplify further.

The purpose is to represent realistic gear power without creating thousands of benchmark sets.

---

## 6.2 Fixed Gear First

The first version should use deterministic standardized gear packages.

Full equipment optimization should be deferred.

Only introduce blueprint/tempering/stat-distribution optimization later if simulations demonstrate that equipment configuration causes meaningful variance that Gear Packages fail to capture.

---

# 7. Essence Profiles

An **Essence Profile** represents:

```text
Number of equipped Essences
+
Build quality percentile
+
Optional archetype
```

Example:

```csharp
public sealed record EssenceProfileDefinition
{
    public required string Id { get; init; }

    public required int EssenceSlots { get; init; }

    public required int BuildPercentile { get; init; }

    public EssenceArchetype? Archetype { get; init; }
}
```

Example:

```text
E4_P50
E4_P75
E4_P90

E5_P50
E5_P75
E5_P90

E6_P50
E6_P75
E6_P90
```

Optional archetype-specific variants can exist where valuable:

```text
E6_P75_Damage
E6_P75_Defense
E6_P75_Sustain
E6_P75_Hybrid
```

Do not multiply dimensions unless they provide useful information.

---

# 8. Representative Essence Build Library

The system should generate and retain a small library of strong, meaningfully different Essence builds.

Example:

```text
E4_P75
    Build 01
    Build 02
    ...
    Build 10

E5_P75
    Build 01
    ...
```

The same representative Essence builds can be equipped with different Gear Packages during simulations.

This is one of the main mechanisms used to prevent combinatorial explosion.

---

## 8.1 Why This Works

The system does not need to prove that every possible legal character can defeat an encounter.

It needs a useful sample answering:

> How does a reasonably strong and diverse set of characters at this progression point perform?

The optimizer explores the wider search space.

The representative build library stores the small subset needed for repeatable content balancing.

---

# 9. Essence Build Optimizer

Because hundreds of Essences across multiple slots creates an enormous search space, do not use exhaustive search.

Recommended initial algorithm:

**Genetic algorithm with elitism, mutation, random injection, and diversity pressure.**

Beam search is acceptable if it integrates more naturally with the codebase.

---

## 9.1 Candidate Representation

Example:

```csharp
public sealed record EssenceBuildGenome
{
    public required IReadOnlyList<EssenceId> EssenceIds { get; init; }
}
```

Initially optimize only Essence selection.

Do not optimize equipment at the same time in the first implementation.

---

## 9.2 Optimization Loop

```text
1. Generate random legal Essence builds
2. Evaluate candidates against benchmark encounters
3. Rank candidates
4. Retain elite candidates
5. Mutate elite candidates
6. Inject fresh random builds
7. Re-evaluate
8. Repeat for N generations
```

Configuration example:

```yaml
optimizer:
  populationSize: 500
  generations: 100
  eliteCount: 50
  mutationRate: 0.25
  randomInjectionRate: 0.10
```

These values are illustrative and must be configurable.

---

# 10. Build Diversity

Without explicit diversity pressure, the optimizer may discover one strong combination and generate dozens of trivial variations.

That is not useful for content testing.

Implement Essence-set similarity.

Simple version:

```text
Similarity =
SharedEssenceCount / MaximumEssenceCount
```

Example:

```text
Build A:
A B C D E F

Build B:
A B C D E X

Similarity:
83%
```

Apply either:

```text
Maximum similarity threshold
```

or preferably:

```text
Fitness penalty based on similarity to already-selected representatives
```

This allows exceptional builds to survive while still encouraging meaningful variation.

The representative library should favor builds that are both:

```text
Strong
Meaningfully different
```

---

# 11. PvE Benchmark Suite

Do not use PvP Essence win rate as the sole measure of PvE strength.

The existing 1v1 and 3v3 simulator remains valuable for Essence balance, but PvE build optimization needs its own benchmark suite.

Initial synthetic benchmarks:

## 11.1 Short Single Target

```text
~30 second target duration
Purpose:
burst
short cooldown value
opening pressure
```

## 11.2 Sustained Single Target

```text
~120 second target duration
Purpose:
sustained DPS
ramp mechanics
long-duration efficiency
```

## 11.3 High Incoming Damage

```text
Purpose:
mitigation
healing
shielding
survival
```

## 11.4 Three Targets

```text
Purpose:
AoE
cleave
multi-target effects
```

## 11.5 Attrition

```text
Long fight
Purpose:
sustain
resource efficiency
long cooldowns
healing
defensive scaling
```

## 11.6 Crowd-Control Resistant Boss

```text
Purpose:
prevent CC-heavy builds from being overvalued
```

## 11.7 High Armor

```text
Purpose:
physical mitigation interactions
penetration
mixed damage
```

## 11.8 High Magic Resistance

```text
Purpose:
magic mitigation interactions
physical alternatives
```

More benchmarks may be introduced later, but start small enough that the results are understandable.

---

# 12. Performance Scoring

Do not reduce character strength to DPS alone.

Capture:

```text
Damage dealt
Damage taken
Survival
Healing
Shielding
Fight duration
Successful clears
Deaths
Benchmark-specific effectiveness
```

Create an aggregate **Benchmark Performance Score**, but always retain the component scores.

Example concept:

```text
Performance =
WeightedAverage(
    Burst,
    Sustained,
    Survival,
    MultiTarget,
    Attrition,
    ResistantBoss
)
```

Weights should be configuration-driven.

Do not hide major weaknesses behind the aggregate score.

---

# 13. Defining Build Percentiles

Percentiles should be derived from the generated candidate population.

Example:

```text
P50
P75
P90
P95
P99
```

Be precise about the interpretation.

If the optimizer population is intentionally stronger than random player builds, then:

```text
P50 generated build
```

does not necessarily equal:

```text
median real player
```

Initially, use percentile terminology only for the generated balance population.

Live-player percentiles can be introduced later if telemetry becomes available.

---

# 14. Selecting Representative Builds

For each useful Essence Profile:

1. Generate/evaluate a sufficiently large candidate population.
2. Determine the target percentile range.
3. Select strong candidates around that range.
4. Apply diversity filtering.
5. Retain a small representative set.

Example:

```yaml
representativeBuilds:
  perProfile: 10
```

A profile may intentionally retain more builds if its variance is unusually large.

The representative build library should be versioned with the balance run.

---

# 15. Progression Power Anchors

A **Power Anchor** combines:

```text
Gear Package
+
Essence Profile
```

and measures the resulting real combat performance.

Example:

```csharp
public sealed record PowerAnchorDefinition
{
    public required string Id { get; init; }

    public required string GearPackageId { get; init; }

    public required string EssenceProfileId { get; init; }
}
```

Example:

```text
R1_Start:
T1 / Common / Fine
4 Essences / P75

R1_End:
T1 / Rare / Exceptional
6 Essences / P75
```

The benchmark runner measures the actual performance of the representative characters generated from these definitions.

---

# 16. Progression Bands

A **Progression Band** represents a range of content progression using a start and end anchor.

For World Tower, a natural structure is one band per major progression block.

Example:

```yaml
worldTowerBands:

  region1:

    floors:
      start: 1
      end: 10

    startAnchor:
      gear:
        tier: 1
        rarity: Rare
        quality: Exceptional

      essences:
        slots: 4
        percentile: 75

    endAnchor:
      gear:
        tier: 1
        rarity: Epic
        quality: Exceptional

      essences:
        slots: 6
        percentile: 75

    powerCurve:
      type: Smooth
```

This means:

```text
Floor 1
≈ T1 Rare Exceptional + competent 4-Essence build

Floor 10
≈ T1 Epic Exceptional + competent 6-Essence build
```

Floors 2-9 do not require manually maintained character profiles.

Their target power can be interpolated between the measured start and end power.

---

# 17. Interpolated Power Curves

Suppose simulation produces:

```text
Region 1 Start Power = 1,000
Region 1 End Power   = 2,200
```

Floors 1-10 can then target a curve such as:

| Floor | Target Power |
|---:|---:|
| 1 | 1,000 |
| 2 | 1,110 |
| 3 | 1,225 |
| 4 | 1,350 |
| 5 | 1,480 |
| 6 | 1,620 |
| 7 | 1,770 |
| 8 | 1,930 |
| 9 | 2,100 |
| 10 | 2,200 |

These values are illustrative.

The curve may be:

```text
Linear
Ease-in
Ease-out
Exponential
Custom
```

Start with a simple configurable curve.

The important design decision is:

> The endpoints describe progression intent; the intermediate floors derive their expected power from the band.

---

# 18. Optional Mid-Band Anchors

If interpolation proves too coarse, allow optional checkpoints.

Example:

```text
Floor 1  → explicit anchor
Floor 5  → optional anchor
Floor 10 → explicit anchor
```

Do not require intermediate anchors by default.

They exist only where the progression curve materially changes.

---

# 19. Combat Rating Validation

Every representative character should have:

```text
Combat Rating
Benchmark Performance Score
Component benchmark scores
```

Measure the relationship between CR and simulated performance.

Required analyses:

```text
Correlation
Performance spread within CR bands
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

For each band:

```text
Median performance
P10 performance
P90 performance
Performance spread
```

---

# 20. CR Health Metrics

Use multiple statistics rather than one magic number.

Possible metrics:

```text
Spearman correlation
R²
Mean absolute prediction error
Performance variance within CR bands
```

Report a human-readable classification:

```text
Excellent
Good
Concerning
Poor
```

Example:

```text
Combat Rating Predictive Accuracy

Tier 1: Good
Tier 2: Good
Tier 3: Concerning
```

The purpose is to detect whether characters with similar CR regularly exhibit dramatically different real combat power.

---

# 21. CR Outlier Detection

Example:

```text
CR OUTLIER

Build:
E6-P75-07

Combat Rating:
4,221

Expected performance at this CR:
9,800

Observed performance:
15,340

Difference:
+56.5%
```

Initially report:

```text
Essences used
Gear package
Archetype
Common pairings
Relevant benchmark breakdown
```

Automatic root-cause diagnosis can come later.

---

# 22. Essence Balance Analysis

Use both:

```text
Existing Admin Essence Simulator data
+
Build-optimizer usage data
```

They answer different questions.

The admin simulator answers questions like:

```text
How does an Essence perform in 1v1 or 3v3 comparisons?
```

The optimizer answers:

```text
How often does an Essence appear in strong PvE builds?
```

Track:

```text
Overall usage
P50 usage
P75 usage
P90 usage
P95 usage
P99 usage
Usage by archetype
Average performance when present
Common Essence partners
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

This should generate an investigation warning.

Do not automatically nerf Essences from these metrics.

---

# 23. Synergy Detection

Analyze common Essence pairs.

Initial approach:

```text
Observed performance with A+B
vs
Expected performance from A and B independently
```

Flag unusually strong or weak combinations.

Example:

```text
Bloodfang + Executioner

Expected uplift:
+8%

Observed uplift:
+31%

Synergy delta:
+23%
```

Start with:

```text
Pairs
Frequently occurring triples
```

Do not attempt exhaustive analysis of every 10-Essence combination.

---

# 24. Real Content Power Targets

Content should reference progression intent instead of a fixed manually authored Combat Rating wherever practical.

Example:

```csharp
public sealed record EncounterPowerTarget
{
    public required string ProgressionBandId { get; init; }

    public required double ProgressWithinBand { get; init; }

    public required int PartySize { get; init; }

    public required double DesiredClearRate { get; init; }
}
```

For World Tower Floor 6:

```text
Progression Band:
WorldTowerRegion1

Progress:
approximately 56% through band
```

The system derives target player power from the Region 1 progression curve.

---

# 25. Alternative Explicit Encounter Anchor

Some special encounters may deserve an explicit anchor rather than interpolation.

Example:

```yaml
worldBossX:

  powerAnchor:
    gear:
      tier: 3
      rarity: Epic
      quality: Exceptional

    essences:
      slots: 8
      percentile: 90

  desiredClearRate: 0.60
```

Support both models:

```text
Progression-band position
or
Explicit power anchor
```

---

# 26. Encounter Simulation

For every encounter:

1. Resolve the intended target power.
2. Resolve representative Gear Package(s).
3. Resolve appropriate Essence Profile(s).
4. Construct representative characters.
5. Generate teams where relevant.
6. Run many seeded simulations.
7. Calculate clear rate and combat statistics.
8. Compare actual result to intended difficulty.

Record:

```text
Clear rate
Average fight duration
Median fight duration
Remaining health
Deaths
Damage taken
Failure reason where practical
Player CR distribution
Team CR distribution
```

---

# 27. Team Generation

Do not test only identical copies of one optimized character.

For team content, generate varied combinations.

Example:

```text
Damage + Damage + Damage
Damage + Damage + Sustain
Damage + Defense + Sustain
Hybrid + Hybrid + Hybrid
Random representative composition
```

If archetypes are not yet formalized in the production game, balance-only archetype labels are acceptable as long as they are derived from behavior rather than hard-coded Essence lists.

The system should report if an encounter appears to require one narrow composition despite being intended as general progression content.

---

# 28. Recommended Combat Rating

After resolving the intended progression power and testing representative characters, derive:

```text
Recommended Player CR
Recommended Team CR
```

For multiplayer encounters, **Team CR should be the more fundamental internal value**.

Example:

```text
Recommended Team CR:
13,140

Approximate player recommendation:
4,380
```

The UI can display the simpler per-player number if that is more understandable.

---

# 29. World Tower Example

For Floors 1-10:

```text
Start:
T1
Common
Fine
4 Essences
P75 Essence profile

End:
T1
Rare
Exceptional
6 Essences
P75 Essence profile
```

The balance system:

```text
1. Measures the start anchor.
2. Measures the end anchor.
3. Creates a power curve from Floor 1 to Floor 10.
4. Assigns each floor a target benchmark power.
5. Simulates that floor against representative characters near that target.
6. Determines whether actual difficulty matches intended difficulty.
7. Derives a recommended CR.
```

No manually maintained Floor 2-Floor 9 character definitions are necessary unless a floor intentionally represents a special progression jump.

---

# 30. Encounter Calibration

Do not allow the system to freely rewrite arbitrary boss stats until a target win rate appears.

Boss mechanics and identity should remain manually authored.

Expose only controlled calibration values.

Initial recommendation:

```text
HealthMultiplier
DamageMultiplier
```

Possible later abstraction:

```text
EncounterPowerMultiplier
```

mapped through a predefined scaling rule.

Example:

```text
Encounter Power = 1.20

Health = x1.20
Damage = x1.12
Armor  = x1.06
```

---

# 31. Automatic Difficulty Search

If an encounter should have:

```text
Target clear rate = 65%
```

the calibrator may search for a suggested multiplier.

Example:

```text
1.00 → 91%
1.50 → 39%
1.25 → 72%
1.375 → 59%
1.31 → 66%
```

Suggested result:

```text
Encounter Power = 1.31
```

Use binary search or another bounded search strategy.

Do not automatically apply the result to production data.

Return it as a recommendation requiring developer approval.

---

# 32. Generic Builds vs Encounter-Specific Builds

Maintain two separate optimization purposes.

## 32.1 Generic Builds

Optimized against the benchmark suite.

They do not know which real boss they will fight.

Use these for:

```text
Progression anchors
Recommended CR
General content difficulty
Representative build library
```

---

## 32.2 Encounter-Specific Builds

Optimized directly against a specific encounter.

Use these only for:

```text
Cheese detection
Hard-counter detection
Immunity abuse
Infinite sustain
Burst exploits
Mechanic bypasses
```

Example:

```text
World Tower Floor 11

Generic P90 clear rate:
64%

Encounter-optimized P90 clear rate:
100%

Median optimized kill time:
14 seconds
```

This should produce a critical investigation warning.

It should not redefine the normal recommended CR.

---

# 33. Content Difficulty Reporting

Produce one record per encounter.

Example:

| Encounter | Target Power | Target Clear | Actual Clear | Result |
|---|---:|---:|---:|---|
| Tower Floor 1 | 1,000 | 75% | 77% | Healthy |
| Tower Floor 2 | 1,110 | 72% | 70% | Healthy |
| Tower Floor 3 | 1,225 | 70% | 83% | Too Easy |
| Tower Floor 10 | 2,200 | 60% | 58% | Healthy |

Include:

```text
Progression band
Progression position
Gear package
Essence profile
Recommended player CR
Recommended team CR
Observed CR range
Fight duration
Clear-rate confidence
Suggested calibration change
```

---

# 34. Statistical Sampling

Combat randomness means one simulation is insufficient.

Use multiple seeds.

Example:

```yaml
simulation:
  optimizerSeeds: 10
  finalValidationSeeds: 100
```

For expensive optimizer work:

```text
Candidate search:
few seeds

Representative finalist validation:
many seeds

Final encounter analysis:
many seeds
```

This provides a good performance/accuracy tradeoff.

---

# 35. Performance and Parallelism

The system may eventually execute millions of battles.

Design simulation state so runs can execute in parallel.

Requirements:

```text
No shared mutable combat state
Per-simulation random generator
Immutable or isolated snapshots
Thread-safe static definitions
```

Use local multicore execution first.

Do not introduce distributed infrastructure until local execution is proven insufficient.

Cache deterministic calculations such as:

```text
Gear package stats
Derived character stats
Essence static modifiers
Combat Rating
```

---

# 36. Structured Result Model

Store results independently from their presentation.

Example:

```csharp
public sealed record BalanceRunResult
{
    public required BalanceRunMetadata Metadata { get; init; }

    public required IReadOnlyList<EssenceBalanceResult> Essences { get; init; }

    public required IReadOnlyList<RepresentativeBuildResult> Builds { get; init; }

    public required IReadOnlyList<PowerAnchorResult> PowerAnchors { get; init; }

    public required CombatRatingAnalysis CombatRating { get; init; }

    public required IReadOnlyList<EncounterBalanceResult> Encounters { get; init; }

    public required IReadOnlyList<BalanceWarning> Warnings { get; init; }
}
```

Serialize to JSON.

Generate the Markdown summary from structured results.

This makes later additions possible:

```text
Web dashboard
Charts
Admin UI
CI output
Historical database
```

without changing the simulation pipeline.

---

# 37. Warning System

Introduce standardized warnings.

Example severities:

```text
Info
Warning
Critical
```

Example categories:

```text
EssenceOverperformance
EssenceUnderperformance
MandatoryEssence
SuspiciousSynergy
LowBuildDiversity
CombatRatingOutlier
CombatRatingPoorAccuracy
EncounterTooEasy
EncounterTooHard
EncounterCheese
EncounterCompositionDependency
SimulationInstability
ProgressionCurveAnomaly
```

Warnings should provide the underlying metrics used to produce them.

---

# 38. Historical Regression Tracking

Persist balance summaries and compare runs.

Track:

```text
Essence usage changes
Representative build performance
Power-anchor changes
CR accuracy changes
Encounter clear-rate changes
Recommended CR changes
Progression curve changes
```

Example:

```text
REGRESSION WARNING

World Tower Floor 7

Previous clear rate:
68%

Current clear rate:
49%

Change:
-19 percentage points
```

If practical, show relevant game-data or code changes between runs.

Do not require automatic causal diagnosis in the first version.

---

# 39. Future Live-Telemetry Integration

Offline simulation should be implemented first.

Later, real-player telemetry can validate simulator assumptions.

Potential telemetry:

```text
Character CR at attempt
Gear
Essences
Doctrine
Party composition
Encounter
Result
Fight duration
```

Compare:

```text
Simulated clear rate
vs
Actual live clear rate
```

Example:

```text
Floor 10

Simulator:
64%

Live players:
58%
```

This can eventually answer whether:

```text
Generated P75
```

actually resembles a competent live player.

This is a later enhancement and should not block the offline system.

---

# 40. Configuration

Keep balance thresholds outside code where practical.

Example:

```yaml
balance:

  representativeBuilds:
    perProfile: 10

  optimizer:
    populationSize: 500
    generations: 100
    eliteCount: 50
    mutationRate: 0.25
    randomInjectionRate: 0.10

  simulation:
    optimizerSeeds: 10
    finalValidationSeeds: 100

  diversity:
    targetMaximumSimilarity: 0.70

  warnings:

    mandatoryEssence:
      p95Usage: 0.80

    crOutlier:
      performanceDifference: 0.25

    contentClearRate:
      tolerance: 0.10
```

These values generate reports and warnings.

They should not automatically alter gameplay.

---

# 41. Recommended Delivery Order

Do not implement the entire system at once.

---

## Milestone 0 — Cleanup

**Status: Complete.**

Implement:

```text
Audit old balance infrastructure
Delete obsolete balance/testing systems
Preserve production combat
Preserve Admin Dashboard Essence Simulator
Fix dependency direction where needed
Verify tests
```

Deliver the cleanup summary before continuing.

---

## Milestone 1 — Balance Runner

**Status: Complete.** See [Milestone 1 Balance Runner](milestone-1-balance-runner.md) for its command, architecture boundary, outputs, and verification.

Implement:

```text
Balance CLI/executable
Production game-data loading
Deterministic combat simulations
JSON result persistence
Markdown reporting
```

Completion target:

```text
One command can load production data,
run a deterministic combat simulation,
and save a report.
```

---

## Milestone 2 — Gear Packages

**Status: Complete.** See [Milestone 2 Region 1 Gear Packages](milestone-2-region-1-gear-packages.md) for the implemented anchors, production construction path, report contract, and verification.

Implement:

```text
GearPackageDefinition
GearPackageFactory
Balanced gear package
Offensive gear package if useful
Defensive gear package if useful
Combat Rating calculation
```

Start with only the progression states immediately required for the World Tower proof of concept.

---

## Milestone 3 — Random Essence Builds

**Status: Complete.** See [Milestone 3 Random Essence Builds](milestone-3-random-essence-builds.md) for the implemented profiles, legality rules, output contract, and current CR interpretation.

Implement:

```text
Legal Essence-loadout generator
4-slot builds
5-slot builds
6-slot builds
Character construction
```

At this stage do not optimize yet.

Generate random builds and inspect:

```text
CR spread
Benchmark performance spread
```

---

## Milestone 4 — PvE Benchmark Suite

**Status: Complete.** See [Milestone 4 PvE Benchmark Suite](milestone-4-pve-benchmark-suite.md) for the implemented scenarios, scoring model, measured sample ranges, and report contract.

Implement the first small benchmark suite:

```text
Short single target
Sustained single target
High incoming damage
Three targets
Attrition
```

Add resistant/high-defense benchmarks afterward if needed.

Calculate:

```text
Component scores
Aggregate benchmark score
```

---

## Milestone 5 — Combat Rating Validation

**Status: In progress.** The statistical and reporting contract is documented in [Milestone 5 Combat Rating Validation](milestone-5-combat-rating-validation.md).

Implement:

```text
CR vs benchmark performance
CR bands
Correlation
Outlier detection
CR-health report
```

This should already provide useful insight before sophisticated optimization exists.

---

## Milestone 6 — Essence Optimizer

Implement:

```text
Genetic or beam-search optimizer
Mutation
Selection
Elitism
Random injection
Diversity penalty
```

Optimize Essence selection only.

---

## Milestone 7 — Essence Profiles + Representative Builds

Implement:

```text
E4 P50/P75/P90
E5 P50/P75/P90
E6 P50/P75/P90
```

Retain only a small configurable number of representative builds per profile.

Do not persist the full optimizer search population as long-term benchmark profiles.

---

## Milestone 8 — Power Anchors

Implement anchor measurement.

Initial World Tower proof of concept:

```text
Region 1 Start

Gear:
T1 Rare Exceptional

Essences:
4 slots / P75
```

and:

```text
Region 1 End

Gear:
T1 Epic Exceptional

Essences:
6 slots / P75
```

Measure:

```text
Benchmark power
CR distribution
Performance variance
```

---

## Milestone 9 — Progression Bands

Implement:

```text
WorldTower Region 1
Floor 1 → Floor 10
Start anchor
End anchor
Interpolation curve
```

Assign target power to each floor without creating explicit builds for each floor.

---

## Milestone 10 — World Tower Content Analysis

Integrate real World Tower encounters.

For Floors 1-10:

```text
Resolve target power
Select representative characters
Run combat simulations
Calculate clear rate
Derive recommended CR
Produce difficulty warnings
```

This is the first major end-to-end proof of the system.

---

## Milestone 11 — Essence Meta Analysis

Implement:

```text
Usage by build percentile
Common pairings
Pair synergy analysis
Mandatory-Essence warnings
Underused-Essence warnings
```

Use the existing admin simulator as complementary evidence, not a replacement for PvE optimization data.

---

## Milestone 12 — Encounter Calibration

Implement:

```text
Health multiplier
Damage multiplier
Bounded/binary difficulty search
Suggested balance changes
```

Do not automatically write suggested values back into production content.

---

## Milestone 13 — Encounter-Specific Optimization

Implement:

```text
Optimize specifically against a boss
Compare generic vs encounter-specific builds
Detect cheese/hard counters
```

---

## Milestone 14 — Additional Progression Bands

Once Region 1 works, extend to:

```text
World Tower Floors 11-20
World Tower Floors 21-30
...
```

or whatever actual progression structure the game uses.

Each band should normally require only:

```text
Start anchor
End anchor
Curve
```

rather than ten separate character definitions.

---

## Milestone 15 — Other Content

Integrate:

```text
Dungeons
Region bosses
Raids
Other fixed PvE encounters
```

Reuse the same power-anchor/progression-band model wherever possible.

---

## Milestone 16 — Historical Regression Analysis

Implement:

```text
Previous-run comparison
CR regressions
Power-anchor regressions
Encounter difficulty regressions
Essence meta changes
```

Optionally integrate into CI after runtime becomes acceptable.

---

## Milestone 17 — Equipment Optimization Only If Needed

Do not build this simply because it is theoretically possible.

Only expand the optimizer into:

```text
Blueprint selection
Equipment variants
Tempering configuration
Stat allocation
```

if evidence shows that standardized Gear Packages fail to represent meaningful equipment-build variance.

This milestone is intentionally late.

---

# 42. First Practical Vertical Slice

The first useful end-to-end implementation should be narrower than the full plan.

Target:

```text
1. Clean up old balance infrastructure.
2. Preserve the Admin Essence Simulator.
3. Create the Balance Runner.
4. Add deterministic simulations.
5. Create two Region 1 Gear Packages:
   - T1 Rare Exceptional
   - T1 Epic Exceptional
6. Generate legal 4-Essence and 6-Essence builds.
7. Add a small PvE benchmark suite.
8. Calculate benchmark performance.
9. Compare benchmark performance against CR.
10. Add Essence optimization.
11. Produce representative P75 4-Essence builds.
12. Produce representative P75 6-Essence builds.
13. Measure Region 1 start/end power anchors.
14. Interpolate Floor 1-Floor 10 target power.
15. Test actual World Tower Floors 1-10.
16. Produce recommended CR and difficulty reports.
```

This proves the core architecture without prematurely solving every balancing problem in the game.

---

# 43. Concrete World Tower Region 1 Example

Design intent:

```text
Floor 1:
Tier 1 gear
Rare rarity
Exceptional quality
4 Essences

Floor 10:
Tier 1 gear
Epic rarity
Exceptional quality
6 Essences
```

Assume both use:

```text
P75 Essence build quality
```

The balance system constructs:

```text
START ANCHOR

Gear Package:
T1_Rare_Exceptional

Essence Profile:
E4_P75
```

and:

```text
END ANCHOR

Gear Package:
T1_Epic_Exceptional

Essence Profile:
E6_P75
```

The benchmark runner measures:

```text
Start Power = 1,000
End Power   = 2,200
```

The progression model creates:

```text
Floor 1  = 1,000
Floor 2  = 1,110
Floor 3  = 1,225
Floor 4  = 1,350
Floor 5  = 1,480
Floor 6  = 1,620
Floor 7  = 1,770
Floor 8  = 1,930
Floor 9  = 2,100
Floor 10 = 2,200
```

Again, these values are illustrative.

For each floor, the system then simulates suitable representative characters around that target power.

Example Floor 7 result:

```text
WORLD TOWER FLOOR 7

Progression Band:
Region 1

Target Power:
1,770

Representative CR:
~3,450

Desired Clear Rate:
65%

Observed Clear Rate:
82%

Result:
Too Easy

Suggested Encounter Power:
1.12
```

Floor 7 does not need its own manually authored:

```text
Rarity
Quality
Essence count
Exact Essence loadout
Exact equipment
```

unless game design specifically requires a discrete progression jump there.

---

# 44. Example Full Balance Report

```text
BALANCE RUN
========================================

SUMMARY
----------------------------------------
Critical warnings: 2
Warnings: 11

ADMIN ESSENCE SIMULATOR
----------------------------------------
Status: Preserved
1v1: Operational
3v3: Operational

REPRESENTATIVE BUILDS
----------------------------------------
E4 P75: 10 retained
E5 P75: 10 retained
E6 P75: 10 retained

POWER ANCHORS
----------------------------------------
Region 1 Start: 1,000
Region 1 End:   2,200

COMBAT RATING
----------------------------------------
Tier 1 predictive accuracy: Good
Largest positive outlier: +31%
Largest negative outlier: -22%

ESSENCE BALANCE
----------------------------------------
Potentially mandatory: 2
Potentially overtuned: 5
Suspicious synergies: 3

WORLD TOWER
----------------------------------------
Floor 1:  Healthy
Floor 2:  Healthy
Floor 3:  Too Easy
Floor 4:  Healthy
Floor 5:  Healthy
Floor 6:  Healthy
Floor 7:  Too Easy
Floor 8:  Healthy
Floor 9:  Slightly Hard
Floor 10: Healthy

CHEESE DETECTION
----------------------------------------
Floor 8:
Encounter-specific P90 build kills boss
42% faster than generic P90 baseline.

RECOMMENDED INVESTIGATIONS
----------------------------------------
1. Review Essence X + Essence Y synergy.
2. Review Floor 7 encounter scaling.
3. Review CR treatment of summon damage.
```

---

# 45. Guardrails

## Do not make CR the authority

Simulation measures real performance.

CR is validated against it.

---

## Do not create a benchmark profile for every possible progression state

Use:

```text
Progression Bands
Power Anchors
Gear Packages
Essence Profiles
Representative Builds
```

---

## Do not make Floor 2-Floor 9 require explicit character definitions if Floor 1 and Floor 10 already define the progression band

Interpolate unless the content intentionally contains a progression discontinuity.

---

## Do not balance normal progression around P99

Extreme optimization is valuable for stress testing, not as the default expectation.

---

## Do not assume PvP Essence strength equals PvE strength

Keep the existing 1v1/3v3 simulator, but use PvE benchmarks for PvE optimization.

---

## Do not optimize equipment in the first version

Use standardized Gear Packages first.

Only add equipment optimization if the data proves it is necessary.

---

## Do not allow encounter auto-tuning to rewrite encounter identity

Mechanics remain manually designed.

Automated calibration should operate only through controlled numerical parameters.

---

## Do not automatically apply balance recommendations

The system analyzes and proposes.

A developer decides.

---

## Do not let the optimizer collapse to one meta build

Representative build diversity is a first-class requirement.

---

## Do not hide benchmark-specific weaknesses behind a single aggregate score

Retain component performance data.

---

# 46. Definition of Success

The system is successful when a developer can define something as simple as:

```text
World Tower Region 1

Floor 1 expectation:
T1 Rare Exceptional
4 Essences
P75 build quality

Floor 10 expectation:
T1 Epic Exceptional
6 Essences
P75 build quality
```

and the balance runner can automatically determine:

```text
Measured start power
Measured end power
Power target for Floors 1-10
Representative Combat Rating at each point
Expected clear rate against each floor
Which floors are too easy or too hard
Suggested controlled encounter scaling
Whether specific Essence combinations break expectations
Whether CR still predicts actual character strength
```

without requiring manually maintained bespoke builds for every floor.

That is the central architectural objective.

---

# 47. Immediate Codex Task

Begin with **Milestone 0**.

First audit and clean the existing balance/testing infrastructure while preserving the Admin Dashboard Essence Simulator.

Then implement the smallest vertical slice required to prove:

```text
Gear Package
+
Essence Profile
+
Representative Builds
+
PvE Benchmark Power
+
Progression Band
+
World Tower Encounter Simulation
+
Recommended CR
```

Do not prematurely implement:

```text
Full equipment optimization
Distributed simulation
Live telemetry
Complex dashboards
Automatic production-data mutation
Every World Tower region
Every PvE system
```

Establish the Region 1 Floor 1-Floor 10 pipeline first.

Once that pipeline is coherent, deterministic, and produces useful results, extend it incrementally.
