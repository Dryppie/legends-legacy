import { Component, signal } from '@angular/core';
import { MarketPlaceBuyComponent } from './market-place-buy/market-place-buy.component';
import { MarketPlaceSellComponent } from './market-place-sell/market-place-sell.component';
import { MarketPlaceFilterComponent } from '../../../../shared/components/market-place/market-place-filter/market-place-filter.component';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { NgIf, NgSwitch, NgSwitchCase } from '@angular/common';
import { MarketplaceStateService } from '../../../../core/services/api/market-place/market-place-state.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { NumberFormatPipe } from '../../../../shared/pipes/number-format/number-format.pipe';
import { MarketPlaceCommodityComponent } from './market-place-commodity/market-place-commodity.component';
import { MarketPlaceOrdersComponent } from './market-place-orders/market-place-orders.component';
import { MarketCategorySelection } from '../../../../shared/models/market-category';

type MarketPlaceMode = 'browse' | 'sell' | 'orders';

@Component({
  selector: 'app-market-place',
  standalone: true,
  imports: [
    MarketPlaceBuyComponent,
    MarketPlaceSellComponent,
    MarketPlaceFilterComponent,
    MarketPlaceCommodityComponent,
    MarketPlaceOrdersComponent,
    NumberFormatPipe,
    NgIf,
    NgSwitch,
    NgSwitchCase,
  ],
  templateUrl: './market-place.component.html',
  styleUrl: './market-place.component.css',
})
export class MarketPlaceComponent {
  readonly ItemType = ItemType;
  readonly mode = signal<MarketPlaceMode>('browse');
  readonly mobileDetailOpen = signal(false);
  readonly selectedMarket = signal<MarketCategorySelection>({
    id: 'resources',
    label: 'Resources',
    itemType: ItemType.Resource,
    subcategory: 'Metal',
  });

  constructor(
    readonly marketplaceState: MarketplaceStateService,
    readonly characterState: CharacterStateService,
  ) {}

  onCategoryChanged(category: MarketCategorySelection): void {
    this.selectedMarket.set(category);
    this.mobileDetailOpen.set(false);
  }

  setMode(mode: MarketPlaceMode): void {
    this.mode.set(mode);
    this.mobileDetailOpen.set(false);
  }

  onMobileDetailChanged(open: boolean): void {
    this.mobileDetailOpen.set(open);
  }
}
