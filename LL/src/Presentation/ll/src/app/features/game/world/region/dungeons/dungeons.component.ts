import { Component } from '@angular/core';
import { DungeonCardComponent } from '../../../../../shared/components/dungeons/dungeon-card/dungeon-card.component';

@Component({
  selector: 'app-dungeons',
  standalone: true,
  imports: [DungeonCardComponent],
  templateUrl: './dungeons.component.html',
})
export class DungeonsComponent {}
