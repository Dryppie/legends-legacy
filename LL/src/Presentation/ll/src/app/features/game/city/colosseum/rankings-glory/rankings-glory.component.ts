import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import { CharacterService } from '../../../../../core/services/api/character/character.service';
import { Subscription } from 'rxjs';
import { ColosseumRank } from '../../../../../shared/models/Dtos/colosseum/colosseumRank';

@Component({
  selector: 'app-rankings-glory',
  standalone: true,
  imports: [NgIf, NgFor, NgClass],
  templateUrl: './rankings-glory.component.html',
})
export class RankingsGloryComponent implements OnInit {
  @Input() rankings: ColosseumRank[] = [];
  id!: string;
  myRanking: ColosseumRank | undefined;
  subscriptions: Subscription = new Subscription();

  constructor(private characterService: CharacterService) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.characterService.getCurrentCharacter().subscribe((character) => {
        if (character) this.id = character.id;
      }),
    );
    this.myRanking = this.getMyRanking();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  getMyRanking(): ColosseumRank | undefined {
    return this.rankings.find((r) => r.characterId === this.id);
  }
}
