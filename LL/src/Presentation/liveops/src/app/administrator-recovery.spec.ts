import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { convertToParamMap } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { PlayerWorkspaceComponent } from './features/players/player-workspace.component';
import { AccountRiskDetailComponent } from './features/account-risk/account-risk-detail.component';
import { AccountRiskListStateService } from './features/account-risk/account-risk-list-state.service';
import { ActionPreviewComponent } from './shared/action-preview/action-preview.component';
import { AnalyticsComponent } from './features/analytics/analytics.component';
import { CasesComponent } from './features/cases/cases.component';
import { AuditComponent } from './features/audit/audit.component';
import { LiveOpsApiService } from './liveops-api.service';
import { OperatorContextService } from './operator-context.service';
import { OperationJournalService } from './operation-journal.service';
import { WorkspaceStateService } from './workspace-state.service';
import { ActionPreview, ApiResponse, PlayerDetails, PlayerSupportSnapshot, TelemetrySnapshot, SupportCaseDetails } from './liveops.models';

const ok = <T>(data: T): ApiResponse<T> => ({ isSuccess: true, data, errorMessage: '' });
const flush = async () => { await Promise.resolve(); await Promise.resolve(); };
function deferred<T>() { let resolve!: (value: T) => void; const promise = new Promise<T>(r => resolve = r); return { promise, resolve }; }
function player(id: string, name = id): PlayerDetails { return { player: { characterId: id, accountId: 'account-' + id, characterName: name }, administrationHistory: [], chatHistory: [], chatAvailable: false, activeMute: null } as unknown as PlayerDetails; }
function operator() { const op = new OperatorContextService(); op.session = { subject: 'test', displayName: 'Operator', environment: 'Test', isDevelopmentOperator: true, permissions: ['liveops.read', 'liveops.accounts.moderate', 'liveops.economy.compensate'] }; return op; }
function preview(id = 'operation'): ActionPreview { return { previewToken: 'token', operationId: id, actionKind: 'CompensationGrant', title: 'Grant compensation items', targetName: 'A', targetId: 'A', riskLevel: 'Normal', expiresAt: new Date(Date.now() + 60000).toISOString(), confirmationText: null, fields: [], warnings: [] }; }
function workspace(api: Partial<LiveOpsApiService>) {
  const params = new BehaviorSubject(convertToParamMap({})); const query = of(convertToParamMap({}));
  const route = { paramMap: params, queryParamMap: query, snapshot: { queryParamMap: convertToParamMap({}) } };
  const state = new WorkspaceStateService(), journal = new OperationJournalService();
  const component = new PlayerWorkspaceComponent(api as LiveOpsApiService, route as never, { navigate: () => Promise.resolve(true) } as never, operator(), state, journal);
  return { component, params, state, journal };
}

describe('Administrator recovery and target isolation', () => {
  it('keeps an uncertain original operation unresolved when a subsequent retry is rejected', () => {
    const journal = new OperationJournalService();
    journal.record('original', 'A', 'Grant', 'Unknown', 'grant', 'Game');
    journal.record('original', 'A', 'Grant', 'Submitting'); journal.record('original', 'A', 'Grant', 'Rejected');
    expect(journal.unresolved[0].operationId).toBe('original');
    for (let i = 0; i < 25; i++) journal.record('other-' + i, 'B', 'Grant', 'Completed');
    expect(journal.unresolved[0].operationId).toBe('original');
    journal.record('original', 'A', 'Grant', 'Completed'); expect(journal.unresolved.length).toBe(0);
  });
  it('keeps the latest target on A-to-B-to-A navigation and clears target-bound drafts', async () => {
    const a1 = deferred<ApiResponse<PlayerDetails>>(), a2 = deferred<ApiResponse<PlayerDetails>>(), b = deferred<ApiResponse<PlayerDetails>>();
    const loads = [a1, b, a2];
    const f = workspace({ playerDetails: () => loads.shift()!.promise, playerSupportSnapshot: async () => ({ isSuccess: false, data: null, errorMessage: 'Snapshot unavailable' }) });
    f.component.ngOnInit(); f.params.next(convertToParamMap({ characterId: 'A' }));
    f.component.banReason = 'A reason'; f.component.signetReason = 'A grant'; f.component.signetConfirmation = true;
    f.params.next(convertToParamMap({ characterId: 'B' })); b.resolve(ok(player('B'))); await flush();
    expect(f.component.banReason).toBe(''); expect(f.component.signetReason).toBe(''); expect(f.component.signetConfirmation).toBeFalse();
    f.params.next(convertToParamMap({ characterId: 'A' })); a2.resolve(ok(player('A', 'Fresh A'))); await flush();
    a1.resolve(ok(player('A', 'Old A'))); await flush();
    expect(f.component.selectedPlayer?.player.characterName).toBe('Fresh A'); f.component.ngOnDestroy();
  });

  it('shows invalid searches and failures even without a selected player and retains finder context', async () => {
    const api = { searchPlayers: jasmine.createSpy().and.rejectWith(new Error('Lookup unavailable')) };
    const f = workspace(api); await f.component.searchPlayers(); expect(f.component.searchMessage).toContain('Enter');
    f.component.searchQuery = 'Player'; await f.component.searchPlayers(); expect(f.component.searchMessage).toContain('Lookup unavailable');
    api.searchPlayers.and.resolveTo(ok([player('A').player])); await f.component.searchPlayers();
    expect(f.state.playerSearch.results.length).toBe(1); expect(f.state.playerSearch.query).toBe('Player');
  });

  it('retains the old snapshot with a visible failed-refresh message', async () => {
    const f = workspace({ playerSupportSnapshot: async () => { throw new Error('Refresh unavailable'); } });
    f.component.selectedPlayer = player('A'); const old = { characterId: 'A' } as PlayerSupportSnapshot; f.component.supportSnapshot = old;
    await f.component.loadSupportSnapshot(); expect(f.component.supportSnapshot).toBe(old); expect(f.component.supportSnapshotError).toContain('Refresh unavailable');
  });

  it('reuses the original grant after a lost response, even after closing the review', async () => {
    const calls: string[] = [];
    const f = workspace({ previewGrantItems: async (_id, body: any) => { calls.push(body.operationId); return ok(preview(body.operationId)); }, grantItems: async () => { throw new HttpErrorResponse({ status: 0 }); } });
    f.component.selectedPlayer = player('A'); f.component.selectedItem = { id: 'potion', itemType: 'Resource' } as never; f.component.grantReason = 'Case replacement';
    await f.component.grantItems(); await f.component.confirmActionPreview();
    expect(f.journal.unresolved.length).toBe(1); f.component.closeActionPreview();
    await f.component.grantItems(); expect(calls[1]).toBe(calls[0]);
    expect(f.component.actionPreview?.operationId).toBe(f.journal.unresolved[0].operationId);
  });

  it('uses the server-returned moderation expiry instead of the browser clock', async () => {
    const expiresAt = '2040-01-02T03:04:05Z'; let submitted: any;
    const f = workspace({ previewBan: async (_id, body: any) => { expect(body.durationMinutes).toBe(1440); expect(body.expiresAt).toBeNull(); return ok({ ...preview(body.operationId), effectExpiresAt: expiresAt }); }, ban: async (_id, body) => { submitted = body; return ok({}); }, playerDetails: async () => ok(player('A')), playerSupportSnapshot: async () => ({ isSuccess: false, data: null, errorMessage: 'Unavailable' }) });
    f.component.selectedPlayer = player('A'); f.component.banReason = 'Confirmed account issue';
    await f.component.applyBan(); await f.component.confirmActionPreview(); expect(submitted.expiresAt).toBe(expiresAt);
  });

  it('retries investigation notes with the same operation and resets drafts when the subject changes', async () => {
    const requests: any[] = [];
    const api = { addAccountRiskNote: async (_id: string, body: object) => { requests.push(body); throw new HttpErrorResponse({ status: 0 }); } };
    const detail = new AccountRiskDetailComponent(api as never, {} as never, {} as never, new AccountRiskListStateService(), operator());
    detail.details = { account: { accountId: 'A' }, notes: [] } as never; detail.note = 'Reviewed evidence';
    await detail.addNote(); await detail.addNote(); expect(requests[1].operationId).toBe(requests[0].operationId);
    detail.details = { account: { accountId: 'B' }, notes: [] } as never; await detail.addNote(); expect(requests[2].operationId).not.toBe(requests[0].operationId);
  });

  it('keeps a conflicting case note until the administrator refreshes and reviews it', async () => {
    const api = { changeCase: jasmine.createSpy().and.rejectWith(new HttpErrorResponse({ status: 409, error: { errorMessage: 'Case changed' } })) };
    const state = new WorkspaceStateService();
    const component = new CasesComponent(api as never, {} as never, {} as never, operator(), new OperationJournalService(), state);
    component.caseId = 'case'; component.detail = { case: { version: 2 }, entries: [] } as unknown as SupportCaseDetails; component.note = 'My evidence';
    component.addNote(); await flush(); expect(component.conflict).toBeTrue(); expect(component.note).toBe('My evidence');
    component.addNote(); await flush(); expect(api.changeCase).toHaveBeenCalledTimes(1);
  });
});

describe('Accessible operation reviews', () => {
  it('focuses and traps keyboard input, blocks busy dismissal, and disables an expired review', async () => {
    await TestBed.configureTestingModule({ imports: [ActionPreviewComponent] }).compileComponents();
    const trigger = document.createElement('button'); document.body.appendChild(trigger); trigger.focus();
    const f = TestBed.createComponent(ActionPreviewComponent);
    f.componentRef.setInput('session', operator().session); f.componentRef.setInput('preview', { ...preview(), confirmationText: 'A' }); f.detectChanges();
    expect(document.activeElement?.tagName).toBe('INPUT');
    const cancel = jasmine.createSpy(); f.componentInstance.cancel.subscribe(cancel);
    f.componentRef.setInput('submitting', true); f.detectChanges(); f.componentInstance.requestCancel(); expect(cancel).not.toHaveBeenCalled();
    f.componentRef.setInput('submitting', false); f.componentRef.setInput('preview', { ...preview(), expiresAt: new Date(Date.now() - 1000).toISOString() }); f.detectChanges();
    expect(f.componentInstance.canSubmit).toBeFalse(); expect(f.nativeElement.textContent).toContain('review expired');
    const buttons = f.nativeElement.querySelectorAll('button:not(:disabled)'); const last = buttons[buttons.length - 1] as HTMLButtonElement; last.focus();
    const tab = new KeyboardEvent('keydown', { key: 'Tab', cancelable: true }); f.componentInstance.onKeydown(tab); expect(tab.defaultPrevented).toBeTrue(); expect(document.activeElement).toBe(buttons[0]);
    f.destroy(); expect(document.activeElement).toBe(trigger); trigger.remove();
  });

  it('does not renew expiry when confirmation text changes and tolerates a skewed browser clock', async () => {
    await TestBed.configureTestingModule({ imports: [ActionPreviewComponent] }).compileComponents(); const f = TestBed.createComponent(ActionPreviewComponent);
    f.componentRef.setInput('session', operator().session);
    f.componentRef.setInput('preview', { ...preview(), serverTimeUtc: '2020-01-01T00:00:00Z', expiresAt: '2020-01-01T00:01:00Z' }); f.detectChanges();
    expect(f.componentInstance.expired).toBeFalse(); f.componentInstance.now += 61000;
    f.componentRef.setInput('confirmation', 'A'); f.detectChanges(); expect(f.componentInstance.expired).toBeTrue(); f.destroy();
  });
});

describe('Analytics comparison semantics', () => {
  it('marks missing reports as gaps and suppresses incomplete period percentage changes', () => {
    const component = new AnalyticsComponent({} as never); component.days = 7;
    component.reports = [{ reportDateUtc: '2026-09-28', population: { dau: 10 } }, { reportDateUtc: '2026-09-26', population: { dau: 0 } }] as TelemetrySnapshot[];
    expect(component.trend.find(x => x.date === '2026-09-27')?.dau).toBeNull();
    expect(component.trend.find(x => x.date === '2026-09-26')?.dau).toBe(0); expect(component.comparison.delta).toBeNull();
  });
  it('compares equal calendar windows using mean daily active accounts', () => {
    const component = new AnalyticsComponent({} as never); component.days = 7;
    component.reports = Array.from({ length: 14 }, (_, i) => ({ reportDateUtc: new Date(Date.UTC(2026, 8, 28 - i)).toISOString().slice(0, 10), population: { dau: i < 7 ? 20 : 10 } })) as TelemetrySnapshot[];
    expect(component.comparison.delta).toBe(100); expect(component.comparison.currentCount).toBe(7);
  });
});

describe('Activity receipt navigation', () => {
  it('opens successive receipt links without inheriting unrelated retained filters', async () => {
    const params = new BehaviorSubject(convertToParamMap({ operationId: 'first', source: 'Game' }));
    const api = { audit: jasmine.createSpy().and.resolveTo(ok({ entries: [], nextCursor: null, unavailableSources: [] })) };
    const state = new WorkspaceStateService(); state.auditFilters = { actor: 'Other operator', target: 'Other player', riskLevel: 'Permanent', from: '2020-01-01T00:00' };
    const component = new AuditComponent(api as never, { queryParamMap: params } as never, {} as never, operator(), state);
    component.ngOnInit(); await flush();
    expect(api.audit.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({ operationId: 'first', actor: '', target: '', riskLevel: '', from: undefined }));
    params.next(convertToParamMap({ operationId: 'second', source: 'Chat' })); await flush();
    expect(api.audit.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({ operationId: 'second', source: 'Chat' }));
    component.ngOnDestroy();
  });

  it('retains personal filters during its own URL update without issuing duplicate requests', async () => {
    const params = new BehaviorSubject(convertToParamMap({}));
    const api = { audit: jasmine.createSpy().and.resolveTo(ok({ entries: [], nextCursor: null, unavailableSources: [] })) };
    const router = { navigate: async (_: unknown, options: any) => { expect(options.queryParams.actor).toBeUndefined(); params.next(convertToParamMap(options.queryParams)); return true; } };
    const component = new AuditComponent(api as never, { queryParamMap: params } as never, router as never, operator());
    component.ngOnInit(); await flush(); component.actor = 'Current operator';
    await component.searchAudit(); expect(component.actor).toBe('Current operator'); expect(api.audit).toHaveBeenCalledTimes(2);
    component.ngOnDestroy();
  });
});
