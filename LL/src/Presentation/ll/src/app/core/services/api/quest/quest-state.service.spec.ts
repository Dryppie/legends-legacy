import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { AuthService } from '../auth/auth.service';
import { QuestStateService } from './quest-state.service';
import { QuestService } from './quest.service';

describe('QuestStateService level-up synchronization', () => {
  it('refreshes area access without redundantly fetching the quest journal', () => {
    const levelUp = signal<any>(null);
    const questJournalChanged = signal<any>(null);
    const api = jasmine.createSpyObj<QuestService>('QuestService', [
      'getAreaAccess',
      'getJournal',
    ]);
    api.getAreaAccess.and.returnValue(of([]));
    api.getJournal.and.returnValue(of({ quests: [] }));

    TestBed.configureTestingModule({
      providers: [
        QuestStateService,
        DomainVersionTracker,
        { provide: QuestService, useValue: api },
        { provide: Router, useValue: { navigateByUrl: () => undefined } },
        {
          provide: GameRealtimeEventRegistry,
          useValue: {
            event: { QuestJournalChanged: questJournalChanged.asReadonly() },
            eventEnvelope: { CharacterLevelUp: levelUp.asReadonly() },
          },
        },
        { provide: EventBusService, useValue: { logout: signal(0) } },
        {
          provide: StateSyncCoordinator,
          useValue: {
            register: jasmine.createSpy('register'),
            activate: jasmine.createSpy('activate'),
          },
        },
        {
          provide: AuthService,
          useValue: { currentCharacter: signal({ id: 'character-id' }) },
        },
      ],
    });
    TestBed.inject(QuestStateService);

    levelUp.set({
      updateId: 'level-up-1',
      event: 'CharacterLevelUp',
      payload: { characterId: 'character-id', level: 2 },
    });
    TestBed.flushEffects();

    expect(api.getAreaAccess).toHaveBeenCalledTimes(1);
    expect(api.getJournal).not.toHaveBeenCalled();
  });
});
