import { Component, EventEmitter, Input, Output } from '@angular/core';
import { EquipmentService } from '../../../../../core/services/api/equipment/equipment.service';
import { NgFor } from '@angular/common';
import { Equipment } from '../../../../models/item';

@Component({
  selector: 'app-equipment-modal',
  standalone: true,
  imports: [NgFor],
  templateUrl: './equipment-modal.component.html',
  styleUrl: './equipment-modal.component.css',
})
export class EquipmentModalComponent {
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
