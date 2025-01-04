import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule, NgFor, NgIf } from '@angular/common';
import { Essence } from '../../../../models/essence';
import { EssencesService } from '../../../../../core/services/essences/essences.service';

@Component({
  selector: 'app-absorb-essence-modal',
  standalone: true,
  imports: [NgIf, NgFor, CommonModule],
  templateUrl: './absorb-essence-modal.component.html',
  styleUrl: './absorb-essence-modal.component.css',
})
export class AbsorbEssenceModalComponent {
  @Input() essences!: Essence[];
  @Output() close = new EventEmitter<void>();

  selectedEssence: any = null;

  constructor(private essencesService: EssencesService) {}

  onSelectEssence(essence: any): void {
    this.selectedEssence = essence;
  }

  onAbsorb(): void {
    if (!this.selectedEssence) {
      return;
    }

    this.essencesService.equipEssence(this.selectedEssence.id);
    this.onClose();
  }

  onClose() {
    this.close.emit();
  }
}
