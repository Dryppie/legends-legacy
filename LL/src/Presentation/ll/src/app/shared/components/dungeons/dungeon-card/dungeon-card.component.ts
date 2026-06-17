import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { RegularButtonComponent } from '../../custom-components/buttons/regular-button/regular-button.component';
import { NgClass, NgFor, NgIf } from '@angular/common';
import { ItemComponent } from '../../item/item.component';
import { DungeonStateService } from '../../../../core/services/api/dungeon/dungeon-state.service';
import {
  DungeonGatheringNodePreview,
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
    if (this.isDifficultyUnlocked(d)) {
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

  isDifficultyUnlocked(difficulty: DungeonDifficulty): boolean {
    return this.previewData.unlockedDifficulties.includes(difficulty);
  }

  difficultyButtonClass(
    difficulty: DungeonDifficulty,
  ): Record<string, boolean> {
    const selected = this.difficulty() === difficulty;
    const unlocked = this.isDifficultyUnlocked(difficulty);

    return {
      'border-primary bg-primary/90 text-black': selected,
      'border-white/25 text-zinc-100 hover:border-primary hover:bg-primary/10':
        !selected && unlocked,
      'border-white/10 text-zinc-500 opacity-45': !unlocked,
    };
  }

  selectedStatusLabel(): string {
    if (this.selectedCanEnter()) {
      return 'Ready';
    }

    if (this.selectedMissingRequirements().length) {
      return 'Requirements missing';
    }

    if (!this.meetsRecommendedRating()) {
      return 'Under recommended rating';
    }

    return 'Locked';
  }

  selectedStatusClass(): string {
    return this.selectedCanEnter()
      ? 'border-primary/40 bg-primary/10 text-primary'
      : 'border-red-400/30 bg-red-950/20 text-red-100';
  }

  selectedMissingRequirements(): string[] {
    return this.selectedPreviewData().missingRequirements ?? [];
  }

  selectedEntryRequirements() {
    return this.selectedPreviewData().entryRequirements ?? [];
  }

  meetsRecommendedRating(): boolean {
    return (
      (this.selectedPreviewData().currentCombatRating ?? 0) >=
      (this.selectedPreviewData().recommendedCombatRating ?? 0)
    );
  }

  entryRequirementClass(requirement: {
    ownedAmount: number;
    requiredAmount: number;
  }): string {
    return requirement.ownedAmount >= requirement.requiredAmount
      ? 'border-primary/30 bg-primary/10 text-primary'
      : 'border-red-400/30 bg-red-950/20 text-red-100';
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

  selectedGatheringNodes(): DungeonGatheringNodePreview[] {
    return this.selectedPreviewData().gatheringNodes ?? [];
  }

  previewGatheringTypes(): string[] {
    return [
      ...new Set(
        (this.previewData.gatheringNodes ?? []).map((node) =>
          this.formatGatheringType(node.type),
        ),
      ),
    ];
  }

  gatheringChanceLabel(node: DungeonGatheringNodePreview): string {
    return `${Math.round((node.procChance ?? 0) * 100)}%`;
  }

  gatheringLevelLabel(node: DungeonGatheringNodePreview): string {
    return node.levelRequirement && node.levelRequirement > 0
      ? `Lv. ${node.levelRequirement}`
      : 'Any level';
  }

  gatheringLootQuantityLabel(
    loot: DungeonGatheringNodePreview['loot'][number],
  ): string {
    return loot.minQuantity === loot.maxQuantity
      ? `${loot.minQuantity}`
      : `${loot.minQuantity}-${loot.maxQuantity}`;
  }

  formatGatheringType(type: string | null | undefined): string {
    if (!type) {
      return 'Gathering';
    }

    return type.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  gatheringTypeClass(type: string | null | undefined): string {
    switch (type?.toLowerCase()) {
      case 'mining':
        return 'border-slate-300/25 bg-slate-200/10 text-slate-100';
      case 'woodcutting':
        return 'border-emerald-300/25 bg-emerald-400/10 text-emerald-100';
      case 'fishing':
        return 'border-sky-300/25 bg-sky-400/10 text-sky-100';
      case 'skinning':
        return 'border-amber-300/25 bg-amber-400/10 text-amber-100';
      default:
        return 'border-primary/25 bg-primary/10 text-primary';
    }
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
