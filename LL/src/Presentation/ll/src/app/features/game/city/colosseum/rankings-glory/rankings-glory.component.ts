import { NgClass, NgFor, NgIf, NgSwitch, NgSwitchCase, NgSwitchDefault } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import { CharacterService } from '../../../../../core/services/api/character/character.service';
import { Subscription } from 'rxjs';
import { ColosseumRank } from '../../../../../shared/models/Dtos/colosseum/colosseumRank';
import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';

@Component({
  selector: 'app-rankings-glory',
  standalone: true,
  imports: [NgIf, NgFor, NgClass, NgSwitch, NgSwitchCase, NgSwitchDefault],
  templateUrl: './rankings-glory.component.html',
})
export class RankingsGloryComponent implements OnInit {
  @Input() rankings: ColosseumRank[] = [];
  readonly id;
  myRanking: ColosseumRank | undefined;
  subscriptions: Subscription = new Subscription();

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

  getMedalForRank(rank: number): string {
    switch(rank) {
      case 1: return 'assets/icons/medals/GoldMedalBlackOutline.svg';
      case 2: return 'assets/icons/medals/SilverMedalBlackOutline.svg';
      case 3: return 'assets/icons/medals/BronzeMedalBlackOutline.svg';
      default: return '';
    }
  }
}
