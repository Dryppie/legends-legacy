import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { ColosseumStateService } from '../../../../../core/services/api/colosseum/colosseum-state.service';
import { ChampionMarketItem } from '../../../../../shared/models/Dtos/colosseum/championMarket';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';

@Component({
  selector: 'app-champions-market',
  standalone: true,
  imports: [NgFor, NgIf, DatePipe, NumberFormatPipe],
  templateUrl: './champions-market.component.html',
})
export class ChampionsMarketComponent {
  constructor(public readonly state: ColosseumStateService) {}

  purchase(item: ChampionMarketItem): void {
    if (!item.canPurchase || this.state.loading()) return;

    this.state.purchaseChampionMarketItem(item.id);
  }
}
