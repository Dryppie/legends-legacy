import { LiveOpsApiService } from './liveops-api.service';
import { ApiResponse, JobScheduleHealth, ServerOperation, ServerOperationPage, ServerOperationStatus } from './liveops.models';
import { OperationJournalService } from './operation-journal.service';
import { OperationsComponent } from './features/operations/operations.component';
import { JobSchedulesComponent } from './features/dashboard/job-schedules.component';

const ok = <T>(data: T): ApiResponse<T> => ({ isSuccess: true, data, errorMessage: '' });
const operation = (id: string): ServerOperation => ({ operationId: id, targetId: 'player-' + id,
  targetKind: 'character', kind: 'grant', source: 'Game', outcome: 'Unknown', attempts: 1,
  receivedAt: '2026-09-28T12:00:00Z', updatedAt: '2026-09-28T12:00:00Z' });
function deferred<T>() { let resolve!: (value: T) => void; const promise = new Promise<T>(r => resolve = r); return { promise, resolve }; }
function journal() { const value = new OperationJournalService(); value.initialize(crypto.randomUUID(), 'Test'); return value; }

describe('Durable operation recovery', () => {
  it('restores every server page in a new browser journal and keeps existing local references', async () => {
    const value = journal(); value.record('local', 'player', 'Grant', 'Unknown');
    const api = { operations: jasmine.createSpy().and.callFake(async (page: number) => ok({
      entries: page === 1 ? Array.from({ length: 25 }, (_, i) => operation(String(i))) : [operation('25')],
      page, total: 26, pageSize: 25 })) };
    await value.restoreServer(api as never);
    expect(value.recoveryIncomplete).toBeFalse(); expect(value.unresolved.length).toBe(27);
    expect(api.operations.calls.allArgs()).toEqual([[1, true], [2, true]]);
    expect(value.unresolved.find(x => x.operationId === '25')).toEqual(jasmine.objectContaining({ kind: 'grant', source: 'Game' }));
  });

  it('retains references and blocks new preparation when a later server page is unavailable', async () => {
    const value = journal();
    await value.restoreServer({ operations: async (page: number) => {
      if (page === 2) throw new Error('Unavailable');
      return ok({ entries: [operation('first')], total: 26, pageSize: 25, page });
    } } as LiveOpsApiService);
    expect(value.recoveryIncomplete).toBeTrue(); expect(value.recoveryMessage).toContain('unavailable');
    expect(value.unresolved[0].operationId).toBe('first');
    await value.restoreServer({ operations: async () => ok({ entries: [], page: 1, pageSize: 25, total: 0 }) } as never);
    expect(value.recoveryIncomplete).toBeFalse(); expect(value.unresolved[0].operationId).toBe('first');
  });

  it('never imports a late response from the previous operator or environment', async () => {
    const value = journal(), response = deferred<ApiResponse<ServerOperationPage>>();
    const pending = value.restoreServer({ operations: () => response.promise } as never);
    value.initialize(crypto.randomUUID(), 'Other environment');
    response.resolve(ok({ entries: [operation('other-operator')], total: 1, pageSize: 25, page: 1 })); await pending;
    expect(value.receipts).toEqual([]); expect(value.recoveryIncomplete).toBeFalse();
  });

  it('does not downgrade a completed local receipt while restoring an older unknown server record', async () => {
    const value = journal(); value.record('completed', 'player', 'Grant', 'Completed');
    await value.restoreServer({ operations: async () => ok({ entries: [operation('completed')], total: 1, pageSize: 25, page: 1 }) } as never);
    expect(value.receipts[0].outcome).toBe('Completed'); expect(value.unresolved).toEqual([]);
  });

  it('marks an oversized recovery scan incomplete rather than silently dropping unknown references', async () => {
    const value = journal();
    await value.restoreServer({ operations: async (page: number) => ok({ entries: [operation(String(page))], total: 1001, pageSize: 25, page }) } as never);
    expect(value.recoveryIncomplete).toBeTrue(); expect(value.recoveryMessage).toContain('1,000'); expect(value.unresolved.length).toBe(40);
  });

  it('accepts a definitive server rejection while preserving uncertainty after an ordinary rejected retry', async () => {
    const value = journal(); value.record('rejected', 'player', 'Grant', 'Unknown', 'grant', 'Game');
    value.record('rejected', 'player', 'Grant', 'Rejected'); expect(value.unresolved.length).toBe(1);
    const audit = jasmine.createSpy();
    await value.reconcile({ operationStatus: async () => ok({ operation: { ...operation('rejected'), outcome: 'Rejected' },
      delivery: null, coverage: 'The first attempt was rejected' }), audit } as never, 'operator');
    expect(value.unresolved).toEqual([]); expect(value.receipts[0].outcome).toBe('Rejected'); expect(audit).not.toHaveBeenCalled();
  });

  it('updates local recovery only from the selected receipt and ignores stale details after changing views', async () => {
    const value = journal(), response = deferred<ApiResponse<ServerOperationStatus>>(), row = operation('one');
    value.record(row.operationId, row.targetId, 'Grant', 'Unknown', 'grant', 'Game');
    const api = { operationStatus: () => response.promise, operations: async () => ok({ entries: [], total: 0, page: 1, pageSize: 25 }) };
    const component = new OperationsComponent(api as never, value);
    const check = component.inspect(row); await component.load();
    response.resolve(ok({ operation: { ...row, outcome: 'Committed' }, delivery: null, coverage: 'Retained receipt' })); await check;
    expect(component.detail).toBeNull(); expect(value.unresolved.length).toBe(1);
    await component.inspect(row); expect(value.unresolved).toEqual([]); expect(component.detail?.operation.outcome).toBe('Committed');
    component.ngOnDestroy();
  });
});

describe('Actual worker schedule evidence', () => {
  it('keeps the previous check explicitly stale when the scheduler becomes unavailable', async () => {
    const row: JobScheduleHealth = { jobName: 'daily', state: 'Scheduled', message: 'Registered', schedule: 'Daily UTC',
      nextFireAt: null, lastStartedAt: null, lastCompletedAt: null, workerCheckInAt: null };
    const api = { jobSchedules: jasmine.createSpy().and.resolveTo(ok([row])) };
    const component = new JobSchedulesComponent(api as never); await component.load(); const checked = component.checkedAt;
    api.jobSchedules.and.rejectWith(new Error('Unavailable')); await component.load();
    expect(component.rows).toEqual([row]); expect(component.checkedAt).toBe(checked);
    expect(component.error).toContain('No healthy or missing-job status can be inferred'); expect(component.error).toContain('previous check');
  });
});
