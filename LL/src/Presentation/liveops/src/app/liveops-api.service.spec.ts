import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { LiveOpsApiService } from './liveops-api.service';

describe('LiveOpsApiService', () => {
  let service: LiveOpsApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [LiveOpsApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(LiveOpsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('uses the antiforgery token for mutations', async () => {
    const tokenPromise = service.initializeAntiforgery();
    http.expectOne('/auth/antiforgery').flush({ requestToken: 'xsrf-token' });
    await tokenPromise;

    const mutation = service.mute('character-1', {
      operationId: 'operation-1',
      reason: 'case-42',
    });
    const request = http.expectOne('/api/liveops/chat/characters/character-1/mutes');
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('xsrf-token');
    request.flush({ isSuccess: true, data: null, errorMessage: '' });
    await mutation;
  });

  it('uses antiforgery and the dedicated endpoint for server previews', async () => {
    const tokenPromise = service.initializeAntiforgery();
    http.expectOne('/auth/antiforgery').flush({ requestToken: 'xsrf-token' });
    await tokenPromise;

    const preview = service.previewBan('account-1', {
      operationId: 'operation-1',
      reason: 'CASE-42',
    });
    const request = http.expectOne('/api/liveops/accounts/account-1/bans/preview');
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('xsrf-token');
    request.flush({ isSuccess: true, data: null, errorMessage: '' });
    await preview;
  });

  it('sends global audit filters and cursor', async () => {
    const promise = service.audit(
      {
        source: 'Chat',
        actor: 'operator@example.com',
        permission: 'liveops.chat.moderate',
        reference: 'CASE-42',
        riskLevel: 'Permanent',
        target: 'ArdentFox',
      },
      'next-page',
      25,
    );
    const request = http.expectOne((candidate) =>
      candidate.url === '/api/liveops/audit' &&
      candidate.params.get('source') === 'Chat' &&
      candidate.params.get('actor') === 'operator@example.com' &&
      candidate.params.get('permission') === 'liveops.chat.moderate' &&
      candidate.params.get('reference') === 'CASE-42' &&
      candidate.params.get('riskLevel') === 'Permanent' &&
      candidate.params.get('target') === 'ArdentFox' &&
      candidate.params.get('cursor') === 'next-page' &&
      candidate.params.get('take') === '25');
    expect(request.request.method).toBe('GET');
    request.flush({
      isSuccess: true,
      data: { entries: [], nextCursor: null, unavailableSources: [] },
      errorMessage: '',
    });

    await promise;
  });

  it('loads the authorized operational status summary', async () => {
    const promise = service.operationalStatus();
    const request = http.expectOne('/api/liveops/status');
    expect(request.request.method).toBe('GET');
    request.flush({ isSuccess: true, data: null, errorMessage: '' });
    await promise;
  });

  it('loads the read-only player support snapshot', async () => {
    const promise = service.playerSupportSnapshot('character-1');
    const request = http.expectOne(
      '/api/liveops/players/character-1/support-snapshot',
    );
    expect(request.request.method).toBe('GET');
    request.flush({ isSuccess: true, data: null, errorMessage: '' });
    await promise;
  });

  it('loads the next bounded page of player transfer history', async () => {
    const promise = service.playerTransferHistory('character-1', 'next-page', 25);
    const request = http.expectOne((candidate) =>
      candidate.url === '/api/liveops/players/character-1/transfers' &&
      candidate.params.get('cursor') === 'next-page' &&
      candidate.params.get('take') === '25');
    expect(request.request.method).toBe('GET');
    request.flush({ isSuccess: true, data: null, errorMessage: '' });
    await promise;
  });

  it('loads a bounded transfer conversation page', async () => {
    const promise = service.playerTransferConversation(
      'character-1',
      'transfer-1',
      'next-page',
      25,
    );
    const request = http.expectOne((candidate) =>
      candidate.url === '/api/liveops/players/character-1/transfers/transfer-1/conversation' &&
      candidate.params.get('cursor') === 'next-page' &&
      candidate.params.get('take') === '25');
    expect(request.request.method).toBe('GET');
    request.flush({ isSuccess: true, data: null, errorMessage: '' });
    await promise;
  });

  it('loads a bounded cross-channel player-message page', async () => {
    const promise = service.playerMessageHistory('character-1', 'next-page', 25);
    const request = http.expectOne((candidate) =>
      candidate.url === '/api/liveops/players/character-1/messages' &&
      candidate.params.get('cursor') === 'next-page' &&
      candidate.params.get('take') === '25');
    expect(request.request.method).toBe('GET');
    request.flush({
      isSuccess: true,
      data: { entries: [], nextCursor: null },
      errorMessage: '',
    });
    await promise;
  });

  it('sends account-risk investigation filters and pagination', async () => {
    const promise = service.accountRisks({
      minimumSeverity: 'High',
      signalType: 'FeederNetwork',
      status: 'Unreviewed',
      sort: 'connected',
    }, 2, 50);
    const request = http.expectOne((candidate) =>
      candidate.url === '/api/liveops/account-risk' &&
      candidate.params.get('minimumSeverity') === 'High' &&
      candidate.params.get('signalType') === 'FeederNetwork' &&
      candidate.params.get('status') === 'Unreviewed' &&
      candidate.params.get('sort') === 'connected' &&
      candidate.params.get('page') === '2' &&
      candidate.params.get('pageSize') === '50');
    expect(request.request.method).toBe('GET');
    request.flush({ isSuccess: true, data: null, errorMessage: '' });
    await promise;
  });

  it('loads bounded temporal correlations independently from risk details', async () => {
    const promise = service.accountTemporalCorrelations('account-1', 60);
    const request = http.expectOne((candidate) =>
      candidate.url === '/api/liveops/account-risk/account-1/temporal-correlations' &&
      candidate.params.get('windowDays') === '60');
    expect(request.request.method).toBe('GET');
    request.flush({ isSuccess: true, data: null, errorMessage: '' });
    await promise;
  });

  it('loads transfer and conversation correlations independently from risk details', async () => {
    const promise = service.accountTransferConversationCorrelations('account-1');
    const request = http.expectOne(
      '/api/liveops/account-risk/account-1/transfer-conversation-correlations',
    );
    expect(request.request.method).toBe('GET');
    request.flush({ isSuccess: true, data: null, errorMessage: '' });
    await promise;
  });

  it('uses antiforgery for investigation workflow mutations', async () => {
    const tokenPromise = service.initializeAntiforgery();
    http.expectOne('/auth/antiforgery').flush({ requestToken: 'xsrf-token' });
    await tokenPromise;

    const promise = service.updateAccountRiskStatus('account-1', {
      operationId: 'operation-1', status: 'Investigating', reason: 'CASE-42',
    });
    const request = http.expectOne('/api/liveops/account-risk/account-1/status');
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('xsrf-token');
    request.flush({ isSuccess: true, data: null, errorMessage: '' });
    await promise;
  });

  it('exports an authorized bounded audit query with antiforgery', async () => {
    const tokenPromise = service.initializeAntiforgery();
    http.expectOne('/auth/antiforgery').flush({ requestToken: 'xsrf-token' });
    await tokenPromise;

    const promise = service.exportAudit(
      { source: 'Game', reference: 'CASE-42', riskLevel: 'HighValue' },
      '2026-08-01T00:00:00.000Z',
      '2026-08-02T00:00:00.000Z',
      'operation-1',
    );
    const request = http.expectOne('/api/liveops/audit/exports');
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('xsrf-token');
    expect(request.request.body).toEqual(jasmine.objectContaining({
      operationId: 'operation-1',
      source: 'Game',
      reference: 'CASE-42',
      riskLevel: 'HighValue',
    }));
    request.flush(new Blob(['csv']), {
      headers: { 'Content-Disposition': 'attachment; filename="audit.csv"' },
    });

    expect((await promise).fileName).toBe('audit.csv');
  });
});
