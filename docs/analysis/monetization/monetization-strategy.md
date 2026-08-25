# LegendsLegacy Monetization Strategy

- **Prepared:** 2026-08-25
- **Scope:** Product, economy, and monetization design only. No monetization feature is implemented by this report.
- **Evidence rule:** “Implemented” means present in the repository, not proven deployed or healthy in production.

## Concise verdict

| Question                            | Verdict                                                                                                                                                                                                                                                                                                                                       |
| ----------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Should the game be monetized now?   | **Not by accepting payments yet.** LegendsLegacy has enough systemic depth to design monetization, but it lacks payment, entitlement, cosmetic-ownership, commerce-support, and purchase-analytics foundations. Retention and economy baselines are also unknown. Build and validate those foundations, then run a small monetization launch. |
| What should be sold first?          | **A one-time, account-bound Supporter Pack** with clearly non-gameplay identity items, followed by permanent saved-loadout capacity and carefully priced identity cosmetics. No resources, Essences, equipment, or boost consumables.                                                                                                         |
| Strongest recurring-revenue option? | **A delayed €7.99/month “Patron’s Oath” membership** built around a permanent monthly cosmetic choice, visible supporter identity, and bounded absence recovery. It should launch only after D30 retention, cosmetic visibility, and renewal operations are proven.                                                                           |
| Greatest danger?                    | **Selling production or outcome multipliers**—especially Essence drop rate, gathering/crafting yield, tempering protection, extra activity slots, dungeon entries, or Auction House capacity. These compound into combat power and tradable wealth and would turn spending into market control.                                               |
| Missing foundation?                 | Server-authoritative commerce and entitlements; idempotent fulfillment/refunds; a real cosmetic/profile surface; payment and economy analytics; tested economy baselines; customer-support tooling; and qualified VAT, consumer-law, privacy, and accounting review.                                                                          |

The recommended strategy is a **narrow Balanced model**: monetize durable identity, configuration flexibility, and limited recovery of time missed beyond the free 24-hour offline window. Do not sell higher output per hour, better random outcomes, more competitive attempts, more simultaneous activities, market priority, or exclusive combat content.

## 1. Executive verdict

LegendsLegacy should prepare for monetization now but should not take money until the “must have before accepting payments” gate in section 15 is complete. The game is no longer a contentless prototype: idle combat, Essence buildcraft, Crafting and Tempering, Dungeons, Prophecies, Guild progression, the Marketplace, Colosseum, Tournament Grounds, the World Tower, and standalone Raids create a credible long-term RPG. However, repository depth is not evidence of product-market fit. The repository contains no commercial payment or entitlement system, and no live MAU, retention, payer-intent, progression-distribution, or inflation baseline was available.

The owner’s position—“paid players may receive an advantage, but spending must not invalidate progression, dominate markets, or determine competition”—is coherent only if “advantage” is narrowly defined. A 10% gathering yield bonus sounds small but runs continuously, supplies crafting, creates tradable inventory, compounds Cinders through the Marketplace, improves gear access, and feeds every power-bearing mode. The enforceable boundary therefore cannot be “small percentages are fine.” It must be:

> **Payment may improve expression, configuration, and recovery from absence. Payment may not increase expected output per active hour, expected value per limited entry, simultaneous production capacity, market reach, or the ceiling of combat power.**

Paying players may obtain a non-exclusive destination faster only through bounded absence recovery or reduced configuration friction. They may obtain cosmetics more reliably because cosmetics are the product. They may not buy an exclusive gameplay destination, a better reward distribution, or an uncapped rate advantage.

## 2. Repository evidence and current-state assessment

### Status classification

| Area                                     | Status                                              | Current repository evidence and monetization relevance                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| ---------------------------------------- | --------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Accounts and characters                  | **Implemented**                                     | Accounts support guest creation/conversion and one current character reference (`LL/src/Core/Domain/Models/Users/AppUser.cs:4-20`; `LL/src/API/API.LL/Controllers/V1/AuthController.cs:50-116`). A guest is too recoverability-sensitive for purchases; commerce should require conversion to a durable account.                                                                                                                                                                                                                                                                                                                                                         |
| Idle combat and offline progress         | **Implemented**                                     | Encounters use a 10-second cadence and a 24-hour maximum offline window (`LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Models/IdleCombatProgressionOptions.cs:3-12`; `LL/src/API/API.LL/appsettings.json:61`). This creates a bounded, understandable membership lever, but extending it creates real resources for players absent over 24 hours.                                                                                                                                                                                                                                                                                               |
| Single activity capacity                 | **Implemented**                                     | One `CharacterAction` represents Combat or Crafting, with Gathering piggybacking on combat; paused Tempering queues are retained (`LL/src/Core/Domain/Models/CharacterActions/CharacterAction.cs:8-55`). A paid second action is therefore not convenience—it approximately adds another production lane.                                                                                                                                                                                                                                                                                                                                                                |
| Gathering                                | **Implemented but shallow/imbalanced**              | Gathering is a victory-triggered combat byproduct with one equipped tool, not a separate action. Base cadence, throughput, content breadth, and the severe profession-level tuning mismatch are documented at `docs/gathering-system-progression-analysis.md:28-71`, `:157-178`, and `:296-321`. Selling gathering speed/yield before fixing this would monetize an unfinished balance problem.                                                                                                                                                                                                                                                                          |
| Essence acquisition and Soul Archive     | **Implemented**                                     | Unbound Essences drop, remain tradable, can be absorbed into a bound Archive entry, dismantled into dust, leveled, ascended, evolved, and attuned (`docs/EssenceSystem.md:5-17`). The content catalog currently contains 75 Essence definitions; representative definitions begin at `LL/src/API/API.LL/Data/essences/essences.json:4` and end near `:1386`.                                                                                                                                                                                                                                                                                                             |
| Essence combat power                     | **Implemented**                                     | Only active-loadout Essences grant attributes and abilities (`docs/EssenceSystem.md:97-125`, `:157-167`). Ascension changes cooldown and effect magnitudes (`LL/src/Core/Domain/Models/Essences/EssenceProgressionConstants.cs:19-33`). Any paid drop, dust, core, catalyst, XP, or extra active slot is paid combat power.                                                                                                                                                                                                                                                                                                                                              |
| Essence slots and presets                | **Implemented**                                     | Active slots unlock from 1 to 10 by character level; the saved loadout limit is 3 (`LL/src/Infrastructure/Service/Services.LL/Essences/EssenceLimitServices.cs:5-17`). Save validation prevents locked slots and duplicate creature sources (`LL/src/Infrastructure/Service/Services.LL/Essences/EssenceSystemService.cs:276-318`). More saved presets are convenience; more simultaneously active Essences are power.                                                                                                                                                                                                                                                   |
| Archive/inventory capacity               | **No bounded capacity found**                       | Inventory is an unbounded collection in the domain model (`LL/src/Core/Domain/Models/Inventories/Inventory.cs:4-12`); the LiveOps roadmap explicitly notes that no bounded capacity read model exists (`docs/liveops-dashboard-additions-roadmap.md:99-100`). The Soul Archive is uniqueness-bound by Essence definition, not storage slots (`docs/EssenceSystem.md:22-36`). Introducing limits merely to sell relief would be artificial degradation.                                                                                                                                                                                                                   |
| Crafting, quality, Potential, blueprints | **Implemented; balance still partial**              | Crafting consumes tiered materials and creates exact equipment with quality, Potential, mastery, and blueprint provenance; Tempering queues and consumes one finite Potential per attempt (`crafting-v2-implementation-status.md:398-446`). Production-ready outcome tuning and later-region content remain incomplete (`crafting-v2-implementation-status.md:350-396`).                                                                                                                                                                                                                                                                                                 |
| Tempering randomness                     | **Implemented**                                     | Every attempt spends Potential. Outcomes include Critical, Positive, Negative, and Neutral; Negative can consume an additional Potential or remove item XP (`LL/src/Infrastructure/Service/Services.LL/Professions/Craftings/TemperingMechanicsService.cs:23-107`, `:293-327`). Paid failure protection or quality odds would materially improve the supply of top equipment.                                                                                                                                                                                                                                                                                            |
| Dungeons                                 | **Implemented with product/telemetry gaps**         | Four JSON families—Goblin Mines, Forgotten Catacombs, Tangled Cave, and Great Tree—each expose three difficulties (`LL/src/API/API.LL/Data/dungeons/dungeons.json:5`, `:268`, `:511`, `:707`). Runs are server-authoritative, seeded, Vigor-based, and claim Pending Loot, but consequence UX, expiry, tier variance, and telemetry are partial (`docs/dungeon-run-experience-implementation-status.md:963-1015`). Entry uses consumable sigils, not a paid-ready energy model.                                                                                                                                                                                          |
| Prophecies                               | **Implemented MVP**                                 | Daily choice, weekly prophecy, Revelation milestones, reward snapshots, cache items, and event-driven progress exist (`docs/prophecies-implementation-status.md:7-29`). Production economy tuning and rollover/cleanup remain incomplete (`docs/prophecies-implementation-status.md:308-316`). Prophecy objectives are not yet a safe paid-pass foundation.                                                                                                                                                                                                                                                                                                              |
| Marketplace and direct trade             | **Implemented**                                     | Stackable commodities use matching and buy orders; exact equipment preserves item identity; escrow, immutable fills, a 3% seller fee, seven-day expiry, and price guidance exist (`LL/docs/marketplace-and-economy-design.md:230-257`). Current caps are 10 sell listings and 10 buy orders (`LL/src/Infrastructure/Service/Services.LL/MarketPlaces/MarketPlaceOptions.cs:3-15`). Cinders and unbound items can also be transferred directly (`LL/src/Infrastructure/Service/Services.LL/Entities/Characters/CurrencyTransferService.cs:12-47`; `LL/src/Core/Application/UseCases/Inventories/Commands/TransferInventoryItem/TransferInventoryItemCommand.cs:157-171`). |
| Cinder sink                              | **Implemented but likely insufficient**             | The Marketplace removes a configurable 3% seller fee (`LL/src/API/API.LL/appsettings.json:103-111`; `LL/src/Infrastructure/Service/Services.LL/MarketPlaces/MarketPlaceService.cs:1048-1054`). Earlier economy analysis found substantial Cinder faucets and identified inflation as likely (`LL/docs/marketplace-and-economy-design.md:57-78`). Live data is required to determine whether the newer fee stabilizes supply.                                                                                                                                                                                                                                             |
| Colosseum                                | **Implemented with integrity gaps**                 | Five tickets cap at one restored every three hours (`LL/src/Core/Domain/Models/Colosseum/ArenaTicketStatus.cs:5-12`; `LL/src/Infrastructure/Service/Services.LL/Colosseum/ColosseumService.cs:732-747`). Rating, Glory, defense snapshots, records, and Champion’s Market exist, but snapshot invalidation and DB-level idempotency/concurrency remain partial (`LL/ColosseumPvPImplementationStatus.md:105-153`).                                                                                                                                                                                                                                                       |
| Tournament Grounds                       | **Implemented v1, seasons partial**                 | Scheduled snapshot-locked single-elimination tournaments and durable rewards exist (`docs/tournament-grounds-implementation-status.md:5-10`, `:93-120`). “Current season” is a computed calendar month (`LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/TournamentGroundsService.cs:467-474`); durable season records, seasonal Hall of Fame, and cosmetic grants do not (`docs/tournament-grounds-implementation-status.md:424-437`).                                                                                                                                                                                                                  |
| General leaderboards                     | **Implemented**                                     | Boards rank total/combat level, Archive completion, achievements, Dungeon mastery/clears, arena, tournaments, guilds, professions, and Raids (`LL/src/Core/Domain/Models/Leaderboards/LeaderboardBoardKey.cs:3-54`). This broad ranking surface makes even “PvE-only” paid acceleration socially competitive.                                                                                                                                                                                                                                                                                                                                                            |
| Achievements and titles                  | **Implemented**                                     | Title definitions, unlocks, prefix/suffix display, and equipped title state exist (`LL/src/Core/Domain/Models/Achievements/TitleDefinition.cs:3-20`; `LL/src/Core/Domain/Models/Entities/Characters/Character.cs:38-40`). Sell only distinctly sourced supporter titles; never sell titles that imply an achievement, rank, realm first, or season placement.                                                                                                                                                                                                                                                                                                            |
| Broader cosmetics/profile identity       | **Partial or absent**                               | Colosseum purchase records currently stand in for title/banner/cosmetic ownership, with real entitlement systems missing (`LL/ColosseumPvPImplementationStatus.md:125-132`). No general portrait, nameplate, skin, or cosmetic inventory domain was found. Cosmetics are not automatically viable until players can see them often.                                                                                                                                                                                                                                                                                                                                      |
| Guilds                                   | **Implemented core; competitive expansion partial** | Missions, personal orders, Guild Favor, Supplies, shop stock, buildings, contribution boards, and frontend tabs exist (`docs/guild-expansion-implementation-status.md:7-156`, `:192-215`). Several building effects are placeholders and guild wars are absent (`docs/guild-expansion-implementation-status.md:299-362`). Do not sell guild progress or placeholder relief.                                                                                                                                                                                                                                                                                              |
| World Tower                              | **Implemented 10-floor MVP; rewards partial**       | Ten floors, realm progression, first-clear Expeditions, Echo Mode, frozen builds, preparation, and Hall of Fame are implemented (`docs/world-tower-implementation-status.md:38-61`). Tokens are granted, but most prestige/cosmetic rewards and downstream unlock consumption are partial (`docs/world-tower-implementation-status.md:54-58`). Realm-first integrity is monetization-sensitive.                                                                                                                                                                                                                                                                          |
| Standalone Raids                         | **Implemented; content/incentives still maturing**  | Free public musters, frozen snapshots, three preparation parties, a combined assault, graded outcomes, claims, Trophies, and a vendor are implemented (`LL/docs/raid-system-game-design-analysis.md:32-95`). One account occupies one Raid slot (`:481-487`). The architecture is strong, but diagnostic clarity and long-term encounter expression remain weak (`:907-915`). These are standalone Raids, not Guild Raids.                                                                                                                                                                                                                                               |
| Guild raids and wars                     | **Not implemented**                                 | The guild status document explicitly lists both as unshipped (`docs/guild-expansion-implementation-status.md:329-362`). Standalone regional Raids must not be mislabeled as Guild Raids.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| Doctrines                                | **Not implemented**                                 | No Doctrine state is present in snapshots or combat (`docs/unified-content-scaling-model.md:81`; `LL/docs/raid-system-game-design-analysis.md:167-175`). Do not sell hypothetical Doctrine slots or exclusives.                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| Rifts                                    | **Documented/planned**                              | The design proposes subscription auto-join as attendance, with unchanged entry caps and value per entry (`LL/docs/rift-system-design.md:695-722`). It also states that no subscription/entitlement system currently exists (`:758-778`). Rifts are not implemented and cannot support launch revenue.                                                                                                                                                                                                                                                                                                                                                                    |
| Payments and commercial entitlements     | **Absent**                                          | Searches found no payment provider, checkout, commercial purchase, subscription, receipt, or premium-entitlement implementation. The only “purchases” are in-game Soulstone, Champion’s Market, Guild Shop, Raid vendor, and Marketplace flows. This is a hard launch blocker.                                                                                                                                                                                                                                                                                                                                                                                           |

### Important unknowns

- Actual deployment and migration state. Status documents repeatedly distinguish repository implementation from deployment.
- MAU, DAU/MAU, D1/D7/D30/D90 retention, session frequency, offline-return distribution, and player tenure.
- Economy baselines: faucet/sink totals, wealth percentiles, price indices, market liquidity, and item velocity.
- Which systems players genuinely value, understand, and revisit versus merely having pages in the repository.
- Population sufficiency for PvP, Tournaments, Tower, public Raids, Guilds, chat, and the Marketplace.
- Production hosting/payment costs, support capacity, refund behavior, target countries, age distribution, and business/tax structure.

## 3. Engagement and economy diagnosis

### Core engagement

The primary loop is:

> choose an area and build → run idle combat → receive XP, Cinders, Soulstones, loot, gathering materials, Essence rolls, and sigil progress → improve equipment/Essences/constellations → tackle harder or more social content → repeat.

Secondary loops create reasons to intervene:

- Configure up to ten active Essences and three saved loadouts.
- Craft gear projects, learn blueprints, and spend finite Potential through Tempering.
- Choose Prophecies and claim daily/weekly progress.
- Route through Dungeons and decide whether to retreat with Pending Loot.
- Trade commodities and exact equipment.
- Spend tickets in Colosseum and register snapshot-locked Tournament builds.
- Complete Guild orders/missions and invest shared Supplies.
- Assemble Tower or Raid groups, freeze a build, and review playback/results.
- Complete collections, achievements, titles, leaderboards, and prestige records.

Likely goal horizons are:

| Horizon                   | Goals                                                                                                                                                                | Return driver                                                                                                    |
| ------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| Short: minutes to one day | Choose action/tool/build, inspect drops, manage inventory, claim Prophecy/order rewards, spend Arena tickets, adjust a Dungeon route                                 | Ten-second resolution cadence, 24-hour offline cap, daily Prophecies/orders, three-hour Arena ticket restoration |
| Medium: days to weeks     | Finish an Essence ascension, learn/craft/temper a blueprint item, clear Dungeon tiers, improve a Guild mission tier, buy limited vendor stock, place in a Tournament | Weekly mission/vendor periods, item projects, pity/resonance, snapshots and scheduled group content              |
| Long: months              | Complete Archive/Codex sets, max Essences, build specialized gear, climb leaderboards, collect titles, master bosses, earn Hall-of-Fame or realm-first prestige      | Collection completion, broad leaderboards, social identity, server progression, content expansion                |

The expected session pattern is short management visits around long idle intervals, plus occasional longer Dungeon, market, replay, and social sessions. The 24-hour offline cap encourages daily return without forcing multiple logins per day. A paid 48-hour cap can help weekend/irregular players without improving the hourly rate, but it still creates more output for subscribers who would otherwise exceed 24 hours and therefore must be monitored.

### Retention strengths

- The Essence system joins collection, monster identity, drop anticipation, pity, bound progression, active/passive abilities, and buildcraft.
- Crafting produces exact-instance long projects, giving gathered and traded inputs durable meaning.
- Prophecies, Arena tickets, Guild orders/missions, limited vendor stock, Tournaments, Tower, and Raid reward periods provide multiple calendar rhythms.
- Marketplace, titles, chat, Guilds, Hall of Fame, and leaderboards make progress visible to others.
- Frozen snapshots make asynchronous group/competitive activities fit an idle game.

### Retention weaknesses

- Breadth can overwhelm purpose: a player can see many systems without knowing the next best goal.
- Gathering levels currently take extremely long while offering few mechanical unlocks (`docs/gathering-system-progression-analysis.md:157-178`). Monetizing this gap would look like selling the repair.
- Several systems are structurally complete but have incomplete reward identity, diagnostics, seasonal records, or cosmetic visibility.
- Many currencies compete for attention; Tower Tokens currently have a source but an incomplete sink.
- Seasonal content is not yet one coherent loop. Monthly Tournament scoring alone is not a season product.
- Live retention, population, and balance data are unknown. No price can compensate for weak D30 retention.

### Player identity and motivation

Strong identity candidates are Essence specialization, crafted-item provenance, titles, Arena rank, Guild affiliation/contribution, Raid/Tower records, and collection completion. Weak identity candidates today are character appearance, portraits, skins, banners, and nameplates because a durable ownership/display system is missing. Cosmetics should be launched only after at least three recurring visibility surfaces—profile/character header, chat/social identity, and competitive/group results—show them naturally.

### Economy map

| Asset                                       | Main sources                                                                    | Main sinks                                                                                                                                 | Trade/competition status                                                       | Monetization sensitivity                                                                                                                                             |
| ------------------------------------------- | ------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Cinders                                     | Idle combat, Dungeons, Prophecies, Guild/Colosseum/Tournament and other rewards | Marketplace seller fee; transfers/purchases move rather than destroy supply                                                                | Directly transferable and Marketplace unit of account                          | **Extreme.** Paid Cinders or yield becomes general buying power and alt-account fuel.                                                                                |
| Soulstones                                  | Combat and multiple reward systems                                              | Soulstone constellation upgrades, with some refund behavior                                                                                | Character balance; not the Marketplace unit                                    | High: bonuses affect combat, gathering, crafting, Essence, sigils, and reward retention through `BonusKind` (`LL/src/Core/Domain/Models/Bonuses/BonusKind.cs:3-20`). |
| Fate Echo                                   | Prophecy and Guild rewards                                                      | Daily Prophecy rerolls (`LL/src/Infrastructure/Service/Services.LL/Prophecies/ProphecyService.cs:269-300`)                                 | Bound character currency                                                       | Medium/high: buying rerolls can optimize objectives/rewards and daily retention. Do not sell.                                                                        |
| Sigil Fragments and sigils                  | Prophecies, PvP/Tournament/Guild rewards, idle drops                            | Assemble sigils; Dungeon entry consumes sigils (`LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonSigilAssemblyService.cs:22-80`) | Sigils/resources may be inventory assets depending on binding                  | High: paid entries multiply Dungeon loot, cores, mastery, and records.                                                                                               |
| Glory                                       | Arena attacks and Tournaments                                                   | Champion’s Market with weekly/lifetime limits (`LL/src/Infrastructure/Service/Services.LL/Colosseum/ColosseumService.cs:449-502`)          | Competitive currency                                                           | Extreme: never sell Glory, tickets, or market access.                                                                                                                |
| Guild Favor                                 | Personal orders and weekly mission rewards                                      | Guild Shop                                                                                                                                 | Personal guild currency                                                        | High: paid Favor converts to progression resources and distorts contribution meaning.                                                                                |
| Guild Supplies                              | Guild activity                                                                  | Guild construction/upgrades                                                                                                                | Shared guild resource                                                          | Extreme: paid Supplies let spenders direct shared progression and social power.                                                                                      |
| Tower Tokens                                | First clears and weekly Echo rewards                                            | Meaningful player sink is not yet implemented                                                                                              | Prestige/endgame currency                                                      | Do not sell; first build a sink and observe supply.                                                                                                                  |
| Raid Trophies                               | Graded Raid outcomes with weekly entitlement behavior                           | Boss vendor, weekly/lifetime limits                                                                                                        | Boss-mastery currency                                                          | High: paid Trophies bypass group mastery and feed enhanced blueprints/materials.                                                                                     |
| Soul Dust                                   | Dismantled unbound Essences and rewards                                         | Essence levels                                                                                                                             | Inventory resource; unbound input supply is market-linked                      | Extreme: paid dust accelerates equipped ability/stat scaling.                                                                                                        |
| Monster Cores and catalysts                 | Dungeons, Prophecies, Champion/Guild markets, Raids and content rewards         | Essence ascension/evolution                                                                                                                | Inventory items; tradeability depends on item definition                       | Extreme: direct Essence power and market demand.                                                                                                                     |
| Core/special crafting materials             | Combat-linked Gathering, Dungeons, vendors, content rewards                     | Crafting                                                                                                                                   | Stackable commodities                                                          | Extreme: paid yield crowds out gatherers and lowers/controls input prices.                                                                                           |
| Blueprints                                  | Dungeons, vendors, rewards, Gathering discoveries/plans                         | Copies are consumed when learned for a recipe; crafted outputs persist                                                                     | Unbound copies can trade                                                       | High: paid copies undermine content and crafting specialization.                                                                                                     |
| Equipment                                   | Crafting and some reward paths                                                  | Scrapping; finite Potential is consumed during Tempering                                                                                   | Exact-instance market; rolled stats matter                                     | Extreme: direct or odds-improved supply changes best-in-slot availability.                                                                                           |
| Unbound Essence items                       | Monster drops with resonance/pity and Dungeon modifiers                         | Absorb or dismantle                                                                                                                        | Tradable commodity; archived copy becomes bound (`docs/EssenceSystem.md:5-16`) | Extreme: drop-rate sales affect collection, dust, combat power, and prices simultaneously.                                                                           |
| Character/Essence/profession XP and mastery | Playing the relevant systems                                                    | Progression thresholds/caps                                                                                                                | Reflected in many public leaderboards                                          | High even when non-tradable because rankings and content access are competitive.                                                                                     |
| Potential                                   | Rolled on crafted equipment                                                     | Every Tempering attempt; negative outcomes can consume more                                                                                | Embedded in exact tradable equipment                                           | Extreme: paid restoration/protection manufactures superior market inventory.                                                                                         |

### Inflation, hoarding, manipulation, and abuse risks

1. **Cinder inflation remains the central monetary risk.** The 3% Marketplace fee is healthy, but it is the only clearly broad permanent Cinder sink found. Direct wires and purchases transfer supply. Paid Cinders, Cinder boosts, or sell-fee waivers must never be added.
2. **Market concentration can arise without direct currency sales.** Paid gathering yield, Essence drops, crafting success, offline rate, Dungeon entries, or extra action capacity all create sellable output and let a spender undercut or corner markets.
3. **Direct gifting increases alt-account leverage.** Guest accounts are restricted from transfers in the command contract, and account-risk/restriction foundations exist, but the current risk audit says Marketplace, botting, and progression coverage remains incomplete (`docs/liveops-account-risk-audit.md:234-246`, `:334-349`). Paid assets must be account-bound and non-giftable.
4. **Listing capacity is economic power.** The existing 10+10 order caps limit reach and operational load. Selling extra slots gives paying merchants more price coverage, inventory throughput, and manipulation capacity.
5. **Finite Potential is a healthy item sink and differentiator.** Paid restoration, outcome protection, or quality chance would inflate high-end equipment and invalidate the risk of Tempering.
6. **Tower Tokens can be hoarded because their reward sink is partial.** Do not monetize or multiply them until a stable, non-mandatory sink and telemetry exist.
7. **Currency fragmentation is already high.** Do not add multiple paid tokens, VIP points, pass points, and shop-specific currencies. If a paid currency is ever justified, use one cosmetic-only balance; direct EUR pricing is preferable at the initial scale.
8. **Gathering progression is currently under-rewarding and content-thin.** Its friction is a design problem, not a product to sell around.

### Legitimate friction versus artificial friction

Legitimate friction includes choosing one tool with one combat destination, finite equipment Potential, Dungeon Vigor and retreat decisions, consumable sigils earned through play, one active character action, Arena participation caps, snapshot locks, weekly vendor limits, and build preparation. These create tradeoffs, pacing, or integrity.

Artificial/unacceptable monetization friction would include adding inventory or Archive caps where none exist, reducing the free 24-hour window, slowing current crafting/gathering, withholding price history, making loadout editing deliberately tedious, introducing a paid repair loop, or making manual Rift attendance worse to sell auto-join.

## 4. Monetization-readiness assessment

| Dimension             | Readiness                  | Verdict                                                                                                                                                     |
| --------------------- | -------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Retention evidence    | **Unknown / not ready**    | No live cohort evidence was provided. Require stable D1/D7/D30 and return-frequency distributions before recurring monetization.                            |
| Content depth         | **Promising**              | Enough systems exist for long-term value, but later-region, seasonal, cosmetic, and tuning depth is uneven.                                                 |
| Social visibility     | **Moderate**               | Chat, Guilds, Marketplace, rankings, Tower, Raid, and Tournament results exist. This can support identity cosmetics once a shared cosmetic system is added. |
| Player identity       | **Moderate/low**           | Titles and records exist; general appearance/profile cosmetics do not. Build the surface before the catalog.                                                |
| Recurring engagement  | **Promising but unproven** | Daily/weekly timers exist. Whether they retain players is unknown.                                                                                          |
| Valuable convenience  | **Narrow**                 | Saved presets and absence recovery are defensible. Inventory, Archive, and queue capacity should not be manufactured as pain points.                        |
| Cosmetic surface area | **Not ready**              | Titles are real, but portrait/nameplate/banner/skin ownership and broad display are absent or partial.                                                      |
| Seasonal structure    | **Not ready for a pass**   | A monthly Tournament leaderboard is computed, but there is no coherent named season, durable record, reward track, or cross-system seasonal loop.           |
| Economy stability     | **Not proven**             | The fee and ledgers are strong foundations; faucet/sink, inflation, concentration, and price-index baselines are missing.                                   |
| Commerce operations   | **Absent**                 | No commercial product catalog, provider integration, entitlement ledger, refunds, reconciliation, VAT handling, or commerce support tooling.                |

**Readiness decision:** conduct monetization discovery and foundation work now; accept payments only after the prerequisite gate. A small supporter launch can precede subscription. Do not launch a pass or paid acceleration simply to test whether anyone will pay.

## 5. Acceptable and unacceptable paid advantages

### Enforceable boundary

Paid advantages are acceptable when all are true:

1. They do not raise expected rewards per active hour or per limited entry.
2. They do not add a simultaneous production/activity lane.
3. They do not change combat stats, abilities, targeting, drop/quality odds, or power ceilings.
4. They do not improve Marketplace capacity, priority, fees, automation, or information unavailable to free players.
5. They do not increase competitive attempts or bypass a participation/reward cooldown.
6. They are account-bound, non-tradable, non-giftable, and cannot seed an alt-account economy.
7. Expiration does not destroy, hide, or trap player-owned goods.
8. The benefit is accurately labeled; gameplay-affecting absence recovery is not called “purely cosmetic.”

Reasonable paid advantages:

- Permanent identity cosmetics and distinctly sourced supporter titles.
- Additional **saved** Essence/build/equipment presets, never additional active Essence slots or combat effects.
- Character rename after the existing free rename, with cooldown and name history.
- A 48-hour instead of 24-hour offline claim window, with the same hourly output and a hard cap.
- Future Rift auto-join only within the same free entry cap, reward formula, fair capacity reserve, and snapshot freshness rules.
- Cosmetic-only Guild heraldry and profile showcase capacity.

Advantages that should never be sold:

- Cinders, Soulstones, Fate Echo, Glory, Guild Favor/Supplies, Tower Tokens, Raid Trophies, sigils/fragments, dust, cores, catalysts, blueprints, Potential, equipment, or Essences.
- Additional active Essence slots, exclusive abilities, superior equipment, Doctrines, or combat-affecting subscription buffs.
- Better Essence/loot/rare-material drop rates; gathering/crafting yield; XP per active hour; quality/rarity odds; or Tempering protection/restoration.
- Additional Dungeon entries, PvP tickets, Tournament entries, Raid/Tower reward claims, daily objectives, or activity lanes.
- Marketplace listing/order capacity, reduced fees, priority, featured placement, auto-trading, private price data, or purchase with premium currency.
- Paid random rewards, gacha, loot boxes, or paid mystery caches.

### Where convenience becomes power

- A saved preset is convenience; an extra equipped slot is power.
- A notification is convenience; automatic market execution is power.
- A longer offline **cap** is bounded recovery; a faster offline **rate** is power.
- Reordering one existing queue is convenience; processing a second queue concurrently is power.
- A cosmetic Guild banner is expression; buying Supplies or a building level is shared power.
- Showing public price history is baseline UX; selling more listings, faster refresh, or hidden order data is market power.
- One free rename plus paid later renames is a service; selling identity that imitates earned prestige is deception.

### Non-paid paths

Every gameplay destination, combat effect, currency, resource, item class, Essence, blueprint, and power ceiling must have a non-paid path. A paid convenience need not have an identical free entitlement if free players can perform the same gameplay manually and at the same active rate. For the 48-hour offline cap, non-payers retain full output by returning within 24 hours; paying only recovers a bounded additional absence interval. Cosmetics can be purchase-only, but earned prestige cosmetics and paid supporter cosmetics must be visually and semantically distinct.

## 6. Monetization-model scorecard

Scales are directional: Fit, Revenue, Acceptance, Fairness, and Retention are **5 = strongest**. Complexity, Economy Risk, and Competitive Risk are **5 = highest/worst**. Scores are comparative hypotheses, not forecast precision.

| Model                                             | Fit | Revenue | Acceptance | Fairness | Retention | Complexity | Econ risk | Comp risk | Verdict                                                                                                                            |
| ------------------------------------------------- | :-: | :-----: | :--------: | :------: | :-------: | :--------: | :-------: | :-------: | ---------------------------------------------------------------------------------------------------------------------------------- |
| Optional membership                               |  4  |    4    |     4      |    4     |     4     |     5      |     2     |     2     | **Later.** Best recurring model if exact benefits remain narrow and operations are mature.                                         |
| One-time account upgrade                          |  5  |    3    |     5      |    5     |     3     |     3      |     1     |     1     | **Launch.** Permanent saved-preset capacity is legible and low risk.                                                               |
| Character/account services                        |  4  |    2    |     4      |    5     |     2     |     2      |     1     |     1     | **Launch carefully.** Paid renames after the existing free rename; avoid selling recovery from support failures.                   |
| Storage/loadout convenience                       |  4  |    3    |     4      |    4     |     3     |     3      |     2     |     2     | **Saved loadouts only.** Inventory/Archive limits do not exist and should not be invented.                                         |
| Essence convenience                               |  3  |    4    |     3      |    2     |     3     |     3      |     4     |     4     | **Mostly reject.** Presets are safe; drops, dust, cores, XP, and active slots are not.                                             |
| Crafting/gathering convenience                    |  2  |    4    |     2      |    1     |     2     |     3      |     5     |     5     | **Reject power levers.** Notifications/filters may be free UX; speed, yield, quality, protection, and extra queues are paid power. |
| Cosmetics/visual identity                         |  4  |    3    |     5      |    5     |     4     |     4      |     1     |     1     | **Build then launch.** Viability depends on visibility and art throughput, not genre convention.                                   |
| Profile/title/portrait/nameplate/social cosmetics |  5  |    4    |     5      |    5     |     4     |     4      |     1     |     1     | **Best identity surface.** Never imitate earned title/placement semantics.                                                         |
| Seasonal pass                                     |  2  |    4    |     3      |    3     |     3     |     5      |     3     |     3     | **Not ready.** No coherent season or reward-production cadence yet. Cosmetic-only later.                                           |
| Supporter/founder packs                           |  5  |    3    |     5      |    5     |     3     |     3      |     1     |     1     | **First offer.** Honest funding proposition; one per account; no resources.                                                        |
| Paid currency                                     |  2  |    4    |     3      |    3     |     2     |     5      |     4     |     3     | **Defer.** Direct EUR is clearer for a small catalog. If ever added, cosmetic-only and non-transferable.                           |
| Direct item purchases                             |  1  |    4    |     1      |    1     |     1     |     3      |     5     |     5     | **Reject** for gameplay items. Direct cosmetic entitlements are evaluated under cosmetics instead.                                 |
| Controlled progression acceleration               |  3  |    4    |     3      |    2     |     3     |     4      |     4     |     4     | **Only absence recovery now.** Do not sell active-rate boosts without evidence and new competitive policy.                         |
| Offline progression benefit                       |  5  |    4    |     4      |    4     |     5     |     3      |     3     |     2     | **Bounded membership benefit.** 48-hour cap, identical rate, monitored for resource and ranking effects.                           |
| Additional task/activity capacity                 |  1  |    5    |     1      |    1     |     2     |     4      |     5     |     5     | **Never.** One extra action lane multiplies combat/crafting/gathering production.                                                  |
| Auction House convenience                         |  2  |    4    |     2      |    2     |     3     |     4      |     5     |     4     | **Free baseline UX only.** Saved searches, alerts, history, and comparable prices should not be paywalled.                         |
| Guild-related purchases                           |  3  |    3    |     4      |    4     |     4     |     4      |     4     |     4     | **Cosmetic heraldry only.** Never sell shared resources, buildings, contribution, Raid/War entries, or buffs.                      |
| Advertising                                       |  2  |    2    |     2      |    3     |     1     |     3      |     3     |     3     | **Reject for launch.** Rewarded ads create the same economy problems as paid consumables; display ads damage a premium fantasy UI. |
| Gacha/loot boxes/randomized paid purchases        |  1  |    5    |     1      |    1     |     1     |     5      |     5     |     5     | **Never.** Existing Essence/gear randomness makes paid randomness especially corrosive and legally burdensome.                     |
| Paid power/exclusive gameplay abilities           |  1  |    5    |     1      |    1     |     1     |     4      |     5     |     5     | **Never.** Invalidates the signature collection/build system and competitive credibility.                                          |

The most deceptive “quality-of-life” ideas are extra Marketplace listings, extra activity capacity, Tempering protection, Essence drop rate, and Dungeon/PvP attempts. Each directly increases either expected tradable output or access to limited rewards.

## 7. Three strategic alternatives

### Alternative A — Conservative: “Support the Realm”

- **Target:** committed fans, collectors, and players who want to fund development without changing play.
- **Revenue model:** one-time Supporter Packs, à-la-carte identity cosmetics, paid rename after the free rename, and permanent saved-preset capacity. No subscription until cosmetics alone prove recurring demand.
- **Strengths:** clearest trust proposition; minimal economy/competitive integration; easiest rollback; compatible with realm-first prestige.
- **Weaknesses:** revenue is lumpy; requires visible cosmetic surfaces and continuing art; low spend ceiling; may underfund ongoing live operations.
- **Likely reaction:** positive among an alpha/community audience if pricing and use of funds are transparent. Cosmetics will underperform if rarely visible.
- **Non-payers:** experience no progression difference.
- **PvP/economy:** effectively unaffected; paid entitlements are bound and excluded from trade.
- **Operations:** moderate product/art load, lower economy load, but full payment/refund/support infrastructure is still required.
- **Sustainability:** defensible but may not create enough recurring revenue for a live RPG.
  **Failure modes:** weak cosmetic visibility, founder packs that feel like donations without value, overproduction of low-quality cosmetics, or “supporter” titles confused with earned prestige.

### Alternative B — Balanced: “Identity, Flexibility, and Time Respect”

- **Target:** fans plus regular adults who value build experimentation and cannot return every day.
- **Revenue model:** all Conservative products plus a delayed €7.99 membership offering a permanent monthly cosmetic choice, active identity treatment, and a 48-hour offline cap. Future Rift auto-join may join the membership only under equal-entry/equal-value rules.
- **Strengths:** creates recurring revenue with a game-specific benefit; respects the idle cadence; does not raise output per active hour; saved configurations reinforce Essence buildcraft.
- **Weaknesses:** the 48-hour window is still a real economic/progression advantage for irregular players; membership value is modest until cosmetic identity and Rifts exist; entitlement expiry requires care.
- **Likely reaction:** acceptable if described honestly as bounded absence recovery, not “no gameplay effect.” Some dedicated daily players may see little value, which is preferable to mandatory membership.
- **Non-payers:** retain the current 24-hour full-rate loop, every power path, equal attempts, and all gameplay content.
- **PvP/economy:** indirect progression impact only for >24-hour absences; no paid combat flags, entries, currencies, market capacity, or reward odds. Competitive embargoes suspend absence recovery for time-limited realm-first races if necessary.
- **Operations:** highest recurring billing/support burden; requires retention cohorts, renewal handling, grace periods, and economy monitoring.
- **Sustainability:** strongest balance of trust and recurring value for this game if retention exists.
  **Failure modes:** players perceive 48 hours as the “real” cap; the free cap is later degraded; cosmetics are not visible; monthly grants create FOMO; auto-join consumes entries poorly; subscription adoption becomes highly concentrated among top-ranked players.

### Alternative C — Aggressive: “Accelerated Legacy”

- **Target:** high-spend optimizers and time-poor competitive players.
- **Revenue model:** membership plus paid currencies, XP boosts, gathering/crafting bonuses, Essence-rate boosts, extra entries/tickets, and market capacity, with nominal caps and matchmaking by effective power.
- **Strengths:** high short-term monetization surface and payer spend ceiling; many existing systems expose obvious bonus hooks through `BonusKind`.
- **Weaknesses:** every lever compounds across crafting, trading, rankings, PvP, Tower, and Raids; matchmaking cannot repair Auction House domination or realm-first races; balancing becomes spend-tier balancing.
- **Likely reaction:** early revenue from a small cohort followed by distrust, “mandatory VIP” pressure, and spender/non-spender social segmentation.
- **Non-payers:** remain technically able to progress but become economically and competitively slower; their gathering/crafting output is devalued by boosted supply.
- **PvP/economy:** high integrity risk even with stat normalization because roster eligibility, gear access, collections, Guild resources, and market wealth remain accelerated.
- **Operations:** heavy live balancing, fraud, refund, pricing, offer rotation, economy, and support burden—poor fit for a solo developer.
- **Sustainability:** fragile. Revenue depends on escalating advantages and continual content invalidation.
  **Failure modes:** whales corner inputs/Essences, paid attempts dominate rankings, content is tuned around subscribers, non-payer churn, chargeback farming, multi-account funnels, and regulatory exposure from random products.

## 8. Recommended strategy

Choose **Alternative B, but launch it in Conservative order**.

1. Build commerce, entitlements, identity display, analytics, and support.
2. Launch a Supporter Pack, identity cosmetics, rename service, and permanent saved-loadout upgrade.
3. Observe at least two full D30 cohorts and economy baselines.
4. Only then launch Patron’s Oath with no resource bundle, XP multiplier, drop boost, market perk, extra entry, or extra active slot.
5. Add Rift auto-join only after Rifts exist and manual/free participation is healthy.

This is superior for LegendsLegacy because the distinctive product is long-term buildcraft in an interconnected economy. Selling output would monetize the exact systems whose meaning needs preservation. Pure cosmetics alone are not yet a complete strategy because cosmetic visibility is thin. Saved configurations and bounded offline recovery create game-specific value without changing active output or power ceilings.

### Hard portfolio limits

- At launch, show **one Supporter Pack family, one identity collection, one permanent account upgrade, and one service**. No rotating deal wall.
- One membership only. No VIP tiers.
- No paid currency at launch; display EUR prices directly.
- No consumable gameplay boosts.
- No “best value” comparison based on invented virtual-currency value.
- No discount countdown unless the deadline is real, infrequent, and announced in advance.

## 9. Detailed offer portfolio

“Launch” below means the first monetization release **after** prerequisite gates, not the current repository state.

### Launch priority 1 — Realm Supporter Packs

- **Receives:** three non-overlapping tiers: Wayfarer (€9.99), Warden (€24.99), and Founder (€49.99). Each contains a clearly sourced permanent supporter title, profile/nameplate treatment, chat flair, and cosmetic-only badge/portrait frame. Higher tiers include the lower-tier identity entitlements, not Cinders or gameplay items.
- **Buyer:** committed alpha/community supporters and collectors.
- **Value:** visible patronage and durable identity; transparent funding proposition.
- **Scope/duration:** account-wide, permanent, once per account per tier; offer the price difference as an upgrade path so buyers never repurchase included goods.
- **Frequency/limits:** one purchase of each tier; no gifting; no randomized contents.
- **Progression/combat:** none.
- **Non-paid path:** not required for supporter cosmetics. Earned titles/frames remain available through achievements, PvP, Tower, Raids, and events with distinct art/labels.
- **PvP/AH:** no stat or eligibility effect; entitlements and items are bound, absent from inventory transfer and Marketplace catalogs.
- **Abuse:** chargeback-driven badge use and resale attempts; mitigate with server entitlement state, revocation, and no trade/gift route.
- **Technical needs:** product/version catalog, account entitlements, title source classification, profile/chat display, pack-upgrade pricing, idempotent fulfillment, refund revocation, audit trail.
- **Metrics:** checkout conversion, tier mix, display/equip rate, refund/chargeback rate, support contacts, D30 retention by exposure/purchase—not as causal proof without experiment controls.
- **Change/remove if:** refund rate exceeds provider norms, players confuse supporter identity with earned prestige, equip rate stays below 20% after visibility is proven, or pack pressure harms new-player retention.

### Launch priority 2 — Legacy Identity Collection

- **Receives:** direct-purchase portrait frames, nameplates, chat flourishes, profile backdrops, and cosmetic title treatments. Do not sell an achievement title’s wording.
- **Buyer:** identity-focused regulars and collectors.
- **Value:** repeated visibility on character header/profile, chat, Guild roster, Marketplace seller/buyer identity where appropriate, and competitive/group results.
- **Scope/duration:** account-wide, permanent.
- **Price/frequency:** €2.99–€8.99 per piece; €11.99–€17.99 for a coherent bundle. No daily rotation; at most one small collection release per content beat.
- **Limits:** owned once; no duplicates; no loot boxes.
- **Progression/combat:** none; no gameplay tags hidden in cosmetics.
- **Non-paid path:** a healthy earned cosmetic catalog must coexist. Purchased and earned sets use separate source labels.
- **PvP/AH:** visual only; never affects targeting/readability; non-tradable.
- **Abuse:** impersonation, offensive combinations, and readability. Use bounded templates, moderation-safe naming, and contrast/accessibility checks.
- **Technical needs:** general cosmetic entitlement type, equip slots, preview, fallback, cross-surface renderer, content versioning, refund-safe unequip.
- **Metrics:** view-to-preview, preview-to-buy, equip rate, cross-surface impressions, repeat cosmetic buyers, refund reason.
- **Change/remove if:** cosmetics are rarely seen/equipped, UI readability declines, or production cost persistently exceeds contribution margin.

### Launch priority 3 — Archivist’s Loadout Library

- **Receives:** +3 saved Essence loadout presets, raising the current preset count from 3 to 6. When unified equipment/build presets exist, the entitlement may add saved configurations there, but never additional equipped slots or simultaneous effects.
- **Buyer:** build experimenters, Dungeon/Raid/Tower organizers, and PvP players managing multiple legal builds.
- **Value:** less destructive reconfiguration and more experimentation around the signature Essence system.
- **Scope/duration:** account-wide entitlement, permanent; initially applied to the one current character and future-proofed for any later characters.
- **Price/frequency:** €6.99–€9.99 once; one purchase; no recurring stacking.
- **Limits:** 6 total paid+free saved Essence loadouts in v1; review usage before increasing. Switching still obeys existing activity/snapshot rules.
- **Progression/combat:** no extra active Essence slots, attributes, abilities, inventory, XP, or mid-combat swapping. It improves flexibility, which can help preparation, but not the legal strength of a frozen snapshot.
- **Non-paid path:** 3 free saved loadouts and unlimited manual editing remain. Consider raising the free baseline if telemetry shows three is frustrating rather than strategic.
- **PvP/AH:** Colosseum/Tournament/Tower/Raid snapshots remain server-authoritative and locked; saved presets cannot replace a submitted build without the mode’s normal refresh action. No economy effect.
- **Abuse:** automation/macros around rapid switching. Enforce server-side activity/snapshot rules; no “swap after matchmaking” loophole.
- **Technical needs:** account entitlement lookup in the loadout-limit provider, over-limit handling, audit, and test coverage.
- **Metrics:** free-cap reach rate, purchase conversion among cap-reached players, preset creation/activation, mode diversity, support complaints.
- **Change/remove if:** it becomes required for ordinary play, paid owners show a material competitive win advantage after matching on power/tenure, or the free cap creates deliberate frustration.

### Launch priority 4 — Name Reforging

- **Receives:** one character rename after the existing free rename has been used.
- **Buyer:** retained players who regret or outgrow their name.
- **Value:** identity repair without abandoning long progression.
- **Scope/duration:** character-specific, permanent service.
- **Price/frequency:** €4.99; maximum one paid rename per 30 days.
- **Limits:** normal name validation/uniqueness; immutable old-name history for moderation, trades, support, and chat correlation.
- **Progression/combat:** none.
- **Non-paid path:** preserve the current one free rename (`LL/src/Core/Application/UseCases/Users/Commands/RenameCharacter/RenameCharacterCommand.cs:23-41`). Support-caused or safety-related corrections remain free at operator discretion.
- **PvP/AH:** rating, history, listings, orders, records, and ownership stay keyed by IDs; recent former name may be shown in support/audit surfaces, not used to evade reputation.
- **Abuse:** harassment evasion, impersonation, market reputation reset. Cooldown, history, moderation linkage, and restricted-account checks are required.
- **Technical needs:** service purchase entitlement/consumption, atomic rename+consumption, idempotency, audit, support override/refund policy.
- **Metrics:** purchase rate after free rename, validation failures, moderation correlations, refunds/support contacts.
- **Change/remove if:** rename is used to evade sanctions/trade reputation or support burden exceeds revenue.

### After stronger retention — Patron’s Oath membership

Detailed in section 10. Target €7.99/month, one membership tier, account-wide. It is the recommended recurring offer but **not a first-payment offer**.

### Future-system dependent — Guild Heraldry

- **Receives:** cosmetic Guild banner/crest frames, roster accents, and group-result treatment; never buildings, Supplies, mission progress, or member buffs.
- **Buyer:** Guild leaders and socially invested members.
- **Value:** shared identity visible in Guild, Tower, Raid, and future War results.
- **Scope/duration:** permanent Guild entitlement with purchaser and Guild audit records. Define a fair disband/transfer policy before sale; recommended: entitlement remains with the Guild, while the buyer retains a personal supporter receipt/badge.
- **Price/frequency:** €9.99–€19.99 per heraldry set; infrequent releases; no member-by-member stacking.
- **Limits:** one active visual set per Guild, multiple owned; role permission required to equip, not to own.
- **Progression/combat:** none.
- **Non-paid path:** earned Guild heraldry from missions, Tower/Raids, and future Wars.
- **PvP/AH:** visual only; never conveys War stats or trade discounts.
- **Abuse:** ownership disputes after leadership changes/disband, coercive fundraising, offensive combinations. Require clear ownership terms and support tools.
- **Technical needs:** Guild-scoped entitlements, permissioned equip, audit, leadership/disband lifecycle, moderation-safe compositing.
- **Metrics:** eligible-Guild conversion, member impressions, equip duration, disputes/refunds.
- **Change/remove if:** purchases drive Guild conflict, entitlement ownership is unclear, or social visibility is insufficient.

### Future-system dependent — Chronicle seasonal cosmetic track

This is not approved for current development. If section 11’s prerequisites are met, test one 8-week cosmetic track at €8.99, with no paid tiers/skips and no paid-track power rewards.

### Offers that should never be introduced

Direct gameplay items/currencies; paid random caches; additional active Essence or task slots; output/drop/quality multipliers; Tempering protection/Potential restoration; extra Dungeon/PvP/Tournament/Raid/Tower entries or reward claims; Marketplace slots/priority/fee reductions/automation; subscription-only abilities/equipment/Essences/blueprints/Doctrines; or paid Guild progression.

## 10. Subscription recommendation

### Patron’s Oath

**Positioning:** “Support ongoing development and make LegendsLegacy easier to fit around real life.” It is not VIP status and should not be marketed as required optimization.

**Price hypothesis:** €7.99/month including applicable VAT in consumer-facing display. No annual plan at first; annual billing raises refund, commitment, and liability complexity before churn is known.

**Exact benefits**

1. **One permanent monthly cosmetic choice** from a non-expiring supporter archive. The choice is granted as a durable account entitlement; unchosen cosmetics remain available in later months. No FOMO calendar.
2. **Active Patron identity treatment:** optional nameplate edge, profile badge, and chat flourish while subscribed. It must not imitate rank, staff, moderator, realm-first, or achievement marks.
3. **48-hour offline reward cap instead of 24 hours.** Encounter cadence, win calculation, drop chances, yield, XP, and currency rate are identical. The benefit only matters when the player returns after more than 24 hours.
4. **Future Rift auto-join, when and only when Rifts are implemented and healthy.** It consumes the same daily entries, produces the same expected value per entry, uses a current-enough snapshot, respects a manual-join capacity reserve, and never buys extra entries (`LL/docs/rift-system-design.md:695-748`). This is not part of the initial membership launch.
5. **No resource stipend, paid currency, XP/drop/yield bonus, crafting protection, extra active slot, market perk, Dungeon entry, Arena ticket, or Guild contribution.**

**What does not belong in the subscription**

- **Extra active or “backup” Essence slots:** never, if they affect combat. Inactive saved presets belong in the permanent Loadout Library, not a recurring hostage mechanic.
- **Loadout capacity:** one-time permanent upgrade, not subscription.
- **Inventory or Soul Archive capacity:** neither currently has a legitimate bounded capacity. Do not create one.
- **Storage:** no subscription storage. This also eliminates over-cap stored-item problems on expiry.
- **Offline duration:** yes, capped at 48 hours and explicitly treated as a gameplay-affecting absence benefit.
- **Progression percentage bonuses:** no.
- **Rift auto-join:** future membership convenience under equal caps/value; free manual attendance must remain good.

**Lifecycle and expiry**

- Account-wide; one active membership; no stacking, gifting, or family sharing until account policy exists.
- Voluntary cancellation retains benefits through the paid-through timestamp.
- Failed renewal receives a 72-hour grace period. Webhook state, not the client clock, determines access.
- Monthly cosmetic entitlements already earned remain permanently owned after expiry.
- Active Patron identity treatment unequips at expiry and falls back to the player’s selected owned/free treatment.
- The offline cap is evaluated at reward resolution. After expiry/grace, only the most recent 24 hours are eligible; never claw back already settled rewards.
- Future Rift auto-join stops scheduling new joins at expiry. Existing joined Rifts resolve normally; entries/rewards are never revoked merely because membership ended.
- No “items over capacity” exist because the subscription grants no storage. No saved builds are locked because loadout capacity is not a subscription benefit.
- Re-subscribing restores active benefits but grants no missed monthly cosmetics; it grants the current billing period’s choice exactly once.

**PvP and Auction House:** membership state never enters combat stat aggregation, matchmaking, reward formulas, ticket counts, listing/order caps, fee calculation, order priority, price information, or tradability. The 48-hour cap may indirectly change long-term progression; measure matched-tenure power and wealth cohorts. During a newly released realm-first race, the safest rule is to suspend all gameplay-affecting paid benefits for the published race window.

**Why it is valuable but not mandatory:** it combines recurring identity value with insurance against missing one daily return. A player who returns within 24 hours and does not care about supporter cosmetics loses nothing by skipping it. If top competitive players adopt it at very high rates or player research calls it “the real offline cap,” improve free scheduling/retention before adding benefits.

## 11. Seasonal-pass recommendation

**Do not launch a pass now.** Current Tournament points are grouped by calendar month, but the repository lacks a coherent named season, durable season record, broad seasonal activity, authored reward cadence, and proven content-production pipeline. Prophecies have economy/idempotency and tuning work remaining. A pass would currently be a monetized checklist laid across unrelated systems.

Prerequisites for reconsideration:

1. One stable named seasonal loop with a beginning, theme, end, durable record, and post-season review.
2. At least two successful free seasons with completion and catch-up telemetry.
3. Reliable event-driven objectives across combat, Dungeons, Crafting, Essence, Guild, and competitive systems.
4. A cosmetic entitlement/catalog pipeline capable of producing a coherent 8-week set without stealing core-content capacity.
5. Economy simulations proving the free track does not inflate resources.

If later approved:

- **Duration:** 8 weeks, not monthly. It matches idle progression and lowers constant reset pressure.
- **Free track:** cosmetics, earned titles, and only modest normal economy rewards already budgeted into seasonal faucet targets.
- **Paid track:** cosmetics and identity treatments only; no exclusive combat effect, currency bundle, core, catalyst, blueprint, Essence, equipment, or power.
- **Progression:** broad season XP from ordinary completed activity, with weekly caps that accumulate rather than expire. No purchase of levels or skips.
- **Catch-up:** missed weekly capacity rolls forward; late joiners can complete through normal play; retroactive premium unlock on purchase.
- **Objectives:** offer multiple equivalent categories and free rerolls. Never require rare random drops, market losses, inefficient gear, Guild membership, PvP wins, or disruptive activity swapping.
- **Expired rewards:** paid cosmetics return to a clearly labeled archive 6–12 months later at comparable pricing; earned prestige variants can remain date-stamped. No false exclusivity.
- **FOMO guardrail:** no daily paid-track task, no expiring unclaimed paid entitlement, and no “buy the last levels” countdown.

## 12. Competitive-integrity policy

Use one simple policy across modes:

> **Purchases cannot change match resolution, participation quantity, reward value per attempt, or market competition. Existing legal character progression remains usable unless a mode is explicitly normalized. Competitive rewards emphasize cosmetics and records, not accelerating resources.**

| Surface               | Policy                                                                                                                                                                                                                                                                                                   |
| --------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Colosseum rated PvP   | No paid tickets, refreshes, combat consumables, stats, Essences, abilities, or defense slots. Existing effective-power opponent selection and rating continue; snapshots must be invalidated correctly. Publish performance by spend cohort.                                                             |
| Tournament Grounds    | One account entry as implemented; no paid registration, reseeding, extra team/entry, retry, or reward. Frozen snapshots resolve identically. Add normalized special formats only if population supports them; do not create spender brackets.                                                            |
| Rankings/leaderboards | Progression boards are “open progression” and grant little/no power. Competitive seasonal boards must use equal participation rules. Display methodology. Never sell board placement, score multipliers, or extra scoring actions.                                                                       |
| Seasonal competition  | Suspend gameplay-affecting paid benefits during race scoring, or use normalized seasonal state. Cosmetic membership benefits remain. Do not sell pass levels or scoring boosts.                                                                                                                          |
| World/server first    | Publish a race window. Suspend the paid offline extension during that window, or make accounts that used it ineligible for first-only prestige before the attempt snapshot. Suspension for everyone is simpler and less punitive. Never sell preparation clicks, entries, roster slots, or Tower Tokens. |
| Guild wars (future)   | No paid attacks, defense refreshes, roster slots, Guild Supplies, buffs, repair, matchmaking preference, or score. Monetize heraldry only.                                                                                                                                                               |
| Raids/Tower groups    | No paid roster priority, Battle Plan simulations, reward upgrades, snapshot refreshes, Trophies/Tokens, or group capacity. One account per group remains.                                                                                                                                                |
| Auction House         | No paid listing/order slots, fee reductions, featured placement, priority, private price data, bot/API execution, or paid-currency settlement. Paid entitlements never enter inventory or trade. Public price history, alerts, and comparable prices are fair baseline tools.                            |

Separate payer/non-payer PvP brackets are not recommended: they fragment population, invite smurfing, and imply two games. Full stat normalization is also not the default because long-term buildcraft should matter. The credible solution for the recommended portfolio is to sell no direct power and keep attempts equal; use normalized events only for special prestige races where progression independence is the explicit fantasy.

## 13. Financial model

This model is parameterized. The example values illustrate formulas; they are **not forecasts or claims about LegendsLegacy’s audience**.

### Variables

- `M`: monthly active users (MAU)
- `D`: daily active users (DAU); `D/M` is stickiness
- `p`: unique monthly payer conversion rate
- `s`: active subscription adoption as a share of MAU (`s <= p`)
- `S`: consumer subscription price including VAT
- `q_j`: average monthly purchase units per MAU for one-time offer `j`
- `P_j`: consumer price of offer `j` including VAT
- `r`: refund/chargeback share of gross bookings
- `v`: effective VAT/sales-tax rate embedded in consumer price
- `f`: platform/payment fee rate
- `c`: variable operating/support cost per MAU per month
- `O`: fixed monthly operating/content/support cost
- `L`: expected active lifetime in months for a defined cohort

### Formulas

```text
Subscription gross bookings = M × s × S

One-time gross bookings = M × Σ(q_j × P_j)

Monthly gross bookings G = subscription gross + one-time gross

Revenue after refunds, indirect tax, and payment fees R
  = G × (1 - r) ÷ (1 + v) × (1 - f)

Monthly net operating contribution N
  = R - (c × M) - O

Gross ARPU = G ÷ M

Gross ARPPU = G ÷ (M × p)

Net revenue per MAU before fixed cost u
  = [(s × S + Σ(q_j × P_j)) × (1 - r) ÷ (1 + v) × (1 - f)] - c

Break-even MAU = O ÷ u, only when u > 0

Player contribution LTV = u × L

Payer contribution LTV
  = {[R - c × M] ÷ (M × p)} × payer active lifetime months
```

Do not subtract VAT twice. Confirm with an accountant whether provider reports are tax-inclusive, merchant-of-record, and net or gross of fees. Fixed per-transaction fees should be added explicitly once the payment mix is known; they can make €2–€3 offers unattractive.

### Hypothetical scenarios

All use direct EUR prices and a blended one-time offer rate/AOV. Payer conversion is deduplicated; subscription and one-time buyers may overlap.

| Input                     | Conservative |   Base | Optimistic |
| ------------------------- | -----------: | -----: | ---------: |
| MAU `M`                   |        5,000 | 20,000 |    100,000 |
| Unique payer rate `p`     |         1.5% |   3.0% |       5.0% |
| Subscription adoption `s` |         0.4% |   1.2% |       2.5% |
| Subscription price `S`    |        €7.99 |  €7.99 |      €7.99 |
| One-time units/MAU `q`    |         1.2% |   2.5% |       4.0% |
| One-time AOV `P`          |       €12.00 | €14.00 |     €16.00 |
| Refund/chargeback `r`     |         2.0% |   2.0% |       1.5% |
| Effective VAT `v`         |          20% |    20% |        20% |
| Payment fee `f`           |           5% |     5% |         4% |
| Variable cost/MAU `c`     |        €0.06 |  €0.08 |      €0.10 |
| Fixed monthly cost `O`    |       €1,500 | €3,000 |    €10,000 |

| Output                                   |   Conservative |          Base |     Optimistic |
| ---------------------------------------- | -------------: | ------------: | -------------: |
| Subscription gross                       |        €159.80 |     €1,917.60 |     €19,975.00 |
| One-time gross                           |        €720.00 |     €7,000.00 |     €64,000.00 |
| Total gross bookings                     |        €879.80 |     €8,917.60 |     €83,975.00 |
| After refund/VAT/fees, before operations |        €682.58 |     €6,918.57 |     €66,172.30 |
| Net after stated variable/fixed costs    | **-€1,117.42** | **€2,318.57** | **€46,172.30** |
| Gross ARPU                               |          €0.18 |         €0.45 |          €0.84 |
| Gross ARPPU                              |         €11.73 |        €14.86 |         €16.80 |
| Formula break-even MAU                   |         19,604 |        11,282 |         17,803 |

The non-monotonic break-even figures are intentional: each scenario assumes different conversion, basket, fees, and costs. This illustrates why MAU alone cannot predict sustainability. Replace every input with observed cohorts and actual provider/accounting data before making a budget.

## 14. Metrics and experiments

### Required analytics events

Use server events for authoritative commerce and economy state; client events only for impressions/navigation.

| Event                                                                     | Key properties                                                                                       |
| ------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| `store_viewed`                                                            | account/character cohort, source surface, catalog version, locale, experiment, device                |
| `offer_impression` / `offer_previewed`                                    | offer/version, displayed EUR/local price, VAT inclusion, eligibility, placement                      |
| `checkout_started`                                                        | purchase intent ID, offer/version, price/currency, provider, region; never raw card data             |
| `purchase_completed`                                                      | server purchase ID, provider transaction ID hash/reference, gross/tax/fee fields, fulfillment status |
| `purchase_failed` / `checkout_abandoned`                                  | normalized reason, provider stage, recoverable flag                                                  |
| `webhook_received` / `webhook_duplicate`                                  | provider event ID, type, signature result, processing result, attempt count                          |
| `entitlement_granted` / `consumed` / `revoked`                            | entitlement type/key/version, source purchase, effective/expiry timestamps, reason                   |
| `refund_requested` / `refund_completed` / `chargeback_received`           | purchase, offer, reason, amount, revocation/reconciliation status                                    |
| `subscription_started` / `renewed` / `cancelled` / `past_due` / `expired` | plan/version, period, cancellation reason, grace state, tenure                                       |
| `cosmetic_equipped`                                                       | entitlement source, surface, time owned                                                              |
| `offline_rewards_resolved`                                                | eligible elapsed hours, clipped hours, membership state, rewards by category, progression deltas     |
| `loadout_limit_reached` / `loadout_saved` / `loadout_activated`           | free/paid cap, mode context, snapshot refresh result                                                 |

Commerce data should join existing game/economy events through stable account and character IDs, not email or payment data in analytics payloads.

### Dashboards

1. **Conversion funnel:** store view → offer view → checkout → provider authorization → webhook → entitlement; split by offer/version, country, device, acquisition cohort, and experiment.
2. **Revenue:** gross/net, VAT, fees, refunds, chargebacks, ARPU, ARPPU, payer conversion, purchase frequency, revenue concentration, and cohort payback.
3. **Subscription:** starts, first renewal, renewal by tenure, voluntary/involuntary churn, grace recovery, cancellation reasons, monthly cosmetic selection, active-benefit use.
4. **Retention:** D1/D7/D30/D90 and return frequency split by payer status, pre-purchase behavior, and matched cohorts. Do not claim purchases cause retention from raw correlation.
5. **Progression velocity:** level, Essence tiers, Archive completion, Dungeon mastery, profession XP, equipment rating, wealth, and content unlock time by tenure/activity/spend cohort.
6. **Competitive integrity:** Arena win/rating, Tournament placement, Tower/Raid participation/firsts, and leaderboard top-percentile representation by spend cohort after matching tenure and activity.
7. **Economy health:** Cinders created/destroyed/transferred, balances by percentile, Gini/top-1% share, Marketplace fee destruction, price indices, spreads, volume, concentration by seller/buyer, item faucet/sink and velocity.
8. **Trust/support:** complaints by topic, refund/chargeback reasons, support response time, entitlement failures, duplicate webhooks, negative reviews/community sentiment.

### Guardrail metrics and initial review triggers

These are investigation triggers to recalibrate with real variance, not automatic shutdown rules.

- Non-payer D7 or D30 retention falls more than 5% relative to a comparable pre-launch/control cohort.
- Matched-tenure subscriber median combat power or liquid wealth exceeds non-subscribers by more than 10–15% and the gap is attributable to the offline extension.
- Spending remains a significant predictor of rated PvP outcome after controlling for effective power, level, build, tenure, and activity.
- Payers occupy leaderboard/competitive top 100 at more than twice their share among comparably active eligible players.
- Marketplace Cinder price index moves >15% in four weeks without a content explanation; top 1% wealth share rises >10 percentage points; or Gini rises >0.05.
- More than 50% of top-quartile competitive players subscribe, or more than 20% of cancellation/non-payer survey responses describe membership as mandatory.
- Offline-clipped reward value or membership-created sellable supply exceeds the planned faucet budget by >5%.
- Content completion time falls >20% below the designed band for the monetized cohort, reducing engagement with a whole tier.
- Refund+chargeback rate exceeds provider/category norms, entitlement failure exceeds 0.1%, or duplicate fulfillment is non-zero.

### Safe experiments

- A/B test storefront entry copy, preview layout, and whether prices appear on catalog cards.
- Test €7.99 versus €8.99 membership only after benefit demand is established; never vary price secretly between comparable users without a documented regional/experiment policy.
- Test Supporter Pack tier contents using cosmetics only, with fixed upgrade pricing.
- Test cosmetic styles and visibility surfaces before producing large collections.
- Test a 48-hour membership offline cap against a 36-hour variant while holding rate constant; monitor retention, clipped hours, supply, and perceived necessity.
- Run fake-door **interest measurement with no payment capture** for membership before implementation, clearly labeled “planned” and with no artificial countdown.

Do not test fake scarcity, preselected subscriptions, obstructive cancellation, variable loot odds, personalized exploitative pricing, loss-framed popups, or intentionally degraded free UX.

## 15. Phased roadmap

### Must have before accepting payments

1. **Product policy:** approve section 5’s boundary, catalog ownership rules, refund/revocation policy, guest-account policy, earned-versus-paid cosmetic taxonomy, and competitive embargo rules.
2. **Baseline instrumentation:** record at least 4–8 weeks of retention, return intervals, progression, faucets/sinks, wealth, Marketplace price/liquidity, competitive participation, and system engagement.
3. **Identity MVP:** account-bound cosmetic/title source, equip state, preview, and visibility on profile/header, chat, and group/competitive results. Do not launch a large art catalog first.
4. **Server-authoritative commerce:** product/price versions, Purchase, PurchaseLine, Entitlement, Subscription, Refund/Reversal, and immutable CommerceAudit records. The client can request checkout but cannot grant benefits.
5. **Idempotency:** unique internal purchase intent; unique provider transaction/event IDs; atomic fulfillment; replay-safe webhook inbox/outbox; safe retries and reconciliation jobs.
6. **Provider security:** signed webhook validation, server-side receipt/API verification where applicable, secret rotation, least privilege, and no payment details stored in game databases/logs.
7. **Refund/revocation:** revoke active cosmetics/services safely, never delete unrelated progression, preserve audit, and define consumed-service handling. Do not automatically ban every chargeback; review fraud patterns and use payment restrictions where proportionate.
8. **Support/LiveOps:** search purchases by account/provider reference, inspect entitlement history, re-run failed fulfillment idempotently, issue/revoke entitlements with audited reasons, record refunds, and export reconciliation.
9. **Account safety:** require guest conversion before checkout; verify ownership recovery path; prevent restricted/banned accounts from purchase or benefit abuse according to explicit policy.
10. **Legal/accounting review:** target-region VAT/sales tax, invoices, digital-content withdrawal/consent, subscription cancellation/renewal disclosures, minors, privacy/GDPR, chargebacks, regional pricing, Terms, and accounting treatment. This report is not legal or tax advice.
11. **Operational gate:** alerting for webhook backlog/failures, entitlement mismatch, revenue reconciliation, chargebacks, and provider outages; tested rollback/kill switches per offer and benefit.

### Launch phase

1. Invite-only or small-percent storefront rollout.
2. Sell Realm Supporter Packs first; add the small Identity Collection when visibility is proven.
3. Add Archivist’s Loadout Library and Name Reforging only after entitlement consumption/limits are tested.
4. No subscription, paid currency, pass, consumable, resource, or randomized offer.
5. Publish a plain-language monetization promise and changelog: what money can and cannot buy.
6. Review conversion, entitlement correctness, refunds, support, retention, and economy weekly.

### Post-launch validation

1. Complete two D30 cohorts and at least one normal weekly/monthly content cycle.
2. Compare payer/non-payer retention and progression with tenure/activity matching.
3. Audit Marketplace concentration, paid-entitlement transfer impossibility, and alt-account attempts.
4. Conduct player interviews on cosmetic visibility, loadout value, price clarity, and perceived fairness.
5. Tune or remove weak offers; do not compensate by adding resources.
6. Run refund, webhook replay, provider outage, account ban, and entitlement reconstruction drills.

### Later expansion

1. Launch Patron’s Oath only if D30 retention is healthy, monthly cosmetic production is sustainable, and the 48-hour simulation passes economy guardrails.
2. Add Guild Heraldry after ownership/disband/support rules and enough Guild visibility exist.
3. Add Rift auto-join to membership only after free Rifts establish entry use, manual fill, capacity, snapshot freshness, and reward equality.
4. Run two free seasons before deciding on the Chronicle cosmetic track.
5. Reconsider cosmetic-only paid currency only if catalog size and cross-platform/provider requirements make direct EUR checkout materially worse.

### Rollback plan

- Every offer and benefit has a server kill switch independent of client deployment.
- Stop new sales first; preserve valid owned entitlements while investigating.
- Disable the 48-hour extension prospectively, never claw back settled rewards, and compensate affected paid time.
- Keep webhook ingestion running during storefront outages so refunds/renewals settle.
- Reconcile provider transactions against internal purchases daily.
- If a benefit breaches economy/competitive guardrails, remove it from future periods, communicate clearly, offer refund/credit choices reviewed for local law, and replace with cosmetic value—not another power benefit.

## 16. Risks and failure modes

| Risk                              | Failure mode                                                                    | Mitigation/exit                                                                                                                     |
| --------------------------------- | ------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| Monetizing before retention       | Purchases briefly convert a small alpha audience while churn remains unsolved   | Gate subscription and catalog expansion on cohort retention and return-frequency data                                               |
| Cosmetics are invisible           | High art cost, low equip/use, weak willingness to pay                           | Build three recurring display surfaces first; ship a tiny catalog; measure impressions and equip rate                               |
| Offline cap becomes mandatory     | Players interpret 48 hours as the intended baseline; irregular non-payers churn | Never reduce 24 hours; test 36/48; monitor clipping and mandate perception; raise free cap if life-fit is a broad retention problem |
| Indirect market power             | Offline recovery adds materials/Cinders and widens wealth gaps                  | Hard 48-hour cap, same rate, faucet attribution, wealth/price guardrails, competitive embargo windows                               |
| Loadouts become combat power      | Paid presets bypass snapshot locks or enable post-match swaps                   | Server snapshot authority, normal refresh rules, no mid-combat switch, paid performance monitoring                                  |
| Prestige confusion                | Paid title looks earned or staff-like                                           | Explicit `Source: Supporter`, separate icon language, prohibited words/colors, audit catalog                                        |
| Founder-pack regret               | Later bundles undercut early supporters or imply exclusivity falsely            | Permanent upgrade pricing, truthful availability, no gameplay contents, documented rerelease policy                                 |
| Subscription hostage design       | Expiry locks storage/builds or deletes value                                    | No storage/loadout capacity in subscription; monthly cosmetics permanent; graceful cosmetic fallback                                |
| Economy manipulation/alt funnels  | Paid or recovered output moves through direct transfers and market              | Paid goods bound; account restriction integration; ledger attribution; rate/relationship alerts; guest checkout blocked             |
| Chargebacks and entitlement drift | Benefit remains after refund or legitimate player loses unrelated progress      | Idempotent revocation by source purchase, commerce audit, support review, no destructive blanket rollback                           |
| Operational overload              | Solo developer spends more time on billing/support/art rotations than game      | Small catalog, one membership, no offer calendar, provider/MoR evaluation, automated reconciliation                                 |
| Legal/tax exposure                | Incorrect VAT, cancellation, minors, digital goods, or random-reward treatment  | Qualified jurisdiction-specific review; avoid paid randomness; clear renewal/cancellation and regional pricing                      |
| Scope creep                       | Every system gains a premium shortcut and currencies multiply                   | Portfolio cap, annual policy audit, owner sign-off for any gameplay-affecting benefit                                               |

## 17. Explicit do-not-monetize list

Do not monetize:

1. Active Essence slots, abilities, ascension/evolution power, drop rate, pity gain, dust, cores, catalysts, or exclusive Essences.
2. Equipment, blueprints, materials, Potential, quality/rarity chance, Tempering protection, negative-outcome reduction, or finished best-in-slot items.
3. Gathering yield, rare chance, XP, node access, tools with superior stats, or active combat output.
4. Cinders or any current gameplay currency; currency exchange; debt; fee waivers; or cash-to-player trade.
5. Additional character action lanes, parallel Crafting/Combat, faster cadence, or extra queue processing.
6. Dungeon sigils/entries, Vigor recovery, route information, reward retention, or extra reward claims.
7. Arena tickets, extra opponents/refreshes, rating/Glory, Tournament entries/retries/seeding, or competitive score.
8. Tower/Raid entries, roster priority, preparation/scouting clicks, Battle Plan simulations, Tokens/Trophies, or first-clear eligibility.
9. Guild Favor/Supplies, missions/orders, buildings, member/roster capacity, buffs, Raid/War entries, or contribution.
10. Marketplace listing/order capacity, fee reductions, priority, visibility, private data, auto-trading, or paid currency settlement.
11. Inventory or Soul Archive capacity created by adding a new free-player cap.
12. Achievement, rank, season, realm-first, Guild, Raid, or Tower titles that falsely imply the player earned them.
13. Paid loot boxes, gacha, mystery chests, randomized cosmetic purchases, or paid keys.
14. Subscriber-only gameplay content, regions, Dungeons, Essences, equipment, blueprints, Doctrines, abilities, or endings.
15. Rewarded ads that grant gameplay value, revive attempts, or multiply idle returns.

## 18. Assumptions and unanswered questions

### Assumptions

- The current intended business model is free-to-play, not a paid box or mandatory subscription.
- One account currently centers on one character; account-wide entitlements should nevertheless be modeled separately from character state.
- Consumer-facing EUR prices include VAT where required.
- Existing in-game items and currencies remain non-cash-out and have no operator redemption value.
- Infrastructure remains suitable for a solo developer, favoring few products and server-owned rules.
- Repository implementation does not guarantee production deployment; all launch gates require environment verification.

### Unanswered questions

1. What are current D1/D7/D30/D90 retention and median return intervals?
2. How many players exceed the 24-hour offline cap, how often, and what reward value is clipped?
3. Which pages/modes drive repeat sessions and which are rarely used?
4. What are Cinder faucet/sink, balance percentile, and Marketplace price/volume trends after the 3% fee?
5. How concentrated are Essence ownership, high-end equipment, Arena rating, Guild contribution, Tower/Raid participation, and leaderboard placement by tenure/activity?
6. Which three cosmetic surfaces can be made naturally visible without cluttering the game UI?
7. Can the solo development/art cadence sustain one worthwhile permanent cosmetic choice per subscriber month?
8. Which countries, platforms, payment provider or merchant of record, legal entity, and refund/support SLA are intended?
9. Will alpha progress persist, reset, or merge into release? Selling permanent identity before a reset requires explicit continuity terms.
10. Are World Tower/realm-first releases expected after monetization, and can a paid-benefit embargo be enforced and communicated?
11. Should one supporter pack remain permanently available, or is a genuine founder deadline planned? Avoid “founder” wording if there is no meaningful founding period.
12. Does the owner want the Marketplace to be a central endgame or a supporting exchange? The answer affects tolerance for any indirect production advantage.

## 19. Final prioritized action list

1. Adopt the monetization boundary in section 5 as a product rule and test every proposed benefit against expected output, limited-entry value, market reach, and combat ceiling.
2. Instrument retention, return intervals, progression, economy, market concentration, and competitive outcomes before building offers.
3. Decide commerce jurisdictions/provider model and obtain qualified tax, consumer-law, privacy, minors, and accounting review.
4. Design server-authoritative purchase, entitlement, subscription, refund, webhook, audit, and reconciliation models.
5. Require guest conversion and prove account recovery before checkout.
6. Build a small identity system visible on profile/header, chat, and group/competitive results; preserve earned-prestige semantics.
7. Implement LiveOps commerce support and rehearse duplicate webhook, refund, chargeback, outage, ban, and reconstruction scenarios.
8. Launch only Supporter Packs first, then identity cosmetics, permanent loadout capacity, and paid post-free rename.
9. Review two D30 cohorts and economy/competitive guardrails before launching Patron’s Oath.
10. Keep seasonal passes, paid currency, Guild heraldry, and Rift auto-join deferred until their explicit prerequisites exist.

## Owner decision checklist

1. **Boundary:** Will you formally prohibit paid output rate, reward odds, limited entries, market capacity, and combat ceilings—even if those would monetize well?
2. **Readiness:** What retention and economy thresholds must be met before the first payment and before subscription?
3. **Audience:** Is the first commercial launch for a small supporter community, a broad F2P audience, or an existing alpha population whose data may reset?
4. **Identity:** Which three surfaces will make purchased cosmetics visible without weakening the game dashboard or earned prestige?
5. **First catalog:** Which Supporter Pack tiers, permanent preset upgrade, and rename policy will launch—and which tempting resource fillers are explicitly excluded?
6. **Membership:** Is €7.99/month with permanent monthly cosmetics and a 48-hour cap valuable enough, or should membership wait for Rifts rather than acquire stronger power perks?
7. **Competition:** Will new realm-first race windows suspend the offline extension, and which leaderboards are “open progression” versus normalized prestige competition?
8. **Economy:** What Cinder inflation, wealth concentration, price-index, and paid-cohort progression guardrails trigger rollback?
9. **Operations:** Which payment/merchant-of-record provider, target countries, customer-support process, refund policy, and entitlement SLA can you realistically operate?
10. **Seasonality:** Are you willing to run two complete free seasons and build durable seasonal records before deciding whether a cosmetic Chronicle track is justified?
