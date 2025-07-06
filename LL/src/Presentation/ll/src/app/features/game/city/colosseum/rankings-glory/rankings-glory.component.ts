import { Component, Input, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';
import { LeaderboardComponent } from '../../../../../shared/components/generic-leaderboard/generic-leaderboard.component';
import {
  LeaderboardColumn,
  LeaderboardEntry,
} from '../../../../../shared/models/Dtos/leaderboard/leaderboardEntry';
import { ARENA_COLUMNS } from '../../../../../shared/models/Dtos/leaderboard/rows/arenaRow';

@Component({
  selector: 'app-rankings-glory',
  standalone: true,
  imports: [LeaderboardComponent],
  templateUrl: './rankings-glory.component.html',
})
export class RankingsGloryComponent implements OnInit {
  @Input() rankings: LeaderboardEntry[] = [];
  readonly id;
  myRanking: LeaderboardEntry | undefined;
  subscriptions: Subscription = new Subscription();

  columns = ARENA_COLUMNS as LeaderboardColumn<LeaderboardEntry>[];
  top3 = this.rankings.slice(0, 3);

  constructor(private state: CharacterStateService) {
    this.id = state.currentCharacterId();
  }

  ngOnInit(): void {
    this.myRanking = this.getMyRanking();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  getMyRanking(): LeaderboardEntry | undefined {
    return this.rankings.find((r) => r.characterId === this.id);
  }
}
