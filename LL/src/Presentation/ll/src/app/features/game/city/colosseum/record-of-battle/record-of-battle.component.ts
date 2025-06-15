import { Component, Input } from '@angular/core';
import { ColosseumMatchResult } from '../../../../../shared/models/Dtos/colosseum/colosseumMatchResult';
import { DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';

@Component({
  selector: 'app-record-of-battle',
  standalone: true,
  imports: [NgIf, NgFor, NgClass, DatePipe],
  templateUrl: './record-of-battle.component.html',
})
export class RecordOfBattleComponent {
  @Input() previousMatches: ColosseumMatchResult[] = [];
  readonly id;

  constructor(private state: CharacterStateService) {
    this.id = state.currentCharacterId();
  }
}
