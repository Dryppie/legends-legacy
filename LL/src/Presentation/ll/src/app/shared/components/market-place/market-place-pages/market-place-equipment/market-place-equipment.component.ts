import { Component, Input, Signal } from '@angular/core';
import { MarketPlaceListing } from '../../../../models/Dtos/market-place/market-place-listing';
import { NgFor, NgIf } from '@angular/common';
import { NumberFormatPipe } from '../../../../pipes/number-format/number-format.pipe';
import { ItemComponent } from '../../../item/item.component';
import { RegularButtonComponent } from '../../../custom-components/buttons/regular-button/regular-button.component';

@Component({
  selector: 'app-market-place-equipment',
  standalone: true,
  imports: [
    NgIf,
    NgFor,
    NumberFormatPipe,
    ItemComponent,
    RegularButtonComponent,
  ],
  templateUrl: './market-place-equipment.component.html',
})
export class MarketPlaceEquipmentComponent {
  @Input() equipmentListings!: Signal<MarketPlaceListing[]>;
}
