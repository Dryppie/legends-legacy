import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule, NgFor, NgIf } from '@angular/common';
import { Essence } from '../../../../models/essence';
import { EssencesService } from '../../../../../core/services/api/essences/essences.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-absorb-essence-modal',
  standalone: true,
  imports: [NgIf, NgFor, FormsModule, CommonModule],
  templateUrl: './absorb-essence-modal.component.html',
  styleUrl: './absorb-essence-modal.component.css',
})
export class AbsorbEssenceModalComponent implements OnInit {
  @Input() essences!: Essence[];
  @Output() close = new EventEmitter<void>();

  selectedEssence: any = null;

  filteredEssences: Essence[] = [];
  searchTerm: string = '';

  constructor(private essencesService: EssencesService) {}
  ngOnInit(): void {
    this.filteredEssences = this.essences;
  }

  onSearchChange(term: string) {
    this.searchTerm = term;
    this.filteredEssences = this.essences.filter((essence) =>
      essence.name.toLowerCase().includes(term.toLowerCase()),
    );
  }

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
