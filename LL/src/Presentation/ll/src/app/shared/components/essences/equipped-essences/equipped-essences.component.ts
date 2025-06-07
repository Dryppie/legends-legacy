import { NgFor } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import { Essence } from '../../../models/essence';
import { ModalService } from '../../../../core/services/client-side/modal/modal.service';
import { EssenceSlot, SlotState } from '../../../models/essenceSlot';

@Component({
  selector: 'app-equipped-essences',
  standalone: true,
  imports: [NgFor],
  templateUrl: './equipped-essences.component.html',
})
export class EquippedEssencesComponent implements OnInit {
  @Input() essenceSlots: EssenceSlot[] = [];
  activeSlotsAmount: string = '';
  reservedSlotsAmount: string = '';
  activeEssenceSlots: EssenceSlot[] = [];
  reservedEssenceSlots: EssenceSlot[] = [];

  constructor(private modalService: ModalService) {}

  ngOnInit(): void {
    this.setActiveEssenceSlots();
    this.setReservedEssenceSlots();
    this.setActiveAndReservedSlotsAmount();
  }

  openEssenceModal(essence?: Essence) {
    this.modalService.toggleEssenceModal(essence); // Pass the essence from the Item to display all necessary info
  }

  setActiveEssenceSlots() {
    this.activeEssenceSlots = this.sortEssenceSlots(
      this.essenceSlots.filter((slot) => slot.slotState === SlotState.Active),
    );
  }

  setReservedEssenceSlots() {
    this.reservedEssenceSlots = this.sortEssenceSlots(
      this.essenceSlots.filter((slot) => slot.slotState === SlotState.Reserved),
    );
  }

  setActiveAndReservedSlotsAmount() {
    const occupiedActive = this.activeEssenceSlots.filter(
      (es) => es.occupiedEssence != null,
    ).length;
    this.activeSlotsAmount = `${occupiedActive}/${this.activeEssenceSlots.length}`;

    const occupiedReserved = this.reservedEssenceSlots.filter(
      (es) => es.occupiedEssence != null,
    ).length;
    this.reservedSlotsAmount = `${occupiedReserved}/${this.reservedEssenceSlots.length}`;
  }

  private sortEssenceSlots(slots: EssenceSlot[]): EssenceSlot[] {
    return slots.sort((a, b) => {
      const aHasEssence = !!a.occupiedEssence;
      const bHasEssence = !!b.occupiedEssence;
      return Number(bHasEssence) - Number(aHasEssence); // Sorts so occupiedEssence comes first
    });
  }
}
