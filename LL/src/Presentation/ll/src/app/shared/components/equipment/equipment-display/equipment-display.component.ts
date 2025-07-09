import { DecimalPipe, NgFor, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { AttributeTypeFormatPipe } from '../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import {
  EquipmentDisplay,
  mapEquipmentToDisplay,
  mapInstanceToDisplay,
} from '../equipment-display';
import { Equipment, EquipmentInstance } from '../../../models/item';

@Component({
  selector: 'app-equipment-display',
  standalone: true,
  imports: [NgIf, NgFor, AttributeTypeFormatPipe, DecimalPipe],
  templateUrl: './equipment-display.component.html',
})
export class EquipmentDisplayComponent {
  @Input({ required: true }) item!: Equipment | EquipmentInstance;

  /** The view-model the template binds to */
  data!: EquipmentDisplay;

  ngOnChanges(): void {
    console.log(this.item);
    this.data = isInstance(this.item)
      ? mapInstanceToDisplay(this.item)
      : mapEquipmentToDisplay(this.item);
  }
}

function isInstance(obj: any): obj is EquipmentInstance {
  // simplest discriminant: only an instance has itemBase
  return 'itemBase' in obj;
}
