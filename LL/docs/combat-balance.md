# Combat calibration

## Purpose

The combat calibration framework answers progression and PvE pacing questions by running representative, obtainable players through the production combat engine. Combat Rating remains a useful curve diagnostic, but simulated combat behavior is the source of truth for encounter assessment.

The dependency direction is:

```text
authored progression + real equipment/Essences
                    |
                    v
           canonical player profile
                    |
                    v
           production combat engine
                    |
                    v
             measured metrics
                    |
                    v
        typed difficulty envelope
                    |
                    v
       area/progression report + tests
```

Production combat does not depend on calibration code or tests. Calibration consumes existing application contracts and `FastCombatEngine` through `PowerAnalysisSimulationRunner`.

## Assessment of the previous tooling

Useful pieces already existed and remain in use:

- `CanonicalEquipmentBuildFactory` constructs deterministic players from authored crafting recipes, real item bases, item stat rolls, tempering, character levels, and representative Essence loadouts.
- `PowerAnalysisSimulationRunner` executes the production combat engine with deterministic seeds and already supports multiple friendly participants.
- `RegionCreatureScalingProvider` owns versioned Region/Area baselines.
- `CreatureScaler` applies the area baseline, then creature archetype, damage/defense identity, and authored overrides.
- specialized dungeon, World Tower, raid, equipment pacing, and Area analyzers contain valuable content-specific logic.

The difficulty was separation of concerns. Specialized analyzers each chose their own profiles, thresholds, aggregation, and messages. Region progression was represented by build IDs and global steps rather than a first-class checkpoint. The Area analyzer reduced balance mostly to average and minimum win rate. Adding another context therefore meant duplicating player selection and deciding new hardcoded thresholds.

The new layer keeps the real build and simulation paths while replacing scattered interpretation with these concepts:

- `ProgressionCheckpoint`
- `CalibrationStrengthBand`
- `CalibrationArchetype`
- `CalibrationEncounterType`
- `CombatCalibrationMetrics`
- `CombatDifficultyEnvelope`
- `CombatCalibrationAssessment`
- `AreaCalibrationReport`
- `ProgressionCurveReport`

## Authoritative progression model

The authoritative Region/Area mapping remains `Data/progression/region-combat-balance.json`. Each calibrated Area supplies:

- Region and ordered Area placement;
- global progression step;
- configured expected equipment build ID;
- recommended Combat Rating;
- the creature scaling profile.

The Area content in `Data/world/regions.json` supplies character level and actual creature membership. `CombatCalibrationService.GetCheckpointAsync` combines both sources and derives equipment tier and currently unlocked Essence slots.

Do not duplicate these values in tests. Tests ask the service for a checkpoint by Area ID.

### Player progression

Expected players use the Area's authored build rung. The player is then built with:

- the checkpoint character level and normal base-attribute calculation;
- real enabled crafting recipes for the selected archetype;
- the actual equipment stat-budget curve;
- deterministic item rolls;
- the rung's real rarity and tempering steps;
- the Essence slots unlocked at that level, capped at the six loadouts currently supported by canonical content;
- a maintained representative Essence loadout for the archetype.

The tutorial checkpoint intentionally uses the actual tutorial mace and one Goblin Essence. Archetypes converge at this earliest checkpoint because the player has not yet acquired a differentiated build.

### Horizontal Area scaling

The active Shenic profile is explicit and versioned. With zero-based Area index `s`:

```text
enemy health     = 1.60 * (1 + 0.180298)^s
enemy offense    = 1.85 * (1 + 0.122646)^s
enemy defense    = 1.15 * (1 + 0.127070)^s
enemy resistance = 1.15 * (1 + 0.127070)^s
```

Attack speed, penetration, soft defenses, critical chance, and critical damage have their own bounded step growth in the same profile. This yields approximately 18.0% Area-to-Area health growth, 12.3% offense growth, and 12.7% defense growth in Region 1.

Recommended Combat Rating is geometrically interpolated from the Region start to endpoint. The progression report displays both the player curve and enemy curves so that this relationship is visible rather than hidden in constants.

### Vertical Region scaling

Each Region has an authored profile and starting/ending Combat Rating anchors. Equipment tier follows Region number in the current canonical policy. Region boundaries must have contiguous global steps and cannot regress below the previous Region's endpoint.

This is a hybrid model: realistic discrete player gains from level, equipment rarity/tempering, item tier, and Essences; smooth exponential enemy baselines within a Region; and explicit, versioned Region boundary anchors. It avoids pretending that equipment rarity gains are a smooth percentage and avoids unrelated formulas for every creature.

### Creature identity

Enemy baseline and identity remain separate. `CreatureScaler` applies:

1. the Area baseline;
2. creature archetype (`Tank`, `Bruiser`, `DPS`, `Support`, `Hazard`, or `Balanced`);
3. physical, magical, or hybrid damage profile;
4. physical, magical, elemental, or balanced defense profile;
5. individual stat overrides.

This is the intended exception mechanism. A dangerous or unusual creature keeps the same progression checkpoint but receives a semantic identity or an explicit authored override.

## Strength bands

Strength bands move through obtainable equipment rungs; they never multiply final stats.

| Band | Equipment/progression assumption | Intended normal-enemy result |
| --- | --- | --- |
| `Undergeared` | One real rarity/tempering rung below the Area's expected build, when available | Killable but slow or dangerous |
| `Expected` | The Area's authored build, character level, and unlocked Essence count | Comfortable repeatable farming |
| `WellGeared` | One available rung above expected | Fast and safe |
| `Optimized` | Two available rungs above expected, capped at the checkpoint's equipment tier | Very fast and very safe |

At the bottom or top of an available tier, adjacent bands can converge. Reports show the resolved build ID so this is visible.

## Calibration archetypes

Progression and build are independent inputs.

| Calibration archetype | Canonical profile | Representative identity |
| --- | --- | --- |
| `Balanced` | `Balanced` | medium armor, greatsword, mixed offense/defense Essence package |
| `Offensive` | `Offense` | light armor, gauntlets, damage-oriented Essences |
| `Defensive` | `Defensive` | heavy armor, maul, mitigation-oriented Essences |
| `Sustain` | `Sustain` | cloth/staff, healing and recovery Essences |
| `AreaDamage` | `Area` | cloth/staff, multi-target and caster Essences |

These loadouts are representatives, not exhaustive Essence combinations. Add or revise a representative loadout only when it covers a genuinely different combat behavior.

## Metrics

An isolated creature simulation records every seeded attempt and aggregates:

- sample count;
- win and death chance;
- average, median, and p95 duration;
- average, median, and p95 health lost;
- remaining health;
- kills per minute;
- damage taken;
- healing done;
- health regenerated.

The production engine also already exposes ability, mitigation, avoidance, threat, barrier, and healing telemetry through `EntityStats`. Future boss adapters can add phase/mechanic counters without changing difficulty evaluation or player generation.

## Difficulty envelopes

The simulator only produces metrics. `CombatDifficultyEvaluator` version 1 interprets them. Every encounter type and strength band has a strongly typed envelope.

The normal-enemy defaults are:

| Strength | Win rate | Median duration | Median health lost |
| --- | ---: | ---: | ---: |
| Undergeared | 45-88% | 8-25 s | 30-90% |
| Expected | 82-100% | 5-15 s | 10-50% |
| Well geared | 95-100% | 2.5-10 s | 0-28% |
| Optimized | 98-100% | 1-7 s | 0-18% |

These are explicit pacing hypotheses, not permanent truths. Revise them centrally when intended idle cadence changes.

Boss categories have separate envelopes for `AreaBoss`, `DungeonBoss`, `TowerBoss`, `WorldBoss`, and `RaidBoss`. Area bosses expect undergeared players to fail often, expected players to win 55-88%, and well-geared players to win reliably. Tower targets are stricter progression checks. World/Raid targets are marked provisional until their group simulators publish the same common metrics.

Assessments report one of:

- `WithinTarget`;
- `TooEasy`;
- `TooHard`;
- `Mixed` (for example, too durable but not dangerous);
- `InsufficientData`.

Diagnostics distinguish survival, defensive durability, and offensive pressure. Suggested health/offense percentages are directional diagnostics only and never modify content.

## Current Region 1 sample

The initial calibration ran 12 seeded simulations per creature for expected/balanced players at early, middle, and late Region 1 checkpoints. No production stats were changed.

| Checkpoint | Finding |
| --- | --- |
| R1/A1 Lumo Ruins | All five isolated creatures are above the normal-farming duration/pressure envelope. Median TTK is 24-54 seconds. Goblin Warrior wins only 41.7%; the other samples won but lost 61-79% median health. |
| R1/A5 Twilight Clearing | All five creatures are safe but exert almost no pressure. Median TTK is about 13-14 seconds, while median health lost is only 0.3-1.2%. |
| R1/A10 Duskmire Hollow | All five creatures are `Mixed`: 18-24 second median TTK is above target, while median health lost is only 0.4-7.0%. They are durable but not dangerous. |

The progression report also found a 172.3% expected Balanced Combat Rating jump from the tutorial checkpoint (CR 47) to the first full common equipment set (CR 128). Later Area-to-Area player increases are approximately 2.4-8.3%, while the enemy baseline continues at 18.0% health and 12.3% offense per step. This mismatch is now visible and should be resolved as a design decision: either introduce intermediate early gear assumptions, treat the tutorial as a separate pre-calibration checkpoint, or retune early content/targets.

These samples are intentionally diagnostic. They are not proof that a particular stat must change, especially because repeated Area encounters can contain multiple enemies and reward cadence also matters.

## API examples

`ICombatCalibrationService` is registered in dependency injection.

The Admin Dashboard diagnostics API also exposes:

- `POST v1/diagnostics/combat-calibration/area` with an `AreaCalibrationRequest` body;
- `GET v1/diagnostics/combat-calibration/progression?regionKey=shenic&archetype=Balanced`.

### Generate a player at a checkpoint

```csharp
var checkpoint = await calibration.GetCheckpointAsync(
    "region_01_area_06",
    cancellationToken);

var player = await calibration.CreatePlayerAsync(
    checkpoint,
    CalibrationStrengthBand.Expected,
    CalibrationArchetype.Balanced,
    cancellationToken);
```

`player` reports the resolved real build, item count, rarity/tempering, Essence count, direct combat stats, and diagnostic rating dimensions.

### Test an Area

```csharp
var report = await calibration.AnalyzeAreaAsync(
    new AreaCalibrationRequest(
        AreaId: "region_01_area_06",
        SimulationsPerEncounter: 100,
        RandomSeed: 73_901,
        StrengthBands: Enum.GetValues<CalibrationStrengthBand>(),
        Archetypes:
        [
            CalibrationArchetype.Balanced,
            CalibrationArchetype.Offensive,
            CalibrationArchetype.Defensive
        ]),
    cancellationToken);

logger.LogInformation("{Report}", report.TextReport);
```

For a fast regression, use a small seeded sample. For a design pass, use 100-500 samples. The service caps requests at 1,000 attempts per creature/profile.

### Evaluate a boss

Boss simulators should publish `CombatCalibrationMetrics`, then use the common evaluator:

```csharp
var assessment = difficultyEvaluator.Evaluate(
    CalibrationEncounterType.AreaBoss,
    CalibrationStrengthBand.Expected,
    bossMetrics);

if (!assessment.IsWithinTarget)
    logger.LogWarning("{Diagnostics}", string.Join(Environment.NewLine, assessment.Diagnostics));
```

Dungeon, Tower, World, and Raid combat retain their specialized simulation/orchestration. They should adapt their results to the common metrics instead of duplicating the envelope logic.

### Generate a progression report

```csharp
var curve = await calibration.CreateProgressionReportAsync(
    "shenic",
    CalibrationArchetype.Balanced,
    cancellationToken);

File.WriteAllText("shenic-balance.txt", curve.TextReport);
```

### Add a progression checkpoint

1. Add the Area and creature membership to `Data/world/regions.json`.
2. Add the Area ID and expected real build ID in order to the Region entry in `Data/progression/region-combat-balance.json`.
3. Increment the balance data version when the authored curve changes.
4. Run the progression report and inspect player/enemy step deltas.
5. Run the Area report across all strength bands and at least Balanced, Offensive, Defensive, and Sustain archetypes.

Do not add a hardcoded `ProgressionCheckpoint` in a test.

## How to Balance a New Area

1. Choose the Area's character level, place it in the Region progression list, and select the expected obtainable build rung.
2. Generate the checkpoint and inspect the expected player profile. Confirm its equipment, Essence count, and direct stats are obtainable.
3. Author creatures from the Region/Area baseline using semantic archetype, damage, and defense profiles.
4. Run an Area calibration with expected/balanced first.
5. Inspect duration and incoming pressure separately. Do not fix every failure by changing health.
6. Run undergeared, expected, well-geared, and optimized bands.
7. Run representative offensive, defensive, sustain, and Area builds to find build-specific walls.
8. Compare kills per minute and the existing Area XP/cinder targets with adjacent Areas.
9. Run the Region progression curve and investigate spikes or backwards movement.
10. Once the intended result is stable, add a tolerant semantic regression assertion or preserve a seeded report expectation.

## Test suites and experiments

- Unit tests validate range evaluation, diagnostics, band ordering, and deterministic helpers. They run in the fast suite.
- Seeded combat regression tests use small samples and semantic/range assertions.
- `BalanceFull` tests run live content samples and larger calibration matrices through `build/run-tests.ps1` according to the repository's balance-suite gate.
- Ad hoc balance experiments call `ICombatCalibrationService` and print reports. They do not need to become permanent unit tests.
- Performance benchmarking remains separate from balance correctness.

## Deliberate first-version boundaries

- Canonical profiles currently support six maintained Essence slots even though the eventual game limit is higher. Expand the loadout catalog before increasing the cap.
- Normal Area creatures have a complete isolated-content adapter. Dungeon/Tower/Raid analyzers still need thin adapters that translate their specialized results to `CombatCalibrationMetrics` and publish mechanic counters.
- Group representation is already possible in the underlying simulation runner, but this service does not yet generate multi-player calibration rosters.
- XP and cinder targets remain visible through the existing Area simulator. Loot/hour, recovery downtime, and deaths/hour need a repeated-farming session model rather than a single-fight estimate.
- Doctrine and future unlocked systems should be added to checkpoint/player generation when there is a deterministic production resolver for them. They should not be approximated with stat multipliers.
- Difficulty envelopes are strongly typed code in version 1. Move them to validated data only if designers need runtime-independent editing; do not make them stringly typed.
