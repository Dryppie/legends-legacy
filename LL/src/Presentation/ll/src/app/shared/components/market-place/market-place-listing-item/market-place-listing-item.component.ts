import { Component, Input } from '@angular/core';
import { MarketPlaceListing } from '../../../models/Dtos/market-place/market-place-listing';
import { ItemComponent } from '../../item/item.component';
import { NumberFormatPipe } from '../../../pipes/number-format/number-format.pipe';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { NgIf } from '@angular/common';
import { EquipmentInstance } from '../../../models/item';
import { ItemType } from '../../../models/enums/itemType';

@Component({
    selector: 'app-market-place-listing-item',
    imports: [ItemComponent, NumberFormatPipe, NgIf],
    templateUrl: './market-place-listing-item.component.html'
})
export class MarketPlaceListingItemComponent {
  @Input() listing!: MarketPlaceListing;

  constructor(public readonly characterState: CharacterStateService) {}

  get qualityLabel(): string {
    return this.listing.itemInstance.itemBase.itemType === ItemType.Equipment
      ? (this.listing.itemInstance as EquipmentInstance).quality
      : '—';
  }
}
