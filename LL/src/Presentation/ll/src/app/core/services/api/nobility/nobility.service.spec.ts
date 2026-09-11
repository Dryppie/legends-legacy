import { signal } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { NobilityService, NobilityStatus } from './nobility.service';
import { ApiService } from '../api.service';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { TimeSyncService } from '../time-sync/time-sync.service';

describe('NobilityService', () => {
  let service: NobilityService;
  let api: jasmine.SpyObj<ApiService>;
  let logout: ReturnType<typeof signal<number>>;
  const snapshot = (overrides: Partial<NobilityStatus> = {}): NobilityStatus => ({
    isNoble: false, serverTime: new Date().toISOString(), expiresAt: null,
    membershipVersion: 'version', availableSignets: 2, listedSignets: 0, hasSupportHistory: false,
    showBadge: true, dailyRewardsThrough: null,
    benefits: { offlineHours: 24, essenceLoadouts: 3, equipmentLoadouts: 3, arenaTicketCap: 5,
      focusCooldownHours: 8, marketSellLimit: 10, marketBuyLimit: 10, freeProphecyRerolls: 1,
      totalProphecyRerolls: 3, experienceBonusBps: 0 }, ...overrides,
  });
  beforeEach(() => {
    logout = signal(0);
    api = jasmine.createSpyObj<ApiService>('ApiService', ['get', 'post', 'put']);
    api.get.and.returnValue(of(snapshot()));
    TestBed.configureTestingModule({ providers: [
      { provide: ApiService, useValue: api },
      { provide: EventBusService, useValue: { logout } },
      { provide: StateSyncCoordinator, useValue: { register: () => undefined,
        latestRevision: () => 1, acceptMutationResponse: jasmine.createSpy('refreshGameplay') } },
      { provide: TimeSyncService, useValue: { now: () => Date.now(), updateFromServerTime: () => undefined } },
    ] });
    service = TestBed.inject(NobilityService); TestBed.flushEffects(); service.hydrate(snapshot());
  });

  it('uses the exact preview units and keeps the operation ID after an uncertain response', () => {
    const expiresAt = '2027-01-31T12:00:00Z';
    api.post.and.callFake(path => path.endsWith('redemption-preview')
      ? of({ membershipVersion: 'version', unitIds: ['one', 'two'], expiresAt, expiryIsEstimate: true })
      : throwError(() => ({ status: 0, message: 'Connection interrupted' })));
    service.redeem(2);
    const original = api.post.calls.mostRecent().args[1] as any;
    service.hydrate(snapshot({ availableSignets: 0 }));
    service.redeem(1);
    expect(api.post.calls.mostRecent().args[1]).toEqual(original);
    expect(api.post.calls.allArgs().filter(args => args[0].endsWith('redemption-preview')).length).toBe(1);
    expect(original.unitIds).toEqual(['one', 'two']);
    expect(original.expectedExpiryDate).toBe('2027-01-31');
  });

  it('discards a stale preview after a conflict', () => {
    api.post.and.callFake(path => path.endsWith('redemption-preview')
      ? of({ membershipVersion: 'v', unitIds: ['one'], expiresAt: '2027-01-31T00:00:00Z', expiryIsEstimate: true })
      : throwError(() => ({ status: 409, error: { errorMessage: 'Preview expired' } })));
    service.redeem(1);
    expect(service.pendingQuantity()).toBeNull();
    expect(service.error()).toBe('Preview expired');
  });

  it('ignores a response from the previous logged-in session', () => {
    const pending = new Subject<unknown>();
    api.post.and.returnValue(pending);
    service.redeem(1);
    logout.set(1); TestBed.flushEffects();
    pending.next({ unitIds: ['old-session'] }); pending.complete();
    expect(service.pendingQuantity()).toBeNull();
    expect(service.status()).toBeNull();
    expect(api.post.calls.count()).toBe(1);
  });

  it('expires local privileges even when reconciliation fails', fakeAsync(() => {
    service.hydrate(snapshot({ isNoble: true, expiresAt: new Date(Date.now() + 1000).toISOString() }));
    expect(service.equipmentLimit()).toBe(6);
    api.post.and.returnValue(throwError(() => new Error('Offline')));
    tick(1001);
    expect(service.isNoble()).toBeFalse();
    expect(service.equipmentLimit()).toBe(3);
    expect(TestBed.inject(StateSyncCoordinator).acceptMutationResponse).toHaveBeenCalledWith({
      essences: 1, equipment: 1, colosseum: 1, prophecies: 1,
    });
  }));
});

describe('NobilityService HTTP response contract', () => {
  let service: NobilityService;
  let http: HttpTestingController;
  let api: ApiService;
  const preview = {
    membershipVersion: '00000000-0000-0000-0000-000000000000',
    unitIds: ['4b34aee8-6f7b-4ea8-ef5b-1f3f63281cd9'],
    expiresAt: '2027-10-11T19:26:28.153502+00:00',
    expiryIsEstimate: true,
  };
  const snapshot = (): NobilityStatus => ({
    isNoble: false, serverTime: new Date().toISOString(), expiresAt: null,
    membershipVersion: preview.membershipVersion, availableSignets: 1, listedSignets: 0,
    hasSupportHistory: false, showBadge: true, dailyRewardsThrough: null,
    benefits: { offlineHours: 24, essenceLoadouts: 3, equipmentLoadouts: 3, arenaTicketCap: 5,
      focusCooldownHours: 8, marketSellLimit: 10, marketBuyLimit: 10, freeProphecyRerolls: 1,
      totalProphecyRerolls: 3, experienceBonusBps: 0 },
  });

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [
      provideHttpClient(), provideHttpClientTesting(),
      { provide: EventBusService, useValue: { logout: signal(0) } },
      { provide: StateSyncCoordinator, useValue: { register: () => undefined,
        latestRevision: () => 1, acceptMutationResponse: () => undefined } },
      { provide: TimeSyncService, useValue: { now: () => Date.now(), updateFromServerTime: () => undefined } },
    ] });
    service = TestBed.inject(NobilityService);
    api = TestBed.inject(ApiService);
    http = TestBed.inject(HttpTestingController);
    TestBed.flushEffects();
    service.hydrate(snapshot());
  });

  afterEach(() => http.verify());

  it('redeems in one action and blocks duplicate clicks during both HTTP requests', () => {
    service.redeem(1);
    service.redeem(1);
    const request = http.expectOne(api.apiUrl + 'nobility/redemption-preview');
    expect(request.request.method).toBe('POST');
    expect(JSON.parse(request.request.body)).toEqual({ quantity: 1 });
    request.flush(preview);
    expect(service.pendingQuantity()).toBe(1);
    expect(service.error()).toBeNull();
    expect(service.busy()).toBeTrue();

    service.redeem(1);
    const redemption = http.expectOne(api.apiUrl + 'nobility/redeem');
    const body = JSON.parse(redemption.request.body);
    expect(body.unitIds).toEqual(preview.unitIds);
    expect(body.membershipVersion).toBe(preview.membershipVersion);
    expect(body.expectedExpiryDate).toBe('2027-10-11');
    expect(body.operationId).toMatch(/^[0-9a-f-]{36}$/i);
    redemption.flush({ id: body.operationId, unitIds: preview.unitIds, expiresAt: preview.expiresAt });
    const refreshed = { ...snapshot(), isNoble: true, expiresAt: preview.expiresAt, availableSignets: 0 };
    http.expectOne(api.apiUrl + 'nobility').flush(refreshed);
    expect(service.pendingQuantity()).toBeNull();
    expect(service.status()).toEqual(refreshed);
    expect(service.error()).toBeNull();
    expect(service.message()).toContain('Signets redeemed. Nobility expires');
    expect(service.busy()).toBeFalse();
  });

  it('accepts the plain boolean appearance response and refreshes settings', () => {
    service.appearance(false);
    const request = http.expectOne(api.apiUrl + 'nobility/appearance');
    expect(request.request.method).toBe('PUT');
    expect(JSON.parse(request.request.body)).toEqual({ showBadge: false });
    request.flush(true);
    const refreshed = { ...snapshot(), showBadge: false };
    http.expectOne(api.apiUrl + 'nobility').flush(refreshed);
    expect(service.status()).toEqual(refreshed);
    expect(service.error()).toBeNull();
    expect(service.busy()).toBeFalse();
  });

  it('shows the server rejection instead of treating an HTTP failure as a preview', () => {
    service.redeem(1);
    http.expectOne(api.apiUrl + 'nobility/redemption-preview').flush(
      { title: 'Action rejected', detail: 'Not enough unreserved Signets.', status: 400 },
      { status: 400, statusText: 'Bad Request' },
    );
    expect(service.pendingQuantity()).toBeNull();
    expect(service.error()).toBe('Not enough unreserved Signets.');
    expect(service.busy()).toBeFalse();
  });

  it('clears the preview and shows the server conflict when a unit is no longer available', () => {
    service.redeem(1);
    http.expectOne(api.apiUrl + 'nobility/redemption-preview').flush(preview);
    http.expectOne(api.apiUrl + 'nobility/redeem').flush(
      { title: 'Conflict', detail: 'A selected Signet is no longer available.', status: 409 },
      { status: 409, statusText: 'Conflict' },
    );
    expect(service.pendingQuantity()).toBeNull();
    expect(service.message()).toBeNull();
    expect(service.error()).toBe('A selected Signet is no longer available.');
    expect(service.busy()).toBeFalse();
    service.redeem(1);
    http.expectOne(api.apiUrl + 'nobility/redemption-preview').flush(
      { detail: 'Not enough unreserved Signets.' }, { status: 400, statusText: 'Bad Request' });
  });

  it('retries the same receipt after the redemption response is lost', () => {
    service.redeem(1);
    http.expectOne(api.apiUrl + 'nobility/redemption-preview').flush(preview);
    const redemption = http.expectOne(api.apiUrl + 'nobility/redeem');
    const originalBody = redemption.request.body;
    redemption.error(new ProgressEvent('error'));
    expect(service.pendingQuantity()).toBe(1);
    expect(service.busy()).toBeFalse();
    service.hydrate({ ...snapshot(), availableSignets: 0 });
    service.redeem(1);
    http.expectNone(api.apiUrl + 'nobility/redemption-preview');
    const retry = http.expectOne(api.apiUrl + 'nobility/redeem');
    expect(retry.request.body).toBe(originalBody);
    retry.flush({ expiresAt: preview.expiresAt });
    http.expectOne(api.apiUrl + 'nobility').flush({ ...snapshot(), availableSignets: 0 });
    expect(service.pendingQuantity()).toBeNull();
    expect(service.error()).toBeNull();
    expect(service.message()).toContain('Signets redeemed.');
  });

  it('rejects invalid quantities without sending any request', () => {
    for (const quantity of [0, -1, 1.5, NaN, 2, 1201]) service.redeem(quantity);
    http.expectNone(() => true);
    expect(service.busy()).toBeFalse();
    expect(service.pendingQuantity()).toBeNull();
  });
});


