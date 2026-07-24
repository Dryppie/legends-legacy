import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Equipment, EquipmentInstance } from '../../../../models/item';
import { EquipmentStateService } from '../../../../../core/services/api/equipment/equipment-state.service';
import { EquipmentSlotType } from '../../../../models/Dtos/equipment-slots/equipmentSlot';
import { getSlotTypeFromEquipmentType } from '../../../../utils/equipment/equipment.utils';
import { EquipmentDisplayComponent } from '../../../equipment/equipment-display/equipment-display.component';

@Component({
    selector: 'app-inventory-equipment-modal',
    imports: [
        EquipmentDisplayComponent,
    ],
    templateUrl: './inventory-equipment-modal.component.html'
})
export class InventoryEquipmentModalComponent implements OnInit {
  @Input() equipmentInstance!: EquipmentInstance;
  @Input() slotType!: EquipmentSlotType;
  equipment!: Equipment;
  @Output() close = new EventEmitter<void>();

  constructor(private equipmentState: EquipmentStateService) {}

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
