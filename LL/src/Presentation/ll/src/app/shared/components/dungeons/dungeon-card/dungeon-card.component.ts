import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  signal,
} from '@angular/core';
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

interface EntryRequirementPreview {
  name: string;
  ownedAmount: number;
  requiredAmount: number;
}

type DungeonDetailTab = 'rewards' | 'gathering';

@Component({
  selector: 'app-dungeon-card',
  standalone: true,
  imports: [NgIf, NgFor, NgClass, RegularButtonComponent, ItemComponent],
  templateUrl: './dungeon-card.component.html',
})
export class DungeonCardComponent implements OnChanges {
  @Input({ required: true }) previewData!: DungeonPreviewData;

  @Input() height = 176;
  @Input() cornerSize = 32;

  @Output() backEvent = new EventEmitter<void>();
  @Output() recordsRequested = new EventEmitter<DungeonPreviewData>();

  readonly dungeonDifficulty = DungeonDifficulty;
  readonly difficulties: DungeonDifficulty[] = [
    DungeonDifficulty.Normal,
    DungeonDifficulty.Heroic,
    DungeonDifficulty.Mythic,
  ];

  readonly detailTabs: { id: DungeonDetailTab; label: string }[] = [
    { id: 'rewards', label: 'Rewards' },
    { id: 'gathering', label: 'Gathering' },
  ];

  showPreview = signal(false);
  difficulty = signal<DungeonDifficulty>(DungeonDifficulty.Normal);
  selectedTab = signal<DungeonDetailTab>('rewards');

  constructor(
    private readonly dungeonState: DungeonStateService,
    private readonly router: Router,
  ) {}

  ngOnChanges(): void {
    if (!this.previewData) {
      return;
    }

    if (!this.isDifficultyUnlocked(this.difficulty())) {
      this.difficulty.set(
        this.previewData.unlockedDifficulties?.[0] ?? DungeonDifficulty.Normal,
      );
    }
  }

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

  selectDifficulty(difficulty: DungeonDifficulty) {
    if (this.isDifficultyUnlocked(difficulty)) {
      this.difficulty.set(difficulty);
    }
  }

  selectDetailTab(tab: DungeonDetailTab) {
    this.selectedTab.set(tab);
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
    return (
      this.previewData?.unlockedDifficulties?.includes(difficulty) ??
      difficulty === DungeonDifficulty.Normal
    );
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
      'cursor-not-allowed border-white/10 text-zinc-500 opacity-45': !unlocked,
    };
  }

  detailTabClass(tab: DungeonDetailTab): Record<string, boolean> {
    const selected = this.selectedTab() === tab;

    return {
      'border-primary bg-primary/90 text-black': selected,
      'border-white/15 bg-black/35 text-zinc-300 hover:border-primary/60 hover:text-zinc-100':
        !selected,
    };
  }

  selectedStatusLabel(): string {
    if (this.selectedCanEnter()) {
      return 'Can enter';
    }

    if (this.selectedMissingRequirements().length) {
      return 'Cannot enter';
    }

    if (!this.meetsRecommendedRating()) {
      return 'Power too low';
    }

    return 'Locked';
  }

  selectedStatusClass(): string {
    return this.selectedCanEnter()
      ? 'border-primary/40 bg-primary/10 text-primary'
      : 'border-red-400/30 bg-red-950/20 text-red-100';
  }

  checkStatusClass(isReady: boolean): string {
    return isReady
      ? 'border-primary/30 bg-primary/10 text-primary'
      : 'border-amber-300/30 bg-amber-950/20 text-amber-100';
  }

  selectedMissingRequirements(): string[] {
    return this.selectedPreviewData().missingRequirements ?? [];
  }

  selectedEntryRequirements(): EntryRequirementPreview[] {
    return this.selectedPreviewData().entryRequirements ?? [];
  }

  selectedHasMissingEntryRequirements(): boolean {
    return this.selectedEntryRequirements().some(
      (requirement) => requirement.ownedAmount < requirement.requiredAmount,
    );
  }

  selectedEntryStatusLabel(): string {
    return this.selectedHasMissingEntryRequirements() ? 'Missing' : 'Ready';
  }

  selectedMissingEntryRequirementMessages(): string[] {
    return this.selectedEntryRequirements()
      .filter(
        (requirement) => requirement.ownedAmount < requirement.requiredAmount,
      )
      .map(
        (requirement) =>
          `Requires ${requirement.requiredAmount} ${requirement.name} (${requirement.ownedAmount}/${requirement.requiredAmount}).`,
      );
  }

  selectedBlockingMessage(): string {
    if (this.selectedCanEnter()) {
      return '';
    }

    const missingEntryRequirements =
      this.selectedMissingEntryRequirementMessages();

    if (missingEntryRequirements.length) {
      return missingEntryRequirements.join(' ');
    }

    if (this.selectedMissingRequirements().length) {
      return this.selectedMissingRequirements().join(' · ');
    }

    if (!this.meetsRecommendedRating()) {
      return `Recommended Power ${this.selectedRecommendedCombatRating()}, your Power ${this.selectedCurrentCombatRating()}.`;
    }

    return 'This dungeon difficulty is locked or unavailable.';
  }

  selectedCurrentCombatRating(): number {
    return this.selectedPreviewData().currentCombatRating ?? 0;
  }

  selectedRecommendedCombatRating(): number {
    return this.selectedPreviewData().recommendedCombatRating ?? 0;
  }

  meetsRecommendedRating(): boolean {
    return (
      this.selectedCurrentCombatRating() >=
      this.selectedRecommendedCombatRating()
    );
  }

  entryRequirementClass(requirement: EntryRequirementPreview): string {
    return requirement.ownedAmount >= requirement.requiredAmount
      ? 'border-primary/30 bg-primary/10 text-primary'
      : 'border-red-400/30 bg-red-950/20 text-red-100';
  }

  selectedRewardGroups(): RewardGroup[] {
    const groups = new Map<string, DungeonPreviewReward[]>();

    for (const reward of this.selectedPreviewData().rewards ?? []) {
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

  selectedRewardSections(): RewardGroup[] {
    const repeatableRewards: DungeonPreviewReward[] = [];
    const firstClearRewards: DungeonPreviewReward[] = [];

    for (const group of this.selectedRewardGroups()) {
      const title = group.title.toLowerCase();

      if (title === 'first completion' || title === 'first clear') {
        firstClearRewards.push(...group.rewards);
        continue;
      }

      repeatableRewards.push(...group.rewards);
    }

    return [
      { title: 'Run Rewards', rewards: repeatableRewards },
      { title: 'First Clear', rewards: firstClearRewards },
    ].filter((section) => section.rewards.length > 0);
  }

  trackRewardGroup(_: number, group: RewardGroup): string {
    return group.title;
  }

  trackReward(_: number, reward: DungeonPreviewReward): string {
    return reward.id || reward.itemBase?.id || reward.itemBase?.name || '';
  }

  trackGatheringNode(_: number, node: DungeonGatheringNodePreview): string {
    return node.id;
  }

  trackGatheringLoot(
    _: number,
    loot: DungeonGatheringNodePreview['loot'][number],
  ): string {
    return (
      loot.id || loot.itemId || loot.itemBase?.id || loot.itemBase?.name || ''
    );
  }

  selectedMainRewards(limit = 3): DungeonPreviewReward[] {
    return this.selectedRewardGroups()
      .flatMap((group) => group.rewards)
      .slice(0, limit);
  }

  selectedRewardCount(): number {
    return this.selectedPreviewData().rewards?.length ?? 0;
  }

  selectedRemainingRewardCount(limit = 3): number {
    return Math.max(0, this.selectedRewardCount() - limit);
  }

  selectedGatheringNodes(): DungeonGatheringNodePreview[] {
    return this.selectedPreviewData().gatheringNodes ?? [];
  }

  selectedGatheringTypes(): string[] {
    return [
      ...new Set(
        this.selectedGatheringNodes().map((node) =>
          this.formatGatheringType(node.type),
        ),
      ),
    ];
  }

  selectedGatheringSummary(): string {
    const types = this.selectedGatheringTypes();
    return types.length ? types.join(' · ') : 'None';
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
