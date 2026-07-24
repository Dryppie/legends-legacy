import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Essence } from '../../../../models/essence';
import { EssenceDetailsComponent } from '../../../essences/essence-details/essence-details.component';

@Component({
    selector: 'app-essence-modal',
    imports: [EssenceDetailsComponent],
    templateUrl: './essence-modal.component.html'
})
export class EssenceModalComponent {
  @Input() essence!: Essence;
  @Output() close = new EventEmitter<void>();

  onClose() {
    this.close.emit();
  }
}
