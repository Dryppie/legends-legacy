import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LiveOpsApiService } from '../../liveops-api.service';
import { AdministrationAuditEntry, AdministrationAuditFilters } from '../../liveops.models';
import { OperatorContextService } from '../../operator-context.service';

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './audit.component.html',
})
export class AuditComponent implements OnInit {
  get permissions() { return this.operator.permissions; }
  entries: AdministrationAuditEntry[] = [];
  source = 'All';
  actionType = '';
  actor = '';
  permission = '';
  reference = '';
  riskLevel = '';
  target = '';
  operationId = '';
  from = '';
  to = '';
  nextCursor: string | null = null;
  previousCursors: Array<string | null> = [];
  currentCursor: string | null = null;
  unavailableSources: string[] = [];
  loading = false;
  exporting = false;
  loaded = false;
  message = '';
  messageTone: 'success' | 'error' | 'info' = 'info';
  expandedOperations = new Set<string>();

  constructor(
    private readonly api: LiveOpsApiService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    readonly operator: OperatorContextService,
  ) {}

  ngOnInit(): void {
    const risk = this.route.snapshot.queryParamMap.get('risk');
    if (risk === 'Permanent' || risk === 'HighValue') this.applyRiskFilter(risk);
    void this.loadAudit();
  }

  hasPermission(permission: string): boolean {
    return this.operator.hasPermission(permission);
  }

  async searchAudit(): Promise<void> {
    this.currentCursor = null;
    this.previousCursors = [];
    await this.loadAudit();
  }

  async applyQuickFilter(filter: 'permanent' | 'large-grants' | 'exports'): Promise<void> {
    this.source = 'Game';
    this.permission = '';
    if (filter === 'permanent') {
      this.actionType = 'AccountBanned';
      this.riskLevel = 'Permanent';
    } else if (filter === 'large-grants') {
      this.actionType = 'CompensationItemsGranted';
      this.riskLevel = 'HighValue';
    } else {
      this.actionType = 'AuditExported';
      this.riskLevel = '';
      this.permission = this.permissions.superadmin;
    }
    await this.searchAudit();
  }

  async exportAudit(): Promise<void> {
    const from = this.localDateToIso(this.from);
    const to = this.localDateToIso(this.to);
    if (!from || !to) {
      this.showError('Choose both From and To before exporting. Exports are limited to 31 days.');
      return;
    }
    this.exporting = true;
    this.message = '';
    try {
      const download = await this.api.exportAudit(this.filters(), from, to, crypto.randomUUID());
      const url = URL.createObjectURL(download.blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = download.fileName;
      anchor.click();
      URL.revokeObjectURL(url);
      this.message = 'Audit export created and recorded in the audit trail.';
      this.messageTone = 'success';
    } catch (error) {
      this.showError(this.errorMessage(error));
    } finally {
      this.exporting = false;
    }
  }

  async nextPage(): Promise<void> {
    if (!this.nextCursor) return;
    this.previousCursors.push(this.currentCursor);
    this.currentCursor = this.nextCursor;
    await this.loadAudit();
  }

  async previousPage(): Promise<void> {
    if (!this.previousCursors.length) return;
    this.currentCursor = this.previousCursors.pop() ?? null;
    await this.loadAudit();
  }

  toggle(operationId: string): void {
    if (this.expandedOperations.has(operationId)) this.expandedOperations.delete(operationId);
    else this.expandedOperations.add(operationId);
  }

  openPlayer(characterId: string): void {
    void this.router.navigate(['/players', characterId]);
  }

  details(entry: AdministrationAuditEntry): string {
    try { return JSON.stringify(JSON.parse(entry.detailsJson || '{}'), null, 2); }
    catch { return entry.detailsJson; }
  }

  private applyRiskFilter(risk: 'Permanent' | 'HighValue'): void {
    const now = new Date();
    this.source = 'Game';
    this.actionType = risk === 'Permanent' ? 'AccountBanned' : 'CompensationItemsGranted';
    this.riskLevel = risk;
    this.from = this.dateToLocalInput(new Date(now.getTime() - 24 * 60 * 60 * 1000));
    this.to = this.dateToLocalInput(now);
  }

  private async loadAudit(): Promise<void> {
    this.loading = true;
    this.message = '';
    try {
      const response = await this.api.audit(this.filters(), this.currentCursor);
      if (!response.isSuccess || !response.data) {
        this.showError(response.errorMessage || 'The audit history could not be loaded.');
        return;
      }
      this.entries = response.data.entries;
      this.nextCursor = response.data.nextCursor;
      this.unavailableSources = response.data.unavailableSources;
      this.loaded = true;
    } catch (error) {
      this.showError(this.errorMessage(error));
    } finally {
      this.loading = false;
    }
  }

  private filters(): AdministrationAuditFilters {
    return {
      source: this.source, actionType: this.actionType, actor: this.actor,
      permission: this.permission, reference: this.reference, riskLevel: this.riskLevel,
      target: this.target, operationId: this.operationId,
      from: this.localDateToIso(this.from), to: this.localDateToIso(this.to),
    };
  }

  private localDateToIso(value: string): string | undefined {
    if (!value) return undefined;
    const parsed = new Date(value);
    return Number.isNaN(parsed.valueOf()) ? undefined : parsed.toISOString();
  }

  private dateToLocalInput(value: Date): string {
    const local = new Date(value.getTime() - value.getTimezoneOffset() * 60_000);
    return local.toISOString().slice(0, 16);
  }

  private showError(message: string): void {
    this.message = message;
    this.messageTone = 'error';
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 401) return 'Your operator session has expired. Sign in again.';
      if (error.status === 403) return 'Your staff role does not permit this action.';
      return error.error?.errorMessage ?? error.error?.message ?? error.message;
    }
    return error instanceof Error ? error.message : 'An unexpected error occurred.';
  }
}
