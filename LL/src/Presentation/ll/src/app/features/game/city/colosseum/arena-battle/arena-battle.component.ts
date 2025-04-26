import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CharacterDto } from '../../../../../shared/models/Dtos/characterDto';
import { NgFor } from '@angular/common';

@Component({
  selector: 'app-arena-battle',
  standalone: true,
  imports: [NgFor],
  templateUrl: './arena-battle.component.html',
  styleUrl: './arena-battle.component.css',
})
export class ArenaBattleComponent {
  @Input() opponents!: CharacterDto[];
  @Output() refreshOpponents = new EventEmitter<void>();
  @Output() challenge = new EventEmitter<string>();

  onRefresh() {
    this.refreshOpponents.emit();
  }
  onChallenge(id: string) {
    this.challenge.emit(id);
  }
}
