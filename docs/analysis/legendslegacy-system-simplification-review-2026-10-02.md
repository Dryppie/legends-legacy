# LegendsLegacy: simplification plan

**2 October 2026 — condensed repository review**

## Recommendation

Keep the central loop: **hunt creatures → collect their Essences and equipment → build a strategy → beat harder encounters.**

Remove or combine roughly a third of the secondary mechanics. The main problem is overlapping progression: one hunt feeds character XP, Essence XP, Style XP, collection bonuses, Soulstone upgrades, quests, Prophecies and guild objectives. Several then award resources that accelerate the others. That creates obligations without equivalent decisions.

This is a source-based design recommendation, not an implementation change or a measured claim about player behavior. The original inspection began at `979d641c1`; concurrent work advanced the checkout during drafting. Deployed settings and player holdings were not inspected.

## What the code actually contains

| Finding | Design implication |
| --- | --- |
| 85 Essence definitions, but no authored evolution modifiers or nonempty catalyst requirements | Evolution currently promises more than it delivers. [Definitions](../../LL/src/API/API.LL/Data/essences/essences.json) |
| Four Combat Styles: Bastion, Conduit, Reaper and Duelist | “Doctrines” are not another independent system. [Rules](../../LL/src/Core/Domain/Models/CombatStyles/CombatStyleRules.cs) |
| Seven Soulstone upgrade tracks, mostly improving acquisition, XP and retention | The overlap is largely **efficiency progression**, not several combat-stat trees. [Catalog](../../LL/src/API/API.LL/Data/progression/soulstone-upgrades.json) |
| Two authored regions, four dungeon families with three difficulties each, fifteen Tower floors | Finish and balance this scope before expanding it. [World](../../LL/src/API/API.LL/Data/world/regions.json), [dungeons](../../LL/src/API/API.LL/Data/dungeons/dungeons.json), [Tower](../../LL/src/API/API.LL/Data/world-tower/tower-floors.json) |
| Tower Tokens have no spending path found; raid vendor is empty; region-boss rewards are disabled in checked-in data | Do not build more shops simply to justify existing currencies. [Tower service](../../LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerService.cs), [vendor](../../LL/src/API/API.LL/Data/raids/trophy-vendor.json), [boss configuration](../../LL/src/API/API.LL/Data/region-bosses/region-bosses.json) |
| No current Stronghold or general gathering/crafting progression found; guild raids/wars mostly appear as future building hooks | Defer these promises; do not count them as implemented features being deleted. [Guild catalog](../../LL/src/API/API.LL/Data/guilds/guild-content.json) |

# Systems That Should Not Be Cut

- **Hunting and offline combat:** the persistent progression foundation.
- **Essence acquisition, paired actives/passives and limited slots:** the game's strongest identity and source of build decisions.
- **Equipment identities, rarity, variants/sets, salvage and bounded reinforcement:** loot should change a build, not merely raise its score.
- **Dungeons, routes, bosses and Vigor:** intentional solo play that tests a build.
- **Presets, source information and readable combat results:** complex mechanics need usable explanations.
- **Chat, guild identity, equipment lending and trade:** meaningful social play without compulsory resource chores.
- **Achievements, titles and records:** useful long-term recognition without another power requirement.

Keep ownership checks, reward receipts, account recovery and moderation. Their complexity protects the game; deleting it would not improve the design.

# If I Had to Delete 30% of LegendsLegacy

| Remove or retire | Why; what replaces its useful purpose |
| --- | --- |
| **Soulstone upgrade tree** | Mostly another efficiency purchase layer. Make necessary drop/retention protections baseline rules; keep investment focused on builds. |
| **Essence levels, evolution and collection bonuses** | The 100-level ladder gates ascension; evolution lacks authored payoff; collection bonuses encourage upgrading unused Essences. Keep acquisition, collection records and three direct investment ranks. |
| **Independent Combat Style XP and mastered-upgrade escalation** | Retraining discourages experimentation. Unlock the existing Style choices through character progression. |
| **Daily Prophecies, Revelation milestones, paid rerolls and caches** | Too many steps around “choose a goal.” Keep one optional, non-expiring objective in the Quest Journal. |
| **Guild personal dailies, material shop and building economy** | They turn belonging into an income obligation and promise future content. Keep membership, roles, vault, chat and at most one shared project. |
| **Dungeon mastery XP and economic bonuses** | Separate grinding raises rewards and lowers Vigor costs. Use difficulty clears for progress and limited exploration benefits. Retune affected encounters first. |
| **Sigil fragments and consumed dungeon admission sigils** | Extra conversion and entry gates support several chore rewards. Prefer permanent access and sequential difficulty clears, after reward-rate balancing. |
| **Equipment quality plus narrow random stat-roll layer** | These scalar differences overlap rarity and reinforcement. Keep meaningful item identities and specialization; preserve old item value during conversion. |
| **Tower contribution quotas, Tokens and gates on ordinary regions** | Keep the cooperative achievement, remove its housekeeping and dependency on server population for solo access. |
| **Arena daily first-win bonus and progression-material shop stock** | PvE players should not need PvP attendance to obtain efficient material income. Keep rating, Glory titles and battle records. |

Also **defer Tournament Grounds, standalone raids and scheduled region bosses**. Support the Colosseum as one PvP activity and a bounded Tower as one cooperative activity. Preserve historical results and reusable encounter content.

Tournament Grounds deserves deferral despite its substantial implementation. Its teams, rounds, scheduling, snapshots, replays and rewards create a second competitive product to maintain. Existing effort does not make that the best use of a solo developer's future time. [Implementation](../../LL/src/Infrastructure/Service/Services.LL/Colosseum/Tournaments/TournamentGroundsService.cs)

This targets roughly a third of secondary scope, not exactly 30% of code. It sacrifices training bars, extra reward income and scheduled variety while preserving the central decisions.

# Systems That Should Be Combined

| Combination | Result |
| --- | --- |
| Soul Archive + Creature Archive + Codex | **Essence Archive:** ownership, sources, Focus and collection records together; no collection-efficiency bonuses |
| Essence levels + ascension | **Three investment ranks:** spend existing Essence Dust directly, with character/region eligibility |
| Character progression + Style unlocks | Train the character once; switch among unlocked Styles without another XP career |
| Equipment presets + Essence presets + Style selection | **Build presets:** one inspected setup and activity assignment; reuse existing ownership/snapshot rules |
| Prophecies + Quest Journal | Story/tutorial quests plus one persistent optional goal; direct rewards |
| Guild levels/buildings/contributions | At most one shared progress record/project; no personal Favor shop or construction currency |
| Dungeon mastery + difficulty clears | One dungeon record; retain only benefits that express knowledge or exploration |

These are consolidations of existing purposes, not proposals for new universal progression frameworks.

# Systems Worth Keeping but Simplifying

**Essences:** retain paired abilities and ordering. Consider reducing the eventual slot ceiling from ten to six, while preserving the first four unlocks used by the early journey. Six is a design proposal requiring late-game balance tests, not a proven optimum. More slots can weaken decisions about what to exclude. [Slot rule](../../LL/src/Core/Domain/Models/Essences/EssenceSlotProgression.cs)

**Combat Styles:** keep four, one refinement and limited upgrade choices. Remove parallel training, not the mechanics that make each Style distinct.

**Equipment:** keep eight slots, rarity, variants/sets and +0–5 reinforcement. Normalize redundant scalar rolls. Do not reopen the retired crafting/affix system to replace what was removed. [Upgrade policy](../../LL/src/Core/Domain/Models/Items/Equipments/Progression/EquipmentUpgradePolicy.cs)

**Dungeons:** keep routes, Vigor, Rest Sites, minibosses and Treasury risk. Mastery currently adds up to **50 percentage points** of equipment-drop chance and affects Vigor feasibility. Removing it and sigil costs requires one coordinated reward/encounter rebalance. [Mastery benefits](../../LL/src/Core/Domain/Models/Dungeons/Mastery/DungeonMasteryBenefits.cs)

**Tower and Arena:** keep the encounters, competition and records. Remove material incentives that make optional modes feel compulsory; defer additional multiplayer formats.

**Quests and records:** preserve focused onboarding and a short regional story. Keep titles cosmetic and collections optional. Avoid requiring players to tour every subsystem.

**Nobility:** retain one entitlement and useful convenience/appearance benefits. Remove exceptions tied to retired rerolls, ticket pressure and Focus cooldowns. Review equal offline allowances: the current policy grants 24 hours free versus 168 hours Noble. Honor existing entitlements during any change. [Benefits](../../LL/src/Core/Domain/Models/Nobility/NobilityBenefits.cs)

## Smaller economy

| Keep | Purpose |
| --- | --- |
| Cinders | General trade, reinforcement payments and blueprint application |
| Essence Dust | Recycle unwanted Essences; improve selected ones |
| Reinforcement Parts | Recycle equipment; reinforce selected pieces |
| Glory | Optional PvP recognition only |
| Signets | Membership items, separate from gameplay currency |

Retire **Soulstones, Fate Echo, Guild Favor, Guild Supplies and Tower Tokens** with their systems. Do not launch the deferred Raid Trophy economy. Merge three Monster Core grades into Dust investment; remove sigil admission resources, evolution catalysts and Advancement Stone after checking holdings/references.

Keep blueprints and unbound Essences as specific tradeable items. Keep Vigor as a run-local tactical resource. Resolve ordinary caches directly; retain selector rewards only where they protect against missing a useful item. Prophetic Favor and guild contribution are records/thresholds, not reasons to invent more currencies.

Do not merge everything into Cinders: separate Essence and equipment salvage loops give specialization a purpose. [Wallet](../../LL/src/Core/Domain/Models/Entities/Characters/Character.cs), [item catalog](../../LL/src/API/API.LL/Data/items/items.json)

## Chores, content and maintenance

The strongest mandatory-activity risks are **daily Prophecies, guild orders, Arena first wins, and weekly material shopping**. All award resources useful outside their own activity. Calling them optional does not remove that incentive. Move necessary supply into ordinary hunts, relevant dungeons and salvage; do not accidentally make progression slower.

Tower Echoes have a weekly token entitlement, but no demonstrated current power loss from skipping them because no sink was found. Raid/region-boss reward pressure is partly prospective: checked-in rewards are disabled or gated. Distinguish these from active material loops.

The biggest content burden is new abilities interacting with existing Essences, Styles, equipment and encounter contexts. Freeze breadth, reuse established mechanics, and finish a bounded catalog. Data-driven definitions reduce coding work; they do not eliminate balance and explanation work.

Preserve expensive **core combat, offline settlement, trade integrity and shared equipment**. Cut weak reward layers and duplicate multiplayer lifecycles. Tower, raids and region bosses each carry their own state, workers, playback and rewards; supporting one offers more savings than merely extracting a common interface.

## Proposed game

```text
Hunt → collect → assemble a build → tackle harder encounters

Progression: character/region access, Essence ranks, equipment investment
Activities: dungeons, optional Tower, optional Colosseum
Social: guilds, chat, equipment vault, market
Records: collections, achievements, titles, leaderboards
Support: Journal, inventory, presets, account/settings
```

Give frequent attention to **Hunt, Build, Inventory and Journal**. Group optional activities/social features separately, and consolidate accomplishments under Records.

Today the player can reasonably ask, “Which progression system am I neglecting?” The proposed game should make the next question, “What am I hunting for, and what would make this build work?”

## Priorities and migration

Scope: **S** = localized; **M** = one subsystem; **L** = several layers plus data conversion.

| Priority | Action | Main impact / migration concern | Scope |
| --- | --- | --- | --- |
| **P0** | Freeze new currencies, material shops and multiplayer expansion; model surviving reward supply | Prevent cuts from becoming an unintended progression slowdown; inspect real balances and unclaimed rewards before conversion | M |
| **P0** | Remove Tower gates on ordinary regional/dungeon access | Decouple solo progress from population; preserve existing unlocks and Tower records | M |
| **P1** | Remove Prophecy/guild/PvP chore economies | Largest reduction in recurring obligations; settle claims, caches, communal investment and currency balances | L |
| **P1** | Consolidate Essence/Style progression; retire Soulstone and collection bonuses | Easier experimentation; preserve owned abilities, earned ranks and spent-resource value | L |
| **P1** | Simplify equipment and dungeon progression | Clearer loot/access; preserve item value and clears, retune reward rates and Vigor together | L |
| **P2** | Merge Archive, build presets and Records; simplify Tower preparation | Reduce screens and conflicting selections; preserve old presets, loans, titles and history | M–L |
| **P2** | Test six Essence slots and simplify Nobility policies | Balance/entitlement changes need explicit conversion; never delete overflow Essences or paid coverage | M |
| **P3** | Defer tournaments, independent raids/region events, routine event seasons, Stronghold and guild wars | Reduce operating/content obligations; finish active runs and settle earned rewards before disabling workers | M to retire safely |

Implement these in separate releases, starting with weak reward wrappers. Preserve historical receipts and required replay readers; do not delete migrations or reuse retired identifiers. Conversion must be repeatable without double grants. Use the existing backend test entry point, `build/run-tests.ps1`, when implementing gameplay changes.

**Success criterion:** a player can build a viable character without PvP, guild chores or scheduled attendance, and can explain their next meaningful upgrade without consulting several unrelated systems.

---

**Document scope:** this edit only condenses the review. No gameplay, configuration, migration or deployment changes. Verify document links and `git diff --check`; runtime tests are unnecessary for this documentation-only change. Economic conversion rates and proposed balance changes still require validation before implementation.
