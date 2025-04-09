import { Component, Input } from '@angular/core';
import { ModalService } from '../../../../core/services/client-side/modal/modal.service';
import { Essence } from '../../../models/essence';

@Component({
  selector: 'app-essence-tooltip',
  standalone: true,
  imports: [],
  templateUrl: './essence-tooltip.component.html',
  styleUrl: './essence-tooltip.component.css',
})
export class EssenceTooltipComponent {
  @Input() essence!: Essence;

  constructor(private modalService: ModalService) {}

  openEssenceModal() {
    this.modalService.toggleEssenceModal(this.essence); // Pass the essence from the Item to display all necessary info
  }
}
