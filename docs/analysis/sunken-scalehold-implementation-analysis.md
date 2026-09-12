# Sunken Scalehold implementation analysis

Repository review: 12 September 2026. Analysis only; no gameplay changes implemented.

The target is the primary LL game service: shared combat definitions/runtime, persisted world content, essence acquisition, and the Angular game frontend. LL-Chat, infrastructure-as-code, and external environments are outside scope.

This is a medium-sized content expansion with several shared combat extensions. Five creature records and five essence records are straightforward; exact passive behavior, random Elementals, and Exposed account for most implementation and testing work. Creatures and equipped essences should reference the same ten active/passive definitions, following existing Meran content. Their behavior must remain correct when a player combines the passives with other creatures' abilities.

## Area and content integration

The current world has four Meran areas, ending with Wolfsbane Reach at entry level 65 and difficulty tier 14. Sunken Scalehold should use `region_02_area_05`. Following the existing progression suggests level 70, difficulty tier 15, creature base level 70, creature tier 2, and the existing Tower Floor 10 access requirement. These are proposed defaults, not values specified in the request. Equal creature spawn weights of 0.2 and the existing Meran spawn distribution are sensible starting points.

Required content locations, relative to the repository root:

| File | Required work |
| --- | --- |
| `LL/src/API/API.LL/Data/world/creatures.json` | Five unique IDs, names, image keys, role/damage/defense profiles, levels and tiers. Current catalog already uses IDs through suffix 107; allocate against the catalog at implementation time. |
| `LL/src/API/API.LL/Data/world/regions.json` | Fifth area, entry/access requirements and five weighted creature references. |
| `LL/src/API/API.LL/Data/combat/abilities.json` | Ten shared active/passive definitions plus summon attack/passive definitions as needed. |
| `LL/src/API/API.LL/Data/combat/creature-abilities.json` | Map each `monster.lizardfolk_*` identity to its active and passive. |
| `LL/src/API/API.LL/Data/combat/statuses.json` | Battle Rhythm and, if used, a bounded next-basic-attack buff. |
| `LL/src/API/API.LL/Data/combat/summons.json` | Flame, Frost and Lightning Elemental definitions. |
| `LL/src/API/API.LL/Data/essences/essences.json` | Five essence definitions linked to the shared abilities. |
| `LL/src/API/API.LL/Data/world/creature-essence-loot-tables.json` | Five source/drop mappings. Neighboring wolves use baseDropChance 0.0001 and one variant of weight 1. |
| `LL/src/API/API.LL/Data/items/items.json` | Five obtainable essence items; essence definitions alone do not complete acquisition. |
| `LL/src/API/API.LL/Data/essences/essence-codex-collections.json` | Optional area collection, consistent with the preceding areas; its reward needs a design choice. |
| `LL/src/API/API.LL/Data/progression/region-combat-balance.json` | Append area and matching default build; review recommended combat ratings. |
| `LL/src/API/API.LL/Data/equipment/equipment-ordinary.v1.json` | Register the area under Meran so ordinary equipment and regional Sigil acquisition have the expected coverage. |
| `LL/src/Infrastructure/Service/Services.LL/Essences/EssenceCatalogService.cs` | Add five entries to the explicitly authored Meran source catalog. |
| `LL/src/Presentation/ll/src/app/core/services/client-side/region/region.service.ts` | Add the area to the frontend's separately authored region list. |

Suggested creature roles: Brute as physical Bruiser; Elementalist as magical DPS; Scout as physical DPS; Shaman as Support; Warrior as physical Bruiser or DPS. Defense profiles should follow the intended encounter balance rather than inventing individual stat overrides.

There is a progression trap: `RegionCreatureScalingProvider` interpolates recommended combat rating between Meran's current 200 and 354 endpoints using the number of authored areas. Appending a fifth area while retaining those endpoints changes the ratings of existing areas. Review the endpoint/build progression explicitly and test the previous four placements. This is a recommended-rating issue, not proof that every combat stat is interpolated by that same formula.

## Creature mechanics

### Lizardfolk Brute — moderate work

Author the active at 1.8 × Power Physical Damage and 170 cooldown ticks. The engine uses ten ticks per second. Stun(2) means two seconds of Stun using the existing standard-condition application rules.

Recommended interpretation of the alternate branch: if already stunned when the attack resolves, deal 2.7 × Power (180% × 1.5) and do not reapply/refresh Stun. Otherwise deal 1.8 × Power and attempt Stun(2). This needs an agreed active name and confirmation that +50% means a relative increase rather than 180% + 50 percentage points = 230%.

`HasCondition` already exists, but the current condition schema has no general negation/else branch. Add a small reusable conditional mechanism or damage modifier that can select the branch before effects mutate target state. Avoid implementing this as two damage hits: that changes crit rolls, defenses, barriers and on-hit reactions. The newly applied Stun must not activate the bonus on the same attack.

Brutal Follow-Up can reuse `OnAbilityUsed` and `ModifyNextBasicAttackDamage`. However, the existing modifier accumulates (`+=`), so two abilities before the next basic would produce +150%. The wording suggests one pending +75% bonus, refreshed by another ability. Implement a bounded, source-owned pending buff if that interpretation is retained. Specify whether a missed/dodged basic consumes it; matching the existing consume-on-attempt behavior is the least surprising default. It must work after other equipped active abilities, including healing and summoning abilities.

### Lizardfolk Elementalist — largest independent feature

Author Conjure Elemental at 200 cooldown ticks and summon lifetime 140 ticks. Existing summons already have owner attribution, inherited attributes, abilities, lifetimes and active limits. Reuse those systems and author three summon templates.

The current summon effect selects one `summonId`. Add a reusable random summon choice using the combat engine's seeded RNG. Assuming equal odds, one cast must select exactly one of the three variants. Three independent 33% chance effects are incorrect because they can produce zero or multiple summons.

Elemental Bond can build on `OnSummonChanged`, which publishes to the owner on spawn and removal paths. Filter for a lost/dead event target or add a removal-reason discriminator; an unfiltered listener would also grant Empower when summoning. Natural expiry sets the summon to zero health and publishes this notification, so a wholly separate summon lifecycle system is unnecessary. Verify death, expiry, consumption, replacement and simultaneous deaths, with exactly one grant per qualifying loss. The wording covers any owned summon, including those from other equipped essences. Apply existing Empower: fixed +20% Power for 10 seconds, refreshing rather than stacking.

Missing values are substantial: summon Health, Power inheritance, attack speed, defenses, attack delivery/damage type, Flame Burn magnitude, Frost Chill stacks, and Lightning crit bonus. Burn and Chill are parameterized conditions; “apply Burn/Chill” is not enough to author them precisely. Also decide whether repeated casts may coexist and whether the cap is shared across all three elements. Ascension and cooldown reduction can make summons overlap even though the base lifetime is shorter than the base cooldown.

Decide whether summon stats snapshot the owner on creation or update dynamically. Follow current summon inheritance unless a different design is intentional. Account for three summon portraits and combat presentation in addition to the five creatures.

### Lizardfolk Scout — moderate to large shared-condition work

Find Weakness is 1.2 × Power Physical Damage, then Exposed, with 160 cooldown ticks. Applying Exposed after damage means this hit does not benefit from the Exposed it just created.

Exposed is currently only a proposed lexicon contract in `docs/combat-lexicon/conditions/exposed.md`; there is no runtime enum entry. That contract specifies a unique 10-second harmful condition granting every attacker +10 percentage points critical chance against the affected target, subject to the existing 75% cap. It refreshes, does not stack, does not grant crit eligibility, and does not affect healing. It also describes Ward/immunity interactions. Implement and test these against the actual current prevention rules rather than assuming all proposed lexicon behavior already exists.

Implementation spans the standard condition identity and lifecycle, target-aware critical rolls, harmful-condition removal, catalog validation/authoring, tooltip/DTO presentation and frontend condition display. Append any new numeric enum identity to preserve existing values. Do not substitute Vulnerable: its damage amplification is different.

Keen Eye needs an agreed Crowd Controlled classification. Stun/Freeze only is different from also including Slow, Chill, Taunt and Silence. There is no generic hard-control query today. `ModifyCriticalDamageAgainstCondition` is an existing primitive, but its runtime sums matching conditions. Registering +25 for both Stun and Freeze would incorrectly give +50 on a target with both. Use a reusable “any qualifying control” query that grants the bonus once, evaluated before critical damage is resolved. Interpret +25% Critical Damage as +25 percentage points to the critical-damage stat unless specified otherwise.

### Lizardfolk Shaman — moderate shared-cleanse work

Herb Mixture is 2.1 × Power healing with 140 cooldown ticks. `LowestHealthAlly` already selects lowest absolute current Health, matching the request rather than lowest health percentage. Decide whether self and summons qualify; this matters in solo combat and because low-Health summons can attract every heal.

Cleansing Herbs should listen to the caster's healing events and cleanse the actual healed event target. This allows the essence passive to work with other healing abilities.

The current `Cleanse` operation removes all authored statuses and multiple harmful standard conditions, with special handling for some condition types. Setting a value of 1 does not currently turn it into a one-effect cleanse. Add an explicit limited cleanse path with a removal budget across eligible negative effects and deterministic selection. Preserve current broad-cleanse behavior for existing abilities. Beneficial custom statuses must not be eligible merely because they live in the same status collection.

Define “one” consistently with each condition's removal rules: one effect is not necessarily one stack. Decide selection priority, self-heal eligibility, full-health/zero-effective heals, periodic healing and lifesteal. Existing `OnHeal` emission includes periodic-effect application and some regeneration paths; blindly using it can cleanse more often than intended. Test an unrelated healing essence and a multi-target heal, not just Herb Mixture.

### Lizardfolk Warrior — moderate to large timing work

Spearhead Assault is 1.5 × Power normally or 2.2 × Power strictly above 70% Health, with 150 cooldown ticks. Existing above/at-or-below predicates cover the threshold. Both branches must use the target's health before this hit; sequential branch evaluation after damage can accidentally make both branches run when the first hit crosses 70%.

Battle Rhythm can use a capped status and existing status-stack/attribute primitives for 2% Attack Speed per basic, up to eight stacks (+16%). Follow the current attack-speed formula and explicitly decide flat percentage points versus a multiplier of existing attack speed.

The consumption portion needs cast-scoped handling: snapshot stacks before a qualifying damage ability, consume them once, and apply +2% damage per consumed stack to that cast. At eight stacks, a relative 16% increase gives Spearhead coefficients 1.74 or 2.552 before other modifiers. Removing the status must also remove its attack-speed contribution.

`OnAbilityUsed` currently dispatches active and passive effects through the same listener sequence; it is not a guaranteed pre-damage phase. Do not rely on essence slot order to consume stacks before damage. Add/reuse a proper cast context or preparation hook. Filtering only for Spearhead would break the equipped essence: other damaging actives must consume stacks, while pure heals/buffs must not. Define handling of multi-hit/AoE attacks, damage-over-time applications, damaging summons, echoes and repeats. A single cast should not consume repeatedly or multiply the bonus once per target.

## Essence progression and presentation

Use the shared active/passive definitions for enemies and player essences, with `owningEssenceId` and existing progression scaling. Authoring five Common single-variant essences with current evolution metadata follows the neighboring wolves; rarity, collection reward and any new evolution behavior still need a choice. Do not invent bespoke evolution mechanics for this request.

Audit `EssenceAbilityProgressionScaler` for every new field/operation: cloning, magnitude scaling, cooldowns, summon Health/Power multipliers and description rendering. Fixed counts/thresholds such as an eight-stack cap, a one-effect cleanse limit, the 70% threshold and random-choice weights should not grow accidentally. Check the intended treatment of Stun duration and per-stack bonuses explicitly. Small integer percentage bonuses can lose progression through rounding.

Allow for creature and summon art, essence item image references, region navigation, Exposed status display/help text, and tooltips for alternate damage branches, random summons and consumed-stack bonuses. Verify actual asset availability before choosing image keys; this review found no lizardfolk/elemental-named files in the searched frontend asset tree.

## Implementation sequence and verification

1. Settle mechanical ambiguities and missing Elemental values; author behavior cases that express the chosen contracts.
2. Implement reusable engine/schema changes: safe conditional hit resolution, random summon selection, bounded cleanse, any-control critical modifier and cast-scoped stack consumption. Extend validation/compiler/cloning paths where needed.
3. Add the ten abilities, summon definitions and five creatures/essences/items/drop mappings.
4. Wire Sunken Scalehold into world seeds, progression, equipment rewards, essence catalog and frontend navigation/presentation.
5. Run focused tests and balance simulations for both enemies and equipped essences, then the relevant broader regressions.

Key tests: Brute unstunned/already-stunned branches and repeated ability priming; exactly one seeded Elemental choice and correct lifetime; Bond on death/expiry but not spawn or unrelated owners; Exposed refresh/cap/removal and damage eligibility; Keen Eye with multiple simultaneous controls; lowest absolute Health healing and exactly one negative effect removed; Warrior at/below/above 70%, threshold-crossing hits, eight-stack cap, reset, non-damage casts and loadout-order independence. Include ascension and cross-essence combinations.

Add executable catalog behavior fixtures in `Data/combat/ability-behaviors.json`. Relevant existing suites include `AbilitySystemTests`, `StandardConditionSystemTests`, `EssenceAbilityProgressionScalerTests`, `EssenceAbilityDtoMappingTests`, `CombatAbilityTooltipTests`, `RegionOneIdleAreaSeedTests`, `EssenceCatalogServiceTests`, `ItemCatalogSeedingTests`, `EquipmentAcquisitionTests` and region scaling tests.

Backend tests must use `./build/run-tests.ps1`, with `-Filter` for focused execution. After frontend changes, use its existing npm `test:ci` and build scripts; place any configured npm cache beneath `$env:TEMP`. Exercise a fresh seed and an existing world: `SeedCreatures.EnsureRemainingRegionOneIdleAreas` already upserts authored creatures and regions despite its old name.

Balance review should include long fights and combined loadouts: Shaman cleansing can be multiplied by fast heals; Bond can trigger from cheap disposable summons; Brute/Scout benefit from sustained control; Warrior depends on attack rate and active cooldown ordering. The three Elementals cannot be balanced meaningfully until their stats and condition values are specified.

## Change and release implications

This analysis adds only this document. Existing unrelated working-tree changes, including the proposed Exposed lexicon, were read but not modified. Verification for this analysis is source inspection and JSON/reference checks; no combat/backend/frontend tests were run because no executable behavior changed. The test commands above are the implementation verification plan, not claimed passing results.

No EF schema migration appears necessary: existing creature/area persistence and JSON catalogs support the new content. Future implementation requires updated backend catalogs/runtime, frontend assets/navigation, and the normal world-content seed/update lifecycle. New runtime condition values should remain serialization-compatible. No database update or deployment was performed, and none is authorized by this analysis request.
