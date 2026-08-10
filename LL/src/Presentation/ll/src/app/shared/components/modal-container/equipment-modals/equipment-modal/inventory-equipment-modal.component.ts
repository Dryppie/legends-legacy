import { NgIf } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Equipment, EquipmentInstance } from '../../../../models/item';
import { EquipmentStateService } from '../../../../../core/services/api/equipment/equipment-state.service';
import { EquipmentSlotType } from '../../../../models/Dtos/equipment-slots/equipmentSlot';
import { getSlotTypeFromEquipmentType } from '../../../../utils/equipment/equipment.utils';
import { EquipmentDisplayComponent } from '../../../equipment/equipment-display/equipment-display.component';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { InventoryItem } from '../../../../models/inventoryItem';
import { InventoryTransferComponent } from '../../../inventory-transfer/inventory-transfer.component';

@Component({
    selector: 'app-inventory-equipment-modal',
    imports: [EquipmentDisplayComponent, InventoryTransferComponent, NgIf],
    templateUrl: './inventory-equipment-modal.component.html'
})
export class InventoryEquipmentModalComponent implements OnInit {
  @Input() equipmentInstance!: EquipmentInstance;
  @Input() slotType!: EquipmentSlotType;
  equipment!: Equipment;
  @Output() close = new EventEmitter<void>();

  constructor(
    private equipmentState: EquipmentStateService,
    private inventoryState: InventoryStateService,
  ) {}

  get inventoryItem(): InventoryItem | undefined {
    return this.inventoryState
      .items()
      .find((item) => item.itemInstance.id === this.equipmentInstance.id);
  }

  ngOnInit(): void {
    this.equipment = this.equipmentInstance.itemBase as Equipment;
  }

  onEquip(): void {
    this.equipmentState.equip(
      this.equipmentInstance,
      this.slotType ?? getSlotTypeFromEquipmentType(this.equipment.equipmentType),
    );
    this.onClose();
  }

  onClose() {
    this.close.emit();
  }
}
