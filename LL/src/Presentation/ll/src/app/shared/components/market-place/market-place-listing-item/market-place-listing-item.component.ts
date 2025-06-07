import { Component, Input } from '@angular/core';
import { MarketPlaceListing } from '../../../models/Dtos/market-place/market-place-listing';
import { ItemComponent } from '../../item/item.component';
import { NumberFormatPipe } from '../../../pipes/number-format/number-format.pipe';

@Component({
  selector: 'app-market-place-listing-item',
  standalone: true,
  imports: [ItemComponent, NumberFormatPipe],
  templateUrl: './market-place-listing-item.component.html',
})
export class MarketPlaceListingItemComponent {
  @Input() listing!: MarketPlaceListing;
}
