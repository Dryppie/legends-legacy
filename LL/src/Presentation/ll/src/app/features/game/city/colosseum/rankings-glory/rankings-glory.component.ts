import { Component, Input, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { ColosseumRank } from '../../../../../shared/models/Dtos/colosseum/colosseumRank';
import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';
import { LeaderboardComponent } from '../../../../../shared/components/generic-leaderboard/generic-leaderboard.component';

@Component({
  selector: 'app-rankings-glory',
  standalone: true,
  imports: [LeaderboardComponent],
  templateUrl: './rankings-glory.component.html',
})
export class RankingsGloryComponent implements OnInit {
  @Input() rankings: ColosseumRank[] = [];
  readonly id;
  myRanking: ColosseumRank | undefined;
  subscriptions: Subscription = new Subscription();

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

  getMyRanking(): ColosseumRank | undefined {
    return this.rankings.find((r) => r.characterId === this.id);
  }
}
