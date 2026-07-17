import { Component, signal } from '@angular/core';
import { MarketPlaceBuyComponent } from './market-place-buy/market-place-buy.component';
import { MarketPlaceSellComponent } from './market-place-sell/market-place-sell.component';
import { MarketPlaceGenericComponent } from '../../../../shared/components/market-place/market-place-pages/market-place-generic/market-place-generic.component';
import { MarketPlaceFilterComponent } from '../../../../shared/components/market-place/market-place-filter/market-place-filter.component';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { NgIf, NgSwitch, NgSwitchCase } from '@angular/common';
import { MarketPlaceResourcesComponent } from '../../../../shared/components/market-place/market-place-pages/market-place-resources/market-place-resources.component';
import { MarketPlaceEssencesComponent } from '../../../../shared/components/market-place/market-place-pages/market-place-essences/market-place-essences.component';
import { MarketPlaceEquipmentComponent } from '../../../../shared/components/market-place/market-place-pages/market-place-equipment/market-place-equipment.component';
import {
  NavigationTab,
  NavigationTabsComponent,
} from '../../../../shared/components/custom-components/tabs/navigation-tabs/navigation-tabs.component';
import { DropdownSelection } from '../../../../shared/components/custom-components/dropdown/dropdown.component';
import { MarketplaceStateService } from '../../../../core/services/api/market-place/market-place-state.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { NumberFormatPipe } from '../../../../shared/pipes/number-format/number-format.pipe';
import { MarketPlaceCommodityComponent } from './market-place-commodity/market-place-commodity.component';

type MarketplaceTradeTab = 'buy' | 'sell';

@Component({
  selector: 'app-market-place',
  standalone: true,
  imports: [
    MarketPlaceBuyComponent,
    MarketPlaceSellComponent,
    NavigationTabsComponent,
    MarketPlaceGenericComponent,
    MarketPlaceResourcesComponent,
    MarketPlaceEssencesComponent,
    MarketPlaceEquipmentComponent,
    MarketPlaceFilterComponent,
    MarketPlaceCommodityComponent,
    NumberFormatPipe,
    NgIf,
    NgSwitch,
    NgSwitchCase,
  ],
  templateUrl: './market-place.component.html',
})
export class MarketPlaceComponent {
  readonly ItemType = ItemType;
  readonly tradeTabs: readonly NavigationTab[] = [
    { key: 'buy', label: 'Buy' },
    { key: 'sell', label: 'Sell' },
  ];
  readonly activeTradeTab = signal<MarketplaceTradeTab>('buy');
  readonly selectedItemType = signal<DropdownSelection<ItemType>>({
    main: ItemType.Resource,
    sub: 'Metal',
  });

  constructor(
    readonly marketplaceState: MarketplaceStateService,
    readonly characterState: CharacterStateService,
  ) {}

  onItemTypeChanged(type: DropdownSelection<ItemType>) {
    this.selectedItemType.set(type);
    this.activeTradeTab.set('buy');
  }

  selectTradeTab(tabKey: string): void {
    if (tabKey === 'buy' || tabKey === 'sell') {
      this.activeTradeTab.set(tabKey);
    }
  }
}
