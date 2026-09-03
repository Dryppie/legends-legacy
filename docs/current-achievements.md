# Current Achievements

Last reviewed: 2026-08-09

This document lists all 101 active achievement definitions in `LL/src/API/API.LL/Data/achievements/`, what completes them, and how their unlock message is delivered. All current achievements are non-repeatable.

## Unlock message types

- **Player-only system message**: only the character who unlocked the achievement receives the regular `System` message.
- **Broadcast to all**: every player receives the global `World` message. The unlocking character also receives their regular player system message.

There are currently 41 world-broadcast achievements and 60 player-only achievements.

## General

| Achievement | Completion requirement                     | Progress scope | Points | Unlock message             |
| ----------- | ------------------------------------------ | -------------- | -----: | -------------------------- |
| First Steps | Create the account or its first character. | Account        |      5 | Player-only system message |
| Adventurer  | Reach character level 10.                  | Character      |     10 | Player-only system message |
| Veteran     | Reach character level 50.                  | Character      |     25 | Player-only system message |

## Legacy

| Achievement   | Completion requirement               | Progress scope | Points | Unlock message       |
| ------------- | ------------------------------------ | -------------- | -----: | -------------------- |
| Renowned      | Earn 1,000 total Achievement Points. | Account        |      0 | **Broadcast to all** |
| Living Legend | Earn 2,500 total Achievement Points. | Account        |      0 | **Broadcast to all** |

## Combat

| Achievement                 | Completion requirement                                   | Progress scope | Points | Unlock message             |
| --------------------------- | -------------------------------------------------------- | -------------- | -----: | -------------------------- |
| First Blood                 | Defeat 1 monster.                                        | Account        |      5 | Player-only system message |
| Monster Hunter I            | Defeat 1,000 monsters.                                   | Account        |     10 | Player-only system message |
| Monster Hunter II           | Defeat 100,000 monsters.                                 | Account        |     25 | Player-only system message |
| Relentless                  | Defeat 3,000,000 monsters.                               | Account        |     50 | **Broadcast to all**       |
| Goblin Bane I               | Defeat 1,000 Goblin-family creatures.                    | Account        |     10 | Player-only system message |
| Goblin Bane II              | Defeat 100,000 Goblin-family creatures.                  | Account        |     25 | Player-only system message |
| Death Teaches               | Experience 1,000 combat defeats.                         | Character      |     25 | Player-only system message |
| The Unyielding _(obscured)_ | Win a combat encounter with 5% Health or less remaining. | Character      |     50 | **Broadcast to all**       |

## Essences

| Achievement       | Completion requirement                       | Progress scope | Points | Unlock message             |
| ----------------- | -------------------------------------------- | -------------- | -----: | -------------------------- |
| First Absorption  | Absorb 1 Essence.                            | Account        |      5 | Player-only system message |
| Soulbound         | Equip 10 Essences at the same time.          | Character      |     25 | Player-only system message |
| Soul Collector I  | Add 10 unique Essences to the Soul Archive.  | Account        |     10 | Player-only system message |
| Soul Collector II | Add 50 unique Essences to the Soul Archive.  | Account        |     25 | Player-only system message |
| Archivist         | Add 100 unique Essences to the Soul Archive. | Account        |     50 | **Broadcast to all**       |
| Master Archivist  | Add 250 unique Essences to the Soul Archive. | Account        |    100 | **Broadcast to all**       |
| Living Archive    | Add 500 unique Essences to the Soul Archive. | Account        |    250 | **Broadcast to all**       |
| Ascendant         | Ascend 1 Essence.                            | Account        |     25 | Player-only system message |
| Essence Paragon   | Ascend 10 Essences to Tier 3.                | Account        |    100 | **Broadcast to all**       |
| Beastbinder       | Complete the Beast Essence collection.       | Account        |    100 | **Broadcast to all**       |

## Dungeons

| Achievement        | Completion requirement                                   | Progress scope | Points | Unlock message             |
| ------------------ | -------------------------------------------------------- | -------------- | -----: | -------------------------- |
| Into the Depths    | Enter 1 dungeon.                                         | Character      |      5 | Player-only system message |
| Dungeon Initiate   | Clear 1 dungeon.                                         | Character      |     10 | Player-only system message |
| Deep Delver        | Clear 1,000 total dungeons.                              | Account        |    250 | **Broadcast to all**       |
| Minebreaker        | Clear Goblin Mines.                                      | Character      |     25 | Player-only system message |
| Catacomb Walker    | Clear Forgotten Catacombs.                               | Character      |     25 | Player-only system message |
| Hivebreaker        | Defeat the Ant Queen in The Hive's Abyss.                | Character      |     50 | **Broadcast to all**       |
| Royal Exterminator | Defeat the Ant King in The Hive's Abyss.                 | Character      |    100 | **Broadcast to all**       |
| Deathless Run      | Clear a dungeon without being defeated.                  | Character      |     50 | **Broadcast to all**       |
| No Step Back       | Clear a dungeon instead of retreating with Pending Loot. | Character      |     25 | Player-only system message |

## Crafting

| Achievement          | Completion requirement     | Progress scope | Points | Unlock message             |
| -------------------- | -------------------------- | -------------- | -----: | -------------------------- |
| First Craft          | Craft 1 item.              | Account        |      5 | Player-only system message |
| Tempered Hands       | Temper items 10,000 times. | Account        |    250 | Player-only system message |
| Masterpiece          | Craft 1 Masterpiece.       | Account        |     25 | Player-only system message |
| Master Smith         | Craft 250 Masterpieces.    | Account        |    100 | **Broadcast to all**       |
| First Blueprint      | Unlock 1 Blueprint.        | Account        |      5 | Player-only system message |
| Blueprint Apprentice | Unlock 10 Blueprints.      | Account        |     25 | Player-only system message |
| Blueprint Scholar    | Unlock 50 Blueprints.      | Account        |    100 | **Broadcast to all**       |
| Set Maker            | Craft 1 Set Item.          | Account        |     25 | Player-only system message |

## Colosseum

| Achievement    | Completion requirement                                             | Progress scope | Points | Unlock message             |
| -------------- | ------------------------------------------------------------------ | -------------- | -----: | -------------------------- |
| First Duel     | Complete 1 Colosseum battle.                                       | Character      |      5 | Player-only system message |
| First Victory  | Win 1 Colosseum battle.                                            | Character      |     10 | Player-only system message |
| Duelist        | Win 100 Colosseum battles.                                         | Character      |     25 | Player-only system message |
| Arena-Blooded  | Win 1,000 Colosseum battles.                                       | Character      |    100 | **Broadcast to all**       |
| Giant Slayer   | Defeat an opponent whose rating is at least 200 higher than yours. | Character      |     50 | **Broadcast to all**       |
| Untouchable    | Win 10 Colosseum battles in a row.                                 | Character      |     50 | **Broadcast to all**       |
| The Challenger | Complete 10,000 Colosseum battles.                                 | Character      |    100 | Player-only system message |

## Hidden

These achievements are active but concealed from players until they are unlocked.

| Achievement           | Completion requirement                                            | Progress scope | Points | Unlock message             |
| --------------------- | ----------------------------------------------------------------- | -------------- | -----: | -------------------------- |
| One Health Wonder     | Win a combat encounter with 1% Health or less remaining.          | Character      |     50 | **Broadcast to all**       |
| Rat Food[^unwired]    | Lose a battle against a Rat while significantly stronger than it. | Character      |     25 | Player-only system message |
| Trap Magnet[^unwired] | Trigger 250 dungeon traps.                                        | Account        |     25 | Player-only system message |
| Potential's End       | Create a high-quality item with less than 10 Potential remaining. | Account        |     50 | **Broadcast to all**       |
| Cursed Hands          | Trigger 250 cursed crafting outcomes.                             | Account        |     25 | Player-only system message |
| Comeback              | Win a Colosseum battle after a losing streak of at least 5.       | Character      |     25 | Player-only system message |

[^unwired]: The definition is active in the catalog, but its requirement type currently has no gameplay progress hook in `AchievementService`. As implemented, this achievement cannot currently unlock through normal gameplay.

## Added achievements

The achievements below were added on 2026-08-09. Their names, thresholds, scopes, points, and delivery types are live catalog values.

### Added using existing progress hooks

These reuse existing progress sources, with a dedicated unique-item-variant requirement added for Set Architect.

| Category  | Suggested achievement | Completion requirement                                             | Scope     | Points | Suggested unlock message   |
| --------- | --------------------- | ------------------------------------------------------------------ | --------- | -----: | -------------------------- |
| General   | Journeyman            | Reach character level 25.                                          | Character |     15 | Player-only system message |
| General   | Pathfinder            | Reach character level 100.                                         | Character |     50 | **Broadcast to all**       |
| General   | Transcendent          | Reach character level 250.                                         | Character |    100 | **Broadcast to all**       |
| Legacy    | Rising Renown         | Earn 500 total Achievement Points.                                 | Account   |      0 | Player-only system message |
| Legacy    | Eternal Legacy        | Earn 5,000 total Achievement Points.                               | Account   |      0 | **Broadcast to all**       |
| Combat    | Monster Hunter III    | Defeat 1,000,000 monsters.                                         | Account   |     50 | **Broadcast to all**       |
| Combat    | World Scourge         | Defeat 10,000,000 monsters.                                        | Account   |    150 | **Broadcast to all**       |
| Combat    | Goblin Bane III       | Defeat 1,000,000 Goblin-family creatures.                          | Account   |     75 | **Broadcast to all**       |
| Combat    | Hard Lessons          | Experience 100 combat defeats.                                     | Character |     15 | Player-only system message |
| Combat    | When Will I Learn     | Experience 1000 combat defeats.                                    | Character |     50 | Player-only system message |
| Essences  | Soul Curator          | Add 25 unique Essences to the Soul Archive.                        | Account   |     15 | Player-only system message |
| Essences  | Ascension Circle      | Ascend 10 Essences.                                                | Account   |     50 | Player-only system message |
| Essences  | Paragon Host          | Ascend 50 Essences to Tier 3.                                      | Account   |    150 | **Broadcast to all**       |
| Dungeons  | Seasoned Delver       | Clear 10 total dungeons.                                           | Account   |     15 | Player-only system message |
| Dungeons  | Dungeon Veteran       | Clear 100 total dungeons.                                          | Account   |     50 | Player-only system message |
| Dungeons  | Dungeon Legend        | Clear 500 total dungeons.                                          | Account   |    100 | **Broadcast to all**       |
| Crafting  | Apprentice Smith      | Craft 100 items.                                                   | Account   |     15 | Player-only system message |
| Crafting  | Prolific Artisan      | Craft 1,000 items.                                                 | Account   |     50 | Player-only system message |
| Crafting  | Tempering Initiate    | Temper items 100,000 times.                                        | Account   |     15 | Player-only system message |
| Crafting  | Tempering Master      | Temper items 1,000,000 times.                                      | Account   |     75 | **Broadcast to all**       |
| Crafting  | Set Architect         | Craft 25 Item Variants.                                            | Account   |     75 | Player-only system message |
| Colosseum | Arena Regular         | Complete 10 Colosseum battles.                                     | Character |     10 | Player-only system message |
| Colosseum | Proven Duelist        | Win 10 Colosseum battles.                                          | Character |     15 | Player-only system message |
| Colosseum | Arena Veteran         | Win 500 Colosseum battles.                                         | Character |     50 | Player-only system message |
| Colosseum | Hot Streak            | Win 5 Colosseum battles in a row.                                  | Character |     25 | Player-only system message |
| Colosseum | Giant's Bane          | Defeat an opponent whose rating is at least 300 higher than yours. | Character |    100 | **Broadcast to all**       |
| Colosseum | The Phoenix           | Win a Colosseum battle after a losing streak of at least 10.       | Character |     75 | **Broadcast to all**       |
| Hidden    | Potential Zero        | Create a high-quality item with less than 5 Potential remaining.   | Account   |     75 | **Broadcast to all**       |

### Added with new progress hooks

These are backed by new requirement types and live gameplay hooks. Recalculation restores historical progress where the underlying system retains enough data.

| Category          | Suggested achievement         | Completion requirement                                  | Scope     | Points | Suggested unlock message   |
| ----------------- | ----------------------------- | ------------------------------------------------------- | --------- | -----: | -------------------------- |
| Prophecies        | First Omen                    | Complete 1 Prophecy.                                    | Account   |      5 | Player-only system message |
| Prophecies        | Fateweaver                    | Complete 100 Prophecies.                                | Account   |     50 | **Broadcast to all**       |
| Prophecies        | Written in the Stars          | Complete every weekly Prophecy during one weekly cycle. | Account   |    100 | **Broadcast to all**       |
| Guild             | Strength in Numbers           | Join a guild.                                           | Character |      5 | Player-only system message |
| Guild             | Order Keeper                  | Complete 100 guild orders.                              | Account   |     50 | Player-only system message |
| Guild             | Mission Accomplished          | Complete 1 guild mission with your guild.               | Character |     15 | Player-only system message |
| Guild             | Pillar of the Guild           | Generate 10,000 Guild Supplies.                         | Account   |    100 | **Broadcast to all**       |
| Marketplace       | First Sale                    | Complete your first Marketplace sale.                   | Account   |      5 | Player-only system message |
| Marketplace       | Merchant                      | Complete 100 Marketplace sales.                         | Account   |     50 | Player-only system message |
| Progression       | Soulstone Spark               | Purchase your first Soulstone upgrade.                  | Account   |      5 | Player-only system message |
| Progression       | Soulstone Sovereign           | Max every available Soulstone upgrade.                  | Account   |    150 | **Broadcast to all**       |
| Dungeons          | Dungeon Master                | Reach mastery level 10 in any dungeon family.           | Account   |    100 | **Broadcast to all**       |
| Achievements      | Trophy Cabinet                | Unlock 25 achievements.                                 | Account   |     25 | Player-only system message |
| Achievements      | Completionist                 | Unlock every active non-hidden achievement.             | Account   |    250 | **Broadcast to all**       |
| Titles            | Name of Renown                | Unlock 10 titles.                                       | Account   |     50 | Player-only system message |
| Colosseum         | Tournament Tested             | Complete your first Colosseum tournament.               | Character |     25 | Player-only system message |
| Colosseum         | Winner Winner, Chicken Dinner | Win the Colosseum tournament.                           | Character |    100 | **Broadcast to all**       |
| Champion's Market | Glory Spender                 | Purchase your first reward from the Champion's Market.  | Character |     10 | Player-only system message |
| Hidden            | Empty-Handed                  | Clear a dungeon after entering without a weapon.        | Character |    100 | **Broadcast to all**       |

### Implementation notes

1. Prophecy, guild, Marketplace, Soulstone, mastery, tournament, and Champion's Market activity now records achievement progress at the gameplay boundary.
2. Empty-Handed snapshots the weapon state when a dungeon begins, so changing equipment during the run cannot alter eligibility.
3. Trophy Cabinet, Completionist, and Name of Renown synchronize from tracked account achievement/title state, including unlocks made in the current transaction.
4. Rat Food and Trap Magnet remain cataloged but unwired because the current combat event does not define "significantly stronger" and the current dungeon system has no trap rooms or trap event.

## Maintenance notes

- Achievement definitions and message templates are authored in `LL/src/API/API.LL/Data/achievements/`.
- A non-empty `globalSystemMessageTemplate` makes an unlock a world broadcast; otherwise it remains a player-only system message.
- The delivery behavior is implemented by `AchievementService.PublishUnlockAnnouncementsAsync` and `AchievementSystemChatPublisher`.
- Update this document whenever an achievement definition, requirement, scope, or message template changes.
