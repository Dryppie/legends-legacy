import { Component } from '@angular/core';
import { NgFor } from '@angular/common';
import { ItemType } from '../../../../../shared/models/enums/itemType';
import { Rarity } from '../../../../../shared/models/enums/rarity';
import { DungeonCardComponent } from '../../../../../shared/components/dungeons/dungeon-card/dungeon-card.component';
import { DungeonPreviewData } from '../../../../../shared/models/Dtos/dungeons/dungeonPreviewData';
import { DungeonDifficulty } from '../../../../../shared/models/enums/dungeonDifficulty';

@Component({
  selector: 'app-dungeons',
  standalone: true,
  imports: [DungeonCardComponent, NgFor],
  templateUrl: './dungeons.component.html',
})
export class DungeonsComponent {
  dungeons: DungeonPreviewData[] = [
    {
      id: 'goblin_mines',
      number: '1',
      title: 'Goblin Mines',
      heroImage: 'entities/optimized/hobgoblin.webp',
      lore: 'The goblins have mined deep into cursed stone, guarding ancient relics.',
      requiredLevel: 5,
      dailyEntries: 1,
      keyItem: {
        name: 'Goblin Key',
        have: 0,
        need: 1,
      },
      roomsRange: [5, 8],
      unlockedDifficulties: [
        DungeonDifficulty.Normal,
        DungeonDifficulty.Heroic,
      ],
      rewards: [
        {
          id: 'goblin-helmet',
          itemBase: {
            id: 'goblin-helmet',
            name: 'Goblin Helmet',
            rarity: Rarity.Common,
            description: 'A sturdy helmet made from goblin metal.',
            itemType: ItemType.Consumable,
            stackable: false,
          },
        },
      ],
    },
    {
      id: 'crypt_of_ash',
      number: '2',
      title: 'Crypt of Ash',
      heroImage: 'entities/optimized/skeleton_warrior.webp',
      lore: 'An ancient burial site where the dead rise beneath soot-covered stone.',
      requiredLevel: 10,
      dailyEntries: 2,
      keyItem: {
        name: 'Ashen Sigil',
        have: 1,
        need: 1,
      },
      roomsRange: [6, 10],
      unlockedDifficulties: [DungeonDifficulty.Normal],
      rewards: [
        {
          id: 'ashen-bone',
          itemBase: {
            id: 'ashen-bone',
            name: 'Ashen Bone',
            rarity: Rarity.Uncommon,
            description: 'A scorched bone infused with necrotic residue.',
            itemType: ItemType.Consumable,
            stackable: true,
          },
        },
      ],
    },
    {
      id: 'verdant_hollow',
      number: '3',
      title: 'Verdant Hollow',
      heroImage: 'entities/optimized/frost_warg.webp',
      lore: 'A living cave overtaken by roots, spores, and ancient territorial beasts.',
      requiredLevel: 20,
      dailyEntries: 1,
      keyItem: {
        name: 'Rootbound Totem',
        have: 0,
        need: 1,
      },
      roomsRange: [8, 12],
      unlockedDifficulties: [
        DungeonDifficulty.Normal,
        DungeonDifficulty.Heroic,
        DungeonDifficulty.Mythic,
      ],
      rewards: [
        {
          id: 'verdant-core',
          itemBase: {
            id: 'verdant-core',
            name: 'Verdant Core',
            rarity: Rarity.Rare,
            description: 'A pulsating heart of overgrown magic.',
            itemType: ItemType.Consumable,
            stackable: false,
          },
        },
      ],
    },
  ];
}
