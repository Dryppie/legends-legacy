import { NgFor, NgIf } from '@angular/common';
import { Component, Input, Signal } from '@angular/core';
import { MarketPlaceListing } from '../../../../models/Dtos/market-place/market-place-listing';
import { ItemComponent } from '../../../item/item.component';
import { NumberFormatPipe } from '../../../../pipes/number-format/number-format.pipe';
import { EssenceItem } from '../../../../models/item';
import { RegularButtonComponent } from '../../../custom-components/buttons/regular-button/regular-button.component';
import { EssenceItemViewService } from '../../../../../core/services/api/essences/essence-item-view.service';

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

  constructor(private readonly essenceItemView: EssenceItemViewService) {}

  itemAsEssence(listing: MarketPlaceListing) {
    return this.essenceItemView.asEssence(
      listing.itemInstance.itemBase as EssenceItem,
    );
  }
}
