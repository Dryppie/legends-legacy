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
import {
  Equipment,
  EquipmentInstance,
  ToolBonusModifier,
} from '../../../models/item';
import {
  AttributeModifier,
  ModifierType,
} from '../../../models/Dtos/attributesDto';
import { Rarity } from '../../../models/enums/rarity';
import { EquipmentType } from '../../../models/enums/equipmentType';
import { AttributeTooltipDirective } from '../../../directives/attribute-tooltip/attribute-tooltip.directive';

@Component({
    selector: 'app-equipment-display',
    imports: [
        NgIf,
        NgFor,
        NgClass,
        AttributeTypeFormatPipe,
        AttributeValueFormatPipe,
        AttributeTooltipDirective,
        DecimalPipe,
    ],
    templateUrl: './equipment-display.component.html'
})
export class EquipmentDisplayComponent {
  @Input({ required: true }) item!: Equipment | EquipmentInstance;
  @Input() useBaseName = false;
  modifierType = ModifierType;
  equipmentType = EquipmentType;
  /** The view-model the template binds to */
  data!: EquipmentDisplay;

  ngOnChanges(): void {
    this.data = isInstance(this.item)
      ? mapInstanceToDisplay(this.item)
      : mapEquipmentToDisplay(this.item, this.useBaseName);
  }

  get rarityClasses() {
    const rarity = this.item.rarity;

    switch (rarity) {
      case Rarity.Common:
        return 'll-rarity-common';
      case Rarity.Uncommon:
        return 'll-rarity-uncommon';
      case Rarity.Rare:
        return 'll-rarity-rare';
      case Rarity.Epic:
        return 'll-rarity-epic';
      case Rarity.Unique:
        return 'll-rarity-unique';
      case Rarity.Legendary:
        return 'll-rarity-legendary';
      case Rarity.Legacy:
        return 'll-rarity-legacy';
      default:
        return 'll-text-muted';
    }
  }

  shouldAppendModifierPercent(attribute: AttributeModifier): boolean {
    return (
      attribute.modifierType !== ModifierType.Flat &&
      !isPercentAttribute(attribute.attributeType)
    );
  }

  get rarityBadgeClasses() {
    const rarity = this.item.rarity;

    switch (rarity) {
      case Rarity.Common:
        return 'll-rarity-common';
      case Rarity.Uncommon:
        return 'll-rarity-uncommon';
      case Rarity.Rare:
        return 'll-rarity-rare';
      case Rarity.Epic:
        return 'll-rarity-epic';
      case Rarity.Unique:
        return 'll-rarity-unique';
      case Rarity.Legendary:
        return 'll-rarity-legendary';
      case Rarity.Legacy:
        return 'll-rarity-legacy';
      default:
        return 'll-item-chip-accent';
    }
  }

  get isTool(): boolean {
    return this.data?.equipmentType === EquipmentType.Tool;
  }

  get hasToolDetails(): boolean {
    return (
      this.isTool &&
      (this.data.toolAffixes.length > 0 || this.data.baseToolBonuses.length > 0)
    );
  }

  get toolAffixSummary(): string {
    const count = this.data?.toolAffixes?.length ?? 0;
    return count === 1 ? '1 Affix' : `${count} Affixes`;
  }

  formatToolBonusType(type: string): string {
    return type
      .replace(/Percent$/, '')
      .replace(/^Specific/, '')
      .replace(/([A-Z])/g, ' $1')
      .trim();
  }

  formatToolBonusAmount(bonus: ToolBonusModifier): string {
    const value = new Intl.NumberFormat(undefined, {
      maximumFractionDigits: 2,
    }).format(bonus.amount);

    return bonus.bonusType.endsWith('Percent') ? `+${value}%` : `+${value}`;
  }
}

function isInstance(obj: any): obj is EquipmentInstance {
  // simplest discriminant: only an instance has itemBase
  return 'itemBase' in obj;
}
