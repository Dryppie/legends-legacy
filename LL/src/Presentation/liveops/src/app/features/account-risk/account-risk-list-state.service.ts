import { Injectable } from '@angular/core';
import { AccountInvestigationStatus, AccountRiskPage } from '../../liveops.models';

export interface AccountRiskListState {
  data: AccountRiskPage;
  search: string;
  minimumSeverity: string;
  signalType: string;
  status: string;
  minimumScore: string;
  maximumAccountAgeDays: string;
  sort: string;
  page: number;
}

@Injectable({ providedIn: 'root' })
export class AccountRiskListStateService {
  private state: AccountRiskListState | null = null;

  save(state: AccountRiskListState): void {
    this.state = state;
  }

  restore(): AccountRiskListState | null {
    const state = this.state;
    this.state = null;
    return state;
  }

  updateInvestigationStatus(accountId: string, status: AccountInvestigationStatus): void {
    if (!this.state) return;

    const entry = this.state.data.entries.find((candidate) => candidate.accountId === accountId);
    if (!entry) return;

    entry.investigationStatus = status;
    if (this.state.status && this.state.status !== status) {
      this.state.data.entries = this.state.data.entries.filter((candidate) => candidate.accountId !== accountId);
      this.state.data.total = Math.max(0, this.state.data.total - 1);
    }
  }
}
