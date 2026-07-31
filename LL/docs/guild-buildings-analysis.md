# Guild Buildings Analysis

## Scope

This document analyzes the current guild building system in the primary game application under `LL/`.

The current building definitions are data-driven through `LL/src/API/API.LL/Data/guild-content.json`, with mirrored fallback defaults in `Services.LL/Guilds/GuildContentProvider.cs`. Actual gameplay effects are implemented mainly in:

- `Services.LL/Guilds/GuildBuildingService.cs`
- `Services.LL/Guilds/GuildMissionService.cs`
- `Services.LL/Guilds/GuildShopService.cs`

The current frontend display lives in:

- `LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-buildings/`

No code changes are proposed directly in this document. This is a design and product analysis of the current state.

## Executive Summary

The guild building system has a solid structural foundation: buildings have definitions, costs, immediate upgrades, activity logs, role-based permissions, and frontend presentation. However, the current gameplay value is concentrated in only a few buildings.

The main issue is that the system presents itself as a strategic headquarters progression tree, but most buildings are either:

- a gate to another existing tab,
- a small passive modifier,
- a data hook without meaningful building-specific behavior,
- or a placeholder for future systems.

This creates a mismatch between player expectation and actual payoff. Spending Guild Supplies on buildings should feel like choosing the identity and power curve of the guild. Right now, many choices feel like buying labels for systems that do not exist yet.

## Current Building Verdicts

| Building         | Current Role                                        | Verdict                                                   |
| ---------------- | --------------------------------------------------- | --------------------------------------------------------- |
| Guild Hall       | Unlock gate for other buildings                     | Keep, but make levels matter more                         |
| Mission Board    | Mission options, daily orders, mission reward bonus | Best current building, needs deeper strategic choices     |
| Market Office    | Guild shop unlocks                                  | Keep, but make stock more guild-activity-driven           |
| Treasury         | Small construction cost/time discount               | Completely rework                                         |
| Workshop         | Crafting-themed mission/shop hook                   | Rework heavily                                            |
| Raid Hall        | Future raid placeholder                             | Hide, disable, or rebuild around a raid MVP               |
| War Room         | Future guild war placeholder                        | Hide, disable, or rebuild around real guild war mechanics |
| Training Grounds | Future raid/war preparation placeholder             | Completely change identity                                |
| Essence Sanctum  | Essence-themed placeholder                          | Rework into a real essence progression building           |

## System Pain Points

### Too Many Buildings Sell Future Content

Raid Hall, War Room, Training Grounds, Essence Sanctum, and parts of Workshop mostly describe planned support rather than delivered mechanics.

The frontend does mark unimplemented benefits as planned, which is good. The problem is that players can still spend Guild Supplies on these buildings, and the system displays them beside real buildings as if they are equally valid progression choices.

The current risk is that leaders spend shared guild resources on a building that looks important but gives no meaningful return.

### Guild Hall Has Dead Upper Levels

Guild Hall has a max level of 10, but its meaningful unlocks currently stop at level 6:

- Level 1: basic headquarters
- Level 2: Workshop and Treasury
- Level 4: Raid Hall, Training Grounds, Essence Sanctum
- Level 6: War Room

Levels 7 through 10 do not currently create a strong gameplay reason to keep upgrading. That makes the core progression spine feel unfinished.

### Strategic Choice Is Thin

The construction system does not currently force meaningful tradeoffs beyond Guild Supplies and time. Tests confirm multiple buildings can be constructed without building slots.

That may be acceptable for a solo-developer-friendly system, but it means buildings need stronger individual identity. If the guild can eventually build everything, the interesting decision becomes build order. Build order only matters when the buildings actually change how members play.

### Mission Board Dominates the Economy

Guild Supplies are generated through mission rewards, and Mission Board increases personal and weekly mission rewards by 5% per level, capped at 25%.

That makes Mission Board the natural first priority because it improves the engine that funds all other buildings. This is not inherently bad, but it does mean other early buildings need sharper reasons to compete.

### Treasury Is Weak Despite Being Implemented

Treasury gives a 2% construction cost reduction per level, maxing at 10%.

Its total undiscounted cost is 2,375 Guild Supplies. That payoff is delayed, abstract, and hard to feel. It only becomes efficient if the guild has a large amount of future construction remaining, and even then it does not change behavior.

Treasury is technically implemented, but emotionally underpowered.

### Some Implemented Benefits Are Too Thin

Some benefits are marked implemented because they unlock a foundation, a tab state, or a data-driven hook. These are technically true, but they are not satisfying player-facing benefits.

Examples:

- "Unlocks the building foundation for combat preparation systems."
- "Workshop-themed shop stock can be configured through guild content data."
- "Unlocks the Raids tab locked state and prepares future raid registration."

Those should not be treated as full building payoffs.

## Building-by-Building Analysis

## Guild Hall

### Current Behavior

Guild Hall exists permanently and is created lazily at level 1 if missing. Its level gates access to other buildings.

Current unlocks:

- Level 2 unlocks Workshop and Treasury.
- Level 4 unlocks Raid Hall, Training Grounds, and Essence Sanctum.
- Level 6 unlocks War Room.

### Pain Points

Guild Hall is structurally important but mechanically passive. It mostly says "you may now build other things." Once all buildings are unlocked, later levels do not carry enough meaning.

The max level is 10, but the current design only justifies level 6.

### Improvements

Guild Hall should become the civic core of the guild. Good effects would be:

- member cap increases,
- additional officer slots,
- construction queue slots,
- guild announcement tools,
- guild tag/banner customization,
- guild policy slots,
- better activity log/history depth,
- recruitment visibility,
- guild-wide weekly planning tools.

### Recommendation

Keep Guild Hall, but either add meaningful level 7-10 benefits or reduce the max level until those benefits exist.

## Mission Board

### Current Behavior

Mission Board is the strongest current building.

It affects:

- weekly mission option count,
- personal daily order count,
- personal order rewards,
- weekly mission rewards.

At level 2, it can add a fourth weekly mission option when enough definitions are available. At level 3, it can add a fourth personal daily order. Its reward bonus is 5% per level, up to 25%.

### Pain Points

Mission Board is effective, but its effects are mostly quantitative. It gives more options and more rewards, but not many interesting strategic choices.

Because it improves the supply-generation loop, it is likely the obvious first building for serious guilds.

### Improvements

Mission Board should become the guild coordination center. Possible effects:

- weekly mission rerolls,
- officer-selected mission focus,
- mission difficulty tiers,
- guild streak bonuses,
- role-based contribution bonuses,
- specialized mission decks unlocked by other buildings,
- member-scaled mission targets,
- catch-up orders for lower-activity members.

### Recommendation

Keep and deepen. Mission Board is the best current model for how guild buildings should work because it directly touches recurring play.

## Market Office

### Current Behavior

Market Office unlocks guild shop stock:

- Level 1: common stock,
- Level 2: expanded common stock,
- Level 3: rotating weekly stock,
- Level 5: prestige stock.

Shop items are currently mostly personal currency/resource bundles such as Cinders, Soulstones, Fate Echo, Sigil Fragments, and Ascension Stone Fragments.

### Pain Points

Market Office works, but it feels generic. The stock is not strongly connected to what the guild has built, what the guild did that week, or which members contributed.

The name "Market Office" also overlaps mentally with the global marketplace. It behaves more like a guild quartermaster than a market.

### Improvements

Market Office should make guild activity visible in stock.

Possible changes:

- Workshop adds crafting crates to stock.
- Raid Hall adds raid caches.
- Essence Sanctum adds essence/soulstone offers.
- War Room adds war spoils.
- Treasury adds supply bundles or donation-matching contracts.
- High weekly mission completion unlocks better weekly stock.

### Recommendation

Keep, but consider renaming to Quartermaster if it remains a guild reward shop. Make stock depend on guild buildings and weekly guild activity.

## Treasury

### Current Behavior

Treasury reduces Guild Supply construction costs by 2% per level, up to 10%.

### Pain Points

Treasury is the weakest implemented building. It does not create new gameplay, new decisions, or visible excitement. It is a small efficiency modifier on the building system itself.

Its value is also awkward because the best time to build it is early, but early guilds are the least able to spend resources on a delayed payoff.

### Improvements

Treasury should become the guild finance and logistics building.

Potential effects:

- member donation tracking,
- weekly Guild Supply stipend,
- contribution matching,
- increased Guild Supplies from all mission rewards,
- shared guild vault,
- spending permissions and budgets,
- building project funding,
- emergency supply reserve,
- weekly dividend based on member activity.

### Recommendation

Completely rework. Keep the name, but replace the current identity. A Treasury should make the guild economy feel alive, not just slightly reduce building costs.

## Workshop

### Current Behavior

Workshop is positioned as crafting-focused progression.

Current benefits mostly say crafting and tempering orders can appear from the data-driven order pool, and workshop-themed shop stock can be configured through content data.

### Pain Points

The current system does not appear to require Workshop for crafting missions to exist. Crafting and tempering orders are already present in the default mission/order definitions.

This makes Workshop feel like flavor rather than a real building.

### Improvements

Workshop should own cooperative crafting.

Possible effects:

- guild crafting commissions,
- shared material turn-ins,
- member crafting order board,
- guild project recipes,
- crafting contribution multipliers,
- tempering milestone rewards,
- unlockable guild-exclusive recipes,
- supply generation from crafted item quality.

### Recommendation

Rework heavily. Workshop should be one of the clearest non-combat guild identities, especially for players who prefer crafting loops.

## Raid Hall

### Current Behavior

Raid Hall is a future raid placeholder. Level 1 is marked implemented because it unlocks a locked state or prepares future registration. Later benefits are planned.

### Pain Points

Raid Hall is expensive and largely nonfunctional. It costs 4,250 Guild Supplies to max before any Treasury discounts, but current rewards are mostly future-facing.

Players should not be encouraged to invest in this until raids exist.

### Improvements

When raids are ready, Raid Hall should own:

- raid registration,
- raid windows,
- raid keys,
- member signup,
- role assignment,
- boss scouting,
- contribution scoring,
- raid reward chests,
- raid practice objectives.

### Recommendation

Hide or disable construction until a raid MVP exists. Once raids exist, rebuild the building around real raid operations.

## War Room

### Current Behavior

War Room is a future guild war placeholder. There is a worker/background-job hint for guild war phase rollover, but no meaningful War Room integration.

### Pain Points

War Room has the highest non-Guild-Hall max cost at 5,000 Guild Supplies before discounts. That is too expensive for a placeholder.

It also requires Guild Hall level 6, so by the time players unlock it, they will expect a major system.

### Improvements

War Room should own guild war strategy.

Possible effects:

- war registration,
- roster locks,
- attack teams,
- defensive formations,
- scouting,
- war phase visibility,
- seasonal rating,
- war shop unlocks,
- guild honors generation,
- strategic buffs that apply only inside war.

### Recommendation

Hide or disable until guild wars are real. If guild wars are near-term, War Room should be designed alongside that system, not as a generic future placeholder.

## Training Grounds

### Current Behavior

Training Grounds is positioned as future raid and war preparation.

### Pain Points

The identity is too vague. It overlaps with Raid Hall and War Room, and "Training Grounds" also risks confusion with tutorial/training content elsewhere in the game.

It does not currently offer a unique loop.

### Improvements

This building needs a new identity. Better options:

### Option A: Barracks

Make it the combat-member development building:

- combat daily orders,
- sparring challenges,
- guild combat milestones,
- PvE preparation bonuses,
- training medals,
- member readiness score.

### Option B: Proving Grounds

Make it an internal challenge mode:

- weekly guild combat trials,
- leaderboard among guild members,
- simulated bosses,
- build testing,
- low-stakes practice rewards.

### Recommendation

Completely change. Either turn it into Barracks/Proving Grounds with a real recurring combat loop, or remove it until raid/war systems need a shared preparation building.

## Essence Sanctum

### Current Behavior

Essence Sanctum is an essence-themed placeholder.

### Pain Points

This building has one of the best thematic fits for Legends Legacy, but it currently does not deliver on that promise.

Since essences appear central to character progression, a guild-level essence building should feel special.

### Improvements

Essence Sanctum should become a real essence progression system.

Possible effects:

- essence donations,
- weekly resonance rituals,
- soulstone conversion,
- Fate Echo or Sigil Fragment rewards,
- guild-wide essence research,
- essence-themed daily orders,
- unlockable essence shop stock,
- shared progression toward essence caches,
- rotating elemental resonance weeks.

### Recommendation

Rework into a core mid-game building. This should probably be more important than Raid Hall or War Room until those larger multiplayer systems exist.

## Recommended Redesign Direction

## Short-Term Fixes

### 1. Gate Placeholder Buildings

Do not let guilds spend supplies on buildings that do not have meaningful effects.

Candidates to hide, disable, or label as unavailable:

- Raid Hall
- War Room
- Training Grounds
- Essence Sanctum

Workshop may also need gating unless its effect becomes real.

### 2. Rework Treasury First

Treasury is implemented but not compelling. Reworking it would improve the early building economy without waiting for raid or war systems.

Best first version:

- Increase Guild Supplies earned from mission rewards by a visible amount.
- Add a weekly supply stipend.
- Track member donations or weekly logistics contribution.

### 3. Add Guild Hall Level 7-10 Benefits

If Guild Hall remains max level 10, give those levels real meaning.

Suggested progression:

- Level 7: second construction queue or faster project finalization.
- Level 8: guild policy slot.
- Level 9: advanced recruitment visibility or larger member cap.
- Level 10: capstone guild identity feature.

### 4. Make Market Office Stock Depend on Buildings

The fastest way to make buildings feel connected is to let buildings influence guild shop stock.

Examples:

- Workshop level adds crafting crates.
- Essence Sanctum level adds soulstone/Fate Echo stock.
- Raid Hall level adds raid preparation bundles.
- War Room level adds honor/prestige stock.
- Treasury level adds supply bundles or reduced purchase requirements.

## Medium-Term Redesign

### Add Building Archetypes

Each building should map to a clear gameplay archetype:

| Archetype               | Building                       |
| ----------------------- | ------------------------------ |
| Civic / management      | Guild Hall                     |
| Coordination / missions | Mission Board                  |
| Economy / logistics     | Treasury                       |
| Rewards / shop          | Market Office or Quartermaster |
| Crafting                | Workshop                       |
| Combat practice         | Barracks or Proving Grounds    |
| Essence progression     | Essence Sanctum                |
| Cooperative PvE         | Raid Hall                      |
| Competitive guild PvP   | War Room                       |

### Replace Generic Bonuses With Loops

The best buildings should unlock verbs, not only percentages.

Weak pattern:

- +2% cost reduction
- +5% reward
- unlocks future support

Stronger pattern:

- reroll a mission
- start a guild commission
- donate materials to a guild project
- open a weekly ritual
- register raid members
- assign a war defense
- unlock a building-specific shop rotation

## Suggested Priority Order

1. Mission Board: keep, polish, and make it the model for recurring guild engagement.
2. Treasury: rework into guild logistics and supply generation.
3. Market Office: connect stock to guild activity and other buildings.
4. Guild Hall: add real levels 7-10 or lower the cap.
5. Workshop: turn into cooperative crafting projects.
6. Essence Sanctum: build essence donations/rituals/research.
7. Training Grounds: rename and redesign as Barracks or Proving Grounds.
8. Raid Hall: wait for raid MVP.
9. War Room: wait for guild war MVP.

## Design Principle

A guild building should answer at least one of these questions:

- What new thing can the guild do now?
- What recurring decision did officers gain?
- What new contribution path did members gain?
- What reward category did the guild unlock?
- What visible identity did the guild choose?

If a building cannot answer one of those questions, it should not be constructible yet.

## Implementation Notes

Most content-facing changes can start in `guild-content.json`, but new effects require service code.

Likely code areas:

- Building definitions and copy: `LL/src/API/API.LL/Data/guild-content.json`
- Default fallback definitions: `GuildContentProvider.cs`
- Construction rules and costs: `GuildBuildingService.cs`
- Mission effects: `GuildMissionService.cs`
- Shop stock and requirements: `GuildShopService.cs`
- Frontend presentation: `guild-buildings.component.*`

If placeholder buildings are disabled, the model likely needs a content flag such as `isAvailable`, `isConstructible`, or `releaseState`. Relying only on `isImplemented` per benefit is not enough, because a building can have one thin "implemented" foundation benefit while still being a poor construction target.

## Verification Notes

This analysis was based on reading the guild building data, services, tests, and frontend models/components. No verification commands are required for this document-only change beyond confirming the markdown file exists.
