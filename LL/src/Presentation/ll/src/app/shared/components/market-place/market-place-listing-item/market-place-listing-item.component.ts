import { Component, Input } from '@angular/core';
import { MarketPlaceListing } from '../../../models/Dtos/market-place/market-place-listing';
import { ItemComponent } from '../../item/item.component';
import { NumberFormatPipe } from '../../../pipes/number-format/number-format.pipe';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { NgIf } from '@angular/common';
import {
  marketplaceEquipmentSummary,
  marketplaceEquipment,
  marketplaceStyleLabel,
} from '../../../utils/market-place/marketplace-equipment';

@Component({
  selector: 'app-market-place-listing-item',
  imports: [ItemComponent, NumberFormatPipe, NgIf],
  templateUrl: './market-place-listing-item.component.html',
})
export class MarketPlaceListingItemComponent {
  @Input() listing!: MarketPlaceListing;

  constructor(public readonly characterState: CharacterStateService) {}

  get activeStyleLabel(): string | null {
    const model = marketplaceEquipment(this.listing.itemInstance)?.progression;
    return model ? marketplaceStyleLabel(model.activeStyleId) : null;
  }

  get equipmentSummary(): string {
    return marketplaceEquipmentSummary(this.listing.itemInstance);
  }
}
