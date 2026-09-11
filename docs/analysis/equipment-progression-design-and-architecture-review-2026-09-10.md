# LegendsLegacy equipment progression: game-design and architecture review

Date: 10 September 2026. Scope: the primary LL game, its API, persistence, combat workers, Angular client, administrative acquisition paths, and balance tooling. Reviewed checkout: `7dcbe382c`, including the working tree as found. This is a design report; no gameplay implementation, configuration edit, migration, database operation, or deployment accompanies it.

**Recommendation:** build a monster-targeted loot system with readable base items, a small number of bounded affixes, recognizable named drops, and personal guarantees for usable equipment. Remove Quality, rank reinforcement, consumable equipment blueprints, and four-piece combat-rule sets from the next design. Potential and Tempering are already absent from live equipment; keep them absent. Preserve ownership, transactional acquisition, snapshots, loadouts, stat budgets, and combat comparison foundations where they serve the new loop.

The important shift is from finding a better scalar version of a manufactured stat package to finding equipment suited to a specific combat job. Essences supply the abilities; Doctrines supply the combat rules; equipment supplies the physical foundation, specialization, and encounter preparation.

Numbers below are **illustrative design inputs**, not validated live balance. Current behavior, historical intent, inferred problems, and proposed behavior are identified separately. Evidence IDs refer to the linked source index at the end. Repository content establishes what this checkout supports; it does not establish which migrations or settings are running on a deployed server.

## 1. Current system summary

### The implementation has already changed substantially

The live system is not the old crafting system described in several historical plans. Crafting, gathering, queued tempering, Potential, equipment XP, and the Forge have been removed through code cleanup and migrations. The current model retains **Quality, seven rarities, frozen item-wide rolls, reinforcement ranks, styles/variants, sets, and consumable blueprints**. Later work reintroduced blueprint consumption for variant conversion; an older engineering document saying that blueprint IDs do not imply inventory items is now incomplete. [E01–E07]

There are **eight equipment slots**: Head, Chest, Legs, Ring, Necklace, Relic, MainHand, and OffHand. A two-handed weapon occupies both hands but counts as one item. Thus a full loadout contains seven or eight distinct items. Current definitions offer **28 archetypes**, **30 named definitions**, **11 styles**, and **11 sets**. The archetypes comprise nine armor pieces, three jewelry types, five one-handed weapons, eight two-handed weapons, and three offhands. There is no second ring slot, boots slot, belt slot, or gloves slot. [E01–E04]

Actual released equipment catalogs support tiers 1–2. World JSON contains Shenic, with ten ordinary combat areas plus the tutorial, and Meran, with four combat areas; there are 101 creature definitions. Four dungeon families each have three difficulties. The canonical progression policy describes ten regions and the equipment curve can extend further, but that is not equivalent to authored Region 3–10 loot. [E08–E12]

### Item identity and stat construction

`EquipmentBase` supplies item-base/type identity. `EquipmentInstance` carries persisted evaluated equipment and `EquipmentData`; the frozen descriptor contains `EquipmentState`, archetype/definition identity, rarity, quality, tier, rank, native/active style, roll multiplier, evaluated stats, weapon behavior, provenance, and ownership. Historical `ModelE*` names survive in some storage/content identifiers; that does not imply the removed Forge is still a player feature. [E01–E03]

For a current newly evaluated item, the approximate pre-allocation budget is:

`B = 100 × 15.2^((tier−1)/9) × slotWeight × rarity × quality × (1 + 0.04×rank) × frozenRoll`

`slotWeight = 2` for two-handed weapons and `1` for other items. Caps and allocation rules affect the final attribute vector. An additive variant then contributes another **15%** of this budget using its style profile; set bonuses sit outside that item budget. The regional budget multiplier is approximately **1.35306**, or **35.3% per equipment tier**. Tier 1 has budget 100; tier 10 has 1,520. [E02–E05]

| Dimension | Current behavior |
| --- | --- |
| Rarity | Common 1.0; Uncommon 1.1; Rare 1.3; Epic 1.6; Unique 2.0; Legendary 2.5; Legacy 3.0. These multiply budget; they are not affix counts. |
| Quality | Crude 0.90; Standard 1.00; Fine 1.12; Exceptional 1.26; Masterpiece 1.42. |
| Quality probabilities | Current ordinary JSON: **12.5%, 50%, 25%, 10%, 2.5%**, respectively, for both area and dungeon equipment. The current specification's 0/35/45/16/4 distribution is stale. |
| Frozen roll | One uniform multiplier from 0.95 to 1.05 on the whole budget. Individual stats are not independently rolled affixes. |
| Rank | 0–5; four percent additional budget per rank, reaching 20%. Area drops start at 0; ordinary dungeon rewards at 1. |
| Variant | Compatible active style adds a fixed authored stat profile and potentially set membership. A replacement can change that profile; it preserves tier, quality, rarity, roll, and rank. |
| Requirements | Equipment tier determines character level: T1 requires level 1; T2 level 50; T3 would require 100. Equipping does not require personally clearing the source region. |

For fixed tier, rarity and rank, Quality and the whole-item roll mostly answer the same question: how large is this otherwise identical package? The raw scalar envelope from Crude/low-roll to Masterpiece/high-roll is `1.42×1.05 / (0.90×0.95) = 1.744`. This is a much larger difference than the apparent ±5% roll suggests. The catalog's average Quality multiplier is 1.054, but its tails matter much more to item replacement. [E03–E05]

The base profiles are highly deterministic. Heavy armor allocates 40% Health, 30% Armor, 30% Resistance; Medium allocates 35% Power, 25% Health, 20% Armor, 20% Resistance; Light allocates 70% Power and 10% each to Health, Armor, Resistance. Head, Chest and Legs share these weights and equal budgets. Base ring is Power, necklace Health, relic regeneration. Weapons generally allocate 70% Power and 30% to a designated secondary. The authored base weapon interval and damage multipliers currently equal 1; the existence of a behavior field does not establish distinct live attack rhythms. [E02–E04]

### Actual sources and progression gates

| Source | What this checkout actually awards |
| --- | --- |
| Ordinary area combat | One equipment roll **per victorious encounter**, at 1/864. Conditional rarity: 85% Common, 12% Uncommon, 3% Rare. Equipment comes from the area/region pool, not the killed creature's own equipment table. |
| Base selection | 40% weapons, 35% armor, 25% jewelry; within weapons 60% one-handed/offhand and 40% two-handed. Then select among eligible bases. A 15% compatible regional variant roll follows. |
| Dungeon completion | Equipment chance 50% + 5 percentage points per mastery level, capped at 100%; mastery at run start determines the roll. Variant chance 50%, with family-specific eligibility. |
| Dungeon rarity | Novice: Uncommon/Rare/Epic 84/14/2; Veteran: Rare/Epic/Unique 84/14/2; Champion: Epic/Unique/Legendary 84/14/2. |
| Dungeon rooms | Miniboss rewards have a 25% equipment path. Treasury reward selection can produce a blueprint or styled equipment, with a 50/50 branch. These are separate from completion rewards. |
| Dungeon blueprints | 25% per completed run, fourth completion guaranteed after three misses, per character/family and shared across grades. This protects blueprints, not a desired equipment item. |
| Starter/tutorial/quests | Chosen bound starter weapon and explicitly authored equipment chests; early armor/jewelry chests give Common T1 equipment. |
| Event rewards | Current random equipment box can award two T1 Uncommon items. |
| Administrative grants | Canonical equipment grant/preview paths exist. They are operational tools, not ordinary progression. |
| Raids | Two authored bosses and five difficulty rows in total; current rewards are trophies, Soul Dust and monster cores. No currently authored equipment reward; trophy-vendor item list is empty. |
| Region boss | Mad King reward configuration is disabled with empty reward brackets. The reward schema supports currencies, not current signature equipment. |
| World Tower | Fifteen authored/released floors and Tower Token rewards, including first-clear and limited Echo rewards; no authored equipment drops. |
| Guild shop / Champion Market / other reward catalogs | Active offers focus on currencies, cores, sigil fragments and titles. Generic item factories are not evidence that these sources currently distribute equipment. |

The generic reward table file is empty and creatures have no active authored equipment reward tables. The infrastructure can resolve generic item rewards, including old-style equipment construction paths, but the audit found no current equipment-base references in those generic content reward lists. This is a future ingress risk rather than a current competing equipment economy. [E08–E17]

The 30 named definitions are not a current direct unique-drop table: natural area/dungeon selection chooses base definitions and then attaches styles. The evaluator's current display naming can also derive “Style + base name” instead of preserving the authored named definition's title. Goblin Mines targets Fury/Phoenix blueprints, Forgotten Catacombs Arcane/Endurance, Tangled Cave Execution, and Great Tree Spirit. Only these six styles have current direct blueprint-item sources; the broader eleven-style catalog can appear as native variants. Dungeon style rolls are subject to base compatibility: the nominal 50% produces about 45.5% styled equipment in Goblin Mines/Tangled Cave and 50% in the other two families. [E04, E07, E09, E13]

**Raids retain a direct crafting-era gate:** Head, Chest and Legs must all satisfy minimum tier and rarity and have an active style when `RequiresBlueprintArmor` is enabled. The loader requires that setting for authored raids. The failure text still describes “Blueprint-crafted” armor. A player can have an effective build and be rejected because it lacks the prescribed rarity/style arrangement. [E18]

### Equipment operations, inventory and economy

Equipping validates ownership, level and hand compatibility, binds eligible personal items, exchanges displaced items with inventory, and publishes the resulting state. Unequipping returns equipment to inventory. Saved equipment loadouts and activity-specific automatic selection already exist. The actual combat loadout can therefore differ from the gear displayed in the basic equipment slots. [E19–E22]

There are three saved equipment loadouts. Equipment uses one inventory row per unique instance with quantity one; the inventory repository currently loads the full inventory and related metadata rather than a paged equipment result. Favorites/unseen status belongs to the inventory ownership row, with favorite state preserved while equipping and returning items. [E01, E19–E20, E24]

The comparison query projects a prospective full attribute loadout, handles two-handed replacement, combines ratings correctly, includes default Essence attributes and attribute-based set bonuses, and returns differences. It does **not** simulate Doctrine triggers, active Essence rotations, set-granted abilities, or encounter outcomes. The displayed Combat Rating similarly values attributes; its single-target and multi-target offense are identical and its control utility is zero. It is useful evidence, not an authoritative “upgrade” classification. [E21–E23]

Reinforcement and dismantling use current prices for T1–2. T1 rank costs in parts are 5/10/20/40/80, with Cinder costs 11,150/22,300/44,600/89,200/178,400; T2 doubles these. The full T1 rank ladder therefore costs **155 parts and 345,650 Cinders**. Dismantling returns base parts equal to tier plus half the cumulative rank part cost, rounded down. Recovery is based on rank, including awarded rank, rather than the item's payment history. A T1 rank-1 dungeon drop returns three parts; T2 returns seven. Rarity and Quality do not change that return. A T3 price lookup would throw; no T3 price data is authored. [E05–E06]

Blueprint conversion consumes one compatible blueprint plus `100×tier` Cinders. It is guaranteed; replacement loses the former variant without refund. It does not itself bind unbound gear. Reinforcement does bind. Upgrade execution locks/reloads, re-quotes current state and records an idempotent operation receipt; it does not require a previously issued preview token. [E06–E07]

The equipment Auction House operates alongside the stackable-item Bazaar. Normal random-discovery equipment can be traded while unbound. Deterministic protected/quest awards are personal. Guild donation creates persistent guild ownership and loans preserve it. The buyer's progression is not checked at purchase; equipping later checks character level. Existing trade provenance, ownership checks, listings and economic ledger are valuable foundations. There is no supported general NPC equipment-sale loop in the audited client/service path; dismantling into Reinforcement Parts is the ordinary disposal loop. [E24–E27]

The Angular client has equipment cards/details, comparisons, slot filtering, type/rarity-aware search and sorting, favorite/unseen indicators, loadouts, upgrade/variant previews and bulk dismantling. Those are browsing tools, not automatic server-side loot admission filters. All retained equipment instances currently enter the reward/inventory flow. Bulk dismantling excludes favorites but can include unseen or saved-loadout items; deletion can null saved slot references. It issues preview/mutation requests item by item. Offline backend rewards preserve instances, but frontend summary grouping by item-base ID can collapse distinct variants and Quality rolls into the first representative item. [E28–E31]

### Character power and the other progression systems

Equipment modifiers feed `AttributeCalculator`, which combines flats, additive percentages and multiplicative modifiers. Armor and Resistance are summed as raw equipment ratings and converted using the character's expected progression tier: `80×normalizedRating/(55+normalizedRating)`. Other equipment percentage attributes are direct percentage points under the current model. This avoids scaling every percentage with tier, but means some old percentage-heavy equipment can retain disproportionate value. Tier normalization changes discretely at levels 51, 101, etc.; T2 equipping begins at 50. [E03, E22–E23]

Essences already carry active abilities, passives, attributes, tags, evolution and ascension. Their attunement slots unlock every ten character levels, from one to a maximum ten. Creature Focus already changes creature spawn weighting and Essence rewards, giving an existing interface for discussing farm targets. [E32]

The user-facing concept called **Doctrines in this brief is implemented as Combat Styles in this checkout**. The four are Bastion, Conduit, Reaper and Duelist. Their mechanics respectively change healing into Barrier, channel the first Essence using Charges from other casts, harvest condition damage, and build Read for stronger Essence hits. They progress to mastery 10 with refinement, upgrade, opening-technique and mastery decisions. No separate live Doctrine subsystem was found. Equipment should be designed against these actual rules. [E33]

## 2. Problems with the current system

1. **Several independent labels largely multiply the same stat package.** Rarity, Quality, rank, the frozen roll and additive variants create more arithmetic than item identity. Randomness rarely asks a new build question.
2. **The monster roster has little equipment identity.** Area pools determine gear. Choosing a creature currently matters far more to Essences than to equipment.
3. **Dungeon difficulty mostly escalates rarity multipliers.** A higher rarity is a strong default improvement; Unique is a numeric rarity rather than a guarantee of unique behavior.
4. **Sets carry substantial combat-rule identity outside the item budget.** For example, Fury's four-piece effect stacks Power after critical hits; Arcane rewards every third cast; Execution boosts damage below a target-health threshold. These overlap the role of Combat Styles and make comparisons incomplete. [E04]
5. **A desired slot and a usable stat direction lack reliable equipment protection.** Blueprint pity does not solve either problem. At the current area rate, Rare equipment averages one every 80 hours of uninterrupted victories, before slot, style or Quality suitability.
6. **Raid eligibility prescribes equipment construction.** It constrains viable builds using rarity and style rather than demonstrated competence.
7. **The UI protects favorites, not the complete set of player intentions.** Saved builds and important unseen items are vulnerable during bulk disposal; the offline summary can hide item-level differences.
8. **The persistent economy relies on binding and dismantling but lacks a designed long-term supply budget.** Infinite unbound random production still creates a growing supply of never-equipped gear. An Auction House fee removes currency, not equipment.
9. **Further regions need content work, not just a formula.** T3–10 require acquisition pools, prices or their replacement, item identities, monster sources and progression validation.
10. **Documentation can mislead a redesign.** Both old “no blueprint inventory” wording and newer Quality probability documentation disagree with current code/content. Design decisions must use the executable paths.

These are design findings, not claims that the existing code is universally broken. The 498 relevant existing backend tests passed. Two additional correctness concerns deserve reproduction before implementation: activity-loadout equipment can be mutated without the same earned-combat settlement used for physically equipped gear, and the static comparison context differs from the activity-specific combat context. [E06, E20–E23]

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

**MODIFY** the budget allocator and attribute catalog into an internal balance tool. A deterministic budget is useful engineering; deterministic player item identity is a separate design decision. The allocator can fund base stats and affixes without exposing recipes, quality or production steps.

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

Healthy targets: complete a basic early outfit through onboarding; receive roughly 25–35 ordinary random items per active day at high win rates; inspect a handful of candidates in one or two daily visits; make frequent early improvements and much slower late optimization. A strong item should normally survive the rest of its region and part of the next. Good decisions should survive bad rolls; exact perfect rolls need no guarantee.

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

| Rarity | Ordinary drop share | Core/affix budget | Role |
| --- | ---: | --- | --- |
| Common | 55% | 100% core; no affix | Immediate foundation; focused raw-stat options can remain useful. |
| Uncommon | 30% | 80% core; one 20% affix | One strong, readable specialization. |
| Rare | 13% | 70% core; two 15% affixes | Main target for a focused endgame build. |
| Epic | 2% | 70% core; three 10% affixes | Broader support for hybrid jobs; less concentrated in each secondary than a Rare. |

All have the **same nominal total budget** at equal base/region/band. Affix rolls are 80–100% of the allocated amount. Thus actual total budget is 100% for Common, 96–100% for Uncommon, and 94–100% for Rare/Epic. This is intentional: rarity buys specialization and combinations. It does not promise more Power or more total points.

A Rare with two desired traits can beat an Epic whose three traits split its specialization too widely. An Uncommon can provide the strongest single secondary. Common should remain a practical foundation and occasionally a deliberate stat choice, but not the best answer for every build: the base and affix pools must be tested for that failure. In particular, redesign today's one-stat jewelry cores into balanced foundations before applying this model.

Remove Unique, Legendary and Legacy as ordinary equipment power-rarity levels. **Named** is an identity/source category over Rare or Epic. Exceptionally scarce appearances and collection distinctions can have presentation labels, but they do not add another power multiplier. Do not alter the shared rarity enum for Essences merely to change equipment rarity; isolate the equipment contract.

## 9. Affix model

Start with a compact library using existing useful attributes: Power, Health, Armor, Resistance, regeneration, crit, attack speed, healing, penetration, cooldown, status resistance and crowd-control resistance. Avoid a large collection of near-synonymous damage percentages. Add new elemental/basic-attack/support traits only when the combat engine has a single defined place to apply and measure them.

Each base has a small allowed pool, initially about six to eight compatible traits. Each source family emphasizes two or three. A Rare has one family-weighted trait and one compatible general trait; it does not roll from every stat in the game. Epic adds a third compatible trait while splitting the same budget. Reject duplicates, contradictory traits and combinations that cannot benefit the base's intended role. Do not allow a trait to buy back the identical core allocation with a better exchange rate.

Affix values use **five bands: 80%, 85%, 90%, 95%, 100%**, equally likely initially. Value bands are deliberately coarse enough that perfect numerical rolls are not a microscopic event. A specific two-affix item has a 1/25 chance of perfect values once its desired affixes are present; its desired identity is the more important chase. Do not add a hidden overall roll on top.

Keep numeric effects in shared attribute buckets. Equipment-only conditional damage/healing bonuses should share additive buckets and a combined loadout cap, initially 20%, rather than multiply each other. General cooldown and attack-speed caps still apply. Equipment does not reduce Doctrine trigger thresholds, add Essence slots, duplicate Essence casts, or create self-triggering proc loops. A conditional trait can activate at most once per originating event; reflected, triggered and summoned events require an explicit eligibility policy.

Percentage-valued specializations require a separate balance pass. Retain the rating principle and use smooth level-relative normalization for vertical gear ratings; do not let a T1 percentage-only accessory remain a permanent best slot. All accessory cores must include a scaling Power/Health foundation. Normalize a contribution once, never by both item region and character level as separate penalties. Display raw rating and effective contribution at the player's current level. Smooth normalization removes today's boundary cliff but still produces gradual level-relative decay, which must be visible and simulated.

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

Give each family two or three preferred bases, two or three emphasized traits and, where worthwhile, one recognizable named object. Individual species can change one preference or carry a signature without requiring 101 bespoke generators. Early examples below use real creature names but **proposed** equipment associations:

| Existing creature/family | Proposed equipment identity | Reason to farm |
| --- | --- | --- |
| Goblin Warrior / martial goblins | One-handed weapons, offhand shields; Power and physical defense | Prepare an attack/guard loadout. |
| Crystal Wisp | Wands, necklaces; magic resistance and modest cooldown specialization | Support a caster facing magical damage. |
| Giant Spider / Blackjaw Spider | Rings, light armor; status resistance and carefully bounded condition support | Prepare against conditions or support an existing condition Essence. |
| Cave Bat / Giant Bat | Relics and medium armor; regeneration and basic-attack support | Improve sustained ordinary combat. |
| Forest Spirit | Staves, defensive jewelry; healing and resistance | Support group healing or a defensive Conduit setup. |

Elite encounters can improve **selection quality**, such as guaranteeing the family branch when the ordinary equipment roll succeeds. Initially keep the same overall item opportunity rate; avoid introducing an elite multiplier before elite frequency is measured. No elite tier classifier was found in the current ordinary gear processor, so this needs an explicit content definition.

Dungeon completions should award **one item**, with a 70% family branch and a visible choice of one of two farm preferences before starting. Proposed rarity weights: Novice U/R/E 65/32/3, Veteran 40/50/10, Champion 20/60/20. Higher difficulties provide better access to flexible items and additional signatures, not 2.5× base stats. This proposal replaces the existing completion/mastery gear chance and the separate room gear rolls; minibosses and treasury rooms improve the run's final selection or provide existing non-equipment rewards, rather than quietly adding multiple independent gear faucets.

Boss signatures use a separate, clearly budgeted reward opportunity: **20% for the selected eligible signature per reward-eligible clear**, with first-copy protection at 15 clears. The signature replaces that activity's ordinary equipment award. Begin with **two opportunities per character per day shared across repeatable boss hunts**, including dungeon final bosses, bankable up to fourteen. A standalone eligible boss clear gives one equipment item whether or not it is the signature; a dungeon completion already gives its one item and is never counted twice. Dungeon completions without a signature opportunity still award ordinary family equipment. This is a server eligibility counter, not a wallet currency; the player selects where to spend opportunities, so an unrelated automatic clear does not consume their saved target attempts. Repeated combat can continue for other rewards. Existing weekly raid limits require separate source-specific tuning; do not apply a fifteen-week first-copy ceiling by copying the ordinary-boss numbers.

The share of gear comes from player choices. A reference day with 24 hours of ordinary victories has an expectation of 34.56 area items. Adding two dungeon completions and two separate standalone eligible boss clears would raise that expectation to 38.56, excluding foundation and first-clear awards; spending those opportunities on the two dungeon completions instead gives 36.56. These are expected supplies, not hard maxima. If activities replace ordinary-combat time, subtract the displaced encounter opportunities. In this illustrative schedule roughly 90% of **item count** is ordinary loot, while bosses/dungeons supply more of the named or deliberately specialized keepers. It is not a required daily checklist.

Raids should offer a small pool of group-role named gear through the existing reward eligibility cadence, initially a choice among three role pools. World Tower should emphasize demonstrated progression: a bound selection at major first-clear milestones and appearances/collection records, with no infinite top-floor equipment faucet. Current raid currencies, Tower Tokens, sigils and Essence materials need not all become equipment-purchase currencies. Do not require raids, Tower and every dungeon for a mandatory complete set.

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

At the proposed frequency, a perfect 24-hour ordinary-combat session generates about 35 equipment objects, not hundreds. At 85% victory it generates about 29. This is an intentional inventory design decision. Thousands of enemy deaths are mostly combat progress and non-equipment rewards.

Not every rolled object needs a durable inventory entity. Use a server pipeline:

`resolve award → apply discovery/guarantee rules → apply protection → classify keep/sell → persist kept instances and sale receipts → aggregate summary`

Retain deterministic award IDs and enough rolled descriptor data in an immutable settlement/sale record to audit a result or support a short recovery window. Aggregate thousands of mundane events; do not insert full inventory rows and immediately delete them. Quest/acquisition credit and pity advance on the actual award, regardless of disposition. A sold item must not vanish from source discovery history or be counted twice on settlement retry.

The first version should offer three understandable settings: **Keep everything**, **Keep candidates for my saved builds**, and **Custom keep rules**. Keep everything is the default until the player sees an actual simulated summary of the proposed rule against recent drops. Auto-sale requires explicit enablement. Custom rules prioritize locked items, selected slots/traits and named identities; rarity is an optional supporting constraint, never the sole safe default.

The candidate evaluator is a conservative shortlist, not automatic equipping. It compares every saved build, handles both hands, shows defense/offense tradeoffs, flags new traits and preserves uncertain or incomparable items. A candidate whose behavior the evaluator cannot understand is retained. It must not dispose of a potentially useful lower-rarity item because an Epic has more colors or a larger static rating.

Show a daily summary such as: “8,640 encounters, 7,344 victories; 29 equipment drops; 5 retained candidates, 2 protected discoveries, 22 sold; foundation progress +7,344.” This is an illustrative presentation, not a guaranteed distribution. Provide individual cards for notable finds with their actual rarity, affixes and source; show sale totals separately. Never group different descriptors under the first item with the same base ID.

Aim for **two to eight candidates to inspect per day once established**, in one or two visits. The first day may contain more protected first discoveries; batch comparison must make this one review session rather than constant interruptions. A first named copy is always retained. For saved loadouts, unread important finds and guild property, no retention limit silently overrides protection.

Use paged inventory queries and batched actions. With a hypothetical 10,000 generated objects from imported/high-volume future content, the same pipeline streams rolls, maintains per-build candidates, aggregates sale ledgers and queues protected finds; it does not serialize all objects to the browser. Do not introduce a hard “best 20” cap that destroys incomparable items. Storage limits require explicit user resolution and a protected overflow inbox.

For automatic sales, a small recent-sales list with exact-instance recovery for 72 hours is reasonable. Recovery requires sufficient Cinders to reverse the original credit, consumes the recovery receipt and restores the original bound/unbound descriptor once in the same transaction; it cannot reroll or double-spend the award. This is inventory error recovery, not a permanent buyback market. If that feature is deferred, auto-sale should initially require stricter conservative rules and never auto-sell unseen Rare/Epic/named gear.

## 17. Auction House and trading rules

Keep ordinary random discoveries tradable until first equip. Keep natural named boss drops tradable under the same rule. Personal foundation rewards and pity-awarded first copies bind on acquisition; they cannot supply the market. **Current random equipment boxes produce bound ProtectedReward equipment**: although the container service passes unbound ownership into construction, the domain coerces non-RandomDiscovery awards to bound ownership. Preserve personal-container binding. Publicly repeatable future rewards follow their explicitly authored supply/ownership policy. [E01, E15, E24]

Equipping binds to the character; moving to another player's inventory does not reset that state. Removing general enhancement eliminates an additional binding trigger. Listing and transfer retain the original source and rolled descriptor. Guild donation permanently removes gear from personal trade; loans do not waive access requirements.

All equip, saved-loadout resolution and guild-borrow/use paths enforce personal region/band access. Named items also require the corresponding first clear. The market should display “usable now,” “region clear required” and “source clear required,” and default search to usable stock while allowing browsing aspirational items. There is no need to block merely buying an unusable item if ownership and future eligibility are clear.

Current defaults are ten listings, ten commodity buy orders, seven-day listing expiry and a seller fee of 3% with a one-Cinder minimum. Preserve this infrastructure initially. Replace Quality/rank/style filters with slot, region/band, trait, named identity and usable-now filters. Static Gear Power must not drive automatic purchasing or disposal. Equipment buy orders are not currently stat-specific; do not promise that feature without implementing its matching and escrow model. [E24–E25]

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

Start with already measurable interactions: healing power, defensive rating, attack speed, crit and modest cooldown. Elemental damage, summon support and condition amplification are potential later traits, not assumed existing universal stats. They need a shared combat effect contract and tests before being authored. Avoid per-Essence hardcoded bonuses.

Gear and Essence farming should sometimes align and sometimes compete. A player may farm Crystal Wisp for its Essence while accepting a lower chance at a physical shield; another family offers the reverse. The archive should make that tradeoff visible. Equipment focus must not accidentally reuse the existing Essence Focus 3× drop multiplier on equipment; the systems have different probability budgets. [E32]

## 20. Interaction with Doctrines / Combat Styles

| Actual Combat Style | Its ownership of build identity | Equipment's supporting role |
| --- | --- | --- |
| Bastion | Healing split into Health and Barrier, with its own refinements and mastery | Health, defensive rating and measured healing support; no extra healing-conversion engine. |
| Conduit | Channeled first Essence and Charge consumption from other casts | Balanced Power/healing and bounded cooldown; no extra Charges, free casts or automatic first-slot replacement. |
| Reaper | Harvesting existing Bleed/Burn/Poison ticks with active damage | Sustain and modest condition support once defined; no extra harvest triggers or self-propagating condition loops. |
| Duelist | Read generation and consumption for a stronger damaging Essence | Attack speed/crit where useful; no reduction of the Read threshold or extra Read-on-proc mechanics. |

This division preserves meaning in choosing the Style. Equipment can make a plan safer or better suited to an encounter; it does not rewrite its trigger economy. Gear-only bonuses should aggregate additively where possible, while Doctrine-owned conversion mechanics retain their own clearly defined stage. In particular, healing-to-Barrier conversion, Reaper payouts and Conduit amplification need tests to prevent one gear bonus being applied twice.

Cap gear-origin conditional modifiers across the full loadout, and simulate combinations with multiple Essences and all four Styles. Testing an item only against a basic attacker would miss its strongest interactions. Comparison should show the selected activity/loadout and label unmodeled behavior rather than imply that a single rating has evaluated it.

## 21. Concrete example items

These are **proposed items and source associations**, not existing drops. Budgets below use current exchange rates only to illustrate a core where stated; they are not production-ready stat values. A point allocation is an internal design measure, never a currency shown on the item. Conditional trait prices require simulation and are not claimed equivalent to a specific damage percentage yet.

| Example | Anatomy and intended decision |
| --- | --- |
| **Groveguard Helm** — Common, late R1, Heavy Head | Budget 108. Using the present Heavy profile and exchange rates: approximately 234 Health, 36 Armor Rating, 36 Resistance Rating. No affixes. A readable foundation and possible defensive alternative to an overly offensive Rare. Proposed martial/forest general pool. |
| **Wisp-touched Wand** — Uncommon, entry R2, one-handed | Budget 118; core 94.4, one cooldown-support affix allocated 23.6 points. At the 90% band the affix realizes 21.24 points, total 115.64. It gives one concentrated specialization; a three-affix Epic may have less of that particular secondary. Proposed Crystal Wisp family/challenge source. |
| **Blackjaw Band** — Rare, late R1, Ring | Budget 108; balanced core 75.6; two 16.2-point traits: status resistance at 100%, regeneration at 90% = 14.58. Total 106.38. It supports surviving condition-heavy enemies; it does not grant Poison. Proposed Blackjaw Spider source. |
| **Meran Scout Mail** — Epic, entry R2, Medium Chest | Budget 118; core 82.6; three 11.8-point traits at 85/95/100%: physical defense, regeneration, crit. Total 115.64. Useful for a hybrid solo build, but a focused Rare can devote more budget to its two essential traits. Proposed regional scout/martial pool, requiring authored family assignment. |
| **Garran's Gateward** — named Rare, late R1, offhand shield | Budget 108; 70% core, 15% fixed signature, 15% resistance trait. Signature proposal: an opening Barrier worth a calibrated multiple of this shield's own Health contribution, lasting at most six seconds, once per encounter. Apply a shared equipment-origin cap; never scale the old shield's barrier from total character Health. No refresh/proc loop. Proposed Garran source; effect magnitude and its 15% cost require short/long-fight value tests. |
| **Heartwood Mercy** — named Rare, late R2, two-handed staff | Budget 254.88; core 178.416, fixed healing trait 38.232, one defensive trait 38.232 before roll. No free heal. It supports existing healing Essences or Conduit. Proposed Great Tree signature; requires a personal source clear. |
| **Morrowmaw's Memorial** — cosmetic chase appearance | Same rolled power and requirements as its attainable underlying named item. Records the source and changes appearance only. It is an optional collection goal, not a raid requirement. |

The staff replaces both hand slots, so its budget and comparison use two units. Do not count it as two set pieces or allow its cosmetic appearance to bypass type restrictions. Source, binding, required access, exact trait bands and “used by loadout” status appear on every relevant item card.

## 22. Example player progression

Region 3 onward is hypothetical future content. Time estimates assume the proposed rate and healthy victory rates, not the current sparse ordinary drops.

| Stage | Goal, farming decision and upgrade meaning | Cadence and unwanted gear |
| --- | --- | --- |
| Early R1 | Onboarding supplies a complete basic Common outfit with a weapon choice. Player farms accessible goblins/nearby families for one suitable offensive or defensive secondary and their first Essences. An upgrade may fill a missing role rather than increase rarity. | Quest improvements within the first play session; roughly 2–5 random useful improvements/day while many slots are weak. Sell clearly obsolete duplicates after previewing keep rules. |
| Late R1 | Choose an actual Style/Essence plan. Farm a shield, resistance item or focused weapon, then one optional signature such as Gateward. Replace current four-piece-set expectations with individual decisions. | Around one random improvement/day in a partially established outfit; guaranteed baseline clears remaining bad slots. Strong Rare/Uncommon pieces remain relevant. |
| Entering R2 | Earn access using R1 gear. Replace the weakest core pieces first; retain the excellent Blackjaw Band against condition-heavy fights. New family pools offer different defensive and support opportunities. | Two or three early replacements across initial sessions, then partial refresh over several days. Foundation progress avoids an unlucky missing-slot stall. |
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

The proposal increases equipment frequency **3.456×**, deliberately remaining far below conventional high-volume action-RPG loot. At the present rate, the chance of seeing no area Rare in a perfect day is about 74%; a particular Rare armor archetype averages roughly 85.7 continuous days before Quality/style suitability. That is a poor source for a recognizable ordinary equipment goal.

Proposed ordinary rarity counts:

| Scenario | Common 55% | Uncommon 30% | Rare 13% | Epic 2% |
| --- | ---: | ---: | ---: | ---: |
| 1,000 kills, ~507.36 wins | 1.116 | 0.609 | 0.264 | 0.041 |
| 1,000 wins | 2.200 | 1.200 | 0.520 | 0.080 |
| 24 hours, all victories | 19.008 | 10.368 | 4.493 | 0.691 |

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

For a practical initial shortlist, retaining 15–25% of the day's ~35 objects produces about 5–9 candidates before protected first discoveries and named loot. Use telemetry to tune relevance, not a destructive quota that forces every session to fit that number.

### Random tails and deterministic ceilings

With `p=0.004`, an equipment inter-arrival time has mean 250 victories. Median is about 173 victories, and about 748 victories give 95% chance of at least one item. The probability of no item in 1,000 victories is `(0.996)^1000 = 1.82%`; in the ~507 victories represented by 1,000 ordinary kills it is about 13.1%.

At 1,800 wins, expected random equipment is 7.2 and the probability of receiving none is about 0.074%. The foundation guarantee is therefore chiefly **slot/role protection**, not protection against seeing absolutely no gear. A single-slot target takes five winning hours; eight slot-budget units take forty. Do not advertise a one-item-per-five-hours guarantee as eight simultaneous slot guarantees.

For a 20% signature probability with hard first-copy guarantee at clear 15:

`E[clears] = Σ(i=0..14) (0.8)^i = (1−0.8^15)/0.2 = 4.824`

Only `0.8^14 = 4.40%` of players reach the guaranteed final attempt. At two eligible clears/day the mean is about 2.4 days of opportunities; the cap is 7.5. These values are for the **selected item**, not an unspecified item from a boss pool. If the roll first selects among five signatures, those numbers would be wrong; the source selection contract must prevent that extra hidden RNG layer.

For two desired affixes each with five equally likely value bands, a perfect-value pair occurs once per 25 correctly composed items on average. Exact trait selection and the item's source determine the real chase time. No promise of universal “perfect gear in 25 drops” follows.

### Currency and item lifespan checks

With a provisional R3 single-slot vendor price of about 27.85 Cinders, selling 80% of 34.56 daily ordinary drops yields about **770 Cinders/day before two-handed weighting**, in addition to current currency rewards. This is merely a faucet estimate; deleting today's 345,650-Cinder T1 reinforcement ladder changes demand much more substantially. Model both sides before choosing a sale price.

An excellent item bridging one region is supported by the 2.7% old-max/new-min budget gap. An old item persisting through several regions would indicate overpowered flat-percentage traits, an underpriced signature, inaccessible alternatives, or an incorrect source-band curve. Track actual replacement ages by slot and build. Do not respond automatically by increasing every region's budget.

## 24. Technical changes required

### Reusable

| Existing system | Reuse and limits |
| --- | --- |
| Equipment instances and frozen descriptors | Preserve stable IDs, evaluated stats, source/ownership, snapshot restoration and row-version concurrency. Change anatomy rather than creating a second permanent item implementation. |
| Domain budget allocator and attribute metadata | Retain stat units, caps, hand weights and constrained allocation. Replace rarity/quality/rank scalar inputs with core/affix budget allocations. Recalibrate the numerical curve. |
| CQRS/MediatR command pipeline | Keep command transactions, service interfaces, request IDs, receipts and existing response/state-sync patterns. |
| Combat reward settlement | Preserve encounter/run identities, frozen rewards, original earned-time handling, batching and retry semantics. |
| Inventory, equipment slots and loadouts | Reuse exact-instance placement and two-hand deduplication. Expand protection and personal-access checks. |
| AH, transfers, guild loans, ledger and provenance | Reuse custody and audit infrastructure. Add consistent access/protection policies and guild retirement. |
| Comparison and snapshots | Reuse the shared full-attribute projector; extend activity context and signature disclosures. Do not duplicate balance arithmetic in Angular. |

### Refactor

**Domain models.** Introduce a single equipment-specific rarity contract, base/core definition, affix definition, affix roll, named definition and source/band requirement. Keep definition IDs and versions stable. Put slot placement, personal eligibility, protections and disposal decisions in Core domain/application policies. Existing gameplay decisions in `EquipmentSlotRepository` should move out of persistence as part of this scoped change; Core must not depend on Infrastructure. [E01–E03, E19]

Proposed service concepts, not implemented APIs:

- A canonical equipment generator accepts an authorized source context, eligible catalog version and server random stream, and returns a frozen candidate. It cannot accept arbitrary client-provided final stats or future-region overrides.
- A reward disposition policy evaluates the candidate, current saved-build protections and filter version, returning Keep or Sell with a reason.
- A progression policy awards personal foundation/signature entitlements from eligible encounters/clears.
- A placement policy evaluates level, personal progression access, handedness, ownership and guild loan availability.

**Database.** Version the existing equipment JSON descriptor or replace its column contract in a one-time migration. Add indexed queryable fields for region/band, equipment rarity, base, named identity and ownership; retain the full descriptor as authority. For AH affix filtering, use an indexed item-affix projection table or carefully chosen JSON indexes; do not scan/deserialise the full market. The appropriate choice depends on existing PostgreSQL query plans and expected item volume.

Add per-character/per-region foundation progress and consumed slot entitlements; per-character/source/signature first-copy progress; banked reward-opportunity timestamps if that cadence is selected; versioned filter preferences; and idempotent sale/recovery receipts. Counters and award receipts need uniqueness constraints on their complete logical keys, plus concurrency protection. Extend existing discovery/history storage where it already fits instead of creating duplicate counters for every UI card.

**Loot content and generation.** Replace area-wide anonymous selection with a validated mapping from creature → family → equipment pools, with explicit base and affix eligibility. Preserve one roll per victorious encounter. Move dungeon completion, named bosses, chests, quests, events, Tower selections and administrative grants through the same typed equipment boundary. Reject generic equipment-base rewards that would invoke `InventoryItemFactory`'s descriptor-less fallback. Reference/canonical builds can use the same evaluator with explicit simulation contexts; they never count as player acquisitions. [E08–E17]

**Combat integration.** Add only a small shared signature-effect adapter where existing attributes are insufficient. Include source eligibility, trigger exclusions, cap buckets and balance versioning. Snapshots must contain everything needed to reproduce the selected equipment loadout; emitted combat still resolves live definition versions only according to an explicit policy. Revisit rating normalization and the 50/51 requirement discontinuity together. Recalibrate creature scaling, canonical builds, dungeon/raid/Tower forecasts and balance harness expectations as one change. [E03, E22–E23, E33]

**Reward transactions.** A settlement must atomically record the award identity, update personal progress, create a kept item or sale receipt, apply Cinders, record discovery/quest progress and enqueue state/history notifications. Replaying the same award must not grant both the sold and retained versions. Saved filters and content versions must be stable for an already computed reward; changing a filter cannot reroll an old batch or change a committed outcome. Pending offline combat must settle with the equipment state that earned it.

**API/application contracts.** Retain equip/unequip, linked-item lookup and loadout routes with revised descriptors. Add source/target queries, target selection, filter preview/update, paged equipment queries, activity-aware comparison, batch sale and optional recent-sale recovery. Use `ICommand<T>` for mutation and `IQuery<T>` for reads, focused feature DTOs, mapping in Application handlers and repository interfaces for persistence. Repositories do not become alternate gameplay engines. Retire upgrade/reinforce/dismantle/variant commands and endpoints after cutover; do not expose no-op compatibility endpoints. [E19–E21, E35]

**Frontend.** Revise `equipment-progression.ts`, equipment DTOs, equipment API/state services, display cards, comparison modal, inventory sorts, marketplace filters, chat links, loadouts and session summary together. Show a concise core/traits/source/requirements layout. Remove Quality, rank, blueprint and set-progress panels. Add safe keep-rule preview, source browsing, guarantee progress, saved-loadout protection reasons and notable-find cards. AdminDashboard's base-item editor is not a full equipment/affix catalog authoring tool; either extend it with validated catalogs or continue using validated source-controlled JSON. [E28–E31, E35]

**Existing consumers requiring explicit updates.** Quests reading `PlainEquipmentEntitlement`, raid styled-armor checks, dungeon preview rewards, event/selection boxes, guild loan availability, marketplace listings, character overviews, power-rating fingerprints, combat snapshots, loot history, support/admin snapshots, chat links, favorites/new-item actions and activity mutation boundaries must all understand the new descriptor. A rename of the equipment screen will not complete this migration.

### Validation needed before future implementation ships

Use invariant and behavior tests rather than assertions that duplicate the generator. Check fixed total budget, no illegal/duplicate traits, equal hand budgets, legal source access, lower-rarity viability, no double application of healing/condition/Doctrine modifiers, complete source coverage and reproducible generation.

Run deterministic scenario comparisons for all four Styles, offensive/defensive/support Essence loadouts, all available regions and planned region checkpoints. Include short burst, attrition, multi-target, status-heavy and group encounters. Validate replacement lifetimes and core-vs-affix tradeoffs. Static budget scores alone cannot price conditional effects.

Test concurrent claim/retry, duplicate settlement, auto-sale versus favorite/loadout changes, sale recovery versus spending, source/band access through direct transfer/AH/guild loans, snapshot restoration, quest credit for sold awards, and no retroactive loadout mutation. Load-test 24-hour settlement and larger synthetic inventories. A database-backed concurrency suite is needed; an EF in-memory test is not evidence of PostgreSQL locking correctness.

## 25. Systems and code to delete

After migration and final reference checks, delete the superseded active feature slices together:

- Rank reinforcement and dismantling logic in `EquipmentUpgradePolicy`, `EquipmentUpgradeModels`, `EquipmentUpgradeService`, `EquipmentUpgradeRepository`, their interfaces/commands/DTOs, upgrade price JSON and player panels. Extract any still-needed generic receipt/locking code into its actual surviving feature before deleting it.
- Consumable blueprint catalog/options/progress/repository/service paths, `ApplyEquipmentVariant`, blueprint item definitions, dungeon blueprint rewards/pity, conversion panels and associated quest rewards. Replace all source references; do not leave dangling quest/dungeon outputs.
- Quality, rank, global-roll and native/active-style allocation fields in the next equipment descriptor and related UI/API/search contracts. Affix IDs and named identity replace their intended new functions; do not carry a hidden old multiplier.
- Active set membership and four/six-piece equipment engine effects, associated granted abilities and set-progress UI. Remove a granted ability only after checking it has no Essence or other legitimate consumer.
- Raid `RequiresBlueprintArmor` validation and rarity/style gates; replace with the chosen personal competence/access contract.
- Descriptor-less equipment creation through generic rewards; old runtime fallback branches only after the data inventory and conversion are complete.
- `EquipmentAttributeRules` is a candidate for dead-code removal: no callers were found during this audit. A final reference/build check must precede deletion.

Potential, Tempering, crafting and gathering are **already deleted feature work**. Do not reopen those projects or count deleting their historical descriptions as a gameplay milestone. Keep applied historical EF migrations; they explain how an existing database reached its schema. Archive/mark stale design documents instead of letting contradictory specifications remain current. Never rewrite applied migration history to make the repository appear as though the old game never existed.

## 26. Migration strategy

No migration is generated or applied in this review. There are two legitimate data situations; determine the actual one before implementation. A historical cleanup document says Alpha data could be discarded at that time. That does not authorize deleting today's player state.

**For disposable development/test worlds**, rebuild equipment test data directly in the new model through the normal development process. This is the simplest development path, but it is not permission to reset a shared environment.

**For retained player data**, plan a finite cutover rather than a permanent dual-model compatibility layer:

1. Inventory all current equipment stores: player inventory, equipped slots, saved loadouts, AH custody, guild vault/loans, pending dungeon rewards, combat snapshots, administrative/support copies and any embedded reward payloads. Count descriptor-less and malformed items explicitly.
2. Prepare a mapping preview by actual archetype, tier, useful stat profile and ownership. Preserve usable roles and stable instance identity when possible. Quality/rank cannot be translated into “the same affixes” mechanically; define a bounded normalization policy and show representative old/new character results. Do not invent boss-clear credit merely because an old item has a matching style.
3. Evaluate legitimate regional progress already earned from durable quest/clear records. Backfill access only from that evidence. If a former owner cannot use converted gear, give an explicit bounded transition entitlement or baseline replacement, not a universal bypass that new purchases inherit.
4. Resolve existing listings and pending claims under a declared release boundary. Cancel/refund outstanding obsolete blueprint/parts orders and return valid equipment with its original ownership before conversion. Completed trade history remains history. Pause or drain pending combat and avoid editing already-running snapshot semantics in place.
5. Convert retained objects once, preserving IDs/ownership/favorites where possible and repairing every loadout reference. Quarantine unknown descriptors for an explicit decision instead of silently dropping them or retaining permanent old evaluation branches.
6. Retire Reinforcement Parts and blueprint inventory through one documented transition policy. Prefer a bounded one-time credit or equivalent regional baseline reward supported by actual records. Decide explicitly whether historical Cinder spending receives any credit; do not create an uncapped refund faucet from inferred rank values. Bound deterministic replacement rewards to avoid a migration-created market flood.
7. Recompute search projections, validate custody totals, compare before/after character roles, verify that no item is simultaneously listed/equipped/owned twice, and reconcile monetary adjustments against a migration ledger.
8. Once every retained item and pending payload is resolved, remove obsolete tables/columns/runtime branches and the old endpoints. Keep migration receipts and source history for audit, not gameplay execution.

Back up and test this sequence on a database copy, including rollback before irreversible player exposure. If persisted combat effects depend on old set definitions, resolve those pending runs before removing the definitions or explicitly retain versioned snapshot resolution for the finite drain period. That is a cutover requirement, not justification for indefinite compatibility hacks.

This will require coordinated backend content/API and frontend releases, and likely worker updates. Normal deployment/migration approval remains a later operational step. Nothing in this analysis deploys services or modifies infrastructure-as-code.

## 27. Recommended implementation phases

| Phase | Deliverable | Exit condition |
| --- | --- | --- |
| 1. Validate design with current combat | Small representative base/affix/named catalog and offline simulation inputs; economy/drop-volume model; settle region-access rules. | Common/U/R/E tradeoffs, two-hand fairness, four Styles and early/late encounters behave as intended. Select actual desired upgrade cadence from evidence. |
| 2. Build the canonical model and read projections | New descriptor/evaluator, source definitions, access/protection policies, schema/migration preview; updated comparison contracts. | Every ingress can construct and explain one legal item; existing player data has a reviewed finite conversion path. |
| 3. Implement acquisition and guarantees | Region/family drops, dungeon final reward, foundation and signature progress with idempotent settlement. | Probability tests and retry/concurrency tests pass; no hidden extra room/boss equipment faucets; all eight slot-budget units have achievable sources. |
| 4. Complete safe inventory and market UX | Source browser, trait comparison, paged inventory, saved-build protection, batch sale, notable offline finds and updated AH/guild rules. | A 24-hour session is understandable in one review; no protected find is silently disposed of; all transfer/use paths enforce eligibility. Auto-sale waits until its safety criteria pass. |
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

LegendsLegacy already has much of the technical foundation needed for good loot: stable equipment instances, source-aware rewards, explicit ownership, loadouts, shared stat evaluation, transactional operations and deterministic reward restoration. Reuse that foundation.

The current gameplay still treats most equipment as a fixed profile multiplied by rarity, Quality, investment and style. That model makes sense as the output of a production process, but it does not make the monster roster or the act of finding an item sufficiently interesting. Its raid eligibility rules reveal the strongest remaining production assumption.

Adopt **monster-targeted bounded loot with personal baseline and signature guarantees**. Make rarity describe specialization; let named equipment provide restrained source identity; let Essences and Combat Styles retain the larger combat decisions. Keep dropped items complete, remove redundant enhancement axes, and make a day's rewards a short set of decisions.

The success criterion is a player who can explain both what they want and why they are farming that source, remain functional through bad luck, and return after a day of idle combat to a few understandable equipment choices.

## Evidence index

Links point to this reviewed workspace. Each ID covers a related source chain; current C#/JSON takes precedence over historical prose.

| ID | Concrete sources and audit use |
| --- | --- |
| E01 | [Equipment slots](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Slots/EquipmentSlotType.cs:3), [EquipmentInstance](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/EquipmentInstance.cs), [EquipmentState](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentState.cs:58), [EquipmentData](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentData.cs). Identity, ownership and frozen evaluation. |
| E02 | [EquipmentEvaluator](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentEvaluator.cs:93), [EquipmentBalance](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentBalance.cs:51), [starter/base catalog](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-starters.v1.json). Budget, scalar factors and authored base profiles. |
| E03 | [Tier curve](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentTierBudgetCurve.cs:9), [stat costs and conversion](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentStatBudgetCatalog.cs:20), [AttributeCalculator](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Components/Attributes/AttributeCalculator.cs:130), [combat caps](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Attributes/AttributeCombatRules.cs:7). |
| E04 | [Named definitions](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-named.v1.json), [styles](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-styles.v1.json), [sets](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-sets.v1.json), [set resolver](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Sets/EquipmentSetBonusResolver.cs:11), [catalog expansion](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/JsonStarterEquipmentCatalog.cs:22). |
| E05 | [Live drop profiles](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-ordinary.v1.json:15), [upgrade prices](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-upgrades.v1.json), [upgrade prices/returns model](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentUpgradeModels.cs:51). |
| E06 | [Upgrade policy](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentUpgradePolicy.cs:35), [execution service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/EquipmentUpgradeService.cs:58), [repository](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Equipments/EquipmentUpgradeRepository.cs:27), [execution tests](C:/repos/Legends-Legacy/legends-legacy/LL/tests/EssenceSystem.Tests/EquipmentUpgradeExecutionTests.cs:19). |
| E07 | [Blueprint content](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-blueprints.v1.json:3), [blueprint domain catalog](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentBlueprintCatalog.cs:13), [superseded naming document](C:/repos/Legends-Legacy/legends-legacy/docs/engineering/equipment-naming-and-compatibility.md:17), [current but partly stale specification](C:/repos/Legends-Legacy/legends-legacy/docs/design/equipment-specification.md). |
| E08 | [Regions and spawn weights](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/world/regions.json), [creatures](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/world/creatures.json), [area acquisition processor](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Idle/CombatAcquisitionRewardProcessor.cs:22). |
| E09 | [Ordinary acquisition domain](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentProgressionOrdinaryAcquisition.cs:224), [selection weights](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentSelectionWeights.cs:14). Base-only natural selection followed by variant attachment. |
| E10 | [Generic loot service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Loots/LootService.cs:117), [empty reward tables](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/rewards/reward-tables.json:2), [generic inventory factory](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Inventories/InventoryItemFactory.cs:75). |
| E11 | [Canonical progression policy](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Regions/CanonicalRegionProgressionPolicy.cs:11), [live creature balance](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/progression/region-combat-balance.json), [scaling provider](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Regions/RegionCreatureScalingProvider.cs). Formula horizon versus authored world. |
| E12 | [Idle options](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Models/IdleCombatProgressionOptions.cs:7), [planner](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Idle/IdleCombatPlanner.cs:31), [configured cadence](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/appsettings.json:60). |
| E13 | [Dungeon definitions](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/dungeons/dungeons.json), [equipment acquisition service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/EquipmentAcquisitionService.cs:21), [frozen reward claim](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Dungeon/DungeonRunRewardClaimer.cs:47). |
| E14 | [Starter grant service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/StarterEquipmentService.cs:38), [starter repository](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Equipments/StarterEquipmentRepository.cs), [equipment quest support](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Quests/EquipmentQuestSupport.cs:20), [plain entitlement](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/PlainEquipmentEntitlement.cs:5). |
| E15 | [Equipment boxes](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Inventories/SelectionCrates/RandomEquipmentBoxCatalog.cs:19), [container acquisition](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Inventories/SelectionCrateService.cs:116), [LiveOps grants](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Administration/LiveOpsService.cs:468). |
| E16 | [Raid content](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/raids/raid-bosses.json), [raid rewards](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Raids/RaidService.cs:989), [region boss reward content](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/region-bosses/region-bosses.json:49). |
| E17 | [Tower floors](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/world-tower/tower-floors.json:2), [Tower reward service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs:1809), [Champion Market](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/market/champion-market.json). |
| E18 | [Raid armor gate](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Raids/RaidService.cs:1602), [required content flag](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Raids/JsonRaidBossDefinitionProvider.cs:46). |
| E19 | [Equipment API](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Controllers/V1/EquipmentController.cs:29), [equip service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/EquipmentSlotService.cs:31), [equip repository](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Equipments/EquipmentSlotRepository.cs:147), [Inventory API](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Controllers/V1/InventoryController.cs:18). |
| E20 | [Loadout service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/EquipmentLoadoutService.cs:64), [loadout repository](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Equipments/EquipmentLoadoutRepository.cs:18), [loadout FK behavior](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/EquipmentSlots/EquipmentLoadoutConfiguration.cs:26). |
| E21 | [Comparison query and handler](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Equipments/Queries/CompareEquipment/CompareEquipmentQuery.cs:81), [instance DTO](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Equipments/Dtos/EquipmentInstanceDto.cs:39). |
| E22 | [Combat setup](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/CombatSetupService.cs:132), [shared mutation boundary](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/CombatStyles/CombatStyleMutationBoundary.cs:15). |
| E23 | [Combat Rating](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/PowerRatings/CombatRatingCalculator.cs:23), [power snapshot/fingerprint](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/PowerRatings/PowerBuildSnapshotFactory.cs:55). |
| E24 | [Marketplace service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/MarketPlaces/MarketPlaceService.cs:470), [trade ownership transition](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentState.cs:195), [inventory/transfer repository](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Inventories/InventoryRepository.cs:433). |
| E25 | [Market defaults](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/MarketPlaces/MarketPlaceOptions.cs:7). Listing limits, fee and expiry. |
| E26 | [Guild vault operations](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Guilds/GuildVaultService.cs:23). Donation, loan and permanent ownership restrictions. |
| E27 | [Equipment persistence](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Items/EquipmentInstanceConfiguration.cs:11), [blueprint progress persistence](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Items/EquipmentBlueprintProgressConfiguration.cs), [upgrade receipts](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Configurations/Items/EquipmentUpgradeReceiptConfiguration.cs). |
| E28 | [Inventory UI](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/inventory/inventory.component.ts:785), [sort policy](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/utils/equipment/inventory-sort.ts), [display](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/equipment/equipment-display/equipment-display.component.ts:103), [compare/equip modal](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/modal-container/equipment-modals/equipment-modal/inventory-equipment-modal.component.ts:173). |
| E29 | [Frontend summary merge](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/client-side/session-summary/session-summary.service.ts:116), [popup grouping](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/session-summary-popup/session-summary-popup.component.ts:122). |
| E30 | [Inventory reward writer](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/InventoryLootRewardWriter.cs:40). Reward insertion, history and notifications before a proposed admission layer. |
| E31 | [Equipment frontend contract](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/models/equipment-progression.ts), [equipment service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/api/equipment/equipment.service.ts), [progression service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/api/equipment/equipment-progression.service.ts), [loadout service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/api/equipment/equipment-loadout.service.ts). |
| E32 | [Essence definitions](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Essences/Definitions/EssenceDefinition.cs), [slot progression](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Essences/EssenceSlotProgression.cs:5), [Creature Focus](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Essences/CreatureFocusRules.cs:5), [Essence service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Essences/EssenceSystemService.cs:655). |
| E33 | [Combat Style content](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/combat-styles/combat-styles.v1.json), [selection/rules](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/CombatStyles/CombatStyleRules.cs), [mastery progression](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/CombatStyles/CombatStyleProgression.cs:7). |
| E34 | [Historical crafting review](C:/repos/Legends-Legacy/legends-legacy/docs/design/equipment-gathering-crafting-review.md:99), [profession/queue removal migration](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260903115622_RemoveAlphaProfessionsAndTemperingQueues.cs:15), [legacy field removal](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260905105144_RemoveLegacyEquipmentFields.cs:19), [post-Alpha cleanup](C:/repos/Legends-Legacy/legends-legacy/docs/design/equipment-post-alpha-cleanup.md). |
| E35 | [Application layer rules](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/AGENTS.md), [service layer rules](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/AGENTS.md), [equip command/handler](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Equipments/Commands/EquipEquipment/EquipEquipmentCommand.cs), [dismantle command/handler](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Equipments/Commands/DismantleEquipment/DismantleEquipmentCommand.cs). |

## Verification and changed files

Changed by this task: **this report only**. Existing unrelated working-tree changes were left intact. The code review used repository searches, direct C#/TypeScript/JSON reads, structured content counts, cross-checks against historical migrations and current data, and independent audits of domain rules, acquisition, and inventory/economy behavior. Arithmetic in the numerical section was evaluated independently with JavaScript.

Executed through the required backend test entry point:

```powershell
./build/run-tests.ps1 -Filter 'FullyQualifiedName~Equipment|FullyQualifiedName~Loot|FullyQualifiedName~Inventory|FullyQualifiedName~MarketPlace|FullyQualifiedName~Marketplace|FullyQualifiedName~AttributeCombatSystem|FullyQualifiedName~PowerRatingCore|FullyQualifiedName~CombatStyleFoundation'
```

Result: **498 passed, zero failed, zero skipped**; Release build succeeded with zero warnings/errors. The initial sandboxed invocation failed because .NET could not read the user's NuGet configuration. The approved retry outside the sandbox completed successfully. The test run exercised the current implementation, not the proposed model.

Document verification checked all **29 numbered sections**, **106 source links** and their referenced line bounds, with no missing paths or trailing whitespace. Independent read-only reviews corrected acquisition ownership, mixed-family probability assumptions, source supply accounting and offline guarantee overflow before delivery.

Frontend/browser tests and gameplay simulations of the proposed model were not run: there are no frontend changes or implemented proposed generator to test. No database-backed migration rehearsal, live-economy measurement or deployed-environment verification was performed. Those limits matter particularly to effect pricing, time-to-upgrade estimates, concurrency guarantees and migration feasibility.

No package changes, configuration changes, migrations or deployment actions were made. The proposed changes would require all of those relevant coordinated implementation decisions later; this report does not apply them.
