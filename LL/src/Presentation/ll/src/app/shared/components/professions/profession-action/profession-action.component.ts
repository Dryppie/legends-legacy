import { Component, effect } from '@angular/core';
import { NgIf } from '@angular/common';
import { ProgressBarComponent } from '../../progress-bar/progress-bar.component';
import { CharacterActionDto } from '../../../models/Dtos/characterActionDto';
import { CharacterActionsStateService } from '../../../../core/services/api/character-actions/character-actions.state.service';

@Component({
  selector: 'app-profession-action',
  standalone: true,
  imports: [ProgressBarComponent, NgIf],
  templateUrl: './profession-action.component.html',
})
export class ProfessionActionComponent {
  currentAction: CharacterActionDto | null = null;
  remainingTime: string = '00:00'; // Add a property to track the remaining time
  performingAction = '';

  constructor(private state: CharacterActionsStateService) {
    effect(() => {
      const action = this.state.currentAction();
      this.currentAction = action;

      if (!action || action.isDeleted) {
        this.performingAction = '';
        return;
      }

      switch (action.characterActionType) {
        case 'Crafting':
          const itemName =
            action.craftingActionDetails?.craftingQueueItems?.[0]
              ?.equipmentInstance?.itemBase?.name ?? 'Unknown Item';
          this.performingAction = `Tempering - ${itemName}`;
          break;

        default:
          this.performingAction = '';
      }
    });
  }

  // Update the remaining time when received from the progress bar
  onRemainingTimeChange(time: string): void {
    this.remainingTime = time;
  }

  stopAction(): void {
    this.state.stopAction();
    this.performingAction = '';
  }
}
