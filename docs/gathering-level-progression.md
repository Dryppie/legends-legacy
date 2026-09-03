# Gathering Level Progression

> Historical Alpha plan, superseded 3 September 2026. Crafting/gathering progression, queued tempering and their obsolete quest content have been removed. Conversion, refund and compatibility/backfill proposals below are not current implementation work. Shared numerical helpers with active consumers may remain. See the [post-Alpha cleanup](design/equipment-post-alpha-cleanup.md) and [current quest flow](../LEGENDSLEGACY_QUEST_FLOW.md) for supported behavior.

## Purpose

This document defines how Mining, Woodcutting, and Skinning levels should progress and what profession levels should provide to a gatherer.

The design preserves Gathering as a passive, combat-adjacent system. Progression should create long-term goals and unlock useful capabilities without introducing active minigames, techniques, or separate reward layers.

## Design Decisions

- Each Gathering profession has a maximum level of 100.
- Each eligible Gathering attempt awards exactly 50 base profession XP.
- Reaching level 100 should take approximately 360 days of uninterrupted, eligible Gathering activity.
- Ordinary materials and Catalysts are both part of the normal Gathering reward model.
- Ordinary crafting materials remain available regardless of profession level.
- Catalysts are rare node drops and do not have profession-level requirements.
- There are no core, signature, secondary-material, or discovery layers.
- There are no Gathering techniques, stances, focus modes, or permanent specializations.
- Gathering levels are long-term mastery and leaderboard goals rather than equipment-access gates.

## Earning Gathering XP

An eligible attempt occurs when all of the following are true:

1. The character has a Gathering tool equipped.
2. The current area or dungeon contains a node matching that tool's profession.
3. A victorious encounter causes the Gathering processor to attempt that node.

Each eligible attempt awards **50 profession XP**, whether or not the node produces materials.

XP is awarded for the attempt rather than only for a successful material proc. This removes extreme variance from profession progression and avoids the current problem where a `0.37%` node chance produces only about `1.33` successful gathers, and therefore `1.33` XP, per hour.

The following should not affect XP earned:

- Material quantity
- Node yield bonuses
- Area abundance
- Tool yield affixes
- Double-gather effects
- Extra reward rolls
- Catalyst drops
- Soulstone yield bonuses
- The market value of the gathered result

These effects improve rewards, not the speed at which the profession levels. This prevents economic bonuses from becoming unintended XP multipliers.

XP should be awarded no more than once per profession for a single combat encounter. Adding more authored nodes to an area must not multiply profession XP.

Offline combat should use the same rules as online combat. The existing 24-hour offline-processing cap remains unchanged.

## Level Curve

The proposed XP requirement for advancing from the current level is:

```text
XP to advance from level L = round(474 × L²)
```

This formula applies for levels 1 through 99.

Examples:

- Level 1 to 2 requires `474 XP`.
- Level 50 to 51 requires `1,185,000 XP`.
- Level 99 to 100 requires `4,645,674 XP`.
- Level 1 to 100 requires `155,637,900 cumulative XP`.

### Timing Assumptions

The 360-day target assumes:

- One encounter every 10 seconds
- 360 victorious encounters and eligible Gathering attempts per hour
- 50 XP per eligible attempt
- 18,000 XP per hour
- 432,000 XP per 24 hours
- Uninterrupted Gathering uptime
- An eligible node in every area used for progression

Under those ideal conditions, level 100 takes approximately **360.27 days**.

| Level reached | Cumulative XP | Ideal cumulative time |
|---:|---:|---:|
| 10 | 135,090 | 0.31 days |
| 20 | 1,170,780 | 2.71 days |
| 30 | 4,055,070 | 9.39 days |
| 40 | 9,735,960 | 22.54 days |
| 50 | 19,161,450 | 44.36 days |
| 60 | 33,279,540 | 77.04 days |
| 70 | 53,038,230 | 122.77 days |
| 80 | 79,385,520 | 183.76 days |
| 90 | 113,269,410 | 262.20 days |
| 100 | 155,637,900 | 360.27 days |

Real elapsed time will normally be longer because of lost encounters, combat defeats, time spent using another profession, time beyond the 24-hour offline cap, and areas without a matching node.

The target is **360 ideal Gathering days per profession**, not 360 days for all three professions combined. Because only one tool can be equipped, independently reaching level 100 in Mining, Woodcutting, and Skinning represents three separate long-term pursuits.

## What Gathering Levels Do

Gathering level is primarily an equipment progression system. It should not directly grant an invisible yield or success percentage on every level.

### Ordinary Material Access

Profession level never prevents a player from collecting a region's ordinary material:

- Mining can gather the region's ordinary Metal material.
- Woodcutting can gather the region's ordinary Wood material.
- Skinning can gather the region's ordinary Hide material.

If the character can reach and defeat enemies in an area, the matching starter tool can attempt its Gathering node and find its ordinary materials. Profession progression provides access to better tools rather than blocking normal material acquisition.

### Catalysts as General Gathering Rewards

Ordinary materials and Catalysts use the same general Gathering reward system. Catalysts are not represented as a separate node layer, signature layer, discovery pool, or parallel Gathering activity.

A Gathering reward table may contain:

- Ordinary material entries
- Rare Catalyst entries used by blueprint recipes

During normal Gathering resolution:

- Ordinary materials remain eligible at every profession level.
- Catalysts are possible rare drops from their configured Gathering nodes.
- Catalyst eligibility does not depend on profession level.
- A level-1 gatherer and a level-100 gatherer can both find a Catalyst from the same node.

Profession level does not control which Catalysts the gatherer can find. A higher-level gatherer may become more effective through better tools, including tools with rare-drop affixes, but the Catalyst itself is never level-gated.

Whether a Catalyst is awarded alongside or instead of an ordinary material is a reward-table balance decision, not a different Gathering layer. The preferred default is for rare Catalysts to be an additional result so that finding one does not reduce expected ordinary-material output.

Catalysts should remain tradable unless a specific blueprint progression rule requires otherwise. This lets gatherers supply the economy while allowing non-gatherers to obtain blueprint ingredients through trade.

### Tool Access

Gathering tools have no character-level or profession-level equip requirements. A player may equip a tool of any tier or rarity as soon as they obtain it.

Tool affixes continue to determine whether a tool favors success chance, yield, duplication, extra reward rolls, or Catalyst finding.

Raw Gathering improvements remain visible on the equipped tool without making the item unusable behind a level gate.

The existing `Prospector's` rare-material affix should be clarified or renamed if Catalysts become the only special Gathering result. A name such as `Catalytic` would communicate its actual purpose more precisely.

## Intended Progression Experience

### Early Progression: Levels 1–20

- Ordinary materials are immediately available.
- Catalysts are possible rare drops immediately.
- The first levels arrive quickly and introduce the profession.
- Better tools may be obtained and equipped at any level.

### Established Progression: Levels 21–60

- Tool drops and affixes create equipment goals without level gates.
- Node-specific Catalysts give the profession a specialized economic role from the beginning.
- Level gains become progressively slower and are treated as long-term milestones.

### Master Progression: Levels 61–99

- The profession's value comes from rare Catalyst finds, strong tools, and reliable ordinary-material supply.
- Earlier areas remain relevant wherever their Catalyst is still economically useful.

### Level 100

Level 100 grants:

- Completion recognition through the existing profession leaderboard and profile presentation

Level 100 should not be required for ordinary regional crafting. It is a long-term mastery goal for blueprint production, trade, equipment optimization, and completion.

## Region 1 Blueprints and Catalysts

Region 1 contains four dungeon blueprints in total. The four blueprints support four different combat styles.

| Dungeon | Blueprint | Combat style | Gathering Catalyst | Gathering profession |
|---|---|---|---|---|
| Goblin Mines | Fury | Fighter | Fury Heart | Any Gathering profession |
| Goblin Mines | Phoenix | Support | Phoenix Ember | Any Gathering profession |
| Forgotten Catacombs | Arcane | Caster | Arcane Focus | Any Gathering profession |
| Forgotten Catacombs | Endurance | Defensive | Endurance Core | Any Gathering profession |

### Goblin Mines

Every Goblin Mines tier uses the same two-blueprint pool:

- Blueprint: Fury
- Blueprint: Phoenix

Fury supports an aggressive physical and critical-hit playstyle. Phoenix supports a healing and recovery-oriented Support playstyle.

### Forgotten Catacombs

Every Forgotten Catacombs tier uses the same two-blueprint pool:

- Blueprint: Arcane
- Blueprint: Endurance

Arcane supports magical damage, penetration, and cooldown-oriented builds. Endurance supports a Defensive playstyle through maximum health, regeneration, armor, and status resistance.

### Dungeon-Tier Rules

- Goblin Mines I, II, and III can all award Fury and Phoenix.
- Forgotten Catacombs I, II, and III can all award Arcane and Endurance.
- Higher dungeon tiers do not introduce additional blueprints.
- The first clear of every dungeon tier awards both of that dungeon's blueprints.
- Subsequent completions make one blueprint roll with a 10% success chance.
- A successful regular blueprint roll selects either of that dungeon's two blueprints with equal 50% weighting.
- A regular completion can therefore award at most one blueprint.
- Higher tiers derive their additional value from other rewards rather than exclusive blueprints.

The guaranteed first-clear blueprints are awarded on all three tiers even when the player already knows them. Blueprint duplicates continue to use the game's ordinary inventory, trade, and economy rules.

### Region 1 Catalyst Rules

The four associated Catalysts form the complete Region 1 Catalyst pool:

- Fury Heart is a rare Region 1 Gathering drop.
- Phoenix Ember is a rare Region 1 Gathering drop.
- Arcane Focus is a rare Region 1 Gathering drop.
- Endurance Core is a rare Region 1 Gathering drop.

Mining, Woodcutting, and Skinning are all eligible to find any of the four Catalysts. Catalyst identity is not tied to a specific Gathering profession.

All four Catalysts:

- Are possible drops from level 1.
- Have no Gathering profession-level requirement.
- Can drop rarely from configured Region 1 Mining, Woodcutting, and Skinning nodes.
- Preferably drop in addition to ordinary materials rather than replacing them.
- Remain relevant across all three tiers because their associated blueprint does not change between tiers.

The implemented shared Catalyst roll has `96` no-drop weight and four equally weighted Catalyst entries. This produces a base `4%` chance of any Catalyst and a base `1%` chance of each specific Catalyst per successful Gathering reward roll. Because it is a separate referenced roll, the Catalyst is additional to the ordinary material roll. Existing rare-drop and bonus-roll effects can improve the effective result.

Blueprints formerly associated with these dungeons but excluded from the Region 1 set have been reassigned rather than deleted: Execution, Venom, and Hive now belong to Tangled Cave, while Spirit, Warden, Primal, and Aegis now belong to The Great Tree.

## Explicitly Excluded Systems

This design does not include:

- Gathering techniques or stances
- Active Gathering minigames
- Core, signature, or discovery reward layers
- Regional secondary materials
- Permanent specialization trees
- Gathering speed increases
- Generic percentage bonuses granted every level
- XP scaling from reward quantity or value
- Level requirements for ordinary regional materials
- Level requirements for Catalyst drops

## Balance Notes

The curve is intentionally heavily backloaded. Under ideal conditions:

- Level 50 takes only about 44 days.
- Level 80 takes about 184 days.
- The final 20 levels take about 176 additional days.

Because Catalysts and tools are not level-gated, Gathering levels represent mastery and completion rather than exclusive reward or equipment access.

Catalyst drop rates should continue to be monitored against:

- Blueprint Catalyst consumption
- The number of players specializing in each profession
- Marketplace tradability
- Tool-affix bonuses
- Online and offline encounter throughput
- Dungeon node frequency

The XP curve should be tested using eligible attempts rather than successful material results. Catalyst economy testing should be performed separately so Catalyst drop-rate changes do not alter level pacing.

## Implementation Status

Implemented:

- Gathering level cap of 100
- The `474 × L²` Gathering XP curve
- 50 base XP per eligible attempt, including failed material attempts
- At most one base XP award per encounter when multiple matching nodes are authored
- Tools of every tier and rarity can be equipped without character or profession level requirements
- A shared four-Catalyst Region 1 Gathering pool with no Catalyst-specific level gates
- Region 1 Catalyst access for Mining, Woodcutting, and Skinning in combat areas and dungeons
- Fury and Phoenix blueprint rewards for all Goblin Mines tiers
- Arcane and Endurance blueprint rewards for all Forgotten Catacombs tiers
- Both blueprints guaranteed on the first clear of each tier
- A single equally weighted 10% blueprint roll on repeat completions only
- Reassignment of the remaining enabled dungeon blueprints and Catalysts to Region 2 families
- Catalyst-selection cache alignment with the four Region 1 Catalysts
- Mining, Woodcutting, and Skinning level/XP presentation on character profiles
- Gathering XP, materials, and rare-find presentation in the combat session summary
- Online and offline combat synchronization for visible Gathering profession progress
- Catch-up aggregation that preserves Gathering XP and items across response batches

Not added:

- A separate Gathering milestone page; the character profile and combat summary now expose the canonical progression values without introducing milestones that the design does not define
- Database migrations; the implementation uses existing profession and content storage
- Deployment or external configuration changes
