import { Component, effect, Input, OnInit } from '@angular/core';
import { Area, AreaGatheringNode } from '../../../models/Dtos/regionDto';
import { MiniButtonComponent } from '../../custom-components/buttons/mini-button/mini-button.component';
import { StartCombatActionRequest } from '../../../../shared/models/Dtos/characterActionDto';
import { CommonModule, NgIf } from '@angular/common';
import { CharacterActionsStateService } from '../../../../core/services/api/character-actions/character-actions.state.service';
import { CharacterActionType } from '../../../models/enums/characterActionType';
import { GatheringType } from '../../../models/enums/gatheringType';
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
import { QuestService } from '../../../../core/services/api/quest/quest.service';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { CombatService } from '../../../../core/services/client-side/combat/combat.service';
import {
  INTO_LUMO_RUINS_QUEST_ID,
  LUMO_RUINS_AREA_ID,
  QuestStatus,
  TRAINING_DAY_QUEST_ID,
  TRAINING_GROUNDS_AREA_ID,
} from '../../../models/quest';
import { catchError, finalize, of, tap } from 'rxjs';
import { ConnectedPosition, OverlayModule } from '@angular/cdk/overlay';
import { EquipmentStateService } from '../../../../core/services/api/equipment/equipment-state.service';
import { EquipmentSlotType } from '../../../models/Dtos/equipment-slots/equipmentSlot';

@Component({
  selector: 'app-combat-area-card',
  imports: [MiniButtonComponent, NgIf, CommonModule, OverlayModule],
  templateUrl: './combat-area-card.component.html',
  styleUrl: './combat-area-card.component.scss',
})
export class CombatAreaCardComponent implements OnInit {
  readonly GatheringType = GatheringType;

  @Input() area!: Area;
  @Input() isLastInRow = false;
  @Input() isActiveBattle = false;

  readonly isStartingIdleCombat;
  isLocked = true;
  isStartingTrainingBattle = false;
  activeGatheringTooltipNodeId: string | null = null;
  readonly gatheringTooltipPositions: ConnectedPosition[] = [
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
      originY: 'center',
      overlayX: 'end',
      overlayY: 'center',
      offsetX: -8,
    },
    {
      originX: 'end',
      originY: 'center',
      overlayX: 'start',
      overlayY: 'center',
      offsetX: 8,
    },
  ];

  constructor(
    private readonly characterActionService: CharacterActionsStateService,
    private readonly questState: QuestStateService,
    private readonly questService: QuestService,
    private readonly inventoryState: InventoryStateService,
    private readonly combatService: CombatService,
    private readonly equipmentState: EquipmentStateService,
  ) {
    this.isStartingIdleCombat = this.characterActionService.loadingCombat;

    effect(() => {
      this.questState.areaAccess();
      this.questState.loaded();
      this.setIsLocked();
    });

    effect(() => {
      const error = this.characterActionService.idleCombatError();
      if (error && this.isIntoLumoRuinsActive()) {
        this.questState.reportError(error);
      }
    });
  }

  ngOnInit(): void {
    this.setIsLocked();
  }

  canStartAction(): boolean {
    return (
      !this.isActiveBattle &&
      !this.isStartingIdleCombat() &&
      this.characterActionService.canStartAction(CharacterActionType.Combat)
    );
  }

  startCombat(): void {
    if (
      this.isStartingTrainingBattle ||
      this.isStartingIdleCombat() ||
      !this.canStartAction()
    )
      return;

    this.questState.clearError();
    if (this.shouldStartTrainingBattle()) {
      this.startTrainingBattle();
      return;
    }

    const startRequest: StartCombatActionRequest = {
      areaId: this.area.id,
    };
    this.characterActionService.startAction(
      CharacterActionType.Combat,
      startRequest,
    );
  }

  battleButtonText(): string {
    if (this.isStartingTrainingBattle || this.isStartingIdleCombat()) {
      return '...';
    }

    const currentAction = this.characterActionService.currentAction();
    return currentAction?.characterActionType === CharacterActionType.Combat &&
      !currentAction.isDeleted
      ? 'Move'
      : 'Battle';
  }

  trainingAreaTourId(): string | null {
    if (this.area?.id === TRAINING_GROUNDS_AREA_ID) {
      return this.selectedTrainingEncounterKey() ? 'training-area-card' : null;
    }

    return this.isQuestGuidedLumoArea() ? 'lumo-ruins-card' : null;
  }

  trainingBattleButtonTourId(): string | null {
    if (this.area?.id === TRAINING_GROUNDS_AREA_ID) {
      return this.selectedTrainingEncounterKey()
        ? 'training-area-battle'
        : null;
    }

    return this.isQuestGuidedLumoArea() ? 'lumo-ruins-battle' : null;
  }

  gatheringTourId(): string | null {
    return this.isQuestGuidedLumoArea() ? 'lumo-ruins-gathering' : null;
  }

  setIsLocked(): void {
    if (!this.area) {
      return;
    }

    const access = this.questState.accessFor(this.area.id);
    this.isLocked =
      !access?.canAccess ||
      (this.area.id === TRAINING_GROUNDS_AREA_ID &&
        !this.selectedTrainingEncounterKey());
  }

  gatheringNodes(): AreaGatheringNode[] {
    return (this.area.gatheringNodes ?? [])
      .filter(
        (node) => this.isAbundant(node) && node.procChance !== undefined,
      )
      .sort(
        (left, right) =>
          this.gatheringTypeOrder(left.type) -
          this.gatheringTypeOrder(right.type),
      );
  }

  gatheringTypeInitial(gatheringType: GatheringType): string {
    return gatheringType.charAt(0).toUpperCase();
  }

  isAbundant(node: AreaGatheringNode): boolean {
    return (node.yieldBonusPercent ?? 0) > 0;
  }

  procChancePercent(node: AreaGatheringNode): number {
    return Math.max(0, node.procChance ?? 0) * 100;
  }

  successfulDropQuantity(node: AreaGatheringNode): string {
    const minimum = node.minQuantity;
    const maximum = node.maxQuantity;
    if (minimum === undefined || minimum === null) return '—';
    if (maximum === undefined || maximum === null || maximum === minimum) {
      return `${minimum}`;
    }
    return `${minimum}–${maximum}`;
  }

  showGatheringTooltip(nodeId: string): void {
    this.activeGatheringTooltipNodeId = nodeId;
  }

  hideGatheringTooltip(nodeId: string): void {
    if (this.activeGatheringTooltipNodeId === nodeId) {
      this.activeGatheringTooltipNodeId = null;
    }
  }

  isGatheringTooltipOpen(nodeId: string): boolean {
    return this.activeGatheringTooltipNodeId === nodeId;
  }

  gatheringTooltipId(node: AreaGatheringNode): string {
    return `gathering-tooltip-${this.area.id}-${node.id}`;
  }

  incorrectToolMessage(node: AreaGatheringNode): string | null {
    const equippedTool = this.equipmentState.getSlot(
      EquipmentSlotType.Tool,
    )?.equipmentInstance;
    if (
      !equippedTool ||
      equippedTool.equipmentBase.gatheringType === node.type
    ) {
      return null;
    }

    return `Incorrect tool equipped — requires ${node.type}.`;
  }

  isEssenceCollectionCompleted(): boolean {
    const progress = this.area.essenceProgress;
    return (
      !!progress && progress.total > 0 && progress.collected >= progress.total
    );
  }

  specificCard(): void {
    // placeholder or actual logic
  }

  private shouldStartTrainingBattle(): boolean {
    return this.area.id === TRAINING_GROUNDS_AREA_ID;
  }

  private gatheringTypeOrder(type: GatheringType): number {
    return [
      GatheringType.Mining,
      GatheringType.Woodcutting,
      GatheringType.Skinning,
    ].indexOf(type);
  }

  private startTrainingBattle(): void {
    if (this.isStartingTrainingBattle) return;

    this.isStartingTrainingBattle = true;
    const encounterKey = this.selectedTrainingEncounterKey() ?? 'training';
    this.questService
      .startEncounter(TRAINING_DAY_QUEST_ID, encounterKey)
      .pipe(
        tap((result) => {
          this.inventoryState.applyVersionedInventoryDelta(result, (response) =>
            this.inventoryState.addOrIncrementMany(response.loot ?? []),
          );
          this.combatService.startTrainingBattleSummary(result.data);
        }),
        catchError((err) => {
          console.error('Failed to start training battle', err);
          this.questState.reportError(
            err?.message ?? 'Failed to start the training battle.',
          );
          return of(null);
        }),
        finalize(() => {
          this.isStartingTrainingBattle = false;
        }),
      )
      .subscribe();
  }

  private selectedTrainingEncounterKey(): string | null {
    const trainingQuest = this.questState
      .journal()
      .quests.find(
        (quest) =>
          quest.questId === TRAINING_DAY_QUEST_ID &&
          quest.status === QuestStatus.Active,
      );
    if (!trainingQuest?.choice) return trainingQuest ? 'training' : null;

    return (
      trainingQuest.choice.options.find(
        (option) => option.key === trainingQuest.choice?.selectedOptionKey,
      )?.encounterKey ?? null
    );
  }

  private isQuestGuidedLumoArea(): boolean {
    return this.area?.id === LUMO_RUINS_AREA_ID && this.isIntoLumoRuinsActive();
  }

  private isIntoLumoRuinsActive(): boolean {
    return this.questState
      .journal()
      .quests.some(
        (quest) =>
          quest.questId === INTO_LUMO_RUINS_QUEST_ID &&
          quest.status === QuestStatus.Active,
      );
  }
}
