import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Equipment, EquipmentInstance } from '../../../../models/item';
import { EquipmentService } from '../../../../../core/services/api/equipment/equipment.service';
import { AttributeTypeFormatPipe } from '../../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { NgClass, NgFor, NgIf } from '@angular/common';
import { EquipmentType } from '../../../../models/Dtos/equipmentSlot';
import { CharacterManagerService } from '../../../../../core/services/client-side/character-manager/character-manager.service';

@Component({
  selector: 'app-overview-equipment-modal',
  standalone: true,
  imports: [AttributeTypeFormatPipe, NgIf, NgFor, NgClass],
  templateUrl: './overview-equipment-modal.component.html',
  styleUrl: './overview-equipment-modal.component.css',
})
export class OverviewEquipmentModalComponent implements OnInit {
  @Input() equipmentType!: EquipmentType;
  equipmentInstances!: EquipmentInstance[] | null;
  selectedEquipmentInstance!: EquipmentInstance;
  selectedEquipment!: Equipment;
  currentEquippedEquipment?: EquipmentInstance;
  @Output() close = new EventEmitter<void>();

  constructor(
    private equipmentService: EquipmentService,
    private characterManager: CharacterManagerService,
  ) {}
  ngOnInit(): void {
    const inventory = this.characterManager.getInventory();
    this.equipmentInstances = inventory?.inventoryItems
      .map((ii) => ii.itemInstance as EquipmentInstance)
      .filter(
        (ii) => (ii.itemBase as Equipment).equipmentType === this.equipmentType,
      )!;
    const equippedItems = this.characterManager.getEquipment();
    this.currentEquippedEquipment = equippedItems.find(
      (ei) => ei.equipmentType === this.equipmentType,
    )?.equipmentInstance;
  }

  selectEquipment(equipment: EquipmentInstance) {
    this.selectedEquipmentInstance = equipment;
  }

  onEquip(): void {
    this.equipmentService.equipEquipment(this.selectedEquipmentInstance);
    this.onClose();
  }

  onUnequip() {
    this.equipmentService.unequipEquipment(this.equipmentType);
    this.onClose();
  }

  onClose() {
    this.close.emit();
  }
}
