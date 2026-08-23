import { Component, effect, Input, OnInit } from '@angular/core';
import { Area } from '../../../models/Dtos/regionDto';
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

@Component({
  selector: 'app-combat-area-card',
  imports: [MiniButtonComponent, NgIf, CommonModule],
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

  constructor(
    private readonly characterActionService: CharacterActionsStateService,
    private readonly questState: QuestStateService,
    private readonly questService: QuestService,
    private readonly inventoryState: InventoryStateService,
    private readonly combatService: CombatService,
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
    return this.isStartingTrainingBattle || this.isStartingIdleCombat()
      ? '...'
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

  gatheringTypes(): GatheringType[] {
    return this.area.gatheringTypes ?? [];
  }

  gatheringTypeInitial(gatheringType: GatheringType): string {
    return gatheringType.charAt(0).toUpperCase();
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
