import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { EquipmentService } from '../../../../../core/services/api/equipment/equipment.service';
import { NgFor } from '@angular/common';
import { Equipment, EquipmentInstance } from '../../../../models/item';
import { AttributeTypeFormatPipe } from '../../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';

@Component({
  selector: 'app-inventory-equipment-modal',
  standalone: true,
  imports: [NgFor, AttributeTypeFormatPipe],
  templateUrl: './inventory-equipment-modal.component.html',
  styleUrl: './inventory-equipment-modal.component.css',
})
export class InventoryEquipmentModalComponent implements OnInit {
  @Input() equipmentInstance!: EquipmentInstance;
  equipment!: Equipment;
  @Output() close = new EventEmitter<void>();

  constructor(private equipmentService: EquipmentService) {}

  ngOnInit(): void {
    this.equipment = this.equipmentInstance.itemBase as Equipment;
  }

  onEquip(): void {
    this.equipmentService.equipEquipment(this.equipmentInstance);
    this.onClose();
  }

  onClose() {
    this.close.emit();
  }
}
