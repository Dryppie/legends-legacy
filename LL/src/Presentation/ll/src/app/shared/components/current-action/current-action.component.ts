import { Component, effect, OnInit } from '@angular/core';
import { CharacterActionDto } from '../../models/Dtos/characterActionDto';
import { CharacterActionType } from '../../models/enums/characterActionType';
import { ProgressBarComponent } from '../progress-bar/progress-bar.component';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';

@Component({
    selector: 'app-current-action',
    imports: [ProgressBarComponent],
    templateUrl: './current-action.component.html'
})
export class CurrentActionComponent {
  currentAction: CharacterActionDto | null = null;
  remainingTime: string = '00:00'; // Add a property to track the remaining time
  performingAction = '';
  duration = 0;
  readonly totalDuration;

  constructor(private state: CharacterActionsStateService) {
    this.totalDuration = this.state.tickingDuration;

    effect(() => {
      const action = this.state.currentAction();
      this.currentAction = action;
      this.setPerformingAction();
    });
  }

  // Update the remaining time when received from the progress bar
  onRemainingTimeChange(time: string): void {
    this.remainingTime = time;
  }

  stopAction(): void {
    this.state.stopAction();
  }

  private setPerformingAction(): void {
    const action = this.currentAction;

    if (!action) {
      this.performingAction = 'Idle';
      return;
    }

    if (action.isDeleted && new Date(action.updatedAt).getTime() > Date.now()) {
      this.performingAction = 'Engaged in Combat - Stopping..';
      return;
    }

    switch (action.characterActionType) {
      case CharacterActionType.Combat:
        this.performingAction = 'Engaged in Combat';
        break;
      case CharacterActionType.Crafting:
        this.performingAction = 'Tempering Items';
        break;
      case CharacterActionType.Idle:
        this.performingAction = 'Idle';
        break;
      default:
        this.performingAction = 'Idle';
        break;
    }
  }
}
