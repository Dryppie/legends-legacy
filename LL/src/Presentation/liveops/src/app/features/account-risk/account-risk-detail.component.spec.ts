import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { LiveOpsApiService } from '../../liveops-api.service';
import { AccountRiskDetails, AccountTemporalCorrelationReport } from '../../liveops.models';
import { OperatorContextService } from '../../operator-context.service';
import { AccountRiskDetailComponent } from './account-risk-detail.component';

describe('AccountRiskDetailComponent', () => {
  it('loads temporal correlation separately and explains that it is not ownership proof', async () => {
    const api = {
      accountRiskDetails: jasmine.createSpy().and.resolveTo({ isSuccess: true, data: riskDetails(), errorMessage: '' }),
      accountTemporalCorrelations: jasmine.createSpy().and.resolveTo({ isSuccess: true, data: temporalReport(), errorMessage: '' }),
    };
    await TestBed.configureTestingModule({
      imports: [AccountRiskDetailComponent],
      providers: [
        { provide: LiveOpsApiService, useValue: api },
        { provide: ActivatedRoute, useValue: {
          paramMap: { subscribe: (callback: () => void) => callback() },
          snapshot: { paramMap: convertToParamMap({ accountId: '11111111-1111-1111-1111-111111111111' }) },
        } },
        { provide: Router, useValue: { navigate: jasmine.createSpy().and.resolveTo(true) } },
        { provide: OperatorContextService, useValue: { permissions: { account: 'account' }, hasPermission: () => false } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(AccountRiskDetailComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.accountRiskDetails).toHaveBeenCalled();
    expect(api.accountTemporalCorrelations).toHaveBeenCalledOnceWith('11111111-1111-1111-1111-111111111111');
    expect(fixture.nativeElement.textContent).toContain('Possible account correlation');
    expect(fixture.nativeElement.textContent).toContain('Similar timing does not establish shared ownership');
    expect(fixture.nativeElement.textContent).toContain('Moderate temporal correlation');
    expect(fixture.nativeElement.textContent).toContain('Does not affect risk score or sanctions');
  });
});

function riskDetails(): AccountRiskDetails {
  return {
    account: {
      accountId: '11111111-1111-1111-1111-111111111111',
      characterId: '22222222-2222-2222-2222-222222222222',
      accountLabel: 'subject@example.test', characterName: 'Subject', characterLevel: 50,
      accountCreatedUtc: '2026-01-01T00:00:00Z', lastSessionUtc: '2026-08-22T10:00:00Z',
      score: 50, severity: 'High', primarySignalType: 'OneSidedRelationship', primaryReason: 'Test',
      connectedAccountCount: 1, incomingCinders: 100, outgoingCinders: 0, transferCount: 1,
      firstFlaggedAt: '2026-08-20T00:00:00Z', lastTriggeredAt: '2026-08-21T00:00:00Z',
      evaluatedAt: '2026-08-22T00:00:00Z', evaluationVersion: 9,
      analysisWindowStart: '2026-05-24T00:00:00Z', evidenceComplete: true,
      analyzedTransferCount: 1, investigationStatus: 'Unreviewed',
    },
    signals: [], relationships: [], transfers: [], totalRetainedTransferCount: 0, history: [], notes: [],
  };
}

function temporalReport(): AccountTemporalCorrelationReport {
  return {
    accountId: '11111111-1111-1111-1111-111111111111',
    windowStart: '2026-05-24T00:00:00Z', evaluatedAt: '2026-08-22T00:00:00Z',
    evidenceComplete: true, analyzedTokenCount: 20, analyzedTransferCount: 2, analysisVersion: 1,
    entries: [{
      relatedAccountId: '33333333-3333-3333-3333-333333333333',
      relatedCharacterId: '44444444-4444-4444-4444-444444444444',
      relatedCharacterName: 'Related', assessment: 'Moderate',
      summary: 'Moderate temporal correlation: test evidence.',
      subjectChainStartCount: 10, relatedChainStartCount: 10,
      subjectActiveDays: 8, relatedActiveDays: 8, sharedActiveDays: 5,
      activeDaySimilarity: 0.45, nearStartMatchCount: 5, strongNearStartMatchCount: 3,
      repeatedMatchDays: 4, matchLift: 3.2, hourOfWeekSimilarity: 0.75,
      transferAdjacentMatchCount: 1, firstObservedAt: '2026-08-01T10:00:00Z',
      lastObservedAt: '2026-08-20T10:00:00Z', windowStart: '2026-05-24T00:00:00Z',
      evaluatedAt: '2026-08-22T00:00:00Z', evidenceComplete: true,
      analyzedTokenCount: 20, analyzedTransferCount: 2, analysisVersion: 1,
      matches: [], limitations: ['No device or network evidence is recorded.'],
    }],
  };
}
