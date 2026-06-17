import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { NgFor } from '@angular/common';
import { Equipment, EquipmentInstance } from '../../../../models/item';
import { AttributeTypeFormatPipe } from '../../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../../pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import { EquipmentStateService } from '../../../../../core/services/api/equipment/equipment-state.service';
import { EquipmentTypePipe } from '../../../../pipes/equipment/equipment-type-format/equipment-type.pipe';
import { EquipmentSlotType } from '../../../../models/Dtos/equipment-slots/equipmentSlot';
import { getSlotTypeFromEquipmentType } from '../../../../utils/equipment/equipment.utils';

@Component({
  selector: 'app-inventory-equipment-modal',
  standalone: true,
  imports: [
    NgFor,
    AttributeTypeFormatPipe,
    AttributeValueFormatPipe,
    EquipmentTypePipe,
  ],
  templateUrl: './inventory-equipment-modal.component.html',
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
