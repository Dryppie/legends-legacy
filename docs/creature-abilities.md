# Creature catalogue

Tier-one creatures, their native abilities, collectible Essences, and spawn locations share one roster. `Data/combat/creature-abilities.json` maps each `monster.*` definition to its native `ability.creature.*` abilities. Every creature has an Essence drop table and collectible Essence item.

The catalogue contains 52 creature profiles and 105 native abilities: 53 active and 52 passive. It contains 53 Essences because Hobgoblin's two active abilities are represented by two Essence variants sharing its passive.

## Area locations

1. Lumo Ruins: Lumo Wisp, Lumo Sentinel, Goblin, Goblin Archer, Goblin Warrior
2. Blood Grove: Vampire Bat, Raven, Venomous Snake, Nightshade Blossom, Blood Zombie
3. Crystal Creek: Frost Imp, Crystal Wisp, Blue Slime, Transparent Slime, Moss Lizard
4. Moonlit Graves: Shadow Imp, Grave Hound, Lost Soul, Grave Wisp, Skeleton
5. Twilight Clearing: Pixie, Wood Nymph, Rainbow Slime, Enchanted Fairy, Illusion Fox
6. Old Forest: Thornback Boar, Hollow Stag, Treant Sapling, Glade Panther, Forest Spirit
7. Thornroot Hollow: Rotroot Shambler, Spider, Giant Spider, Venomous Spiderling, Blackjaw Spider
8. Embercap Burrows: Flame Imp, Smolder Rat, Cinder Beetle, Red Slime, Giant Worm
9. Moonveil Marsh: Bog Mite, Green Slime, Large Rat, Viper, Poisonous Rat
10. Duskmire Hollow: Rotfly Toad, Brown Slime, Cave Bat, Giant Bat, Undead

## Dungeon locations

- Goblin Mines: Goblin, Goblin Archer, Goblin Warrior, Goblin Shaman, Hobgoblin (boss)
- Forgotten Catacombs: Skeleton

Combat time uses ten ticks per second. Active abilities without a supplied cooldown use the catalogue default of 100 ticks (10 seconds); Sniper's Strike uses its authored 25-second cooldown.

Run `build/generate-creature-abilities.ps1` and then `build/generate-creature-essences.ps1` from the repository root after changing the authored roster.
