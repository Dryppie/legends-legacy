import { Component, Input } from '@angular/core';
import { NgClass, NgIf, NgFor } from '@angular/common';

@Component({
  selector: 'app-leaderboard',
  standalone: true,
  imports: [
    NgClass,
    NgIf,
    NgFor
  ],
  templateUrl: './generic-leaderboard.component.html',
  styleUrls: ['./generic-leaderboard.component.css'],
})

export class LeaderboardComponent {
  @Input() displayMode: 'arena' | 'skill' | 'wealth' | 'total-level' = 'arena'
  @Input() rankings: any[] = [];
  @Input() myRanking: any;
  @Input() title: string = 'Leaderboard';

  get podium(): any[] {
    return this.rankings?.slice(0, 3) || [];
  }

  get others(): any[] {
    return this.rankings?.slice(3) || [];
  }
}
