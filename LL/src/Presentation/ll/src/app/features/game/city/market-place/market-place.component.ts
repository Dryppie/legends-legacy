import { Component } from '@angular/core';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';
import { TabComponent } from '../../../../shared/components/tabs/tab/tab.component';
import { MarketPlaceBuyComponent } from './market-place-buy/market-place-buy.component';
import { MarketPlaceSellComponent } from './market-place-sell/market-place-sell.component';
import { TabsComponent } from '../../../../shared/components/tabs/tabs.component';

@Component({
  selector: 'app-market-place',
  standalone: true,
  imports: [
    BannerComponent,
    TabComponent,
    MarketPlaceBuyComponent,
    MarketPlaceSellComponent,
    TabsComponent,
    TabComponent,
  ],
  templateUrl: './market-place.component.html',
})
export class MarketPlaceComponent {}
