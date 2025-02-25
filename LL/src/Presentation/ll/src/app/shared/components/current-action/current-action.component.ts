import { Component, OnDestroy, OnInit } from '@angular/core';
import { CharacterActionsService } from '../../../core/services/character-actions/character-actions.service';
import { Subscription } from 'rxjs';
import { CharacterActionDto } from '../../models/Dtos/characterActionDto';
import { CharacterActionType } from '../../models/enums/characterActionType';
import { NgIf } from '@angular/common';
import { ProgressBarComponent } from '../progress-bar/progress-bar.component';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-current-action',
  standalone: true,
  imports: [NgIf, ProgressBarComponent],
  templateUrl: './current-action.component.html',
  styleUrl: './current-action.component.css',
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

  getDuration(): number {
    if (!this.currentAction) {
      return 0;
    }
    switch (this.currentAction.characterActionType) {
      case CharacterActionType.Combat:
        const updatedAt = new Date(this.currentAction.updatedAt).getTime();
        const timeUntilFinished = (updatedAt - Date.now()) / 1000;
        return Math.floor(timeUntilFinished);
      case CharacterActionType.Gathering:
        return environment.baseDuration;
      case CharacterActionType.Crafting:
        return environment.baseDuration;
      case CharacterActionType.Idle:
        return 0;
      default:
        return 0;
    }
  }

  getPerformingAction(): string {
    if (!this.currentAction) {
      return 'Idle';
    }
    if (
      this.currentAction.isDeleted &&
      new Date(this.currentAction.updatedAt).getTime() > Date.now()
    ) {
      return 'Engaged in Combat - Stopping..';
    }

    switch (this.currentAction.characterActionType) {
      case CharacterActionType.Combat:
        return 'Engaged in Combat';
      case CharacterActionType.Gathering:
        return 'Gathering Resources';
      case CharacterActionType.Crafting:
        return 'Crafting Items';
      case CharacterActionType.Idle:
        return 'Idle';
      default:
        return 'Idle';
    }
  }
}
