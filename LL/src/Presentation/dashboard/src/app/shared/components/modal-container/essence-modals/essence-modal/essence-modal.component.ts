import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Essence } from '../../../../models/essence';
import { TicksToSecondsPipe } from '../../../../pipes/ticks-to-seconds/ticks-to-seconds.pipe';

@Component({
  selector: 'app-essence-modal',
  standalone: true,
  imports: [TicksToSecondsPipe],
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
