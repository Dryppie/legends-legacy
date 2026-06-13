import { Component, computed } from '@angular/core';
import { NgFor } from '@angular/common';
import { DungeonCardComponent } from '../../../../../shared/components/dungeons/dungeon-card/dungeon-card.component';
import { DungeonPreviewData } from '../../../../../shared/models/Dtos/dungeons/dungeonPreviewData';
import { DungeonDifficulty } from '../../../../../shared/models/enums/dungeonDifficulty';
import { DungeonStateService } from '../../../../../core/services/api/dungeon/dungeon-state.service';

const dungeonPresentation: Record<string, Partial<DungeonPreviewData>> = {
  goblin_mines: {
    number: '1',
    heroImage: 'entities/optimized/hobgoblin.webp',
    lore: 'The goblins have mined deep into cursed stone, guarding ancient relics.',
    requiredLevel: 5,
    dailyEntries: 1,
    keyItem: {
      name: 'Goblin Sigil',
      have: 0,
      need: 1,
    },
    unlockedDifficulties: [
      DungeonDifficulty.Normal,
      DungeonDifficulty.Heroic,
    ],
  },
  forgotten_catacombs: {
    number: '2',
    heroImage: 'entities/optimized/skeleton_warrior.webp',
    lore: 'An ancient burial site where the dead rise beneath soot-covered stone.',
    requiredLevel: 10,
    dailyEntries: 2,
    keyItem: {
      name: 'Catacomb Sigil',
      have: 1,
      need: 1,
    },
    unlockedDifficulties: [DungeonDifficulty.Normal],
  },
  hives_abyss: {
    number: '3',
    heroImage: 'entities/optimized/frost_warg.webp',
    lore: 'A living cave overtaken by roots, spores, and ancient territorial beasts.',
    requiredLevel: 20,
    dailyEntries: 1,
    keyItem: {
      name: 'Hive Sigil',
      have: 0,
      need: 1,
    },
    unlockedDifficulties: [
      DungeonDifficulty.Normal,
      DungeonDifficulty.Heroic,
      DungeonDifficulty.Mythic,
    ],
  },
};

@Component({
  selector: 'app-dungeons',
  standalone: true,
  imports: [DungeonCardComponent, NgFor],
  templateUrl: './dungeons.component.html',
})
export class DungeonsComponent {
  dungeons = computed(() =>
    this.dungeonState.dungeons().map((dungeon, index) => {
      const presentation =
        dungeonPresentation[this.getDungeonFamilyId(dungeon.id)] ?? {};

      return {
        ...dungeon,
        ...presentation,
        number: presentation.number ?? index + 1,
        heroImage: presentation.heroImage ?? 'entities/optimized/hobgoblin.webp',
        lore: presentation.lore ?? '',
        requiredLevel: presentation.requiredLevel ?? 1,
        roomsRange: dungeon.roomsRange ?? [
          (dungeon as DungeonPreviewData & { minRooms?: number }).minRooms ?? 0,
          (dungeon as DungeonPreviewData & { maxRooms?: number }).maxRooms ?? 0,
        ],
        unlockedDifficulties: presentation.unlockedDifficulties ?? [
          DungeonDifficulty.Normal,
        ],
      } as DungeonPreviewData;
    }),
  );

  constructor(private readonly dungeonState: DungeonStateService) {}

  private getDungeonFamilyId(dungeonId: string): string {
    return dungeonId.replace(/_(i|ii|iii)$/i, '');
  }
}
