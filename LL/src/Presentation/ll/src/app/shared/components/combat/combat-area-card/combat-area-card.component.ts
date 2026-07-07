import { Component, effect, Input, OnInit } from '@angular/core';
import { Area } from '../../../models/Dtos/regionDto';
import { MiniButtonComponent } from '../../custom-components/buttons/mini-button/mini-button.component';
import {
  CharacterActionDto,
  StartCombatActionRequest,
} from '../../../../shared/models/Dtos/characterActionDto';
import { CommonModule, NgIf } from '@angular/common';
import { CharacterService } from '../../../../core/services/api/character/character.service';
import { CharacterActionsStateService } from '../../../../core/services/api/character-actions/character-actions.state.service';
import { CharacterActionType } from '../../../models/enums/characterActionType';
import { GatheringType } from '../../../models/enums/gatheringType';
import { TutorialStateService } from '../../../../core/services/api/tutorial/tutorial-state.service';
import { TutorialService } from '../../../../core/services/api/tutorial/tutorial.service';
import { CombatService } from '../../../../core/services/client-side/combat/combat.service';
import {
  TUTORIAL_STEP_DEFEAT_TRAINING_CREATURE,
  TUTORIAL_TRAINING_GROUNDS_AREA_ID,
} from '../../../models/tutorial';
import { catchError, finalize, of, tap } from 'rxjs';

@Component({
  selector: 'app-combat-area-card',
  standalone: true,
  imports: [MiniButtonComponent, NgIf, CommonModule],
  templateUrl: './combat-area-card.component.html',
})
export class CombatAreaCardComponent implements OnInit {
  @Input() area!: Area;
  @Input() isLastInRow = false;

  currentAction: CharacterActionDto | null = null;
  readonly currentCharacter;
  isLocked = true;
  isStartingTrainingBattle = false;

  constructor(
    private readonly characterActionService: CharacterActionsStateService,
    private readonly characterService: CharacterService,
    private readonly tutorialState: TutorialStateService,
    private readonly tutorialService: TutorialService,
    private readonly combatService: CombatService,
  ) {
    this.currentCharacter = this.characterService.getCurrentCharacter();

    effect(() => {
      this.currentAction = this.characterActionService.currentAction();
    });

    effect(() => {
      this.tutorialState.state();
      this.tutorialState.hasLoaded();
      this.setIsLocked();
    });
  }

  ngOnInit(): void {
    this.setIsLocked();
  }

  canStartAction(): boolean {
    return (
      this.currentAction == null ||
      (new Date(this.currentAction.updatedAt).getTime() <= Date.now() &&
        this.currentAction.isDeleted)
    );
  }

  startCombat(): void {
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
    return this.isStartingTrainingBattle ? '...' : 'Battle';
  }

  trainingAreaTourId(): string | null {
    return this.area?.id === TUTORIAL_TRAINING_GROUNDS_AREA_ID
      ? 'training-area-card'
      : null;
  }

  trainingBattleButtonTourId(): string | null {
    return this.area?.id === TUTORIAL_TRAINING_GROUNDS_AREA_ID
      ? 'training-area-battle'
      : null;
  }

  setIsLocked(): void {
    if (!this.area) {
      return;
    }

    const character = this.currentCharacter();
    const tutorial = this.tutorialState.state();
    const isTrainingArea = this.area.id === TUTORIAL_TRAINING_GROUNDS_AREA_ID;
    const isTutorialUnknown = !this.tutorialState.hasLoaded();
    const isTutorialActive = !!tutorial && !tutorial.isCompleted;
    const isFirstTutorialStep =
      tutorial?.currentStep === TUTORIAL_STEP_DEFEAT_TRAINING_CREATURE;

    this.isLocked =
      !character ||
      character.level < this.area.levelRequirement ||
      (isTrainingArea && !isFirstTutorialStep) ||
      (isTutorialUnknown && !isTrainingArea) ||
      (isTutorialActive && !isTrainingArea);
  }

  gatheringTypes(): GatheringType[] {
    return this.area.gatheringTypes ?? [];
  }

  specificCard(): void {
    // placeholder or actual logic
  }

  private shouldStartTrainingBattle(): boolean {
    return this.area.id === TUTORIAL_TRAINING_GROUNDS_AREA_ID;
  }

  private startTrainingBattle(): void {
    if (this.isStartingTrainingBattle) return;

    this.isStartingTrainingBattle = true;
    this.tutorialService
      .startTrainingBattle()
      .pipe(
        tap((result) => {
          this.combatService.startTrainingBattleSummary(result);
        }),
        catchError((err) => {
          console.error('Failed to start training battle', err);
          return of(null);
        }),
        finalize(() => {
          this.isStartingTrainingBattle = false;
        }),
      )
      .subscribe();
  }
}
