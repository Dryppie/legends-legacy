import { Component, signal } from '@angular/core';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';
import { TabComponent } from '../../../../shared/components/tabs/tab/tab.component';
import { MarketPlaceBuyComponent } from './market-place-buy/market-place-buy.component';
import { MarketPlaceSellComponent } from './market-place-sell/market-place-sell.component';
import { TabsComponent } from '../../../../shared/components/tabs/tabs.component';
import { MarketPlaceGenericComponent } from '../../../../shared/components/market-place/market-place-pages/market-place-generic/market-place-generic.component';
import { MarketPlaceFilterComponent } from '../../../../shared/components/market-place/market-place-filter/market-place-filter.component';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { NgSwitch, NgSwitchCase } from '@angular/common';
import { MarketPlaceResourcesComponent } from '../../../../shared/components/market-place/market-place-pages/market-place-resources/market-place-resources.component';
import { MarketPlaceEssencesComponent } from '../../../../shared/components/market-place/market-place-pages/market-place-essences/market-place-essences.component';
import { MarketPlaceEquipmentComponent } from '../../../../shared/components/market-place/market-place-pages/market-place-equipment/market-place-equipment.component';

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
    MarketPlaceGenericComponent,
    MarketPlaceResourcesComponent,
    MarketPlaceEssencesComponent,
    MarketPlaceEquipmentComponent,
    MarketPlaceFilterComponent,
    NgSwitch,
    NgSwitchCase,
  ],
  templateUrl: './market-place.component.html',
})
export class MarketPlaceComponent {
  readonly ItemType = ItemType;
  readonly selectedItemType = signal<ItemType>(ItemType.Equipment);

  onItemTypeChanged(type: ItemType) {
    this.selectedItemType.set(type);
  }
}
