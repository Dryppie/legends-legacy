import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { NgFor } from '@angular/common';
import { Equipment, EquipmentInstance } from '../../../../models/item';
import { AttributeTypeFormatPipe } from '../../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { EquipmentStateService } from '../../../../../core/services/api/equipment/equipment-state.service';
import { EquipmentTypePipe } from '../../../../pipes/equipment/equipment-type-format/equipment-type.pipe';

@Component({
  selector: 'app-inventory-equipment-modal',
  standalone: true,
  imports: [NgFor, AttributeTypeFormatPipe, EquipmentTypePipe],
  templateUrl: './inventory-equipment-modal.component.html',
})
export class InventoryEquipmentModalComponent implements OnInit {
  @Input() equipmentInstance!: EquipmentInstance;
  equipment!: Equipment;
  @Output() close = new EventEmitter<void>();

  constructor(private equipmentState: EquipmentStateService) {}

  ngOnInit(): void {
    this.equipment = this.equipmentInstance.itemBase as Equipment;
  }

  onEquip(): void {
    this.equipmentState.equip(this.equipmentInstance);
    this.onClose();
  }

  onClose() {
    this.close.emit();
  }
}
