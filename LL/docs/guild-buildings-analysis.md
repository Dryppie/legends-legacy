# Guild Buildings: Current-System Analysis

**Equipment progression update, 2 September 2026:** the analysis below records the legacy building system. Equipment progression building overviews and mutation responses now use descriptions from [equipment-guild.v1.json](../src/API/API.LL/Data/equipment/equipment-guild.v1.json). Market Office copy matches the implemented rotating Tempered Scrap and reusable-style Blueprint stock. Workshop copy explicitly states that it has no equipment progression benefit; no new Workshop mechanic was added. Prices, level requirements, client building visibility and shared legacy definitions are preserved. The [implementation ledger](../../docs/design/equipment-implementation-status.md) covers the separate mission, shop and permanent guild-equipment integration, its tests, and remaining conversion work. Equipment progression activation flags remain off.

## Scope

This document analyzes the guild building system as it currently exists in `LL/`. It covers:

- the rules enforced by the backend,
- the effects that are actually consumed by missions, shop, membership, and building costs,
- the information shown on the Buildings tab,
- progression and Guild Supply economics,
- content and code redundancies,
- misleading or unclear behavior,
- and a recommended redesign order.

This is an analysis document. It does not treat a benefit as functional merely because its content definition marks it as implemented. A benefit is considered functional only when another part of the game reads the building or its level and changes player-facing behavior.

Primary sources reviewed:

- `LL/src/API/API.LL/Data/guilds/guild-content.json`
- `LL/src/Infrastructure/Service/Services.LL/Guilds/GuildBuildingService.cs`
- `LL/src/Infrastructure/Service/Services.LL/Guilds/GuildMissionService.cs`
- `LL/src/Infrastructure/Service/Services.LL/Guilds/GuildShopService.cs`
- `LL/src/Infrastructure/Service/Services.LL/Guilds/GuildContentProvider.cs`
- `LL/src/Core/Domain/Extensions/Guilds/GuildExtensions.cs`
- `LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-buildings/`
- `LL/tests/EssenceSystem.Tests/GuildBuildingServiceTests.cs`
- `LL/tests/EssenceSystem.Tests/GuildExtensionsTests.cs`

## Executive Verdict

The building system has a good technical and visual foundation, but it currently overstates how much game is behind it.

There are nine buildings. Only four have real runtime effects:

1. Guild Hall
2. Mission Board
3. Market Office
4. Treasury

The other five have no runtime consumer outside their content definitions:

1. Workshop
2. Raid Hall
3. War Room
4. Training Grounds
5. Essence Sanctum

This is the central problem. More than half of the nominal building progression cost is attached to buildings that currently do nothing after purchase.

The system is strongest when a building connects to a recurring loop:

- Guild Hall changes recruitment capacity and unlock progression.
- Mission Board changes recurring missions, orders, and rewards.
- Market Office changes purchasable shop stock.

It is weakest when the building is only a theme, future hook, or percentage:

- Treasury is one passive discount described four different ways.
- Workshop claims ownership of crafting content that already exists without it.
- Raid Hall and War Room promise tabs that do not exist.
- Training Grounds and Essence Sanctum currently buy only a level number.

The current system should be reduced to honest, useful choices before it is expanded. A smaller set of functional buildings would suit the game better than nine equally purchasable buildings with radically unequal value.

## Current Rules

### Construction And Upgrades

- Buildings and upgrades complete immediately.
- Only the leader and officers can spend Guild Supplies.
- Every building type can exist only once per guild.
- The Guild Hall permanently exists and is lazily created at level 1 for older guilds.
- Guild Hall level gates the initial construction of secondary buildings.
- Guild Hall level does not gate later upgrades of a building.
- There are no building slots, mutually exclusive choices, queues, demolition, refunds, or respecs.
- Every building can eventually be owned and maxed if the guild has enough supplies.

This makes the system an investment-order problem, not a headquarters-layout problem. The strategic question is simply which permanent upgrade should receive supplies first.

### Currency

- Every building currently costs only Guild Supplies.
- Cost increases linearly by level.
- Treasury reduces future Guild Supply costs by 2% per Treasury level.
- Costs are calculated and validated by the server.

The API models costs as a resource dictionary, but only Guild Supplies are currently produced by the building service. This is reasonable future-proofing, although it adds contract and frontend complexity that is not yet used.

### Permissions And Feedback

- Leaders and officers can construct and upgrade.
- Regular members can inspect buildings but cannot spend supplies.
- Successful actions create activity-log entries.
- Building changes publish a guild realtime event, causing guild state to refresh for members.
- The interface shows the next cost, available supplies, shortfall, level progression, benefits, and recent activity.

These are good boundaries. Shared-currency authority is enforced server-side rather than trusted to the client.

## Cost And Value Map

The table below shows nominal costs before Treasury discounts. Guild Hall starts at level 1, so its total is the cost of levels 2 through 10. Other buildings start unbuilt, so their total covers levels 1 through 5.

| Building | Hall Requirement | Max Level | First Purchase | Nominal Cost To Max | Real Runtime Effect? |
| --- | ---: | ---: | ---: | ---: | --- |
| Guild Hall | 1 | 10 | 300 | 8,100 | Yes |
| Mission Board | 1 | 5 | 100 | 1,500 | Yes |
| Market Office | 1 | 5 | 150 | 2,000 | Yes |
| Treasury | 2 | 5 | 175 | 2,375 | Yes, one passive effect |
| Workshop | 2 | 5 | 175 | 2,375 | No |
| Raid Hall | 4 | 5 | 400 | 4,250 | No |
| Training Grounds | 4 | 5 | 300 | 3,500 | No |
| Essence Sanctum | 4 | 5 | 300 | 3,500 | No |
| War Room | 6 | 5 | 500 | 5,000 | No |

Total nominal spend from the starting state to every maximum level is 32,600 Guild Supplies.

The five buildings with no runtime effect account for 18,625 supplies, or roughly 57% of that total. This is too much shared progression currency to attach to placeholder content.

## What Is Great

### The Layout Is Strong

The Buildings tab has a useful information hierarchy:

- built, ready, and Hall-locked buildings are separated in a stable left rail,
- the selected building owns the main workspace,
- current level and level segments remain visible,
- cost and supply progress have a dedicated action panel,
- benefits are presented as an upgrade path,
- and recent guild activity is available without leaving the page.

This layout supports repeated use and comparison well. It is compact, game-like, and consistent with the rest of the guild area.

### Immediate Completion Fits The Current System

Removing construction timers was the right choice for the current depth of the feature. There is no queue management, worker assignment, acceleration economy, or meaningful scheduling decision that would justify waiting. Immediate completion keeps the interaction focused on the actual decision: whether the benefit is worth the Guild Supplies.

### The Guild Hall Member Benefit Is Clear

The Guild Hall now has a concrete per-level effect:

- base guild capacity is 10,
- each Guild Hall level adds one member,
- level 1 therefore supports 11 members,
- and level 10 supports 20 members.

The Buildings tab explicitly shows current capacity, maximum capacity, and the next level's `+1 member slot` benefit. This is one of the clearest pieces of the system.

### Server Authority Is Good

The server validates:

- guild membership,
- leader or officer permissions,
- valid building types,
- uniqueness,
- Guild Hall requirements,
- maximum levels,
- and available supplies.

The client receives `CanConstruct`, `CanUpgrade`, and `LockedReason`, but it is not the source of truth. This is the correct approach for shared guild resources.

### Mission Board And Market Office Prove The Concept

Both buildings affect recurring systems outside the Buildings tab. Members can feel the consequences of those purchases during normal play. That is the standard every building should meet.

### Planned Benefits Are Visually Distinguishable

The upgrade path can show `Active`, `Next`, level requirements, and `Planned`. That is a good presentation mechanism. The problem is not the badge design; it is that some nonfunctional level-1 benefits are incorrectly marked implemented.

## System-Wide Problems

### 1. The Content Definitions Are Not Truthful Enough

`IsImplemented` currently mixes several meanings:

- a real mechanical effect,
- the existence of a building record,
- a possible data configuration hook,
- or preparation for a future feature.

Those are not equivalent.

Examples:

- Raid Hall says it unlocks a Raids tab state, but no Raids tab exists in the guild UI.
- War Room says it unlocks a Wars tab state, but no Wars tab exists.
- Workshop says crafting orders can appear, but crafting orders already appear without Workshop.
- Training Grounds and Essence Sanctum mark their level-1 foundations implemented even though no service consumes either building.

An implemented badge should mean that buying the level changes current gameplay. By that definition, those claims are false.

### 2. Placeholder Buildings Are Fully Purchasable

The backend does not stop construction or upgrades when every useful benefit is planned. A leader can spend thousands of shared supplies and receive no effect.

The `IsImplemented` field is presentation metadata only. It does not participate in validation, `CanConstruct`, or `CanUpgrade`.

This is the highest-priority product problem because it permits irreversible shared-currency mistakes.

### 3. The Progression Tree Is Shallower Than It Looks

Guild Hall requirements gate only the first construction:

- Hall 1: Mission Board and Market Office
- Hall 2: Workshop and Treasury
- Hall 4: Raid Hall, Training Grounds, and Essence Sanctum
- Hall 6: War Room

After construction, a guild can immediately upgrade that secondary building to level 5 without further Hall requirements. For example, a Hall 1 guild can max Mission Board and Market Office.

This is simple, but it is not communicated. It also weakens the sense that headquarters progression and secondary-building progression are connected.

### 4. Many Purchasable Levels Have No Immediate Payoff

The interface allows every level below max to be purchased, but not every level has a distinct effect.

Market Office no longer has this problem: level 4 now unlocks a rotating Blueprint. Mission Board level 4 also has an effect because the reward bonus rises from 15% to 20%, but that effect is not stated in the upgrade path. The next visible milestone remains level 5.

For nonfunctional buildings, every level is effectively dead.

### 5. The Next Upgrade Is Not Explained Precisely

The action panel clearly shows cost, but only Guild Hall gets a dedicated next-level benefit summary.

For other buildings, users must infer the result from the entire benefit path. That fails when:

- the next numbered level has no milestone row,
- a passive effect changes every level,
- the next milestone is more than one level away,
- or the benefit applies only after a reset.

Every upgrade should state exactly what changes at the next level before the button is pressed.

### 6. Shared Spending Has No Confirmation

Selecting a building and pressing Build or Upgrade immediately spends shared supplies. There is no confirmation containing:

- building name,
- current and target level,
- final cost,
- remaining balance,
- and exact unlocked effect.

The selected-building step prevents accidental clicks from the rail, but it is not enough protection for expensive, irreversible shared purchases.

### 7. "Ready To Build" Is Not Always Ready

The left rail puts every unbuilt building whose Hall requirement is met under `Ready to Build`.

That grouping ignores:

- insufficient supplies,
- regular-member permissions,
- and whether the building has any implemented effect.

A regular member can therefore see a building labeled ready with a visible `Build` command even though the action is disabled. The detailed panel eventually explains the lock, but the rail classification is misleading.

Better states would be:

- Available
- Affordable
- Requires supplies
- Officer approval required
- Coming later
- Requires Hall level X

### 8. "Building Log" Is Actually The Whole Guild Activity Log

The building overview returns the latest ten entries from the shared guild activity table. Those entries include:

- construction and upgrades,
- mission selection,
- personal order reward claims,
- weekly mission reward claims,
- and shop purchases.

The UI labels this stream `Building Log`, which is inaccurate. Non-building activity can also push actual building history out of the ten-entry result.

The log DTO includes `CharacterId`, but the UI does not show an actor name or identity. The visible entry therefore answers what happened and when, but not who spent the supplies or took the action.

The visible timestamp is time-only. Older entries require hovering to discover the date.

### 9. Data-Driven Content Is Duplicated

Building definitions exist twice:

1. JSON content in `guild-content.json`
2. C# fallback definitions in `GuildContentProvider.cs`

Names, descriptions, costs, benefits, and implementation flags must remain manually synchronized. This is a substantial redundancy and an easy source of drift.

The content validator checks structural correctness, but not parity between both sources.

It also does not validate several important design rules:

- duplicate benefit levels,
- missing effects on purchasable levels,
- Hall requirements above the Hall maximum,
- whether implemented claims have a runtime effect,
- whether shop stock exists for a Market Office milestone,
- or whether a placeholder building should be purchasable.

### 10. The Frontend Activity Type Is Incorrectly Narrow

The backend activity enum contains six event types, but the TypeScript `GuildActivityLogType` declares only:

- `BuildingConstructed`
- `BuildingUpgraded`

The building endpoint can and does return mission and shop event types. JavaScript accepts those strings at runtime, but the TypeScript contract does not describe the real API response.

### 11. Some API State Is Redundant Or Underused

`GuildBuildingOverview` exposes `CanManageBuildings`, but the Buildings component does not use it for a page-level permission state. It relies on per-building action flags and locked reasons instead.

This information should either be used to explain the member's role once at page level or removed from the contract.

Similarly, the generic next-cost dictionary supports multiple resources, but the current UI and service are effectively Guild-Supplies-only. This is acceptable if multi-resource costs are planned; otherwise, a simpler contract would be clearer.

### 12. Test Coverage Does Not Match The Shared-Currency Risk

The shop now has focused coverage for Common and Rare catalyst pools, weekly rotation, the level-4 Blueprint rotation, item grants, and headline default reward values. Building-system coverage remains thin elsewhere.

Current focused tests cover:

- lazy creation of Guild Hall level 1,
- immediate construction and supply spending,
- immediate upgrades,
- and member-cap calculations.

Important missing tests include:

- leader, officer, and regular-member permissions,
- insufficient supplies,
- duplicate construction,
- Hall requirement enforcement,
- maximum-level rejection,
- cost growth by level,
- Treasury rounding and discounts,
- self-discount behavior while upgrading Treasury,
- locked-reason accuracy,
- placeholder-building purchase policy,
- Mission Board reset timing,
- the remaining Market Office level-to-stock thresholds,
- JSON and fallback-definition parity,
- and frontend rendering of non-building activity types.

### 13. Concurrent Officer Purchases Are Not Protected

`GuildResource` has no concurrency token, and building levels have no concurrency token. The service loads the current supply amount, checks it in memory, subtracts from the tracked entity, and saves at the end of the command transaction.

A transaction makes each individual command atomic, but the default isolation does not prevent two officers from reading the same starting balance at the same time. Concurrent requests can therefore pass the same affordability check and overwrite one another's resource update. Concurrent upgrades of the same building can also calculate from the same starting level.

Possible outcomes include:

- two different buildings being purchased while only one effective deduction survives,
- duplicate upgrade activity for one resulting level,
- or inconsistent charging when simultaneous actions overlap.

The unique guild-and-building-type index prevents duplicate records of the same building type, but it does not protect the shared resource balance or an existing building level.

This should be treated as a correctness issue, not an optional optimization. Use an atomic guarded resource update, row locking, a concurrency token with retry/rejection, or a suitably isolated transaction for shared purchases.

## Building Verdicts

| Building | Actual Current Value | Clarity | Verdict |
| --- | --- | --- | --- |
| Guild Hall | Strong progression spine and member-cap growth | Mostly clear | Keep and enrich high levels |
| Mission Board | Strongest recurring gameplay effect | Exact percentages and reset timing are unclear | Keep and deepen |
| Market Office | Strong progression stock, weekly choices, and Blueprint access | Balance is not backed by live economy telemetry | Keep, monitor, connect to other buildings |
| Treasury | Small future cost discount | Savings and return are hard to evaluate | Completely rework mechanically |
| Workshop | No actual runtime effect | "Implemented" claims are misleading | Disable now, rebuild around guild crafting |
| Raid Hall | No actual runtime effect | Promises a nonexistent tab | Hide until raid MVP exists |
| War Room | No actual runtime effect | Promises a nonexistent tab | Hide until guild-war MVP exists |
| Training Grounds | No actual runtime effect | Identity overlaps raid and war | Replace with a recurring challenge building |
| Essence Sanctum | No actual runtime effect | Strong theme, no delivered loop | Rebuild as a core game-specific system |

## Building-By-Building Analysis

## Guild Hall

### What It Actually Does

- Exists permanently at level 1.
- Adds one member slot per Hall level.
- Raises member capacity from 11 at level 1 to 20 at level 10.
- Unlocks Workshop and Treasury construction at level 2.
- Unlocks Raid Hall, Training Grounds, and Essence Sanctum construction at level 4.
- Unlocks War Room construction at level 6.

### What Is Great

- It is a natural root building.
- Its member-cap effect matters at every level.
- Capacity is enforced when invitations and applications are accepted, not merely displayed.
- The UI calls out current and next capacity clearly.
- Its unlock levels give the building tree an understandable spine.

### What Is Unclear Or Weak

- The benefit path shows milestone unlocks at levels 1, 2, 4, and 6, while the per-level member benefit lives in separate UI. The two presentations are correct but fragmented.
- Levels 7 through 10 provide only member capacity. That is not dead progression, but it becomes expensive: the level-10 upgrade costs 1,500 supplies before Treasury discount for one final slot.
- Several Hall unlocks currently lead to nonfunctional buildings, reducing the excitement of reaching Hall 4 and Hall 6.
- The frontend hard-codes maximum member capacity as 20 while the backend derives it from a default base capacity of 10 plus Hall level. Those values can drift if configuration changes later.

### Recommendation

Keep Guild Hall. It is one of the best parts of the current system.

Add one operational benefit at selected upper levels without weakening the member-cap rule. Suitable additions include:

- recruitment listing improvements,
- an extra officer or management permission slot,
- guild announcement or planning tools,
- deeper activity-history retention,
- guild policy slots,
- or cosmetic guild identity customization.

Do not add waiting-time or construction-queue benefits now that construction is immediate.

## Mission Board

### What It Actually Does

Mission Board affects recurring guild play at every level:

| Level | Actual Effect |
| ---: | --- |
| 0 | 3 weekly options, 3 daily orders, no reward bonus |
| 1 | 5% bonus to Favor, Guild XP, and Guild Supplies from order and weekly-mission reward claims |
| 2 | 10% reward bonus and a fourth weekly mission option |
| 3 | 15% reward bonus and a fourth personal daily order |
| 4 | 20% reward bonus |
| 5 | 25% reward bonus |

Weekly mission targets are paced around a ten-player guild and the ten-second action timer. One player can perform 43,200 uninterrupted actions in five days, so ten players can perform 432,000. Monster Extermination and Craftsmen's Commission use that full guild capacity. Platinum requires a 10% personal share, placing it at exactly 43,200 kills or tempering actions.

| Weekly Mission | Guild Target | Personal Platinum Threshold |
| --- | ---: | ---: |
| Monster Extermination | 432,000 creatures | 43,200 creatures |
| Dungeon Expedition | 1,000 rooms | 100 rooms |
| Craftsmen's Commission | 432,000 actions | 43,200 actions |
| Dungeon Vanguard | 100 runs | 10 runs |

Guild progress stops at the shared target, but matching personal contribution continues until the weekly reset. This prevents an active guild from ending personal tier progression before a member reaches Platinum.

Weekly missions are never selected automatically. The service generates the available choices, then waits for a guild leader or officer to select one. The former calendar-week fallback, which selected the first option after Monday had been underway for 24 hours, was removed because it caused newly created midweek guilds to lose their choice immediately.

### What Is Great

- It changes systems members use every day and every week.
- It offers both breadth and reward growth.
- It improves the Guild Supply engine that funds headquarters progression.
- Every level has a real effect.
- Its cost is modest enough to feel attainable.

This is the best current example of what a guild building should be.

### What Is Unclear Or Redundant

- The level-1 description says it "keeps the mission system active," but missions and orders exist without Mission Board.
- The exact 5% per-level reward increase is not stated in the upgrade path.
- "Mission currency" should be named explicitly as Guild Favor.
- Level 4 has no benefit row even though its reward bonus increases.
- Weekly options are generated once per week. Upgrading to level 2 after options exist does not add an option until the next weekly generation.
- Personal orders are generated once per member per day. Upgrading to level 3 after today's orders exist does not add the fourth order until the next daily generation.
- Reward bonuses are evaluated when rewards are claimed, so that part takes effect immediately. The UI does not explain this split timing.

### Strategic Problem

Mission Board is likely the optimal first investment because it increases Guild Supplies, which then funds every other building. That is acceptable only if competing buildings offer equally visible strategic value. Currently they do not.

### Recommendation

Keep and deepen Mission Board.

First, fix the copy and exact-level preview. Then add officer decisions rather than only passive growth:

- one weekly reroll,
- mission focus categories,
- difficulty and reward tiers,
- building-specific mission pools,
- or catch-up orders for less active members.

## Market Office

### What It Actually Does

| Level | Actual Effect |
| ---: | --- |
| 0 | All shop items are locked by Market Office requirement |
| 1 | Unlocks Common stock: two weekly catalyst caches drawn from the same five catalyst families as Rare stock, plus 25 Soulstones |
| 2 | Adds two purchases of 10 Sigil Fragments, gated by 100 weekly contribution |
| 3 | Unlocks Rare stock, including one rotating six-catalyst cache and stronger resource offers |
| 4 | Unlocks one rotating Blueprint selected from eleven existing crafting designs |
| 5 | Adds a second rotating six-catalyst Rare offer |

Both Common and Rare stock reset weekly. Their catalyst pools both rotate across Fury, Arcane, Venom, Hive, and Primal recipes. Items can also have contribution, Favor, and weekly-limit requirements.

### What Is Great

- The level ladder is easy to understand conceptually.
- It changes visible stock in another tab.
- Every level now has a concrete stock unlock.
- Rare rotation gives the building recurring relevance while a guaranteed Blueprint offer gives level 4 a distinct identity.
- Contribution requirements connect shop access to guild participation.
- The Shop tab shows reset timing, cost, limits, and locked reasons.
- Soulstones, Sigil Fragments, Blueprints, and Blueprint recipe catalysts are the shop's primary rewards.
- The shop does not sell Cinders or award Fate Echo, protecting the main currency economy and keeping stock focused on meaningful progression materials.

### What Is Unclear Or Weak

- The building name can be confused with the global marketplace; this behaves more like a quartermaster or guild commissary.
- Stock is generic and depends only on Market Office level, not on the rest of the headquarters.
- The building's level-2 and level-3 benefits are meaningful, but the Buildings tab does not preview the exact items or slot count unlocked next.
- Blueprint rotation does not account for what a member has already learned, and the deterministic weekly picker can select the same design in consecutive weeks.
- Reward values are intentionally substantial, but no production telemetry currently validates Favor income, purchase rates, or player currency balances.

### Recommendation

Keep Market Office in its current five-level shape. Its progression now has a useful cadence: Common catalysts and Soulstones, Sigil Fragments, Rare stock, Blueprints, then a second Rare catalyst slot.

The next improvements should focus on quality rather than adding more raw stock:

- collect purchase-rate and currency-balance telemetry before another numeric rebalance,
- prevent frustrating Blueprint repetition or offer a choice mechanism,
- make learned Blueprint duplicates intentionally tradable or otherwise reusable,
- and preview the exact next-level stock on the Buildings tab.

Longer term, make other functional buildings add stock categories. This creates a connected headquarters without requiring building slots.

## Treasury

### What It Actually Does

- Reduces future building costs by 2% per Treasury level.
- Reaches a maximum discount of 10% at level 5.
- Uses ceiling rounding, so fractional discounted costs round upward.
- Discounts its own later upgrades because the current Treasury level is read before each next cost is calculated.

The nominal cost to max Treasury is 2,375 supplies. Its actual self-discounted sequence is approximately:

- level 1: 175
- level 2: 319
- level 3: 456
- level 4: 588
- level 5: 713
- total: 2,251

### What Is Great

- The effect is implemented consistently in the server cost calculation.
- The discount immediately updates costs across all remaining buildings.
- The theme of financial efficiency suits a Treasury.

### What Is Redundant

Four benefit rows describe one linear passive effect:

- Supply Ledger
- Supply Storage
- Supply Efficiency
- Quartermaster Network

There is no separate storage mechanic, ledger, budget, stipend, donation system, or quartermaster action. The names imply multiple systems that do not exist.

### What Is Weak Or Unclear

- The UI shows discounted cost but not base cost, discount percentage, or supplies saved.
- The player cannot judge return on investment without external calculation.
- At a 10% maximum discount, a maxed Treasury needs roughly 22,510 supplies of later nominal spending to recover its own 2,251 cost. Progressive discounts make the real comparison dependent on purchase order.
- The best mathematical time to buy it is early, but early guilds are least able to afford a long-term efficiency investment.
- It changes no member behavior and creates no recurring guild decision.

### Recommendation

Completely rework Treasury's mechanics while keeping the building name.

Its primary identity should be guild logistics, not only cheaper buildings. A focused first version could combine:

- visible member supply donations,
- a weekly supply stipend based on guild activity,
- officer spending history and budgets,
- and a smaller retained building-cost discount.

That would give members a contribution path, officers a management tool, and the guild a recurring economic loop.

## Workshop

### What It Actually Does

Nothing currently reads Workshop type or level outside its definition.

Its stated implemented effects are not real dependencies:

- Crafting and tempering missions already exist in the default pools without Workshop.
- Shop items can be themed through content, but the shop definition has only a Market Office level requirement. It cannot require Workshop level.

### What Is Good

- The theme is appropriate for a game with crafting and tempering systems.
- It offers a natural non-combat guild identity.
- Its Hall 2 position could make it an early alternative to pure mission optimization.

### What Is Misleading

- Levels 1 and 2 are marked implemented despite producing no current behavior.
- "Workshop Stock" describes developer configurability, not a player benefit.
- The guild can pay 2,375 supplies to max a record that no gameplay service reads.

### Recommendation

Disable construction now, then rebuild Workshop around cooperative crafting.

A suitable minimum viable loop would be:

1. The guild receives a weekly crafting commission.
2. Members contribute qualifying crafted or tempered items.
3. Progress produces Guild Supplies and crafting-themed personal rewards.
4. Workshop levels add commission choice, reward quality, or an additional project.
5. Completed Workshop milestones add real Workshop-gated stock to Market Office.

This would make Workshop distinct, social, and useful without needing a massive new combat system.

## Raid Hall

### What It Actually Does

Nothing currently reads Raid Hall type or level outside its definition. There is no Raids tab in the guild interface.

### What Is Good

- A dedicated operations building can suit cooperative raid content.
- Hall level 4 is a reasonable point for a more advanced guild system.

### What Is Misleading

- Level 1 claims to unlock a Raids tab state that does not exist.
- Level 1 is marked implemented while providing no runtime behavior.
- It costs 4,250 supplies to max, making it one of the most expensive placeholder purchases.

### Recommendation

Hide or disable Raid Hall until a raid MVP exists.

When raids are playable, Raid Hall should own concrete operations such as registration, member signup, role assignment, raid windows, contribution scoring, and raid rewards. It should be designed with that system rather than ahead of it.

## War Room

### What It Actually Does

Nothing currently reads War Room type or level outside its definition. There is no Wars tab in the guild interface.

### What Is Good

- It has a clear fantasy if guild wars become a real system.
- Hall level 6 creates appropriate anticipation for an advanced competitive feature.

### What Is Misleading

- Level 1 claims to unlock a Wars tab state that does not exist.
- Level 1 is marked implemented while providing no runtime behavior.
- At 5,000 supplies to max, it is the most expensive non-Hall building and currently has no return.

### Recommendation

Hide or disable War Room until guild wars have a playable minimum version.

When implemented, it should own registration, roster management, scouting, attack and defense planning, phase visibility, seasonal rating, and war-specific rewards. Generic passive combat bonuses would not justify this building's cost or Hall requirement.

## Training Grounds

### What It Actually Does

Nothing currently reads Training Grounds type or level outside its definition.

### What Is Good

- A recurring combat-practice building could work without waiting for raids or wars.
- It could provide an accessible combat contribution path for all guild members.

### What Is Redundant Or Unclear

- Its current identity overlaps with both Raid Hall and War Room.
- "Unlocks the building foundation" is not a gameplay effect.
- Its planned raid and war preparation bonuses depend on two other unimplemented systems.
- The name does not tell the player what recurring action becomes available.

### Recommendation

Completely replace its current identity.

The strongest replacement is `Proving Grounds`: a weekly guild challenge space with simulated encounters, member scoreboards, build testing, and modest repeatable rewards. That creates a real loop using existing combat systems and avoids waiting for raid or war development.

If that loop is not planned, remove the building until it has a unique purpose.

## Essence Sanctum

### What It Actually Does

Nothing currently reads Essence Sanctum type or level outside its definition.

### What Is Good

- It is one of the strongest thematic fits for Legends Legacy.
- Existing essence, Soulstone, Fate Echo, and Sigil Fragment economies provide material for a guild-level loop.
- It can become meaningful without requiring a large multiplayer battle mode.

### What Is Weak

- Every current benefit is only a future promise.
- Level 1 is marked implemented solely because a building foundation exists.
- Its name creates a high expectation that the current system does not meet.

### Recommendation

Rebuild Essence Sanctum as a core mid-game building before Raid Hall or War Room.

A focused version could provide:

- weekly resonance projects,
- member essence or Soulstone contributions,
- rotating essence themes,
- shared progress toward personal caches,
- and Sanctum-gated Market Office stock.

This suits the game's existing identity better than another generic combat bonus.

## Redundancy Summary

The following elements are currently redundant or unnecessarily duplicated:

1. Building content is maintained in both JSON and C# fallback definitions.
2. Treasury uses four named benefits to describe one passive percentage.
3. Several "implemented" level-1 benefits mean only that a database row can exist.
4. `CanManageBuildings` is returned but not used for a page-level permission treatment.
5. `CharacterId` is returned in activity logs but not translated into visible actor identity.
6. The TypeScript activity type describes only two of the six backend event types.
7. The generic cost dictionary supports resources that building costs do not currently use.
8. Placeholder building levels repeat the same absence of behavior at increasing prices.
9. Unlock summary, description, and benefit copy sometimes restate the same passive effect without adding decision-relevant detail.

## Recommended Product Direction

### Keep As Core Buildings

- Guild Hall
- Mission Board
- Market Office

These already answer a useful question: what can the guild do now that it could not do before?

### Rework Immediately

- Treasury

Keep the fantasy, but replace its single passive identity with logistics, donations, and recurring supply management.

### Disable Until Rebuilt

- Workshop
- Essence Sanctum
- Training Grounds

These can become good buildings using systems already close to the game's current identity, but they should not consume supplies before their loops exist.

### Hide Until Their Parent Systems Exist

- Raid Hall
- War Room

Their value cannot be honestly delivered without raids and guild wars.

## Recommended Implementation Priorities

### Priority 0: Stop Selling No-Effect Levels

1. Protect Guild Supply spending and building levels from concurrent officer actions.
2. Make a building or level non-purchasable when it has no current gameplay effect.
3. Correct all false `IsImplemented` values.
4. Remove nonexistent Raids and Wars tab claims.
5. Fix the `Ready to Build` grouping so it reflects permissions and affordability.
6. Add a shared-spending confirmation.

### Priority 1: Make The Current Four Honest And Complete

1. Add exact next-level effect summaries for every building.
2. Explain Mission Board percentages, affected rewards, and reset timing.
3. Add telemetry for Guild Favor earnings, shop purchases, and skipped stock so Market Office values can be tuned against real play.
4. Show Treasury base cost, discount, and supplies saved.
5. Rename Building Log to Guild Activity or filter it to actual building events.
6. Show the actor and full date for shared-currency actions.

### Priority 2: Strengthen The Headquarters Network

1. Let functional specialist buildings add stock to Market Office.
2. Add building-specific mission pools to Mission Board.
3. Give upper Guild Hall levels management or identity benefits alongside member capacity.
4. Decide whether secondary-building levels should require higher Hall levels.

### Priority 3: Build The Best Near-Term Specialist Loops

1. Workshop cooperative commissions.
2. Essence Sanctum resonance projects.
3. Proving Grounds weekly combat challenges.

These can use existing crafting, essence, combat, mission, and reward systems.

### Priority 4: Add Large Multiplayer Buildings With Their Systems

1. Raid Hall alongside raid MVP.
2. War Room alongside guild-war MVP.

Do not build their level ladders independently from the features they are supposed to operate.

## Technical Recommendations

### Establish One Content Source Of Truth

Prefer one of these approaches:

- package the JSON as required application content and fail clearly if it is missing,
- generate fallback content from the same source,
- or add a parity test that compares every JSON and C# building field.

Manual duplication is the least reliable option.

### Separate Availability From Implementation

A building needs explicit states such as:

- implemented and purchasable,
- visible preview but unavailable,
- hidden,
- or deprecated.

Per-benefit `IsImplemented` is not enough to control safe purchasing.

### Define Exact Effects Per Purchased Level

Every purchasable next level should have a machine-readable effect summary. The server and UI should be able to answer:

- what changes at this exact level,
- whether it applies immediately or on reset,
- what systems and rewards it affects,
- and whether the effect is already active.

### Align Activity Contracts

- Use the complete backend activity enum in TypeScript.
- Rename the panel to Guild Activity or filter event types.
- Include actor display information when accountability matters.
- Provide date-aware timestamps and a route to older history if history is meant to be useful.

### Expand Focused Tests

Add a table-driven building test suite covering every building and level for:

- cost,
- Hall requirement,
- permissions,
- affordability,
- exact effect,
- maximum-level behavior,
- and UI-facing locked reason.

Add integration tests for Mission Board generation timing, Market Office stock thresholds, and Treasury discount rounding.

Add a relational-database concurrency test that issues two officer purchases against the same Guild Supply row. In-memory EF tests cannot validate the required locking or optimistic-concurrency behavior.

### Make Shared Spending Concurrency-Safe

Do not rely on a read-then-subtract sequence for Guild Supplies. Prefer one explicit invariant-preserving operation, for example an update whose predicate requires `Amount >= cost` and whose affected-row count determines success.

Protect building level changes as well. A version column or guarded update should ensure that an upgrade succeeds only when the persisted level still equals the level used to calculate its cost.

## Design Standard For A Guild Building

A purchasable guild building should satisfy at least two of these conditions:

- It unlocks a new recurring action.
- It creates a new officer decision.
- It creates a new member contribution path.
- It unlocks a visible reward category.
- It changes another guild system in a way members can feel.
- It expresses a distinct guild identity.

It should also answer all of these before purchase:

- What changes at the next level?
- When does that change take effect?
- Who benefits?
- How much does it cost?
- Can the guild undo the decision?

If a building cannot answer those questions, it should be shown as future content rather than sold as current progression.

## Final Assessment

The current Guild Buildings page looks more complete than the underlying game system. That is both its success and its risk.

The layout, server-side rules, immediate completion, member-cap integration, mission effects, and shop effects are strong. They form a credible base.

The main work is not adding more buildings. It is making the current list honest:

- three buildings are solid,
- one needs a major economic redesign,
- three need real specialist loops,
- and two should wait for their parent multiplayer systems.

The best next version would temporarily offer fewer purchases, but every offered purchase would visibly change how the guild plays. That would make the headquarters feel smaller on paper and much larger in practice.
