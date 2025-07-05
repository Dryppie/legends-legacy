import { Component, Input } from '@angular/core';
import {
  LeaderboardColumn,
  LeaderboardEntry,
} from '../../../models/Dtos/leaderboard/leaderboardEntry';
import { NgClass, NgFor, NgIf } from '@angular/common';

@Component({
  selector: 'app-leaderboard-podium',
  standalone: true,
  imports: [NgFor, NgIf, NgClass],
  templateUrl: './leaderboard-podium.component.html',
})
export class LeaderboardPodiumComponent<T extends LeaderboardEntry> {
  @Input({ required: true }) entries: readonly T[] = [];
  @Input({ required: true }) columns: readonly LeaderboardColumn<T>[] = [];
}
