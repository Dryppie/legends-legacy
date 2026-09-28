import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DraftHandle, OperatorDraftService } from '../operator-draft.service';

@Component({ selector: 'app-draft-status', standalone: true, imports: [CommonModule], template: `
  @if (draft; as value) {
    <div class="draft-status" role="status">
      @if (value.error) {
        <p class="error">{{ value.error }}</p>
        @if (value.loaded && !value.conflict) { <button class="secondary" type="button" (click)="drafts.save(value)">Retry saving draft</button> }
        <button class="secondary" type="button" [disabled]="value.saving" (click)="reload()">{{ value.loaded ? 'Replace local text with saved draft' : 'Retry loading draft' }}</button>
        @if (value.dirty) { <button class="secondary" type="button" (click)="copy()">Copy unsaved draft</button> }
      } @else if (value.saving || value.dirty) { <span>Saving private draft… Keep this tab open until saved.</span> }
      @else if (value.loaded) { <span>{{ value.updatedAt && value.version !== emptyVersion ? 'Private draft saved ' + (value.updatedAt | date:'medium') : 'Drafts are saved privately to your operator account.' }}</span> }
      <span>{{ copyMessage }}</span>
    </div>
  }
` })
export class DraftStatusComponent {
  readonly emptyVersion = '00000000-0000-0000-0000-000000000000';
  @Input() draft?: DraftHandle;
  @Output() restored = new EventEmitter<void>();
  copyMessage = '';
  constructor(readonly drafts: OperatorDraftService) {}
  async reload(): Promise<void> { if (this.draft) { await this.drafts.reload(this.draft); if (!this.draft.error) this.restored.emit(); } }
  async copy(): Promise<void> {
    try { await navigator.clipboard.writeText(Object.entries(this.draft?.data ?? {}).map(([key, value]) => `${key}: ${value}`).join('\n')); this.copyMessage = 'Draft copied.'; }
    catch { this.copyMessage = 'Clipboard unavailable; select and copy your text before replacing it.'; }
  }
}
