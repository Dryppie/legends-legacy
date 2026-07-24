import { Component } from '@angular/core';

interface RoadmapItem {
  title: string;
  description: string;
  estimatedRelease: Date;
}

@Component({
    selector: 'app-roadmap',
    imports: [],
    templateUrl: './roadmap.component.html'
})
export class RoadmapComponent {
  roadmapItems: RoadmapItem[] = [
    {
      title: '0.0.1 – Login System',
      description: 'Users can register and login.<br>',
      estimatedRelease: new Date(2024, 8, 1),
    },
    {
      title: '0.1.0 – Base interactions',
      description:
        'Player can perform actions – professions.<br>Map to interact with – Or simple dropdown menu / list of items.<br>Professions can level up and unlock new features related to the profession.<br><br>New Professions:<br>•	Fishing<br>•	Mining',
      estimatedRelease: new Date(2024, 9, 1),
    },
    {
      title: '0.2.0 – Combat, Inventory, and Equipment',
      description:
        'Player can engage in combat to gain character levels – player can level up attributes<br>Inventory to contain all items gained through combat and professions<br>Player can equip items<br><br>New Professions:<br>•	Foraging / Herbalism<br>•	Woodcutting<br><br>New Monsters:<br>•	Four news monsters on every tier from Tier 1 to Tier 5',
      estimatedRelease: new Date(2024, 10, 1),
    },
    {
      title: '0.3.0 – Abilities and Essences',
      description:
        'Active and passive abilities for the player to use and pick from<br>Monsters can drop essences<br>Essences contain two abilities – one active, one passive<br><br>New essences for all current monsters, each with two abilities<br><br>New Professions:<br>•	Hunting<br>•	Farming<br><br>New Monsters and loot:<br>•	Two new monsters on every tier from Tier 1 to Tier 7<br>•	One new monster on Tier 8 and Tier 9',
      estimatedRelease: new Date(2024, 11, 1),
    },
    {
      title: '0.4.0 – Achievements and Titles',
      description:
        'Players can complete a multitude of different achievements<br>Players can earn Titles, with each giving certain bonus points to attributes<br><br>Achievements: 30 new achievements<br><br>Titles: 20 new titles<br><br>New Professions:<br>•	Smithing<br>•	Cooking<br><br>New Monsters and loot:<br>•	Two new monsters on every tier from Tier 1 to Tier 7<br>•	One new monster on Tier 8 and Tier 9',
      estimatedRelease: new Date(2025, 0, 1),
    },
    {
      title: '0.5.0 – Dungeons and Parties',
      description:
        'Players can fight in a dungeon, with some requiring solo play, while others require a party<br>Access to a dungeon is gained through the use of a Dungeon Key (other item name? map, scroll?)<br>A player can create or join a party. A party can contain up to 4 players<br><br>Achievements: 10 different achievements<br><br>Titles: 5 different titles<br><br>Professions:<br>•	Tailoring<br>•	Alchemy<br><br>New Monsters and loot:<br>•	Two new monsters on every tier from Tier 1 to Tier 7<br>•	One new monster on Tier 8 and Tier 9',
      estimatedRelease: new Date(2025, 1, 1),
    },
    {
      title: '0.6.0 – Quests, NPCs, and Echoes',
      description:
        'Introduction tutorial to get started with the game<br>NPCs to interact with<br>Echoes are special items that can be obtained from unique monsters or NPCs. Echo fragments can be combined into a complete Echo<br>An Echo can be put onto one’s weapon, giving it a special boost.<br>Multiple of the same Echo can upgrade an Echo to +1<br><br>Quests: 8 Quests for the player to complete<br><br>NPCs: 10 different NPCs<br><br>Echoes: 5 different Echoes<br><br>Achievements: 10 different achievements<br><br>Titles: 5 different titles<br><br>Professions:<br>•	Construction<br>•	Jewelry<br><br>New Monsters and loot:<br>•	Two new monsters on every tier from Tier 1 to Tier 7<br>•	One new monster on Tier 8 and Tier 9',
      estimatedRelease: new Date(2025, 2, 1),
    },
    {
      title: '0.7.0 – Town and Buildings',
      description:
        'Towns will be located across the map, with each location having certain advantages of disadvantages<br>Towns have unique NPCs and quests.<br>A town has different buildings, each with their own purpose.<br>Towns: Make 8 unique towns<br>Buildings: make 10 unique buildings<br>Quests: 2 Quests for the player to complete<br>NPCs: 5 different NPCs<br>Echoes: 5 different Echoes<br>Achievements: 10 different achievements<br>Titles: 5 different titles<br>Professions:<br>•	Merchantry<br>•	Performance<br>Combat – Monsters and loot:<br>•	Two new monsters on every tier from Tier 1 to Tier 7<br>•	One new monster on Tier 8 and Tier 9',
      estimatedRelease: new Date(2025, 3, 1),
    },
    {
      title: '0.8.0 – Guilds',
      description:
        'Towns will be located across the map, with each location having certain advantages of disadvantages<br>Towns have unique NPCs and quests.<br>A town has different buildings, each with their own purpose.<br>Towns: Make 8 unique towns<br>Buildings: make 10 unique buildings<br>Quests: 2 Quests for the player to complete<br>NPCs: 5 new NPCs<br><br>Echoes: 5 new Echoes<br><br>Achievements: 10 new achievements<br><br>Titles: 5 new titles<br><br>Professions:<br>•	Merchantry<br>•	Performance<br><br>New Monsters and loot:<br>•	Two new monsters on every tier from Tier 1 to Tier 7<br>•	One new monster on Tier 8 and Tier 9',
      estimatedRelease: new Date(2025, 4, 1),
    },
    {
      title: '0.9.0 – Colosseum? Guild Raids? Unsure',
      description:
        '??? ??? ???<br>Quests: 2 Quests for the player to complete<br>NPCs: 5 different NPCs<br>Echoes: 5 different Echoes<br>Achievements: 10 different achievements<br>Titles: 5 different titles<br>Combat – Monsters and loot:<br>•	Two new monsters on every tier from Tier 1 to Tier 7<br>•	One new monster on Tier 8 and Tier 9',
      estimatedRelease: new Date(2025, 5, 1),
    },
    {
      title: '1.0.0	– World Tower',
      description:
        'The World Tower can be entered by anyone. Each floor contain a boss and it needs to be defeated before players can progress to the next floor. New areas are unlocked after certain milestones have been cleared, such as every 5th or 10th floor<br>Once a floor has been defeated, players can farm monsters on that floor. After x amount of monsters have been defeated, that players can progress to the next floor, but never past the one containing a boss monster<br>World Tower: 50 Floors<br>Quests: 2 Quests for the player to complete<br>NPCs: 5 different NPCs<br>Echoes: 5 different Echoes<br>Achievements: 10 different achievements<br>Titles: 5 different titles<br>Combat – Monsters and loot:<br>•	Two new monsters on every tier from Tier 1 to Tier 7<br>•	One new monster on Tier 8 and Tier 9',
      estimatedRelease: new Date(2025, 6, 1),
    },
    {
      title: '1.1.0	– World Boss',
      description:
        'World Tower: 25 Floors<br>Quests: 2 Quests for the player to complete<br>NPCs: 5 different NPCs<br>Achievements: 10 different achievements<br>Titles: 5 different titles<br>Combat – Monsters and loot:<br>•	Two new monsters on every tier from Tier 1 to Tier 7<br>•	One new monster on Tier 8 and Tier 9',
      estimatedRelease: new Date(2025, 7, 1),
    },
    {
      title: '1.2.0 – ',
      description:
        'World Tower: 25 Floors<br>Quests: 2 Quests for the player to complete<br>NPCs: 5 different NPCs<br>Echoes: 5 different Echoes',
      estimatedRelease: new Date(2025, 8, 1),
    },
  ];
}
