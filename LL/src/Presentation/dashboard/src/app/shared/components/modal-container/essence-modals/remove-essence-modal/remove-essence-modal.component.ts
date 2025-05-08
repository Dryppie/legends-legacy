import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Essence } from '../../../../models/essence';
import { CommonModule, NgFor, NgIf } from '@angular/common';
import { EssencesService } from '../../../../../core/services/api/essences/essences.service';

@Component({
  selector: 'app-remove-essence-modal',
  standalone: true,
  imports: [NgIf, NgFor, CommonModule],
  templateUrl: './remove-essence-modal.component.html',
  styleUrl: './remove-essence-modal.component.css',
})
export class RemoveEssenceModalComponent {
  @Input() essences!: Essence[];
  @Output() close = new EventEmitter<void>();

  selectedEssence: any = null;

  removeClickCount = 3;

  constructor(private essencesService: EssencesService) {}

  onSelectEssence(essence: any): void {
    this.selectedEssence = essence;
    this.removeClickCount = 3;
  }

  onRemove(): void {
    if (!this.selectedEssence) {
      return;
    }

    if (this.removeClickCount > 0) {
      // Provide feedback (e.g., console log) on how many more clicks are needed
      this.removeClickCount--;
      return;
    }

    this.essencesService.deleteEquippedEssence(this.selectedEssence.id);
    this.onClose();
  }

  onClose() {
    this.close.emit();
  }
}
