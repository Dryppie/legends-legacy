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

  constructor(
    private readonly characterActionService: CharacterActionsStateService,
    private readonly characterService: CharacterService,
  ) {
    this.currentCharacter = this.characterService.getCurrentCharacter();

    effect(() => {
      this.currentAction = this.characterActionService.currentAction();
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
    const startRequest: StartCombatActionRequest = {
      areaId: this.area.id,
    };
    this.characterActionService.startAction(
      CharacterActionType.Combat,
      startRequest,
    );
  }

  setIsLocked(): void {
    const character = this.currentCharacter();
    this.isLocked = !character || character.level < this.area.levelRequirement;
  }

  specificCard(): void {
    // placeholder or actual logic
  }
}
