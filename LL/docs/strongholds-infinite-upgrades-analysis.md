# Strongholds: Infinite Upgrade Analysis

**Date:** 25 September 2026  
**Related design:** [Mechanical Progression Revision](strongholds-mechanical-progression-revision.md)  
**Status:** This document controls where the two designs differ on levels, access, and costs.

## Verdict

Infinite building levels can give Strongholds permanent relevance. They also risk runaway rewards, old-player snowballing, and meaningless late upgrades.

The workable version is:

- levels have no design cap;
- costs grow faster than benefits;
- benefits use diminishing-return formulas;
- most benefits improve targeting, not reward volume;
- competitive modes ignore or normalize Stronghold bonuses;
- every facility and directive is available from the start;
- levels improve strength and appearance, not access.

Do not use linear scaling. A permanent `+1%` per level eventually breaks every affected system.

## Core Model

Each facility has an uncapped level.

- Every facility starts at level 1.
- Every directive is usable at level 1.
- Further levels cost **Soulstones only**.
- Every level improves the facility.
- Visual changes happen automatically at authored level thresholds.
- There are no timers, daily tasks, or reward claims.

Illustrative cost curve:

```text
SoulstoneCost(nextLevel) = roundTo25(25 × (nextLevel - 1)^1.9)
```

This roughly follows the current early Soulstone curve, then grows indefinitely. Tune it against actual Soulstone income before release.

The formulas must use checked 64-bit values and a documented technical ceiling. The design can be uncapped even though storage cannot be literally infinite.

## Currency Choice

| Model                     | Strength                                                                                                 | Main problem                                                           |
| ------------------------- | -------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------- |
| Soulstones only           | Matches permanent progression, preserves existing investment, and avoids marketplace wealth buying power | May require higher long-term Soulstone income                          |
| Cinders only              | Familiar, steadily earned, and thematically fits construction                                            | Competes with Equipment and trading; large hoards can skip progression |
| Both currencies per level | Sinks both economies                                                                                     | Creates two scarcity gates and is harder to balance                    |

### Recommendation: Soulstones only

Use Soulstones for all mechanical building levels.

Soulstones already fund permanent upgrades and lose their main purpose when the standalone page is removed. They are also outside the Cinder marketplace and Equipment-cost loops. This gives the Stronghold a clear economy without disturbing trading or reinforcement prices.

Cinders may fund optional themes or visual renovations, but not building levels. That keeps cosmetics separate from mechanical progression.

Do not require both currencies for one level. Players would often have plenty of one and be blocked by the other, making half their earnings feel irrelevant.

## Scaling Functions

Use different curves for different effects.

### Target weighting

```text
SelectedWeightMultiplier = 1 + coefficient × sqrt(level)
```

This keeps improving but approaches certainty slowly after weights are normalized.

### Reward-rate bonuses

```text
RelativeBonus = coefficient × ln(1 + level)
```

This is unbounded but grows very slowly. It suits rare drops and experience better than linear scaling.

### Probabilities and retention

```text
Effect = ceiling × (1 - e^(-level / pace))
```

Chance-based effects cannot scale past 100%. An asymptotic curve lets every level help without reaching the ceiling.

Illustrative growth:

| Level | `sqrt(level)` | `ln(1 + level)` |
| ----: | ------------: | --------------: |
|     1 |          1.00 |            0.69 |
|    10 |          3.16 |            2.40 |
|   100 |         10.00 |            4.62 |
| 1,000 |         31.62 |            6.91 |

These formulas are design examples, not approved balance values.

## Buildings

### Arsenal

Each level strengthens the selected Equipment category, subtype, and Variant weight.

Suggested formula:

```text
SelectedWeightMultiplier = 1 + 0.4 × sqrt(level)
```

At levels 1, 10, 100, and 1,000, the multiplier is about `1.4x`, `2.3x`, `5x`, and `13.6x`. Other eligible drops remain possible after normalization.

Category, subtype, and Variant targeting are all available at level 1. Levels only strengthen their weights.

The Arsenal should never increase Equipment quantity, rarity, or quality. Infinite scaling already makes targeting powerful.

### Essence Conservatory

Each level improves the current Essence package:

- relative Essence drop rate;
- pity progression;
- focused-creature bonus;
- duplicate-dismantle chance.

Example drop-rate curve:

```text
RelativeEssenceBonus = 0.08 × ln(1 + level)
```

That gives roughly `+14%` at level 5, `+37%` at level 100, and `+55%` at level 1,000.

Duplicate chance should use an asymptotic ceiling. Creature Focus remains one shared setting with the Creature Archive.

All four effects and Creature Focus controls are available at level 1.

### Cartographer's Lodge

Each level improves Sigil acquisition and the weight of the surveyed Dungeon family.

Example curves:

```text
RelativeSigilBonus = 0.05 × ln(1 + level)
SurveyWeight = 1 + 0.4 × sqrt(level)
```

If Sigils use deterministic progress, levels shorten the required progress instead of modifying drop chance. Do not run random and deterministic bonuses together.

Rest Site reward retention must use an asymptotic ceiling below 100%.

Survey selection, Sigil improvement, and Rest Site retention are available at level 1.

### War College

Each level improves combat experience and defeat retention.

Example experience curve:

```text
RelativeExperienceBonus = 0.04 × ln(1 + level)
```

This gives roughly `+7%` at level 5, `+18%` at level 100, and `+28%` at level 1,000.

Defeat retention should approach a ceiling. The War College should not grant direct combat stats.

### Gallery of Legacy

Gallery levels increase prestige, display capacity, and visual detail. They do not improve reward rates.

All display controls are available immediately. Higher levels automatically make the Gallery larger and more elaborate.

### Main Hall

Do not sell Main Hall levels separately. A global Hall multiplier would always be the best first purchase and would compound every other facility.

Instead, derive Hall level from total facility investment:

```text
HallLevel = floor(total facility levels / facility count)
```

Hall thresholds automatically change Stronghold-wide visuals. They do not gate facilities, directives, or benefit tiers.

## Soulstone Migration

Remove the standalone Soulstones page and convert its active upgrades into facility progress.

| Current branch    | Facility             |
| ----------------- | -------------------- |
| EssenceArchive    | Essence Conservatory |
| CombatProgression | War College          |
| Dungeons          | Cartographer's Lodge |

Migration should preserve both value and spending:

1. Calculate invested Soulstones per current branch.
2. Buy target-facility levels sequentially with that value.
3. Return any remainder to the Soulstone balance.
4. Preserve an old effect as a legacy floor if conversion would reduce it.
5. Preserve retired-upgrade refunds.

Do not reset players or refund everything into a shared balance. That would force them to rebuild existing progression and could change their power unexpectedly.

## Progression Loop

```text
Play existing content
→ earn Soulstones
→ improve a chosen facility
→ strengthen its directives
→ snapshot directives when starting an activity
→ receive more focused rewards
```

Players choose which building advances first. They do not choose which completed building is active.

## Main Risks

| Risk                        | Control                                                                        |
| --------------------------- | ------------------------------------------------------------------------------ |
| Reward inflation            | Logarithmic quantity bonuses and steeper cost growth                           |
| One optimal building        | Different goals, no global Hall multiplier                                     |
| Old-player advantage        | Normalize or disable bonuses in competitive modes                              |
| Meaningless late levels     | Show exact next-level impact and automatic visual progress                     |
| Currency overflow           | Checked 64-bit math and a documented technical ceiling                         |
| Balance becoming unreadable | Centralize formulas and expose resolved effects in the UI                      |
| Too many buildings          | Keep the focused facility set; do not restore the original 39-building catalog |

## Recommended MVP

1. Implement uncapped Arsenal levels and category targeting.
2. Add the Soulstone-only level curve.
3. Derive Main Hall level from facility levels.
4. Move existing Soulstone upgrades into their facilities.
5. Add the Lodge and Conservatory only after simulations validate the formulas.

Run long-horizon simulations at levels `1`, `10`, `100`, `1,000`, and the highest technically reachable level. Measure reward volume, target share, currency time-to-level, and competitive impact.

## Recommendation

Use infinite levels only with diminishing returns. The strongest endless benefit should be **control over reward composition**, not endlessly multiplying the number or quality of rewards.

Use Soulstones as the sole mechanical level cost. Keep every facility and directive available from level 1. Linear bonuses, global Hall multipliers, or uncapped direct combat stats would make the Stronghold dominant and should be rejected.
