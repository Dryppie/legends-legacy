# Gathering System Progression Analysis

> Historical Alpha plan, superseded 3 September 2026. Crafting/gathering progression, queued tempering and their obsolete quest content have been removed. Conversion, refund and compatibility/backfill proposals below are not current implementation work. Shared numerical helpers with active consumers may remain. See the [post-Alpha cleanup](design/equipment-post-alpha-cleanup.md) and [current quest flow](../LEGENDSLEGACY_QUEST_FLOW.md) for supported behavior.

The core issue is sharper than “Gathering is bare-bones”:

> LegendsLegacy persists Gathering levels, but current Gathering levels almost never affect Gathering.

The repository already contains a good idle-friendly foundation—combat-adjacent collection, one equipped tool, data-driven nodes, regional abundance, tool affixes, dungeon bonuses, Soulstone upgrades, and trading. The strongest redesign is therefore not a separate minigame. It is to give profession levels actual capabilities, make locations materially different, and turn existing tools into understandable strategic choices.

One important scope finding: the design may target 10 Regions, but the current repository only authors two—Shenic and the first two Meran areas. The recommendation below extrapolates a consistent model across the intended 10 Regions.

## 1. Existing System

### Professions

There are three active Gathering professions:

- Mining
- Woodcutting
- Skinning

Fishing is retired and filtered from visible professions. Each active profession has an independent persisted `Level` and `Experience`.

Relevant implementation:

- `LL/src/Core/Domain/Models/Professions/ProfessionType.cs`
- `LL/src/Infrastructure/Service/Services.LL/Professions/ProfessionService.cs`

### Actual Runtime Loop

Gathering is not a standalone action.

The real loop is:

> Equip one Gathering tool → choose a combat area or dungeon → start or continue combat → each victory attempts the matching node → receive resources and 1 profession XP on a successful proc → repeat until changing tool, area, or action.

A Pickaxe only processes Mining nodes, a Hatchet Woodcutting nodes, and a Skinning Knife Skinning nodes. No tool means no Gathering.

This is important: the player chooses both a combat destination and a Gathering target with the same idle session.

### Starting and Completing Gathering

For normal areas:

1. The frontend starts a combat action with an `AreaId`.
2. The server creates a persistent combat `CharacterAction`.
3. The first encounter resolves immediately.
4. Later encounters resolve through client polling or offline catch-up.
5. Gathering is calculated as part of the combat reward calculation.
6. Loot, profession XP, currencies, quest progress, and prophecy progress are persisted through the normal transaction and outbox flow.

There is no independent Gathering completion timer and no Gathering background job.

### Duration and Throughput

Normal idle combat has:

- One encounter every 10 seconds.
- A maximum of 24 hours of offline rewards.
- A 10-second combat action switching lock.
- No Gathering speed stat.
- No level-based Gathering speed improvement.

Relevant implementation:

- `LL/src/API/API.LL/appsettings.json`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Idle/IdleCombatPlanner.cs`
- `LL/src/Core/Domain/Models/CharacterActions/CharacterActionTimingConstants.cs`

Each victory gives each matching node one proc attempt. Therefore Gathering throughput depends indirectly on combat success, but not on profession level or action duration modifiers.

Dungeon Gathering also resolves per victorious encounter, using the equipped tool and the dungeon's authored nodes.

### Nodes and Rewards

An area node contains:

- Node ID and name
- Gathering type
- Optional profession-level requirement
- Proc chance
- Area yield bonus
- Reward-table reference

Areas are intended to contain at most one node per Gathering type. Nodes are persisted database entities seeded from the world JSON.

The processor:

1. Finds nodes matching the equipped tool.
2. Removes nodes above the profession level.
3. Rolls success once per victory.
4. Optionally makes a bonus reward roll.
5. Applies tool yield, node-specific yield, area yield, double-gather, Soulstone yield, and rare-weight bonuses.
6. Awards 1 profession XP per successful node proc.

Relevant implementation:

- `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Idle/CombatGatheringRewardProcessor.cs`
- `LL/src/Core/Domain/Models/Regions/Areas/AreaGatheringNode.cs`
- `LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Models/CombatGatheringNode.cs`

Normal area nodes currently use a `0.0037`, or 0.37%, proc chance. At 100% combat wins:

- 360 attempts per hour
- Approximately 1.33 successful procs per hour
- Approximately 1.33 base profession XP per hour

Dungeon nodes use much higher authored chances, currently roughly 18–45%, but dungeon access and encounter counts constrain them.

### Area Abundance

Normal areas use a baseline yield multiplier of `2/3`. An Abundant area adds 50%, bringing the multiplier to `1.0`.

Relevant implementation:

- `LL/src/Core/Domain/Models/Regions/Areas/AreaGatheringYieldBalance.cs`

For the Tier-1 reward range of 8–24 resources:

- Normal area: roughly 10.7 resources per successful proc
- Abundant area: 16 resources per successful proc
- At perfect win rate: approximately 14 versus 21 resources per hour before tool and Soulstone bonuses

This is currently the main meaningful normal-area Gathering choice.

### Current Regional Content

The backend currently authors:

- Shenic: 10 normal idle areas plus the tutorial area
- Meran: 2 normal idle areas
- No authored Regions 3–10 yet

Every non-training authored area contains all three Gathering node types. The frontend only marks whichever types are Abundant.

Shenic's named nodes mostly point back to the same three reward tables:

- Mining → Ore
- Woodcutting → Wood
- Skinning → Hide

Thus Crystal Seam, Grave Ore, Ember Ore Seam, and similar nodes are mostly presentation names for the same Ore result.

Meran introduces:

- Copper Ore
- Bloodwood
- Thick Hide

Those are Tier-2 versions of the same Metal, Wood, and Hide crafting families.

Relevant content:

- `LL/src/API/API.LL/Data/world/regions.json`
- `LL/src/API/API.LL/Data/rewards/reward-tables.json`
- `LL/src/API/API.LL/Data/crafting/materials.json`

### Profession XP and Levels

The profession XP curve is the generic entity-level curve. Level-up consumes the required XP and carries over excess XP.

However:

- All Shenic nodes have no profession requirement.
- All Meran nodes require only profession level 1.
- Every current dungeon node requires level 1.
- Profession level does not increase yield.
- Profession level does not increase proc chance.
- Profession level does not increase rare chance.
- Profession level does not improve tools.
- Profession level does not unlock a technique, perk, UI, or reward table.

At the base Shenic area rate and perfect wins:

- Level 1→2 requires 104 XP, approximately 78 hours.
- Reaching level 10 requires 2,110 cumulative XP, approximately 66 days.
- Reaching level 80 would take approximately 62 continuous area-only years.

Dungeons and bonuses can accelerate this, but the figures expose a fundamental tuning mismatch: a steep long-term curve exists without corresponding rewards or milestones.

### Tools and Equipment

Gathering tools occupy a separate Tool equipment slot, so they do not replace combat weapons. Only one can be equipped.

Starter tools are awarded by onboarding. Higher-rarity tools appear in combat reward tables. They are not currently craftable or temperable.

When a tool instance is created, it rolls affixes based on rarity:

- Abundant: yield
- Reliable: node success
- Prospector's: rare material weighting
- Duplicating: double yield
- Opportunist's: extra reward roll

Common tools have no affixes; higher rarities receive up to three.

Relevant implementation:

- `LL/src/Infrastructure/Service/Services.LL/Inventories/ToolAffixGenerator.cs`
- `LL/src/Infrastructure/Service/Services.LL/Inventories/InventoryItemFactory.cs`
- `LL/src/Core/Domain/Models/Items/Equipments/Tools/ToolBonusType.cs`

This is a useful foundation, but three affixes—general yield, double yield, and extra roll—currently overlap heavily as “more resources.”

More seriously, the current area reward tables have no rare-tagged Gathering entries, and dungeon Gathering loot has no authored rare entries. Consequently:

- Prospector's tools have nothing meaningful to improve.
- The Soulstone Rare Node Sense upgrade has nothing meaningful to improve.

The infrastructure works, but the content it expects does not yet exist.

### Other Modifiers and Progression

Gathering interacts with:

- **Soulstones:** up to 5% yield, 7.5% Gathering XP, and 10% relative rare weight through the current five-rank upgrades.
- **Dungeon mastery:** mastery levels 3 and 8 each add 5 percentage points to dungeon Gathering chance. This is one of the few existing examples of mechanical Gathering progression worth preserving.
- **Quests:** onboarding teaches tool selection; one side quest asks the player to use all three tools.
- **Prophecies:** daily profession-specific Gathering objectives and a weekly general Gathering objective.
- **Leaderboards:** separate Mining, Woodcutting, and Skinning boards; profession levels also contribute to the overall level board.
- **Achievements and titles:** model categories can represent Gathering, but there is no current Gathering achievement or title catalog.
- **Guilds:** `ResourcesGathered` exists as a guild mission metric, but no current Gathering flow records it.

Relevant implementation and content:

- `LL/src/API/API.LL/Data/progression/soulstone-upgrades.json`
- `LL/src/Core/Domain/Models/Dungeons/Mastery/DungeonMasteryBenefits.cs`
- `LL/src/API/API.LL/Data/quests/side-quests/stone-timber-and-hide.v1.json`
- `LL/src/API/API.LL/Data/prophecies/daily.json`
- `LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Leaderboards/LeaderboardRepository.cs`

### Crafting Integration

Gathered materials are the standard tier-defining crafting materials:

- Metal
- Wood
- Hide

Crafting selects the material matching the requested equipment tier. Current content has only Tier 1 and Tier 2 standard Gathering materials, and all 31 current recipes support tiers 1–2 with minimum Crafting level 1.

Special crafting resources come mainly from dungeon rewards, not Gathering.

This makes Gathering important as a bulk supplier, but gives gathered resources little identity beyond family and tier.

### Economy and Trading

Gathered resources are stackable and unbound, so they can be:

- Listed on the marketplace
- Purchased through buy orders
- Used in crafting
- Deposited or otherwise moved through normal inventory systems

Tools are non-stackable equipment and can be individually traded when unbound.

Thus Gathering already has a legitimate economic role. Its weakness is not absence of a market—it is that gatherers at different experience levels supply effectively identical goods in effectively identical ways.

### Persistence and Background Processing

The system persists:

- One profession row per character and profession
- Profession level and current XP
- Area and node definitions
- Persistent combat action and next resolution boundary
- Gathered inventory resources
- Tool instances and rolled affixes
- Quest, prophecy, leaderboard, loot-history, and economy side effects

Offline progress is resolved lazily when the API receives a resolve or polling request. It is deterministic and batched, rather than continuously simulated by the worker service. There is no separate Gathering session or active Gathering worker.

An unused `GatheringSummary` domain class exists, but the runtime communicates Gathering through combat results instead.

### Frontend Communication

What the frontend does well:

- Onboarding clearly explains equipping one tool.
- Area cards clearly mark Abundant types and their +50% yield.
- Active combat warns when the equipped tool does not match available nodes.
- Dungeon previews show node type, level requirement, proc chance, and loot range.
- Tool equipment views expose affixes.

What is missing:

- There is no Gathering profession page; the Professions route only exposes Crafting.
- Character Overview displays Crafting level, but not Mining, Woodcutting, or Skinning.
- Combat summaries classify gathered resources under Crafting.
- Gathering XP is not shown in the summary.
- Level-ups, next unlocks, expected output, node requirements, rare pools, and applied bonuses are not surfaced.
- The server returns detailed `GatheringRewards` and `AppliedBonusEffects`, but no normal combat UI renders them.
- The combat client applies character XP but does not directly apply returned Gathering XP to visible profession state.

The player can mostly see that resources appeared, not why they appeared or how their Gathering character improved.

## 2. Actual Design Problems

### Clearly Present

- **Profession levels are currently vestigial gates.** The gate exists technically, but all authored gates are level 1 or absent.
- **Level 80 would be mechanically identical to level 5.**
- **Higher resource tiers work identically.** Copper Ore is Tier-2 Ore; Bloodwood is Tier-2 Wood.
- **No specialization or profession-specific capabilities exist.**
- **There are no profession goals beyond level and leaderboard rank.**
- **Area choice is usually obvious:** choose the Abundant area for the desired material, unless combat or quest rewards override it.
- **Named nodes have weak identities.** Many names reuse the same reward table.
- **Gathering is subordinate to Crafting.** Prophecies and trading help, but almost every resource's principal identity is “crafting material.”
- **Rare-find systems are configured but have no content.**
- **The player cannot see or compare Gathering efficiency.**
- **The progression curve is dramatically slower than the current content supports.**
- **Combat progression controls Gathering access more than Gathering progression does.** Character level, quests, and Tower floor unlock regions; Gathering level contributes almost nothing.

### Partially Present, Not Absent

- **The player does have something to optimize:** tool affixes, abundance, dungeon mastery, market demand, prophecy target, and combat win rate.
- **Earlier content is not completely useless:** Tier-1 recipes and newer players sustain Tier-1 demand. But an expert gatherer gains no new way to interact with it.
- **Gathering is not purely larger numbers:** tool choice selects a profession. The problem is that progression after that initial choice adds no new possibilities.

### Not a Current Problem

Gathering does not require excessive micromanagement. Its passive attachment to combat is one of the system's best properties and should be preserved.

## 3. What Progression Should Mean

Good Gathering progression for LegendsLegacy should have five dimensions.

### Horizontal Progression

Progress unlocks:

- New material tiers
- New node profiles
- Regional secondary materials
- Difficult or hidden opportunities
- Rare discovery pools
- New preparation options

### Vertical Progression

Use only a few legible axes:

- Access to better node grades
- Reliability
- Primary yield
- Discovery capability

Speed is a poor primary lever because Gathering shares the combat encounter cadence. Stacking many small percentages would also be difficult to understand.

### Mechanical Progression

Higher progression should change the pre-session setup:

- Which region and area?
- Reliable node, rich node, or discovery-oriented node?
- Which tool profile?
- Which unlocked Gathering technique?
- Is the objective bulk material, a regional secondary, or an uncommon discovery?

These choices happen before an idle session, not during it.

### Economic Progression

Materials need distinct economic roles:

- **Core materials:** high-volume tiered inputs
- **Regional signatures:** lower-volume optional crafting inputs with specific purposes
- **Discoveries:** blueprints, tool components, cosmetics, or similarly infrequent rewards

This is enough differentiation. Every ore stack does not need quality metadata.

### Long-Term Progression

A long-term gatherer should pursue:

- Profession capability milestones
- Access to high-grade and discovery nodes
- A specialized tool collection
- Regional discoveries
- Efficient supply of valuable regional materials
- Existing profession leaderboards

The answer should not be an infinite generic percentage tree or prestige reset.

## 4. Four Viable Directions

### Approach A — Profession Capabilities and Techniques

1. **Core idea:** profession levels unlock milestone capabilities and a very small set of Gathering techniques.
2. **Player change:** level becomes a reason to continue, not just a number.
3. **Early to late:** early levels unlock baseline visibility and node access; midgame unlocks a choice such as steady harvest versus discovery focus; late levels unlock difficult node grades and expert surveys.
4. **Integration:** uses existing profession rows, node requirements, and Soulstone modifiers.
5. **Benefit:** directly solves meaningless levels.
6. **Downside:** risks becoming a generic skill tree or locking players out of essential materials.
7. **Complexity:** medium.
8. **Fit:** high, provided milestones are sparse and baseline regional materials remain accessible.

Important safeguard: high Gathering level should unlock better opportunities, not block a player from obtaining a region's basic crafting material.

### Approach B — Regional Nodes and Resource Identity

1. **Core idea:** locations have distinct node profiles and regional reward pools.
2. **Player change:** area choice becomes a Gathering decision alongside combat efficiency.
3. **Early to late:** Region 1 teaches abundance; Regions 2–3 add reliable versus rich locations and one regional secondary; later Regions add discovery nodes and difficult surveys using the same vocabulary.
4. **Integration:** directly extends existing area nodes, yield bonuses, reward tables, and data-driven crafting special-resource requirements.
5. **Benefit:** solves identical locations, obvious choices, weak resource identity, and lack of earlier-region goals.
6. **Downside:** content and economy balancing are substantial; combat rewards can overwhelm the Gathering decision.
7. **Complexity:** medium code complexity, high content and balance effort.
8. **Fit:** very high. This is the most natural extension of the current architecture.

### Approach C — Tool-Driven Progression

1. **Core idea:** Gathering strategy is primarily expressed through tools and their profiles.
2. **Player change:** players maintain tools for reliability, bulk yield, or discovery rather than merely replacing Common with Rare.
3. **Early to late:** starter tools gather normally; later tools gain defined profiles, targeted tuning, and region or node affinities.
4. **Integration:** builds on the existing Tool slot, affix persistence, rarity drops, equipment UI, and marketplace.
5. **Benefit:** creates optimization and trade without adding active play.
6. **Downside:** as the primary system, progression would depend more on combat drops and RNG than on being an experienced gatherer.
7. **Complexity:** low-to-medium because much infrastructure exists.
8. **Fit:** high as a supporting layer, medium as the foundation.

The current three volume modifiers should be clarified or consolidated. For example:

- Yield affects the primary material.
- Bonus roll creates another chance at secondary or discovery entries.
- Double Gather is removed or reserved for unusual tools.

That produces distinct behaviors instead of three versions of “more quantity.”

### Approach D — Dedicated Gathering Expeditions

1. **Core idea:** let players start a separate long Gathering expedition with profession, location, duration, and plan.
2. **Player change:** Gathering becomes an independent profession rather than a combat side reward.
3. **Early to late:** short simple expeditions expand into longer specialist surveys and rare opportunities.
4. **Integration:** could reuse `CharacterAction` scheduling and offline resolution.
5. **Benefit:** gives Gathering the strongest independent identity and cleanest output forecasting.
6. **Downside:** competes with combat and tempering for the single action slot, duplicates location logic, and risks turning Gathering into a disconnected menu.
7. **Complexity:** high.
8. **Fit:** medium-low for the present game.

This should not be chosen now. The combat-adjacent loop is a distinctive and efficient foundation. A dedicated action should only be reconsidered if Gathering is intentionally meant to replace combat during long sessions.

## 5. Ideal Relationship With Crafting

Gathering and Crafting should remain interdependent, but not identical.

The ideal relationship is:

- Gatherers determine availability and price of core and regional inputs.
- Crafters transform those inputs into equipment.
- Skilled gatherers unlock access to materials that are not merely larger Tier numbers.
- Skilled crafters create demand through optional designs, blueprints, and targeted outcomes.
- No mandatory recipe should depend on an extremely rare jackpot drop.

A restrained material structure would be:

1. **One core material per profession and Region tier.** Preserve the existing Metal, Wood, and Hide model.
2. **A small regional secondary pool.** Used in selected recipes or optional crafting choices, not every item.
3. **A discovery pool.** Blueprints, tool components, cosmetics, or other infrequent rewards.

Regional materials should add a specific possibility to Crafting, not five extra ingredients to every recipe. The current `SpecialResourceRequirements` system is already an appropriate integration point.

## 6. Features to Avoid

Do not add:

- Active node clicking or reaction events
- Timing minigames
- Tool durability and repair chores
- Ten unrelated regional Gathering rule sets
- A large generic perk tree
- Permanent specialization choices that require respec management
- Per-stack material quality variants
- Many profession-specific byproducts with no clear sink
- Required power materials with casino-level drop rates
- Rotating nodes that demand frequent tool or location swapping
- Separate bags or currencies for every regional discovery
- More additive percentage systems before the existing ones are made meaningful and visible

Byproducts are only worthwhile if each has a defined economic purpose. Mining dropping twelve kinds of gemstones would create inventory clutter, not progression.

## 7. Recommended Direction

Use a hybrid of Approaches A, B, and a restrained C.

### The Three Reinforcing Systems

1. **Profession capability milestones**
2. **Regional node profiles and signature reward pools**
3. **Readable tool profiles**

Rare finds belong inside regional node profiles; they do not need to become a fourth standalone system.

### Intended Long-Term Loop

> Choose a material or discovery objective → select profession, tool profile, and unlocked technique → compare region and area node profiles against combat viability → start a long combat session → receive clearly summarized resources, discoveries, and profession XP → unlock new node capabilities and improve the tool setup → craft, trade, or pursue another regional objective.

### Early Game

Region 1 should remain close to the current system:

- Receive all three starter tools.
- Equip one tool.
- Every normal area supports all three professions.
- Abundant icons teach that certain locations favor certain resources.
- Receive one common material per profession.
- Show Gathering XP and the next milestone in the combat summary.

No secondary-resource web or specialization screen is needed yet.

### Midgame

Starting around Regions 2–3:

- Areas begin to use two or three reusable node profiles:
  - Reliable: more frequent, smaller yields
  - Rich: less frequent, larger yields
  - Discovery: lower bulk efficiency, access to a regional secondary pool
- Profession milestones unlock the ability to exploit richer or hidden parts of nodes.
- A first technique or focus choice becomes available before the session.
- Tool profiles make reliability, bulk yield, and discovery materially different.
- Regional secondary resources create optional Crafting and marketplace demand.

This is where “where should I gather?” becomes situational rather than obvious.

### Late Game

A highly progressed gatherer gains capabilities a beginner does not have:

- Access to high-grade node opportunities
- Discovery pools and specialist finds
- Expert techniques that alter reward composition
- Tools tuned for different objectives
- Profitable access to regional signature materials
- High-level survey opportunities in earlier Regions
- Better information and output forecasting

The difference is not just “+40% yield.” It is access, composition, and preparation strategy.

### Long-Term Goals

After ordinary materials are unlocked:

- Complete profession milestone bands
- Find or build specialist tools
- Pursue optional blueprints and discoveries
- Supply scarce regional materials
- Optimize specific old-region surveys
- Compete on existing profession leaderboards
- Prepare for new Region content without invalidating earlier investments

Earlier Regions remain useful through their signature pools and expert survey opportunities, not by making high-level players endlessly farm basic Ore.

### Player Decisions

The proposed system creates five meaningful decisions:

- Which profession or resource is currently valuable?
- Which area balances combat rewards and Gathering output?
- Reliable volume, rich bursts, or discovery focus?
- Which tool profile fits that objective?
- Is this session for core Crafting supply, regional economy, or a long-term discovery?

### Idle Compatibility

All decisions happen before the session.

During the session:

- No clicks
- No reaction windows
- No rotating prompts
- No mandatory tool swaps
- No penalty for staying offline

On return, the summary should explain:

- Materials gathered
- Rare and secondary finds
- Gathering XP and levels gained
- Technique and tool effects
- Estimated versus actual outcome
- Newly unlocked milestones

### Necessary Foundation Work Before Detailed Feature Design

Before deciding exact milestones or formulas, a later design phase should first:

1. Recalibrate the profession XP curve against real area and dungeon proc rates.
2. Decide the intended pace at which one profession reaches each Region's capability band.
3. Define the three material roles: core, regional secondary, and discovery.
4. Give all current rare-chance bonuses actual tagged rewards.
5. Expose Gathering progression in the UI.

Without this, adding Regions 3–10 would simply multiply the existing problem.

## 8. Current Versus Proposed

| Area | Current System | Proposed Direction |
| --- | --- | --- |
| Level progression | XP number and unused level gates; no effect beyond level 1 content | Sparse capability milestones unlocking node grades, techniques, and discovery access |
| Resource progression | Ore → Copper Ore; same family and use at a higher tier | Core tier material plus restrained regional secondary and discovery pools |
| Player decisions | Choose one tool and usually the matching Abundant area | Choose objective, node profile, technique, tool profile, and combat/Gathering tradeoff |
| Long-term goals | Higher leaderboard number and better random tool | Capability bands, specialist tools, regional discoveries, economic roles, and old-region surveys |
| Gathering locations | Same three resources across most areas; selected areas grant +50% yield | Reliable, rich, and discovery-oriented profiles using consistent rules across Regions |
| Rare outcomes | Supporting bonuses exist, but current Gathering tables contain no rare content | Optional rare discoveries and region signatures with clear purposes |
| Crafting integration | Supplies generic tiered Metal, Wood, and Hide | Continues supplying core materials while enabling selected optional crafting possibilities |
| Economy | Tradable bulk materials; tiers primarily separate markets | Bulk supply plus lower-volume regional niches and valuable specialist finds |
| Idle gameplay | Strong: automatic after victories and 24-hour catch-up | Preserved; all strategy occurs before the session, followed by a clear return summary |

## Verification

The analysis was verified against the repository's backend, frontend, authored world content, Gathering reward tables, Crafting material definitions, tool-affix system, dungeon mastery, Soulstone upgrades, quests, prophecies, marketplace rules, and persistence flow.

`build/run-tests.ps1 -NoBuild` passed all 1,396 backend tests with 0 failures during the analysis.

No gameplay implementation, database migration, configuration change, or deployment change is included in this document.
