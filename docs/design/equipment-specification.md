# Equipment system contract

Updated: 5 September 2026.

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

## Acquisition

Supported sources are starter grants, random area drops, random dungeon drops, and explicitly authored rewards from other current systems. All grants must reference a valid equipment definition and must be evaluated by the canonical equipment evaluator before persistence.

Equipment rarity and base archetype are rolled before variant identity. Areas award rank 0 equipment with a 15% chance of a compatible regional variant. Dungeons award rank 1 equipment with a 50% chance of a compatible variant from that dungeon family's blueprint pool. These are conditional on an equipment drop; existing equipment drop chances and rarity/quality odds remain separate. Both sources award unbound equipment. Catalog growth cannot change the explicit base-versus-variant roll.

Equipment rarity has seven values: Common, Uncommon, Rare, Epic, Unique, Legendary, and Legacy. It multiplies the base stat budget independently from rank. The multipliers are 1.0, 1.1, 1.3, 1.6, 2.0, 2.5, and 3.0 respectively; each rank still adds four percent of the rarity-adjusted budget.

Equipment quality has five values: Crude, Standard, Fine, Exceptional, and Masterpiece. Its stat multipliers are 0.90, 1.00, 1.12, 1.26, and 1.42. Area and dungeon drops use the former Mastery 100 distribution: 0% Crude, 35% Standard, 45% Fine, 16% Exceptional, and 4% Masterpiece. Quality is independent of rarity and rank.

Every randomly dropped equipment item also receives one frozen attribute-budget roll from 0.95 through 1.05. The same multiplier is applied to the item's whole stat budget before constrained attribute allocation, preserving the authored stat profile and hard caps. Quality and the roll are persisted with the item and are never rerolled by binding, transfer, or Reinforcement. Authored starter and administrative grants default to Standard and 1.00 unless explicitly specified.

The current rarity weights are Common/Uncommon/Rare/Epic/Unique/Legendary/Legacy = 70%/20%/7%/2%/0.8%/0.18%/0.02% in areas and 40%/30%/18%/8%/3%/0.9%/0.1% in dungeons. These weights are data-driven regional content rather than hard-coded evaluator behavior.

Every released combat area may independently drop a random dungeon Sigil for its region at 1/4,320 per victorious encounter. Sigil drops do not require selecting a family or unlocking its dungeon first.

The Soul Archive introductory quest awards an Arms Chest and one Fury blueprint. All five chest weapons accept Fury. Conversion is optional and its 100-Cinder tier-one cost is covered by the quest's existing Cinder reward.

## Variants and consumable blueprints

New equipment allocates its entire base budget first. A variant adds 15% of that budget using its own stat profile, respecting the remaining attribute caps and overflowing into base attributes when necessary. No base attribute is reduced to fund the variant. Set bonuses are additional power and must be included in balance reviews.

One family blueprint plus 100 Cinders per equipment tier applies that variant with guaranteed success. It works across tiers and rarities on explicitly compatible archetypes, including already-styled items. One variant is active at a time; replacement removes the previous variant's contribution and set identity, with no refund. Applying the current variant is rejected. Conversion preserves ownership restrictions and all reinforcement investment. Dropped and converted variants with equal rarity, tier, quality, roll and rank have equal stats.

Completed dungeons independently award a themed Blueprint Choice with a 25% chance. After three consecutive completions without one, the fourth guarantees a choice. Counters are per character and dungeon family, shared across that family's grades, and reset on award rather than container opening. Completion retries do not add progress or duplicate rewards. Failed or unfinished runs do not count. Players see sources and remaining completions in the equipment panel. Later-region pools also contain earlier variant families.

Blueprints and choice containers are stackable and tradable. Conversion is performed from the equipment panel with a stat preview, cost, set replacement notice and explicit confirmation. Prices, source families, and probabilities are authored in `equipment-blueprints.v1.json`.

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
