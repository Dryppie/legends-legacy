# Balance Harness Plan

## 1. Balance Goals / Constraints

- Define balance goals per content type
- Define the primary success metric for each scenario
- Define secondary diagnostic metrics separately
- Target win / clear rates where applicable
- Target fight duration only where duration is relevant
- Expected character or party power level
- Expected progression point
- Acceptable build performance variance
- Role expectations where applicable
- Required mechanical checks
- Failure conditions / enrage limits where applicable
- Expected difficulty bands

### Example Content Goals

- Normal enemies: fight duration + win rate
- Elites: win rate + survivability + duration range
- Bosses: clear rate as the primary metric
- World Tower: floor clear rate by party strength
- PvP: matchup win-rate distribution

### Metric Classification

- Primary metrics determine whether the balance target is met
- Diagnostic metrics explain why a result occurred
- A diagnostic metric should not cause a failure unless that metric is explicitly part of the scenario's balance goal

## 2. Scenario Definitions

- Single-target
- Multi-target
- Burst
- Sustained combat
- High-defense enemies
- High-damage enemies
- Healing / sustain checks
- Crowd-control scenarios
- Dungeon / boss scenarios
- World Tower scenarios

## 3. Benchmark Suite

- Standard benchmark characters
- Standard benchmark essence loadouts
- Standard benchmark parties
- Standard benchmark encounters
- Standard progression tiers
- Fixed RNG seeds where appropriate
- Expected result ranges
- Performance / runtime benchmarks
- Historical benchmark results
- Compare current results against previous versions

## 4. Essence Simulator

- Individual essence performance
- Active / passive contribution
- Synergy testing
- Scaling by level / ascension
- Scenario-specific usefulness
- Detect overperforming and underperforming essences

## 5. Essence Scoring Model

- Damage contribution
- Survivability contribution
- Healing / support contribution
- Control contribution
- Utility contribution
- Synergy value
- Overall score
- Scenario-specific scores

## 6. Character Profiles

- Representative progression stages
- Equipment tiers
- Essence levels
- Ascension states
- Different archetypes / builds
- Weak / average / optimized characters

## 7. Build Generator

- Generate essence loadouts automatically
- Random builds
- Archetype-based builds
- Optimized builds
- Intentionally bad builds
- Detect dominant combinations

## 8. Party Creator

- Five-character World Tower parties
- Role-balanced parties
- Random parties
- Optimized parties
- Extreme / specialized parties
- Duplicate / non-duplicate build configurations

## 9. Enemy / Encounter Profiles

- Standard monsters
- Elites
- Bosses
- Dungeon encounters
- World Tower floors
- Different stat / mechanic profiles

## 10. Combat Simulator

- Use the real combat engine
- Seeded / reproducible simulations
- Large batches of repeated battles
- Capture detailed combat telemetry

## 11. Simulation Matrix

- Define which builds fight which encounters
- Character × encounter
- Essence × scenario
- Party × World Tower floor
- Progression tier × content tier

## 12. Metrics Collector

- Win rate
- Fight duration
- Damage dealt
- Damage taken
- Healing
- Death timing
- Ability usage
- Essence contribution
- Resource usage
- Variance

## 13. Baseline System

- Store current known-good balance
- Store benchmark outputs
- Compare changes against baseline
- Detect balance regressions
- Track historical balance movement

## 14. Outlier / Dominance Detection

- Overpowered essences
- Useless essences
- Mandatory combinations
- Unviable builds
- Excessive synergy
- Difficulty spikes

## 15. Balance Rules / Assertions

- Define measurable balance requirements
- Prevent individual essences from dominating every scenario
- Keep optimized parties within intended power ranges
- Ensure expected progression builds can clear intended content

## 16. Parameter / Tuning Layer

- Damage coefficients
- Cooldowns
- Proc chances
- Stat scaling
- Enemy stats
- Encounter modifiers
- Centralized values for easy experimentation

## 17. Automatic Tuning / Search

- Try parameter variations
- Find values closer to balance targets
- Rank suggested changes
- Recommend changes rather than automatically modifying production values initially

## 18. Orchestrator

- Build content snapshots
- Generate profiles, builds, and parties
- Select benchmark and exploratory scenarios
- Run simulation matrix
- Aggregate results
- Execute balance checks
- Compare against benchmark baselines
- Produce recommendations

## 19. Reporting

- Balance scorecards
- Benchmark comparison reports
- Essence rankings
- Scenario rankings
- Party performance
- World Tower difficulty curve
- Regression reports
- Suggested tuning changes

## 20. Reproducibility / Versioning

- Content snapshot
- Game version
- RNG seeds
- Balance configuration version
- Ability / essence definitions used
- Benchmark suite version

## 21. Validation Harness

- Ensure simulator behavior matches actual gameplay
- Detect differences between harness behavior and the real combat engine
- Prevent simplified simulation logic from producing misleading balance results

---

## Benchmarking Flow

**Benchmark Suite → Simulator → Metrics → Baseline Comparison → Regression Detection**

The **Benchmark Suite** defines the fixed reference tests.

The **Baseline System** stores previous benchmark results and provides the comparison point used to detect regressions.

---

## Core Priorities

Beyond the simulator, character profiles, party creator, and orchestrator, the most important additional systems are:

1. **Scenario Definitions**
2. **Balance Goals / Targets**
3. **Benchmark Suite**
4. **Build Generator**
5. **Metrics / Analysis**
6. **Baseline / Regression System**

## Suggested Separation of Responsibilities

### Combat Simulator

Answers:

> What happened during the fight?

Responsible for executing battles and recording their results.

### Essence Analysis

Answers:

> How much did each essence contribute, and in which situations is it useful?

Responsible for converting combat results into usefulness, contribution, synergy, and scenario-specific scores.

### Benchmark Suite

Answers:

> Are the same representative builds and encounters behaving better, worse, or differently than before?

Responsible for defining stable reference cases that can be rerun after balance or code changes.
