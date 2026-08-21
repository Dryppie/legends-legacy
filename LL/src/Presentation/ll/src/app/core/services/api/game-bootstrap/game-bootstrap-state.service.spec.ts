import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AuthService } from '../auth/auth.service';
import { CharacterActionsStateService } from '../character-actions/character-actions.state.service';
import { QuestStateService } from '../quest/quest-state.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { TimeSyncService } from '../time-sync/time-sync.service';
import { GameBootstrapDto, GameBootstrapService } from './game-bootstrap.service';
import { GameBootstrapStateService } from './game-bootstrap-state.service';

describe('GameBootstrapStateService', () => {
  it('does not hydrate character or quests from a stale bootstrap response', () => {
    const auth = {
      isAuthenticated: () => true,
      currentCharacter: signal(null),
      updateCharacter: jasmine.createSpy('updateCharacter'),
    };
    const quests = {
      initialize: jasmine.createSpy('initialize'),
      initializeAreaAccess: jasmine.createSpy('initializeAreaAccess'),
    };
    const actions = {
      initializeFromBootstrap: jasmine.createSpy('initializeFromBootstrap'),
    };
    const stateSync = jasmine.createSpyObj<StateSyncCoordinator>(
      'StateSyncCoordinator',
      ['acceptSnapshotResponse'],
    );

    TestBed.configureTestingModule({
      providers: [
        GameBootstrapStateService,
        DomainVersionTracker,
        { provide: GameBootstrapService, useValue: {} },
        { provide: AuthService, useValue: auth },
        { provide: QuestStateService, useValue: quests },
        { provide: CharacterActionsStateService, useValue: actions },
        {
          provide: GameRealtimeEventRegistry,
          useValue: { event: { AccountAccessChanged: signal(null) } },
        },
        { provide: TimeSyncService, useValue: { updateFromServerTime: () => undefined } },
        { provide: StateSyncCoordinator, useValue: stateSync },
      ],
    });
    const versions = TestBed.inject(DomainVersionTracker);
    versions.observe({ character: 5, quests: 7, 'area-access': 9 });
    const service = TestBed.inject(GameBootstrapStateService);
    const bootstrap = {
      character: { id: 'character-id' },
      questJournal: { quests: [] },
      areaAccess: [],
      currentAction: null,
      serverTimeUtc: '2026-08-21T12:00:00Z',
      attributeDefinitions: [],
      stateVersions: { character: 4, quests: 6, 'area-access': 8 },
    } as unknown as GameBootstrapDto;

    (service as unknown as { hydrate(value: GameBootstrapDto): void }).hydrate(
      bootstrap,
    );

    expect(auth.updateCharacter).not.toHaveBeenCalled();
    expect(quests.initialize).not.toHaveBeenCalled();
    expect(quests.initializeAreaAccess).not.toHaveBeenCalled();
    expect(stateSync.acceptSnapshotResponse).toHaveBeenCalledOnceWith(
      bootstrap.stateVersions,
      [],
    );
  });

  it('hydrates area access from bootstrap without a follow-up request', () => {
    const auth = {
      isAuthenticated: () => true,
      currentCharacter: signal(null),
      updateCharacter: jasmine.createSpy('updateCharacter'),
    };
    const quests = {
      initialize: jasmine.createSpy('initialize'),
      initializeAreaAccess: jasmine.createSpy('initializeAreaAccess'),
    };
    const stateSync = jasmine.createSpyObj<StateSyncCoordinator>(
      'StateSyncCoordinator',
      ['acceptSnapshotResponse'],
    );

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        GameBootstrapStateService,
        DomainVersionTracker,
        { provide: GameBootstrapService, useValue: {} },
        { provide: AuthService, useValue: auth },
        { provide: QuestStateService, useValue: quests },
        {
          provide: CharacterActionsStateService,
          useValue: { initializeFromBootstrap: jasmine.createSpy() },
        },
        {
          provide: GameRealtimeEventRegistry,
          useValue: { event: { AccountAccessChanged: signal(null) } },
        },
        { provide: TimeSyncService, useValue: { updateFromServerTime: () => undefined } },
        { provide: StateSyncCoordinator, useValue: stateSync },
      ],
    });

    const service = TestBed.inject(GameBootstrapStateService);
    const areaAccess = [{ areaId: 'region_01_area_01', canAccess: true }];
    const bootstrap = {
      character: { id: 'character-id' },
      questJournal: { quests: [] },
      areaAccess,
      currentAction: null,
      serverTimeUtc: '2026-08-21T12:00:00Z',
      attributeDefinitions: [],
      stateVersions: { character: 1, quests: 1, 'area-access': 1 },
    } as unknown as GameBootstrapDto;

    (service as unknown as { hydrate(value: GameBootstrapDto): void }).hydrate(
      bootstrap,
    );

    expect(quests.initializeAreaAccess).toHaveBeenCalledOnceWith(areaAccess);
    expect(stateSync.acceptSnapshotResponse).toHaveBeenCalledOnceWith(
      bootstrap.stateVersions,
      ['character', 'quests', 'area-access'],
    );
  });
});
