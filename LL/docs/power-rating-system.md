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

`CombatRatingCalculator` values persisted base attributes before
primary-derived contributions, preventing Fortitude, Precision, and Spirit from
being counted again through derived attributes. Base attributes use Tier-1
`EquipmentStatBudgetCatalog` costs as a stable reference.

Each distinct equipped item is valued with the same amount-times-tier-cost
formula as `EquipmentBudgetEvaluator`, using that item's tier. This is the same
tier-aware attribute budget used by crafting and item DTOs. It preserves the
intentional marginal-cost changes for attributes such as Armor and Fortitude
across the progression curve.

Each active Essence is resolved independently through the production Essence
loadout resolver. Level-scaled bonuses and evolution-added attribute modifiers
are valued with the same catalog at the Essence's Potential Tier. Potential,
Ascension, and evolution do not receive arbitrary rating bonuses; they matter
only when they change resolved attributes.

Capped attributes are valued only to their combined useful cap across base,
equipment, and active Essence sources. Mixed-tier over-cap budget is discounted
proportionally, making the result independent of source ordering. The internal
total is rounded once to a whole number using
`MidpointRounding.AwayFromZero`; there is no ten-point quantization.

Formula:

```text
Combat Rating =
    round(
        Base Attribute Budget
        + Σ Equipped Item Attribute Budget
        + Σ Active Essence Attribute Budget
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

1. `CanonicalEquipmentBuildFactory` creates the attainable equipment ladder.
2. Each canonical profile runs the actual dungeon over the fixed route seeds.
3. The first rung reaching the 72% completion target is selected.
4. That build's direct attributes are passed to `CombatRatingCalculator`.
5. The Balanced profile supplies Recommended Combat Rating; specialized
   profiles supply the diagnostic lower and upper range.

The character number and dungeon recommendation therefore use the exact same
weights and scale.

Canonical builds exclude player-specific permanent bonuses and player Essence
loadouts. The low end starts at base attributes, then the tutorial chest, a real
two-handed weapon, and whole-slot acquisition. Later rungs cover tier, quality,
rarity, and tempering progression.

The requirement profile still examines real encounter groups and creature
abilities to describe physical, magical, area, boss, control, and attrition
pressure.

## Persistence and startup calibration

The API startup worker loads recommendations from
`DungeonPowerRecommendationCacheEntries`. Rows are reusable only when dungeon
identity, tier, content hash, algorithm version, combat-rules version, rating
definition version, recommendation seed version, and equipment balance version
match.

`PowerRatingAlgorithm.Version` is 15 for active Essence attribute inclusion.
The legacy-named `BenchmarkDefinitionVersion` column now stores
the deterministic rating-definition version and is 10. Existing recommendations
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
version-13 recommendations on the new scale.
