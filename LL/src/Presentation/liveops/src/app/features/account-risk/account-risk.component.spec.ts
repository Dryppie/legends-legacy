import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AccountRiskPage } from '../../liveops.models';
import { LiveOpsApiService } from '../../liveops-api.service';
import { AccountRiskListStateService } from './account-risk-list-state.service';
import { AccountRiskComponent } from './account-risk.component';

describe('AccountRiskComponent', () => {
  it('includes low-priority accounts and shows unreviewed accounts by default', async () => {
    const api = {
      accountRisks: jasmine.createSpy().and.resolveTo({ isSuccess: true, data: riskPage(), errorMessage: '' }),
    };
    await TestBed.configureTestingModule({
      imports: [AccountRiskComponent],
      providers: [
        { provide: LiveOpsApiService, useValue: api },
        { provide: Router, useValue: { navigate: jasmine.createSpy().and.resolveTo(true) } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(AccountRiskComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.accountRisks).toHaveBeenCalledOnceWith(jasmine.objectContaining({
      minimumSeverity: 'Low',
      status: 'Unreviewed',
    }), 1);
    expect(fixture.componentInstance.minimumSeverity).toBe('Low');
    expect(fixture.componentInstance.status).toBe('Unreviewed');
  });

  it('restores the review queue without requesting it again after opening an account', async () => {
    const api = {
      accountRisks: jasmine.createSpy().and.resolveTo({ isSuccess: true, data: riskPage(), errorMessage: '' }),
    };
    const router = { navigate: jasmine.createSpy().and.resolveTo(true) };
    await TestBed.configureTestingModule({
      imports: [AccountRiskComponent],
      providers: [
        { provide: LiveOpsApiService, useValue: api },
        { provide: Router, useValue: router },
      ],
    }).compileComponents();

    const initialFixture = TestBed.createComponent(AccountRiskComponent);
    initialFixture.detectChanges();
    await initialFixture.whenStable();
    initialFixture.componentInstance.search = 'Lulz2';
    initialFixture.componentInstance.open('11111111-1111-1111-1111-111111111111');
    initialFixture.destroy();

    const restoredFixture = TestBed.createComponent(AccountRiskComponent);
    restoredFixture.detectChanges();
    await restoredFixture.whenStable();

    expect(router.navigate).toHaveBeenCalledOnceWith(['/account-risk', '11111111-1111-1111-1111-111111111111']);
    expect(api.accountRisks).toHaveBeenCalledTimes(1);
    expect(restoredFixture.componentInstance.search).toBe('Lulz2');
    expect(restoredFixture.componentInstance.data?.entries[0].characterName).toBe('Lulz2');
  });

  it('removes a reviewed account from a cached unreviewed queue', () => {
    const listState = new AccountRiskListStateService();
    listState.save({
      data: riskPage(), search: '', minimumSeverity: 'Low', signalType: '', status: 'Unreviewed',
      minimumScore: '', maximumAccountAgeDays: '', sort: 'risk', page: 1,
    });

    listState.updateInvestigationStatus('11111111-1111-1111-1111-111111111111', 'Investigating');

    const restored = listState.restore();
    expect(restored?.data.entries).toEqual([]);
    expect(restored?.data.total).toBe(0);
  });
});

function riskPage(): AccountRiskPage {
  return {
    entries: [{
      accountId: '11111111-1111-1111-1111-111111111111',
      characterId: '22222222-2222-2222-2222-222222222222',
      accountLabel: 'account@example.test',
      characterName: 'Lulz2',
      characterLevel: 58,
      accountCreatedUtc: '2026-08-10T10:00:00Z',
      lastSessionUtc: '2026-08-20T10:00:00Z',
      score: 55,
      severity: 'High',
      primarySignalType: 'IncomingItemFunnel',
      primaryReason: 'Test evidence',
      connectedAccountCount: 20,
      incomingCinders: 0,
      outgoingCinders: 0,
      transferCount: 300,
      firstFlaggedAt: '2026-08-20T10:00:00Z',
      lastTriggeredAt: '2026-08-20T10:00:00Z',
      evaluatedAt: '2026-08-20T10:00:00Z',
      evaluationVersion: 9,
      analysisWindowStart: '2026-05-22T10:00:00Z',
      evidenceComplete: true,
      analyzedTransferCount: 300,
      investigationStatus: 'Unreviewed',
    }],
    total: 1,
    counts: { High: 1 },
    lastEvaluatedAt: '2026-08-20T10:00:00Z',
    firstEvidenceAt: '2026-08-11T10:00:00Z',
    directTransferCount: 1,
    directItemTransferCount: 1,
    evaluatedAccountCount: 1,
    eligibleAccountCount: 1,
    upToDateAccountCount: 1,
    pendingEvaluationCount: 0,
    incompleteEvaluationCount: 0,
    evaluationVersion: 9,
    lookbackDays: 90,
    page: 1,
    pageSize: 50,
  };
}
