import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { TutorialState } from '../../../../shared/models/tutorial';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { FirstPartyTourService } from '../../client-side/first-party-tour/first-party-tour.service';
import { GameEventService } from '../../real-time/game-event.service';
import { TutorialService } from './tutorial.service';
import { TutorialStateService } from './tutorial-state.service';

describe('TutorialStateService', () => {
  let service: TutorialStateService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        TutorialStateService,
        {
          provide: TutorialService,
          useValue: jasmine.createSpyObj<TutorialService>('TutorialService', [
            'getState',
            'acknowledgeWelcome',
            'skip',
          ]),
        },
        {
          provide: Router,
          useValue: jasmine.createSpyObj<Router>('Router', ['navigateByUrl']),
        },
        {
          provide: FirstPartyTourService,
          useValue: jasmine.createSpyObj<FirstPartyTourService>(
            'FirstPartyTourService',
            ['stop'],
          ),
        },
        {
          provide: GameEventService,
          useValue: {
            event: {
              TutorialProgressedMsg: signal(null),
              TutorialCompletedMsg: signal(null),
            },
          },
        },
        {
          provide: EventBusService,
          useValue: {
            logout: signal(0),
          },
        },
      ],
    });

    service = TestBed.inject(TutorialStateService);
  });

  it('ignores a stale response that would regress the current step', () => {
    service.initialize(tutorialState('equip_essence', 3));
    service.initialize(tutorialState('absorb_essence', 2));

    expect(service.state()?.currentStep).toBe('equip_essence');
    expect(service.state()?.currentStepIndex).toBe(3);
  });

  it('accepts a forward tutorial step', () => {
    service.initialize(tutorialState('absorb_essence', 2));
    service.initialize(tutorialState('equip_essence', 3));

    expect(service.state()?.currentStep).toBe('equip_essence');
    expect(service.state()?.currentStepIndex).toBe(3);
  });
});

function tutorialState(
  currentStep: string,
  currentStepIndex: number,
): TutorialState {
  return {
    tutorialId: 'tutorial.first_steps',
    title: 'First Steps',
    version: 1,
    currentStep,
    objective: 'Test objective',
    currentAmount: 0,
    requiredAmount: 1,
    currentStepIndex,
    totalSteps: 6,
    presentation: {
      actionLabel: 'Continue',
      destinationRoute: '/game/character/essences',
      tourPageId: 'tutorial-test',
    },
    actionLabel: 'Continue',
    destinationRoute: '/game/character/essences',
    tourPageId: 'tutorial-test',
    requiresWelcome: false,
    isCompleted: false,
  };
}
