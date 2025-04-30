import { Component, Input } from '@angular/core';
import { ColosseumMatchResult } from '../../../../../shared/models/Dtos/colosseum/colosseumMatchResult';
import { DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { CharacterService } from '../../../../../core/services/api/character/character.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-record-of-battle',
  standalone: true,
  imports: [NgIf, NgFor, NgClass, DatePipe],
  templateUrl: './record-of-battle.component.html',
  styleUrl: './record-of-battle.component.css',
})
export class RecordOfBattleComponent {
  @Input() previousMatches: ColosseumMatchResult[] = [];
  id!: string;
  subscriptions: Subscription = new Subscription();

  constructor(private characterService: CharacterService) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.characterService.getCurrentCharacter().subscribe((character) => {
        if (character) this.id = character.id;
      }),
    );
  }
}
