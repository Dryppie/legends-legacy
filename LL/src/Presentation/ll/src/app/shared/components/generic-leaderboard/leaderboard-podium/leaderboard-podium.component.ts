import { Component, Input } from '@angular/core';
import {
  LeaderboardColumn,
  LeaderboardEntry,
} from '../../../models/Dtos/leaderboard/leaderboardEntry';
import { NgClass, NgFor, NgIf } from '@angular/common';
import { CharacterTagComponent } from '../../character/character-tag/character-tag.component';

@Component({
  selector: 'app-leaderboard-podium',
  imports: [NgFor, NgIf, NgClass, CharacterTagComponent],
  templateUrl: './leaderboard-podium.component.html',
})
export class LeaderboardPodiumComponent<T extends LeaderboardEntry> {
  @Input({ required: true }) entries: readonly T[] = [];
  @Input({ required: true }) columns: readonly LeaderboardColumn<T>[] = [];
}
