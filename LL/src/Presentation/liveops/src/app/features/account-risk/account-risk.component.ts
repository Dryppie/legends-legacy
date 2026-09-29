import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LiveOpsApiService } from '../../liveops-api.service';
import { AccountRiskFilters, AccountRiskPage, AccountRiskSeverity } from '../../liveops.models';
import { AccountRiskListStateService } from './account-risk-list-state.service';

@Component({
  selector: 'app-account-risk',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './account-risk.component.html',
})
export class AccountRiskComponent implements OnInit, OnDestroy {
  private generation = 0;
  data: AccountRiskPage | null = null;
  search = '';
  minimumSeverity = 'Low';
  signalType = '';
  status = 'Unreviewed';
  minimumScore = '';
  maximumAccountAgeDays = '';
  sort = 'risk';
  page = 1;
  loading = false;
  message = '';

  constructor(
    private readonly api: LiveOpsApiService,
    private readonly router: Router,
    private readonly listState: AccountRiskListStateService,
    private readonly route?: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    const restored = this.listState.restore();
    const requested = this.route?.snapshot.queryParamMap.get('status');
    if (requested && ['Unreviewed', 'Investigating', 'Watchlisted', 'Cleared', 'ConfirmedAbuse', 'Actioned'].includes(requested)) {
      this.status = requested; void this.load(); return;
    }
    if (!restored) {
      void this.load();
      return;
    }

    this.data = restored.data;
    this.search = restored.search;
    this.minimumSeverity = restored.minimumSeverity;
    this.signalType = restored.signalType;
    this.status = restored.status;
    this.minimumScore = restored.minimumScore;
    this.maximumAccountAgeDays = restored.maximumAccountAgeDays;
    this.sort = restored.sort;
    this.page = restored.page;
  }

  async applyFilters(): Promise<void> {
    this.page = 1;
    await this.load();
  }

  async quickFilter(filter: 'critical' | 'high' | 'moderate' | 'feeders' | 'item-sources' | 'item-coordination' | 'new' | 'all'): Promise<void> {
    this.signalType = '';
    this.maximumAccountAgeDays = '';
    this.minimumSeverity = filter === 'critical' ? 'Critical' : filter === 'high' ? 'High' : filter === 'all' ? 'Low' : 'Moderate';
    if (filter === 'feeders') this.signalType = 'FeederNetwork';
    if (filter === 'item-sources') this.signalType = 'YoungItemSourceNetwork';
    if (filter === 'item-coordination') this.signalType = 'YoungItemCoordinationNetwork';
    if (filter === 'new') this.maximumAccountAgeDays = '14';
    await this.applyFilters();
  }

  async changePage(delta: number): Promise<void> {
    const next = this.page + delta;
    if (next < 1 || (delta > 0 && this.data && next > Math.ceil(this.data.total / this.data.pageSize))) return;
    this.page = next;
    await this.load();
  }

  ngOnDestroy(): void { this.generation++; }

  rememberQueue(): void {
    if (this.data) {
      this.listState.save({
        data: this.data,
        search: this.search,
        minimumSeverity: this.minimumSeverity,
        signalType: this.signalType,
        status: this.status,
        minimumScore: this.minimumScore,
        maximumAccountAgeDays: this.maximumAccountAgeDays,
        sort: this.sort,
        page: this.page,
      });
    }
  }

  open(accountId: string): void { this.rememberQueue(); void this.router.navigate(['/account-risk', accountId]); }

  count(severity: AccountRiskSeverity): number | string { return this.data ? this.data.counts[severity] ?? 0 : '—'; }

  get emptyMessage(): string {
    if (this.loading) return 'Loading risk summaries…';
    if (!this.data) return 'Investigation data is unavailable. Apply the filters to retry.';
    if (!this.data.directTransferCount && !this.data.directItemTransferCount) {
      return 'No retained direct cinder or item transfers are available to evaluate.';
    }
    if (this.minimumSeverity !== 'Low' && this.data.evaluatedAccountCount > 0) {
      return `${this.data.evaluatedAccountCount} accounts were evaluated, but none reached the selected severity. Use “Include low” to inspect the observed relationships.`;
    }
    return 'No accounts matched these investigation filters.';
  }

  ageDays(createdUtc: string): number {
    return Math.max(0, Math.floor((Date.now() - new Date(createdUtc).getTime()) / 86_400_000));
  }

  private async load(): Promise<void> {
    const generation = ++this.generation;
    this.loading = true;
    this.data = null;
    this.message = '';
    try {
      const response = await this.api.accountRisks(this.filters(), this.page);
      if (generation !== this.generation) return;
      if (!response.isSuccess || !response.data) {
        this.message = response.errorMessage || 'Account-risk summaries could not be loaded.';
        return;
      }
      this.data = response.data;
    } catch (error) {
      if (generation === this.generation) this.message = this.errorMessage(error);
    } finally {
      if (generation === this.generation) this.loading = false;
    }
  }

  private filters(): AccountRiskFilters {
    return {
      search: this.search,
      minimumSeverity: this.minimumSeverity,
      signalType: this.signalType,
      status: this.status,
      minimumScore: this.minimumScore,
      maximumAccountAgeDays: this.maximumAccountAgeDays,
      sort: this.sort,
    };
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) return error.error?.errorMessage ?? error.message;
    return error instanceof Error ? error.message : 'An unexpected error occurred.';
  }
}
