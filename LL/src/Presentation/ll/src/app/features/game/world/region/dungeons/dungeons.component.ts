import { Component, OnInit, computed, signal } from '@angular/core';
import { NgClass, NgFor, NgIf } from '@angular/common';
import { DungeonCardComponent } from '../../../../../shared/components/dungeons/dungeon-card/dungeon-card.component';
import { DungeonPreviewData } from '../../../../../shared/models/Dtos/dungeons/dungeonPreviewData';
import { DungeonDifficulty } from '../../../../../shared/models/enums/dungeonDifficulty';
import { DungeonStateService } from '../../../../../core/services/api/dungeon/dungeon-state.service';
import {
  DungeonRecordEntryData,
  DungeonRecordsData,
  DungeonTierRecordsData,
} from '../../../../../shared/models/Dtos/dungeons/dungeonRecordsData';
import { finalize } from 'rxjs/operators';
import { CharacterTagComponent } from '../../../../../shared/components/character/character-tag/character-tag.component';
import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';

type DungeonLeaderboardMode = 'firstClears' | 'mostClears' | 'recentClears';

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
    imports: [DungeonCardComponent, NgFor, NgIf, NgClass, CharacterTagComponent],
    templateUrl: './dungeons.component.html'
})
export class DungeonsComponent implements OnInit {
  selectedRecordsDungeon = signal<DungeonPreviewData | null>(null);
  recordsData = signal<DungeonRecordsData | null>(null);
  recordsLoading = signal(false);
  recordsError = signal<string | null>(null);
  selectedLeaderboardMode = signal<DungeonLeaderboardMode>('firstClears');

  readonly leaderboardTabs: {
    mode: DungeonLeaderboardMode;
    label: string;
    description: string;
  }[] = [
    {
      mode: 'firstClears',
      label: 'First Clears',
      description: 'Earliest players to clear each difficulty.',
    },
    {
      mode: 'mostClears',
      label: 'Most Clears',
      description: 'Players with the highest clear counts.',
    },
    {
      mode: 'recentClears',
      label: 'Recent Clears',
      description: 'Most recent players to finish a run.',
    },
  ];

  dungeons = computed(() =>
    this.groupDifficultyVariants(this.dungeonState.dungeons()),
  );

  constructor(
    private readonly dungeonState: DungeonStateService,
    characterState: CharacterStateService,
  ) {
    characterState.refreshIfDirty();
  }

  ngOnInit(): void {
    this.dungeonState.refresh();
  }

  openRecords(dungeon: DungeonPreviewData): void {
    this.selectedRecordsDungeon.set(dungeon);
    this.recordsData.set(null);
    this.recordsError.set(null);
    this.recordsLoading.set(true);
    this.selectedLeaderboardMode.set('firstClears');

    this.dungeonState
      .getDungeonRecords(dungeon.familyId ?? dungeon.id)
      .pipe(finalize(() => this.recordsLoading.set(false)))
      .subscribe({
        next: (records) => this.recordsData.set(records),
        error: (e) =>
          this.recordsError.set(e.message ?? 'Failed to load dungeon records'),
      });
  }

  selectLeaderboardMode(mode: DungeonLeaderboardMode): void {
    this.selectedLeaderboardMode.set(mode);
  }

  selectedLeaderboardDescription(): string {
    return (
      this.leaderboardTabs.find(
        (tab) => tab.mode === this.selectedLeaderboardMode(),
      )?.description ?? ''
    );
  }

  sortedTierRecords(tier: DungeonTierRecordsData): DungeonRecordEntryData[] {
    const records = [...tier.records];

    switch (this.selectedLeaderboardMode()) {
      case 'mostClears':
        return records.sort((a, b) => {
          const clears = b.totalClears - a.totalClears;
          if (clears !== 0) return clears;

          return (
            new Date(a.firstClearedAt).getTime() -
            new Date(b.firstClearedAt).getTime()
          );
        });

      case 'recentClears':
        return records.sort(
          (a, b) =>
            new Date(b.lastClearedAt).getTime() -
            new Date(a.lastClearedAt).getTime(),
        );

      default:
        return records.sort(
          (a, b) =>
            new Date(a.firstClearedAt).getTime() -
            new Date(b.firstClearedAt).getTime(),
        );
    }
  }

  recordMetricValue(record: DungeonRecordEntryData): string {
    switch (this.selectedLeaderboardMode()) {
      case 'mostClears':
        return record.totalClears.toString();

      case 'recentClears':
        return this.formatRecordDate(record.lastClearedAt);

      default:
        return this.formatRecordDate(record.firstClearedAt);
    }
  }

  recordMetricLabel(record: DungeonRecordEntryData): string {
    switch (this.selectedLeaderboardMode()) {
      case 'mostClears':
        return record.totalClears === 1 ? 'clear' : 'clears';

      case 'recentClears':
        return 'last clear';

      default:
        return 'first clear';
    }
  }

  closeRecords(): void {
    this.selectedRecordsDungeon.set(null);
    this.recordsData.set(null);
    this.recordsError.set(null);
    this.recordsLoading.set(false);
  }

  totalRecordClears(): number {
    return (
      this.recordsData()?.tiers.reduce(
        (total, tier) =>
          total +
          tier.records.reduce(
            (tierTotal, record) => tierTotal + record.totalClears,
            0,
          ),
        0,
      ) ?? 0
    );
  }

  formatRecordDate(value: string | null | undefined): string {
    if (!value) {
      return 'Never';
    }

    return new Intl.DateTimeFormat(undefined, {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    }).format(new Date(value));
  }

  private groupDifficultyVariants(
    dungeons: DungeonPreviewData[],
  ): DungeonPreviewData[] {
    const groups = new Map<string, DungeonPreviewData[]>();

    for (const dungeon of dungeons) {
      const familyId = dungeon.familyId ?? dungeon.id;
      groups.set(familyId, [...(groups.get(familyId) ?? []), dungeon]);
    }

    return Array.from(groups.entries())
      .flatMap(([familyId, variants], index) => {
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
      })
      .sort((a, b) => this.compareDungeons(a, b));
  }

  private compareDungeons(
    first: DungeonPreviewData,
    second: DungeonPreviewData,
  ): number {
    const numberSort =
      this.getDungeonSortValue(first) - this.getDungeonSortValue(second);
    if (numberSort !== 0) {
      return numberSort;
    }

    const levelSort = (first.requiredLevel ?? 0) - (second.requiredLevel ?? 0);
    if (levelSort !== 0) {
      return levelSort;
    }

    return first.title.localeCompare(second.title);
  }

  private getDungeonSortValue(dungeon: DungeonPreviewData): number {
    const parsedNumber = Number(dungeon.number);
    return Number.isFinite(parsedNumber)
      ? parsedNumber
      : Number.MAX_SAFE_INTEGER;
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
    switch (dungeon.difficulty?.toString().toLowerCase()) {
      case 'veteran':
      case 'heroic':
        return DungeonDifficulty.Heroic;
      case 'champion':
      case 'mythic':
        return DungeonDifficulty.Mythic;
      case 'novice':
      case 'normal':
        return DungeonDifficulty.Normal;
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
