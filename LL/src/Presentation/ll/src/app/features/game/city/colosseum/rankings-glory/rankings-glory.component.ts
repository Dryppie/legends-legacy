import { NgFor, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-rankings-glory',
  standalone: true,
  imports: [NgIf, NgFor],
  templateUrl: './rankings-glory.component.html',
  styleUrl: './rankings-glory.component.css',
})
export class RankingsGloryComponent {
  @Input() rankings: {
    rank: number;
    name: string;
    rating: number;
    playerId: string;
  }[] = [];
  @Input() playerId!: string;

  get myRanking() {
    return this.rankings.find((r) => r.playerId === this.playerId);
  }
}
