import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { PlayerSupportSnapshot } from '../../liveops.models';

@Component({
  selector: 'app-support-snapshot',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './support-snapshot.component.html',
})
export class SupportSnapshotComponent {
  @Input() snapshot: PlayerSupportSnapshot | null = null;
  @Input() loading = false;
  @Input() error = '';
  @Input() transferLoading = false;
  @Input() transferError = '';
  @Output() refresh = new EventEmitter<void>();
  @Output() loadMoreTransfers = new EventEmitter<void>();
  @Output() copyIdentifier = new EventEmitter<{ value: string; label: string }>();

  copy(value: string, label: string): void {
    this.copyIdentifier.emit({ value, label });
  }
}
