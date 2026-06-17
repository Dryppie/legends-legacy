import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import {
  AttributeTypeFormatPipe,
  isPercentAttribute,
} from '../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import {
  EquipmentDisplay,
  mapEquipmentToDisplay,
  mapInstanceToDisplay,
} from '../equipment-display';
import { Equipment, EquipmentInstance } from '../../../models/item';
import { AttributeModifier, ModifierType } from '../../../models/Dtos/attributesDto';
import { Rarity } from '../../../models/enums/rarity';
import { EquipmentType } from '../../../models/enums/equipmentType';

@Component({
  selector: 'app-equipment-display',
  standalone: true,
  imports: [
    NgIf,
    NgFor,
    NgClass,
    AttributeTypeFormatPipe,
    AttributeValueFormatPipe,
    DecimalPipe,
  ],
  templateUrl: './equipment-display.component.html',
})
export class EquipmentDisplayComponent {
  @Input({ required: true }) item!: Equipment | EquipmentInstance;
  modifierType = ModifierType;
  equipmentType = EquipmentType;
  /** The view-model the template binds to */
  data!: EquipmentDisplay;

  ngOnChanges(): void {
    this.data = isInstance(this.item)
      ? mapInstanceToDisplay(this.item)
      : mapEquipmentToDisplay(this.item);
  }

  get rarityClasses() {
    const rarity = this.item.rarity;

    switch (rarity) {
      case Rarity.Common:
        return 'text-slate-200';
      case Rarity.Uncommon:
        return 'text-emerald-600';
      case Rarity.Rare:
        return 'text-blue-600';
      case Rarity.Epic:
        return 'text-fuchsia-600';
      case Rarity.Unique:
        return 'text-yellow-400';
      case Rarity.Legendary:
        return 'text-orange-600';
      case Rarity.Legacy:
        return 'text-rose-700';
      default:
        return 'text-light_gray';
    }
  }

  shouldAppendModifierPercent(attribute: AttributeModifier): boolean {
    return (
      attribute.modifierType !== ModifierType.Flat &&
      !isPercentAttribute(attribute.attributeType)
    );
  }

  formatToolBonusType(type: string): string {
    return type.replace(/Percent$/, ' %').replace(/([A-Z])/g, ' $1').trim();
  }
}

function isInstance(obj: any): obj is EquipmentInstance {
  // simplest discriminant: only an instance has itemBase
  return 'itemBase' in obj;
}
