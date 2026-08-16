import { NgClass, NgFor, NgIf } from '@angular/common';
import {
  Component,
  DestroyRef,
  EventEmitter,
  Input,
  OnInit,
  Output,
  inject,
} from '@angular/core';
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
import {
  EquipmentComparison,
  EquipmentService,
} from '../../../../../core/services/api/equipment/equipment.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AttributeTypeFormatPipe } from '../../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../../pipes/attributes/attribute-value-format/attribute-value-format.pipe';

@Component({
  selector: 'app-inventory-equipment-modal',
  imports: [
    EquipmentDisplayComponent,
    InventoryTransferComponent,
    NgClass,
    NgFor,
    NgIf,
    AttributeTypeFormatPipe,
    AttributeValueFormatPipe,
  ],
  templateUrl: './inventory-equipment-modal.component.html',
})
export class InventoryEquipmentModalComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  @Input() equipmentInstance!: EquipmentInstance;
  @Input() slotType: EquipmentSlotType | null = null;
  equipment!: Equipment;
  selectedSlotType!: EquipmentSlotType;
  equippedComparisons: EquippedComparison[] = [];
  characterComparison: EquipmentComparison | null = null;
  comparisonLoading = false;
  @Output() close = new EventEmitter<void>();

  constructor(
    readonly equipmentState: EquipmentStateService,
    private inventoryState: InventoryStateService,
    private equipmentApi: EquipmentService,
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
    this.loadCharacterComparison();
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
    this.loadCharacterComparison();
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

  private loadCharacterComparison(): void {
    this.comparisonLoading = true;
    this.characterComparison = null;
    this.equipmentApi
      .compareEquipment(this.equipmentInstance.id, this.selectedSlotType)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (comparison) => {
          this.characterComparison = comparison;
          this.comparisonLoading = false;
        },
        error: () => {
          this.comparisonLoading = false;
        },
      });
  }
}
