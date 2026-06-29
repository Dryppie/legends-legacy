import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, computed, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { AchievementService } from '../../../../core/services/api/achievements/achievement.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { RegularButtonComponent } from '../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
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

@Component({
  selector: 'app-achievements',
  standalone: true,
  imports: [
    NgClass,
    NgFor,
    NgIf,
    FormsModule,
    DefaultHeaderComponent,
    RegularButtonComponent,
  ],
  templateUrl: './achievements.component.html',
})
export class AchievementsComponent implements OnInit {
  readonly categories: AchievementTab[] = [
    'All',
    'General',
    'Combat',
    'Essences',
    'Dungeons',
    'Crafting',
    'Colosseum',
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
  readonly achievementState = signal<AchievementStateFilter>('All');
  readonly sortMode = signal<AchievementSortMode>('Progress');
  readonly titleSearch = signal('');
  readonly titleState = signal<TitleStateFilter>('All');
  readonly titleDisplayPosition = signal<TitleDisplayPosition>('Prefix');

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

  readonly filteredAchievements = computed(() => {
    const category = this.activeCategory();
    const search = this.search().trim().toLowerCase();
    const state = this.achievementState();
    const sort = this.sortMode();

    let achievements = this.achievements();
    if (category !== 'All') {
      achievements = achievements.filter(
        (achievement) => achievement.category === category,
      );
    }

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
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');

    this.achievementsApi.getOverview().subscribe({
      next: (overview) => this.overview.set(overview),
      error: (err) => this.error.set(err.message),
    });

    this.achievementsApi
      .getAchievements()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (achievements) => this.achievements.set(achievements),
        error: (err) => this.error.set(err.message),
      });

    this.refreshTitles();
  }

  setCategory(category: AchievementTab): void {
    this.activeCategory.set(category);
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
          this.refreshTitles();
          this.characterState.refresh();
        },
        error: (err) => this.error.set(err.message),
      });
  }

  unequip(): void {
    this.achievementsApi.unequipTitle().subscribe({
      next: () => {
        this.characterState.updateEquippedTitle(null);
        this.refreshTitles();
        this.characterState.refresh();
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
      title.isUnlocked &&
      (!title.isEquipped ||
        title.displayPosition !== this.titleDisplayPosition())
    );
  }

  equipButtonText(title: TitleDto): string {
    if (!title.isUnlocked) {
      return 'Locked';
    }

    if (title.isEquipped) {
      return title.displayPosition === this.titleDisplayPosition()
        ? 'Equipped'
        : 'Update';
    }

    return 'Equip';
  }

  private completedAtTicks(achievement: AchievementDto): number {
    return achievement.completedAt
      ? new Date(achievement.completedAt).getTime()
      : 0;
  }

  private refreshTitles(): void {
    this.achievementsApi.getTitles().subscribe({
      next: (titles) => this.titles.set(titles),
      error: (err) => this.error.set(err.message),
    });
  }
}
