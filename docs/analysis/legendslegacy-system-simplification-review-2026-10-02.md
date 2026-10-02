# LegendsLegacy: depth through subtraction

**Design and implementation review — 2 October 2026**  
**Repository baseline:** `979d641c1`, plus the working tree inspected on that date.  
**Deliverable:** a proposed redesign, not an implementation change or a deployment plan to execute automatically.

## Executive judgment

LegendsLegacy has a coherent game inside it: **hunt creatures, acquire their abilities, assemble a build, and use that build to overcome increasingly demanding encounters and find better equipment.** Preserve that game.

The excess is concentrated around it. The same hunting activity feeds character experience, Essence experience, Combat Style experience, collection bonuses, Soulstone upgrades, quests, Prophecies, guild orders, guild contribution, and achievements. Several of those systems then reward resources that accelerate the others. This creates more obligations and accounting than distinct decisions.

My recommendation is to remove or consolidate roughly **a third of the secondary mechanics and progression surfaces**, with substantial additional deferral of unfinished multiplayer modes. This is a product-scope judgment, not a measured percentage of code, content, or development effort. Deleting 30% of files would be the wrong target. Some expensive systems, especially the combat engine and equipment ownership protections, earn their cost.

The decisive changes are:

1. Keep Essence acquisition, paired abilities, limited attunement slots, and a short investment path. Remove the separate 100-level training ladder, empty evolution layer, and collection-ascension bonuses.
2. Keep the four implemented Combat Styles and their meaningful choices. Remove independent training of each Style; unlock choices through existing character progression.
3. Retire the Soulstone upgrade tree. Make basic acquisition protection and reward-retention rules baseline policies, not another progression purchase.
4. Replace daily Prophecies, weekly Prophecies, Revelation milestones, caches, and paid rerolls with one optional, non-expiring objective inside the Quest Journal. No new reward currency.
5. Keep guild membership, communication, roles, and the equipment vault. Remove guild personal dailies, the material shop, and the building economy. Retain at most one shared project and one guild record of progress.
6. Keep the Colosseum as the one supported PvP activity. Remove its general-progression shopping incentives and daily first-win bonus. Defer Tournament Grounds despite its substantial implementation.
7. Keep a bounded World Tower as the main cooperative activity. Remove its contribution chores, unsupported token economy, and gates on ordinary regional progression. Defer standalone raids and scheduled region-boss events; retain their useful encounter content for possible reuse.
8. Keep equipment identity, rarity, reinforcement, and blueprints. Remove the overlapping quality-plus-narrow-roll lottery, not the choices between equipment types.

The resulting game still has long-term collection, specialization, loot, difficult PvE, competition, trade, and community. What it loses is the expectation that all of those must be separately serviced to progress efficiently.

## Scope, evidence, and limits

The primary target is the **LL game service**, its Angular client, and the content definitions that ship with it. I also inspected the independently deployed **LL-Chat** domain/API surface, persistence declarations, runtime configuration defaults, background workers, and relevant administrative integration points. The actual API directories are `API.LL`, `API.AdminDashboard`, and `API.LiveOps` under `LL/src/API`; the repository overview uses shorter boundary names.

This is a static source and configuration review. I did not play a running instance, inspect a player database, measure retention, or verify deployed configuration. “Implemented” means supported by inspected code/data, not proven active in production. Maintenance estimates are comparative judgments. “Players will feel compelled” means a reward-structure risk, not a claim based on player research. Older design documents were not treated as proof of implementation.

Evidence references such as **[E04]** point to the source index at the end. The index links to repository files so this document can remain in project documentation. These are the most important corrections to a hypothetical feature inventory:

| Subject | What the repository actually supports |
| --- | --- |
| Stronghold | No current Stronghold gameplay implementation found in the domain, use cases, services, routes, or controllers searched. Do not count deleting it as a saving of implemented functionality. |
| Guild raids / wars | Building definitions and contribution-source hooks exist. Raid Hall, War Room, Training Grounds, and Essence Sanctum largely advertise future functionality. The standalone `Raids` system is a different implementation, not proof of implemented guild raids. [E13, E16] |
| Doctrines | The current feature is **Combat Styles**. Some telemetry calls the selected Style a Doctrine. There is no separately established Doctrine progression system to count twice. Exactly four Styles are validated: Bastion, Conduit, Reaper, Duelist. [E05] |
| Essence attunement | Selecting Essences into level-unlocked slots, not an independent attunement XP/currency system. [E03] |
| Essence evolution | An endpoint, saved flag, modifiers, and catalyst item definitions exist. All **85** checked-in Essence definitions have empty catalyst IDs and no authored evolution modifier changes. It is scaffolding without a substantive authored evolution catalog. [E03, E04] |
| Soulstones | The active catalog contains **seven five-rank upgrades**, primarily acquisition/experience/retention effects. It is not currently a large permanent combat-stat constellation tree. [E07] |
| Attributes | Checked-in API defaults select **attribute rules 18 and equipment release 4**. Cooldown, old resistance attributes, and older item catalogs are partly compatibility history. Do not count every enum value as a current player-facing stat. [E06] |
| Regions / dungeons | World JSON defines **two regions and sixteen areas including training**. Dungeon JSON defines **four families with three difficulties each**. Five delve definitions include a Hive definition outside those four current dungeon families. A policy constant mentioning ten regions does not mean ten fully authored regions. [E01, E08] |
| Raids / region bosses | Two raid bosses are authored; raid UI is gated off when runtime environment is `prod`, and reward options default off. One region boss is authored, with rewards explicitly disabled and no reward brackets. No claim is made about environment overrides. [E13, E14, E21] |
| Seasons | Tournament scheduling, ranking/history, and season-capable title metadata exist. I did not find a general seasonal character reset or a battle-pass progression system. Do not invent one for this audit. [E12, E17] |
| Premium | Nobility/Signet redemption and trading exist. The registered purchase gateway returns “purchases unavailable during alpha.” The retained `NobilityDailyGrant` entity is not evidence of a current daily subscription payout. [E20] |
| Gathering / crafting / NPCs | No current gathering profession or general crafting progression found. Equipment reinforcement and blueprint application exist. `NPC : Entity` is empty; the Tavern route displays leaderboards rather than a separate NPC relationship system. [E06, E18] |
| Account progression | Most power/economy state is character-scoped. Achievements/titles support account and character scopes; Nobility is an account entitlement. “Soul Archive” should not automatically be described as an account-wide power system. [E03, E17, E20] |

## 1. Complete system inventory

**Complexity / ongoing burden:** L = local rules and a small surface; M = several state transitions or significant tuning; H = cross-system state, simulations, multiplayer, or extensive content. These are relative estimates, not effort already spent. “Optional” describes intended player choice; reward pressure is audited separately. Inputs are dependencies; outputs name systems consuming the result. Related submechanics share a row when they do not form independent activities.

### Core, build, and acquisition systems

| ID / system | Current purpose and main interaction | Primary rewards / progression type | Inputs → dependent systems | Core or optional | Complexity / ongoing burden |
| --- | --- | --- | --- | --- | --- |
| S01 Character level | Choose encounters and accumulate XP; base Power/Health grow automatically | Levels, access, Essence slots; vertical character progression | Combat/quest/reward XP → regions, slots, encounter readiness | Core | M / H pacing |
| S02 Regions and areas | Choose a hunting location and discover creatures; some access is quest/Tower gated | New opponents, loot and Essence sources; spatial access | Level, quests, server Tower clear for Meran → idle combat, dungeon access, quests | Core | M / H content |
| S03 Automated and offline combat | Select a hunt, let the build execute, review outcomes | XP, Cinders, loot, Soulstones, Essence drops/XP; repeatable accumulation | Character, gear, Essences, Styles, spawn tables → most progression/event consumers | Core | H / H balance |
| S04 Essence acquisition and duplicates | Find/trade unbound Essences; absorb new ones or shatter extras | Permanent character collection, Dust; horizontal options | Creature loot, market, reward selectors → attunement, collection records, leveling | Core | H / H abilities |
| S05 Essence actives/passives and slots | Equip paired active/passive abilities into ordered slots | Build behavior; horizontal selection with expanding capacity | Collection, character level, combat catalog → every combat mode | Core | H / H combinatorics |
| S06 Essence levels, Dust, ascension | Train equipped Essences or spend Dust; spend tiered cores at caps | Levels 1–100, three ascensions, scaled ability effects | Combat XP, Dust, three core grades → combat scaling, collection bonuses, achievements | Supporting core | H / H cross-effects |
| S07 Essence evolution | Attempt a catalyst-gated upgrade after sufficient ascension | Saved evolution flag; authored modifiers are currently empty | Ascension/catalysts → ability snapshot machinery | Incomplete secondary | M / potentially H |
| S08 Creature Archive, Focus, resonance | Inspect discoveries and sources; focus one creature | Targeted spawns/drops, kill records, a small accumulating drop bonus | Kills, selected Focus, Nobility/bonuses → acquisition, codex, objectives | Core support | M / M |
| S09 Codex collections | Complete groups of absorbed Essences, then ascend all members | Small XP/drop/pity bonuses; collection and minimum-member ascension | Essence ownership/ascension → bonus aggregation and acquisition | Optional with efficiency pressure | M / M per collection |
| S10 Combat Styles / Doctrines | Select Bastion, Conduit, Reaper, or Duelist; refine and upgrade after training | Distinct mechanics plus Style levels 0–10; mastery choices | Combat XP, selected Essences → engine behavior and snapshots | Core build support | H / H |
| S11 Attributes and combat rules | Compare offense, defense, sustain, speed, and control resistance | Derived effectiveness; no separate manual stat-allocation loop found | Base level, equipment, effects → all encounters, previews, ratings | Core | H / H |
| S12 Equipment identity, rarity, quality, rolls | Choose weapon/armor/accessories; inspect random drops | Eight equipment slots; archetype, rarity, tier, quality, roll, variant, sets | Hunt/dungeon/rewards/market → combat, loadouts, trade, guild vault | Core | H / H |
| S13 Reinforcement, dismantling, blueprints | Rank items up to +5, salvage Parts, apply a blueprint variant | Controlled power and specialization changes | Cinders, Parts, blueprint items → equipment and binding state | Core loot support | M / M |
| S14 Equipment acquisition protection | Starter grants, selector items, blueprint protection, Tower preparation supplies | Useful gear and directed acquisition | Quests, dungeon completions, server floors → inventory/build readiness | Core support | H / M |
| S15 Equipment and Essence presets | Save loadouts and assign activity auto-use | Convenience, adaptation; three free/six Noble slots in each system | Owned/borrowed gear, Essences, activity, entitlement → combat snapshots | Core support | M / M |
| S16 Soulstone upgrades | Spend Soulstones on seven repeatable rank tracks; reset/refund | Drop, XP, Focus, duplicate-Dust, defeat-XP, Rest Site retention improvements | Many activity rewards → acquisition/XP/dungeon rewards | Secondary meta | M / M economy |

### Activities, community, and support

| ID / system | Current purpose and main interaction | Primary rewards / progression type | Inputs → dependent systems | Core or optional | Complexity / ongoing burden |
| --- | --- | --- | --- | --- | --- |
| S17 Dungeons / bosses / routes / Vigor | Spend family sigils, choose paths, manage Vigor, fight minibosses/bosses, rest or press on | Gear/blueprints/materials, first clears, XP; encounter progression | Build, sigils, prior difficulty, sometimes Tower → mastery, loot, quests, guild/Prophecy counters | Core deliberate PvE | H / H |
| S18 Dungeon mastery | Repeated attempts and clears train a family across difficulties | Levels 0–10; vision, Vigor efficiency, currency and equipment-drop bonuses | Dungeon outcomes → run feasibility, loot probability, completion rewards | Secondary local progression | M / H reward tuning |
| S19 World Tower | Form a rally, arrange parties, beat a server floor, replay Echoes | Server unlocks, titles, Hall of Fame, Tower Tokens | Builds, roster/snapshots, server progress → regions/dungeons, supply eligibility, titles | Major co-op; currently gates core access | H / H |
| S20 Tower scouting / preparation | Make individually capped research/supply/ward/weak-point contributions | Reveals and encounter modifiers; communal preparation | Weekly contribution counters, failed attempts → Tower fight conditions | Secondary | M / M |
| S21 Standalone raids | Build a multi-lane roster and resolve staged boss combat | Outcome records; coded weekly/upgraded/repeat rewards and Trophies, gated off by default | Team composition, gear/Essence eligibility, snapshots → raid records, possible vendor/rewards | Deferred/gated activity | H / H |
| S22 Scheduled region boss | Join a short signup window; fight escalating boss levels with revival/fury rules | Encounter record; reward infrastructure currently disabled | Schedule, Tower 10, matchmaking, builds → event results/chat | Secondary live event | H / H |
| S23 Colosseum | Attack saved defenses using replenishing tickets | Rating, ranks, Glory, first-win bonus, battle records | Snapshots, combat, tickets → leaderboards, shop, achievements | Optional PvP with PvE rewards | H / H |
| S24 Champion Market | Spend Glory, often within weekly limits | Titles, Soulstones, sigil fragments, three core grades | PvP currency/rank → general PvE/Essence progression | Secondary economy | M / H cross-mode tuning |
| S25 Tournament Grounds | Register/organize entrants and follow scheduled rounds/replays | Placement, tournament records/points, configured Glory/Cinders/Soulstones/fragments | Schedule, teams, snapshots, combat artifacts → rewards, ranking, chat | Optional competitive event | H / H |
| S26 Guild membership / roles / invitations | Form a group, recruit, assign permissions, coordinate | Belonging and access to shared functions | Accounts/characters/chat → missions, buildings, vault | Optional social | M / M support |
| S27 Guild XP, level, buildings, Supplies | Earn guild resources and select construction targets | Capacity/access, better mission rewards, cheaper construction | Missions/orders → Hall, Board, Market, Treasury and future hooks | Secondary progression | H / H expansion pressure |
| S28 Guild weekly mission / contribution tiers | Choose a collective target and earn an individual contribution tier | Guild XP, Supplies, personal Favor; weekly progress | Combat/dungeons/Essence events → guild progression/shop/rankings | Optional with reward/social pressure | H / M |
| S29 Personal guild daily orders | Complete three base daily orders; Board can alter variety/count | Favor, Guild XP, Supplies | Same play events as other objectives → buildings/shop | Recurring secondary | M / M |
| S30 Guild shop | Spend Favor within weekly stock limits | Soulstones and sigil fragments | Guild participation and Market Office → core progression | Secondary economy | M / M |
| S31 Guild equipment vault | Donate, borrow, return, and manage equipment | Shared gear access; no separate XP bar | Item ownership, guild permissions → equipment/loadouts | Optional social | M / M integrity |
| S32 Prophecies | Choose one of daily offers, pursue an accepted weekly Greater Prophecy, reroll | XP, Soulstones, fragments, Fate Echo, materials; daily/weekly objectives | Event counters, level-scaled rewards, reroll benefits → other progression | Recurring secondary | H / H |
| S33 Weekly Revelation and caches | Accumulate Prophetic Favor, claim milestones, open caches | More of the same currencies/materials; threshold reward track | Prophecy completion → inventory and reroll economy | Secondary overlay | M / M |
| S34 Quests and guided journey | Follow onboarding/region chains, choose rewards, pin and turn in objectives | Access, starter gear, directed resources, narrative | Combat/collection/equipment events → region access, journey UI, build formation | Core guidance | H / H authored narrative |
| S35 Event quests | Contribute to dated community objectives and personal milestones | Item/resource rewards and shared completion | Event period, tutorial completion, tracked events → claims/chat | Optional live operations | H / H cadence risk |
| S36 Achievements, renown, titles | Accumulate records and equip a display title | Points/renown and recognition; account/character records | Many gameplay events → profile, chat, leaderboards | Optional identity | M / M catalog |
| S37 Marketplace and transfers | Buy/sell commodities/items, place orders, transfer Cinders | Specialization through trade; economic accumulation | Item binding, Cinders, orders, fees/expiry → gear/Essences, Signet trading | Optional economy | H / M operations |
| S38 Leaderboards / Tavern | Compare level, collection, renown, dungeon, PvP and guild records | Recognition; derivative ranking rather than another stat tree | Existing progression/results → social comparison | Optional | M / L–M |
| S39 Chat / presence / invitations | Global, trade, help, guild, whisper, raid, recruitment channels | Coordination and community | LL identity/group data, LL-Chat → group formation and support | Optional social foundation | H / M moderation |
| S40 Accounts / Nobility / Signets | Register/recover identity; redeem or trade membership items | Larger offline allowance, presets/order capacity, Focus and reroll/ticket benefits | Account entitlement and item ledgers → combat settlement, market, Focus, PvP/Prophecies | Support / optional premium | H / M compatibility |
| S41 Inventory, loot history, settings and notifications | Inspect rewards, compare items, control presentation, recover state | Comprehension and convenience, not progression | Most gameplay → decisions and reliable UI | Necessary support | M–H / M |

No separate Stronghold, profession, relationship, reincarnation, pet, daily-login streak, or battle-pass implementation was established by the searches. Those are exclusions, not invitations to add features. LiveOps, moderation, outbox delivery, and administrative CRUD are operational support costs, not player progression systems to delete to satisfy the premise.

## 2. The true core game

### Four pillars

1. **Directed, persistent hunting.** Decide what to fight and what to pursue; the character continues executing that decision. Region access, offline combat, source discovery, and Focus support this.
2. **Monster-derived buildcraft.** Collect abilities from enemies and combine them under slot constraints. Essence pairs, Combat Styles, equipment specialization, and understandable combat rules support this.
3. **Loot with a purpose.** Acquire equipment that enables a particular build, then make selective investments. Distinct archetypes, rarity, blueprint variants, dismantling, and trade support this.
4. **Encounters that validate the build.** Use the same build language in increasingly demanding hunts, branching dungeons, bounded cooperative challenges, and optional PvP. Wins must reveal something about the build, not merely certify attendance elsewhere.

Long-term progression is the connective tissue, not a justification for an unlimited number of progression bars. Community makes the game a PBBG, but does not require a separate resource economy for each social activity.

**What survives the loss of half the secondary systems?** A player hunts a creature for its Essence, combines that active/passive pair with a complementary Style and equipment, tests the result in a dungeon, and changes the build when the encounter exposes a weakness. They can trade or discuss discoveries and join a Tower rally. That remains recognizably LegendsLegacy without Fate Echo, guild construction, a Revelation cache, independent Style XP, Tower Tokens, or a scheduled Mad King event.

## 3. System purpose audit

Each entry covers unique purpose and motivation, decisions, loss and substitutes, relationship to the core, likely reason for participation, gameplay versus accumulation, cost/value, and whether I would rebuild it today. “Compelled” is a structural incentive assessment.

| Systems | Purpose / motivation / meaningful decision | Loss if removed; overlapping value | Core contribution and participation pressure | Depth versus cost; rebuild verdict |
| --- | --- | --- | --- | --- |
| S01–03 Level, regions, hunting | Persistent growth and discovery; choose a target, encounter risk, and time allocation | Would lose the idle RPG foundation. Quests guide this loop but cannot replace it | Directly core; intrinsically useful even without external daily rewards | Complexity justified. **Rebuild**, with one access hierarchy and one offline policy |
| S04 Essence acquisition | Own an enemy's combat identity; absorb, trade, or shatter; target a missing ability | Would lose the game's strongest collection/build connection; equipment alone is insufficient | Core and desirable. Avoid collection bonuses turning all acquisitions into obligations | Distinct options justify catalog cost. **Rebuild**; stop requiring every new enemy to imply a new player ability |
| S05 Actives/passives/slots | Construct an automated combat plan with opportunity costs | Lose interactions and identity. Styles and gear complement rather than duplicate abilities | Core; chosen for gameplay | **Rebuild** paired abilities and ordering. Ten eventual slots need scrutiny because more capacity can erase exclusion decisions |
| S06 Levels/ascension | Invest in favorite abilities; allocate Dust/cores and training time | Lose gradual attachment and investment. Character level already provides gradual growth | Supports core, but training makes a new build weaker before it can be fairly tested | Level XP primarily feeds ascension eligibility while ascension scales effects. **Rebuild one short Essence investment path, not both ladders** |
| S07 Evolution | Intended transformation fantasy | Today, little authored gameplay would disappear; ascension already promises advancement | Currently adjacent scaffolding | **Do not rebuild now. Delete** empty evolution state from the active design after checking historical data |
| S08 Archive/Focus/resonance | Understand where an ability comes from and pursue it | Lose informed agency without source information; generic loot lists are inadequate substitutes | Strong support. Focus is a choice; cooldown management and tiny purchased pity acceleration are not | **Rebuild Archive and Focus**. Simplify the drop-assistance rule rather than expanding its upgrades |
| S09 Collection bonuses | Completionism and broader hunting | Lose small passive acceleration. Achievements/collection records already recognize breadth | Pulls specialists toward collecting and ascending things they do not use | **Rebuild the record, not the bonus layer**. Minimum-member ascension creates maintenance without build choice |
| S10 Combat Styles | Give builds distinct operating rules; choose a mechanic/refinement/upgrades | Lose useful structure that helps different Essence combinations cohere | Core-adjacent but genuinely interactive; training can discourage experimentation | **Rebuild the four Styles and choices**. Remove per-Style XP and mastered-upgrade escalation; cap breadth |
| S11 Attributes/engine | Make offense, defense, speed and sustain meaningfully different | Lose the medium of buildcraft. A single power number cannot replace it | Core | **Rebuild**. Keep current version-aware interpretation; simplify exposed vocabulary, not every internal mechanic |
| S12 Equipment | Find stronger or better-fitting gear; trade power, sustain, tempo and set interactions | Lose the loot pillar. Essences do not replace item discovery | Core | **Rebuild** identity/rarity/sets. Two extra scalar quality dimensions create comparisons more than decisions; remove one combined layer |
| S13 Reinforcement/blueprints | Choose what deserves investment and tune specialization | Lose deterministic progress and agency under loot randomness; market is only a partial substitute | Core support, intrinsically useful | **Rebuild** bounded reinforcement and one variant choice. Do not restore the retired general crafting framework |
| S14 Acquisition protection | Prevent a missing drop from blocking a viable build | Lose a reliable path to equipment. Random loot and trade alone do not guarantee access | Core support | **Rebuild protection**, consolidate selectors/entitlements. Its transactional complexity is justified; its many branded boxes are not |
| S15 Presets | Try and reuse different strategies with low friction | Lose practical build experimentation; manual rearrangement is not meaningful depth | Strongly core-supporting | **Rebuild**, as one build preset experience encompassing gear, Essence order and Style |
| S16 Soulstone upgrades | Long-term efficiency investment; choose the order of upgrades | Lose percentage accelerators and some recovery improvements; baseline rules and Essence/gear investment can absorb these | Primarily “useful because it makes everything faster” | **Do not rebuild** the tree. Seven tracks compete poorly with direct investment in a build |
| S17 Dungeons | Deliberate route/risk decisions and boss tests | Lose the clearest alternative to passive hunting; Tower/PvP do not replace solo pacing | Strongly core; loot is a legitimate reason to choose a content activity | **Rebuild** bounded dungeon families, routes and Vigor. Reduce access/accounting layers |
| S18 Dungeon mastery | Familiarity and long-term reason to return | Lose map/efficiency milestones; difficulty clears already express mastery | Mixed: local knowledge is valuable; large drop advantages encourage grinding the bar | **Keep records and clear milestones**, remove separate XP and automatic loot/currency multipliers |
| S19–20 Tower | Shared server history, roster coordination and difficult encounters | Lose a communal accomplishment. Raids/region boss offer overlapping co-op battles, not the same persistent history | Worth keeping as optional co-op; regional gates make the rest of the population depend on it | **Rebuild a bounded Tower**. Do not rebuild manual preparation quotas or an unspent token balance |
| S21 Raids | Multi-lane preparation and staged cooperation | Lose a distinct formation puzzle; Tower retains group identity and combat | Potentially valuable but currently outside the production-facing route policy | **Do not rebuild alongside Tower now**. Preserve authored mechanics as reusable content; defer independent service/product lifecycle |
| S22 Region boss | Shared spectacle and escalating endurance | Lose a live appointment; Tower already supplies cooperative bosses | Adjacent. A 10-minute signup every 4–8 hours becomes attendance pressure if valuable rewards are enabled | **Do not rebuild as an independent timed activity**. Reuse the boss later without another calendar |
| S23–24 Colosseum/market | Benchmark against other builds; choose opponents/defense | Lose direct rivalry; leaderboards alone cannot replace it | PvP reinforces builds, but core materials in weekly stock encourage uninterested players to participate | **Rebuild Arena**, keep Glory for recognition. Remove daily first-win economics and general-progression stock |
| S25 Tournament Grounds | Bracket drama, teams, scheduled spectator experience | Lose special occasions; ordinary Arena preserves competition | Peripheral to the idle loop and expensive to operate | **Do not rebuild for current scope**. Defer. Existing implementation is not a sufficient reason to launch weekly tournaments |
| S26/S31/S39 Guilds, vault, chat | Belonging, coordination, sharing equipment and knowledge | Lose meaningful PBBG identity; currency rewards cannot substitute for people | Social and optional if power is not tied to attendance | **Rebuild** these. Permissions, membership checks, ownership and moderation are justified costs |
| S27–30 Guild progression economy | Collective construction, contribution recognition and material access | Lose building targets and Favor spending; a shared project/record can preserve communal progress | Daily tasks and progression materials make membership a productivity obligation | **Do not rebuild this economy**. Keep at most one shared objective; delete personal dailies and nested reward buildings |
| S32–33 Prophecies/Revelation | Suggest a goal and vary a routine | Lose some prompt variety. Quests, target hunting and guild objectives already supply goals | Acceptance, rerolls, favor, milestone claims and cache opening add rewards around existing work | **Rebuild only optional goal selection inside Quests**. Remove reset pressure and the reward-of-a-reward chain |
| S34 Quests | Teach the game and provide world context | Lose a directed beginning and authored discoveries | Core guidance; finite tasks are acceptable gates when tied to what they teach | **Rebuild a short onboarding/region spine**. Avoid quests whose only purpose is touring every secondary subsystem |
| S35 Event quests | Temporary community goals and shared moments | Lose scheduled variation; permanent core content already offers long-term goals | Conditional usefulness; power rewards and expiry create fear of missing out | **Retain dormant infrastructure if cheap, defer a content calendar**. No exclusive build progression |
| S36/S38 Achievements, titles, boards | Recognition, memory, collection and self-chosen goals | Lose social expression if removed; collection catalog can share records | Properly optional because titles are not a combat-stat layer in the inspected model | **Rebuild a finite catalog**, keep largely as-is. Merge displays, not every identity into a single score |
| S37 Trade | Let players specialize and exchange surplus | Lose economic agency and a useful alternative to farming every source | Supports hunting and loot; order maintenance is chosen | **Rebuild existing bounded trade**. Keep fees, ownership, receipts and reliable expiry rather than replacing it with more shops |
| S40–41 Account/premium/support | Preserve identity and make play understandable/convenient | Losing these harms trust and usability, not just breadth | Some current premium differences monetize activity pressure | **Rebuild identity/support**. Keep one membership entitlement; simplify benefits after cutting their dependent chores |

## 4. Overlap analysis

| Overlap | Current evidence | Decision and concrete destination |
| --- | --- | --- |
| Character, Essence and Style XP from the same combat | `LevelingService`, `EssenceSystemService.GrantCombatXpToAttunedEssencesAsync`, `CombatStyleProgression` | **Share progression.** Character level governs access and Style choices. Essence investment remains a resource decision in three ranks; remove its separate XP ladder |
| Collection ascension and Soulstone acquisition bonuses | `EssenceCodexBonusProvider`, seven Soulstone upgrades | **Delete overlapping mechanics.** Keep collection records; implement one explicit baseline acquisition-protection rule. No passive collection-efficiency tree |
| Dungeon mastery versus difficulty clears | Family mastery levels plus three sequential difficulties | **Become a sub-feature.** First clears confer the few surviving route-information benefits; saved completion records replace mastery XP |
| Prophecy dailies, Greater Prophecy, Revelation and guild personal orders | Separate periods, acceptance/claim records, points and caches | **Delete guild personal orders; combine personal goals under Quests.** One persistent chosen objective, direct rewards, no favor/reroll currency |
| Guild XP, Hall level, building levels, Supplies and Favor | `Guild`, building catalog, mission and shop services | **One shared guild progression record**, or no power progression. Keep a project as a social focus; eliminate construction and personal material spending |
| Arena and tournaments | Separate simulations/snapshots/results, scheduling and reward grants | **One supported competitive parent: Colosseum.** Arena stays; tournaments move out of current scope. Reusing engine code alone does not justify two activities |
| Tower, standalone raids and region bosses | Independent roster/event lifecycles, rewards, playback and chat consumers | **One supported cooperative parent: World Tower.** Do not immediately build an all-purpose encounter platform. Reuse individual boss definitions/mechanics only when needed |
| Equipment and Essence presets with global Style selection | Separate loadout tables and activity assignment; Style selection resolved independently | **One player-facing Build preset.** Save the three existing dimensions together with one activity assignment; migrate existing presets explicitly |
| Soul Archive / Creature Archive / Codex / achievements | Ownership, discoveries, sources and collections are displayed through overlapping concepts | **One Archive surface**, keeping distinct underlying records where needed. Achievement titles remain in Records; no duplicated collection-power reward |
| Item rarity, quality, attribute roll, reinforcement | Rarity + five quality labels + a 0.95–1.05 roll in dungeon acquisition + +0–5 rank | **Lose overlapping scalar mechanics.** Keep rarity and reinforcement. Collapse quality/roll into a normalized item budget, preserving build specialization |
| Activity shops selling the same materials | Champion Market and Guild Shop both sell Soulstones/fragments; Arena also supplies cores | **Remove cross-activity material stock.** Core activity rewards come from core play; PvP recognition stays isolated |
| Reward boxes and selector items | Prophecy caches, regional Essence tokens, gear boxes, Tower supply chests | **Differentiate, then consolidate.** Delete randomized reward wrappers where direct grants suffice; keep selection entitlements when they solve a missing-item problem |

Sharing an event stream is sensible; forcing all quest, achievement, guild, and commercial state into a universal progression engine is not the recommendation. Similar event inputs can still have different lifetimes and correctness requirements.

## 5. Progression layers and the simpler hierarchy

### Current vectors and disposition

| Vector | Independent state or rule today | Decision quality / risk | Proposed treatment |
| --- | --- | --- | --- |
| Character level | Character XP/level | Clear vertical spine | Keep |
| World access | Area level, quest, and server Tower requirements | Multiple authorities can obscure the next step | Make region/quest/character progression authoritative; remove server Tower gates |
| Equipment tier | Regional power bands | Useful progression context | Keep; do not turn tier into another upgrade currency |
| Rarity | Authored item rarity | Useful chase and expectation | Keep |
| Quality and attribute roll | `ItemQuality`, `AttributeRollMultiplier` | Parallel scalar luck; weak decisions | Retire for new awards with value-preserving migration |
| Reinforcement | Rank 0–5 | Choose where to invest | Keep bounded |
| Variants, blueprint access, sets | Specialization/ownership and equipped combinations | Changes build behavior/stat allocation | Keep; cap catalog, not choice |
| Essence acquisition | Owned Essence definitions | Opens distinct abilities | Keep |
| Essence levels / XP | Level 1–100 with ascension-dependent caps | Another training gate before a new build is competitive | Remove |
| Essence ascension | Three tiers and three core grades | Investment useful; resources/gates overcomplicated | Keep three short ranks, paid in existing Dust and gated by existing progression |
| Evolution | Flag/catalyst/modifier infrastructure | Empty authored payoff today | Delete active mechanic |
| Attunement capacity | One slot initially; one more each ten levels, capped at ten | Capacity progression matters, but ten pairs can dilute constraints | Keep early cadence; proposed ceiling six. Validate late encounters before changing existing characters |
| Focus/resonance | Selected target/cooldown and failed-roll accumulation | Target choice useful; weak/misleading pity growth | Keep one acquisition rule and target selection, remove purchases that enhance pity |
| Codex collection ascension | Minimum ascension among all group members | Encourages training unused abilities for small efficiency | Remove bonuses and ascension tiers; retain completion |
| Combat Style mastery | Four separately trained level-10 paths; refinement, upgrade, mastery gates | Good choices hidden behind parallel training | Keep Style/refinement/upgrades; use character milestones and remove mastered-upgrade layer |
| Soulstone upgrade ranks | Seven independent rank-5 tracks | Purchase order affects efficiency rather than build identity | Remove |
| Dungeon difficulty | First-clear sequence | Clear proof of capability | Keep |
| Dungeon mastery | Per-family XP, levels, completion count, cap reward | Knowledge benefits mixed with up to +50 percentage points equipment drop chance | Remove XP/economic multipliers; attach limited information benefits to clears |
| Tower server conquest | Shared floor progress/unlocks | Distinct communal history | Keep within Tower |
| Tower scouting/preparation | Communal values and weekly contribution counters | Attendance or population subsidy for boss tuning | Remove manual quotas/modifiers; reveal information through attempts if needed |
| Tower personal record / Echo reward | Clear records plus once-per-week character token entitlement across floors | Records good; token entitlement has no found spend path | Keep records/Echo challenge; remove token reward schedule |
| Arena rating and rank | Per-character performance/history | Meaningful optional competition | Keep; remove general-power pressure |
| Tournament placement/points | Separate event/round/team/results state | Recognition already possible in Arena | Defer |
| Guild level / Hall / buildings | XP and several spending-based levels | Nested access and reward efficiency | One shared record; no building economy |
| Guild contribution tiers | Weekly thresholds tied to mission target | Can privilege volume over belonging | Keep contribution history; remove personal material tiers |
| Prophecies / Revelation | Daily/weekly completion plus favor thresholds | Expiring parallel objective bars | One non-expiring selected objective; no nested track |
| Achievements / renown / titles | Records, points, unlocks, equipped display | Optional long-term goals without combat bonuses | Keep; combine presentation |
| Event participation/milestones | Dated shared and personal counters | Can become compulsory if rewards are unique or oversized | Defer routine events; keep rewards substitutable |
| Currency balances | Seven personal balances plus guild Supplies and many item resources | Savings are not inherently extra gameplay depth | Reduce to the economy in section 6 |
| Account entitlement/identity | Membership coverage, title/achievement scope | Support, not another power grind | Keep; do not add account-stat progression |

There are too many independent vectors, even after excluding historical enums and incomplete ideas. The clearest warning is not the number of bars itself: it is that **progress in a preferred build repeatedly depends on investing in systems outside that build**.

### Proposed hierarchy of growth

```text
Character level + regional journey
    → encounter access, Essence slots, Combat Style choices

Build
    → selected Essence pairs + short Essence investment
    → equipment identity/rarity + bounded reinforcement + blueprint variant
    → one Combat Style with a refinement and limited upgrade choices

Proof of capability
    → dungeon difficulty clears
    → optional Tower conquest / Arena rating

Memory and belonging
    → Archive completion, achievements, titles, guild records
```

Character progression answers “where can I go?”; collection answers “what can I build?”; investment answers “what do I prioritize?”; encounters answer “does it work?” Records need not answer “how much stronger must I become?”

