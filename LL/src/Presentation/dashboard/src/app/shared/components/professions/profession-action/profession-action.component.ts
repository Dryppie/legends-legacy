import { Component, OnDestroy, OnInit } from '@angular/core';
import { NgIf } from '@angular/common';
import { ProgressBarComponent } from '../../progress-bar/progress-bar.component';
import { CharacterActionDto } from '../../../models/Dtos/characterActionDto';
import { Subscription } from 'rxjs';
import { CharacterActionsService } from '../../../../core/services/api/character-actions/character-actions.service';

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
  remainingTime: string = '00:00';
  performingAction = '';

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

  onRemainingTimeChange(time: string): void {
    this.remainingTime = time;
  }

  stopAction(): void {
    this.characterActionsService.stopCharacterAction();
  }
}
