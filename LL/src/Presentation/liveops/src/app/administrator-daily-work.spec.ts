import { HttpErrorResponse } from '@angular/common/http';
import { OperatorDraftService } from './operator-draft.service';
import { WorkspaceStateService } from './workspace-state.service';
import { GlobalSearchComponent } from './shared/global-search.component';
import { WorkQueueComponent } from './features/dashboard/work-queue.component';
import { OperatorContextService } from './operator-context.service';
import { OperationJournalService } from './operation-journal.service';
import { operationSummary } from './shared/admin-presentation';
import { AdministrationAuditEntry, OperatorDraft } from './liveops.models';

const emptyVersion = '00000000-0000-0000-0000-000000000000';
const ok = <T>(data: T) => ({ isSuccess: true, data, errorMessage: '' });
function deferred<T>() { let resolve!: (value: T) => void; const promise = new Promise<T>(r => resolve = r); return { promise, resolve }; }
function operator() { const result = new OperatorContextService(); result.session = { subject: 'operator', environment: 'Test', displayName: 'Operator', isDevelopmentOperator: true, permissions: ['liveops.superadmin'] }; return result; }
function storageApi() {
  const values = new Map<string, OperatorDraft>();
  return {
    values,
    operatorDraft: async (key: string) => ok(values.get(key) ?? { key, content: '{}', version: emptyVersion, updatedAt: '' }),
    saveOperatorDraft: async (key: string, version: string, content: string) => {
      if ((values.get(key)?.version ?? emptyVersion) !== version) throw new HttpErrorResponse({ status: 409 });
      const value = { key, version: crypto.randomUUID(), content, updatedAt: new Date().toISOString() }; values.set(key, value); return ok(value);
    },
  };
}

describe('Durable private drafts', () => {
  it('restores notes and personal filters into a new browser session without local text storage', async () => {
    const api = storageApi(), first = new OperatorDraftService(api as never); first.initialize('operator', 'Test');
    const note = await first.open('case:one'); first.update(note, { note: 'Private evidence', resolution: 'Follow up' });
    const state = new WorkspaceStateService(); await state.initialize(first);
    state.playerSearch = { query: 'player@example.test', results: [], message: '', searched: false };
    state.caseFilters = { search: 'missing item', status: 'Waiting' };
    expect(await first.flush()).toBeTrue(); first.clear();
    const second = new OperatorDraftService(api as never); second.initialize('operator', 'Test');
    expect((await second.open('case:one')).data['note']).toBe('Private evidence');
    const restored = new WorkspaceStateService(); await restored.initialize(second);
    expect(restored.playerSearch.query).toBe('player@example.test'); expect(restored.caseFilters['status']).toBe('Waiting'); second.clear();
  });

  it('serializes autosaves and preserves text entered while the previous save is pending', async () => {
    const api = storageApi(), waiting = deferred<ReturnType<typeof ok<OperatorDraft>>>();
    const save = jasmine.createSpy().and.returnValue(waiting.promise);
    const drafts = new OperatorDraftService({ ...api, saveOperatorDraft: save } as never);
    const handle = await drafts.open('case:one'); drafts.update(handle, { note: 'First' }); const saving = drafts.save(handle);
    drafts.update(handle, { note: 'Second' }); save.and.callFake(api.saveOperatorDraft);
    waiting.resolve(ok({ key: handle.key, version: emptyVersion, content: '{"note":"First"}', updatedAt: '' })); await saving;
    expect(save).toHaveBeenCalledTimes(2); expect(JSON.parse(api.values.get(handle.key)!.content).note).toBe('Second'); expect(handle.dirty).toBeFalse(); drafts.clear();
  });

  it('does not overwrite another tab and retains a conflicting local draft for copying', async () => {
    const api = storageApi(), a = new OperatorDraftService(api as never), b = new OperatorDraftService(api as never);
    const first = await a.open('case:one'), second = await b.open('case:one');
    a.update(first, { note: 'First tab' }); await a.save(first);
    b.update(second, { note: 'Second tab' }); await b.save(second);
    expect(second.conflict).toBeTrue(); expect(second.data['note']).toBe('Second tab'); expect(await b.flush()).toBeFalse();
    expect(JSON.parse(api.values.get('case:one')!.content).note).toBe('First tab');
    await b.reload(second); expect(second.data['note']).toBe('First tab'); expect(second.conflict).toBeFalse(); a.clear(); b.clear();
  });

  it('cannot overwrite an unreadable server draft with empty defaults', async () => {
    const save = jasmine.createSpy();
    const drafts = new OperatorDraftService({ operatorDraft: async () => { throw new Error('Unavailable'); }, saveOperatorDraft: save } as never);
    const handle = await drafts.open('workspace'); drafts.update(handle, { playerQuery: 'New query' });
    expect(await drafts.flush()).toBeFalse(); expect(save).not.toHaveBeenCalled(); expect(handle.error).toBe('Unavailable'); drafts.clear();
  });

  it('does not restore the previous operator response after switching identities', async () => {
    const pending = deferred<ReturnType<typeof ok<OperatorDraft>>>();
    const api = storageApi(), drafts = new OperatorDraftService({ ...api, operatorDraft: () => pending.promise } as never);
    drafts.initialize('alice', 'Test'); const loading = drafts.open('workspace'); drafts.initialize('bob', 'Test');
    pending.resolve(ok({ key: 'workspace', version: emptyVersion, content: '{"playerQuery":"Alice private search"}', updatedAt: '' }));
    expect((await loading).loaded).toBeFalse(); drafts.clear();
  });
});

describe('Administrator work discovery', () => {
  it('keeps successful queues when an independent source fails and honours status links', async () => {
    const api = { cases: async (filters: Record<string, string>) => filters['status'] === 'Waiting' ? Promise.reject(new Error('Cases offline')) : ok({ cases: [], total: 0 }), accountRisks: async () => ok({ entries: [], total: 3 }) };
    const component = new WorkQueueComponent(api as never, operator()); component.ngOnInit();
    await Promise.resolve(); await Promise.resolve(); await Promise.resolve();
    expect(component.groups.find(x => x.status === 'Waiting')?.error).toContain('Cases offline');
    expect(component.groups.find(x => x.status === 'Open')?.total).toBe(0);
    expect(component.groups.find(x => x.status === 'Investigating')?.total).toBe(3); component.ngOnDestroy();
  });

  it('ignores old search responses after editing the global query', async () => {
    const pending = deferred<any>();
    const component = new GlobalSearchComponent({ searchPlayers: () => pending.promise, cases: async () => ok({ cases: [] }) } as never, operator());
    component.query = 'Alice'; const search = component.search(); component.query = 'Bob'; component.invalidate();
    pending.resolve(ok([{ characterId: 'a', characterName: 'Alice' }])); await search;
    expect(component.results).toEqual([]); expect(component.searched).toBeFalse();
  });

  it('finds exact operation references and reports incomplete Chat coverage', async () => {
    const id = '22222222-2222-2222-2222-222222222222';
    const component = new GlobalSearchComponent({ searchPlayers: async () => ok([]), cases: async () => ok({ cases: [] }), audit: async () => ok({ entries: [{ operationId: id, source: 'Game', actionType: 'SupportCaseCreated', detailsJson: '{}', occurredAt: '2026-09-28T12:00:00Z' }], unavailableSources: ['Chat'] }) } as never, operator());
    component.query = id; await component.search();
    expect(component.results[0].kind).toBe('Operation'); expect(component.results[0].params?.['operationId']).toBe(id); expect(component.warnings[0]).toContain('Chat');
  });
});

describe('Operation recovery and support summaries', () => {
  it('recovers only a matching completed receipt and never treats absence or another actor as failure', async () => {
    const journal = new OperationJournalService(); journal.record('found', 'player', 'Grant', 'Unknown', 'grant', 'Game'); journal.record('absent', 'player', 'Grant', 'Unknown', 'grant', 'Game'); journal.record('other', 'player', 'Grant', 'Unknown', 'grant', 'Game');
    await journal.reconcile({ audit: async (filters: any) => ok({ entries: filters.operationId === 'absent' ? [] : [{ operationId: filters.operationId, actorSubject: filters.operationId === 'other' ? 'someone-else' : 'operator', source: 'Game' }], unavailableSources: [] }) } as never, 'operator');
    expect(journal.receipts.find(x => x.operationId === 'found')?.outcome).toBe('Completed'); expect(journal.unresolved.map(x => x.operationId).sort()).toEqual(['absent', 'other']);
  });
  it('copies a useful receipt without private notes or technical JSON', () => {
    const text = operationSummary({ actionType: 'CompensationItemsGranted', detailsJson: '{"quantity":2,"itemName":"Potion","secret":"technical-only"}', reason: 'Case replacement', operationId: 'operation', source: 'Game', outcome: 'Completed', internalNotes: 'Private investigation', occurredAt: '2026-09-28T12:00:00Z' } as AdministrationAuditEntry, 'Test');
    expect(text).toContain('2 × Potion'); expect(text).toContain('Case replacement'); expect(text).not.toContain('Private investigation'); expect(text).not.toContain('technical-only');
  });
});
