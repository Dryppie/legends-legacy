import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { combineLatest, Subscription } from 'rxjs';
import { LiveOpsApiService } from '../../liveops-api.service';
import { ApiResponse, PlayerSummary, SupportCaseDetails, SupportCasePage, SupportCaseStatus } from '../../liveops.models';
import { OperatorContextService } from '../../operator-context.service';
import { OperationJournalService } from '../../operation-journal.service';
import { WorkspaceStateService } from '../../workspace-state.service';
import { readableName } from '../../shared/admin-presentation';
import { DraftHandle, OperatorDraftService } from '../../operator-draft.service';
import { DraftStatusComponent } from '../../shared/draft-status.component';

@Component({ selector: 'app-cases', standalone: true, imports: [CommonModule, FormsModule, RouterLink, DraftStatusComponent], templateUrl: './cases.component.html' })
export class CasesComponent implements OnInit, OnDestroy {
  readonly statuses: SupportCaseStatus[] = ['Open', 'Waiting', 'Resolved', 'Closed'];
  readonly categories = ['Missing item', 'Activity', 'Restriction', 'Chat', 'Transfer', 'Signets', 'Other'];
  readonly timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  readonly readableName = readableName;
  list: SupportCasePage | null = null; detail: SupportCaseDetails | null = null;
  loading = false; saving = false; message = ''; failed = false; conflict = false;
  caseId = ''; characterId = ''; playerName = ''; query = ''; status = ''; page = 1;
  filterCategory = ''; sort = 'follow-up'; overdue = false;
  priority = 'Normal'; followUpAt = ''; nextAction = '';
  title = ''; category = 'Other'; description = ''; externalReference = '';
  playerResponse = '';
  note = ''; evidence = ''; resolution = ''; nextStatus: SupportCaseStatus = 'Resolved';
  linkedOperation = ''; linkedSource = 'Game'; linkReason = '';
  playerQuery = ''; players: PlayerSummary[] = []; playerSearchMessage = '';
  draft?: DraftHandle; draftLoading = false; summaryMessage = '';
  private draftGeneration = 0;
  private generation = 0; private subscription?: Subscription; private pending = new Map<string, string>();
  constructor(private api: LiveOpsApiService, private route: ActivatedRoute, private router: Router,
    readonly operator: OperatorContextService, private journal: OperationJournalService, private state: WorkspaceStateService,
    readonly drafts?: OperatorDraftService) {}
  get draftBlocked(): boolean { return this.draftLoading || (!!this.draft && (!this.draft.loaded || this.draft.conflict)); }
  get canManage(): boolean { return this.operator.hasPermission(this.operator.permissions.account); }
  ngOnInit(): void {
    this.query = this.state.caseFilters['search'] ?? ''; this.status = this.route.snapshot?.queryParamMap.get('status') ?? this.state.caseFilters['status'] ?? '';
    this.subscription = combineLatest([this.route.paramMap, this.route.queryParamMap]).subscribe(([params, query]) => {
      this.saveDraft(); this.caseId = params.get('caseId') ?? '';
      const queueLink = query.has('status') || query.has('sort') || query.has('overdue');
      if (query.has('lookup')) this.query = this.state.caseFilters['search'] ?? '';
      else if (queueLink) this.query = '';
      this.status = query.get('status') ?? (queueLink ? '' : this.state.caseFilters['status']) ?? '';
      this.sort = query.get('sort') || (queueLink ? 'follow-up' : this.state.caseFilters['sort']) || 'follow-up';
      this.overdue = (query.get('overdue') ?? (queueLink ? 'false' : this.state.caseFilters['overdue'])) === 'true';
      this.filterCategory = queueLink ? '' : this.state.caseFilters['category'] ?? '';
      this.priority = 'Normal'; this.followUpAt = this.nextAction = '';
      const characterId = query.get('characterId') ?? '';
      if (this.characterId !== characterId) { this.title = ''; this.description = ''; this.externalReference = ''; this.playerName = ''; this.players = []; this.playerSearchMessage = ''; }
      this.characterId = characterId;
      this.linkedOperation = query.get('operationId') ?? ''; this.linkedSource = query.get('source') === 'Chat' ? 'Chat' : 'Game';
      this.detail = null; this.message = ''; this.conflict = false; this.saving = false;
      const draft = this.state.caseDrafts[this.caseId]; this.note = draft?.note ?? ''; this.evidence = draft?.evidence ?? '';
      this.resolution = ''; this.linkReason = ''; this.pending.clear(); this.page = this.characterId || query.has('lookup') || queueLink ? 1 : Math.max(1, Number(this.state.caseFilters['page']) || 1);
      // Consume a queue shortcut into the private saved view, so Back resumes subsequent filtering/pagination.
      if (queueLink && !this.caseId && !this.characterId) {
        this.rememberFilters();
        void this.router.navigate(['/cases'], { replaceUrl: true });
        return;
      }
      if (this.canManage) { void this.load(); void this.openDraft(); }
    });
  }
  ngOnDestroy(): void { this.saveDraft(); this.generation++; this.draftGeneration++; this.subscription?.unsubscribe(); }
  async openDraft(): Promise<void> {
    const generation = ++this.draftGeneration; this.draft = undefined;
    if (!this.drafts || (!this.caseId && !this.characterId)) { this.draftLoading = false; return; }
    this.draftLoading = true;
    const draft = await this.drafts.open(this.caseId ? 'case:' + this.caseId : 'new-case:' + this.characterId);
    if (generation !== this.draftGeneration) return;
    this.draft = draft; this.draftLoading = false; this.restoreDraft();
  }
  restoreDraft(useOperationLink = true): void {
    if (!this.draft?.loaded) return;
    for (const key of ['title', 'description', 'externalReference', 'note', 'evidence', 'resolution', 'linkReason', 'playerResponse', 'followUpAt', 'nextAction'] as const) this[key] = this.draft.data[key] ?? '';
    this.category = this.draft.data['category'] || 'Other';
    this.priority = this.draft.data['priority'] || 'Normal';
    const status = this.draft.data['nextStatus'] as SupportCaseStatus;
    this.nextStatus = this.statuses.includes(status) ? status : 'Resolved';
    // An explicit completed-operation link takes precedence over an earlier draft.
    this.linkedOperation = (useOperationLink ? this.route.snapshot?.queryParamMap.get('operationId') : null) ?? this.draft.data['linkedOperation'] ?? '';
    this.linkedSource = (useOperationLink ? this.route.snapshot?.queryParamMap.get('source') : null) ?? this.draft.data['linkedSource'] ?? 'Game';
  }
  saveDraft(): void {
    if (this.caseId) this.state.caseDrafts[this.caseId] = { note: this.note, evidence: this.evidence };
    if (!this.draft?.loaded || this.draftLoading || !this.drafts) return;
    const data: Record<string, string> = {};
    for (const key of ['title', 'category', 'description', 'externalReference', 'note', 'evidence', 'resolution', 'nextStatus', 'linkedOperation', 'linkedSource', 'linkReason', 'playerResponse', 'priority', 'followUpAt', 'nextAction'] as const)
      if (this[key] && !(key === 'category' && this[key] === 'Other') && !(key === 'nextStatus' && this[key] === 'Resolved') && !(key === 'linkedSource' && this[key] === 'Game')) data[key] = this[key];
    this.drafts.update(this.draft, data);
  }
  discardDraft(): void {
    if (!this.draft || !this.drafts || this.draftBlocked || this.saving) return;
    this.drafts.update(this.draft, {}); this.restoreDraft(false); void this.drafts.save(this.draft);
  }
  preparePlayerResponse(): void {
    if (this.detail && !this.playerResponse) { this.playerResponse = `Hello ${this.detail.case.characterName},

We have reviewed your support request.

[Describe the finding and any next steps you intend to share.]

Reference: ${this.detail.case.id}`; this.saveDraft(); }
  }
  async copyPlayerResponse(): Promise<void> {
    try { await navigator.clipboard.writeText(this.playerResponse); this.summaryMessage = 'Reviewed response copied. Nothing has been sent.'; }
    catch { this.summaryMessage = 'Clipboard unavailable. Select the response text to copy it.'; }
  }
  async copySummary(): Promise<void> {
    if (!this.detail) return;
    const c = this.detail.case;
    const operations = this.detail.entries.filter(x => x.linkedOperationId).map(x => `${x.linkedSource}: ${x.linkedOperationId}`);
    const summary = [`Support case: ${c.title}`, `Player: ${c.characterName}`, `Case reference: ${c.id}`, `Status: ${c.status}`, `Category: ${c.category}`,
      ...(c.externalReference ? [`External reference: ${c.externalReference}`] : []), `Resolution: ${c.resolution || 'Not yet recorded'}`,
      `Updated: ${new Date(c.updatedAt).toISOString()}`, ...(operations.length ? ['Linked operations in loaded history:', ...operations] : []),
      ...(this.detail.nextBeforeSequence ? ['Older history is available in LiveOps.'] : [])].join('\n');
    try { await navigator.clipboard.writeText(summary); this.summaryMessage = 'Internal case summary copied. It includes the recorded resolution; review it before sharing.'; }
    catch { this.summaryMessage = 'Clipboard unavailable. Select the visible case details to copy them.'; }
  }
  async load(older = false): Promise<void> {
    const generation = ++this.generation; const caseId = this.caseId;
    this.loading = true; this.failed = false; this.message = '';
    try {
      if (caseId) {
        const result = await this.api.supportCase(caseId, older ? this.detail?.nextBeforeSequence ?? undefined : undefined);
        if (generation !== this.generation) return;
        if (!result.isSuccess || !result.data) throw new Error(result.errorMessage);
        if (older && this.detail) {
          const seen = new Set(this.detail.entries.map(x => x.id));
          this.detail = { ...result.data, entries: [...this.detail.entries, ...result.data.entries.filter(x => !seen.has(x.id))] };
        } else this.detail = result.data;
        this.conflict = false;
      } else {
        this.rememberFilters();
        const result = await this.api.cases({ ...(this.characterId ? { characterId: this.characterId } : {}),
          ...(this.status ? { status: this.status } : {}), search: this.query, page: String(this.page), category: this.filterCategory, sort: this.sort, overdue: String(this.overdue) });
        if (generation !== this.generation) return;
        if (!result.isSuccess || !result.data) throw new Error(result.errorMessage);
        this.list = result.data;
        if (this.characterId) {
          const player = await this.api.playerDetails(this.characterId);
          if (generation === this.generation && player.data) this.playerName = player.data.player.characterName;
        }
      }
    } catch (error) { if (generation === this.generation) { this.failed = true; this.message = this.error(error); } }
    finally { if (generation === this.generation) this.loading = false; }
  }
  filter(): void { this.page = 1; void this.load(); }
  private rememberFilters(): void { this.state.caseFilters = { search: this.query, status: this.status, category: this.filterCategory, sort: this.sort, overdue: String(this.overdue), page: String(this.page) }; }
  changePage(delta: number): void { this.page += delta; void this.load(); }
  async findPlayer(): Promise<void> {
    if (!this.playerQuery.trim()) { this.playerSearchMessage = 'Enter a character name, account email or identifier.'; return; }
    const query = this.playerQuery; const generation = this.generation;
    try {
      const result = await this.api.searchPlayers(query);
      if (generation !== this.generation || query !== this.playerQuery) return;
      if (!result.isSuccess || !result.data) throw new Error(result.errorMessage);
      this.players = result.data; this.playerSearchMessage = this.players.length ? 'Select the exact player. Up to 20 matches are shown.' : 'No matching player.';
    } catch (error) { if (generation === this.generation && query === this.playerQuery) this.playerSearchMessage = this.error(error); }
  }
  selectPlayer(player: PlayerSummary): void {
    this.saveDraft();
    if (this.characterId !== player.characterId) { this.title = ''; this.description = ''; this.externalReference = ''; }
    this.characterId = player.characterId; this.playerName = player.characterName; this.players = [];
    void this.openDraft();
  }
  create(): void {
    if (!this.characterId || this.title.trim().length < 3 || !this.description.trim()) { this.failed = true; this.message = 'Select a player and enter a title and issue description.'; return; }
    const payload = { characterId: this.characterId, title: this.title.trim(), category: this.category,
      body: this.description.trim(), externalReference: this.externalReference.trim() || null };
    void this.mutate('create', payload, operationId => this.api.createCase({ operationId, ...payload }));
  }
  addNote(): void { this.change('notes', { body: this.note.trim(), evidenceReference: this.evidence.trim() || null }); }
  updateStatus(): void { this.change('status', { status: this.nextStatus, body: this.resolution.trim() }); }
  link(): void { this.change('operations', { linkedOperationId: this.linkedOperation.trim(), source: this.linkedSource, body: this.linkReason.trim() }); }
  useCurrentPlan(): void {
    if (!this.detail) return;
    const c = this.detail.case;
    this.priority = c.priority || 'Normal'; this.nextAction = c.nextAction || '';
    const date = c.followUpAt ? new Date(c.followUpAt) : null;
    this.followUpAt = date ? new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16) : '';
    this.saveDraft();
  }
  planFollowUp(): void {
    const date = this.followUpAt ? new Date(this.followUpAt) : null;
    if (!this.nextAction.trim() || (date && !Number.isFinite(date.getTime()))) { this.failed = true; this.message = 'Enter a next action and a valid follow-up time.'; return; }
    this.change('follow-up', { priority: this.priority, followUpAt: date?.toISOString() ?? null, nextAction: this.nextAction.trim(),
      body: `${this.priority} priority. Follow-up: ${date?.toISOString() ?? 'Unscheduled'}. Next action: ${this.nextAction.trim()}` });
  }
  isOverdue(date?: string | null): boolean { return !!date && Date.parse(date) <= Date.now(); }
  resetFilters(): void { this.query = this.status = this.filterCategory = ''; this.overdue = false; this.sort = 'follow-up'; this.filter(); }
  private change(action: 'notes' | 'status' | 'operations' | 'follow-up', payload: Record<string, unknown>): void {
    if (!this.detail || !payload['body']) { this.failed = true; this.message = 'Explain the note, decision or operation link before saving.'; return; }
    const caseId = this.caseId;
    const body = { expectedVersion: this.detail.case.version, ...payload };
    void this.mutate(action, { caseId, ...body }, operationId => this.api.changeCase(caseId, action, { operationId, ...body }));
  }
  private async mutate(kind: string, payload: object, submit: (operationId: string) => Promise<ApiResponse<SupportCaseDetails>>): Promise<void> {
    if (this.saving || this.loading || this.conflict || this.draftBlocked || this.operator.sessionExpired) return;
    if (this.journal.recoveryIncomplete) { this.failed = true; this.message = this.journal.recoveryMessage; return; }
    const key = JSON.stringify([kind, payload]); const operationId = this.pending.get(key) ?? this.journal.unresolved.find(x => x.targetId === (this.caseId || this.characterId) && x.kind === 'case-' + kind)?.operationId ?? crypto.randomUUID(); this.pending.set(key, operationId);
    const generation = this.generation; const target = this.caseId || this.characterId;
    this.saving = true; this.failed = false; this.message = ''; this.journal.record(operationId, target, 'Support case ' + kind, 'Submitting', 'case-' + kind, 'Game');
    try {
      const result = await submit(operationId);
      this.journal.record(operationId, target, 'Support case ' + kind, result.isSuccess ? 'Completed' : 'Rejected');
      if (generation !== this.generation) return;
      if (!result.isSuccess || !result.data) throw new Error(result.errorMessage);
      this.pending.delete(key); this.detail = result.data;
      if (kind === 'create') {
        this.title = ''; this.description = ''; this.externalReference = ''; this.saveDraft();
        void this.router.navigate(['/cases', result.data.case.id]); return;
      }
      if (kind === 'notes') { this.note = ''; this.evidence = ''; this.saveDraft(); }
      if (kind === 'follow-up') { this.nextAction = this.followUpAt = ''; this.priority = 'Normal'; }
      if (kind === 'status') this.resolution = '';
      if (kind === 'operations') { this.linkedOperation = ''; this.linkReason = ''; }
      this.saveDraft();
      this.message = 'Case saved. The change is recorded in the activity log.';
    } catch (error) {
      if (error instanceof HttpErrorResponse) this.journal.record(operationId, target, 'Support case ' + kind, error.status >= 400 && error.status < 500 ? 'Rejected' : 'Unknown');
      if (generation !== this.generation) return;
      this.conflict = error instanceof HttpErrorResponse && error.status === 409;
      this.failed = true; this.message = this.error(error) + (this.conflict ? ' Your draft is retained; refresh before saving again.' : ' For an uncertain result, check the activity log or retry this unchanged request.');
    } finally { if (generation === this.generation) this.saving = false; }
  }
  private error(error: unknown): string { return error instanceof HttpErrorResponse ? error.error?.errorMessage ?? error.message : error instanceof Error ? error.message : 'The request failed.'; }
}
