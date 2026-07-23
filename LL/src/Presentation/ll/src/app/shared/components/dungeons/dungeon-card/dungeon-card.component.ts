import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  signal,
} from '@angular/core';
import { RegularButtonComponent } from '../../custom-components/buttons/regular-button/regular-button.component';
import {
  DecimalPipe,
  NgClass,
  NgFor,
  NgIf,
  NgTemplateOutlet,
} from '@angular/common';
import { ItemComponent } from '../../item/item.component';
import { DungeonStateService } from '../../../../core/services/api/dungeon/dungeon-state.service';
import {
  DungeonGatheringNodePreview,
  DungeonMastery,
  DungeonMasteryBenefitLevel,
  DungeonPreviewData,
  DungeonPreviewReward,
} from '../../../models/Dtos/dungeons/dungeonPreviewData';
import { DungeonDifficulty } from '../../../models/enums/dungeonDifficulty';
import { Router } from '@angular/router';
import { Equipment } from '../../../models/item';
import { EquipmentType } from '../../../models/enums/equipmentType';
import { BaseItemComponent } from '../../base-item/base-item.component';
import { ConnectedPosition, OverlayModule } from '@angular/cdk/overlay';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';

interface RewardGroup {
  title: string;
  rewards: DungeonPreviewReward[];
}

interface EntryRequirementPreview {
  itemId: string;
  name: string;
  ownedAmount: number;
  requiredAmount: number;
}

interface MasteryBonusDisplay {
  id: string;
  label: string;
}

type DungeonDetailTab = 'rewards' | 'gathering' | 'mastery';

@Component({
  selector: 'app-dungeon-card',
  standalone: true,
  imports: [
    NgIf,
    NgFor,
    NgClass,
    NgTemplateOutlet,
    DecimalPipe,
    OverlayModule,
    RegularButtonComponent,
    ItemComponent,
    BaseItemComponent,
  ],
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
    { id: 'mastery', label: 'Mastery' },
  ];

  showPreview = signal(false);
  difficulty = signal<DungeonDifficulty>(DungeonDifficulty.Normal);
  selectedTab = signal<DungeonDetailTab>('rewards');
  readonly previewMasteryTooltipOpen = signal(false);
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
    private readonly characterState: CharacterStateService,
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
    this.previewMasteryTooltipOpen.set(false);
    this.selectedMasteryTooltipOpen.set(false);
    if (!this.showPreview()) {
      this.characterState.refreshIfDirty();
    }
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
    return (
      this.dungeonState.sigilAssemblyEnabled() &&
      !!requirement &&
      requirement.ownedAmount < requirement.requiredAmount
    );
  }

  canAssembleSelectedSigil(): boolean {
    return (
      !this.dungeonState.loading() &&
      !!this.selectedPreviewData().canAssembleSigil &&
      this.dungeonState.sigilFragments() >=
        this.dungeonState.sigilAssemblyCost()
    );
  }

  assembleSelectedSigil(): void {
    if (!this.canAssembleSelectedSigil()) return;
    this.dungeonState.assembleSigil(this.selectedPreviewData().id);
  }

  sigilAssemblyBlockingMessage(): string {
    const accessRequirements =
      this.selectedPreviewData().sigilAssemblyMissingRequirements ?? [];
    if (accessRequirements.length) {
      return accessRequirements.join(' · ');
    }

    const missing =
      this.dungeonState.sigilAssemblyCost() -
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

  selectedPartyPower(): number | null {
    const power = this.characterState.overview()?.power;
    return power?.state === 'Available' ? power.overall : null;
  }

  selectedRecommendedPartyPower(): number | null {
    return this.selectedPreviewData().recommendedPartyPower ?? null;
  }

  recommendationPendingLabel(): string {
    return this.selectedPreviewData().powerRecommendationUnavailable
      ? 'Unavailable'
      : 'Calibrating…';
  }

  powerComparisonClass(): string {
    const recommended = this.selectedRecommendedPartyPower();
    const partyPower = this.selectedPartyPower();
    if (!recommended || partyPower === null) return 'll-badge-muted';
    if (this.selectedPreviewData().powerRecommendationLowConfidence)
      return 'll-badge-warning';
    return partyPower >= recommended * 0.9
      ? 'll-badge-accent'
      : 'll-badge-warning';
  }

  entryRequirementClass(requirement: EntryRequirementPreview): string {
    return requirement.ownedAmount >= requirement.requiredAmount
      ? 'll-card-accent text-primary'
      : 'll-list-row-danger text-danger';
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
      {
        title: 'Run Rewards',
        rewards: this.withUniqueTools(repeatableRewards),
      },
      {
        title: 'First Clear',
        rewards: this.withUniqueTools(firstClearRewards),
      },
    ].filter((section) => section.rewards.length > 0);
  }

  trackRewardGroup(_: number, group: RewardGroup): string {
    return group.title;
  }

  trackReward(_: number, reward: DungeonPreviewReward): string {
    return reward.id || reward.itemBase?.id || reward.itemBase?.name || '';
  }

  isToolReward(reward: DungeonPreviewReward): boolean {
    return this.isToolItem(reward);
  }

  rewardQuantityLabel(reward: DungeonPreviewReward): string {
    const min = reward.minQuantity ?? 1;
    const max = reward.maxQuantity ?? min;

    return min === max ? `x${min}` : `x${min}-${max}`;
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

  previewMasteryLevel(): number {
    return this.previewData.mastery?.level ?? 0;
  }

  previewMasteryExperienceLabel(): string {
    return this.formatMasteryExperienceLabel(this.previewData.mastery);
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
    if (benefits.gatheringProcChanceBonus > 0) {
      bonuses.push({
        id: 'gathering',
        label: `+${Math.round(benefits.gatheringProcChanceBonus * 100)} percentage points to gathering chance`,
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
        return 'll-badge-muted';
      case 'woodcutting':
        return 'll-badge-success';
      case 'fishing':
        return 'll-badge-info';
      case 'skinning':
        return 'll-badge-warning';
      default:
        return 'll-item-chip-accent';
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

  private withUniqueTools(
    rewards: DungeonPreviewReward[],
  ): DungeonPreviewReward[] {
    const seenTools = new Set<string>();
    const toolRewards: DungeonPreviewReward[] = [];
    const otherRewards: DungeonPreviewReward[] = [];

    for (const reward of rewards) {
      if (!this.isToolItem(reward)) {
        otherRewards.push(reward);
        continue;
      }

      const equipment = reward.itemBase as Equipment;
      const key = [
        equipment.gatheringType ?? '',
        reward.itemBase.name.trim().toLowerCase(),
      ].join(':');

      if (seenTools.has(key)) {
        continue;
      }

      seenTools.add(key);
      toolRewards.push(reward);
    }

    return [...toolRewards, ...otherRewards];
  }

  private isToolItem(reward: DungeonPreviewReward): boolean {
    return (reward.itemBase as Equipment).equipmentType === EquipmentType.Tool;
  }

  back() {
    this.backEvent.emit();
  }
}
