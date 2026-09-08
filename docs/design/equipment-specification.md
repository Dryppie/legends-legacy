# Equipment system contract

Updated: 8 September 2026.

This document defines the supported equipment system after the removal of crafting, gathering, tempering, salvaging, and the equipment Forge.

## Equipment identity

Every equipment instance is a frozen, server-authored item. Its descriptor contains:

- definition and item-base identity;
- equipment type, rarity, quality, tier, and rank;
- the frozen attribute-roll multiplier;
- native and active style identifiers;
- weapon behavior where applicable;
- evaluated stats and equipment-set identity;
- provenance and ownership.

Reinforcement can increase rank. Consumable blueprints can apply or replace a compatible variant. Both operations require an exact server preview and transactional payment. Quality, rarity, tier and attribute rolls are preserved.

## Armor archetypes and mitigation

Head, Chest, and Legs each offer Heavy, Medium, and Light armor. Their base stat
budget weights are identical across those slots:

| Armor | Power | Max Health | Armor rating | Resistance rating |
| --- | ---: | ---: | ---: | ---: |
| Heavy | 0% | 40% | 30% | 30% |
| Medium | 35% | 25% | 20% | 20% |
| Light | 70% | 10% | 10% | 10% |

Only these four attributes occur in base armor profiles. Heavy omits Power.
Variant stat profiles and set bonuses remain additional contributions and may
grant other attributes. Medium is the exact budget midpoint of Heavy and Light.

Cloth bases, archetypes, named equipment, and variant compatibility entries are
removed from active content. There are nine armor bases and 28 plain equipment
options per released tier. Canonical Balanced builds use Medium; former Cloth
reference builds use Light.

Armor and Resistance share one rating conversion and budget cost. For either
attribute, normalize the combined rating by the character's progression-tier
scale, then calculate `reductionPercent = 80 * normalizedRating / (55 + normalizedRating)`.
Both approach an 80% reduction cap and give 40% reduction at 55 normalized rating.
Both cost 0.9 budget per raw rating point, so equal base budget weights buy equal
physical and magical mitigation. Penetration continues to act in rating space.

This update changes active content and runtime rating conversion; it does not
rewrite existing frozen equipment descriptors or delete player-owned instances.
Existing rating values use the shared mitigation curve at runtime. No database
migration or environment setting is included. Deploy the updated API content and
shared Domain assembly together; converting existing Cloth to Light and
rebalancing old item rolls requires a separate data migration.

## Acquisition

Supported sources are starter grants, random area drops, random dungeon drops, and explicitly authored rewards from other current systems. All grants must reference a valid equipment definition and must be evaluated by the canonical equipment evaluator before persistence.

Equipment rarity and base archetype are rolled before variant identity. Areas award rank 0 equipment with a 15% chance of a compatible regional variant. Dungeons award rank 1 equipment with a 50% chance of a compatible variant from that dungeon family's blueprint pool. These are conditional on an equipment drop; existing equipment drop chances and rarity/quality odds remain separate. Both sources award unbound equipment. Catalog growth cannot change the explicit base-versus-variant roll.

After rarity, area and dungeon equipment drops select a category: 40% Weapons,
35% Armor (Head, Chest, Legs), and 25% Jewelry (Ring, Necklace, Relic). Weapons then
select 60% OneHanded/OffHand or 40% TwoHanded, followed by a uniform item selection
within that group. This gives overall shares of 24% OneHanded/OffHand, 16%
TwoHanded, 35% Armor, and 25% Jewelry, independent of the number of designs in
each group. Off-hand shields, wards, and grimoires share the one-handed pool.

Both regional pools author these probabilities under `selectionWeights` in
`equipment-ordinary.v1.json`; category weights and handedness weights must each
be finite, non-negative, and total one. Dungeon treasury equipment uses the same
selector after filtering to its compatible styles. If a restricted source lacks
a category or handedness, its weight is redistributed proportionally among the
eligible groups at that stage; a missing handedness does not reduce the overall
weapon allocation. Ship the API content and backend together. No database
migration or environment setting is required, and frozen items and saved pending
rewards retain their descriptors.

Equipment rarity has seven values: Common, Uncommon, Rare, Epic, Unique, Legendary, and Legacy. It multiplies the base stat budget independently from rank. The multipliers are 1.0, 1.1, 1.3, 1.6, 2.0, 2.5, and 3.0 respectively; each rank still adds four percent of the rarity-adjusted budget.

Equipment quality has five values: Crude, Standard, Fine, Exceptional, and Masterpiece. Its stat multipliers are 0.90, 1.00, 1.12, 1.26, and 1.42. Area and dungeon drops use the former Mastery 100 distribution: 0% Crude, 35% Standard, 45% Fine, 16% Exceptional, and 4% Masterpiece. Quality is independent of rarity and rank.

Every randomly dropped equipment item also receives one frozen attribute-budget roll from 0.95 through 1.05. The same multiplier is applied to the item's whole stat budget before constrained attribute allocation, preserving the authored stat profile and hard caps. Quality and the roll are persisted with the item and are never rerolled by binding, transfer, or Reinforcement. Authored starter and administrative grants default to Standard and 1.00 unless explicitly specified.

Areas drop Common / Uncommon / Rare equipment at conditional rarity weights of 85% / 12% / 3%; higher rarities have zero weight. The equipment chance is 1/864 per victorious encounter (approximately 0.1157407%). At the configured 10-second combat cadence, 24 hours of continuous victories provides 8,640 rolls and an expected 10 pieces: 8.5 Common, 1.2 Uncommon, and 0.3 Rare. Drops are random; losses and time spent outside area combat reduce this average.

Dungeons have a 50% base equipment chance per completion, plus 5 percentage points per shared dungeon mastery level (100% at mastery 10). The roll uses mastery captured at run start. Mastery XP and levels are shared across Novice, Veteran, and Champion; mastery 10 requires 75,000 cumulative XP. See [shared dungeon mastery](../../LL/docs/shared-dungeon-mastery.md) for the curve and existing-progress migration. When equipment drops, the dungeon's difficulty selects its rarity distribution in every released region:

| Difficulty | 84% | 14% | 2% |
| --- | --- | --- | --- |
| Novice (Grade I) | Uncommon | Rare | Epic |
| Veteran (Grade II) | Rare | Epic | Unique |
| Champion (Grade III) | Epic | Unique | Legendary |

These are conditional rarity probabilities, not additional equipment drop rolls. Common and Legacy are excluded from dungeon equipment rolls. Tier, rank, quality, attribute-roll distribution, and variant odds are unchanged. Already awarded equipment and saved pending rewards retain their existing rarity.

The regional `equipment-ordinary.v1.json` content defines the three distributions under `dungeonEquipment.rarities.novice`, `.veteran`, and `.champion`. All three must be present and valid. The area profiles define their rate and rarity weights under `areaEquipment`. Ship updated content with the API and frontend. These drop-rate, rarity, and reward-display changes need no new database migration or environment setting; shared mastery still requires the separate migration described above.

Every released combat area may independently drop a random dungeon Sigil for its region at 1/4,320 per victorious encounter. Sigil drops do not require selecting a family or unlocking its dungeon first.

The Soul Archive introductory quest awards an Arms Chest and one Fury blueprint. All five chest weapons accept Fury. Conversion is optional and its 100-Cinder tier-one cost is covered by the quest's existing Cinder reward.

## Variants and consumable blueprints

New equipment allocates its entire base budget first. A variant adds 15% of that budget using its own stat profile, respecting the remaining attribute caps and overflowing into base attributes when necessary. No base attribute is reduced to fund the variant. Set bonuses are additional power and must be included in balance reviews.

One family blueprint plus 100 Cinders per equipment tier applies that variant with guaranteed success. It works across tiers and rarities on explicitly compatible archetypes, including already-styled items. One variant is active at a time; replacement removes the previous variant's contribution and set identity, with no refund. Applying the current variant is rejected. Conversion preserves ownership restrictions and all reinforcement investment. Dropped and converted variants with equal rarity, tier, quality, roll and rank have equal stats.

Completed dungeons independently award one blueprint directly with a 25% chance. After three consecutive completions without one, the fourth guarantees a blueprint. Counters are per character and dungeon family, shared across that family's grades, and reset on award. Completion retries do not add progress or duplicate rewards. Failed or unfinished runs do not count. Dungeon Run Rewards lists each possible blueprint and its current chance, including the next-clear guarantee. Mastery increases equipment chance only.

| Dungeon | Direct blueprint drops | Chance per blueprint before guarantee |
| --- | --- | --- |
| Goblin Mines | Fury or Phoenix | 12.5% each |
| Forgotten Catacombs | Arcane or Endurance | 12.5% each |
| Tangled Cave | Execution only | 25% |
| Great Tree | Spirit only | 25% |

Two-blueprint pools choose uniformly when a blueprint drops; they never award both. Blueprint drop pools (`blueprintStyleIds`) are separate from equipment variant pools (`styleIds`). Later-region equipment can still roll earlier variants. Gravebound and Raidforged blueprint/style definitions and consumables are retired. Other existing consumable blueprints remain usable.

Blueprints are stackable and tradable. Conversion is performed from the equipment panel with a stat preview, cost, set replacement notice and explicit confirmation. Prices, source families, and probabilities are authored in `equipment-blueprints.v1.json`. The `DirectDungeonBlueprintDrops` migration converts old dungeon choice stacks and pending rewards, removes retired consumables, and refunds their unfilled Bazaar buy orders. Completed trade history is retained.

Frozen items predating additive variants retain their recorded stats and allocation mode when loaded, transferred or reinforced. Applying a different variant explicitly switches them to additive evaluation. Existing items are not silently rewritten.

## Ownership

Equipment can be bound personal, unbound personal, or guild-owned. Equip, transfer, marketplace, donation, loan, and return checks must use the frozen ownership state. A transfer changes ownership without rerolling rank, style, quality, the attribute roll, or stats.

## Removed operations and currencies

The retired Forge, permanent style learning, crafting, tempering, recovery, and Tempered Scrap remain removed. Reinforcement, dismantling for Reinforcement Parts, and consumable blueprint conversion are the current equipment operations. Blueprint choice protection does not provide guaranteed equipment rarity or equipment targeting.

Shared stat-allocation and offline simulation code may retain recipe-oriented internal inputs where a current tool still consumes them. Those helpers do not grant a player crafting or equipment-mutation capability.

## Content validation

Released content must fail loading when it references an unknown equipment definition, item base, style, pool, reward table, or set. Blank and duplicate identifiers are invalid. Authored rewards must not reference removed Forge resources or containers.

## Deferred design work

Further quest gear-selection rules require separate design decisions. Blueprint extraction, mastery, rarity upgrades, and variant upgrades are not part of this feature.

`AddEquipmentBlueprintProgress` adds the guarantee counter table; it must be applied through the normal release process before enabling this code. Blueprint items are supplied through the existing item-content seeding process. The migration is generated only, and no database changes or deployment are performed by this implementation.

See [implementation status](equipment-implementation-status.md) and [Forge removal](equipment-forge-removal.md).
