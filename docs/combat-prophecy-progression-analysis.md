# Location, Character, and Prophecy Progression Analysis

## Current reward ownership

Character XP and Cinders are owned by the activity that creates the encounter, not by the defeated creature.

- Areas budget rewards per hour from area difficulty and expected encounter size.
- Dungeons budget rewards per victorious combat encounter from dungeon tier and room type.
- Prophecies scale XP with the character level curve and deliberately do not grant Cinders.
- Creature definitions own combat identity, stats, loot, and Essence relationships. They do not contain XP or Cinder values.

This separation prevents a creature reused in multiple areas or dungeons from forcing those activities to share progression rates.

## Area rewards

Area settings live in `LL/src/API/API.LL/Data/progression/area-experience.json`:

```json
{
  "areaExperience": {
    "baseExperiencePerHour": 10000,
    "baseCindersPerHour": 1000,
    "difficultyTierMultiplier": 1.08
  }
}
```

Nominal perfect-victory throughput is:

```text
XP/hour = 10,000 × 1.08 ^ area difficulty tier
Cinders/hour = 1,000 × 1.08 ^ area difficulty tier
```

Both rewards are divided by 360 encounters per hour and the area's expected creature count. The actual encounter then receives the per-creature budget multiplied by its actual creature count and rounded once.

```text
expected creatures = Σ(group size × normalized group probability)
reward per creature = target reward/hour ÷ 360 ÷ expected creatures
encounter reward = round(reward per creature × actual creatures)
```

Nominal targets therefore remain a constant 10:1 XP-to-Cinder ratio across tiers. Integer rounding is more visible for Cinders because each encounter grants only a few units; long-run realized Cinders can differ modestly from the nominal target.

| Area | Tier | Nominal XP/hour | Nominal Cinders/hour |
| --- | ---: | ---: | ---: |
| Training Area | 0 | 10,000 | 1,000 |
| Lumo Ruins | 1 | 10,800 | 1,080 |
| Blood Grove | 2 | 11,664 | 1,166 |
| Crystal Creek | 3 | 12,597 | 1,260 |
| Twilight Clearing | 4 | 13,605 | 1,360 |
| Oak Thicket | 5 | 14,693 | 1,469 |
| Old Forest | 6 | 15,869 | 1,587 |
| Bleak Orchard | 7 | 17,138 | 1,714 |
| Rotting Hamlet | 8 | 18,509 | 1,851 |
| Wormburrow Depths | 9 | 19,990 | 1,999 |
| Forgotten Ruins | 10 | 21,589 | 2,159 |

Victory rate affects both rewards. Combat-XP bonuses and defeat-retention bonuses affect XP only; they do not multiply Cinders.

## Dungeon rewards

Dungeon settings live in `LL/src/API/API.LL/Data/progression/dungeon-rewards.json`. Dungeons deliberately use encounter budgets rather than hourly budgets because players run approximately two or three dungeons per day.

```text
base tier-1 combat encounter = 2,500 XP + 100 Cinders
tier reward = base reward × 1.40 ^ (dungeon tier - 1)
miniboss reward = tier reward × 1.50
boss reward = tier reward × 2.50
```

| Tier | Combat encounter | Miniboss | Boss |
| ---: | ---: | ---: | ---: |
| 1 | 2,500 XP / 100 Cinders | 3,750 / 150 | 6,250 / 250 |
| 2 | 3,500 XP / 140 Cinders | 5,250 / 210 | 8,750 / 350 |
| 3 | 4,900 XP / 196 Cinders | 7,350 / 294 | 12,250 / 490 |

With the current 10–16 room definitions, 80% normal-combat room weighting, one miniboss, one boss, and one checkpoint, a representative successful run grants approximately:

| Dungeon tier | Approximate XP/run | Approximate combat Cinders/run |
| ---: | ---: | ---: |
| 1 | 26,000–28,000 | 1,040–1,120 |
| 2 | 39,000–42,000 | 1,570–1,680 |
| 3 | 59,000–67,000 | 2,350–2,670 |

These estimates exclude event rewards, route rewards, completion tables, mastery multipliers, losses, and checkpoint penalties. They describe only victorious combat encounters.

## Character XP curve

The unlimited character curve is data-driven in `character-experience.json`:

```text
XP required for next level = round-to-25(100 + 93 × level²)
```

Representative requirements are:

| Level | XP for next level |
| ---: | ---: |
| 1 | 200 |
| 20 | 37,300 |
| 40 | 148,900 |
| 60 | 334,900 |
| 80 | 595,300 |
| 100 | 930,100 |

The curve continues beyond level 100 and is stored as 64-bit character experience. Area throughput, 24-hour offline processing, and the expected victory-rate model target roughly two months to reach level 100.

## Prophecy comparison

Prophecy XP remains a percentage of the next-level requirement. Prophecies do not grant Cinders; Cinder acquisition remains attached to world combat, dungeons, and other economy activities. This gives Prophecies a clearer reward identity around XP, Soulstones, Sigil Fragments, Fate Echo, caches, and progression materials.

## Content-authoring rules

When adding an area:

1. Assign a unique difficulty tier.
2. Author group-size probabilities and creature weights for combat variety.
3. Do not tune creature data to alter XP or Cinders.
4. Expect both area reward targets to grow by `1.08` per tier.

When adding a dungeon:

1. Assign its progression tier on the dungeon definition.
2. Tune room count and room-type composition deliberately because each victorious combat room pays a reward.
3. Change global dungeon bases, tier growth, or room multipliers in `dungeon-rewards.json` rather than changing creatures.
4. Account for route, event, completion, and mastery rewards separately from combat rewards.

## Remaining risks

- Per-encounter integer rounding is proportionally significant for low Cinder values. Measure realized hourly income in telemetry rather than assuming the nominal target is exact.
- Dungeon event and completion Cinders remain separate sources and can make total run income exceed the combat-only estimates.
- Marketplace and crafting prices should be evaluated against daily income after this source change.
- Existing local balances and database columns are not converted. A recreated local database uses the new schema without `Creature.ExperienceReward`.
