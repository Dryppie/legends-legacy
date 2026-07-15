# Prophecy Objective Target Scaling

## Action budget

Idle combat and tempering perform one action every 10 seconds:

```text
360 actions per hour
8,640 actions per day
60,480 actions per week
```

Daily action-driven targets use approximately one hour for Common, two hours for Uncommon, three hours for Rare, and four hours for Epic. Weekly targets remain multi-day objectives, ranging from roughly 40 to 100 action-hours depending on difficulty and staying below five full days so the weekly objective remains compatible with the Revelation track's two-day allowance.

## Objective conversion

| Objective family | Conversion |
| --- | --- |
| Encounter wins | Action budget multiplied by the 85% reference win rate. |
| Creature kills | Uses one creature per successful encounter as the guaranteed baseline. Multi-creature encounters complete the objective faster. |
| Tempering and Potential | One progress unit per 10-second crafting action. |
| Gathering | Lower than raw actions because progress requires a victorious encounter, a matching equipped tool, and a successful node roll. |
| Essence XP | Benchmarked against the configured 10,000 tier-0 XP per hour; additional attuned Essences can accelerate it. |
| Treasure | Uses loot items actually produced, so it has a separate conservative budget. |
| Dungeons | Uses the intended 2–3 runs per day rather than the idle action budget. |
| Unique creatures and archive actions | Content-availability objectives; they are not multiplied by action count. |

## Representative targets

| Objective | Daily Common | Daily Rare | Weekly Uncommon | Weekly Epic |
| --- | ---: | ---: | ---: | ---: |
| Win encounters | 300 | 900 | 18,350 | 30,600 |
| Defeat creatures | 300 | 900 | 35,000 | 58,000 |
| Temper items | 360 | 1,080 | 21,600 | 36,000 |
| Gain Essence XP | 10,000 | 30,000 | 600,000 | 1,000,000 |
| Gather resources | 125 | 375 | 5,000 | 8,000 |

Dungeon completion is deliberately separate: the active Rare daily requires 3 runs and the Rare weekly requires 14. The weekly target is reachable through two runs on all seven days or close to three runs across five days.

Targets remain snapshots. Updating `targets.json` changes future offers and rerolls, not accepted Prophecies already in progress.
