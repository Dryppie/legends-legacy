# Legends Legacy - Game Design Depth Analysis

## Executive Summary

Legends Legacy already has the skeleton of a deep idle fantasy PBBG. The strongest parts are not individual features, but the fact that many systems are already data-driven and interconnected: Essences feed combat, crafting feeds equipment, dungeons feed resources and progression, prophecies guide daily play, guild missions consume normal activity, and achievements/titles track long-term history.

The main design problem is that the game has breadth ahead of depth. Many systems exist, but several risk becoming "do the thing, receive broad currency, repeat." The current design has enough currencies, enough feature surfaces, and enough progression axes. It does not need many new standalone systems right now. It needs stronger reasons to make choices inside existing systems, clearer long-term goals, and more visible consequences from builds, rewards, crafting, dungeon paths, and collection decisions.

The best next phase is consolidation: make Essences and crafted equipment feel build-defining, make dungeons meaningfully different from idle combat, make prophecies and achievements guide experimentation, make guilds create cooperative purpose through existing activities, and audit reward sinks before adding more reward sources.

## Strongest Existing Systems

- **Essences and combat abilities**: The Essence system has loadouts, leveling, potential, ascension, evolution, active/passive abilities, tags, archive ownership, and combat integration. `PlayerEssence`, `EssenceDefinition`, `EssenceLoadout`, and `EssenceProgressionConstants` show a strong foundation for collectible buildcraft rather than simple stat pets.
- **Data-driven combat engine**: `AbilitySpec` supports triggers, conditions, target selectors, statuses, summons, healing, barriers, resource restoration, and attribute modification. This gives the game enough expressive power to create build diversity without introducing classes or new combat frameworks.
- **Dungeons**: `DungeonRunState`, pressure, routes, events, boons, checkpoints, secured/unsecured loot, mastery bonuses, and dungeon-specific JSON definitions provide a real structure for risk/reward gameplay.
- **Crafting and tempering**: The current `base recipe + form + optional blueprint` model is strong. Blueprints, material families, special modifiers, quality, potential, rarity, item XP, and tempering outcomes can support long-term gear goals.
- **Prophecies**: Daily and weekly prophecies already connect to combat, dungeons, crafting, gathering, tempering, Essences, and defeat recovery. This is one of the best retention frameworks in the repository because it can guide behavior without becoming a separate feature island.
- **Guild missions**: Guild missions, personal daily orders, contribution tiers, guild shop, buildings, and contribution hooks from normal play are a good lightweight cooperation model for an idle game.
- **Achievements and titles**: The JSON-backed achievement/title system has hooks across major systems, hidden achievements, title rewards, achievement points, recalculation, and display formatting. It can become a long-term motivation layer rather than just a checklist.

## Biggest Design Weaknesses

- **Too many systems pay broad currencies instead of changing player behavior**: Cinders, Soulstones, Fate Echo, Sigil Fragments, Ascension Stone Fragments, Arena Glory, Guild Favor, Guild Honors, Guild Supplies, Soul Dust, Monster Cores, Essence Potential Cores, materials, blueprints, and equipment all coexist. Without a strict source/sink identity, rewards risk feeling interchangeable.
- **Dungeons may collapse into idle combat plus extra clicks**: The code supports pressure, boons, routes, events, checkpoints, and loot security, but some implementation paths appear flatter than the design intent. For example, room generation heavily favors combat, event handling can resolve into simple reward/pressure outcomes, and older richer room concepts such as treasure, shrine, trap, and checkpoint Essence swap logic appear commented out or reduced.
- **Build decisions are powerful but not yet legible enough**: Combat supports complex ability behavior, Essence modifiers, statuses, summons, and equipment stats. Players need clearer feedback about why a build wins or loses, what an Essence contributed, and what tradeoff they are making.
- **Crafting has depth but risks opaque RNG frustration**: Tempering consumes potential, can generate negative outcomes, can improve quality, can add item XP, and can upgrade rarity. This is potentially exciting, but if players cannot plan around it, it becomes a slot machine attached to expensive gear.
- **Guilds are still mostly parallel solo progress**: Guild missions are valuable, but the current cooperative layer mostly converts individual activity into shared meters. Buildings such as Raid Hall, War Room, Training Grounds, and Essence Sanctum are present as foundations/placeholders, but many listed benefits are not yet implemented.
- **Achievements and titles are underused as motivation tools**: The system is technically broad, but titles appear primarily cosmetic. Achievements can do more to teach, nudge, and reward experimentation across builds, dungeons, crafting, gathering tools, PvP defense, and guild contribution styles.
- **Frontend likely exposes systems but not enough strategic meaning**: Existing Angular pages cover many systems, but the game needs stronger "why this matters now" UX: next goals, reward sources/sinks, build contribution summaries, dungeon decision stakes, and gear comparison.

## System-by-System Analysis

### Character Progression and Idle Actions

- **Current state**: Character progression is connected to idle combat, rewards, equipment, professions, prophecies, achievements, guild contribution hooks, and snapshots.
- **What works**: Idle play is a good fit for the genre. The system can reward both offline accumulation and periodic optimization.
- **What feels shallow or risky**: If idle action choice is mostly "farm the best unlocked area," progression becomes linear and repetitive.
- **Improvement opportunities**: Add clearer tradeoffs between farming XP, materials, Essences, dungeon sigils, guild mission progress, prophecy completion, and crafting goals.
- **Suggested changes**: Add activity intent labels such as "Essence XP focus," "material focus," "guild mission focus," or "blueprint support" using existing reward data. This should guide decisions without adding a new activity type.
- **Implementation complexity**: Medium. Mostly backend aggregation plus frontend display.
- **Expected player impact**: Higher agency during idle setup and better reasons to check in multiple times per day.

### Combat and Build Depth

- **Current state**: Combat has a rich ability model with triggers, conditions, target selectors, statuses, summons, barriers, healing, damage, attribute modification, and Essence/equipment integration.
- **What works**: The engine can support many archetypes: burst, sustain, summons, status stacking, defensive play, crit scaling, life steal, cooldown builds, and hybrid Essence loadouts.
- **What feels shallow or risky**: A complex engine does not automatically create meaningful choices. If a few triggers or stat packages outperform everything, players will converge on obvious best builds.
- **Improvement opportunities**: Use existing combat logs and aggregation to expose build contribution and detect dominant patterns.
- **Suggested changes**: Show per-ability and per-Essence contribution after combat: damage, healing, mitigation, summons, status uptime, deaths prevented, and kill participation.
- **Implementation complexity**: Medium. `CombatStatsAggregator` already provides a useful foundation.
- **Expected player impact**: Players can understand builds, experiment intelligently, and value non-damage roles.

### Essences and Soul Archive

- **Current state**: Essences are collectible, archived, leveled, ascended, evolved, upgraded through potential, and equipped through loadouts. Duplicate unbound Essences can be dismantled for dust, while archived duplicates are rejected.
- **What works**: The system has strong long-term progression and can define builds. Tags, active/passive abilities, ascension scaling, evolution modifiers, and loadouts are a serious foundation.
- **What feels shallow or risky**: Duplicate and unused Essences risk becoming simple dust. Loadouts may become static if players find one optimal combination. Archive ownership may feel like storage rather than a long-term collection journey.
- **Improvement opportunities**: Make the archive matter through existing tags, regions, rarity, achievements, prophecies, and loadout goals.
- **Suggested changes**: Add collection milestones by region/tag/rarity, Essence usage achievements, and recommended loadout experiments tied to prophecies or achievements. Preserve the current system; do not add another Essence layer.
- **Implementation complexity**: Low to medium.
- **Expected player impact**: Essences become collectible, build-defining, and emotionally valuable rather than just upgrade objects.

### Dungeons

- **Current state**: Dungeons include pressure, rooms, routes, events, boons, checkpoints, secured/unsecured loot, boss modifiers, mastery awards, dungeon definitions, route definitions, event definitions, boon definitions, and mastery bonus data.
- **What works**: Pressure and boons are a good idle-friendly dungeon identity. The secured/unsecured loot split can create real risk/reward tension.
- **What feels shallow or risky**: Dungeon rooms can become too combat-heavy. Some room decision concepts appear simplified or commented out. Events can feel like reward/pressure buttons rather than meaningful strategic choices.
- **Improvement opportunities**: Deepen the existing dungeon decision vocabulary: route choice, pressure control, boon selection, checkpoint decisions, event flags, loot security, and boss preparation.
- **Suggested changes**: Make each dungeon family have a clearer identity. Goblin Mines could emphasize alarm/ambush/route control, Catacombs could emphasize curse/attrition/cleansing, Hive could emphasize poison/swarm/resource pressure.
- **Implementation complexity**: Medium to high, depending on how much existing event/route data is expanded.
- **Expected player impact**: Dungeons become a distinct mode rather than idle combat with a boss at the end.

### Crafting, Equipment, Blueprints, and Tempering

- **Current state**: Crafting uses base recipes, forms, blueprints, materials, special resources, crafted equipment instances, quality, potential, rarity, affixes, special modifiers, and tempering. Tools are not craftable.
- **What works**: The move from exact recipes to recipe/form/blueprint composition is excellent. It gives the game a scalable gear chase without needing endless bespoke items.
- **What feels shallow or risky**: Tempering can become opaque RNG. Blueprints may become simple unlocks instead of long-term identity choices. Gear comparison and crafting goals may be hard to reason about.
- **Improvement opportunities**: Make crafted gear feel like a project. Let players understand what they are trying to make, what materials they need, and what risks tempering introduces.
- **Suggested changes**: Add crafting goal previews, source links for missing materials, tempering outcome history, pity/guardrails for extreme bad luck, and recipe mastery milestones.
- **Implementation complexity**: Medium.
- **Expected player impact**: Crafted gear remains exciting over time and players feel ownership over gear progression.

### Gathering

- **Current state**: Gathering is tied to area and dungeon nodes, tools, profession levels, proc chances, loot tables, and combat reward processing.
- **What works**: Gathering supports the idle nature of the game. It links combat, tools, professions, and crafting materials without requiring active minigames.
- **What feels shallow or risky**: The primary decision may be only "equip a tool and farm the right area." Node identity, tool specialization, and material demand may not be visible enough.
- **Improvement opportunities**: Deepen gathering through planning, specialization, and reward visibility rather than manual actions.
- **Suggested changes**: Show expected gathered resources per area, tool bonus impact, profession progress, and which crafting goals each resource supports.
- **Implementation complexity**: Low to medium.
- **Expected player impact**: Gathering becomes a meaningful support pillar for crafting instead of passive incidental loot.

### PvP, Colosseum, and Tournament Grounds

- **Current state**: Colosseum includes tickets, rating, battle history, Glory, streaks, defense snapshots, rank tiers, first-win rewards, Champion's Market, and tournament-related endpoints/services.
- **What works**: PvP has more than rating: market purchases, weekly caches, rank requirements, records, and tournaments all create goals.
- **What feels shallow or risky**: If PvP rewards too many general PvE currencies, it can become mandatory for non-PvP players. If defense snapshots are not meaningful or current, PvP becomes stale.
- **Improvement opportunities**: Make PvP reward prestige, build experimentation, and defensive identity more than raw economic efficiency.
- **Suggested changes**: Emphasize titles, banners, cosmetics, seasonal records, defense performance summaries, and build matchup feedback. Keep Cinders/Soulstones as secondary rewards.
- **Implementation complexity**: Medium.
- **Expected player impact**: PvP becomes engaging for competitive players without feeling compulsory for everyone.

### Guilds

- **Current state**: Guilds include members, Guild XP/level, Favor, Honors, Supplies, missions, personal orders, contribution tiers, shop stock, buildings, logs, and contribution hooks from combat, dungeons, crafting, and tempering.
- **What works**: The current system smartly avoids heavy coordination requirements while still making normal play benefit the guild.
- **What feels shallow or risky**: Guild progression may feel like solo chores pooled into shared meters. Several buildings are present as foundations but do not yet strongly change play.
- **Improvement opportunities**: Use existing missions, orders, buildings, and shop stock to create weekly guild identity.
- **Suggested changes**: Let buildings influence mission pools, contribution bonuses, shop stock, or preferred activities. Add role-like contribution paths without adding formal roles.
- **Implementation complexity**: Medium.
- **Expected player impact**: Guilds feel cooperative and strategic while remaining idle-friendly.

### Achievements and Titles

- **Current state**: Achievements and titles are JSON-backed, categorized by system, support hidden achievements, title rewards, achievement points, display formatting, recalculation, and system chat announcements.
- **What works**: The system has broad hooks and can support long-term collection, social recognition, and account identity.
- **What feels shallow or risky**: Achievements can become passive checkboxes. Titles appear mostly cosmetic, which is acceptable, but they need stronger prestige context.
- **Improvement opportunities**: Use achievements to encourage underused systems, alternate builds, dungeon decisions, crafting approaches, and PvP defense styles.
- **Suggested changes**: Add achievement chains for "win with three Essence families," "clear dungeon with high pressure," "craft with low potential remaining," "complete guild orders across categories," and "win PvP with defensive loadout."
- **Implementation complexity**: Low to medium.
- **Expected player impact**: Achievements become a design tool that teaches depth and creates long-term goals.

### Prophecies and Daily Retention

- **Current state**: Prophecies provide daily choices, a weekly Greater Prophecy, favor milestones, reward snapshots, caches, live progress, and contextual links.
- **What works**: This is a strong daily system because it already touches combat, dungeons, crafting, gathering, tempering, Essences, and recovery from defeat.
- **What feels shallow or risky**: If prophecy objectives become generic chores, the system will feel like mandatory daily homework.
- **Improvement opportunities**: Use prophecies to nudge meaningful variety and to revive older systems later in progression.
- **Suggested changes**: Weight prophecies toward the player's neglected but relevant systems: unused Essence tags, underleveled professions, older dungeons with missing mastery, untested crafting forms, or guild mission alignment.
- **Implementation complexity**: Medium.
- **Expected player impact**: Daily play feels guided and varied without requiring constant manual attention.

### Rewards, Loot, and Economy

- **Current state**: The economy includes many currencies and resources across combat, dungeons, Essences, crafting, guilds, PvP, prophecies, achievements, and marketplace-related surfaces.
- **What works**: Many systems already have reward hooks, which makes cross-system progression possible.
- **What feels shallow or risky**: Broad currency rewards appear in many places. This can create inflation, dead resources, and unclear reward identity.
- **Improvement opportunities**: Define each resource by source, sink, player emotion, and progression tier.
- **Suggested changes**: Create an economy matrix before adding rewards: every currency should have primary sources, primary sinks, secondary sinks, target velocity, and late-game relevance.
- **Implementation complexity**: Medium.
- **Expected player impact**: Rewards become clearer, more exciting, and less likely to collapse into "just more currency."

### Frontend and UX

- **Current state**: The frontend appears to have dedicated flows for prophecies, achievements/titles, guilds, Colosseum, crafting, and other major systems.
- **What works**: Most major systems have a UI home, and several docs mention contextual links, filters, modals, tabs, and progress displays.
- **What feels shallow or risky**: Complexity may be visible without being understandable. Players may see many systems but not know what matters next.
- **Improvement opportunities**: Add strategic summaries instead of more pages.
- **Suggested changes**: Add "next best goals," reward source/sink tooltips, build contribution summaries, dungeon decision previews, gear comparison panels, and material source links.
- **Implementation complexity**: Medium.
- **Expected player impact**: Players understand why they should care about systems and are more likely to return with a goal.

## Cross-System Depth Opportunities

- **Essences affecting crafting goals**: Use Essence tags and builds to recommend crafted gear affixes. A summon-heavy Essence loadout should point players toward SummonPower, cooldown, survivability, or relevant blueprint families.
- **Dungeons feeding crafting progression**: Dungeon families should have recognizable material, blueprint, and special modifier identities. Clearing a dungeon should not only give currency; it should move a specific crafting project forward.
- **Achievements encouraging build experimentation**: Achievements should reward alternate playstyles, not just volume. Examples: clear a dungeon with no healing Essence, win PvP with three defensive Essences, complete prophecy objectives using an underused Essence family.
- **Guilds giving purpose to existing activities**: Guild missions should rotate focus across combat, dungeons, crafting, tempering, and gathering. Buildings can bias which mission types are more rewarding.
- **PvP rewarding different builds**: PvP should highlight matchup identity: sustain defense, burst offense, anti-summon, status cleanse, dodge/block, barrier stacking. Battle history should teach why a defense won or failed.
- **Soul Archive supporting account-wide goals**: Archive completion by region, rarity, monster family, tag, or evolution path can create long-term collection goals without inventing new mechanics.
- **Prophecies reviving older content**: Daily/weekly objectives can send players back to older dungeons, older gathering nodes, or lower-tier crafting goals when those systems still provide relevant secondary rewards.
- **Gathering supporting equipment identity**: Tools and gathering nodes should visibly map to crafting outcomes. The player should know which area supports the item they want to make.
- **Tempering feeding achievements and guild missions**: Risky tempering outcomes, low-potential successes, and upgraded rarity milestones can become prestige achievements and guild contribution events.
- **Economy connecting prestige and progression**: PvP and guild systems should lean toward prestige, choice, and targeted support instead of becoming universal currency faucets.

## Priority Improvement Roadmap

### Tier 1 - Highest Impact / Lowest Risk

1. Add build contribution summaries using existing combat aggregation.
2. Improve reward clarity with source/sink displays for currencies and materials.
3. Add material and blueprint source links in crafting UI.
4. Add dungeon decision previews for pressure, loot risk, boon impact, and route consequences.
5. Add achievement and prophecy objectives that encourage build variety and older-system engagement.
6. Audit broad currency rewards across prophecies, guild shop, PvP market, tournaments, and dungeons.

### Tier 2 - High Value / Medium Complexity

1. Strengthen Essence Archive milestones by region, tag, rarity, and evolution.
2. Add recipe mastery and crafting project tracking.
3. Improve tempering with clearer odds, history, and bad-luck protection.
4. Make dungeon event tables and route choices more dungeon-specific.
5. Let guild buildings modify mission pools, shop stock, or contribution bonuses.
6. Add Colosseum defense performance summaries and matchup feedback.

### Tier 3 - Larger Design Investments

1. Redesign dungeon families around distinct mechanical identities.
2. Add deeper late-game Essence collection goals without increasing ability-system complexity.
3. Rebalance crafting, tempering, and equipment around build archetypes.
4. Create a full economy matrix with currency velocity targets and late-game sinks.
5. Expand tournament seasons only after PvP rewards and defense snapshots are stable.
6. Turn guild buildings into long-term strategic identity choices before adding raids or wars.

## Top 10 Recommended Improvements

### 1. Add a Cross-System "Next Goals" Layer

- **Problem**: Players may have many systems available but no clear sense of what matters next.
- **Proposed solution**: Add goal recommendations based on existing state: upgrade an Essence, attempt a dungeon, finish a prophecy, craft a target item, complete a guild order, or farm a needed material.
- **Why it improves depth**: It turns breadth into directed motivation.
- **Affected systems**: Prophecies, crafting, Essences, dungeons, guilds, achievements, frontend.
- **Backend impact**: Add aggregation/query service; no new core gameplay required.
- **Frontend impact**: Add compact goal cards and contextual links.
- **Data/model impact**: May need lightweight goal metadata.
- **Balance risks**: Recommendations must not always point to the mathematically optimal grind.
- **Suggested implementation approach**: Start read-only. Generate goals from existing progression gaps.

### 2. Make Essence Archive Collection Matter More

- **Problem**: Essences are strong individually, but unused or duplicate Essences risk becoming dust or storage.
- **Proposed solution**: Add archive milestones by region, tag, rarity, ascension, and evolution.
- **Why it improves depth**: Collecting supports long-term goals and build experimentation.
- **Affected systems**: Essences, Soul Archive, achievements, prophecies, combat.
- **Backend impact**: Mostly achievement/progress queries.
- **Frontend impact**: Archive progress displays and missing Essence hints.
- **Data/model impact**: Add milestone definitions if not encoded as achievements.
- **Balance risks**: Avoid account-wide power inflation from collection bonuses.
- **Suggested implementation approach**: Prefer cosmetic/prestige/reward milestones first.

### 3. Make Dungeons More Decision-Driven

- **Problem**: Dungeon systems exist, but decisions may not yet feel meaningfully different from idle combat.
- **Proposed solution**: Strengthen route, event, checkpoint, boon, pressure, and loot security decisions.
- **Why it improves depth**: Dungeons become the main place where idle progression turns into strategic risk.
- **Affected systems**: Dungeons, combat, rewards, crafting, Essences.
- **Backend impact**: Expand event/route resolution and dungeon-specific definitions.
- **Frontend impact**: Clear previews for pressure, reward, risk, and boon consequences.
- **Data/model impact**: Add or tune dungeon event tables, route flags, and boon families.
- **Balance risks**: Too much manual decision-making can hurt idle pacing.
- **Suggested implementation approach**: Improve one dungeon family first before scaling.

### 4. Turn Crafting Into Long-Term Gear Projects

- **Problem**: Crafting has strong mechanics but may feel opaque or overly RNG-driven.
- **Proposed solution**: Add crafting projects, recipe mastery, material source visibility, and tempering history.
- **Why it improves depth**: Players can plan gear instead of simply rolling gear.
- **Affected systems**: Crafting, equipment, gathering, dungeons, blueprints, frontend.
- **Backend impact**: Add tracking and query support.
- **Frontend impact**: Gear project panel, comparison view, missing material links.
- **Data/model impact**: Possible recipe mastery definitions.
- **Balance risks**: Too much determinism can remove excitement from tempering.
- **Suggested implementation approach**: Start with visibility, then add mastery rewards.

### 5. Add Tempering Guardrails

- **Problem**: Negative tempering outcomes can feel punitive if players lack control or recovery paths.
- **Proposed solution**: Add clearer odds, outcome history, and limited protection against extreme bad streaks.
- **Why it improves depth**: Risk remains, but players trust the system.
- **Affected systems**: Tempering, equipment, crafting, achievements.
- **Backend impact**: Track recent tempering outcomes or item tempering history.
- **Frontend impact**: Show odds, potential cost, recent results, and item trajectory.
- **Data/model impact**: Possible balance config for pity thresholds.
- **Balance risks**: Guardrails may flood the economy with strong gear.
- **Suggested implementation approach**: Add transparency first, then tune protection conservatively.

### 6. Use Prophecies to Encourage Variety

- **Problem**: Daily systems can become chores.
- **Proposed solution**: Weight prophecies toward neglected but useful systems and alternate playstyles.
- **Why it improves depth**: Daily play becomes a rotating exploration prompt.
- **Affected systems**: Prophecies, achievements, Essences, crafting, dungeons, gathering.
- **Backend impact**: Improve prophecy selection logic.
- **Frontend impact**: Show why a prophecy is relevant.
- **Data/model impact**: Add tags or recommendation metadata to prophecy definitions.
- **Balance risks**: Avoid asking players to do inefficient or frustrating tasks.
- **Suggested implementation approach**: Add soft weighting, not hard forcing.

### 7. Make Guild Buildings Affect Existing Loops

- **Problem**: Guild buildings exist, but several are foundations rather than meaningful live choices.
- **Proposed solution**: Let buildings influence missions, contribution bonuses, shop stock, or personal orders.
- **Why it improves depth**: Guild progression changes weekly priorities without adding raids or wars.
- **Affected systems**: Guilds, missions, shop, crafting, dungeons, Essences.
- **Backend impact**: Apply building modifiers to existing guild generation/reward flows.
- **Frontend impact**: Show building effects and guild strategic direction.
- **Data/model impact**: Implement currently placeholder benefit definitions selectively.
- **Balance risks**: Large guilds may compound advantages too quickly.
- **Suggested implementation approach**: Begin with mission/shop modifiers, not combat power.

### 8. Make PvP Prestige-First

- **Problem**: PvP risks becoming a mandatory currency source.
- **Proposed solution**: Emphasize titles, banners, records, seasonal history, defense performance, and matchup reports.
- **Why it improves depth**: Competitive players gain identity while PvE players do not feel forced.
- **Affected systems**: Colosseum, tournaments, achievements, titles, frontend.
- **Backend impact**: More battle summary and season metadata.
- **Frontend impact**: Better battle history, defense analytics, reward presentation.
- **Data/model impact**: Add prestige reward definitions.
- **Balance risks**: Prestige rewards need enough desirability without power creep.
- **Suggested implementation approach**: Reduce emphasis on broad weekly currency caches over time.

### 9. Clarify Gathering's Role in Crafting

- **Problem**: Gathering may feel too passive and disconnected from gear goals.
- **Proposed solution**: Show area node yields, tool bonuses, expected resource rates, and linked crafting uses.
- **Why it improves depth**: Players make informed idle setup decisions.
- **Affected systems**: Gathering, crafting, tools, professions, frontend.
- **Backend impact**: Aggregate node/tool/profession reward expectations.
- **Frontend impact**: Area resource panels and crafting dependency links.
- **Data/model impact**: Mostly none if loot tables are already sufficient.
- **Balance risks**: Exact expected values can make players optimize too narrowly.
- **Suggested implementation approach**: Show qualitative rates first, exact values later if desired.

### 10. Create an Economy Matrix Before Adding Rewards

- **Problem**: The game already has many currencies and reward channels.
- **Proposed solution**: Define each currency's purpose, source, sink, progression tier, and expected velocity.
- **Why it improves depth**: Rewards stay meaningful and resources avoid becoming dead drops.
- **Affected systems**: All reward systems.
- **Backend impact**: May reveal need for reward config consolidation.
- **Frontend impact**: Better currency explanations and source/sink views.
- **Data/model impact**: Add economy documentation and possibly move hardcoded reward tables into data.
- **Balance risks**: Rebalancing may affect player expectations.
- **Suggested implementation approach**: Audit first, rebalance second, communicate clearly.

## Systems That Should NOT Be Expanded Yet

- **Guild raids and wars**: The building foundations exist, but the current guild loop should be deepened before adding large coordinated systems.
- **New regions or large new monster batches**: Region One already has enough systems needing depth. More content will not solve unclear decisions.
- **New currencies**: The economy already has many currencies and fragments. Add sinks and purpose before adding more reward types.
- **New combat classes or standalone build systems**: Essences, equipment, abilities, stats, statuses, and summons already provide enough build surface.
- **Active gathering minigames**: Gathering should remain idle-friendly and support crafting. Active play would fight the genre unless introduced very carefully.
- **Marketplace expansion**: Economy balance should be audited before player trading or market complexity becomes more important.
- **More dungeon families**: Existing dungeon systems should be made excellent first. One deep dungeon family is more valuable than several shallow ones.
- **Power-bearing titles**: Titles should remain prestige, identity, or light utility at most. Turning them into major stat sources risks mandatory cosmetic optimization.

## Dangerous Design Traps To Avoid

- Adding new standalone features instead of improving existing loops.
- Creating more currencies to solve reward problems.
- Making idle systems require constant manual attention.
- Letting one Essence or gear archetype become the obvious best answer.
- Making PvP mandatory through general progression rewards.
- Letting guild buildings promise systems that do not affect real play.
- Turning dungeons into idle combat with extra confirmation buttons.
- Making tempering feel like pure punishment or pure gambling.
- Using achievements only as volume counters.
- Hiding important decisions behind complex data without UI explanation.
- Rewarding everything with Cinders/Soulstones until all activities feel the same.
- Expanding content before the game has a strong answer to "why log in today?"

## Concrete Next Steps

1. Create an economy and reward audit covering Cinders, Soulstones, Fate Echo, Sigil Fragments, Ascension Stone Fragments, Arena Glory, Guild Favor, Guild Honors, Guild Supplies, Soul Dust, Monster Cores, Essence Potential Cores, materials, blueprints, and equipment.
2. Improve combat and build feedback first: expose ability, Essence, status, summon, mitigation, and healing contributions after combat.
3. Pick one dungeon family and deepen it end-to-end with stronger route choices, event flags, pressure decisions, boon identity, checkpoint tradeoffs, and reward clarity.
4. Add crafting project visibility: target item, required materials, blueprint source, expected stat identity, tempering risk, and current best comparison.
5. Expand achievements and prophecies as guidance tools: reward build experiments, older dungeon mastery, crafting variety, gathering specialization, and guild contribution diversity.
6. Make guild buildings affect existing guild missions, orders, or shop stock before implementing raids or wars.
7. Reposition Colosseum rewards toward prestige and battle insight, keeping general progression rewards secondary.
8. Do not implement new large features until the above loops make existing systems feel more connected, more strategic, and easier to understand.
