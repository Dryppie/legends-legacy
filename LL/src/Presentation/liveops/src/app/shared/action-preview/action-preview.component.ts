import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActionPreview, OperatorSession } from '../../liveops.models';

@Component({
  selector: 'app-action-preview',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './action-preview.component.html',
})
export class ActionPreviewComponent {
  @Input({ required: true }) preview!: ActionPreview;
  @Input({ required: true }) session!: OperatorSession;
  @Input() confirmation = '';
  @Input() submitting = false;
  @Output() confirmationChange = new EventEmitter<string>();
  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  get canSubmit(): boolean {
    return !this.submitting &&
      (!this.preview.confirmationText || this.confirmation === this.preview.confirmationText);
  }
}
