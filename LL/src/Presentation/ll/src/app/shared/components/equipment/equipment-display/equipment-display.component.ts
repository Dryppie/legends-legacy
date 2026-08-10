import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import {
  AttributeTypeFormatPipe,
  isPercentAttribute,
} from '../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import {
  EquipmentDisplay,
  EquipmentAttributeComparison,
  ToolBonusComparison,
  buildAttributeComparisons,
  buildToolBonusComparisons,
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
  templateUrl: './equipment-display.component.html',
})
export class EquipmentDisplayComponent {
  @Input({ required: true }) item!: Equipment | EquipmentInstance;
  @Input() useBaseName = false;
  @Input() comparisonItem: EquipmentInstance | null = null;
  @Input() compactCraftingDesign = false;
  @Input() showPossibleUpgradeAttributes = false;
  modifierType = ModifierType;
  equipmentType = EquipmentType;
  /** The view-model the template binds to */
  data!: EquipmentDisplay;
  comparisonData: EquipmentDisplay | null = null;
  comparisonRows: EquipmentAttributeComparison[] = [];
  toolComparisonRows: ToolBonusComparison[] = [];

  ngOnChanges(): void {
    this.data = isInstance(this.item)
      ? mapInstanceToDisplay(this.item)
      : mapEquipmentToDisplay(this.item, this.useBaseName);
    this.comparisonData = this.comparisonItem
      ? mapInstanceToDisplay(this.comparisonItem)
      : null;
    this.comparisonRows = this.comparisonData
      ? buildAttributeComparisons(this.data, this.comparisonData)
      : [];
    this.toolComparisonRows = this.comparisonData
      ? buildToolBonusComparisons(this.data, this.comparisonData)
      : [];
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

  get sideBySideComparison(): EquipmentDisplay | null {
    return this.isTool ? null : this.comparisonData;
  }

  get hasToolDetails(): boolean {
    return this.isTool && this.data.toolBonuses.length > 0;
  }

  formatToolBonusType(type: string): string {
    return type
      .replace(/Percent$/, '')
      .replace(/^Specific/, '')
      .replace(/([A-Z])/g, ' $1')
      .trim();
  }

  formatToolBonusAmount(bonus: ToolBonusModifier): string {
    return this.formatToolBonusValue(bonus.amount, bonus.bonusType, true);
  }

  formatToolBonusValue(
    amount: number,
    bonusType: string,
    forceSign = false,
  ): string {
    const value = new Intl.NumberFormat(undefined, {
      maximumFractionDigits: 2,
    }).format(amount);
    const sign = forceSign && amount > 0 ? '+' : '';

    return bonusType.endsWith('Percent')
      ? `${sign}${value}%`
      : `${sign}${value}`;
  }

  formatToolBonusLabel(type: string, scopeId?: string): string {
    const label = this.formatToolBonusType(type);
    return scopeId ? `${label} · ${this.formatToolScope(scopeId)}` : label;
  }

  private formatToolScope(scopeId: string): string {
    return scopeId
      .replace(/[._-]+/g, ' ')
      .replace(/\b\w/g, (character) => character.toUpperCase());
  }

  possibleDesignAttributes(
    primary: readonly string[],
    secondary: readonly string[],
  ): string[] {
    return [...new Set([...primary, ...secondary])];
  }

  comparisonClass(difference: number): string {
    if (difference > 0) return 'text-emerald-400';
    if (difference < 0) return 'text-rose-400';
    return 'text-secondary';
  }

  comparisonMeta(item: EquipmentDisplay): string {
    return [
      item.equipmentType,
      item.craftingDesign?.role,
      item.rarity,
      item.quality,
    ]
      .filter((value): value is string => !!value)
      .join(' · ');
  }

  rarityClass(rarity: Rarity): string {
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
}

function isInstance(obj: any): obj is EquipmentInstance {
  // simplest discriminant: only an instance has itemBase
  return 'itemBase' in obj;
}
