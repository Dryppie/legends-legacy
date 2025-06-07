import { Component, OnDestroy, OnInit } from '@angular/core';
import { CharacterActionsService } from '../../../core/services/api/character-actions/character-actions.service';
import { Subscription } from 'rxjs';
import { CharacterActionDto } from '../../models/Dtos/characterActionDto';
import { CharacterActionType } from '../../models/enums/characterActionType';
import { ProgressBarComponent } from '../progress-bar/progress-bar.component';

@Component({
  selector: 'app-current-action',
  standalone: true,
  imports: [ProgressBarComponent],
  templateUrl: './current-action.component.html',
})
export class CurrentActionComponent implements OnInit, OnDestroy {
  currentAction: CharacterActionDto | null = null;
  private subscription: Subscription = new Subscription();
  remainingTime: string = '00:00'; // Add a property to track the remaining time
  isGatheringAction = false;
  performingAction = '';
  duration = 0;

  constructor(private characterActionsService: CharacterActionsService) {}

  ngOnInit(): void {
    this.subscription.add(
      this.characterActionsService.currentAction$.subscribe((action) => {
        this.currentAction = action;
        this.setPerformingAction();
      }),
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  // Update the remaining time when received from the progress bar
  onRemainingTimeChange(time: string): void {
    this.remainingTime = time;
  }

  stopAction(): void {
    this.characterActionsService.stopCharacterAction();
  }

  setPerformingAction() {
    if (!this.currentAction) {
      this.performingAction = 'Idle';
      return;
    }
    if (
      this.currentAction.isDeleted &&
      new Date(this.currentAction.updatedAt).getTime() > Date.now()
    ) {
      this.performingAction = 'Engaged in Combat - Stopping..';
      return;
    }

    switch (this.currentAction.characterActionType) {
      case CharacterActionType.Combat:
        this.performingAction = 'Engaged in Combat';
        break;
      case CharacterActionType.Gathering:
        this.performingAction = 'Gathering Resources';
        break;
      case CharacterActionType.Crafting:
        this.performingAction = 'Tempering Items';
        break;
      case CharacterActionType.Idle:
        this.performingAction = 'Idle';
        break;
      default:
        this.performingAction = 'Idle';
    }
  }
}
