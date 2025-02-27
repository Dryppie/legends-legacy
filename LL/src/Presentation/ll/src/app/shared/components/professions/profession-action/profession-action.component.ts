import { Component, OnDestroy, OnInit } from '@angular/core';
import { NgIf } from '@angular/common';
import { ProgressBarComponent } from '../../progress-bar/progress-bar.component';
import { CharacterActionDto } from '../../../models/Dtos/characterActionDto';
import { Subscription } from 'rxjs';
import { CharacterActionsService } from '../../../../core/services/character-actions/character-actions.service';
import { CharacterActionType } from '../../../models/enums/characterActionType';

@Component({
  selector: 'app-profession-action',
  standalone: true,
  imports: [ProgressBarComponent, NgIf],
  templateUrl: './profession-action.component.html',
  styleUrl: './profession-action.component.css',
})
export class ProfessionActionComponent implements OnInit, OnDestroy {
  currentAction: CharacterActionDto | null = null;
  private subscription: Subscription = new Subscription();
  remainingTime: string = '00:00'; // Add a property to track the remaining time
  isGatheringAction = false;
  performingAction = '';

  constructor(private characterActionsService: CharacterActionsService) {}

  ngOnInit(): void {
    this.subscription.add(
      this.characterActionsService.currentAction$.subscribe((action) => {
        if (action?.isDeleted) {
          this.isGatheringAction = false;
          return;
        }
        this.currentAction = action;
        this.isGatheringAction =
          this.currentAction?.characterActionType ===
          CharacterActionType.Gathering;

        if (this.isGatheringAction)
          this.performingAction = `Cutting: ${action?.gatheringActionDetails!.name}`;
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
    this.isGatheringAction == false;
    this.characterActionsService.stopCharacterAction();
  }
}
