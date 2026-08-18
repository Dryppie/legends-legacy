import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, NgZone, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { LiveOpsApiService } from '../../liveops-api.service';
import { OperationalStatus } from '../../liveops.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit, OnDestroy {
  operationalStatus: OperationalStatus | null = null;
  statusLoading = false;
  statusError = '';
  statusClockSkewSeconds = 0;
  private refreshTimer: ReturnType<typeof setInterval> | null = null;

  constructor(
    private readonly api: LiveOpsApiService,
    private readonly router: Router,
    private readonly ngZone: NgZone,
  ) {}

  ngOnInit(): void {
    void this.loadOperationalStatus();
    this.ngZone.runOutsideAngular(() => {
      this.refreshTimer = setInterval(
        () => this.ngZone.run(() => void this.loadOperationalStatus(true)),
        30_000,
      );
    });
  }

  ngOnDestroy(): void {
    if (this.refreshTimer) clearInterval(this.refreshTimer);
  }

  async loadOperationalStatus(silent = false): Promise<void> {
    if (!silent) this.statusLoading = true;
    this.statusError = '';
    try {
      const response = await this.api.operationalStatus();
      if (!response.isSuccess || !response.data) {
        this.statusError = response.errorMessage || 'Operational status could not be loaded.';
        return;
      }
      this.operationalStatus = response.data;
      this.statusClockSkewSeconds = Math.round(
        (Date.now() - Date.parse(response.data.serverTimeUtc)) / 1000,
      );
    } catch (error) {
      this.statusError = this.errorMessage(error);
    } finally {
      this.statusLoading = false;
    }
  }

  openRiskAudit(riskLevel: 'Permanent' | 'HighValue'): void {
    void this.router.navigate(['/audit'], { queryParams: { risk: riskLevel } });
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
