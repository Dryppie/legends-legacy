import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HarnessLoader, TestKey } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgObjectiveHarness } from '../../testing/objective.harness';
import {
  LgObjectiveComponent,
  LgObjectivePanelComponent,
} from './objective.component';

// The parity case i-objective: a pinned quest with a tracker, and a plain button beside it. The tracker also holds a
// button here, so that focus can leave the summary and be seen to come back.
@Component({
  imports: [LgObjectiveComponent, LgObjectivePanelComponent],
  template: `
    <div>
      <lg-objective
        kicker="Quest"
        heading="The First Hunt"
        objective="Defeat wolves"
        [current]="3"
        [required]="5"
        [(open)]="open"
      >
        <lg-objective-panel>
          <p>Tracker</p>
          <button type="button" class="in-panel">Abandon</button>
        </lg-objective-panel>
      </lg-objective>
      <button type="button" class="away">Away</button>
    </div>
  `,
})
class ObjectiveHost {
  readonly open = signal(false);
}

/** A real pointer press on the host's own markup: pointerdown, focus, then click. */
function pointerPress(el: HTMLElement): void {
  el.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true }));
  el.focus();
  el.click();
}

describe('LgObjectiveComponent', () => {
  let fixture: ComponentFixture<ObjectiveHost>;
  let loader: HarnessLoader;
  let objective: LgObjectiveHarness;

  beforeEach(async () => {
    fixture = TestBed.createComponent(ObjectiveHost);
    loader = TestbedHarnessEnvironment.loader(fixture);
    objective = await loader.getHarness(
      LgObjectiveHarness.with({ heading: 'The First Hunt' }),
    );
  });

  afterEach(() => fixture.destroy());

  const away = () =>
    fixture.nativeElement.querySelector('button.away') as HTMLElement;
  /** The tracker's button, on the overlay while it is open. */
  const inTracker = () =>
    document.querySelector('.lg-popover button.in-panel') as HTMLElement;

  it('shows the quest, its objective and the count, as a closed disclosure', async () => {
    expect(await objective.getHeading()).toBe('The First Hunt');
    expect(await objective.getObjective()).toBe('Defeat wolves');
    expect(await objective.getCount()).toBe('3 / 5');
    expect(await objective.isDisclosure()).toBeTrue();
    expect(await objective.isExpanded()).toBeFalse();
    expect(await objective.isOpen()).toBeFalse();
  });

  it('a press opens the tracker beneath it, and focus moves into it', async () => {
    await objective.press();

    expect(await objective.isOpen()).toBeTrue();
    expect(await objective.isExpanded()).toBeTrue();
    expect(await objective.getPanelLabel()).toBe('The First Hunt');
    expect(fixture.componentInstance.open()).toBeTrue();
    expect(document.activeElement?.closest('.lg-popover')).toBeTruthy();
  });

  it('Escape closes the tracker and leaves focus on the summary', async () => {
    await objective.press();
    await objective.pressKey(TestKey.ESCAPE);

    expect(await objective.isOpen()).toBeFalse();
    expect(await objective.isExpanded()).toBeFalse();
    expect(fixture.componentInstance.open()).toBeFalse();
    expect(await objective.isFocused()).toBeTrue();
  });

  it('Escape from inside the tracker returns focus to the summary', async () => {
    await objective.press();
    const inPanel = inTracker();
    inPanel.focus();
    inPanel.dispatchEvent(
      new KeyboardEvent('keydown', {
        key: 'Escape',
        bubbles: true,
        cancelable: true,
      }),
    );
    await fixture.whenStable();

    expect(await objective.isOpen()).toBeFalse();
    expect(await objective.isFocused()).toBeTrue();
  });

  it('an Escape something above the tracker took first leaves it open (the tip, an overlay)', async () => {
    await objective.press();
    const inPanel = inTracker();
    inPanel.focus();
    // What the tip and the CDK overlays do with a key they use: mark it taken.
    inPanel.addEventListener('keydown', (e) => e.preventDefault(), {
      once: true,
    });
    inPanel.dispatchEvent(
      new KeyboardEvent('keydown', {
        key: 'Escape',
        bubbles: true,
        cancelable: true,
      }),
    );
    await fixture.whenStable();

    expect(await objective.isOpen()).toBeTrue();
    expect(document.activeElement).toBe(inPanel);
  });

  it('the summary pressed again reopens it after Escape', async () => {
    await objective.press();
    await objective.pressKey(TestKey.ESCAPE);
    await objective.press();

    expect(await objective.isOpen()).toBeTrue();
    expect(await objective.isExpanded()).toBeTrue();
  });

  it('a press on the summary while open closes it', async () => {
    await objective.press();
    await objective.press();

    expect(await objective.isOpen()).toBeFalse();
    expect(await objective.isExpanded()).toBeFalse();
    expect(await objective.isFocused()).toBeTrue();
  });

  it('a press inside the tracker keeps it open', async () => {
    await objective.press();
    pointerPress(inTracker());
    await fixture.whenStable();

    expect(await objective.isOpen()).toBeTrue();
  });

  it('Tab past the end of the tracker closes it and goes back to the summary', async () => {
    await objective.press();
    const inPanel = inTracker();
    inPanel.focus();
    inPanel.dispatchEvent(
      new KeyboardEvent('keydown', {
        key: 'Tab',
        bubbles: true,
        cancelable: true,
      }),
    );
    await fixture.whenStable();

    expect(await objective.isOpen()).toBeFalse();
    expect(await objective.isFocused()).toBeTrue();
  });

  it('a click outside closes it and leaves focus where the player clicked', async () => {
    await objective.press();
    pointerPress(away());
    await fixture.whenStable();

    expect(await objective.isOpen()).toBeFalse();
    expect(await objective.isExpanded()).toBeFalse();
    expect(fixture.componentInstance.open()).toBeFalse();
    expect(document.activeElement).toBe(away());
  });
});
