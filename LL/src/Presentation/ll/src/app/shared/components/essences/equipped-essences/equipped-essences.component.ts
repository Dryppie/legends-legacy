import { NgFor } from '@angular/common';
import { Component, Input } from '@angular/core';
import { Essence } from '../../../models/essence';
import { ModalService } from '../../../../core/services/modal/modal.service';
import { EssenceSlot } from '../../../models/essenceSlot';

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

  openEssenceModal(essence: Essence) {
    this.modalService.toggleEssenceModal(essence); // Pass the essence from the Item to display all necessary info
  }
}
