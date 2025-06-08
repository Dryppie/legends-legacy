import { NgFor, NgIf, NgClass } from '@angular/common';
import { Component, computed, Input, Signal, signal } from '@angular/core';
import { EssenceStateService } from '../../../../../core/services/api/essences/essence-state.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { RegularButtonComponent } from '../../../../../shared/components/buttons/regular-button/regular-button.component';
import { EssenceDescriptionComponent } from '../../../../../shared/components/essences/essence-description/essence-description.component';
import {
  EssenceSlot,
  SlotState,
} from '../../../../../shared/models/essenceSlot';
import { AttributeTypeFormatPipe } from '../../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';

@Component({
  selector: 'app-essences-equipped',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    NgClass,
    AttributeTypeFormatPipe,
    RegularButtonComponent,
    EssenceDescriptionComponent,
  ],
  templateUrl: './essences-equipped.component.html',
})
export class EssencesEquippedComponent {
  @Input({ required: true }) essenceSlots!: Signal<EssenceSlot[]>;

  readonly activeSlots = computed(() => {
    return this.essenceSlots().filter(
      (es) => es.slotState === SlotState.Active,
    );
  });

  readonly reservedSlots = computed(() => {
    return this.essenceSlots().filter(
      (es) => es.slotState === SlotState.Reserved,
    );
  });

  private readonly selectedEssenceSlotId = signal<string | null>(null);
  readonly selectedSlot = computed<EssenceSlot | null>(() => {
    const id = this.selectedEssenceSlotId();
    return id ? (this.essenceSlots().find((r) => r.id === id) ?? null) : null;
  });

  constructor(
    private essenceState: EssenceStateService,
    private inventoryState: InventoryStateService,
  ) {}

  selectEssence(essenceSlot: EssenceSlot): void {
    this.selectedEssenceSlotId.set(essenceSlot.id);
  }

  canEquipSelected(): boolean {
    return true;
  }

  remove() {
    const essenceSlot = this.selectedSlot();
    const inventoryItemId = this.selectedEssenceSlotId();
    if (!essenceSlot || !essenceSlot.occupiedEssence || !inventoryItemId)
      return;
    this.essenceState.remove(essenceSlot.occupiedEssence.id);
    this.inventoryState.removeItem(inventoryItemId);
    this.selectedEssenceSlotId.set(null);
  }
}
