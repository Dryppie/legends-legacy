import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import {
  AttributeTypeFormatPipe,
  isPercentAttribute,
} from '../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import {
  EquipmentDisplay,
  EquipmentAttributeRollRange,
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
import { AttributeType } from '../../../models/enums/attributeType';
import { EquipmentSlotType } from '../../../models/Dtos/equipment-slots/equipmentSlot';
import { sortAttributes } from '../../../utils/attributes/attribute-order.utils';
import { EquippedComparison } from '../../../utils/equipment/equipment.utils';

interface EquipmentComparisonView {
  slotType: EquipmentSlotType | null;
  data: EquipmentDisplay;
  rows: EquipmentAttributeComparison[];
  toolRows: ToolBonusComparison[];
}

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
  styleUrl: './equipment-display.component.css',
})
export class EquipmentDisplayComponent {
  @Input({ required: true }) item!: Equipment | EquipmentInstance;
  @Input() useBaseName = false;
  @Input() comparisonItem: EquipmentInstance | null = null;
  @Input() comparisonItems: readonly EquippedComparison[] = [];
  @Input() comparisonSubjectLabel = 'Hovered';
  @Input() fitComparisonToContainer = false;
  @Input() inlineComparison = false;
  @Input() embedded = false;
  @Input() compactCraftingDesign = false;
  @Input() showPossibleUpgradeAttributes = false;
  modifierType = ModifierType;
  equipmentType = EquipmentType;
  /** The view-model the template binds to */
  data!: EquipmentDisplay;
  comparisonData: EquipmentDisplay | null = null;
  comparisonRows: EquipmentAttributeComparison[] = [];
  toolComparisonRows: ToolBonusComparison[] = [];
  comparisonViews: EquipmentComparisonView[] = [];
  comparisonAttributeRows: EquipmentAttributeComparison[] = [];

  ngOnChanges(): void {
    this.data = isInstance(this.item)
      ? mapInstanceToDisplay(this.item)
      : mapEquipmentToDisplay(this.item, this.useBaseName);
    const comparisonTargets = this.comparisonItems.length
      ? this.comparisonItems
      : this.comparisonItem
        ? [{ slotType: null, equipmentInstance: this.comparisonItem }]
        : [];
    this.comparisonViews = comparisonTargets.map((comparison) => {
      const equipped = mapInstanceToDisplay(comparison.equipmentInstance);
      return {
        slotType: comparison.slotType,
        data: equipped,
        rows: buildAttributeComparisons(this.data, equipped),
        toolRows: buildToolBonusComparisons(this.data, equipped),
      };
    });
    this.comparisonData = this.comparisonViews[0]?.data ?? null;
    this.comparisonRows = this.comparisonData
      ? buildAttributeComparisons(this.data, this.comparisonData)
      : [];
    this.toolComparisonRows = this.comparisonData
      ? buildToolBonusComparisons(this.data, this.comparisonData)
      : [];
    const comparisonRowsByAttribute = new Map<
      AttributeType,
      EquipmentAttributeComparison
    >();
    for (const comparison of this.comparisonViews) {
      for (const row of comparison.rows) {
        if (!comparisonRowsByAttribute.has(row.attributeType)) {
          comparisonRowsByAttribute.set(row.attributeType, {
            ...row,
            equippedAmount: 0,
          });
        }
      }
    }
    this.comparisonAttributeRows = sortAttributes([
      ...comparisonRowsByAttribute.values(),
    ]);
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

  get isTool(): boolean {
    return this.data?.equipmentType === EquipmentType.Tool;
  }

  get sideBySideComparisons(): EquipmentComparisonView[] {
    return this.isTool ? [] : this.comparisonViews;
  }

  get hasToolDetails(): boolean {
    return this.isTool && this.data.toolBonuses.length > 0;
  }

  get isInstanceItem(): boolean {
    return isInstance(this.item);
  }

  get attributeSectionLabel(): string {
    return this.isTool || !this.isInstanceItem
      ? 'Attributes'
      : 'Rolled attributes';
  }

  equipmentMeta(item: EquipmentDisplay): string {
    return [
      this.formatDisplayLabel(item.equipmentType),
      item.quality
        ? `${this.formatDisplayLabel(item.quality)} quality`
        : this.formatDisplayLabel(item.rarity),
    ].join(' · ');
  }

  attributeRollRange(
    item: EquipmentDisplay,
    attributeType: AttributeType,
  ): EquipmentAttributeRollRange | null {
    return (
      item.attributeRollRanges?.find(
        (range) => range.attributeType === attributeType,
      ) ?? null
    );
  }

  rollPercentage(value: number, minimum: number, maximum: number): number {
    if (maximum <= minimum) return 100;
    return Math.min(
      100,
      Math.max(0, ((value - minimum) / (maximum - minimum)) * 100),
    );
  }

  formatToolBonusType(type: string): string {
    return type
      .replace(/Percent$/, '')
      .replace(/^Specific/, '')
      .replace(/([A-Z])/g, ' $1')
      .trim();
  }

  private formatDisplayLabel(value: string): string {
    return value
      .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
      .replace(/[_-]+/g, ' ')
      .replace(/^./, (character) => character.toUpperCase());
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

  comparisonSlotLabel(slotType: EquipmentSlotType | null): string {
    if (!slotType) return 'Equipped';
    return `Equipped · ${slotType.replace(/([A-Z])/g, ' $1').trim()}`;
  }

  usesV16EquipmentPresentation(item: EquipmentDisplay): boolean {
    return item.statModelVersion >= 16;
  }

  equippedAmount(
    comparison: EquipmentComparisonView,
    attributeType: AttributeType,
  ): number {
    return (
      comparison.rows.find((row) => row.attributeType === attributeType)
        ?.equippedAmount ?? 0
    );
  }

  hasAttribute(item: EquipmentDisplay, attributeType: AttributeType): boolean {
    return item.attributes.some(
      (attribute) => attribute.attributeType === attributeType,
    );
  }

  hasToolBonus(
    item: EquipmentDisplay,
    comparison: ToolBonusComparison,
  ): boolean {
    return item.toolBonuses.some(
      (bonus) =>
        bonus.bonusType === comparison.bonusType &&
        bonus.scopeId === comparison.scopeId,
    );
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
