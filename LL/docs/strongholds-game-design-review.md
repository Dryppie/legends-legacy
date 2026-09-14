# Strongholds Game-Design Review

> **Direction update — 14 September 2026:** The recommendation in this review overcorrected toward a cosmetic legacy estate. The revised direction makes the Stronghold a bounded, mechanical progression and loot-direction system, and moves the standalone Soulstones experience into its buildings. See [Strongholds Mechanical Progression Revision](strongholds-mechanical-progression-revision.md). Where the two documents conflict on direct/reward benefits, building activation, the Arsenal, Essence integration, Soulstones, or the MVP, the revision supersedes this review.

Review date: 2026-09-14
Reviewed proposal: `LegendsLegacy_Strongholds_Game_Design.md`

This review treats the proposal as unapproved design material. Repository statements below describe the current checkout, not older plans or migration history.

## 1. Executive Verdict

**Conditional approval of the fantasy; rejection of the current feature definition.**

A personal Stronghold can improve LegendsLegacy, but the attached document does not yet define a compelling game system. It defines a theme, a Main Hall, and 39 possible labels for interfaces. Most proposed buildings either duplicate systems that are already mature, depend on systems that do not exist, or would create passive timers, compulsory bonuses, and menu sprawl. Building all or even half of them would make Strongholds expensive and unfocused without making them deep.

The idea is worth pursuing only if it is reframed as a **legacy estate**: a sparse, visibly evolving personal headquarters that turns existing accomplishments and Cinder wealth into permanent, mostly expressive development. Its central verbs should be **restore, choose, commemorate, and display**. It should not be a second inventory, second quest board, second archive, second Colosseum, or NPC timer game.

The key recommendation is:

> Launch a five-stage Main Hall, a permanent Gallery of Legacy, and four optional showcase wings competing for two visible plots. Progress through Cinder investment plus broad, player-chosen legacy deeds. Add no new spendable currency, no timers, no direct combat power, no resource generation, no daily interactions, and no NPC expeditions.

That version earns development time because it supplies something the current game lacks: one coherent visual record of a character's long journey. The current “many buildings that expose useful services” version does not.

## 2. What the Existing Game Actually Supports

### Implemented now

| System                         | Repository-backed state                                                                                                                                                                                                                                                                                                                           | Stronghold implication                                                                                                                                                                                      |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Character progression          | Character XP/level, base and combat attributes, power rating, equipment, Essences, titles, Combat Styles, and Soulstone upgrades are implemented. Character level uses an authored quadratic curve.                                                                                                                                               | Stronghold must not become another dense numerical level tree or another source of mandatory stats.                                                                                                         |
| Account/character scope        | The current authentication/repository flow resolves a single character with `FirstOrDefault` for a user, but the database index on `Character.UserId` is not unique. Achievements and titles already model account, character, seasonal, and server scopes.                                                                                       | Decide ownership explicitly before any multi-character evolution. Character-owned is the best fit for the stated fantasy.                                                                                   |
| Regions and areas              | Two authored regions (Shenic and Meran), 16 areas, and 106 creature definitions are present. Area access can depend on level, quests, and shared Tower progress. The progression policy anticipates ten regions, but only two are authored.                                                                                                       | Region commemoration is viable; a map-management building would be premature.                                                                                                                               |
| Idle combat                    | Repeating automated encounters, 10-second cadence, up to 24 hours of free offline resolution, detailed event logs, entity/ability statistics, damage types, healing, barriers, threat, stagger, and loot settlement are implemented.                                                                                                              | Baseline analytics already belong to combat. A Stronghold can later offer controlled testing, not confiscate existing statistics.                                                                           |
| Combat Styles                  | Styles have ten mastery levels, refinements, opening techniques, upgrade slots, and activity-aware build boundaries.                                                                                                                                                                                                                              | This major progression system is absent from the proposal. It fits a future Proving Grounds better than a separate generic Training Grounds buff track.                                                     |
| Equipment acquisition          | Ordinary regional combat drops equipment and sigils; Dungeons drop equipment, variants, and blueprints. Equipment has tier, rarity/quality, rank, variants, provenance, and ownership.                                                                                                                                                            | The proposal correctly notices loot surplus, but its Salvage Yard is already obsolete.                                                                                                                      |
| Equipment disposal and utility | Dismantling already converts equipment into bound Reinforcement Parts; equipment can be reinforced with Parts and Cinders, compared, favorited, traded through the Cinder Bazaar when eligible, donated/borrowed through the Guild Vault, and saved in activity-aware loadouts. Free players have three equipment presets; Nobility provides six. | Salvage, comparison, storage, filtering, and loadouts must remain normal inventory features. An Arsenal can survive only as a collection/display wing.                                                      |
| Inventory                      | Inventory items support quantities, new/seen state, favorites, transfers, selection crates, and no evident slot-capacity model. Loot history is separately persisted.                                                                                                                                                                             | A Vault solving “storage” has no current problem to solve.                                                                                                                                                  |
| Essences and Soul Archive      | Soul Archive, Creature Archive, Essence Codex collections, creature focus, absorption, dismantling to Essence Dust, leveling, ascension, evolution, favorites, activity-aware loadouts, and one-to-ten level-derived attunement slots are implemented.                                                                                            | Nearly every utility claimed by Essence Sanctum already exists. Keep the core UI intact; a Stronghold wing may only showcase or frame advanced experiments.                                                 |
| Soulstones                     | Soulstones drop from combat and feed a permanent, resettable constellation upgrade tree.                                                                                                                                                                                                                                                          | This is already a major permanent power system. Stronghold power would stack another compulsory layer on top.                                                                                               |
| Dungeons                       | Four Dungeon families are authored. Runs have sigil entry costs, branching routes, Vigor, rest sites, Treasuries, minibosses, bosses, pending/secured loot, failure analysis, completion records, leaderboards, and ten-level family mastery.                                                                                                     | A Dungeon Archive is redundant; mastery and trophies can feed the Gallery.                                                                                                                                  |
| World Tower                    | Fifteen released floors, server-wide floor state, scouting, preparation contributions, player rallies called **Expeditions**, stored playback, personal expedition history, Hall of Fame, titles, and Tower Token awards are implemented.                                                                                                         | “Expedition Lodge” creates naming and mental-model collision. Tower Tokens currently have award paths but no observed spend path; do not appropriate them for Strongholds before Tower economy is resolved. |
| Colosseum / PvP                | Direct arena battles, rating tiers, tickets, defense snapshots, Glory, battle history, Champion's Market, and automated Tournament Grounds with brackets, teams/loadouts, replays, rewards, history, and Hall of Fame are implemented in current code.                                                                                            | Arena and Strategy Chamber proposals duplicate an already broad activity. PvP results should be display inputs only.                                                                                        |
| Guilds                         | Membership, roles/permissions, invites, public profiles, rankings, chat, shared Vault, weekly missions, personal daily orders, Guild Favor shop, Guild Supplies, and immediate building construction/upgrades exist.                                                                                                                              | Personal buildings must not duplicate cooperative headquarters functions. Several proposed Stronghold names already exist as Guild buildings.                                                               |
| Achievements and titles        | 82 achievement definitions and 47 title definitions are authored across combat, Essences, Dungeons, Colosseum, Raids, Tower, and hidden/general categories. Points, recency, near-completion, equipping, and multiple scopes exist.                                                                                                               | These are the best data source for Stronghold deeds and displays; do not create a parallel Renown progression ledger.                                                                                       |
| Prophecies                     | Daily/weekly offers, multiple slots, acceptance, rerolls, Fate Echo spending, completion rewards, Prophetic Favor milestones, caches, and explicit claim flows exist.                                                                                                                                                                             | The game already carries recurring checklist pressure. Strongholds should not add another reset cadence or a Hall of Prophecy menu.                                                                         |
| Quests and events              | Onboarding, region chains, side quests, choices, pinning, combat encounters, event quests, contributions, milestones, and rewards are implemented.                                                                                                                                                                                                | Stronghold “projects” must not become a second quest journal. Use broad historical deeds, not more daily objectives.                                                                                        |
| Raids                          | Two asynchronous public Raid bosses are authored, with tiered rosters, three preparation parties, snapshots, background resolution, replay, graded outcomes, trophies, vendors, weekly reward reduction, leaderboards, and realm-first title support. The frontend currently hides Raids in production through an environment feature flag.       | Raids are implemented-but-not-production-exposed, not merely hypothetical. Strongholds should commemorate Raid feats, not own Raid preparation.                                                             |
| Region Boss                    | One shared Region Boss with matchmaking, snapshots, development/progression worker support, playback, scaling, and reward definitions exists.                                                                                                                                                                                                     | Another valuable trophy source; not grounds for a Guardian Hall.                                                                                                                                            |
| Marketplace and transfers      | Cinders support player-to-player marketplace orders and direct wiring, with fees and account-risk analysis.                                                                                                                                                                                                                                       | Cinder Stronghold costs are a real economy sink, but accomplishment gates are needed so wealth transfers cannot purchase “legacy” by themselves.                                                            |
| Leaderboards and social shell  | Character/Guild boards cover level, Soul Archive, achievement points, Dungeon mastery/clears, Raids, arena, tournaments, and Guild contribution. Public character lookup and public Guild views exist.                                                                                                                                            | A later static Stronghold inspection card is feasible and valuable; a walkable visitation system is not justified.                                                                                          |
| Nobility                       | Paid/entitled benefits include longer offline duration, more loadouts, ticket capacity, faster creature-focus changes, market limits, and rerolls. Historical daily resource grants are explicitly retired.                                                                                                                                       | Do not repeat the convenience-pressure pattern by selling Stronghold project slots, mechanical plots, or completion speed.                                                                                  |

### Partially implemented or constrained

- Content breadth is much smaller than the architectural horizon: two of ten anticipated regions, four Dungeon families, one Region Boss, fifteen Tower floors, and two Raid bosses.
- Raids are substantial in backend and frontend code but disabled in the production frontend environment.
- Tower Tokens are earned, but this review found no player spend path.
- Several Guild buildings are purchasable shells. Guild Hall, Mission Board, and Market Office have real effects; Treasury is a cost discount; Workshop, Training Grounds, Essence Sanctum, Raid Hall, and War Room contain substantial future/placeholder promises. This is a direct warning against repeating “building first, gameplay later” personally.
- Some cosmetic purchase records exist without a fully general banner/cosmetic entitlement and presentation system.
- Combat analytics are rich per encounter, but a player-facing longitudinal comparison lab is not evident.

### Planned or absent

- Strongholds themselves.
- Meaningful NPC gameplay: the only domain NPC type is an empty subclass. There is no visitor, retainer, companion, merchant-residency, relationship, expedition-agent, faction, or diplomacy system.
- Guild wars.
- Pets, mounts, familiars, and a Menagerie parent system.
- Faction politics, embassies, territory, and diplomacy.
- A general relic/artifact collection distinct from equipment, Essences, titles, and existing inventory items.
- Walkable housing or social visitation technology.

### Obsolete remnants that must be ignored

Crafting and Gathering references survive in historical migrations and old documents, but active Core, API, service, and Angular gameplay code no longer exposes those loops. Strongholds must not resurrect them as timber, stone, smithing queues, workshops, gathering expeditions, or material production.

### Important systems omitted by the proposal

Combat Styles, Quests, event quests, Region Bosses, Raids, Marketplace trading, Guild missions/buildings, leaderboards, Nobility, equipment reinforcement/variants/blueprints, Creature Archive/focus, and the existing rich combat-stat UI all materially affect the design. The proposal's broadness is partly caused by designing around abstract feature names rather than this actual system map.

## 3. What Problem Strongholds Solve

The game already has many progression bars. Strongholds should not exist merely to add another. Their defensible purpose is narrower:

1. **Unify a fragmented legacy.** Achievements, titles, Dungeon mastery, Tower feats, Raid results, PvP records, rare equipment, and the Soul Archive live in separate pages. No one place communicates “this is the history of my character.”
2. **Turn wealth into visible permanence.** Cinders already circulate through upgrades, variants, trading, and fees. A high-end non-power sink can remove wealth while producing an enduring visual result.
3. **Support expression without combat inflation.** The current game has build choices, but limited persistent spatial/visual identity. A Stronghold can make chosen interests visible.
4. **Give major accomplishments an afterlife.** A title can be equipped one at a time; a Stronghold can curate several achievements into a coherent story.

“Personal headquarters” is a strong-enough fantasy only if the headquarters visibly changes and reflects player choices. A mostly menu-driven directory of shortcuts is not. Without strong visual staging and curation, this feature is peripheral and should not be built.

Could the same benefits be achieved more simply? A public character profile plus expanded achievement page could deliver much of the record/inspection value at far lower cost. Strongholds justify their extra scope only through the combination of visible restoration, long-term economic investment, and spatial curation. If visual development is not affordable, build the profile first and postpone Strongholds.

## 4. Core Loop Analysis

### Loop implied by the proposal

```text
Play existing content
→ accumulate Cinders, surplus loot, records, and accomplishments
→ open Stronghold menus
→ spend resources / start projects / configure buildings
→ wait or claim
→ receive utilities or bonuses
→ repeat
```

This is weak. Existing content supplies all interesting actions, while the Stronghold becomes a tax booth between activities. If utilities or power are good, visits become mandatory. If they are weak, players ignore the feature. Timers, visitors, expeditions, and project slots then tempt the design toward claim-and-restart chores to manufacture engagement.

### Recommended loop

```text
Play whichever existing systems the player enjoys
→ earn permanent, broad legacy deeds automatically
→ periodically choose a restoration goal
→ contribute Cinders when ready
→ complete a deed-backed restoration milestone immediately
→ see the estate and chosen wing visibly change
→ curate what the Stronghold communicates to other players
```

Important differences:

- Stronghold progress is event-driven and historical, not a parallel activity grind.
- There is no passive clock, claim chest, generated resource, or daily reset.
- The decision is which part of the legacy to express next, not which percentage buff is compulsory.
- A player may ignore the Stronghold for weeks without losing value; accomplishments continue to qualify in the background.
- Late-game relevance comes from new deed/display content, not endlessly escalating combat bonuses.

The unavoidable weakness is that a non-power Stronghold will not be a high-frequency feature. That is acceptable. It should be a memorable periodic destination, not a daily retention mechanic.

## 5. Main Hall Review

The Main Hall is the strongest concept in the proposal, but it should use **five named stages**, not a long numerical level track:

1. Ruined Outpost
2. Restored Keep
3. Fortress
4. Great Citadel
5. Legendary Seat

Main Hall progress should represent the character's transition from survivor to established legend. Each stage requires:

- a broad career gate such as level or regional chapter completion;
- a substantial Cinder contribution;
- a choice of several legacy deeds from different systems; and
- completion of at least one or two prior wing restoration milestones.

Players should never be required to clear every major mode. At a stage requiring three deeds, offer at least six across PvE, PvP, collection, social, and general achievement paths. “Reach Tower floor X” and “reach arena tier Y” may be alternatives, not cumulative requirements.

The Hall should unlock:

- visual estate stages;
- one initial and eventually two visible optional-wing plots in the MVP (a third only in a later expansion);
- eligibility to restore higher wing ranks;
- more featured display positions;
- cosmetic facade/theme options;
- later, read-only social inspection.

It should **not** control baseline loadouts, inventory, filtering, archives, combat statistics, fast travel, Dungeon access, Raid access, PvP access, rewards, NPC timers, or direct combat bonuses. It also does not need project-capacity levels when the recommended system has one focused restoration goal and no clock queue.

To prevent blind Hall rushing:

- sparse stages are hard-gated by optional deed sets rather than raw Cinders alone;
- advancing requires some developed side-wing identity;
- side wings provide the visual and curatorial detail, while the Hall provides scale;
- no Hall stage increases combat/reward efficiency, so rushing is not mathematically mandatory.

This model differs from character level because it is sparse, composite, choice-based, and representational. The character gets stronger through play; the Hall becomes grander because the player chooses how to memorialize that play.

## 6. Building-by-Building Audit

The audit includes every named core and future building. “Merge” means the fantasy may survive, but not as its own building.

| Building               | Classification                                                               | Verdict                                                                                                                                                                                                                                                                            |
| ---------------------- | ---------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Trophy Hall            | **Core — Strong candidate for launch**                                       | Keep permanently, but broaden and rename it **Gallery of Legacy**. It should curate selected achievements, titles, boss/Tower/Raid/Dungeon feats, and a few favorite collectibles without duplicating their management pages.                                                      |
| Salvage Yard           | **Redundant — Existing game systems already solve this**                     | Equipment dismantling already exists, returns Reinforcement Parts, protects favorites, and is part of the equipment upgrade loop. Moving it would split baseline loot disposal and destabilize an existing closed economy.                                                         |
| Arsenal                | **Good — Worth keeping but not essential initially**                         | Keep only as an equipment collection/appearance and favorite-item display wing. Loadouts, comparison, filters, reinforcement, and storage stay in Inventory.                                                                                                                       |
| Essence Sanctum        | **Good — Worth keeping but not essential initially**                         | Viable as a Soul Archive showcase and future no-reward experiment space. Absorption, dust, ascension, evolution, focus, Codex, and loadouts already belong to the core Essence page. Avoid confusion with the existing placeholder Guild building of the same name.                |
| Training Grounds       | **Good — Worth keeping but not essential initially**                         | A controlled, rewardless test encounter could fill a real gap. Existing combat already exposes detailed statistics, so this should add fixed scenarios and comparison—not hide analytics. Rename **Proving Grounds** to avoid Guild overlap. Defer due to simulation/backend cost. |
| War Room               | **Merge — Overlaps enough with another building**                            | Merge with Strategy Chamber and Intelligence Office into Proving Grounds or a single **Campaign Room** for saved encounter dossiers, test results, and personal best comparison. It must not reproduce every combat page.                                                          |
| Expedition Lodge       | **Cut — Actively harms or bloats the concept**                               | The World Tower already calls player rallies Expeditions. NPC timers would add a second idle lane, inflation, claim/restart behavior, and a confusing duplicate term without an NPC system.                                                                                        |
| Library                | **Merge — Overlaps enough with another building**                            | Merge world lore, records, monsters, Dungeons, Prophecies, and maps into the existing Creature/Soul Archives or the Gallery's discovery displays. A broad “everything database” is a menu, not a building.                                                                         |
| Shrine                 | **Weak — Does not currently justify a building**                             | Selectable blessings become compulsory loadout power and pre-content maintenance. Spiritual cosmetics can be a theme; character customization needs a better parent system.                                                                                                        |
| Guest Quarters         | **Future — Strong idea, but dependent on systems/content not justified yet** | Residency could create narrative identity, but no visitor/relationship/merchant-residency system exists. Daily visitors and rotating merchants would be chore/FOMO traps.                                                                                                          |
| Observatory            | **Merge — Overlaps enough with another building**                            | Combine with Watchtower and the informational portion of Intelligence Office. Tower and Prophecy pages already expose their own state. Only a later world-event overview could justify one consolidated facility.                                                                  |
| Watchtower             | **Merge — Overlaps enough with another building**                            | Same informational territory as Observatory/Intelligence Office. “Incoming opportunities” implies timers and login pressure; do not make it a separate feed.                                                                                                                       |
| Vault                  | **Redundant — Existing game systems already solve this**                     | Inventory has no evident capacity problem; Guild Vault serves shared logistics. Essential storage and organization should remain baseline.                                                                                                                                         |
| Hall of Records        | **Merge — Overlaps enough with another building**                            | Character history, milestones, and statistics belong in Gallery of Legacy and existing achievement/leaderboard records.                                                                                                                                                            |
| Chapel                 | **Merge — Overlaps enough with another building**                            | Merge with Shrine as an architectural theme. Resurrection/cleansing systems do not exist and should not be invented to validate a building.                                                                                                                                        |
| Barracks               | **Future — Strong idea, but dependent on systems/content not justified yet** | Retainers or companions could justify it later, but no personnel system exists. Defensive-force simulation would be a new game, not an MVP wing.                                                                                                                                   |
| Monster Hall           | **Merge — Overlaps enough with another building**                            | Creature Archive already records kills, focus, locations, tags, and Essence discovery. Feature selected hunts in the Gallery rather than cloning it.                                                                                                                               |
| Hall of Heroes         | **Future — Strong idea, but dependent on systems/content not justified yet** | Could display persistent companions/allies if those become a real progression system. Historical NPC lore alone is not enough.                                                                                                                                                     |
| Cartographer's Chamber | **Good — Worth keeping but not essential initially**                         | A strong showcase wing for region completion, Dungeon mastery, discovered locations, and chosen journey highlights. Do not use it for baseline travel or access. One of the best optional MVP wing themes.                                                                         |
| Reliquary              | **Merge — Overlaps enough with another building**                            | Rare artifacts can be a Gallery/Collector display category. There is no separate relic progression system that warrants a facility.                                                                                                                                                |
| Menagerie              | **Future — Strong idea, but dependent on systems/content not justified yet** | Keep the name in a future idea file only. Pets/mounts/familiars do not exist; adding them solely for Strongholds is unjustified.                                                                                                                                                   |
| Portal Chamber         | **Cut — Actively harms or bloats the concept**                               | Baseline travel and content routing should stay on the World Map. A Stronghold-exclusive portal either duplicates navigation or gates content for no good reason.                                                                                                                  |
| Dungeon Archive        | **Merge — Overlaps enough with another building**                            | Dungeons already expose completion records, mastery, failure analysis, and leaderboards. Display milestones in Cartographer/Gallery.                                                                                                                                               |
| Arena                  | **Redundant — Existing game systems already solve this**                     | The Colosseum and Tournament Grounds already own PvP, replays, rankings, Glory, and Hall of Fame; Proving Grounds can own private test scenarios.                                                                                                                                  |
| Council Chamber        | **Weak — Does not currently justify a building**                             | Politics, reputation, territory, and world influence are absent. This is a placeholder for several hypothetical games.                                                                                                                                                             |
| Bounty Office          | **Redundant — Existing game systems already solve this**                     | Quests, event quests, Prophecies, Creature Focus, and Guild orders already direct players toward targets. Another contract board adds checklist pressure.                                                                                                                          |
| Intelligence Office    | **Merge — Overlaps enough with another building**                            | Enemy information belongs with core encounter previews and Creature Archive; advanced comparison can merge into Campaign Room/Proving Grounds.                                                                                                                                     |
| Memorial Garden        | **Good — Worth keeping but not essential initially**                         | A low-mechanics, high-expression place for retired events, memorials, and selected achievements. Strong social/cosmetic expansion candidate after static inspection exists.                                                                                                        |
| Grand Gallery          | **Merge — Overlaps enough with another building**                            | This is the same job as Trophy Hall, Reliquary, Collector's Wing, and Archive of Legends. Use **Gallery of Legacy** once.                                                                                                                                                          |
| Throne Room            | **Merge — Overlaps enough with another building**                            | Late-game status and visual authority should be the final Main Hall stage, not a separate facility competing with the Hall.                                                                                                                                                        |
| Embassy                | **Weak — Does not currently justify a building**                             | No faction/diplomacy system exists. Guild relationships should not be duplicated personally without a clear future design.                                                                                                                                                         |
| Archive of Legends     | **Merge — Overlaps enough with another building**                            | “Extremely rare records” are a Gallery tier/filter, not another archive.                                                                                                                                                                                                           |
| Challenger's Hall      | **Future — Strong idea, but dependent on systems/content not justified yet** | Rewardless challenge modifiers, boss rematches, and personal records could become excellent endgame build puzzles. Build it only alongside that content and likely merge with Proving Grounds.                                                                                     |
| Guardian Hall          | **Merge — Overlaps enough with another building**                            | Tower Guardians, bosses, and prestige records already belong to Tower Hall of Fame and the personal Gallery.                                                                                                                                                                       |
| Wayfarer's Lodge       | **Merge — Overlaps enough with another building**                            | Merge its region/travel identity into Cartographer's Chamber. Do not combine it with NPC expeditions.                                                                                                                                                                              |
| Treasury               | **Merge — Overlaps enough with another building**                            | Wealth display can be a Main Hall/Gallery cosmetic. The Stronghold itself is the wealth sink; it does not need a room whose mechanic is showing the currency used to build it. Avoid confusion with Guild Treasury.                                                                |
| Hall of Prophecy       | **Redundant — Existing game systems already solve this**                     | Prophecies already have daily/weekly state, rerolls, rewards, and milestones. Stronghold integration should be limited to commemorating durable feats.                                                                                                                             |
| Strategy Chamber       | **Merge — Overlaps enough with another building**                            | Equipment and Essence activity-aware loadouts already exist. Merge genuine advanced comparison into Proving Grounds; never gate saved configurations here.                                                                                                                         |
| Collector's Wing       | **Merge — Overlaps enough with another building**                            | It is an umbrella description of Gallery, Arsenal, Sanctum, Reliquary, and Menagerie. Use themed display wings rather than a generic collection-of-collections.                                                                                                                    |

Result: one clear launch core, four or five promising themed wings, several legitimate future ideas, and a large consolidation/cut list. This is healthier than preserving 39 names.

## 7. Buildings to Merge or Remove

### Merge map

| Surviving concept                                             | Absorbs                                                                                                                                                                           |
| ------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Gallery of Legacy**                                         | Trophy Hall, Hall of Records, Grand Gallery, Archive of Legends, Guardian Hall, Monster Hall displays, Reliquary, Treasury wealth display, Collector's Wing, and parts of Library |
| **Proving Grounds** (future)                                  | Training Grounds, Challenger's Hall, private Arena testing, and the test/comparison portions of War Room and Strategy Chamber                                                     |
| **Campaign Room** (only if Proving Grounds becomes too broad) | War Room, Strategy Chamber, Intelligence Office, Observatory/Watchtower encounter dossiers; it should not exist until a persistent comparison need is proven                      |
| **Cartographer's Chamber**                                    | Wayfarer's Lodge, Dungeon Archive displays, map/discovery portions of Library, Observatory, and Watchtower                                                                        |
| **Spiritual architectural theme**                             | Shrine and Chapel; no mechanical building at launch                                                                                                                               |
| **Final Main Hall stage**                                     | Throne Room                                                                                                                                                                       |

### Remove from the roadmap unless a parent system appears

- Salvage Yard, Vault, Portal Chamber, Arena, Bounty Office, Hall of Prophecy.
- Expedition Lodge in its proposed timer/reward form.
- Council Chamber and Embassy until an actual faction/diplomacy design exists.
- Shrine/Chapel blessings and resurrection functions.
- Generic Collector's Wing and duplicate archives/galleries.

The Guild already uses Guild Hall, Mission Board, Market Office, Raid Hall, War Room, Workshop, Training Grounds, Essence Sanctum, and Treasury. Reusing those names for personal buildings would make the UI and future design conversations needlessly ambiguous. Personal wings should be named and scoped around **legacy and curation**, while Guild buildings remain about **shared coordination and investment**.

## 8. Missing Building Opportunities

There is no important missing launch building. That is a useful finding: the proposal's problem is overpopulation, not omission.

One future facility is worth reserving conceptually:

### Steward's Office — Future, conditional

- **Purpose:** present restoration plans, lifetime Cinder contributions, completed deed sets, and the next estate milestone in one place.
- **Interacts with:** Stronghold progression itself, not another game system.
- **Why it may deserve to exist:** only if projects eventually become numerous enough that Main Hall presentation becomes crowded.
- **Timing:** not launch. In the MVP these functions belong in the Main Hall. Splitting them early would create a building whose sole purpose is administering the feature that created it.

Combat Styles are a genuine omission from the attached proposal, but they do not require a new building name: they strengthen the case for a future Proving Grounds. Marketplace, Quests, and Region Bosses should feed deeds/displays without receiving dedicated buildings.

## 9. Building Slots and Specialization

Simple plot limitation is dangerous. If plots contain loadouts, storage, filtering, blessings, travel, or reward bonuses, players will either keep the mathematically best configuration or swap buildings before every activity. That is inconvenience disguised as strategy.

Use three different concepts:

1. **Permanent structures:** Main Hall and Gallery of Legacy. Plot-free and never disabled.
2. **Restored wings:** permanently owned and permanently retain rank/progress. A player never demolishes or loses one.
3. **Showcase plots:** determine which two wings appear on the Stronghold facade and which wing's cosmetic restoration project is currently featured. They do not disable baseline services or remove earned functionality.

For the MVP, offer four wing themes and two visible plots. More plots are not automatically better; the constraint must remain readable. Switching a showcase should be free and immediate when no focused restoration is selected. If a focused project is active, switching may simply move the focus without deleting banked deed progress or contributed Cinders. No cooldown is needed because there is no economic bonus to exploit.

Layouts should be primarily visual. Genuine character identity emerges from:

- which wings the player restored first and furthest;
- which two are displayed;
- selected trophies, titles, equipment, and Essences;
- facade theme, banners, and environmental treatment;
- the player's chosen legacy-deed route through Hall stages.

Do not launch a formal archetype system. “Warlord Stronghold,” “Scholar Stronghold,” and similar labels would merely package obvious best-in-slot bonuses and add balancing obligations. Let identity emerge from curation. A formal archetype becomes useful only if later data shows choices are unreadable to visitors.

## 10. Economy and Resource Review

The game already has many currencies/resources:

- Cinders: combat income, equipment upgrade/variant cost, Marketplace medium, wire-transfer currency.
- Soulstones: permanent constellation upgrades.
- Fate Echo: Prophecy rerolls.
- Prophetic Favor: weekly Prophecy progress.
- Guild Favor: personal Guild shop spending.
- Guild Supplies: shared Guild building spending.
- Arena Glory: Colosseum market spending.
- Tower Tokens: awarded by Tower, with no observed spend path yet.
- Raid Trophies: Raid vendor spending.
- Sigils and Sigil Fragments: Dungeon access.
- Reinforcement Parts: dismantle-to-reinforce equipment loop.
- Essence Dust and Monster Cores: Essence leveling/ascension.
- Boss/event/vendor-specific items and blueprints.

### Recommended Stronghold resource model

| Proposed resource                               | Decision                           | Reason                                                                                                                                                                  |
| ----------------------------------------------- | ---------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Cinders / “gold”                                | **Use**                            | Existing universal wealth is the correct economic input. Strongholds can be a durable Cinder sink without another exchange rate.                                        |
| Salvage                                         | **Do not add**                     | Dismantling already returns Reinforcement Parts. A second salvage output would force valuation and farming choices against reinforcement.                               |
| Reinforcement Parts                             | **Do not consume**                 | Keep the equipment loop legible. Taking Parts for buildings makes every bad drop a Stronghold tax decision.                                                             |
| Renown                                          | **Do not add as currency**         | Achievement points and concrete deeds already express renown. Use them as non-spent requirements. The leaderboard even labels achievement points as Achievement Renown. |
| Timber, stone, supplies, work orders            | **Do not add**                     | These recreate Gathering/Crafting abstractions and require sources, sinks, inventory, balancing, and UI without creating decisions.                                     |
| Tower Tokens, Raid Trophies, Glory, Guild Favor | **Do not spend**                   | Each belongs to its parent system. Use milestones earned in those systems as optional deed gates or display records.                                                    |
| Stronghold Prestige                             | **Computed score only, if needed** | A non-spendable derived display score can support inspection/ranking later. It must not become a farmable currency.                                                     |

Cinder costs need telemetry before numbers are authored. They should be meaningful relative to late-game earning and existing reinforcement/Marketplace demand, but never the only gate. Since Cinders are tradable and directly wireable, a pure-Cinder Stronghold would measure transfers and market wealth rather than personal history. Deed gates prevent that. Do not make costs scale with current balance; that punishes saving and invites manipulation.

Avoid objectives such as “dismantle 1,000 items after starting this project” or “kill low-tier monsters 100,000 times.” They create unhealthy farming. Prefer broad existing lifetime records and achievements, with several alternative deed paths.

## 11. Equipment Integration

### Keep in core Inventory/Equipment UI

- Dismantling and Reinforcement Part preview.
- Favoriting and favorite-dismantle protection.
- Equip/unequip and comparisons.
- Filters, sorting, new/seen state, and any future auto-disposal rules.
- Equipment loadouts and activity assignment.
- Variant/blueprint application and reinforcement.
- Storage and Marketplace/Guild transfer eligibility.

These are baseline ownership and loot-flow tools. Locking them behind a Stronghold would make early loot worse so a later building can repair it.

### Legitimate Stronghold use

An Arsenal wing may:

- feature favorite equipment appearances or a retired “historic loadout” snapshot;
- display first/highest-tier/rare-variant acquisition provenance;
- contribute non-spent equipment accomplishments toward visual restoration;
- later expose an appearance collection, if a real appearance-entitlement system is built.

It should not consume displayed equipment; a display should reference an entitlement/snapshot so dismantling or trading the physical item does not destroy the estate layout unexpectedly. Building that entitlement model is real scope and may justify deferring Arsenal display depth.

Unwanted equipment does not currently need another sink. It already has dismantling, Marketplace sale where eligible, and Guild donation/borrowing contexts. Tune those loops before inventing Stronghold salvage.

## 12. Essence Integration

### Keep in the Soul Archive

- Collection browsing and Creature Archive search.
- Essence absorption/dismantling.
- Essence Dust spending, leveling, ascension, and evolution.
- Creature Focus and its cooldown.
- Favorites.
- Codex collections and their benefits.
- Loadout creation, editing, deletion, attunement, and activity auto-use.
- Drop information and normal Essence analytics.

### Legitimate Stronghold use

An Essence-themed display wing may:

- feature selected absorbed/evolved Essences and completed Codex sets;
- visually change based on collection/ascension milestones;
- later host rewardless fixed-scenario experimentation in partnership with Proving Grounds;
- show a curated “signature soul” identity on the public Stronghold card.

Do not add an Essence acquisition bonus, dust generator, ascension discount, extra combat slot, focus cooldown benefit, or mandatory Sanctum ritual. All would turn the Stronghold into direct power or recurring optimization and would also overlap Nobility benefits.

## 13. Combat / Analytics Integration

The current combat layer already captures more than the proposal acknowledges: entity damage, damage taken, healing, barriers, damage types, ability uses/hits/crits, threat, stagger, summons, deaths, combat log events, and specialized Raid/Tower reports. These should remain visible wherever the battle occurs.

What is still potentially valuable:

- a rewardless fixed target with stable defenses and duration;
- a defensive endurance scenario;
- saved result snapshots for A/B comparison;
- normalized damage/healing/threat rates;
- encounter-specific failure comparisons where existing content persists enough data.

That is one future **Proving Grounds**, not Training Grounds + War Room + Strategy Chamber + Intelligence Office + Arena.

Rules:

- Never remove or gate current battle statistics.
- Test encounters grant no XP, Cinders, drops, achievement progress, Guild contribution, resonance, or Combat Style XP.
- Use a small authored scenario catalog; arbitrary enemy simulation multiplies validation and content-support costs.
- Rate-limit or execute efficiently because exposing the combat engine as a free simulator creates server cost and abuse risk.
- Do not promise accurate “expected DPS” from one stochastic run. Provide multiple-run ranges only if computation and UI can explain them.
- Buff/debuff uptime and longitudinal comparisons are valuable core analytics improvements even without Strongholds. Location in the Stronghold does not justify withholding them elsewhere.

## 14. Expedition Review

**Reject NPC expeditions for Strongholds.**

LegendsLegacy already has idle progression and a World Tower subsystem explicitly called Expeditions. A second, offline NPC expedition loop would create:

- competing timers and terminology;
- claim-and-restart optimization;
- reward inflation detached from the character's build;
- pressure to log in at completion boundaries;
- a new NPC roster/progression/equipment economy;
- substantial scheduler, persistence, notification, balancing, and content work.

The feature's idle nature does not make more timers automatically appropriate. Existing idle combat is the main “set an activity and return” loop; Strongholds should not compete with it.

If a future companion system creates a genuine need, expeditions should focus on **permanent discovery**—lore, cosmetic scenes, rare visitor introductions, or map revelations—not bulk currencies. One long-running route should continue automatically, store discoveries without a claim deadline, and never require restarting several slots daily. Until that parent system exists, cut the Expedition Lodge.

## 15. Stronghold Project Review

Real-time construction projects are unnecessary waiting. “Pay now, return in three days” contains no decision after payment and exists mainly to sell acceleration or generate another notification.

Keep the word **project**, but redefine it as a permanent restoration plan:

- one focused project at a time for presentation clarity, not throughput control;
- several deed requirements, chosen from alternatives;
- optional Cinder contributions in chunks;
- lifetime/historical progress recognized where reliable data exists;
- no expiration and no reset;
- no construction clock;
- completion applies automatically as soon as requirements and contribution are met;
- no claim button;
- changing focus never destroys deed progress or paid Cinders;
- rewards are visual development, display capacity, and cosmetic options—not resource yield or combat power.

Optional “acceleration objectives” are inappropriate because there is no timer to accelerate. Alternative deeds are good: they let a PvP player, collector, or Dungeon specialist reach a Hall stage without being forced through every mode.

The Main Hall does not need multiple simultaneous slots in this model. Additional slots would only add menu management or become monetizable convenience pressure.

## 16. Chore / FOMO Risk Analysis

| Risky proposal direction     | Likely player behavior                                                      | Redesign                                                                                           |
| ---------------------------- | --------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| NPC expedition timers        | Log in on completion boundaries, claim, restart, resent missed cycles       | Cut; existing idle combat and Tower Expeditions are enough                                         |
| Daily/rotating visitors      | Check Stronghold before playing; FOMO over rare merchants                   | Visitors, if ever added, remain until resolved and are triggered by permanent deeds, not daily RNG |
| Shrine blessings             | Visit before every activity and swap to the optimal buff                    | No direct power or time-limited blessings                                                          |
| Passive building income      | Mandatory collection and inflation; “wrong” building choices feel punitive  | No resource production and no collection buttons                                                   |
| Construction timers/slots    | Queue optimization and pressure to buy speed/slots                          | Requirement-based projects with automatic completion                                               |
| Limited utility plots        | Constant swapping for Inventory, Essence, PvP, Dungeon, or Raid tasks       | Limit visual showcase/focus, never baseline utility                                                |
| Maintenance/repair/morale    | Work performed before “real” gameplay                                       | No decay, upkeep, staffing, or repair                                                              |
| Stronghold dailies           | Another checklist alongside Prophecies, Guild orders, tickets, event quests | No reset cadence; use permanent deeds                                                              |
| Unclaimed completion rewards | Notification clutter and delayed gratification                              | Unlock visuals automatically; show a recap on next voluntary visit                                 |
| Time-limited cosmetics       | Seasonal FOMO unrelated to achievement                                      | Seasonal displays may be earned during events but remain permanently placeable afterward           |

Success metric: a player who does not open the Stronghold for a month should lose **nothing**. When they return, newly qualified deeds and display options should be waiting.

## 17. Power Progression Recommendation

Choose **No direct power** for launch and establish it as a durable design boundary.

The game already has character level, equipment tier/quality/rank/variants/sets, Essences, Soulstone upgrades, Combat Style mastery/refinements, Dungeon mastery, and content-specific preparation systems. Another global multiplier layer would be mandatory, worsen old-player snowballing, complicate every balance target, and turn “personal headquarters” into a tax on combat readiness.

“Limited power” is not safely optional in an optimization-driven idle RPG. A 2% global benefit is still mandatory if permanent. “Specialization power” creates loadout swapping and makes plots into pre-activity chores. “Significant power” changes the feature's stated purpose entirely.

Permitted benefits:

- visual upgrades and cosmetics;
- display/curation capacity;
- non-combat information presentation;
- rewardless testing scenarios;
- convenience that is genuinely Stronghold-specific, such as saving its own layout;
- computed prestige/inspection value.

Not permitted:

- character stats, damage, defense, drop rates, XP, Cinders, Essence chance, Dungeon Vigor, extra tickets, Raid/Tower preparation, extra loadouts, faster cooldowns, market limits, offline duration, or resource discounts.

## 18. Integration With Existing Game Systems

| Existing system         | Recommended relationship                                                                                                                         |
| ----------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| Combat                  | Feed broad victories and selected personal records into displays. Add rewardless fixed testing later. Do not gate stats or grant buffs.          |
| Equipment               | Arsenal appearance/history display only. Dismantle, compare, filter, loadouts, upgrade, trade, and storage remain core.                          |
| Essences                | Sanctum-style curation of favorite/advanced Essences and Codex sets. All Archive functionality remains core.                                     |
| Regions                 | Region chapter completion and visual themes feed Cartographer/Gallery. Stronghold never gates travel.                                            |
| Dungeons                | Mastery, notable clear conditions, bosses, and favorite family displays. No Dungeon Archive menu clone or entry bonuses.                         |
| World Tower             | Display floor titles, first-clear participation, and highest released progress. Do not spend Tower Tokens or duplicate Hall of Fame.             |
| Colosseum               | Display best tier, tournament placements, notable achievements, and chosen banner. No Arena building or combat bonus.                            |
| Guilds                  | Show current/historic affiliation and selected Guild accomplishments only with clear privacy/history rules. No shared resource or Guild benefit. |
| Achievements            | Primary deed eligibility and display source. Achievement points are non-spent gates, not a new currency.                                         |
| Titles                  | Feature several unlocked titles/trophies while only one remains equipped on the character.                                                       |
| Prophecies              | At most commemorate durable weekly-cycle or rare achievements. No rerolls, offers, forecasts, or rewards in Stronghold.                          |
| NPCs                    | No launch integration. The parent system does not exist.                                                                                         |
| Quests and event quests | Major completed chains/events can unlock visual mementos. Do not create a second objective board.                                                |
| Region Bosses           | Display participation, best outcome, rare trophy, or title where durable data exists.                                                            |
| Raids                   | Display tier clears, realm-first titles, and favorite boss trophy. Raids remain standalone asynchronous content.                                 |
| Marketplace             | Cinders spent on Strongholds become a sink; no Stronghold trading bonuses or special market.                                                     |
| Combat Styles           | Future Proving Grounds scenarios and style-specific visual banners; no mastery acceleration.                                                     |
| Soulstones              | No mechanical interaction. Constellation completion may unlock a cosmetic observatory/sky treatment.                                             |
| Leaderboards/social     | Later add Stronghold prestige/inspection, but avoid a single total rank that pressures everyone into identical completion.                       |
| Nobility                | Cosmetic bundles may integrate with entitlement infrastructure. No exclusive mechanical plots, project speed, or power.                          |
| Endgame                 | Stronghold is an endgame expression/sink and never an entry requirement for endgame modes.                                                       |

Not every system needs a building. Most should contribute a deed or display option to one shared Gallery model.

## 19. Stronghold vs Guild Progression

| Personal Stronghold                              | Guild headquarters                                                                 |
| ------------------------------------------------ | ---------------------------------------------------------------------------------- |
| Character-funded, mainly with Cinders            | Group-funded with Guild Supplies                                                   |
| Expresses one character's history and tastes     | Expresses collective coordination and investment                                   |
| Permanent restoration and curation               | Officer/member decisions, weekly missions, shop, shared Vault, membership capacity |
| No group power or shared economy                 | May legitimately affect Guild systems and cooperative opportunities                |
| No daily orders or contribution quotas           | Recurring missions/orders are already part of its social loop                      |
| Read-only public inspection later                | Public Guild profile and internal management already exist                         |
| Optional wings are visual/legacy specializations | Buildings should unlock new group actions, decisions, or rewards                   |

Never duplicate:

- Guild missions or personal Guild orders;
- Guild Supply generation/spending;
- Guild shop or Guild Favor;
- shared Vault logistics;
- member capacity/roles/permissions;
- Raid/Guild-war roster coordination;
- Guild Training Grounds or Essence Sanctum bonuses;
- communal buffs.

The clean player explanation is: **“My Stronghold tells my story; my Guild headquarters lets us accomplish things together.”** If a personal building changes what the Guild can do, or a Guild building is required to decorate the personal Stronghold, that boundary has failed.

## 20. Social / Prestige / Visual Progression

Social visibility materially strengthens the concept, because curation has more meaning when someone can see it. It should not be MVP scope.

Recommended expansion:

- a read-only, static inspection page reachable from existing character tags/public lookup;
- selected facade stage/theme, two showcased wings, banner, equipped title, and a limited number of featured records;
- owner controls over what is featured; hidden achievements stay hidden;
- no real-time avatar visitation, guestbook moderation, pathfinding, furniture collision, or synchronous interaction;
- cacheable presentation data so profile views do not query every gameplay subsystem.

Visual development is essential. A menu-only Stronghold does not earn its theme. Use a manageable presentation model:

- one layered facade illustration for each of the five Main Hall stages;
- wing silhouettes/overlays in fixed plot positions;
- player-selected environment/theme and banner;
- achievement-dependent details such as a Tower standard, Raid trophy, or Essence glow;
- accessibility-friendly text summaries for every visual state.

Main Hall stage controls the architectural scale. Achievements unlock optional accents. Cosmetic ownership controls alternative styles, not grandeur. Players choose which unlocked treatment to show.

## 21. Monetization Review

### Healthy

- Complete architectural themes with free default equivalents.
- Facade materials, roof styles, banners, standards, heraldry, lighting, weather, skyboxes, vegetation, and seasonal visual sets.
- Cosmetic wing skins and Gallery frames/plinths.
- Stronghold nameplate typography and non-exclusive ambient effects.
- Bundles that are purely presentational and remain owned permanently.

### Questionable

- Extra **visual-only** showcase layouts or decorative slots. They may be acceptable, but must not increase deed progress, services, or prestige score.
- Convenience for managing large cosmetic libraries. This is only acceptable after the base UI is already good.
- Nobility-only themes. Reasonable as cosmetics, but the free Stronghold must still look complete rather than deliberately shabby.

### Harmful; explicitly avoid

- Project speedups, instant completion, timer skips, extra project queues.
- Extra mechanical building plots or active buffs.
- Exclusive buildings with gameplay functions.
- Cinder/material packs marketed around Stronghold costs.
- Resource generators, yield multipliers, drop-rate buffs, or Stronghold combat stats.
- Visitor rerolls or limited-time merchant access.
- Paid protection from decay/maintenance.
- Selling the final visual stage rather than requiring legacy deeds.

Cosmetic monetization becomes stronger after static public inspection. Do not add social scope solely to monetize it; first prove that players value their own estate.

## 22. Solo-Developer Scope Review

### Deceptively expensive proposal elements

| Element                     | Hidden cost                                                                                                                                                                         |
| --------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 39 buildings                | Domain schema, definitions, unlock/cost logic, API contracts, migrations, state sync, routing, responsive pages, art/icons, copy, tests, balance, and future compatibility for each |
| NPC visitors                | NPC identities, schedules, random selection, offers/dialogue, persistence, expiry, notifications, reward safety, localization/content cadence                                       |
| NPC expeditions             | New scheduler/state machine, agent roster, mission generation, offline settlement, reward economy, claims, failure, anti-abuse, live-ops content                                    |
| Combat simulator            | Snapshot safety, deterministic scenarios, repeated-run computation, rate limiting, abuse prevention, result comparison, balance/support burden                                      |
| Cross-system archive        | Persistent historical data may not exist for every desired statistic; backfills and joins become expensive and fragile                                                              |
| Functional plot swapping    | Rules for active content, loadouts, buffs, cooldowns, rollback, deactivation, and UI warnings                                                                                       |
| Direct power                | Permanent balance matrix across idle combat, Dungeons, Tower, PvP, Tournaments, Region Bosses, and Raids                                                                            |
| Walkable/social visits      | Public APIs, privacy, caching, asset streaming, layout validation, moderation, responsive presentation, and possibly real-time infrastructure                                       |
| Freeform decoration         | Placement editor, collision/grid logic, persistence format, touch controls, undo, asset catalog, ownership, loading, and accessibility                                              |
| Many independent currencies | Sources, sinks, valuation, exploits, admin tooling, telemetry, compensation, and player education                                                                                   |

### Manageable core

- One aggregate with five Main Hall stages.
- One focused restoration project model with no clock.
- Cinder contribution plus achievement/deed gates.
- One Gallery page using existing achievement/title/record DTOs or a materialized presentation snapshot.
- Four optional wing definitions with three visual ranks, two showcase positions, and no mechanical buffs.
- Five facade backgrounds plus fixed wing overlays rather than a placement editor.
- Existing state-sync/outbox patterns for completed deeds and presentation refresh.

Even this MVP needs careful backfill and content work. A technical spike should prove that deed eligibility can be derived from existing records without synchronous fan-out across every service.

## 23. Design Decisions I Would Strongly Reconsider

### Treating every useful menu as a building

**Problem:** Arsenal, Sanctum, Vault, Archives, Strategy Chamber, and similar concepts mostly rename existing pages.
**Likely behavior:** players bounce through extra navigation or resent losing normal functions when a building is inactive.
**Alternative:** buildings curate legacy or add one genuinely new advanced tool; core functions stay where they are.

### Salvage Yard as a central Stronghold pillar

**Problem:** equipment dismantling already produces Reinforcement Parts and is integrated with favorites and upgrades.
**Likely behavior:** players feel forced to visit the Stronghold for routine disposal, or a second salvage currency competes with reinforcement.
**Alternative:** leave dismantling in Inventory; Arsenal displays equipment history.

### NPC expeditions

**Problem:** duplicate idle loop and collision with the World Tower's established Expedition name.
**Likely behavior:** timer optimization, daily claims, inflation, and disappointment when offline NPCs become more valuable than character activity.
**Alternative:** cut. Future companion discoveries, if ever justified, should auto-continue and be mostly cosmetic.

### Functional utility behind limited plots

**Problem:** mutually exclusive loadouts, storage, analysis, travel, or blessings are not identity; they are access friction.
**Likely behavior:** constant swapping or one solved meta-layout.
**Alternative:** limit visual showcase and restoration focus, not baseline access.

### Main Hall as a conventional level ladder

**Problem:** it becomes another number optimized with Cinders.
**Likely behavior:** rush Hall first, then backfill side buildings; all Strongholds converge.
**Alternative:** five named stages requiring Cinders, alternative legacy deeds, and some chosen wing restoration.

### Direct or specialization combat power

**Problem:** “optional” permanent power is not optional in an idle RPG.
**Likely behavior:** players treat Stronghold upkeep and swapping as mandatory pre-content work.
**Alternative:** no combat/reward power; visual identity and rewardless testing only.

### Dedicated Stronghold currencies

**Problem:** the current economy already has numerous purpose-bound balances.
**Likely behavior:** another farm route, exchange-rate confusion, and abandoned currencies as content ages.
**Alternative:** Cinders as the spend, existing accomplishments as non-spent gates, optional computed prestige only.

### Timed projects and capacity upgrades

**Problem:** waiting is not gameplay; capacity mainly creates monetization pressure.
**Likely behavior:** queue checking and paid acceleration demand.
**Alternative:** permanent restoration plans completed by requirements, with automatic application and no claim.

### Visitors and special encounters controlled by Main Hall

**Problem:** no NPC system exists, and random arrivals naturally become expiring content.
**Likely behavior:** daily checks and FOMO.
**Alternative:** defer. If built later, visitors unlock from permanent deeds and wait indefinitely.

### Building the full visual/layout fantasy

**Problem:** a freeform castle builder is a separate product-sized frontend/art commitment.
**Likely behavior:** development stalls on placement polish while gameplay remains shallow.
**Alternative:** fixed facade stages and layered wing slots first.

### Reusing Guild building concepts and names

**Problem:** War Room, Training Grounds, Essence Sanctum, Treasury, and Hall-like progression already belong to Guild headquarters, some as unfinished promises.
**Likely behavior:** player confusion and duplicated balance/feature obligations.
**Alternative:** personal Strongholds memorialize; Guild buildings coordinate. Rename personal survivors accordingly.

## 24. Ideas Worth Protecting

### Main Hall as the progression spine

This gives the feature a readable center and a strong visual metaphor. Protect it, but make progression sparse and composite.

### Permanent personal headquarters

LegendsLegacy has many durable systems but no unified visual home. A persistent estate fits the “Legacy” name and provides a long-horizon identity distinct from moment-to-moment gear.

### More possible wings than visible plots

The idea is sound when plots control expression and restoration focus. It creates visible differences without withholding essential utility.

### Progress retained for inactive buildings

No demolition or destructive respec is exactly right for a long-term feature. Preserve every contribution and deed.

### Accomplishments as architecture

Turning Tower, Dungeon, Raid, PvP, region, title, and collection achievements into visible details is the most game-specific part of the concept. It strengthens systems players already use rather than competing with them.

### No passive resource generation

This rule prevents the Stronghold from becoming a mandatory economic engine. Keep it absolute.

### Visual journey from ruin to legendary seat

This is more important than most proposed mechanics. The feature needs this transformation to feel like a place rather than a settings page.

### Expandable data-driven framework

A small definition-driven deed/wing/display system can accept future content without a new bespoke page per activity. Protect extensibility, but do not pre-build empty facilities.

## 25. Unanswered Design Questions

| Question                                          | Why it matters                                                                     | Recommended answer                                                                                                                    |
| ------------------------------------------------- | ---------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Is a Stronghold account-owned or character-owned? | Achievements/titles have multiple scopes and future multiple characters may exist. | Character-owned estate; account-owned cosmetics may be usable across estates.                                                         |
| When does it unlock?                              | Too early adds onboarding overload; too late removes aspiration.                   | After the focused first-region journey, approximately level 30 plus its capstone quest. Tease it earlier without exposing management. |
| What exactly is “active”?                         | This determines whether plot choice is identity or friction.                       | Active means visually showcased and eligible as the current restoration focus, never required for utility.                            |
| How is historical progress backfilled?            | Players will reject having already completed feats ignored.                        | Derive from achievements, mastery, titles, and durable records. Do not author launch deeds that cannot be reliably backfilled.        |
| Can wealth alone finish the Hall?                 | Cinders are tradable/wireable and could turn legacy into purchased status.         | No. Every stage requires alternative accomplishment deeds as well as Cinders.                                                         |
| Are deeds spent?                                  | Consuming accomplishments is unintuitive and creates currency semantics.           | No. Deeds are permanent eligibility flags.                                                                                            |
| Can projects expire or fail?                      | Expiry creates FOMO and punishes breaks.                                           | Never.                                                                                                                                |
| What happens when a wing is unshowcased?          | Destructive switching undermines long-term investment.                             | It retains rank, contributions, unlocks, and display configuration.                                                                   |
| Does the Stronghold grant power?                  | This changes balance, mandatory status, and monetization.                          | No direct character or reward power. Write this into the feature charter.                                                             |
| What data is public?                              | Hidden achievements, Guild history, and records may be sensitive or misleading.    | Owner selects featured public items; hidden data remains hidden; default inspection is conservative.                                  |
| What is the visual production budget?             | Without visuals the fantasy collapses; freeform visuals explode scope.             | Five fixed stages, four wing overlays, a small theme system. Do not start implementation until this asset plan is credible.           |
| What is the Cinder-cost target?                   | The feature may erase early upgrade choices or be instantly completed by veterans. | Use live economy percentiles and expected earning time; accomplishment gates carry much of the pacing.                                |
| What is the completed-state plan?                 | A capped Stronghold can become irrelevant.                                         | Continue adding display mementos and optional wing ranks; do not add infinite Hall levels. Completion is allowed.                     |
| How does renaming/reworking buildings migrate?    | A long-lived expandable system will change.                                        | Stable machine keys, versioned definitions, never delete paid contribution state, and map retired wings to equivalent displays.       |
| Is static profile inspection enough?              | Full visitation changes frontend/backend scope radically.                          | Yes. Prove value with static inspection before considering anything richer.                                                           |

## 26. Recommended MVP

### Core concept

A personal legacy estate unlocked after the first major journey. The player restores it over months by investing Cinders and demonstrating broad accomplishments. It visibly communicates what the character values without granting combat power.

### Main Hall

- Five named stages: Ruined Outpost → Restored Keep → Fortress → Great Citadel → Legendary Seat.
- Unlock around level 30 plus the current journey capstone.
- Each advancement requires one Cinder threshold, a choice of deeds, and at least one prior wing milestone.
- Unlocks facade scale, display positions, wing rank caps, and two showcase plots by the middle stages.
- No numerical stat bonus, timer, NPC slot, or service gate.

### Permanent buildings

1. **Main Hall** — restoration plan, stage requirements, facade/theme selection.
2. **Gallery of Legacy** — feature a limited selection of achievements, titles, Dungeon/Tower/Raid/PvP records, and collection milestones. It links to existing source pages rather than duplicating them.

### Optional wings

Offer four definitions, of which two can be displayed:

1. **Arsenal** — equipment provenance, favorite historic pieces, equipment-related visual restoration.
2. **Essence Sanctum** (rename if Guild terminology remains) — favorite Essences and Codex/ascension displays.
3. **Cartographer's Chamber** — region chapters, Dungeon mastery, notable discoveries.
4. **Challenger's Gallery** — Colosseum, Tournament, Tower, Region Boss, and Raid feats. This is display-only; a Proving Grounds simulator is not part of MVP.

Each wing has three visual ranks. Rank requirements use Cinders plus existing backfillable deeds. No wing grants normal UI functionality.

### Plots and selection

- One showcase plot at Stage 2; two at Stage 3 or 4.
- All restored wings keep progress while not displayed.
- Switching is free; no cooldown or destruction.
- One focused restoration goal is highlighted at a time, but qualification progress accumulates globally.

### Resources

- Cinders are the only spent resource.
- Existing achievement/record conditions act as non-spent deeds.
- No Renown, stone, timber, salvage, labor, project token, or premium completion currency.
- Optional computed estate prestige may be displayed internally but is not ranked or spent at launch.

### Progression mechanism

- No elapsed-time construction.
- Players contribute Cinders in safe, confirmed chunks.
- Deeds are alternative sets, not a completionist checklist.
- Completion applies automatically.
- No claims, daily reset, maintenance, or resource output.

### Equipment interaction

Display/provenance only. Dismantling, parts, loadouts, filtering, comparison, storage, and upgrades remain in Inventory.

### Essence interaction

Display favorite/advanced Essences and Codex milestones only. Soul Archive management remains unchanged.

### Trophy/achievement interaction

Use achievements/titles and existing durable records as the primary content source. The player curates a small featured set. Avoid new one-off tracking unless the same event should become a normal achievement.

### Visual scope

- Five fixed facade illustrations.
- Four fixed wing overlays with three ranks each.
- A small free theme/banner set.
- No freeform furniture or walkable scene.

### Deliberately excluded

- Direct or indirect character power.
- Salvage Yard, Vault, NPC expeditions, visitors, blessings, resource production.
- Combat simulator and longitudinal analytics.
- Social visiting/inspection.
- Monetization beyond preparing cosmetic entitlement hooks.
- New currencies.
- Construction timers and project queues.
- Guild integration beyond optional display of affiliation.
- Every speculative building not supported by a current parent system.

This MVP is substantial because its progression, visuals, curation, and choice reinforce the whole game. It remains buildable because it has one progression spine, one display model, four themed definitions, and no new scheduler or combat balance layer.

## 27. Long-Term Expansion Path

### Stage 1 — Initial Stronghold

- Recommended MVP above.
- Instrument visits, Cinder contributions, stage completion time, wing choices, swaps, and display edits.
- Validate that players value visual permanence without power rewards.

### Stage 2 — Static social prestige

- Read-only public Stronghold card/page through character tags.
- Owner-curated privacy-safe featured records.
- Memorial Garden as a cosmetic/event-history wing.
- Architectural themes, banners, environments, and cosmetic monetization.
- Optional non-ranked prestige summary; avoid one completionist score if it homogenizes choices.

### Stage 3 — Advanced build laboratory

- Proving Grounds with a very small rewardless scenario catalog.
- Saved A/B result snapshots and build labels.
- Combat Style integration.
- Challenger modifiers/boss rematches only if they are designed as genuine endgame content, not just another building rank.
- No rewards or character-power bonuses from testing.

### Stage 4 — Parent-system integrations

- Hall of Heroes/Barracks only after companions or retainers exist.
- Menagerie only after pets/mounts have their own compelling system.
- Guest Quarters only after persistent NPC relationships exist, with no daily rotations.
- World-event Observatory only after world events need a consolidated overview.
- Additional showcase wings for new regions, Raid families, or durable collection types.

No stage should add infinite Main Hall levels, resource generators, daily maintenance, or mechanical plot monetization.

## 28. Final Revised Stronghold Vision

### Feature pitch

The Stronghold is a character's permanent legacy estate: a ruined outpost restored into a legendary seat through Cinder investment and accomplishments earned across LegendsLegacy. Players choose which parts of their journey to memorialize, develop a small number of themed wings, and curate a visible headquarters that grows grander without becoming another source of mandatory combat power or daily chores.

### Primary player fantasy

**“I have left a mark on this world, and this place visibly tells my story.”** The player is not a mayor managing production. They are an adventurer whose victories, discoveries, collections, alliances, and wealth have become architecture.

### Primary gameplay purpose

Unify fragmented long-term accomplishments, provide an enduring non-power Cinder sink, and create personal expression that existing level/gear screens cannot provide.

### Core loop

Play existing content freely; qualify for permanent deeds automatically; choose a restoration goal; contribute Cinders; complete an accomplishment-backed milestone; see the estate change; curate the result.

### Main Hall role

Five sparse, named stages control estate scale, wing rank caps, showcase capacity, and visual grandeur. Stages require wealth plus alternative deeds and some wing development. They never grant combat stats or normal service access.

### Building model

Main Hall and Gallery are permanent. Optional wings are permanent investments but only two are visually showcased at once. Wings are coherent legacy themes, not copies of existing menus. Inactive wings retain everything.

### Progression model

Requirement-based restoration with no timers. Cinders show investment; non-spent deeds show accomplishment; selected wing milestones show identity. Progress is finite enough to be legible and expandable through new wings/displays rather than infinite Hall levels.

### Resource model

Cinders only. No dedicated Stronghold currency. Achievement points and concrete records are gates, never spent. Parent-system currencies remain with their parent systems.

### Relationship with existing systems

Existing systems produce deeds and display options. Strongholds link back to them but do not move their baseline interfaces, alter their rewards, or gate their content.

### Player choice

Players choose alternative deed routes, restoration order, two showcased wings, featured records, favorite items/Essences, facade theme, and visual accents. Choices communicate identity rather than optimize universal power.

### Avoiding chores

No daily state, visitor rotation, maintenance, passive income, claim buttons, timers, queues, expiring projects, or buff swapping. Ignoring the feature never causes loss.

### Avoiding mandatory power creep

The feature charter prohibits character stats, reward rates, drop rates, activity capacity, and content access. Advanced testing may arrive later but produces no rewards.

### Differentiating players

Two visible wings from a larger owned set, different advancement deeds, selected trophies/titles, equipment/Essence displays, and architectural themes make estates distinct without requiring mechanically unequal utility.

### Why it deserves development time

LegendsLegacy already has enough systems that accomplishments risk becoming isolated numbers on separate pages. A carefully scoped Stronghold turns that breadth into one legible, emotional, and monetizable-without-power expression of “legacy.” It deserves time only if the visual plan is credible and the implementation stays within these boundaries. If it degrades into menus, timers, buffs, or duplicate archives, the same effort is better spent improving existing systems and public character profiles.

## 29. Prioritized Recommendations

### Critical — Resolve before implementation

1. Replace the 39-building service catalog with a legacy-estate scope and approve explicit non-goals.
2. Write “no direct/reward power, no daily cadence, no resource production, no construction timers, and no baseline UX gates” into the feature charter.
3. Decide that active plots govern visual showcase/restoration focus, not utility access.
4. Use Cinders plus non-spent, alternative legacy deeds; add no Stronghold currency or salvage resource.
5. Keep dismantling, loadouts, storage, filters, Essence management, combat stats, travel, and activity access in their current core pages.
6. Prove a credible five-stage visual asset plan. If that is unaffordable, build a public legacy profile instead of Strongholds.
7. Define character/account ownership and reliable historical backfill before schema design.

### High — Major improvement

1. Make Main Hall progression five named composite stages, not an open numerical ladder.
2. Merge trophy/record/archive/gallery concepts into one permanent Gallery of Legacy.
3. Launch with four coherent display wings and only two showcase plots.
4. Cut NPC expeditions and defer all visitors/retainers until an NPC parent system exists.
5. Keep personal and Guild headquarters philosophies separate and rename overlapping concepts.
6. Unlock after the first major journey (around level 30), not during onboarding.
7. Design deed alternatives so no mode, including PvP, Guild, Raid, or Tower, is universally mandatory.

### Medium — Worth addressing

1. Instrument Cinder income/wealth percentiles and competing sinks before setting costs.
2. Materialize/cache a Stronghold presentation snapshot rather than querying every subsystem on each view.
3. Add static public inspection after the MVP proves personal value.
4. Plan safe migration for renamed/retired wings and versioned definitions.
5. Reserve Proving Grounds for rewardless fixed testing after combat-simulation cost is measured.
6. Ensure displayed equipment/Essences reference durable snapshots or entitlements, not fragile inventory ownership.

### Low — Optional refinement

1. Add Memorial Garden after event-history and social display needs are clear.
2. Offer cosmetic themes, banners, environments, and wing skins after a complete free visual path exists.
3. Consider a computed, non-spendable prestige summary, but avoid a leaderboard if it homogenizes Strongholds.
4. Add Steward's Office only if Main Hall project presentation genuinely becomes crowded.

## Repository evidence consulted

Key evidence included:

- `src/Core/Domain/Models/Entities/Characters/Character.cs`
- `src/Core/Domain/Models/Progression/CharacterExperienceCurveSettings.cs`
- `src/Core/Domain/Models/CombatStyles/`
- `src/Infrastructure/Service/Services.LL/Combat/`
- `src/Presentation/ll/src/app/shared/components/combat/`
- `src/Core/Domain/Models/Items/Equipments/`
- `src/Infrastructure/Service/Services.LL/Items/EquipmentAcquisitionService.cs`
- `src/Infrastructure/Service/Services.LL/Items/EquipmentUpgradeService.cs`
- `src/Infrastructure/Service/Services.LL/Items/EquipmentLoadoutService.cs`
- `src/Core/Domain/Models/Essences/`
- `src/Infrastructure/Service/Services.LL/Essences/EssenceSystemService.cs`
- `src/Core/Domain/Models/Dungeons/`
- `src/Infrastructure/Service/Services.LL/Dungeons/DungeonRunService.cs`
- `src/Core/Domain/Models/WorldTower/`
- `src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs`
- `src/Core/Domain/Models/Colosseum/`
- `src/Infrastructure/Service/Services.LL/Colosseum/`
- `src/Core/Domain/Models/Guilds/`
- `src/Infrastructure/Service/Services.LL/Guilds/`
- `src/API/API.LL/Data/guilds/guild-content.json`
- `src/Core/Domain/Models/Achievements/`
- `src/API/API.LL/Data/achievements/` and `Data/titles/`
- `src/Core/Domain/Models/Prophecies/`
- `src/Infrastructure/Service/Services.LL/Prophecies/`
- `src/Core/Domain/Models/Raids/` and `Services.LL/Raids/`
- `src/Core/Domain/Models/RegionBosses/` and `Services.LL/RegionBosses/`
- `src/Presentation/ll/src/app/core/services/client-side/sidebar/sidebar.service.ts`
- `src/Presentation/ll/src/app/core/services/client-side/player-journey/player-journey.ts`
- current JSON content under `src/API/API.LL/Data/`
