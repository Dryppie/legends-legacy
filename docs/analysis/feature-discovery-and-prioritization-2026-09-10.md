# LegendsLegacy — Feature Discovery and Prioritization

Repository review: 10 September 2026. Analysis only; no gameplay implementation.

## 1. Current Game Overview

**LegendsLegacy already has enough major systems to support a substantial game. Its best next additions would make existing progression easier to pursue, combat easier to understand, and familiar encounters worth mastering.** A large personal stronghold could eventually connect these systems, but building one now would risk adding another destination before the existing destinations consistently justify returning.

### What this review establishes

This analysis follows the current working tree through domain models, application commands and queries, API endpoints, service implementations, persistence declarations, JSON catalogs, workers, and Angular routes/components. It also examines the independent chat service, admin dashboard, LiveOps application, and balance harness. Existing uncommitted work was included as source evidence and left untouched.

“Implemented” below means that a substantive code path exists. It does **not** establish production availability, correctness under load, or a healthy live economy. I did not access a running game, a database, player telemetry, or deployment configuration. Retention and balance conclusions are design hypotheses grounded in code, not measured player outcomes.

The source is materially newer than several documents. For example, the root README describes equipment properties as immutable, but current services implement reinforcement, dismantling, and consumable blueprint conversion. Older guild and tournament status documents also describe retired currencies, daily schedules, or missing team functionality that no longer match the code. Current executable paths take precedence here.

### The game that exists

| System | Current repository evidence | Design implication |
|---|---|---|
| Idle combat | Scheduled automatic encounters, offline catch-up, reward settlement, and build-change boundaries. Checked-in API settings use a 10-second encounter cadence and 24-hour offline cap. | The fundamental action is choosing a productive fight and preparing the character, then returning to assess results. |
| World progression | Two authored regions: Shenic and Meran. Shenic contains a tutorial area plus ten ordinary areas; Meran contains four ordinary areas. Ordinary entry levels run from 1 through 65. | This is a relatively compact world with enough existing encounters to support substantial reuse. |
| Equipment | Combat drops, dungeon rewards, starter grants, rarity, quality, tier, rank, frozen attribute rolls, themed variants, sets, reinforcement, dismantling into Reinforcement Parts, and consumable blueprints. | Equipment already has both acquisition and investment decisions. Another generic upgrading system is unnecessary. |
| Essences | 80 authored definitions; active/passive ability packages, absorption, attunement, three essence loadouts, activity selection, XP, ascension, evolution operation, favorites, and duplicate dismantling into Essence Dust. | Collection can expand tactical options, but acquisition alone does not prove that all options have useful niches. |
| Creature archive and Codex | Kill history, discovery, source locations, creature focus with an eight-hour switch cooldown, resonance/pity, 19 collection definitions, and applied collection bonuses that scale with collection ascension. | Bestiary, target focus, collection bonuses, and bad-luck mitigation already exist. |
| Combat Styles | Four validated styles: Bastion, Conduit, Reaper, Duelist. Refinements, upgrades, mastery selections, remembered choices, and ten-level progression. | Seven style design documents do not mean seven implemented styles. The four current styles already provide a meaningful identity layer. |
| Soulstone Constellations | Seven authored upgrade definitions with finite ranks and effects supporting essence progression and related utility. | This already occupies part of the “persistent personal investment” space. |
| Dungeons | Four families: Goblin Mines, Forgotten Catacombs, Tangled Cave, The Great Tree; three difficulties per family. Routes, Vigor, rest sites, treasuries, retreat, pending rewards, completion records, and shared family mastery. | Branching dungeons, risk/reward routes, and dungeon mastery are existing features, not new proposals. |
| PvP | Ticketed asynchronous Colosseum fights, defense snapshots, rating, Glory, first-win rewards, history, and Champion Market. Tournament brackets, teams, invitations/applications, playback, loadout updates, and a computed monthly standings view. | New PvP should offer a distinct competitive question rather than another queue with different rewards. |
| Guilds | Membership, recruitment, contributions, personal orders, weekly missions, supplies, construction targets, Guild Favor shop, equipment donation and loans. | Guilds have economic coordination, but shared tactical purpose is less developed. |
| Cooperative/endgame combat | World Tower with 15 released floors, parties, server first clears, Echo replay mode, scouting/preparation, and Hall of Fame. Two authored raid bosses with wing assignments, snapshots, tier/plus concepts, resolution, and replay. One authored regional boss definition with scheduled event machinery. | There is already cooperative endgame infrastructure. Another tower, raid framework, or world-boss scheduler would duplicate it. |
| Quests and recurring goals | 28 ordinary quest files: three onboarding, eleven Shenic, fourteen side quests. Quest choices, prerequisites, pinning, event-driven progression. Twelve daily and six weekly prophecy definitions, daily selection/rerolls, weekly revelation, caches. | Goals are plentiful; the missing layer is a clearer personal strategy across goals and less dependence on period boundaries. |
| Server events | Event quest instances, global and personal contributions, milestones, claim windows, and announcements. The checked-in Great Treasure Hunt is enabled for 6–13 September UTC and rewards equipment discoveries. | A server-event framework already exists; meaningful consequences would be an expansion of it. |
| Identity/social/economy | Achievements, titles, account/character scope, leaderboards, player inspection, trade listings, commodity buy orders, history, transfers, and chat. Chat supports General, Trade, Help, Guild, Raid, and Whisper channels. | Trading, titles, leaderboards, guild banks, and chat are not gaps. |
| Operations/content | JSON content providers, admin creature/item/essence tools and combat diagnostics, separate LiveOps support/audit tools, durable Quartz jobs, an outbox, snapshots, and a substantial balance harness. | There is good infrastructure for bounded systemic extensions, but each extension still needs player UI, rule validation, and settlement semantics. |

Content counts describe authored entries, not distinct released encounters or balanced choices. For example, there are five delve documents but only four current dungeon families; the extra delve data must not be counted as a fifth released dungeon. Similarly, 234 ability entries, 25 status definitions, and 101 creature entries do not independently establish their reachability in current player content.

Frontend access is staged through journey guards, including level constants of 10 for social access, 20 for economy, and 30 for the wider game in the focused-beta flow. Actual visibility depends on the journey configuration and quest state. The city includes guilds, Colosseum, marketplace, and a Tavern that currently serves leaderboards—not a developed NPC inn system.

### Important unfinished boundaries

- **Raid rewards:** `RaidOptions.RewardsEnabled` defaults to false, and the inspected API configuration files do not override it. Claiming is guarded by this option. The trophy vendor implements purchases and limits, but its JSON stock is empty. Environment overrides could change availability; they were not inspected.
- **Tower rewards:** Tower Tokens are awarded. The searched domain/application/service paths expose no token spending operation. This is a repository gap, not proof of live inflation.
- **Guild buildings:** Raid Hall, War Room, Training Grounds, and Essence Sanctum contain future benefits and are hidden by the current player building component. They are scaffolding, not currently visible finished activities.
- **Season identity:** Tournament monthly standings are calculated from completed placements. That is different from durable, settled season records and rewards.
- **Combat capability:** The engine supports rich statuses, summons, threat, and stagger, but explicitly rejects abilities requiring Mana. A mana economy would be a new combat subsystem.
- **Account scope:** Most power progression is character-owned. Achievement/title models explicitly support account scope. “Permanent character progress” and “account-wide inheritance” must not be treated as interchangeable.

### Evidence register

The following references are used throughout the report. They identify implementation anchors rather than merely design plans.

| Reference | Evidence |
|---|---|
| E1 — World and access | [Regions][regions], [world routes][world-routes], [player journey][journey] |
| E2 — Idle loop | [CharacterActionService][actions], [idle progression options][idle-options], [API defaults][api-config] |
| E3 — Equipment | [EquipmentAcquisitionService][equipment-acquisition], [EquipmentUpgradePolicy][equipment-policy], [upgrade prices][equipment-prices], [blueprints][blueprints] |
| E4 — Builds | [EquipmentLoadoutService][equipment-loadouts], [EssenceSystemService][essences], [CombatStyleService][styles], [CombatStyleRules][style-rules] |
| E5 — Collection | [CreatureArchiveService][archive], [EssenceCodexCollectionService][codex], [EssenceCodexBonusProvider][codex-bonuses], [ascension rules][ascension] |
| E6 — Dungeons | [DungeonRunService][dungeons], [DungeonMasteryService][mastery], [DungeonMasteryBenefits][mastery-benefits], [family definitions][dungeon-data] |
| E7 — Combat evidence | [FastCombatEngine][engine], [CompactCombatTelemetry][telemetry], [player combat statistics][combat-stats], [BalanceHarness][harness] |
| E8 — Guilds | [GuildMissionService][guild-missions], [GuildVaultService][vault], [GuildBuildingService][buildings], [guild content][guild-data], [hidden building filter][building-ui] |
| E9 — PvP | [ColosseumService][arena], [TournamentGroundsService][tournaments], [Champion Market][champion-market] |
| E10 — Tower | [WorldTowerService][tower], [floor definitions][tower-data], [party rules][tower-parties] |
| E11 — Raids | [RaidService][raids], [RaidOptions][raid-options], [raid definitions][raid-data], [trophy vendor][raid-vendor] |
| E12 — Goals | [QuestService][quests], [ProphecyService][prophecies], [quest tracker][quest-tracker], [EventQuestService][events] |
| E13 — Identity | [AchievementDefinition][achievements], [requirement types][achievement-types], [PlayerAchievementProgress][achievement-progress], [PlayerTitleUnlock][titles] |
| E14 — Economy and infrastructure | [MarketPlaceService][market], [Character][character], [SoulstoneUpgradeService][soulstones], [GameEventTypes][outbox], [worker registration][jobs], [ChatHub][chat], [LLDbContext][db] |

## 2. Current Gameplay Loop Analysis

The central loop is:

**Choose an accessible area → configure equipment, essences, and style → resolve fights over time → gain XP, currencies, essences, gear, and sigils → invest or change the build → attempt a harder or more rewarding objective.**

Dungeons turn sigils into encounters, mastery, cores, gear, and blueprints. Cores unlock essence ascension. Duplicate essences become Dust for leveling alternatives. Duplicate equipment becomes parts, while Cinders pay reinforcement costs. Guild and PvP rewards also feed essence progression and dungeon access. These are already useful connections. [E3–E6, E8–E9]

### Goal horizons

| Horizon | A plausible current player goal | Weakness/opportunity |
|---|---|---|
| Next 5 minutes | Pick a hunt, finish an onboarding step, absorb an essence, inspect a drop, configure a style. | New-player guidance exists. Beyond it, choosing which improvement matters can require visiting several screens. |
| Next 30 minutes | Test an area, compare a build change, advance a dungeon route, complete an immediately achievable objective. | At 10 seconds per encounter this allows at most 180 scheduled encounters, not 180 guaranteed wins. Rare equipment drops cannot reliably provide the session's emotional payoff. |
| This session | Make a dungeon decision, reach a quest milestone, spend accumulated resources, prepare a team. | Statistics exist, but linking a failed encounter to a useful build change takes interpretation. |
| Today | Make a prophecy choice, progress guild orders, use arena tickets, settle offline combat, hunt a focused creature. | Several clocks and claim surfaces can make “returning” feel like administration. |
| Several days | Acquire a missing essence, find a needed blueprint, reinforce a useful item, improve dungeon mastery. | Bad luck is mitigated in some systems; knowing the best next source remains fragmented. |
| A week | Guild mission, weekly tournament, revelation milestones, cooperative boss attempts. | Shared success can be constrained by roster availability, fixed mission targets, and event timing. |
| Several weeks | Ascend a broader archive, complete more sets, master styles, reach Meran and later tower floors. | Strong progression breadth; less evidence of repeatable tests that reward maintaining several different builds. |
| Months | Finish difficult collections, optimize equipment rolls, climb rating, conquer released tower/raid encounters. | Collection and optimization remain, but much of the challenge can converge on numeric improvement. |
| A season | Monthly tournament points and competitive recognition. | The computed standings view lacks a fully durable seasonal legacy loop. |
| Years | Collection completion, social standing, accumulated character identity. | The repository alone does not justify a years-long content claim. Recurring rule variations and preserved accomplishments offer a more sustainable route than endless tiers. |

### Progression assessment

**Horizontal:** Strong raw ingredients: essence combinations, style refinements, sets, equipment variants, threat roles, and party assignments. The main missing layer is making several configurations practical to maintain and giving them distinct jobs.

**Vertical:** Already crowded: character levels, essence levels/ascension, equipment tier/rank/rarity/quality, style levels, dungeon mastery, and constellation ranks. Add meaningful thresholds or encounter access before adding another multiplier. An upgrade matters most when it enables a new solution.

**Collection:** Essence collection is mature enough to build around. Equipment collecting has real use through sets and alternate builds, but ownership must compete with dismantling and trade. Achievement/title identity exists. A new cosmetic collection system is lower priority than useful deployment of collected gear and essences.

**Replayability:** Earlier dungeons already remain useful through mastery, blueprint sources, cores, and rewards. Earlier creatures retain essence/collection value. The gap is what happens after those reasons are exhausted; “make old content relevant” should extend these connections, not pretend they are absent.

**Return motivation:** Tomorrow is supported by idle settlement and personal goals; next week by guilds/tournaments; next month by collection and progression. Six-month motivation is the least convincingly established layer: veterans need different accomplishments and tactical problems, not just more totals.

## 3. Major Strengths

1. **Preparation has genuine mechanical substance.** Threat, stagger, statuses, summons, style mechanics, and essence order support decisions beyond attack-stat optimization. The combat engine already gives these ideas something to operate on. [E4, E7]
2. **Monsters are attached to permanent discovery.** Creature history, essence sources, resonance, and Codex collections make enemies more than XP containers. [E5]
3. **Equipment has a coherent combat-based lifecycle.** Acquire, evaluate, equip, convert a variant, reinforce, donate/trade where permitted, or dismantle. Crafting and Gathering do not need to return. [E3, E8]
4. **Dungeons already contain decisions within a run.** Vigor and treasuries create a cost for greed; routes and retreat offer a foundation for interesting mastery. [E6]
5. **Cooperative encounters are more developed than guild labels suggest.** Raids have distinct wing jobs; tower parties and snapshots already exist. Connecting guilds to these systems is cheaper than writing guild combat from scratch. [E10–E11]
6. **Content and settlement have useful foundations.** JSON definitions, event ledgers, receipt-based mutations, deterministic simulation inputs, and scheduled jobs make finite systemic expansions plausible. These are useful building blocks, not a guarantee that adding a mode is cheap. [E3, E7, E12, E14]

## 4. Major Weaknesses / Missing Layers

### Existing depth is spread across separate decisions

Equipment and essence loadouts have activity mappings, while Combat Style selection has its own state. The player can own the pieces of a specialized build without having one durable “this is my complete anti-summon build” object. Current quest pinning also follows authored quests rather than arbitrary personal collection/investment goals. [E4, E12]

### Content mastery is more numeric than expressive

Dungeon mastery already provides visibility, Vigor efficiency, currency bonuses, and equipment drop chance. That is valuable, but leveling mastery is not the same accomplishment as solving a dungeon under a meaningful constraint. Existing no-weapon/no-defeat achievement requirements show a starting point; the new opportunity is a coherent, visible portfolio of such feats. [E6, E13]

### Social infrastructure needs a shared tactical purpose

Current weekly guild definitions are predominantly totals: creatures, dungeon rooms, absorbed essences, completed dungeons. They reward activity but do not inherently require members to plan complementary contributions. Meanwhile, raid wings and tower parties already create that need elsewhere. [E8, E10–E11]

Guild size also matters: the inspected mission creation uses authored `BaseTarget` values. The monster mission's 432,000-creature target should be evaluated against eligible active membership and spawn composition, rather than assumed equally appropriate for every guild. A creature count is not an encounter count.

### Economic gaps are specific, not universal

| Resource | Current sources/uses | Assessment |
|---|---|---|
| Cinders | Combat/rewards/trade; reinforcement, blueprint application, and marketplace transaction fees. | Has substantial sinks. Tier-1 rank 0→5 costs total 345,650 Cinders; tier 2 costs 691,300 from the current price arrays. Inflation cannot be concluded without earning/spending telemetry. |
| Reinforcement Parts | Dismantled gear → reinforcement; tier-1 rank 0→5 costs 155 parts, tier 2 costs 310. | Existing item sink. Additional gear-consuming systems must compete fairly with upgrades and donations. |
| Soulstones | Several reward paths → seven finite constellation definitions. | Broad source access and finite sinks create a plausible late-game surplus risk, not a demonstrated surplus. |
| Essence Dust / monster cores | Duplicate essences and rewards; leveling and ascension. | Already support build breadth. Catch-up ascension discounts after ten tier-1/tier-2 ascensions also exist. |
| Sigils / fragments | Combat and shops/rewards → dungeon access and sigil assembly. | Useful connection. Universal cheap conversion could erase dungeon identity and economic scarcity. |
| Fate Echo | Prophecy-related rewards and paid rerolls. | Existing narrow currency; improve the decision it serves before adding more currencies. |
| Guild Favor / Guild Supplies | Personal guild reward spending / collective buildings. | Different ownership makes this separation defensible. Current shop has four authored stocks; hidden buildings are not finished sinks. |
| Arena Glory | Arena/tournaments → titles, Soulstones, fragments, and cores. | Already connects PvP to the broader game; no new PvP currency needed. |
| Raid Trophies | Guarded reward grants → implemented vendor with zero authored stock. | Unfinished loop; current reward default is disabled. Finish the reward design before enabling a source. |
| Tower Tokens | First-clear/Echo grants; no spending path found. | The clearest incomplete currency loop in inspected code. |

Marketplace listings and commodity buy orders already exist, including escrow/refunds and expiry settlement. The configured seller fee is 300 basis points, with a minimum fee of one Cinder. Trading redistributes money; only the fee removes it. Adding a market is not an opportunity; improving demand for varied items is. [E3, E9–E11, E14]

### Too many clocks could undermine the idle promise

Daily prophecy selection, guild orders, arena incentives, weekly missions/stocks, focus switching, offline limits, and event windows should be considered together. A sensible addition lets a player commit once and make progress through normal play. More independent daily checklists would increase attendance pressure.

### Long-term identity should not become inherited combat dominance

A veteran can already have better gear, more options, titles, and collection bonuses. A new account progression system should preferentially preserve knowledge, challenge access, records, and configuration convenience. Permanent uncapped account power would compound every existing progression layer and worsen entry barriers.

## 5. Underutilized Existing Systems

| Existing asset | Additional gameplay it could support | Boundary to respect |
|---|---|---|
| Creature kill/source history and focus | Personal hunt planning; tactical dossiers; regional contracts. | A second kill-XP track would add little. |
| Essence collection membership and ascension | Suggest a useful missing member and its sources; contextual build tests. | Collection bonuses and catch-up already exist. |
| Equipment + essence activity loadouts | Complete named build presets. | Switching must settle earned idle work and respect locked snapshots. |
| Detailed combat statistics and compact telemetry | Compare attempts; explain observed failure patterns. | Existing stats UI is already extensive. Do not sell a damage meter as new. |
| Dungeon route/Vigor state and completion records | Mastery feats and carefully bounded rule variants. | Some new predicates need new facts; historical runs may not contain them. |
| Achievement account scope and event ledgers | Durable accomplishment records and challenge unlocks. | `SeasonId` fields alone do not implement season settlement. |
| Guild contribution ledger and weekly selection | Cooperative expedition charters. | A global raid completion does not automatically produce a guild-eligible completion event. |
| Guild-owned equipment and borrower checks | Armory requests matched to actual donated gear. | Preserve binding, guild ownership, and return-on-departure rules. |
| Raid vendor purchase limits and unlock predicates | Meaningful, bounded Trophy purchases. | Empty content and disabled reward defaults require an explicit economy decision. |
| Tower Echo mode, first-clear records, party snapshots | Legacy spending and versioned challenge records. | Normal Echo replay already exists; first-clear significance must survive. |
| Event quest global/personal progress | Community choices with temporary gameplay consequences. | An “enabled” JSON file does not prove a production event is running. |
| Balance harness and real combat preparation | Bounded player practice against discovered enemies. | The developer harness is not a player API and cannot simply be exposed. |

## 6. Broad Feature Ideation Pool

I considered **44 candidates**. Twenty survive as additions or substantive expansions; twenty-four are rejected or deferred as standalone work. “Survives” means worth considering, not a commitment to build.

| ID | Candidate | Decision |
|---|---|---|
| F01 | Personal Pursuit Board spanning collections, upgrades, and encounters | Keep |
| F02 | Complete Build Presets linking equipment, essences, and Combat Style | Keep |
| F03 | Combat Debrief and Attempt Comparison | Keep |
| F04 | Dungeon Mastery Feats | Keep |
| F05 | Raid Trophy Requisitions | Keep, conditional on raid reward direction |
| F06 | Tower Legacy Exchange | Keep |
| F07 | Guild Expedition Charters | Keep |
| F08 | Dungeon Rule Variants | Keep |
| F09 | Veteran Regional Contracts | Keep |
| F10 | Tactical Creature Dossiers | Keep |
| F11 | Discovered-Encounter Practice | Keep |
| F12 | Flexible Prophecy Commitments | Keep |
| F13 | Normalized Tournament Exhibition | Keep, later |
| F14 | Durable Season Records | Keep |
| F15 | Guild Armory Requests | Keep |
| F16 | Saved Cooperative Formations | Keep |
| F17 | Shareable Build Pages | Keep |
| F18 | Sanctuary of Deeds: personal long-term projects | Keep as a later framework |
| F19 | Branching Community Campaigns | Keep, later |
| F20 | Tower Echo Challenge Records | Keep |
| R01 | Add a bestiary | Reject: already exists |
| R02 | Add essence pity/focus | Reject: already exists |
| R03 | Add collection stat bonuses | Reject: already exists; avoid multiplier growth |
| R04 | Add equipment reinforcement | Reject: already exists |
| R05 | Salvage unwanted gear into an upgrade resource | Reject: current dismantling already fills this role |
| R06 | Add basic equipment/essence loadouts | Reject: already exists; F02 is the missing connection |
| R07 | Add branching dungeons and mastery XP | Reject: already exists |
| R08 | Add a tower or generic raid mode | Reject: already exists |
| R09 | Add team tournaments | Reject: team flows already exist |
| R10 | Add guild bank/donations/loans | Reject: already exists |
| R11 | Add marketplace or commodity buy orders | Reject: already exists |
| R12 | Add titles and general leaderboards | Reject: already exists |
| R13 | Add a global contribution event framework | Reject: already exists; F19 changes consequences |
| R14 | Restore Crafting/Gathering | Reject: contradicts current direction |
| R15 | City-builder with wood, stone, workers, and queues | Reject: high subsystem cost and unrelated resource chores |
| R16 | Idle NPC expeditions generating resources while combat also runs | Defer/reject: duplicates the idle income loop and needs a new balance model |
| R17 | Permanent combat-stat prestige resets | Reject: undermines collections and compounds power gaps |
| R18 | Endless HP-scaling dungeon floors | Reject: overlaps tower/raid progression and has weak tactical value |
| R19 | Real-time manual combat raids | Reject: changes the interaction model and raises scheduling burden |
| R20 | Territorial guild wars with map ownership | Defer: population, matchmaking, diplomacy, and content/operations cost |
| R21 | Another daily bounty board | Reject: overlaps Prophecies and guild orders |
| R22 | A new monster-mastery currency and XP bar | Reject: overlaps archive/resonance/mastery without a new decision |
| R23 | Pure housing decoration/cosmetic wardrobe | Defer: character expression is useful, but gameplay return is lower here |
| R24 | Full configurable combat scripting/mana system | Defer: major engine/UX expansion before current choices are validated |

## 7. Rejected Ideas and Why

The most consequential rejection is **adding named systems that are already implemented**. Bestiary, focus, pity, mastery, loadouts, guild loans, trade orders, team tournaments, and server contribution events would produce misleading recommendations if inferred from older documents alone.

The second rejection is **parallel progression with no new decision**. A monster mastery level would compete with creature kills, resonance, collections, and essence XP. A second gear-upgrade resource would compete with parts and Cinders. A permanent account-stat reset loop would deepen veteran advantage without supplying new encounters.

The third rejection is **population-hungry breadth**. Territorial warfare or several separate tournament queues need enough concurrent interest to work. Existing team formation already has coordination costs. Shared formations, charters, and occasional exhibitions create social value with much less population risk.

The fourth rejection is **attendance disguised as content**. Another daily bounty list, building timers, or NPC dispatch timers would make returning more obligatory rather than more interesting. F12 deliberately revisits the cadence of an existing system.

Cosmetics are not inherently bad. Titles and recognition are useful rewards for demonstrated achievements. They rank poorly here only when the proposal consists primarily of producing a new cosmetic catalog without improving play, cooperation, or accomplishment.

The retained practice, dossier, and debrief ideas are related but distinct: practice supplies a controlled experiment; dossiers describe known enemy mechanics before entry; debriefs explain evidence from a completed attempt. Their first releases should share presentation and data work rather than become three disconnected “analysis” screens.

## 8. Detailed Surviving Feature Concepts

Scope estimates include domain rules, persistence where necessary, application/API work, player UI, and focused verification. **XS** means a very small change; **S** a narrow extension; **M** coordinated work across an existing feature; **L** substantial new rules or several feature boundaries; **XL** a major subsystem. These are comparative estimates, not delivery promises. None of the twenty deserves an XS label simply because its UI looks small.

### F01 — Personal Pursuit Board

**Core concept.** Let players pin a few personal goals such as completing Goblin Warband, obtaining an Execution blueprint, reinforcing a chosen item, or meeting a raid entry requirement. Show the relevant sources, remaining requirements, and next useful action together. This extends the current quest tracker to goals the player chooses outside an authored quest.

- **Player fantasy:** “I know what my character is working toward, and today's hunt contributes to it.”
- **Loop:** Choose a goal → inspect missing requirements → fight its sources → review actual progress → choose the next investment.
- **Fit and reuse:** `CreatureArchiveService` supplies source locations; `EssenceCodexCollectionService` supplies membership; equipment blueprint previews expose source/guarantee progress; `QuestService` and the quest tracker establish the interaction pattern. [E3, E5, E12]
- **New development:** A small set of goal types and saved references; a read model combining existing progress; a compact frontend panel with source links. No universal quest-authoring engine.
- **Progression / stage:** Collection, Horizontal, Retention; all stages, strongest after onboarding.
- **Scope / content / value:** **M / Low / High.**
- **Risks:** Stale requirements after balance edits; spoilers from undiscovered sources; falsely precise drop ETAs; a checklist that pressures players to fill every slot. Keep goals optional and make uncertainty visible.

### F02 — Complete Build Presets

**Core concept.** Save a complete named configuration combining an equipment loadout, essence loadout, and Combat Style choices. Let a player review and apply the configuration as one coherent choice, with clear feedback when an item is missing or a mode has already locked its snapshot. Existing activity-specific loadouts remain the underlying building blocks.

- **Player fantasy:** “I am prepared to tank this guardian, clear its adds, or hunt efficiently without rebuilding my character by hand.”
- **Loop:** Assemble a specialized build → save it → select an encounter → apply a valid preset → compare performance.
- **Fit and reuse:** `EquipmentLoadoutService`, `EssenceSystemService`, `CombatStyleService`, `CombatStyleRules`, and character snapshot/build mutation boundaries already own most constituent rules. [E4, E7]
- **New development:** A linked preset definition, coordinated validation/application, preview of unavailable components, and a single selection surface. The operation needs coherent failure behavior rather than a frontend sequence of unrelated writes.
- **Progression / stage:** Horizontal, Collection, Cooperative, Retention; midgame onward.
- **Scope / content / value:** **M / Low / Very High.**
- **Risks:** Snapshot exploits, retroactively changing idle rewards, missing loaned items, and accidental overwrites of activity choices. Do not automatically switch builds midway through combat or a locked tournament phase.

### F03 — Combat Debrief and Attempt Comparison

**Core concept.** Add a concise interpretation layer above existing damage, healing, barrier, threat, and ability statistics. Compare two compatible attempts and highlight observed differences: add-clear timing, wasted healing, a fragile participant, or an ability contributing little. State what the evidence shows without claiming to calculate the perfect build.

- **Player fantasy:** “I understand why we failed and can make a deliberate improvement.”
- **Loop:** Finish a fight → inspect a few supported observations → change one choice → repeat → compare results.
- **Fit and reuse:** `CombatResult`, `CompactCombatTelemetry`, current combat statistics components, replay snapshots, and the harness's comparison approach. Existing meters are explicitly not the new feature. [E7]
- **New development:** Versioned comparable summaries, a small rule-based observation set, saved comparison references, and a frontend difference view. Some diagnostics may need additional telemetry; exclude those from the first release.
- **Progression / stage:** Horizontal, Cooperative, Competitive; all stages.
- **Scope / content / value:** **M / Low / High.**
- **Risks:** Mistaking correlation for cause, comparing different enemy tuning or random circumstances, storage growth, and encouraging players to optimize damage while ignoring their role. Label comparisons by encounter, configuration, and content version.

### F04 — Dungeon Mastery Feats

**Core concept.** Give each dungeon family a small, permanent set of tactical accomplishments alongside existing mastery levels. Examples include a restricted essence-slot clear or successfully completing a declared treasury-risk route; exact conditions should be chosen from encounter facts that can be verified. Completing a coherent group opens an optional challenge variant and records the achievement, rather than adding permanent global damage.

- **Player fantasy:** “I have mastered this place in several ways, not merely farmed it.”
- **Loop:** Choose a feat → prepare an appropriate build → complete the run under its rules → earn a permanent record/access unlock → attempt a different solution.
- **Fit and reuse:** `DungeonRunService`, route/Vigor state, `DungeonCompletionRecord`, achievement requirements, and `DungeonMasteryService`. No-weapon and no-defeat achievements already exist; reuse them within a more deliberate set. [E6, E13]
- **New development:** Feat definitions, run-start commitments, validation of a few predicates, durable records, bounded reward claims, and a visible checklist on the dungeon page.
- **Progression / stage:** Horizontal, Collection, Endgame, Retention; midgame through endgame.
- **Scope / content / value:** **M / Medium / Very High.**
- **Risks:** Trivialization by overgearing, inaccessible feat combinations, retroactive awards without enough evidence, and feats becoming mandatory chores. Use appropriate difficulty/gear boundaries and permanent availability.

### F05 — Raid Trophy Requisitions

**Core concept.** Finish the existing raid vendor with a small selection of useful rewards, such as bounded supplies of existing cores or sigil fragments gated by accomplishments. Preserve raid victories as the source of Trophies and choose rewards that help players develop alternate builds. This is conditional on deciding to activate and balance the existing reward path.

- **Player fantasy:** “Even an imperfect expedition contributes to preparing my next useful build.”
- **Loop:** Participate in an eligible raid → claim its earned Trophies → choose a useful requisition → improve the next roster/build.
- **Fit and reuse:** `RaidService` already has claims, vendor eligibility, weekly/lifetime limits, item delivery, and currency spending; `trophy-vendor.json` is empty. [E11]
- **New development:** Authored stock and price/reward tuning, player-facing availability clarity, and validation of the complete claim-to-purchase path. New reward kinds would enlarge scope and are unnecessary initially.
- **Progression / stage:** Cooperative, Economic, Horizontal, Retention; late game/endgame.
- **Scope / content / value:** **S / Low / High if raids are being activated.** End-to-end reward launch/balance is a separate **M** effort if still unfinished operationally.
- **Risks:** Rewards default off; do not treat this as a live broken shop. Excessively efficient cores could make dungeons obsolete, while unlimited rewards for repeated weak outcomes could encourage intentional failure. Reward and participation limits must be evaluated together.

### F06 — Tower Legacy Exchange

**Core concept.** Give Tower Tokens a bounded purpose after first clears and Echo victories. A compact exchange could offer existing build-development supplies and permanent tower challenge access tied to personal floor accomplishments. Spendable rewards should not buy direct victory over the next server-first guardian.

- **Player fantasy:** “Helping the tower advance leaves me with useful choices and a lasting expedition history.”
- **Loop:** Clear or revisit an eligible floor → earn Tokens → select a supply or legacy unlock → prepare for another distinct challenge.
- **Fit and reuse:** `WorldTowerService` already grants Tokens and records clear/Echo participation. Raid and Champion Market purchase patterns provide precedents, but no tower spending implementation was found. [E9–E11]
- **New development:** Tower stock/eligibility definitions, purchase receipts and limits, token spending rules, and a compact exchange UI.
- **Progression / stage:** Economic, Cooperative, Collection, Endgame; late game/endgame.
- **Scope / content / value:** **M / Low / High.**
- **Risks:** Existing token balances may differ greatly; analyze them before setting prices. Repeated easy Echo farming could dominate rewards. Avoid a freely convertible currency network or token-only best equipment.

### F07 — Guild Expedition Charters

**Core concept.** Let a guild commit to one multi-part expedition objective using current hunts, dungeons, and cooperative encounters. Members choose complementary contributions toward a shared destination—for example, regional preparation followed by a qualifying guild party clear. Extend weekly mission selection rather than introducing another guild mission board.

- **Player fantasy:** “My smaller contribution helps the guild accomplish something we planned together.”
- **Loop:** Select a charter → members choose complementary jobs → assemble an eligible party → finish the objective → distribute existing contribution-based rewards.
- **Fit and reuse:** `GuildMissionService`, mission instances/contribution ledgers, membership, and current raid/tower rosters. Guild missions presently focus on totals; this adds a shared tactical endpoint. [E8, E10–E11]
- **New development:** Multi-part charter rules, qualifying roster/membership facts, adapters from encounter completion, contribution presentation, and a shared progress panel.
- **Progression / stage:** Social, Cooperative, Retention; midgame onward.
- **Scope / content / value:** **M / Medium / High** for one bounded charter format; a branching campaign would be L.
- **Risks:** Large-guild advantage, late join/leave reward abuse, carrying, and officers choosing inaccessible content. Set expectations using eligible active membership and let preparation count without demanding synchronized attendance.

### F08 — Dungeon Rule Variants

**Core concept.** Reuse a dungeon family under a clearly declared tactical rule such as enhanced enemy barriers or altered rest-site recovery. Offer a small curated set of persistent variants rather than an arbitrary stack of modifiers. A variant should change which build or route is attractive while retaining the original dungeon's identity.

- **Player fantasy:** “The place is familiar, but this expedition requires a different plan.”
- **Loop:** Inspect the variant → choose a counter-build/route → complete it → earn a record and bounded existing rewards.
- **Fit and reuse:** Dungeon definitions, route state, encounter preparation modifiers, snapshots, reward handling, and existing statuses. These provide hooks; they are not a finished affix framework. [E6–E7]
- **New development:** A small variant catalog, immutable per-run rule selection, modifier application, compatibility checks, reward rules, and preview text.
- **Progression / stage:** Horizontal, Endgame, Retention; midgame onward.
- **Scope / content / value:** **M / Medium / High.**
- **Risks:** Impossible combinations, degenerate easy variants, invalidated records after tuning, and endless balancing combinations. Begin with one rule at a time; avoid daily forced rotation.

### F09 — Veteran Regional Contracts

**Core concept.** Give veterans a finite set of optional return journeys through earlier areas, culminating in an existing or lightly recomposed quest encounter. The commitment can require a regional essence or a bounded equipment profile so an old area asks a different question. Ordinary low-level enemies remain unchanged for new players.

- **Player fantasy:** “I return as an expert and prove something new about a region I know.”
- **Loop:** Accept a permanent regional contract → choose an eligible build → complete a short hunt objective → defeat its trial → record the deed/unlock another challenge.
- **Fit and reuse:** `QuestService`, `QuestEncounterService`, region access rules, creature identities, snapshots, and achievement records. [E1, E5, E12–E13]
- **New development:** Contract definitions, supported build constraints, eligibility-aware progress, a small encounter variant set, and journal presentation.
- **Progression / stage:** Horizontal, Collection, Endgame, Retention; late game.
- **Scope / content / value:** **M / Medium / High.**
- **Risks:** Becoming another repetitive kill quota; rewarding weaker gear without ensuring meaningful difficulty; low-level reward farming. Keep hunt portions short and make the distinctive encounter the point.

### F10 — Tactical Creature Dossiers

**Core concept.** Extend discovered creature entries with concise mechanical profiles: the dangerous ability, relevant condition interactions, and why a particular counter might help. Link from a known mechanic to compatible owned essences without declaring one optimal solution. Start with existing bosses whose mechanics already differ substantially.

- **Player fantasy:** “Learning the enemy changes how I prepare.”
- **Loop:** Discover/fight an enemy → inspect its known mechanics → find a suitable owned response → revisit the fight.
- **Fit and reuse:** `CreatureArchiveService`, creature ability definitions, essence tags/effects, current descriptions, and combat lexicon. Archive source locations and essence links already exist; the addition is tactical interpretation. [E5, E7]
- **New development:** A few curated mechanic/counter annotations, a read-only dossier projection, links to owned abilities, and disclosure rules.
- **Progression / stage:** Horizontal, Collection; early game through endgame.
- **Scope / content / value:** **S / Medium / High.**
- **Risks:** Advice becoming stale or oversimplifying counters; exhaustive hand-authored coverage becoming expensive. Keep derived facts separate from authored suggestions and limit initial coverage.

### F11 — Discovered-Encounter Practice

**Core concept.** Offer a reward-free rehearsal against a small set of already discovered encounters using the player's legal owned build. The goal is to test hypotheses cheaply before committing scarce dungeon entry items or a cooperative roster. Use a bounded seed set or explicit randomness so practice does not imply a guaranteed next-run outcome.

- **Player fantasy:** “I can experiment with the archive I collected without wasting everyone's expedition.”
- **Loop:** Pick a discovered practice encounter → choose a legal preset → simulate → read a debrief → adjust.
- **Fit and reuse:** `CombatPreparationPipeline`, `CombatSetupService`, `CombatEngineExecutor`, snapshots, and BalanceHarness examples of production-backed simulation. [E4, E7]
- **New development:** A dedicated restricted practice boundary, enemy access rules, compute limits, result presentation, and strict separation from reward/progression writers. Do not expose developer harness endpoints.
- **Progression / stage:** Horizontal, Cooperative; midgame onward.
- **Scope / content / value:** **M / Low / High.**
- **Risks:** Compute abuse, hidden enemy disclosure, simulation/live divergence, accidental XP or loot, and replacing discovery with brute-force optimization. Start with a few encounters and manual comparisons, not automatic best-build search.

### F12 — Flexible Prophecy Commitments

**Core concept.** Preserve Prophecies' choice among alternatives while giving accepted commitments a more forgiving completion window or a tightly bounded carryover. A player should be able to work on the chosen task through ordinary idle play instead of losing its value at an inconvenient reset. This changes cadence, not the number of chores.

- **Player fantasy:** “The game respects the commitment I made even when I cannot log in at a particular hour.”
- **Loop:** Choose a prophecy → progress through normal play → complete within a clear grace window → claim within a stable weekly budget.
- **Fit and reuse:** `ProphecyService` already has offers, acceptance, period boundaries, rerolls, weekly revelation, and progress tracking. [E12]
- **New development:** Carryover/grace eligibility, precise weekly attribution and anti-double-count rules, expired-state handling, and clear remaining-time UI.
- **Progression / stage:** Retention; all eligible stages.
- **Scope / content / value:** **M / Low / High.**
- **Risks:** Multiplying weekly rewards, ambiguous occurrence versus settlement time, hoarding favorable tasks, and undermining current Fate Echo reroll value. Preserve the weekly reward budget and test cross-period offline settlement.

### F13 — Normalized Tournament Exhibition

**Core concept.** Occasionally replace or accompany a regular event with an exhibition whose explicit rules normalize key power variables. Players still select legal essence combinations and styles, but account age contributes less to the outcome. This is a new ruleset over existing tournament machinery, not a new live PvP game.

- **Player fantasy:** “My understanding of builds can matter even against a much older character.”
- **Loop:** Inspect fixed exhibition rules → prepare a qualifying build → lock the normalized snapshot → compete → review results.
- **Fit and reuse:** `TournamentGroundsService`, teams, brackets, snapshots, playback, and scoring. [E4, E7, E9]
- **New development:** Explicit normalization of equipment, essence progression, style progression, and relevant bonuses; a separate ruleset/version identity; eligibility and rewards; a readable preview.
- **Progression / stage:** Competitive, Horizontal; midgame/endgame.
- **Scope / content / value:** **L / Medium / High**, conditional on player population.
- **Risks:** Incomplete normalization, disincentivizing collection, multiplying queues, and a dominant exhibition meta. Run occasional events; do not create a permanent second ladder first.

### F14 — Durable Season Records

**Core concept.** Settle tournament seasons into permanent records with final standings, a player's best result, and a small number of earned distinctions. Preserve the period's rules so later balance changes do not rewrite the meaning of the result. Current monthly points and Hall of Fame become the input rather than being replaced.

- **Player fantasy:** “My competitive season becomes part of my account's history.”
- **Loop:** Compete over a season → improve a personal target → settle final results → retain a record → enter the next season with a new goal.
- **Fit and reuse:** Current tournament placements/monthly scoring, achievement/title account scope, and durable job execution patterns. [E9, E13–E14]
- **New development:** Season definition and settlement records, retry-safe grants, historical views, and rules for late corrections/restricted accounts.
- **Progression / stage:** Competitive, Collection, Retention, Cosmetic; midgame/endgame.
- **Scope / content / value:** **M / Low / High.**
- **Risks:** Attendance dominating skill, winner-only motivation, settlement corrections, and uncapped reward power. Include personal milestones and identity rewards without resetting accumulated character power.

### F15 — Guild Armory Requests

**Core concept.** Let members state a concrete equipment need and match it against guild-owned items or future donations. Officers can resolve a request through the existing loan flow. This makes the vault serve a planned build rather than requiring members to repeatedly ask whether anything useful has arrived.

- **Player fantasy:** “My spare discovery helps a guildmate take on a role the group needs.”
- **Loop:** Post a slot/style requirement → match available donated gear → approve an existing loan → use the item → return it under current ownership rules.
- **Fit and reuse:** `GuildVaultService`, equipment descriptors, membership/roles, borrower validation, and guild chat. Donations and loans already exist. [E3, E8, E14]
- **New development:** Request persistence and status, a simple descriptor matcher, officer/member UI, and optional in-game activity notices.
- **Progression / stage:** Social, Cooperative, Economic, Horizontal; early game onward.
- **Scope / content / value:** **M / Low / High** for active guilds.
- **Risks:** Officer workload, stale requests, pressure to donate valuable unbound drops, and returned items invalidating presets. Keep requests voluntary and preserve all binding rules.

### F16 — Saved Cooperative Formations

**Core concept.** Allow a raid or tower leader to save a formation template using existing party/wing positions and intended build roles. Reapply the template to a new expedition, flagging missing members and unavailable configurations for confirmation. It saves repeated coordination without freezing the same people into permanent roles.

- **Player fantasy:** “Our team learns and improves between expeditions instead of rebuilding its plan from scratch.”
- **Loop:** Build a successful formation → save the template → create the next run → reconcile current members/snapshots → adjust and attempt.
- **Fit and reuse:** `RaidService.UpdatePartiesAsync`, `PreviewBattlePlanAsync`, `WorldTowerService.UpdateRallyPartiesAsync`, and `WorldTowerPartyRules`. [E10–E11]
- **New development:** Template storage, valid-assignment mapping, ownership/edit permissions, and a review/apply UI.
- **Progression / stage:** Cooperative, Social, Retention; late game/endgame.
- **Scope / content / value:** **M / Low / High** for repeat groups.
- **Risks:** Roster changes, stale snapshots, role locking, and leader authority over other players' builds. Templates should suggest positions; they must not silently mutate members' characters.

### F17 — Shareable Build Pages

**Core concept.** Publish an opt-in read-only build description containing essence order, Combat Style choices, equipment requirements, and a short author note. A recipient can compare it against their own archive and see missing components. This enables advice and discovery without making imported text an executable combat script.

- **Player fantasy:** “I can teach a guildmate what worked, and adapt their idea to the equipment I actually own.”
- **Loop:** Save a build → share its page/link in existing chat → inspect differences → acquire missing options → create a personal variant.
- **Fit and reuse:** Loadout read models, snapshots, catalog identities, current character/item inspection, and chat link handling. [E4, E7, E14]
- **New development:** Explicit publication/versioning, a safe read-only projection, owned/missing comparison, and a share/inspect UI. Existing snapshots are not automatically safe public documents.
- **Progression / stage:** Social, Horizontal, Collection; all stages.
- **Scope / content / value:** **M / Low / High.**
- **Risks:** Exposing private inventory, stale balance assumptions, homogenized builds, and copied configurations lacking context. Publish only selected data and display content version and author intent.

### F18 — Sanctuary of Deeds

**Core concept.** Create a personal investment framework with a small selection of permanent projects earned through combat accomplishments and paid for using existing resources. A project opens something concrete—such as a regional challenge chapter or an additional encounter-study option—rather than merely increasing a settlement level. Choose one active project at a time; completed projects become an enduring record of the character's interests.

- **Player fantasy:** “My adventures gradually establish a place with a purpose that reflects who I am.”
- **Loop:** Choose a project → complete its hunt/collection/feat prerequisites → commit a bounded investment → open a useful capability → choose a different project.
- **Fit and reuse:** Quest prerequisites and choices, collections, achievements, character currencies, and the proposed permanent challenge content. It shares conceptual space with Soulstone Constellations; it must add access/choice rather than copy their passive bonuses. [E5, E12–E14]
- **New development:** Project definitions/state, capability entitlements, deliberate character/account scope, resource payments, and a compact personal-place UI. New gameplay modules are additional work, not free consequences of building the framework.
- **Progression / stage:** Horizontal, Collection, Economic, Retention; midgame onward.
- **Scope / content / value:** **L / Medium / High only after useful project rewards exist.**
- **Risks:** Becoming a prettier checklist, mandatory resource tax, duplicate passive bonuses, and overpromised NPC systems. Do not start with buildings, production timers, a farm, or free parallel income.

### F19 — Branching Community Campaigns

**Core concept.** Extend current server events so the community can choose between a small number of temporary outcomes. Contributions might open one of two existing dungeon rule variants or select a regional challenge chapter. The consequence should alter available play for a broad window rather than only grant another box after a global counter fills.

- **Player fantasy:** “The server's collective choice changes what we explore next.”
- **Loop:** Inspect the campaign alternatives → contribute through ordinary play → resolve the community choice → play the resulting temporary chapter → preserve its history.
- **Fit and reuse:** `EventQuestService`, global/personal contributions, reward claims, outbox announcements, and existing region/dungeon content. [E1, E6, E12, E14]
- **New development:** Branch resolution, outcome activation/expiry, participation attribution, history, and clear campaign choice presentation.
- **Progression / stage:** Cooperative, Social, Retention, Endgame; all eligible stages.
- **Scope / content / value:** **L / Medium / High.**
- **Risks:** Dominant guilds deciding for everyone, low-population failure, permanent missed content, and substantial operations burden. Losing branches should return later; no irreversible server stat advantage.

### F20 — Tower Echo Challenge Records

**Core concept.** Give cleared-floor Echo runs optional, versioned accomplishment goals such as no participant defeats or a restricted essence budget. Record a team's best valid result and roster against that precise challenge. This extends existing Echo replay and Hall of Fame without making faster farming the only reason to return.

- **Player fantasy:** “Our group can become known for mastering a particular guardian in an unusual way.”
- **Loop:** Select a cleared floor and challenge → prepare a legal party → complete the attempt → compare its record → refine the composition.
- **Fit and reuse:** `WorldTowerService`, Echo mode, party snapshots, attempts, combat results, and Hall of Fame. [E7, E10]
- **New development:** Challenge definitions/validation, content-versioned records, eligibility categories, and comparison views. Reward access must remain separate from ordinary first-clear rules.
- **Progression / stage:** Cooperative, Competitive, Horizontal, Endgame; endgame.
- **Scope / content / value:** **M / Medium / High.**
- **Risks:** Gear inflation invalidating comparisons, record farming via random seeds, reducing support roles to speed metrics, and thin competition. Prioritize categorical feats and personal/team records over a universal fastest-clear ladder.

## 9. High-Leverage Opportunities

There are at least ten credible candidates with **S/M development scope and High/Very High expected value**. Their value remains conditional on the underlying feature being available and used. Reuse here is qualitative: I have not measured or claimed “70% complete.” Existing combat code does not remove the need for new eligibility, persistence, reward, and UI work.

| Candidate | Why the development/value ratio is unusually good | Main cost that remains |
|---|---|---|
| F01 — Personal Pursuit Board | Makes existing collections, sources, upgrades, and requirements actionable together. | Consistent cross-feature read models and saved goal references. |
| F02 — Complete Build Presets | Makes every collected sidegrade and essence easier to use; constituent configurations already exist. | Safe coordinated switching and snapshot boundaries. |
| F03 — Combat Debrief | Turns already-generated statistics into better decisions across several modes. | Trustworthy observations and comparable result versions. |
| F04 — Dungeon Mastery Feats | Gives four families new lasting objectives using existing completion/achievement infrastructure. | A small set of meaningful, verifiable conditions. |
| F05 — Raid Trophy Requisitions | Can finish an entire reward-to-choice loop mainly through an existing vendor's content. | Reward activation decision and price/earning validation. |
| F06 — Tower Legacy Exchange | Gives an existing reward currency a purpose without another combat mode. | A narrow purchase flow and bounded stock. |
| F07 — Guild Expedition Charters | Connects current guild contributions to current cooperative encounters. | Guild eligibility, final-clear attribution, and one composite objective format. |
| F10 — Tactical Creature Dossiers | Helps players discover uses for abilities already authored. | Limited, maintained tactical annotations. |
| F11 — Discovered-Encounter Practice | Reuses the production combat preparation/resolution path to reduce experimentation friction. | A safe, compute-bounded player boundary. |
| F12 — Flexible Prophecy Commitments | Improves an existing retention mechanism without authoring more daily content. | Correct period attribution and reward-budget preservation. |
| F15 — Guild Armory Requests | Gives donated items a destination and makes existing loans socially useful. | Request/matching UX and stale-state handling. |
| F17 — Shareable Build Pages | Players supply much of the teaching and build-discovery content. | Safe opt-in publication and catalog-version handling. |

F16 saved formations also qualifies for repeat groups, but its reach depends on how often players repeat cooperative content with similar rosters. It should move up the list if assembling parties is a major observed source of abandonment.

## 10. Existing Content Multipliers

| Existing content | Recommended multiplier | Why the content remains useful |
|---|---|---|
| Earlier creatures | F01 pursuits, F09 contracts, F10 dossiers | Missing archive members become concrete goals; veteran revisits test something beyond raw kill count. |
| Unusual essences | F02 presets, F03 debriefs, F11 practice, F17 sharing | Players can find a niche, test it, save it, and teach it. |
| Low-demand equipment variants | F02 presets, F04 feats, F15 armory requests | Situational gear supports another challenge or another member's role. |
| Existing dungeon families | F04 feats and F08 rule variants | The same routes/encounters ask new preparation and risk decisions after mastery rewards are exhausted. |
| Cleared tower floors | F06 exchange and F20 challenge records | Echo runs contribute to a bounded investment choice or a different accomplishment. |
| Current raid bosses | F05 requisitions, F07 charters, F16 formations | Rewards develop future rosters, and groups retain what they learned. |
| Tournament machinery | F13 exhibitions and F14 season records | Occasional alternate rules and durable legacy extend the value of the same bracket/replay system. |
| Existing regions and event machinery | F19 campaigns | Community outcomes temporarily change the opportunity presented by familiar places. |

The strongest multiplier is the combination **complete builds + understandable outcomes + distinct challenges**. A new essence is more valuable when a player has somewhere to use it, a way to test it, and a way to return to that configuration.

Do not make every old region yield current-tier equipment at the same efficiency as the latest region. That would erase advancement. Preserve older areas through unique collections, identity, conditional challenges, and bounded alternate-build opportunities.

## 11. Missing Glue Systems

| Connection | Current state | Useful addition |
|---|---|---|
| Monsters ↔ Essences | Strong: authored source tables, archive, focus, resonance. | F01/F10 make source pursuit and tactical use clearer. Do not add another pity track. |
| Monsters ↔ Equipment | Strong acquisition connection, but ordinary drops are broad pools. | Show relevant existing variant/source information through F01; preserve drop-based acquisition. |
| Equipment ↔ Buildcraft | Stats, sets, variants, upgrades, and equipment loadouts exist. | F02 makes the equipment choice part of a complete build; F04 gives alternatives a job. |
| Essences ↔ Long-term progression | XP, ascension, Codex bonuses, and constellation support already connect them. | F11/F17 turn archive breadth into practical experimentation and learning. |
| Achievements ↔ Character progression | Many requirements and titles exist; no need for a generic permanent-stat reward layer. | F04/F18 can use accomplishments to open optional challenges/capabilities. |
| PvP ↔ Account progression | Glory buys broader progression supplies; account-scoped achievement/title structures exist. | F14 preserves seasonal accomplishments. Avoid permanent winner power multipliers. |
| Guilds ↔ Character progression | Favor, shop supplies, missions, and loans already exist. | F07/F15 connect progression to an agreed role or group objective. |
| Dungeons ↔ Regions | Region tiers, source identities, sigils, and quest access already connect them. | F09 makes regional return journeys lead into existing dungeon trials. |
| Old regions ↔ Endgame | Collection/source reasons persist, then diminish after completion. | F09 and F20 offer bounded mastery, not endless old-area XP. |
| Prophecies ↔ Core gameplay | Event progress and themed objectives exist. | F01 makes overlap visible; F12 reduces pressure from rigid completion timing. |
| Collections ↔ Gameplay benefits | Applied Codex bonuses already exist. | Prefer unlocking use cases and challenge access over further passive accumulation. |
| Combat feedback ↔ Next decision | Detailed statistics exist; multi-attempt interpretation is less coherent. | F03 explicitly connects what happened to the next experiment. |

The most isolated elements are **Tower Tokens, the unfinished raid reward/vendor loop, future guild building identities, and the gap between owning several build components and operating complete builds**. These are stronger targets than a new standalone progression currency.

## 12. Personal Investment / Stronghold-Type Opportunities

There is room for a personal investment system, but only if it supplies a kind of ownership the existing archive, equipment investment, and constellations do not already provide.

The useful design question is: **What lasting choice does the player make here, and what can they do afterward that they could not do before?** If the answer is only “earn resources slightly faster,” use or adjust an existing progression system instead.

### Four substantially different interpretations

| Interpretation | Distinct player decision and investment | What it could provide | Cost/burden | Verdict |
|---|---|---|---|---|
| **Sanctuary of Deeds** | Choose a long-term project based on specific collections, feats, and bounded Cinder/Soulstone investment. | Permanent access to optional regional trial chapters and study capabilities; a record of chosen accomplishments. | L / Medium; depends on worthwhile capabilities existing. | Best eventual framework; retained as F18. |
| **Commander's Keep** | Invest in preparation facilities tied to the encounters the player actually leads. | Formation libraries, rehearsal tools, and expedition planning conveniences. | M–L / Low content, substantial cross-feature UI. | Useful fantasy, but first deliver F11/F16 directly. Do not lock basic group usability behind a building grind. |
| **Reliquary of Echoes** | Collect trophies and choose one of several mutually exclusive, bounded local expedition preparations. | Different approaches to selected challenge modes, with costly changes expressed using existing resources. | L / Medium; new modifier combinations require validation. | Defer. Risks becoming another set/constellation system unless preparations change tactics rather than yields. |
| **Warden's Domain** | Complete regional commitments and choose which region to develop next. | NPC contacts or regional contract branches that unlock specific hunts and challenge access. | L / High if NPC stories are extensively authored. | Strong narrative identity, weaker solo-developer efficiency. Start with F09 before building an estate layer. |

These are alternatives, not four systems to ship together. Commander's Keep, Reliquary, and Domain are not additional surviving implementation recommendations beyond their identified existing-feature components.

### Recommended boundaries

For an eventual F18, start with **three finite projects and one active commitment**, using existing achievements/collections as prerequisites. At least one project must provide genuinely new playable access. Avoid a generic “Sanctuary level,” resource production, construction queues, NPC happiness, raids on houses, or another currency.

Keep character investment character-owned initially. Account-wide records and challenge discovery can be considered separately using existing achievement/title scope. Do not silently make Cinder purchases grant every future character a permanent power advantage.

Do not charge players simply to see their collections or use a sensible build interface. A sanctuary can celebrate achievements and provide optional capabilities; it should not manufacture inconvenience to sell its own upgrades.

The sequence matters: **prove a useful project reward first; build the personal framework second**. If three meaningful projects cannot be described without inventing unrelated subsystems, defer the stronghold.

## 13. Endgame Opportunities

| Veteran question | Current answer | Recommended development |
|---|---|---|
| What am I optimizing? | Gear quality/rarity/rolls/rank, ascensions, styles, team composition, fight outcomes. | F02/F03/F11 make optimization understandable and testable. |
| What am I collecting? | Essences, Codex sets, equipment variants, achievements, titles. | F04/F20 add a portfolio of completed tactical feats. |
| What am I competing over? | Arena rating, tournament results/monthly points, first-clear recognition. | F14 preserves seasons; F13 offers occasional competition with less accumulated-power advantage. |
| What am I cooperating on? | Tower floors, raids, regional bosses, guild totals, server events. | F07 ties group play to guild purpose; F16 preserves successful plans. |
| What remains difficult? | Later released guardians/raids and stronger opponents. | F04/F08/F20 change constraints, composition, and route choices rather than multiplying HP indefinitely. |
| Why keep improving? | Reach remaining content, optimize results, broaden the archive. | Make a new build unlock a new solution, not merely a higher power-rating display. |
| Why return to Shenic? | Missing essences, collection bonuses, dungeon sources and mastery. | F09 provides permanent return contracts when collection reasons diminish. |
| What distinguishes two veterans? | Build, progress, titles, social position, first-clear history. | Complete-build portfolios, specialist challenge records, saved team plans, and eventually selected personal projects. |

The preferred endgame is **mastery across a set of different problems**. A tanking build, an add-clear build, a constrained solo build, and a coordinated party clear should represent different accomplishments. A single generalized power total should not explain all of them.

Keep permanent feats available. Use seasons for renewed competition and recognition, not forced account resets or exclusive power. Keep community events broad enough that asynchronous participation matters and missing one evening does not erase the opportunity.

## 14. Top 15 Features I Would Seriously Consider Building

This ranking emphasizes strengthening other systems and reducing recurring content work. Cost is the bounded version described above. Conditional reward work is ranked below improvements with broader current reach.

| Rank | Feature | Gameplay Impact | Dev Cost | Content Cost | Existing Systems Reused | Why Build It |
|---|---|---|---|---|---|---|
| 1 | F02 — Complete Build Presets | Very High | M | Low | Equipment/essence loadouts, styles, snapshots | Makes collected alternatives practical everywhere. |
| 2 | F04 — Dungeon Mastery Feats | Very High | M | Medium | Runs, Vigor/routes, achievements, completion facts | Adds lasting goals and reasons to solve old content differently. |
| 3 | F01 — Personal Pursuit Board | High | M | Low | Archive, Codex, blueprint previews, quest tracker | Converts existing progression breadth into a clear personal plan. |
| 4 | F03 — Combat Debrief and Attempt Comparison | High | M | Low | Combat results, telemetry, stats UI, snapshots | Makes experimentation productive instead of opaque. |
| 5 | F07 — Guild Expedition Charters | High | M | Medium | Guild missions/ledger, tower/raid rosters | Gives guild activity a cooperative destination. |
| 6 | F05 — Raid Trophy Requisitions | High, conditional | S | Low | Existing vendor, claims, eligibility, limits | Finishes a reward choice if raids are moving toward reward activation. |
| 7 | F06 — Tower Legacy Exchange | High | M | Low | Tokens, clears, Echo participation, shop precedents | Gives an existing currency a bounded purpose. |
| 8 | F10 — Tactical Creature Dossiers | High | S | Medium | Archive and ability catalogs | Helps players recognize the value of unusual counters. |
| 9 | F17 — Shareable Build Pages | High | M | Low | Loadouts, snapshots, catalog IDs, chat | Lets players teach each other and creates demand for alternatives. |
| 10 | F11 — Discovered-Encounter Practice | High | M | Low | Production combat path and harness precedent | Makes deliberate build testing accessible. |
| 11 | F12 — Flexible Prophecy Commitments | High | M | Low | Existing periods, choices, progress, rewards | Improves retention without another chore system. |
| 12 | F15 — Guild Armory Requests | High for active guilds | M | Low | Donations, ownership, loans, equipment descriptors | Connects surplus equipment to real group needs. |
| 13 | F16 — Saved Cooperative Formations | High for repeat groups | M | Low | Party/wing assignments, snapshots | Reduces repeated coordination work. |
| 14 | F08 — Dungeon Rule Variants | High | M | Medium | Dungeon definitions, preparation, rewards | Expands replayability once feats/build usability are proven. |
| 15 | F20 — Tower Echo Challenge Records | High | M | Medium | Echo mode, attempts, parties, Hall of Fame | Gives cleared guardians new tactical significance. |

F14 durable seasons is close behind, and should rise if tournament participation is a core retention driver. F09 regional contracts has useful identity but more authored content burden. F13 normalized exhibitions, F18 Sanctuary, and F19 branching campaigns are credible later work, not the best immediate investment.

## 15. My Recommended Next Five Features

### 1. Complete Build Presets

1. **Why selected:** Build diversity only pays off if changing between valid builds is practical. This strengthens equipment, essences, styles, dungeons, and cooperative roles at once.
2. **Why now:** Separate loadout systems and a style mutation boundary already exist. This is a natural consolidation of recent depth.
3. **Systems improved:** Collection utility, equipment sets/variants, essence order, style identity, party preparation.
4. **Complexity:** M. The difficult part is coherent application and preserving idle/snapshot semantics, not rendering a preset dropdown.
5. **Exclude from version one:** Automatic best-build recommendations, automatic mid-run swaps, automatic changes to other players, extra preset currencies, and arbitrary scripting. Support a few complete saved configurations with a clear validity preview.

**Evidence of success:** Players use more than one complete configuration and can reliably restore it. This is a proposed measurement, not an observed baseline.

### 2. Dungeon Mastery Feats

1. **Why selected:** It provides the clearest new gameplay from existing content and turns collection breadth into different solutions.
2. **Why now:** Routes, Vigor, completion records, mastery, and challenge-like achievements already exist.
3. **Systems improved:** Dungeon replayability, alternate equipment, essence experimentation, achievements, long-term goals.
4. **Complexity:** M, with a medium initial design/balance burden. Start with a single family and two or three feats that use reliable facts.
5. **Exclude from version one:** Random affix combinations, an infinite ladder, a new currency, seasonal expiry, extensive story writing, and global stat rewards. Include one meaningful optional challenge/access reward and permanent accomplishment records.

**Evidence of success:** Players revisit the family with different builds after ordinary progression goals; difficulty remains understandable rather than solved solely by excess stats.

### 3. Personal Pursuit Board

1. **Why selected:** The game already offers many goals. Helping players choose one has better value than adding another progression track.
2. **Why now:** The existing quest tracker provides a familiar surface, and archive/blueprint services expose much of the required information.
3. **Systems improved:** Creature hunting, Codex completion, equipment investment, dungeon selection, onboarding-to-midgame transition.
4. **Complexity:** M. First support only a Codex collection, a blueprint source, and an equipment reinforcement goal.
5. **Exclude from version one:** An AI planner, precise RNG completion promises, automatic purchasing, automatic area switching, notification spam, and every possible resource dependency. Preserve discovery boundaries and show the next actionable source.

**Evidence of success:** Returning players can identify their next useful action without visiting several unrelated screens.

### 4. Combat Debrief and Attempt Comparison

1. **Why selected:** A buildcraft game needs players to learn from outcomes. Existing detailed statistics make a small explanatory layer feasible.
2. **Why now:** The combat engine and balance harness already expose useful facts, while recent style and equipment work increases the need to understand tradeoffs.
3. **Systems improved:** Solo encounter learning, cooperative role selection, essence valuation, preparation for feats.
4. **Complexity:** M. First compare two compatible results and show a few deterministic observations supported by current telemetry.
5. **Exclude from version one:** Automated optimal loadout search, speculative causal claims, full replay search/storage, and a universal combat score. Do not implement another basic damage meter; reuse the current one.

**Evidence of success:** A player can state what changed between attempts and choose a plausible next experiment.

### 5. Guild Expedition Charters

1. **Why selected:** It gives social play a purpose using systems already built on both sides of the connection.
2. **Why now:** Missions, contribution records, loans, and cooperative encounter rosters exist, while current guild objectives mainly aggregate activity.
3. **Systems improved:** Guild retention, role planning, gear donations, dungeon/tower participation, cooperation across different power levels.
4. **Complexity:** M for one charter that ends in one chosen encounter type. Use an existing rewarded/available mode; do not depend on turning raid rewards on.
5. **Exclude from version one:** Guild wars, territory, building timers, a new raid engine, guild-only best gear, and a separate daily mission set. Extend the existing selection flow with one composite charter, clear contribution eligibility, and existing rewards.

**Evidence of success:** Members deliberately coordinate different contributions and complete the charter without needing to attend simultaneously.

These five form a useful sequence: **save a complete build → give it a different challenge → make the pursuit clear → learn from results → use that knowledge with a guild**. Debrief and pursuit work can precede feats if player observation reveals basic comprehension to be the immediate bottleneck.

The raid vendor and tower exchange deserve a separate small economy-completion pass. Raid reward activation should remain an explicit product/balance decision; empty vendor content is not authorization to enable it.

## 16. Overall Recommendation

**Prioritize depth and connections over another major system.** The repository is already unusually broad relative to its currently authored regions and dungeon families. Its strongest next phase is to make existing options legible, reusable, socially useful, and worth mastering.

Preserve the good connections: monsters to essence collections, duplicate gear to reinforcement, dungeons to cores/blueprints, and PvP/guild rewards to wider progression. Complete specific unfinished reward loops without adding currencies. Use permanent challenges to make old content useful after its ordinary collection incentives run out.

Challenge or trim features that do not yet justify their place:

- Keep unfinished guild buildings hidden until they provide concrete gameplay. A building whose benefit is “ready for future additions” is not a meaningful investment reward.
- Reassess high Market Office levels whose authored descriptions add no new stock. Expand useful choices or adjust expectations before presenting them as a destination.
- Evaluate all daily/weekly obligations as a combined player workload. Expand Prophecies' flexibility before adding another recurring task list.
- Treat the seven finite constellation definitions and nineteen scaling Codex collections as existing personal progression. A Sanctuary should not duplicate them with more passive efficiency bonuses.
- Preserve the distinction between character growth and account identity. Add durable deeds and challenge access before account-wide combat multipliers.
- Keep new combat rule combinations bounded. The current balance harness is valuable, but it cannot prove that every arbitrary modifier combination will be fun or fair.

Before committing to larger designs, the most useful missing evidence would be actual build switching, encounter abandonment, guild active-member distribution, unused currency balances, resource earning/spending rates, and which content players revisit after their first clears. No numeric retention lift or live economy diagnosis is asserted by this review.

### Delivery and verification

- **Changed file:** This analysis report only. Existing user changes were left untouched.
- **Design decisions:** Combat remains the equipment source; no Crafting/Gathering restoration, new currency, mandatory daily feature, or prestige power reset is recommended. The first five favor existing boundaries and low recurring content work.
- **Verification performed:** Repository inventory and targeted source tracing; JSON parsing/counts; command/query, service, model, route, and configuration cross-checks; duplicate-feature searches; source-link validation and report structure checks; whitespace checking of the new report diff.
- **Tests/builds not run:** Backend tests through `build/run-tests.ps1`, frontend tests/builds, chat tests, and balance simulations were not run because this deliverable changes no executable behavior. Historical test counts in documents were not treated as current verification results.
- **Runtime checks not performed:** No running game, database, live economy, production flags, or deployed content was inspected. A few initial guessed source paths did not exist; repository discovery located the current route/options files used in the report.
- **Migrations/configuration/deployment:** None created, edited, applied, or deployed. Future features may require persisted state and balance configuration, but this report intentionally does not specify a migration or architecture plan.

[regions]: C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/world/regions.json
[world-routes]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/world/world.routes.ts
[journey]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/core/services/client-side/player-journey/player-journey.ts
[actions]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/CharacterActions/CharacterActionService.cs
[idle-options]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Layers/Orchestration/Models/IdleCombatProgressionOptions.cs
[api-config]: C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/appsettings.json
[equipment-acquisition]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/EquipmentAcquisitionService.cs
[equipment-policy]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentUpgradePolicy.cs
[equipment-prices]: C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-upgrades.v1.json
[blueprints]: C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/equipment/equipment-blueprints.v1.json
[equipment-loadouts]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Items/EquipmentLoadoutService.cs
[essences]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Essences/EssenceSystemService.cs
[styles]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/CombatStyles/CombatStyleService.cs
[style-rules]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/CombatStyles/CombatStyleRules.cs
[archive]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Essences/CreatureArchiveService.cs
[codex]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Essences/EssenceCodexCollectionService.cs
[codex-bonuses]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Essences/EssenceCodexBonusProvider.cs
[ascension]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Essences/EssenceProgressionConstants.cs
[dungeons]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonRunService.cs
[mastery]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Dungeons/DungeonMasteryService.cs
[mastery-benefits]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Dungeons/Mastery/DungeonMasteryBenefits.cs
[dungeon-data]: C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/dungeons/dungeons.json
[engine]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs
[telemetry]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Combat/CompactCombatTelemetry.cs
[combat-stats]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/shared/components/combat/combat-entity-stats/combat-entity-stats.component.ts
[harness]: C:/repos/Legends-Legacy/legends-legacy/LL/tools/BalanceHarness/README.md
[guild-missions]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Guilds/GuildMissionService.cs
[vault]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Guilds/GuildVaultService.cs
[buildings]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Guilds/GuildBuildingService.cs
[guild-data]: C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/guilds/guild-content.json
[building-ui]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/features/game/city/guild/in-a-guild/guild-buildings/guild-buildings.component.ts
[arena]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Colosseum/ColosseumService.cs
[tournaments]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/TournamentGroundsService.cs
[champion-market]: C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/market/champion-market.json
[tower]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs
[tower-data]: C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/world-tower/tower-floors.json
[tower-parties]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/WorldTower/WorldTowerPartyRules.cs
[raids]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Raids/RaidService.cs
[raid-options]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Raids/RaidOptions.cs
[raid-data]: C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/raids/raid-bosses.json
[raid-vendor]: C:/repos/Legends-Legacy/legends-legacy/LL/src/API/API.LL/Data/raids/trophy-vendor.json
[quests]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Quests/QuestService.cs
[prophecies]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Prophecies/ProphecyService.cs
[quest-tracker]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Presentation/ll/src/app/layout/dashboard/quest-tracker/quest-tracker.component.ts
[events]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Quests/Events/EventQuestService.cs
[achievements]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Achievements/AchievementDefinition.cs
[achievement-types]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Achievements/AchievementRequirementType.cs
[achievement-progress]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Achievements/PlayerAchievementProgress.cs
[titles]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Achievements/PlayerTitleUnlock.cs
[market]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/MarketPlaces/MarketPlaceService.cs
[character]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Domain/Models/Entities/Characters/Character.cs
[soulstones]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Service/Services.LL/Soulstones/SoulstoneUpgradeService.cs
[outbox]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Core/Application/UseCases/Outbox/GameEventTypes.cs
[jobs]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Worker/Worker.LL/BackgroundJobs/BackgroundJobRegistrationExtensions.cs
[chat]: C:/repos/Legends-Legacy/legends-legacy/LL-Chat/API/API.Chat/Hubs/ChatHub.cs
[db]: C:/repos/Legends-Legacy/legends-legacy/LL/src/Infrastructure/Persistence/Persistence.LL/LLDbContext.cs
