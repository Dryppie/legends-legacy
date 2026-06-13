import { Component, Input, Signal } from '@angular/core';
import { MarketPlaceListing } from '../../../../models/Dtos/market-place/market-place-listing';
import { EssenceItem } from '../../../../models/item';
import { EssenceItemViewService } from '../../../../../core/services/api/essences/essence-item-view.service';

@Component({
  selector: 'app-market-place-essences',
  standalone: true,
  imports: [],
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
