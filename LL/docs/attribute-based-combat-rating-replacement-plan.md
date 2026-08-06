# Attribute-Based Combat Rating Replacement Plan

## Objective

Replace the simulation-benchmark Power Rating system with a deterministic,
attribute-based Combat Rating for:

1. the player character's displayed Combat Rating; and
2. every dungeon's recommended Combat Rating.

The player-facing name remains **Combat Rating**. Existing internal API and
persistence names containing `PowerRating` or `PowerRecommendation` may remain
for compatibility, but their semantics become attribute-based.

Active Essence attribute bonuses are included. Active abilities, passive
abilities, and other non-attribute effects remain intentionally excluded until
an ability valuation model exists.

## Why the Existing System Is Being Replaced

The current character rating is the highest synthetic benchmark intensity a
build can clear. That approach:

- produces coarse ten-point jumps;
- is sensitive to benchmark enemy tuning and cooldown policy;
- makes rating changes difficult to explain;
- is expensive compared with a direct calculation; and
- can disagree with real dungeon performance when the synthetic encounter is
  not representative.

The game already has a versioned attribute-cost catalog used to balance
equipment. Combat Rating should use the same source of truth.

## Rating Contract

### Inputs

The current version uses:

- the character's persisted base attributes;
- modifiers from currently equipped, distinct equipment instances; and
- level-scaled and evolution-added attribute modifiers from Essences in the
  active loadout; and
- the active `EquipmentStatBudgetCatalog` attribute weights.

The current version does not use:

- Essence active or passive abilities;
- temporary combat modifiers;
- buffs, consumables, dungeon modifiers, or transient health;
- synthetic benchmark outcomes; or
- item rarity, quality, Potential, or tempering as independent bonuses.

Quality, rarity, Potential, and tempering affect Combat Rating only through the
attribute modifiers they produce.

### Direct Attributes and Equipment Budgets

Persisted base attributes are valued directly. Power is valued as the universal
ability coefficient; Max Health, defenses, recovery, and utility each receive their
own explicit weight. No attribute implicitly produces another attribute.

Each distinct equipped item separates authored base modifiers from generated
instance modifiers. Base modifiers use stable Tier-1 reference weights so the
same item identity cannot lose rating at a higher crafting tier. Generated and
tempered instance modifiers use the item's tier-aware costs, matching
`EquipmentBudgetEvaluator`. This prevents quality, rarity, or tier from receiving
a second independent bonus while keeping canonical tier progression monotonic.

Each active Essence is resolved independently through
`IEssenceCombatLoadoutResolver`. Its fixed and evolution-added attribute
modifiers are valued at the catalog's reference tier.

Capped attributes are valued only up to their combined useful combat cap across
base, equipment, and active Essence sources. When sources have different tier
weights, over-cap budget is discounted proportionally so source ordering cannot
change the result.

### Weights and Scale

Base attributes and authored equipment-base modifiers use the Tier-1
`EquipmentStatBudgetCatalog.CostPerPoint` as a stable reference. Generated and
tempered equipment modifiers use the catalog cost for that item's tier.
Tier-aware generated weights are required because Armor, Max Health, and other
diminishing-return attributes intentionally have different marginal costs
across the progression curve.

The unrounded sum is converted to a non-negative whole number using
`MidpointRounding.AwayFromZero`.

Formula:

```text
CombatRating =
    round(
        BaseAttributeBudget
        + Σ EquippedItemAttributeBudget
        + Σ ActiveEssenceAttributeBudget
    )
```

There is no ten-point quantization.

### Component Fields

The existing detailed rating response is preserved for API compatibility.
`Overall` becomes the Combat Rating. Component fields become deterministic
attribute-budget contributions:

- single-target offense: offensive attributes;
- multi-target offense: the same attribute-only offense contribution until
  ability shape is modeled;
- physical durability: health and physical-defense attributes;
- magical durability: health and magical-defense attributes;
- sustain: healing, regeneration, life-steal, and Spirit contribution; and
- control utility: zero until abilities and control effects are modeled.

Dungeon readiness must not create qualitative strength/weakness insights from
these partial component contributions until Essence and ability valuation is
implemented. Its real-dungeon completion simulation remains authoritative for
the readiness probability.

## Character Calculation

1. Load the server-owned character overview and equipped item instances.
2. Deduplicate equipment instances so a two-handed item is counted once.
3. Value base attributes at the fixed reference tier, each distinct equipped
   item's modifiers at its own tier, and each active Essence's resolved
   attribute modifiers at the fixed reference tier.
4. Calculate the deterministic Combat Rating and component breakdown.
5. Return `Available` with `High` confidence.
6. Retain the combat snapshot and its full fingerprint for real-dungeon
   readiness simulation. Essence state remains in that fingerprint because it
   affects the readiness simulation, even though it does not yet affect Combat
   Rating.

No rating cache is necessary because the calculation is in-memory arithmetic
after the character load.

## Dungeon Recommendation Calculation

Dungeon recommendations retain real-dungeon calibration, but no synthetic Power
benchmark is involved:

1. Build the ordered canonical equipment progression ladder.
2. For each canonical profile, simulate the actual dungeon until the first rung
   reaches the target completion rate.
3. Calculate that rung's Combat Rating from its direct baseline attributes and
   tier-aware equipment budget using the same calculator as a real character.
4. Use the Balanced profile's rating as the recommendation.
5. Preserve the specialized-profile range, completion diagnostics, persistence,
   and confidence reporting.

This ensures the number shown on a dungeon card and the number shown on the
character are on exactly the same scale.

## Versioning and Persistence

- Increment `PowerRatingAlgorithm.Version` because the public rating semantics
  change.
- Increment the rating-definition version used in dungeon calibration identity.
- Continue storing `EquipmentStatBudgetCatalog.BalanceVersion`.
- Existing persisted recommendations become stale automatically and are
  recalibrated by the startup worker.
- No database schema migration is required for the replacement itself.
- The already-created equipment-balance-version migration remains required if
  it has not been applied.

## Implementation Steps

1. Add a pure `CombatRatingCalculator` that:
   - projects direct attributes;
   - applies caps and reference weights;
   - returns an overall rating and deterministic component breakdown.
2. Extend `PowerBuildSnapshot` with the projected rating attributes.
3. Replace `PowerRatingService` benchmark searches and seed execution with the
   calculator.
4. Extend canonical builds with direct rating attributes.
5. Change `DungeonPowerAnalyzer` to rate the first passing canonical build
   directly rather than sending it through the synthetic Overall benchmark.
6. Stop producing attribute-only readiness strengths and weaknesses that imply
   ability knowledge.
7. Update dependency registration and documentation.
8. Replace benchmark-oriented rating tests with deterministic calculation tests.
9. Verify character rating, canonical rating, cache invalidation identity,
   dungeon persistence, readiness behavior, and the broader test suite.

## Acceptance Criteria

- Character Combat Rating performs no combat simulations.
- The same base attributes, equipped item modifiers, and active Essence
  progression always produce the same rating.
- Adding a positive equipment attribute cannot lower Combat Rating.
- Derived primary contributions are not double counted.
- Fixed-cap overflow does not increase Combat Rating.
- Essence loadout or evolution changes alter Combat Rating only when they
  change the resolved active Essence attribute budget. Essence levels and
  Ascension Tiers do not scale attribute bonuses.
- Equipment changes do alter Combat Rating.
- Dungeon recommended Combat Rating uses the identical calculator and weights.
- Persisted recommendations from the previous algorithm are rejected as stale.
- All player-facing labels continue to say **Combat Rating**.
- Relevant tests and the solution build pass, or any environmental blockers are
  reported.

## Deferred Essence Ability Phase

The later Essence ability phase should extend the separately testable Essence
contribution with:

- expected active-ability output per cooldown cycle;
- passive trigger frequency and expected value;
- healing, barriers, summons, and control uptime;
- Ascension scaling;
- evolution modifiers; and
- conditional-effect reliability.

The total can then become:

```text
CombatRating =
    AttributeAndEquipmentCombatRating
    + EssenceAttributeCombatRating
    + EssenceAbilityCombatRating
```

The attribute portion defined by this plan should remain unchanged unless the
equipment balance catalog itself changes.
