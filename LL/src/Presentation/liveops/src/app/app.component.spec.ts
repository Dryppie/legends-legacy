import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AppComponent } from './app.component';
import { AuditComponent } from './features/audit/audit.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { PlayerWorkspaceComponent } from './features/players/player-workspace.component';
import { LiveOpsApiService } from './liveops-api.service';
import { OperatorContextService } from './operator-context.service';
import { ActionPreviewComponent } from './shared/action-preview/action-preview.component';

describe('LiveOps routed frontend', () => {
  it('loads authentication once in the shared operator shell', async () => {
    const api = {
      session: jasmine.createSpy().and.resolveTo(session()),
      initializeAntiforgery: jasmine.createSpy().and.resolveTo(),
    };
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([]), { provide: LiveOpsApiService, useValue: api }],
    }).compileComponents();

    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.session).toHaveBeenCalledOnceWith();
    expect(api.initializeAntiforgery).toHaveBeenCalledOnceWith();
    expect(fixture.nativeElement.textContent).toContain('Test Operator');
    expect(fixture.nativeElement.querySelectorAll('.primary-nav a').length).toBe(4);
  });

  it('loads operational status inside the dashboard route component', async () => {
    const api = {
      operationalStatus: jasmine.createSpy().and.resolveTo({
        isSuccess: true,
        data: operationalStatus(),
        errorMessage: '',
      }),
    };
    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [provideRouter([]), { provide: LiveOpsApiService, useValue: api }],
    }).compileComponents();

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.operationalStatus).toHaveBeenCalledOnceWith();
    expect(fixture.nativeElement.textContent).toContain('LiveOps status');
    expect(fixture.nativeElement.textContent).toContain('Game database');
    fixture.destroy();
  });

  it('loads the global audit explorer in its route component', async () => {
    const api = {
      audit: jasmine.createSpy().and.resolveTo({
        isSuccess: true,
        data: { entries: [], nextCursor: null, unavailableSources: [] },
        errorMessage: '',
      }),
    };
    await TestBed.configureTestingModule({
      imports: [AuditComponent],
      providers: [
        provideRouter([]),
        { provide: LiveOpsApiService, useValue: api },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({}) } } },
      ],
    }).compileComponents();
    TestBed.inject(OperatorContextService).session = session();

    const fixture = TestBed.createComponent(AuditComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.audit).toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Global audit explorer');
  });

  it('loads a routed player and preserves partial support snapshot results', async () => {
    const characterId = '22222222-2222-2222-2222-222222222222';
    const api = {
      playerDetails: jasmine.createSpy().and.resolveTo({ isSuccess: true, data: playerDetails(characterId), errorMessage: '' }),
      playerSupportSnapshot: jasmine.createSpy().and.resolveTo({ isSuccess: true, data: supportSnapshot(characterId), errorMessage: '' }),
      playerTransferHistory: jasmine.createSpy().and.resolveTo({
        isSuccess: true,
        data: {
          isAvailable: true,
          source: 'Game database',
          fetchedAtUtc: '2026-08-18T10:05:00Z',
          message: null,
          data: {
            historyLimit: 25,
            nextCursor: null,
            entries: [{
              transferId: '66666666-6666-6666-6666-666666666666', direction: 'Incoming', kind: 'InventoryItem',
              senderAccountId: '44444444-4444-4444-4444-444444444444', senderCharacterId: '55555555-5555-5555-5555-555555555555',
              senderCharacterName: 'EmberKnight', recipientAccountId: '11111111-1111-1111-1111-111111111111',
              recipientCharacterId: characterId, recipientCharacterName: 'ArdentFox', assetId: 'item:potion',
              assetName: 'Potion', sourceItemInstanceId: '77777777-7777-7777-7777-777777777777',
              destinationItemInstanceId: '88888888-8888-8888-8888-888888888888', quantity: 2,
              occurredAtUtc: '2026-08-17T10:00:00Z',
              conversation: conversation('OneWayConversation'),
            }],
          },
        },
        errorMessage: '',
      }),
      playerTransferConversation: jasmine.createSpy().and.resolveTo({
        isSuccess: true,
        data: {
          transferId: '33333333-3333-3333-3333-333333333333',
          summary: conversation('EstablishedConversation'),
          messages: [{
            id: '99999999-9999-9999-9999-999999999999',
            senderId: characterId,
            senderName: 'ArdentFox',
            body: 'Send it to EmberKnight.',
            targetCharacterId: '55555555-5555-5555-5555-555555555555',
            targetCharacterName: 'EmberKnight',
            sentAt: '2026-08-18T09:55:00Z',
          }],
          nextCursor: null,
        },
        errorMessage: '',
      }),
    };
    await TestBed.configureTestingModule({
      imports: [PlayerWorkspaceComponent],
      providers: [
        provideRouter([]),
        { provide: LiveOpsApiService, useValue: api },
        { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({ characterId })) } },
      ],
    }).compileComponents();
    TestBed.inject(OperatorContextService).session = session();

    const fixture = TestBed.createComponent(PlayerWorkspaceComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.playerDetails).toHaveBeenCalledWith(characterId);
    expect(api.playerSupportSnapshot).toHaveBeenCalledWith(characterId);
    expect(fixture.nativeElement.textContent).toContain('Player support snapshot');
    expect(fixture.nativeElement.textContent).toContain('Activity timed out for this player.');
    expect(fixture.nativeElement.textContent).toContain('Transfer and wire history');
    expect(fixture.nativeElement.textContent).toContain('500 × Cinders');
    expect(fixture.nativeElement.textContent).toContain('ArdentFox → EmberKnight');
    expect(fixture.nativeElement.textContent).toContain(characterId);

    const conversationButton = fixture.nativeElement.querySelector('.conversation-status') as HTMLButtonElement;
    conversationButton.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(api.playerTransferConversation).toHaveBeenCalledWith(
      characterId,
      '33333333-3333-3333-3333-333333333333',
      null,
      25,
    );
    expect(fixture.nativeElement.textContent).toContain('Send it to EmberKnight.');

    const loadMore = fixture.nativeElement.querySelector('.transfer-pagination button') as HTMLButtonElement;
    loadMore.click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(api.playerTransferHistory).toHaveBeenCalledWith(characterId, 'next-transfer-page', 25);
    expect(fixture.nativeElement.textContent).toContain('2 × Potion');
  });

  it('keeps typed confirmation inside the extracted action preview', async () => {
    await TestBed.configureTestingModule({ imports: [ActionPreviewComponent] }).compileComponents();
    const fixture = TestBed.createComponent(ActionPreviewComponent);
    fixture.componentInstance.session = session('Production');
    fixture.componentInstance.preview = {
      previewToken: 'preview-1', operationId: 'operation-1', actionKind: 'AccountBan',
      title: 'Apply account ban', targetName: 'ArdentFox', targetId: 'account-1',
      riskLevel: 'Permanent', expiresAt: new Date(Date.now() + 60_000).toISOString(),
      confirmationText: 'ArdentFox', fields: [{ label: 'Expiry', value: 'Permanent' }],
      warnings: ['This action is permanent until explicitly revoked.'],
    };
    fixture.detectChanges();

    const confirm = fixture.nativeElement.querySelector('.preview-actions .danger-button') as HTMLButtonElement;
    expect(fixture.nativeElement.textContent).toContain('Server-verified action');
    expect(fixture.nativeElement.textContent).toContain('Test Operator');
    expect(confirm.disabled).toBeTrue();
    fixture.componentInstance.confirmation = 'ArdentFox';
    fixture.detectChanges();
    expect(confirm.disabled).toBeFalse();
  });
});

function session(environment = 'Development') {
  return {
    subject: 'operator-1', displayName: 'Test Operator', permissions: ['liveops.read'],
    environment, isDevelopmentOperator: environment === 'Development',
  };
}

function playerDetails(characterId: string) {
  return {
    player: {
      accountId: '11111111-1111-1111-1111-111111111111', characterId,
      accountLabel: 'account@example.test', email: 'account@example.test', characterName: 'ArdentFox',
      characterLevel: 42, createdUtc: '2026-08-01T10:00:00Z', activeBanId: null,
      activeBanReason: null, activeBanExpiresAt: null,
    },
    activeMute: null, chatAvailable: true, chatStatusMessage: null,
    administrationHistory: [], chatHistory: [],
  };
}

function supportSnapshot(characterId: string) {
  const fetchedAtUtc = '2026-08-18T10:00:00Z';
  const available = <T>(data: T) => ({ isAvailable: true, source: 'Game database', fetchedAtUtc, message: null, data });
  return {
    accountId: '11111111-1111-1111-1111-111111111111', characterId, generatedAtUtc: fetchedAtUtc,
    account: available({ accountCreatedUtc: '2026-08-01T10:00:00Z', lastSessionIssuedUtc: null, activeSessionCount: 0, loginActivityMessage: 'Dedicated login events are not retained.', restrictions: [] }),
    activity: { isAvailable: false, source: 'Game database', fetchedAtUtc, message: 'Activity timed out for this player.', data: null },
    economy: available({ cinders: 25, soulstones: 5, fateEcho: 0, sigilFragments: 0, guildFavor: 0, towerTokens: 0, inventoryRowCount: 0, inventoryQuantity: 0, unseenInventoryRows: 0, recentAcquisitions: [], recentCompensationGrants: [] }),
    guild: available({ isMember: false, guildId: null, guildName: null, guildTag: null, role: null, joinedAtUtc: null, guildLevel: null, memberCount: null }),
    marketplace: available({ activeListingCount: 0, activeBuyOrderCount: 0, recentTrades: [] }),
    transfers: available({
      historyLimit: 25,
      nextCursor: 'next-transfer-page',
      entries: [{
        transferId: '33333333-3333-3333-3333-333333333333', direction: 'Outgoing', kind: 'Cinders',
        senderAccountId: '11111111-1111-1111-1111-111111111111', senderCharacterId: characterId,
        senderCharacterName: 'ArdentFox', recipientAccountId: '44444444-4444-4444-4444-444444444444',
        recipientCharacterId: '55555555-5555-5555-5555-555555555555', recipientCharacterName: 'EmberKnight',
        assetId: 'currency:cinders', assetName: 'Cinders', sourceItemInstanceId: null,
        destinationItemInstanceId: null, quantity: 500, occurredAtUtc: fetchedAtUtc,
        conversation: conversation('EstablishedConversation'),
      }],
    }),
    synchronization: available({ pendingDeliveries: 0, failedDeliveries: 0, oldestPendingAtUtc: null, lastOutboxEventAtUtc: null, revisions: [], pendingRewardMessage: 'No pending-reward registry exists.' }),
  };
}

function conversation(status: 'EstablishedConversation' | 'OneWayConversation' | 'SharedChannelActivity' | 'NoRecordedConversation' | 'ChatUnavailable') {
  return {
    status,
    isAvailable: status !== 'ChatUnavailable',
    message: status === 'ChatUnavailable' ? 'Chat is unavailable.' : null,
    senderToRecipientMessageCount: status === 'EstablishedConversation' ? 2 : status === 'OneWayConversation' ? 1 : 0,
    recipientToSenderMessageCount: status === 'EstablishedConversation' ? 1 : 0,
    immediateMessageCount: status === 'EstablishedConversation' ? 2 : 0,
    firstMessageAt: status === 'EstablishedConversation' ? '2026-08-18T09:50:00Z' : null,
    lastMessageAt: status === 'EstablishedConversation' ? '2026-08-18T09:55:00Z' : null,
    sharedChannelCount: status === 'SharedChannelActivity' ? 1 : 0,
    sharedChannelMessageCount: status === 'SharedChannelActivity' ? 4 : 0,
    windowFrom: '2026-07-19T10:00:00Z',
    windowTo: '2026-08-18T12:00:00Z',
  };
}

function operationalStatus() {
  return {
    overallStatus: 'Healthy', environment: 'Development', serverTimeUtc: new Date().toISOString(),
    build: { releaseVersion: '1.2.3', frontendVersion: '1.2.3', gameVersion: '1.2.3', chatVersion: '1.2.3', commitSha: 'abc123', deployedAtUtc: '2026-08-18T06:00:00Z', processStartedAtUtc: '2026-08-18T06:00:00Z' },
    dependencies: [{ key: 'game_database', name: 'Game database', status: 'Healthy', message: 'Ready.', affectedCapabilities: ['Player lookup'] }],
    outbox: { isAvailable: true, status: 'Healthy', pendingDeliveries: 0, failedDeliveries: 0, oldestPendingAtUtc: null },
    restrictions: { isAvailable: true, expiringWithinSevenDays: 0, nextExpiryAtUtc: null },
    permanentActionsLast24Hours: 0, highValueActionsLast24Hours: 0, recentActions: [], warnings: [],
  };
}
