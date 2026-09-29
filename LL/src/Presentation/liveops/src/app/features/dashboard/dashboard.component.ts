import { FormsModule } from '@angular/forms';
import { actionLabel } from '../../shared/admin-presentation';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { LiveOpsApiService } from '../../liveops-api.service';
import { OperationalStatus, OperationalDetailPage } from '../../liveops.models';
import { WorkQueueComponent } from './work-queue.component';
import { JobSchedulesComponent } from './job-schedules.component';
import { OperatorContextService } from '../../operator-context.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, WorkQueueComponent, JobSchedulesComponent, FormsModule],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit, OnDestroy {
  readonly actionLabel = actionLabel;
  readonly timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  detailPage = 1; detailStatus = ''; detailView = ''; diagnosticMessage = '';
  detail: OperationalDetailPage | null = null; detailError = ''; detailLoading = false;
  private detailGeneration = 0; private statusGeneration = 0; private statusPending = false;
  operationalStatus: OperationalStatus | null = null;
  statusLoading = false;
  statusError = '';
  statusClockSkewSeconds = 0;
  private refreshTimer: ReturnType<typeof setInterval> | null = null;

  constructor(
    private readonly api: LiveOpsApiService,
    private readonly router: Router,
    private readonly ngZone: NgZone,
    private readonly route: ActivatedRoute,
    readonly operator: OperatorContextService = new OperatorContextService(),
  ) {}
  get canRepair(): boolean { return this.operator.hasPermission(this.operator.permissions.superadmin); }

  ngOnInit(): void {
    void this.loadOperationalStatus();
    const view = this.route.snapshot.queryParamMap.get("view");
    if (view) void this.openDetails(view);
    this.ngZone.runOutsideAngular(() => {
      this.refreshTimer = setInterval(
        () => this.ngZone.run(() => void this.loadOperationalStatus(true)),
        30_000,
      );
    });
  }

  ngOnDestroy(): void {
    this.detailGeneration++; this.statusGeneration++;
    if (this.refreshTimer) clearInterval(this.refreshTimer);
  }

  async loadOperationalStatus(silent = false): Promise<void> {
    if (this.statusPending) return;
    this.statusPending = true; const generation = ++this.statusGeneration;
    if (!silent) this.statusLoading = true;
    this.statusError = '';
    try {
      const response = await this.api.operationalStatus();
      if (generation !== this.statusGeneration) return;
      if (!response.isSuccess || !response.data) {
        this.statusError = response.errorMessage || 'Operational status could not be loaded.';
        return;
      }
      this.operationalStatus = response.data;
      this.statusClockSkewSeconds = Math.round(
        (Date.now() - Date.parse(response.data.serverTimeUtc)) / 1000,
      );
    } catch (error) {
      if (generation === this.statusGeneration) this.statusError = this.errorMessage(error);
    } finally {
      this.statusPending = false;
      if (generation === this.statusGeneration) this.statusLoading = false;
    }
  }

  openRiskAudit(riskLevel: 'Permanent' | 'HighValue'): void {
    void this.router.navigate(['/audit'], { queryParams: { source: 'Game', riskLevel, from: this.localDate(new Date(Date.parse(this.operationalStatus!.serverTimeUtc) - 86400000)), to: this.localDate(new Date(this.operationalStatus!.serverTimeUtc)) } });
  }

  private localDate(value: Date): string { return new Date(value.getTime() - value.getTimezoneOffset() * 60000).toISOString().slice(0, -1); }
  async openDetails(view: string, page = 1): Promise<void> {
    if (view !== this.detailView) this.detailStatus = '';
    this.detailView = view; this.detailPage = page;
    const generation = ++this.detailGeneration;
    this.detail = null; this.detailError = ''; this.detailLoading = true;
    void this.router.navigate([], { relativeTo: this.route, queryParams: { view }, replaceUrl: true });
    try {
      const result = await this.api.operationalDetails(view, page, this.detailStatus);
      if (generation !== this.detailGeneration) return;
      if (!result.isSuccess || !result.data) throw new Error(result.errorMessage);
      this.detail = result.data;
    } catch (error) { if (generation === this.detailGeneration) this.detailError = this.errorMessage(error); }
    finally { if (generation === this.detailGeneration) this.detailLoading = false; }
  }

  async copyDiagnostics(): Promise<void> {
    if (!this.detail) return;
    const bundle = { audience: 'Internal support', environment: this.operationalStatus?.environment,
      build: this.operationalStatus?.build, coverage: 'Only the displayed page; recorded states are not proof of reward loss or delivery.', filter: this.detailStatus, ...this.detail };
    try { await navigator.clipboard.writeText(JSON.stringify(bundle, null, 2)); this.diagnosticMessage = 'Internal diagnostic page copied with its time, filter and coverage.'; }
    catch { this.diagnosticMessage = 'Clipboard unavailable. Copy the displayed diagnostic references.'; }
  }
  openAudit(): void {
    void this.router.navigate(['/audit']);
  }

  openPlayer(characterId: string): void {
    void this.router.navigate(['/players', characterId]);
  }

  statusClass(value: string): string {
    return value.toLowerCase();
  }

  absoluteSkewSeconds(): number {
    return Math.abs(this.statusClockSkewSeconds);
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      return error.error?.errorMessage ?? error.error?.message ?? error.message;
    }
    return error instanceof Error ? error.message : 'An unexpected error occurred.';
  }
}
