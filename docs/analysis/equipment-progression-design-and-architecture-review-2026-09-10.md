# LegendsLegacy equipment progression: game-design and architecture review

Originally written: 10 September 2026. Revalidated: **2 October 2026**, against gameplay code at `3e5b6d2e1`; checkout advanced to `02c07b0db` during review with only unrelated currency artwork added. Scope: the primary LL game, its API, persistence, combat workers, Angular client, administrative acquisition paths, and balance tooling. This is a design report; no gameplay implementation, configuration edit, migration, database operation, or deployment accompanies it.

**Recommendation:** retain the direction of monster-targeted loot, readable bases, bounded specialization, recognizable named drops and personal guarantees. Build on the now-implemented core/specialization allocation, activity-aware comparison, versioned equipment catalogs and audited conversion tooling. Removing Quality, reinforcement, blueprints and combat-rule sets remains a **proposed design choice**, not an approved cleanup or a description of the current game. Potential and Tempering are already absent; keep them absent.

The remaining shift is toward choosing a monster source for equipment suited to a specific combat job. Current specialization already gives gear more identity than the original report credited. Essences supply abilities; Doctrines supply combat rules; equipment supplies the foundation, specialization and encounter preparation.

Numbers below are **illustrative design inputs**, not validated live balance. Current behavior, historical intent, inferred problems, and proposed behavior are identified separately. Evidence IDs refer to the linked source index at the end. Repository content establishes what this checkout supports; it does not establish which migrations or settings are running on a deployed server.

### What changed since the original review

| Change now implemented | Consequence for this review |
| --- | --- |
| Equipment release 4, with 70% core / 30% specialization and legal slot-specific profiles | Retire the claim that gear is only a fixed scalar package. Proposed independently rolled affixes remain additional work. |
| Attribute rules 18: item-tier defense normalization, Restoration, Ability Haste and Tenacity | The old character-level defense cliff is resolved. Use current units and legality in examples. |
| Set-bearing styles reserve 10% budget for identity; fixed authored set bonuses were restored | Acknowledge the reservation; effectiveness still needs combat valuation. Removing sets is a design proposal. |
| Activity-aware comparisons, richer metrics and comparison observation | Reuse these APIs; encounter/trigger simulation and market comparisons remain separate gaps. |
| Versioned migration preview/apply/rollback, one-time specialization choice and startup conversion | Extend existing conversion contracts; do not propose rebuilding them or deleting required historical catalogs. |
| Nobility: up to 168 hours retained offline combat, six presets and 30 market orders/listings | Model a weekly return as well as a daily session; limits remain three presets and ten orders/listings without Nobility. |
| New Meran area and five additional creatures | Current world has 106 creatures and five Meran combat areas; later regions remain unauthored. |
| Optional Grimoire character overview and frontend migration rules | Inventory and Auction House remain legacy screens; a future redesign must follow the deliberate screen migration policy. |

Repository defaults select **attribute rules 18 and equipment release 4**. Startup equipment conversion is enabled in the API configuration. Historical `.v1` catalogs are not automatically the active catalog; use the release registry. These observations establish checkout behavior, not deployed state. [E36–E42]

## 1. Current system summary

### The implementation has already changed substantially

The live system is not the old crafting system described in several historical plans. Crafting, gathering, queued tempering, Potential, equipment XP, and the Forge have been removed through code cleanup and migrations. The current model retains **Quality, seven rarities, frozen item-wide rolls, reinforcement ranks, styles/variants, sets, and consumable blueprints**. Later work reintroduced blueprint consumption for variant conversion; an older engineering document saying that blueprint IDs do not imply inventory items is now incomplete. [E01–E07]

There are **eight equipment slots**: Head, Chest, Legs, Ring, Necklace, Relic, MainHand, and OffHand. A two-handed weapon is one inventory instance but occupies two slots and now contributes **two occupied slots to set thresholds**. A full loadout contains seven or eight distinct items. Release 4 offers **28 archetypes**, **30 named definitions**, **11 styles** and **11 current sets**; the resolver also retains 11 historical set definitions. Thirteen specialization profiles are defined, with twelve explicitly referenced by archetypes. They expand into 115 additional archetype/profile combinations across seven rarities: **1,031 evaluator definitions** comprise 196 base, 805 specialized and 30 named definitions. These are not 1,031 separately designed item identities. The archetypes comprise nine armor pieces, three jewelry types, five one-handed weapons, eight two-handed weapons and three offhands. [E01–E04, E36–E37]

Actual released equipment catalogs support tiers 1–2. World JSON contains Shenic, with ten ordinary combat areas plus the tutorial, and Meran, with **five** combat areas; there are **106 creature definitions**. Meran's added Sunken Scalehold is level 70, difficulty 15, with Tower floor 10 access. Four dungeon families each have three difficulties. The canonical progression policy describes ten regions and the equipment curve can extend further, but that is not equivalent to authored Region 3–10 loot. [E08–E12]

### Item identity and stat construction

`EquipmentBase` supplies item-base/type identity. `EquipmentInstance` carries persisted evaluated equipment and `EquipmentData`; the frozen descriptor contains `EquipmentState`, archetype/definition identity, rarity, quality, tier, rank, native/active style, roll multiplier, evaluated stats, weapon behavior, provenance, and ownership. Historical `ModelE*` names survive in some storage/content identifiers; that does not imply the removed Forge is still a player feature. [E01–E03]

For a current newly evaluated item, the approximate pre-allocation budget is:

`B = 100 × 15.2^((tier−1)/9) × slotWeight × rarity × quality × (1 + 0.04×rank) × frozenRoll`

`slotWeight = 2` for two-handed weapons and `1` for other items. The evaluator splits the base budget into **70% core and 30% specialization**. An additive style has a nominal extra 15%; a set-bearing style allocates **5% to style stats and reserves 10% for set identity**. The current fixed set effects are not dynamically scaled by that reservation, so it is an accounting allowance rather than proof of equal combat value. The regional budget multiplier remains approximately **1.35306**, or **35.3% per equipment tier**. Tier 1 has budget 100; tier 10 has 1,520. [E02–E05, E37]

| Dimension | Current behavior |
| --- | --- |
| Rarity | Common 1.0; Uncommon 1.1; Rare 1.3; Epic 1.6; Unique 2.0; Legendary 2.5; Legacy 3.0. These multiply budget; they are not affix counts. |
| Quality | Crude 0.90; Standard 1.00; Fine 1.12; Exceptional 1.26; Masterpiece 1.42. |
| Quality probabilities | Current ordinary JSON: **12.5%, 50%, 25%, 10%, 2.5%**, respectively, for both area and dungeon equipment. The current specification's 0/35/45/16/4 distribution is stale. |
| Frozen roll | One uniform multiplier from 0.95 to 1.05 on the whole budget. Individual stats are not independently rolled affixes. |
| Specialization | Authored legal profiles allocate the 30% share; ordinary generation selects an archetype first, then one of its eligible profiles. This adds build choices without an unrestricted affix generator. |
| Rank | 0–5; four percent additional budget per rank, reaching 20%. Area drops start at 0; ordinary dungeon rewards at 1. |
| Variant | Compatible active style adds a fixed authored stat profile and potentially set membership. A replacement can change that profile; it preserves tier, quality, rarity, roll, and rank. |
| Requirements | Equipment tier determines character level: T1 requires level 1; T2 level 50; T3 would require 100. Equipping does not require personally clearing the source region. |

For fixed tier, rarity and rank, Quality and the whole-item roll mostly answer the same question: how large is this otherwise identical package? The raw scalar envelope from Crude/low-roll to Masterpiece/high-roll is `1.42×1.05 / (0.90×0.95) = 1.744`. This is a much larger difference than the apparent ±5% roll suggests. The catalog's average Quality multiplier is 1.054, but its tails matter much more to item replacement. [E03–E05]

Within the **70% core share**, Heavy armor allocates 40% Health, 30% Armor, 30% Resistance; Medium allocates 35% Power, 25% Health, 20% Armor, 20% Resistance; Light allocates 70% Power and 10% each to Health, Armor, Resistance. Head, Chest and Legs share core weights and budgets but have different legal specialization pools. A default ring combines a Power core with crit specialization; necklace combines Health with Tenacity; relic combines regeneration with Ability Haste. Weapons have a Power core and a legal specialization. Shields normalize their Health/defense core separately from Block specialization. The authored base weapon interval and damage multipliers currently equal 1. [E02–E04, E37]

Current ordinary gear uses 14 attributes: Power, Max Health, Armor, Resistance, Crit Chance, Crit Damage, Armor Penetration, Magic Penetration, Attack Speed, Block Chance, Health Regeneration, **Restoration, Ability Haste and Tenacity**. Older Healing Power/Cooldown/Status Resistance/Crowd-Control Resistance wording is not the current equipment contract; Dodge, general Damage Reduction and Life Steal are also excluded from ordinary gear. Crit Damage requires Crit Chance on the same item, and Attack Speed cannot coexist with Ability Haste. Slot legality is enforced. Release 4's authored stat costs, not the global default price table, determine current evaluation. [E37–E38]

### Actual sources and progression gates

| Source | What this checkout actually awards |
| --- | --- |
| Ordinary area combat | One equipment roll **per victorious encounter**, at 1/864. Conditional rarity: 85% Common, 12% Uncommon, 3% Rare. Equipment comes from the area/region pool, not the killed creature's own equipment table. |
| Base selection | 40% weapons, 35% armor, 25% jewelry; within weapons 60% one-handed/offhand and 40% two-handed. Select archetype first, then specialization, so profile count does not inflate an ordinary base's chance. A 15% compatible regional variant roll follows. |
| Dungeon completion | Equipment chance 50% + 5 percentage points per mastery level, capped at 100%; mastery at run start determines the roll. Variant chance 50%, with family-specific eligibility. |
| Dungeon rarity | Novice: Uncommon/Rare/Epic 84/14/2; Veteran: Rare/Epic/Unique 84/14/2; Champion: Epic/Unique/Legendary 84/14/2. |
| Dungeon rooms | Miniboss rewards have a 25% equipment path. Treasury reward selection can produce a blueprint or styled equipment, with a 50/50 branch. These are separate from completion rewards. |
| Dungeon blueprints | 25% per completed run, fourth completion guaranteed after three misses, per character/family and shared across grades. This protects blueprints, not a desired equipment item. |
| Starter/tutorial/quests | Chosen bound starter weapon and explicitly authored equipment chests; early armor/jewelry chests give Common T1 equipment. |
| Event rewards | Current random equipment box can award two T1 Uncommon items. |
| Administrative grants | Canonical equipment grant/preview paths exist. They are operational tools, not ordinary progression. |
| Raids | Two authored bosses and five difficulty rows in total; current rewards are trophies, Soul Dust and monster cores. No currently authored equipment reward; trophy-vendor item list is empty. |
| Region boss | Mad King reward configuration is disabled with empty reward brackets. The reward schema supports currencies, not current signature equipment. |
| World Tower | Fifteen authored/released floors, Tower Tokens and successful-participation title rewards. Tower supply equipment code/catalogs exist but are disabled and not wired into active completion rewards; they are not a current equipment guarantee. |
| Guild shop / Champion Market / other reward catalogs | Active offers focus on currencies, cores, sigil fragments and titles. Generic item factories are not evidence that these sources currently distribute equipment. |

The generic reward table file is empty and creatures have no active authored equipment reward tables. The infrastructure can resolve generic item rewards, including old-style equipment construction paths, but the audit found no current equipment-base references in those generic content reward lists. This is a future ingress risk rather than a current competing equipment economy. [E08–E17]

Random equipment boxes differ from ordinary generation: they sample eligible **definitions**, so expanded specialization counts weight bases. A jewelry box currently selects Ring/Necklace/Relic with shares **5/12, 4/12 and 3/12**. Do not assume every acquisition path shares the archetype-first weighting. [E09, E15, E42]

The 30 named definitions are not a current direct unique-drop table: natural area/dungeon selection chooses base definitions and then attaches styles. The evaluator's current display naming can also derive “Style + base name” instead of preserving the authored named definition's title. Goblin Mines targets Fury/Phoenix blueprints, Forgotten Catacombs Arcane/Endurance, Tangled Cave Execution, and Great Tree Spirit. Only these six styles have current direct blueprint-item sources; the broader eleven-style catalog can appear as native variants. Dungeon style rolls are subject to base compatibility: the nominal 50% produces about 45.5% styled equipment in Goblin Mines/Tangled Cave and 50% in the other two families. [E04, E07, E09, E13]

**Raids retain a direct crafting-era gate:** Head, Chest and Legs must all satisfy minimum tier and rarity and have an active style when `RequiresBlueprintArmor` is enabled. The loader requires that setting for authored raids. The failure text still describes “Blueprint-crafted” armor. A player can have an effective build and be rejected because it lacks the prescribed rarity/style arrangement. [E18]

### Equipment operations, inventory and economy

Equipping validates ownership, level and hand compatibility, binds eligible personal items, exchanges displaced items with inventory, and publishes the resulting state. Unequipping returns equipment to inventory. Saved equipment loadouts and activity-specific automatic selection already exist. The actual combat loadout can therefore differ from the gear displayed in the basic equipment slots. [E19–E22]

There are **three saved equipment loadouts, or six with Nobility**. Equipment uses one inventory row per unique instance with quantity one; the inventory repository loads the full inventory and related metadata rather than a paged equipment result. Favorites/unseen status belongs to the inventory ownership row, with favorite state preserved while equipping and returning items. [E01, E19–E20, E24, E39]

The comparison query now accepts an **activity**, resolves that activity's equipment and Essence loadouts, applies current rules and exposes Doctrine identity and per-Essence cooldown changes. It handles both displaced hands, set changes, core/specialization budget, raw/effective/over-cap attributes, critical output, effective health, barrier, regeneration, Restoration and attack interval. Comparison observations can be recorded. It explicitly excludes temporary effects and Doctrine triggers; it does not simulate rotations or encounter outcomes. It accepts inventory candidates, not arbitrary Auction House stock. The older Combat Rating projection remains limited, including identical single/multi-target offense and zero control utility; neither it nor Gear Power proves an upgrade. [E21–E23, E38]

Reinforcement and dismantling use current prices for T1–2. Single-slot T1 rank costs in parts are 5/10/20/40/80, with Cinder costs 11,150/22,300/44,600/89,200/178,400; T2 doubles these. The full T1 ladder costs **155 parts and 345,650 Cinders per occupied slot**; a two-hander costs **310 parts and 691,300 Cinders**. Dismantling returns tier plus half the cumulative rank part cost, rounded down, then multiplied by occupied slots. Recovery is based on rank, including awarded rank, not payment history. T1 rank-1 returns three parts for a single-slot item or six for a two-hander; T2 returns seven or fourteen. Rarity and Quality do not change that return. T3 price data remains unauthored. [E05–E06]

Blueprint conversion consumes one compatible blueprint plus `100×tier` Cinders **per occupied slot**: a two-hander consumes two blueprints and `200×tier` Cinders. It is guaranteed; replacement loses the former variant without refund. Conversion does not itself bind unbound gear; reinforcement does. Upgrade execution locks/reloads, re-quotes, evaluates using the item's recorded equipment release and records an idempotent operation receipt. It does not require a previously issued preview token. [E06–E07, E36]

The equipment Auction House operates alongside the stackable-item Bazaar. Normal random-discovery equipment can be traded while unbound. Deterministic protected/quest awards are personal. Guild donation creates persistent guild ownership and loans preserve it. The buyer's progression is not checked at purchase; equipping later checks character level. Existing trade provenance, ownership checks, listings and economic ledger are valuable foundations. There is no supported general NPC equipment-sale loop in the audited client/service path; dismantling into Reinforcement Parts is the ordinary disposal loop. [E24–E27]

The Angular client has equipment cards/details, activity comparisons, filtering, sorting, favorites/unseen indicators, loadouts, upgrade/variant previews and bulk dismantling. These are browsing tools, not automatic loot admission filters. Bulk dismantling uses rarity **or** a Gear Power threshold and excludes favorites, but can include unseen or saved-loadout items. It previews/mutates sequentially per item. Missing saved-slot references are now pruned; partial and even empty presets can remain usable, so deletion can silently weaken a saved build rather than necessarily trigger fallback. Backend rewards preserve instances, but frontend base-ID summary grouping can collapse distinct descriptors into the first representative item. No general auto-sell or auto-dismantle policy is implemented. [E20, E28–E31]

The optional Grimoire character overview uses the shared device-persisted New look setting, default off. Inventory and Auction House routes still use legacy screens. The migrated-specialization chooser is a one-time conversion repair choice, not a general affix reroll system. [E40–E41]

### Character power and the other progression systems

Under attribute rules 18, each item's Armor/Resistance contribution is normalized using **that item's tier before aggregation**, not character level. With normalized rating `R`, mitigation is `0.8×R/(R+165)` as a fraction, before typed penetration. Corrosion reduces rating first; penetration subtracts percentage points after the curve, capped at 40 and floored at zero mitigation. **The former level 51/101 defense cliff is resolved.** T2 equipment still requires level 50. Higher-tier gear does not automatically provide more normalized defense simply because its raw tier budget is larger. [E03, E38]

Restoration scales healing, regeneration and barrier support; Ability Haste uses `ceil(baseTicks/(1+haste/100))`, capped at 66⅔; Tenacity governs resistance to harmful effect application. These are current shared combat semantics, not new attributes this proposal must invent. Percentage-heavy old gear and replacement incentives still deserve simulation, but character-level decay should not be reintroduced as a presumed bug fix. [E38]

Offline retention is now coverage-aware: nominal benefits are **24 hours without Nobility and 168 hours with Nobility**. A continuous eligible seven-day return represents 60,480 encounter opportunities at the unchanged ten-second cadence. This is a scenario, not a universal catch-up ceiling: retained historical paid windows plus a current free window can exceed seven days in one settlement. Nobility also changes Creature Focus cooldown from eight to two hours. Loot UX must handle these windows without assuming daily logins or retroactive full-week entitlement after a late subscription. [E39]

Essences already carry active abilities, passives, attributes, tags, evolution and ascension. Their attunement slots unlock every ten character levels, from one to a maximum ten. Creature Focus already changes creature spawn weighting and Essence rewards, giving an existing interface for discussing farm targets. [E32]

The user-facing concept called **Doctrines in this brief is implemented as Combat Styles in this checkout**. The four are Bastion, Conduit, Reaper and Duelist. Their mechanics respectively change healing into Barrier, channel the first Essence using Charges from other casts, harvest condition damage, and build Read for stronger Essence hits. They progress to mastery 10 with refinement, upgrade, opening-technique and mastery decisions. No separate live Doctrine subsystem was found. Equipment should be designed against these actual rules. [E33]

## 2. Problems with the current system

1. **Several labels still multiply the same underlying budget.** Rarity, Quality, rank and whole-item roll overlap. However, the new specialization profiles already create meaningful stat-direction choices; the remaining problem is excessive scalar layers and weak source targeting, not a complete absence of item variety.
2. **The monster roster has little equipment identity.** Area pools determine gear. Choosing a creature currently matters far more to Essences than to equipment.
3. **Dungeon difficulty mostly escalates rarity multipliers.** A higher rarity is a strong default improvement; Unique is a numeric rarity rather than a guarantee of unique behavior.
4. **Sets carry substantial combat-rule identity beyond static comparison.** The 10% identity reservation now acknowledges their cost, but does not scale the restored fixed effects to their actual value. Fury's crit-triggered Power, Arcane's third-cast reward and Execution's low-health damage overlap Combat Style ownership. Removing them remains a proposed simplification, not correction of missing accounting. [E04, E37]
5. **A desired slot and a usable stat direction lack reliable equipment protection.** Blueprint pity does not solve either problem. At the current area rate, Rare equipment averages one every 80 hours of uninterrupted victories, before slot, style or Quality suitability.
6. **Raid eligibility prescribes equipment construction.** It constrains viable builds using rarity and style rather than demonstrated competence.
7. **The UI protects favorites, not the complete set of player intentions.** Saved builds and important unseen items are vulnerable during bulk disposal; base-ID summary grouping hides differences. Nobility raises the required review window from a day to as much as a week.
8. **The persistent economy relies on binding and dismantling but lacks a designed long-term supply budget.** Infinite unbound random production still creates a growing supply of never-equipped gear. An Auction House fee removes currency, not equipment.
9. **Further regions need content work, not just a formula.** T3–10 require acquisition pools, prices or their replacement, item identities, monster sources and progression validation.
10. **Documentation and historical catalog filenames can mislead a redesign.** Older blueprint wording, Quality probabilities and character-level defense rules are stale. Select catalogs through the release registry and verify active service wiring; inactive Tower supply code is not proof of an available reward.

These are design findings, not claims that the existing code is universally broken. The revalidation suite passed **831 tests, with eight skipped**. The former activity-comparison gap is resolved. A remaining **unreproduced correctness risk** is mutation of inventory-resident activity-loadout gear: upgrade settlement is gated by physical `IsEquipped`, and dismantling does not use that settlement path. Verify earned-combat behavior before changing mutation rules. [E06, E20–E23]

## 3. Crafting-era mechanics to remove

| Mechanic | Classification | Reason in a loot-driven game |
| --- | --- | --- |
| Potential | **REMOVE / keep deleted** | A ceiling on future item labor is redundant when the interesting object is a completed drop. It adds another hidden valuation axis. |
| Queued/directed Tempering | **REMOVE / keep deleted** | Makes finding gear the beginning of another production queue, adds investment lock-in, and competes with actual combat farming. |
| Quality | **REMOVE** | Duplicates rarity and roll strength and retains explicit craftsmanship language. Narrow per-affix values provide enough variation. |
| Consumable variant blueprints | **REPLACE** with directly dropped regional affixes and named items | Source identity should belong to the item found. Applying a generic style to a bought base weakens that connection. |
| Rank reinforcement and Reinforcement Parts | **REMOVE** | The rank ladder primarily adds numbers and salvaging obligations. Guarantee useful gear through combat milestones instead. |
| Whole-item random multiplier | **REPLACE** with bounded per-affix values | A coherent core plus a few independently meaningful rolls makes comparisons clearer. |
| Rarity-wide budget multipliers | **REPLACE** | Color should communicate specialization and complexity, not automatic statistical superiority. |
| Mandatory blueprint/Epic armor gate | **REMOVE** | Raids should test competence and progression access, not adherence to obsolete production rules. |
| Four-piece sets that grant a combat engine | **REPLACE** with self-contained named items | They bundle too much identity into equipment and discourage experimenting with individual slots. |
| Recipe/material/player-profession progression | **REMOVE / keep deleted** | No proposed equipment feature needs these systems back. |

## 4. Crafting-era mechanics worth preserving

**KEEP** slot and handedness rules, archetype identity, complete dropped objects, source provenance, ownership, item IDs, loadouts, favorites, server authority, idempotent rewards and mutation receipts where mutations remain.

**KEEP and extend** the existing budget allocator, core/specialization split, legality rules and versioned attribute catalog as internal balance tools. Current profiles already fund build-specific choices. Independent affix selection and narrow per-trait rolls would extend that model; they do not justify replacing working allocation/versioning infrastructure. [E36–E38]

**MODIFY** equipment tier into a readable region/band requirement. Preserve finite authored content validation and explicit access rules; remove independent tier, item-level and region labels that repeat the same information.

**KEEP the function** of directed farming and bad-luck protection. Replace blueprint consumption with source-specific dropped items and personal acquisition progress. The valuable idea is player agency, not the old item used to express it.

Historical purpose deserves explicit treatment:

| System | What it currently does | Historical purpose and evidence | Decision |
| --- | --- | --- | --- |
| Tempering | No live operation; old queues/fields were removed. | Historical design describes spending Potential on timed development attempts, item XP/rarity progress and occasional quality changes. It extended the crafted object's development life. | Keep deleted. Let combat acquisition and build preparation occupy that time. |
| Potential | No live equipment property; removed from item instances and snapshots. | Historical formula combined tier, slot weight, Quality, mastery and crafting level into an investment allowance. It distinguished promising crafting outcomes. | Keep deleted. Do not rename it capacity, upgrade slots or item growth. |
| Quality | Live 0.90–1.42 whole-budget multiplier, randomly awarded and preserved. | Historical Quality strengthened the item and its Potential; skilled production influenced outcomes. Current drops inherit the stat multiplier even though the profession context is gone. | Delete from the next item model. Its remaining function is covered more cleanly by bounded affix rolls. |

The historical formula and intent are drawn from the explicitly historical crafting review, not reconstructed as current code behavior. [E34]

## 5. Design goals and three alternatives

Equipment should make a player say, “This improves my survival against these enemies,” or “This is the weapon profile my Essence loadout needs.” It should create recognizable farm goals and occasional satisfying surprises. It should usually **support** a build, occasionally adjust its preferred matchup, and rarely introduce one small conditional behavior. It should not supply a second deck of abilities or another Doctrine.

Healthy targets: complete a basic early outfit through onboarding; receive roughly 25–35 ordinary random items per eligible day at high win rates; inspect a handful of candidates on a daily visit or a grouped batch after a longer absence; make frequent early improvements and much slower late optimization. Nobility's weekly return must not require seven separate daily review chores. A strong item should normally survive the rest of its region and part of the next. Good decisions should survive bad rolls; exact perfect rolls need no guarantee.

| Dimension | A. Broad randomized loot | B. Monster-targeted bounded loot — recommended | C. Deterministic named equipment |
| --- | --- | --- | --- |
| Core loop | Farm efficient content, compare many generated affix combinations, sell/trade improvements. | Pick a combat need and source, find a readable base with constrained affixes, use personal guarantees for gaps, hunt signatures. | Select a named item, complete its encounter/milestone, receive a mostly fixed object; progress through alternate named choices. |
| Strengths | Surprise, trading depth, very long roll chase. | Strong monster identity, visible goals, manageable comparisons, room for both drops and agency. | Excellent readability and certainty; easiest inventory experience. |
| Weaknesses | Long-tail frustration, filter dependence, market concentration, content becomes item-level delivery. | Requires thoughtful family pools and target UI; poor pool design can still create dead drops. | Farming can end once the catalog is checked off; solved equipment lists emerge quickly. |
| Development complexity | High generator/filter/economy complexity, moderate individual item authoring. | Medium-high initial contracts; moderate ongoing data authoring using shared pools. | Low generator complexity, high ongoing bespoke item/content demand. |
| Long-term replayability | High numerical tail, potentially weak meaningful variety. | High across encounter goals and alternate loadouts; bounded rather than infinite power chase. | Moderate; heavily dependent on new challenges and catalog expansions. |
| Idle suitability | Weak without aggressive automation; reasonable after substantial UX work. | Strong at the proposed volume and shortlist protections. | Very strong for low attention. |
| Auction House | Becomes the main RNG bypass and price-discovery engine. | Useful access to build options after earned progression; binding controls resale. | Mostly duplicates/collection trade; deterministic supply suppresses prices. |
| Build diversity | High potential, but universal affixes and perfect items can dominate. | Broad but constrained by actual Essences/Styles and combat jobs. | Authored variety is clear but easiest to solve into fixed best lists. |
| RNG | High across base, rarity, affixes and values. | Moderate base/affix RNG, narrow values, explicit personal safety nets. | Low; random drops are mostly early unlocks or cosmetics. |

Choose **B with C's personal baseline guarantees**. Do not attach a full ARPG generator to A and call a loot filter the idle-game design. Conversely, do not make every desired item a deterministic purchase: surprise should remain relevant after a player has a functional outfit.

## 6. Recommended equipment loop

1. Choose an activity and a combat objective: survive physical bursts, improve basic attacks, support healing, or complete a source collection.
2. The source panel shows likely bases, emphasized affixes, signature items and guarantee progress. Select a hunting focus if available.
3. The server resolves a victorious encounter and makes one equipment opportunity roll, independently of how many monsters spawned.
4. On success, choose the source family, base, rarity and limited affixes; freeze the complete item and its provenance.
5. Classify it against the player's saved builds and explicit keep rules. Protected or interesting items enter the shortlist; eligible unwanted equipment can be sold automatically under an enabled rule.
6. At review time, equip, retain, list, donate eligible gear, or sell. There is no mandatory post-drop enhancement chore.
7. Stronger and better-matched gear improves win rate and enables new challenges. Personal baseline progress continues even when random drops miss the desired slot.

At the existing fixed idle cadence, equipment does not increase rewards simply by shortening a rendered fight. Its immediate farming benefits are winning more encounters, surviving harder activities, and meeting useful challenge targets. Model reward throughput using victories, not animation DPS. [E12]

Farming a cleared area remains useful for a desired base/affix, a boss signature, an alternate loadout, a collection appearance, an Essence, or saleable unbound equipment. Every region should have several sources with overlapping basic coverage; no essential defensive property should require one rare monster that barely spawns.

## 7. Item anatomy

| Property | Proposed meaning and necessity |
| --- | --- |
| Instance ID | Stable ownership, trade, retention, comparison and audit identity. |
| Definition/base ID and name | Recognizable object, weapon behavior and slot compatibility. Ordinary names describe the base; signatures retain their authored names. |
| Slot/type/handedness | Existing eight slots and hand rules. Two-handed items receive two hand-slots' budget and are compared against both displaced items. |
| Region and source band | Vertical progression and access. Show “Region 3 • late-region” rather than three redundant level labels. Internal numeric band is acceptable. |
| Rarity | Describes how much of the budget is allocated to customizable specialization. No universal rarity multiplier. |
| Core stats | Fixed, readable archetype foundation; most of the item's useful power. No random quality multiplier. |
| Affixes | Zero to three readable traits, drawn from small legal pools; fixed budget shares and narrow value rolls. Store affix IDs, rolled values and definition versions. |
| Named identity / signature | Optional authored identity with at most one conditional trait. It consumes ordinary affix budget. |
| Provenance | Source creature/family, activity, region, award identity and content version; supports discovery, guarantees and audit. |
| Ownership | Unbound personal, bound personal or guild-owned, using the current ownership distinction. |
| Frozen evaluated descriptor | Ensures moving/trading an item does not reroll it; enables reward retries and stable displays. Live balance versioning remains explicit. |

No Quality, Potential, rank, equipment XP, sockets, prefix/suffix naming grammar, transferable extracted modifiers, durability, maintenance timer, or general skill tree on equipment. A family-themed implicit is simply a visible fixed trait funded by the same budget, not an additional bonus layer. There is no separate item-level treadmill inside each region: use three bands at 1.00, 1.04 and 1.08.

## 8. Rarity model

**Proposed replacement, not current behavior.** Today all ordinary rarities use the 70/30 core/specialization split and rarity still multiplies total budget. The table below changes both contracts and requires simulation before adoption.

| Rarity | Ordinary drop share | Core/affix budget | Role |
| --- | ---: | --- | --- |
| Common | 55% | 100% core; no affix | Immediate foundation; focused raw-stat options can remain useful. |
| Uncommon | 30% | 80% core; one 20% affix | One strong, readable specialization. |
| Rare | 13% | 70% core; two 15% affixes | Main target for a focused endgame build. |
| Epic | 2% | 70% core; three 10% affixes | Broader support for hybrid jobs; less concentrated in each secondary than a Rare. |

All have the **same nominal total budget** at equal base/region/band. Affix rolls are 80–100% of the allocated amount. Thus actual total budget is 100% for Common, 96–100% for Uncommon, and 94–100% for Rare/Epic. This is intentional: rarity buys specialization and combinations. It does not promise more Power or more total points.

A Rare with two desired traits can beat an Epic whose three traits split its specialization too widely. An Uncommon can provide the strongest single secondary. Common should remain a practical foundation and occasionally a deliberate stat choice, but not the best answer for every build. Current jewelry already has a separate specialization; its cores remain concentrated in Power, Health or regeneration. Test balanced accessory foundations before making the proposed Common allocate 100% to those cores.

Remove Unique, Legendary and Legacy as ordinary equipment power-rarity levels. **Named** is an identity/source category over Rare or Epic. Exceptionally scarce appearances and collection distinctions can have presentation labels, but they do not add another power multiplier. Do not alter the shared rarity enum for Essences merely to change equipment rarity; isolate the equipment contract.

## 9. Affix model

Start from the **current 14 ordinary equipment attributes and slot legality** listed in section 1. Use Restoration, Ability Haste and Tenacity with their current shared semantics; do not revive removed healing/cooldown/resistance aliases as additional affixes. Retain the no-Attack-Speed-plus-Haste rule and paired Crit Chance requirement for Crit Damage. Avoid near-synonymous damage percentages. Add elemental/basic-attack/support traits only when combat has a defined place to apply and measure them. [E37–E38]

Reuse the current small per-slot allowed pools; do not force six to eight traits onto slots whose legal pool is smaller. Each source family emphasizes two or three. A proposed Rare has one family-weighted trait and one compatible general trait; Epic adds a third while splitting the same budget. Reject duplicates, contradictory traits and combinations that cannot benefit the base's intended role. Preserve linked traits such as Crit Chance/Crit Damage as explicit legal packages when necessary. Do not let a trait buy back the identical core allocation at a better exchange rate.

Affix values use **five bands: 80%, 85%, 90%, 95%, 100%**, equally likely initially. Value bands are deliberately coarse enough that perfect numerical rolls are not a microscopic event. A specific two-affix item has a 1/25 chance of perfect values once its desired affixes are present; its desired identity is the more important chase. Do not add a hidden overall roll on top.

Keep numeric effects in shared attribute buckets. Equipment-only conditional damage/healing bonuses should share additive buckets and a combined loadout cap, initially 20%, rather than multiply each other. General cooldown and attack-speed caps still apply. Equipment does not reduce Doctrine trigger thresholds, add Essence slots, duplicate Essence casts, or create self-triggering proc loops. A conditional trait can activate at most once per originating event; reflected, triggered and summoned events require an explicit eligibility policy.

Percentage-valued specializations require a separate balance pass. **Preserve rules 18's item-tier defense normalization**; the original recommendation to fix a character-level cliff is obsolete. Display raw and effective contributions, as the current comparison already does. Establish replacement pressure through useful core growth, affordable alternatives and encounter demands, then measure whether old percentage-heavy accessories persist too long. Do not assume rising raw defense buys rising mitigation, or silently introduce character-level decay as part of this loot redesign. Any different normalization policy would be a separate, explicitly simulated combat decision. [E38]

## 10. Regional progression

Proposed single-slot nominal budget:

`B(region, band) = 100 × 1.18^(region−1) × band`, where `band ∈ {1.00, 1.04, 1.08}`.

| Region | Entry budget | Late budget |
| --- | ---: | ---: |
| 1 | 100.00 | 108.00 |
| 2 | 118.00 | 127.44 |
| 3 | 139.24 | 150.38 |
| 5 | 193.88 | 209.39 |
| 7 | 269.96 | 291.55 |
| 10 | 443.55 | 479.03 |

This replaces the current approximately 35.3% tier jump with 18%, plus a modest 8% spread within a region. It is not a safe isolated balance patch: monster scaling, character/Essence contributions, Tower baselines and raid targets must be recalibrated with it.

An ideal late R1 Rare has budget 108. A low-roll entry R2 Rare has 118×0.94 = **110.92**, only 2.7% more total budget. The R1 item's better trait fit can easily justify keeping it. An entry R3 low-roll Rare has **130.89**, over 21% more than that R1 maximum; broadly useful replacements should normally win by then. Total budget is an illustration, not a damage simulator.

Entering a region should replace the weakest two or three pieces early, replace most of the remaining foundation across the first several days, and allow one or two excellent specialized pieces to bridge into the next region. The target lifespan of a strong drop is the rest of its current region plus roughly the first third of the next; content time must be measured before converting that into a fixed number of days.

Every new region needs new source identities and named choices, not 28 mechanically unrelated base types. Reuse equipment types while changing family emphasis, defensive demands, and a few signatures. An old source may have an explicitly unlocked challenge version that awards a new-region instance of its signature; an owned old item does not scale up for free. These revisits are optional content, not a second mandatory upgrade ladder.

Equipping requires both the existing appropriate character-level gate and a **personal progression clear** granting access to the item's region/band. The entry trial must be beatable using the prior region's ordinary foundation and reasonable Essence/Style choices. Later-band access follows personal progression within that region. A named boss item additionally requires one personal eligible clear of its source. Purchasing, receiving a gift, or borrowing guild gear does not grant these clears.

This intentionally allows wealthy players to buy strong equipment **after** earning access. No tradable design can simultaneously allow a real market and prohibit all purchased acceleration. Region requirements alone prevent cross-region skipping; binding, source clears and scarce specialized supply address the remaining economy concerns.

## 11. Monster and boss loot model

For ordinary combat, initially use **0.004 equipment probability per victorious encounter**, about one item per 250 victories. On a successful roll, allocate 65% to the selected defeated creature's family pool, 25% to the region's general pool and 10% to a region-legal wildcard pool. The wildcard supplies surprise, not early access to future regions. Shared percentages are authoring defaults, not additional drop rolls.

In mixed encounters, select one source creature from the actually defeated roster using a published normalized rule. Default to uniform selection among defeated creatures, with an explicit limited focus weight if used. Do not award one equipment roll per monster or silently multiply the chance for multi-monster encounters. Family attribution must survive into the reward descriptor and loot history.

Give each family two or three preferred bases, two or three emphasized traits and, where worthwhile, one recognizable named object. Individual species can change one preference or carry a signature without requiring 106 bespoke generators. Early examples below use real creature names but **proposed** equipment associations:

| Existing creature/family | Proposed equipment identity | Reason to farm |
| --- | --- | --- |
| Goblin Warrior / martial goblins | One-handed weapons, offhand shields; Power and physical defense | Prepare an attack/guard loadout. |
| Crystal Wisp | Wands with Ability Haste; necklaces with Tenacity or regeneration | Support a caster while respecting each slot's current legal pool. |
| Giant Spider / Blackjaw Spider | Necklaces and light head/leg armor with Tenacity; later, carefully bounded condition support if implemented | Prepare against harmful effects or support an existing condition Essence. |
| Cave Bat / Giant Bat | Relics and medium armor; regeneration and basic-attack support | Improve sustained ordinary combat. |
| Forest Spirit | Staves with Restoration, necklaces with Health/Restoration | Support group healing or a defensive Conduit setup. |

Elite encounters can improve **selection quality**, such as guaranteeing the family branch when the ordinary equipment roll succeeds. Initially keep the same overall item opportunity rate; avoid introducing an elite multiplier before elite frequency is measured. No elite tier classifier was found in the current ordinary gear processor, so this needs an explicit content definition.

Dungeon completions should award **one item**, with a 70% family branch and a visible choice of one of two farm preferences before starting. Proposed rarity weights: Novice U/R/E 65/32/3, Veteran 40/50/10, Champion 20/60/20. Higher difficulties provide better access to flexible items and additional signatures, not 2.5× base stats. This proposal replaces the existing completion/mastery gear chance and the separate room gear rolls; minibosses and treasury rooms improve the run's final selection or provide existing non-equipment rewards, rather than quietly adding multiple independent gear faucets.

Boss signatures use a separate, clearly budgeted reward opportunity: **20% for the selected eligible signature per reward-eligible clear**, with first-copy protection at 15 clears. The signature replaces that activity's ordinary equipment award. Begin with **two opportunities per character per day shared across repeatable boss hunts**, including dungeon final bosses, bankable up to fourteen. A standalone eligible boss clear gives one equipment item whether or not it is the signature; a dungeon completion already gives its one item and is never counted twice. Dungeon completions without a signature opportunity still award ordinary family equipment. This is a server eligibility counter, not a wallet currency; the player selects where to spend opportunities, so an unrelated automatic clear does not consume their saved target attempts. Repeated combat can continue for other rewards. Existing weekly raid limits require separate source-specific tuning; do not apply a fifteen-week first-copy ceiling by copying the ordinary-boss numbers.

The share of gear comes from player choices. A reference day with 24 hours of ordinary victories has an expectation of 34.56 area items. Adding two dungeon completions and two separate standalone eligible boss clears would raise that expectation to 38.56, excluding foundation and first-clear awards; spending those opportunities on the two dungeon completions instead gives 36.56. These are expected supplies, not hard maxima. If activities replace ordinary-combat time, subtract the displaced encounter opportunities. In this illustrative schedule roughly 90% of **item count** is ordinary loot, while bosses/dungeons supply more of the named or deliberately specialized keepers. It is not a required daily checklist.

Raids should offer a small pool of group-role named gear through the existing reward eligibility cadence, initially a choice among three role pools. World Tower should emphasize demonstrated progression: a bound selection at major first-clear milestones and appearances/collection records, with no infinite top-floor equipment faucet. Current raid currencies, Tower Tokens, sigils and Essence materials need not all become equipment-purchase currencies. Do not require raids, Tower and every dungeon for a mandatory complete set.

This remains a proposed acquisition policy. The repository's disabled Tower supply service does not satisfy it; evaluate that code's fit and wire a chosen policy explicitly rather than treating unused chest definitions as released rewards. [E42]

## 12. Target farming

The source browser should answer “where,” “what,” “how likely,” and “what happens if I miss.” Show the actual bases/traits and source gates, not just a rarity icon. Equipment should appear in the existing creature archive alongside Essence information; a unified focus screen can offer separate, explicitly labeled Essence and equipment preferences without silently multiplying both drop systems.

Choose a slot or combat role, then show suitable families. For the chosen family, weight about half its equipment pool toward the desired slot. Using a simplified eight-slot baseline, a 65% family branch and 35% untargeted remainder gives:

`P(desired slot | equipment) = 0.65×0.50 + 0.35×(1/8) = 36.875%`

This is approximately **2.95×** the uniform baseline **when all family-attributed rolls come from the chosen family**, such as a pure-family encounter. In mixed encounters let `q` be the probability that the chosen family receives source attribution; if the other families stay uniform, the result is `0.125 + 0.24375×q`. At `q=0.20`, it is 17.375%, only 1.39× baseline. Actual hand/base category probabilities require authored validation; this is an explanatory single-slot model, not a claim that current loot is uniform across slots. Focus changes **relevance**, not the number of equipment rolls. Source UI must show mixed-encounter odds, and family-targeted encounter access matters to the promised farming efficiency.

A named object belongs to a clear source, with at most a few thematic alternative sources. General bases remain broadly available. This gives the large monster catalog purpose without requiring every monster to own a mandatory unique or forcing permanent low-spawn bottlenecks.

No permanent destination should maximize everything. Some sources favor an Essence, some a defensive item, some a signature, some current-band fundamentals. Deliberate tradeoffs between these goals are healthier than universal magic-find gear.

## 13. RNG and bad-luck protection

Keep randomness in whether ordinary equipment drops, base selection within small pools, rarity, compatible trait selection and coarse trait values. Remove randomness from the core's scalar strength, post-drop enhancement success and item destruction.

Use only two acquisition safeguards:

**A. Personal foundation progress.** One active slot/role target earns progress from victorious ordinary encounters in its eligible region. At **1,800 victories**, grant a bound entry-band Rare of the selected base/role with one selected role trait and one fixed useful companion trait, both at the 80% band. Consume the 1,800 progress and select the next target. Each distinct slot may receive this baseline once per region. A two-handed grant consumes both hand entitlements; use a 3,600-victory requirement for its double budget. No parallel counters award an entire outfit at once.

Eight slot-budget units therefore require 14,400 victories, approximately **40 hours of winning combat** at the existing cadence. Random improvements arrive during that time; nobody needs to claim every baseline. This is the ceiling for filling foundational gaps, not a promised best-in-slot kit. Onboarding should provide a complete basic Common outfit much sooner through quests and starter choices; the current starter/chests do not themselves guarantee full slot coverage.

Progress is a personal acquisition ledger, not a token item or tradable recipe. It cannot be charged in R1 and redeemed in R5, or upgraded by waiting until a later band unlocks. It persists across logout and focus changes within the same region, but only qualifying wins count; failed combat does not. On reaching the selected target's threshold, freeze and grant that baseline once; **bank excess eligible victories in that region**, up to the remaining eight-slot-budget entitlement cost. Selecting the next unclaimed slot can immediately consume that bank. An offline player therefore loses no progress and need not log in every five hours. Changing the selected base does not reroll an earned reward. Grant and counter consumption share one idempotent transaction.

**B. Named first-copy protection.** Select one signature from a source's eligible catalog. Its first copy rolls at 20% per reward-eligible clear; after fourteen failures, clear fifteen grants a baseline bound copy. A natural, bought or gifted copy completes that item's first-copy objective and does not allow an additional free guarantee. Progress is source- and item-specific, never transferred between bosses. Later natural duplicates remain possible, but their exact values have no pity. At two eligible clears/day, the hard ceiling is 7.5 days of opportunities, with banked opportunities supporting fewer logins.

Do not add generic boss coins, duplicate dust, affix extraction, unlimited rerolls, item feeding and collection-stat bonuses on top. Pity is useful for access; a perfect result should remain a long-term optional chase. These two guarantees are not promises of uninterrupted upgrades forever.

The foundation system is milestone acquisition, not crafting: the player earns an already-complete equipment reward through combat, never combines ingredients or develops a half-finished object. Its UI should resemble combat progress and a reward choice, not a workbench.

## 14. Equipment upgrade and enhancement system

**Recommend no general equipment enhancement system at launch.** The dropped item is complete. There is no rank ladder, reroll resource, rarity promotion, blueprint application or Potential replacement.

The function often assigned to upgrades—visible progress during bad luck—is already served by foundation and named acquisition progress. The function of customizing a promising item is served by source/role targeting before the drop. Removing enhancements also reduces hesitation about replacing an old item and removes a reason to hoard inferior salvage fodder.

For attachment, retain favorites, loadouts, source records and earned appearances. A named item's higher-region version comes from new eligible combat, not automatic item growth. If later evidence shows that players lack a satisfying short Cinder goal, first improve equipment trading, cosmetics and existing non-equipment sinks. Do not restore a universal +20% item multiplier merely because it gives currency somewhere to go.

## 15. Unwanted equipment and sinks

Use one ordinary disposal action: **sell to an NPC for Cinders**. Selling destroys the equipment instance. Reuse the existing currency; remove Reinforcement Parts from this loop. No dismantle-vs-sell puzzle, salvage dust, extraction library or feeding system is required.

Price using source band and bounded budget, with a small named premium only if it does not incentivize selling protected finds. Do not pay massive premiums for a color that no longer means higher power. A provisional price could be `20 × 1.18^(region−1) × band` Cinders per single-slot item, doubled for two-handed items; the actual price must be set against measured currency faucets and sinks, not treated as final economy balance.

| Alternative | Decision |
| --- | --- |
| NPC sale | Keep as the only routine equipment disposal; destroys items and gives familiar currency. |
| Salvage / dismantle currency | Remove with reinforcement; no downstream need remains. |
| Extracting modifiers | Reject initially; weakens source identity and creates another permanent progression system. |
| Feeding items into items | Reject; creates a salvage quota and delays using a good drop. |
| Collection turn-ins | Do not consume items for permanent combat stats. Record first acquisition/appearance automatically. |
| Auction House | Keep for valuable unbound items, but it transfers an object rather than destroying it. |
| Guild donation | Keep as a social market exit, with explicit guild retirement. It is not item destruction. |
| Auto-sell | Keep as opt-in rule automation with protected-item priority and receipts. |

Protected items are equipped, referenced by any saved build, favorited/locked, borrowed/guild-owned, reserved in a trade, first named discoveries, and unseen Epic/named items. The same protection policy must cover manual bulk sale, auto-sale, transfer, listing and guild donation. Ownership and location restrictions are hard rules: a personal sale cannot override equipped status, guild ownership/loans or trade custody. An individual confirmation may override a favorite, unread status or saved-loadout reference after explaining and resolving its consequences; a bulk rarity cutoff cannot.

Guild property needs an officer-authorized retirement action with audit records, no personal conversion and no return to unbound stock. Retiring an obsolete guild item gives at most the normal vendor return to the guild treasury. Permanently reusable guild gear helps recruitment but still requires personal source/region eligibility when equipped.

## 16. Idle and offline loot handling

At the proposed frequency, 24 hours of ordinary victories generates about **35 equipment objects**, or about 29 at 85% victory. A continuous eligible **168-hour Noble return generates about 242**, or 206 at 85% victory, before other sources. Current rates give 70 and 59.5 respectively for that weekly case. Historical retention windows can produce still larger catch-up batches. Daily-scale readability alone is therefore insufficient. [E39]

Not every rolled object needs a durable inventory entity. Use a server pipeline:

`resolve award → apply discovery/guarantee rules → apply protection → classify keep/sell → persist kept instances and sale receipts → aggregate summary`

Retain deterministic award IDs and enough rolled descriptor data in an immutable settlement/sale record to audit a result or support a short recovery window. Aggregate thousands of mundane events; do not insert full inventory rows and immediately delete them. Quest/acquisition credit and pity advance on the actual award, regardless of disposition. A sold item must not vanish from source discovery history or be counted twice on settlement retry.

The first version should offer three understandable settings: **Keep everything**, **Keep candidates for my saved builds**, and **Custom keep rules**. Keep everything is the default until the player sees an actual simulated summary of the proposed rule against recent drops. Auto-sale requires explicit enablement. Custom rules prioritize locked items, selected slots/traits and named identities; rarity is an optional supporting constraint, never the sole safe default.

The candidate evaluator is a conservative shortlist, not automatic equipping. It compares every saved build, handles both hands, shows defense/offense tradeoffs, flags new traits and preserves uncertain or incomparable items. A candidate whose behavior the evaluator cannot understand is retained. It must not dispose of a potentially useful lower-rarity item because an Epic has more colors or a larger static rating.

Show a daily summary such as: “8,640 encounters, 7,344 victories; 29 equipment drops; 5 retained candidates, 2 protected discoveries, 22 sold; foundation progress +7,344.” This is an illustrative presentation, not a guaranteed distribution. Provide individual cards for notable finds with their actual rarity, affixes and source; show sale totals separately. Never group different descriptors under the first item with the same base ID.

Aim for **two to eight candidates per eligible day once established**, not an eight-item cap on a weekly return. A 15–25% shortlist of 242 weekly drops yields about **36–60 candidates before protected discoveries**. Provide grouped batch comparison by slot and intended build; safely group strictly dominated like-for-like items, but retain uncertain and incomparable alternatives. First discoveries can raise the count. A first named copy is always retained, and no quota overrides saved-loadout, unread-find or guild protections.

Use paged inventory queries and batched actions. With a hypothetical 10,000 generated objects from imported/high-volume future content, the same pipeline streams rolls, maintains per-build candidates, aggregates sale ledgers and queues protected finds; it does not serialize all objects to the browser. Do not introduce a hard “best 20” cap that destroys incomparable items. Storage limits require explicit user resolution and a protected overflow inbox.

For automatic sales, exact-instance recovery for 72 hours **after the settlement is delivered for review**, rather than the historical kill time, is a reasonable starting policy. Otherwise a weekly returning player could lose recovery before seeing the sale. Recovery requires sufficient Cinders to reverse the credit, consumes the receipt and restores the original bound/unbound descriptor once transactionally; it cannot reroll or double-spend. This is error recovery, not a permanent buyback market. If deferred, auto-sale should initially use stricter rules and never auto-sell unseen Rare/Epic/named gear.

## 17. Auction House and trading rules

Keep ordinary random discoveries tradable until first equip. Keep natural named boss drops tradable under the same rule. Personal foundation rewards and pity-awarded first copies bind on acquisition; they cannot supply the market. **Current random equipment boxes produce bound ProtectedReward equipment**: although the container service passes unbound ownership into construction, the domain coerces non-RandomDiscovery awards to bound ownership. Preserve personal-container binding. Publicly repeatable future rewards follow their explicitly authored supply/ownership policy. [E01, E15, E24]

Equipping binds to the character; moving to another player's inventory does not reset that state. Removing general enhancement eliminates an additional binding trigger. Listing and transfer retain the original source and rolled descriptor. Guild donation permanently removes gear from personal trade; loans do not waive access requirements.

All equip, saved-loadout resolution and guild-borrow/use paths enforce personal region/band access. Named items also require the corresponding first clear. The market should display “usable now,” “region clear required” and “source clear required,” and default search to usable stock while allowing browsing aspirational items. There is no need to block merely buying an unusable item if ownership and future eligibility are clear.

Current limits are ten listings and ten commodity buy orders, rising to **30 each with Nobility**; listing expiry is seven days and seller fee is 3% with a one-Cinder minimum. Preserve this infrastructure initially. Replace Quality/rank/style filters only if the corresponding proposed model is adopted. Add slot, region/band, trait, named identity and usable-now filtering. Static Gear Power must not drive automatic purchasing or disposal. Equipment buy orders are not currently stat-specific; matching and escrow would require additional work. [E24–E25, E39]

Supply over time needs honest treatment:

`ΔunboundStock = tradableDrops + recoveredUnbound − firstUseBindings − unboundNPCsales − guildDonations − otherUnboundDestruction`

AH transactions cancel out of that equation. Currency follows a different equation: vendor proceeds are a faucet, marketplace taxes and other spending are sinks. Removing reinforcement removes a large existing Cinder sink, so the vendor price and broader currency economy must be recalibrated together.

For illustration, 10,000 daily farmers at 34.56 random items/day generate **345,600 items/day** before victory/activity reductions. Even a 5% share retained unbound is 17,280 objects/day. Most ordinary prices will fall toward vendor value; this is expected. Binding and a realistic power ceiling do not guarantee that unused perfect items stay expensive forever.

Do not solve this by exponential power inflation, item expiry or a disguised durability tax. Make the economy useful for access and interesting combinations, accept mature-region price compression, and monitor unbound stock, actual equip-binding rates, sale/destruction rates, listings, transaction concentration and currency supply. Scarce source opportunities and varying encounter needs can preserve some signature demand; no perpetual high price is promised. Multiple-account farming remains an account/economy concern beyond equipment rules.

## 18. Sets, uniques and chase items

Support ordinary and affixed equipment, and a restrained catalog of **named Rare/Epic items**. Each named item has a source, a recognizable appearance, one purposeful fixed trait and at most one conditional behavior. It is not universally stronger than an ordinary well-fitted item.

Do not launch new statistical sets. Existing four- and six-piece engine effects should leave the equipment progression model. A visually themed collection can span multiple objects without a combat bonus. If a later two-piece interaction is proven worthwhile, it must consume the combined items' trait budget, remain optional, and fit within the same effect caps; that is deferred design work, not part of this recommendation.

Use chase content in three forms: an excellent fit with high affix bands, a signature for a specific matchup, and a very rare appearance of an already attainable power item. Cosmetic chase variants can be approximately 1/500 eligible signature-source opportunities as a trial value. Do not attach an exclusive mandatory combat mechanic to a 1/50,000 idle drop.

A named defensive shield that grants a small opening Barrier should be useful for short burst encounters and less impressive in long attrition. A condition-support relic should amplify conditions already supplied by Essences, not apply a free condition engine. A healing staff should help an existing healing plan without giving every build a new healing ability.

## 19. Interaction with Essences

Essences own active abilities, passives, important attributes, targeting patterns and most specialized combat tools. Equipment owns baseline Power/Health/mitigation and a narrow ability to tune the effectiveness of the selected plan.

An Essence should still determine whether the character can poison, heal allies, summon, cleanse or control. Equipment may improve a relevant existing attribute or one bounded aspect of that behavior. It does not grant a substitute Essence, extra attunement slots, duplicate passives or free casts. Do not let a single signature make several otherwise irrelevant Essences mandatory.

Start with already measurable interactions: Restoration, defensive rating, Attack Speed, crit and Ability Haste. Restoration affects healing, regeneration and barrier support, so value its whole contribution rather than pricing it as a heal-only bonus. Elemental damage, summon support and condition amplification are later possibilities, not existing universal gear stats. They need a shared effect contract and tests; avoid per-Essence hardcoded bonuses. [E38]

Gear and Essence farming should sometimes align and sometimes compete. A player may farm Crystal Wisp for its Essence while accepting a lower chance at a physical shield; another family offers the reverse. The archive should make that tradeoff visible. Equipment focus must not accidentally reuse the existing Essence Focus 3× drop multiplier on equipment; the systems have different probability budgets. [E32]

## 20. Interaction with Doctrines / Combat Styles

| Actual Combat Style | Its ownership of build identity | Equipment's supporting role |
| --- | --- | --- |
| Bastion | Healing split into Health and Barrier, with its own refinements and mastery | Health, defensive rating and Restoration; verify no double application through healing-to-Barrier conversion. |
| Conduit | Channeled first Essence and Charge consumption from other casts | Power, Restoration and bounded Ability Haste; no extra Charges, free casts or automatic first-slot replacement. |
| Reaper | Harvesting existing Bleed/Burn/Poison ticks with active damage | Sustain and modest condition support once defined; no extra harvest triggers or self-propagating condition loops. |
| Duelist | Read generation and consumption for a stronger damaging Essence | Attack speed/crit where useful; no reduction of the Read threshold or extra Read-on-proc mechanics. |

This division preserves meaning in choosing the Style. Equipment can make a plan safer or better suited to an encounter; it does not rewrite its trigger economy. Gear-only bonuses should aggregate additively where possible, while Doctrine-owned conversion mechanics retain their own clearly defined stage. In particular, healing-to-Barrier conversion, Reaper payouts and Conduit amplification need tests to prevent one gear bonus being applied twice.

Cap gear-origin conditional modifiers across the full loadout, and simulate combinations with multiple Essences and all four Styles. Testing an item only against a basic attacker would miss its strongest interactions. Comparison should show the selected activity/loadout and label unmodeled behavior rather than imply that a single rating has evaluated it.

## 21. Concrete example items

These are **proposed items and source associations**, not existing drops. Budgets follow the proposed rarity model, not today's universal 70/30 split. Where a core is quantified, it uses release 4 exchange rates; the values are not production-ready. A point allocation is an internal design measure, never a player currency. Conditional trait prices require simulation and are not claimed equivalent to a damage percentage.

| Example | Anatomy and intended decision |
| --- | --- |
| **Groveguard Helm** — Common, late R1, Heavy Head | Budget 108. Using the present Heavy profile and exchange rates: approximately 234 Health, 36 Armor Rating, 36 Resistance Rating. No affixes. A readable foundation and possible defensive alternative to an overly offensive Rare. Proposed martial/forest general pool. |
| **Wisp-touched Wand** — Uncommon, entry R2, one-handed | Budget 118; core 94.4, one Ability Haste trait allocated 23.6 points. At the 90% band it realizes 21.24 points, total 115.64. A concentrated specialization; a three-affix Epic may provide less of that secondary. Proposed Crystal Wisp family/challenge source. |
| **Blackjaw Gorget** — Rare, late R1, Necklace | Budget 108; core 75.6; two 16.2-point traits: Tenacity at 100%, regeneration at 90% = 14.58. Total 106.38. Supports surviving harmful effects without granting Poison. These traits are legal on a necklace; the original ring example was incompatible with current slot rules. Proposed Blackjaw Spider source. |
| **Meran Scout Mail** — Epic, entry R2, Medium Chest | Budget 118; core 82.6; three 11.8-point traits at 85/95/100%: Armor, Resistance and Restoration. Total 115.64. Current chest legality supports these; the original regeneration/crit example did not. A focused Rare can devote more to its two essential traits. Proposed regional martial pool, requiring authored family assignment. |
| **Garran's Gateward** — named Rare, late R1, offhand shield | Budget 108; 70% core, 15% fixed signature, 15% resistance trait. Signature proposal: an opening Barrier worth a calibrated multiple of this shield's own Health contribution, lasting at most six seconds, once per encounter. Apply a shared equipment-origin cap; never scale the old shield's barrier from total character Health. No refresh/proc loop. Proposed Garran source; effect magnitude and its 15% cost require short/long-fight value tests. |
| **Heartwood Mercy** — named Rare, late R2, two-handed staff | Budget 254.88; core 178.416, fixed Restoration trait 38.232, Ability Haste trait 38.232 before roll. Both are legal weapon specialization attributes. No free heal. Supports existing healing Essences or Conduit. Proposed Great Tree signature; requires a personal source clear. |
| **Morrowmaw's Memorial** — cosmetic chase appearance | Same rolled power and requirements as its attainable underlying named item. Records the source and changes appearance only. It is an optional collection goal, not a raid requirement. |

The staff replaces both hand slots, so its budget and comparison use two units. Current set thresholds also count two occupied slots; the proposed model removes those sets rather than changing that rule silently. Cosmetic appearance cannot bypass type restrictions. Source, binding, access, trait bands and “used by loadout” status appear on every relevant item card.

## 22. Example player progression

Region 3 onward is hypothetical future content. Time estimates assume the proposed rate and healthy victory rates, not the current sparse ordinary drops.

| Stage | Goal, farming decision and upgrade meaning | Cadence and unwanted gear |
| --- | --- | --- |
| Early R1 | Onboarding supplies a complete basic Common outfit with a weapon choice. Player farms accessible goblins/nearby families for one suitable offensive or defensive secondary and their first Essences. An upgrade may fill a missing role rather than increase rarity. | Quest improvements within the first play session; roughly 2–5 random useful improvements/day while many slots are weak. Sell clearly obsolete duplicates after previewing keep rules. |
| Late R1 | Choose an actual Style/Essence plan. Farm a shield, resistance item or focused weapon, then one optional signature such as Gateward. Replace current four-piece-set expectations with individual decisions. | Around one random improvement/day in a partially established outfit; guaranteed baseline clears remaining bad slots. Strong Rare/Uncommon pieces remain relevant. |
| Entering R2 | Earn access using R1 gear. Replace the weakest core pieces first; retain the excellent Blackjaw Gorget against harmful effects. New family pools offer different defensive and support opportunities. | Two or three early replacements across initial sessions, then partial refresh over several days. Foundation progress avoids an unlucky missing-slot stall. |
| Late R3 | Hypothetical full role loadout; farm a precise family for a desired two-trait Rare or a second defensive set of individual items. A useful upgrade changes win reliability or resolves a specific weakness. | Focused random upgrades every few days; one or two carried R2 pieces may remain. Auto-sale handles known low-fit bases; saved-build gear stays protected. |
| Midgame, R4–6 | Maintain two or three activity loadouts. Seek different mitigation/sustain profiles and a signature supporting a chosen role. Revisit an unlocked challenge source when its specific item is useful. | New-region foundations improve regularly; within-region optimization about every 2–7 days with deliberate targeting. Gear goals coexist with Essence progression. |
| Late game, R7–9 | Prepare for particular raid/Tower demands using alternative individual slots. Higher difficulty gives more specialized choices, not mandatory six-piece sets. | Strong items usually last through the current region and part of the next. A lower-rarity concentrated trait can remain deliberate. |
| R10 | Finish regional baseline gaps, then pursue high-fit Rare/Epic pieces and named encounter options. There is no new Quality/rank ladder revealed after collecting gear. | Foundation stops RNG blocking access; exact optimal combinations take weeks. A nearly complete outfit may see no meaningful random improvement for many days. |
| Endgame farming | Refine matchup loadouts, hunt a cosmetic signature, trade an unbound find, help guild recruits and tackle harder encounters for achievement rather than endless item-level inflation. | Near-perfect improvements can take one or two months. The game needs encounters, Essence/Style goals and collection interest during that interval; equipment alone cannot supply infinite meaningful progression. |

“Meaningful upgrade” should mean a useful change in an intended loadout: measurable win reliability, survival through a known burst, a new viable role, or a relevant offense/sustain improvement. A 0.1% point increase without a gameplay consequence is not counted as the design's upgrade cadence.

## 23. Illustrative balance numbers and scenarios

### Current baseline versus proposal

Current idle configuration gives 360 encounters/hour and approximately 8,640 per day; an inclusive due endpoint can change an individual settlement's count by one. Ordinary combat averages 1.971 monsters/encounter outside Lumo, assuming its authored spawn distribution; Lumo averages 1.032. Expected kills per victory and wins per attempted encounter must not be confused. For a worked **1,000-kill** example, assume all kills are from complete victorious encounters and use the ordinary 1.971 average. Failed fights with partial kills would change the conversion. [E08, E12]

| Scenario | Current equipment, 1/864 per win | Proposed equipment, 0.004 per win |
| --- | ---: | ---: |
| 1,000 victories | 1.157 | 4.000 |
| 1,000 kills in a typical current area | 0.587 | 2.029 |
| 24 hours, all victories | 10.000 | 34.560 |
| 24 hours, 85% victory | 8.500 | 29.376 |
| Continuous eligible 168 hours, all victories | 70.000 | 241.920 |
| Continuous eligible 168 hours, 85% victory | 59.500 | 205.632 |

The proposal increases equipment frequency **3.456×**. At the present rate, the chance of seeing no area Rare in a perfect day is about 74%; a particular Rare armor archetype still averages roughly 85.7 continuous winning days before Quality/style suitability. Requiring one specific current profile adds another factor: five alternatives on Head/Chest yield about **428.6 days**, four on Legs about **342.9 days**. Specialization improves variety, but also makes exact targeting more important. The weekly baseline is 59.5 Common, 8.4 Uncommon and 2.1 Rare on average at perfect victory; it is not a drop guarantee.

Proposed ordinary rarity counts:

| Scenario | Common 55% | Uncommon 30% | Rare 13% | Epic 2% |
| --- | ---: | ---: | ---: | ---: |
| 1,000 kills, ~507.36 wins | 1.116 | 0.609 | 0.264 | 0.041 |
| 1,000 wins | 2.200 | 1.200 | 0.520 | 0.080 |
| 24 hours, all victories | 19.008 | 10.368 | 4.493 | 0.691 |
| Continuous eligible 168 hours, all victories | 133.056 | 72.576 | 31.450 | 4.838 |

For hypothetical R3 with the same encounter structure, 1,000 kills therefore produce about two objects. If 35% are relevant to currently wanted slots, 45% of those suit the build, and 15% of those improve it, the expected random upgrades are:

`2.029 × 0.35 × 0.45 × 0.15 = 0.0479`

About **0.71** match wanted slots, **0.32** also fit the build, and around **0.4–0.6** deserve inspection under a conservative shortlist that also retains unfamiliar traits. Most such 1,000-kill sessions will contain no meaningful upgrade. This is a small, roughly 85-minute sample at perfect victory, not a full idle day. It also earns about 507 foundation progress, without necessarily crossing a milestone.

At 8,640 wins/day, the same established-state assumptions produce approximately **0.816 random upgrades/day**. New-region improvements, deterministic foundation rewards and signature drops are additional sources; do not silently include them in the random rate.

### Upgrade-frequency sensitivity model

Let `r = P(wanted slot) × P(build fit | slot) × P(meaningful improvement | fit)`. Then:

`expected random upgrades/day = 8,640 × victoryFraction × 0.004 × r`

`mean hours per random upgrade = 1 / (360 × victoryFraction × 0.004 × r)`

| State | Slot / fit / better assumptions | Upgrades/day, all wins | Mean interval |
| --- | --- | ---: | ---: |
| New-region weak outfit | 0.60 / 0.60 / 0.35 | 4.355 | 5.5 hours |
| Established outfit | 0.35 / 0.45 / 0.15 | 0.816 | 29.4 hours |
| Strong outfit, focused source | 0.36875 / 0.35 / 0.05 | 0.223 | 4.5 days |
| Near-perfect, focused source | 0.36875 / 0.25 / 0.005 | 0.0159 | 62.8 days |

These fit/better probabilities are **explicit scenario assumptions**, not measurements and not a simulation demonstrating actual item balance. The two focused rows assume pure target-family attribution (`q=1`); mixed-roster farming uses section 12's lower slot probability. Within a real run fit/improvement chances decrease as items improve and may be correlated. At 85% victory, rates multiply by 0.85 and intervals divide by 0.85. The table shows the conditions the content/pools must achieve, and why an uncontrolled many-affix tail would be unacceptable.

For a practical initial shortlist, retaining 15–25% of the day's ~35 objects produces about 5–9 candidates; a seven-day batch produces **36–60** before protected discoveries and named loot. Use the existing comparison-observation foundation to measure useful choices, replacement ages and review effort, then add missing acquisition/disposition telemetry. No destructive quota should force every return into the daily count. [E38–E39]

### Random tails and deterministic ceilings

With `p=0.004`, an equipment inter-arrival time has mean 250 victories. Median is about 173 victories, and about 748 victories give 95% chance of at least one item. The probability of no item in 1,000 victories is `(0.996)^1000 = 1.82%`; in the ~507 victories represented by 1,000 ordinary kills it is about 13.1%.

At 1,800 wins, expected random equipment is 7.2 and the probability of receiving none is about 0.074%. The foundation guarantee is therefore chiefly **slot/role protection**, not protection against seeing absolutely no gear. A single-slot target takes five winning hours; eight slot-budget units take forty. Do not advertise a one-item-per-five-hours guarantee as eight simultaneous slot guarantees.

For a 20% signature probability with hard first-copy guarantee at clear 15:

`E[clears] = Σ(i=0..14) (0.8)^i = (1−0.8^15)/0.2 = 4.824`

Only `0.8^14 = 4.40%` of players reach the guaranteed final attempt. At two eligible clears/day the mean is about 2.4 days of opportunities; the cap is 7.5. These values are for the **selected item**, not an unspecified item from a boss pool. If the roll first selects among five signatures, those numbers would be wrong; the source selection contract must prevent that extra hidden RNG layer.

For two desired affixes each with five equally likely value bands, a perfect-value pair occurs once per 25 correctly composed items on average. Exact trait selection and the item's source determine the real chase time. No promise of universal “perfect gear in 25 drops” follows.

### Currency and item lifespan checks

With a provisional R3 single-slot vendor price of about 27.85 Cinders, selling 80% of 34.56 daily ordinary drops yields about **770 Cinders/day before two-handed weighting**, in addition to current currency rewards. This is merely a faucet estimate; deleting today's 345,650-Cinder single-slot T1 reinforcement ladder (**691,300 for a two-hander**) changes demand much more substantially. Model both sides before choosing a sale price.

An excellent item bridging one region is supported by the 2.7% old-max/new-min budget gap. An old item persisting through several regions would indicate overpowered flat-percentage traits, an underpriced signature, inaccessible alternatives, or an incorrect source-band curve. Track actual replacement ages by slot and build. Do not respond automatically by increasing every region's budget.

## 24. Technical changes required

### Reusable

| Existing system | Reuse and limits |
| --- | --- |
| Equipment instances and frozen descriptors | Preserve stable IDs, evaluated stats, source/ownership, snapshot restoration and row-version concurrency. Change anatomy rather than creating a second permanent item implementation. |
| Domain budget allocator and attribute metadata | Retain the implemented 70/30 core/specialization structure, slot legality, rules 18 units/caps and occupied-slot weighting. Extend to proposed rarity-specific shares and rolls only if adopted; recalibrate the curve through simulation. |
| Versioned catalogs and migration tooling | Retain release registry, exact-version evaluation, audited previews, receipts, rollback guards and the finite specialization transition. Extend them for any approved descriptor change. |
| CQRS/MediatR command pipeline | Keep command transactions, service interfaces, request IDs, receipts and existing response/state-sync patterns. |
| Combat reward settlement | Preserve encounter/run identities, frozen rewards, original earned-time handling, batching and retry semantics. |
| Inventory, equipment slots and loadouts | Reuse exact-instance placement and two-hand deduplication. Expand protection and personal-access checks. |
| AH, transfers, guild loans, ledger and provenance | Reuse custody and audit infrastructure. Add consistent access/protection policies and guild retirement. |
| Comparison and snapshots | Reuse implemented activity-aware projection, Essence cooldowns, raw/effective/over-cap metrics, budget disclosure, snapshots and comparison observation. Add candidate support for market stock and explicit new-signature limitations where needed. Do not duplicate arithmetic in Angular. |

### Refactor

**Domain models.** Evolve the existing equipment-specific rarity contract, core/specialization definitions and versioned frozen descriptor. Add independent affix rolls and source/band requirements only for the approved design. Preserve stable IDs, version resolution and legal slot pools. Put personal eligibility, protection and disposition decisions in Core domain/application policies. Move touched placement decisions out of `EquipmentSlotRepository` when this feature requires it; avoid an unrelated architecture rewrite. Core must not depend on Infrastructure. [E01–E03, E19, E36–E38]

Proposed service concepts, not implemented APIs:

- A canonical equipment generator accepts an authorized source context, eligible catalog version and server random stream, and returns a frozen candidate. It cannot accept arbitrary client-provided final stats or future-region overrides.
- A reward disposition policy evaluates the candidate, current saved-build protections and filter version, returning Keep or Sell with a reason.
- A progression policy awards personal foundation/signature entitlements from eligible encounters/clears.
- A placement policy evaluates level, personal progression access, handedness, ownership and guild loan availability.

**Database.** Extend the already versioned equipment JSON descriptor and conversion receipt contract. Add or reuse indexed queryable fields for region/band, rarity, base, named identity and ownership; retain the descriptor as authority. For new AH affix filtering, choose a projection table or JSON indexes from actual PostgreSQL query plans and volume. Existing versioning/migration infrastructure is not an absent prerequisite. [E27, E36, E40]

Add per-character/per-region foundation progress and consumed slot entitlements; per-character/source/signature first-copy progress; banked reward-opportunity timestamps if that cadence is selected; versioned filter preferences; and idempotent sale/recovery receipts. Counters and award receipts need uniqueness constraints on their complete logical keys, plus concurrency protection. Extend existing discovery/history storage where it already fits instead of creating duplicate counters for every UI card.

**Loot content and generation.** Replace area-wide anonymous selection with a validated mapping from creature → family → equipment pools, with explicit base and affix eligibility. Preserve one roll per victorious encounter. Move dungeon completion, named bosses, chests, quests, events, Tower selections and administrative grants through the same typed equipment boundary. Reject generic equipment-base rewards that would invoke `InventoryItemFactory`'s descriptor-less fallback. Reference/canonical builds can use the same evaluator with explicit simulation contexts; they never count as player acquisitions. [E08–E17]

**Combat integration.** Add only a small shared signature-effect adapter where current attributes are insufficient, with source eligibility, exclusions, caps and balance versions. Preserve reproducible loadout snapshots. The character-level defense discontinuity is already resolved; retain item-tier normalization unless a separately reviewed combat change is justified. Recalibrate creature scaling, canonical builds and dungeon/raid/Tower forecasts if the proposed budget curve or rarity model changes. [E03, E22–E23, E33, E38]

**Reward transactions.** A settlement must atomically record the award identity, update personal progress, create a kept item or sale receipt, apply Cinders, record discovery/quest progress and enqueue state/history notifications. Replaying the same award must not grant both the sold and retained versions. Saved filters and content versions must be stable for an already computed reward; changing a filter cannot reroll an old batch or change a committed outcome. Pending offline combat must settle with the equipment state that earned it.

**API/application contracts.** Retain equip/unequip, linked-item, loadout, activity-aware comparison and comparison-observation routes. Add source/target queries, target selection, filter preview/update, paged equipment queries, batch sale and optional recovery. Use `ICommand<T>`/`IQuery<T>`, focused DTOs, Application mapping and repository interfaces; repositories do not become alternate gameplay engines. Retire upgrade/dismantle/variant endpoints only if the corresponding systems are removed after an approved cutover. [E19–E21, E35, E38]

**Frontend.** Revise `equipment-progression.ts`, equipment DTOs, equipment API/state services, display cards, comparison modal, inventory sorts, marketplace filters, chat links, loadouts and session summary together. Show a concise core/traits/source/requirements layout. Remove Quality, rank, blueprint and set-progress panels. Add safe keep-rule preview, source browsing, guarantee progress, saved-loadout protection reasons and notable-find cards. AdminDashboard's base-item editor is not a full equipment/affix catalog authoring tool; either extend it with validated catalogs or continue using validated source-controlled JSON. [E28–E31, E35]

The preceding removals are conditional on adopting the proposed item model. Follow the frontend's **Grimoire rules for new or intentionally migrated screens**, using the existing New look preference and shared primitives. Inventory and Auction House are still legacy screens; migrate a complete chosen screen rather than mixing design systems. Reuse the richer comparison and receipt-backed migration chooser where relevant. Weekly summaries need actual instance cards and retention-history-aware wording, not only the current nominal Nobility hour label. [E41]

**Existing consumers requiring explicit updates.** Quests reading `PlainEquipmentEntitlement`, raid styled-armor checks, dungeon preview rewards, event/selection boxes, guild loan availability, marketplace listings, character overviews, power-rating fingerprints, combat snapshots, loot history, support/admin snapshots, chat links, favorites/new-item actions and activity mutation boundaries must all understand the new descriptor. A rename of the equipment screen will not complete this migration.

### Validation needed before future implementation ships

Use invariant and behavior tests rather than assertions that duplicate the generator. Check fixed total budget, no illegal/duplicate traits, equal hand budgets, legal source access, lower-rarity viability, no double application of healing/condition/Doctrine modifiers, complete source coverage and reproducible generation.

Run deterministic scenario comparisons for all four Styles, offensive/defensive/support Essence loadouts, all available regions and planned region checkpoints. Include short burst, attrition, multi-target, status-heavy and group encounters. Validate replacement lifetimes and core-vs-affix tradeoffs. Static budget scores alone cannot price conditional effects.

Test concurrent claim/retry, duplicate settlement, auto-sale versus favorite/loadout changes, sale recovery versus spending, source/band access through transfer/AH/guild loans, snapshot restoration, sold-award quest credit and no retroactive loadout mutation. Load-test **24-hour, continuously covered 168-hour and mixed historical retention windows**, plus larger inventories. Existing opt-in PostgreSQL equipment/startup rehearsals provide a starting point; extend and run them on a disposable database. In-memory tests do not establish PostgreSQL locking correctness. [E39–E40]

## 25. Systems and code to delete

**Conditional on choosing the proposed redesign**, remove superseded active feature slices after migration and reference checks. This is not a list of already-approved deletions:

- Rank reinforcement and dismantling logic in `EquipmentUpgradePolicy`, `EquipmentUpgradeModels`, `EquipmentUpgradeService`, `EquipmentUpgradeRepository`, their interfaces/commands/DTOs, upgrade price JSON and player panels. Extract any still-needed generic receipt/locking code into its actual surviving feature before deleting it.
- Consumable blueprint catalog/options/progress/repository/service paths, `ApplyEquipmentVariant`, blueprint item definitions, dungeon blueprint rewards/pity, conversion panels and associated quest rewards. Replace all source references; do not leave dangling quest/dungeon outputs.
- Quality, rank, global-roll and native/active-style allocation fields in the next equipment descriptor and related UI/API/search contracts. Affix IDs and named identity replace their intended new functions; do not carry a hidden old multiplier.
- Active set membership and four/six-piece equipment engine effects, associated granted abilities and set-progress UI. Remove a granted ability only after checking it has no Essence or other legitimate consumer.
- Raid `RequiresBlueprintArmor` validation and rarity/style gates; replace with the chosen personal competence/access contract.
- Descriptor-less equipment creation through generic rewards; old runtime fallback branches only after the data inventory and conversion are complete.
- `EquipmentAttributeRules` is a candidate for dead-code removal: no callers were found during this audit. A final reference/build check must precede deletion.

Potential, Tempering, crafting and gathering are **already deleted feature work**. Do not reopen them or count deleting historical descriptions as a gameplay milestone. Preserve applied EF migrations. Also preserve catalog releases, evaluation rules, receipts and historical snapshot support still referenced by stored data or supported rollback. Versioned history is a correctness requirement, not automatically dead code; remove a version only after an explicit reference and retention audit. Mark superseded design prose clearly. [E36, E40]

## 26. Migration strategy

No migration is generated or applied in this review. **Substantial migration tooling already exists**: release-specific previews with source/result hashes, idempotent apply receipts and revisions, latest-only rollback with changed-item/used-choice guards, pending reward conversion and a one-time specialization allowance. LiveOps exposes audit/preview/apply/rollback. The existing `VersionEquipmentRebalances` EF migration stores this infrastructure. These are foundations to extend, not evidence that the proposed loot model has already been migrated. [E40]

The configured startup converter targets equipment release 4 with attribute rules 18. It uses a lock, batches and per-item transactions, resumes remaining work, refreshes Arena defenses, and audits unsupported pending/active historical tournament payloads. It intentionally **does not pause or settle scheduled combat**. Retained historical references remain supported. Do not start the API as a read-only documentation check: startup is configured to mutate equipment. [E36, E40]

For a later redesign, distinguish disposable development worlds from retained player data. A historical Alpha reset allowance does not authorize deleting today's state.

**For disposable development/test worlds**, rebuild equipment test data directly in the new model through the normal development process. This is the simplest development path, but it is not permission to reset a shared environment.

**For retained player data**, plan a finite active-item cutover using the existing preview/receipt/version framework, while retaining the historical versions still needed for audit, snapshots and rollback:

1. Inventory all current equipment stores: player inventory, equipped slots, saved loadouts, AH custody, guild vault/loans, pending dungeon rewards, combat snapshots, administrative/support copies and any embedded reward payloads. Count descriptor-less and malformed items explicitly.
2. Prepare a mapping preview by actual archetype, tier, useful stat profile and ownership. Preserve usable roles and stable instance identity when possible. Quality/rank cannot be translated into “the same affixes” mechanically; define a bounded normalization policy and show representative old/new character results. Do not invent boss-clear credit merely because an old item has a matching style.
3. Evaluate legitimate regional progress already earned from durable quest/clear records. Backfill access only from that evidence. If a former owner cannot use converted gear, give an explicit bounded transition entitlement or baseline replacement, not a universal bypass that new purchases inherit.
4. Resolve listings and pending claims under a declared release boundary. If blueprints/parts are removed, cancel/refund obsolete orders and return valid equipment with its original ownership. Completed trade history remains history. A major loot/set redesign needs an explicit policy for pending combat and old effects; unlike today's release-4 conversion, it may need a drain or pause. Do not silently alter already-earned snapshot semantics.
5. Convert retained objects once, preserving IDs/ownership/favorites and repairing loadouts. Extend current receipts and validation for the new mapping. Quarantine unknown descriptors for a decision; keep exact-version resolution wherever retained data still requires it. Preserve recorded legacy ownership rather than treating every imported object as a fresh protected reward.
6. Retire Reinforcement Parts and blueprint inventory through one documented transition policy. Prefer a bounded one-time credit or equivalent regional baseline reward supported by actual records. Decide explicitly whether historical Cinder spending receives any credit; do not create an uncapped refund faucet from inferred rank values. Bound deterministic replacement rewards to avoid a migration-created market flood.
7. Recompute search projections, validate custody totals, compare before/after character roles, verify that no item is simultaneously listed/equipped/owned twice, and reconcile monetary adjustments against a migration ledger.
8. Once every affected retained item and pending payload is resolved, remove obsolete active tables/columns/branches and endpoints. Retain receipts, source history and the historical evaluators/catalogs required by the declared snapshot and rollback policy.

Rehearse this sequence on a disposable database copy, including rollback and changes made after conversion. Current receipt guards are valuable but do not make arbitrary future affix/set conversion safe automatically. If combat payloads depend on old sets, drain them or explicitly retain versioned resolution for their supported lifetime. Historical support should have a documented lifecycle; it should not be removed merely to simplify the new schema.

This will require coordinated backend content/API and frontend releases, and likely worker updates. Normal deployment/migration approval remains a later operational step. Nothing in this analysis deploys services or modifies infrastructure-as-code.

## 27. Recommended implementation phases

| Phase | Deliverable | Exit condition |
| --- | --- | --- |
| 1. Validate design with current combat | Small representative base/affix/named catalog and offline simulation inputs; economy/drop-volume model; settle region-access rules. | Common/U/R/E tradeoffs, two-hand fairness, four Styles and early/late encounters behave as intended. Select actual desired upgrade cadence from evidence. |
| 2. Extend the canonical model and read projections | Evolve versioned descriptors, current allocation/legality, source/access/protection policies and existing migration previews; extend comparison only where needed. | Every ingress constructs and explains a legal item; retained data has a reviewed conversion and historical-version policy. |
| 3. Implement acquisition and guarantees | Region/family drops, dungeon final reward, foundation and signature progress with idempotent settlement. | Probability tests and retry/concurrency tests pass; no hidden extra room/boss equipment faucets; all eight slot-budget units have achievable sources. |
| 4. Complete safe inventory and market UX | Source browser, current comparison extensions, pagination, saved-build protection, batch sale, grouped offline finds and AH/guild rules, following Grimoire screen migration policy. | Daily, weekly and mixed-window catch-up batches are understandable; protected finds survive; transfer/use paths enforce eligibility. Auto-sale waits for its safety criteria. |
| 5. Migrate and remove old systems | Data reconciliation, pending-run drain, economy transition, obsolete contract removal and documentation update. | No active Quality/rank/blueprint/set dependency remains; historic migrations preserved; all retained custody and balances reconcile. |
| 6. Author and expand progression | Regional source matrices, optional signatures and challenge versions; R3–10 content in independently playable increments. | Each region has functional baseline access, meaningful farm alternatives and measured replacement times; no missing-price/catalog failure like the current T3 gap. |

For a solo developer, ship the small affix library and a few signatures before a large catalog of conditional effects. Use family-level data to give the monster roster identity. Do not build sockets, affix crafting, a general loot scripting language and a new equipment profession in anticipation of hypothetical later needs.

Phase 1 can reject or adjust this proposal without wasting migration work. In particular, equal-budget Epic breadth must be tested for desirability; if it feels unrewarding, improve trait combinations and messaging before restoring large rarity multipliers.

## 28. Open design questions

These questions affect final balance/content, but none prevented this analysis or requires preserving the old system by default:

1. What is the intended elapsed duration of each region and the expected daily victory fraction? The numerical proposal assumes long-running idle play and illustrates both 100% and 85% victory.
2. Which personal milestone should grant region/band equipment access, and how can each be cleared using prior-region baseline gear? This must replace raid composition gates without circular requirements.
3. Does a two- or three-trait Epic feel desirable when a focused Rare can win? Validate against actual Style/Essence builds and the redesigned jewelry cores.
4. Should the daily named opportunities be shared across a character, a source category or individual families? **Recommended default: two shared signature opportunities/day across repeatable boss hunts, banked to fourteen**, so players do not inherit a per-boss daily checklist. Existing dungeon/raid eligibility should substitute where appropriate, with one consistent supply budget.
5. How much acceleration through the AH is acceptable after personal access? The recommendation permits it and does not pretend binding removes the value of wealth.
6. Which current players/data must be retained? The answer determines the transition policy, not the future item's gameplay rules.
7. What Cinder sink replaces the demand lost when reinforcement disappears? Measure the broader game economy before approving vendor returns; do not automatically replace one busywork system with another.
8. How much equipment-specific effect infrastructure is justified? The first release can use ordinary attributes and only a few carefully tested named behaviors.
9. Should auto-sale ship with exact-instance recent recovery, or launch later after manual batch sale? Recommended: protect first, then automate; none of the core acquisition design depends on enabling auto-sale immediately.
10. How much permanent guild equipment stock is desirable? Keep loans useful for onboarding, enforce personal access, and provide audited retirement.

## 29. Final verdict

LegendsLegacy now has a stronger equipment foundation than the original report described: stable instances, source-aware rewards, ownership, activity loadouts/comparison, legal core/specialization allocation, rules 18 attributes, versioned evaluation and audited conversion. Reuse this work. The defense cliff and missing activity comparison should no longer appear as open implementation gaps.

Current specialization improves individual choices, but rarity, Quality, investment and whole-item roll still overlap; monster sources remain weakly differentiated and exact desired gear is poorly protected. The styled-Epic raid armor gate remains the clearest production-era assumption. Inventory safety and longer offline returns are immediate concerns independent of whether the larger redesign is accepted.

The recommended next direction remains **monster-targeted bounded loot with personal baseline and signature guarantees**. Test the proposed rarity/enhancement simplification against the improved current system before committing to removal. Let named gear add restrained source identity and Essences/Combat Styles retain the larger combat decisions. Design review tools for the player's whole retained reward window.

The success criterion is a player who can explain what they want and why they are farming that source, remain functional through bad luck, and understand the equipment choices awaiting them after either a daily or weekly return.

## Evidence index

Links point to this reviewed workspace. Each ID covers a related source chain; current C#/JSON takes precedence over historical prose.

| ID | Concrete sources and audit use |
| --- | --- |
| E01 | [Equipment slots](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Slots/EquipmentSlotType.cs), [EquipmentInstance](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/EquipmentInstance.cs), [EquipmentState](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentState.cs), [EquipmentData](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentData.cs). Identity, ownership and frozen evaluation. |
| E02 | [EquipmentEvaluator](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentEvaluator.cs), [EquipmentBalance](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentBalance.cs), [starter/base catalog](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-starters.v4.json). Budget, scalar factors and authored base profiles. |
| E03 | [Tier curve](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentTierBudgetCurve.cs), [stat costs and conversion](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentStatBudgetCatalog.cs), [AttributeCalculator](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Components/Attributes/AttributeCalculator.cs), [combat caps](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Attributes/AttributeCombatRules.cs). |
| E04 | [Named definitions](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-named.v4.json), [styles](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-styles.v4.json), [sets](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-sets.v4.json), [set resolver](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Sets/EquipmentSetBonusResolver.cs), [catalog expansion](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/JsonStarterEquipmentCatalog.cs). |
| E05 | [Live drop profiles](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-ordinary.v1.json), [upgrade prices](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-upgrades.v1.json), [upgrade prices/returns model](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentUpgradeModels.cs). |
| E06 | [Upgrade policy](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentUpgradePolicy.cs), [execution service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/EquipmentUpgradeService.cs), [repository](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Equipments/EquipmentUpgradeRepository.cs), [execution tests](C:/repos/Legends-Legacy/legends-legacy/LL/tests/EssenceSystem.Tests/EquipmentUpgradeExecutionTests.cs). |
| E07 | [Blueprint content](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-blueprints.v1.json), [blueprint domain catalog](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentBlueprintCatalog.cs), [superseded naming document](C:/repos/Legends-Legacy/legends-legacy/docs/engineering/equipment-naming-and-compatibility.md), [current but partly stale specification](C:/repos/Legends-Legacy/legends-legacy/docs/design/equipment-specification.md). |
| E08 | [Regions and spawn weights](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/world/regions.json), [creatures](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/world/creatures.json), [area acquisition processor](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Idle/CombatAcquisitionRewardProcessor.cs). |
| E09 | [Ordinary acquisition domain](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentProgressionOrdinaryAcquisition.cs), [selection weights](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentSelectionWeights.cs). Base-only natural selection followed by variant attachment. |
| E10 | [Generic loot service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Loots/LootService.cs), [empty reward tables](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/rewards/reward-tables.json), [generic inventory factory](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Inventories/InventoryItemFactory.cs). |
| E11 | [Canonical progression policy](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Regions/CanonicalRegionProgressionPolicy.cs), [live creature balance](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/progression/region-combat-balance.json), [scaling provider](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Regions/RegionCreatureScalingProvider.cs). Formula horizon versus authored world. |
| E12 | [Idle options](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Models/IdleCombatProgressionOptions.cs), [planner](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Idle/IdleCombatPlanner.cs), [configured cadence](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/appsettings.json). |
| E13 | [Dungeon definitions](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/dungeons/dungeons.json), [equipment acquisition service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/EquipmentAcquisitionService.cs), [frozen reward claim](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Dungeon/DungeonRunRewardClaimer.cs). |
| E14 | [Starter grant service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/StarterEquipmentService.cs), [starter repository](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Equipments/StarterEquipmentRepository.cs), [equipment quest support](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Quests/EquipmentQuestSupport.cs), [plain entitlement](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/PlainEquipmentEntitlement.cs). |
| E15 | [Equipment boxes](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Inventories/SelectionCrates/RandomEquipmentBoxCatalog.cs), [container acquisition](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Inventories/SelectionCrateService.cs), [LiveOps grants](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Administration/LiveOpsService.cs). |
| E16 | [Raid content](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/raids/raid-bosses.json), [raid rewards](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Raids/RaidService.cs), [region boss reward content](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/region-bosses/region-bosses.json). |
| E17 | [Tower floors](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/world-tower/tower-floors.json), [Tower reward service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs), [Champion Market](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/market/champion-market.json). |
| E18 | [Raid armor gate](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Raids/RaidService.cs), [required content flag](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Raids/JsonRaidBossDefinitionProvider.cs). |
| E19 | [Equipment API](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Controllers/V1/EquipmentController.cs), [equip service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/EquipmentSlotService.cs), [equip repository](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Equipments/EquipmentSlotRepository.cs), [Inventory API](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Controllers/V1/InventoryController.cs). |
| E20 | [Loadout service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/EquipmentLoadoutService.cs), [loadout repository](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Equipments/EquipmentLoadoutRepository.cs), [loadout FK behavior](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/EquipmentSlots/EquipmentLoadoutConfiguration.cs). |
| E21 | [Comparison query and handler](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Equipments/Queries/CompareEquipment/CompareEquipmentQuery.cs), [instance DTO](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Equipments/Dtos/EquipmentInstanceDto.cs). |
| E22 | [Combat setup](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/CombatSetupService.cs), [shared mutation boundary](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/CombatStyles/CombatStyleMutationBoundary.cs). |
| E23 | [Combat Rating](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/PowerRatings/CombatRatingCalculator.cs), [power snapshot/fingerprint](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/PowerRatings/PowerBuildSnapshotFactory.cs). |
| E24 | [Marketplace service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/MarketPlaces/MarketPlaceService.cs), [trade ownership transition](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentState.cs), [inventory/transfer repository](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Inventories/InventoryRepository.cs). |
| E25 | [Market defaults](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/MarketPlaces/MarketPlaceOptions.cs). Listing limits, fee and expiry. |
| E26 | [Guild vault operations](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Guilds/GuildVaultService.cs). Donation, loan and permanent ownership restrictions. |
| E27 | [Equipment persistence](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Items/EquipmentInstanceConfiguration.cs), [blueprint progress persistence](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Items/EquipmentBlueprintProgressConfiguration.cs), [upgrade receipts](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Items/EquipmentUpgradeReceiptConfiguration.cs). |
| E28 | [Inventory UI](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/inventory/inventory.component.ts), [sort policy](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/utils/equipment/inventory-sort.ts), [display](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/equipment/equipment-display/equipment-display.component.ts), [compare/equip modal](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/modal-container/equipment-modals/equipment-modal/inventory-equipment-modal.component.ts). |
| E29 | [Frontend summary merge](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/client-side/session-summary/session-summary.service.ts), [popup grouping](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/session-summary-popup/session-summary-popup.component.ts). |
| E30 | [Inventory reward writer](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/InventoryLootRewardWriter.cs). Reward insertion, history and notifications before a proposed admission layer. |
| E31 | [Equipment frontend contract](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/models/equipment-progression.ts), [equipment service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/api/equipment/equipment.service.ts), [progression service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/api/equipment/equipment-progression.service.ts), [loadout service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/api/equipment/equipment-loadout.service.ts). |
| E32 | [Essence definitions](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Essences/Definitions/EssenceDefinition.cs), [slot progression](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Essences/EssenceSlotProgression.cs), [Creature Focus](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Essences/CreatureFocusRules.cs), [Essence service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Essences/EssenceSystemService.cs). |
| E33 | [Combat Style content](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/combat-styles/combat-styles.v1.json), [selection/rules](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/CombatStyles/CombatStyleRules.cs), [mastery progression](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/CombatStyles/CombatStyleProgression.cs). |
| E34 | [Historical crafting review](C:/repos/Legends-Legacy/legends-legacy/docs/design/equipment-gathering-crafting-review.md), [profession/queue removal migration](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260903115622_RemoveAlphaProfessionsAndTemperingQueues.cs), [legacy field removal](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260905105144_RemoveLegacyEquipmentFields.cs), [post-Alpha cleanup](C:/repos/Legends-Legacy/legends-legacy/docs/design/equipment-post-alpha-cleanup.md). |
| E35 | [Application layer rules](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/AGENTS.md), [service layer rules](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/AGENTS.md), [equip command/handler](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Equipments/Commands/EquipEquipment/EquipEquipmentCommand.cs), [dismantle command/handler](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Equipments/Commands/DismantleEquipment/DismantleEquipmentCommand.cs). |
| E36 | [Configured versions and startup conversion](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/appsettings.json:2), [release registry](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-releases.json), [version provider](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/JsonEquipmentCatalogProvider.cs), [settings](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentBalanceSettings.cs). Active versus historical catalogs and exact-version lookup. |
| E37 | [Core/specialization and style allocation](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentEvaluator.cs:155), [slot legality](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentSpecializationRules.cs:11), [release 4 costs](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-starters.v4.json:814), [occupied-slot set counting](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Sets/EquipmentSetBonusResolver.cs:58), [restored set decision](C:/repos/Legends-Legacy/legends-legacy/docs/analysis/restored-set-bonuses-2026-09-28.md). |
| E38 | [Current attribute units and legality](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Attributes/AttributeRules.cs:24), [item-tier normalization](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Components/Attributes/AttributeCalculator.cs:165), [activity comparison](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Equipments/Queries/CompareEquipment/CompareEquipmentQuery.cs:104), [comparison observation](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Equipments/Commands/ObserveEquipmentComparison/ObserveEquipmentComparisonCommand.cs), [comparison disclosures](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/modal-container/equipment-modals/equipment-modal/inventory-equipment-modal.component.html:113). |
| E39 | [Nobility benefits](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Nobility/NobilityBenefits.cs:9), [history-aware retention](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Nobility/NobilityRetention.cs:9), [combat retention integration](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/CharacterActions/CombatService.cs:50), [mixed-window regression](C:/repos/Legends-Legacy/legends-legacy/LL/tests/EssenceSystem.Tests/IdleCombatPlannerTests.cs:13). |
| E40 | [Migration service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/EquipmentMigrationService.cs), [migration domain](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentMigration.cs), [startup converter](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/EquipmentStartupConversion.cs), [startup invocation](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Program.cs:263), [LiveOps controller](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LiveOps/Controllers/EquipmentMigrationController.cs), [existing receipt migration](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260927132322_VersionEquipmentRebalances.cs), [equipment PostgreSQL rehearsal](C:/repos/Legends-Legacy/legends-legacy/LL/tests/EssenceSystem.Tests/EquipmentPostgresRehearsalTests.cs), [startup PostgreSQL rehearsal](C:/repos/Legends-Legacy/legends-legacy/LL/tests/EssenceSystem.Tests/EquipmentStartupPostgresRehearsalTests.cs). |
| E41 | [Frontend rules](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/AGENTS.md), [overview switch](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/character-overview-switch.component.ts:15), [shared preference](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/client-side/grimoire-preview/grimoire-preview-preference.service.ts:10), [character routes](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/character.routes.ts), [city routes](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/city.routes.ts), [migration chooser](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/equipment/migrated-specialization/migrated-specialization.component.ts). |
| E42 | [Disabled Tower supply option](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/Interfaces/Services/LL/Items/IStarterEquipmentService.cs:11), [inactive supply service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/TowerEquipmentSupplyService.cs), [supply catalog](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/TowerEquipmentSupplyCatalog.cs), [box definition weighting](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Inventories/SelectionCrateService.cs:163). Availability was checked against service registration and reward call sites, not inferred from catalog existence. |

## Verification and changed files

Changed by this task: **this report only**. Existing unrelated working-tree changes were left intact. The code review used repository searches, direct C#/TypeScript/JSON reads, structured content counts, cross-checks against historical migrations and current data, and independent audits of domain rules, acquisition, and inventory/economy behavior. Arithmetic in the numerical section was evaluated independently with JavaScript.

Executed through the required backend test entry point:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~Equipment|FullyQualifiedName~Loot|FullyQualifiedName~Inventory|FullyQualifiedName~MarketPlace|FullyQualifiedName~Marketplace|FullyQualifiedName~Attribute|FullyQualifiedName~PowerRatingCore|FullyQualifiedName~CombatStyleFoundation'
./build/run-tests.ps1 -NoBuild -Filter 'FullyQualifiedName~Nobility'
```

October revalidation: the first command produced **831 passed, zero failed, eight skipped (839 total)**. Its Release build completed with **94 warnings, zero errors**, including nullable/member-hiding and xUnit analyzer warnings. The approved run used the normal user environment outside the sandbox to allow NuGet configuration access. The separate Nobility command produced **23 passed, zero failed, zero skipped**. These runs exercise current implementation, not the proposed loot model; they replace the original report's 498-test result.

The eight skips comprise two opt-in PostgreSQL rehearsals and six archive-dependent balance studies. The database cases require a disposable local PostgreSQL connection; archived studies require their explicit fixtures/environment switches. They were not counted as passing tests. No production or shared database was used.

Document verification checks all **29 numbered sections**, source paths, current line anchors and whitespace. Historical source anchors were removed where intervening edits made their line positions stale. Independent read-only reviews rechecked domain rules, acquisition counts, weekly retention, migration and UX/economy assumptions. Numerical checks cover current/proposed daily and weekly supply, specificity waits and two-handed costs.

Frontend/browser tests and simulations of the proposed generator were not run: this task changes documentation only and that generator is unimplemented. No database-backed migration rehearsal, live-economy measurement or deployed-environment verification was performed. The API was not started because its configured startup conversion mutates equipment. These limits matter to effect pricing, time-to-upgrade estimates, concurrency guarantees and migration feasibility.

No package changes, configuration changes, migrations or deployment actions were made. The proposed changes would require all of those relevant coordinated implementation decisions later; this report does not apply them.
