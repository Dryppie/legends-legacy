import { NgFor, NgIf } from '@angular/common';
import { Component, Input, Signal } from '@angular/core';
import { MarketPlaceListing } from '../../../../models/Dtos/market-place/market-place-listing';
import { ItemComponent } from '../../../item/item.component';
import { NumberFormatPipe } from '../../../../pipes/number-format/number-format.pipe';
import { EssenceItem, essenceItemToEssence } from '../../../../models/item';
import { RegularButtonComponent } from '../../../custom-components/buttons/regular-button/regular-button.component';

@Component({
  selector: 'app-market-place-essences',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    ItemComponent,
    NumberFormatPipe,
    RegularButtonComponent,
  ],
  templateUrl: './market-place-essences.component.html',
})
export class MarketPlaceEssencesComponent {
  @Input() essenceListings!: Signal<MarketPlaceListing[]>;

  itemAsEssence(listing: MarketPlaceListing) {
    return essenceItemToEssence(listing.itemInstance.itemBase as EssenceItem);
  }
}
