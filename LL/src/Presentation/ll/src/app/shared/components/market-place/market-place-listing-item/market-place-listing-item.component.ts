import { Component, Input } from '@angular/core';
import { MarketPlaceListing } from '../../../models/Dtos/market-place/market-place-listing';
import { ItemComponent } from '../../item/item.component';
import { NumberFormatPipe } from '../../../pipes/number-format/number-format.pipe';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-market-place-listing-item',
  standalone: true,
  imports: [ItemComponent, NumberFormatPipe, NgIf],
  templateUrl: './market-place-listing-item.component.html',
})
export class MarketPlaceListingItemComponent {
  @Input() listing!: MarketPlaceListing;

  constructor(public readonly characterState: CharacterStateService) {}
}
