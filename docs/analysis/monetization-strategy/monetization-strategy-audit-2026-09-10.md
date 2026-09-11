# LegendsLegacy monetization strategy and repository audit

> **Historical recommendation:** This report predates the current Signet model. Its membership, trading, progression-benefit, catalog and spending-ceiling recommendations are superseded wherever they conflict with the [subscription specification](C:/repos/Legends-Legacy/legends-legacy/docs/analysis/monetization-strategy/subscription-specification.md) and [initial product catalog](C:/repos/Legends-Legacy/legends-legacy/docs/analysis/monetization-strategy/initial-product-catalog.md). Other offers remain proposals, not implemented products.

**Analysis date:** 10 September 2026

**Later product decisions:** Ornaments and the Nobility profile header were removed on 11 September 2026. The Noble badge is now an optional ◆ icon before the character name. Superseded proposals below are retained only as historical research.

**Scope:** Current LegendsLegacy game, API, application/domain rules, infrastructure services, Angular UI and relevant LL-Chat integration. Investigation and recommendations only.

**Source baseline:** `7dcbe382c8aa3d6ffb8792dd46278f067f883fd0`, plus the working tree inspected during this review. The checkout contains unrelated, ongoing changes. Source links identify the inspected implementation; this is not a claim about the deployed database or production configuration.

**Deliverable:** A proposed business model, not an implementation specification or authorization to implement it.

## 1. Executive Summary

**Choose Strategy B: a small, direct-purchase supporter and identity store, with a €4.99 / $4.99 monthly supporter membership. Do not sell progression, premium currency, Essence storage, competitive attempts or gameplay currencies.** Launch three products after the identity and payment prerequisites are ready. Consider one bounded permanent preset expansion later, only after making the free preset allowance adequate.

LegendsLegacy's strongest commercial opportunity is attachment to a persistent character, its builds, its collection and its place in a community. Its weakest opportunity is a conventional character-skin shop: the present UI is primarily names, titles, panels, equipment icons, Essence entries and combat telemetry. Build the first purchases around those visible surfaces.

Several discoveries materially change the initial premise:

1. **Essence storage is already effectively uncapped.** The Soul Archive holds owned Essences; three saved Essence loadouts are a different limitation. Selling storage would introduce a problem the game does not currently have.
2. **Useful build convenience already exists.** There are three free Essence presets and three free equipment presets, with automatic selection for seven combat activities. Combat Style switching and remembered choices also exist. Selling these existing functions back to players would damage trust.
3. **The current Doctrine-like system is Combat Styles.** Bastion, Conduit, Reaper and Duelist have independent mastery progression. Do not design a second monetized Doctrine system from older terminology.
4. **PvE purchases would reach competition.** Arena and tournaments use equipment, Essences, attributes and Combat Styles from the wider progression system. Tower first clears and account achievement rankings also matter. A label saying “PvE boost” cannot isolate its effects.
5. **An apparently cosmetic title can affect progression.** Title unlock counts feed achievements. Supporter recognition should be a separate badge entitlement, excluded from earned-title counts, achievement points and renown.
6. **Some systems are not commercially ready.** Raids have substantial implementation but are hidden by the production frontend setting; their rewards default off and the trophy vendor is empty. Region-boss rewards are also disabled in the inspected content. Existing seasonal schedules do not establish a sustainable paid pass.

The proposed monthly membership buys visible support recognition and a small fixed set of presentation choices. It does not require a monthly art treadmill. Permanent support and heraldry packs serve people who dislike subscriptions. A €500 monthly budget should buy **zero additional combat power** over €5 or €10, and the launch store should not provide enough repeatable products to absorb it.

**The game is not ready for a broad monetization launch today.** It is close enough to justify a deliberately small support product after reliable entitlements, refunds, guest-account ownership, and credible public identity surfaces exist. Do not postpone support monetization until every endgame system is finished; equally, do not sell benefits dependent on unfinished systems.

All prices and commercial expectations below are proposals to validate. No player population, conversion, willingness-to-pay, retention or revenue data was supplied.

## 2. Current Game Monetization Audit

### Evidence standard and limits

This audit follows frontend actions through API requests and service/domain behavior, including reward and combat preparation paths. “Implemented” means there is meaningful code and a corresponding route or application flow; it does not establish that production has the same seeded data or configuration. “Configured” refers to checked-in defaults/content. “Proposed” denotes a new recommendation.

The repository supports conclusions about what players *can* do and what the systems reward. It cannot establish what players actually spend most of their attention on, whether a currency is abundant in the live economy, or which cosmetic will sell. Those require telemetry and player interviews. Historical design documents, migration names and leftover assets were not treated as proof of a current gameplay loop.

### The current loop

The mechanical backbone is: choose an accessible combat area; run automated encounters; collect experience, Cinders, equipment, Essences and access materials; improve equipment and Essence builds; select a Combat Style; attempt harder combat and limited or cooperative content; pursue collections, achievements, social standing and recurring objectives.

Idle combat uses a **10-second encounter cadence** and a **24-hour retained offline window** in the checked-in API configuration. Internal batching limits are processing safeguards, not player tickets. The planner clamps old unresolved work to the retained window, and the service aggregates batches. This is already substantial free offline progression. [API defaults](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/appsettings.json:59), [idle planner](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Idle/IdleCombatPlanner.cs:26), [batch settlement](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/CharacterActions/CombatService.cs:38).

Successful combat feeds multiple progression tracks together. Ordinary equipment awards and Sigil drops are explicitly processed from victorious encounters. Character XP also feeds attuned Essence XP; Combat Style XP uses eligible **base** combat XP, so it should not be assumed to inherit every character-XP modifier. Character levels increase Power and maximum health. The inspected level-up loop has no explicit endgame level cap; do not describe the game as a finite leveling race without another verified cap. [Equipment acquisition](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Idle/CombatAcquisitionRewardProcessor.cs:23), [reward calculation](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Idle/IdleCombatRewardCalculator.cs:45), [character and Essence XP](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/CharacterExperienceRewardWriter.cs:87), [Style XP](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Idle/IdleCombatOrchestrator.cs:108), [leveling](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Levels/LevelingService.cs:24).

Elapsed game time therefore centers on automated combat; active attention is likely to center on build decisions, reward management, targets, dungeons, scheduled participation and social interaction. The second statement is a design inference, not a measured distribution of playtime.

Crafting and gathering are removed from the current action flow. The action enum and API expose combat rather than those old professions. Equipment improvement remains, but that does not make old crafting/gathering product ideas applicable. [Current action types](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/CharacterActions/CharacterActionType.cs:2), [action controller](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Controllers/V1/CharacterActionsController.cs:24).

### Progression, capacity and identity

| System | Current implementation and economic significance | Monetization implication |
| --- | --- | --- |
| Character progression | Combat XP, rising attributes, level/quest/World Tower area gates. | Selling XP changes access and power, not just a number on the profile. |
| Essences | Collection, limited attunement, XP, ascension, loadouts and collection bonuses. One starting attunement slot, another at each ten-level milestone, at most ten. | Never sell equipped slots, collection completion or ascension. |
| Soul Archive | Persistent owned Essence collection, with one absorbed instance per definition; no inspected storage cap. It is separate from saved build limits. | Do not add a storage paywall. |
| Creature Archive / Codex | Discovery, collection objectives and focus targeting; collections can grant lasting acquisition bonuses. | Collection purchases would have effects beyond the purchased Essence. |
| Soulstones / permanent upgrades | Separate upgrade-branch interface, under the legacy `soulstone-archive` route, for permanent character bonuses bought with gameplay Soulstones. | The similar route name must not obscure that Soulstones buy acceleration. |
| Equipment | Combat acquisition, variants/sets, upgrading and item management; three saved loadouts. | Equipment sales or upgrades would bypass a central loop. |
| Combat Styles | Four available styles, independently mastering from 0 to 10; global selection with remembered choices. Refinement at 3, upgrade slots at 5/8, opening technique at 7 and Upgrade Mastery at 9. | Build identity is valuable, but the mechanics and switching belong in the free game. |
| Inventory | Items, stack handling and equipment organization; no inspected general bag-slot cap. | Selling bags would be manufactured scarcity. |
| Saved builds | Three Essence and three equipment presets, automatic use by combat activity. | Existing automation stays free; extra memory is a possible later product, not a launch necessity. |
| Account | Account-level achievements/title ownership coexist with mainly character-owned gameplay progression. The app selects one current character; no player-facing roster/character-slot product was found. | Paid entitlements should be account-owned; do not promise transferable account-wide gameplay upgrades or sell character slots. |
| Public identity | Names, equipped earned titles, public profiles, shared character tags, guilds and leaderboards. | Strongest existing base for modest badges and heraldry. |

The most important direct references are the [Essence limit service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Essences/EssenceLimitServices.cs:6), [equipment preset limit](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Loadouts/EquipmentLoadout.cs:8), [Combat Style selection](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/CombatStyles/CombatStyleService.cs:120), [Style milestones](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/CombatStyles/CombatStyleProgression.cs:7), and [area access checks](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Quests/CombatAreaAccessService.cs:93).

Creature Focus is already a free strategic tool: **3× base Essence drop chance** for the focused creature and **1.2× spawn weighting**, with an **eight-hour change cooldown**. Weighting is not a guaranteed 20-percentage-point increase in encounter probability. Other creatures are not excluded from Essence rolls. Paid focus slots or cooldown removal would accelerate acquisition. [Focus rules](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Essences/CreatureFocusRules.cs:3), [focus cooldown](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Essences/CreatureArchiveService.cs:18), [spawn application](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Idle/IdleCombatOrchestrator.cs:77).

### Currencies, faucets and sinks

All six balances on `Character` below are gameplay currencies, not existing premium wallets. Arena Glory is held separately. An existing bonus-provider architecture does not establish an existing paid subscription. Searches of the current domain, services and API controllers found no payment checkout, premium-wallet or paid-entitlement implementation. [Character balances](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Entities/Characters/Character.cs:24), [bonus kinds](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Bonuses/BonusKind.cs:3).

| Currency/resource | Faucets and role | Sinks / limits / concern |
| --- | --- | --- |
| Cinders | Combat and rewards; player trade redistributes them. | Equipment improvement and marketplace purchases; marketplace seller fee removes currency. Direct player transfers make paid Cinders especially dangerous. Guild buildings spend Guild Supplies instead. |
| Soulstones | Combat and selected activity/reward channels, including competitive rewards. | Permanent constellation upgrades affecting drops, pity, XP and reward retention. A full-reset refund is a reallocation, not a net currency sink. |
| Fate Echo | Prophecy reward economy. | Additional prophecy rerolls; buying it would buy objective selection and reward efficiency. |
| Guild Favor | Guild participation/reward economy. | Guild shop purchases of Soulstones and Sigil Fragments, with weekly limits and building requirements. |
| Guild Supplies / Guild XP | Daily orders and weekly guild missions. | Supplies fund buildings/upgrades; XP raises guild level. These are collective progression resources, not Cinder purchases. |
| Tower Tokens | Tower first clears and weekly Echo rewards. | No implemented spending path found. This is an economy-design gap, not a premium-store opportunity. |
| Raid Trophies | Implemented reward currency, but raid rewards default off. | Current trophy vendor has no items. Do not sell a currency with no established live use. |
| Arena Glory | Arena outcomes, first daily win and tournament rewards. | Champion Market offers, including progression resources and titles, with limits. |
| Essence Dust / monster cores | Shattering Essence items and specified reward paths. | One dust buys one Essence level within its current cap; generic Lesser/Greater/Primal cores pay ascension. These are not creature-specific cores. |
| Sigils / fragments | Combat and activity rewards. | Dungeon access; more supply means more opportunities for XP and equipment. |
| Equipment / salvage resources | Combat drops, dungeon rewards and dismantling. | Reinforcement and blueprint variant application. Tradability/binding affects who else receives any paid advantage. Older tempering/crafting terminology does not describe the current commands. |
| Tickets / weekly eligibility / event slots | Regeneration and scheduled participation. | Opportunity constraints rather than premium balances; paid resets would alter reward throughput and competition. |

The marketplace defaults are **ten listings**, **ten buy orders**, **seven-day lifetimes**, and a **3% seller fee**, with a minimum fee of one Cinder. Buy orders reserve Cinders; cancellation returns escrow rather than generating new wealth. Item binding is enforced. Extra listing slots, fee reductions or paid trade automation would be economic advantages, even without stat bonuses. [Market options](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/MarketPlaces/MarketPlaceOptions.cs:7), [market transactions](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/MarketPlaces/MarketPlaceService.cs:65), [Cinder transfers](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Entities/Characters/CurrencyTransferRepository.cs:12).

**Scarcity must be split into structural and observed scarcity.** Structurally scarce resources include specific Essence drops, ascension materials, tickets, weekly rewards, unlocked attunement slots and scheduled event participation. Repeated combat makes XP, Cinders and ordinary loot renewable; that does not establish that players have too much of them. Storage and ordinary build switching are not currently scarce goods. Before pricing anything, measure balances, income/spend rates, median time to a target build, and the upper tail of unsuccessful acquisition.

### Competition, recurrence and reachability

Arena regenerates tickets and offers a first-daily-win reward. Tournaments add scheduled competition; Prophecies add daily/weekly objectives and a longer revelation goal; guilds add donations, buildings and missions; Tower adds shared first clears, weekly activity and permanent records. These provide recurring play without a paid pass.

Arena uses live attacker preparation and valid defender snapshots, with a live fallback. Tournament combat prepares stored character snapshots. The shared preparation path applies real equipment, Essence modifiers and Styles; no equal-stat normalization was found in the inspected callers. Snapshotting freezes a build; it does not equalize it. [Arena service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Colosseum/ColosseumService.cs:72), [tournament preparation](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/TournamentGroundsService.cs:2409), [shared preparation](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Resolution/CombatPreparationPipeline.cs:27), [build application](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/CombatSetupService.cs:129).

Feature visibility requires care. The frontend disables raids when the runtime environment is `prod`; focused beta progression is enabled by default. Onboarding and journey gates can delay social/economy/world screens. A route or class alone is not proof that a new player sees a complete system. Raid rewards default false and the trophy vendor is empty. [Frontend feature settings](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/environments/environment.ts:24), [raid options](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Raids/RaidOptions.cs:3), [empty trophy vendor](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/raids/trophy-vendor.json:1).

Where raids are enabled, Battle Plan already offers free simulation with ten samples per preview and a 30-preview/hour limit. The Mad King region-boss content schedules events every four to eight hours with ten-minute signup and server Tower-floor-ten access; rewards are disabled and brackets empty in that content. These establish implemented planning and time-gated participation, not active monetizable reward faucets. [Raid planning](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Raids/RaidService.cs:53), [region-boss configuration](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/region-bosses/region-bosses.json:1).

### Immediate evidence gaps

There is no basis here for claims about live inflation, actual population, payment conversion or revenue. Production overrides, seeded database state, retention cohorts, player interviews and live UI usability were not inspected. This review did not run the game or access a shared database. It also did not establish that every authored Essence or boss is obtainable in production. These limits matter most when deciding whether to monetize a specific collection or recurring activity.

## 3. Player Spending Motivations

| Player motivation | Specific LegendsLegacy expression | Legitimate value | Friction to reject |
| --- | --- | --- | --- |
| Support a persistent world | A committed player wants a solo-developed game to survive. | A clearly priced optional membership or permanent support pack. | Implying support purchases are necessary to progress or obtain developer attention. |
| Be recognizable | Names and titles recur in chat, guilds, profiles and rankings. | A small badge, nameplate ornament and profile treatment. | Paid chat prominence, louder messages or ranking placement. |
| Express build identity | An Essence collection and Style choice describe how the player fights. | Earned collection showcases with optional surrounding decoration. | Selling missing Essences, special Style mechanics or collection bonuses. |
| Remember experiments | Three preset slots must serve several combat activities. | Potentially more saved configurations once the free allowance covers ordinary use. | Keeping too few slots so normal participation demands a purchase. |
| Celebrate shared history | Guild membership and Tower participation create stories. | A guild banner treatment or frame around genuine earned records. | Purchasing first-clear credit, achievement completion or guild power. |
| Reduce repeated administrative work | Organizing long-lived builds can become repetitive. | Optional extra saved organizational views, if research shows value. | Charging for search, filters, comparison, accessibility or reliable offline settlement. |
| Own an attractive collection | A finite set of themed profile ornaments can appeal to collectors. | Exact preview, permanent ownership, no random outcomes. | Rotating scarcity, duplicate cosmetic drops or collectible spending tiers. |

The strongest unproven demand hypothesis is **recognition among people who already know the character**. The weakest is that anonymous players will buy expensive art simply because the game has a shop. Establish where profiles and character tags are actually seen before investing in a large catalog.

## 4. Recommended Monetization Principles

1. **The complete core game is free.** Payment does not unlock regions, endgame, guild participation, builds or the ability to compete.
2. **Identical play yields identical gameplay rewards regardless of payment.** The default strategy has no paid modifiers to XP, drops, attempts, cooldowns or reward eligibility.
3. **Preserve existing free conveniences.** Never turn current preset auto-use, filtering, comparison, readability settings, battle skipping or offline settlement into paid benefits.
4. **Sell presentation and, at most, bounded memory.** A saved configuration may recall already-owned choices; it must not store extra Essences, expand equipped slots, optimize automatically or bypass combat locks.
5. **Audit indirect power.** Trace every purchase through currencies, trading, collections, achievements, guilds and PvP. A cosmetic title entering an earned-title counter is not safely cosmetic.
6. **Protect earned prestige.** Paid badges have a distinct vocabulary and presentation from achievements, renown, first clears and competitive titles.
7. **No spend-dependent cap.** High spending cannot raise a combat ceiling, progression rate or competitive opportunity count.
8. **Account ownership, no paid-asset trading.** Entitlements follow the authenticated account. They cannot become equipment, Cinders, gifts for resale or marketplace listings.
9. **Use real prices and exact contents.** No premium wallet at launch, mystery rewards, awkward top-ups, countdown sales or disguised subscriptions.
10. **Expiration never damages a character.** Subscription expiry affects rented presentation only. It does not erase builds, items, history or earned rewards.
11. **A healthy free experience comes first.** If ordinary play hits an inconvenient limit, improve the baseline before testing a paid extension.
12. **Every product needs an operating owner: the solo developer.** Count refunds, reconciliation, art, support and balancing, not merely the size of the feature code.

A feature passes review only if a free player can reach the same gameplay outcome without a recurring payment requirement; a nonpayer with the same play history and choices has the same combat resources; and the product remains appealing without making free play worse. Merely being technically obtainable for free after an excessive grind is insufficient.

PvE can tolerate more convenience than PvP in principle. In this game, permanent PvE advancement feeds ranked and shared systems, so the practical safe boundary is narrower. A genuinely isolated, unranked, nonrewarding practice environment could support harmless presentation experiments; building a separate progression universe just to justify boosts is not worthwhile for a solo developer.

## 5. System-by-System Monetization Analysis

### Mechanics that determine the boundaries

**Essences are a progression network, not just collectible skills.** The inspected catalogs contain 80 Essence definitions and 77 creature loot tables. Every authored ordinary base chance is 0.0001 per eligible creature roll before modifiers. Ordinary resonance adds one point per failure and reaches only a 1% *relative* chance increase at 12,000 points; it is not a guaranteed-drop counter. Featured dungeon bosses use separate 10× chance, 1,000× resonance gain and 10× resonance-cap modifiers. That distinction matters before predicting grind or pricing any acquisition offer. [Loot tables](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/world/creature-essence-loot-tables.json:6), [resonance constants](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Essences/CreatureResonanceConstants.cs:5), [dungeon Essence modifiers](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Rewards/Dungeon/DungeonCombatRewardCalculator.cs:21).

Essence levels cap at **10/30/60/100** by ascension stage. Ascension at levels 10/30/60 costs **6 Lesser / 12 Greater / 24 Primal cores**, with existing collection-based discounts reducing the first two costs to 3/8 after the qualifying ten-Essence milestones. Dust buys levels, one level per unit. The combat scaling path uses ascension tier; do not assume every Essence level directly adds a universal stat bonus. Ascension strengthens configured damage/healing/barrier/attribute/summon effects and some cooldowns. [Progression constants](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Essences/EssenceProgressionConstants.cs:5), [dust and ascension](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Essences/EssenceSystemService.cs:199), [combat scaling](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Essences/EssenceAbilityProgressionScaler.cs:8).

The **19 Codex collections** contain three to six specified Essences. Completion bonuses apply even when those Essences are not equipped, and scale with the lowest ascension tier among the collection's members. Purchases that complete collections can therefore accelerate future collection. Evolution is a separate caveat: although an endpoint remains, all 80 inspected evolution definitions have empty catalyst IDs and no added tags or modifiers. Do not market evolution as an established current loop. [Collection definitions](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/essences/essence-codex-collections.json:4), [collection calculation](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Essences/EssenceCodexCollectionService.cs:96), [evolution content](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/essences/essences.json:13).

**Equipment improvements are deterministic progression.** Reinforcement reaches rank five, with a default 4% budget increase per rank. Tier-one costs are 5/10/20/40/80 parts and 11,150/22,300/44,600/89,200/178,400 Cinders; tier two doubles them. Reinforcement binds gear. Dismantling recovers base parts plus half the cumulative rank-part investment, rounded down, without refunding Cinders. Blueprints change stat variants and cost one matching blueprint plus 100 Cinders per equipment tier to apply. Dungeon blueprint drops have a 25% roll and a guarantee by the fourth qualifying completion since the last drop. These are gameplay systems, not transmog. [Upgrade costs](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-upgrades.v1.json:3), [reinforcement state](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentState.cs:154), [blueprints](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-blueprints.v1.json:2).

**Dungeons are Sigil-gated delves, not daily energy bars.** Four authored families materialize twelve difficulty definitions. An entry consumes one family Sigil; assembly costs ten fragments. Access can require prior difficulty completion and server Tower progress. Vigor begins at 100, is spent within the run, and can be restored at rest sites. At zero, the run fails and loses pending loot; a suspended delve expires after 48 hours. Paid Vigor or loot insurance would change risk-taking and rewards. Family mastery caps at ten, is shared across difficulties, and can add up to 50 percentage points to equipment drop chance alongside other benefits. [Sigil assembly](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/dungeons/sigil-assembly.json:1), [run lifecycle](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonRunService.cs:144), [Vigor](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonVigorService.cs:11), [mastery benefits](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Dungeons/Mastery/DungeonMasteryBenefits.cs:17).

**Arena tickets are reward opportunities even on a loss.** Capacity is five, restoring one every three hours; this is not five attempts per day. Wins/draws/losses award 12/8/5 Glory, with 20 extra for the first daily victory. Glory purchases Soulstones, fragments and ascension cores as well as titles. For example, six Lesser cores cost 160 Glory, twice weekly; four Greater cores cost 260, once weekly with Silver rank. Buying tickets buys both ranking opportunities and power resources. [Ticket restoration](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Colosseum/ColosseumService.cs:689), [Arena rewards](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Colosseum/ArenaRewards.cs:7), [Champion Market](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/market/champion-market.json:1).

Tournament registration is configured from Monday 00:00 to Saturday 00:00 UTC, with battles beginning Saturday noon. This is weekly 3v3 single elimination: the configured 4–32 limits apply to teams, with up to 96 character registrations. Rewards include Glory, Soulstones and fragments; season points and Hall of Fame are implemented. This is already an earned competitive economy, not a reason to sell tournament entry priority. [Tournament configuration](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/appsettings.json:78), [team limits](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/TournamentGroundsService.cs:1641), [registration capacity](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/TournamentGroundsService.cs:3289).

**Tower competition extends beyond PvP.** Authored release reaches floor 15, with 5/10/15-person expeditions organized into parties of five. First server clears create permanent records and unlocks, and award four times normal tokens. Echo rewards are once per character per UTC ISO week **across all floors**, not once per floor; further Echo play is possible without another weekly award. One account may occupy one rally slot and a character may have one active expedition. No ticket cost was found in rally entry. API configuration allows three manual scouting actions and ten preparation actions per character/week, with preparation worth 0.25% per point up to 10%. The class's lower preparation default is overridden by API configuration. [Tower content](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/world-tower/tower-floors.json:1), [first-clear and Echo rewards](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs:1777), [Tower settings](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/appsettings.json:2).

**Guild progression redistributes benefits socially.** The initial effective membership limit is eleven: ten base plus the starting Guild Hall contribution, rising to twenty at its cap. Daily personal orders, a selected weekly mission, buildings, supplies, permissions and an equipment-lending vault are implemented. Three daily orders can become four through Mission Board progression. Favor buys limited weekly Soulstone and fragment packages; all four current shop rows are nonrotating. Guild raids/wars and several building benefits remain future content despite some foundation flags. Unbound donated equipment becomes guild property and can be borrowed; no vault capacity paywall was found. [Guild rules](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Extensions/Guilds/GuildExtensions.cs:9), [guild content and shop](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/guilds/guild-content.json:1), [mission rewards](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Guilds/GuildMissionService.cs:718), [vault rules](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Guilds/GuildVaultService.cs:23).

**Prophecies already offer recurring goals with some slack.** Players accept one of three daily offers; a Greater Prophecy is accepted weekly. Daily rerolls affect all offers before acceptance: one free, then 40 and 80 earned Fate Echo, at most three total. The configuration's `paidRerollsEnabled` refers to this gameplay-currency spending. Daily completion grants one Prophetic Favor and weekly completion two; the seven-Favor weekly milestone can be reached with five dailies plus the weekly objective. Rewards include XP, Soulstones, fragments, Fate Echo and caches/materials. [Prophecy service](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Prophecies/ProphecyService.cs:122), [reroll economy](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/prophecies/economy.json:1), [weekly milestones](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/prophecies/weekly-revelation.json:1).

### Product decisions by system

“Potential” means plausible voluntary spending value, not permission to monetize the core mechanic. None of these ratings is a revenue forecast.

| System | Potential | Reasonable purchase and motivation | Must remain free | What would damage it / become P2W | Worth doing? |
| --- | --- | --- | --- | --- | --- |
| Essences | Medium | Decoration around an earned collection; express a chosen identity. | Acquisition, attunement, XP, ascension, all abilities. | Paid drops, cores, dust, level caps or equipped slots; collection feedback amplifies power. | Presentation later; no direct progression sales. |
| Soul Archive | Medium | Decorative showcase frame for collected favorites. | Unlimited current collection storage, search, favorites and collection access. | Inventing an archive limit; charging to retain owned Essences. | Free showcase first, ornament later. |
| Creature Focus / Codex | Low | A visual collection journal treatment. | Target information, focus, ordinary cooldown rules, all bonuses. | Multiple paid focuses, shorter cooldowns, bought collections or pity. | Do not monetize directly. |
| Equipment | Low | Only a separate future visual representation, if visible enough. | Drops, reinforcement, blueprint variants, sets, dismantling. | Better gear, quality guarantees, blueprints, salvage bonuses or parts. | No current direct product. |
| Combat / offline | None | No suitable core purchase. | Full normal cadence, 24-hour retention, readable results and existing skip tools. | Faster combat, more retained reward time, paid recovery or stronger simulation tools. | Do not monetize directly. |
| Dungeons / mastery | Low | Optional decoration around earned completion records. | Sigil acquisition, Vigor rules, mastery, fair loot, normal attempts. | Sigils, revives, loot insurance, paid routes, mastery or fragments. | No direct product. |
| Regions / quests | None | No suitable current core purchase. | Area access through gameplay, quests and shared unlocks. | Region paywalls or quest completion; player-base fragmentation. | Do not monetize directly. |
| Combat Styles / former Doctrines | Medium | Cosmetic emblem showing the selected style. | All four styles, mastery, choices, preview, switching. | Paid refinements, mastered upgrades, Style XP or exclusive mechanics. | Use identity art only if it also has a free display purpose. |
| Guilds | Medium | Curated crest/banner treatment; shared identity. | Membership progression, buildings, Favor, chat, vault and permissions. | Capacity, supplies, Favor, better loan gear or contribution multipliers. | Later, after ownership/refund rules. |
| Arena / Champion Market | Low | Small supporter badge next to an unchanged earned rank. | Tickets, opponents, snapshots, earned market and matchmaking. | Tickets, Glory, rank boosts, fee access, paid counter-selection. | Shared identity only. |
| Tournaments | Low | Profile/tournament-history ornament. | Registration, season participation, rewards and fair preparation. | Entry priority, seeded placement, retries or exclusive analytics. | No direct competition product. |
| Prophecies | Low | Optional presentation theme, with uncertain demand. | Offers, acceptance, rerolls, milestone rewards and information. | Extra accepted objectives, paid Fate Echo, rerolls or doubled rewards. | Do not monetize directly. |
| Achievements | Medium | Decorative frame around genuine completed achievements. | Achievement earning, display and renown. | Buying completion or paid-title contributions to earned counts. | Useful after free showcases. |
| Titles | Medium | Separate supporter badges, rather than paid earned-title unlocks. | Prestigious titles and prefix/suffix controls. | Impersonating earned titles or triggering title-count achievements. | Badge separation is a launch prerequisite. |
| World Tower | Low | Visual expedition/profile treatment reflecting actual participation. | Rallies, preparation, scouting, Echo rewards and server unlocks. | Preparation purchases, first-clear priority, token/reward resets. | No direct progression product. |
| Inventory / guild vault | None | No capacity purchase justified by current code. | Existing storage, sorting, comparing, transferring and safe management. | Creating slot pressure, paid sorting, additional power-bearing vault privileges. | Improve usability universally. |
| Build management | Medium | Bounded extra saved configurations for experimenters. | Adequate presets, editing, auto-use, preview and identical combat locks. | Selling required activity coverage or automated best-build selection. | Later; current three-per-system baseline needs evaluation. |
| Account / profile | High | Badges, nameplate ornaments, compact profile themes; identity and support. | Useful public profile, account security/recovery, first rename. | Paid verification authority, account power or exclusive support access. | Best initial surface, after small identity foundation. |
| Social / chat | Medium | Restrained, server-verified identity adornments. | Channels, whispers, moderation, blocking and guild coordination. | Boosted messages, purchased authority, attention spam or paid safety. | Reuse the same identity entitlement. |
| Marketplace / transfers | None | No launch product. | Trading, normal listing limits, records and equal rules. | Paid Cinders, fee discounts, priority listings or listing-cap advantages. | Do not monetize directly. |
| Soulstones / constellation upgrades | None | No direct currency product. | All seven progression branches and refunding reset. | Bought Soulstones or rank unlocks; direct acquisition/XP amplification. | Protect as earned progression. |
| Other gameplay currencies | None | No direct currency product. | Gameplay acquisition and appropriate sinks. | Bought Favor, Fate Echo, Tokens, Glory or Trophies and conversion chains. | Resolve incomplete sinks before expansion. |
| Standalone raids / region bosses | Low | Eventually group identity on existing profile surfaces. | Access, planning tools, reward rules and grouping. | Paid readiness, simulation advantages, reward claims or event priority. | Defer; readiness and reward configuration are incomplete. |
| Character slots / alternate characters | None | Not an established current feature. | Current account's full character experience. | More reward-generating characters or account-vs-character entitlement loopholes. | Do not build a roster merely to sell slots. |
| Evolution / guild wars / unfinished features | None | No product until there is useful complete gameplay. | Any eventual core mechanics. | Selling advance access or benefits against a speculative roadmap. | Do not monetize promises. |

## 6. Subscription Analysis

**Recommend an optional “Nobility” at €4.99 / $4.99 per month**, once public recognition and payment management work. It is an explicit supporter membership, not a premium gameplay tier. Its viability depends on players wanting to support the game; no assertion of subscription demand can be made from code.

Exact launch benefits:

- An opt-in **Nobility supporter badge** beside the character name on the public profile and supported chat/guild/leaderboard character-tag surfaces. Always smaller and visually distinct from earned rank and titles.
- A fixed **Nobility presentation set**: one compact profile header treatment and two selectable nameplate ornaments. These are available while subscribed; they do not affect name color meaning, rarity colors or visibility priority.
- A permanent **“Supported LegendsLegacy” history badge** after the first successfully settled paid month. It records support, not its amount, duration or a renown tier. Revoke that entitlement if its only qualifying purchase is refunded.

These are new integration requirements, not existing badge capabilities. Before launch, verify display on the public profile, general/whisper chat, chosen guild roster entries and the Tavern leaderboard entries that use character tags. Some current callers lack the necessary identity metadata. Show the permanent history badge principally on the profile and at most one optional supporter mark beside a social name; do not accumulate a row of badges.

Do not promise monthly art, login gifts, premium currency, exclusive gameplay, priority moderation, balance influence, special developer access or early competitive access. Published development updates should remain available to everyone. Membership benefits are continuously available during the paid period, with no daily claim.

Start monthly only. Do not offer weekly billing, auto-selected annual commitments or lifetime memberships. After retention and billing reliability are established, an optional **€49.99 / $49.99 annual** plan is reasonable: the same benefits, explicit annual billing and modest transparent savings. No added power for longer commitments. Avoid prepaid stacking at launch; it complicates refunds and turns a support product into a high-spend target.

Cancellation stops the next renewal and preserves access through the paid-through date. Expiry removes the active badge and rented decoration from display, falls back to the free visual style, and retains the stored cosmetic preference for a later return. Permanent purchases, the valid history badge, earned titles, items, builds and play history remain intact. A failed-payment grace period may preserve cosmetic display briefly while billing is retried; it never affects gameplay. Explain this on the purchase page and account billing page.

**Reject subscriber Essence “backup slots” as the headline benefit.** There are three different possible meanings: more equipped slots are P2W; more Archive storage introduces an artificial restriction; more saved configurations are legitimate but create practical build flexibility. The last option is better as a small permanent unlock after the free baseline is sufficient. Rental pressure is unnecessary.

Prevent mandatory membership by leaving all gameplay and current convenience intact, omitting expiring gameplay rewards, keeping earned recognition prominent, and providing attractive free profile choices. If players will not subscribe for the stated support-and-identity offer, do not rescue conversion by moving essential features behind it. Launch or retain the one-time supporter pack instead.

## 7. Premium Currency Analysis

**Do not introduce premium currency.** Three initial products do not need a wallet, exchange rate, bonus bundles, purchase ledger balances, earned/purchased balance ordering and refund-after-spending rules. Direct prices make value and spending limits easier to understand.

Existing Soulstones, Cinders, Glory, Fate Echo, Guild Favor, Tower Tokens and Raid Trophies must not quietly become premium money. Their gameplay connections are too extensive. In particular, “subscribers receive a few Soulstones” is a progression benefit even if it looks like a harmless stipend.

If a much larger future cosmetic catalog demonstrates that a wallet solves a real problem, the conditions would be:

| Question | Required answer before adoption |
| --- | --- |
| Uses | Cosmetic entitlements only, with exact previews. |
| Forbidden uses | Every gameplay currency/resource, drops, attempts, access, XP, time reduction and competitive capability. |
| Purchasable? | Only if direct purchase remains available and the exact required balance can be bought; no forced overbuying. |
| Earnable? | Small transparent event grants could be acceptable without daily grind requirements. They must not replace current gameplay rewards. |
| Ownership | Account-wide, nontransferable, no marketplace redemption or gifting initially. |
| Internal refunds | Reverse the cosmetic entitlement and credit the exact original currency amount once, with an immutable audit trail. Real-money refunds remain a separate payment process. |
| Balances | Track purchased and promotional value separately; define consumption and reversal rules before launch. Prevent double credit across wallet refund and payment refund. |
| Bundles | No bulk discount that makes real prices ambiguous; no awkward remainders; any residual balance has a practical use or a supported resolution. |

This is a contingency assessment, not a recommendation to build a wallet. EU consumer authorities' principles emphasize transparent real costs, avoiding forced currency purchases and respecting withdrawal rights and consumer vulnerabilities. Direct purchase is the simpler fit here. [European Commission explanation of the CPC principles](https://commission.europa.eu/news-and-media/news/european-commission-hosts-stakeholders-talks-application-cpc-networks-key-principles-games-virtual-2025-06-03_en).

## 8. Cosmetics Analysis

The current game displays text identity far more consistently than a visual player body. Public profiles, chat character tags and guild/leaderboard entries are the promising surfaces. Combat is largely names, health/barrier indicators, ability information and results. No implemented player cosmetic wardrobe, portrait selector or animated character appearance system was found. [Public profile](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/character/character-overview/character-overview.component.html:168), [shared character tag](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/character/character-tag/character-tag.component.ts:15), [combat presentation](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/combat/combat.component.html:156).

| Rank | Cosmetic | Likely player value | Effort / ongoing burden | Decision |
| ---: | --- | --- | --- | --- |
| 1 | Supporter badge and restrained nameplate ornament | Recognition in repeated social contact. | Low–medium art; medium integration across API, profile and chat. | Launch foundation. |
| 2 | Compact profile header/background/border set | Ownership of a character's public page. | Medium initial surface work; low per static set. | Launch one carefully previewed collection. |
| 3 | Decoration around an earned Essence/achievement showcase | Lets committed collectors display choices and history. | Medium; must build useful free showcase first. | Strong later hypothesis. |
| 4 | Curated guild crest/banner | Shared identity and group pride. | Medium–high; ownership, permissions and dissolution support. | Later, based on active guild demand. |
| 5 | Small Style/Essence presentation emblem | Reinforces an existing build identity. | Low–medium if it reuses shared presentation; low if it is only a token icon. | Include in a coherent pack, not many micro-SKUs. |
| 6 | General decorative UI theme | Mainly personal enjoyment; little public exposure. | Medium and permanent contrast/layout QA cost. | Defer; accessibility and layout remain free. |
| 7 | Portrait/avatar packs | Unproven without frequent portrait exposure. | New free portrait surface plus ongoing art. | Do not build solely to monetize. |
| 8 | Combat or ability effects | Limited fit with present stat/log UI. | High rendering, performance and readability burden. | Reject for launch. |
| 9 | Full-body skins / equipment transmog | Little current display value. | High new rendering/content scope; blueprints are not a substitute. | Reject for the present game. |

Paid cosmetics must not recolor rarity, disguise a moderator, imitate an earned competitive title, enlarge leaderboard rows or make battle information harder to read. Respect reduced-motion preferences. Let players hide decorative effects locally. Use curated assets; custom image uploads and arbitrary paid title text bring disproportionate moderation/support work.

The title trap deserves an explicit launch test: buying any store item must leave title-count achievements, achievement points, renown, collection counts and rankings unchanged. Current title unlocks invoke dependent achievements, including the account-scoped ten-title achievement worth 50 points. [Title unlock path](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Achievements/AchievementService.cs:164), [title-count achievement](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/achievements/additional.json:662).

## 9. Convenience and Account Upgrades

### Good convenience, subject to a healthy baseline

The strongest candidate is a **permanent Extended Build Library**, provisionally **€4.99 / $4.99 once**, expanding from a proposed **seven free Essence and seven free equipment configurations to twelve of each**. These numbers are a design proposal, not the current three-per-system limit. Seven is a starting baseline because there are seven automatic combat contexts; ordinary users must not have to buy their way out of mismatched presets. Boss-specific experimentation may show that even seven is inadequate, in which case increase the baseline or abandon the sale.

Only saved references increase. The player still owns the same Essences and equipment, has the same equipped slots, can assign only the same activity choices, and follows the same mutation/snapshot locks. No extra simultaneous builds, automated best-counter choice, optimizer, reward simulation advantage or extra item ownership. Purchase once per account, never stack it. If unified builds are added, migrate the entitlement to equivalent total capacity rather than charging separately for each subsystem.

This is **generally acceptable convenience with a residual preparation advantage**, not literally zero advantage. Require free editing, replacement, deletion and a clear active/fallback selection before selling it. Lightweight validated export/import of preset references is a possible new free improvement, not an existing capability or a reason to build a large tooling system. Unassigned Essence activities currently fall back through a sorted preset list; that should be understandable and stable before commercial capacity changes. [Existing preset validation](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Essences/EssenceSystemService.cs:302), [selection fallback](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Essences/EssenceLoadoutSelection.cs:5).

A later additional rename service could cost **€4.99 / $4.99**, retaining the existing first free rename. Prefer a free long-cooldown rename if demand is mainly correcting regret. A paid service needs immutable IDs, name availability checks, a visible moderation history and a cooldown; it must not help evade blocks or reports. Do not launch it before there is actual demand. [Current one-free-rename rule](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Users/Commands/RenameCharacter/RenameCharacterCommand.cs:25).

### Questionable convenience

Longer cloud combat-history retention might appeal to enthusiasts, but the existing idle log retains 100 records in client memory; paid cloud history would be new persistence and operations work. Basic recent-result inspection and export should be free. Do not sell exclusive opponent intelligence, longer private competitive scouting or a stronger battle simulator. Defer until measured use justifies storage and support costs. [Current log retention](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/client-side/combat/combat-log/combat-log.service.ts:17).

Longer offline retention, larger ticket banks, queued dungeon farming and automatic optimal target changes are throughput improvements. They are not harmless merely because they reduce clicks. Marketplace capacity and lower fees likewise create economic advantage. Reject them under the recommended strategy.

### Selling relief from bad UX

Never charge for existing filters, favorites, comparison, basic sorting, notification controls, readable battle outcomes, font/text-size options, account security, recovery, chat safety, dungeon skipping or ordinary build switching. Settings already provides readability and layout controls; these are part of product quality. [Settings](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/settings/settings.component.html:39), [dungeon skip control](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/region/dungeons/dungeon-page/dungeon-page.component.html:149).

## 10. Progression Monetization Analysis

**Recommend no paid progression acceleration in the current shared game.** This is based on its specific progression connections, not a rule that every convenience advantage is unacceptable.

| Candidate | New / returning players | Existing players and high spenders | PvP / public competition | Economy, pacing and fairness | Decision |
| --- | --- | --- | --- | --- | --- |
| Character XP boost | Reaches stat and attunement milestones sooner; can trivialize early pacing. | Devalues prior effort; repeatable boosts compound access advantages. | Higher attributes and Essence advancement enter snapshots and level rankings. | Boosted character awards also feed equipped Essence XP; Style base XP needs separate treatment. | Reject. |
| Essence XP, dust or cores | Reaches ascension thresholds sooner. | Lets money complete more competitive builds and Codex bonuses. | Ascension directly changes configured ability strength/cooldowns. | Dust buys levels, not negligible XP; cores are critical gates. | Reject. |
| Equipment/drop-rate bonus | More or better gear sooner. | More selection and tradable supply; whales acquire best rolls faster. | Equipment and sets transfer into all shared combat. | Changes scarcity, market supply, reinforcement demand and mastery pacing. | Reject. |
| 24→48/72-hour offline retention | Helps infrequent returners, but makes free absence feel penalized. | Paid and free players with identical login patterns retain different combat totals. | Additional earned gear/XP persists after subscription expiry. | Multiplies reward-bearing time for some patterns; not just UI convenience. | Reject as paid; consider universal retention change if evidence supports it. |
| Extra Sigils / dungeon attempts | Faster dungeon learning and progress. | More gear, cores, blueprints and mastery; repeatable spending scales. | Feeds Arena, tournaments and dungeon rankings. | Bypasses fragment/access scarcity and mastery loops. | Reject. |
| Vigor recovery / revive / loot insurance | Makes risky routes easier; purchases encouraged at emotional loss moments. | Allows different risk-taking and more successful completions. | Stronger mastery and reward acquisition. | Undermines run design and monetizes failure. | Reject. |
| Faster focus changes / multiple focus targets | Faster acquisition of desired builds. | Better adaptation to content and more collection bonuses. | Broader ready build pool; indirect power. | Changes target-farming efficiency and existing eight-hour choices. | Reject. |
| Arena tickets / higher cap / faster regeneration | More losses may still buy Glory. | High spenders gain more rating and currency opportunities. | Directly changes competition; losing still pays rewards. | Champion Market converts Glory into progression resources. | Never sell. |
| Tower preparation / Echo resets | Purchases apparent catch-up or group usefulness. | Organized spenders buy first-clear readiness and additional tokens. | First-clear prestige and server progression affected. | Social pressure can force every participant to pay. | Never sell. |
| Cinders / Soulstones / Favor / reward multipliers | Bypasses understandable earned goals. | Wealth can be concentrated through markets, transfers and guilds. | Upgrade resources reach competitive builds. | Inflation, binding/refund complexity and RMT exposure. | Reject. |
| Prophecy rerolls / extra rewards | Easier objective matching, faster rewards. | More efficient XP/material selection and completion. | Shared resources and achievement progression affected. | Turns an existing forgiving recurring system into a spending loop. | Reject. |
| Paid returning-player catch-up | Sells relief from being behind. | Can be exploited through inactivity/alternate accounts. | Arrival date and spending determine competitive readiness. | Encourages churn cycles and resents returners. | Make any evidence-based catch-up free and bounded. |

Two simple counterexamples show why labels are insufficient. At equal efficiency, a player returning every 48 hours could retain roughly twice as much combat with a 48-hour cap as with a 24-hour cap, subject to game-state and resolution rules. A flat 10% XP multiplier reduces time to a fixed XP target by about 9.1% in a simplified constant-rate model, but can have a larger knock-on effect when it unlocks harder areas, extra attunement or ascension. These are illustrations, not measured progression forecasts.

Free catch-up may still be good design: preserve existing ascension discounts and free Soulstone resets; improve weak acquisition protection if the observed failure tail is unreasonable; consider a universally longer absence allowance if normal schedules suffer. New and existing players should receive coherent rules, not a price attached to a deliberately worsening free pace.

No proposed paid product changes new-player power, incumbent investment, reward faucets, progression caps or leaderboard opportunities. The later preset product changes remembered preparation only, with a finite ceiling and free ways to make the same choices.

## 11. Seasonal / Battle Pass Analysis

**Do not launch a battle pass.** Prophecies, guild missions, weekly reward limits and tournament seasons provide recurring activity, but the repository does not establish a complete seasonal commercial loop: a stable release cadence, sustainable cosmetic production, progression pacing, purchase/claim lifecycle and sufficient engaged players.

Putting a paid track over existing Prophecies would attach paid rewards to an existing free objective loop and risk a second reward obligation around the same activities. Adding XP, Soulstones, cores or Sigils to justify its price would compromise the recommended boundaries. Adding exclusively cosmetic rewards still creates art, schedule, support and completion-pressure work.

Reconsider only after several actual seasons demonstrate participation without paid incentives and the developer can produce content without delaying the game. A possible later **Chronicle collection at €7.99 / $7.99 per season** would grant a fully previewed set of cosmetic rewards through flexible play. Purchased access should remain completable after the season, with no paid skips, gameplay rewards, daily attendance requirement or paid contribution to achievement counts. If it is merely a small art bundle, sell it as a bundle without an artificial progress track. This is a conditional alternative, not an initial product or a committed roadmap item.

## 12. Founder and Supporter Products

Recommend a **Legacy Supporter Pack at €19.99 / $19.99 once per account**. It grants a permanent “Legacy Supporter” badge, one permanent profile-header treatment, and two permanent nameplate ornaments. These designs are distinct from the membership set and the Heraldry Collection. All contents are shown before purchase. No currency, power, paid gameplay title, subscription time or account-advancement multiplier is included.

Keeping subscription time out of this pack avoids automatic-renewal misunderstandings and partial refunds across two different benefits. Keeping gameplay upgrades out avoids an irreversible advantage for early adopters. Do not sell name reservations, custom content commissions, special guild authority or promises of future features.

If there is a genuine Beta/Founder period, the purchase date may attach an optional “Beta supporter” provenance label to the badge. Close that label only when Beta actually ends, with a published policy; do not run fake countdowns. The main supporter pack remains available later with comparable visual quality. Give Beta participation recognition through gameplay too, so the historical record is not exclusively a payment receipt.

“Permanent” means an entitlement for the operating life of the service, subject to valid refund/revocation rules. Disclose planned character wipes and ensure paid cosmetic ownership survives them before accepting Beta purchases. No lifetime subscription or obligation to deliver infinite future content.

## 13. Advertising Analysis

**No advertising at launch.** The compact game dashboard needs legibility, and a solo developer benefits from fewer privacy, integration, layout, availability and support dependencies.

| Model | Revenue possibility | Fit and cost | Recommendation |
| --- | --- | --- | --- |
| Banner ads | Impression-based income requires traffic; none was supplied to estimate it. | Competes with dense game UI, may lower perceived quality, adds vendor/privacy work. | Reject. |
| Rewarded ads for XP/currency/tickets | Converts attention into a progression faucet. | Creates “optimal play includes ads,” adds rewards/fraud cases and geographic availability differences. | Reject. |
| Optional ads without power rewards | Could fund purely cosmetic recognition. | Still adds SDK/vendor and UX burden, with uncertain player value. | Not worth building without evidence. |
| Fixed sponsorship | A clearly labeled sponsor on an external development update or event page could support the project. | Requires an appropriate partner and transparent separation from ranking and gameplay. | Consider only if organically available; no in-combat placement. |

Do not sell “remove ads” after intentionally degrading the free game. Advertising revenue cannot be credibly forecast from this repository. A soundtrack/art collection could become a low-operations direct product if those assets already have independent value and clear rights; do not commission merchandise, physical fulfillment or bespoke patron rewards just to expand the store.

## 14. Pay-to-Win Audit

Use four classes. **Safe:** no meaningful progression/competitive advantage. **Generally acceptable:** bounded convenience with practical value but unchanged legal gameplay choices. **Risky:** implementation can materially change preparation, throughput or prestige. **Pay-to-win:** spending buys substantial power, reward opportunities or competitive standing, directly or through another system.

| Mechanic | Classification | Decision / guardrail |
| --- | --- | --- |
| Nobility active/history badges, profile treatment and ornaments | Safe | Separate cosmetic entitlements; no achievement counters, bonus providers or rank changes. |
| Permanent Supporter Pack | Safe | No gameplay rewards; purchase quantity one. |
| Heraldry Collection | Safe | Exact cosmetic contents, no rarity/stat remapping. |
| Optional annual Nobility | Safe | Same benefits as monthly; no extra progression. |
| Later earned-collection showcase decorations | Safe | Base showcase free; owned Essences and earned achievements displayed honestly. |
| Later curated guild banner/crest | Safe for gameplay | Ownership and officer permissions add operational risk; no guild bonuses. |
| Extra preset memory, seven→twelve per system | Generally acceptable | Finite, permanent, no extra equipped items, selection rights, automated optimization or combat-lock bypass. Reclassify Risky if free baseline is inadequate. |
| Additional rename service | Generally acceptable | Existing first rename free; cooldown and moderation identity preserved. |
| Cosmetic Chronicle / decorative UI themes | Safe for gameplay, operationally risky | Conditional only; no accessibility paywall, earned-counter contamination or completion pressure. |
| Clearly labeled external sponsorship / existing soundtrack-art sale | Safe if kept outside gameplay | Conditional only; no paid influence over rankings, rewards or design, and no bespoke production commitment. |
| Paid extra log retention | Risky | Can become exclusive opponent intelligence; defer. Free export/recent clarity first. |
| Extra preset slots tied to automatic best-counter selection | Pay-to-win in competition | Not approved; unlike storing a configuration, this sells decision-making capability. |
| Paid titles inserted into current earned-title path | Risky, potentially purchased leaderboard progress | Existing title-count achievement awards points. Use separate badges instead. |
| Paid offline retention or XP boosts | Risky to P2W, depending on magnitude | Repeatedly changes real progression and ranked inputs; reject here even at modest percentages. |
| Paid drops, equipment, quality, blueprints, Essence acquisition/ascension | Pay-to-win | Buys build strength and sometimes economic supply. Never sell. |
| Paid Arena tickets/Glory, tournament priority | Pay-to-win | Buys rating opportunities and progression rewards. Never sell. |
| Paid Sigils, Vigor, revives, dungeon mastery/insurance | Pay-to-win in the shared progression economy | Buys access, success probability and power rewards. Never sell. |
| Paid Tower preparation, scouting caps, token resets or first-clear access | Pay-to-win in cooperative competition | Changes server records and group power. Never sell. |
| Paid resource multipliers, currency, guild supplies/Favor | Pay-to-win | Upgrade and trade paths spread advantages across players. Never sell. |
| Free universal catch-up or improved free baseline | Safe as monetization policy | Still requires ordinary game-balance validation; not a product. |

Competition includes **combat level/XP, Soul Archive completion, Achievement Renown, Dungeon Mastery, dungeon clears, Arena rating, Tournament Points, weekly guild contributions, Guild Renown and raid records**, where implemented/reachable. Protecting only Arena would miss much of the game's public comparison. [Leaderboard keys](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Leaderboards/LeaderboardBoardKey.cs:5), [ranking queries](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/Repositories/Leaderboards/LeaderboardRepository.cs:173).

The practical audit follows these paths: **money → reward throughput → gear/Essence/Style/attributes → competitive snapshot**; **money → tradable assets/Cinders → other players' builds**; and **money → cosmetic title unlock → achievement count → renown/rank**. A purchase must be blocked from all three. Shared PvE first clears add a fourth path through group contribution and permanent prestige.

## 15. Free vs Paying Player Experience

| Capability | Never pays | Pays under the recommended model |
| --- | --- | --- |
| Core game and endgame | Complete access through normal gameplay requirements. | Identical requirements and content. |
| Combat power and progression ceilings | Can obtain every gameplay build, Essence, Style, gear effect and upgrade. | Identical possibilities and caps. |
| Progression rate | Normal rewards and existing earned bonuses. | Identical rewards for identical play and choices. |
| Offline combat | Current 24-hour allowance; any future universal improvement applies equally. | Identical allowance. |
| Arena / tournaments / Tower | Same tickets, limits, preparation, reward eligibility and rules. | Identical rules and opportunities. |
| Collection / inventory | Current uncapped storage behavior, full collection functions. | Identical item and Essence capacity. |
| Build tools | Existing free presets/auto-use; proposed healthy baseline before any paid expansion. | Same tools; possible later extra saved memory only. |
| Identity | Useful profile, earned titles/achievements, attractive basic decoration. | Optional additional badge and presentation designs. |
| Guilds / market / chat | Full functional participation and equal safety/moderation. | Same functions and economic rules. |
| Account support/security | Standard recovery, safety and purchase-independent treatment. | Billing support for purchases, no superior game-rule treatment. |
| Subscription expiry | Not applicable. | Cosmetic display fallback only; no loss of gameplay ownership. |

“Can compete” does not promise identical outcomes regardless of skill, choices, time played or earlier start date. It promises that money does not buy the difference. A free veteran may be stronger than a paying newcomer; a free player can also reach endgame and earn prestigious records without buying access or routinely fighting purchased advantages.

## 16. Whale / Spending Ceiling Analysis

The initial catalog has two one-time products and one monthly membership. **Total first-month catalog price is €32.97 / $32.97**: 19.99 + 7.99 + 4.99. EUR uses the tax-inclusive convention; USD amounts here are subtotals before applicable sales tax, which also applies to renewals. Later months contain only the 4.99 membership price until another finite cosmetic product is deliberately added. This is a catalog ceiling, not a prediction of average spending or an all-in USD tax quote. Do not permit duplicate pack purchases or concurrent subscriptions.

| Available monthly budget | What the player can choose | Additional gameplay advantage |
| ---: | --- | --- |
| €5 | Nobility at €4.99. | None. |
| €10 | Nobility, or the €7.99 permanent Heraldry Collection; remaining budget need not be spent. | None. |
| €25 | Supporter Pack plus one month of Nobility at €24.98, or other lower-cost choices. | None. |
| €50 | Entire launch catalog at €32.97; no product for the remaining €17.03. | None. |
| €100 | Same complete catalog; no added tier or repeatable reward. | None. |
| €500 | Same complete catalog. There is no €500 progression package, currency purchase or collectible spend ladder. | None. |

If the later €4.99 Build Library passes validation, it adds only one finite configuration entitlement. A €10 budget can buy Nobility plus that upgrade once for €9.98. A €500 spender has no more preset capacity than the person who bought the same one-time upgrade. The seven-free baseline must allow equally effective competitive builds; twelve stored configurations should save remembering, not unlock winning combinations.

Intentionally target a **low practical recurring spend of about €5**, with occasional voluntary cosmetic purchases. Do not optimize the business around extracting €50–€500 every month. No uncapped tip slider, prestigious donor rankings, bulk prepaid memberships or escalating “supporter levels” at launch. Someone wanting to support more can wait for a useful future product; the store need not consume every available budget.

The tradeoff is real: lower revenue per heavy spender means viability relies on enough satisfied players and controlled operating costs. If revenue is insufficient, improve retention and reach, reassess costs and cosmetic value, and test modest prices. Selling power is not an automatic financial solution; it can damage the very community supporting the game.

## 17. Pricing Strategy

These are **proposed nominal price points**, not currency conversions, competitor benchmarks or measured willingness-to-pay. EUR prices should be presented as tax-inclusive consumer prices where required. USD checkout should disclose applicable sales tax and the final total before payment.

| Product / option | EUR | USD | Timing |
| --- | ---: | ---: | --- |
| Nobility monthly | €4.99 | $4.99 | Initial catalog after prerequisites. |
| Legacy Supporter Pack | €19.99 | $19.99 | Initial, once/account. |
| Heraldry Collection | €7.99 | $7.99 | Initial, once/account. |
| Extended Build Library | €4.99 | $4.99 | Conditional later, once/account. |
| Nobility annual | €49.99 | $49.99 | Later, same benefits; not prepaid stacking. |
| Extra rename | €4.99 | $4.99 | Low-priority conditional service, with cooldown. |
| Curated guild heraldry | €9.99 | $9.99 | Later only if ownership/support work is justified. |
| Cosmetic Chronicle | €7.99 | $7.99 | Alternative seasonal model only after evidence; not a promised product. |

Using .99 prices is acceptable if billing is clear. Round €5/€20 prices would also fit an honest supporter identity; the difference is not a business strategy. Avoid a €9.99 subscription unless real optional value or costs justify it. Do not manufacture more benefits by including power. Sell coherent packs rather than €0.49 fragments: fixed transaction costs and support disproportionately affect tiny purchases.

For illustration only, a €4.99 tax-inclusive purchase at an assumed 25% VAT leaves **€3.992 before payment fees and operating costs**. That arithmetic is not a claim that every buyer or this business is subject to 25% VAT. Merchant country, customer location, business status and product classification need confirmation. EU OSS can simplify applicable cross-border VAT reporting, but does not make tax obligations disappear. [Your Europe: VAT One Stop Shop](https://europa.eu/youreurope/business/finance-and-tax/vat/one-stop-shop/index_en.htm).

Published provider prices checked for this analysis: Stripe's Denmark page lists **1.5% + DKK 1.80** for standard EEA cards; Paddle lists **5% + 50¢** per checkout transaction and merchant-of-record tax/billing services. These are different service scopes, not directly comparable all-in quotes; eligibility, additional products, payment mix, FX, disputes and sub-$10 pricing can change the effective cost. Prefer a hosted checkout and seriously evaluate a merchant of record to reduce solo-developer administration, but obtain provider approval and a game-specific quote before choosing. [Stripe Denmark pricing](https://stripe.com/en-dk/pricing), [Paddle pricing](https://www.paddle.com/pricing).

Use stable country-based regional prices when payment data supports them. Show the final real price; avoid individualized prices based on perceived desperation or willingness to spend. Account ownership and nontransferable cosmetics reduce regional arbitrage without punishing ordinary travel. Do not assume the current browser game owes an app-store commission; revisit platform rules if distribution changes.

Judge viability with a transparent model: net subscriptions + net one-time purchases − hosting − provider/dispute costs − art − support/admin time. Measure the inputs. No conversion percentages or precise monthly revenue forecasts are defensible from this audit.

## 18. Store Structure

Use one quiet **“Support & Appearance”** entry in account/settings navigation. Start with three product cards and a **Manage purchases** link. Do not build empty tabs for services, currencies or passes that do not exist.

The homepage should show: a short statement that purchases support development and do not change gameplay; Nobility with a visible monthly billing label; the two permanent packs with previews; and a small free-versus-paid comparison. Each item states its exact contents, ownership scope, current owned state and expiry behavior where applicable. Preview on the player's actual name/profile, with free appearance options visible too.

The purchase flow is product → preview and terms → final price/billing period → hosted checkout → pending/confirmed entitlement state. A successful browser redirect alone must never grant an item. Receipts, cancellation and refund contact belong in purchase history. Returning from checkout should not interrupt an active battle.

No loss-triggered popups, fake sales, countdown pressure, disabled-combat purchase prompts, red-dot sales badges, hidden taxes, default annual selection, prechecked extras or confusing bundles. Do not repeatedly recommend items already owned. Cosmetic pages should retain the game's compact dark-fantasy visual language, with restrained borders and readable panels.

If validated later, add **Appearance**, **Build Library** and **Account services** categories only when they contain useful products. The shop should remain smaller than the gameplay navigation.

## 19. Player Lifecycle Strategy

| Stage | What is valuable | When to surface it | Avoid |
| --- | --- | --- | --- |
| New player / guest | Understand combat, obtain a build, secure the account. | Store remains discoverable in settings; no tutorial sales step. Require a recoverable account before checkout. | Selling catch-up, starter power, first Essence access or name reservation. |
| Established player | Community identity and supporting a game they now enjoy. | A single dismissible introduction after onboarding and meaningful play; profile appearance is a natural entry. | Repeated prompts or targeting a frustrating acquisition streak. |
| Long-term player | Permanent identity, genuine collection/achievement display, optional new art. | Preview packs from profile customization; notify only about relevant new content by preference. | Endless stat growth, monthly art promises or donor ladders. |
| Returning player | Restore context, review builds and know what changed. | Free return summary and normal reward settlement first; billing state quietly visible. | Paid catch-up, expiring comeback offers or resubscribe-to-recover-builds prompts. |

Use the existing focused journey rather than introducing commercial milestones at every level. Current social/economy/broader-content thresholds are roughly level 10/20/30 after onboarding, with separate feature gates. These are context for research, not a requirement to show a sales modal at those levels. [Journey thresholds](C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/client-side/player-journey/player-journey.ts:22).

Monetization can support retention through voluntary belonging, visual identity and preserving experiments. A useful free collection showcase also gives players a reason to revisit long-term goals. Neither effect is proven revenue causality: established players are more likely both to stay and to buy. Compare similar-age/activity cohorts, interview cancellations, and examine free-player frustration rather than claiming purchases caused retention.

The largest retention hazards are an Archive subscription, paid combat resources, guild pressure to buy boosts, a grindy pass, lost configurations on expiry and cosmetics that impersonate earned prestige. Also watch support-only membership churn: it may indicate that people supported once and are satisfied, not that the free game needs another restriction.

## 20. Solo Developer Feasibility

Scores are comparative estimates: **1 = low burden, 5 = high burden**; Value uses **1 = weak/unproven, 5 = strong plausible value**. They are not precise engineering estimates. Dev includes integration complexity; FE/BE split visible and server work. Payment includes purchase/refund operations; Legal includes tax/consumer/accounting administration.

| Idea | Dev | FE | BE | Payment | Content | Support | Live ops | Fraud | Balance | Legal | Value |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Shared payment/entitlement foundation | 4 | 3 | 4 | 5 | 1 | 4 | 3 | 4 | 1 | 4 | 5 |
| One-time supporter pack after foundation | 2 | 2 | 2 | 2 | 2 | 2 | 1 | 2 | 1 | 2 | 4 |
| Nobility membership | 3 | 3 | 3 | 4 | 2 | 3 | 2 | 3 | 1 | 3 | 3 |
| Static heraldry/profile pack | 2 | 3 | 2 | 2 | 3 | 2 | 1 | 2 | 1 | 2 | 3 |
| Permanent extra preset memory | 3 | 3 | 3 | 2 | 1 | 3 | 1 | 2 | 2 | 2 | 3 |
| Additional rename service | 2 | 2 | 2 | 2 | 1 | 4 | 2 | 4 | 1 | 2 | 2 |
| Guild heraldry | 3 | 3 | 3 | 2 | 3 | 4 | 2 | 3 | 1 | 3 | 3 |
| Paid cloud history | 4 | 3 | 4 | 2 | 1 | 3 | 4 | 2 | 3 | 3 | 2 |
| Premium wallet | 4 | 3 | 5 | 5 | 1 | 4 | 3 | 5 | 2 | 5 | 1 |
| Cosmetic battle pass | 5 | 4 | 4 | 3 | 5 | 4 | 5 | 3 | 2 | 4 | 2 |
| Paid progression / currency | 4 | 3 | 4 | 4 | 2 | 5 | 5 | 5 | 5 | 4 | 2 |
| Rewarded ads | 4 | 4 | 4 | 3 | 2 | 4 | 4 | 4 | 4 | 4 | 1 |
| Character skins / combat effects | 5 | 5 | 3 | 2 | 5 | 3 | 3 | 2 | 3 | 2 | 1 |

The foundation is the expensive part even for “just a badge.” Keep one catalog and one entitlement system; reuse profile/nameplate presentation instead of independently monetizing every page. A static pack has low recurring burden; a subscription has cancellation/retry/expiry burden even without content production. A pass multiplies both operational and content obligations. Paid progression adds balancing and rollback problems to every future patch.

No need to launch five products merely because five sounds manageable. **Three coherent products are enough.** Defer guild cosmetics, history, renames and seasonal rewards until actual player use justifies them. Reject premium currency, power sales and new character rendering because their current operating burden exceeds the demonstrated value.

## 21. Abuse / Exploit Considerations

The business model deliberately avoids transferable paid assets and consumable power because these are difficult to reverse fairly after trades, guild loans, dungeon completions or ranked matches. Cosmetic revocation is much more manageable than undoing an economy.

| Risk | Consequence in LegendsLegacy | Required business rule / foundation |
| --- | --- | --- |
| Multiple accounts | Separate accounts can evade same-account participation checks and concentrate earned resources. | Cosmetic benefits only; no purchase-triggered currency, account boosts, referral rewards or multipliers. Existing same-account checks are not proof against all multi-accounting. |
| Guest purchases / account loss | Payment could attach to an unrecoverable session. | Bind purchases to a recoverable authenticated account; show the destination character/account before payment. Preserve entitlements through supported guest conversion and wipes. |
| Checkout retries / concurrent tabs | Duplicate purchases or two active memberships. | One ownership rule per permanent SKU and one active membership; stable purchase identity, database uniqueness and idempotent fulfillment. |
| Duplicate / late / out-of-order webhooks | Double grants or a canceled/refunded purchase reactivated by an older event. | Durable event receipt, validated provider state, monotonic lifecycle rules and reconciliation. Entitlement and purchase state change together. |
| Forged checkout success | Browser return manipulated to grant benefits. | Grant only from verified server-side payment state; pending is a real UI state. |
| Chargebacks | Fees and revocation after cosmetics were used. | Retain purchase evidence and revoke the affected paid entitlement when appropriate; do not erase unrelated gameplay progress or automatically treat every dispute as fraud. |
| Refund after consumption | A user has displayed a cosmetic or used rented presentation. | Offer a simple proposed 14-day goodwill refund for cosmetic packs, revoke those items, and preserve free progression. Provider rules and statutory rights take precedence. Membership refunds need a clear current-period policy and human exception path. |
| Refund after support-history grant | Permanent badge survives a refunded qualifying payment. | Derive history ownership from at least one still-valid qualifying payment. Revoke only when none remain. |
| Refund of future preset expansion | Stored builds exceed free capacity. | Preserve configurations, allow viewing and copying/replacing into free slots, and let the player choose retained auto-use assignments. Do not silently change an in-progress snapshot or delete items. |
| Gifting / stolen cards | Chargeback consequences reach innocent recipients. | No gifting at launch. Reconsider only with clear destination, delay, limits and revocation policy. |
| Premium duplication | A wallet magnifies retry/refund exploits. | No premium wallet. Do not simulate one through tradable vouchers. |
| Marketplace / RMT | Paid gear/currency could spread to others before refund. | Purchased cosmetics cannot be listed, transferred, dismantled, donated as gear or redeemed into gameplay currencies. |
| Account trading | Permanent cosmetics can add resale appeal. | Entitlements nontransferable; secure recovery and moderation records. Do not market account resale value or claim it can be eliminated entirely. |
| Guild purchases | Buyer leaves, loses rank, or guild dissolves. | Defer. A future guild-owned heraldry purchase must name the recipient guild, require authorized acceptance, remain with that guild on buyer departure, and disclose dissolution behavior. Never sell guild authority. |
| Spoofed supporter identity in chat | A paid or authority-like mark can be claimed without ownership. | Server resolves canonical identity and cosmetic entitlement. Never trust caller-supplied display text as proof of payment. |
| Configuration/price changes | Old purchases change contents or refunds no longer match. | Store original amount, currency, tax, product version and benefits. Keep promised ownership stable; explicitly migrate if a feature changes. |

The chat concern is concrete: `ChatHub.Send` currently normalizes caller-supplied `senderTitleDisplayName` and prefers it to the title claim, without entitlement verification in that path. Adding premium status to the same free-text payload would be unsafe. Existing character command transactions and outbox facilities are useful patterns, but payment operations need account-level ownership and durable payment-event deduplication; a character lock alone is insufficient. [Chat identity input](C:/repos/Legends-Legacy/legends-legacy/LL-Chat/API/API.Chat/Hubs/ChatHub.cs:100), [transaction pipeline](C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/MediatR/Behaviors/TransactionBehavior.cs:36).

Payment providers can deliver duplicate events and do not guarantee event order; checkout creation also needs retry-safe requests. Those behaviors must be exercised before taking money, not left to manual customer support. [Stripe webhook delivery guidance](https://docs.stripe.com/webhooks), [Stripe idempotent requests](https://docs.stripe.com/api/idempotent_requests).

Adopt clear consumer terms for seller identity, exact contents, recurring billing, cancellation, refunds and service lifetime. Digital-content withdrawal exceptions are conditional, including explicit agreement and acknowledgement; do not assume a checkbox eliminates all refund rights or that the same treatment automatically applies to a subscription service. Have the selected provider and appropriate adviser validate the intended checkout for the actual seller and markets. These are concrete launch requirements, not a full legal analysis. [Your Europe: digital sales and distance-selling obligations](https://europa.eu/youreurope/business/selling-in-eu/selling-goods-services/ecommerce-distance-selling/index_en.htm).

A refund should go through the payment provider to the original payment method where supported, with internal entitlement reversal recorded separately. Do not substitute gameplay currency for a cash refund, create negative gameplay balances or try to reclaim earned ranked rewards—those problems are avoided by keeping purchases cosmetic.

## 22. Conservative Strategy

**Strategy A: support first, minimal commerce.** Launch one **€9.99 / $9.99 permanent support badge pack**, with a modest profile treatment. No subscription initially, no premium currency, no paid convenience, no pass, no ads and no paid progression. All functional improvements, including any preset increase, are free.

Players buy primarily to acknowledge the developer. The store is a single clear page and can run with a small entitlement surface. Future art packs are optional, slow additions only when they have obvious value.

Strengths: clearest fairness promise, lowest product/support scope, little recurring obligation, easy refunds and limited fraud incentives. Weaknesses: no recurring revenue product, little reason for repeat purchases, and support demand alone may be insufficient. Even this strategy still needs reliable payment ownership, refund handling and server-verified badges; it is not “just add a payment link.”

Choose A if engagement is still too uncertain to support a membership or the developer cannot yet maintain subscription operations. It is a good fallback, but it leaves plausible recurring support and presentation demand unexplored.

## 23. Balanced Strategy

**Strategy B: recurring voluntary support plus finite identity products.** Launch Nobility at **€4.99 / $4.99 monthly**, the **€19.99 / $19.99 Legacy Supporter Pack**, and the **€7.99 / $7.99 Heraldry Collection**. No premium currency, ads, paid power or paid attempts. No battle pass. Current gameplay conveniences remain free.

The recurring product supports operating costs without inventing a content treadmill. Permanent packs serve collectors and people who avoid subscriptions. Existing profiles and shared character names provide the distribution surface. A later bounded preset upgrade is possible after usability evidence; it is not required to make membership valuable.

Strengths: explicit competitive integrity, modest operational scope, repeatable support revenue without infinite power creep, and three understandable purchase decisions. Weaknesses: identity demand remains unproven, membership may feel mostly philanthropic, and a low spending ceiling limits revenue from a small audience. Basic cosmetic infrastructure and cross-service identity verification are still meaningful work.

Validation order: demonstrate purchases display correctly and can be refunded; confirm buyers understand there is no power benefit; measure voluntary renewal and free-player trust; only then test another useful product. Avoid chasing average revenue at the expense of progression fairness.

## 24. Aggressive Strategy

**Strategy C is a rejected comparison, not an implementation recommendation.** A higher-monetization approach could combine the same cosmetic store with:

- **€9.99 / $9.99 monthly Adventurer Membership:** replaces Nobility rather than stacking with it; 10% extra character XP and 48-hour retained offline combat, plus the Nobility visuals.
- **€4.99 / $4.99 weekly Expedition Supply:** two dungeon Sigils, one purchase per account per UTC week.
- A **€7.99 / $7.99 cosmetic seasonal Chronicle**, with no tier skips, only if a seasonal pipeline exists.

No premium wallet or randomized cash rewards would be necessary. These caps avoid an unlimited €500 booster ladder; over four weekly purchase opportunities the membership plus supplies would cost €29.95 / $29.95 before cosmetics or a Chronicle and before applicable USD sales tax. A calendar month can contain five weekly reset dates. More money beyond the caps would not buy additional boosts.

Nevertheless, C is materially less fair. Paid offline retention and XP feed character and Essence progression; Sigils feed equipment, blueprints, cores and mastery. Current ranked snapshots and PvE records retain those gains. Low spending caps prevent unlimited dominance but **do not make purchased advantages fair**. Even €10–€30 can create lasting differences. New players are encouraged to pay to reach the “normal” pace, incumbents resent shortcuts, and groups can expect members to buy supplies.

The commercial upside is only a hypothesis: advancement is a strong motivation, but reduced trust and retention may offset conversion. It adds reward accounting, content balancing, exploit detection and refund-after-progression problems. The pass adds recurring production work. It is therefore less suitable for this solo-developed game than B.

Removing boost flags during Arena fights would not repair C: previously acquired gear, ascension and levels remain. Truly equal competitive access would require separating or normalizing the relevant progression and protecting every other leaderboard, trade path and first-clear race. That is a substantial game redesign, not a monetization toggle. Do not undertake it merely to make C acceptable.

## 25. Recommended Strategy

**Choose B, with a staged, small release after the foundation is proven.** It fits a game where the value is long-term character attachment, collection and shared history, and where progression is tightly connected to competition. It also avoids asking one developer to operate a wallet, a pass factory and an ongoing boost economy.

This recommendation challenges the earlier subscription-slot concept: the Archive is not capacity-limited, and existing automatic presets already provide meaningful convenience. It also challenges the assumption that ordinary titles are automatically safe cosmetics, and the assumption that PvE boosts can be separated from PvP through wording.

The unusually useful opportunity already present is the **repeated visibility of a persistent character name across public profiles, chat and social/competitive lists**. A single tasteful identity entitlement can appear in several relevant places without needing character animation or dozens of products. Its value still needs player validation; it should not be inflated into a forecast.

Do not monetize before ownership, billing and identity work. Do not wait for unrelated guild wars, evolution, new raids, a portrait renderer or a seasonal content factory. If the early audience wants only a one-time way to support, use A until there is evidence for the complete B catalog. The strategic choice remains B; its release pace should follow observed readiness.

## 26. Recommended Initial Product Catalog

All contents below are **proposed new products**, account-owned and nontransferable. Permanent means for the operating life of the service. No product grants currencies, gameplay titles, achievement points, extra attempts, XP, drops or combat slots. Development effort assumes the shared payment and identity foundation already exists.

| Product | Price | Recurring? | What Player Gets | Why They Buy It | P2W Risk | Dev Effort |
| ------- | ----: | ---------- | ---------------- | --------------- | -------- | ---------- |
| Nobility | €4.99 / $4.99 monthly | Yes; cancel renewal any time | Active supporter badge, one rented Nobility profile header, two rented nameplate ornaments; permanent support-history badge after first valid settled month | Support development and show current belonging | Safe once separated from earned progression and verified server-side | Medium: subscription lifecycle and cross-surface identity |
| Legacy Supporter Pack | €19.99 / $19.99 | No; once/account | Permanent Legacy Supporter badge, one permanent profile header and two permanent ornaments; distinct from Nobility designs | Support once and retain a visible personal memento | Safe with no title-count/renown side effects | Low–medium after foundation |
| Heraldry Collection | €7.99 / $7.99 | No; once/account | Three coordinated permanent nameplate ornaments and three matching compact profile-header treatments, all fully previewed | Personalize the character without a subscription or supporter label | Safe; presentation only | Medium initial art and readability QA; low recurring burden |

The catalog contains **five static profile-header treatments and seven ornaments in total**, plus the recognition badge designs. This is a finite initial art scope, not a monthly delivery promise. Use a shared renderer and small curated style catalog. If the first art batch is too expensive or previews do not appeal to players, delay the Heraldry Collection rather than filling it with low-value recolors.

Release the one-time Supporter Pack first during the controlled introduction because its ownership/refund lifecycle is simpler. Enable Nobility and Heraldry within the initial launch program once subscription and presentation checks pass. Do not advertise unavailable products as purchasable promises. Existing buyers keep exact contents and receive no forced subscription conversion.

No annual plan, preset expansion, rename, guild service, pass, gift, tip ladder or wallet in the initial catalog. The next product should be chosen from observed demand, not from a fixed release quota.

## 27. Implementation Roadmap

This is a plan for a later implementation task. No implementation, migration, configuration change, payment integration or deployment was performed in this analysis. Effort ranges below are rough focused solo-development ranges, not estimates based on completed technical design; observation and external provider approval can take longer.

| Phase | Why it exists / work | Dependencies | Approximate complexity | What to learn / exit condition |
| --- | --- | --- | --- | --- |
| **1 — Before monetization** | Verify actual release flags and obtainable content; instrument resource flows, acquisition tails and free-player friction; publish the fairness boundary; settle guest/account ownership and Beta-wipe policy; design a useful free identity baseline. Clarify Soulstones versus Soul Archive, incomplete evolution, and Tower Token expectations. | Current game, access to legitimate production analytics/configuration in a separately authorized task. | Medium; roughly 1–3 focused weeks for bounded audit/instrumentation work, plus observation. Major balance repairs are additional scope. | Is free progression healthy? Are people attached to profiles/community? Are limits real player needs? A simple supporter launch need not wait for a complete Tower Token sink if token expectations are clear and nothing sells tokens. |
| **2 — Monetization foundation** | Choose hosted checkout/provider; purchase records, account entitlements, versioned products, webhook receipt/deduplication, reconciliation, refunds, billing portal and entitlement revocation. Add distinct cosmetic recognition; server-verify identity in LL and LL-Chat. | Phase1 ownership/principles; provider eligibility, tax/consumer handling and clear products. | High; approximately 2–4+ focused weeks depending on provider and cross-service work. | Retry/refund/cancel/expiry work reliably; one payment cannot create two grants; payment cannot affect gameplay or earned counters. Support can locate and explain every purchase. |
| **3 — First products** | Controlled release of Supporter Pack, then Nobility and Heraldry after lifecycle checks. Preview exact art on real surfaces, retain free identity choices, use quiet navigation and purchase history. | Foundation, approved assets, verified public profile/chat/guild/Tavern display. | Medium; roughly 1–2 focused weeks beyond foundation, depending on art readiness. | Do players understand and value the offer? Are refunds and support manageable? Observe billing cycles before larger catalog expansion. No revenue forecast required. |
| **4 — Expansion** | Choose one evidence-backed item: extra preset memory after free baseline improvements, collection-showcase decoration, optional annual membership or curated guild heraldry. Add account services only for actual demand. | Reliable existing products and real usage/cohort data; each candidate's free gameplay foundation. | Small–medium per item; several days to a few weeks. Guild ownership can exceed this. | Does it solve an observed desire without creating a payment requirement? Is incremental net value worth production/support cost? |
| **5 — Avoid unless evidence supports it** | Evaluate cloud history or an evergreen cosmetic Chronicle only if their gameplay use exists independently. Paid acceleration remains rejected for the current shared game; revisiting it requires explicit strategy review and proof of competitive/economic isolation. | Stable game operations, measured demand, sustainable content/storage budget, integrity review. | High/ongoing; weeks to months for seasonal or progression isolation systems. | If demand is only “we need more revenue,” do not proceed. Evidence must show player value, operability and preserved fairness. |

The minimum future verification suite should prove that an identical combat state produces the same gameplay behavior regardless of store ownership; that no purchase increments titles, renown, collection counters or currency; that duplicate and out-of-order payment notifications are safe; and that refund/expiry changes only the intended entitlement. Include guest conversion, account switching, concurrent checkout, failed renewal and cross-service badge visibility. Future backend tests must run through [build/run-tests.ps1](C:/repos/Legends-Legacy/legends-legacy/build/run-tests.ps1:1), with appropriately scoped frontend checks through npm.

For measurement, capture the currency earned/spent by source, time to first useful Essence/build, dry-streak distribution, ascension progress, successful combat rate, dungeon entry/completion/failure rates, ticket-cap losses, offline-retention losses, preset edits and full-library frequency, profile visits, voluntary purchase/renewal/cancellation, refunds and support time. Do not collect more identifying data than these purposes need. Compare free and paying cohorts while accounting for tenure/activity; purchasers are self-selected.

Record business decision thresholds before each expansion once actual costs and baseline behavior are known. Examples: unresolved grant/refund failures block launch; paid-item changes to gameplay counters block launch; frequent free-library friction triggers a free usability change; low profile exposure delays new art. Avoid inventing a universal conversion threshold or using a high-spend minority to justify unfair products.

### Audit verification and repository impact

This task adds only this Markdown analysis document. Existing unrelated changes were left untouched. Verification consists of source/route/service inspection, configuration/content checks, independent read-only reviews of the findings, document structure and local source-link validation, and whitespace/diff checks. No application code was written or modified for this task.

Verification results: PowerShell document checks confirmed all **30 numbered sections in order**, both closing reference sections, **ten final actions**, and **83 existing local source links with valid line numbers**. Pricing arithmetic checks confirmed 32.97, 24.98, 9.98 and the aggressive four-opportunity example of 29.95. A trailing-whitespace check passed. `git diff --check` reported no issues; because this is a new untracked document, it was also checked with `git diff --no-index --check -- NUL docs/analysis/monetization-strategy-audit-2026-09-10.md`, which reported no whitespace errors, an expected new-file difference exit status of 1 and the repository's LF-to-CRLF notice. `git status --porcelain=v1` was inspected before and after; other tasks were concurrently adding unrelated analysis files.

Backend tests, frontend builds/tests, a live game session, payment-provider sandbox transactions and database verification were not run: this is an investigation with no application changes, and no payment implementation exists to test. No shared/production database was accessed or migrated; no service was deployed. A small number of exploratory file searches initially used nonexistent paths and were corrected by locating the actual files. No required implementation command remains blocked.

The future work described here would require payment configuration/secrets, new persistent purchase/entitlement records and likely migrations, plus coordinated game/chat identity changes. Those are deployment implications of a future project, not changes made in this one. Infrastructure-as-code remains outside this repository and outside this analysis task's change scope.

## 28. Systems Worth Building Before Monetization

| Addition / repair | Makes the free game better because… | Needed before first product? |
| --- | --- | --- |
| Clear, useful public identity with a basic free appearance | Players can recognize one another and keep earned titles visible. | Yes, a small extension of existing profiles/tags; no portrait renderer required. |
| Separate earned prestige from purchased recognition | Achievements and ranks remain meaningful; paid marks cannot impersonate accomplishment. | Yes. |
| Server-authoritative identity in chat and shared displays | Reduces spoofing and keeps profile/chat identity consistent. | Yes for selling badges on those surfaces. |
| Recoverable account ownership and durable purchase history | Players can safely return, recover accounts and understand ownership. | Yes for purchases; account recovery is valuable regardless. |
| Acquisition and economy observability | Makes balancing responsive to actual friction, especially Essence dry streaks and incomplete sinks. | Baseline observation first; not a demand for a large analytics platform. |
| Clear preset selection/fallback and adequate free capacity | Reduces accidental mismatches across content and supports experimentation. | Before any paid preset product, not a blocker for a simple support badge. |
| Basic unified build save/preview, if players need it | Equipment, Essences and Style can be understood together. | No; only if ordinary use justifies the new editor. |
| Free collection/achievement showcase | Turns earned progress into social stories and personal goals. | No; before selling showcase decoration. |
| Clarified Tower Token purpose / unfinished rewards | Avoids players earning unexplained balances or expecting advertised rewards. | Clarify now; do not build a store sink purely to monetize it. |
| Stable seasons, if seasons improve competition | Gives tournaments understandable starts, ends and earned history. | Not needed for supporter products; required before a seasonal commercial track. |

The current UI does not need a new character-rendering engine, dozens of cosmetic categories, housing, pets, a premium wallet or multiple characters to become monetizable. Build only the features that improve the actual game and support a small credible purchase.

Soulstone upgrades illustrate why maintaining the earned game is sufficient: seven enabled branches already provide five ranks each, with rank costs 25/75/150/300/600 and a full-cost reset refund. Their maximum effects include acquisition, pity, focused drops, duplicate dust, combat XP and reward-retention bonuses. That is already a long-term earned upgrade layer; selling another cash version is unnecessary. [Soulstone upgrade definitions](C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/progression/soulstone-upgrades.json:1), [free reset](C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Soulstones/SoulstoneUpgradeService.cs:117).

## 29. Things LegendsLegacy Should Never Sell

- Equipped Essence slots, exclusive Essence/Style mechanics, ascension, dust, monster cores or collection completion.
- Equipment, better rolls/rarity, reinforcement power, stat-changing blueprints or better salvage returns.
- Arena tickets, ticket regeneration/cap upgrades, Glory, rating, tournament seeding or registration priority.
- Dungeon Sigils/fragments, Vigor, revives, mastery, reward insurance or additional reward eligibility.
- Tower first-clear access, contribution power, preparation/scouting limits, token grants or weekly resets.
- Cinders, Soulstones, Fate Echo, Guild Favor, Tower Tokens, Raid Trophies or currency-conversion vouchers.
- Core regions, quests, endgame access, functional guild membership capacity, buildings, supplies or vault power.
- Achievement points, renown, earned prestigious titles or purchased recognition counted as earned progress.
- Reliability, account recovery, moderation fairness, blocking, accessibility, basic information or existing free quality-of-life features.
- Invented bag/Archive limits, paid relief from intentionally worsened free pacing, or access to retained gameplay data after membership expires.
- Loot boxes, gacha, mystery paid rewards, paid currency bundles designed to leave leftovers or spending-ranked donor status.
- Tradable premium assets, account resale value, custom paid balance changes or promises about unfinished gameplay.

These are product boundaries for the current game. A later strategic redesign would need an explicit new integrity review; renaming a rejected purchase “convenience” does not change its effect.

## 30. Final Recommendation

Build a game people want to support, then give them a modest, visible way to do so. **Launch Strategy B: €4.99 monthly Nobility, €19.99 permanent Supporter Pack, €7.99 Heraldry Collection; direct real-money prices; no premium currency, paid power, attempts or ads.** Release it only after payment ownership, refunds and separate server-verified cosmetic identity are reliable.

Do not monetize Essence Archive storage. Do not rent build access. Do not sell “PvE-only” boosts into a game whose real progression becomes ranked combat and shared first-clear power. Keep optional extra preset memory as a later, capped permanent purchase after the free experience is demonstrably adequate.

The primary commercial risk is insufficient demand for support and modest cosmetics in a small text-heavy game. The primary implementation risk is assuming that badges and payments are only frontend work. The primary design risk is quietly adding power to make the subscription easier to sell. Address the first through player understanding and visible identity, the second through a small robust foundation, and the third through explicit product rules.

## The Monetization Model in One Page

| Decision | Reference |
| --- | --- |
| **What we sell** | Voluntary support, restrained public identity, permanent cosmetic collections; possibly finite extra preset memory later. |
| **What we do not sell** | Power, currencies, equipment/Essences, drop rates, XP, tickets, Sigils, extra rewards, progression caps, core content, earned prestige or relief from invented friction. |
| **Subscription** | Nobility, €4.99 / $4.99 monthly. Active badge + one rented header + two rented ornaments; permanent valid-payment history badge. No gameplay benefits or monthly reward claims. |
| **Expiry** | Paid-period access ends with a free visual fallback. Gameplay, builds, collection and permanent purchases remain. |
| **Premium currency** | None. Direct prices and exact contents. |
| **Spending ceiling** | Entire launch catalog €32.97 / $32.97 in month one; €4.99 / $4.99 recurring thereafter. EUR tax-inclusive convention; USD before applicable sales tax. No duplicate permanent packs, stacked memberships, consumable power or donor ladder. |
| **Free-player philosophy** | Complete core game and endgame, normal progression, every build, meaningful competition, current conveniences and uncapped collection/storage behavior. |
| **Competitive integrity** | Identical gameplay rewards for identical play; no bought inputs to Arena, tournaments, Tower or other rankings; paid badges excluded from earned title/achievement counts. |
| **Initial products** | Nobility €4.99/month; Legacy Supporter Pack €19.99 once; Heraldry Collection €7.99 once. Same nominal USD price points; taxes clearly disclosed. |
| **Future candidates** | Optional annual membership; one €4.99 permanent preset extension after an adequate free baseline; earned-showcase decoration; curated guild heraldry. Add one only when justified. |
| **Not planned** | Battle pass, advertising, premium wallet, character skins, paid boosts, rented storage or character-slot economy. |
| **Major risks** | Weak willingness to pay for text-based identity; cross-service badge spoofing; title-count achievement leakage; payment/refund complexity; incomplete rewards/sinks; pressure to sell power if revenue disappoints. |
| **Launch gate** | Healthy free baseline, recoverable accounts, source-separated cosmetics, verified payment/entitlement lifecycle, exact previews and understandable cancellation/refunds. No gameplay implementation authorized by this report. |

## Top 10 Actions

1. **Adopt the Strategy B boundaries in writing:** no paid progression/resources/attempts, no premium wallet, no invented Archive or inventory cap, and no payment effect on earned prestige.
2. **Verify the actual release state and measure progression:** deployed feature flags, obtainable Essence content, resource income/spend, dry streaks, dungeon progression, offline losses and free-player friction. Clarify Tower Tokens and disabled reward expectations.
3. **Validate willingness to buy identity and support with committed players:** show exact profile/nameplate previews and the three proposed prices; ask what they would choose and why. Do not substitute a generic idle-game benchmark.
4. **Define a small cosmetic identity model separate from titles and achievements:** free baseline, distinct badges, unchanged rank/rarity meaning, and zero earned-counter side effects.
5. **Make identity authoritative across game and chat:** specify account ownership, canonical display data and explicit profile/chat/guild/Tavern surfaces; eliminate reliance on caller-supplied text for paid status.
6. **Select hosted payments and settle the commercial rules:** recoverable-account requirement, Beta wipes, seller/tax obligations, receipts, recurring consent, refunds and provider eligibility. Obtain an actual fee quote.
7. **Build and test the payment/entitlement foundation in a later authorized task:** idempotent purchases, durable webhook processing, concurrency, reconciliation, refunds, cancellation and expiry; no changes to gameplay rewards.
8. **Introduce the three-product catalog gradually:** permanent Supporter Pack first, then Nobility and Heraldry after lifecycle checks. Use quiet navigation, exact previews and no gameplay prompts.
9. **Review renewals, refunds, free-player sentiment and support cost before expansion:** improve value or simplify the offer if demand is weak; do not add power to fix conversion.
10. **Investigate preset usage as the first functional expansion candidate:** clarify fallback and improve the free allowance first; only then consider a once-only seven→twelve library upgrade, or keep all capacity free if that better serves the game.
