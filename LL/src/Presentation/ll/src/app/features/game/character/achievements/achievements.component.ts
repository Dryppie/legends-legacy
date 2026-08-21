import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, computed, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin, tap } from 'rxjs';
import { AchievementService } from '../../../../core/services/api/achievements/achievement.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { StateSyncCoordinator } from '../../../../core/services/real-time/game-realtime/state-sync-coordinator.service';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { RegularButtonComponent } from '../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import {
  DropdownComponent,
  DropdownOption,
  DropdownSelection,
} from '../../../../shared/components/custom-components/dropdown/dropdown.component';
import {
  AchievementCategory,
  AchievementDto,
  AchievementOverviewDto,
  TitleDto,
  TitleDisplayPosition,
} from '../../../../shared/models/achievement';

type AchievementTab = AchievementCategory | 'All';
type AchievementStateFilter = 'All' | 'Unlocked' | 'Locked';
type AchievementSortMode = 'Progress' | 'Points' | 'Name' | 'Recent';
type TitleStateFilter = 'All' | 'Unlocked' | 'Locked';
type CollectionView = 'Achievements' | 'Titles';

export interface AchievementListItem extends AchievementDto {
  chainPosition: number;
  chainLength: number;
}

export function collapseAchievementChains(
  achievements: readonly AchievementDto[],
  selection: 'current' | 'latestCompleted' = 'current',
): AchievementListItem[] {
  const chains = new Map<string, AchievementDto[]>();

  for (const achievement of achievements) {
    const chainKey = [
      achievement.category,
      achievement.scope,
      achievement.requirementType,
      achievement.requirementTarget ?? '',
    ].join('|');
    chains.set(chainKey, [...(chains.get(chainKey) ?? []), achievement]);
  }

  return [...chains.values()].flatMap((chain) => {
    const ordered = [...chain].sort(
      (a, b) =>
        a.requiredAmount - b.requiredAmount || a.key.localeCompare(b.key),
    );
    const current =
      selection === 'latestCompleted'
        ? [...ordered].reverse().find((achievement) => achievement.isCompleted)
        : (ordered.find((achievement) => !achievement.isCompleted) ??
          ordered[ordered.length - 1]);
    if (!current) return [];

    const chainPosition = ordered.indexOf(current) + 1;

    return {
      ...current,
      chainPosition,
      chainLength: ordered.length,
    };
  });
}

@Component({
  selector: 'app-achievements',
  host: { class: 'block h-full min-h-0' },
  imports: [
    NgClass,
    NgFor,
    NgIf,
    FormsModule,
    DefaultHeaderComponent,
    DropdownComponent,
  ],
  templateUrl: './achievements.component.html',
})
export class AchievementsComponent implements OnInit, OnDestroy {
  readonly collectionViews: CollectionView[] = ['Achievements', 'Titles'];
  readonly categories: AchievementTab[] = [
    'All',
    'General',
    'Combat',
    'Essences',
    'Dungeons',
    'Crafting',
    'Colosseum',
    'Guild',
    'Prophecies',
    'Economy',
    'Hidden',
    'Legacy',
  ];

  readonly activeCategory = signal<AchievementTab>('All');
  readonly overview = signal<AchievementOverviewDto | null>(null);
  readonly achievements = signal<AchievementDto[]>([]);
  readonly titles = signal<TitleDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly search = signal('');
  readonly activeView = signal<CollectionView>('Achievements');
  readonly achievementState = signal<AchievementStateFilter>('All');
  readonly sortMode = signal<AchievementSortMode>('Progress');
  readonly titleSearch = signal('');
  readonly titleState = signal<TitleStateFilter>('All');
  readonly titleDisplayPosition = signal<TitleDisplayPosition>('Prefix');
  readonly titlePositionUpdating = signal(false);
  private readonly unregisterStateSync: () => void;

  readonly achievementStates: AchievementStateFilter[] = [
    'All',
    'Unlocked',
    'Locked',
  ];
  readonly sortModes: AchievementSortMode[] = [
    'Progress',
    'Points',
    'Name',
    'Recent',
  ];
  readonly titleStates: TitleStateFilter[] = ['All', 'Unlocked', 'Locked'];
  readonly titleDisplayPositions: TitleDisplayPosition[] = ['Prefix', 'Suffix'];
  readonly achievementStateOptions: readonly DropdownOption<AchievementStateFilter>[] =
    this.achievementStates.map((state) => ({ label: state, value: state }));
  readonly sortModeOptions: readonly DropdownOption<AchievementSortMode>[] =
    this.sortModes.map((mode) => ({ label: mode, value: mode }));

  readonly filteredAchievements = computed(() => {
    const category = this.activeCategory();
    const search = this.search().trim().toLowerCase();
    const state = this.achievementState();
    const sort = this.sortMode();

    let candidateAchievements = this.achievements();
    if (category !== 'All') {
      candidateAchievements = candidateAchievements.filter(
        (achievement) => achievement.category === category,
      );
    }

    let achievements = collapseAchievementChains(
      candidateAchievements,
      state === 'Unlocked' ? 'latestCompleted' : 'current',
    );

    if (state === 'Unlocked') {
      achievements = achievements.filter(
        (achievement) => achievement.isCompleted,
      );
    } else if (state === 'Locked') {
      achievements = achievements.filter(
        (achievement) => !achievement.isCompleted,
      );
    }

    if (search) {
      achievements = achievements.filter((achievement) =>
        [
          achievement.name,
          achievement.description,
          achievement.rewardTitleName ?? '',
          achievement.category,
          achievement.rarity,
        ]
          .join(' ')
          .toLowerCase()
          .includes(search),
      );
    }

    return [...achievements].sort((a, b) => {
      if (a.isCompleted !== b.isCompleted) {
        return a.isCompleted ? 1 : -1;
      }

      switch (sort) {
        case 'Points':
          return b.points - a.points || a.name.localeCompare(b.name);
        case 'Name':
          return a.name.localeCompare(b.name);
        case 'Recent':
          return this.completedAtTicks(b) - this.completedAtTicks(a);
        case 'Progress':
        default:
          return (
            this.progressPercent(b) - this.progressPercent(a) ||
            b.points - a.points
          );
      }
    });
  });

  readonly unlockedTitles = computed(() =>
    this.titles().filter((title) => title.isUnlocked),
  );

  readonly lockedTitles = computed(() =>
    this.titles().filter((title) => !title.isUnlocked),
  );

  readonly equippedTitle = computed(
    () => this.titles().find((title) => title.isEquipped) ?? null,
  );

  readonly filteredTitles = computed(() => {
    const search = this.titleSearch().trim().toLowerCase();
    const state = this.titleState();

    let titles = this.titles();
    if (state === 'Unlocked') {
      titles = titles.filter((title) => title.isUnlocked);
    } else if (state === 'Locked') {
      titles = titles.filter((title) => !title.isUnlocked);
    }

    if (search) {
      titles = titles.filter((title) =>
        [
          title.name,
          title.description,
          title.preview,
          title.prefixPreview,
          title.suffixPreview,
          title.rarity,
          title.scope,
        ]
          .join(' ')
          .toLowerCase()
          .includes(search),
      );
    }

    return [...titles].sort((a, b) => {
      if (a.isEquipped !== b.isEquipped) {
        return a.isEquipped ? -1 : 1;
      }

      if (a.isUnlocked !== b.isUnlocked) {
        return a.isUnlocked ? -1 : 1;
      }

      return a.name.localeCompare(b.name);
    });
  });

  constructor(
    private readonly achievementsApi: AchievementService,
    private readonly characterState: CharacterStateService,
    stateSync: StateSyncCoordinator,
  ) {
    this.unregisterStateSync = stateSync.register(
      'achievements',
      'achievement-page',
      () => this.synchronize(),
    );
  }

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.unregisterStateSync();
  }

  load(): void {
    this.synchronize().subscribe({
      error: (err) => this.error.set(err.message),
    });
  }

  private synchronize() {
    this.loading.set(true);
    this.error.set('');

    return forkJoin({
      overview: this.achievementsApi.getOverview(),
      achievements: this.achievementsApi.getAchievements(),
      titles: this.achievementsApi.getTitles(),
    }).pipe(
      tap(({ overview, achievements, titles }) => {
        this.overview.set(overview);
        this.achievements.set(achievements);
        this.applyTitles(titles);
      }),
      finalize(() => this.loading.set(false)),
    );
  }

  setCategory(category: AchievementTab): void {
    this.activeCategory.set(category);
  }

  setAchievementStateFromDropdown(
    selection: DropdownSelection<AchievementStateFilter>,
  ): void {
    this.achievementState.set(selection.main);
  }

  setSortModeFromDropdown(
    selection: DropdownSelection<AchievementSortMode>,
  ): void {
    this.sortMode.set(selection.main);
  }

  equip(title: TitleDto): void {
    if (!this.canEquip(title)) {
      return;
    }

    this.achievementsApi
      .equipTitle(title.key, this.titleDisplayPosition())
      .subscribe({
        next: (equippedTitle) => {
          this.characterState.updateEquippedTitle(equippedTitle);
        },
        error: (err) => this.error.set(err.message),
      });
  }

  setTitleDisplayPosition(position: TitleDisplayPosition): void {
    if (
      position === this.titleDisplayPosition() ||
      this.titlePositionUpdating()
    ) {
      return;
    }

    const previousPosition = this.titleDisplayPosition();
    this.titleDisplayPosition.set(position);

    const equippedTitle = this.equippedTitle();
    if (!equippedTitle) {
      return;
    }

    this.titlePositionUpdating.set(true);
    this.error.set('');

    this.achievementsApi
      .equipTitle(equippedTitle.key, position)
      .pipe(finalize(() => this.titlePositionUpdating.set(false)))
      .subscribe({
        next: (updatedTitle) => {
          this.characterState.updateEquippedTitle(updatedTitle);
          this.titles.update((titles) =>
            titles.map((title) =>
              title.key === equippedTitle.key
                ? {
                    ...title,
                    displayPosition: position,
                    preview:
                      position === 'Prefix'
                        ? title.prefixPreview
                        : title.suffixPreview,
                  }
                : title,
            ),
          );
        },
        error: (err) => {
          this.titleDisplayPosition.set(previousPosition);
          this.error.set(err.message);
        },
      });
  }

  unequip(): void {
    this.achievementsApi.unequipTitle().subscribe({
      next: () => {
        this.characterState.updateEquippedTitle(null);
      },
      error: (err) => this.error.set(err.message),
    });
  }

  progressPercent(achievement: AchievementDto): number {
    if (achievement.isCompleted) {
      return 100;
    }

    if (!achievement.requiredAmount) {
      return 0;
    }

    return Math.min(
      100,
      Math.round(
        (achievement.currentAmount / achievement.requiredAmount) * 100,
      ),
    );
  }

  categoryProgress(category: AchievementTab): string {
    if (category === 'All') {
      return '';
    }

    const summary = this.overview()?.categorySummaries.find(
      (item) => item.category === category,
    );
    return summary ? `${summary.unlocked}/${summary.available}` : '0/0';
  }

  clearFilters(): void {
    this.search.set('');
    this.achievementState.set('All');
    this.sortMode.set('Progress');
  }

  clearTitleFilters(): void {
    this.titleSearch.set('');
    this.titleState.set('All');
  }

  titlePreview(title: TitleDto): string {
    return this.titleDisplayPosition() === 'Prefix'
      ? title.prefixPreview || title.preview
      : title.suffixPreview || title.preview;
  }

  canEquip(title: TitleDto): boolean {
    return (
      title.isUnlocked && !title.isEquipped && !this.titlePositionUpdating()
    );
  }

  equipButtonText(title: TitleDto): string {
    if (!title.isUnlocked) {
      return 'Locked';
    }

    if (title.isEquipped) {
      return 'Equipped';
    }

    return 'Equip';
  }

  private completedAtTicks(achievement: AchievementDto): number {
    return achievement.completedAt
      ? new Date(achievement.completedAt).getTime()
      : 0;
  }

  private applyTitles(titles: TitleDto[]): void {
    this.titles.set(titles);
    const equippedTitle = titles.find((title) => title.isEquipped);
    if (equippedTitle) {
      this.titleDisplayPosition.set(equippedTitle.displayPosition);
    }
  }
}
