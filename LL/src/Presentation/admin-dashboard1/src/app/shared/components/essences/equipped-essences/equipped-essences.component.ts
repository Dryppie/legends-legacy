import { NgFor } from '@angular/common';
import { Component, Input } from '@angular/core';
import { Essence } from '../../../models/essence';
import { ModalService } from '../../../../core/services/client-side/modal/modal.service';
import { EssenceSlot, SlotState } from '../../../models/essenceSlot';

@Component({
  selector: 'app-equipped-essences',
  standalone: true,
  imports: [NgFor],
  templateUrl: './equipped-essences.component.html',
  styleUrl: './equipped-essences.component.css',
})
export class EquippedEssencesComponent {
  @Input() essenceSlots: EssenceSlot[] = [];

  constructor(private modalService: ModalService) {}

  openEssenceModal(essence?: Essence) {
    this.modalService.toggleEssenceModal(essence); // Pass the essence from the Item to display all necessary info
  }

  get activeEssenceSlots(): EssenceSlot[] {
    return this.sortEssenceSlots(
      this.essenceSlots.filter((slot) => slot.slotState === SlotState.Active),
    );
  }

  get reservedEssenceSlots(): EssenceSlot[] {
    return this.sortEssenceSlots(
      this.essenceSlots.filter((slot) => slot.slotState === SlotState.Reserved),
    );
  }

  private sortEssenceSlots(slots: EssenceSlot[]): EssenceSlot[] {
    return slots.sort((a, b) => {
      const aHasEssence = !!a.occupiedEssence;
      const bHasEssence = !!b.occupiedEssence;
      return Number(bHasEssence) - Number(aHasEssence); // Sorts so occupiedEssence comes first
    });
  }
}
