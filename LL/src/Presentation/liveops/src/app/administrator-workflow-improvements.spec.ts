import { AccountRiskComponent } from './features/account-risk/account-risk.component';
import { AccountRiskListStateService } from './features/account-risk/account-risk-list-state.service';
import { AccountRiskPage, TelemetrySnapshot } from './liveops.models';
import { AuditComponent } from './features/audit/audit.component';
import { AnalyticsState } from './features/analytics/analytics-state.service';
import { PreparationDraft } from './shared/preparation-draft';
import { OperatorDraftService } from './operator-draft.service';
import { csvText } from './shared/csv-export';
import { CasesComponent } from './features/cases/cases.component';
import { WorkspaceStateService } from './workspace-state.service';
import { SupportEvidenceComponent } from './shared/support-evidence.component';
import { convertToParamMap } from '@angular/router';
import { BehaviorSubject } from 'rxjs';

function deferred<T>() { let resolve!: (value: T) => void; let reject!: (error: unknown) => void; const promise = new Promise<T>((a, b) => { resolve = a; reject = b; }); return { promise, resolve, reject }; }
function page(ids: string[], total = ids.length): AccountRiskPage { return { entries: ids.map(accountId => ({ accountId, investigationStatus: 'Unreviewed' })), counts: { Low: ids.length }, total, page: 1, pageSize: 2, directTransferCount: 1 } as AccountRiskPage; }
const ok = <T>(data: T) => ({ isSuccess: true, data, errorMessage: '' });
function queue(ids: string[], total = ids.length) {
  const state = new AccountRiskListStateService();
  state.save({ data: page(ids, total), page: 1, status: 'Unreviewed', minimumSeverity: 'Low', search: '', signalType: '', minimumScore: '', maximumAccountAgeDays: '', sort: 'risk' });
  return state;
}
describe('Administrator workflow improvements', () => {
  it('opens the whole due queue without old search/status/category constraints and resumes its saved page', async () => {
    const state = new WorkspaceStateService();
    state.caseFilters = { search: 'old player', status: 'Closed', category: 'Chat', sort: 'recent', page: '4' };
    const params = new BehaviorSubject(convertToParamMap({}));
    const query = new BehaviorSubject(convertToParamMap({ sort: 'follow-up', overdue: 'true' }));
    const route = { paramMap: params, queryParamMap: query };
    const api = { cases: jasmine.createSpy().and.resolveTo(ok({ cases: [], total: 0 })) };
    const router = { navigate: jasmine.createSpy().and.callFake(() => { query.next(convertToParamMap({})); return Promise.resolve(true); }) };
    const operator = { permissions: { account: 'account' }, hasPermission: () => true };
    const component = new CasesComponent(api as any, route as any, router as any, operator as any, {} as any, state);
    component.ngOnInit(); await Promise.resolve();
    expect(api.cases.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({ search: '', category: '', sort: 'follow-up', overdue: 'true', page: '1' }));
    expect(api.cases.calls.mostRecent().args[0].status).toBeUndefined();
    component.page = 3; await component.load();
    query.next(convertToParamMap({})); expect(component.page).toBe(3);
    component.ngOnDestroy();
  });
  it('stages selected evidence without overwriting other case preparation or submitting a case note', async () => {
    const api = { supportCase: jasmine.createSpy().and.resolveTo(ok({ case: { characterId: 'player' } })) };
    const draft = { loaded: true, data: { note: 'Existing note', resolution: 'Private resolution' }, dirty: false };
    const drafts = { open: jasmine.createSpy().and.resolveTo(draft), update: (_draft: unknown, data: any) => draft.data = data, save: jasmine.createSpy().and.resolveTo(undefined) };
    const router = { navigate: jasmine.createSpy().and.resolveTo(true) };
    const component = new SupportEvidenceComponent(api as any, drafts as any, router as any);
    component.caseId = 'case'; component.snapshot = { characterId: 'player', generatedAtUtc: '2026-09-28T00:00:00Z', economy: { data: null }, transfers: { data: null }, synchronization: { data: { pendingDeliveries: 1, failedDeliveries: 0 }, source: 'Game', fetchedAtUtc: '2026-09-28T00:00:00Z' } } as any;
    component.ngOnChanges(); component.toggle(component.rows[0].reference); await component.prepareCase();
    expect(draft.data.note).toContain('Existing note\n\nEvidence for character player');
    expect(draft.data.note).toContain('not correlated to a reward or grant');
    expect(draft.data.resolution).toBe('Private resolution'); expect(router.navigate).toHaveBeenCalledWith(['/cases', 'case']);
    api.supportCase.and.resolveTo(ok({ case: { characterId: 'other-player' } })); await component.prepareCase();
    expect(component.message).toContain('could not be verified'); expect(drafts.save).toHaveBeenCalledTimes(1);
  });
  it('lets package-grant evidence lines be selected independently while retaining the operation reference', () => {
    const component = new SupportEvidenceComponent({} as any, {} as any, {} as any);
    const line = { operationId: 'package-operation', itemBaseId: 'potion', itemName: 'Potion', quantity: 1, occurredAtUtc: '2026-09-28T00:00:00Z' };
    component.snapshot = { characterId: 'player', economy: { data: { recentCompensationGrants: [line, line] } }, transfers: { data: null }, synchronization: { data: null } } as any;
    component.ngOnChanges(); component.toggle(component.rows[0].reference);
    expect(new Set(component.rows.map(x => x.reference)).size).toBe(2);
    expect(component.selected.has(component.rows[1].reference)).toBeFalse();
    expect(component.rows[0].reference).toContain('package-operation');
  });
  it('ignores out-of-order investigation successes and failures', async () => {
    const first = deferred<any>(), second = deferred<any>();
    const api = { accountRisks: jasmine.createSpy().and.returnValues(first.promise, second.promise) };
    const component = new AccountRiskComponent(api as any, {} as any, new AccountRiskListStateService());
    const a = component.quickFilter('high'), b = component.quickFilter('all');
    second.resolve(ok(page(['new']))); await b;
    first.reject(new Error('old failure')); await a;
    expect(component.data?.entries[0].accountId).toBe('new'); expect(component.message).toBe(''); expect(component.loading).toBeFalse();
  });
  it('does not invent zero counts or missing evidence on initial failure', async () => {
    const component = new AccountRiskComponent({ accountRisks: () => Promise.reject(new Error('offline')) } as any, {} as any, new AccountRiskListStateService());
    await component.applyFilters(); expect(component.count('Critical')).toBe('—'); expect(component.emptyMessage).toContain('unavailable');
  });
  it('advances A to B to C without marking a viewed record reviewed', () => {
    const state = queue(['A', 'B', 'C']); expect(state.nextAccount('A')).toBe('B'); expect(state.nextAccount('B')).toBe('C'); expect(state.nextAccount('C')).toBeNull();
    state.updateInvestigationStatus('B', 'Investigating'); expect(state.nextAccount('B')).toBe('C');
  });
  it('rechecks a shortened page before moving on, then reaches a clear end', async () => {
    const state = queue(['A', 'B'], 3); state.updateInvestigationStatus('B', 'Investigating');
    const api = { accountRisks: jasmine.createSpy().and.resolveTo(ok(page(['A', 'C'], 2))) };
    expect(await state.advance(api as any, 'B')).toBe('C');
    expect(await state.advance(api as any, 'C')).toBeNull(); expect(state.exhausted).toBeTrue();
    expect(api.accountRisks.calls.first().args[0].data).toBeUndefined();
  });
  it('loads another queue page when it has unseen accounts', async () => {
    const state = queue(['A', 'B'], 3);
    const api = { accountRisks: jasmine.createSpy().and.returnValues(Promise.resolve(ok(page(['A', 'B'], 3))), Promise.resolve(ok(page(['C'], 3)))) };
    expect(await state.advance(api as any, 'B')).toBe('C'); expect(api.accountRisks.calls.mostRecent().args[1]).toBe(2);
  });
  it('quick audit views remove receipt and personal constraints', async () => {
    const api = { audit: jasmine.createSpy().and.resolveTo(ok({ entries: [], nextCursor: null, unavailableSources: [] })) };
    const component = new AuditComponent(api as any, {} as any, { navigate: () => Promise.resolve(true) } as any, { permissions: { superadmin: 'superadmin' } } as any);
    component.operationId = 'previous'; component.actor = 'someone'; component.target = 'target'; component.reference = 'ticket'; component.from = '2026-09-20';
    await component.applyQuickFilter('exports');
    expect(api.audit.calls.mostRecent().args[0]).toEqual(jasmine.objectContaining({ actionType: 'AuditExported', operationId: '', actor: '', target: '', reference: '', from: undefined }));
  });
  it('anchors trend and comparison to the selected day and excludes newer reports', () => {
    const state = new AnalyticsState({} as any); state.days = 7; state.selectedDate = '2026-09-20';
    state.reports = ['2026-09-27', '2026-09-20', '2026-09-13'].map((date, i) => ({ reportDateUtc: date, population: { dau: [100, 10, 5][i] } } as TelemetrySnapshot));
    expect(state.trend.at(-1)?.date).toBe('2026-09-20'); expect(state.comparison.current).toBe(10); expect(state.comparison.previous).toBe(5); expect(state.comparison.delta).toBeNull();
  });
  it('keeps preparation private to its target and never restores preview tokens', async () => {
    const api = { operatorDraft: jasmine.createSpy().and.resolveTo(ok({ content: '{}', version: 'v1' })), saveOperatorDraft: jasmine.createSpy().and.resolveTo(ok({ version: 'v2' })) };
    const drafts = new OperatorDraftService(api as any); drafts.initialize('operator', 'local');
    const owner = { reason: '', previewToken: 'secret' }; const binding = new PreparationDraft(drafts, owner, ['reason']);
    await binding.open('player:A'); owner.reason = 'Draft A'; binding.save(); binding.detach();
    owner.reason = ''; await binding.open('player:B'); expect(owner.reason).toBe('');
    binding.detach(); owner.reason = ''; await binding.open('player:A'); expect(owner.reason).toBe('Draft A');
    await drafts.flush(); expect(Object.keys(JSON.parse(api.saveOperatorDraft.calls.first().args[2]))).toEqual(['reason']); drafts.clear();
  });
  it('does not restore an earlier target when its draft load finishes late', async () => {
    const slow = deferred<any>(); const api = { operatorDraft: (key: string) => key.endsWith('A') ? slow.promise : Promise.resolve(ok({ content: JSON.stringify({ reason: JSON.stringify('B') }), version: 'b' })) };
    const service = new OperatorDraftService(api as any); const owner = { reason: '' }; const binding = new PreparationDraft(service, owner, ['reason']);
    const a = binding.open('player:A'); binding.detach(); await binding.open('player:B'); slow.resolve(ok({ content: JSON.stringify({ reason: JSON.stringify('A') }), version: 'a' })); await a;
    expect(owner.reason).toBe('B'); service.clear();
  });
  it('exports context, blanks for unavailable values and spreadsheet-safe text', () => {
    const csv = csvText([{ name: '=HYPERLINK("bad")', measured: 0, unavailable: null }], { day: '2026-09-20' });
    expect(csv).toContain('"day","2026-09-20"'); expect(csv).toContain("'=HYPERLINK"); expect(csv).toContain('"0",""');
  });
});
