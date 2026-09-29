import { Injectable } from '@angular/core';
import { PlayerSummary } from './liveops.models';
import { DraftHandle, OperatorDraftService } from './operator-draft.service';

@Injectable({ providedIn: 'root' })
export class WorkspaceStateService {
  private drafts?: OperatorDraftService;
  draft?: DraftHandle;
  private search = { query: '', results: [] as PlayerSummary[], message: '', searched: false };
  private cases: Record<string, string> = {};
  private audit: Record<string, string> = {};
  get playerSearch() { return this.search; }
  set playerSearch(value: typeof this.search) { this.search = value; this.persist(); }
  get caseFilters() { return this.cases; }
  set caseFilters(value: Record<string, string>) { this.cases = value; this.persist(); }
  caseDrafts: Record<string, { note: string; evidence: string }> = {};
  get auditFilters() { return this.audit; }
  set auditFilters(value: Record<string, string>) { this.audit = value; this.persist(); }
  async initialize(drafts: OperatorDraftService): Promise<void> {
    this.drafts = drafts; this.draft = await drafts.open('workspace');
    const data = this.draft.data;
    this.search = { query: data['playerQuery'] ?? '', results: [], message: '', searched: false };
    this.cases = {};
    for (const key of ['search', 'status', 'category', 'sort', 'overdue', 'page']) this.cases[key] = data['case' + key[0].toUpperCase() + key.slice(1)] ?? '';
    this.audit = {};
    for (const key of ['source', 'actionType', 'actor', 'permission', 'reference', 'riskLevel', 'target', 'operationId', 'from', 'to'])
      this.audit[key] = data['audit' + key[0].toUpperCase() + key.slice(1)] ?? '';
  }
  get analyticsPreferences(): string { return this.draft?.data['analyticsPreferences'] ?? '{}'; }
  set analyticsPreferences(value: string) { if (this.draft && this.drafts) this.drafts.update(this.draft, { ...this.draft.data, analyticsPreferences: value }); }
  private persist(): void {
    if (!this.draft || !this.drafts) return;
    const data: Record<string, string> = { playerQuery: this.search.query, caseSearch: this.cases['search'] ?? '', caseStatus: this.cases['status'] ?? '' };
    for (const key of ['category', 'sort', 'overdue', 'page']) data['case' + key[0].toUpperCase() + key.slice(1)] = this.cases[key] ?? '';
    if (this.draft.data['analyticsPreferences']) data['analyticsPreferences'] = this.draft.data['analyticsPreferences'];
    for (const [key, value] of Object.entries(this.audit)) data['audit' + key[0].toUpperCase() + key.slice(1)] = value;
    this.drafts.update(this.draft, data);
  }
  clear(): void {
    this.draft = undefined; this.drafts = undefined;
    this.playerSearch = { query: '', results: [], message: '', searched: false };
    this.auditFilters = {}; this.caseFilters = {}; this.caseDrafts = {};
  }
}
