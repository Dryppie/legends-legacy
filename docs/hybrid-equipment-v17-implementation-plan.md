# Hybrid Equipment v17 Implementation Plan

Status: Approved recipe composition implemented; remaining system conversion not
yet implemented.

Companion design: `docs/hybrid-equipment-attribute-units-design.md`

## Purpose

This document is the execution plan for completing the hybrid equipment model
after the armor recipe composition change. It deliberately follows the same
sequence used for equipment v16: establish the canonical rules, change crafting
and migration behavior, then update combat and presentation.

The target is equipment stat model **v17**.

## Completed prerequisite: armor recipe composition

All Head, Chest, and Legs recipes now use the following family profiles. Their
base-roll allocations, tempering weights, and tempering maximum budget shares are
kept in sync.

| Family | Attribute | Budget |
| ------ | --------- | -----: |
| Heavy | Armor | 35% |
| | Maximum Health | 35% |
| | Resistance | 30% |
| Medium | Armor | 25% |
| | Maximum Health | 25% |
| | Critical Chance | 25% |
| | Critical Damage | 25% |
| Light | Maximum Health | 25% |
| | Health Regeneration | 25% |
| | Dodge Chance | 25% |
| | Attack Speed | 25% |
| Cloth | Resistance | 25% |
| | Health Regeneration | 25% |
| | Healing Power | 25% |
| | Cooldown Reduction | 25% |

This recipe change does **not** make the affected percentage attributes direct
percentages yet. Under v16, they continue to materialize as ratings until the
remaining phases are implemented and v17 is activated.

### Defensive offhand profiles

The two defensive offhands use a shared shield core:

| Offhand | Maximum Health | Block Chance | Typed defense |
| ------- | -------------: | -----------: | ------------- |
| Towershield | 35% | 35% | Armor 30% |
| Spiritward | 35% | 35% | Resistance 30% |

Their base rolls and tempering profiles use the same allocations. This gives both
shields a 65% tier-anchor share while Block becomes a stable direct percentage in
v17.

## Canonical v17 unit contract

### Tier-scaled flat values

- Power
- Maximum Health
- Health Regeneration

### Tier-scaled opposed ratings

- Armor
- Resistance

Only Armor and Resistance remain equipment ratings. Their item labels are
`Armor` and `Resistance`; tooltips and the character sheet explain the effective
damage reduction derived from the combined rating.

### Direct percentage-point values

- Critical Chance
- Critical Damage
- Cooldown Reduction
- Attack Speed
- Dodge Chance
- Block Chance
- Damage Reduction
- Healing Power
- Life Steal
- Status Resistance
- Crowd Control Resistance
- Armor Penetration
- Magic Penetration

A direct percentage printed on a recipe or item is intrinsic to that item and is
identical in inventory, equipment, marketplace, vault, and chat-link contexts.
Viewer level or progression must never alter the displayed item value.

## Materialization model

The allocator must distinguish normalized budget from tier scale.

```text
Normalized Budget = Tier Budget / Tier Scale

Flat or opposed-rating points =
    Normalized Budget * Allocation / Cost Per Point * Tier Scale

Direct percentage points =
    Normalized Budget * Allocation / Cost Per Percentage Point
```

Slot weight, quality, rarity, mastery, Blueprint bonus, and permitted tempering
investment still modify the appropriate normalized budget. Equipment tier alone
must not increase a direct percentage magnitude.

Consequences:

- an equivalent Tier 10 and Tier 11 Medium piece may show the same intrinsic
  Critical Chance range;
- the Tier 11 piece must show larger Armor and Maximum Health ranges;
- a higher quality or stronger Blueprint may still improve a direct percentage;
- every recipe must retain enough tier-anchor budget to make the next tier a real
  upgrade.

## Phase 1: encode the v17 stat contract

### Domain catalog

Change `EquipmentStatBudgetCatalog` to:

1. set `BalanceVersion` to 17;
2. retain version 16 as the immediately previous model rather than treating 15 as
   the only legacy source;
3. add an explicit scaling kind such as `DirectPercentage`;
4. classify only Armor and Resistance as
   `ProgressionNormalizedRating`;
5. classify Power, Maximum Health, and Health Regeneration as `Flat`;
6. classify every other equipment-eligible attribute as `DirectPercentage`;
7. replace `IsRating` assumptions with scaling-kind queries where callers need to
   distinguish all three classes;
8. prohibit rating conversion for direct percentages.

The scaling kind must be explicit metadata. No caller may infer it from an
attribute name, `%` suffix, cap, or display unit.

### Attribute catalog

Update `AttributeCatalog` so equipment metadata agrees with the contract:

- Armor and Resistance use rating equipment units and display as `Armor Rating`
  and `Resistance Rating` in equipment and crafting contexts;
- direct percentages use their ordinary percentage names and `%` suffix;
- descriptions state exactly what each percentage does;
- Critical Damage distinguishes bonus percentage from the complete critical-hit
  multiplier;
- Cooldown Reduction documents the direct cooldown formula;
- penetration describes a percentage of target rating ignored, not percentage
  points removed from mitigation.

### Validation

Extend crafting content validation to reject:

- an equipment-eligible attribute without a budget rule;
- a mismatch between catalog equipment unit and budget scaling kind;
- a base recipe whose tier-anchor share is below 25%;
- a tempering stat missing from its owning base recipe unless `CanIntroduce` is an
  intentional Blueprint behavior;
- base profile weights that do not sum to one;
- tempering weights or maximum shares that contradict the approved base profile.

For the approved armor profiles, validation must assert:

- Heavy has a 100% tier-anchor share;
- Medium, Light, and Cloth each have a 50% tier-anchor share;
- every family has exactly the approved attributes in all three armor slots.

## Phase 2: crafting, previews, and tempering

### Base rolls and ranges

Refactor `ItemStatRollService` and the constrained allocator so the same code path
calculates actual rolls and preview ranges under the materialization model above.
Do not introduce a preview-only conversion.

Required invariants:

- direct percentage ranges are stable across equal designs at different tiers;
- tier-scaled ranges grow with the open-ended tier curve;
- quality and random roll percentile behave identically in preview and crafting;
- quantization cannot erase an adjacent-tier anchor increase;
- recipe plus Blueprint composition remains budget-conserving.

### Tempering

Tempering must purchase normalized value using the target attribute's v17 cost.

- percentage gains remain direct percentages and are not multiplied by tier;
- flat and opposed-rating gains use the item tier scale;
- maximum budget share remains character-independent;
- a capped character may waste part of a percentage gain, but the item itself is
  not rewritten for that character;
- UI previews display the exact intrinsic gain before the action is confirmed.

The current Primary/Secondary categories can continue controlling tempering gain
shape. For armor, the tier anchors are Primary and percentage identity attributes
are Secondary, except Heavy Resistance remains the third defensive Secondary.
This classification does not alter the base recipe allocations.

### Tier-upgrade gates

For equivalent recipe, Blueprint, quality, mastery, and roll percentile:

```text
Anchor(Tier N + 1) > Anchor(Tier N)
```

For the base roll envelope:

```text
Minimum ordinary anchor at Tier N + 1
    > Maximum ordinary anchor at Tier N
```

If the second rule cannot pass with the current 95%-105% roll variance, tune the
tier curve or variance deliberately. Do not make percentage stats tier-scaled to
hide an anchor failure.

Run adjacent-tier checks through the authored range and explicit checkpoints at
Tiers 1, 2, 5, 10, 20, 50, and 100.

## Phase 3: deterministic v16-to-v17 migration

The existing `StatModelVersion` column is sufficient unless implementation
discovers another persistence requirement. A new schema column should not be
added merely for the version bump.

### Conversion rule

For every v16 direct-percentage candidate stored as raw rating:

```text
v17 percentage = v16 effective value(raw rating, item tier)
```

Then quantize using the v17 percentage precision and set `StatModelVersion = 17`.
This preserves what the item actually provided at its own intended tier at the
moment of conversion. It avoids granting a windfall based on the current viewer
and produces one canonical persisted value thereafter.

Armor and Resistance keep their raw v16 rating amounts. Power, Maximum Health,
and Health Regeneration keep their flat amounts.

### Version chain

Migration must be staged and idempotent:

1. v15 equipment follows the existing v15-to-v16 represented-budget conversion;
2. its existing `Broken ` crafted-name prefix remains intact;
3. the resulting v16 values then pass through the v16-to-v17 unit conversion;
4. native v16 items execute only step 3;
5. native v17 items are unchanged.

Never run the v16 formula directly against an unconverted v15 item.

### Complete equipment coverage

The migration entry point must cover every persisted or embedded equipment
instance, including:

- character inventory;
- equipped items;
- marketplace listings and escrow;
- guild storage or vaults;
- mail, trade, or reward containers if they can hold instances;
- crafting and tempering queues;
- dungeon/run snapshots and other durable snapshots;
- administrative item retrieval paths.

Prefer a centralized materialization/save interceptor plus a bounded database
backfill. The backfill supplies production completeness; lazy migration protects
instances created by old fixtures, imports, or temporarily missed stores.

Migration tests must prove idempotency, version chaining, unchanged Armor and
Resistance ratings, preserved `Broken ` prefixes, and coverage of every storage
location discovered by the persistence audit.

## Phase 4: aggregation and combat formulas

### Aggregation

`AttributeCalculator` must aggregate persisted v17 percentages directly. It must
not send them through an equipment rating curve.

Apply caps once to the complete character total:

```text
Total = Base + Equipment + Essences + Temporary Modifiers
Effective = clamp(Total, Minimum, Cap)
```

Per-item clamping is prohibited because it makes identical total investment
behave differently depending on how it is distributed between slots.

### Armor and Resistance

Continue summing their raw ratings and translating the total through the existing
progression-normalized diminishing-return curve. The mitigation cap remains a
character result, not an additive item percentage.

### Critical stats

```text
Critical Multiplier = 1 + Total Critical Damage Bonus / 100
```

Critical Chance rolls directly against its capped percentage total. Item cards
show contributed Critical Damage; the character sheet may additionally show the
complete multiplier.

### Direct cooldown reduction

Replace the v16 cooldown-rate conversion with:

```text
Final Cooldown = Base Cooldown * (1 - Cooldown Reduction / 100)
```

Clamp the complete Cooldown Reduction total to the approved 40% candidate cap,
round through the canonical combat-tick rule, and retain a minimum of one tick.
Remove or retire cooldown rating/rate constants and compatibility methods once no
v16 migration path needs them.

### Penetration

Armor Penetration and Magic Penetration become percentages of target rating:

```text
Effective Armor = max(0, Target Armor * (1 - Armor Penetration / 100))
Effective Resistance = max(0, Target Resistance * (1 - Magic Penetration / 100))
```

The documented resolution order is:

1. target-side rating modifiers;
2. Corrosion and similar target-side percentage reductions;
3. attacker's percentage penetration;
4. conversion of remaining rating to mitigation.

### Other direct percentages

Update Dodge, Block, Damage Reduction, Healing Power, Life Steal, Status
Resistance, Crowd Control Resistance, and Attack Speed to consume the direct
aggregated value. Preserve their established ordering and caps unless a separately
documented design change is approved.

Increment `PowerRatingAlgorithm.CombatRulesVersion` when combat interpretation
changes.

## Phase 5: API and player-facing presentation

### Canonical item values

Recipe previews, item DTOs, marketplace listings, guild storage, and chat links
must serialize the same persisted values and units. No endpoint may format a
percentage by converting it against the requesting character.

Keep these concepts separate:

- `Attributes`: canonical intrinsic item values;
- `EffectiveAttributes` or comparison projections: optional character-specific
  results;
- character sheet: complete totals and effective outcomes.

Character-specific comparison data must be labeled as a comparison and must not
replace the canonical item lines.

### Angular presentation

Update the shared attribute definition and formatting pipes so:

- only Armor and Resistance take the equipment-rating path;
- their visible item labels are `Armor Rating` and `Resistance Rating`;
- direct percentage recipe ranges and item values include `%`;
- Critical Damage tooltips distinguish item bonus from total multiplier;
- recipe, inventory, equipment, market, vault, chat, and tempering components use
  the shared formatter rather than local suffix logic.

The admin dashboard must use the same unit metadata or an equivalent generated
contract so authored content is not displayed with obsolete rating terminology.

## Required verification matrix

### Domain and catalog tests

- exact scaling-kind classification for every equipment attribute;
- Armor and Resistance are the only ratings;
- direct percentage conversion helpers reject or bypass rating conversion;
- fixed and contextual caps remain canonical;
- all equipment attributes have display and budget metadata.

### Recipe and crafting tests

- exact approved armor profiles in all twelve recipes;
- exact matching tempering attributes, weights, and maximum shares;
- profiles sum to 100%;
- tier-anchor validation passes for every recipe and Blueprint composition;
- direct percentage ranges are tier-stable;
- flat/rating ranges increase through Tier 100;
- preview and deterministic roll use the same allocator;
- adjacent-tier dominance passes after quantization.

### Migration tests

- v15 -> v16 -> v17 chaining;
- v16 -> v17 effective-value preservation;
- v17 no-op and repeated-run idempotency;
- `Broken ` prefix preservation for legacy v15 equipment;
- inventory, equipped, market, storage, queue, and snapshot coverage;
- no duplicate or lost modifiers after conversion.

### Combat tests

- direct Critical Chance and Critical Damage;
- direct Cooldown Reduction formula, cap, tick rounding, and one-tick floor;
- percentage penetration order against Armor and Resistance ratings;
- complete-character cap application;
- Dodge, Block, Damage Reduction, Healing Power, Life Steal, status resistance,
  crowd-control resistance, and Attack Speed;
- mixed v16/v17 fixture handling during migration-only compatibility testing.

### API and frontend tests

- identical canonical values across crafting, inventory, equipment, market, vault,
  and chat DTOs;
- only Armor and Resistance are formatted as rating units internally;
- no visible `Critical Damage Rating`, `Cooldown Rating`, or equivalent obsolete
  label remains;
- Armor and Resistance item labels explicitly include `Rating`;
- their character-sheet secondary lines use `<value> Armor Rating` and
  `<value> Resistance Rating` without an additional `from equipment` phrase;
- comparison projections cannot replace canonical item values.

## Rollout and production readiness

Before production activation:

1. complete every phase above on one branch;
2. run the complete backend and Angular test suites;
3. generate and review migration SQL without applying it to shared or production
   databases;
4. run a production-like database copy audit that counts equipment by storage
   location and stat model version;
5. execute the migration twice and prove the second pass changes zero rows;
6. smoke-test crafting, tempering, equip, market listing, market purchase, storage,
   and chat linking with migrated and newly crafted items;
7. deploy API and frontend together so v17 values are never interpreted by a v16
    client contract.

Rollback must restore both code and data semantics. Because v17 persists direct
percentages where v16 persisted ratings, a simple application rollback is unsafe
after the backfill. Prepare either a tested reverse conversion or a database
restore point before activation.

## Definition of done

Equipment v17 is complete only when:

- the approved armor profiles remain exact;
- every equipment attribute follows its canonical unit contract;
- all persisted equipment is v17 or deterministically migratable to v17;
- every item displays the same intrinsic values to every viewer;
- higher tiers visibly improve every recipe through its tier anchors;
- all relevant gameplay systems consume the new units correctly;
- the complete correctness verification matrix passes.
