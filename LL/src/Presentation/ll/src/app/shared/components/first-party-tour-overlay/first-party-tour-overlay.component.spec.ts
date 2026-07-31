import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FirstPartyTourService } from '../../../core/services/client-side/first-party-tour/first-party-tour.service';
import { FirstPartyTourViewState } from '../../../core/services/client-side/first-party-tour/first-party-tour.models';
import { FirstPartyTourOverlayComponent } from './first-party-tour-overlay.component';

describe('FirstPartyTourOverlayComponent', () => {
  let component: FirstPartyTourOverlayComponent;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [FirstPartyTourOverlayComponent],
      providers: [
        {
          provide: FirstPartyTourService,
          useValue: {
            state: signal(null).asReadonly(),
          },
        },
      ],
    });

    component = TestBed.createComponent(
      FirstPartyTourOverlayComponent,
    ).componentInstance;
  });

  it('shows Back for a tutorial step that opts in', () => {
    expect(component.showBackButton(viewState('tutorial-crafting', true))).toBe(
      true,
    );
  });

  it('keeps Back hidden for tutorial steps that do not opt in', () => {
    expect(component.showBackButton(viewState('tutorial-crafting'))).toBe(
      false,
    );
  });
});

function viewState(
  pageId: string,
  showBack?: boolean,
): FirstPartyTourViewState {
  return {
    pageId,
    step: {
      id: 'test-step',
      kind: 'info',
      element: '[data-tour=test]',
      description: 'Test step',
      position: 'bottom',
      alignment: 'center',
      showBack,
    },
    stepIndex: 1,
    stepCount: 2,
    targetRect: null,
    canGoBack: true,
    canGoNext: false,
    canFinish: false,
    blocksInteraction: false,
    instruction: null,
  };
}
