# Region Area Balancing Plan

## Purpose

Build a reusable balancing system for idle-combat areas, beginning with the ten combat areas in Shenic and extending naturally to future regions.

The system should provide the same confidence that the dungeon balance tooling provides today, but area balance should not stop at reporting current combat numbers. It should own a versioned, smooth creature-scaling curve, apply that curve to creatures at runtime, and validate the result through deterministic simulations.

## Fixed design decisions

### XP and Cinders remain area rewards

Experience and Cinders per hour continue to be determined by the area through the existing area progression configuration and `IAreaExperienceBalanceProvider`.

- Creatures do not receive authored XP or Cinder values.
- Creature identity, archetype, abilities, or individual power must not change the area's XP/Cinder curve.
- Encounter cadence and the area's spawn distribution may still be used internally to normalize an area-owned hourly target into encounter rewards.
- A simulation reports effective XP/Cinders per hour by applying the measured area win rate to the existing area target. It does not invent a new creature-based reward model.
- Loot, Essences, gathering rewards, and dungeon sigils remain separate reward systems and are not folded into XP/Cinder scaling.

This preserves the current progression model in `Data/progression/area-experience.json` while allowing combat difficulty to be calibrated independently.

### Creature scaling is system-owned

Areas should not be balanced by manually assigning final health, power, armor, or resistance values to each area.

Instead:

1. Every area receives a stable position on a region progression curve.
2. A versioned scaling profile calculates the combat multipliers for that position.
3. `CreatureScaler` applies those calculated values before applying creature archetypes, damage profiles, defense profiles, and exceptional creature overrides.
4. Deterministic simulations validate that the resulting curve produces the intended win rates.
5. Calibration adjusts the small set of curve parameters, rather than maintaining ten unrelated sets of area numbers.

Runtime scaling must be deterministic and content-driven. It must not dynamically weaken enemies for a particular player or alter difficulty based on live win/loss results.

## Current Region 1 baseline

Shenic currently has ten non-tutorial combat areas:

| Area | Required level | Current difficulty tier | Expected enemies per encounter |
| --- | ---: | ---: | ---: |
| Lumo Ruins | 1 | 1 | 1.03 |
| Blood Grove | 5 | 2 | 1.97 |
| Crystal Creek | 10 | 3 | 1.97 |
| Moonlit Graves | 15 | 4 | 1.97 |
| Twilight Clearing | 20 | 5 | 1.97 |
| Old Forest | 25 | 6 | 1.97 |
| Thornroot Hollow | 30 | 7 | 1.97 |
| Embercap Burrows | 35 | 8 | 1.97 |
| Moonveil Marsh | 40 | 9 | 1.97 |
| Duskmire Hollow | 45 | 10 | 1.97 |

Each area currently contains five equally weighted creatures. Apart from Lumo Ruins, the areas also share effectively the same one/two/three-creature distribution. The present progression is consequently driven mainly by `DifficultyTier`, creature archetypes, abilities, and stat overrides.

The existing area XP curve starts at 10,000 XP and 1,000 Cinders per hour and grows by 8% per difficulty tier. The configured reference idle-combat win rate is 85%.

## Target outcome

For each region, the completed system should be able to answer and enforce:

- Which progression step does each area occupy?
- What player build is reasonably attainable when that area unlocks?
- What creature scale should that progression step use?
- Does the intended build win approximately 85% of encounters?
- Are all supported build profiles viable?
- Is each step harder than the preceding step without an abrupt spike?
- Does advancing remain worthwhile after the real win rate is applied to the area's existing XP/Cinder target?
- Did a creature, ability, equipment, or combat-engine change invalidate the calibrated curve?

## Proposed architecture

### 1. Region balance definitions

Add a versioned JSON definition, for example:

`Data/progression/region-combat-balance.json`

It should describe policies and curve parameters, not final per-area stats. A conceptual structure is:

```json
{
  "version": 1,
  "profiles": [
    {
      "id": "standard-region-v1",
      "targetWinRateBasisPoints": 8500,
      "healthCurve": {
        "model": "Power",
        "baseMultiplier": 1.0,
        "growthPerStep": 0.0,
        "exponent": 1.0
      },
      "offenseCurve": {
        "model": "Power",
        "baseMultiplier": 1.0,
        "growthPerStep": 0.0,
        "exponent": 1.0
      },
      "defenseCurve": {
        "model": "Power",
        "baseMultiplier": 1.0,
        "growthPerStep": 0.0,
        "exponent": 1.0
      },
      "attackSpeedGrowthPerStep": 0.0,
      "maximumStepIncrease": 0.0
    }
  ],
  "regions": [
    {
      "regionKey": "shenic",
      "profileId": "standard-region-v1",
      "startingGlobalStep": 1,
      "areaIds": [
        "region_01_area_01",
        "region_01_area_02"
      ]
    }
  ]
}
```

The example values are placeholders for schema illustration only. The first calibration run must determine the real parameters.

Each area's global combat step is calculated from the region's starting step and its ordered position. A future region can therefore either:

- Continue the same profile from a later global step.
- Reuse the curve with a different starting step.
- Select a new profile when the expected player-progression model materially changes.

The area order should be explicit rather than inferred from IDs such as `region_01_area_07`, because current IDs do not always follow progression order.

### 2. Scaling provider

Introduce an application-facing contract such as `IRegionCreatureScalingProvider` returning a `CreatureScalingProfile` for an area.

The returned profile should contain calculated multipliers for at least:

- Maximum Health.
- Power/offense.
- Armor.
- Resistance.
- Precision.
- Penetration and soft defensive attributes where applicable.

`CreatureScaler` should then apply scaling in a stable order:

1. Initialize the creature baseline.
2. Resolve the area's region profile and global progression step.
3. Apply the calculated curve multipliers.
4. Apply creature archetype.
5. Apply damage and defense profiles.
6. Apply creature-specific overrides for intentional exceptions.
7. Clamp and synchronize final combat attributes.

Creature-specific overrides remain useful for identity, such as a tanky Treant or fragile high-damage caster, but they must not become the main area progression mechanism.

Unknown profiles, duplicate area placement, missing areas, invalid curve parameters, and non-monotonic calculated scaling should fail during startup validation.

### 3. Deterministic area simulator

Add `IAreaCombatSimulator` under the Region application interfaces and implement it in `Services.LL/Regions`.

The simulator should:

- Load the real area, creatures, abilities, and scaling profile.
- Use the production combat setup and combat engine.
- Create detached canonical or manually configured characters.
- Reproduce the area's real group-size probabilities and creature weights.
- Accept a base seed and produce repeatable encounter suites.
- Perform no persistence, inventory mutation, reward granting, or player-state mutation.
- Support one selected area and a batch containing every area in a region.

The existing spawning implementation owns an unseeded `Random`. Its weighted-selection logic should be extracted into deterministic functions that accept a random source. Production can continue using a normal random source while simulations supply a seeded source.

Each simulation report should include:

- Encounter count, wins, defeats, and timeouts.
- Win rate.
- Median, average, and p95 combat duration.
- Average and p95 damage taken.
- Remaining-health distribution.
- Results grouped by hostile count.
- Results grouped by creature and encounter composition.
- The calculated scaling profile used for the area.
- The area's configured XP/Cinder targets.
- Effective XP/Cinders per hour at the measured win rate.

The simulator must label XP/Cinders as area targets. It must not expose them as creature values.

### 4. Canonical player progression

Extend `CanonicalEquipmentBuildFactory` with an area-aware build method instead of creating an unrelated build model.

The system needs a data-driven policy that determines, from an area's region progression step:

- Character level.
- Expected equipment tier and rarity.
- Expected occupied equipment slots.
- Available Essence slots.
- Which Essences are considered realistically obtainable by that point.

Use the existing balanced, offense, sustain, defensive, and area-damage profiles. The expected build should be derived from region progression policy rather than manually entering complete character stats for every area.

Manual character inputs should remain available in the dashboard for investigation, but automated calibration and regression tests must use the canonical progression policy.

### 5. Curve calibrator

Add an offline/admin `RegionCombatCurveCalibrator` that uses deterministic simulation results to fit the scaling profile.

For a selected region, it should:

1. Generate the canonical build for every area progression step.
2. Simulate every supported build profile against that area.
3. Measure the error between the observed and target win rates.
4. Adjust the shared health, offense, and defense curve parameters.
5. Penalize non-monotonic results and abrupt step-to-step growth.
6. Repeat until the region is inside the accepted tolerance or the run limit is reached.
7. Validate the fitted curve using a separate seed set that was not used while fitting.
8. Produce an exportable curve definition and a human-readable report.

The calibrator should optimize a small number of shared curve parameters. It should not generate ten arbitrary area multipliers as its normal output.

An emergency per-area adjustment may be supported, but it should be optional, bounded, visible in diagnostics, and accompanied by a reason. A region should not be considered smoothly calibrated when several such exceptions are required.

Calibration is a development operation. Runtime services load the approved curve and apply it deterministically; they do not run simulations or rewrite balance content at startup.

### 6. Balance analyzer and content identity

Add an `AreaPowerAnalyzer` comparable to `DungeonPowerAnalyzer`.

For each area it should report:

- Target and observed win rates.
- Results for every canonical profile.
- Lowest passing canonical equipment rung.
- Recommended Combat Rating range.
- Expected build versus minimum passing build.
- Previous-area and next-area difficulty comparisons.
- Contribution from group size and individual creature compositions.
- Whether the result is in tolerance, too easy, too hard, or low confidence.

Recommendations should include a content hash covering:

- Region balance definition and version.
- Area order and spawn distribution.
- Referenced creatures and their profiles/overrides.
- Referenced creature abilities.
- Combat rules version.
- Canonical build definition version.
- Equipment budget version.
- Calibration and validation seed-set versions.

This prevents an old recommendation from appearing valid after relevant content changes.

## Balance targets

Use the existing 85% reference win rate as the center of the Region 1 curve.

Initial acceptance criteria should be:

- Reference aggregate win rate between 80% and 90% for each area.
- No supported canonical profile below an agreed viability floor; 75% is a reasonable initial floor to validate during the baseline run.
- The preceding area's win rate should normally be at least 95% for the next area's intended build.
- The next area should remain meaningfully more difficult than the current area at the current area's intended build.
- Median and p95 duration should grow smoothly and remain inside limits established from the baseline.
- No unexplained timeout or stalemate rate.
- Calculated health, offense, and defense scaling must be non-decreasing across the region.
- Step-to-step scaling growth must remain under the profile's configured maximum increase.

These are development tolerances, not new reward formulas.

## XP and Cinder validation

The existing area progression provider remains the source of truth.

For an area with target XP per hour `X`, target Cinders per hour `C`, and observed win rate `W`, the balance report projects:

```text
effective XP/hour      = X × W
effective Cinders/hour = C × W
```

Any existing internal normalization needed to turn the area target into encounter awards should continue to use the area's cadence and authored spawn distribution. Changing a Goblin to a Lumo Sentinel must not, by itself, change the area's XP/Cinder target.

The progression validation should ensure:

- Area target XP/Cinders remain monotonic according to the existing area curve.
- Effective XP/Cinders do not fall unexpectedly when the player advances.
- A difficult encounter composition cannot change the authored rate except through its effect on win rate.
- The 24-hour offline projection uses the same area target and reference win rate.
- Character-level milestone projections distinguish full-win target rates from realistic reference-win rates.

Combat loot, Essence drops, gathering rewards, and sigil availability should be reported separately so they can be balanced without coupling them to area XP/Cinders.

## Admin dashboard

Add an Area Simulator beside the existing Dungeon Simulator.

Suggested endpoints:

- `GET diagnostics/area-simulation-options`
- `POST diagnostics/area-simulation`
- `POST diagnostics/region-area-balance`

The dashboard should provide:

- Single-area simulation using a canonical or manual character.
- Full-region validation across all canonical profiles.
- Current curve parameters and calculated scaling for each step.
- Target versus observed win-rate visualization.
- Smoothness and monotonicity warnings.
- Effective area XP/Cinders per hour shown separately from loot.
- Per-creature and per-composition drill-down.
- An explicit export action for calibrated JSON; calibration must not silently alter production content.

## Tuning workflow

### Phase 1: Establish the baseline

Run all ten Shenic areas against every canonical profile using fixed calibration and validation seed sets. Do not change content until this report exists.

Capture:

- Win-rate curve.
- Duration and damage curve.
- Profile spread.
- Composition outliers.
- Minimum passing equipment rung.
- Effective area XP/Cinders at measured win rates.

### Phase 2: Fit the shared curve

Use the calibrator to fit Region 1's shared health, offense, and defense curves to the 85% target.

Prefer the simplest curve that keeps all areas within tolerance. If the curve cannot fit the region, inspect whether the cause is:

- A creature ability defect.
- A creature archetype/profile mismatch.
- A disproportionately dangerous composition.
- The large encounter-size jump between Lumo Ruins and Blood Grove.
- An unrealistic canonical player-progression assumption.

### Phase 3: Fix content outliers

Tune exceptional content in this order:

1. Ability or combat behavior defects.
2. Incorrect creature archetype, damage profile, or defense profile.
3. Area creature weights.
4. Area group-size probabilities.
5. Bounded creature stat overrides for intentional identity.
6. A documented area adjustment only when no shared/content-level solution is appropriate.

Do not change the global curve to solve one exceptional creature.

### Phase 4: Validate progression and rewards

Rerun the validation seed suite and confirm:

- Smooth Region 1 combat progression.
- Canonical profile viability.
- Stable dungeon balance for shared creatures and scaling code.
- Existing area XP/Cinder targets remain unchanged.
- Effective XP/Cinders behave as expected at the measured win rates.
- Character progression remains within the intended milestone timeline.

### Phase 5: Approve the curve

Commit the versioned curve, content hash expectations, and updated regression snapshots together. The report should state which curve version produced the approved Region 1 balance.

## Test plan

### Unit tests

- Curve calculation is deterministic.
- Scaling is monotonic for valid profiles.
- Invalid and missing profiles fail validation.
- Region area order resolves to the correct global steps.
- Weighted spawning produces repeatable results for a fixed seed.
- Simulation aggregation and percentile calculations are correct.
- XP/Cinder reporting reads area targets and never creature reward values.

### Integration tests

- Every Region 1 area and creature resolves from real JSON content.
- Runtime creature setup receives the calculated area scaling profile.
- Simulator combat uses the same scaling and abilities as production idle combat.
- Simulation does not persist entities, inventory, rewards, or character state.
- Calibration output passes validation on a separate seed set.

### Balance regression tests

- Every Region 1 area remains inside the approved win-rate tolerance.
- Every supported canonical profile remains above its viability floor.
- Calculated enemy scaling is smooth and non-decreasing.
- Effective XP/Cinders remain monotonic at the reference win rate.
- Character progression snapshots retain their approved targets.
- Tier 1 dungeon balance tests still pass after shared creature/scaler changes.

## Expected file impact

Likely additions:

- `LL/src/API/API.LL/Data/progression/region-combat-balance.json`
- Region balance interfaces and report models under `LL/src/Core/Application/Interfaces/Services/LL/Regions/`
- Scaling provider, simulator, calibrator, and analyzer under `LL/src/Infrastructure/Service/Services.LL/Regions/`
- Admin Dashboard queries under `LL/src/Core/Application/UseCases/_AdminDashboard/Diagnostics/`
- Admin Dashboard API endpoints in `DiagnosticsController`
- Area simulator models, service methods, route, and components in `LL/src/Presentation/dashboard/`
- Region area balance and simulation tests in `LL/tests/EssenceSystem.Tests/`

Likely modifications:

- `CreatureScaler` to consume the new calculated scaling profile.
- Dependency injection registration and startup validation.
- Deterministic spawning support.
- Canonical equipment build creation for area progression.
- Character progression snapshot tests to distinguish full and reference win-rate projections.

No change should be needed to the ownership of area XP/Cinder targets.

## Migration, configuration, and deployment implications

The preferred implementation keeps region balance definitions in versioned JSON and resolves them by area ID, which avoids adding persisted columns to `Area` and therefore avoids an EF Core migration.

An EF Core migration is required only if progression-step or scaling-profile data is added directly to persisted Region/Area entities. That is not the recommended first implementation.

Deployment eventually requires rebuilding/restarting the game API and Admin Dashboard so the new JSON definitions and services load. Content seeding should not be responsible for calculating or storing final creature stats. No external environment or infrastructure-as-code changes are part of this work.

## Suggested delivery slices

1. Region balance schema, scaling provider, deterministic spawning, and startup validation.
2. Area simulator and canonical area builds.
3. Curve calibrator, analyzer, and balance regression suite.
4. Admin Dashboard Area Simulator and full-region report.
5. Region 1 calibration, content outlier fixes, and progression validation.

Estimated effort is approximately 8–13 focused development days, followed by manual playtesting. Future regions should primarily require a region entry, ordered areas, a starting global step, and—only when progression assumptions differ—a new reusable curve profile.

## Implemented foundation

The first implementation slice now includes the versioned curve catalog, explicit Shenic area order, reusable global steps, startup validation, `CreatureScaler` integration, deterministic weighted spawning, canonical area builds, real idle-mode combat simulation, full-region profile analysis, Admin Dashboard endpoints, and an Area Simulator page.

The initial `shenic-area-v1` profile deliberately reproduces the former difficulty-tier formulas. Moving those formulas into the system-owned profile therefore does not silently rebalance live combat while establishing the new curve boundary. The full-region analyzer currently identifies the legacy baseline as too easy for canonical builds; that result is calibration input, not an approved 85% curve. The shared curve and any genuine content outliers can now be tuned and revalidated without authoring final stats for each area.

The implementation reads XP and Cinders exclusively from `area-experience.json`. Reports show the authored area targets and calculate effective hourly values from observed win rate; no creature reward field or creature-specific rate was introduced.
