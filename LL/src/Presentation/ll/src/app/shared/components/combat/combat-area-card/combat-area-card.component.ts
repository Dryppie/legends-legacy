import { Component, Input } from '@angular/core';
import { Area } from '../../../models/Dtos/regionDto';
import { MiniButtonComponent } from '../../mini-button/mini-button.component';
import { StartCombatActionRequest } from '../../../../shared/models/Dtos/characterActionDto';
import { CharacterActionsService } from '../../../../core/services/character-actions/character-actions.service';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-combat-area-card',
  standalone: true,
  imports: [MiniButtonComponent, NgIf],
  templateUrl: './combat-area-card.component.html',
  styleUrl: './combat-area-card.component.css',
})
export class CombatAreaCardComponent {
  @Input() area!: Area;

  constructor(private characterActionService: CharacterActionsService) {}

  ngOnInit(): void {}

  ngOnDestroy(): void {}

  specificCard(): void {}

  startCombat() {
    const startCharacterActionRequest: StartCombatActionRequest = {
      areaId: this.area.id,
    };
    this.characterActionService.startCombatAction(startCharacterActionRequest);
  }

  cancelCharacterAction() {}
}
