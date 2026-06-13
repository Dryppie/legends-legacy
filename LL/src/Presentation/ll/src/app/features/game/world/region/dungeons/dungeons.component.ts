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
    this.groupDifficultyVariants(this.dungeonState.dungeons()),
  );

  constructor(private readonly dungeonState: DungeonStateService) {}

  private groupDifficultyVariants(
    dungeons: DungeonPreviewData[],
  ): DungeonPreviewData[] {
    const groups = new Map<string, DungeonPreviewData[]>();

    for (const dungeon of dungeons) {
      const familyId = dungeon.familyId ?? dungeon.id;
      groups.set(familyId, [...(groups.get(familyId) ?? []), dungeon]);
    }

    return Array.from(groups.entries()).flatMap(
      ([familyId, variants], index) => {
        const presentation = dungeonPresentation[familyId] ?? {};
        const variantMap = this.createVariantMap(variants);
        const normalVariant =
          variantMap[DungeonDifficulty.Normal] ?? variants[0] ?? null;
        const selectedBase = normalVariant ?? variants[0];
        if (!selectedBase) return [];

        return {
          ...selectedBase,
          ...presentation,
          id: normalVariant?.id ?? selectedBase.id,
          familyId,
          familyTitle: selectedBase.familyTitle ?? selectedBase.title,
          title: selectedBase.familyTitle ?? selectedBase.title,
          number: presentation.number ?? index + 1,
          heroImage:
            presentation.heroImage ?? 'entities/optimized/hobgoblin.webp',
          lore: presentation.lore ?? '',
          requiredLevel: presentation.requiredLevel ?? 1,
          roomsRange: selectedBase.roomsRange ?? [
            (selectedBase as DungeonPreviewData & { minRooms?: number })
              .minRooms ?? 0,
            (selectedBase as DungeonPreviewData & { maxRooms?: number })
              .maxRooms ?? 0,
          ],
          unlockedDifficulties: this.getUnlockedDifficulties(variantMap),
          difficultyVariants: variantMap,
        } as DungeonPreviewData;
      },
    );
  }

  private createVariantMap(
    variants: DungeonPreviewData[],
  ): Partial<Record<DungeonDifficulty, DungeonPreviewData>> {
    return variants.reduce<Partial<Record<DungeonDifficulty, DungeonPreviewData>>>(
      (map, dungeon) => {
        map[this.getDifficulty(dungeon)] = dungeon;
        return map;
      },
      {},
    );
  }

  private getUnlockedDifficulties(
    variants: Partial<Record<DungeonDifficulty, DungeonPreviewData>>,
  ): DungeonDifficulty[] {
    return [
      DungeonDifficulty.Normal,
      DungeonDifficulty.Heroic,
      DungeonDifficulty.Mythic,
    ].filter((difficulty) => !!variants[difficulty]);
  }

  private getDifficulty(dungeon: DungeonPreviewData): DungeonDifficulty {
    if (dungeon.difficulty) {
      return dungeon.difficulty;
    }

    switch (dungeon.grade) {
      case 'Grade II':
        return DungeonDifficulty.Heroic;
      case 'Grade III':
        return DungeonDifficulty.Mythic;
      default:
        return DungeonDifficulty.Normal;
    }
  }
}
