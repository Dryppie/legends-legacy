import { Component, effect, OnInit } from '@angular/core';
import { CharacterActionDto } from '../../models/Dtos/characterActionDto';
import { CharacterActionType } from '../../models/enums/characterActionType';
import { ProgressBarComponent } from '../progress-bar/progress-bar.component';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';

@Component({
  selector: 'app-current-action',
  standalone: true,
  imports: [ProgressBarComponent],
  templateUrl: './current-action.component.html',
})
export class CurrentActionComponent {
  currentAction: CharacterActionDto | null = null;
  remainingTime: string = '00:00'; // Add a property to track the remaining time
  isGatheringAction = false;
  performingAction = '';
  duration = 0;

  constructor(private state: CharacterActionsStateService) {
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
      this.isGatheringAction = false;
      return;
    }

    if (action.isDeleted && new Date(action.updatedAt).getTime() > Date.now()) {
      this.performingAction = 'Engaged in Combat - Stopping..';
      this.isGatheringAction = false;
      return;
    }

    switch (action.characterActionType) {
      case CharacterActionType.Combat:
        this.performingAction = 'Engaged in Combat';
        this.isGatheringAction = false;
        break;
      case CharacterActionType.Gathering:
        this.performingAction = 'Gathering Resources';
        this.isGatheringAction = true;
        break;
      case CharacterActionType.Crafting:
        this.performingAction = 'Tempering Items';
        this.isGatheringAction = false;
        break;
      case CharacterActionType.Idle:
        this.performingAction = 'Idle';
        this.isGatheringAction = false;
        break;
      default:
        this.performingAction = 'Idle';
        this.isGatheringAction = false;
        break;
    }
  }
}
