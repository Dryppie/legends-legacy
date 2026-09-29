import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LiveOpsApiService } from '../../liveops-api.service';
import { AdministrationAuditEntry, AdministrationAuditFilters } from '../../liveops.models';
import { administrationActions, actionLabel, actionEffect, operationSummary } from '../../shared/admin-presentation';
import { WorkspaceStateService } from '../../workspace-state.service';
import { OperationJournalService } from '../../operation-journal.service';
import { OperatorContextService } from '../../operator-context.service';

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './audit.component.html',
})
export class AuditComponent implements OnInit, OnDestroy {
  readonly actions = administrationActions;
  readonly actionLabel = actionLabel;
  readonly actionEffect = actionEffect;
  readonly timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  advanced = false;
  private requestGeneration = 0;
  private querySubscription?: Subscription;
  private updatingUrl = false;
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
  targetNames: Record<string, string> = {};

  constructor(
    private readonly api: LiveOpsApiService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    readonly operator: OperatorContextService,
    private readonly workspace: WorkspaceStateService = new WorkspaceStateService(),
    private readonly journal: OperationJournalService = new OperationJournalService(),
  ) {}

  ngOnInit(): void {
    this.querySubscription = this.route.queryParamMap.subscribe(params => {
      if (this.updatingUrl) return;
      // A receipt or dashboard link defines a fresh scope. Retained personal filters
      // must never silently hide the operation the administrator is trying to inspect.
      const retained = params.keys.length ? {} : this.workspace.auditFilters;
      for (const key of ['source', 'actionType', 'actor', 'permission', 'reference', 'riskLevel', 'target', 'operationId', 'from', 'to'] as const)
        this[key] = params.get(key) ?? retained[key] ?? (key === 'source' ? 'All' : '');
      const risk = params.get('risk');
      if (risk === 'Permanent' || risk === 'HighValue') this.applyRiskFilter(risk);
      this.advanced = !!(this.actor || this.permission || this.reference || this.operationId);
      if (this.operationId) this.expandedOperations.add(this.operationId);
      this.currentCursor = null; this.previousCursors = [];
      void this.loadAudit();
    });
  }

  ngOnDestroy(): void { this.querySubscription?.unsubscribe(); this.requestGeneration++; }

  hasPermission(permission: string): boolean {
    return this.operator.hasPermission(permission);
  }

  async copySummary(entry: AdministrationAuditEntry): Promise<void> {
    try { await navigator.clipboard.writeText(operationSummary(entry, this.operator.session?.environment ?? 'Unknown')); this.message = 'Internal operation summary copied. It includes the recorded reason; review it before sharing.'; this.messageTone = 'success'; }
    catch { this.showError('Clipboard unavailable. Select the visible receipt to copy it.'); }
  }

  async searchAudit(): Promise<void> {
    const saved: Record<string, string> = {};
    for (const key of ['source', 'actionType', 'actor', 'permission', 'reference', 'riskLevel', 'target', 'operationId', 'from', 'to'] as const) saved[key] = this[key];
    this.workspace.auditFilters = saved;
    // Keep personal search text in memory; only nonsensitive filter values enter history.
    const { actor, reference, target, ...safe } = saved;
    this.updatingUrl = true;
    try { await this.router.navigate([], { relativeTo: this.route, queryParams: safe, replaceUrl: true }); }
    finally { this.updatingUrl = false; }
    this.currentCursor = null;
    this.previousCursors = [];
    await this.loadAudit();
  }

  async applyQuickFilter(filter: 'permanent' | 'large-grants' | 'exports'): Promise<void> {
    this.actor = this.reference = this.target = this.operationId = this.from = this.to = '';
    this.expandedOperations.clear();
    this.advanced = false;
    this.source = 'Game';
    this.permission = '';
    if (filter === 'permanent') {
      this.actionType = '';
      this.riskLevel = 'Permanent';
    } else if (filter === 'large-grants') {
      this.actionType = '';
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
    else {
      this.expandedOperations.add(operationId);
      const id = this.entries.find(x => x.operationId === operationId)?.targetCharacterId;
      if (id && !this.targetNames[id]) void this.resolveTarget(id);
    }
  }

  private async resolveTarget(id: string): Promise<void> {
    const generation = this.requestGeneration;
    try { const response = await this.api.searchPlayers(id); const player = response.data?.find(x => x.characterId === id);
      if (player && generation === this.requestGeneration) this.targetNames[id] = player.characterName;
    } catch { /* Keep the verified identifier if its current name is unavailable. */ }
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
    this.actionType = '';
    this.riskLevel = risk;
    this.from = this.dateToLocalInput(new Date(now.getTime() - 24 * 60 * 60 * 1000));
    this.to = this.dateToLocalInput(now);
  }

  private async loadAudit(): Promise<void> {
    const generation = ++this.requestGeneration;
    this.loading = true;
    this.entries = []; this.nextCursor = null; this.loaded = false; this.unavailableSources = [];
    this.message = '';
    try {
      const response = await this.api.audit(this.filters(), this.currentCursor);
      if (generation !== this.requestGeneration) return;
      if (!response.isSuccess || !response.data) {
        this.showError(response.errorMessage || 'The audit history could not be loaded.');
        return;
      }
      this.entries = response.data.entries;
      for (const entry of this.entries) if (this.expandedOperations.has(entry.operationId) && entry.targetCharacterId) void this.resolveTarget(entry.targetCharacterId);
      for (const entry of this.entries) {
        if (this.journal.receipts.some(x => x.operationId === entry.operationId)) this.journal.record(entry.operationId, entry.targetCharacterId ?? entry.targetAccountId ?? '', actionLabel(entry.actionType), 'Completed');
      }
      this.nextCursor = response.data.nextCursor;
      this.unavailableSources = response.data.unavailableSources;
      this.loaded = true;
    } catch (error) {
      if (generation === this.requestGeneration) this.showError(this.errorMessage(error));
    } finally {
      if (generation === this.requestGeneration) this.loading = false;
    }
  }

  resetFilters(): void {
    this.source = 'All'; this.actionType = this.actor = this.permission = this.reference = this.riskLevel = this.target = this.operationId = this.from = this.to = '';
    void this.searchAudit();
  }

  async copy(value: string): Promise<void> {
    try { await navigator.clipboard.writeText(value); this.message = 'Operation reference copied.'; this.messageTone = 'success'; }
    catch { this.showError('Could not copy. Select the full operation reference in details.'); }
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
