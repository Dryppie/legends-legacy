import { Component, effect, Input, OnInit } from '@angular/core';
import { Area } from '../../../models/Dtos/regionDto';
import { MiniButtonComponent } from '../../custom-components/buttons/mini-button/mini-button.component';
import {
  CharacterActionDto,
  StartCombatActionRequest,
} from '../../../../shared/models/Dtos/characterActionDto';
import { CommonModule, NgIf } from '@angular/common';
import { CharacterActionsStateService } from '../../../../core/services/api/character-actions/character-actions.state.service';
import { CharacterActionType } from '../../../models/enums/characterActionType';
import { GatheringType } from '../../../models/enums/gatheringType';
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
import { QuestService } from '../../../../core/services/api/quest/quest.service';
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
})
export class CombatAreaCardComponent implements OnInit {
  @Input() area!: Area;
  @Input() isLastInRow = false;

  currentAction: CharacterActionDto | null = null;
  readonly isStartingIdleCombat;
  isLocked = true;
  isStartingTrainingBattle = false;

  constructor(
    private readonly characterActionService: CharacterActionsStateService,
    private readonly questState: QuestStateService,
    private readonly questService: QuestService,
    private readonly combatService: CombatService,
  ) {
    this.isStartingIdleCombat = this.characterActionService.loadingCombat;

    effect(() => {
      this.currentAction = this.characterActionService.currentAction();
    });

    effect(() => {
      this.questState.areaAccess();
      this.questState.loaded();
      this.setIsLocked();
    });

    effect(() => {
      const error = this.characterActionService.idleCombatError();
      if (
        error && this.isIntoLumoRuinsActive()
      ) {
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
      (this.currentAction == null ||
        (new Date(this.currentAction.updatedAt).getTime() <= Date.now() &&
          this.currentAction.isDeleted))
    );
  }

  startCombat(): void {
    if (this.isStartingTrainingBattle || this.isStartingIdleCombat()) return;

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
      return 'training-area-card';
    }

    return this.isQuestGuidedLumoArea() ? 'lumo-ruins-card' : null;
  }

  trainingBattleButtonTourId(): string | null {
    if (this.area?.id === TRAINING_GROUNDS_AREA_ID) {
      return 'training-area-battle';
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
    this.isLocked = !access?.canAccess;
  }

  gatheringTypes(): GatheringType[] {
    return this.area.gatheringTypes ?? [];
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
    this.questService
      .startEncounter(TRAINING_DAY_QUEST_ID, 'training')
      .pipe(
        tap((result) => {
          this.combatService.startTrainingBattleSummary(result);
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

  private isQuestGuidedLumoArea(): boolean {
    return (
      this.area?.id === LUMO_RUINS_AREA_ID && this.isIntoLumoRuinsActive()
    );
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
