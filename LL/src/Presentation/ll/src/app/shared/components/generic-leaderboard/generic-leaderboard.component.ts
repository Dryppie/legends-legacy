import { Component, Input } from '@angular/core';
import { NgClass, NgIf, NgFor } from '@angular/common';
import {
  LeaderboardColumn,
  LeaderboardEntry,
} from '../../models/Dtos/leaderboard/leaderboardEntry';
import { LeaderboardPodiumComponent } from './leaderboard-podium/leaderboard-podium.component';

@Component({
    selector: 'app-leaderboard',
    imports: [NgClass, NgIf, NgFor, LeaderboardPodiumComponent],
    templateUrl: './generic-leaderboard.component.html'
})
export class LeaderboardComponent<T extends LeaderboardEntry> {
  @Input({ required: true }) title!: string;
  @Input({ required: true }) data: readonly T[] = [];
  @Input({ required: true }) columns: readonly LeaderboardColumn<T>[] = [];
  /** id of “my” row so we can highlight it */
  @Input() highlightId?: string | number;

  /** convenience getters */
  get podium(): readonly T[] {
    return this.data.slice(0, 3);
  }
  get rows(): readonly T[] {
    return this.data.slice(3);
  }

  trackByRank = (_: number, row: T) => row.rank;
}
