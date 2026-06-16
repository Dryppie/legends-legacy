import { Component, Input, OnChanges } from '@angular/core';
import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';
import { LeaderboardComponent } from '../../../../../shared/components/generic-leaderboard/generic-leaderboard.component';
import {
  LeaderboardColumn,
  LeaderboardEntry,
} from '../../../../../shared/models/Dtos/leaderboard/leaderboardEntry';
import { ARENA_COLUMNS } from '../../../../../shared/models/Dtos/leaderboard/rows/arenaRow';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';

@Component({
  selector: 'app-rankings-glory',
  standalone: true,
  imports: [LeaderboardComponent, NumberFormatPipe],
  templateUrl: './rankings-glory.component.html',
})
export class RankingsGloryComponent implements OnChanges {
  @Input() rankings: LeaderboardEntry[] = [];
  readonly id;
  myRanking: LeaderboardEntry | undefined;

  columns = ARENA_COLUMNS as LeaderboardColumn<LeaderboardEntry>[];

  constructor(private state: CharacterStateService) {
    this.id = state.currentCharacterId();
  }

  ngOnChanges(): void {
    this.myRanking = this.getMyRanking();
  }

  get champion(): LeaderboardEntry | undefined {
    return this.rankings[0];
  }

  get trackedFighters(): number {
    return this.rankings.length;
  }

  getMyRanking(): LeaderboardEntry | undefined {
    return this.rankings.find((r) => r.characterId === this.id);
  }
}
