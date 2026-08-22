import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
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
export class AccountRiskDetailComponent implements OnInit {
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
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(() => void this.load());
  }

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
    this.saving = true;
    try {
      const response = await this.api.updateAccountRiskStatus(this.details.account.accountId, { operationId: crypto.randomUUID(), status: this.selectedStatus, reason: this.statusReason.trim() });
      if (!response.isSuccess) { this.showError(response.errorMessage); return; }
      this.details.account.investigationStatus = this.selectedStatus;
      this.listState.updateInvestigationStatus(this.details.account.accountId, this.selectedStatus);
      this.statusReason = '';
      this.showSuccess('Investigation status updated and recorded in the global audit.');
    } catch (error) { this.showError(this.errorMessage(error)); }
    finally { this.saving = false; }
  }

  async addNote(): Promise<void> {
    if (!this.details || !this.note.trim()) { this.showError('Enter an investigation note.'); return; }
    this.saving = true;
    try {
      const response = await this.api.addAccountRiskNote(this.details.account.accountId, { operationId: crypto.randomUUID(), note: this.note.trim() });
      if (!response.isSuccess || !response.data) { this.showError(response.errorMessage); return; }
      if (response.data.note) this.details.notes.unshift(response.data.note);
      this.note = '';
      this.showSuccess('Investigation note added to the append-only audit trail.');
    } catch (error) { this.showError(this.errorMessage(error)); }
    finally { this.saving = false; }
  }

  private async load(): Promise<void> {
    const accountId = this.route.snapshot.paramMap.get('accountId');
    if (!accountId) { this.showError('The account ID is missing.'); this.loading = false; return; }
    this.loading = true;
    this.temporalReport = null;
    this.temporalError = '';
    this.transferConversationReport = null;
    this.transferConversationError = '';
    try {
      const response = await this.api.accountRiskDetails(accountId);
      if (!response.isSuccess || !response.data) { this.showError(response.errorMessage || 'The investigation could not be loaded.'); return; }
      this.details = response.data;
      this.selectedStatus = response.data.account.investigationStatus;
      void this.loadTemporalCorrelations(accountId);
      void this.loadTransferConversationCorrelations(accountId);
    } catch (error) { this.showError(this.errorMessage(error)); }
    finally { this.loading = false; }
  }

  private async loadTemporalCorrelations(accountId: string): Promise<void> {
    this.temporalLoading = true;
    this.temporalError = '';
    try {
      const response = await this.api.accountTemporalCorrelations(accountId);
      if (!response.isSuccess || !response.data) {
        this.temporalError = response.errorMessage || 'Temporal account correlation could not be loaded.';
        return;
      }
      this.temporalReport = response.data;
    } catch (error) {
      this.temporalError = this.errorMessage(error);
    } finally {
      this.temporalLoading = false;
    }
  }

  private async loadTransferConversationCorrelations(accountId: string): Promise<void> {
    this.transferConversationLoading = true;
    this.transferConversationError = '';
    try {
      const response = await this.api.accountTransferConversationCorrelations(accountId);
      if (!response.isSuccess || !response.data) {
        this.transferConversationError = response.errorMessage || 'Transfer conversation evidence could not be loaded.';
        return;
      }
      this.transferConversationReport = response.data;
    } catch (error) {
      this.transferConversationError = this.errorMessage(error);
    } finally {
      this.transferConversationLoading = false;
    }
  }

  private showError(message: string): void { this.message = message; this.messageTone = 'error'; }
  private showSuccess(message: string): void { this.message = message; this.messageTone = 'success'; }
  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) return error.error?.errorMessage ?? error.message;
    return error instanceof Error ? error.message : 'An unexpected error occurred.';
  }
}
