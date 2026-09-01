# LegendsLegacy Post-Alpha Game Assessment

This assessment treats the repository as evidence of the game a player could experience on 31 August 2026. The original assessment remains the design baseline; the dated progress ledger below records implementation completed afterward.

## Beta Preparation Progress — Updated 1 September 2026

### Current sequence status

| Step | Status | Current interpretation |
|---:|---|---|
| 1 | **Implemented; awaiting player validation** | The focused early journey, staged navigation, route restrictions, mobile quest access, and onboarding presentation fixes are in place. Beta acceptance criteria still require an unaided new-player playtest. |
| 2 | **Next** | Define and implement the three authored level-1-to-30 chapters and their memorable milestones. |
| 3 | Not started | Four deliberately attainable build identities remain a design and content task. |
| 4 | Not started | Chapter teaching encounters and boss capability tests remain to be authored and certified. |
| 5 | Not started | Combat-result diagnosis and inexpensive experiment/retry support remain outstanding. |
| 6 | Not started | Build-fit equipment comparisons and the player-facing stat contract remain outstanding. |
| 7 | Not started; supporting feedback added | Quest completion now produces a durable personal system-chat message, but the wider reward cadence has not been redesigned. |
| 8 | Not started | The connected gathering/crafting combat project remains outstanding. |
| 9 | Not started | Beta-scoped daily intentions remain outstanding. |
| 10 | Not started | Slice certification, instrumentation, and the pre-Beta feature freeze remain outstanding. |

### Step 1 implementation record

The following player-facing changes now establish the focused Beta journey:

- A goal-led journey card on Character Overview presents the current chapter, recommended action, recent progress, next unlock, and one optional goal.
- Sidebar destinations are progressively revealed from quest and character state instead of presenting the full feature catalog immediately.
- Direct routes into out-of-slice destinations are guarded while focused Beta mode is enabled. City systems, Prophecies, Tower, raids, region bosses, and Region 2 are outside the early journey; Shenic is intentionally limited beyond level 30.
- The active quest remains available on constrained/mobile layouts.
- The focused journey is controlled by `features.focusedBetaJourney` and currently defaults to enabled in the frontend environment and runtime environment templates.
- An unresolved First Hunt keeps World Map hidden and unhighlighted. The three hunt options receive the attention treatment instead, without suggesting that one has already been selected.
- First Hunt cards are top-aligned, retain a stable bottom reward row, and use a continuous highlight sweep without an unattractive held end frame.
- The training fight now opens directly on the combat result. The former two-step combat-summary tour was removed so the normal quest tracker can lead into the next quest.
- Completing a quest now persists and broadcasts a personal `System` chat message in the form `Quest completed: <quest title>.` Stable message IDs prevent duplicates when progression events are retried.

Primary implementation evidence:

- `LL/src/Presentation/ll/src/app/core/services/client-side/player-journey/player-journey.ts`
- `LL/src/Presentation/ll/src/app/core/guards/focused-beta-journey.guard.ts`
- `LL/src/Presentation/ll/src/app/features/game/character/character-overview/character-overview.component.*`
- `LL/src/Presentation/ll/src/app/layout/dashboard/sidebar/sidebar.component.*`
- `LL/src/Presentation/ll/src/app/layout/dashboard/quest-tracker/quest-tracker.component.html`
- `LL/src/Presentation/ll/src/app/layout/dashboard/dashboard.routes.ts`
- `LL/src/Presentation/ll/src/app/features/game/world/world.routes.ts`
- `LL/src/Presentation/ll/src/app/features/game/world/region/region.component.*`
- `LL/src/Presentation/ll/src/app/features/game/quests/quest-journal-page.component.html`
- `LL/src/Presentation/ll/src/app/shared/components/combat/combat.component.ts`
- `LL/src/Infrastructure/Service/Services.LL/Quests/QuestService.cs`
- `LL/src/Infrastructure/Service/Services.LL/Quests/QuestSystemChatPublisher.cs`

Verification completed during Step 1 and its follow-ups:

- The full Angular suite passed with 615 tests when the focused journey landed.
- A development Angular build and the affected journey, overview, quest-journal, and combat suites passed after the follow-up presentation fixes.
- The required backend runner passed all 1,785 tests after quest-completion chat was added.
- No database migration was introduced.
- Quest-completion chat reuses the existing `Chat:SystemMessages` configuration. It requires the updated game API/worker service code to be deployed; no deployment was performed during this work.

### Step 1 acceptance gate

Implementation does not by itself complete the design objective. Step 1 should be considered Beta-validated only after an unaided fresh-character session demonstrates that the player:

1. Chooses a First Hunt without attempting to navigate to World Map first.
2. Understands that all three highlighted cards are available choices and that none is preselected.
3. Completes the training fight without needing a combat-summary walkthrough.
4. Follows the quest tracker into the Soul Archive and subsequent onboarding steps.
5. Can state the immediate goal and next unlock after 15 minutes.
6. Does not encounter an exposed out-of-slice route through navigation or a direct URL.

Until that run is recorded, Step 1 is implemented but not empirically validated.

## 1. Direct Executive Assessment

LegendsLegacy's strongest asset is already real: Essences turn creatures into collectible active/passive ability packages, and those packages sit inside an automated combat game with unusually detailed internal combat statistics. The opening choice between the Goblin Warrior, Hollow Stag, and Skeleton is an effective miniature of the intended game. The player chooses a combat idea, wins the corresponding Essence, equips it, and sees a different way to fight. That is the central source of fun: **collect a capability, form a build hypothesis, and prepare it for a challenge**.

The game is not yet organized around that source of fun. Its weakest element is the authored sequence of decisions and challenges that should connect its many functioning systems. After a promising tutorial, the Shenic main quest mostly becomes repeated encounter-win and five-level gates. Regular areas randomize among five enemies, so players cannot reliably choose the problem their build is meant to solve. Rare Essence acquisition is extremely slow and mostly random. Meanwhile the navigation presents Essences, Soulstones, achievements, Tower, Prophecies, guilds, Colosseum, bazaar, leaderboards, crafting, and more as near-equal destinations. The result is a deep game model presented as a collection of dashboards.

The actual game partly supports the intended identity. Combat, Essences, equipment, idle rewards, gathering, crafting, and the two Region-1 dungeon routes can reinforce one another. The session-return summary is particularly good: it turns elapsed time into battles, power, gathering, crafting, Essence, dungeon access, and currency categories. The first-hunt quest, Soul Archive, three saved loadouts, armor profiles, weapon configurations, ability tags, dungeon route choices, and Vigor create genuine strategic material. These are not placeholders.

However, breadth currently hides the coherent center. World Tower progression is multiplayer-gated and not reliably calibrated. Region 2 is gated behind Tower floor 10 and has no equivalent authored quest arc. Region bosses have rewards disabled. Raids are disabled in production. Equipment sets have membership scaffolding but no live bonuses. Gathering levels barely change gathering. Essence evolution data contains no meaningful changes. Prophecy targets can demand thousands of wins or hundreds of tempering actions. PvP balance evidence cannot yet discriminate builds. Those surfaces should not frame the first Beta.

The largest gameplay risk is therefore not a lack of features. It is that players will experience **many progression axes without a clear causal chain**: what goal to pursue, which build decision would help, where to test it, and why the resulting reward matters. If Beta began immediately, the loudest feedback would likely concern menu overload, repetitive progression gates, unclear reward currencies, inaccessible population-dependent features, and unfinished destinations. Feedback on Essence attachment, meaningful build variation, combat adaptation, and the crafting-combat relationship would be contaminated by those obvious problems.

The central fun is strong enough to justify Beta after a deliberately narrow preparation pass. It is not yet strong enough to be found consistently by an unaided player. The next work should make the existing center unmistakable rather than extend the perimeter.

### Alpha-evidence confidence

**Directly Observed During Alpha:** No player survey, playtest transcript, session analytics export, player bug/feature-request export, or GitHub issue export was found in the repository. No claim in this assessment is presented as observed Alpha-player behavior.

**Strongly Suggested by the Game:** The conclusions below are supported by current game data, UI routes and components, domain/application rules, balance reports, developer-authored design/status documents, and the recent feature history. These show what the game permits and communicates, but not how frequently real players behaved in a particular way.

**Unknown Because Alpha Did Not Test It Adequately:** Actual time to comprehension; first-day and first-week retention; time to the first non-guaranteed desired Essence; which builds players independently discover; whether players change builds after losses; whether crafting feels valuable; whether Prophecies feel helpful or compulsory; whether the current pace is acceptable to casual players; and whether any social mode has a viable concurrent population.

The pre-Alpha assessment in `docs/alpha-release-game-analysis-and-14-day-plan.md` is useful historical context but is materially superseded by the current content. It is developer-authored evidence, not Alpha-player evidence.

## 2. Current Player Experience Map

| Stage | Goal, activity, and reward | Decision and system introduced | Likely player experience |
|---|---|---|---|
| First 15 minutes | Create/sign in to an account, open the quest journal, choose a First Hunt target, fight in the Training Area, absorb and equip the guaranteed Essence. The reward is the first active/passive ability pair. | Goblin Warrior offers aggressive repeated physical attacks, Hollow Stag offers magical weakening and resilience, and Skeleton offers sturdy physical protection. This introduces combat, quests, absorption, and the Soul Archive. | **Excitement:** a monster becomes part of the character. **Confusion:** the global sidebar already presents many destinations unrelated to this goal. **Stop risk:** a player who misses the welcome modal or quest CTA has no strong screen hierarchy telling them which system matters first. Evidence: `training-day.v4.json`, `soul-archive.v2.json`, `global-quest-tracker.component.*`, and `sidebar.service.ts`. |
| First session | Craft and equip a Tier-1 one-handed weapon, equip one of three rewarded tools, then win an encounter in Lumo Ruins. Rewards include the basic tools and entry into normal progression. | The player chooses a weapon base and a gathering tool. Crafting, equipment, passive gathering, and the world map are introduced in quick succession. | **Excitement:** the game demonstrates a connected fight-Essence-craft-travel loop. **Frustration:** the weapon decision is hard to relate to the chosen Essence without a simple build summary; several systems arrive before the player has repeated the Essence decision. Evidence: `first-weapon.v1.json`, `tools-of-the-trade.v1.json`, `into-lumo-ruins.v1.json`, and the crafting/equipment UI. |
| First day | Accumulate automated battles, XP, cinders, gathering materials, loot, and occasional collection progress; advance through Lumo and toward Blood Grove. | Decide whether to keep the current area running, change gear, level an Essence, craft, or inspect other menus. The return-session summary categorizes the haul well. | **Continue:** offline progress and the session summary make returning satisfying. **Confusion:** reward types arrive faster than their strategic uses are taught. **Stop risk:** the next main goal is mostly a level/win threshold, while desired Essence drops are too rare to structure the day. Evidence: `IdleCombatRewardCalculator.cs`, `character-experience.json`, `area-experience.json`, the combat session-summary component, and Shenic quest JSON. |
| First several days | Progress through five-level area bands, complete side quests that introduce a second Essence, ascension, focus, dungeons, crafting, gathering, Prophecies, and Colosseum. | Allocate newly unlocked Essence slots, select an Essence Focus, choose armor/weapon profiles, and decide which side systems to enter. | **Excitement:** two to four Essence slots begin to create combinations. **Frustration:** system introductions compete rather than form a sequence; random area enemy pools favor a general build; the player sees advanced collection and currency concepts before a stable build identity exists. Evidence: `EssenceSlotUnlockService`, `regions.json`, quest definitions, and Soul Archive screens. |
| First week | Under the idealized current 10,000-XP/hour victorious-combat model, level 40 takes about 117 hours and level 45 about 158 hours; real progress will be slower because of losses and interruptions. A highly engaged player can therefore approach the end of Shenic within a week. | Five Essence slots are available at level 40. The player can maintain three loadouts, craft/temper gear, pursue dungeons and Prophecies, or inspect population-dependent systems. | **Continue:** meaningful build identity can emerge from several active/passive pairs and differentiated equipment. **Stop risk:** the main quest remains repeated encounter counts, normal enemies do not create clear build tests, and many side goals resemble administration. Real player pacing remains unknown. Evidence: XP definitions, slot unlock rules, Shenic quests, and Soul Archive loadouts. |
| Early progression (levels 1-30) | Clear Tutorial, Lumo, Blood Grove, Crystal Creek, Moonlit Graves, Twilight Clearing, Old Forest, and reach Thornroot Hollow. Build Tier-1 equipment and enter Region-1 dungeons. | Choose up to four Essence pairs, a weapon configuration, an armor profile, and a dungeon route. | This is the strongest potential Beta slice: it contains the whole promise at manageable scale. It currently needs clearer milestones, curated acquisitions, differentiated tests, and reward placement. |
| Midgame (levels 30-45/50) | Continue through Embercap Burrows, Moonveil Marsh, and Duskmire Hollow; pursue ascension, blueprints, Prophecies, dungeons, and Tower preparation. | More slots and currencies increase optimization space. | Numerical breadth rises faster than authored choices. The final Shenic quest asks for 20 wins and awards three Lesser Monster Cores; there is no strong authored culmination proportional to the journey. A level-50 crafting side quest also sits oddly after an early crafting dependency. |
| Current end of available content | Reach Meran levels 50-65, additional dungeon families, Tower floors, region-boss/raid/PvP/guild surfaces, and long-term collection systems. | Choose among many progression tracks, several population-dependent or incomplete. | The end does not form one reliable goal ladder. All four Meran areas require Tower floor 10, a 15-player floor, while Meran lacks a corresponding main quest chain. Tower validation is incomplete; region-boss rewards are disabled; raids are production-disabled; equipment-set bonuses and Essence evolution are not live. Players either hit a population wall or discover that visible systems do not deliver their implied reward arc. |

### Where the journey currently breaks down

- **Confusion:** immediately broad navigation; similar visual weight for the core journey and optional/endgame systems; numerous currencies and collection pages.
- **Boredom:** Shenic's main progression repeatedly combines “win encounters” with “reach the next five levels”; regular-area enemy selection is random rather than a chosen tactical target.
- **Loss of confidence:** combat exposes detailed numbers but gives little actionable “why this build lost” interpretation; intended equipment percentage semantics are still unsettled in the v17 design work.
- **Punished experimentation:** Essence loadouts are relatively forgiving, but crafting, blueprint consumption, tempering Potential, and opaque item comparisons make equipment experiments feel costly.
- **Excessive waiting:** a base Essence chance of 0.01% per eligible creature and a creature-specific resonance ramp measured over thousands of failures do not support a timely desired-build goal.
- **Excessive complexity:** ten eventual Essence slots imply ten active and ten passive abilities, while collection, ascension, focus, codex, Soulstones, gear, and currencies accumulate alongside them.
- **Goals running out:** Shenic ends without a decisive solo capstone, while the next region is behind multiplayer Tower floor 10.
- **Irrelevant systems:** gathering levels have little capability progression; equipment sets have no bonuses; Essence evolution definitions contain no changes; region-boss rewards are off.

## 3. Core Loop Assessment

| Loop | Rating | What works | Break and minimum pre-Beta change |
|---|---|---|---|
| Fight → receive XP/cinders/loot → increase power → enter harder area | **Functional but shallow** | Automated combat, offline accrual, capped return rewards, and the session summary create a good idle rhythm. | Area progression mostly changes numbers and enemy pools; the follow-up decision is often simply “wait or equip the higher number.” Give each Beta chapter a named capability test and a reward that prepares the next one. |
| Defeat creature → acquire Essence → build loadout → test against content | **Strong in concept; confusing in acquisition** | The first guaranteed Essence proves the loop. Eighty active/passive packages, multiple slots, three loadouts, and rich ability mechanics provide depth. | Random 0.01% drops cannot carry the authored first-week arc; ordinary areas do not reliably test specialized answers. Curate 24 Beta Essences and place several deterministic choice rewards. |
| Lose/underperform → inspect combat → revise build → retry | **Incomplete** | Per-entity, per-ability, damage-type, healing, barrier, threat, summon, stagger, and W/L data exist. | Data is not translated into a diagnosis or a cheap comparison workflow. Add failure causes, encounter demands, before/after loadout comparison, and a fast retry path. |
| Gather materials → craft gear → temper it → improve combat | **Disconnected/overcomplicated** | Gathering happens naturally after wins; area abundance and tool choice can make location matter. Armor, weapons, and blueprints can support build identity. | Gathering skill progression is mostly nominal; crafting introduces quality, rarity, Potential, mastery, blueprints, and tempering before the player can judge an upgrade. Make one visible item project connect area, tool, recipe, and combat goal; restrict Beta to Tier 1. |
| Acquire dungeon access → choose route/Vigor trade-offs → secure or risk rewards | **Strong with targeted improvements** | Forecast maps, 100 Vigor, rests, retreat, pending loot, and first-clear blueprints make this the best active PvE loop. | Failure advice is generic, success/retreat summaries lack route learning, tiers are more scaling than mechanics, and repeat rewards vary. Include only both Region-1 families at Difficulty I and make their tests and rewards distinct. |
| Complete Prophecies → earn favor/currencies/caches → return daily/weekly | **Overcomplicated** | The system can point idle play toward goals and create return structure. | Targets such as hundreds/thousands of wins or hundreds of tempering actions can become compulsory and can reference systems outside the Beta slice. Ship a small post-onboarding daily set only; defer weekly revelation and paid rerolls. |
| Enter Tower/PvP/guild/market → gain prestige/social/economic progression | **Not currently worth including in first Beta** | Each contains substantial mechanics and future identity. | They require population, stable balance, complete rewards, or endgame context not available in a controlled first slice. Hide them rather than asking them to validate the core game. |
| Complete achievements/codex → earn titles/small bonuses → pursue collections | **Functional but shallow** | Passive recognition supports long-term attachment. | With 101 achievements, 37 titles, and 19 codex collections, the surfaces can become checklist administration. Surface a small set of nearby milestones; keep the rest passive and outside Beta evaluation. |

The most important missing link is not another loop. It is the connective sentence the game should repeatedly make true: **“I want to beat that challenge, so I will obtain or develop this Essence/equipment option, test it, understand the result, and earn the next option.”**

## 4. Game-System Scorecard

| Area | Rating | Current player-facing purpose | Main strength | Main weakness | Evidence | Minimum improvement required |
|---|---|---|---|---|---|---|
| Core combat | Beta-ready with targeted improvements | Automated resolution of prepared builds | Broad mechanics and detailed telemetry | Preparation consequences are not explained after combat | Combat models, 232 abilities, 25 statuses, combat stats UI | Actionable win/loss summary and certified Beta encounters |
| Essence acquisition | Needs substantial work before Beta | Turn defeated creatures into build capabilities | First Hunt proves acquisition can be exciting | 0.01% base drops and long creature-specific resonance do not support planned goals | Creature loot tables; `CreatureResonanceConstants` | Deterministic milestone choices plus bounded targeted acquisition |
| Essence buildcraft | Beta-ready with targeted improvements | Combine active/passive pairs into a distinct character | 80 pairs, rich ability tags, multiple slots/loadouts | Full catalog is cognitively excessive; empty Essence tags/evolution changes; live player diversity unknown | Essence/ability data; Soul Archive; meta reports | Curate 24, teach four directions, certify no obvious traps |
| Equipment choices | Needs substantial work before Beta | Complement the Essence build and provide power progression | Meaningful slots, configurations, quality/rarity/tempering | Stat meaning and upgrade comparisons remain hard to trust | Base recipes; equipment UI; `hybrid-equipment-v17-implementation-plan.md` | Freeze player-facing stat semantics and add clear comparisons |
| Armor-type identity | Beta-ready with targeted improvements | Choose defense/offense profile | Heavy, medium, light, and cloth have distinct authored distributions | Medium/light offensive stats can blur “armor” role and no guided examples show trade-offs | Armor base definitions | Validate four profiles and teach who each is for; no full redesign required |
| Weapon and off-hand identity | Beta-ready with targeted improvements | Shape offense, speed, penetration, defense, or cooldown | One-/two-hand and off-hand profiles create real choices | New players cannot easily connect profiles to abilities | Recipe/base definitions; equipment slots | Build-fit labels and side-by-side outcome preview |
| Jewelry identity | Needs substantial work before Beta | Add focused Power, health, regeneration, or blueprint effects | Blueprint layer can specialize jewelry | Base ring/amulet/relic identities are one-stat and shallow | Base recipes and blueprint data | Include a small curated blueprint set that produces visible build choices |
| Crafting | Needs substantial work before Beta | Convert gathered resources into chosen equipment | Player-directed base/blueprint creation and exciting temper outcomes | Too many axes before value is legible; balance/status documents show unfinished contract work | Crafting UI/data; 31 base recipes; 13 blueprints; v17 plan | Tier-1 project flow, stable stat semantics, understandable outcome/upgrade |
| Gathering | Needs substantial work before Beta | Passively collect crafting resources through tool and area choice | Low-friction attachment to combat and area abundance | Skill levels barely unlock or change capability | `gathering-system-progression-analysis.md`; region/node data | Present as a simple resource-targeting support loop; de-emphasize profession levels |
| Character progression | Beta-ready with targeted improvements | Unlock stats, areas, and Essence slots | Predictable levels and a slot every 10 levels | Five-level bands can reduce progress to waiting; real pace not observed | XP tables; slot service | Add meaningful chapter milestones and validate first-week pace |
| Region progression | Needs substantial work before Beta | Provide the world journey and escalating content | Shenic has an ordered area chain | Repetitive gates; Meran is flat and Tower-10 gated | `regions.json`; current main quests | Deliberately finish Shenic 1-30 only and hide later progression |
| Dungeon progression | Beta-ready with targeted improvements | Active route/risk challenge and blueprint source | Strong map/Vigor/retreat/pending-loot loop | Failure learning and repeat value are uneven; difficulty identity is thin | Dungeon family/map/reward data and status docs | Two Region-1 families at Difficulty I, distinct tests, clear results/rewards |
| Boss design | Needs substantial work before Beta | Test preparation and adaptation | Authored dungeon bosses contain mechanics | Mechanics/counters are not consistently taught or diagnosed | Dungeon room/boss definitions; failure analysis | Two readable bosses with pre-fight demands and post-loss guidance |
| World Tower | Should not be included in the first Beta | Multiplayer expeditions and long-term prestige gate | Substantial scouting/preparation/playback design | 5-15 player requirements, incomplete reward use, and only limited validation | `tower-floors.json`; Region-1 scaling reports | Hide for first Beta; later run a separately scoped multiplayer test |
| PvP | Should not be included in the first Beta | Competitive async combat, rating, Glory | Arena/defense/rank/reward foundation exists | Seasons/weekly arc incomplete and balance simulator cannot discriminate builds | Colosseum UI/status; Essence meta balance report | Exclude from core Beta; controlled opt-in test only after PvE comprehension |
| Guilds | Should not be included in the first Beta | Social identity and cooperative goals | Membership/chat and extensive building/mission/shop content | Population-dependent breadth can become obligation and distract from core | Guild screens; 9 buildings, missions/orders/shop data | Hide the guild system; if tester community needs it, expose membership/chat only outside the evaluated progression |
| Daily and weekly systems | Needs substantial work before Beta | Direct returning players toward goals | Prophecies can make idle time intentional | Extreme/misaligned targets, extra currencies, paid rerolls, compulsory feel | Daily/weekly Prophecy data | A small unlocked daily pool tied only to Beta goals; defer weekly layer |
| Achievements and collection | Beta-ready with targeted improvements | Recognition, titles, collection goals | Broad passive coverage and attachment potential | Large checklists and codex micro-bonuses risk administration | 101 achievements, 37 titles, 19 codex collections | Surface nearby meaningful milestones; hide out-of-slice entries |
| Reward design | Needs substantial work before Beta | Make every activity feel productive | Session summary categorizes rewards clearly | Too many small currencies/materials and several rewards lack immediate decisions | Reward calculators, quest/dungeon/Prophecy data | Define a reward cadence where each milestone enables a visible choice |
| Economy motivation | Should not be included in the first Beta | Trade via Cinder Bazaar and orders | Mature browse/sell/order surface | Small population and unsettled item value would distort progression | Bazaar screens/routes | Disable player market; balance the slice as self-found/self-crafted |
| Onboarding | Beta-ready with targeted improvements | Teach first hunt, Essence, craft, tool, and Lumo | Real guided chain with a meaningful first choice | Too many systems remain visible; sequencing ends before build thinking is learned | Onboarding quests, welcome modal, guides/tours | Progressive navigation and two more guided build decisions |
| UI comprehension | Needs substantial work before Beta | Navigate and understand a broad persistent RPG | Consistent, capable, responsive feature screens | Many equal-weight destinations and dense administrative panels | Sidebar, route trees, Soul Archive and city screens | Goal-led home/navigation, lock/hide out-of-slice surfaces, mobile objective visibility |
| Combat feedback | Needs substantial work before Beta | Explain combat performance | Excellent raw metrics | No concise causal diagnosis or adaptation path | Combat stats/result components; dungeon failure result | “What happened / what to try” summary and comparison between loadouts |
| Build experimentation | Beta-ready with targeted improvements | Save, switch, and compare ideas | Three Essence loadouts and inexpensive Essence switching | Equipment experimentation is more costly/opaque; no outcome comparison | Soul Archive/loadout UI; crafting/tempering rules | Free/cheap Beta respec and a saved build+gear comparison/test flow |
| Short-term goals | Beta-ready with targeted improvements | Decide what to do next session | Quest tracker and return summary provide anchors | Goals often count activity instead of expressing a build objective | Quest tracker and Shenic quests | One recommended objective and one optional goal at a time |
| Medium-term goals | Needs substantial work before Beta | Develop a build and clear a milestone over days | Area chain, dungeon blueprints, ascension, and slots can support this | They are not combined into authored chapters | Regions, dungeons, quests, Essence progression | Level-10/20/30 chapters with clear challenge and build reward |
| Long-term goals | Unable to determine | Pursue Tower, collections, guild/PvP prestige, regions | Many possible future aspirations | Current endgame is population-gated, incomplete, or unvalidated; no player evidence | Tower/region/guild/PvP/collection data | Show future aspirations but do not attempt to validate months-long retention in first Beta |
| Overall game identity | Needs substantial work before Beta | Be an idle buildcraft RPG about captured creature abilities | The distinctive Essence premise is materially implemented | Navigation and progression make breadth more visible than the build-test-learn cycle | Whole player journey | Narrow the slice and make every included system serve build development |

## 5. The Next 10 Steps Before Beta

### Step 1 — A New Player Sees One Coherent Journey, Not Thirteen Equal Destinations

#### Implementation Status

**Implemented on 1 September 2026; awaiting the Step 1 acceptance playtest.** See the progress ledger above for the shipped scope, verification, and remaining validation gate. The problem statement below is retained as the baseline this implementation is intended to solve.

#### Current Problem

The onboarding chain is good, but the surrounding product exposes major Character, World, Profession, City, and System destinations immediately. Optional, incomplete, endgame, and population-dependent features look as important as the First Hunt. The player must infer the intended game structure from menus.

#### Evidence From the Game

`sidebar.service.ts` exposes Inventory, Essences, Achievements, Soulstones, World Map, World Tower, Quests, Prophecies, Crafting, Guild, Colosseum, Cinder Bazaar, Leaderboard, and Settings. The landing experience redirects to login and then `/game/character/character-overview`; public world/roadmap/FAQ routes are commented out. The welcome modal and five onboarding quests provide a strong route, but the pinned quest display is hidden below the `sm` breakpoint. Raids are production-disabled, region-boss rewards are disabled, and Tower is not suitable for the early journey.

#### Why This Must Happen Before Beta

Otherwise Beta will primarily measure whether players can navigate an unfinished feature catalog. Confusion and abandonment will be attributed to the core loop even when the player never reached it.

#### Desired Player Experience

At every early stage, the player can answer: “My next meaningful goal is this; these one or two screens help me reach it; these other aspirations unlock later.” Optional systems feel optional, not neglected obligations.

#### Required Game Changes

- Make the character overview a goal-led home: current chapter, recommended action, recent progress, next unlock, and one optional side goal.
- Progressively reveal navigation after First Hunt, first crafted item, first two-Essence build, and first dungeon.
- Hide all features outside the Beta slice rather than showing disabled/rewardless pages.
- Keep the active objective visible on constrained screens.
- Explain each unlock in terms of what new decision it enables.

#### Systems Affected

Onboarding, navigation, character overview, quest tracker, feature gates, mobile UI, help guides.

#### Minimum Beta-Ready Version

One recommended-action card; four staged navigation groups; hidden Tower, PvP, bazaar, raids, region boss, Region 2, and deep guild progression; persistent mobile objective access.

#### How Beta Should Validate It

At least 80% of testers can state the primary goal after 15 minutes without external help; most reach and equip the first Essence; navigation-related help requests do not dominate first-session notes.

#### Dependencies

None. This sets the visible boundary used by every later step.

#### Risk of Deferring It

Every subsequent Beta observation will be confounded by players entering the wrong system or assuming unfinished surfaces are the intended game.

#### Scope

**Medium.** The work is mostly hierarchy, gating, copy, and a home-state presentation, but it touches shared navigation and unlock messaging.

### Step 2 — Levels 1-30 Form Three Authored Chapters With Memorable Milestones

#### Current Problem

After onboarding, Shenic progression frequently asks for a number of encounter wins plus the next five character levels. The areas change, but the player's reason for advancing does not. The final available path then leads toward a multiplayer gate rather than a satisfying solo culmination.

#### Evidence From the Game

`regions.json` orders Lumo (1), Blood Grove (5), Crystal Creek (10), Moonlit Graves (15), Twilight Clearing (20), Old Forest (25), and Thornroot Hollow (30). Current Shenic main quests commonly require 5-20 wins and the next five-level threshold. Meran's level 50-65 areas all require Tower floor 10, while its progression lacks an equivalent main quest arc.

#### Why This Must Happen Before Beta

Beta needs to test progression satisfaction, not tolerance for repeated counters. Without authored milestones, testers cannot assess whether build development creates anticipation over several days.

#### Desired Player Experience

Players remember an opening chapter where they form a build, a middle chapter where they solve a weakness, and a capstone chapter where their chosen identity is tested. Levels mark progress toward those events rather than being the events.

#### Required Game Changes

- Group levels 1-10, 11-20, and 21-30 into named chapters with a visible goal and promised reward.
- Replace some generic win counts with capability goals: survive a pressure pattern, exploit a vulnerability, clear a multi-target challenge, craft a build-supporting item, and defeat a dungeon boss.
- End level 30 with a solo-capable capstone and a clear “Beta journey complete” moment.
- Show future Shenic/World aspirations without allowing them to become the next required task.

#### Systems Affected

Regions, areas, quests, encounters, rewards, character overview, unlock messaging.

#### Minimum Beta-Ready Version

Three chapters, one named milestone per chapter, one meaningful choice reward in each, and one level-30 capstone. Existing areas and enemies should be reused wherever possible.

#### How Beta Should Validate It

Players can name their current chapter goal and remember at least two milestones; completion pace supports several return sessions; progression feedback discusses choices and challenges rather than only grind length.

#### Dependencies

Step 1's visible scope. The exact milestones should coordinate with Steps 3, 4, and 7.

#### Risk of Deferring It

First-week feedback will say the game is repetitive or directionless even if its combat systems are deep.

#### Scope

**Large.** It requires quest, reward, encounter, and presentation coordination, but should primarily restructure existing content rather than create a new region.

### Step 3 — Every Tester Can Deliberately Build Toward One of Four Recognizable Combat Identities

#### Current Problem

The first guaranteed Essence choice is meaningful, but subsequent build formation depends heavily on exceptionally rare random creature drops and a very large catalog. Players cannot reliably pursue a concept during the Beta window. Eighty Essences and eventual 10-slot loadouts expose more combinations than first-time comprehension can support.

#### Evidence From the Game

The game contains 80 Essences (72 Common and 8 Rare), each with an active and passive; all current Essence-level tags are empty, though ability tags are rich. Base creature Essence drop chance is 0.0001, with creature-specific resonance increasing over thousands of failures. Slots unlock as `floor(level / 10) + 1`, capped at 10, and three loadouts are available. Evolution records exist without actual changes. Optimizer evidence suggests diversity, but current live-player diversity is unknown.

#### Why This Must Happen Before Beta

If testers cannot intentionally assemble builds, Beta cannot answer the product's central question. Feedback would instead measure luck, catalog comprehension, and patience.

#### Desired Player Experience

By level 10 a player has chosen a direction; by level 20 they have made a synergy or coverage decision; by level 30 their four-slot loadout is recognizably theirs. A new Essence is exciting because it creates a legible choice, not another archival entry.

#### Required Game Changes

- Curate 24 Essences for the slice and hide out-of-slice/undiscovered catalog clutter.
- Support four named but flexible directions: physical burst/tempo, magical control/damage-over-time, sustain/barrier, and summon/attrition.
- Give deterministic choice rewards at chapter milestones and bounded targeted pursuits through creature focus or quests.
- Add build-role and synergy language derived from actual ability mechanics; do not rely on empty Essence tags.
- Treat leveling and at most the first ascension as progression; exclude evolution from Beta claims.
- Ensure no early choice permanently prevents switching direction.

#### Systems Affected

Essence drops, resonance/focus, quests, Soul Archive, codex visibility, ability presentation, balance, rewards.

#### Minimum Beta-Ready Version

Exactly 24 obtainable Essences, four supported directions, three guaranteed choice points after First Hunt, understandable role/synergy labels, and a catch-up path for a player who changes direction.

#### How Beta Should Validate It

Players can describe their build in ordinary language; at least four materially different patterns appear across testers; most milestone Essence acquisitions cause an equip/loadout decision; no single Essence appears compulsory across nearly all successful builds.

#### Dependencies

Step 2 determines when choices occur. Step 4 determines what validates each direction.

#### Risk of Deferring It

The game's signature collection system will feel like lottery-driven inventory administration, producing almost no useful buildcraft feedback.

#### Scope

**Large.** The content exists, but selection, acquisition, presentation, and bounded balance must be coordinated.

### Step 4 — Each Chapter Teaches a Combat Demand Before a Boss Tests It

#### Current Problem

Regular areas select from five creatures with near-uniform weights, making it hard to pursue or prepare for a particular opponent. General power is therefore more reliable than specialization. Dungeons contain stronger mechanics, but current failure guidance is broad and their difficulty variants lean heavily on scaling.

#### Evidence From the Game

Shenic areas generally contain five equally weighted creatures and mostly one- or two-enemy random encounters. Creature and ability data contain damage types, control, healing, barrier, summons, poison, bleed, area, defensive, and other mechanics. Goblin Mines and Forgotten Catacombs already provide route/Vigor structures and distinct first-clear blueprints. Dungeon failure results report broad causes and static suggestions, while World Tower balance reports show most floors are not yet dependable calibration targets.

#### Why This Must Happen Before Beta

Build diversity is only meaningful if content rewards different preparation. Without readable tests, Beta will conclude that the highest general Combat Rating is the only strategy.

#### Desired Player Experience

Players see a mechanic in ordinary fights, recognize it in an elite/preview, prepare for it, and then feel their choice matter against a boss. After losing, the next experiment is obvious but not mandatory.

#### Required Game Changes

- Assign one primary demand and one secondary variation to each of the three chapters.
- Use selected existing enemies to teach pressure, multi-target control, sustain/attrition, damage-type defense, interruption, or burst windows.
- Let players deliberately enter a lesson/target encounter rather than waiting for a random roll.
- Give the two included dungeon bosses distinct, forecast demands; avoid hard single-build counters.
- Certify the 24-Essence/four-direction cohort against every milestone, including weak but valid builds.

#### Systems Affected

Areas, encounter selection, creatures, dungeon bosses, ability telegraphs, quests, balance runner, rewards.

#### Minimum Beta-Ready Version

Three lesson encounters, three milestone tests, Goblin Mines I and Forgotten Catacombs I with distinct bosses, pre-fight demand summaries, and no unknown instant-fail mechanic.

#### How Beta Should Validate It

After a loss, players identify at least one relevant demand; successful players use more than one archetype; a meaningful share change a loadout or equipment choice before retrying; difficulty feedback references mechanics as well as numbers.

#### Dependencies

Steps 2 and 3. Step 5 supplies feedback after each test.

#### Risk of Deferring It

Buildcraft will exist mathematically but not experientially, and Beta will reward only passive numerical accumulation.

#### Scope

**Large.** It needs encounter curation, communication, and balance certification, though little new art or world content is required.

### Step 5 — A Loss Tells the Player What Happened and Makes the Next Experiment Cheap

#### Current Problem

Combat records rich performance data, but the player must interpret it without a concise causal summary. Dungeon failure advice is static and broad. Essence loadouts are saved, yet gear comparison and tempering make whole-build experiments harder to evaluate and reverse.

#### Evidence From the Game

Combat UI/data expose damage, healing, barrier, threat, damage type, ability contribution, summons, stagger, duration, wins, and losses. The session summary is already a strong model for categorization. The Soul Archive supports three loadouts. Dungeon results have a final location/cause/suggestions/Vigor/rooms/lost-loot presentation but limited route and build diagnosis.

#### Why This Must Happen Before Beta

Without a learn-and-retry loop, a failed build feels like wasted time. Testers cannot provide informed feedback on whether mechanics are fair or whether alternatives are meaningful.

#### Desired Player Experience

The player leaves a loss thinking, “I ran out of sustain during poison pressure; my barrier Essence helped, but my slow single-target damage did not. I can try loadout B or change this item.” Retrying does not require destructive investment.

#### Required Game Changes

- Add a short result hierarchy: encounter demand, decisive failure/win factors, strongest contributor, weakest gap, and one or two relevant options the player owns.
- Compare the result to the player's prior attempt or alternate saved loadout.
- Make changing equipped Essences and ordinary Tier-1 equipment free; provide a safe preview or refund for Beta crafting/tempering mistakes where needed.
- Add a one-action retry from the result while preserving access to the detailed statistics.
- Improve dungeon result summaries with route, Vigor curve, retreat opportunity, and secured/lost reward context.

#### Systems Affected

Combat results, statistics, loadouts, inventory/equipment, dungeon result screens, retry flow, help copy.

#### Minimum Beta-Ready Version

Actionable summaries for milestone encounters and two dungeon bosses, previous-attempt comparison, cheap loadout switching, and fast retry. It need not diagnose every generic idle battle.

#### How Beta Should Validate It

Players accurately explain a loss, make a relevant change, and retry; repeated attempts show purposeful changes rather than only waiting for levels; players trust that experiments are recoverable.

#### Dependencies

Step 4 supplies explicit encounter demands; Step 6 supplies trustworthy equipment meaning.

#### Risk of Deferring It

Losses will be read as opaque stat walls, causing churn or pure over-leveling and invalidating combat-depth feedback.

#### Scope

**Medium.** The telemetry exists; the main work is interpretation rules, presentation, and an experiment-friendly interaction contract.

### Step 6 — Equipment Comparisons Explain Build Fit With Stable, Trustworthy Stat Meaning

#### Current Problem

Equipment contains real choices, but percentage-like attributes, ratings, rarity, quality, Potential, blueprints, and tempering are hard to compare. The player cannot confidently tell whether an item supports their Essence plan or merely raises a displayed total. Set memberships imply a system whose benefits are not live.

#### Evidence From the Game

Heavy, medium, light, and cloth use distinct stat profiles; one-/two-handed weapons and off-hands specialize Power, crit, attack speed, penetration, armor, resistance, or cooldown; base jewelry is mostly a single-stat identity. `hybrid-equipment-v17-implementation-plan.md` records an implemented recipe-composition stage but an unfinished conversion to stable player-facing percentage semantics. `equipment-sets.json` is intentionally empty and set bonuses remain deferred. Current qualities are Crude, Standard, Fine, Exceptional, and Masterwork; rarities run Common through Legacy.

#### Why This Must Happen Before Beta

Testers cannot evaluate equipment/build interaction if the numbers do not have a settled meaning or the comparison UI rewards an opaque aggregate score.

#### Desired Player Experience

The player understands what an armor/weapon/jewelry choice sacrifices and gains, how it supports the current loadout, and whether a new item is an upgrade for this build rather than universally better.

#### Required Game Changes

- Freeze and consistently display the stat contract intended by the v17 work.
- Show effective before/after values and a small set of build-relevant deltas, not only item rolls or total CR.
- Give armor, weapon, off-hand, and jewelry concise role language with no false “best” recommendation.
- Limit Beta item outputs to known Tier-1 combinations; remove set terminology/affordances until bonuses exist.
- Validate that defensive, sustain, tempo, and damage profiles can each be rational choices within the four builds.

#### Systems Affected

Equipment generation, attributes, display, comparison, combat calculations, crafting previews, balance anchors, help text.

#### Minimum Beta-Ready Version

One settled stat model, effective-value tooltips, build-fit comparisons for all combat slots, and no visible incomplete set-bonus promise.

#### How Beta Should Validate It

Players choose different armor/weapon profiles for stated reasons; comparison questions decline; an item with lower generic CR is sometimes deliberately selected because it improves the intended build.

#### Dependencies

Step 3's four build directions. It should precede Step 7's final reward tuning.

#### Risk of Deferring It

Equipment feedback will be dominated by mistrust and confusion; players will equip the largest number and the crafting loop will lose strategic value.

#### Scope

**Large.** Player-facing presentation is only part of the work because semantics, generation, calculations, and balance references must agree.

### Step 7 — Every Chapter Reward Immediately Enables the Next Meaningful Decision

#### Current Problem

Rewards are abundant but fragmented across cinders, Soulstones, Fate Echo, sigil fragments, ascension fragments, Essence Dust, Monster Cores, materials, dungeon access items, Glory, Tower currencies, guild resources, and more. Generic quest material bundles and micro-bonuses often do not create an immediate choice. Region-1 dungeon repeat rewards are uneven.

#### Evidence From the Game

The idle reward calculator and return summary classify many reward channels effectively. Shenic quests often grant ore, wood, rawhide, Soul Dust, or cores after generic gates. Prophecies introduce additional currencies and caches. Dungeon first clears grant blueprints, while repeat completion reward definitions are inconsistent across regions. Codex collections frequently grant very small relative bonuses.

#### Why This Must Happen Before Beta

Reward satisfaction and progression pace cannot be tested when players do not understand what rewards are for or receive them long before/after the relevant decision.

#### Desired Player Experience

Every milestone produces one of four readable outcomes: a new capability, a choice between capabilities, progress toward a named project, or access to the next challenge. Routine rewards visibly advance one selected goal.

#### Required Game Changes

- Define the level-1-to-30 reward cadence before adjusting individual values.
- Use chapter rewards for Essence choices, build-supporting blueprints/items, the first ascension, and dungeon access.
- Reduce visible Beta currencies; postpone or convert out-of-slice rewards.
- Give repeat dungeons a modest targeted reason to run without making them mandatory.
- Show “this advances…” on material/currency gains and let the player pin a goal.
- Balance time-to-reward using actual Beta sessions, not only continuous idealized combat.

#### Systems Affected

Quests, idle rewards, dungeons, crafting, Essences, currencies, codex, inventory, session summary, goal tracking.

#### Minimum Beta-Ready Version

A written and implemented cadence for three chapters; no unexplained out-of-slice currency; one targeted reward per milestone; pinned-goal progress in results; useful repeat rewards for both included dungeons.

#### How Beta Should Validate It

Players can explain why major rewards matter, use them within the next session, and identify what routine play is advancing. Reward excitement is not limited to rare random drops.

#### Dependencies

Steps 2, 3, and 6 define the milestones, capabilities, and item meaning.

#### Risk of Deferring It

Beta will report that progress feels either stingy or cluttered, but the feedback will not reveal whether the core build loop is satisfying.

#### Scope

**Large.** It crosses most progression data, but the small Beta boundary prevents an economy-wide rebalance.

### Step 8 — Gathering and Crafting Complete One Visible Combat-Equipment Project

#### Current Problem

Gathering passively produces materials and crafting has substantial machinery, but their joint strategic purpose is obscured. Gathering profession levels provide little capability change. Crafting exposes recipes, mastery, quality, rarity, blueprints, Potential, and tempering; the depth exceeds what the early player's decision currently requires.

#### Evidence From the Game

The player equips one of three tools; wins can trigger gathering; each area can favor an abundant resource by 50%. The current data contains 31 base recipes, 13 blueprints, 19 materials, and 188 items. `gathering-system-progression-analysis.md` finds authored node gates at level 1 or absent and little mechanical difference between low and high gathering levels. Crafting produces a base/form/optional blueprint item, then tempering consumes Potential and can improve or harm outcomes.

#### Why This Must Happen Before Beta

If players cannot see why they gather or craft, these systems add waiting and menus while distracting from combat. If they can complete a desired build item, they validate a key part of the product promise.

#### Desired Player Experience

The player pins a Tier-1 item, sees which tool and Beta area supply its materials, watches return-session progress, crafts a predictable base with a chosen specialization, and tests the result in the next challenge.

#### Required Game Changes

- Present gathering as resource targeting attached to combat; de-emphasize or hide profession levels that unlock nothing.
- Add a pinned crafting project with material sources, progress, expected result, and build-fit preview.
- Restrict to Tier-1 combat equipment and four curated blueprint identities.
- Introduce tempering only after the first successful project; make its risk, Potential cost, and possible outcomes explicit and forgiving.
- Ensure material pacing supports a project in each chapter without compulsory farming.

#### Systems Affected

Gathering tools/nodes/areas, session summary, crafting recipes/blueprints, equipment preview, tempering, quests, inventory.

#### Minimum Beta-Ready Version

One guided item project in chapter 1 and one player-chosen project later; all three tools remain usable; Tier 1 only; no claimed gathering specialization; simple transparent tempering tutorial.

#### How Beta Should Validate It

Players intentionally change tool or area for a project, complete at least one desired item, and can explain how it changed their build. Track whether they ignore gathering after the tutorial and why.

#### Dependencies

Steps 2, 6, and 7 establish milestone timing, item meaning, and reward cadence.

#### Risk of Deferring It

Crafting and gathering will be judged as isolated timers, and one of the intended core progression connections will remain untested.

#### Scope

**Medium.** It narrows and connects existing systems rather than building a full profession endgame.

### Step 9 — Daily Goals Reinforce the Player's Chosen Plan Without Becoming Obligations

#### Current Problem

Prophecies can direct returning play but current targets range from hundreds to tens of thousands of wins and hundreds/thousands of tempering actions, while rewards introduce more currencies and revelation/reroll layers. A new player can be asked to optimize systems not yet meaningful to their build.

#### Evidence From the Game

There are 19 daily and 8 weekly Prophecy definitions. Daily targets include 300-1,200 combat wins, 12-24 dungeon rooms, 1-4 dungeon completions, 10,000-40,000 Essence XP, and 360-1,440 temper actions/Potential measures; weekly targets rise dramatically. Favor, Soulstones, sigil fragments, Fate Echo, caches, weekly revelations, and paid rerolls create several incentive layers.

#### Why This Must Happen Before Beta

Compulsory or impossible daily tasks distort retention. Testers may return for fear of missing rewards rather than because the build loop is satisfying, giving misleading engagement data.

#### Desired Player Experience

After understanding the core game, the player chooses one of a few achievable daily intentions aligned with combat, an Essence, a craft project, or a dungeon. Missing a day has no permanent cost.

#### Required Game Changes

- Unlock Prophecies only after the player has a two-Essence build and understands the return summary.
- Curate a small pool that references only systems the player has unlocked and goals they can complete in ordinary play.
- Let the player select a goal category; scale targets to actual Beta pace.
- Remove paid rerolls, weekly revelation, and out-of-slice currencies from the first Beta.
- Frame Prophecies as optional focus, not the optimal mandatory source of core power.

#### Systems Affected

Prophecies, unlocks, objectives, rewards, currencies, session summary, notifications.

#### Minimum Beta-Ready Version

Three daily choices, one active choice at a time, no weekly system, no paid reroll, no streak penalty, and rewards that advance a pinned Beta goal.

#### How Beta Should Validate It

Players understand Prophecies, choose different categories, and often complete them through intended play without feeling forced to change plans. Returning motivation remains present on days without a Prophecy reward.

#### Dependencies

Steps 1, 2, 7, and 8 define unlocked systems, pace, rewards, and valid goals.

#### Risk of Deferring It

Retention observations will measure obligation and target imbalance rather than the intrinsic strength of the game.

#### Scope

**Medium.** The framework exists; the work is unlock logic, target/reward curation, scaling, and presentation.

### Step 10 — Certify and Instrument the Narrow Slice, Then Stop Adding Features

#### Current Problem

The game has sophisticated automated balance tooling, but existing reports explicitly retain uncertainty: elite build search is unstable in some profiles, the balanced singleton simulation provides no discrimination, and the Region-1 Tower scaling gate validates only a limited subset. Repository evidence cannot answer real-player comprehension, pace, or attachment questions.

#### Evidence From the Game

`docs/content-balancing/milestone-11-essence-meta-analysis.md` reports no >=80% mandatory Essence but four underused Essences and sparse pair warnings; its singleton side-alternation result gives every Essence a 0.5 rate and cannot support PvP conclusions. `docs/content-balancing/region-1-scaling-validation.md` and elite certification reports document unstable/review outcomes and missing curated player evidence. The repository contains no Alpha survey or analytics export.

#### Why This Must Happen Before Beta

Beta should begin with known gross traps removed and with a focused observation plan. It should not wait for perfect mathematical balance, but it also should not unknowingly ship impossible encounters or a dominant starter path.

#### Desired Player Experience

All four promised build directions can complete the slice with different strengths, weaker legal combinations are recoverable, progression pace is deliberate, and testers are asked focused questions at the moments that matter.

#### Required Game Changes

- Create a frozen Beta matrix: 24 Essences, four representative directions, relevant Tier-1 gear states, six chapter/milestone encounters, and two dungeon bosses.
- Run deterministic and multi-seed simulation to find gross dominance, traps, non-monotonic difficulty, and reward/power discontinuities.
- Perform short human journey sessions for comprehension and feel; automated balance cannot certify those.
- Record only game-relevant Beta events: onboarding milestones, objective changes, loadout/equipment changes, losses/retries, chosen rewards, crafting project completion, dungeon route/result, and exit point.
- Define a release gate and freeze scope once it passes; do not add another system.

#### Systems Affected

Balance content, encounters, Essences, equipment, progression pacing, Beta observation and feedback prompts.

#### Minimum Beta-Ready Version

No encounter is impossible for all four directions; no single option is required; no obvious trap blocks progress; a complete new-player human run reaches the level-30 capstone; the focused Beta questions in Section 9 can be answered with consented observation and feedback.

#### How Beta Should Validate It

Beta supplies behavior and explanations that confirm or reject the four build directions, challenge readability, reward cadence, experiment loop, crafting relevance, and first-week pace. Results can be segmented by path rather than reduced to general sentiment.

#### Dependencies

All earlier steps define the slice to certify. This is the final gate, not a parallel content-expansion phase.

#### Risk of Deferring It

Known tooling limitations and missing Alpha evidence will be mistaken for design confidence; Beta may discover gross issues that should have been bounded beforehand and still fail to answer the core questions.

#### Scope

**Medium.** The balance infrastructure exists. Scope is driven by curated scenarios, human runs, focused event capture, and the discipline to freeze the slice.

## 6. Required Progression Slice for Beta

The first Beta should be a **self-contained Shenic level-1-to-30 campaign**, deliberately balanced as a solo/self-found experience with optional lightweight social contact.

| Dimension | Exact first-Beta recommendation |
|---|---|
| Regions | Shenic only. Region 2/Meran is visible only as a future aspiration, not enterable. |
| Areas | Tutorial plus seven meaningful areas: Lumo Ruins, Blood Grove, Crystal Creek, Moonlit Graves, Twilight Clearing, Old Forest, and Thornroot Hollow. The level-30 Thornroot chapter contains the capstone. Embercap, Moonveil, and Duskmire remain hidden until the slice is extended deliberately. |
| Dungeons | Two families, Difficulty I only: Goblin Mines and Forgotten Catacombs. Preserve route, rest, Vigor, retreat, pending-loot, and first-clear identity. Hide Difficulties II-III. |
| Bosses | Hobgoblin Warden and The Bound Wraith as the two dungeon tests, plus one curated solo level-30 capstone encounter using an existing fitting enemy/mechanic where possible. Three lesson/milestone elites precede them. |
| Equipment tiers | Tier 1 only. Tier 2 is neither required nor visible as an actionable Beta goal. |
| Rarity and quality | Common through Epic should be realistically attainable. Crude through Exceptional qualities should occur naturally; Masterwork may remain a rare aspirational result, not an expected balance state. Unique, Legendary, and Legacy are outside the certified slice. |
| Essences | Exactly 24 obtainable/visible Essences, including the three First Hunt choices. Curate six candidates around each of four overlapping directions; an Essence may support more than one direction. |
| Viable directions | Four: physical burst/tempo; magical control/DoT; sustain/barrier; summon/attrition. Hybrids are welcome, but Beta certification guarantees these four readable starting identities. |
| Essence progression | Levels and no more than the first ascension. Focus may support bounded targeting after it is taught. Evolution is excluded; full codex optimization is not a Beta goal. Four equipped slots at level 30 are enough to test synergy without the endgame's 20 simultaneous ability effects. |
| World Tower | Not included. Its population requirements and incomplete validation make it a separate future multiplayer test. It must not gate any Beta content. |
| PvP | Not included in the primary Beta. If desired later, run a time-boxed, opt-in arena experiment with no progression-critical rewards and analyze it separately. |
| Guilds | At most guild membership and chat, if already reliable and useful for tester community. Hide buildings, missions, daily orders, shop, and rankings. Disabling the entire guild navigation is also acceptable for a small cohort. |
| Gathering | Available in its simple form: three tools, win-triggered collection, area abundance, and project material sourcing. Do not present gathering levels as a deep profession. |
| Crafting | Fully usable only for the Tier-1 project flow: base equipment, four curated blueprint identities, clear outcome preview, and forgiving tempering. Hide Tier 2 and unvalidated recipes/blueprints. |
| Prophecies | Limited daily-choice version after the two-Essence milestone. No weekly revelation, paid rerolls, or out-of-slice targets. |
| Achievements/titles | Passive and limited to nearby slice milestones. Hide unreachable collection entries. |
| Soulstones | Important but limited: expose only understandable nodes relevant to the slice after the core build is established; do not make the entire account tree a first-session concern. |
| Hidden/disabled | Meran, World Tower, Colosseum, Cinder Bazaar, raids, region boss, equipment-set benefits/affordances, Essence evolution, high dungeon difficulties, full guild progression, and all rewards/currencies exclusive to those systems. |

This is large enough to test the real game. It contains idle progression, four-slot Essence buildcraft, meaningful equipment profiles, gathering-to-crafting, two active dungeons, several combat demands, a first ascension, daily return guidance, and a multi-day progression arc. It is narrow enough for one developer to inspect every reward, encounter, Essence, recipe, tooltip, and transition and to interpret Beta behavior without population-dependent noise.

## 7. Beta Player Experience Target

On Beta day, a tester should experience the following concrete contract:

1. Within the first minute after entry, the overview names the current objective and routes to the First Hunt.
2. Within the first 15 minutes, the tester chooses one of three combat ideas, obtains that creature's Essence, understands its active and passive, equips it, and sees its contribution in combat.
3. During the first session, the player crafts a Tier-1 weapon whose displayed role connects to that Essence, equips a gathering tool, enters Lumo, and knows the next stopping milestone.
4. By level 10, the player has made a second Essence decision and can describe a tentative build direction.
5. Every return summary answers “what happened while away?” and “which selected goal advanced?” Routine gains are useful; rare gains are visibly consequential.
6. A lesson encounter exposes a mechanic before a milestone boss uses it. A win confirms a decision; a loss names the decisive pressure and presents a cheap relevant experiment.
7. Equipment comparison shows effective trade-offs and build fit. Switching between saved build ideas is safe; no early choice permanently ruins the character.
8. Gathering answers “where should I fight for this project?” Crafting answers “what predictable item am I building and how will it affect my combat?” Tempering risk is explicit.
9. Goblin Mines I and Forgotten Catacombs I feel like different adventures and different build tests, not two stat-scaled corridors. Route, Vigor, retreat, and reward consequences are understandable.
10. By level 30, the player has a recognizable four-Essence identity, a coherent Tier-1 equipment set, at least one crafted specialization, and a completed capstone. The game celebrates that milestone and previews future regions, deeper dungeons, larger builds, and multiplayer aspirations without sending the tester into unfinished content.

The desired feeling is not “I have seen every system.” It is “I understand my character, I made it mine, I solved several problems with it, and I can imagine how it will grow.”

## 8. What Should Be Cut, Disabled, or Deferred

| Item to defer | Why it loses priority before first Beta |
|---|---|
| New regions or Meran expansion | The existing Shenic arc does not yet convert its content into a coherent build journey. More areas would repeat the same structural weakness. |
| World Tower expansion/calibration beyond a future isolated test | Floors require 5-15 players, floor 10 gates Meran, and current reports do not validate the full curve. It cannot answer the solo core-loop questions. |
| Raids and other large multiplayer encounters | Raids are production-disabled and demand high-level gear, multiple Essences, and population. They are endgame content for a later cohort. |
| Region boss development | The current Mad King is Tower-10 gated and has rewards disabled. It should not compete with making two included bosses excellent. |
| Colosseum seasons, tournaments, rewards, and PvP-wide balance | PvP adds a different balance target; current singleton evidence cannot discriminate Essences, and a small population gives poor competitive data. |
| Guild buildings, missions, orders, shop, and rankings | Nine buildings and several recurring systems would create social obligation before the player base or core retention is established. Membership/chat is sufficient if any guild surface remains. |
| Cinder Bazaar/player economy | A small Beta population and unsettled equipment values would create artificial scarcity, transfers, and progression distortion. Test self-found/self-crafted pacing first. |
| Equipment-set bonuses | Membership scaffolding exists but the live benefit set is empty. A set system would add another build axis before basic item meaning is stable. Hide the promise. |
| Essence evolution content | Evolution records contain no meaningful changes. Leveling and one ascension already provide enough progression to test the four-slot build. |
| Tier-2 equipment and full rarity/quality ladder | Tier 1 already supports the intended early equipment decisions. Higher tiers multiply balance and economy cases without answering a new first-Beta question. |
| A full gathering-profession redesign | Gathering levels are shallow, but the useful tool/area/project choice can be tested now. Do not build skill trees or new nodes before learning whether players value the supporting loop. |
| More Essences, enemies, dungeons, achievements, titles, or blueprints | The repository already contains 80 Essences, 101 creatures, 4 dungeon families, 101 achievements, 37 titles, and numerous recipes. Selection, distinction, and placement are the bottlenecks. |
| Perfect balance across all 80 Essences and 10-slot builds | This is unbounded and unnecessary. Certify 24 Essences, four slots, four directions, and the included encounters; use Beta to discover the next balance questions. |
| Full weekly Prophecy/revelation and monetized reroll tuning | These systems can manufacture activity while hiding whether intrinsic goals work. Validate a small optional daily layer first. |
| Broad cosmetic polish | Visual work should be limited to comprehension, hierarchy, readable mechanics, reward excitement, and game identity. Cosmetic polish that does not change understanding should wait. |
| Infrastructure, deployment, and enterprise-readiness initiatives unrelated to player experience | They are outside this game-focused plan and do not strengthen the intended Beta question. Any genuinely blocking operational minimum should be handled separately. |

## 9. Beta Questions

The Beta should answer these in priority order. Feedback prompts and observation should be attached to the relevant moment, not collected only as a general end survey.

1. **Do players understand that the core game is acquiring capabilities, forming a build, testing it, and adapting?** Ask after First Hunt, first milestone, and capstone.
2. **Can players independently describe their build and why its Essences work together?** Record descriptions at levels 10, 20, and 30, then compare them with loadout behavior.
3. **Does a new Essence create an understandable equip/replace/save decision, and do players become attached to particular Essences?** Distinguish attachment from simple rarity or higher numbers.
4. **Do the four build directions produce materially different successful play, with no compulsory Essence or obvious unrecoverable trap?** Compare builds, results, changes, and reasons—not only clear rates.
5. **Can players identify why they won or lost and choose a relevant next experiment?** Observe the first milestone loss and the next action.
6. **Do regular encounters teach the mechanics that dungeon bosses test, and do bosses reward adaptation rather than only waiting for power?** Ask players to predict and then explain each boss.
7. **Does equipment strengthen build identity, and can players understand a trade-off without relying only on Combat Rating?** Inspect comparison choices and stated reasons.
8. **Does the gathering-to-crafting project feel like purposeful progress or passive waiting?** Measure project completion, tool/area changes, perceived agency, and abandonment.
9. **Are major rewards understandable and immediately useful, while routine rewards visibly advance a chosen goal?** Ask what the player plans to do with each milestone reward.
10. **Is the level-1-to-30 pace satisfying across different play schedules?** Compare active and idle players; identify dead time, rushed introductions, walls, and natural stopping points.
11. **Does the return-session summary motivate the next action, or merely report accumulated numbers?** Observe whether a player changes goal, build, area, or project after it.
12. **Do limited daily Prophecies help players choose a plan without feeling compulsory?** Compare return motivation with and without completion and ask about missing a day.
13. **Which included systems are ignored, and is that because they are optional, poorly introduced, or not valuable?** In particular, inspect Soulstones, crafting, gathering, codex, and achievements.
14. **At level 30, do players want more because of their character and future challenges, or only because more bars exist?** Ask what they most want to pursue next before showing future features.

Do not use the first Beta to answer Tower raid composition, endgame PvP balance, guild-economy health, multi-month retention, the full Essence meta, or the Region-2 economy. Those require different cohorts and later slices.

## 10. Final Prioritized Sequence

| Order | Step | Player Problem Solved | Core Systems Affected | Must Be Completed Before Beta? | Beta Validation |
|---:|---|---|---|---|---|
| 1 | One coherent guided journey — implemented, validate next | Too many equal destinations obscure the core | Navigation, onboarding, overview, unlocks | Yes | Players state and follow the primary goal unaided |
| 2 | Three authored level-1-to-30 chapters | Progress feels like repeated counters and levels | Quests, regions, encounters, rewards | Yes | Players remember milestones and sustain multi-day goals |
| 3 | Four deliberately attainable build identities | Random acquisition and catalog breadth prevent intentional builds | Essences, drops, Soul Archive, quests | Yes | Distinct builds emerge and players explain them |
| 4 | Teach demands before bosses test them | General power dominates specialization | Enemies, areas, bosses, dungeons, balance | Yes | Players recognize mechanics and adapt |
| 5 | Actionable loss diagnosis and cheap retry | Players cannot learn confidently from experiments | Combat results, stats, loadouts, retry | Yes | Relevant change follows a loss |
| 6 | Trustworthy build-fit equipment comparison | Gear choices are opaque or reduced to one score | Equipment, attributes, crafting, UI | Yes | Players knowingly choose different trade-offs |
| 7 | Decision-producing reward cadence | Rewards are fragmented and weakly connected to goals | Quests, currencies, Essences, dungeons | Yes | Major rewards are understood and promptly used |
| 8 | One connected gathering/crafting project loop | Supporting systems feel like isolated timers | Gathering, crafting, equipment, areas | Yes | Players target resources and craft a desired build item |
| 9 | Optional, aligned daily intentions | Recurring tasks risk obligation and scope mismatch | Prophecies, rewards, return flow | Yes, if Prophecies remain visible | Players choose goals without feeling forced |
| 10 | Certify, observe, and freeze the slice | Gross traps and unfocused testing would waste Beta | Balance, content, progression, feedback | Yes | Beta can answer the defined design questions |

1. **The single most important remaining change before Beta:** Turn levels 1-30 into three authored build-test-adapt chapters now that the focused journey shell is implemented.
2. **The most serious weakness currently hidden by the game's large feature set:** The game lacks an authored cadence of consequential decisions and specialized challenges after its strong opening tutorial.
3. **The strongest existing part of the game that should receive more focus:** Essence active/passive buildcraft, supported by automated combat and unusually rich performance telemetry.
4. **The system most likely to require simplification:** The combined equipment/crafting presentation—especially percentage semantics, rarity, quality, Potential, blueprints, and tempering before the player can judge build fit.
5. **The system most likely to distract from Beta preparation:** World Tower, because its visible scale, multiplayer requirements, Region-2 gate, and balance work can absorb large effort without validating the first game's core loop.
6. **The feature most likely to seem important but should actually be deferred:** PvP. It feels like a natural proof of build diversity, but it introduces population and balance noise before players can understand PvE build outcomes.
7. **The largest unanswered game-design question:** Will players form an emotional and strategic attachment to an Essence-based build when acquisition is deliberate and content actually asks that build to adapt?
8. **The exact purpose the first Beta should have:** Validate that a new player can understand, construct, test, revise, and become attached to a distinctive four-Essence character across a satisfying multi-day Shenic progression arc, with equipment and crafting reinforcing that arc.
9. **The point at which Beta preparation should stop and testing should begin:** When an unaided new-player run can complete the level-1-to-30 slice; all four certified directions can clear its six milestone encounters and two dungeons without a compulsory option; rewards and losses are understood; and every outside system is hidden or clearly unavailable. Do not wait for perfect balance or more content.
10. **The next concrete action:** Run the Step 1 fresh-character acceptance session, record any blockers, then write the Step 2 level-1-to-30 content contract listing the seven areas, three chapters, six milestone encounters, two dungeons, 24 Essences, four build directions, and milestone rewards. Keep out-of-slice systems hidden while that contract is implemented.
