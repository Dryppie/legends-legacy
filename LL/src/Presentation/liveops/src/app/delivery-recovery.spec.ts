import { HttpErrorResponse } from '@angular/common/http';
import { convertToParamMap } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { DeliveryRecoveryComponent } from './features/operations/delivery-recovery.component';
import { OperationsComponent } from './features/operations/operations.component';
import { ActionPreview, ApiResponse, ServerOperation, ServerOperationStatus } from './liveops.models';
import { OperatorContextService } from './operator-context.service';
import { OperationJournalService } from './operation-journal.service';

const ok = <T>(data: T): ApiResponse<T> => ({ isSuccess: true, data, errorMessage: '' });
function deferred<T>() { let resolve!: (value: T) => void; const promise = new Promise<T>(r => resolve = r); return { promise, resolve }; }
function operator(permission = 'liveops.superadmin') { const op = new OperatorContextService(); op.session = { subject: 'test', displayName: 'Test', environment: 'Test', isDevelopmentOperator: true, permissions: [permission] }; return op; }
function preview(id: string): ActionPreview { return { previewToken: 'token', operationId: id, actionKind: 'StateRefreshRecovery', title: 'Retry player state refresh', targetName: 'Player A', targetId: 'delivery-A', riskLevel: 'Normal', expiresAt: new Date(Date.now() + 60000).toISOString(), confirmationText: null, fields: [], warnings: [] }; }

describe('Bounded delivery recovery', () => {
  it('reuses the uncertain reference after a lost response and submits the reviewed reason', async () => {
    const calls: any[] = [];
    const api = { previewStateRefresh: async (_: string, body: any) => { calls.push(body); return ok(preview(body.operationId)); }, retryStateRefresh: jasmine.createSpy().and.rejectWith(new HttpErrorResponse({ status: 0 })) };
    const journal = new OperationJournalService(); const component = new DeliveryRecoveryComponent(api as never, {} as never, operator(), journal);
    component.deliveryId = 'delivery-A'; component.reason = 'Reviewed case reason'; await component.review(); const id = component.operationId;
    component.reason = 'Later form text'; await component.submit();
    expect(api.retryStateRefresh.calls.mostRecent().args[1].reason).toBe('Reviewed case reason'); expect(journal.unresolved[0].operationId).toBe(id);
    component.closePreview(); component.reason = 'Reviewed case reason'; await component.review(); expect(calls[1].operationId).toBe(id);
    const restored = new DeliveryRecoveryComponent(api as never, {} as never, operator(), journal); restored.deliveryId = 'delivery-A'; restored.reason = 'Reviewed case reason';
    await restored.review(); expect(restored.operationId).toBe(id);
  });

  it('does not prepare without permission or while recovery loading is incomplete', async () => {
    const api = { previewStateRefresh: jasmine.createSpy() }, journal = new OperationJournalService(), op = operator('liveops.read');
    const component = new DeliveryRecoveryComponent(api as never, {} as never, op, journal); component.deliveryId = 'delivery-A'; component.reason = 'Valid reason';
    await component.review(); expect(api.previewStateRefresh).not.toHaveBeenCalled();
    op.session!.permissions = ['liveops.superadmin']; journal.recoveryIncomplete = true; journal.recoveryMessage = 'Recovery unavailable';
    await component.review(); expect(component.message).toBe('Recovery unavailable'); expect(api.previewStateRefresh).not.toHaveBeenCalled();
  });

  it('ignores a late preview after switching delivery targets', async () => {
    const pending = deferred<ApiResponse<ActionPreview>>(), params = new BehaviorSubject(convertToParamMap({deliveryId:'delivery-A'}));
    const component = new DeliveryRecoveryComponent({ previewStateRefresh: () => pending.promise } as never, {paramMap:params} as never, operator(), new OperationJournalService());
    component.ngOnInit(); component.reason = 'A reason'; const review = component.review();
    params.next(convertToParamMap({deliveryId:'delivery-B'})); pending.resolve(ok(preview('A-operation'))); await review;
    expect(component.deliveryId).toBe('delivery-B'); expect(component.preview).toBeNull(); expect(component.reason).toBe(''); expect(component.loading).toBeFalse(); component.ngOnDestroy();
  });

  it('records a successful queue separately from downstream delivery', async () => {
    const journal = new OperationJournalService(), api = { previewStateRefresh: async (_: string, body: any) => ok(preview(body.operationId)),
      retryStateRefresh: async (_: string, body: any) => ok({ operationId:body.operationId, deliveryId:'delivery-A', characterId:'A', replacementMessageId:'replacement', wasAlreadyProcessed:false }) };
    const component = new DeliveryRecoveryComponent(api as never, {} as never, operator(), journal); component.deliveryId = 'delivery-A'; component.reason = 'Reviewed case';
    await component.review(); await component.submit(); expect(component.result?.replacementMessageId).toBe('replacement');
    expect(journal.receipts[0].outcome).toBe('Completed'); expect(component.preview).toBeNull(); expect(component.reason).toBe('');
  });

  it('opens a linked operation directly even if it is outside the first register page', async () => {
    const row: ServerOperation = { operationId:'older', targetId:'delivery-A', targetKind:'delivery', kind:'delivery-retry', source:'Game', outcome:'Committed', attempts:1, receivedAt:'2026-09-29', updatedAt:'2026-09-29' };
    const status: ServerOperationStatus = {operation:row,delivery:null,coverage:'Consumer processing does not prove player receipt.'};
    const api = { operations: async () => ok({entries:[],total:30,page:1,pageSize:25}), operationStatus:jasmine.createSpy().and.resolveTo(ok(status)) };
    const component = new OperationsComponent(api as never, new OperationJournalService()); await component.load('older');
    expect(api.operationStatus).toHaveBeenCalledWith('older'); expect(component.detail).toEqual(status); component.ngOnDestroy();
  });
});
