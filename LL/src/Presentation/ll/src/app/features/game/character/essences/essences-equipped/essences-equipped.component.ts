import { NgFor, NgIf, NgClass } from '@angular/common';
import { Component, computed, Input, Signal, signal } from '@angular/core';
import { EssenceStateService } from '../../../../../core/services/api/essences/essence-state.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { RegularButtonComponent } from '../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import {
  EssenceSlot,
  SlotState,
} from '../../../../../shared/models/essenceSlot';
import { EssenceDetailsComponent } from '../../../../../shared/components/essences/essence-details/essence-details.component';
import { TourService } from '../../../../../core/services/client-side/tutorial-tour/tour.service';

@Component({
  selector: 'app-essences-equipped',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    NgClass,
    RegularButtonComponent,
    EssenceDetailsComponent,
  ],
  templateUrl: './essences-equipped.component.html',
})
export class EssencesEquippedComponent {
  @Input({ required: true }) essenceSlots!: Signal<EssenceSlot[]>;

  readonly activeSlots = computed(() => {
    return this.essenceSlots()
      .filter((es) => es.slotState === SlotState.Active)
      .sort((a, b) => {
        const aHasEssence = !!a.occupiedEssence;
        const bHasEssence = !!b.occupiedEssence;

        if (aHasEssence === bHasEssence) return 0;
        return aHasEssence ? -1 : 1;
      });
  });

  readonly reservedSlots = computed(() => {
    return this.essenceSlots()
      .filter((es) => es.slotState === SlotState.Reserved)
      .sort((a, b) => {
        const aHasEssence = !!a.occupiedEssence;
        const bHasEssence = !!b.occupiedEssence;

        if (aHasEssence === bHasEssence) return 0;
        return aHasEssence ? -1 : 1;
      });
  });

  private readonly selectedEssenceSlotId = signal<string | null>(null);
  readonly selectedSlot = computed<EssenceSlot | null>(() => {
    const id = this.selectedEssenceSlotId();
    return id ? (this.essenceSlots().find((r) => r.id === id) ?? null) : null;
  });

  constructor(
    private essenceState: EssenceStateService,
    private inventoryState: InventoryStateService,
    private readonly tour: TourService,
  ) {
    this.tour.start('character-essences');
  }

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
