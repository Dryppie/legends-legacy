import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { TutorialPresenterService } from '../../../../core/services/api/tutorial/tutorial-presenter.service';
import { TutorialStateService } from '../../../../core/services/api/tutorial/tutorial-state.service';
import {
  TUTORIAL_STEP_EQUIP_GATHERING_TOOL,
  TutorialState,
} from '../../../../shared/models/tutorial';
import { InventoryComponent } from './inventory.component';

describe('InventoryComponent tutorial presentation', () => {
  it('starts gathering-tool guidance when that step begins in Inventory', () => {
    const tutorial = signal<TutorialState | null>(
      tutorialState(TUTORIAL_STEP_EQUIP_GATHERING_TOOL),
    );
    const presenter = jasmine.createSpyObj<TutorialPresenterService>(
      'TutorialPresenterService',
      ['presentCurrentStep'],
    );

    TestBed.configureTestingModule({});
    TestBed.runInInjectionContext(
      () =>
        new InventoryComponent(
          {} as InventoryStateService,
          { state: tutorial.asReadonly() } as TutorialStateService,
          presenter,
        ),
    );
    TestBed.flushEffects();

    expect(presenter.presentCurrentStep).toHaveBeenCalledOnceWith();
  });
});

function tutorialState(currentStep: string): TutorialState {
  return {
    tutorialId: 'tutorial.first_steps',
    title: 'First Steps',
    version: 3,
    currentStep,
    objective: 'Equip a gathering tool.',
    currentAmount: 0,
    requiredAmount: 1,
    currentStepIndex: 6,
    totalSteps: 7,
    actionLabel: 'Head to Inventory',
    destinationRoute: '/game/character/inventory',
    tourPageId: 'tutorial-gathering-tool',
    requiresWelcome: false,
    isCompleted: false,
  };
}
