import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { AttributeTypeFormatPipe, isPercentAttribute } from '../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import {
  EquipmentAttributeComparison,
  EquipmentDisplay,
  buildAttributeComparisons,
  mapEquipmentToDisplay,
  mapInstanceToDisplay,
} from '../equipment-display';
import { Equipment, EquipmentInstance } from '../../../models/item';
import { AttributeModifier, ModifierType } from '../../../models/Dtos/attributesDto';
import { Rarity } from '../../../models/enums/rarity';
import { AttributeTooltipDirective } from '../../../directives/attribute-tooltip/attribute-tooltip.directive';
import { AttributeType } from '../../../models/enums/attributeType';
import { EquipmentSlotType } from '../../../models/Dtos/equipment-slots/equipmentSlot';
import { sortAttributes } from '../../../utils/attributes/attribute-order.utils';
import { EquippedComparison } from '../../../utils/equipment/equipment.utils';
import { EquipmentSetProgressComponent } from '../equipment-set-progress/equipment-set-progress.component';

interface EquipmentComparisonView {
  slotType: EquipmentSlotType | null;
  data: EquipmentDisplay;
  rows: EquipmentAttributeComparison[];
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
    EquipmentSetProgressComponent,
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
  @Input() equippedItems: readonly EquipmentInstance[] | null = null;

  modifierType = ModifierType;
  data!: EquipmentDisplay;
  comparisonData: EquipmentDisplay | null = null;
  comparisonRows: EquipmentAttributeComparison[] = [];
  comparisonViews: EquipmentComparisonView[] = [];
  comparisonAttributeRows: EquipmentAttributeComparison[] = [];

  ngOnChanges(): void {
    this.data = isInstance(this.item)
      ? mapInstanceToDisplay(this.item)
      : mapEquipmentToDisplay(this.item);
    const targets = this.comparisonItems.length
      ? this.comparisonItems
      : this.comparisonItem
        ? [{ slotType: null, equipmentInstance: this.comparisonItem }]
        : [];
    this.comparisonViews = targets.map((comparison) => {
      const equipped = mapInstanceToDisplay(comparison.equipmentInstance);
      return {
        slotType: comparison.slotType,
        data: equipped,
        rows: buildAttributeComparisons(this.data, equipped),
      };
    });
    this.comparisonData = this.comparisonViews[0]?.data ?? null;
    this.comparisonRows = this.comparisonData
      ? buildAttributeComparisons(this.data, this.comparisonData)
      : [];
    const rows = new Map<AttributeType, EquipmentAttributeComparison>();
    for (const comparison of this.comparisonViews) {
      for (const row of comparison.rows) {
        rows.set(row.attributeType, { ...row, equippedAmount: 0 });
      }
    }
    this.comparisonAttributeRows = sortAttributes([...rows.values()]);
  }

  get rarityClasses(): string {
    return this.rarityClass(this.item.rarity);
  }

  get sideBySideComparisons(): EquipmentComparisonView[] {
    return this.comparisonViews;
  }

  shouldAppendModifierPercent(attribute: AttributeModifier): boolean {
    return attribute.modifierType !== ModifierType.Flat && !isPercentAttribute(attribute.attributeType);
  }

  equipmentMeta(item: EquipmentDisplay): string {
    return [
      this.formatDisplayLabel(item.equipmentType),
      this.formatDisplayLabel(item.rarity),
      item.progression ? `Rank ${item.progression.rank}` : null,
      item.progression
        ? this.formatDisplayLabel(item.progression.activeStyleId?.split('.').pop()?.replace(/[_-]/g, ' ') || 'Plain')
        : null,
      item.progression
        ? item.progression.ownership === 'BoundPersonal'
          ? 'Bound'
          : item.progression.ownership === 'GuildOwned'
            ? 'Guild owned'
            : 'Unbound'
        : null,
      item.quality ? `${this.formatDisplayLabel(item.quality)} quality` : null,
    ].filter((label): label is string => !!label).join(' · ');
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
    return !!item.progression;
  }

  equippedAmount(comparison: EquipmentComparisonView, attributeType: AttributeType): number {
    return comparison.rows.find((row) => row.attributeType === attributeType)?.equippedAmount ?? 0;
  }

  hasAttribute(item: EquipmentDisplay, attributeType: AttributeType): boolean {
    return item.attributes.some((attribute) => attribute.attributeType === attributeType);
  }

  rarityClass(rarity: Rarity): string {
    return `ll-rarity-${rarity.toLowerCase()}`;
  }

  private formatDisplayLabel(value: string): string {
    return value
      .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
      .replace(/[_-]+/g, ' ')
      .replace(/^./, (character) => character.toUpperCase());
  }
}

function isInstance(value: Equipment | EquipmentInstance): value is EquipmentInstance {
  return 'itemBase' in value;
}
