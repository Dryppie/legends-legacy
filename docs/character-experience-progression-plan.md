# Character Experience Progression

## Implementation status

Implemented locally on July 15, 2026:

- Character levels are uncapped and use a JSON-backed quadratic requirement curve.
- Area character XP is no longer derived from creature XP.
- Difficulty tier defines an area's nominal XP/hour through one shared formula.
- Expected group size normalizes the XP assigned to each creature in an encounter.
- Prophecy character XP remains a percentage of the next-level requirement.
- Idle combat supports 24 hours of offline time and processes at most 500 encounters per batch.
- The deterministic balance snapshot covers area rates, victory rates, combat bonuses, Prophecy shares, offline contribution, and level milestones.

The current perfect-combat simulation reaches level 100 in approximately 1,446.32 hours. At 24 successful combat hours per day, that is approximately 60.26 days. At the configured 85% reference win rate, it is approximately 70.9 elapsed days.

## Character requirement curve

Character XP requirements use:

```text
raw requirement = 100 + 93 × level²
required XP = raw requirement rounded up to the next 25 XP
```

The settings live in `LL/src/API/API.LL/Data/progression/character-experience.json`:

```json
{
  "characterLevelCurve": {
    "baseExperience": 100,
    "linearExperiencePerLevel": 0,
    "quadraticExperiencePerLevelSquared": 93,
    "roundingIncrement": 25
  }
}
```

The coefficient was recalibrated from 74 to 93 when area XP moved to the fixed throughput model. This preserves the two-month level-100 target while allowing early areas to start at approximately 10,000 XP/hour.

Representative requirements are:

| Level | XP required |
| ----: | ----------: |
| 1 | 200 |
| 5 | 2,425 |
| 10 | 9,400 |
| 20 | 37,300 |
| 30 | 83,800 |
| 40 | 148,900 |
| 45 | 188,425 |
| 60 | 334,900 |
| 75 | 523,225 |
| 100 | 930,100 |
| 101 | 948,800 |

The same formula continues beyond level 100. Character XP storage is 64-bit, and profession XP remains on its independent progression model.

## Area XP throughput

Area XP settings live in `LL/src/API/API.LL/Data/progression/area-experience.json`:

```json
{
  "areaExperience": {
    "baseExperiencePerHour": 10000,
    "difficultyTierMultiplier": 1.08
  }
}
```

An area's nominal perfect-victory throughput is:

```text
target XP/hour = 10,000 × 1.08 ^ difficulty tier
```

Difficulty zero is the 10,000 XP/hour baseline. The current progression tiers produce:

| Difficulty tier | Nominal XP/hour |
| --------------: | --------------: |
| 0 | 10,000 |
| 1 | 10,800 |
| 2 | 11,664 |
| 3 | 12,597 |
| 4 | 13,605 |
| 5 | 14,693 |
| 6 | 15,869 |
| 7 | 17,138 |
| 8 | 18,509 |
| 9 | 19,990 |
| 10 | 21,589 |

These values are before victory rate and character combat-XP bonuses.

The region API exposes the resolved values through `GET /api/v1/region/area-experience`. Area cards display `Base XP: {value}/hr`; the frontend does not receive or reproduce the tier formula, spawn normalization, or creature XP calculation.

## Expected group-size normalization

At the configured ten-second cadence, idle combat schedules 360 encounters per hour.

For an area's spawn probabilities:

```text
expected creature count = Σ(group size × normalized probability)
XP per creature = target XP/hour ÷ 360 ÷ expected creature count
encounter XP = round(XP per creature × actual creature count)
```

The implementation calculates the full `XP per creature × actual creature count` value and rounds the encounter once. This avoids rounding every creature independently.

Consequences:

- An area with fewer expected creatures gives more XP per individual creature.
- An area with more expected creatures divides its hourly budget across those creatures.
- Actual larger encounters still grant proportionally more XP than smaller encounters in the same area.
- Over many encounters, an area's nominal output remains close to its tier target, with a small difference possible from integer encounter rounding.
- The same creature can grant different character XP in different areas because character XP belongs to the area encounter, not the creature definition.

Creature `ExperienceReward` remains available to systems that have not adopted area throughput, including the current dungeon and Cinder calculations. It no longer affects idle-area character XP.

## Reward order

Idle encounter character XP is resolved in this order:

```text
1. Resolve the area's target XP/hour from difficulty tier.
2. Normalize it using cadence and expected group size.
3. Multiply by the encounter's actual hostile creature count.
4. Round the complete encounter reward to integer XP.
5. Apply positive combat-XP bonuses.
6. Grant the result on victory, or the configured retained portion on defeat.
```

At an 85% victory rate, effective base throughput is approximately:

```text
effective XP/hour = nominal area XP/hour × 0.85
```

The reference victory rate is used only by balance simulations. Actual encounter results determine runtime rewards.

## Current milestone simulation

The simulator always selects the highest-throughput area available at the character's current level.

| Reached level | Cumulative perfect-combat hours |
| ------------: | ------------------------------: |
| 10 | 2.38 |
| 20 | 17.80 |
| 30 | 54.67 |
| 40 | 117.02 |
| 45 | 158.12 |
| 60 | 334.13 |
| 75 | 625.47 |
| 100 | 1,446.32 |

Early milestones are faster than in the previous creature-XP model, while the increased quadratic requirement keeps the full level-100 journey at approximately two months of uninterrupted successful combat.

## Prophecy contribution

Prophecy XP is resolved as a percentage of the character's next-level requirement and stored in the reward snapshot. It does not use absolute early-level XP floors.

| Reward profile | Next-level share |
| -------------- | ---------------: |
| Daily Common | 4% |
| Daily Uncommon | 5% |
| Daily Rare | 6% |
| Weekly Uncommon | 25% |
| Weekly Rare | 30% |
| Weekly Epic | 35% |

Five completed dailies plus one weekly provide between 45% and 65% of a level. Because the reward is snapshotted, later level-ups or balance changes do not alter an already generated Prophecy reward.

## Adding future areas

When adding an area:

1. Assign a difficulty tier based on the intended combat and progression band.
2. Author valid group-size probabilities; they do not need to match other areas in the tier.
3. Do not add creature XP values to balance idle-area character progression.
4. Re-run the progression snapshot after adding or changing an unlock tier.
5. Check milestone timing at 70%, 85%, and 100% victory rates.

Multiple areas may share a difficulty tier. They will have the same nominal XP/hour even when their group sizes differ. Such areas can differentiate themselves through enemies, loot, gathering, risk, and other rewards.

The 1.08 multiplier compounds indefinitely. Difficulty tiers should therefore remain broad progression bands rather than increasing once per character level. New tiers before level 100 will shorten the existing two-month target unless the requirement curve is recalibrated again.

## Dungeon boundary

The area formula is intentionally limited to idle areas. Dungeons currently continue using their encounter creatures' authored XP.

If dungeon XP is converted later, it should use a fixed run budget derived from its associated difficulty tier:

```text
dungeon XP budget = tier XP/hour × intended equivalent combat hours
```

The fixed run budget should then be distributed across rooms and completion. Random room counts and creature counts should not change the dungeon's total progression budget.

## Validation and tests

Startup validation rejects:

- missing or duplicate area IDs;
- negative difficulty tiers;
- missing or invalid spawn probabilities;
- empty or invalid creature weights;
- references to missing creatures;
- non-positive base XP/hour;
- a difficulty multiplier below one;
- a non-positive encounter cadence.

Automated coverage verifies:

- exact character requirements at representative and very high levels;
- unlimited leveling and overflow preservation;
- area tier targets using the 1.08 multiplier;
- higher per-creature XP in a lower-density area;
- milestone timing and the two-month level-100 target;
- 70%, 85%, and 100% area throughput projections;
- Prophecy weekly contribution bounds;
- 24-hour offline contribution.

No database migration or deployment action is required for the area XP change.
