import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { EssenceDescriptionComponent } from '../../essences/essence-description/essence-description.component';
import {
  EquipmentInstance,
  EquipmentSetBonusMetadata,
  EquipmentSetMetadata,
} from '../../../models/item';
import { EquipmentType } from '../../../models/enums/equipmentType';

@Component({
  selector: 'app-equipment-set-progress',
  imports: [NgClass, NgFor, NgIf, EssenceDescriptionComponent],
  templateUrl: './equipment-set-progress.component.html',
  styleUrl: './equipment-set-progress.component.css',
})
export class EquipmentSetProgressComponent {
  @Input({ required: true }) equipmentSet!: EquipmentSetMetadata;
  @Input() equippedItems: readonly EquipmentInstance[] | null = null;
  @Input() highlightAllBonuses = false;

  get equippedCount(): number {
    return this.matchingEquippedItems.reduce(
      (count, equipment) =>
        count +
        (equipment.equipmentBase.equipmentType === EquipmentType.TwoHanded
          ? 2
          : 1),
      0,
    );
  }

  get hasEquippedTwoHandedItem(): boolean {
    return this.matchingEquippedItems.some(
      (equipment) =>
        equipment.equipmentBase.equipmentType === EquipmentType.TwoHanded,
    );
  }

  get maximumThreshold(): number {
    return this.equipmentSet.bonuses.reduce(
      (maximum, bonus) => Math.max(maximum, bonus.requiredEquippedItems),
      0,
    );
  }

  bonusClass(bonus: EquipmentSetBonusMetadata): string {
    if (this.highlightAllBonuses) return 'equipment-set-bonus-active';
    if (this.equippedCount >= bonus.requiredEquippedItems) {
      return 'equipment-set-bonus-active';
    }

    const nextThreshold = this.equipmentSet.bonuses
      .filter(
        (candidate) => candidate.requiredEquippedItems > this.equippedCount,
      )
      .reduce(
        (minimum, candidate) =>
          Math.min(minimum, candidate.requiredEquippedItems),
        Number.POSITIVE_INFINITY,
      );
    return bonus.requiredEquippedItems === nextThreshold
      ? 'equipment-set-bonus-next'
      : 'equipment-set-bonus-locked';
  }

  bonusProgressLabel(bonus: EquipmentSetBonusMetadata): string {
    if (this.equippedCount >= bonus.requiredEquippedItems) return 'Active';

    const remaining = bonus.requiredEquippedItems - this.equippedCount;
    return `${remaining} more item${remaining === 1 ? '' : 's'}`;
  }

  private get matchingEquippedItems(): EquipmentInstance[] {
    if (!this.equippedItems) return [];

    const normalizedSetId = this.equipmentSet.id.toLowerCase();
    const matchingItems = new Map<string, EquipmentInstance>();
    this.equippedItems
      .filter(
        (equipment) =>
          equipment.equipmentSet?.id.toLowerCase() === normalizedSetId,
      )
      .forEach((equipment) => matchingItems.set(equipment.id, equipment));
    return Array.from(matchingItems.values());
  }
}
