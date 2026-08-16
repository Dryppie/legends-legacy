# Hybrid Equipment Attribute Units and Tier-Upgrade Contract

Status: Partially implemented. The approved armor recipe profiles are authored;
the hybrid unit conversion and activation work remains pending.

## Purpose

Equipment attributes should use the unit that best communicates their gameplay
effect. Probabilities, rates, capped modifiers, and proportional effects should
be genuine percentages. Flat quantities should remain flat. Ratings should be
reserved for the small number of opposed, open-ended defensive values that need
to scale continuously with enemy progression.

This design also establishes a mandatory tier-upgrade contract. Every equipment
recipe must contain at least one attribute whose raw value grows with equipment
tier. Percentage-only equipment is prohibited because its values intentionally
remain stable across tiers and would otherwise allow an item to linger unchanged
through later regions.

The intended player experience is:

- a recipe, crafted item, marketplace listing, and chat link always show the same
  intrinsic values to every viewer;
- a percentage printed on an item is the percentage stored by that item;
- percentage attributes do not secretly become ratings after crafting;
- only Armor and Resistance remain ratings;
- every next-tier item has a visible progression attribute that increases;
- exceptional rarity, quality, tempering, and Blueprint investment can still make
  a lower-tier item temporarily competitive without making ordinary previous-tier
  equipment the default choice indefinitely.

## Design principles

### One printed unit has one meaning

If an item says:

```text
Critical Chance +10%
```

the item contains ten percentage points of Critical Chance. It must not contain a
hidden rating whose effective result depends on the viewer.

If an item says:

```text
Armor +64
```

it contains 64 Armor rating. The character sheet may translate the character's
total Armor into Physical Damage Reduction, but the item itself remains a stable
64 Armor for every player.

### Intrinsic item values are universal

The following must never vary by viewer:

- recipe roll ranges;
- crafted item attributes;
- marketplace listing attributes;
- guild-vault item attributes;
- chat-linked item attributes;
- tempering results;
- Blueprint-added attributes.

An optional `Compare with equipped` view may calculate personal before-and-after
results, but those projections are supplemental and are not part of the item's
canonical identity.

### Percentage effects are already scale-independent

A genuine +10% Critical Chance remains proportionally useful when the character's
damage grows. A genuine 20% Cooldown Reduction remains proportionally useful when
an ability becomes stronger. These attributes do not need exponentially larger
numbers at later tiers to remain relevant.

Tier scaling should therefore increase flat progression values and opposed
ratings, while direct percentages retain a stable normalized magnitude.

### Every item needs a tier-scaling anchor

Because direct percentage magnitudes remain stable by tier, every recipe must
allocate a meaningful share to at least one progression attribute:

- Power;
- Maximum Health;
- Health Regeneration;
- Armor;
- Resistance.

These attributes are called **tier anchors**. A recipe without a tier anchor fails
content validation.

## Canonical attribute classification

### Flat tier-scaling attributes

| Attribute           | Item unit | Player-facing meaning                | Tier behavior       |
| ------------------- | --------- | ------------------------------------ | ------------------- |
| Power               | Points    | Scales damage, healing, and barriers | Increases with tier |
| Maximum Health      | Health    | Maximum survivable Health            | Increases with tier |
| Health Regeneration | HP/5s     | Health restored every five seconds   | Increases with tier |

These attributes are stored and displayed directly. Their raw amounts use the
open-ended equipment tier scale.

### Opposed tier-scaling ratings

| Attribute  | Item unit     | Character-sheet result    | Tier behavior       |
| ---------- | ------------- | ------------------------- | ------------------- |
| Armor      | Rating points | Physical Damage Reduction | Increases with tier |
| Resistance | Rating points | Magical Damage Reduction  | Increases with tier |

Armor and Resistance remain ratings because they oppose tier-scaled incoming
pressure and must continue growing without printing impossible additive damage-
reduction percentages on individual items.

Equipment and crafting labels use `Armor Rating` and `Resistance Rating`. The
explicit suffix distinguishes these raw, additive item values from the effective
damage-reduction percentages shown on the character sheet.

### Direct percentage attributes

| Attribute                | Item unit         | Initial cap | Exact meaning                                                 |
| ------------------------ | ----------------- | ----------: | ------------------------------------------------------------- |
| Critical Chance          | Percentage points |        100% | Chance for a crit-eligible action to critically strike        |
| Critical Damage          | Percentage points |       +500% | Bonus output added to a critical strike                       |
| Cooldown Reduction       | Percentage points |         40% | Percentage removed from an active ability's base cooldown     |
| Attack Speed             | Percentage points |       +300% | Increase to basic-attack rate, preserving the 4x rate ceiling |
| Dodge Chance             | Percentage points |         40% | Chance to avoid a dodgeable attack                            |
| Block Chance             | Percentage points |         60% | Chance to block a blockable attack                            |
| Damage Reduction         | Percentage points |         40% | General reduction after typed defense and Block               |
| Healing Power            | Percentage points |       +300% | Increase to eligible Health restoration                       |
| Life Steal               | Percentage points |        100% | Delivered damage restored as Health                           |
| Status Resistance        | Percentage points |         80% | Reduction to ordinary harmful-status duration                 |
| Crowd Control Resistance | Percentage points |         80% | Reduction to crowd-control duration                           |
| Armor Penetration        | Percentage points |         60% | Percentage of target Armor ignored                            |
| Magic Penetration        | Percentage points |         60% | Percentage of target Resistance ignored                       |

The caps are activation candidates and must pass the combat analyzer before the
new stat model is enabled. They mostly preserve the current effective ceilings;
Cooldown Reduction changes to a direct 40% cap.

## Percentage stacking

Direct equipment percentages stack additively as percentage points and clamp once
at the final character cap:

```text
Uncapped Total = Base Value + Equipment + Essences + Temporary Modifiers
Effective Total = clamp(Uncapped Total, Minimum, Cap)
```

For example:

```text
Base Critical Chance        5%
Weapon                     10%
Armor                       6%
Essence                     4%
Final Critical Chance      25%
```

The item values are universally true. If a build exceeds a cap, the item does not
change; the personal comparison view may indicate how much of the value is
currently over the cap.

Temporary combat modifiers use the same final cap unless an authored mechanic is
explicitly allowed to exceed it.

## Critical Damage presentation

Critical Damage on an item is the bonus percentage contributed by that item:

```text
Critical Damage +20%
```

The character sheet displays the complete critical multiplier. If the character
has a built-in 50% critical bonus and equipment provides another 20%:

```text
Critical Damage Bonus     +70%
Critical Hit Damage        170%
```

Combat continues to use:

```text
Critical Multiplier = 1 + Critical Damage Bonus / 100
```

This distinction must be consistent in recipes, item cards, tooltips, combat
attributes, and ability explanations.

## Direct Cooldown Reduction

Cooldown Reduction becomes a direct capped percentage:

```text
Final Cooldown = Base Cooldown * (1 - Cooldown Reduction / 100)
```

The combat engine continues to round the resulting duration to whole ticks and
enforces a minimum of one tick.

Example:

```text
Base cooldown             10.0 seconds
Cooldown Reduction        20%
Final cooldown             8.0 seconds
```

The previously documented equipment-rating-to-cooldown-rate conversion is
superseded by this decision. The rate-based documentation, formulas, catalog
definitions, analyzers, and tests must be updated when this model is implemented.

## Percentage penetration against ratings

Armor Penetration and Magic Penetration become genuine percentages of the
target's corresponding rating:

```text
Effective Armor = Armor * (1 - Armor Penetration / 100)
Effective Resistance = Resistance * (1 - Magic Penetration / 100)
```

The reduced rating then enters the ordinary diminishing-return mitigation curve.

Example:

```text
Target Armor             200
Armor Penetration         25%
Effective Armor          150
```

Corrosion and other rating reductions must declare their order. The recommended
order is:

1. apply target-side Armor or Resistance modifiers;
2. apply Corrosion and comparable target-side percentage reductions;
3. apply attacker penetration percentage;
4. convert the remaining rating into mitigation;
5. apply Block and general Damage Reduction in their existing stages.

## Tier-normalized equipment budget

Recipe weights continue to express the same role allocation at every tier. The
system first allocates one normalized budget and then materializes the result
according to the attribute's unit class.

```text
Normalized Allocation = Normalized Item Budget * Recipe Weight
```

Materialization then differs:

```text
Flat or Rating Amount = Purchased Normalized Amount * Tier Scale
Direct Percentage = Purchased Normalized Percentage Amount
```

This does not change recipe weights by tier. A 70% Power / 30% Critical Chance
weapon remains a 70/30 design at every region:

- the Power amount grows with tier;
- the Critical Chance percentage remains stable for an equivalent roll;
- the same Critical Chance becomes more valuable in absolute damage because it
  multiplies the weapon's greater Power output.

Quality, rarity, mastery, Blueprint budgets, and tempering may improve direct
percentages, but equipment tier does not multiply their printed values.

The budget catalog therefore needs three explicit scaling kinds instead of the
current flat-versus-rating split:

```text
TierScaledFlat
TierScaledOpposedRating
DirectPercentage
```

No combat-facing code should infer scaling behavior from an attribute's name or
display suffix.

## Tier-anchor rule

Every base recipe must satisfy all of the following:

1. At least one initial attribute is a tier anchor.
2. At least 25% of the recipe's normalized initial budget is assigned to tier
   anchors.
3. The anchor remains present in every compatible Blueprint result unless another
   tier anchor replaces it.
4. Tempering cannot remove or reduce the item's existing anchor.
5. The anchor amount strictly increases from Tier N to Tier N+1 for an equivalent
   recipe, design, rarity, quality, mastery state, and roll percentile.
6. Quantization cannot erase the increase at any supported tier checkpoint.

The 25% minimum is the content-validation floor. The approved armor recipes all
exceed it, but it remains useful for weapons, jewelry, offhands, and future content.
It is large enough for the tier change to remain visible rather than existing as a
token one-point technicality.

## Base-crafted tier dominance

In addition to equivalent-roll monotonicity, an ordinary freshly crafted item
must not linger behind the previous tier because of overlapping base roll ranges.

For the same base recipe and design, before tempering:

```text
Minimum Tier N+1 Anchor > Maximum Tier N Anchor
```

The comparison occurs after persistence quantization. The minimum difference is
one visible display quantum:

- one point for whole-number attributes;
- `0.01` for two-decimal attributes.

If the tier curve and quality range fail this condition, content activation fails.
The correction should normally narrow the base quality spread rather than add a
hidden per-recipe tier multiplier.

This dominance guarantee applies to equivalent base recipe families and the
ordinary untempered crafting range. It does not promise that every untempered
Common Tier N+1 item beats all possible lower-tier Legendary, Masterpiece,
Blueprint, or fully tempered items. Those systems intentionally create exceptional
equipment and must retain temporary value.

The intended lifecycle is:

- a normal next-tier craft replaces a normal previous-tier craft;
- an exceptionally developed previous-tier item may remain competitive briefly;
- continued tempering and crafting at the new tier ultimately provide a decisive
  upgrade;
- no ordinary item remains best for several later tiers solely because its
  percentage attributes stopped changing.

## Outcome-level upgrade gate

Raw anchor growth is necessary but not sufficient. The canonical analyzer must
also verify role-relevant outcomes.

For each base recipe at representative checkpoints, equip a minimum-roll Tier N+1
item in place of a maximum-roll ordinary Tier N version while holding the rest of
the build constant. At least one declared role outcome must improve:

- offensive equipment: sustained damage or reference TTK;
- defensive equipment: raw TTD or effective Health;
- sustain equipment: effective TTD or restored Health over the reference window;
- hybrid equipment: its declared primary role outcome.

The replacement must not make every relevant outcome worse. A percentage
secondary may roll lower, but the tier anchor must produce a measurable net role
upgrade under the ordinary tier-dominance comparison.

These gates supplement the existing complete-build overgear targets. They test
individual recipe progression, while the canonical TTK/TTD analyzer tests the
whole equipment ecosystem.

## Current recipe audit

### Armor

The approved profiles are already authored for Head, Chest, and Legs recipes:

| Family | Approved attributes                                                      | Tier-anchor share | Hybrid-unit work remaining                         |
| ------ | ------------------------------------------------------------------------ | ----------------: | -------------------------------------------------- |
| Heavy  | Armor 35%, Max Health 35%, Resistance 30%                               |              100% | Preserve all three as tier-scaled values           |
| Medium | Armor 25%, Max Health 25%, Critical Chance 25%, Critical Damage 25%      |               50% | Convert both critical attributes to direct %       |
| Light  | Max Health 25%, Health Regen 25%, Dodge 25%, Attack Speed 25%            |               50% | Convert Dodge and Attack Speed to direct %         |
| Cloth  | Resistance 25%, Health Regen 25%, Healing Power 25%, Cooldown Reduction 25% |            50% | Convert Healing Power and Cooldown Reduction to direct % |

Each slot within a family uses the same profile. Heavy spends its complete budget
on open-ended defensive progression. Medium, Light, and Cloth each reserve half
their budget for visible tier progression and half for stable percentage identity.
No armor recipe contains Power.

### Jewelry

| Recipe | Attributes                | Tier-anchor share | Required change |
| ------ | ------------------------- | ----------------: | --------------- |
| Ring   | Power 100%                |              100% | None            |
| Amulet | Max Health 100%           |              100% | None            |
| Relic  | Health Regeneration 100%  |              100% | None            |

Each Jewelry recipe has one clear tier-scaled identity and remains a guaranteed
tier upgrade under the hybrid unit model. Every equipment item uses a 1.00 stat
budget except two-handed items, which use 2.00 so they retain the same total hand
funding as two one-handed items.

### Weapons

Every current weapon assigns 70% to Power and 30% to one secondary:

| Weapons                         | Percentage secondary |
| ------------------------------- | -------------------- |
| Dagger, Gauntlets, Spear        | Attack Speed         |
| Hand Axe, Battle Axe            | Critical Damage      |
| Shortsword, Greatsword, Longbow | Critical Chance      |
| Mace, Maul, Crossbow            | Armor Penetration    |
| Wand, Staff                     | Magic Penetration    |

Power is a 70% tier anchor on every weapon. All weapon secondaries become direct
percentages. An equivalent higher-tier weapon therefore has greater Power while
retaining a stable, understandable secondary percentage.

### Offhands

| Recipe      | Approved attributes                                 | Tier-anchor share | Required change                               |
| ----------- | --------------------------------------------------- | ----------------: | --------------------------------------------- |
| Towershield | Max Health 35%, Block 35%, Armor 30%                 |               65% | Convert Block to direct %                     |
| Spiritward  | Max Health 35%, Block 35%, Resistance 30%            |               65% | Convert Block to direct %                     |
| Grimoire    | Power 70%, Cooldown Reduction 30%                   |               70% | Keep Cooldown Reduction as direct %           |

The two defensive shields share the same Health and Block core, then specialize
into physical or magical defense. Every Offhand satisfies the 25% anchor rule.

## Recipe and item presentation

Recipes and item cards display their intrinsic values directly:

```text
Greatsword · T4

Power                    +84–102
Critical Chance            +8–11%
```

```text
Heavy Legplates · T4

Maximum Health          +420–510
Armor Rating             +138–166
Resistance Rating          +82–99
```

```text
Light Legwraps · T4

Maximum Health          +190–230
Dodge Chance                +6–8%
Attack Speed                +9–12%
```

Every player sees the same values in crafting, inventory, marketplace, guild
storage, and chat. `Possible Upgrades` uses the same canonical names and units.

The character sheet separately displays effective outcomes:

```text
Physical Damage Reduction     46.7%
Magical Damage Reduction      41.2%
Critical Chance               24.0%
Critical Hit Damage          175.0%
Cooldown Reduction            18.0%
```

Physical and Magical Damage Reduction show their underlying values immediately
below the percentage as `182.27 Armor Rating` and `115.13 Resistance Rating`.

An optional personal comparison may show before and after totals, but it never
replaces the values printed on the item.

## Combat formula changes

Implementation of this design requires the following formula changes:

- `AttributeCalculator` aggregates direct percentages without converting them
  through a rating curve.
- Armor and Resistance remain tier-normalized ratings and continue using their
  diminishing-return mitigation formulas.
- Armor Penetration and Magic Penetration reduce the target's rating by a direct
  percentage rather than subtracting an opposing penetration rating.
- Critical Chance, Dodge, and Block roll directly against their capped totals.
- Critical Damage uses the direct bonus percentage.
- Cooldown Reduction uses the direct capped cooldown formula and no longer derives
  from cooldown rate rating.
- Attack Speed uses the direct percentage in the existing attack-rate calculation.
- Healing Power and Life Steal use their direct percentages.
- Status Resistance and Crowd Control Resistance reduce eligible durations by
  their direct capped percentages.
- Damage Reduction remains a direct percentage at its existing resolution stage.

Caps are applied to complete character totals, not separately to each item.

## Crafting and tempering changes

The crafting system must:

- allocate normalized recipe weights before tier materialization;
- multiply only flat and opposed-rating results by the tier scale;
- persist direct percentage results without tier multiplication;
- preserve the current number of decimal places declared by each attribute;
- validate the 25% anchor rule for base recipes and Blueprint compositions;
- validate adjacent-tier dominance after quantization;
- prevent tempering from purchasing values beyond a character-independent per-item
  safety bound where such a bound is required;
- report direct percentage ranges in recipe and Blueprint previews;
- remove `Rating` from every direct-percentage equipment label and tooltip.

Tempering percentage gains remain intrinsically stable. Tier-scaled anchor gains
increase with the item's tier. Potential cost and marginal-value calibration must
be rerun so neither unit class dominates tempering choices.

## Stat-model versioning and existing equipment

Existing v16 equipment stores most percentage-like attributes as raw progression-
normalized ratings. Those values cannot be reinterpreted directly as percentages.
For example, a stored Critical Chance rating of 50 must not become +50% without an
explicit conversion.

Implementation therefore requires a new equipment stat-model version. The
recommended next version is v17.

Two migration policies are possible:

### Deterministic conversion

For every v16 direct-percentage candidate:

1. recover its normalized v16 budget value from the stored rating and item tier;
2. preserve the item's budget share;
3. purchase the corresponding v17 direct percentage at the new constant normalized
   price;
4. quantize using the v17 percentage precision;
5. preserve recipe, Blueprint, rarity, quality, potential, and tempering metadata;
6. mark the instance as v17.

This preserves economic investment rather than pretending that the old raw rating
was already a percentage. It requires full balance validation because aggregating
direct item percentages differs from aggregating ratings before one shared curve.

### Legacy separation

Alternatively, v16 remains a supported legacy model and receives the existing
visible legacy treatment until explicitly converted through crafting or tempering.
New crafts use v17 exclusively.

The migration decision must be made before implementation. No database migration
should silently reinterpret stored numeric values in place.

## Validation and analyzer gates

### Unit and cap gates

- Every direct percentage prints the same value in recipe, item, market, vault, and
  chat DTOs.
- Every direct percentage bypasses rating conversion.
- Every direct percentage respects its complete-character cap.
- Armor and Resistance remain the only equipment ratings.
- Penetration applies in the documented order and never produces negative defense.
- Cooldown Reduction never exceeds its cap and never reduces an authored cooldown
  below one tick.
- Critical Damage item bonus and character total multiplier are displayed
  consistently.

### Tier-progression gates

- Every recipe has at least 25% tier-anchor allocation.
- Every Medium, Light, and Cloth recipe allocates exactly 50% to tier anchors;
  every Heavy recipe allocates 100%.
- Equivalent-roll anchors strictly increase at every adjacent tier.
- Minimum Tier N+1 base anchor exceeds maximum Tier N base anchor after
  quantization.
- The rule passes at least Tiers 1, 2, 5, 10, 20, 50, and 100, with adjacent-tier
  checks performed across the complete supported authored range.
- No attribute increase is lost to display precision.
- A next-tier ordinary replacement improves at least one declared role outcome.
- Full-build overgear TTK and TTD remain within the existing activation bands.

### Marginal-value gates

- A fixed percentage remains valuable at every tier because its underlying damage,
  healing, cooldown, or pressure basis scales.
- No percentage attribute becomes universally best across all scenarios.
- No rating attribute becomes mandatory outside its intended defensive role.
- Percentage caps do not make an intended recipe allocation routinely wasted in a
  canonical build.
- Direct percentage secondary allocations retain the intended 5-12% scenario
  advantage over an irrelevant equal-budget secondary.

## Documentation changes

Implementation requires coordinated updates to:

- the open-ended equipment scaling plan;
- the rate-based cooldown documentation;
- the attribute formula reference;
- Armor, Resistance, penetration, Critical Chance, Critical Damage, Attack Speed,
  sustain, and resistance tooltips;
- crafting and tempering documentation;
- character-sheet descriptions;
- equipment comparison wording;
- the combat lexicon where these values are used by abilities or conditions.

Documentation must distinguish three concepts consistently:

```text
Item percentage      A universal direct percentage stored on an item
Item rating          Armor or Resistance points stored on an item
Effective outcome    The complete character result after aggregation and caps
```

## Implementation sequence

1. Add explicit attribute unit/scaling classifications and v17 catalog rules.
2. Implement direct percentage aggregation and the revised combat formulas.
3. Add cap enforcement and over-cap comparison telemetry.
4. Revise the recipe allocator to materialize normalized budgets by unit class.
5. Change Light Armor from Critical Chance to Maximum Health.
6. Add tier-anchor and adjacent-tier dominance content validators.
7. Update crafting, item, marketplace, vault, chat, and character presentation.
8. Implement the selected v16-to-v17 equipment policy.
9. Recalibrate costs, tempering, marginal values, TTK/TTD, and overgear gates.
10. Run the complete activation analyzer before enabling v17 crafting.

## Production readiness

The hybrid model is not production-ready until:

- all current recipes pass the tier-anchor rule;
- the new Light Armor profile is approved;
- percentage caps are calibrated;
- direct percentage costs pass marginal-value analysis;
- adjacent-tier dominance is proven across the authored and projected tier range;
- v16 compatibility behavior is selected and tested;
- combat, crafting, tempering, comparison, marketplace, chat-link, simulation, and
  checkpoint tests pass;
- the canonical TTK/TTD and overgear gates pass under one declared v17 model;
- no shared or production database is modified automatically.
