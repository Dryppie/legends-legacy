import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';

import { MarketplaceStateService } from '../../../../../core/services/api/market-place/market-place-state.service';
import { RegularButtonComponent } from '../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { ItemComponent } from '../../../../../shared/components/item/item.component';
import { MarketPlaceBuyOrder } from '../../../../../shared/models/Dtos/market-place/market-place-buy-order';
import { MarketPlaceListing } from '../../../../../shared/models/Dtos/market-place/market-place-listing';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';
import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';
import { EquipmentInstance } from '../../../../../shared/models/item';
import { ItemType } from '../../../../../shared/models/enums/itemType';
import { ItemQuality } from '../../../../../shared/models/enums/itemQuality';

@Component({
    selector: 'app-market-place-orders',
    imports: [
        CommonModule,
        ItemComponent,
        NumberFormatPipe,
        RegularButtonComponent,
    ],
    templateUrl: './market-place-orders.component.html'
})
export class MarketPlaceOrdersComponent implements OnInit {
  constructor(
    readonly marketplaceState: MarketplaceStateService,
    readonly characterState: CharacterStateService,
  ) {}

  ngOnInit(): void {
    this.marketplaceState.load();
  }

  cancelListing(listing: MarketPlaceListing): void {
    this.marketplaceState.cancelListing(listing.id).subscribe();
  }

  cancelBuyOrder(order: MarketPlaceBuyOrder): void {
    this.marketplaceState.cancelBuyOrder(order.id).subscribe();
  }

  listingQuality(listing: MarketPlaceListing): ItemQuality | null {
    return listing.itemInstance.itemBase.itemType === ItemType.Equipment
      ? (listing.itemInstance as EquipmentInstance).quality
      : null;
  }

  trackListing = (_: number, listing: MarketPlaceListing) => listing.id;
  trackBuyOrder = (_: number, order: MarketPlaceBuyOrder) => order.id;
}
