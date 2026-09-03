# Quest Content Backlog

> Historical Alpha plan, superseded 3 September 2026. Crafting/gathering progression, queued tempering and their obsolete quest content have been removed. Conversion, refund and compatibility/backfill proposals below are not current implementation work. Shared numerical helpers with active consumers may remain. See the [post-Alpha cleanup](../../docs/design/equipment-post-alpha-cleanup.md) and [current quest flow](../../LEGENDSLEGACY_QUEST_FLOW.md) for supported behavior.

## Purpose

This document is a content backlog for quests that could be added to Legends
Legacy. It is intentionally broader than the current tutorial chain, but each
idea is labeled according to how much quest-engine work it requires.

Quest names, objective counts, and rewards are proposals rather than final
balance values. Stable quest IDs should be chosen before a quest is released
and should not be renamed after player progress exists.

## Implemented Content

The first content release is now represented in the JSON quest catalog:

- `Into the Ruins` is the final automatic Tutorial quest.
- `Trial of Lumo` is the first non-Tutorial Shenic quest and activates as soon
  as its prerequisites are met.
- The nine regional quests after `Trial of Lumo` form the area-unlock chain
  through Duskmire Hollow.
- All quests in **Additional Quests Supported Now** are implemented. `A Second
  Soul` intentionally requires only absorbing any additional Essence; it does
  not require equipping it.
- `Stone, Timber, and Hide` is implemented as the first post-tutorial gathering
  quest. It counts Lumo Ruins actions separately for each equipped tool type.
- `Focused Pursuit`, `The Arena Calls`, and `An Omen Fulfilled` introduce
  Essence Focus, Colosseum, and daily Prophecy activities as side quests.

## Implementation Labels

- **Ready now:** Can be authored with the current quest catalog and existing
  game events.
- **Small extension:** Requires one new objective evaluator or game event, but
  fits the current non-repeatable quest lifecycle.
- **System extension:** Requires a larger addition such as currency rewards,
  choices, repeatability, or account-wide progress.

## Current Quest Capabilities

The current system supports:

- availability based on character level and completed prerequisite quests;
- automatic activation when availability requirements are met;
- sequential or simultaneous objectives;
- winning encounters in a specific combat area;
- absorbing and equipping a specified Essence;
- crafting and equipping qualifying equipment;
- equipping gathering tools;
- counting area actions by equipped gathering-tool type;
- setting Essence Focus for a known creature;
- starting a Colosseum battle;
- completing a daily Prophecy;
- reaching a character level;
- item rewards;
- quest-driven combat-area unlocks;
- one pinned quest with objective navigation.

The current system does not yet support creature-specific kills, gathering
results, dungeon completion, Colosseum outcome filters, Guild activity, marketplace
activity, Soulstone upgrades, broader Prophecy filters, currency rewards,
repeatable quests, dialogue choices, or branching outcomes.

## Recommended First Content Release

### Shenic Regional Progression

This chain is the most direct continuation after `Into the Ruins`. Every quest
can be implemented with the existing `CombatEncounterCompleted` objective.
Each area's level requirement should remain in place alongside its quest
requirement.

| Order | Quest                    | Stable ID                             | Minimum level | Objective                              | Unlock                  | Status      |
| ----: | ------------------------ | ------------------------------------- | ------------: | -------------------------------------- | ----------------------- | ----------- |
|     1 | Trial of Lumo            | `quest.shenic.trial_of_lumo`          |             1 | Win 10 encounters in Lumo Ruins        | Blood Grove             | Implemented |
|     2 | Blood in the Grove       | `quest.shenic.blood_in_the_grove`     |             5 | Win 5 encounters in Blood Grove        | Crystal Creek           | Implemented |
|     3 | Crystal Currents         | `quest.shenic.crystal_currents`       |            10 | Win 7 encounters in Crystal Creek      | Moonlit Graves          | Implemented |
|     4 | The Restless Dead        | `quest.shenic.restless_dead`          |            15 | Win 8 encounters in Moonlit Graves     | Twilight Clearing       | Implemented |
|     5 | Between Day and Night    | `quest.shenic.between_day_and_night`  |            20 | Win 10 encounters in Twilight Clearing | Old Forest              | Implemented |
|     6 | The Roots Remember       | `quest.shenic.roots_remember`         |            25 | Win 10 encounters in Old Forest        | Thornroot Hollow        | Implemented |
|     7 | Heart of the Hollow      | `quest.shenic.heart_of_the_hollow`    |            30 | Win 12 encounters in Thornroot Hollow  | Embercap Burrows        | Implemented |
|     8 | Ash Beneath the Earth    | `quest.shenic.ash_beneath_the_earth`  |            35 | Win 12 encounters in Embercap Burrows  | Moonveil Marsh          | Implemented |
|     9 | The Veil Over the Marsh  | `quest.shenic.veil_over_the_marsh`    |            40 | Win 15 encounters in Moonveil Marsh    | Duskmire Hollow         | Implemented |
|    10 | Last Light in Duskmire   | `quest.shenic.last_light_in_duskmire` |            45 | Win 20 encounters in Duskmire Hollow   | Complete the Shenic arc | Implemented |

Suggested structure:

- make each quest available after completing the previous regional quest;
- require the destination area's existing minimum level;
- activate regional quests automatically after the tutorial chain;
- use one item bundle appropriate to the area's level as the reward;
- add the completed quest ID to the following area's access definition;
- keep the final quest ungated from future regions until another region is
  ready to ship.

### Regional Quest Summaries

#### Blood in the Grove

Blood Grove has begun spreading toward the old roads. Thin the creatures
feeding its growth and find proof that the corruption is not natural.

#### Crystal Currents

The waters through Crystal Creek carry fragments that resonate with absorbed
Essences. Secure the creek before that resonance draws something worse.

#### The Restless Dead

The dead of Moonlit Graves no longer remain quiet. Break their hold on the
grave paths and uncover what is waking them.

#### Between Day and Night

Twilight Clearing has stopped following the natural cycle of the sun. Survive
the creatures gathering beneath its permanent dusk.

#### The Roots Remember

The Old Forest remembers every wound dealt to Shenic. Push through its
guardians and follow the corrupted roots deeper into the region.

#### Heart of the Hollow

Thornroot Hollow is strangling the forest around it. Cut through its defenders
before the hollow becomes impossible to contain.

#### Ash Beneath the Earth

Heat and spores are rising from Embercap Burrows. Descend far enough to learn
what is feeding the fires below Shenic.

#### The Veil Over the Marsh

Moonveil Marsh hides the source of the region's spreading corruption. Cross
the flooded paths and force its creatures out of hiding.

#### Last Light in Duskmire

The trail ends in Duskmire Hollow, where even moonlight fails. Break the force
holding the hollow and complete the first Shenic campaign.

## Additional Quests Supported Now

These quests can be built with existing objective types. Item filters and
reward IDs still need to be selected from live content before authoring JSON.

| Quest                 | Category  | Implemented objective                                  | Reward                  | Status      |
| --------------------- | --------- | ------------------------------------------------------ | ----------------------- | ----------- |
| A Second Soul         | Essences  | Absorb any additional Essence                          | 10 Soul Dust            | Implemented |
| An Adaptable Archive  | Essences  | Equip Goblin, Lumo Wisp, and Lumo Sentinel in sequence | 25 Soul Dust            | Implemented |
| Arms of Choice        | Crafting  | Craft one weapon from each supported one-handed family | 50 Ore and 25 Wood      | Implemented |
| Blood Grove Veteran   | Combat    | Win 25 encounters in Blood Grove                       | 25 Rawhide              | Implemented |
| A Name in Shenic      | Character | Reach level 10                                         | 1 Advancement Stone     | Implemented |
| Tested Wanderer       | Character | Reach level 25                                         | 2 Advancement Stones    | Implemented |
| Warden of Shenic      | Character | Finish the chain at level 45, then defend Duskmire     | 5 Advancement Stones    | Implemented |
| Stone, Timber, and Hide | Gathering | Complete 10 Lumo Ruins actions with each tool type   | 12 Ore, Wood, and Hide each | Implemented |
| Focused Pursuit        | Essences  | Set Essence Focus for one known creature               | 10 Soul Dust            | Implemented |
| The Arena Calls        | Colosseum | Start one Colosseum battle                              | 1 Advancement Stone     | Implemented |
| An Omen Fulfilled      | Prophecies | Complete one daily Prophecy                            | 1 Advancement Stone     | Implemented |

## Quests Requiring Small Engine Extensions

### Gathering

#### The Bloodwood Cut

- **Concept:** Gather Bloodwood from Blood Grove several times.
- **New objective:** `GatheringNodeCompleted` with a node ID filter and counter.
- **Reward:** Bloodwood or a crafting recipe component.

#### Relics in the Seam

- **Concept:** Mine Crystal Seam and Grave Ore nodes.
- **New objective:** `GatheringNodeCompleted` supporting simultaneous
  objectives.
- **Reward:** A mining-focused tool or regional crafting materials.

### Crafting and Tempering

#### Tempered Resolve

- **Concept:** Temper a crafted weapon once, then equip it.
- **New objective:** `EquipmentTempered`, ideally with tier, item type, and
  minimum tempering-progress filters.
- **Reward:** Tempering materials.

#### A Crafter's Signature

- **Concept:** Craft an uncommon-or-better item with a minimum potential.
- **New objective:** Extend `EquipmentCrafted` filters with rarity, quality,
  potential, and equipment type.
- **Reward:** A blueprint or premium crafting component.

#### Tools Worth Keeping

- **Concept:** Craft or temper one tool for each gathering profession.
- **New objective:** Equipment crafting/tempering filters for tool type and
  gathering type.
- **Reward:** Tool materials or a tool-affix component.

### Essences and Soulstones

#### The Archive Deepens

- **Concept:** Upgrade or ascend an Essence after absorbing it.
- **New objective:** `EssenceUpgraded` with definition, rank, or tier filters.
- **Reward:** Essence dust or a selected Essence item.

#### Resonant Pair

- **Concept:** Equip two Essences sharing a compatible tag or role.
- **New objective:** Extend the loadout evaluator to support count and tag
  filters rather than one exact definition ID.
- **Reward:** Essence materials.

#### Stone Awakening

- **Concept:** Purchase the first Soulstone upgrade.
- **New objective:** `SoulstoneUpgraded` with stone and level filters.
- **Reward:** Soulstone currency or an item equivalent.
- **Additional work:** Direct Soulstone currency rewards need a new reward
  handler; item rewards can be used initially.

### Dungeons

#### Sigils in the Dust

- **Concept:** Assemble a dungeon sigil and inspect an available route.
- **New objectives:** `DungeonSigilAssembled` and optionally
  `DungeonRouteSelected`.
- **Reward:** Entry materials.

#### Into the Depths

- **Concept:** Complete the first dungeon run and claim its rewards.
- **New objectives:** `DungeonRunCompleted` and `DungeonRewardsClaimed`.
- **Reward:** Dungeon-specific item bundle.

#### No Room Unanswered

- **Concept:** Complete every room in a dungeon run without abandoning it.
- **New objective:** `DungeonRunCompleted` with completion-quality filters.
- **Reward:** Mastery or sigil materials.

### City Activities

#### The Bazaar Beckons

- **Concept:** Visit the Cinder Bazaar and create the first listing.
- **New objective:** `MarketListingCreated`.
- **Reward:** An item bundle; Cinders require a new reward handler.

#### A Fair Exchange

- **Concept:** Complete one marketplace purchase and one sale.
- **New objectives:** `MarketPurchaseCompleted` and `MarketSaleCompleted`.
- **Reward:** Cinders after currency rewards exist.

#### Oathbound

- **Concept:** Join or create a Guild.
- **New objective:** `GuildMembershipChanged`.
- **Reward:** Guild contribution items or currency.

#### For the Guild

- **Concept:** Contribute to a Guild building and complete a Guild mission.
- **New objectives:** `GuildContributionMade` and `GuildMissionCompleted`.
- **Reward:** Guild currency or a mission item bundle.

#### Tournament Tested

- **Concept:** Complete a tournament and reach a specified placement.
- **New objective:** `TournamentCompleted` with placement filters.
- **Reward:** Tournament currency, equipment, or a title.

## Narrative Hunts Requiring Creature Filters

These can reuse combat progression once `CombatEncounterCompleted` includes
the defeated creature IDs and quest filters support them.

| Quest                    | Area                               | Hunt concept                                   | Suggested targets                                    |
| ------------------------ | ---------------------------------- | ---------------------------------------------- | ---------------------------------------------------- |
| Goblin Trouble           | Lumo Ruins                         | Break an organized goblin raiding party        | Goblin, Goblin Archer, Goblin Shaman, Goblin Warrior |
| Fangs in the Grove       | Blood Grove                        | Cull predators altered by the grove            | Blood Grove creature subset                          |
| Lights Beneath the Water | Crystal Creek                      | Track the wisps gathering around the creek     | Crystal Wisp and related creatures                   |
| Restless Company         | Moonlit Graves                     | Put several kinds of undead to rest            | Skeleton, Undead, Grave Wisp, Grave Hound            |
| Root and Thorn           | Old Forest and Thornroot Hollow    | Follow the region's corrupted plant life       | Treant Sapling, Forest Spirit, Rotroot Shambler      |
| Fire Below               | Embercap Burrows                   | Hunt creatures thriving around the buried heat | Flame Imp, Cinder Beetle, Smolder Rat                |
| Mire Hunters             | Moonveil Marsh and Duskmire Hollow | Clear the marsh's most dangerous predators     | Marsh and hollow creature subsets                    |

Creature-specific quests should store stable creature IDs in filters and use
names only for player-facing descriptions.

## Larger Quest-System Extensions

### Repeatable Bounties

Examples:

- win a number of encounters in a chosen unlocked area;
- gather from a chosen node type;
- complete a dungeon;
- win a Colosseum match;
- fulfill a Prophecy.

This requires repeatable quest definitions, reset schedules, occurrence IDs,
reward limits, and protection against events from a previous occurrence being
credited after reset.

### Branching Quest Decisions

Examples:

- give a recovered relic to the Guild, Bazaar, or Archivist;
- choose which area receives protection first;
- choose one of several permanent reward items.

This requires dialogue/choice objectives, mutually exclusive branches,
persisted decisions, alternate follow-up availability, and a selection reward
handler.

### Account and Legacy Quests

Examples:

- complete Shenic with multiple characters;
- unlock a set of titles;
- reach a Legacy Renown threshold;
- complete one achievement in every category.

These require account-scoped progress and should not reuse character quest
rows without an explicit scope model.

## Reward Backlog

The current engine grants item rewards only. Useful future reward handlers
would be:

1. Cinders;
2. Soulstones and other feature currencies;
3. titles;
4. recipes or blueprints;
5. fixed equipment instances;
6. randomized item caches;
7. choice rewards;
8. Guild, Colosseum, Dungeon, or Prophecy currency;
9. account or Legacy Renown rewards.

Every reward handler should use the quest reward key as an idempotency key so
retries cannot grant the reward twice.

## Suggested Delivery Order

1. Author the nine-quest Shenic progression chain using existing combat
   objectives and item rewards.
2. Add gathering-result objectives for node-specific gathering quests.
3. Add tempering and broader equipment filters for the crafting side quests.
4. Add Dungeon completion and Colosseum outcome filters for mid-game quest lines.
5. Add Guild, marketplace, Soulstone, and broader Prophecy objectives.
6. Add currency, title, and blueprint reward handlers.
7. Add creature-specific hunts once combat events expose defeated creature
   IDs.
8. Add repeatable quests only after the non-repeatable catalog has enough
   content and telemetry to establish safe reward values.

## Content Authoring Checklist

Before implementing any proposed quest:

- confirm its stable ID, category, version, and sort order;
- confirm all prerequisite quest, area, creature, Essence, and item IDs;
- confirm the level and quest prerequisites that unlock it;
- ensure every objective is backed by a durable game event or current-state
  evaluator;
- author destination routes and optional guide/tour metadata;
- validate the reward item exists and is appropriate for the required level;
- add the next area's quest gate only when the unlocking quest ships;
- test duplicate event delivery and reward idempotency;
- test characters already above the minimum level;
- test the complete chain from a new character and from migrated progress.
