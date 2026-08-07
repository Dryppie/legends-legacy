# Attribute-based Combat Rating and dungeon readiness

## Player-facing contract

The feature is called **Combat Rating** everywhere the player sees it. Some
internal types, endpoints, and database columns retain their historical
`PowerRating` names for compatibility.

Combat Rating is deterministic attribute arithmetic. It is not the result of a
synthetic benchmark encounter.

The current version includes:

- persisted character base attributes; and
- modifiers on currently equipped, distinct equipment instances; and
- resolved attribute modifiers from Essences in the active loadout.

The current version deliberately excludes Essence abilities, temporary combat modifiers,
consumables, and dungeon-only effects. The API status message states that
Essence abilities are not yet included.

The detailed design and rollout checklist are in
`attribute-based-combat-rating-replacement-plan.md`.

## Calculation

`CombatRatingCalculator` first projects the character's final direct attributes
from persisted base attributes, each distinct equipped item, and resolved active
Essence modifiers. It then values every final attribute at the Tier-1
`EquipmentStatBudgetCatalog` reference cost. This makes identical combat stats
worth identical Combat Rating regardless of whether they came from an authored
item base, a generated or tempered modifier, or an Essence.

The projection applies each modifier's real flat, additive, or multiplicative
semantics before valuation. Tier-aware costs remain crafting-budget rules; they
do not change the combat value of an already-generated stat point.

Each active Essence is resolved independently through the production Essence
loadout resolver. Fixed bonuses and evolution-added attribute modifiers are
valued at the catalog's reference tier. Essence level and Ascension do not
receive arbitrary rating bonuses; evolution matters only when it changes
resolved attributes.

Capped attributes are valued only to their combined useful cap after projection.
The internal total is rounded once to a whole number using
`MidpointRounding.AwayFromZero`; there is no ten-point quantization.

Formula:

```text
Combat Rating =
    round(
        Σ useful final attribute points
          × that attribute's Tier-1 reference cost
    )

Displayed Combat Rating = Combat Rating / 10
```

The UI applies the display divisor after calculation and rounds down to a whole
number. Internal calculation, persistence, readiness comparisons, and
calibration retain the unscaled integer so small attribute improvements keep
their full precision.

Equipment rarity, quality, Potential, and tempering have no independent rating
bonus. They matter only through the equipment attributes they produce.

The existing detailed response shape is retained. `Overall` is the internal
Combat Rating consumed by the UI's display conversion. Other fields are
deterministic attribute contribution groups, not independent encounter
benchmarks. Control Utility remains zero until Essence and ability valuation is
introduced.

## Character snapshots

The server still builds a production `CombatEntity` with the active Essence
loadout for real-dungeon readiness simulation. The same snapshot carries the
deterministic base, equipment, and active-Essence attribute rating.

The fingerprint includes character level, base attributes, equipped item
identity and modifiers, item progression fields, affinity tags, and active
Essence progression. Essence fields remain in the fingerprint because they
affect both active Essence attributes and real-dungeon readiness.

Character rating is cheap in-memory arithmetic after the character load and has
no process-local rating cache or combat seeds.

## Dungeon recommended Combat Rating

`DungeonPowerAnalyzer` continues to calibrate against the real dungeon rather
than an invented benchmark:

1. `CanonicalEquipmentBuildFactory` crafts detached instances from authored
   recipes and item bases using the production stat-roll and tempering rules.
2. Each canonical profile runs the actual dungeon over the fixed route seeds.
3. Each profile's equipment rungs are ordered by calculated Combat Rating, and
   the lowest-rated rung reaching the 72% completion target is selected.
4. Actual equipment modifiers and Region 1 Essence attributes are passed to
   `CombatRatingCalculator`.
5. The lowest eligible first-passing profile rating supplies Recommended Combat Rating;
   all valid profiles supply the diagnostic lower and upper range.

The character number and dungeon recommendation therefore use the exact same
weights and scale.

Canonical builds exclude player-specific permanent bonuses. They equip these
fixed, reproducible game-content loadouts. Essence order matters: Tier I uses
the first two, Tier II the first four, and Tier III all six.

| Profile | Level anchor | Armor | Weapon | Ordered Region 1 Essence pool |
| --- | ---: | --- | --- | --- |
| Balanced | 5 | Medium Mail, Medium Helm, Medium Greaves | Greatsword | Goblin, Vampire Bat, Goblin Warrior, Enchanted Fairy, Goblin Archer, Pixie |
| Offense | 15 | Light Vest, Light Hood, Light Legwraps | Gauntlets | Goblin Archer, Glade Panther, Goblin Warrior, Flame Imp, Hobgoblin, Vampire Bat |
| Sustain | 15 | Cloth Robe, Cloth Cowl, Cloth Pants | Staff | Enchanted Fairy, Pixie, Treant Sapling, Goblin Shaman, Brown Slime, Green Slime |
| Defensive | 10 | Heavy Breastplate, Heavy Helm, Heavy Legplates | Maul | Brown Slime, Goblin Warrior, Treant Sapling, Goblin Shaman, Blue Slime, Moss Lizard |
| Area | 15 | Cloth Robe, Cloth Cowl, Cloth Pants | Staff | Flame Imp, Pixie, Frost Imp, Shadow Imp, Goblin Shaman, Rainbow Slime |

The effective character level is raised when necessary to make the loadout
legal under the production slot-unlock rule: at least level 10 for two
Essences, level 30 for four, and level 50 for six.

Every profile also equips the authored Ring, Amulet, and Relic recipes. The
calibration ladder is a complete matrix of 120 full equipment sets: equipment
Tiers 1 through 20, each represented once at Common, Uncommon, Rare, Epic,
Unique, and Legendary rarity. Quality is always Standard, so rarity and
equipment tier are the only gear-progression axes.

Tiers 1 through 10 use the production equipment budget table. Tiers 11 through
20 are calibration-only projections that continue the Tier-10 budget by 25% per
tier. They do not unlock live crafting content; they expose when a dungeon
currently requires more equipment power than the live Tier-10 catalog can
provide. Recommendation diagnostics label such a selected rung as projected.

Essence abilities are the actual abilities owned by those Essences; dungeon
calibration no longer injects synthetic canonical damage, healing, or barrier
abilities. Combat Rating itself still counts only their resolved attributes.

The deterministic Goblin Mines regressions currently resolve to:

- Tier I: displayed Combat Rating 154, Tier-2 Standard/Uncommon Defensive
  equipment, and two Essences.
- Tier II: displayed Combat Rating 608, Tier-5 Standard/Uncommon Balanced
  equipment, and four Essences.
- Tier III: displayed Combat Rating 2490, Tier-11 Standard/Common Offense
  equipment, and six Essences. Tier 11 is a calibration-only projection beyond
  the live equipment table.

The Tier-III result now exposes that its current combat tuning extends beyond
the live Tier-10 equipment budget. These snapshots are deterministic calibration
results, not separately authored dungeon numbers.

The requirement profile still examines real encounter groups and creature
abilities to describe physical, magical, area, boss, control, and attrition
pressure.

## Persistence and startup calibration

The API startup worker loads recommendations from
`DungeonPowerRecommendationCacheEntries`. Rows are reusable only when dungeon
identity, tier, content hash, algorithm version, combat-rules version, rating
definition version, recommendation seed version, and equipment balance version
match.

`PowerRatingAlgorithm.Version` is 23 for source-independent final-attribute
valuation over the full Standard-quality tier-by-rarity profile matrix.
Character levels use a deterministic base-attribute curve: every completed
level-up grants `+0.25 Power` and `+20 Max Health`. At the current attribute
costs, those gains consume 6 and 4 internal rating-budget points respectively,
for 10 internal points (1 displayed Combat Rating) per completed level-up.
`CombatRulesVersion` is 10 for the Power-only damage model and compounded dungeon difficulty
curve: Tier I is 2.75× the authored creature baseline, Tier II is 5× Tier I
(13.75× authored), and Tier III is 5× Tier II (68.75× authored).
The legacy-named `BenchmarkDefinitionVersion` column now stores
the deterministic rating-definition version and is 13. Existing recommendations
are stale and will be recalibrated.

`DungeonPowerCalibration:Enabled` controls whether missing or stale entries may
be calculated. Database loading occurs regardless. Normal dungeon lists read
the in-memory recommendation store and never wait for calibration simulations.

## Readiness probability

Detailed dungeon readiness remains a separate, real-dungeon simulation. It runs
the player's complete combat snapshot, including equipped Essences, in batches
from 8 to 24 deterministic samples. The 95% Wilson interval maps to:

- Very Unlikely: below 15%
- Risky: 15% to below 40%
- Uncertain: 40% to below 60%
- Favored: 60% to below 80%
- Comfortable: 80% or higher

Attribute-only component totals cannot describe area ability shape, control
uptime, or conditional Essence utility. Qualitative strength/weakness insights
are therefore suppressed until ability valuation exists. The actual completion
probability remains available.

Simulation is detached and does not synchronize health, grant rewards, spend
items or Vigor, publish events, or persist dungeon state.

## Versioning

- Increment `PowerRatingAlgorithm.Version` when overall Combat Rating semantics
  change.
- Increment the rating-definition version when weights, caps, projection, or
  canonical rating policy changes.
- Increment `CombatRulesVersion` when actual dungeon combat changes.
- Increment the dungeon or recommendation seed version when those seed suites
  change.
- Increment `EquipmentStatBudgetCatalog.BalanceVersion` when attribute costs or
  equipment balance changes.

Any relevant increment invalidates persisted dungeon recommendations.

## Deferred Essence ability rating

A later phase will extend the separately testable Essence contribution with
active cooldown throughput, passive trigger frequency, healing, barriers,
summons, control uptime, Ascension ability scaling, evolution ability modifiers,
and conditional reliability.

The intended composition is:

```text
Combat Rating =
    Attribute and Equipment Combat Rating
    + Essence Attribute Combat Rating
    + Essence Ability Combat Rating
```

## Database and deployment

No new schema migration is required for the attribute-based replacement.

Apply the existing `PersistDungeonPowerRecommendations` and
`AddDungeonPowerEquipmentBalanceVersion` EF Core migrations before deploying if
they have not already been applied. The application does not apply migrations
automatically. Restarting the API with calibration enabled recalculates stale
recommendations on the current scale.
