# Dungeon Recommended Power Recalibration Plan

## Purpose

This document defines the plan for rebuilding Dungeon Recommended Power after implementation of the attribute and equipment balancing system.

The target definition is:

> Dungeon Recommended Power is the Overall Power of the weakest realistic, balanced build that completes the dungeon at the approved target success rate.

Recommended Power must remain simulation-backed. It must not be calculated by adding static attribute weights, copying Gear Value, or applying a hand-authored multiplier to enemy statistics.

The equipment balancing system and the Power system have different responsibilities:

- **Equipment Item Budget** determines how much fairly priced attribute value an item contains.
- **Overall Power** measures what a complete build can accomplish in the production combat engine.
- **Dungeon Recommended Power** identifies the Overall Power associated with a realistic build that can clear a specific dungeon reliably.
- **Dungeon Readiness** directly simulates the actual selected player build and remains the most authoritative prediction for that player.

## Current state

The repository already contains a simulation-backed foundation:

- `PowerRatingService` finds the highest neutral benchmark intensity a real build can defeat.
- `DungeonPowerAnalyzer` searches for a canonical build capable of reaching the dungeon completion target.
- `PowerAnalysisSimulationRunner` runs the production combat engine, generated dungeon routes, tier-scaled enemies, Vigor attrition, and Rest Sites.
- `DungeonReadinessService` directly simulates a real player build through the selected dungeon.
- `DungeonPowerCalibrationWorker` loads versioned recommendations and recalibrates missing or stale entries.
- recommendations are persisted in `DungeonPowerRecommendationCacheEntries`.
- cache identities include dungeon content, algorithm, combat-rules, benchmark-definition, and recommendation-seed versions.

Current relevant versions:

- Power algorithm: `13`
- Combat rules: `4`
- Benchmark definition: `6`
- Recommendation seed set: `2`
- Equipment balance: `5`

The architecture is directionally correct. The missing connection is that dungeon calibration still uses synthetic intensity characters rather than builds funded by the real equipment system.

## Current problems

### Synthetic canonical characters

`PowerAnalysisSimulationRunner.CreateCanonicalCombatantAsync` currently derives canonical attributes from arbitrary intensity formulas, including:

- Max Health from `100 + intensity × 18`;
- Power from `10 + intensity × 3`;
- Armor and Resistance from `intensity × 1.5`;
- direct profile multipliers;
- special Spirit and Health Regeneration grants for the sustain profile.

These builds are not assembled from:

- real equipment slots;
- tier budgets;
- active attribute prices;
- quality or rarity;
- recipe or blueprint profiles;
- tempering;
- primary-derived attribute costs;
- equipment and combat caps.

Consequently, increasing intensity does not necessarily represent a realistic or evenly funded progression step.

### Invalid low-end ranges

A canonical profile can clear an easy dungeon while receiving zero Overall Power from the neutral benchmark. This produces a zero lower recommendation and fails the requirement that the recommendation sit inside a positive lower/upper range.

This is evidence of a low-end benchmark-resolution mismatch. It should not be hidden by clamping the recommendation to an arbitrary positive number.

### Discontinuous family progression

Adjacent dungeon difficulties can produce recommendations separated by more than the current `4x` diagnostic limit.

Possible causes include:

- synthetic canonical stat growth;
- combat rounding;
- Health Regeneration survival breakpoints;
- defensive diminishing returns;
- an actual dungeon-content difficulty jump;
- a low recommendation close to zero exaggerating the ratio;
- different adaptive-search and validation seed sets.

The present diagnostic marks both adjacent recommendations as invalid. That can remove a valid recommendation alongside the actual outlier and leave an entire family unavailable.

### Search assumptions

The current exponential/binary search assumes that canonical intensity is monotonic: every higher intensity should be at least as capable as the preceding intensity.

That assumption is unsafe when the synthetic progression crosses:

- stat caps;
- cooldown and attack-speed breakpoints;
- integer damage or healing thresholds;
- five-second regeneration survival thresholds;
- different fight-duration boundaries.

### Statistical inconsistency

The adaptive search and final validation use different fixed seed sets. The search currently uses eight seeds, while final profile validation uses 24.

An intensity can therefore pass the search sample and produce a materially different completion rate during final validation.

## Target recommendation contract

For each dungeon:

1. Construct an ordered ladder of realistic, attainable canonical builds.
2. Simulate each Balanced build against deterministic dungeon routes.
3. Find the first build that reaches the approved completion target.
4. Verify that the preceding build does not reach the target.
5. Calculate the passing build's Overall Power using the normal Power benchmark.
6. Publish that Overall Power as `RecommendedPartyPower`.
7. Evaluate specialized builds around the same progression region to produce matchup range and confidence.

The current `72%` completion target can remain initially, but its statistical interpretation must be made explicit.

## Phase 1: deterministic canonical equipment builds

Introduce a canonical build factory, tentatively:

```text
CanonicalEquipmentBuildFactory
```

It should construct detached combatants for analysis without creating or persisting player-owned items.

Every build must use:

- the active equipment balance profile;
- the real full-equipment slot budget;
- real hand configurations;
- active tier-specific attribute costs;
- the constrained equipment allocator;
- direct and primary-derived cap accounting;
- deterministic allocation;
- production combat attribute construction;
- fixed canonical abilities appropriate to the profile.

Do not duplicate attribute prices or allocation weights in the Power subsystem. Extract or reuse the canonical loadout definitions already exercised by the attribute-balance analyzer.

### Canonical identities

Retain the following identities:

- **Balanced**: determines the displayed recommendation.
- **Offensive**: exposes single-target damage sensitivity.
- **Defensive**: exposes physical and magical durability sensitivity.
- **Sustain**: exposes healing, barrier, and Health Regeneration sensitivity.
- **Area**: exposes multi-target sensitivity.

All profiles must receive the same total progression funding at a given ladder rung. They may distribute that funding differently, but no profile may receive free attributes outside the shared budget.

### Ability rules

- All numeric ability output continues to scale from Power.
- Canonical abilities remain isolated simulation definitions.
- Every profile must have enough baseline offense to finish ordinary encounters.
- Sustain and defensive profiles must not be judged successful merely for surviving until the combat tick limit.
- Dungeon completion still requires actual room victories and successful route completion.

## Phase 2: reachable progression ladder

Replace synthetic intensity with an ordered, deterministic build ladder.

The ladder should represent meaningful equipment progression states, including:

1. entry/base equipment;
2. increasing equipment tiers;
3. increasing quality;
4. increasing rarity;
5. tempering progression;
6. blueprint-enhanced variants where appropriate;
7. Masterwork quality;
8. Legacy rarity;
9. the complete maximum-progression loadout.

The exact ladder must be derived from active crafting rules rather than copied into a second hand-authored table.

### Maximum equipment

Maximum-quality, Legacy-rarity equipment is a ceiling test, not the basis for every recommendation.

It should prove that:

- every supported dungeon can be completed by at least one attainable maximum build;
- no recommendation exceeds attainable maximum Power;
- the hardest dungeon still exercises meaningful portions of maximum progression.

An early dungeon should be calibrated against the first realistic build that clears it, not against Tier-10 maximum equipment.

### Character progression outside equipment

The canonical ladder must explicitly define its treatment of:

- character level and base attributes;
- permanent bonuses;
- Essence slots and Essence progression;
- abilities available at each progression stage.

Recommended initial approach:

- use deterministic level/base-stat anchors;
- use fixed canonical abilities;
- exclude player-specific permanent bonuses;
- use a declared canonical Essence policy;
- allow the Power benchmark to account for the resulting complete combatant.

Whatever policy is selected must be versioned and included in calibration diagnostics.

## Phase 3: Power-scale validation

Before generating dungeon recommendations, validate the Overall Power scale against the canonical progression ladder.

Required properties:

- every valid entry build has positive Overall Power;
- Overall Power is non-decreasing across progression;
- material progression steps usually increase Power;
- no ordinary step causes an unexplained multi-fold jump;
- the maximum build remains below `MaximumBenchmarkIntensity`;
- equal-budget profile variations remain reasonably comparable in Overall Power;
- Power remains derived from production-engine results rather than Gear Value.

If realistic entry builds still receive zero Power, adjust the neutral benchmark's low-end resolution.

Do not fix this by assigning an arbitrary minimum recommendation.

### Versioning

Use the version according to the scope of the change:

- bump `PowerRatingAlgorithm.Version` if the player-facing Overall Power scale changes;
- bump `BenchmarkDefinitionVersion` if canonical/neutral benchmark definitions change without changing broad semantics;
- bump `RecommendationSeedSetVersion` if recommendation seeds change;
- bump `CombatRulesVersion` only when runtime combat behavior changes.

The canonical recommendation identity must also depend on the active equipment balance version. This can be represented explicitly in the persisted identity or through a clearly documented benchmark-definition version dependency.

## Phase 4: replace intensity search

Replace `FindMinimumCanonicalIntensityAsync` with a discrete progression search.

Suggested flow:

```text
for each canonical progression rung, weakest to strongest:
    build the Balanced combatant
    run deterministic dungeon simulations
    record completion and confidence
    stop at the first approved passing rung

validate the preceding rung as failing
calculate Overall Power for the passing build
evaluate specialized profiles near the passing rung
```

A binary search may be used only after the ladder has been proven monotonic. Even then, the immediate failing and passing neighbors must be explicitly simulated and retained in the report.

### Recommendation semantics

- `RecommendedPartyPower`: Overall Power of the first passing Balanced build.
- `LowerRecommendedPower`: lowest positive passing Power among valid specialized profiles.
- `UpperRecommendedPower`: highest passing Power among valid specialized profiles.
- `Confidence`: derived from statistical confidence, unavailable profiles, and profile spread.
- `EstimatedRunDuration`: average duration from the final validation sample.
- `CanonicalPartyCompletionRates`: final validation rates, not adaptive-search rates.

One specialized profile failing to calibrate should lower confidence and remain visible in diagnostics. It should not automatically erase a valid Balanced recommendation.

## Phase 5: statistical calibration

Use one declared deterministic seed population for both selection and final validation.

Recommended initial policy:

- at least 24 final simulations per candidate;
- deterministic route and combat seeds;
- a recorded Wilson completion interval;
- no passing decision from a separate, smaller seed population;
- optional small-seed probing only for finding a candidate region;
- mandatory full-seed validation for the passing and preceding rungs.

The report should include:

- attempts and completions;
- point completion estimate;
- confidence interval;
- checkpoint reach rate;
- average and percentile duration;
- furthest room reached;
- failure reasons;
- actual and potential Health Regeneration;
- regeneration utilization;
- damage taken and prevention;
- remaining health;
- build funding and progression identity.

## Phase 6: non-destructive diagnostics

Separate intrinsic recommendation validity from family progression diagnostics.

### Intrinsic errors

Reject only the affected recommendation for:

- non-positive `RecommendedPartyPower`;
- invalid lower/recommended/upper ordering;
- zero simulations;
- invalid completion rates;
- non-positive duration;
- missing Balanced result;
- non-finite output;
- no attainable passing build.

### Family diagnostics

Family-level checks should initially report warnings for:

- non-increasing difficulty;
- unusually large adjacent jumps;
- unexpectedly small adjacent increases;
- large canonical-profile disagreement;
- extreme changes in duration or failure mode.

Do not automatically remove the preceding valid recommendation when the next difficulty is an outlier.

If an authored `_ii` or `_iii` dungeon is genuinely easier than its predecessor, treat that as dungeon-content evidence. Do not silently force the recommendations into a monotonic curve.

### Publication

Calibration should:

1. calculate candidates into a staging collection;
2. validate each candidate;
3. validate complete dungeon families;
4. emit a reviewable report;
5. publish and persist only the accepted set.

Avoid mutating the live recommendation store while the candidate set is still being validated.

## Phase 7: developer workflow

Move experimental recalibration out of ordinary startup iteration.

Preferred workflow:

1. run an explicit Admin or command-line calibration operation;
2. generate a complete report without persistence;
3. review failures and outliers;
4. correct Power benchmarks, canonical builds, or dungeon content;
5. rerun until all release gates pass;
6. explicitly persist the approved recommendation set.

The startup worker can remain responsible for:

- loading current persisted recommendations;
- identifying stale identities;
- optionally invoking the approved calibrator;
- atomically publishing only validated results.

Startup logging should summarize results and provide concise failure diagnostics without hiding detailed reports.

## Phase 8: automated tests

### Canonical build tests

- Every profile spends the same authorized budget at each rung.
- No profile exceeds item or combat caps.
- Direct and primary-derived cap use is counted together.
- Builds are deterministic.
- Builds use the active equipment balance version.
- No profile receives unbudgeted attributes.

### Power progression tests

- Entry builds have positive Power.
- Power is non-decreasing across the ladder.
- Maximum equipment remains within the benchmark search range.
- Repeated ratings are identical.
- Equal-funded profiles stay within an approved Overall Power spread.

### Dungeon threshold tests

- The selected Balanced rung reaches the target.
- The preceding Balanced rung does not reach the target.
- Final rates use the declared seed set.
- Final outputs contain positive Power, simulation count, and duration.
- Specialized profile failures produce Low Confidence rather than destroying the Balanced recommendation.

### Family tests

- Difficulty suffixes are evaluated in authored order.
- Intrinsic failure removes only the invalid recommendation.
- An outlier warning does not remove its valid neighbor.
- Staged family publication is atomic.
- Real authored dungeon families produce reviewable monotonic results or explicit content failures.

### Maximum progression tests

- Every supported dungeon is clearable by at least one approved maximum build, or is explicitly marked unsupported.
- No recommendation exceeds maximum attainable Overall Power.
- Maximum Health Regeneration cannot create a false completion through timeout survival.
- Long-fight regeneration and duration remain bounded and observable.

### Cache and version tests

- Equipment balance changes invalidate canonical recommendations.
- benchmark changes invalidate recommendations;
- seed changes invalidate recommendations;
- combat-rule changes invalidate recommendations;
- stale rows never enter the live store;
- repeated approved calibration persists identical payloads.

## Calibration acceptance criteria

A dungeon recommendation is approved only when:

- the Balanced build has a positive Overall Power;
- the Balanced build reaches the completion target;
- the preceding progression rung fails the target;
- the result uses the full final seed population;
- lower, recommended, and upper Power form a positive ordered range;
- duration and simulation counts are positive;
- completion rates are within `[0, 1]`;
- all combat and recommendation outputs are finite;
- the recommendation does not exceed attainable maximum Power;
- the result is deterministic;
- no intrinsic diagnostic error remains.

A complete dungeon family is approved when:

- every required difficulty has an approved recommendation;
- progression is explainable;
- any large adjacent jump is reviewed against dungeon content;
- no valid entry was removed because of a neighbor's failure;
- the family can be published atomically.

## Recommended implementation order

1. Add a read-only calibration report that exposes the current raw canonical intensities, Overall Power values, completion rates, and diagnostic failures.
2. Extract shared canonical loadout identities from the attribute/equipment analyzer.
3. Implement `CanonicalEquipmentBuildFactory`.
4. Generate and validate the reachable progression ladder.
5. Add low-end and maximum-end Overall Power progression tests.
6. Replace synthetic intensity search with discrete build-rung selection.
7. Unify selection and validation seeds.
8. Separate intrinsic errors from family warnings.
9. Stage and atomically publish recommendation families.
10. Add equipment balance versioning to recommendation identity.
11. Increment the appropriate Power versions.
12. Generate the full authored-dungeon calibration report.
13. Review and fix genuine dungeon-content outliers.
14. Persist the approved recommendations.
15. Update `power-rating-system.md` with the final contract and workflow.

## Definition of done

The Dungeon Recommended Power recalibration is complete when:

- canonical recommendation builds are funded through the active equipment system;
- synthetic direct-stat intensity formulas no longer determine dungeon recommendations;
- the full progression ladder is attainable, deterministic, and cap-safe;
- entry and maximum builds have valid Power;
- the first passing Balanced rung defines each recommendation;
- the preceding rung is proven insufficient;
- specialized profiles affect range and confidence without invalidating a sound Balanced result;
- all authored dungeons have positive, deterministic recommendations;
- family diagnostics no longer cascade-delete valid neighbors;
- all supported dungeons remain within attainable maximum progression;
- recommendation cache identity includes every relevant combat, benchmark, seed, content, and equipment-balance dependency;
- the full calibration report passes;
- relevant backend tests pass;
- documentation matches the implemented semantics;
- approved recommendations are persisted and load cleanly without recalibration errors.

## Primary implementation locations

- `LL/src/Infrastructure/Service/Services.LL/PowerRatings/PowerAnalysisSimulationRunner.cs`
- `LL/src/Infrastructure/Service/Services.LL/PowerRatings/DungeonPowerAnalyzer.cs`
- `LL/src/Infrastructure/Service/Services.LL/PowerRatings/PowerRatingService.cs`
- `LL/src/Infrastructure/Service/Services.LL/PowerRatings/DungeonPowerRecommendationDiagnostics.cs`
- `LL/src/Infrastructure/Service/Services.LL/PowerRatings/DungeonReadinessService.cs`
- `LL/src/API/API.LL/HostedServices/DungeonPowerCalibrationWorker.cs`
- `LL/src/Core/Application/Interfaces/Services/LL/PowerRatings/IPowerRatingService.cs`
- `LL/src/Core/Domain/Models/Professions/Crafting/V2/EquipmentStatBudgetCatalog.cs`
- `LL/src/Core/Domain/Models/Professions/Crafting/V2/EquipmentConstraintProfile.cs`
- `LL/src/Infrastructure/Service/Services.LL/Balance/AttributeMarginalValueAnalyzer.cs`
- `LL/tests/EssenceSystem.Tests/DungeonPowerRecommendationDiagnosticsTests.cs`
- `LL/tests/EssenceSystem.Tests/DungeonPowerPersistenceTests.cs`
- `LL/tests/EssenceSystem.Tests/PowerRatingCoreTests.cs`
- `LL/docs/power-rating-system.md`

## Handoff note

The first implementation task should be the deterministic equipment-funded canonical build factory and its progression tests.

Do not begin by deleting cached recommendations, weakening diagnostics, or manually editing dungeon Power values. Those actions would hide the calibration mismatch without connecting Recommended Power to the equipment system.
