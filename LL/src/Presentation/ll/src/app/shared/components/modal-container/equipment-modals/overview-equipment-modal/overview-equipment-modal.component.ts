import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Equipment, EquipmentInstance } from '../../../../models/item';
import {
  AttributeTypeFormatPipe,
} from '../../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../../pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import { NgClass, NgFor, NgIf } from '@angular/common';
import { EquipmentSlotType } from '../../../../models/Dtos/equipment-slots/equipmentSlot';
import { getAllowedEquipmentTypesForSlot } from '../../../../utils/equipment/equipment.utils';
import { EquipmentStateService } from '../../../../../core/services/api/equipment/equipment-state.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { EquipmentTypePipe } from '../../../../pipes/equipment/equipment-type-format/equipment-type.pipe';
import { AttributeDisplayPipe } from '../../../../pipes/attributes/attribute-display/attribute-display.pipe';
import { AttributeTooltipDirective } from '../../../../directives/attribute-tooltip/attribute-tooltip.directive';

@Component({
    selector: 'app-overview-equipment-modal',
    imports: [
        AttributeTypeFormatPipe,
        AttributeValueFormatPipe,
        AttributeDisplayPipe,
        AttributeTooltipDirective,
        EquipmentTypePipe,
        NgIf,
        NgFor,
        NgClass,
    ],
    templateUrl: './overview-equipment-modal.component.html'
})
export class OverviewEquipmentModalComponent implements OnInit {
  @Input() equipmentSlotType!: EquipmentSlotType;
  equipmentInstances!: EquipmentInstance[] | null;
  selectedEquipmentInstance!: EquipmentInstance;
  selectedEquipment!: Equipment;
  currentEquippedEquipment?: EquipmentInstance;
  @Output() close = new EventEmitter<void>();

  constructor(
    private inventoryState: InventoryStateService,
    private equipmentState: EquipmentStateService,
  ) {}
  ngOnInit(): void {
    const items = this.inventoryState.items();
    const allItems =
      items.map((ii) => ii.itemInstance as EquipmentInstance) ?? [];

    const allowedTypes = getAllowedEquipmentTypesForSlot(
      this.equipmentSlotType,
    );

    this.equipmentInstances = allItems.filter((ii) =>
      allowedTypes.includes((ii.itemBase as Equipment).equipmentType),
    );

    const equipmentSlots = this.equipmentState.equipmentSlots();
    this.currentEquippedEquipment = equipmentSlots.find(
      (ei) => ei.equipmentSlotType === this.equipmentSlotType,
    )?.equipmentInstance;
  }

  selectEquipment(equipment: EquipmentInstance) {
    this.selectedEquipmentInstance = equipment;
  }

  onEquip(): void {
    this.equipmentState.equip(
      this.selectedEquipmentInstance,
      this.equipmentSlotType,
    );

    this.onClose();
  }

  onUnequip() {
    this.equipmentState.unequip(this.equipmentSlotType);
    this.onClose();
  }

  onClose() {
    this.close.emit();
  }
}
