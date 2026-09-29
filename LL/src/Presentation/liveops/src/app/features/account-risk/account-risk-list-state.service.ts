import { Injectable } from '@angular/core';
import { AccountInvestigationStatus, AccountRiskPage } from '../../liveops.models';
import { LiveOpsApiService } from '../../liveops-api.service';

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
  private order: string[] = [];
  private visited = new Set<string>();
  exhausted = false;
  get hasContext(): boolean { return this.state !== null; }

  save(state: AccountRiskListState): void {
    this.state = state;
    this.order = state.data.entries.map(x => x.accountId);
    this.visited.clear(); this.exhausted = false;
  }

  nextAccount(currentId: string): string | null {
    const index = this.order.indexOf(currentId);
    if (index < 0) return null;
    return this.order.slice(index + 1).find(id => this.state?.data.entries.some(x => x.accountId === id && x.investigationStatus === 'Unreviewed')) ?? null;
  }
  async advance(api: LiveOpsApiService, currentId: string): Promise<string | null> {
    const state = this.state;
    if (!state) return null;
    for (const id of this.order.slice(0, this.order.indexOf(currentId) + 1)) this.visited.add(id);
    const next = this.nextAccount(currentId);
    if (next) return next;
    // Re-read the current page first: dispositions can remove rows and move the
    // next page's first records into it. Never revisit an already traversed row.
    let page = state.page;
    while (true) {
      const { data, page: currentPage, ...filters } = state;
      const response = await api.accountRisks(filters, page, data.pageSize);
      if (this.state !== state) return null;
      if (!response.isSuccess || !response.data) throw new Error(response.errorMessage || 'The next queue page could not be loaded.');
      state.data = response.data; state.page = page;
      const unseen = response.data.entries.filter(x => !this.visited.has(x.accountId));
      for (const row of unseen) if (!this.order.includes(row.accountId)) this.order.push(row.accountId);
      const candidate = unseen.find(x => x.investigationStatus === 'Unreviewed');
      if (candidate) return candidate.accountId;
      if (page * response.data.pageSize >= response.data.total) { this.exhausted = true; return null; }
      page++;
    }
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
