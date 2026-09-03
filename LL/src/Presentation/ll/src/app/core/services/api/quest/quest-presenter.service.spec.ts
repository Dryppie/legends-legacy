import { signal } from '@angular/core';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import {
  QuestObjectiveState,
  QuestState,
  QuestStatus,
} from '../../../../shared/models/quest';
import { FirstPartyTourService } from '../../client-side/first-party-tour/first-party-tour.service';
import { QuestStateService } from './quest-state.service';
import { QuestPresenterService } from './quest-presenter.service';

describe('QuestPresenterService equipment onboarding', () => {
  const objective: QuestObjectiveState = {
    key: 'win',
    description: 'Win in Lumo',
    type: 'CombatEncounterCompleted',
    currentAmount: 0,
    requiredAmount: 1,
    isCompleted: false,
    presentation: {
      actionLabel: 'Battle',
      destinationRoute: '/game/world/shenic?area=region_01_area_01',
      tourPageId: 'tutorial-lumo-ruins',
    },
  };
  const quest = signal<QuestState | null>(null);
  const tour = { start: jasmine.createSpy('start').and.resolveTo() };

  function start() {
    quest.set({
      questId: 'quest.region01.into_lumo_ruins',
      version: 2,
      title: 'Into the Ruins',
      summary: '',
      category: 'Tutorial',
      objectiveMode: 'Sequential',
      sortOrder: 1,
      status: QuestStatus.Active,
      isPinned: true,
      requiresWelcome: false,
      objectives: [objective],
      rewards: [],
    });
    TestBed.configureTestingModule({
      providers: [
        {
          provide: QuestStateService,
          useValue: {
            pinnedQuest: quest,
            pinnedObjective: () => quest()?.objectives[0],
          },
        },
        { provide: FirstPartyTourService, useValue: tour },
        {
          provide: Router,
          useValue: { url: '/game/world/shenic', events: new Subject() },
        },
      ],
    });
    TestBed.inject(QuestPresenterService);
    TestBed.flushEffects();
  }

  beforeEach(() => tour.start.calls.reset());

  it('launches the walkthrough authored by the current Lumo quest', fakeAsync(() => {
    start();
    tick();
    expect(tour.start).toHaveBeenCalledOnceWith('tutorial-lumo-ruins');
  }));

  it('does not launch a queued walkthrough after its quest is unpinned', fakeAsync(() => {
    start();
    quest.set(null);
    tick();
    expect(tour.start).not.toHaveBeenCalled();
  }));
});
