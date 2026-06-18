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

  get rarityBadgeClasses() {
    const rarity = this.item.rarity;

    switch (rarity) {
      case Rarity.Common:
        return 'border-slate-300/30 bg-slate-300/10 text-slate-100';
      case Rarity.Uncommon:
        return 'border-emerald-400/40 bg-emerald-500/10 text-emerald-300';
      case Rarity.Rare:
        return 'border-blue-400/40 bg-blue-500/10 text-blue-300';
      case Rarity.Epic:
        return 'border-fuchsia-400/40 bg-fuchsia-500/10 text-fuchsia-300';
      case Rarity.Unique:
        return 'border-yellow-300/40 bg-yellow-400/10 text-yellow-200';
      case Rarity.Legendary:
        return 'border-orange-400/40 bg-orange-500/10 text-orange-300';
      case Rarity.Legacy:
        return 'border-rose-400/40 bg-rose-500/10 text-rose-300';
      default:
        return 'border-primary/40 text-primary';
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
