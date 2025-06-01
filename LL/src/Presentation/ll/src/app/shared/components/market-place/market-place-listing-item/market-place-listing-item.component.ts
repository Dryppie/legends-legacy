import { Component, Input } from '@angular/core';
import { MarketPlaceListing } from '../../../models/Dtos/market-place/market-place-listing';
import { ItemComponent } from '../../item/item.component';
import { NgIf } from '@angular/common';
import { NumberFormatPipe } from '../../../pipes/number-format/number-format.pipe';

@Component({
  selector: 'app-market-place-listing-item',
  standalone: true,
  imports: [ItemComponent, NgIf, NumberFormatPipe],
  templateUrl: './market-place-listing-item.component.html',
  styleUrl: './market-place-listing-item.component.css',
})
export class MarketPlaceListingItemComponent {
  @Input() listing!: MarketPlaceListing;
}
