import { equipmentSourceLabel } from '../../../utils/equipment/acquisition-source';
import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  signal,
} from '@angular/core';
import { RegularButtonComponent } from '../../custom-components/buttons/regular-button/regular-button.component';
import { NgClass, NgFor, NgIf, NgTemplateOutlet } from '@angular/common';
import { ItemComponent } from '../../item/item.component';
import { DungeonStateService } from '../../../../core/services/api/dungeon/dungeon-state.service';
import {
  DungeonMastery,
  DungeonMasteryBenefitLevel,
  DungeonPreviewData,
  DungeonPreviewReward,
  DungeonRecord,
} from '../../../models/Dtos/dungeons/dungeonPreviewData';
import { DungeonDifficulty } from '../../../models/enums/dungeonDifficulty';
import { Router } from '@angular/router';
import { ConnectedPosition, OverlayModule } from '@angular/cdk/overlay';

interface RewardGroup {
  title: string;
  rewards: DungeonPreviewReward[];
}

interface EntryRequirementPreview {
  itemId: string;
  name: string;
  ownedAmount: number;
  requiredAmount: number;
  description?: string | null;
}

interface MasteryBonusDisplay {
  id: string;
  label: string;
}

type DungeonDetailTab = 'rewards' | 'mastery';

@Component({
  selector: 'app-dungeon-card',
  imports: [
    NgIf,
    NgFor,
    NgClass,
    NgTemplateOutlet,
    OverlayModule,
    RegularButtonComponent,
    ItemComponent,
  ],
  templateUrl: './dungeon-card.component.html',
  styleUrl: './dungeon-card.component.scss',
})
export class DungeonCardComponent implements OnChanges {
  @Input({ required: true }) previewData!: DungeonPreviewData;

  @Output() recordsRequested = new EventEmitter<DungeonPreviewData>();

  readonly dungeonDifficulty = DungeonDifficulty;
  readonly difficulties: DungeonDifficulty[] = [
    DungeonDifficulty.Normal,
    DungeonDifficulty.Heroic,
    DungeonDifficulty.Mythic,
  ];

  private readonly allDetailTabs: { id: DungeonDetailTab; label: string }[] = [
    { id: 'rewards', label: 'Rewards' },
    { id: 'mastery', label: 'Mastery' },
  ];

  get detailTabs(): { id: DungeonDetailTab; label: string }[] {
    return this.allDetailTabs;
  }

  difficulty = signal<DungeonDifficulty>(DungeonDifficulty.Normal);
  private difficultyChosenManually = false;
  private appliedDefaultForDungeonId: string | null = null;
  selectedTab = signal<DungeonDetailTab>('rewards');
  readonly sigilAssemblyQuantity = signal(1);
  readonly selectedMasteryTooltipOpen = signal(false);
  readonly masteryTooltipPositions: ConnectedPosition[] = [
    {
      originX: 'center',
      originY: 'top',
      overlayX: 'center',
      overlayY: 'bottom',
      offsetY: -8,
    },
    {
      originX: 'center',
      originY: 'bottom',
      overlayX: 'center',
      overlayY: 'top',
      offsetY: 8,
    },
    {
      originX: 'start',
      originY: 'top',
      overlayX: 'start',
      overlayY: 'bottom',
      offsetY: -8,
    },
    {
      originX: 'end',
      originY: 'top',
      overlayX: 'end',
      overlayY: 'bottom',
      offsetY: -8,
    },
  ];

  constructor(
    readonly dungeonState: DungeonStateService,
    private readonly router: Router,
  ) {}

  ngOnChanges(_changes: SimpleChanges): void {
    if (!this.previewData) {
      return;
    }

    const dungeonId = this.previewData.familyId ?? this.previewData.id;
    if (dungeonId !== this.appliedDefaultForDungeonId) {
      this.appliedDefaultForDungeonId = dungeonId;
      this.difficultyChosenManually = false;
    }

    if (!this.difficultyChosenManually) {
      this.difficulty.set(this.defaultDifficulty());
    } else if (!this.isDifficultyUnlocked(this.difficulty())) {
      this.difficulty.set(this.defaultDifficulty());
    }

    this.setSigilAssemblyQuantity(this.sigilAssemblyQuantity());
  }

  /**
   * Preselects the difficulty of the most recent clear so returning players
   * do not have to reselect it on every visit. Falls back to the lowest
   * unlocked difficulty when the dungeon has never been completed.
   */
  private defaultDifficulty(): DungeonDifficulty {
    const fallback =
      this.previewData.unlockedDifficulties?.[0] ?? DungeonDifficulty.Normal;

    let best: DungeonDifficulty | null = null;
    let bestClearedAt = -Infinity;

    for (const difficulty of this.difficulties) {
      if (!this.isDifficultyUnlocked(difficulty)) continue;

      const record = this.recordFor(difficulty);
      if (!record?.hasCleared && !(record?.totalClears ?? 0)) continue;

      const clearedAt = this.toTimestamp(
        record?.lastClearedAt ?? record?.firstClearedAt,
      );

      // Iterated from lowest to highest difficulty, so an equal (or missing)
      // timestamp resolves to the hardest difficulty that was cleared.
      if (best === null || clearedAt >= bestClearedAt) {
        best = difficulty;
        bestClearedAt = clearedAt;
      }
    }

    return best ?? fallback;
  }

  private recordFor(
    difficulty: DungeonDifficulty,
  ): DungeonRecord | null | undefined {
    const variant = this.previewData.difficultyVariants?.[difficulty];
    if (variant) return variant.record;

    return this.previewData.difficulty === difficulty ||
      (difficulty === DungeonDifficulty.Normal &&
        !this.previewData.difficultyVariants)
      ? this.previewData.record
      : null;
  }

  private toTimestamp(value: string | null | undefined): number {
    if (!value) return 0;
    const parsed = new Date(value).getTime();
    return Number.isNaN(parsed) ? 0 : parsed;
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

  isActiveDungeonPreview(): boolean {
    const activeDungeon = this.dungeonState.activeDungeon();
    if (!activeDungeon) return false;

    const previewDefinitionIds = [
      this.previewData.id,
      ...Object.values(this.previewData.difficultyVariants ?? {})
        .map((variant) => variant?.id)
        .filter((id): id is string => !!id),
    ];
    const activeDefinitionId = activeDungeon.dungeonDefinitionId.toLowerCase();

    return previewDefinitionIds.some(
      (id) => id.toLowerCase() === activeDefinitionId,
    );
  }

  continueDungeon(): void {
    if (!this.isActiveDungeonPreview()) return;
    void this.router.navigate(['/game/world/dungeon']);
  }

  openRecords() {
    this.recordsRequested.emit(this.previewData);
  }

  selectDifficulty(difficulty: DungeonDifficulty) {
    if (this.isDifficultyUnlocked(difficulty)) {
      this.difficultyChosenManually = true;
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
      'll-card-accent text-primary': selected,
      'text-white': !selected && unlocked,
      'cursor-not-allowed opacity-45': !unlocked,
    };
  }

  detailTabClass(tab: DungeonDetailTab): Record<string, boolean> {
    const selected = this.selectedTab() === tab;

    return {
      'll-segmented-button-active': selected,
      'text-secondary': !selected,
    };
  }

  selectedStatusLabel(): string {
    if (this.selectedCanEnter()) {
      return 'Can enter';
    }

    if (this.selectedMissingRequirements().length) {
      return 'Cannot enter';
    }

    return 'Locked';
  }

  selectedStatusClass(): string {
    return this.selectedCanEnter() ? 'll-badge-accent' : 'll-badge-danger';
  }

  checkStatusClass(isReady: boolean): string {
    return isReady ? 'll-badge-accent' : 'll-badge-warning';
  }

  selectedMissingRequirements(): string[] {
    return this.selectedPreviewData().missingRequirements ?? [];
  }

  selectedEntryRequirements(): EntryRequirementPreview[] {
    return this.selectedPreviewData().entryRequirements ?? [];
  }

  selectedSigilRequirement(): EntryRequirementPreview | null {
    const selected = this.selectedPreviewData();
    if (!selected.sigilItemId) {
      return null;
    }

    return (
      this.selectedEntryRequirements().find(
        (requirement) => requirement.itemId === selected.sigilItemId,
      ) ?? null
    );
  }

  shouldShowSigilAssembly(): boolean {
    const requirement = this.selectedSigilRequirement();
    return this.dungeonState.sigilAssemblyEnabled() && !!requirement;
  }

  maximumSigilsAssemblable(): number {
    const cost = this.dungeonState.sigilAssemblyCost();
    return cost > 0 ? Math.floor(this.dungeonState.sigilFragments() / cost) : 0;
  }

  setSigilAssemblyQuantity(value: number): void {
    const maximum = Math.max(1, this.maximumSigilsAssemblable());
    const normalized = Number.isFinite(value) ? Math.floor(value) : 1;
    this.sigilAssemblyQuantity.set(Math.min(Math.max(normalized, 1), maximum));
  }

  setMaximumSigilAssemblyQuantity(): void {
    this.setSigilAssemblyQuantity(this.maximumSigilsAssemblable());
  }

  canAssembleSelectedSigil(): boolean {
    return (
      !this.dungeonState.loading() &&
      !!this.selectedPreviewData().canAssembleSigil &&
      this.sigilAssemblyQuantity() <= this.maximumSigilsAssemblable()
    );
  }

  assembleSelectedSigil(): void {
    if (!this.canAssembleSelectedSigil()) return;
    this.dungeonState.assembleSigil(
      this.selectedPreviewData().id,
      this.sigilAssemblyQuantity(),
    );
  }

  sigilAssemblyBlockingMessage(): string {
    const accessRequirements =
      this.selectedPreviewData().sigilAssemblyMissingRequirements ?? [];
    if (accessRequirements.length) {
      return accessRequirements.join(' · ');
    }

    const missing =
      this.dungeonState.sigilAssemblyCost() * this.sigilAssemblyQuantity() -
      this.dungeonState.sigilFragments();
    return missing > 0
      ? `Earn ${missing} more Sigil Fragments to assemble this sigil.`
      : '';
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

    return 'This dungeon difficulty is locked or unavailable.';
  }

  entryRequirementClass(requirement: EntryRequirementPreview): string {
    return requirement.ownedAmount >= requirement.requiredAmount
      ? 'll-card-accent text-primary'
      : 'll-list-row-danger text-danger';
  }

  selectedRewardGroups(): RewardGroup[] {
    const groups = new Map<string, DungeonPreviewReward[]>();

    for (const reward of this.selectedPreviewData().rewards ?? []) {
      const key = reward.category || equipmentSourceLabel(reward.source);
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
      {
        title: 'Run Rewards',
        rewards: repeatableRewards,
      },
      {
        title: 'First Clear',
        rewards: firstClearRewards,
      },
    ].filter((section) => section.rewards.length > 0);
  }

  selectedRunRewards(): DungeonPreviewReward[] {
    return (
      this.selectedRewardSections().find(
        (section) => section.title === 'Run Rewards',
      )?.rewards ?? []
    );
  }

  selectedFirstClearRewards(): DungeonPreviewReward[] {
    return (
      this.selectedRewardSections().find(
        (section) => section.title === 'First Clear',
      )?.rewards ?? []
    );
  }

  guaranteedRewards(rewards: DungeonPreviewReward[]): DungeonPreviewReward[] {
    return rewards.filter((reward) => this.rewardChancePercent(reward) >= 100);
  }

  chanceRewards(rewards: DungeonPreviewReward[]): DungeonPreviewReward[] {
    return rewards.filter((reward) => this.rewardChancePercent(reward) < 100);
  }

  trackRewardGroup(_: number, group: RewardGroup): string {
    return group.title;
  }

  trackReward(_: number, reward: DungeonPreviewReward): string {
    return reward.id || reward.itemBase?.id || reward.itemBase?.name || '';
  }

  rewardQuantityLabel(reward: DungeonPreviewReward): string {
    const min = reward.minQuantity ?? 1;
    const max = reward.maxQuantity ?? min;

    return min === max ? `Qty ${min}` : `Qty ${min}–${max}`;
  }

  rewardQuantityValue(reward: DungeonPreviewReward): string {
    const min = reward.minQuantity ?? 1;
    const max = reward.maxQuantity ?? min;

    return min === max ? `${min}` : `${min}–${max}`;
  }

  rewardDropChanceLabel(reward: DungeonPreviewReward): string | null {
    const chance = reward.dropChancePercent;
    if (chance === null || chance === undefined) {
      return null;
    }

    return `${this.formatDropChance(chance)} drop`;
  }

  rewardChancePercent(reward: DungeonPreviewReward): number {
    return Math.max(0, Math.min(100, reward.dropChancePercent ?? 100));
  }

  rewardChanceValueLabel(reward: DungeonPreviewReward): string {
    return this.formatDropChance(this.rewardChancePercent(reward));
  }

  rewardChanceBarWidth(reward: DungeonPreviewReward): number {
    return Math.sqrt(this.rewardChancePercent(reward) / 100) * 100;
  }

  selectedRewardCount(): number {
    return this.selectedPreviewData().rewards?.length ?? 0;
  }

  selectedRemainingRewardCount(limit = 3): number {
    return Math.max(0, this.selectedRewardCount() - limit);
  }

  selectedMasteryLevel(): number {
    return this.selectedPreviewData().mastery?.level ?? 0;
  }

  selectedMasteryExperience(): number {
    return this.selectedPreviewData().mastery?.experience ?? 0;
  }

  selectedMasteryNextExperience(): number | null {
    return (
      this.selectedPreviewData().mastery?.experienceRequiredForNextLevel ?? null
    );
  }

  selectedMasteryCompletionCount(): number {
    return this.selectedPreviewData().mastery?.completionCount ?? 0;
  }

  selectedMasteryBenefitLevels(): DungeonMasteryBenefitLevel[] {
    return this.selectedPreviewData().mastery?.benefitLevels ?? [];
  }

  masteryCurrentBonuses(
    mastery: DungeonMastery | null | undefined,
  ): MasteryBonusDisplay[] {
    const benefits = mastery?.benefits;
    if (!benefits) return [];

    const bonuses: MasteryBonusDisplay[] = [];
    if (benefits.additionalVisibilityRows > 0) {
      const rows = benefits.additionalVisibilityRows;
      bonuses.push({
        id: 'visibility',
        label: `+${rows} visibility ${rows === 1 ? 'row' : 'rows'}`,
      });
    }
    if (benefits.restSiteVigorBonus > 0) {
      bonuses.push({
        id: 'rest',
        label: `+${benefits.restSiteVigorBonus} Vigor from Rest Sites`,
      });
    }
    if (benefits.combatVigorCostReduction > 0) {
      bonuses.push({
        id: 'combat-vigor',
        label: `-${benefits.combatVigorCostReduction} Vigor from combat costs`,
      });
    }
    if (benefits.completionCurrencyBonusPercent > 0) {
      bonuses.push({
        id: 'completion-currency',
        label: `+${benefits.completionCurrencyBonusPercent}% completion Cinders and Soulstones`,
      });
    }
    return bonuses;
  }

  masteryNextBenefit(
    mastery: DungeonMastery | null | undefined,
  ): DungeonMasteryBenefitLevel | null {
    const currentLevel = mastery?.level ?? 0;
    return (
      mastery?.benefitLevels?.find((benefit) => benefit.level > currentLevel) ??
      null
    );
  }

  trackMasteryBonus(_: number, bonus: MasteryBonusDisplay): string {
    return bonus.id;
  }

  isMasteryBenefitUnlocked(benefit: DungeonMasteryBenefitLevel): boolean {
    return this.selectedMasteryLevel() >= benefit.level;
  }

  masteryBenefitClass(
    benefit: DungeonMasteryBenefitLevel,
  ): Record<string, boolean> {
    const unlocked = this.isMasteryBenefitUnlocked(benefit);
    return {
      'll-card-accent border-primary/40': unlocked,
      'border-white/10 opacity-60': !unlocked,
    };
  }

  trackMasteryBenefit(_: number, benefit: DungeonMasteryBenefitLevel): string {
    return benefit.id;
  }

  selectedMasteryProgressPercent(): number {
    const next = this.selectedMasteryNextExperience();
    if (!next || next <= 0) {
      return 100;
    }

    return Math.max(
      0,
      Math.min(
        100,
        Math.round((this.selectedMasteryExperience() / next) * 100),
      ),
    );
  }

  masteryExperienceLabel(): string {
    return this.formatMasteryExperienceLabel(
      this.selectedPreviewData().mastery,
    );
  }

  private formatMasteryExperienceLabel(
    mastery: DungeonMastery | null | undefined,
  ): string {
    const experience = mastery?.experience ?? 0;
    const next = mastery?.experienceRequiredForNextLevel ?? null;

    return next ? `${experience} / ${next} XP` : `${experience} XP`;
  }

  private formatDropChance(chance: number): string {
    const clamped = Math.max(0, Math.min(100, chance));
    const maximumFractionDigits = clamped > 0 && clamped < 1 ? 4 : 2;
    return `${Number(clamped.toFixed(maximumFractionDigits))}%`;
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

}
