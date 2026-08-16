import { Component, EventEmitter, Input, Output } from '@angular/core';
import { EquipmentService } from '../../../../../core/services/api/equipment/equipment.service';
import { NgFor } from '@angular/common';
import { Equipment } from '../../../../models/item';
import {
  AttributeTypeFormatPipe,
  AttributeValueFormatPipe,
} from '../../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';

@Component({
  selector: 'app-inventory-equipment-modal',
  standalone: true,
  imports: [NgFor, AttributeTypeFormatPipe, AttributeValueFormatPipe],
  templateUrl: './inventory-equipment-modal.component.html',
  styleUrl: './inventory-equipment-modal.component.css',
})
export class InventoryEquipmentModalComponent {
  @Input() equipment!: Equipment;
  @Output() close = new EventEmitter<void>();

  constructor(private equipmentService: EquipmentService) {}

  onEquip(): void {
    this.equipmentService.equipEquipment(this.equipment);
  }

  onClose() {
    this.close.emit();
  }
}
