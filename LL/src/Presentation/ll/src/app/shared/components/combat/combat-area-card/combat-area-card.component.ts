import { Component, Input } from '@angular/core';
import { Area } from '../../../models/Dtos/regionDto';
import { MiniButtonComponent } from '../../mini-button/mini-button.component';
import {
  CharacterActionDto,
  StartCombatActionRequest,
} from '../../../../shared/models/Dtos/characterActionDto';
import { CharacterActionsService } from '../../../../core/services/api/character-actions/character-actions.service';
import { CommonModule, NgIf } from '@angular/common';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-combat-area-card',
  standalone: true,
  imports: [MiniButtonComponent, NgIf, CommonModule],
  templateUrl: './combat-area-card.component.html',
  styleUrl: './combat-area-card.component.css',
})
export class CombatAreaCardComponent {
  canStartAction(): boolean {
    return (
      this.currentAction == null ||
      (new Date(this.currentAction.updatedAt).getTime() <= Date.now() &&
        this.currentAction.isDeleted)
    );
  }
  @Input() area!: Area;
  @Input() isLastInRow: boolean = false;
  currentAction: CharacterActionDto | null = null;
  private subscription: Subscription = new Subscription();

  constructor(private characterActionService: CharacterActionsService) {}

  ngOnInit(): void {
    this.subscription.add(
      this.characterActionService.currentAction$.subscribe((action) => {
        this.currentAction = action;
      }),
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  specificCard(): void {}

  startCombat() {
    const startCharacterActionRequest: StartCombatActionRequest = {
      areaId: this.area.id,
    };
    this.characterActionService.startCombatAction(startCharacterActionRequest);
  }

  cancelCharacterAction() {}
}
