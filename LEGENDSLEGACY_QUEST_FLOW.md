# LegendsLegacy — Quest Flow Guide

Updated 3 September 2026. Covers the current catalog of **29 regular quests and one expired example event quest**. Equipment drops, starter grants and the Forge are the supported equipment path; crafting and gathering quests have been removed.

## Equipment progression in the quest chain

| Milestone | Current behavior |
| --- | --- |
| The Soul Archive v3 | Absorb the First Hunt Essence, then equip any Essence; receive 500 Cinders. |
| First Weapon v2 | Choose and equip the full starter hands/armor kit in Equipment & Forge. Completion awards 10 Scrap and grants a band, amulet and vial. |
| Ready for the Road v2 | Equip those accessories. The internal quest ID remains quest.onboarding.tools_of_trade. |
| Into the Ruins v2 | Win the introductory Lumo encounter. |
| All ten Shenic quests v4 | Award two Scrap and an area Essence Token; other authored rewards remain. |
| Trial of Lumo / Crystal Currents v4 | Also grant the first Goblin Mines / Forgotten Catacombs sigil. |
| The Restless Dead v4 | Equip an archetype earned through a plain target, win five Moonlit Graves encounters and reach level 20. |
| Blood Grove Veteran v2 | Win 25 Blood Grove encounters; receive two Scrap. |

Missing entitled starter and earned plain-target equipment can be recovered in Equipment & Forge at rank 0, plain and bound. Recovery grants no extra resources or quest rewards. Forge investment is optional during onboarding.

The removed quests are Arms of Choice, Armor and Adornment, Made by Your Own Hand, Tempered Resolve, A Crafter's Signature, Exceptional Work, and Stone, Timber, and Hide. Their definitions and superseded quest versions are gone. The [quest-progress cleanup migration](LL/src/Infrastructure/Persistence/Persistence.LL/Migrations/20260903121634_RemoveRetiredAlphaQuestProgress.cs) deletes saved progress absent from the frozen current catalog, including its objectives. Retained current versions keep their progress; removed versions can restart through normal availability. Rebuild and restart the local API to apply pending migrations. See [cleanup details](docs/design/equipment-post-alpha-cleanup.md) for verification and database limitations.

## How to read this

- Regular quests appear automatically in **Quests** when their level and prerequisite conditions are met. There is no NPC or location you must visit to pick them up.
- **Starts after** means the named quest must be completed, not merely started. A listed level is an additional requirement; earlier quests can already require a higher level.
- **Where / task** tells you where to do the quest, not where to collect it.
- Follow-ups unlock when their own requirements are met. Multiple follow-ups are parallel branches, not a required order. **None** means no further quest currently depends on that quest.
- These are one-time regular quests. Daily/weekly Prophecies are a separate system, although one quest asks you to complete a daily Prophecy.

## 1. Tutorial — the starting chain

All five quests have a minimum level of 1. Do them in order:

**Your First Hunt → The Soul Archive → First Weapon → Ready for the Road → Into the Ruins**

| Quest | Starts after | Where / task | Follow-up |
|---|---|---|---|
| Your First Hunt | New character; introductory welcome | Quests: choose Goblin Warrior, Hollow Stag, or Skeleton. Training Area: defeat your chosen creature. | The Soul Archive |
| The Soul Archive | Your First Hunt | Essences: absorb the First Hunt Essence, then equip any Essence in a loadout. | First Weapon |
| First Weapon | The Soul Archive | Equipment & Forge: choose hands and armor, then equip the full starter kit. | Ready for the Road |
| Ready for the Road | First Weapon | Inventory: equip the granted band, amulet and vial. | Into the Ruins |
| Into the Ruins | Ready for the Road | Lumo Ruins: win 1 encounter. | Trial of Lumo and the side-quest branches below |

The first quest is displayed as **Choose Your First Hunt** before choosing, then **Hunt the [chosen creature]**. These are names for the same quest, not three different chains.

Existing ownership of the selected First Hunt Essence satisfies The Soul Archive's absorption step. Any Essence already equipped in a loadout satisfies the following equipment step. These checks run when quests become available, during event processing, and when opening the journal, so players whose quests restarted during the ongoing Alpha can continue without absorbing or equipping the same Essence again. Prerequisites, First Hunt choices, and sequential objective order still apply.

A Second Soul uses the current number of distinct owned Essences, including those absorbed before the quest unlocked. Active saved rows receive the two-Essence requirement without another reset; completed quests retain their completion. Reconciled quests grant their normal rewards once and unlock follow-ups. Restarted quests can grant their rewards again after the earlier reset removed their reward markers; repeated journal reads and event retries do not grant additional rewards. This repair needs no new database migration or configuration setting. Release the updated quest catalog with the backend; this change does not deploy or modify the running Alpha.

Completing **Into the Ruins** finishes the tutorial. Alongside the main campaign, it unlocks A Second Soul, Focused Pursuit, The Arena Calls and An Omen Fulfilled. **Sigils in the Dust** also requires Lv5. Scheduled event participation becomes available after this point when an event is active.

## 2. Shenic campaign — the main path

Each row follows the previous row, starting after **Into the Ruins**. Every quest here rewards **one Essence Token for its area**. Redeem it in Inventory to choose one of that area's five Essences; no option is preselected.

### Chapter I — First Blood

| Quest | Starts after / level | Where / task | Follow-up |
|---|---|---|---|
| Trial of Lumo | Into the Ruins; Lv1 | Lumo Ruins: win 4 encounters and reach Lv5. | Blood in the Grove; A Name in Shenic |
| Blood in the Grove | Trial of Lumo; Lv5 | Blood Grove: win 4 encounters; clear Goblin Mines I; reach Lv10. | Crystal Currents; Blood Grove Veteran |

### Chapter II — Resonant Paths

| Quest | Starts after / level | Where / task | Follow-up |
|---|---|---|---|
| Crystal Currents | Blood in the Grove; Lv10 | Crystal Creek: win 5 encounters and reach Lv15. | The Restless Dead |
| The Restless Dead | Crystal Currents; Lv15 | Earn a plain target through Shenic combat and equip that archetype; win 5 encounters in Moonlit Graves; reach Lv20. | Between Day and Night |

### Chapter III — Heart of Shenic

| Quest | Starts after / level | Where / task | Follow-up |
|---|---|---|---|
| Between Day and Night | The Restless Dead; Lv20 | Twilight Clearing: win 5 encounters and reach Lv25. | The Roots Remember |
| The Roots Remember | Between Day and Night; Lv25 | Old Forest: win 6 encounters and reach Lv30. | Heart of the Hollow |
| Heart of the Hollow | The Roots Remember; Lv30 | Win 6 encounters in Thornroot Hollow, **then** clear Forgotten Catacombs I. | Ash Beneath the Earth at Lv35 |

**Heart of the Hollow** is the level-30 milestone used by the focused Beta journey. It is not the end of all authored quests. The next campaign quest waits for **Lv35**; there is no separate campaign quest for levels 31–34.

### Beyond the Focused Beta — later Shenic

These quests exist in the current catalog, despite the development-oriented chain name.

| Quest | Starts after / level | Where / task | Follow-up |
|---|---|---|---|
| Ash Beneath the Earth | Heart of the Hollow; Lv35 | Embercap Burrows: win 12 encounters and reach Lv40. | The Veil Over the Marsh |
| The Veil Over the Marsh | Ash Beneath the Earth; Lv40 | Moonveil Marsh: win 15 encounters and reach Lv45. | Last Light in Duskmire |
| Last Light in Duskmire | The Veil Over the Marsh; Lv45 | Duskmire Hollow: win 20 encounters. | Warden of Shenic, if Tested Wanderer is also complete |

There is currently **no authored Meran/Region 2 quest chain** after Shenic. Other gameplay systems and unfinished side quests remain available according to their normal unlock rules.

## 3. Side quests — parallel branches

These branches run alongside the campaign. They do not need to be completed before continuing the main Shenic quest line. No extra level above 1 is required unless shown.

### Essences

**A Second Soul** opens three independent follow-ups. **Focused Pursuit** is a separate branch.

| Quest | Starts after / level | Where / task | Follow-up |
|---|---|---|---|
| A Second Soul | Into the Ruins | Essences: own two distinct Essences; existing collections count. | An Adaptable Archive at Lv5; The Archive Deepens; Resonant Pair at Lv20 |
| An Adaptable Archive | A Second Soul; Lv5 | Essences: attune Goblin, then Lumo Wisp, then Lumo Sentinel. | None |
| The Archive Deepens | A Second Soul | Essences: ascend an Essence. | None |
| Resonant Pair | A Second Soul; Lv20 | Essences: equip **three** Essences sharing an ability tag other than Physical or Melee. | None |
| Focused Pursuit | Into the Ruins | Creature Archive / combat: receive an Essence from the creature selected as your Essence Focus. | None |

Despite its title, **Resonant Pair currently requires three Essences**, not two. **An Adaptable Archive** requires three named attunements in sequence, not a three-way reward choice.

### Character milestones and combat

| Quest | Starts after / level | Where / task | Follow-up |
|---|---|---|---|
| A Name in Shenic | Trial of Lumo | Normal gameplay: reach Lv10. | Tested Wanderer |
| Tested Wanderer | A Name in Shenic | Normal gameplay: reach Lv25. | Warden of Shenic, after Last Light in Duskmire |
| Warden of Shenic | **Both** Last Light in Duskmire and Tested Wanderer; Lv45 | Duskmire Hollow: win 1 encounter. | None |
| Blood Grove Veteran | Blood in the Grove; effectively Lv10+ | Blood Grove: win 25 encounters. | None |

**Blood Grove Veteran** declares Lv5, but completing the current **Blood in the Grove** already requires Lv10. Level milestones can complete immediately if the character has already reached the required level.

### Dungeons

| Quest | Starts after / level | Where / task | Follow-up |
|---|---|---|---|
| Sigils in the Dust | Into the Ruins; Lv5 | World Map / Dungeons: enter a dungeon. | Into the Depths |
| Into the Depths | Sigils in the Dust; Lv5 | Dungeons: complete a run and claim its rewards. | None |

These side quests accept any qualifying dungeon; the main campaign's Goblin Mines I and Forgotten Catacombs I objectives require those specific dungeons.

### Colosseum

| Quest | Starts after | Where / task | Follow-up |
|---|---|---|---|
| The Arena Calls | Into the Ruins | Colosseum: start a battle. | Tournament Tested |
| Tournament Tested | The Arena Calls | Colosseum: complete a tournament battle. | None |

### Prophecies

| Quest | Starts after | Where / task | Follow-up |
|---|---|---|---|
| An Omen Fulfilled | Into the Ruins | Prophecies: complete 1 daily Prophecy. | None |

## 4. Scheduled server-wide event quests

The catalog retains **The Defense of Lumo** as an example: 250,000 shared Lumo encounter victories, scheduled for 10–16 August 2026, with a final claim deadline of 18 August 2026 at 23:59:59 UTC. Its activity and claim windows have expired. An enabled definition does not override those dates. Participation requires Into the Ruins to be complete.

The old tempering and gathering events, **A broken Curse** and **A Realm Replenished**, were removed with their definitions. No event schedule was extended or reopened. Further LiveOps work is deferred.

## Quick takeaways

- **New character:** follow the five tutorial quests, then Trial of Lumo.
- **Main progression:** follow the ten Shenic area quests; each awards its area's Essence Token.
- **Several quests at once:** expected after Into the Ruins; side branches are optional alongside the campaign.
- **Finished Heart of the Hollow:** reach Lv35 for Ash Beneath the Earth.
- **Finished Last Light in Duskmire:** finish Tested Wanderer to unlock Warden of Shenic, or pursue remaining side branches. No further regional campaign is currently defined.

*Source scope: the main game's current Data/quests and Data/event-quests catalogs, checked against quest availability rules. This describes repository content, not deployed configuration or a live account. No legacy quest publication flag or alternate cohort catalog remains.*
