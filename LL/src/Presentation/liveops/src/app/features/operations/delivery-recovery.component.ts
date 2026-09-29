import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { LiveOpsApiService } from '../../liveops-api.service';
import { ActionPreview, StateRefreshRecoveryResult } from '../../liveops.models';
import { OperationJournalService } from '../../operation-journal.service';
import { OperatorContextService } from '../../operator-context.service';
import { OperatorDraftService } from '../../operator-draft.service';
import { PreparationDraft } from '../../shared/preparation-draft';
import { DraftStatusComponent } from '../../shared/draft-status.component';
import { ActionPreviewComponent } from '../../shared/action-preview/action-preview.component';

@Component({ selector: 'app-delivery-recovery', standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, DraftStatusComponent, ActionPreviewComponent], template: `
  <main class="case-workspace"><p class="eyebrow">Bounded Game recovery</p><h1>Retry player state refresh</h1>
    <p>Review a failed notification that asks one player's game to fetch its current state. The server checks its type and current status before preparing a retry.</p>
    <p>No inventory, currency, rewards or restrictions are changed. The original failure stays available for diagnosis.</p>
    <div class="workspace-tools"><a routerLink="/operations">Operations &amp; recovery</a><a routerLink="/dashboard" [queryParams]="{view:'deliveries'}">Delivery diagnostics</a></div>
    <p>Original delivery: <code class="full-code">{{ deliveryId }}</code></p>
    @if (!canRepair) { <p class="message">A super administrator is required to retry a state-refresh notification.</p> }
    @if (message) { <p class="message" role="status">{{ message }}</p> }
    @if (result; as receipt) { <section class="operation-receipt" role="status"><h2>Replacement notification queued</h2><p>The Game worker will use its normal retry policy. This does not confirm that the player received it.</p><code class="full-code">{{ receipt.operationId }}</code><div class="workspace-tools"><a routerLink="/operations" [queryParams]="{operation:receipt.operationId}">Inspect replacement delivery</a><a routerLink="/audit" [queryParams]="{operationId:receipt.operationId,source:'Game'}">Open audit receipt</a><a routerLink="/cases" [queryParams]="{characterId:receipt.characterId,operationId:receipt.operationId,source:'Game'}">Player cases</a></div></section> }
    @if (canRepair && !result) {
      <app-draft-status [draft]="preparation.handle" (restored)="preparation.restore()" />
      <fieldset class="preparation-fields" [disabled]="preparation.blocked || loading || previewSubmitting"><label>Reason or case reference<textarea maxlength="1000" [(ngModel)]="reason" (ngModelChange)="preparation.save()"></textarea></label>
        <button (click)="review()" [disabled]="reason.trim().length < 3">{{ loading ? 'Checking eligibility' : 'Review state-refresh retry' }}</button></fieldset>
      @if (operationId) { <p>Recovery reference: <code class="full-code">{{ operationId }}</code>. Retain this reference if the response is interrupted.</p> }
    }
    @if (preview && operator.session) { <app-action-preview [preview]="preview" [session]="operator.session" [submitting]="previewSubmitting" [message]="message" (confirm)="submit()" (cancel)="closePreview()" (refreshPreview)="review()" /> }
  </main>` })
export class DeliveryRecoveryComponent implements OnInit, OnDestroy {
  deliveryId = ''; reason = ''; operationId = ''; message = ''; loading = false; previewSubmitting = false;
  preview: ActionPreview | null = null; result: StateRefreshRecoveryResult | null = null;
  readonly preparation: PreparationDraft;
  private generation = 0; private subscription?: Subscription; private reviewedReason = '';
  constructor(private api: LiveOpsApiService, private route: ActivatedRoute, readonly operator: OperatorContextService,
    private journal: OperationJournalService, drafts?: OperatorDraftService) {
    this.preparation = new PreparationDraft(drafts, this, ['reason']);
  }
  get canRepair(): boolean { return this.operator.hasPermission(this.operator.permissions.superadmin); }
  ngOnInit(): void { this.subscription = this.route.paramMap.subscribe(params => {
    this.preparation.detach(); this.generation++; this.deliveryId = params.get('deliveryId') ?? '';
    this.reason = ''; this.operationId = ''; this.message = ''; this.preview = null; this.result = null; this.loading = false;
    if (this.canRepair && this.deliveryId) void this.preparation.open('delivery:' + this.deliveryId);
  }); }
  ngOnDestroy(): void { this.generation++; this.subscription?.unsubscribe(); this.preparation.detach(); }
  closePreview(): void { if (!this.previewSubmitting) this.preview = null; }
  async review(): Promise<void> {
    if (!this.canRepair || this.loading || this.previewSubmitting || this.preparation.blocked || this.reason.trim().length < 3) return;
    if (this.journal.recoveryIncomplete) { this.message = this.journal.recoveryMessage; return; }
    const generation = this.generation; this.loading = true; this.preview = null; this.message = '';
    this.operationId ||= this.journal.unresolved.find(x => x.kind === 'delivery-retry' && x.targetId === this.deliveryId)?.operationId ?? crypto.randomUUID();
    const reason = this.reason.trim();
    try {
      const response = await this.api.previewStateRefresh(this.deliveryId, { operationId: this.operationId, reason });
      if (generation !== this.generation) return;
      if (!response.isSuccess || !response.data) throw new Error(response.errorMessage);
      this.preview = response.data; this.reviewedReason = reason;
    } catch (error) { if (generation === this.generation) this.message = this.error(error); }
    finally { if (generation === this.generation) this.loading = false; }
  }
  async submit(): Promise<void> {
    if (!this.preview || this.previewSubmitting || !this.canRepair) return;
    const generation = this.generation, id = this.operationId, delivery = this.deliveryId;
    this.previewSubmitting = true; this.message = '';
    this.journal.record(id, delivery, 'Retry player state refresh', 'Submitting', 'delivery-retry', 'Game');
    try {
      const response = await this.api.retryStateRefresh(delivery, { operationId: id, reason: this.reviewedReason, previewToken: this.preview.previewToken });
      this.journal.record(id, delivery, 'Retry player state refresh', response.isSuccess ? 'Completed' : 'Rejected');
      if (generation !== this.generation) return;
      if (!response.isSuccess || !response.data) throw new Error(response.errorMessage);
      this.result = response.data; this.preview = null; this.reason = ''; this.preparation.save();
    } catch (error) {
      // Only a received 4xx establishes rejection; a lost response stays uncertain.
      if (error instanceof HttpErrorResponse) this.journal.record(id, delivery, 'Retry player state refresh', error.status >= 400 && error.status < 500 ? 'Rejected' : 'Unknown');
      else if (this.journal.receipts.find(x => x.operationId === id)?.outcome === 'Submitting') this.journal.record(id, delivery, 'Retry player state refresh', 'Unknown');
      if (generation === this.generation) this.message = this.error(error) + ' Keep the original recovery reference; check Operations & recovery before retrying.';
    } finally { if (generation === this.generation) this.previewSubmitting = false; }
  }
  private error(error: unknown): string { return error instanceof HttpErrorResponse ? error.error?.errorMessage || 'The recovery request could not be confirmed.' : error instanceof Error ? error.message : 'The recovery request could not be confirmed.'; }
}
