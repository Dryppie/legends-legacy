import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Equipment, EquipmentInstance } from '../../../../models/item';
import { EquipmentStateService } from '../../../../../core/services/api/equipment/equipment-state.service';
import { EquipmentSlotType } from '../../../../models/Dtos/equipment-slots/equipmentSlot';
import {
  EquippedComparison,
  findEquippedComparisonForSlot,
  findEquippedComparisons,
  getEquipSlotOptions,
  getSlotTypeFromEquipmentType,
} from '../../../../utils/equipment/equipment.utils';
import { EquipmentDisplayComponent } from '../../../equipment/equipment-display/equipment-display.component';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { InventoryItem } from '../../../../models/inventoryItem';
import { InventoryTransferComponent } from '../../../inventory-transfer/inventory-transfer.component';
import { EquipmentType } from '../../../../models/enums/equipmentType';

@Component({
  selector: 'app-inventory-equipment-modal',
  imports: [
    EquipmentDisplayComponent,
    InventoryTransferComponent,
    NgClass,
    NgFor,
    NgIf,
  ],
  templateUrl: './inventory-equipment-modal.component.html',
})
export class InventoryEquipmentModalComponent implements OnInit {
  @Input() equipmentInstance!: EquipmentInstance;
  @Input() slotType: EquipmentSlotType | null = null;
  equipment!: Equipment;
  selectedSlotType!: EquipmentSlotType;
  equippedComparisons: EquippedComparison[] = [];
  @Output() close = new EventEmitter<void>();

  constructor(
    readonly equipmentState: EquipmentStateService,
    private inventoryState: InventoryStateService,
  ) {}

  get inventoryItem(): InventoryItem | undefined {
    return this.inventoryState
      .items()
      .find((item) => item.itemInstance.id === this.equipmentInstance.id);
  }

  ngOnInit(): void {
    this.equipment = this.equipmentInstance.itemBase as Equipment;
    this.selectedSlotType =
      this.slotType ??
      getSlotTypeFromEquipmentType(this.equipment.equipmentType);
    this.updateComparisons();
  }

  get equipSlotOptions(): EquipmentSlotType[] {
    return getEquipSlotOptions(this.equipment.equipmentType);
  }

  get requiresHandSelection(): boolean {
    return this.equipment.equipmentType === EquipmentType.OneHanded;
  }

  private updateComparisons(): void {
    const slots = this.equipmentState.equipmentSlots();
    if (this.requiresHandSelection) {
      const comparison = findEquippedComparisonForSlot(
        this.equipmentInstance,
        this.selectedSlotType,
        slots,
      );
      this.equippedComparisons = comparison ? [comparison] : [];
      return;
    }

    this.equippedComparisons = findEquippedComparisons(
      this.equipmentInstance,
      slots,
    );
  }

  selectSlot(slotType: EquipmentSlotType): void {
    this.selectedSlotType = slotType;
    this.updateComparisons();
  }

  slotLabel(slotType: EquipmentSlotType): string {
    return slotType === EquipmentSlotType.MainHand ? 'Main Hand' : 'Off Hand';
  }

  onEquip(): void {
    this.equipmentState.equip(this.equipmentInstance, this.selectedSlotType);
    this.onClose();
  }

  onClose() {
    this.close.emit();
  }
}
