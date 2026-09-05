import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FirstPartyTourService } from '../../../core/services/client-side/first-party-tour/first-party-tour.service';
import { FirstPartyTourViewState } from '../../../core/services/client-side/first-party-tour/first-party-tour.models';
import { FirstPartyTourOverlayComponent } from './first-party-tour-overlay.component';

describe('FirstPartyTourOverlayComponent', () => {
  let component: FirstPartyTourOverlayComponent;
  let fixture: ComponentFixture<FirstPartyTourOverlayComponent>;
  const tourState = signal<FirstPartyTourViewState | null>(null);
  const stop = jasmine.createSpy('stop');

  beforeEach(() => {
    tourState.set(null);
    stop.calls.reset();
    TestBed.configureTestingModule({
      imports: [FirstPartyTourOverlayComponent],
      providers: [
        {
          provide: FirstPartyTourService,
          useValue: {
            state: tourState.asReadonly(),
            stop,
          },
        },
      ],
    });

    fixture = TestBed.createComponent(FirstPartyTourOverlayComponent);
    component = fixture.componentInstance;
  });

  it('keeps the archive clickable when a tutorial equip target is missing', () => {
    const tour = viewState('tutorial-essence-loadout');
    tour.step.kind = 'click';
    tourState.set(tour);
    fixture.detectChanges();

    const backdrop = fixture.nativeElement.querySelector(
      '.first-party-tour-backdrop',
    ) as HTMLElement;
    expect(backdrop.style.pointerEvents).toBe('none');
    expect(fixture.nativeElement.querySelector('.first-party-tour-highlight')).toBeNull();
  });

  it('does not block the whole screen when a blocking guide loses its target', () => {
    const tour = viewState('essences');
    tour.blocksInteraction = true;
    tourState.set(tour);
    fixture.detectChanges();

    const backdrop = fixture.nativeElement.querySelector(
      '.first-party-tour-backdrop',
    ) as HTMLElement;
    expect(backdrop.style.pointerEvents).toBe('none');
  });

  it('lets the player close a stuck tutorial without marking it completed', () => {
    tourState.set(viewState('tutorial-essence-loadout'));
    fixture.detectChanges();

    const close = fixture.nativeElement.querySelector(
      'button[aria-label="Close guidance"]',
    ) as HTMLButtonElement;
    close.click();

    expect(stop).toHaveBeenCalledOnceWith(false);
  });

  it('keeps archive and slot controls usable around a highlighted tutorial action', () => {
    const tour = viewState('tutorial-essence-loadout');
    tour.targetRect = { top: 100, left: 100, bottom: 140, right: 200, width: 100, height: 40 };
    tourState.set(tour);
    fixture.detectChanges();

    const backdrops = Array.from(fixture.nativeElement.querySelectorAll(
      '.first-party-tour-backdrop',
    )) as HTMLElement[];
    expect(backdrops.length).toBe(4);
    expect(backdrops.every((backdrop) => backdrop.style.pointerEvents === 'none')).toBeTrue();
  });

  it('shows Back for a tutorial step that opts in', () => {
    expect(component.showBackButton(viewState('tutorial-inventory', true))).toBe(
      true,
    );
  });

  it('keeps Back hidden for tutorial steps that do not opt in', () => {
    expect(component.showBackButton(viewState('tutorial-inventory'))).toBe(
      false,
    );
  });

  it('positions a mobile tutorial box clear of the required action', () => {
    const action = document.createElement('button');
    action.dataset['tour'] = 'required-action';
    spyOn(action, 'getBoundingClientRect').and.returnValue({
      top: 100,
      right: 200,
      bottom: 140,
      left: 100,
      width: 100,
      height: 40,
      x: 100,
      y: 100,
      toJSON: () => ({}),
    });
    document.body.appendChild(action);
    spyOnProperty(window, 'innerWidth').and.returnValue(390);
    spyOnProperty(window, 'innerHeight').and.returnValue(800);

    const tour = viewState('tutorial-essence-loadout');
    tour.step.kind = 'click';
    tour.step.actionSelector = '[data-tour=required-action]';

    expect(component.popoverStyle(tour)['top']).toBe('152px');

    action.remove();
  });

  it('positions a mobile tutorial box clear of every matching action', () => {
    const firstAction = document.createElement('button');
    const lastAction = document.createElement('button');
    firstAction.dataset['tour'] = 'recipe-action';
    lastAction.dataset['tour'] = 'recipe-action';
    spyOn(firstAction, 'getBoundingClientRect').and.returnValue({
      top: 300,
      right: 360,
      bottom: 340,
      left: 30,
      width: 330,
      height: 40,
      x: 30,
      y: 300,
      toJSON: () => ({}),
    });
    spyOn(lastAction, 'getBoundingClientRect').and.returnValue({
      top: 600,
      right: 360,
      bottom: 640,
      left: 30,
      width: 330,
      height: 40,
      x: 30,
      y: 600,
      toJSON: () => ({}),
    });
    document.body.append(firstAction, lastAction);
    spyOnProperty(window, 'innerWidth').and.returnValue(390);
    spyOnProperty(window, 'innerHeight').and.returnValue(800);

    const tour = viewState('tutorial-inventory');
    tour.step.kind = 'click';
    tour.step.actionSelector = '[data-tour=recipe-action]';

    const style = component.popoverStyle(tour);
    expect(style['top']).toBe('auto');
    expect(style['bottom']).toBe('512px');

    firstAction.remove();
    lastAction.remove();
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
