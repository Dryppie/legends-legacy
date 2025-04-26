import { Component, Input } from '@angular/core';
import { ColosseumMatchResult } from '../../../../../shared/models/Dtos/colosseum/colosseumMatchResult';
import { DatePipe, NgFor, NgIf } from '@angular/common';

@Component({
  selector: 'app-record-of-battle',
  standalone: true,
  imports: [NgIf, NgFor, DatePipe],
  templateUrl: './record-of-battle.component.html',
  styleUrl: './record-of-battle.component.css',
})
export class RecordOfBattleComponent {
  @Input() previousMatches: ColosseumMatchResult[] = [];
}
