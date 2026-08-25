# LegendsLegacy Gameplay Depth Analysis: Soulstone Constellations

## Executive conclusion

The next major depth expansion should be the **Soulstone Archive**, rebuilt into a genuine **Soulstone Constellation system with branching paths, mutually exclusive keystones, and playstyle trade-offs**.

Soulstones already touch almost every major activity, have persistent character progression, a data-driven definition system, purchasing/reset APIs, frontend state management, and bonus aggregation. Yet the player decision is currently almost entirely: **buy the most efficient affordable rank, then eventually max everything**.

That is the best opportunity in the repository for adding meaningful decisions per unit of development effort.

## 1. Current game model

This analysis follows the current services, models, controllers, frontend flows, and live JSON definitions rather than relying on older design documents. Some documents describe raids or the World Tower as missing or embryonic, while the current code contains substantial implementations for both.

The main game structure is:

- **Core repetition:** idle combat and timed character actions produce character XP, Cinders, Soulstones, equipment, Essences, gathering materials, sigils, and activity-specific progression.
- **Build progression:** equipment, tempering, blueprints, active/passive Essences, Essence ascension, and loadout assignment improve or specialize combat performance.
- **Challenge progression:** regions, dungeons, region bosses, raids, Colosseum, and World Tower convert builds into clears, mastery, currencies, and aspirational milestones.
- **Direction systems:** quests, Prophecies, guild orders, missions, achievements, and titles give players shorter goals and route them between activities.
- **Economy:** gathering and combat supply materials and items; crafting transforms them; the marketplace supports commodities, exact equipment instances, order matching, escrow, pricing history, and fees.
- **Meta progression:** Soulstone upgrades permanently improve almost all the preceding loops.

The strongest existing decision-making is found in Essence loadouts, equipment/crafting optimization, dungeon risk management, marketplace activity, and high-level challenge preparation.

The weakest systems are not necessarily the ones with the least code. The important gap is where considerable infrastructure exists but player interaction still resolves into linear accumulation.

## 2. Five strongest depth candidates

### Candidate 1 — Soulstone Archive / permanent meta-progression

#### Current state

The current definition file contains **14 upgrades across five branches**:

- Essence Archive: 4
- Combat Progression: 2
- Gathering: 3
- Crafting: 3
- Dungeons: 2

All 14 upgrades have five ranks and the same cost curve: 25, 75, 150, 300, and 600 Soulstones. That creates 70 total ranks costing 16,100 Soulstones.

Crucially:

- No current definition uses `RequiresUpgradeIds`.
- All owned bonuses are simultaneously active.
- There are no mutually exclusive choices.
- Reset clears the purchased ranks and refunds their full cost.
- The UI emphasizes ranks, purchasable upgrades, and number maxed.
- An achievement explicitly rewards maxing every upgrade.

Evidence:

- [`soulstone-upgrades.json`](../../LL/src/API/API.LL/Data/progression/soulstone-upgrades.json)
- [`SoulstoneUpgradeDefinition.cs`](../../LL/src/Core/Domain/Models/Soulstones/UpgradeDefinition/SoulstoneUpgradeDefinition.cs)
- [`SoulstoneBonusProvider.cs`](../../LL/src/Infrastructure/Service/Services.LL/Bonuses/SoulstoneBonusProvider.cs)
- [`SoulstoneUpgradeService.cs`](../../LL/src/Infrastructure/Service/Services.LL/Soulstones/SoulstoneUpgradeService.cs)
- [`soulstone-archive.component.ts`](../../LL/src/Presentation/ll/src/app/features/game/character/soulstone-archive/soulstone-archive.component.ts)
- [`additional.json`](../../LL/src/API/API.LL/Data/achievements/additional.json)

#### Current player loop

`Play normal activities → accumulate Soulstones → purchase the best affordable scalar bonus → every applicable activity becomes more efficient → repeat until all upgrades are maxed`

The reset makes experimentation safe, but because every upgrade can ultimately coexist, it does not create persistent build identity.

#### Existing strengths

- One currency already earned from many activities.
- Five established thematic branches.
- Data-driven definitions with unused prerequisite support.
- Persistent character ranks.
- Centralized bonus aggregation.
- API, CQRS handlers, frontend state, and state synchronization already work.
- The system is relevant to nearly every character from early progression through endgame.

#### Where the depth stops

The branches are categories, not paths. Most effects are variations of:

- More experience
- More yield
- Better drop chance
- Better pity progress
- Reduced negative outcome chance
- Better retention

The optimal long-term answer is always “own everything.” The only temporary decision is purchase order, which is usually an efficiency calculation rather than a playstyle decision.

The shared cost curve and unused prerequisites reinforce the impression of a checklist masquerading as a progression tree.

#### Depth potential

The existing branches naturally support:

- Branch investment gates
- Mutually exclusive keystones
- Risk versus reliability choices
- Targeted progression versus broad collection
- Quantity versus rarity
- Safe crafting versus high-ceiling crafting
- Dungeon security versus expedition endurance
- Later, limited cross-branch synergies

#### Expected impact

Very high impact on agency, long-term identity, and system connectivity. It can give players reasons to choose different regions, monsters, tools, recipes, and dungeon routes without introducing another major activity.

#### Cost / risk

Low-to-medium engineering cost for the foundation. The largest design risk is producing false choices or a mandatory respec meta.

### Candidate 2 — Essence evolution and long-term Essence identity

#### Current state

The current Essence system is already one of the game’s strongest foundations:

- 80 defined Essences
- Active and passive abilities
- Essence XP, levels, ascension, Dust, duplicate handling, and focus/pity
- Multiple combat-context loadouts
- 19 Codex collections
- Ability modifier infrastructure in the combat engine

However, all 80 definitions contain an evolution entry with:

- An empty catalyst ID
- No added tags
- No attribute changes
- No active ability modifiers
- No passive ability modifiers

The service attempts to consume the configured catalyst before evolving, so evolution is effectively an unpopulated shell.

Evidence:

- [`essences.json`](../../LL/src/API/API.LL/Data/essences/essences.json)
- [`EssenceSystemService.cs`](../../LL/src/Infrastructure/Service/Services.LL/Essences/EssenceSystemService.cs)
- [`EssenceDefinitionValidator.cs`](../../LL/src/Infrastructure/Service/Services.LL/Essences/EssenceDefinitionValidator.cs)
- [`AbilityRuntime.cs`](../../LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityRuntime.cs)

#### Current player loop

`Hunt creature → obtain/focus Essence → gain Essence XP and duplicates → dismantle or invest Dust → ascend → equip in contextual loadout`

#### Existing strengths

This is signature game content with excellent combat, collection, and build-diversity potential. The runtime already supports modifiers that could make evolved abilities behave differently.

#### Where the depth stops

Level and ascension progression are predominantly vertical. Evolution is nominally present but carries no authored mechanical identity. Once the strongest loadouts are understood, progression tends toward raising the same pieces rather than transforming how they play.

#### Depth potential

- Branching evolutions
- Alternative ability behavior
- Role specialization
- Collection-driven evolution goals
- Horizontal sidegrades
- More meaningful duplicate decisions

#### Expected impact

Potentially the highest raw impact on combat builds and replayability.

#### Cost / risk

High authoring and balance burden. Properly differentiating even two paths for 80 Essences implies a large ability-design matrix. Shipping only a few complete evolutions could also make the rest of the collection feel unfinished.

### Candidate 3 — Dungeons

#### Current state

Dungeons already have:

- Multiple dungeon families and difficulties
- Seeded route maps
- Vigor and Vigor thresholds
- Route forecasts
- Rest Sites
- Pending loot at risk
- Retreat, failure, and completion outcomes
- Mastery levels
- Sigil access
- Frozen combat snapshots

The combat orchestration even accepts run-level ability modifiers, although the current run service supplies an empty list.

Evidence:

- [`dungeons.json`](../../LL/src/API/API.LL/Data/dungeons/dungeons.json)
- [`dungeon-delves.json`](../../LL/src/API/API.LL/Data/dungeons/dungeon-delves.json)
- [`DungeonRunService.cs`](../../LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonRunService.cs)
- [`DungeonVigorService.cs`](../../LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonVigorService.cs)
- [`DungeonRouteService.cs`](../../LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonRouteService.cs)

#### Current player loop

`Acquire sigil → choose dungeon and difficulty → freeze build snapshot → select routes using encounter and Vigor forecasts → fight/rest → retreat or risk pending loot → gain rewards and mastery`

#### Existing strengths

This is already an honest risk-management loop with durable persistence, seeded content, and good server authority.

#### Where the depth stops

Most routes differ primarily in Vigor expectation, encounter tags, and reward risk. The room vocabulary is deliberately restrained, and snapshots prevent mid-run loadout adaptation. Once the Vigor math is understood, many decisions risk becoming route arithmetic.

#### Depth potential

Dungeon-family mechanics, route-specific stances, limited run modifiers, or more tag-aware route/build interactions could add depth.

#### Expected impact

High for engaged PvE players, but less universal than Soulstones.

#### Cost / risk

Medium-to-high. Expanding room mechanics can quickly become bespoke content production or drift into a roguelike design the current system appears intentionally to avoid.

### Candidate 4 — Gathering professions

#### Current state

Mining, Woodcutting, and Skinning are integrated into combat areas and dungeons. Players equip one gathering tool, and matching nodes can produce materials and profession XP during normal activity.

Tools already support modifiers such as yield, node chance, rare chance, double rewards, and bonus rolls. Materials feed crafting, Prophecies, guild progression, and the marketplace.

#### Current player loop

`Equip a tool → fight in an area containing matching nodes → automatically gather → receive materials and profession XP → improve tool or choose another area`

#### Existing strengths

- Strong economy connection
- Data-driven node and reward tables
- Existing tool affixes
- Naturally accompanies idle combat
- Easy to extend without creating new combat content

#### Where the depth stops

Profession levels mostly improve access or numbers. Current content makes many level requirements effectively nonbinding, and gathering has little specialization or mastery of its own. Tool and area choice can become an obvious material-per-hour calculation.

#### Depth potential

- Quantity versus rare-material specializations
- Surveying and area mastery
- Tool projects
- Node-specific expertise
- Market-responsive production strategies

#### Expected impact

Strong economic and crafting impact, but gathering remains a support loop rather than the game’s central identity layer.

#### Cost / risk

Medium. The major danger is converting a pleasant passive reward into another mandatory maintenance chore.

### Candidate 5 — Guild buildings and missions

#### Current state

Guilds have membership roles, favor, supplies, XP, a shop, vault, daily orders, weekly missions, and nine building definitions.

Several buildings still expose only a fraction of their planned effects. Weekly and daily definition pools are also relatively small. Most member interaction contributes to pooled meters, while building selection and mission direction are concentrated in leadership.

Evidence:

- [`guild-content.json`](../../LL/src/API/API.LL/Data/guilds/guild-content.json)
- [`GuildBuildingService.cs`](../../LL/src/Infrastructure/Service/Services.LL/Guilds/GuildBuildingService.cs)

#### Current player loop

`Perform normal activities → advance personal orders and guild mission meters → claim favor/supplies/XP → leadership selects linear building upgrade → use shop/building benefits`

#### Existing strengths

It already connects social structure, normal play, pooled goals, rewards, and permanent progression.

#### Where the depth stops

Most members contribute passively to a target selected by leadership. Building growth is mostly linear, and multiple buildings do not yet provide their full effect sets. There is limited guild identity beyond upgrade order.

#### Depth potential

- Mutually exclusive weekly guild doctrines
- Temporary projects
- Building combinations
- Mission portfolios with opportunity costs
- Guild economic specialization

#### Expected impact

Potentially excellent social retention, but only for an adequately populated guild ecosystem.

#### Cost / risk

Medium-to-high engineering, balance, and operational cost. Social systems are also much harder to validate without healthy concurrent player populations.

## 3. Candidate ranking

Higher “Current Shallowness” means the system currently lacks more meaningful depth.

| Candidate | Shallowness | Foundation | Player Impact | Long-Term | Strategy | Connectivity | Replayability | Content Scaling | Dev Efficiency | Overall |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Soulstone Constellations | 9 | 9 | 10 | 9 | 9 | 10 | 9 | 9 | 9 | **10** |
| Essence Evolution | 7 | 10 | 10 | 10 | 10 | 9 | 10 | 8 | 6 | **9** |
| Dungeons | 6 | 9 | 8 | 9 | 8 | 8 | 8 | 8 | 7 | **8** |
| Gathering Professions | 9 | 8 | 7 | 8 | 8 | 9 | 7 | 9 | 8 | **8** |
| Guild Progression | 8 | 8 | 6 | 9 | 8 | 9 | 8 | 8 | 6 | **7** |

“Overall” is not a mechanical average. It includes scope risk and whether the system already contains substantial decision-making.

## 4. Chosen system: Soulstone Constellations

This is the system I would deepen next.

### Why it wins

Soulstones are the connective tissue of the game, but they currently provide almost no player identity. Improving them changes how players approach existing content rather than demanding an entirely new content pipeline.

A modest number of well-designed choices can create dozens of combinations. Five binary branch keystones already produce 32 broad configurations before equipment, Essences, tools, regions, and dungeon choices are considered.

### Why now?

The repository already contains many functional destinations for progression:

- Substantial combat and Essence infrastructure
- Crafting and tempering
- Dungeons
- World Tower
- Raids and bosses
- Colosseum
- Prophecies
- Guild rewards
- Marketplace activity

What is missing is a meta layer that helps players answer:

> Which of these activities and progression styles define my character?

Soulstones are already awarded throughout that ecosystem. The reward funnel exists; the spending decision is the weak link.

### Why not Essence evolution?

Essence evolution is the runner-up and should eventually receive major investment.

It comes second because the base Essence system already has considerably more player decision-making than Soulstones: collection, focus, leveling priorities, active/passive pairings, and contextual loadouts.

Doing evolution properly also demands significant ability authoring and balance across 80 Essences. Soulstone Constellations can deliver broad strategic value with roughly ten keystones and a handful of reusable rule hooks.

### What happens if nothing changes?

- Soulstones become a solved return-on-investment checklist.
- Free full resets erase even temporary commitment.
- Every mature character converges on the same completed state.
- The five branches remain visual organization rather than meaningful specialization.
- “Max every upgrade” becomes the only aspirational endpoint.
- Once completed, a widely awarded currency becomes dead.
- The game’s breadth risks feeling like a collection of chores because the meta system gives players no reason to prefer one progression identity over another.

## 5. Proposed depth expansion

### Core structure

Keep the current 14 upgrades as **minor constellations**. They remain dependable, incremental investments.

Add three structural concepts:

1. **Branch gates:** investing a required number of ranks in a branch unlocks its keystone choice.
2. **Exclusive keystones:** each branch offers two rule-changing options; owning one disables its sibling.
3. **Constellation identity:** the UI explains the combined behavior of the player’s five choices rather than merely reporting completion percentage.

Do not add another currency. Soulstones remain the only purchase resource.

### Illustrative keystone pairs

Exact values require simulation and telemetry; these describe behavior, not final balance.

| Branch | Path A | Path B | Meaningful tension |
|---|---|---|---|
| Essence Archive | **Predator’s Oath:** substantially improves focused-creature pity and drops while weakening unfocused acquisition | **Archivist’s Lantern:** improves duplicate conversion and broad Codex completion but disables or reduces focused bonuses | Targeted chase versus collection breadth |
| Combat Progression | **Disciplined Study:** rewards stable winning streaks and efficient farming; defeat breaks momentum | **Hard Lessons:** improves progression recovery when challenging difficult areas or losing, but is weaker in safe farming | Reliability versus pushing difficulty |
| Gathering | **Abundant Hands:** more common-material quantity but lower rare weighting | **Prospector’s Eye:** better rare rolls but lower ordinary yield | Throughput versus rare-value hunting |
| Crafting | **Patient Temper:** negative results consume less Potential, but critical tempering is weaker | **Reckless Temper:** stronger critical results with harsher failure and Potential consequences | Preservation versus high ceiling |
| Dungeons | **Quartermaster:** Rest Sites secure part of pending loot but restore less Vigor | **Vanguard:** stronger expedition endurance, but loot remains fully exposed until retreat or completion | Security versus deeper pushing |

These choices affect what the player does:

- Which creature to focus
- Whether to farm safely or challenge a harder area
- Which tool and node supply to pursue
- Which item is worth risking in tempering
- Which dungeon route and retreat threshold are appropriate

That is depth. It is not merely choosing which percentage becomes larger.

## 6. Player loop

### Current loop

`Normal activity → Soulstone drop → compare affordable percentage upgrades → buy rank → passive efficiency increases → repeat → eventually max everything`

Meaningful choice is largely limited to upgrade order.

### Proposed loop

`Choose a medium-term character goal → invest in the relevant branch → unlock an exclusive keystone fork → accept one benefit and its opportunity cost → configure existing activities around that choice → observe results → continue investing or reset and experiment → build a multi-branch constellation identity`

Decision points occur at:

- **Goal selection:** collection, difficult combat, material throughput, rare hunting, crafting ceiling, or dungeon security.
- **Keystone commitment:** selecting one option excludes its sibling.
- **Activity configuration:** area, creature focus, tool, crafting project, or dungeon route changes in response.
- **Resource allocation:** Soulstones spent opening one branch delay another.
- **Mastery:** players learn which circumstances make their trade-off valuable.
- **Long-term identity:** the combination of branch choices remains different even after extensive progression.

## 7. Progression horizons

### Session-level: minutes to hours

- Receive a Soulstone and decide whether to buy now or save for a keystone.
- See a chosen mechanic visibly affect combat, gathering, crafting, or dungeon results.
- Adjust the next action around the current constellation.
- Compare the actual cost and effect of the excluded sibling.

The system must surface why a bonus triggered; otherwise keystones become invisible background math.

### Short-term: several days

- Reach the investment gate for a desired branch.
- Assemble equipment, a tool, or an Essence loadout that benefits from the selected path.
- Pursue a material, Codex, or dungeon goal that validates the specialization.

### Medium-term: several weeks

- Complete one or two branch paths.
- Learn the trade-offs well enough to make informed respec decisions.
- Combine two branches into a broader identity, such as focused Essence hunter or rare-material crafter.

### Long-term: several months

- Build a distinctive five-branch configuration.
- Pursue limited cross-branch Bonds in Phase 2.
- Complete branch mastery challenges that test use of the mechanic, rather than simple rank ownership.
- Experiment with alternate constellations when entering a substantially different stage of progression.

### Endgame

Endgame relevance comes from exclusive configuration rather than endless larger ranks.

New regions, dungeon families, blueprints, and Essence collections create new contexts in which an existing choice may become valuable. New content can occasionally add a horizontal keystone or Bond without invalidating earlier investment.

## 8. Natural connections to existing systems

| Connection | Two-way value |
|---|---|
| Combat | Combat supplies Soulstones and measurable challenge contexts. Combat keystones give players a reason to choose safe farming or difficult-area progression. |
| Essences / Soul Archive | Essence hunting supplies collection and build goals. The Essence branch changes whether the player pursues one creature or broad Codex completion. |
| Gathering | Areas and tools supply materials. Gathering specialization makes region and tool choice economically meaningful. |
| Crafting / tempering | Materials and blueprints create Soulstone-supported projects. Crafting keystones change whether the player protects investment or pursues high-ceiling outcomes. |
| Dungeons | Dungeons award Soulstones and provide Vigor/pending-loot decisions. Dungeon keystones change route and retreat strategy. |
| Prophecies | Prophecies already direct players into these activities and can reward Soulstones. A constellation helps the player judge which offered Prophecy fits their current plan. Prophecies should not be required for respecs. |
| Guilds | Guild missions and shops already participate in Soulstone acquisition. Character specializations make different mission contributions attractive without requiring guild-exclusive keystones. |
| Marketplace | Gathering and crafting configurations alter the player’s supply strategy. This creates player-driven material availability and demand without inventing a Soulstone marketplace. |
| Achievements and titles | Replace “own every upgrade” as the only success state with valid-path completion, branch mastery, or using both sides of a trade-off over time. |
| Regions | Area creature pools and gathering nodes provide contexts for specialization. Region progression remains the source of opportunities rather than a mandatory Soulstone gate. |

Initially keep direct combat-power effects out of Arena, raids, and World Tower. Those systems use competitive or frozen snapshots, making direct constellation combat stats much more expensive to reason about and balance.

## 9. Avoiding fake depth

### Complexity without depth

A node graph is not automatically deep. Every keystone must change the player’s optimal behavior or risk preference. If two nodes both say “gain 8% more rewards,” the graph is decorative.

### Mandatory chore

Do not create activity-specific profiles that players swap before every action. A constellation should be a relatively stable character configuration, not a pre-click checklist.

Phase 1 should retain the current free full reset so experimentation is safe. Instrument reset frequency before adding any cost or cooldown.

### False choice

Pairs should target comparable expected value with different variance, timing, or goals.

Telemetry should compare:

- Selection rates
- Resource velocity
- Progression velocity
- Reset frequency
- Performance by character stage
- Whether one option dominates after players understand it

Avoid obvious temporary pairs such as “XP while leveling” versus “loot at cap.” That is scheduling, not identity.

### Progression inflation

Do not add generic damage, health, or universal item-power multipliers. The game already has multiple vertical progression layers.

### Resource bloat

No constellation fragments, keystone dust, respec tokens, or branch currencies. Use Soulstones and existing progression achievements.

### Feature isolation

Every Phase 1 keystone should be implemented in an existing activity service and visible in its existing result flow.

### Content explosion

Ship ten reusable keystones, not bespoke nodes for every creature, dungeon, recipe, region, or Essence. The depth should emerge from combining them with existing content.

## 10. Solo-developer rollout

### Phase 1 — Foundation

This should capture roughly 60–70% of the value:

- Add investment gates and exclusive choice groups to Soulstone definitions.
- Add two keystones for each of the five existing branches.
- Implement one clear trade-off mechanic per branch.
- Reuse the existing `CharacterSoulstoneUpgrade` rows to represent ownership.
- Retain full-tree reset and full refund.
- Update server validation to enforce prerequisites and sibling exclusion.
- Upgrade the current branch-card UI into a compact path view with side-by-side keystone comparison.
- Explain excluded choices and reset consequences.
- Surface keystone triggers in existing combat, gathering, crafting, and dungeon summaries.
- Replace or reinterpret `AllSoulstoneUpgradesMaxed`, which becomes impossible once choices are exclusive.
- Add telemetry and balance tests.

Do **not** include Bonds, presets, targeted respec, prestige, regional keystones, new currencies, or direct competitive combat bonuses in Phase 1.

### Phase 2 — Expansion

- Add a small number of limited cross-branch **Bonds**.
- Allow at most one or two Bonds, preserving opportunity cost.
- Add mastery objectives based on using the selected mechanic.
- Connect selected unlocks to existing milestones such as Codex completion, dungeon mastery, or Tower progression.
- Consider targeted branch respec only if full reset becomes an actual usability problem.

Bonds should represent natural relationships—such as Essence hunting plus combat challenge—not arbitrary “collect three unrelated tokens” gates.

### Phase 3 — Long-term

- Regional or endgame legendary keystones.
- A limited “Crown” or primary constellation that further defines the character.
- Horizontal mastery and collection goals.
- Optional presets only if telemetry demonstrates that players genuinely maintain multiple strategic identities.
- Additional keystone pairs added slowly as new content creates meaningful contexts.

Avoid infinite percentage tiers. Endgame should expand configurations, not merely add another exponential ladder.

## 11. Technical fit

The concept fits the existing architecture well.

### Existing concepts to extend

- [`SoulstoneUpgradeDefinition.cs`](../../LL/src/Core/Domain/Models/Soulstones/UpgradeDefinition/SoulstoneUpgradeDefinition.cs) already contains branch, rank costs, effects, applicability notes, prerequisites, frontend hints, and convenience classification.
- [`SoulstoneUpgradeEffect.cs`](../../LL/src/Core/Domain/Models/Soulstones/UpgradeDefinition/SoulstoneUpgradeEffect.cs) provides typed bonus categories.
- [`CharacterSoulstoneUpgrade.cs`](../../LL/src/Core/Domain/Models/Soulstones/CharacterSoulstoneUpgrade.cs) already persists definition ID and rank per character.
- [`SoulstoneUpgradeDefinitionProvider.cs`](../../LL/src/Infrastructure/Service/Services.LL/Providers/SoulstoneUpgradeDefinitionProvider.cs) provides the data-driven definition boundary.
- [`SoulstoneUpgradeController.cs`](../../LL/src/API/API.LL/Controllers/V1/SoulstoneUpgradeController.cs) already exposes get, purchase, and reset operations.
- [`soulstone-upgrade.state.service.ts`](../../LL/src/Presentation/ll/src/app/core/services/api/soulstone-upgrade/soulstone-upgrade.state.service.ts) already owns frontend loading, mutation, affordability, branches, and state synchronization.

Likely definition additions:

- `NodeType`
- `RequiresBranchRanks`
- `ChoiceGroup`
- Possibly explicit rank prerequisites rather than the current any-rank ownership check
- A typed trade-off or keystone descriptor

### Persistence

Phase 1 can avoid a database migration. An owned keystone can remain a normal `CharacterSoulstoneUpgrade`, with exclusivity derived from definition metadata.

A later system with saved profiles, active/inactive constellations, or separately equipped Bonds would require a new persistence concept and EF Core migration.

### State synchronization

Purchase and reset already invalidate Soulstone and character state through the existing synchronization flow. The new response shape should remain authoritative and return changed sibling availability immediately.

[`StateSyncCommandScopeCatalog.cs`](../../LL/src/Core/Application/MediatR/Synchronization/StateSyncCommandScopeCatalog.cs) is the relevant registration point.

### Combat-engine implications

Phase 1 should favor progression and activity-rule effects over raw combat attributes. Direct combat-stat nodes would require careful semantics across:

- Idle combat
- Dungeon snapshots
- Raid parties
- Tower expeditions
- Arena and tournament snapshots
- Power ratings and encounter calibration

Keeping the first release out of direct combat power materially reduces implementation and balance risk.

### Architectural concern

[`SoulstoneUpgradeService.cs`](../../LL/src/Infrastructure/Service/Services.LL/Soulstones/SoulstoneUpgradeService.cs) works directly against `IDbContext`. That is inconsistent with the repository preference for persistence boundaries.

A depth expansion is a sensible point to introduce a focused Soulstone repository for loading ownership, purchasing, resetting, and transactions. This is not required by the game design, but adding more rules directly to the current service would deepen the architectural coupling.

## 12. Final recommendation

### Recommended system

Deepen the Soulstone Archive into a branching Soulstone Constellation system with exclusive, rule-changing keystones.

### Core problem

Soulstones are a universal, long-lived reward whose spending layer currently offers purchase-order optimization but little specialization, opportunity cost, or character identity.

### Core design idea

Keep existing incremental upgrades, then add branch investment gates and paired keystones that trade reliability against risk, targeting against breadth, quantity against rarity, and security against potential.

### Why this adds actual depth

Players change what they pursue and how they use existing systems. One benefit excludes another, and the best choice depends on personal goals rather than a universal numerical ranking.

### Why it fits LegendsLegacy

It connects combat, Essences, gathering, crafting, dungeons, Prophecies, guild rewards, regions, and the economy through infrastructure that already exists.

### Why it is worth building

Approximately ten keystones and five reusable behavior hooks can make most existing activities more strategically expressive. That is unusually high player value for a solo-developer scope.

### Phase 1

Five branch gates, ten exclusive keystones, server-enforced choice groups, a path-oriented frontend, visible effect feedback, free full reset, telemetry, and no new currency or persistence model.

### What I would not build

- Another Soulstone-related currency
- Per-activity constellation swapping
- Dozens of bespoke nodes per region or creature
- Infinite percentage ranks
- Direct PvP/raid/Tower power bonuses in Phase 1
- Cross-branch Bonds in the initial release
- Prestige before the base choices are proven
- A punitive respec cost without evidence that frequent switching is harmful
- An elaborate graphical star map whose complexity exceeds the decisions it represents

### Confidence

**9/10.**

The strongest evidence is the contrast between the system’s reach and its decision structure: 14 persistent upgrades, 70 total ranks, five apparent branches, one identical cost curve, zero used prerequisites, no exclusivity, full refund resets, all bonuses active simultaneously, and an achievement that defines completion as maxing everything.

That is an unusually clear case of a mature technical foundation supporting only the first portion of its possible gameplay depth.

## Verification and repository impact

- No implementation, configuration, migration, or deployment assets were changed as part of the analysis.
- All gameplay JSON examined during the audit parsed successfully.
- The prescribed backend command, `./build/run-tests.ps1`, passed: **1,588 tests, 0 failures, 0 skipped**.
- The initial sandboxed attempt could not read the user-level NuGet configuration; rerunning with the required access completed successfully.
- The repository already contained unrelated working-tree changes, which were left untouched.
