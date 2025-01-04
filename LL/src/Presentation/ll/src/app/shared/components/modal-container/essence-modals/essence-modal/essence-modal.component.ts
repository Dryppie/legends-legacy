import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Essence } from '../../../../models/essence';

@Component({
  selector: 'app-essence-modal',
  standalone: true,
  imports: [],
  templateUrl: './essence-modal.component.html',
  styleUrl: './essence-modal.component.css',
})
export class EssenceModalComponent {
  @Input() essence!: Essence;
  @Output() close = new EventEmitter<void>();

  onClose() {
    this.close.emit();
  }
}
