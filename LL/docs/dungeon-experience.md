# Dungeon experience

Dungeon encounter XP uses the dungeon's region as its progression tier. Normal,
Heroic and Mythic are difficulty values 1, 2 and 3 respectively. The existing
runtime `DungeonDefinition.Tier` property continues to represent difficulty.

```text
XP = round(1000 × 2.2^(region − 1) × 1.4^(difficulty − 1) × room multiplier)
```

Room multipliers are 1 for regular combat, 1.5 for minibosses and 2.5 for bosses.
Round once after all multipliers, with midpoint values rounded away from zero.
Combat XP bonuses apply afterward, rounded down to whole XP.

| Progression tier | Dungeons                          | Normal combat | Heroic combat | Mythic combat |
| ---------------- | --------------------------------- | ------------: | ------------: | ------------: |
| 1                | Goblin Mines, Forgotten Catacombs |         1,000 |         1,400 |         1,960 |
| 2                | Tangled Cave, The Great Tree      |         2,200 |         3,080 |         4,312 |

The 2.2 region multiplier is based on the first normal combat areas in Regions
1 and 2. Area XP targets are `10000 × 1.08^areaDifficultyTier` per hour:
Lumo Ruins (area tier 1) targets 10,800 XP/hour, while Warfang Frontier
(area tier 11) targets approximately 23,316 XP/hour. Their ratio is
`1.08^10 ≈ 2.159`; 2.2 is a rounded tuning value within 2% of that ratio.
These are targets before XP bonuses, assuming victories at the configured
10-second encounter cadence. Integer reward rounding and enemy-count variation
make actual awards differ slightly. Lumo usually awards 29 XP for one enemy;
Warfang usually awards 66 XP for two enemies.

Only victorious encounters add XP to pending loot. XP is paid on reward claim
after completion or safe retreat; failure loses pending XP. The selected dungeon
Essence loadout receives the claimed XP through the existing Essence award path.

The region multiplier applies only to XP. Cinders retain their difficulty and
room scaling. Dungeon mastery progression is separate and unchanged.

Balance values live in `src/API/API.LL/Data/progression/dungeon-rewards.json`:
`difficultyMultiplier` replaces the old, ambiguous `tierMultiplier` setting;
`progressionTierExperienceMultiplier` controls the region curve. Ship this file
with the updated backend and restart processes that load dungeon reward balance.
No database migration is required. Existing pending XP is not recalculated.
