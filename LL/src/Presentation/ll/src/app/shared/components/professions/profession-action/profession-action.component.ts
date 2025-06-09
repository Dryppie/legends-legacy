import { Component, OnDestroy, OnInit } from '@angular/core';
import { NgIf } from '@angular/common';
import { ProgressBarComponent } from '../../progress-bar/progress-bar.component';
import { CharacterActionDto } from '../../../models/Dtos/characterActionDto';
import { Subscription } from 'rxjs';
import { CharacterActionsService } from '../../../../core/services/api/character-actions/character-actions.service';
import { CharacterActionType } from '../../../models/enums/characterActionType';

@Component({
  selector: 'app-profession-action',
  standalone: true,
  imports: [ProgressBarComponent, NgIf],
  templateUrl: './profession-action.component.html',
})
export class ProfessionActionComponent implements OnInit, OnDestroy {
  currentAction: CharacterActionDto | null = null;
  private subscription: Subscription = new Subscription();
  remainingTime: string = '00:00'; // Add a property to track the remaining time
  performingAction = '';

  constructor(private characterActionsService: CharacterActionsService) {}

  ngOnInit(): void {
    this.subscription.add(
      this.characterActionsService.currentAction$.subscribe((action) => {
        if (action?.isDeleted) {
          this.performingAction = '';
          return;
        }
        this.currentAction = action;

        if (
          this.currentAction?.characterActionType ===
          CharacterActionType.Gathering
        )
          this.performingAction = `Gathering - ${action?.gatheringActionDetails!.name}`;

        if (
          this.currentAction?.characterActionType ===
          CharacterActionType.Crafting
        )
          this.performingAction = `Tempering - ${action?.craftingActionDetails!.craftingQueueItems[0].equipmentInstance.itemBase.name}`;
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
    this.performingAction = '';
  }
}
