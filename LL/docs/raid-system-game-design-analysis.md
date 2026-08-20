# Raid System Game Design Analysis

> Review status: updated after implementation of the free-entry, three-party preparation / combined-boss redesign on 2026-08-20.
> This document analyzes standalone Raids. It does not treat Raids as guild content.

## 1. Executive Summary

The Raid foundation is worth keeping, but it is not yet strong enough to justify producing many more bosses without another design pass.

The system has a genuinely distinct structure: a public asynchronous roster is divided into Rearguard, Vanguard, and Main Guard parties. Each party performs a specialized preparation encounter, then all three regroup fully restored for a combined Final Assault against the boss. Preparation results create visible consequences in that final battle, combat resolves in the background from frozen character snapshots, and all four encounters can be replayed. That is meaningfully more than ordinary combat against an enemy with extra Health. It fits an idle RPG especially well because the player's primary interaction is preparation rather than real-time execution.

Its central weakness is not engineering. The persistent state machine, worker leases, combat snapshots, playback artifacts, atomic assignments, and claim flow are robust. The weakness is that the current encounter content does not yet make enough use of that machinery. The redesign establishes three readable build tests—Area clearing, focused single-target damage, and defensive endurance—but the player's own Essence, equipment, damage-type, sustain, and targeting choices are not explained clearly enough to turn failure into a reliable build puzzle.

Creating and joining a raid are free, which avoids circular entry economies, leader-only costs, non-poolable repayment, and incentives to join rather than lead. Direct limits—one active raid per character, one account slot per raid, one led raid per character, and a cap on open musters—control abuse without taxing the organizer.

The remaining highest-priority issues are:

1. Weekly reward reduction is spent by the first reward from a boss, even on a weak outcome or lower tier.
2. Contribution and payout are damage-only, undervaluing healing, barriers, mitigation, debuffs, and other support.
3. Players receive too little diagnostic information to understand why an encounter failed.
4. Boss and tier identity is still dominated by stat scaling and enemy counts rather than new automated-combat rules.
5. Public first-come joining lacks leader approval/removal and participant readiness, creating avoidable griefing and stale-snapshot risks.
6. Only the leader makes the important multiplayer decisions; participants are mostly passive after joining.
7. The fixed three-party preparation schema is a good identity today but may become restrictive if every future raid uses identical objectives and consequence formulas.

The preferred direction is:

> **Raids are asynchronous shared build-puzzle encounters where three specialized preparation parties create visible consequences, then reunite for one combined automated boss encounter. Players use clear forecasts and combat diagnostics to adapt their builds and master boss-specific mechanics across inexpensive repeated attempts.**

Overall Raid design score after the structural redesign: **7 / 10**. The architecture, idle compatibility, party identities, and climax are strong; strategic communication, content calibration, reward incentives, and social control still need work.

## 2. Current Raid System

### Gameplay Loop

The implemented loop is:

```text
Discover a regional Raid boss
→ satisfy personal level and shared World Tower gates
→ select an available tier
→ create a free public muster or join an existing one
→ snapshot the participating character's current build
→ leader assigns every participant to Rearguard, Vanguard, or Main Guard
→ participants may refresh their snapshots before commencement
→ leader optionally runs Battle Plan simulations
→ leader commences, or the muster auto-commences at its deadline if valid
→ server resolves Rearguard, Vanguard, and Main Guard preparation tasks
→ all participants regroup fully restored for the combined Final Assault
→ watch or skip the four sequential encounter playbacks
→ receive Repelled, Wounded, Broken, or Slain outcome
→ claim Trophies and guaranteed materials
→ spend boss-specific Trophies on vendor rewards and blueprints
→ improve elsewhere, change the roster/build/allocation, or attempt a higher tier
```

This loop is implemented primarily by `RaidService`, `RaidCombatResolver`, the Raid worker, JSON boss definitions, and the Angular raid-site and raid-run pages.

Evidence:

- `src/Infrastructure/Service/Services.LL/Raids/RaidService.cs`
- `src/Infrastructure/Service/Services.LL/Raids/RaidCombatResolver.cs`
- `src/API/API.LL/HostedServices/RaidResolutionWorker.cs`
- `src/API/API.LL/Data/raids/raid-bosses.json`
- `src/Presentation/ll/src/app/features/game/world/region/raids/raids.component.ts`
- `src/Presentation/ll/src/app/features/game/world/raid/raid-page.component.ts`

### Player Experience

The player first encounters a permanent boss attached to a region. Unlocking is personal where level is concerned and can be server-wide where World Tower progress is concerned. `TowerFloorProgress` is keyed by server rather than character, so the Sanguine Horror's floor requirement is a shared server gate. This is an important player-facing rule: a new eligible character may see a Raid unlock because other players advanced the Tower.

The player selects a tier. The Hive's Abyss currently provides Tiers I–III; the Sanguine Horror provides Tiers II–III. Maximum rosters are 9, 12, and 15 because each tier provides 3, 4, or 5 slots in each of three parties. Minimum rosters are 3, 6, and 9. The authored signup window is 24 hours.

Creating and joining are free. Creation immediately adds the leader, freezes the leader's character snapshot, and opens the public chat/muster state. Joining also freezes a snapshot. A character may refresh that snapshot during mustering, which is essential because otherwise equipment or Essence improvements made during the 24-hour window would not participate.

The leader assigns every signup to an exact slot in one of three parties. The exact numerical slot has no identified combat meaning; the party does. All participants must be assigned before commencement. The apparent Bench is therefore an editing state, not a substitute roster that can remain outside the battle.

The leader can run a ten-sample Battle Plan preview, rate-limited to 30 per hour. The preview predicts outcome and encounter readiness. At commencement, no player can alter the result: combat is automated and already determined by snapshots, definitions, and random seeds. Players can watch, skip, or replay the stored battles, but those are presentation choices.

After settlement, every participant has a claim. The result page exposes encounter outcome, damage, duration, surviving reinforcements, Guardian Break, Signature Disruption, final boss Health, participant damage ranking, and rewards. A player can then spend boss-specific Trophies at the vendor.

### Important Rules

- One character can be in only one active Raid.
- One account can occupy only one place in the same Raid.
- A character can lead only one active Raid.
- A boss can have at most 20 open musters.
- Joining is public and immediate; there is no approval or kick flow.
- Only the leader assigns parties, previews the Battle Plan, and commences.
- A valid muster auto-commences when its window expires; an invalid one cancels.
- Rearguard, Vanguard, and Main Guard must each be staffed and every participant assigned.
- Resolution order is hardcoded: Rearguard → Vanguard → Main Guard → Final Assault.
- A definition snapshot is stored at creation, but referenced creature, ability, item, and Essence definitions are resolved from current content at resolution.
- Rewards are claim-based and reduced after the character's first reward from the same boss in the ISO week.
- Creation and joining have no ticket, key, or currency cost.

Evidence: `RaidService`, `RaidModels.cs`, `RaidDefinitions.cs`, `RaidConfigurations.cs`, `RaidController.cs`, and `raid-bosses.json`.

## 3. What Raids Are Currently Trying to Be

### Asynchronous cooperative preparation

The strongest implemented pillar is asynchronous cooperation. Players do not need to be online together; snapshots and a long signup window let a roster form over time. The server resolves work in the background and stores playback. This pillar is strong and clearly different from ordinary personal combat.

### Three-party allocation puzzle

The leader must distribute player power across three preparation battles whose results are linked to a combined climax. No party can simply be ignored: Rearguard determines surviving adds, Vanguard determines boss defenses, and Main Guard determines signature-mechanic strength. Every participant then appears in the Final Assault. This is the system's most distinctive strategic idea. It is strong at the structural level, although exact slot positioning still has less depth than the UI complexity suggests.

### Automated build checks

The calibration profiles now express three direct pressures: Area performance for Rearguard, focused offense for Vanguard, and Sustain for Main Guard. The Final Assault uses the whole roster. The content also contains summons, attrition, physical/magical defenses, barriers, and overtime. The intent is that different characters and builds belong in different parties without excluding anyone from the boss.

This pillar is only partially realized. Players are shown recommended power and simulation readiness, but not enough encounter-specific explanation to understand which Essence, damage type, sustain tool, or targeting behavior should change.

### Boss mastery and tier progression

Players unlock higher tiers by Slaying the prior tier as participants, then farm boss-specific Trophies and blueprints. This creates a boss mastery ladder. It is present but shallow because tiers mainly increase health, offense, defenses, enemy counts, timers, and reward quantities rather than introducing clear new rules.

### Server prestige

The first Slain result can award a realm-first title to participating characters and create a global announcement. This provides a prestige moment. It is a milestone, not a complete competitive loop: there are no meaningful fastest-kill, personal-best, seasonal, or build-category goals in the current player experience.

## 4. Raids vs Normal Combat

| Dimension | Normal Combat | Raids |
| --- | --- | --- |
| Encounter duration | Repeating short encounters planned by the idle combat loop | Three preparation battles plus a separately budgeted combined boss battle, with playback transitions |
| Enemy mechanics | A normal creature group resolved by the combat engine | Authored adds, guardian, boss projection, boss variants, linked consequences, overtime |
| Build requirements | Usually one general-purpose loadout for an area | Explicit Area, focused-offense, and defensive-endurance specialization, followed by a combined roster check |
| Failure conditions | Party loses or cannot sustain an idle loop | Graded outcome based on boss Health; preparation parties can fall short while the Raid still earns partial rewards |
| Preparation | Select area, party, equipment, and active build | Form public roster, freeze snapshots, allocate three specialized parties, preview plan |
| Attrition | Relevant within each repeated fight | Especially relevant to Main Guard and under boss overtime |
| Target selection | Engine rules within one encounter | Targeting inside each encounter plus strategic selection of which player enters which preparation party |
| Scaling | Area/creature progression | Large multiplicative per-tier Raid scaling plus preparation-derived boss changes |
| Rewards | Repeated loot/experience from the activity | Graded Trophies, guaranteed materials, weekly reduction, boss vendor |
| Repeatability | Continuous idle repetition | Discrete 24-hour public attempts and post-settlement claims |
| Progress persistence | Character progression and ordinary drops | Character progression, tier unlocks, Trophies, vendor purchases; boss damage does not persist |
| Strategic decisions | Mostly build and activity selection | Build plus roster and party allocation; no decisions after commencement |

If Raid enemies had normal monster stats, Raids would still feel structurally distinct because of public mustering, frozen snapshots, three linked preparation parties, the combined Final Assault, graded outcome, Battle Plan preview, stored replays, and boss-specific reward progression. That is a significant success.

However, they might not remain compelling. Once the roster assignment is solved, each encounter is still a standard automated battle. The distinct wrapper currently carries more identity than some individual encounters. Future bosses need rules that change build valuation, not merely larger multipliers inside the same four-stage structure.

Evidence: compare `RaidCombatResolver.cs` and `RaidCombatScaling.cs` with `IdleCombatPlanner.cs` and the shared combat engine.

## 5. Strategic Depth and Build Diversity

### Pre-Raid Decisions

Meaningful decisions currently include:

- Whether to create a new muster or join one already likely to fill.
- Which tier to attempt.
- Which characters belong in Rearguard, Vanguard, or Main Guard.
- How to balance Area clearing, focused guardian damage, and defensive endurance before the whole roster fights the boss.
- Whether to refresh a snapshot after changing equipment or Essences.
- Whether the Battle Plan result justifies commencement or further preparation.

These decisions are real, but almost all authority belongs to the leader. A participant chooses to join and can refresh a build, yet cannot declare a preferred party, reserve a role, mark ready, approve the final assignment, or supply contextual build information beyond power rating.

### During-Raid Decisions

There are effectively **no gameplay decisions after commencement**. Players may watch, skip, or replay, but cannot affect combat. This is appropriate for the game's automated-combat identity; it should not be “fixed” with real-time interrupts or movement. The missing depth should be added before combat and in post-failure analysis.

### Post-Raid Decisions

The player can claim, review encounter performance, change build, try another allocation, farm the same tier, progress externally, attempt the next tier, or buy from the Trophy vendor. The system does not yet provide enough causal feedback to make the build-change decision precise.

### Build-system interaction

Essences and equipment are present in `CharacterSnapshot`, so their attributes and combat abilities affect resolution. Physical/magical specialization, Area damage, sustain, barriers, summons, penetration, conditions, and defensive investment can therefore matter through the normal combat engine.

No implemented Doctrine state was found in the Raid snapshot or combatant construction path. The supplied game-design context describes Doctrines as an intended rules-changing build system, but the current Raid implementation cannot be credited with Doctrine-driven encounter adaptation.

The likely player conclusion after a loss is still often “we need more power” rather than “I should replace these two Essences.” Recommended party power reinforces this numerical interpretation. The Battle Plan can reveal which encounter is weak, which is useful, but not why a particular build is weak.

Current position on the desired spectrum:

> **Closer to “assign power correctly, then gain more stats” than to “build differently for this boss,” but with enough underlying combat hooks to improve without replacing the architecture.**

Evidence: `CharacterSnapshotRepository.cs`, `ISnapshotCombatantBuilder`, `RaidPowerCalibration.cs`, `RaidCombatResolver.cs`, and the absence of Doctrine data in Raid snapshots.

## 6. Encounter and Boss Design

### Rearguard

Rearguard is the Area-damage preparation party. It fights ten consecutive waves drawn from the tier's authored add group under one shared encounter budget. Killing the last enemy in a wave immediately releases the next wave. The party remains in the same combat runtime throughout: Health, barriers, resources, statuses, threat, active effects, and cooldowns do not reset between waves.

Every add still alive when Rearguard falls or reaches its time limit transfers into the Final Assault at its remaining Health. Adds belonging to waves that Rearguard never reached also transfer at full Health. Clearing more waves therefore produces a directly visible and graduated consequence instead of a hidden generic penalty; clearing all ten prevents any Rearguard reinforcements from reaching the boss.

Categories:

- **Area/build check:** multi-target performance, sustain, and crowd control are valuable across ten waves.
- **Continuous attrition check:** recovery must come from the party's build because wave transitions provide no reset.
- **Targeting check:** killing dangerous or fragile adds efficiently matters through normal targeting behavior.
- **Linked mechanical check:** each survivor physically appears in the combined boss battle.

Rearguard therefore answers a simple player question: “How many reinforcements will the whole Raid need to handle later?”

### Vanguard

Vanguard is the focused-offense preparation party. It attacks a durable guardian, accompanied by optional escorts, that maintains the boss's protection. Health removed and barrier absorbed contribute to Guardian Break. Partial Guardian Break proportionally reduces the final boss's Armor, Resistance, and Damage Reduction up to the authored cap. Defeating the guardian completes the task even if an escort remains.

Categories:

- **Single-target build check:** focused damage, penetration, and burst are valuable.
- **Damage/attrition check:** the party must remain active long enough to break a durable target.
- **Linked mechanical check:** partial progress changes the boss even without clearing every hostile.

Vanguard answers: “How much of the boss's protection will remain in the Final Assault?”

### Main Guard

Main Guard is the defensive-endurance preparation party. It confronts a highly durable projection of the actual boss for 60 seconds. Surviving to 33%, 67%, and 100% of that duration reaches three discrete Signature Disruption thresholds; destroying the projection also grants full disruption. Full success is therefore not restricted to passive stalling, but defensive builds have the clearest advantage.

Each threshold weakens the final boss. At full disruption, authored values currently reduce boss Power by up to 30% and apply up to a 25% penalty to its ability cadence. These modifiers are the current automated-combat representation of weakening the boss's signature mechanic.

Categories:

- **Defensive build check:** healing, barriers, mitigation, regeneration, and defensive utility are valuable.
- **Escalating endurance check:** progress depends on time survived rather than damage ranking alone.
- **Linked mechanical check:** discrete thresholds make partial survival legible in the Final Assault.

Main Guard answers: “How dangerous and frequent will the boss's signature pressure be?”

### Final Assault

All three parties enter the Final Assault fully restored from their saved snapshots. No preparation deaths, missing Health, or cooldown state carry forward. The combined friendly roster fights the boss plus every surviving Rearguard add. The boss's defenses reflect Vanguard's Guardian Break, and its Power and ability cadence reflect Main Guard's Signature Disruption. After the authored overtime start, the boss gains repeated Power stacks every 300 ticks.

Categories:

- **Combined roster check:** every participant reaches the actual boss.
- **Stat and attrition check:** the roster must defeat the boss before overtime overwhelms it.
- **Linked mechanical check:** all three preparation results remain visible and consequential.

This structure resolves the previous excitement hierarchy in which only one party fought the real boss. Preparation establishes specialization; the Final Assault provides the shared climax.

### Approved terminology

The permanent assignment names are **Rearguard**, **Vanguard**, and **Main Guard**. These names intentionally describe troop positions rather than raw game statistics:

- **Rearguard** secures the Raid against reinforcements. It was preferred over “Suppression,” which could be misread as suppressing the boss or applying debuffs.
- **Vanguard** advances first and breaks the guardian. “Spearhead” was a strong alternative, but Vanguard forms a clearer military pair with Rearguard.
- **Main Guard** holds against the boss's projected power. It was preferred over Bulwark, Defiance, Aegis, Shieldwall, and Battleline to complete the same army-structure vocabulary.

Raid-specific narrative objective names should sit beneath these permanent roles. For example, Rearguard might “Seal the Brood Tunnels,” Vanguard might “Slay the Obsidian Warden,” and Main Guard might “Withstand the Queen's Wrath.” This keeps assignments readable while allowing future bosses to vary their fiction.

### The Hive's Abyss

The default Ant Queen has the clearer Raid identity. She summons brood based on living enemies, deals Area magic damage, consumes a summon to heal 4% maximum Health, and gains Damage Reduction and Attack Speed for living brood. This creates a credible Swarm/Area pressure.

An 8% Ant King variant uses a substantially different physical, lifesteal, and burst profile. This is mechanically interesting but poorly forecast. With only ten preview samples, there is roughly a 43% chance of seeing at least one 8% variant and therefore roughly a 57% chance of seeing none. A roster can receive a confident-looking preview that never sampled the actual variant.

### Sanguine Horror

The Sanguine Horror uses the Corpse Golem and reused undead abilities. It has working attrition and condition pressure but less unique Raid identity than the Ant Queen. Its distinctiveness comes more from composition and scaling than a memorable rule players can prepare around.

### Overall encounter judgment

The system has an appropriate amount of automation-compatible complexity. It is not overloaded with MMO reaction mechanics. The main problem is underuse: the architecture can express adds, objectives, variants, barriers, conditions, summons, overtime, and preparation consequences, but current tiers rarely transform how players build.

## 7. Idle-Game Compatibility

Raids are highly compatible with an idle browser RPG.

- Manual attention is concentrated into discovery, joining, snapshot refresh, assignment, preview, and claim.
- Players do not need to be co-present.
- Combat resolves from saved state and survives disconnects/restarts.
- Offline participants still receive their result and claim.
- Playback is optional and replayable.
- There are no real-time dodge, movement, or interrupt requirements.

The implemented cadence closely matches the desired model:

```text
Preparation → automation → result → analysis → adaptation
```

The weak link is analysis. Waiting is operationally meaningful because it allows public recruitment, but a fixed 24-hour window can feel excessive for a roster that fills and becomes ready quickly. The leader can commence early, mitigating this. Settlement also waits for the authored visual playback duration even if nobody watches; this can delay claims by roughly 22–25 minutes at current maximum budgets and transitions. Playback should remain available without holding settlement hostage.

The Raid worker polls every five seconds, handles a bounded batch, and uses leases. These are good technical choices for idle reliability. They become design-relevant only where processing/visual duration delays settlement.

## 8. Failure and Learning

### What the player loses

After removal of the entry-resource economy, an attempt no longer consumes a leader-only key. This materially improves experimentation. The costs are now time, roster coordination, the one-active-Raid lock, and the weekly reward consequence if the player claims a weak result first.

### What partial progress does

Boss damage does not persist into another Raid. Partial performance matters only inside the current resolution and its graded outcome. Repelled, Wounded, and Broken provide different Trophy/material rewards, so an attempt need not be all-or-nothing.

### What the player learns

The report exposes:

- Boss Health remaining.
- Rearguard reinforcement remainder.
- Vanguard Guardian Break.
- Main Guard Signature Disruption.
- Preparation-party damage and duration.
- Individual damage, contribution rank, and payout.
- Full encounter replays with damage/healing/barrier totals by entity and ability.

It does not provide a concise diagnosis of:

- Death cause, killing ability, or damage type.
- Effective healing versus overhealing.
- Mitigation and preventable damage by source.
- Condition uptime and cleanse value.
- Taunt/threat or target-pressure outcomes.
- The tick at which each participant died, despite `DeathTick` existing in the domain model.
- Which boss ability or rule caused the encounter to fall behind.
- A comparison with previous attempts or the preview.

The replay data contains more detail than the result summary uses, but requiring players to manually interpret a long playback is not a sufficient diagnostic system.

Natural response after failure: usually **“this party needs higher numbers”**, sometimes **“rebalance Rearguard/Vanguard/Main Guard,”** and not reliably **“I know which build rule to change.”** Removing the entry cost makes testing safer, but information still limits learning.

## 9. Progression

### External Progression

External progression is the dominant source of increased Raid success:

- Character level and attributes.
- Equipment quality and physical/magical specialization.
- Essence collection, upgrades, and active loadout.
- Region progression.
- Shared World Tower progression for authored unlocks.

This is appropriate. Raids should give the broader build ecosystem reasons to matter rather than replace it with a separate character sheet.

### Raid-Specific Progression

Implemented Raid progression includes:

- Boss and regional unlocks.
- Prior-tier Slain requirement before leading the next tier.
- Boss-specific Trophies.
- Vendor purchase limits and tier requirements.
- Raid-exclusive materials and blueprints.
- Realm-first title/announcement.
- Informal mastery of party allocation and boss behavior.

The vendor's Raidforged/Gravebound blueprint families provide a 25% bonus stat budget. This makes Raid progression relevant to crafting, which is a good systemic connection. It also risks making repeated Raid farming mandatory for competitive equipment if the bonus is broadly best-in-slot rather than encounter-specialized.

A Raid talent tree, Raid level, Raid stats, or Raid-only equipment layer is not needed. The system already has Trophies, tier mastery, crafting rewards, Essences, equipment, and character growth. More progression layers would add maintenance cost without fixing the core encounter problem.

## 10. Replayability

Reasons to repeat currently include:

- Farm Trophies and guaranteed materials.
- Reach weekly vendor purchase goals.
- Unlock and beat higher tiers.
- Obtain Raid blueprint families.
- Help other public groups.
- Improve contribution rank.
- Encounter the rare boss variant.
- Earn or help achieve realm first before it is claimed.

### Healthy Replayability

- Higher-tier unlock and mastery.
- Experimenting with party allocation and build specialization.
- Boss-specific crafting progression.
- Helping asynchronous public groups.
- Improving an initially weak outcome to Slain.

### Mechanical Repetition

- Farming the same solved tier for Trophies/materials.
- Repeating tiers that change only multipliers, counts, and timers.
- Running attempts after the weekly best reward is established primarily for 25% repeat returns.
- Waiting through the same fixed muster and playback cadence without a new goal.

There are no personal-best records, fastest clear goals exposed as a durable player loop, rotating modifiers, seasonal challenges, alternate objectives, or announced variant mastery. Those omissions are not all defects—feature restraint is good—but the current long-term loop leans too heavily on mandatory reward farming.

## 11. Rewards

### Current rewards

- Boss-specific Raid Trophies.
- Guaranteed Soul Dust and Monster Cores, scaled by tier and outcome.
- Boss-vendor materials and blueprint rewards.
- Realm-first title for the first Slain participants.
- Global first-kill announcement naming the leader.

Raids have no dedicated entry resource or organizer fee.

### Scaling and frequency

Authored Trophy and guaranteed-item values depend on tier and outcome. Every approved participant now receives the same outcome package. The first result establishes the weekly entitlement; later packages grant any positive Trophy or per-item difference, while results that do not improve the entitlement pay 25%.

This makes the weekly result order-independent: a Repelled or lower-tier attempt no longer consumes the value of a later stronger result. Upgrade deltas are calculated from awarded entitlement claims rather than claim timing, so leaving an earlier reward unclaimed provides no advantage.

### Contribution

Contribution score and rank remain damage-only informational statistics. Healing, barriers, mitigation, debuffs, control, threat management, and other support are not represented in that ranking, so it should not be read as a complete measure of contribution.

Damage ranking no longer changes rewards. Equal outcome rewards preserve support and organizational value unless the game can eventually measure role-neutral contribution credibly.

### Reward identity

Trophies and boss-specific blueprints belong in Raids because they express mastery of a named boss and feed crafting. Generic Soul Dust and Monster Cores are useful but not inherently Raid-shaped; they are acceptable supporting rewards, not a strong identity.

Realm-first recognition belongs in Raids. The title going to all first-clear participants is appropriately cooperative. Naming only the leader in the announcement slightly overcredits the organizer relative to the roster.

## 12. Scaling

Raid scaling multiplies creature Max Health, Power, Armor, Resistance, penetration, and regeneration. Current authored values reach approximately 118× Health and 63× offense for the highest variant. Preparation consequence caps are also authored: Vanguard can remove roughly 50–62% of boss defenses/reduction, while full Main Guard disruption currently reduces boss Power by 30% and penalizes ability cadence by 25%. Rearguard consequences are concrete surviving adds rather than an additional scalar boss-Power penalty.

This creates significant tuning leverage but not proportionate new gameplay. The danger is multiplicative interaction among:

- Long-term character power inflation across ten regions.
- Large boss base multipliers.
- Rearguard add survival.
- Vanguard defensive removal.
- Main Guard signature weakening.
- Overtime stacks.
- Variant scaling.
- Roster size differences.

There is no participant-count scaling. More eligible players are therefore always beneficial, and a full roster can overpower composition questions. This is not automatically wrong—public cooperation should be rewarded—but it means roster availability can dominate build mastery and small servers can face a fundamentally different difficulty.

The stored tier JSON pins major authored values at creation. Referenced creature, ability, Essence, equipment, and other content definitions are not fully embedded, so a deploy during a 24-hour muster can change eventual combat despite the definition hash. Long-term content operations need either complete dependency versioning or an explicit policy that active musters use current combat content.

The architecture can survive ten regions technically, but current content scaling cannot remain mostly multiplicative without creating unreadable balance cliffs.

## 13. Multiplayer / Shared Participation

### How it works

Multiplayer is asynchronous and public. Any eligible player can join an open muster immediately. One account gets one slot, one character can be active in one Raid, and the leader assigns the whole roster.

Player count matters directly because bosses do not scale to the roster. Late joining is possible until muster closes or the roster is full. Strong players can carry weaker players, while the 70% payout floor ensures weaker players still receive material rewards.

### What multiplayer adds

The system would lose something important if every Raid were solo:

- The leader's distribution of diverse characters across the three linked parties.
- Public server cooperation and helping behavior.
- The social anticipation of a long-form shared result.
- Realm-first group prestige.
- A use for specialized builds that may be inefficient in personal idle farming.

Multiplayer therefore justifies its existence. It is not merely a technical wrapper around a solo boss.

### Current weaknesses

- Public immediate joining has no leader approval or removal.
- A weak, stale, or intentionally bad participant can occupy a scarce slot.
- The leader cannot replace a signup even before commencement.
- Participants cannot communicate preferred party or readiness through structured state.
- Only the leader previews, assigns, and commences.
- Strong carries may make weak members' choices irrelevant.
- Damage-only contribution discourages support specialization.
- A participant can refresh to an unexpected build after the leader planned around the earlier snapshot.

Carrying is desirable in moderation because it supports public cooperation and later-player inclusion. It becomes undesirable when higher tiers are routinely solved by a few players while the rest of the roster has no meaningful preparation obligation.

## 14. Social and Competitive Design

Implemented social/competitive elements include public open musters, Raid chat during active states, contribution ranking, a realm-first title, and global Slain announcements.

These create a visible cooperative event, but not a deep competitive ecosystem. Contribution rank is damage-biased; realm first is one-time; and chat closes on settlement. There is no durable personal best, fastest preparation, strongest Guardian Break, longest Main Guard stand, lowest-reinforcement clear, or seasonal objective.

Potential unhealthy incentives:

- Join rather than organize is no longer economically favored because creation is free—a resolved issue.
- Players may avoid helping a lower tier before their desired high-tier weekly reward.
- Damage-focused players may avoid support even when support raises clear probability.
- A leader may over-stack Vanguard to place favored players high on visible damage ranks.
- First-come signup can reward constant checking and allow slot squatting.
- The rare unannounced variant can make social blame fall on the leader for an outcome the preview did not sample.

There is no persistent shared boss Health, so kill stealing, last-hit sniping, or waiting for others to weaken the boss do not apply.

## 15. Technical Rules Affecting Game Design

### Snapshot locking

This technical rule functions as a design rule because the build used in combat is the frozen snapshot, not necessarily the character's live build. Snapshot refresh makes preparation explicit but also requires participants to remember to update.

### One active Raid per character

This functions as a design rule because joining a 24-hour muster prevents participation elsewhere. It raises the importance of roster quality, leader reliability, and the ability to leave before commencement.

### One account slot per Raid

This functions as a design rule because it blocks alt-stacking and preserves multiplayer breadth. It is a strong rule worth preserving.

### Hardcoded encounter order

This functions as a design rule because every Raid currently expresses causality as three specialized preparations followed by one combined Final Assault. It gives the mode identity but limits alternative encounter structures.

### Worker polling and leases

This functions as a design rule because resolution is eventually consistent rather than immediate. Five-second polling is acceptable; lease safety matters more than instant response in an idle game.

### Playback-gated settlement

This functions as a design rule because rewards/chat settlement wait for authored playback duration even if nobody watches. Presentation time becomes reward latency.

### Definition snapshot boundary

This functions as a design rule because tier values are pinned but their dependencies are not fully pinned. A content deployment can alter a mustering Raid.

### Free entry plus open-muster cap

This functions as a design rule because anti-spam is now enforced through participation/leadership locks and a 20-muster boss cap rather than currency scarcity. This is fairer, more legible, and better aligned with experimentation.

## 16. Exploits and Degenerate Strategies

### Technical exploits or edge cases

- **Definition drift:** deploy changed creature/ability content after creation but before resolution.
- **Preview sampling blind spot:** ten simulations can miss an 8% variant most of the time.
- **Stale snapshot slot:** a participant can occupy a slot with an outdated build.
- **Late snapshot change:** a participant may refresh after the leader used a prior preview unless planning state is clearly invalidated.
- **Playback/settlement coupling:** maximum-duration playback delays claim even with no viewer.

### Game-theory exploits

- **Weekly claim ordering:** do the highest desired tier first; avoid helping lower tiers until the full reward is secured.
- **Damage padding:** optimize visible damage/payout rather than group success or support.
- **Minimum viable participation:** occupy a slot, contribute almost nothing, receive the 70% floor and at least one item.
- **Full-roster dominance:** fill every slot because there is no participant scaling, even when composition is poor.
- **Preparation threshold gaming:** once a party reliably reaches its consequence cap, nuanced allocation may collapse into moving every additional strong build elsewhere.
- **Public slot griefing:** join immediately with a weak/stale build, leaving the leader unable to remove or replace the signup.
- **Preview fishing:** repeatedly simulate within the generous rate limit until a favorable result appears, even though the final seed remains uncertain.

The former circular entry-cost and leader public-good problem has been removed and is no longer a current exploit.

## 17. Player Information and Feedback

The UI communicates tier, roster size, party allocation, signup deadline, recommended party power, Battle Plan readiness, broad outcome probabilities, surviving reinforcements, Guardian Break, Signature Disruption, boss Health, damage rank, rewards, and replays.

It does not adequately communicate before combat:

- Boss abilities and damage types in actionable terms.
- The possible boss variant and its probability.
- What each variant changes.
- Overtime start and stacking rate.
- Exact Vanguard and Main Guard maximum modifiers.
- What guardian barrier absorption contributes to Guardian Break.
- Which build properties make a character suitable for each party.
- Whether the shown snapshot matches the participant's intended/current build.

It does not adequately summarize after combat:

- Why each character died.
- Damage taken by type/source.
- Healing, barrier, mitigation, and cleanse value at participant level.
- Condition or debuff uptime.
- Time-to-first-death and collapse sequence.
- Comparison against preview or previous best.
- One or two actionable recommendations grounded in observed events.

The raw playback bundle already tracks entity and ability totals for damage, healing, barrier, damage taken, regeneration, and blocked damage. The next step should be a diagnostic summary derived from existing data, not an entirely new telemetry system.

## 18. Long-Term Content Scalability

The architecture currently supports:

- Data-driven boss/tier scaling.
- Authored Rearguard add templates repeated through ten continuous waves, Vanguard guardian/escorts, and Main Guard projection.
- Probabilistic creature groups and boss variants.
- Summons through normal abilities.
- Conditions, barriers, buffs, debuffs, and damage types through the combat engine.
- Overtime Power stacks.
- Different tick budgets and modifier caps.
- Boss-specific rewards and vendors.
- Stored definitions and replay artifacts.

It does not cleanly support as authored Raid rules:

- Multiple boss phases with explicit phase transitions.
- Encounter rules reacting to player behavior or damage composition.
- Per-boss changes to preparation consequence formulas.
- Different encounter order or optional preparations.
- Alternative victory conditions.
- Boss-specific target-priority rules.
- Cross-encounter events during resolution rather than only consequence handoff.
- Tier mechanics that add rules instead of numbers.

Can Raid #20 feel fundamentally different from Raid #1? **Not reliably with the current fixed formulas alone.** Creature abilities can produce variation, but every Raid remains “clear adds, damage objective, fight modified boss.”

The recommended architectural direction is a small composable encounter-rule library: data-driven hooks for preparation start, enemy death, guardian progress, endurance threshold, Final Assault start, overtime, and consequence handoff. It should extend the existing three-party-plus-climax identity, not replace it with a generic MMO scripting platform.

## 19. Design Scorecard

| Category | Score | Reason |
| --- | ---: | --- |
| Core gameplay loop | 8/10 | Three specialized preparations now lead into a shared climax; free entry removed its worst friction. |
| Strategic depth | 6/10 | Allocation has three clear axes, but most diagnosis and optimization remain power-centric. |
| Build diversity | 6/10 | Area, focused offense, and defensive endurance are explicit, though encounter diagnostics remain weak. |
| Boss design | 4/10 | Hive has identity; Sanguine and higher tiers rely heavily on scaling/reuse. |
| Idle-game compatibility | 8/10 | Excellent asynchronous snapshots, automation, offline safety, and optional playback. |
| Failure design | 4/10 | Free retries help, but weekly reward behavior and weak diagnostics still discourage learning. |
| Progression | 5/10 | Tier/vendor/crafting loop is useful without extra bloat, but tiers add little mechanical mastery. |
| Replayability | 5/10 | Preparation consequences add variation, but farming and higher stats still dominate once allocation is solved. |
| Reward structure | 4/10 | Trophies/blueprints fit; weekly ordering and damage-only payout are unhealthy. |
| Multiplayer design | 4/10 | Multiplayer adds real allocation value, but control, readiness, and fairness are incomplete. |
| Player feedback | 5/10 | Strong high-level result and replay; insufficient causal/build feedback. |
| Long-term scalability | 6/10 | Robust content pipeline and a stronger four-encounter grammar, but consequences remain fixed. |
| Technical robustness | 8/10 | Durable state, transactions, locks, leases, snapshots, playback, and claims are strong. |

### Overall Raid Design Score

**7 / 10**

This is not a mechanical average. The system earns a higher overall judgment than several content categories because its asynchronous preparation-and-reunion structure is valuable and difficult to replace. It remains short of exceptional because the player-facing strategy and learning loop have not caught up with the engineering foundation.

## 20. Biggest Problems

### 1. Weekly rewards punish natural play order

**Problem:** The first reward from a boss spends the weekly full-reward state regardless of outcome or tier.

**Why It Matters:** Players are encouraged to avoid lower-tier help, delay claims, and fear experimentation before their preferred tier.

**Evidence:** Weekly claim lookup and reward multiplier logic in `RaidService.cs`; `RaidRewardClaim.WeekKey` in `RaidModels.cs`.

**Severity:** Critical

**Recommended Direction:** Spend the full weekly reward only on Slain, or track the highest tier/outcome rewarded and grant the difference when improved.

**Implementation Status (2026-08-20):** Resolved with per-component weekly entitlement upgrades and 25% non-upgrade repeat rewards.

### 2. Damage-only contribution misprices support

**Problem:** Contribution score and payout use damage while ignoring healing, barriers, mitigation, debuffs, and control.

**Why It Matters:** Players are financially pushed toward personal damage even when support wins the Raid. The ranking communicates a false definition of contribution.

**Evidence:** Participant aggregation and payout calculation in `RaidService.cs` and `RaidCombatResolver.cs`; `RaidParticipantResult`.

**Severity:** Major

**Recommended Direction:** Prefer equal outcome rewards with cosmetic/statistical contribution reporting. Add role-neutral contribution only if it can be validated across combat archetypes.

**Implementation Status (2026-08-20):** Resolved. Damage remains informational and no longer scales payout.

### 3. Failure does not explain build changes

**Problem:** Results say which encounter failed but rarely why.

**Why It Matters:** The adaptation loop becomes “gain power” instead of “change Essences/equipment strategy.”

**Evidence:** Result DTO/UI versus richer playback totals in `RaidDtos.cs` and `RaidPlaybackBundleBuilder.cs`; unused `DeathTick`.

**Severity:** Major

**Recommended Direction:** Produce encounter diagnostics for deaths, damage types, ability pressure, sustain shortfall, add survival, objective timing, and overtime.

### 4. Tiers add numbers more often than mechanics

**Problem:** Higher tiers primarily increase multipliers, counts, caps, and rewards.

**Why It Matters:** Long-term mastery is gear progression rather than encounter learning; ten regions will become repetitive and difficult to balance.

**Evidence:** Repeated tier structures in `raid-bosses.json` and multiplicative `RaidCombatScaling.cs`.

**Severity:** Major

**Recommended Direction:** Give each higher tier one clearly announced additional rule or changed build pressure.

### 5. Public rosters lack basic control and readiness

**Problem:** Joining is immediate, and leaders cannot approve/remove members. Participants cannot mark ready or declare a preferred party.

**Why It Matters:** Scarce slots can be occupied by weak, stale, or griefing signups. Planning authority becomes unilateral and socially awkward.

**Evidence:** Join/leave/assign endpoints in `RaidController.cs` and `RaidService.cs`; no leader removal command or readiness field in `RaidSignup`.

**Severity:** Major

**Recommended Direction:** Add leader acceptance/removal during mustering, participant preferred party, snapshot-ready state, and clear invalidation when refreshed.

### 6. Encounter identity is uneven

**Problem:** The Hive expresses a Swarm mechanic, while the Sanguine Horror feels closer to reused undead combat with Raid scaling.

**Why It Matters:** Bosses risk feeling like themed stat blocks rather than build puzzles.

**Evidence:** Creature/ability references in `raid-bosses.json`, `abilities.json`, and `creature-abilities.json`.

**Severity:** Moderate

**Recommended Direction:** Define one build-changing mechanical sentence for every boss before authoring its numbers.

### 7. Rare variants undermine forecasts

**Problem:** An 8% variant can be absent from all ten Battle Plan samples and has a different combat profile.

**Why It Matters:** A preview can appear reliable while omitting the actual encounter, turning preparation into uncommunicated variance.

**Evidence:** Hive variant chance in `raid-bosses.json`; ten-sample constant in `RaidService.cs`.

**Severity:** Moderate

**Recommended Direction:** Announce variants and preview each separately, or make variants cosmetic/minor until the system can forecast them explicitly.

### 8. Playback delays settlement

**Problem:** Settlement and chat closure are tied to total authored playback duration.

**Why It Matters:** Players who do not watch still wait to claim, and presentation becomes backend progression latency.

**Evidence:** playback start/end calculation and finalization in `RaidService.cs`.

**Severity:** Moderate

**Recommended Direction:** Settle as soon as resolution is durable; keep playback independently available.

### 9. Fixed preparation grammar may constrain future content

**Problem:** Every Raid uses identical preparation order and consequence types.

**Why It Matters:** Future bosses may differ only cosmetically unless more rule composition is possible.

**Evidence:** hardcoded resolver sequence and fixed `RaidFlankDefinition`/`RaidWardDefinition`.

**Severity:** Moderate

**Recommended Direction:** Preserve three preparation parties and the combined Final Assault, but make their objectives and consequences composable per encounter.

## 21. What Should Be Preserved

### The asynchronous snapshot model

It respects the idle game, different schedules, offline participation, and server reliability. Future changes should not require synchronous attendance.

### Three linked parties

Rearguard, Vanguard, and Main Guard changing the Final Assault is the feature's clearest identity. Future design should deepen those consequences without excluding any party from the boss.

### Free creation and direct anti-abuse limits

Removing the entry-resource economy makes leadership fair and experimentation possible. Do not reintroduce a leader-only consumable under another name.

### Early commencement plus automatic deadline behavior

Leaders can proceed when ready, while offline leaders cannot strand a valid group indefinitely. This is good idle-system hygiene.

### Frozen definitions and character snapshots

They make results reproducible and protect long-running attempts. Dependency pinning can improve, but the principle should remain.

### Battle Plan previews

Forecasting is exactly the right kind of idle-game interaction. It should become more explanatory and variant-aware, not be removed.

### Stored per-encounter playback

Playback provides spectacle, auditability, and a foundation for diagnostics without synchronous combat. It should become optional with respect to settlement.

### Graded outcomes

Repelled/Wounded/Broken/Slain makes partial progress meaningful and supports learning better than binary success—provided the weekly reward rule stops punishing partial attempts.

### Boss Trophies feeding crafting

Boss-specific rewards and blueprints give Raids an identity while reinforcing an existing system. Avoid replacing this with Raid gear or multiple new currencies.

### Robust worker/state architecture

Locks, leases, uniqueness constraints, transactions, outbox events, and durable claims are a strong base. Design iteration should build on it rather than rewrite it.

## 22. Recommended Raid Identity

> **Raids are asynchronous shared build-puzzle encounters where three specialized preparation parties create visible consequences, then reunite for one combined automated boss encounter. Players use clear forecasts and combat diagnostics to adapt their builds and master boss-specific mechanics across inexpensive repeated attempts.**

### Design Pillars

**1. Build Adaptation**  
Every boss should contain at least one rule that materially changes the relative value of Essences, damage types, sustain, targeting, or defenses.

**2. Linked Preparation Consequences**  
Every preparation party should change the Final Assault in visible, encounter-specific ways. Partial performance must matter.

**3. Preparation Over Reflexes**  
Difficulty should be solved through build selection, assignment, forecasting, and automated interactions—not real-time reactions.

**4. Learnable Failure**  
Every failed attempt should identify at least one actionable roster, party, or build change. Experiments should be cheap.

**5. Fair Shared Participation**  
Organizing, supporting, and joining should all be socially and economically reasonable. Rewards should not privilege damage padding or punish helping.

## 23. Recommended Changes

### Tier 1 — Fix Before Expanding

1. Change weekly rewards to preserve upgrade value: a weak/lower-tier result must not consume the full reward opportunity for a later Slain/higher-tier result.
2. Replace damage-scaled payout with equal outcome rewards, retaining contribution as informational until role-neutral scoring is proven.
3. Add leader approval/removal during mustering and participant readiness/preferred-party state.
4. Expose boss abilities, damage profile, variants, overtime, Rearguard composition, Vanguard cap, Main Guard thresholds, and objective behavior before signup/commencement.
5. Add failure diagnostics using existing playback totals: death source/tick, damage types, sustain/mitigation, add survival, objective timing, and overtime state.
6. Make Raid settlement independent of whether the authored playback duration has elapsed.
7. Define whether higher-tier carrying is intentional; tune roster gates and rewards accordingly.
8. Add invariant tests for weekly reward ordering, support-friendly reward equality, free creation, and variant-aware forecasting.

### Tier 2 — Improve Current Raids

1. Give each existing boss one unique, clearly explained automated-combat rule.
2. Give each higher tier one added or transformed mechanic rather than only scaling.
3. Show participant build summary, preferred party, snapshot age, and ready state to the leader.
4. Preview announced variants separately and let the leader compare plans.
5. Add personal bests such as lowest reinforcement, highest Guardian Break, longest Main Guard stand, fastest Slain, and first Slain per tier.
6. Keep chat/results accessible for a limited post-settlement period.
7. Explain how Area, focused-offense, and Sustain profiles map to actual build properties rather than presenting only recommended power.

### Tier 3 — Future Expansion

1. Add a small composable Raid-rule library for phases, thresholds, objective types, and preparation consequence handoffs.
2. Add optional rotating challenge modifiers only after core bosses have strong identities.
3. Add a solo practice simulation that gives diagnostics but no progression rewards.

Do not add a Raid talent tree, Raid gear tier, Raid-only combat stat, profession, or additional currency at this stage.

## 24. Three Example Raid Encounter Designs

### Encounter 1 — Swarm Control

**Encounter Identity**  
The group must prevent a growing enemy population from empowering and healing the boss.

**Core Mechanic**  
Every surviving Rearguard add enters the Final Assault. While any add lives, the boss gains stacking Damage Reduction and periodically consumes one to heal.

**Secondary Mechanics**

- Rearguard adds reproduce or summon at authored intervals if not killed quickly.
- Vanguard guardian periodically shields surviving adds unless sufficiently damaged.
- Main Guard thresholds reduce the boss's consume frequency in the Final Assault.
- Final Assault overtime accelerates while adds remain alive.

**Build Pressure**  
Area damage, controlled multi-target output, anti-summon effects, and sustained damage become stronger. Pure single-target burst may excel on the boss but leave the group overwhelmed.

**Failure Pattern**  
An underprepared group allows the Rearguard population to survive, enters the Final Assault against too many enemies, then cannot overcome boss healing/mitigation before overtime.

**Idle Compatibility**  
All decisions are made through loadouts and party assignment. The automated engine handles summon timing and targeting; diagnostics report population at transfer and healing caused by survivors.

### Encounter 2 — Retaliation and Attrition

**Encounter Identity**  
Enemies punish uncontrolled burst and prolonged condition exposure rather than simply demanding maximum damage.

**Core Mechanic**  
Rearguard enemies explode on death based on their accumulated offensive conditions, while the Final Assault boss converts uncleansed conditions on allies into periodic Area damage.

**Secondary Mechanics**

- Vanguard guardian regenerates rapidly unless healing reduction is applied.
- Main Guard projection applies escalating conditions that reward cleansing and resistance.
- Vanguard escorts apply stacking physical and magical vulnerabilities.
- Boss overtime increases condition application rather than raw Power.

**Build Pressure**  
Cleanse, resistance, barriers, controlled kill timing, healing reduction, and mitigation become valuable. Glass-cannon damage and condition spam without protection become weaker.

**Failure Pattern**  
Rearguard kills itself through chained death bursts, Vanguard cannot suppress regeneration, or Main Guard and the Final Assault collapse under accumulated vulnerabilities.

**Idle Compatibility**  
No manual dispels or reaction windows are required. Players prepare cleanse/mitigation/healing-reduction tools, then study automated condition uptime and explosion damage after the attempt.

### Encounter 3 — Adaptive Defense

**Encounter Identity**  
The Raid must diversify damage types and place specialists intelligently because the boss adapts to dominant damage.

**Core Mechanic**  
The boss gains resistance to the damage type that dominated each preparation encounter and exposes a different weakness. Rearguard, Vanguard, and Main Guard therefore shape the Final Assault's optimal composition.

**Secondary Mechanics**

- Rearguard enemy groups have asymmetric Armor/Resistance profiles.
- Vanguard guardian alternates physical and magical barriers on a predictable schedule.
- Main Guard projection alternates physical and magical pressure at its survival thresholds.
- Final Assault overtime locks in the current adaptation and amplifies it.

**Build Pressure**  
Mixed physical/magical rosters, penetration specialists, and damage-type-aware Essence loadouts become stronger. A universally stacked damage type becomes risky.

**Failure Pattern**  
The roster solves its preparations with one damage type, then faces a boss heavily resistant to the same Final Assault composition.

**Idle Compatibility**  
Adaptation rules are deterministic and forecastable. The Battle Plan shows expected damage composition and the resulting boss resistance before commencement.

## 25. Preferred Long-Term Direction

The system should remain asynchronous, public, and multiplayer. It should become a shared build puzzle rather than a stat-scaled automated spectacle.

1. **What the player should primarily solve:** which builds and characters belong in each preparation party, and how boss-specific rules alter normal build value.
2. **What should create difficulty:** interacting mechanics, damage/sustain profiles, target pressure, deterministic overtime, and linked preparation consequences—not entry scarcity or extreme Health multipliers alone.
3. **How repeated attempts should work:** attempts remain free; previews establish hypotheses; results explain failures; players adjust snapshots/allocation and try again without spending the week's best reward opportunity.
4. **What progression should matter:** Essences, equipment, character development, tier mastery, boss knowledge, and crafting rewards. No separate Raid progression tree is needed.
5. **What rewards should accomplish:** recognize boss mastery, feed existing crafting/build systems, and motivate continued participation without making damage padding or calendar ordering optimal.
6. **How multiplayer should work:** public independent teams, one character/account slot, leader formation with approval/removal, participant preference/readiness, shared outcome rewards, and optional carrying within explicit limits.
7. **How idle compatibility should remain:** all meaningful choices occur before combat or during post-result adaptation. Resolution stays automated, durable, offline-safe, and replayable.

The strongest future Raid is not the one with the most phases or currencies. It is the one where a player reads one clear encounter rule, changes two or three build decisions, sees those decisions play out automatically, and understands the result.

## 26. Final Verdict

The current Raid system is a **promising, technically strong foundation with a moderately weak content and incentive layer**.

It is already structurally distinct from normal combat. The three-party preparation model, combined Final Assault, public asynchronous roster, frozen snapshots, forecast simulations, graded outcomes, and stored playback justify maintaining Raids as a standalone mode. Free creation and joining provide the correct economy foundation: leadership is fair, there is no circular entry loop, and failure can support experimentation.

The system is not yet strategically expressive enough for long-term expansion across ten regions. Current players can make meaningful allocation decisions, but insufficient encounter explanation and failure diagnostics push them back toward power ratings and general-purpose damage. Damage-only contribution, weekly reward ordering, limited roster control, and mostly numerical tier scaling create unhealthy behavior around an otherwise sound loop.

The recommendation is to **keep the asynchronous three-party preparation and combined-boss architecture and stop adding bosses temporarily**. First calibrate the newly implemented Main Guard and Final Assault, then fix weekly rewards, contribution fairness, roster readiness/control, variant forecasting, and failure diagnostics. Next revise the existing boss identities so each higher tier teaches one new rule. Once those changes are proven, the architecture is worth building upon.
