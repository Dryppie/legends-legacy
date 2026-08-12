# Legacy's Ascension — balancing plan

Last updated: 2026-08-12

## Purpose

Balance the first ten World Tower floors against progression that players can actually obtain before Region 2. Floor 10 is the Region 1 capstone and its first clear unlocks Region 2, so neither its recommendation nor its combat tuning may assume Tier 2 equipment, Region 2 Essences, or any other post-unlock power.

This plan covers balance tooling, target outcomes, calibration, regression protection, and live validation. It does not replace the implementation tracker in `docs/world-tower-implementation-status.md`.

## Current diagnosis

The current floor recommendations are not on the same scale as the live character progression model.

- Tower readiness compares the roster's **average per-character Power Rating** with `recommendedPowerRating`; it does not compare total roster power.
- Region 1's current calibrated recommendation curve is `47, 54, 62, 72, 83, 96, 110, 127, 147, 169`.
- Canonical Tier 1 Epic builds currently display roughly 132–145 Power Rating, depending on profile.
- Canonical Tier 1 Legendary builds currently display roughly 138–151 before applying the Region 1 end-state level and Essence policy.
- The Tower catalog currently recommends 750–12,000 per character. Those values cannot communicate useful readiness for Tier 1 players.
- Guardian difficulty currently uses one scalar that multiplies health, offense, armor, resistance, penetration, and regeneration together. That makes time-to-kill and incoming damage inseparable and will make reliable calibration unnecessarily difficult.
- Expedition sizes vary from 3 to 15. A floor with more slots already receives substantially more player throughput, so its Guardian cannot be tuned by looking only at the floor number or at one strength multiplier.

The current values should therefore be treated as placeholder content, not as a baseline to preserve.

## Non-negotiable balance rules

1. **Floor 10 must be clearable with pre-unlock progression.** The benchmark roster is level 50, Tier 1 Legendary equipment, six equipped Essences, and only Essences/unlocks obtainable in Region 1.
2. **No circular progression.** Automated validation must fail if a Floor 10 benchmark uses Tier 2 equipment or content gated by the Floor 10 unlock.
3. **Recommendation remains advisory.** There is no minimum Power Rating gate. Strong builds may clear early, and weaker builds may attempt the floor.
4. **Recommendation means average per character.** UI copy, simulation reports, and tests must use the same meaning as `TowerRosterReadinessDto`.
5. **A complete, sensible roster is the balance unit.** Difficulty is calibrated against the floor's real `requiredSlots`, not a single character multiplied afterward.
6. **Preparation helps; it is not mandatory until the capstone.** Floors 1–9 must not be balanced around all three bonuses being maxed. Floor 10 may expect the server to use preparation and scouting knowledge, but it must remain possible without perfect preparation.
7. **Victory before the 6,000-tick limit is the only timing requirement.** A faster clear is not more balanced than a slower clear; any timeout is simply a failed attempt and is already reflected in win rate.
8. **Power Rating describes readiness; it does not drive enemy scaling.** Guardian stats remain deterministic content. The service must not secretly scale a Guardian to the participating roster.

## Reuse before adding models

The balancing workflow should reuse:

- `CanonicalEquipmentBuildFactory` for real recipes, item bases, stat rolls, tempering, and Region 1 Essence loadouts;
- `CanonicalRegionProgressionPolicy` and `region-combat-balance.json` for attainable Region 1 levels, equipment tiers, default build milestones, and rating anchors;
- `PowerAnalysisSimulationRunner` patterns for detached canonical combatants, fixed seeds, and Wilson confidence intervals;
- the production combat setup and combat engine used by `WorldTowerService`;
- the existing Tower floor JSON as the authored source of floor balance;
- existing combat reports and timelines for duration, survival, damage, healing, and ability statistics.

Do not add Tower-only character stats, equipment models, combatants, snapshots, or a second combat engine.

One additional balance structure is justified: replace `guardianStrengthMultiplier` with independent Guardian scaling axes. The Region scaler already establishes this pattern, so the Tower version should follow its vocabulary rather than invent another generalized curve system.

```json
"guardianScaling": {
  "health": 1.0,
  "offense": 1.0,
  "defense": 1.0,
  "resistance": 1.0,
  "penetration": 1.0,
  "regeneration": 1.0
}
```

Health controls the total damage required to win before timeout, offense controls roster pressure, defense/resistance control damage-profile matchups, and penetration controls how much defensive builds matter. Regeneration should be tuned last. A temporary compatibility reader may map the old scalar to every axis while content is migrated, but the old property should then be removed rather than maintained as a second permanent format.

## Canonical progression anchors

The Tower begins after ordinary early Region 1 progression. Its authored benchmark curve now runs from level 30 with full Tier 1 Uncommon gear and four equipped Essences to level 50 with full Tier 1 Legendary gear and six equipped Essences:

| Floor | Progression checkpoint | Initial recommendation candidate |
| ---: | --- | ---: |
| 1 | Level 30 / Tier 1 Uncommon / 4 Essences | 146 |
| 2 | Level 32 / Tier 1 Uncommon / 4 Essences | 147 |
| 3 | Level 34 / Tier 1 Rare / 4 Essences | 155 |
| 4 | Level 37 / Tier 1 Rare / 4 Essences | 158 |
| 5 | Level 39 / Tier 1 Epic / 4 Essences | 163 |
| 6 | Level 41 / Tier 1 Epic / 5 Essences | 166 |
| 7 | Level 43 / Tier 1 Unique / 5 Essences | 171 |
| 8 | Level 46 / Tier 1 Unique / 5 Essences | 175 |
| 9 | Level 48 / Tier 1 Legendary / 5 Essences | 176 |
| 10 | Level 50 / Tier 1 Legendary / 6 Essences | 179 |

These recommendations are derived from the canonical average at each authored checkpoint and remain advisory. The analyzer verifies the recommendation against the generated roster rather than allowing the UI value to drift from the balance loadout.

The endpoint must be obtained from the active Region 1 balance definition during analysis rather than copied permanently into code. This keeps the Tower aligned when Region 1 is recalibrated.

## Test roster matrix

For every floor, build full rosters at three progression points:

- **Previous checkpoint:** demonstrates that advancing the Tower has meaningful difficulty.
- **Intended checkpoint:** determines the recommendation and primary tuning result.
- **Next checkpoint:** detects walls and confirms that ordinary progression produces a noticeable advantage.

Run these roster shapes for each checkpoint:

| Roster | Purpose |
| --- | --- |
| Mixed standard | Primary balance roster, proportionally mixing Balanced, Offense, Sustain, and Defensive profiles. |
| Damage-heavy | Detects encounters that are trivialized by burst or lack survival pressure. |
| Sustain-heavy | Detects stalemates, excessive healing, and timeout risk. |
| Defensive-heavy | Detects Guardians that cannot threaten tanks or cannot be killed in time. |
| Profile stress tests | Fill the roster with one profile at a time to find hard exclusions; Area is diagnostic because the Tower currently has one hostile. |

For small rosters, use deterministic round-robin allocation. For larger rosters, use approximately 40% Offense, 20% Balanced, 20% Sustain, and 20% Defensive as the mixed standard. The exact identities must be printed in each report so results are reproducible.

Run every roster in these preparation states:

- no preparation;
- each bonus individually maxed;
- all bonuses maxed;
- all bonuses maxed with a scouting-informed counter composition.

Scouting itself does not change combat stats. Its benefit is measured only through the different composition/loadout selected after the relevant reveal.

## Target outcome bands

Use at least 256 fixed seeds per scenario during iteration and 1,000 held-out seeds for release validation. Report a 95% Wilson interval rather than treating the observed percentage as exact.

| Floor band | Intended-checkpoint win rate, no preparation | Intended-checkpoint win rate, prepared/countered |
| --- | ---: | ---: |
| Floors 1–4 | 75–90% | 85–97% |
| Floor 5 Warden | 60–75% | 75–90% |
| Floors 6–9 | 65–82% | 78–92% |
| Floor 10 Sovereign | 55–70% | 70–85% |

Additional acceptance boundaries:

- A next-checkpoint standard roster should win at least 90% of non-Warden floors, unless a documented mechanic intentionally counters it.
- A previous-checkpoint roster should not have a higher win rate than the intended-checkpoint roster for the same composition and preparation state.
- The strongest and weakest reasonable mixed compositions should normally be within 20 percentage points. A larger gap requires a documented Guardian mechanic and a scouting reveal that explains the counterplay.
- No single profile should be mandatory for Floors 1–9. Floor 10 may reward composition planning, but at least two distinct mixed roster shapes must satisfy its prepared target band.
- A full Tier 1 Legendary, level-50 mixed roster with six Region 1-only Essences must satisfy the Floor 10 prepared target band.
- The same Floor 10 test must assert that every item has `Tier == 1` and every Essence/unlock is available before the Region 2 unlock.

The Floor 10 unprepared band deliberately permits early clears without making them routine. Preparation plus informed composition should turn the capstone into a reliable server objective, not a lottery or a Tier 2 gear check.

## Metrics to collect

Each scenario report should include:

- wins, losses, timeouts, and Wilson interval;
- duration in ticks and playback seconds for diagnostics only, never as a balance acceptance target;
- survivor count and remaining-health distribution;
- Guardian remaining health on defeats;
- per-character damage dealt, damage taken, healing, barriers, deaths, and ability contribution;
- outcome by canonical profile and roster shape;
- exact floor definition, balance version, equipment balance version, progression policy version, seed set, preparation state, and roster average Power Rating.

These metrics distinguish three problems that a simple win rate hides: a health sponge, unavoidable burst damage, and a composition/build mismatch.

## Calibration workflow

### Phase 1 — Baseline analyzer

Add a non-persisting `WorldTowerBalanceAnalyzer` in the service layer and an application-facing interface/report DTO. It should load the real floor and Guardian, generate full canonical rosters, invoke the production combat path, and emit JSON plus a human-readable table. It must not create Expeditions, grant rewards, advance floors, or write player state.

First run the current catalog unchanged. Preserve this report as evidence for why values changed.

### Phase 2 — Independent Guardian scaling

Introduce the `guardianScaling` axes in the existing floor definition and JSON. Reuse the attribute-modifier approach already used by dungeon/region combat; do not build a parallel stat system.

Tune in this order:

1. offense to establish survival pressure and meaningful deaths;
2. health to make the intended roster win reliably before the 6,000-tick timeout;
3. defense and resistance to keep physical and magical approaches viable;
4. penetration to prevent defense from being either mandatory or irrelevant;
5. regeneration only when it expresses the Guardian's authored identity.

Changing one axis must trigger the full roster matrix because abilities and sustain can create nonlinear outcomes.

### Phase 3 — Floor-by-floor calibration

Calibrate Floors 1–4 first and lock their curve. Tune Floor 5 as the first local difficulty spike. Then calibrate Floors 6–9 and finish with Floor 10.

For each floor:

1. run the intended checkpoint and mixed standard roster;
2. tune offense and health into the primary target band;
3. run every composition and preparation state;
4. adjust defense, resistance, and penetration for matchup spread;
5. run previous/next progression checkpoints;
6. derive the recommendation from the successful canonical rating rather than selecting it by feel;
7. validate with held-out seeds before accepting the content change.

Do not force the raw multipliers to increase monotonically. Different Guardian base creatures and Expedition sizes make that misleading. Enforce monotonic player-facing recommendations and measured encounter difficulty instead.

### Phase 4 — Floor 10 release gate

Floor 10 cannot ship as balanced until all of these pass:

- a full level-50 Tier 1 Legendary mixed roster with six equipped Essences using only Region 1 content reaches the prepared target;
- at least one different reasonable mixed composition also reaches the band;
- the unprepared canonical roster remains capable of winning;
- victories complete within the engine's 6,000-tick limit; clear speed inside that limit is not an acceptance criterion;
- there is no dependency on a server unlock granted by Floor 10;
- the clear persists the Region 2 unlock, and Region 2's access check consumes that exact key;
- the recommendation is no higher than the active Region 1 ending rating and is shown as an average per-character value.

The current key `region_expansion_1` is only a persisted marker and downstream unlock consumption is still partial. Before Region 2 is released, define one stable semantic key for Region 2 access, migrate/alias the existing key if necessary, and add an integration test proving that a Floor 10 clear changes Region 2 from locked to accessible.

### Phase 5 — Regression and operational validation

Add a fast fixed-seed smoke matrix to normal tests and keep the 1,000-seed validation suite as an explicit balance/CI job. Fail validation when:

- Floor 10 loses its Tier 1-only clear path;
- recommendations diverge from their canonical attainable ratings;
- a floor falls outside its target confidence interval;
- timeouts or defeats move the scenario below its target win-rate band;
- a combat, ability, equipment, Essence, Guardian, or preparation change invalidates the approved baseline.

After release, record aggregate Tower attempt telemetry: floor, mode, balance version, roster size, average/min/max rating, preparation values, outcome, duration, survivors, and Guardian remaining health. Do not store another build snapshot for analytics; attempts already reference the authoritative locked roster.

Compare First Clear and Echo attempts separately. First Clear has very few samples per server, while Echo supplies repeatable post-clear evidence. Use telemetry to identify regressions and propose versioned content changes; never adapt live Guardian stats to a particular roster.

## Suggested delivery order

| Step | Deliverable | Completion condition |
| ---: | --- | --- |
| 1 | Baseline analyzer and report | All ten current floors run deterministically with canonical full rosters and no persistence. |
| 2 | Independent Guardian scaling | JSON, validation, runtime application, compatibility migration, and focused tests pass. |
| 3 | Floors 1–5 calibration | Recommendations and outcome bands pass held-out validation. |
| 4 | Floors 6–9 calibration | Recommendations and outcome bands pass held-out validation. |
| 5 | Floor 10 Tier 1 release gate | Region 1-only canonical rosters meet every capstone criterion. |
| 6 | Region 2 unlock integration | Floor 10 first clear demonstrably unlocks Region 2; no Tier 2 prerequisite exists. |
| 7 | Regression suite and telemetry | Balance job, versioned report, and operational measurements are available. |

## Definition of balanced for the first release

The first ten floors are balanced when the catalog recommendations reflect attainable per-character Region 1 ratings, every floor meets its measured win-rate targets across reasonable roster shapes within the 6,000-tick limit, and a fully progressed Tier 1 server can clear Floor 10 reliably with preparation and informed composition. Region 2 access must be caused by that clear, never required to achieve it.
