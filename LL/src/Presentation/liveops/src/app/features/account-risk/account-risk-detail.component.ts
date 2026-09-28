import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { OperationJournalService } from '../../operation-journal.service';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LiveOpsApiService } from '../../liveops-api.service';
import { AccountInvestigationStatus, AccountRiskDetails, AccountRiskTransfer, AccountTemporalCorrelationReport, TransferConversationCorrelationReport } from '../../liveops.models';
import { OperatorContextService } from '../../operator-context.service';
import { AccountRiskListStateService } from './account-risk-list-state.service';

@Component({
  selector: 'app-account-risk-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './account-risk-detail.component.html',
})
export class AccountRiskDetailComponent implements OnInit, OnDestroy {
  private generation = 0;
  private subscription?: Subscription;
  private pending = new Map<string, string>();
  section = 'summary';
  readonly timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  details: AccountRiskDetails | null = null;
  temporalReport: AccountTemporalCorrelationReport | null = null;
  transferConversationReport: TransferConversationCorrelationReport | null = null;
  loading = true;
  temporalLoading = false;
  temporalError = '';
  transferConversationLoading = false;
  transferConversationError = '';
  saving = false;
  message = '';
  messageTone: 'error' | 'success' = 'success';
  selectedStatus: AccountInvestigationStatus = 'Unreviewed';
  statusReason = '';
  note = '';
  direction = '';
  kind = '';
  counterparty = '';

  constructor(
    private readonly api: LiveOpsApiService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly listState: AccountRiskListStateService,
    readonly operator: OperatorContextService,
    private readonly journal: OperationJournalService = new OperationJournalService(),
  ) {}

  ngOnInit(): void {
    this.subscription = this.route.paramMap.subscribe(() => void this.load());
  }

  ngOnDestroy(): void { this.generation++; this.subscription?.unsubscribe(); }
  setSection(section: string): void { this.section = section; }
  get nextAccountId(): string | null { return this.listState.nextAccount(this.details?.account.accountId ?? ''); }
  nextAccount(): void { const id = this.nextAccountId; if (id) this.openAccount(id); }
  inspectTransfer(id: string): void { this.section = 'transfers'; this.counterparty = ''; this.direction = ''; this.kind = ''; requestAnimationFrame(() => document.getElementById('transfer-' + id)?.scrollIntoView({ block: 'center' })); }
  private operationId(kind: string, payload: object): string { const key = JSON.stringify([this.details?.account.accountId, kind, payload]); const id = this.pending.get(key) ?? this.journal.unresolved.find(x => x.targetId === this.details?.account.accountId && x.kind === 'risk-' + kind)?.operationId ?? crypto.randomUUID(); this.pending.set(key, id); return id; }

  get canModerate(): boolean { return this.operator.hasPermission(this.operator.permissions.account); }

  get filteredTransfers(): AccountRiskTransfer[] {
    const term = this.counterparty.trim().toLowerCase();
    return (this.details?.transfers ?? []).filter((entry) =>
      (!this.direction || entry.direction === this.direction) &&
      (!this.kind || entry.kind === this.kind) &&
      (!term || entry.counterpartyCharacterName.toLowerCase().includes(term) || entry.counterpartyAccountId.toLowerCase().includes(term)));
  }

  back(): void { void this.router.navigate(['/account-risk']); }
  openPlayer(characterId: string): void { if (characterId && !/^0+$/.test(characterId.replaceAll('-', ''))) void this.router.navigate(['/players', characterId]); }
  openAccount(accountId: string): void { void this.router.navigate(['/account-risk', accountId]); }

  transferConversationAssessment(assessment: string): string {
    switch (assessment) {
      case 'UncommunicativeValueTransferPattern': return 'Uncommunicative value-transfer pattern';
      case 'RecordedBidirectionalConversation': return 'Bidirectional conversation recorded';
      case 'BelowPatternThreshold': return 'Below pattern threshold';
      default: return 'Chat evidence unavailable';
    }
  }

  async updateStatus(): Promise<void> {
    if (!this.details || !this.statusReason.trim()) { this.showError('Add a reason for the review-state change.'); return; }
    if (this.saving) return;
    const generation = this.generation;
    const accountId = this.details.account.accountId;
    const payload = { status: this.selectedStatus, reason: this.statusReason.trim() };
    const operationId = this.operationId('status', payload);
    this.saving = true;
    this.journal.record(operationId, accountId, 'Investigation status', 'Submitting', 'risk-status', 'Game');
    try {
      const response = await this.api.updateAccountRiskStatus(accountId, { operationId, ...payload });
      this.journal.record(operationId, accountId, 'Investigation status', response.isSuccess ? 'Completed' : 'Rejected');
      if (generation !== this.generation) return;
      if (!response.isSuccess) { this.showError(response.errorMessage); return; }
      this.details!.account.investigationStatus = payload.status;
      this.listState.updateInvestigationStatus(accountId, payload.status);
      this.pending.delete(JSON.stringify([accountId, 'status', payload]));
      this.statusReason = '';
      this.showSuccess('Investigation status updated and recorded in the global audit.');
    } catch (error) { this.journal.record(operationId, accountId, 'Investigation update', error instanceof HttpErrorResponse && error.status >= 400 && error.status < 500 ? 'Rejected' : 'Unknown'); if (generation === this.generation) this.showError(`${this.errorMessage(error)} Check operation ${operationId} or retry the unchanged request.`); }
    finally { if (generation === this.generation) this.saving = false; }
  }

  async addNote(): Promise<void> {
    if (!this.details || !this.note.trim()) { this.showError('Enter an investigation note.'); return; }
    if (this.saving) return;
    const generation = this.generation;
    const accountId = this.details.account.accountId;
    const payload = { note: this.note.trim() };
    const operationId = this.operationId('note', payload);
    this.saving = true;
    this.journal.record(operationId, accountId, 'Investigation note', 'Submitting', 'risk-note', 'Game');
    try {
      const response = await this.api.addAccountRiskNote(accountId, { operationId, ...payload });
      this.journal.record(operationId, accountId, 'Investigation note', response.isSuccess ? 'Completed' : 'Rejected');
      if (generation !== this.generation) return;
      if (!response.isSuccess || !response.data) { this.showError(response.errorMessage); return; }
      if (response.data.note && !this.details!.notes.some(note => note.id === response.data!.note!.id)) this.details!.notes.unshift(response.data.note);
      this.pending.delete(JSON.stringify([accountId, 'note', payload]));
      this.note = '';
      this.showSuccess('Investigation note added to the append-only audit trail.');
    } catch (error) { this.journal.record(operationId, accountId, 'Investigation update', error instanceof HttpErrorResponse && error.status >= 400 && error.status < 500 ? 'Rejected' : 'Unknown'); if (generation === this.generation) this.showError(`${this.errorMessage(error)} Check operation ${operationId} or retry the unchanged request.`); }
    finally { if (generation === this.generation) this.saving = false; }
  }

  private async load(): Promise<void> {
    const accountId = this.route.snapshot.paramMap.get('accountId');
    if (!accountId) { this.showError('The account ID is missing.'); this.loading = false; return; }
    const generation = ++this.generation;
    this.section = 'summary'; this.direction = this.kind = this.counterparty = '';
    this.details = null; this.message = ''; this.note = ''; this.statusReason = ''; this.pending.clear(); this.saving = false;
    this.temporalLoading = this.transferConversationLoading = false;
    this.loading = true;
    this.temporalReport = null;
    this.temporalError = '';
    this.transferConversationReport = null;
    this.transferConversationError = '';
    try {
      const response = await this.api.accountRiskDetails(accountId);
      if (generation !== this.generation) return;
      if (!response.isSuccess || !response.data) { this.showError(response.errorMessage || 'The investigation could not be loaded.'); return; }
      this.details = response.data;
      this.selectedStatus = response.data.account.investigationStatus;
      void this.loadTemporalCorrelations(accountId, generation);
      void this.loadTransferConversationCorrelations(accountId, generation);
    } catch (error) { if (generation === this.generation) this.showError(this.errorMessage(error)); }
    finally { if (generation === this.generation) this.loading = false; }
  }

  private async loadTemporalCorrelations(accountId: string, generation: number): Promise<void> {
    this.temporalLoading = true;
    this.temporalError = '';
    try {
      const response = await this.api.accountTemporalCorrelations(accountId);
      if (generation !== this.generation) return;
      if (!response.isSuccess || !response.data) {
        this.temporalError = response.errorMessage || 'Temporal account correlation could not be loaded.';
        return;
      }
      this.temporalReport = response.data;
    } catch (error) {
      if (generation === this.generation) this.temporalError = this.errorMessage(error);
    } finally {
      if (generation === this.generation) this.temporalLoading = false;
    }
  }

  private async loadTransferConversationCorrelations(accountId: string, generation: number): Promise<void> {
    this.transferConversationLoading = true;
    this.transferConversationError = '';
    try {
      const response = await this.api.accountTransferConversationCorrelations(accountId);
      if (generation !== this.generation) return;
      if (!response.isSuccess || !response.data) {
        this.transferConversationError = response.errorMessage || 'Transfer conversation evidence could not be loaded.';
        return;
      }
      this.transferConversationReport = response.data;
    } catch (error) {
      if (generation === this.generation) this.transferConversationError = this.errorMessage(error);
    } finally {
      if (generation === this.generation) this.transferConversationLoading = false;
    }
  }

  private showError(message: string): void { this.message = message; this.messageTone = 'error'; }
  private showSuccess(message: string): void { this.message = message; this.messageTone = 'success'; }
  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) return error.error?.errorMessage ?? error.message;
    return error instanceof Error ? error.message : 'An unexpected error occurred.';
  }
}
