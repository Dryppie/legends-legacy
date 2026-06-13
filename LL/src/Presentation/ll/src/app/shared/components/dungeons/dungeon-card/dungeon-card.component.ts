import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { RegularButtonComponent } from '../../custom-components/buttons/regular-button/regular-button.component';
import { NgClass, NgFor, NgIf } from '@angular/common';
import { ItemComponent } from '../../item/item.component';
import { DungeonStateService } from '../../../../core/services/api/dungeon/dungeon-state.service';
import {
  DungeonPreviewData,
  DungeonPreviewReward,
} from '../../../models/Dtos/dungeons/dungeonPreviewData';
import { DungeonDifficulty } from '../../../models/enums/dungeonDifficulty';
import { Router } from '@angular/router';

interface RewardGroup {
  title: string;
  rewards: DungeonPreviewReward[];
}

@Component({
  selector: 'app-dungeon-card',
  standalone: true,
  imports: [NgIf, NgFor, NgClass, RegularButtonComponent, ItemComponent],
  templateUrl: './dungeon-card.component.html',
})
export class DungeonCardComponent {
  @Input({ required: true }) previewData!: DungeonPreviewData;

  @Input() height = 176;
  @Input() cornerSize = 32;

  @Output() backEvent = new EventEmitter<void>();
  @Output() recordsRequested = new EventEmitter<DungeonPreviewData>();

  dungeonDifficulty = DungeonDifficulty;
  showPreview = signal(false);
  difficulty = signal<DungeonDifficulty>(DungeonDifficulty.Normal);

  constructor(
    private readonly dungeonState: DungeonStateService,
    private readonly router: Router,
  ) {}

  startDungeon() {
    if (!this.selectedCanEnter()) {
      return;
    }

    const selectedDungeon = this.selectedPreviewData();

    this.dungeonState.startDungeon(
      selectedDungeon.id,
      this.difficulty(),
      () => {
        void this.router.navigate(['/game/world/dungeon']);
      },
    );
  }

  togglePreview() {
    this.showPreview.set(!this.showPreview());
  }

  openRecords() {
    this.recordsRequested.emit(this.previewData);
  }

  selectDifficulty(d: DungeonDifficulty) {
    if (this.previewData.unlockedDifficulties.includes(d)) {
      this.difficulty.set(d);
    }
  }

  difficultyLabel(difficulty: DungeonDifficulty): string {
    switch (difficulty) {
      case DungeonDifficulty.Heroic:
        return 'Veteran';
      case DungeonDifficulty.Mythic:
        return 'Champion';
      default:
        return 'Novice';
    }
  }

  selectedPreviewData(): DungeonPreviewData {
    return (
      this.previewData.difficultyVariants?.[this.difficulty()] ??
      this.previewData
    );
  }

  selectedCanEnter(): boolean {
    return this.selectedPreviewData().canEnter ?? true;
  }

  readinessState(): string {
    return this.selectedPreviewData().readinessState ?? 'Ready';
  }

  readinessClass(): string {
    switch (this.readinessState().toLowerCase()) {
      case 'locked':
        return 'border-zinc-500/40 bg-zinc-900/60 text-zinc-300';
      case 'risky':
        return 'border-amber-500/40 bg-amber-950/20 text-amber-200';
      case 'dominating':
        return 'border-emerald-400/40 bg-emerald-950/20 text-emerald-200';
      default:
        return 'border-primary/40 bg-primary/10 text-primary';
    }
  }

  difficultyCanEnter(difficulty: DungeonDifficulty): boolean {
    const preview =
      this.previewData.difficultyVariants?.[difficulty] ??
      (difficulty === this.difficulty() ? this.previewData : null);

    return preview?.canEnter ?? true;
  }

  selectedMissingRequirements(): string[] {
    return this.selectedPreviewData().missingRequirements ?? [];
  }

  selectedWarnings(): string[] {
    return this.selectedPreviewData().warnings ?? [];
  }

  selectedEntryRequirements() {
    return this.selectedPreviewData().entryRequirements ?? [];
  }

  selectedRewardGroups(): RewardGroup[] {
    const groups = new Map<string, DungeonPreviewReward[]>();

    for (const reward of this.selectedPreviewData().rewards) {
      const key = reward.category || reward.source || 'Rewards';
      groups.set(key, [...(groups.get(key) ?? []), reward]);
    }

    return Array.from(groups.entries())
      .map(([title, rewards]) => ({ title, rewards }))
      .sort(
        (first, second) =>
          this.rewardGroupSortValue(first.title) -
            this.rewardGroupSortValue(second.title) ||
          first.title.localeCompare(second.title),
      );
  }

  rewardSources(rewards: DungeonPreviewReward[]): string[] {
    return Array.from(
      new Set(
        rewards
          .map((reward) => reward.source)
          .filter((source): source is string => !!source),
      ),
    );
  }

  private rewardGroupSortValue(title: string): number {
    switch (title.toLowerCase()) {
      case 'completion loot':
        return 1;
      case 'tier loot':
        return 2;
      case 'monster cores':
        return 3;
      case 'first completion':
        return 4;
      default:
        return 99;
    }
  }

  back() {
    this.backEvent.emit();
  }
}
